using Seguridad;
using Servicios;
using System;

namespace BLL
{
    // Partial de BLL.Usuario — reseteo y recuperación de contraseña (individual, masivo,
    // y cambio de clave propia). Ver BLL.Usuario (Usuario.cs) para el resto de grupos.
    public partial class Usuario
    {
        // Clave temporal por defecto para el reset masivo. Configurable en App.config
        // (appSettings["ClaveTemporalDefault"]); si falta o está vacía, usa un fallback válido.
        // Antes estaba hardcodeada; sacarla a config evita exponer la clave en el binario.
        private static readonly string ClaveTemporalDefault = LeerClaveTemporalDefault();

        private static string LeerClaveTemporalDefault()
        {
            string v = System.Configuration.ConfigurationManager.AppSettings["ClaveTemporalDefault"];
            return string.IsNullOrWhiteSpace(v) ? "Wardrobe1!" : v;
        }

        // Resetea la contraseña de un usuario generando una nueva automáticamente.
        // El administrador NO ingresa la contraseña — se genera aquí y se exporta
        // a un archivo .txt en CredencialesGeneradas/.
        // Devuelve la ruta del archivo de credenciales generado.
        public string ResetearClave(string modulo, int idUsuario, string usernameObjetivo)
        {
            ValidarEsAdministrador();

            // T07 — Verificar integridad de la base ANTES de modificar usuarios.
            Configuracion.AsegurarIntegridadUsuarios();

            var admin = SessionManager.GetInstance().Usuario;

            new VersionUsuario().GrabarVersion(idUsuario, admin.Username,
                "Snapshot antes de reset de contraseña por '" + admin.Username + "'.");

            string contrasena    = GeneradorCredenciales.GenerarContrasena();
            string claveHasheada = Encriptador.Hash(contrasena);
            usuarioDAL.ResetearClave(idUsuario, claveHasheada);

            string rutaArchivo = GeneradorCredenciales.ExportarCredenciales(usernameObjetivo, contrasena);

            bitacora.RegistrarSinSesion(
                modulo:     modulo,
                actividad:  "Reset Contrasena",
                criticidad: BE.Criticidad.RecuperacionClave,
                idUsuario:  admin.Id,
                detalle:    "Admin '" + admin.Username + "' (ID: " + admin.Id + ") reseteo la contrasena del usuario ID " + idUsuario + " a las " + DateTime.Now.ToString("HH:mm:ss") + "."
            );

            return rutaArchivo;
        }

        // Resetea la contraseña de TODOS los usuarios a la clave temporal por defecto. Solo Administrador.
        // Devuelve la clave usada para que la GUI pueda informarla al usuario sin conocerla.
        public string ResetearTodasLasClaves(string modulo)
        {
            ResetearTodasLasClaves(modulo, ClaveTemporalDefault);
            return ClaveTemporalDefault;
        }

        // Resetea la contraseña de TODOS los usuarios a una clave temporal. Solo Administrador.
        public void ResetearTodasLasClaves(string modulo, string claveTemporal)
        {
            ValidarEsAdministrador();

            var (valida, clave, mensaje) = Encriptador.ValidarContrasena(claveTemporal);
            if (!valida)
                throw new BE.AppException(clave, mensaje);

            string hash = Encriptador.Hash(claveTemporal);
            usuarioDAL.ResetearTodasLasClaves(hash);

            var admin = SessionManager.GetInstance().Usuario;
            bitacora.RegistrarSinSesion(
                modulo:     modulo,
                actividad:  "Reset Masivo Contrasenas",
                criticidad: BE.Criticidad.Alta,
                idUsuario:  admin.Id,
                detalle:    $"Admin '{admin.Username}' (ID: {admin.Id}) reseteo todas las contrasenas a clave temporal a las {DateTime.Now:HH:mm:ss}."
            );
        }

        // Las claves de emergencia (autodesbloqueo de Admin) viven en BLL.RecuperacionAdmin (SRP).

        // Cambio de clave por el PROPIO usuario en sesión. Lo usa el cambio OBLIGATORIO posterior
        // al login (cuando RequiereCambioClave=1) y también puede usarlo "Mi Perfil".
        // Valida la clave nueva, exige que difiera de la actual, persiste, baja el flag y
        // actualiza la sesión. No requiere ser administrador: cada uno cambia SU propia clave.
        public void CambiarClavePropia(string modulo, string claveNueva)
        {
            if (!SessionManager.IsLoggedIn)
                throw new BE.SesionException("err.seg.sesion_no_iniciada",
                    "La sesión no está iniciada. Iniciá sesión primero.");

            var u = SessionManager.GetInstance().Usuario;

            var (valida, clave, mensaje) = Encriptador.ValidarContrasena(claveNueva);
            if (!valida)
                throw new BE.AppException(clave, mensaje);

            // La clave nueva no puede ser la misma que la actual (evita "cambiarla" por la temporal).
            if (Encriptador.VerificarContrasena(claveNueva, u.Contraseña))
                throw new BE.AppException("err.bll.usuario.clave_igual_actual",
                    "La nueva contraseña no puede ser igual a la actual.");

            string hash = Encriptador.Hash(claveNueva);
            usuarioDAL.CambiarClave(u.Id, hash);

            // Reflejar el cambio en la sesión para que no se vuelva a pedir.
            u.Contraseña          = hash;
            u.RequiereCambioClave = false;

            bitacora.Registrar(modulo, "Cambio de Contrasena Propia", BE.Criticidad.Media);
        }

        // Registra una solicitud de recuperación de clave en la bitácora.
        // Retorna true si el usuario existe, false si no se encontró.
        // Lanza excepción solo si ocurre un error inesperado en BD.
        public bool SolicitarRecuperacionClave(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            bool existe = usuarioDAL.ObtenerPorUsername(username) != null;
            if (!existe) return false;

            bitacora.RegistrarSinSesion(
                modulo:     "Recuperar Contrasena",
                actividad:  "Solicitud Recuperacion Clave",
                criticidad: BE.Criticidad.RecuperacionClave,
                detalle:    $"Solicitud de recuperacion de clave para '{username}' a las {DateTime.Now:HH:mm:ss}."
            );

            return true;
        }

        // Expone la validación de contraseña para que la GUI pueda dar feedback
        // temprano sin acceder directamente a la capa Seguridad.
        public (bool valida, string clave, string mensaje) ValidarContrasena(string contrasena)
        {
            return Encriptador.ValidarContrasena(contrasena);
        }

        // Valida credenciales sin abrir sesión — para operaciones que requieren confirmación de admin.
        // Retorna true solo si el usuario existe, no está bloqueado, la clave es correcta y tiene rol Administrador.
        public bool ValidarCredencialesAdmin(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var usuario = usuarioDAL.ObtenerPorUsername(username);
            if (usuario == null) return false;
            if (usuario.Bloqueado) return false;

            if (!Encriptador.VerificarContrasena(password, usuario.Contraseña)) return false;

            return usuario.EsAdministrador;
        }
    }
}
