using Seguridad;
using Servicios;
using System;

namespace BLL
{
    // Partial de BLL.Usuario — Autenticación: login/logout, bloqueo progresivo de cuenta
    // y lectura de la sesión activa. Ver BLL.Usuario (Usuario.cs) para el resto de grupos.
    public partial class Usuario
    {
        private const int MaxIntentosFallidos = 3;

        // Bloqueo PROGRESIVO: duración (en minutos) según cuántas veces ya se bloqueó la cuenta.
        // 1er bloqueo → 1 min, 2do → 5, 3ro → 15, 4to → 60; superada la escala, queda permanente.
        private static readonly int[] _minutosBloqueo = { 1, 5, 15, 60 };

        // Evalúa una cuenta bloqueada. Devuelve:
        //   expirado    = el bloqueo TEMPORAL ya venció → se puede reactivar y continuar.
        //   permanente  = no auto-expira (bloqueo manual del admin, sin fecha, o escala agotada).
        //   minutosRest = minutos que faltan si todavía no expiró.
        private static (bool expirado, bool permanente, int minutosRestantes) EvaluarBloqueo(BE.Usuario u)
        {
            // Sin fecha de bloqueo (bloqueo manual del admin o BD sin migrar) → no auto-expira.
            if (!u.FechaBloqueo.HasValue) return (false, true, 0);
            // Escala agotada → bloqueo permanente.
            if (u.CantidadBloqueos <= 0 || u.CantidadBloqueos > _minutosBloqueo.Length)
                return (false, true, 0);

            int minutos = _minutosBloqueo[u.CantidadBloqueos - 1];
            double transcurridos = (DateTime.Now - u.FechaBloqueo.Value).TotalMinutes;
            if (transcurridos >= minutos) return (true, false, 0);
            return (false, false, (int)Math.Ceiling(minutos - transcurridos));
        }

        /// <summary>Autentica al usuario y establece la sesión. Bloquea la cuenta tras 3 intentos fallidos.</summary>
        public bool Login(string modulo, string username, string contraseña)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(contraseña))
                throw new BE.LoginException(BE.LoginException.TipoError.CamposVacios,
                    "Usuario y contraseña son obligatorios.");

            if (ContadorSesion.GetInstance().LimiteAlcanzado)
                throw new BE.LoginException(BE.LoginException.TipoError.LimiteAlcanzado,
                    "Demasiados intentos fallidos en esta sesión.\n" +
                    "Reiniciá la aplicación para volver a intentarlo.");

            BE.Usuario usuario = usuarioDAL.ObtenerPorUsername(username);
            if (usuario == null)
            {
                // Anti-enumeración: igualar el costo temporal de un usuario real (corre PBKDF2
                // contra un hash señuelo), contar el intento en la sesión y registrarlo. Se
                // lanza EXACTAMENTE la misma excepción, mensaje y contador (de sesión) que para
                // una contraseña incorrecta, de modo que el atacante no pueda distinguir si el
                // usuario existe — ni por el texto, ni por la presencia del contador, ni por el tiempo.
                Encriptador.VerificacionSenuelo(contraseña);
                ContadorSesion.GetInstance().RegistrarIntento();
                bitacora.RegistrarSinSesion(
                    modulo:     modulo ?? "Login",
                    actividad:  "Intento Fallido Login",
                    criticidad: BE.Criticidad.IntentosLogin,
                    detalle:    $"Intento de login para usuario inexistente '{username}' a las {DateTime.Now:HH:mm:ss}.");
                throw new BE.LoginException(BE.LoginException.TipoError.CredencialesInvalidas,
                    "Usuario o contraseña incorrectos.",
                    intentosRestantes: ContadorSesion.GetInstance().IntentosRestantes);
            }

            if (usuario.Bloqueado)
            {
                var (expirado, permanente, minutos) = EvaluarBloqueo(usuario);
                if (permanente)
                    throw new BE.LoginException(BE.LoginException.TipoError.CuentaBloqueada,
                        $"La cuenta '{username}' está bloqueada.\n" +
                        "Contactá al Administrador (o usá una clave de emergencia) para reactivarla.");
                if (!expirado)
                    throw new BE.LoginException(BE.LoginException.TipoError.CuentaBloqueada,
                        $"La cuenta '{username}' está bloqueada temporalmente.\n" +
                        $"Reintentá en {minutos} minuto(s) o usá una clave de emergencia.");

                // El bloqueo temporal EXPIRÓ → se reactiva sola y el login continúa normalmente.
                usuarioDAL.AutoDesbloquear(usuario.Id);
                usuario.Bloqueado        = false;
                usuario.IntentosFallidos = 0;
            }

            bool esValido = Encriptador.VerificarContrasena(contraseña, usuario.Contraseña);

            if (esValido)
            {
                ContadorSesion.GetInstance().Resetear();
                usuarioDAL.ResetearIntentosFallidos(username);
                // T04 — Permisos EFECTIVOS resueltos recursivamente sobre el árbol Composite
                // (rol → roles/familias → patentes), con deduplicación de permisos repetidos.
                usuario.Permisos = perfilesBLL.ObtenerPermisosEfectivos(usuario.Rol ?? usuario.Perfil);
                SessionManager.Login(usuario);
                bitacora.Registrar(modulo, "Inicio Sesion", BE.Criticidad.None);
            }
            else
            {
                ContadorSesion.GetInstance().RegistrarIntento();
                usuarioDAL.IncrementarIntentosFallidos(username);
                int intentos = usuario.IntentosFallidos + 1;

                RegistrarIntentoFallidoInterno(modulo, username, intentos, usuario.Id);

                if (intentos >= MaxIntentosFallidos)
                {
                    // Bloqueo PROGRESIVO: cada bloqueo dura más (1/5/15/60 min) y tras agotar la
                    // escala queda permanente (requiere admin / clave de emergencia).
                    usuarioDAL.BloquearConTiempo(usuario.Id);
                    RegistrarBloqueo(modulo, username, usuario.Id);

                    int nuevaCantidad = usuario.CantidadBloqueos + 1;
                    string msgBloqueo = nuevaCantidad > _minutosBloqueo.Length
                        ? $"La cuenta '{username}' fue bloqueada permanentemente tras varios bloqueos.\n" +
                          "Contactá al Administrador (o usá una clave de emergencia) para reactivarla."
                        : $"La cuenta '{username}' fue bloqueada por {_minutosBloqueo[nuevaCantidad - 1]} " +
                          $"minuto(s) tras {MaxIntentosFallidos} intentos fallidos.\n" +
                          "Reintentá más tarde o usá una clave de emergencia.";

                    throw new BE.LoginException(BE.LoginException.TipoError.CuentaBloqueada, msgBloqueo);
                }

                // Mismo mensaje y mismo contador (de sesión) que el caso "usuario inexistente":
                // indistinguibles entre sí (anti-enumeración). El bloqueo de la CUENTA ya se
                // resolvió arriba; acá solo se informa el intento fallido genérico.
                throw new BE.LoginException(BE.LoginException.TipoError.CredencialesInvalidas,
                    "Usuario o contraseña incorrectos.",
                    intentosRestantes: ContadorSesion.GetInstance().IntentosRestantes);
            }

            return esValido;
        }

        // Cierra la sesión: registra en bitácora y destruye la sesión Singleton.
        public void Logout(string modulo)
        {
            bitacora.Registrar(modulo, "Cierre Sesion", BE.Criticidad.None);
            SessionManager.Logout();
        }

        // Retorna el usuario en sesión (con sus permisos) desde el SessionManager.
        public BE.Usuario ObtenerUsuarioActivo()
        {
            if (!SessionManager.IsLoggedIn) return null;
            return SessionManager.GetInstance().Usuario;
        }

        // Retorna la fecha/hora de inicio de la sesión activa, o null si no hay sesión.
        public DateTime? ObtenerFechaInicioSesion()
        {
            if (!SessionManager.IsLoggedIn) return null;
            return SessionManager.GetInstance().FechaInicio;
        }

        // Persiste la preferencia de idioma del usuario activo.
        // También actualiza el objeto en sesión para que las consultas inmediatas reflejen el cambio.
        public void GuardarPreferenciaIdioma(int idUsuario, string idIdioma)
        {
            usuarioDAL.GuardarIdioma(idUsuario, idIdioma);
            if (Seguridad.SessionManager.IsLoggedIn)
                Seguridad.SessionManager.GetInstance().Usuario.IdIdioma = idIdioma;
        }

        // Registra un intento de login fallido en bitácora.
        private void RegistrarIntentoFallidoInterno(string modulo, string username,
                                                     int numeroIntento, int? idUsuario = null)
        {
            bitacora.RegistrarSinSesion(
                modulo:      modulo ?? "Login",
                actividad:   "Intento Fallido Login",
                criticidad:  BE.Criticidad.IntentosLogin,
                idUsuario:   idUsuario,
                detalle:     $"Intento fallido #{numeroIntento}/{MaxIntentosFallidos} " +
                             $"para '{username}' (ID: {idUsuario?.ToString() ?? "?"}) " +
                             $"a las {DateTime.Now:HH:mm:ss}.");
        }

        // Registra el bloqueo de cuenta en bitácora.
        private void RegistrarBloqueo(string modulo, string username, int? idUsuario = null)
        {
            bitacora.RegistrarSinSesion(
                modulo:      modulo ?? "Login",
                actividad:   "Bloqueo de Cuenta",
                criticidad:  BE.Criticidad.BloqueosCuenta,
                idUsuario:   idUsuario,
                detalle:     $"Cuenta '{username}' (ID: {idUsuario?.ToString() ?? "?"}) " +
                             $"bloqueada automáticamente tras {MaxIntentosFallidos} " +
                             $"intentos fallidos consecutivos a las {DateTime.Now:HH:mm:ss}.");
        }
    }
}
