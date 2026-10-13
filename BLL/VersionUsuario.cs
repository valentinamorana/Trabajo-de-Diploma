using Seguridad;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Control de Cambios de Usuarios — orquesta el patrón MEMENTO.
    ///
    ///   Originator → BE.Usuario        (crea y restaura su propio estado)
    ///   Memento    → BE.VersionUsuario (cápsula del estado, implementa IMemento)
    ///   Caretaker  → BLL.CuidadorHistorial (guarda/recupera el historial)
    ///
    /// Esta clase es la fachada que usan la GUI y la BLL: delega la captura del
    /// estado en el Originator y su almacenamiento en el Caretaker.
    /// </summary>
    public class VersionUsuario
    {
        private readonly DAL.VersionUsuario  _dalVersion = new DAL.VersionUsuario();
        private readonly DAL.Interfaces.IUsuarioDAL _dalUsuario;
        private readonly Servicios.IRegistroBitacora  _bitacora   = Servicios.FabricaBitacora.CrearSistema();

        // Operaciones del Caretaker (recuperar / guardar un Memento). Por defecto, el
        // CuidadorHistorial real; reemplazables en las pruebas unitarias.
        private readonly System.Func<int, BE.Memento.IMemento>   _obtenerVersion;
        private readonly System.Action<int, BE.Memento.IMemento> _guardarVersion;

        public VersionUsuario()
        {
            var caretaker   = new CuidadorHistorial();
            _dalUsuario     = new DAL.Usuario();
            _obtenerVersion = caretaker.Obtener;
            _guardarVersion = caretaker.Guardar;
        }

        // Para tests: Usuario inyectable y Caretaker en memoria.
        internal VersionUsuario(DAL.Interfaces.IUsuarioDAL dalUsuario,
                                System.Func<int, BE.Memento.IMemento> obtenerVersion,
                                System.Action<int, BE.Memento.IMemento> guardarVersion)
        {
            _dalUsuario     = dalUsuario;
            _obtenerVersion = obtenerVersion;
            _guardarVersion = guardarVersion;
        }

        /// <summary>
        /// Captura el estado actual del usuario (Originator) en un Memento y lo guarda
        /// en el historial (Caretaker). Debe llamarse ANTES de una operación destructiva.
        /// FAIL-SAFE: si el Caretaker no puede guardar, lanza AppException y aborta.
        /// Interno: solo lo invocan las operaciones de la BLL que ya validaron el permiso
        /// (ABM de usuarios, gestión de perfiles); no es una escritura expuesta a la GUI.
        /// </summary>
        internal void GrabarVersion(int idUsuario, string actor, string detalle)
        {
            BE.Usuario originator = _dalUsuario.ObtenerPorId(idUsuario);
            if (originator == null) return;

            BE.Memento.IMemento memento = originator.CrearMemento(actor, detalle);
            _guardarVersion(idUsuario, memento);
        }

        public List<BE.VersionUsuario> ObtenerPorUsuario(int idUsuario)
        {
            return _dalVersion.ObtenerPorUsuario(idUsuario);
        }

        /// <summary>
        /// Restaura un usuario al estado de una versión histórica (deshacer).
        /// Antes de restaurar, guarda un Memento del estado actual (para poder deshacer
        /// la propia restauración — "rollback de un rollback"). Solo Administrador.
        /// </summary>
        public void RestaurarVersion(string modulo, int idVersion)
        {
            BLLHelper.ExigirAdministrador("err.bll.version.sin_permiso",
                "Solo un Administrador puede restaurar una versión histórica de un usuario.");

            // Memento elegido (el Caretaker lo recupera como abstracción IMemento).
            var memento = _obtenerVersion(idVersion) as BE.VersionUsuario;
            if (memento == null)
                throw new BE.AppException("err.bll.version.no_existe",
                    "La versión seleccionada no existe.");

            string actor = SessionManager.GetInstance().Usuario.Username;

            // Originator en su estado ACTUAL.
            BE.Usuario originator = _dalUsuario.ObtenerPorId(memento.IdUsuario);
            if (originator == null)
                throw new BE.AppException("err.bll.usuario.no_existe",
                    "El usuario de la versión ya no existe.");

            // El nombre de usuario de la versión puede haber pasado a otro usuario (activo o
            // archivado: el UNIQUE(Username) es global). Se avisa con un mensaje claro en vez de
            // dejar que el UPDATE choque con UQ_Usuario_Username.
            if (UsernameUsadoPorOtro(memento.UsernameSnapshot, memento.IdUsuario))
                throw new BE.AppException("err.bll.version.username_en_uso",
                    "No se puede restaurar esta versión: el nombre de usuario '{0}' ya lo usa otro usuario.",
                    memento.UsernameSnapshot);

            // Detalle del cambio que produce el rollback ("campo: 'actual' → 'restaurado'").
            string diff    = DescribirRestauracion(originator, memento);
            string detalle = diff == null
                ? $"Restauración a versión ID {idVersion}: sin cambios de datos."
                : $"Restauración a versión ID {idVersion} — {diff}";

            // 1) Aplicar la restauración: el Originator toma el estado de la versión elegida y se
            //    persiste (UPDATE a la tabla Usuario).
            originator.RestaurarDesde(memento);
            _dalUsuario.RestaurarVersion(memento);

            // 2) Snapshot del NUEVO estado (= valores restaurados), en el MISMO orden que una
            //    edición (Modificar: primero UPDATE, después snapshot). Así el último snapshot
            //    SIEMPRE refleja el estado vigente del usuario y el Historial muestra el rollback
            //    como un cambio más. El estado previo al rollback ya quedó como snapshot anterior,
            //    de modo que el propio rollback también se puede deshacer.
            _guardarVersion(memento.IdUsuario, originator.CrearMemento(actor, detalle));

            _bitacora.Registrar(modulo,
                $"{BE.ActividadesBitacora.RestauracionAVersionPrefijo}ID {idVersion} — usuario ID {memento.IdUsuario} ({memento.UsernameSnapshot})",
                BE.Criticidad.Alta);
        }

        // ¿Algún OTRO usuario (activo o archivado) tiene ya ese nombre? Misma comparación sin
        // distinguir mayúsculas que la validación del ABM (la intercalación de la base tampoco).
        private bool UsernameUsadoPorOtro(string username, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            var otros = new List<BE.Usuario>(_dalUsuario.ObtenerTodos());
            otros.AddRange(_dalUsuario.ObtenerArchivados());
            return otros.Exists(u => u.Id != idUsuario &&
                string.Equals((u.Username ?? "").Trim(), username.Trim(), System.StringComparison.OrdinalIgnoreCase));
        }

        // Describe el cambio que produce un rollback comparando el estado ACTUAL (antes) con el
        // estado DESTINO de la versión a restaurar (después). Mismo formato "campo: 'a' → 'b'; ..."
        // que usa una edición normal, así el historial muestra QUÉ cambió el rollback (no solo que
        // ocurrió). Solo datos administrativos no sensibles. Devuelve null si no cambia nada.
        private static string DescribirRestauracion(BE.Usuario actual, BE.VersionUsuario destino)
        {
            var partes = new System.Collections.Generic.List<string>();
            void Cmp(string campo, string viejo, string nuevo)
            {
                if (!string.Equals(viejo ?? "", nuevo ?? "", System.StringComparison.Ordinal))
                    partes.Add($"{campo}: '{viejo ?? ""}' → '{nuevo ?? ""}'");
            }
            Cmp("Usuario",  actual.Username, destino.UsernameSnapshot);
            Cmp("Nombre",   actual.Nombre,   destino.NombreSnapshot);
            Cmp("Apellido", actual.Apellido, destino.ApellidoSnapshot);
            Cmp("Email",    actual.Email,    destino.EmailSnapshot);

            string fechaVieja = actual.FechaNacimiento?.ToString("yyyy-MM-dd") ?? "";
            string fechaNueva = destino.FechaNacSnapshot?.ToString("yyyy-MM-dd") ?? "";
            if (!string.Equals(fechaVieja, fechaNueva, System.StringComparison.Ordinal))
                partes.Add($"Fecha nac.: '{fechaVieja}' → '{fechaNueva}'");

            return partes.Count == 0 ? null : string.Join("; ", partes);
        }
    }
}
