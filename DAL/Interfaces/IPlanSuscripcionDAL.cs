using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de PlanSuscripcion (permite inyección y dobles de prueba).</summary>
    public interface IPlanSuscripcionDAL
    {
        List<BE.PlanSuscripcion> ObtenerActivos();
        List<BE.PlanSuscripcion> ObtenerTodos();
        BE.PlanSuscripcion ObtenerPorId(int idPlan);
        void Alta(BE.PlanSuscripcion plan);
        void Modificar(BE.PlanSuscripcion plan);
        void Desactivar(int idPlan);
        void Activar(int idPlan);
    }
}
