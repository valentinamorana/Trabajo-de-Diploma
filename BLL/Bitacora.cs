using System;
using System.Data;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para consultas de auditoría (Bitácora del sistema y de negocio).
    /// La GUI no accede directamente a Servicios.Bitacora ni a Servicios.BitacoraNegocio.
    /// </summary>
    public class Bitacora : Interfaces.IBitacoraService
    {
        private readonly Servicios.Bitacora        srvSistema = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio srvNegocio = new Servicios.BitacoraNegocio();

        // ── Registro ──────────────────────────────────────────────────────────

        /// <summary>
        /// Registra un evento del sistema sin requerir sesión activa (arranque, login fallido,
        /// excepciones no controladas). Único punto de escritura que la GUI debería usar.
        /// </summary>
        public void RegistrarSinSesion(string modulo, string actividad, BE.Criticidad criticidad,
                                        int? idUsuario = null, string detalle = null)
            => Servicios.FabricaBitacora.CrearSistema().RegistrarSinSesion(modulo, actividad, criticidad, idUsuario, detalle);

        // ── Sistema ───────────────────────────────────────────────────────────

        // Lectura de la bitácora del SISTEMA: exige permiso en la BLL (no alcanza con que la GUI
        // oculte la pestaña o el widget). Ver ExigirVerSistema. La de NEGOCIO no lleva este guard:
        // la consumen ReporteJornada y los indicadores del panel de roles operativos.
        public DataTable ObtenerTodosSistema()
        {
            ExigirVerSistema();
            return srvSistema.ObtenerTodos();
        }

        public DataTable ObtenerUltimosNDiasSistema(int dias)
        {
            ExigirVerSistema();
            return srvSistema.ObtenerUltimosNDias(dias);
        }

        public DataTable BuscarPorFiltrosSistema(
            DateTime? desde, DateTime? hasta,
            int idUsuario, string actividad, int criticidad)
        {
            ExigirVerSistema();
            return srvSistema.BuscarPorFiltros(desde, hasta, idUsuario, actividad, criticidad);
        }

        // ── Negocio ───────────────────────────────────────────────────────────

        public DataTable ObtenerTodosNegocio()
            => srvNegocio.ObtenerTodos();

        public DataTable BuscarPorFiltrosNegocio(
            DateTime? desde, DateTime? hasta,
            string tipo, int? idCliente, int? idPedido)
            => srvNegocio.BuscarPorFiltros(desde, hasta, tipo, idCliente, idPedido);

        // ── Acceso por rol ────────────────────────────────────────────────────

        /// <summary>
        /// Determina si el usuario activo puede ver la Bitácora del Sistema. LISTA BLANCA: solo el
        /// Administrador (bypass por rol) o quien tenga la patente de Auditoría (mnuAuditoria, p.
        /// ej. el rol Auditor). Antes era una lista negra que solo excluía al Gerente Comercial,
        /// así que cualquier otro rol (o uno nuevo) quedaba habilitado por omisión.
        /// </summary>
        public bool UsuarioPuedeVerSistema()
        {
            if (!Seguridad.SessionManager.IsLoggedIn) return false;
            var sm = Seguridad.SessionManager.GetInstance();
            if (sm.Usuario == null) return false;
            return sm.Usuario.EsAdministrador || sm.TienePermiso(BE.Patentes.Auditoria);
        }

        // Guard fail-closed de la lectura de la bitácora del sistema: sesión activa, permiso
        // (lista blanca de UsuarioPuedeVerSistema) y usuario todavía vigente en la base.
        private void ExigirVerSistema()
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");
            if (!UsuarioPuedeVerSistema())
                throw new BE.AppException("err.bll.bitacora.sin_permiso_sistema",
                    "No tenés permiso para consultar la bitácora del sistema.");
            BLLHelper.ExigirVigente();
        }
    }
}
