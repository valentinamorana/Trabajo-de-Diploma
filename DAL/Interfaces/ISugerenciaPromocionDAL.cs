using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de SugerenciaPromocion (PN03, permite dobles de prueba).</summary>
    public interface ISugerenciaPromocionDAL
    {
        List<BE.SugerenciaPromocion> ObtenerPendientes();
        BE.SugerenciaPromocion ObtenerPorId(int idSugerencia);
        int Alta(BE.SugerenciaPromocion sugerencia);
        void MarcarEvaluada(int idSugerencia);
    }
}
