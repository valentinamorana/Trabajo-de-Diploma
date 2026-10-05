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

        // "Registrar sugerencia": para un plan o una categoría de prenda (nunca ambos), con su origen.
        int RegistrarSugerencia(string modulo, BE.OrigenMetrica origen, int? idPlan, string categoriaPrenda,
                                string motivo, BE.TipoDescuento tipoSugerido, decimal beneficioEstimado);

        // "¿Acepta la sugerencia? No → Descartar sugerencia" (motivo obligatorio).
        void DescartarSugerencia(string modulo, int idSugerencia, string motivo);
    }
}
