namespace BLL
{
    /// <summary>
    /// Fachada de SOLO LECTURA de la sesión activa para la GUI. La GUI no referencia el proyecto
    /// Seguridad: cuando necesita saber quién está logueado (para el actor de una operación, la
    /// seguridad por control o la bitácora de una excepción) lo pide acá. Iniciar y cerrar sesión
    /// sigue siendo BLL.Usuario.Login/Logout.
    /// </summary>
    public static class Sesion
    {
        public static bool Activa => Seguridad.SessionManager.IsLoggedIn;

        /// <summary>Usuario logueado, o null si no hay sesión.</summary>
        public static BE.Usuario Usuario => Activa ? Seguridad.SessionManager.GetInstance().Usuario : null;

        /// <summary>
        /// Username del usuario logueado (el "actor" que se registra en las operaciones), o null.
        /// Los servicios de BLL (Cobro, Renovacion, Prenda.CambiarEstado, CargoPrenda,
        /// InspeccionDevolucion, ListaEspera) lo resuelven ACÁ: la GUI no les pasa el actor, así
        /// no puede registrar una operación a nombre de otro usuario.
        /// </summary>
        public static string Actor => Usuario?.Username;

        /// <summary>
        /// Actor explícito de los procesos que corre el propio sistema, sin un usuario que los
        /// dispare (p. ej. liberar las reservas vencidas de la Lista de Espera).
        /// </summary>
        public const string ActorSistema = "sistema";

        public static int? IdUsuario => Usuario?.Id;

        /// <summary>
        /// Vuelve a resolver desde la base los permisos efectivos del usuario en sesión y los
        /// reemplaza en la sesión (re-aplicación de seguridad EN VIVO tras editar roles/permisos).
        /// Es el ÚNICO punto, fuera del login, que escribe el estado de autorización de la sesión:
        /// la GUI pide el refresco y recibe la lista nueva para re-aplicar menús y controles, pero
        /// no la arma ni la asigna. Devuelve null si no hay sesión activa.
        /// </summary>
        public static System.Collections.Generic.List<BE.Permiso> RefrescarPermisos()
            => RefrescarPermisos(new Familia());

        // Sobrecarga con la resolución inyectada (tests sin base de datos).
        internal static System.Collections.Generic.List<BE.Permiso> RefrescarPermisos(Familia resolutor)
        {
            if (!Activa) return null;
            var usuario  = Seguridad.SessionManager.GetInstance().Usuario;
            var permisos = resolutor.ObtenerPermisosEfectivos(usuario.Rol ?? usuario.Perfil)
                           ?? new System.Collections.Generic.List<BE.Permiso>();
            // Se reemplaza la lista entera (no se muta la vieja): un lector concurrente ve la
            // lista anterior completa o la nueva completa, nunca una a medio armar.
            usuario.Permisos = permisos;
            PermisosAccion.LimpiarCacheVigencia();
            return permisos;
        }
    }
}
