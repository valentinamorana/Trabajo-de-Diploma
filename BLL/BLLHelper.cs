namespace BLL
{
    /// <summary>
    /// Helpers compartidos por todas las clases BLL.
    /// Evita duplicar la misma lógica de validación en cada clase de negocio.
    /// </summary>
    internal static class BLLHelper
    {
        // T04 — Re-validación de permisos en el BACKEND (fail-closed).
        // El Administrador siempre pasa. Sin sesión activa se rechaza la operación.
        // Centralizado acá para no repetir el mismo bloque en Pedido, Cliente, Prenda y PlanSuscripcion.
        internal static void ValidarPermiso(string nombrePatente)
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");
            if (!Seguridad.SessionManager.GetInstance().TienePermiso(nombrePatente))
                throw new BE.AppException("err.bll.sin_permiso",
                    "No tiene permiso para ejecutar esta operación ('{0}').", nombrePatente);
            ExigirVigente();
        }

        // El usuario de la sesión tiene que seguir activo y con el mismo rol en la base: un usuario
        // archivado (o al que le cambiaron el rol) con la sesión abierta no puede seguir operando.
        // Mismo control que PermisosAccion.Exigir; antes estos guards lo salteaban.
        // Excepción: con la integridad comprometida la tabla Usuario no es confiable (el ingreso se
        // validó contra el espejo) y revalidar podría dejar afuera al Administrador justo cuando
        // tiene que reparar o restaurar la base.
        internal static void ExigirVigente()
        {
            if (Configuracion.IntegridadComprometida) return;
            var sm = Seguridad.SessionManager.GetInstance();
            PermisosAccion.ExigirUsuarioVigente(sm.Usuario, sm);
        }

        // Exige una sesión activa de ADMINISTRADOR (fail-closed). Centraliza el guard que antes
        // estaba duplicado en BLL.Usuario, BLL.Backup y BLL.RecuperacionAdmin. La clave/mensaje
        // permiten personalizar el error de "sin permiso" por módulo.
        internal static void ExigirAdministrador(string claveSinPermiso, string mensajeSinPermiso)
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");
            if (!Seguridad.SessionManager.GetInstance().Usuario.EsAdministrador)
                throw new BE.AppException(claveSinPermiso, mensajeSinPermiso);
            ExigirVigente();   // p. ej. un Administrador archivado no restaura backups ni crea usuarios
        }

        // Resuelve el IdEmpleado vinculado al usuario en sesión. Centraliza el guard que antes
        // estaba duplicado, casi textual, en BLL.Pedido y BLL.Contratacion.
        internal static int ResolverEmpleadoActivo(DAL.Interfaces.IEmpleadoDAL dalEmpleado)
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");

            var usuario  = Seguridad.SessionManager.GetInstance().Usuario;
            var empleado = dalEmpleado.ObtenerPorUsuario(usuario.Id);
            if (empleado == null)
                throw new BE.AppException("err.bll.empleado_sin_vinculo",
                    "El usuario '{0}' no tiene un Empleado vinculado. " +
                    "Pedíle al Administrador que configure el vínculo.",
                    usuario.Username);
            return empleado.IdEmpleado;
        }

        // IdUsuario en sesión (PN03: quién crea, dictamina, solicita o resuelve). Fail-closed.
        internal static int ResolverUsuarioActivo()
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");
            return Seguridad.SessionManager.GetInstance().Usuario.Id;
        }

        // Exige poder gestionar usuarios/permisos: Administrador (bypass) o un rol que tenga la
        // patente de Gestión de Usuarios (mnuUsuarios). Centraliza el guard duplicado en
        // BLL.Familia y BLL.ControlMapeado.
        internal static void ExigirGestionUsuarios()
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");
            var u = Seguridad.SessionManager.GetInstance().Usuario;
            bool tieneGestion = u.EsAdministrador
                                || (u.Permisos != null && u.Permisos.Exists(p => p.NombreMenu == "mnuUsuarios"));
            if (!tieneGestion)
                throw new BE.AppException("err.bll.familia.sin_permiso",
                    "No tenés permiso para gestionar usuarios y permisos.");
            ExigirVigente();
        }
    }
}
