using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de SugerenciaPromocion (PN03, permite dobles de prueba).</summary>
    public interface ISugerenciaPromocionDAL
    {
        List<BE.SugerenciaPromocion> ObtenerPendientes();
        List<BE.SugerenciaPromocion> ObtenerTodas();
        BE.SugerenciaPromocion ObtenerPorId(int idSugerencia);
        int Alta(BE.SugerenciaPromocion sugerencia);

        // Claim atómico: Pendiente → Evaluada UNA sola vez. false si otra sesión ya la evaluó o descartó.
        bool MarcarEvaluada(int idSugerencia, DateTime fecha);

        // Compensación: devuelve a Pendiente una sugerencia recién reclamada si la promoción no llegó a crearse.
        void ReabrirEvaluacion(int idSugerencia);

        // Claim atómico: Pendiente → Descartada con su motivo. false si otra sesión ya la evaluó o descartó.
        bool Descartar(int idSugerencia, string motivo, DateTime fecha);
    }
}
