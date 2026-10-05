using System;
using BE;

namespace Seguridad
{
    /// <summary>Singleton que gestiona la sesión del usuario autenticado.</summary>
    public sealed class SessionManager
    {
        private static readonly object _lock    = new object();
        // volatile: se escribe dentro de lock (Login/Logout) pero se lee sin lock en GetInstance/
        // IsLoggedIn/TienePermiso, los puntos de entrada más usados de BLL/GUI — sin esto, una
        // escritura protegida por lock en un hilo no garantiza que otro hilo que lee sin
        // sincronización vea el valor actualizado de inmediato (visibilidad, no solo atomicidad).
        // Mismo criterio que ya usa la clase hermana Seguridad.ContadorSesion.
        private static volatile SessionManager _session;

        // Usuario actualmente en sesión y fecha/hora de inicio. 
        public Usuario Usuario    { get; set; }

        // Marca de tiempo del momento en que se inició la sesión.
        public DateTime FechaInicio { get; set; }

        // Retorna la sesión activa. Lanza SesionException (traducible) si no hay sesión iniciada.
        public static SessionManager GetInstance()
        {
            if (_session == null)
                throw new BE.SesionException("err.seg.sesion_no_iniciada",
                    "La sesión no está iniciada. Iniciá sesión primero.");

            return _session;
        }

        // Indica si hay una sesión activa sin lanzar excepción.
        public static bool IsLoggedIn => _session != null;

        // T04 — Re-validación de permisos en el BACKEND.
        // Devuelve true si el usuario en sesión posee la patente indicada.
        // El Administrador tiene acceso total. Si no hay usuario, no tiene permiso.
        // Evalúa los permisos de ESTA sesión (la instancia), no de la sesión global vigente: una
        // referencia vieja a una sesión ya cerrada no debe responder con los permisos de la
        // sesión que se haya abierto después.
        public bool TienePermiso(string nombreMenu)
        {
            var u = this.Usuario;
            if (u == null) return false;
            if (u.EsAdministrador) return true;
            return u.Permisos != null &&
                   u.Permisos.Exists(p => string.Equals(p.NombreMenu, nombreMenu, StringComparison.OrdinalIgnoreCase));
        }

        // Crea la sesión para el usuario autenticado.
        public static void Login(Usuario usuario)
        {
            lock (_lock)
            {
                if (_session == null)
                {
                    _session             = new SessionManager();
                    _session.Usuario     = usuario;
                    _session.FechaInicio = DateTime.Now;
                }
                else
                {
                    throw new BE.SesionException("err.seg.sesion_ya_iniciada",
                        "Ya hay una sesión iniciada. Cerrá la sesión actual antes de iniciar otra.");
                }
            }
        }

        // Destruye la sesión activa. Idempotente: si ya no hay sesión, no hace nada.
        public static void Logout()
        {
            lock (_lock) { _session = null; }
        }

        private SessionManager() { }
    }
}
