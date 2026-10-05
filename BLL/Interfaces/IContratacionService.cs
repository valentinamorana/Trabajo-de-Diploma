using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PN02 — Comercialización de la suscripción. Un método por actividad del diagrama de
    /// actividad (ver el encabezado de <see cref="BLL.Contratacion"/>).
    /// </summary>
    public interface IContratacionService
    {
        // ── Vendedor ──
        List<BE.Cliente> IdentificarCliente(string identificacion);
        List<BE.PlanSuscripcion> PresentarPlanes();
        int AsentarDesistimiento(string modulo, int idCliente, int? idPlan, BE.Builders.ModalidadCobro? modalidad, string motivo);
        BE.PlanSuscripcion ValidarContratacion(int idCliente, int idPlan);
        int RegistrarContratacion(string modulo, int idCliente, int idPlan, BE.Builders.ModalidadCobro modalidad);
        BE.LiquidacionContratacion EstimarImporte(int idCliente, int idPlan, BE.Builders.ModalidadCobro modalidad);

        // ── Caja ──
        List<BE.Contratacion> ObtenerPendientesDePago();
        List<BE.Contratacion> ObtenerResueltas();
        BE.Contratacion ObtenerPorId(int idContratacion);
        List<BE.MedioPago> ObtenerMediosPago();
        List<BE.IntentoPago> ObtenerIntentos(int idContratacion);
        BE.DesistimientoContratacion ObtenerDesistimiento(int idDesistimiento);
        BE.LiquidacionContratacion CalcularImporte(BE.Contratacion contratacion);
        Dictionary<int, BE.LiquidacionContratacion> CalcularImportes(List<BE.Contratacion> contrataciones);
        BE.LiquidacionContratacion ConfirmarCobro(string modulo, BE.Contratacion contratacion, int idMedioPago, decimal? importeConfirmado = null);
        int ContarPendientesDePago();
        BE.ResultadoIntentoPago RegistrarIntentoFallido(string modulo, BE.Contratacion contratacion, int? idMedioPago, string motivo);
    }
}
