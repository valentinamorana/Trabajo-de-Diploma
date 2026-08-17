using Seguridad;
using System.Collections.Generic;

namespace BLL
{
    // Partial de BLL.Usuario — RF-10: desbloqueo, baja lógica (archivado) y purga física de
    // usuarios. Ver BLL.Usuario (Usuario.cs) para el resto de grupos.
    public partial class Usuario
    {
        // RF-10 — Días de retención antes de habilitar la purga física de un usuario archivado.
        // Como en una empresa real: el ex-empleado queda "archivado" 1 año (no contamina la
        // operación ni las métricas) y recién después puede eliminarse definitivamente.
        public const int DiasRetencionPurga = 365;

        // Desbloquea la cuenta de un usuario y resetea el contador de intentos. Solo Administrador.
        public void Desbloquear(string modulo, int idUsuario, string usernameObjetivo)
        {
            ValidarEsAdministrador();

            // T07 — Verificar integridad de la base ANTES de modificar usuarios.
            Configuracion.AsegurarIntegridadUsuarios();

            new VersionUsuario().GrabarVersion(idUsuario,
                SessionManager.GetInstance().Usuario.Username,
                $"Snapshot antes de desbloqueo por '{SessionManager.GetInstance().Usuario.Username}'.");

            usuarioDAL.Desbloquear(idUsuario);

            bitacora.Registrar(modulo,
                $"Desbloqueo de Cuenta: '{usernameObjetivo}'",
                BE.Criticidad.Alta);
        }

        // RF-10 — Baja LÓGICA (archivar) de un usuario. Solo Administrador.
        // Reglas de protección:
        //   • No se puede archivar al propio usuario en sesión.
        //   • No se puede archivar al ÚLTIMO Administrador activo del sistema.
        // Se graba un snapshot (Memento) antes para preservar trazabilidad (RF-14/18).
        public void Eliminar(string modulo, int idUsuario, string usernameObjetivo)
        {
            ValidarEsAdministrador();

            // T07 — Verificar integridad de la base ANTES de modificar usuarios.
            Configuracion.AsegurarIntegridadUsuarios();

            var admin = SessionManager.GetInstance().Usuario;

            // Determinar el perfil del usuario objetivo para la protección del último admin.
            var objetivo = usuarioDAL.ObtenerPorUsername(usernameObjetivo);
            string perfilObjetivo = objetivo?.Perfil ?? "";
            ValidarPuedeArchivar(perfilObjetivo, idUsuario, admin.Id,
                                 usuarioDAL.ContarAdministradoresActivos());

            // Snapshot del estado actual antes de archivar (control de cambios).
            new VersionUsuario().GrabarVersion(idUsuario, admin.Username,
                $"Snapshot antes de archivar (baja lógica) por '{admin.Username}'.");

            usuarioDAL.BajaLogica(idUsuario);

            bitacora.RegistrarSinSesion(
                modulo:     modulo,
                actividad:  "Baja Logica Usuario",
                criticidad: BE.Criticidad.Alta,
                idUsuario:  admin.Id,
                detalle:    $"Admin '{admin.Username}' archivó al usuario '{usernameObjetivo}' (ID {idUsuario}) a las {System.DateTime.Now:HH:mm:ss}.");
        }

        // RF-10 — Reglas PURAS de protección para archivar un usuario. Se extraen acá para poder
        // testearlas sin sesión ni base de datos (caso de prueba "eliminar el último admin"):
        //   • No se puede archivar al usuario que tiene la sesión abierta.
        //   • No se puede archivar al último Administrador activo del sistema.
        public static void ValidarPuedeArchivar(string perfilObjetivo, int idObjetivo,
                                                 int idEnSesion, int adminsActivos)
        {
            if (idEnSesion == idObjetivo)
                throw new BE.AppException("err.bll.usuario.autobaja",
                    "No podés archivar tu propio usuario mientras tenés la sesión abierta.");

            if (BE.Roles.EsAdministrador(perfilObjetivo)
                && adminsActivos <= 1)
                throw new BE.AppException("err.bll.usuario.ultimo_admin",
                    "No se puede archivar al último Administrador activo del sistema. " +
                    "Creá o activá otro Administrador antes de archivar este.");
        }

        // RF-10 — Lista de usuarios archivados (Activo=0) para la vista de gestión.
        public List<BE.Usuario> ObtenerArchivados()
        {
            return usuarioDAL.ObtenerArchivados();
        }

        // RF-10 — Usuarios archivados elegibles para purga física (archivados hace más de 1 año).
        public List<BE.Usuario> ObtenerArchivadosParaPurga()
        {
            return usuarioDAL.ObtenerArchivadosParaPurga(DiasRetencionPurga);
        }

        // RF-10 — Purga FÍSICA de todos los usuarios archivados con más de DiasRetencionPurga
        // días de antigüedad. Solo Administrador. Devuelve cuántos se eliminaron definitivamente.
        public int PurgarArchivados(string modulo)
        {
            ValidarEsAdministrador();
            Configuracion.AsegurarIntegridadUsuarios();

            var purgables = usuarioDAL.ObtenerArchivadosParaPurga(DiasRetencionPurga);
            if (purgables.Count == 0) return 0;

            var admin = SessionManager.GetInstance().Usuario;
            int eliminados = 0;
            foreach (var u in purgables)
            {
                usuarioDAL.EliminarFisico(u.Id);
                eliminados++;
            }

            bitacora.RegistrarSinSesion(
                modulo:     modulo,
                actividad:  "Purga Usuarios Archivados",
                criticidad: BE.Criticidad.Alta,
                idUsuario:  admin.Id,
                detalle:    $"Admin '{admin.Username}' purgó definitivamente {eliminados} usuario(s) archivado(s) con más de {DiasRetencionPurga} días a las {System.DateTime.Now:HH:mm:ss}.");

            return eliminados;
        }
    }
}
