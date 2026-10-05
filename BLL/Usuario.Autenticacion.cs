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

        // T07 — Lectores para el login con la integridad comprometida (reemplazables en tests).
        // Espejo: último estado legítimo conocido del usuario (Usuario_Seguridad).
        internal static Func<string, BE.FilaUsuarioDV> LectorEspejo =
            u => new DAL.EspejoUsuario().ObtenerPorUsername(u);

        // Evalúa una cuenta bloqueada. Devuelve:
        //   expirado    = el bloqueo TEMPORAL ya venció → se puede reactivar y continuar.
        //   permanente  = no auto-expira (bloqueo manual del admin, sin fecha, o escala agotada).
        //   minutosRest = minutos que faltan si todavía no expiró.
        private static (bool expirado, bool permanente, int minutosRestantes) EvaluarBloqueo(BE.Usuario u)
        {
            // Sin fecha de bloqueo (bloqueo manual del admin) → no auto-expira.
            if (!u.FechaBloqueo.HasValue) return (false, true, 0);
            // Escala agotada → bloqueo permanente.
            if (u.CantidadBloqueos <= 0 || u.CantidadBloqueos > _minutosBloqueo.Length)
                return (false, true, 0);

            int minutos = _minutosBloqueo[u.CantidadBloqueos - 1];
            double transcurridos = (DateTime.Now - u.FechaBloqueo.Value).TotalMinutes;
            if (transcurridos >= minutos) return (true, false, 0);
            return (false, false, (int)Math.Ceiling(minutos - transcurridos));
        }

        // Huella corta (no reversible) de lo que se tipeó como usuario: permite correlacionar
        // intentos en la bitácora sin guardar el texto, que podría ser una contraseña tipeada
        // en el campo equivocado.
        internal static string HuellaUsuario(string username)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] h = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes((username ?? "").Trim().ToLowerInvariant()));
                return BitConverter.ToString(h, 0, 4).Replace("-", "");
            }
        }

        private BE.LoginException CredencialesInvalidas() =>
            new BE.LoginException(BE.LoginException.TipoError.CredencialesInvalidas,
                "Usuario o contraseña incorrectos.",
                intentosRestantes: ContadorSesion.GetInstance().IntentosRestantes);

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

            // T07 — Si la verificación de arranque detectó que la tabla Usuario (u otra protegida)
            // fue manipulada, NO se confía en ella para autenticar: un atacante podría haberse
            // puesto rol Administrador y llegar a la consola de recuperación ("Asumir pérdida").
            if (Configuracion.IntegridadComprometida)
                return LoginConIntegridadComprometida(modulo, username, contraseña);

            BE.Usuario usuario = usuarioDAL.ObtenerPorUsername(username);
            if (usuario == null)
            {
                // Anti-enumeración: igualar el costo temporal de un usuario real (corre PBKDF2
                // contra un hash señuelo), contar el intento en la sesión y registrarlo. Se
                // lanza EXACTAMENTE la misma excepción, mensaje y contador (de sesión) que para
                // una contraseña incorrecta. En la bitácora NO se guarda lo tipeado (podría ser
                // una contraseña): solo una huella.
                Encriptador.VerificacionSenuelo(contraseña);
                ContadorSesion.GetInstance().RegistrarIntento();
                bitacora.RegistrarSinSesion(
                    modulo:     modulo ?? "Login",
                    actividad:  BE.ActividadesBitacora.IntentoFallidoLogin,
                    criticidad: BE.Criticidad.IntentosLogin,
                    detalle:    $"Intento de login para un usuario inexistente (huella {HuellaUsuario(username)}) " +
                                $"a las {DateTime.Now:HH:mm:ss}.");
                throw CredencialesInvalidas();
            }

            // La contraseña se verifica SIEMPRE (mismo costo PBKDF2 en todos los caminos) y ANTES
            // de revelar que la cuenta está bloqueada: sin la clave correcta, una cuenta bloqueada
            // responde igual que cualquier otro intento fallido (no se enumera el estado).
            bool esValido = Encriptador.VerificarContrasena(contraseña, usuario.Contraseña);

            if (usuario.Bloqueado)
            {
                var (expirado, permanente, minutos) = EvaluarBloqueo(usuario);
                if (!expirado)
                {
                    ContadorSesion.GetInstance().RegistrarIntento();
                    if (!esValido)
                    {
                        RegistrarIntentoFallidoInterno(modulo, username, usuario.IntentosFallidos, usuario.Id);
                        throw CredencialesInvalidas();
                    }
                    if (permanente)
                        throw new BE.LoginException(BE.LoginException.TipoError.CuentaBloqueada,
                            $"La cuenta '{username}' está bloqueada.\n" +
                            "Contactá al Administrador (o usá una clave de emergencia) para reactivarla.");
                    throw new BE.LoginException(BE.LoginException.TipoError.CuentaBloqueada,
                        $"La cuenta '{username}' está bloqueada temporalmente.\n" +
                        $"Reintentá en {minutos} minuto(s) o usá una clave de emergencia.");
                }

                // El bloqueo temporal EXPIRÓ → se reactiva sola y el login continúa normalmente.
                usuarioDAL.AutoDesbloquear(usuario.Id);
                usuario.Bloqueado        = false;
                usuario.IntentosFallidos = 0;
            }

            if (esValido)
            {
                ContadorSesion.GetInstance().Resetear();
                usuarioDAL.ResetearIntentosFallidos(username);
                AbrirSesion(modulo, usuario);
                return true;
            }

            ContadorSesion.GetInstance().RegistrarIntento();
            // Contador leído en la MISMA sentencia que lo incrementa (OUTPUT inserted): dos
            // intentos simultáneos no deciden el bloqueo con un valor viejo.
            int intentos = usuarioDAL.IncrementarIntentosFallidos(username) ?? (usuario.IntentosFallidos + 1);

            RegistrarIntentoFallidoInterno(modulo, username, intentos, usuario.Id);

            if (intentos >= MaxIntentosFallidos)
            {
                // Bloqueo PROGRESIVO: cada bloqueo dura más (1/5/15/60 min) y tras agotar la
                // escala queda permanente (requiere admin / clave de emergencia).
                usuarioDAL.BloquearConTiempo(usuario.Id);
                RegistrarBloqueo(modulo, username, usuario.Id);
            }

            // Mismo mensaje y mismo contador (de sesión) que el caso "usuario inexistente":
            // indistinguibles entre sí (anti-enumeración). Tampoco se informa acá el bloqueo:
            // quien tenga la clave correcta lo verá al reintentar.
            throw CredencialesInvalidas();
        }

        private void AbrirSesion(string modulo, BE.Usuario usuario)
        {
            // T04 — Permisos EFECTIVOS resueltos recursivamente sobre el árbol Composite
            // (rol → roles/familias → patentes), con deduplicación de permisos repetidos.
            usuario.Permisos = perfilesBLL.ObtenerPermisosEfectivos(usuario.Rol ?? usuario.Perfil);
            PermisosAccion.LimpiarCacheVigencia();
            SessionManager.Login(usuario);
            bitacora.Registrar(modulo, BE.ActividadesBitacora.InicioSesion, BE.Criticidad.None);
        }

        // T07 — Login cuando la integridad está comprometida. Se autentica contra el ESPEJO de
        // integridad (último estado legítimo conocido de cada usuario), cuyo propio DVH debe
        // verificar: la clave, el rol y el estado salen de ahí y no de la tabla Usuario, que pudo
        // haber sido alterada. Si el usuario no tiene fila íntegra en el espejo, se rechaza.
        // No se escribe nada en Usuario (escribir recalcularía el DVH de una fila alterada).
        private bool LoginConIntegridadComprometida(string modulo, string username, string contraseña)
        {
            BE.FilaUsuarioDV fila = null;
            try { fila = LectorEspejo(username); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.Usuario] Espejo: " + ex.Message); }

            bool filaIntegra = fila != null && fila.DVHAlmacenado.HasValue
                && Seguridad.CalculadorDV.Crear().CalcularDVH(fila.CamposParaDVH()) == fila.DVHAlmacenado.Value;

            bool esValido = filaIntegra
                ? Encriptador.VerificarContrasena(contraseña, fila.Clave)
                : Encriptador.VerificacionSenuelo(contraseña) && false;

            bool activo = filaIntegra && fila.Activo == "1" && fila.Estado == "1";
            if (!esValido || !activo)
            {
                ContadorSesion.GetInstance().RegistrarIntento();
                bitacora.RegistrarSinSesion(
                    modulo:     modulo ?? "Login",
                    actividad:  BE.ActividadesBitacora.IntentoFallidoLogin,
                    criticidad: BE.Criticidad.Alta,
                    idUsuario:  filaIntegra ? (int?)fila.Id : null,
                    detalle:    "Login rechazado con la integridad de datos comprometida " +
                                $"(huella {HuellaUsuario(username)}): " +
                                (filaIntegra ? "credenciales inválidas o cuenta inactiva en el espejo."
                                             : "el usuario no tiene una fila íntegra en el espejo de integridad.") +
                                $" {DateTime.Now:HH:mm:ss}.");
                throw CredencialesInvalidas();
            }

            ContadorSesion.GetInstance().Resetear();
            var usuario = new BE.Usuario
            {
                Id         = fila.Id,
                Username   = fila.Username,
                Contraseña = fila.Clave,
                Rol        = string.IsNullOrEmpty(fila.Rol) ? null : fila.Rol,
                Perfil     = string.IsNullOrEmpty(fila.Perfil) ? null : fila.Perfil,
                IdIdioma   = "ES"
            };
            // Con la integridad comprometida el árbol de permisos tampoco es confiable: la sesión
            // solo sirve para la consola de recuperación (exclusiva del Administrador según el espejo).
            usuario.Permisos = new System.Collections.Generic.List<BE.Permiso>();
            PermisosAccion.LimpiarCacheVigencia();
            SessionManager.Login(usuario);
            bitacora.RegistrarSinSesion(
                modulo:     modulo ?? "Login",
                actividad:  BE.ActividadesBitacora.InicioSesion,
                criticidad: BE.Criticidad.Alta,
                idUsuario:  usuario.Id,
                detalle:    $"Inicio de sesión de '{usuario.Username}' validado contra el espejo de integridad " +
                            $"(integridad comprometida) a las {DateTime.Now:HH:mm:ss}.");
            return true;
        }

        // Cierra la sesión: registra en bitácora y destruye la sesión Singleton.
        public void Logout(string modulo)
        {
            bitacora.Registrar(modulo, BE.ActividadesBitacora.CierreSesion, BE.Criticidad.None);
            SessionManager.Logout();
            PermisosAccion.LimpiarCacheVigencia();
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

        // Persiste la preferencia de idioma del usuario activo. Solo el PROPIO usuario de la
        // sesión puede cambiar su preferencia (antes cualquier llamador podía escribirla para
        // cualquier IdUsuario).
        public void GuardarPreferenciaIdioma(int idUsuario, string idIdioma)
        {
            if (!SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada", "La sesión expiró. Volvé a iniciar sesión.");
            var u = SessionManager.GetInstance().Usuario;
            if (u.Id != idUsuario)
                throw new BE.AppException("err.bll.usuario.preferencia_ajena",
                    "Solo podés cambiar tu propia preferencia de idioma.");

            usuarioDAL.GuardarIdioma(idUsuario, idIdioma);
            u.IdIdioma = idIdioma;
        }

        // Registra un intento de login fallido en bitácora.
        private void RegistrarIntentoFallidoInterno(string modulo, string username,
                                                     int numeroIntento, int? idUsuario = null)
        {
            bitacora.RegistrarSinSesion(
                modulo:      modulo ?? "Login",
                actividad:   BE.ActividadesBitacora.IntentoFallidoLogin,
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
                actividad:   BE.ActividadesBitacora.BloqueoDeCuenta,
                criticidad:  BE.Criticidad.BloqueosCuenta,
                idUsuario:   idUsuario,
                detalle:     $"Cuenta '{username}' (ID: {idUsuario?.ToString() ?? "?"}) " +
                             $"bloqueada automáticamente tras {MaxIntentosFallidos} " +
                             $"intentos fallidos consecutivos a las {DateTime.Now:HH:mm:ss}.");
        }
    }
}
