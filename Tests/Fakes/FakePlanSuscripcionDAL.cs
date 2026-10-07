using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IPlanSuscripcionDAL (sin base de datos).</summary>
    public class FakePlanSuscripcionDAL : IPlanSuscripcionDAL
    {
        public BE.PlanSuscripcion PlanPorId { get; set; }

        // Simula el WHERE Estado = 1 del DAL real sobre la misma lista que ObtenerTodos.
        public List<BE.PlanSuscripcion> ObtenerActivos() => Planes.FindAll(p => p.Estado);
        // Lista devuelta por ObtenerTodos (los tests que la necesitan la siembran).
        public List<BE.PlanSuscripcion> Planes { get; set; } = new List<BE.PlanSuscripcion>();
        public List<BE.PlanSuscripcion> ObtenerTodos() => Planes;
        // Si el plan está en Planes se devuelve ese (upgrade: plan actual y plan nuevo); si no, PlanPorId.
        public BE.PlanSuscripcion ObtenerPorId(int idPlan) => Planes.Find(p => p.IdPlan == idPlan) ?? PlanPorId;
        // Espías de Alta/Modificar: el plan tal como llegó al DAL.
        public BE.PlanSuscripcion UltimoAlta { get; private set; }
        public BE.PlanSuscripcion UltimoModificado { get; private set; }
        public void Alta(BE.PlanSuscripcion plan) => UltimoAlta = plan;
        public void Modificar(BE.PlanSuscripcion plan) => UltimoModificado = plan;
        public int DesactivarVeces { get; private set; }
        public void Desactivar(int idPlan) => DesactivarVeces++;
        public void Activar(int idPlan) { }
    }
}
