using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IPlanSuscripcionDAL (sin base de datos).</summary>
    public class FakePlanSuscripcionDAL : IPlanSuscripcionDAL
    {
        public BE.PlanSuscripcion PlanPorId { get; set; }

        public List<BE.PlanSuscripcion> ObtenerActivos() => new List<BE.PlanSuscripcion>();
        // Lista devuelta por ObtenerTodos (los tests que la necesitan la siembran).
        public List<BE.PlanSuscripcion> Planes { get; set; } = new List<BE.PlanSuscripcion>();
        public List<BE.PlanSuscripcion> ObtenerTodos() => Planes;
        public BE.PlanSuscripcion ObtenerPorId(int idPlan) => PlanPorId;
        public void Alta(BE.PlanSuscripcion plan) { }
        public void Modificar(BE.PlanSuscripcion plan) { }
        public void Desactivar(int idPlan) { }
        public void Activar(int idPlan) { }
    }
}
