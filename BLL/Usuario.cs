using Seguridad;
using Servicios;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para autenticación y gestión de usuarios.
    ///
    /// Dividida en partial classes por responsabilidad (era una God Class de ~790 líneas
    /// hasta la auditoría técnica del 2026-08-17): este archivo tiene el núcleo (estado/DI)
    /// y el guard compartido; el resto vive en:
    ///   Usuario.Autenticacion.cs → login/logout, bloqueo progresivo, sesión activa.
    ///   Usuario.Abm.cs           → alta, modificación, cambio de rol, búsqueda.
    ///   Usuario.Archivado.cs     → RF-10: desbloqueo, baja lógica y purga.
    ///   Usuario.Claves.cs        → reset/recuperación de contraseña.
    /// El API pública no cambió: sigue siendo la misma clase BLL.Usuario, solo separada
    /// en varios archivos para que cada responsabilidad se pueda leer de forma aislada.
    /// </summary>
    public partial class Usuario
    {
        private readonly DAL.Interfaces.IUsuarioDAL usuarioDAL;
        // perfilesBLL y bitacora son PEREZOSOS: solo se instancian cuando una operación los usa
        // (Login resuelve permisos; las escrituras registran bitácora). Así construir BLL.Usuario
        // —y testear con un IUsuarioDAL falso— no toca la BD a través de sus DAL internos.
        private BLL.Familia _perfilesLazy;
        private BLL.Familia perfilesBLL => _perfilesLazy ?? (_perfilesLazy = new BLL.Familia());
        private Servicios.Bitacora _bitacoraLazy;
        private Servicios.Bitacora bitacora => _bitacoraLazy ?? (_bitacoraLazy = new Servicios.Bitacora());

        // DI: el constructor por defecto usa el DAL real; el otro permite inyectar un doble.
        public Usuario() : this(new DAL.Usuario()) { }
        public Usuario(DAL.Interfaces.IUsuarioDAL usuarioDAL)
        {
            this.usuarioDAL = usuarioDAL;
        }

        // Re-validación en el BACKEND: la gestión de usuarios es una operación EXCLUSIVA del
        // Administrador. Se verifica el rol en sesión por Perfil, de forma consistente con
        // SessionManager.TienePermiso (que también identifica al admin por su Perfil).
        // Compartido por los 4 partial de arriba (Abm/Archivado/Claves).
        private static void ValidarEsAdministrador()
        {
            // Guard centralizado (DRY): sesión activa + rol Administrador.
            BLLHelper.ExigirAdministrador("err.bll.usuario.sin_permiso",
                "Solo un Administrador puede gestionar usuarios.");
        }
    }
}
