using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de SugerenciaPromocion (PN03, permite dobles de prueba).</summary>
    public interface ISugerenciaPromocionDAL
    {
        List<BE.SugerenciaPromocion> ObtenerPendientes();
        BE.SugerenciaPromocion ObtenerPorId(int idSugerencia);
        int Alta(BE.SugerenciaPromocion sugerencia);
        // Reclamo atómico: solo pasa de Pendiente a Evaluada UNA vez. Devuelve false si otra sesión ya la evaluó.
        bool MarcarEvaluada(int idSugerencia);

        // Compensación: devuelve a Pendiente una sugerencia recién reclamada si la promoción no llegó a crearse.
        void ReabrirEvaluacion(int idSugerencia);
    }
}
