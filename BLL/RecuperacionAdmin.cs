using Seguridad;
using Servicios;
using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Recuperación de cuentas de Administrador mediante CLAVES DE EMERGENCIA de un solo uso
    /// (tipo códigos de respaldo de Steam / 2FA).
    ///
    /// Se EXTRAJO de BLL.Usuario para respetar SRP (Single Responsibility): el ciclo de vida de
    /// los usuarios y la recuperación de acceso son responsabilidades distintas. Usa inyección de
    /// dependencias por interfaces (DIP) para poder testearse con dobles sin tocar la base de datos.
    /// </summary>
    public class RecuperacionAdmin
    {
        private readonly DAL.Interfaces.IUsuarioDAL           usuarioDAL;
        private readonly DAL.Interfaces.IClaveRecuperacionDAL claveDAL;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();

        // DI: el constructor por defecto usa los DAL reales; el otro permite inyectar dobles.
        public RecuperacionAdmin() : this(new DAL.Usuario(), new DAL.ClaveRecuperacion()) { }
        public RecuperacionAdmin(DAL.Interfaces.IUsuarioDAL usuarioDAL,
                                 DAL.Interfaces.IClaveRecuperacionDAL claveDAL)
        {
            this.usuarioDAL = usuarioDAL;
            this.claveDAL   = claveDAL;
        }

        // Genera N claves de emergencia: las hashea, persiste y exporta el .txt (copia del admin).
        // Estático para poder seedearlo al instalar (sin sesión). Devuelve la ruta del .txt.
        public static string GenerarClavesEmergencia(int cantidad)
        {
            var dal    = new DAL.ClaveRecuperacion();
            var planas = new List<string>();
            for (int i = 0; i < cantidad; i++)
            {
                string clave = GeneradorCredenciales.GenerarClaveRecuperacion();
                planas.Add(clave);
                dal.Insertar(Encriptador.Hash(clave));
            }
            return GeneradorCredenciales.ExportarClavesRecuperacion(planas);
        }

        // Claves de emergencia todavía disponibles. Sin caller en la GUI por ahora (candidato
        // natural para una futura pantalla de administración de claves de emergencia); se deja
        // documentado en vez de eliminarlo porque, a diferencia de un método realmente muerto, es
        // una consulta de una sola línea sin lógica propia que pueda desincronizarse.
        public int ContarClavesDisponibles()
        {
            return claveDAL.ContarDisponibles();
        }

        // Valida la CLAVE MAESTRA DE RECUPERACIÓN contra su hash PBKDF2 en App.config
        // (AppSettings["MasterRecoveryKeyHash"]). Es el "break glass" para autorizar operaciones
        // críticas SIN usuario cuando ningún Administrador puede ingresar (BD corrupta).
        // Devuelve false si la clave maestra está desactivada (hash vacío) o no coincide.
        // La lectura de configuración y la verificación criptográfica viven acá, no en la GUI.
        public bool ValidarClaveMaestra(string clave)
        {
            if (string.IsNullOrEmpty(clave)) return false;
            string hashMaestra =
                System.Configuration.ConfigurationManager.AppSettings["MasterRecoveryKeyHash"];
            return !string.IsNullOrEmpty(hashMaestra) &&
                   Encriptador.VerificarContrasena(clave, hashMaestra);
        }

        // Autodesbloqueo de un Administrador bloqueado mediante una clave de emergencia de un solo uso.
        // Valida: usuario existe, es Administrador y está bloqueado; la clave es válida y no usada.
        // Si todo OK: consume la clave (uso único), desbloquea la cuenta y registra en bitácora.
        public bool DesbloquearConClave(string modulo, string username, string clavePlana)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(clavePlana))
                throw new BE.AppException("err.bll.emergencia.campos",
                    "Ingresá tu usuario y una clave de emergencia.");

            string user  = username.Trim();
            string clave = clavePlana.Trim().ToUpperInvariant();   // las claves son en mayúsculas

            // Límite de intentos de la sesión (el mismo contador que el login): sin esto se podía
            // probar claves sin fin.
            if (ContadorSesion.GetInstance().LimiteAlcanzado)
                throw new BE.AppException("err.bll.emergencia.limite",
                    "Demasiados intentos fallidos en esta sesión. Reiniciá la aplicación para volver a intentarlo.");

            var disponibles = claveDAL.ObtenerDisponibles();
            if (disponibles.Count == 0)
                throw new BE.AppException("err.bll.emergencia.sin_claves",
                    "No quedan claves de emergencia. Pedile a otro Administrador que genere un nuevo set.");

            // PRIMERO se valida la clave (PBKDF2 contra cada hash disponible, mismo costo para todo
            // intento); recién con una clave válida se informa el estado de la cuenta. Antes se
            // revelaba si el usuario existía, si era Administrador y si estaba bloqueado sin
            // necesidad de tener ninguna clave.
            int idClave = -1;
            foreach (var kv in disponibles)
                if (Encriptador.VerificarContrasena(clave, kv.Value)) { idClave = kv.Key; break; }

            if (idClave < 0)
            {
                ContadorSesion.GetInstance().RegistrarIntento();
                bitacora.RegistrarSinSesion(
                    modulo:     modulo ?? "Login",
                    actividad:  "Clave de emergencia inválida",
                    criticidad: BE.Criticidad.Alta,
                    detalle:    $"Intento de desbloqueo con una clave de emergencia inválida (usuario: huella " +
                                $"{Usuario.HuellaUsuario(user)}) a las {DateTime.Now:HH:mm:ss}.");
                throw new BE.AppException("err.bll.emergencia.invalida",
                    "Usuario o clave de emergencia inválidos.");
            }

            var usuario = usuarioDAL.ObtenerPorUsername(user);
            if (usuario == null)
                throw new BE.AppException("err.bll.emergencia.invalida",
                    "Usuario o clave de emergencia inválidos.");

            // Solo Administradores pueden autodesbloquearse con clave de emergencia.
            if (!usuario.EsAdministrador)
                throw new BE.AppException("err.bll.emergencia.solo_admin",
                    "Las claves de emergencia solo desbloquean cuentas de Administrador.");

            if (!usuario.Bloqueado)
                throw new BE.AppException("err.bll.emergencia.no_bloqueada",
                    "La cuenta no está bloqueada: podés iniciar sesión normalmente.");

            // Consumir la clave (uso único). Si otro la consumió en paralelo, abortar.
            if (!claveDAL.MarcarUsada(idClave, user))
                throw new BE.AppException("err.bll.emergencia.invalida",
                    "Usuario o clave de emergencia inválidos.");

            usuarioDAL.Desbloquear(usuario.Id);

            // Resetear el contador de intentos EN MEMORIA de esta ejecución de la app.
            ContadorSesion.GetInstance().Resetear();

            bitacora.RegistrarSinSesion(
                modulo:     modulo ?? "Login",
                actividad:  BE.ActividadesBitacora.DesbloqueoConClaveDeEmergencia,
                criticidad: BE.Criticidad.Alta,
                idUsuario:  usuario.Id,
                detalle:    $"La cuenta '{user}' se autodesbloqueó con una clave de emergencia a las {DateTime.Now:HH:mm:ss}. Claves restantes: {claveDAL.ContarDisponibles()}.");
            return true;
        }
    }
}
