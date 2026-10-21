using Seguridad;
using Servicios.Serializacion;
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
        // Historial completo de un usuario (lectura). Reemplazable en las pruebas.
        private readonly System.Func<int, List<BE.VersionUsuario>> _obtenerHistorial;

        public VersionUsuario()
        {
            var caretaker     = new CuidadorHistorial();
            _dalUsuario       = new DAL.Usuario();
            _obtenerVersion   = caretaker.Obtener;
            _guardarVersion   = caretaker.Guardar;
            _obtenerHistorial = _dalVersion.ObtenerPorUsuario;
        }

        // Para tests: Usuario inyectable y Caretaker en memoria.
        internal VersionUsuario(DAL.Interfaces.IUsuarioDAL dalUsuario,
                                System.Func<int, BE.Memento.IMemento> obtenerVersion,
                                System.Action<int, BE.Memento.IMemento> guardarVersion)
            : this(dalUsuario, obtenerVersion, guardarVersion, null) { }

        // Para tests de exportación/importación: también el historial por usuario en memoria.
        internal VersionUsuario(DAL.Interfaces.IUsuarioDAL dalUsuario,
                                System.Func<int, BE.Memento.IMemento> obtenerVersion,
                                System.Action<int, BE.Memento.IMemento> guardarVersion,
                                System.Func<int, List<BE.VersionUsuario>> obtenerHistorial)
        {
            _dalUsuario       = dalUsuario;
            _obtenerVersion   = obtenerVersion;
            _guardarVersion   = guardarVersion;
            _obtenerHistorial = obtenerHistorial
                ?? new System.Func<int, List<BE.VersionUsuario>>(_dalVersion.ObtenerPorUsuario);
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
            return _obtenerHistorial(idUsuario);
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

            Restaurar(modulo, memento, $"ID {idVersion}");
        }

        // Núcleo COMPARTIDO de la restauración (versión del historial de la base o versión importada
        // de un archivo XML o JSON): mismas validaciones, mismo UPDATE con DV, mismo snapshot posterior y
        // misma entrada de bitácora. "etiqueta" identifica la versión en el detalle y en la bitácora
        // ("ID 12" o "importada ID 12 (archivo XML ...)" / "(archivo JSON ...)"); ambos textos empiezan igual
        // ("Restauración a versión ...") para que el historial y el tablero los reconozcan.
        private void Restaurar(string modulo, BE.VersionUsuario memento, string etiqueta)
        {
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
                ? $"Restauración a versión {etiqueta}: sin cambios de datos."
                : $"Restauración a versión {etiqueta} — {diff}";

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
                $"{BE.ActividadesBitacora.RestauracionAVersionPrefijo}{etiqueta} — usuario ID {memento.IdUsuario} ({memento.UsernameSnapshot})",
                BE.Criticidad.Alta);
        }

        // ══ A02 Serialización — exportar / importar las versiones (Mementos) a XML o JSON ═════════

        /// <summary>Tamaño máximo de un archivo de versiones que se acepta importar (5 MB).</summary>
        public const long TamanoMaximoArchivo = 5L * 1024 * 1024;

        // Largos de las columnas de la tabla Usuario: un archivo editado a mano no puede llevar
        // valores que después choquen con el UPDATE de la restauración.
        private const int LargoMaxUsername = 100, LargoMaxNombre = 100, LargoMaxApellido = 100, LargoMaxEmail = 200;

        /// <summary>
        /// Carpeta por defecto de los archivos de versiones: %LOCALAPPDATA%\WardrobeFlow\Versiones.
        /// La arma Servicios.Serializacion.GestorArchivos con Environment.SpecialFolder (nunca una ruta
        /// fija), así funciona en cualquier PC y con un usuario de Windows sin permisos de administrador.
        /// </summary>
        public static string CarpetaVersiones =>
            GestorArchivos.Instancia.Carpeta(GestorArchivos.SubcarpetaVersiones);

        /// <summary>Crea la carpeta de versiones si no existe y devuelve su ruta.</summary>
        public static string PrepararCarpetaVersiones() =>
            GestorArchivos.Instancia.PrepararCarpeta(GestorArchivos.SubcarpetaVersiones);

        /// <summary>
        /// Nombre sugerido: versiones_&lt;username&gt;_&lt;aaaaMMdd_HHmmss&gt;&lt;extensión&gt; (sin
        /// caracteres inválidos). Por defecto .xml.
        /// </summary>
        public static string NombreArchivoSugerido(string username, System.DateTime fecha,
                                                   string extension = FabricaSerializadores.ExtensionXml)
        {
            string nombre = string.IsNullOrWhiteSpace(username) ? "usuario" : username.Trim();
            return GestorArchivos.Instancia.NombreArchivoSugerido("versiones", nombre, fecha, extension);
        }

        /// <summary>
        /// Serializa el historial de versiones (Mementos) del usuario a XML o JSON, según la extensión
        /// de la ruta (.xml / .json). Solo Administrador: el archivo lleva datos personales (nombre,
        /// email, fecha de nacimiento). La clave y el estado de bloqueo nunca se exportan ([XmlIgnore]
        /// e [IgnoreDataMember] en BE.VersionUsuario).
        /// </summary>
        public BE.HistorialUsuarioXml Exportar(string modulo, int idUsuario, string ruta)
        {
            ExigirAdministradorSerializacion();
            if (string.IsNullOrWhiteSpace(ruta))
                throw new BE.AppException("err.bll.version.xml.sin_ruta", "Indique el archivo de destino.");
            var serializador = FabricaSerializadores.Crear<BE.HistorialUsuarioXml>(ruta);   // rechaza otra extensión

            BE.Usuario usuario = _dalUsuario.ObtenerPorId(idUsuario);
            if (usuario == null)
                throw new BE.AppException("err.bll.usuario.no_existe", "El usuario de la versión ya no existe.");

            var versiones = _obtenerHistorial(idUsuario) ?? new List<BE.VersionUsuario>();
            if (versiones.Count == 0)
                throw new BE.AppException("err.bll.version.xml.sin_versiones",
                    "El usuario '{0}' no tiene versiones guardadas para exportar.", usuario.Username);

            var archivo = new BE.HistorialUsuarioXml
            {
                Version          = BE.HistorialUsuarioXml.FormatoActual,
                Equipo           = System.Environment.MachineName,
                FechaExportacion = System.DateTime.Now,
                ExportadoPor     = SessionManager.GetInstance().Usuario.Username,
                IdUsuario        = usuario.Id,
                Username         = usuario.Username,
                Versiones        = versiones
            };
            try { serializador.GuardarArchivo(archivo, ruta); }
            catch (System.Exception ex) when (ex is System.IO.IOException || ex is System.UnauthorizedAccessException)
            {
                throw new BE.AppException("err.bll.version.xml.no_escribe",
                    "No se pudo guardar el archivo '{0}': {1}", System.IO.Path.GetFileName(ruta), ex.Message);
            }
            archivo.FormatoArchivo = serializador.Formato;

            RegistrarSerializacion(modulo,
                $"Exportación de versiones a {serializador.Formato}: {versiones.Count} versión(es) del usuario ID {usuario.Id} ({usuario.Username}) a '{System.IO.Path.GetFileName(ruta)}'");
            return archivo;
        }

        /// <summary>
        /// Deserializa un archivo de versiones y lo valida: tamaño, versión de formato, coherencia
        /// interna y que pertenezca a un usuario EXISTENTE de este sistema (mismo Id y mismo nombre,
        /// o un nombre que figure en su historial). Las versiones importadas NO se graban en
        /// HistorialUsuario: solo se muestran y se pueden restaurar con <see cref="RestaurarVersionImportada"/>.
        /// </summary>
        public BE.HistorialUsuarioXml Importar(string modulo, string ruta)
        {
            ExigirAdministradorSerializacion();
            if (string.IsNullOrWhiteSpace(ruta) || !System.IO.File.Exists(ruta))
                throw new BE.AppException("err.serial.no_existe", "No se encontró el archivo '{0}'.", ruta ?? "");
            if (new System.IO.FileInfo(ruta).Length > TamanoMaximoArchivo)
                throw new BE.AppException("err.bll.version.xml.muy_grande",
                    "El archivo supera el tamaño máximo admitido ({0} MB); no parece un historial de versiones.",
                    TamanoMaximoArchivo / (1024 * 1024));

            var serializador = FabricaSerializadores.Crear<BE.HistorialUsuarioXml>(ruta);   // rechaza otra extensión
            BE.HistorialUsuarioXml archivo;
            try { archivo = serializador.LeerArchivo(ruta); }
            catch (System.Exception ex) when (ex is System.IO.IOException || ex is System.UnauthorizedAccessException)
            {
                throw new BE.AppException("err.bll.version.xml.no_lee",
                    "No se pudo leer el archivo '{0}': {1}", System.IO.Path.GetFileName(ruta), ex.Message);
            }
            ValidarArchivo(archivo);
            ValidarPertenencia(archivo);
            archivo.FormatoArchivo = serializador.Formato;

            RegistrarSerializacion(modulo,
                $"Importación de versiones desde {serializador.Formato}: {archivo.Versiones.Count} versión(es) del usuario ID {archivo.IdUsuario} ({archivo.Username}) desde '{System.IO.Path.GetFileName(ruta)}'");
            return archivo;
        }

        /// <summary>
        /// Restaura al usuario a una versión IMPORTADA de un archivo XML o JSON, por el MISMO flujo que una
        /// versión del historial (permiso de Administrador, aviso de nombre en uso, UPDATE con DV,
        /// snapshot del nuevo estado en HistorialUsuario y bitácora). El archivo se vuelve a validar
        /// por si cambió el estado de la base desde que se importó.
        /// </summary>
        public void RestaurarVersionImportada(string modulo, BE.HistorialUsuarioXml archivo, int idVersion)
        {
            BLLHelper.ExigirAdministrador("err.bll.version.sin_permiso",
                "Solo un Administrador puede restaurar una versión histórica de un usuario.");

            ValidarArchivo(archivo);
            ValidarPertenencia(archivo);

            var memento = archivo.Versiones.Find(v => v.Id == idVersion);
            if (memento == null)
                throw new BE.AppException("err.bll.version.no_existe",
                    "La versión seleccionada no existe.");

            Restaurar(modulo, memento,
                $"importada ID {idVersion} (archivo {archivo.FormatoArchivo ?? FabricaSerializadores.FormatoXml} del {archivo.FechaExportacion:dd/MM/yyyy HH:mm}, equipo {archivo.Equipo})");
        }

        /// <summary>
        /// Validación PURA del archivo (sin base de datos): formato, cabecera completa, versiones de un
        /// único usuario, sin Id repetidos y con datos que entran en las columnas de la tabla Usuario.
        /// </summary>
        internal static void ValidarArchivo(BE.HistorialUsuarioXml archivo)
        {
            if (archivo == null)
                throw new BE.AppException("err.serial.vacio", "El archivo está vacío.");
            SerializadorAbstracto.ExigirVersionFormato(archivo.Version, BE.HistorialUsuarioXml.FormatoActual);

            if (archivo.IdUsuario <= 0 || string.IsNullOrWhiteSpace(archivo.Username))
                throw new BE.AppException("err.bll.version.xml.incompleto",
                    "El archivo no indica a qué usuario pertenece (faltan el Id o el nombre de usuario).");
            if (archivo.Versiones == null || archivo.Versiones.Count == 0)
                throw new BE.AppException("err.bll.version.xml.vacio",
                    "El archivo no contiene versiones.");

            var ids = new HashSet<int>();
            foreach (var v in archivo.Versiones)
            {
                if (v == null || v.IdUsuario != archivo.IdUsuario || !ids.Add(v.Id))
                    throw new BE.AppException("err.bll.version.xml.inconsistente",
                        "El archivo es inconsistente: contiene versiones repetidas o de otro usuario.");
                if (string.IsNullOrWhiteSpace(v.UsernameSnapshot)
                    || v.UsernameSnapshot.Length > LargoMaxUsername
                    || (v.NombreSnapshot   ?? "").Length > LargoMaxNombre
                    || (v.ApellidoSnapshot ?? "").Length > LargoMaxApellido
                    || (v.EmailSnapshot    ?? "").Length > LargoMaxEmail)
                    throw new BE.AppException("err.bll.version.xml.dato_invalido",
                        "La versión ID {0} del archivo tiene datos inválidos (nombre de usuario vacío o valores demasiado largos).", v.Id);
            }
        }

        // El archivo debe pertenecer a un usuario EXISTENTE de esta base: mismo Id y mismo nombre de
        // usuario que al exportar, o (si después lo renombraron) un nombre que figure en el historial
        // de ESE usuario en la base. Así un archivo traído de otra base, donde el mismo Id es otra
        // persona, se rechaza en vez de pisar los datos de quien no corresponde.
        private void ValidarPertenencia(BE.HistorialUsuarioXml archivo)
        {
            BE.Usuario actual = _dalUsuario.ObtenerPorId(archivo.IdUsuario);
            if (actual == null)
                throw new BE.AppException("err.bll.version.xml.usuario_inexistente",
                    "El archivo pertenece al usuario '{0}' (ID {1}), que no existe en este sistema. No se puede importar.",
                    archivo.Username, archivo.IdUsuario);

            if (MismoNombre(actual.Username, archivo.Username)) return;

            var historial = _obtenerHistorial(actual.Id) ?? new List<BE.VersionUsuario>();
            if (historial.Exists(v => MismoNombre(v.UsernameSnapshot, archivo.Username))) return;

            throw new BE.AppException("err.bll.version.xml.otro_usuario",
                "El archivo pertenece al usuario '{0}' (ID {1}), pero en este sistema el ID {1} corresponde a '{2}' y su historial no registra el nombre '{0}'. Por seguridad no se importa: probablemente el archivo proviene de otra base de datos.",
                archivo.Username, archivo.IdUsuario, actual.Username);
        }

        private static bool MismoNombre(string a, string b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), System.StringComparison.OrdinalIgnoreCase);

        private static void ExigirAdministradorSerializacion() =>
            BLLHelper.ExigirAdministrador("err.bll.version.xml.sin_permiso",
                "Solo un Administrador puede exportar o importar las versiones de un usuario.");

        // La bitácora no debe impedir la exportación/importación ya hecha (mismo criterio que
        // BLL.ErroresSerializados). Criticidad Media: el archivo lleva datos personales.
        private void RegistrarSerializacion(string modulo, string actividad)
        {
            try { _bitacora.Registrar(modulo, actividad, BE.Criticidad.Media); }
            catch (System.Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.VersionUsuario] " + ex.Message); }
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
