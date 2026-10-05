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

        /// <summary>Username del usuario logueado (el "actor" que se registra en las operaciones), o null.</summary>
        public static string Actor => Usuario?.Username;

        public static int? IdUsuario => Usuario?.Id;
    }
}
