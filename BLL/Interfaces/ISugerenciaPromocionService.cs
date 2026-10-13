using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PN03 — «Sugerencia de promoción»: Gerencia la registra tras analizar las métricas y
    /// Administración la acepta (BLL.Promocion.CrearDesdeSugerencia) o la descarta.
    /// </summary>
    public interface ISugerenciaPromocionService
    {
        List<BE.SugerenciaPromocion> ObtenerPendientes();
        List<BE.SugerenciaPromocion> ObtenerTodas();
        BE.SugerenciaPromocion ObtenerPorId(int idSugerencia);

        // "Registrar sugerencia": siempre para un plan, con su origen. El beneficio estimado lo calcula
        // la BLL: ingreso mensual en riesgo × tasa de retención (0-100] / 100.
        int RegistrarSugerencia(string modulo, BE.OrigenMetrica origen, int idPlan,
                                string motivo, BE.TipoDescuento tipoSugerido, decimal ingresoMensualEnRiesgo,
                                decimal tasaRetencion = BE.SugerenciaPromocion.TasaRetencionPorDefecto);

        // "¿Acepta la sugerencia? No → Descartar sugerencia" (motivo obligatorio).
        void DescartarSugerencia(string modulo, int idSugerencia, string motivo);
    }
}
