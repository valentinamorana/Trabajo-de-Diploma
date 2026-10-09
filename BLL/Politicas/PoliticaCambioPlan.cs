using System;

namespace BLL.Politicas
{
    /// <summary>
    /// PN02 — Cambio a un plan superior (upgrade) durante un período ya pagado (decisión de la alumna,
    /// 07/10): el plan nuevo rige DESDE HOY y se cobra la diferencia. Los días que le quedaban al plan
    /// actual no se pierden: se descuentan del cobro como crédito, valuados al precio mensual del plan
    /// actual (precio × días restantes / 30).
    ///
    /// Solo es upgrade si el plan nuevo es más caro que el actual. Renovar el mismo plan, o pasar a
    /// uno igual o más barato, sigue la regla general: el período nuevo arranca al vencer el actual
    /// (así nunca se devuelve dinero).
    /// </summary>
    public static class PoliticaCambioPlan
    {
        public const int DiasPorMes = 30;

        /// <summary>
        /// Vencimiento real del período pagado: si la suscripción está pausada, al pausar el
        /// vencimiento se corrió por toda la pausa; los días de pausa que todavía no pasaron no son
        /// días pagados (misma cuenta que BLL.Cliente.DescontarPausaNoUsada).
        /// </summary>
        public static DateTime? VencimientoPagado(BE.Cliente cliente, DateTime hoy)
        {
            if (cliente?.FechaVencimiento == null) return null;
            var vence = cliente.FechaVencimiento.Value.Date;
            if (cliente.FechaPausaHasta.HasValue)
            {
                int diasPausaSinUsar = (cliente.FechaPausaHasta.Value.Date - hoy.Date).Days;
                if (diasPausaSinUsar > 0) vence = vence.AddDays(-diasPausaSinUsar);
            }
            return vence;
        }

        /// <summary>Días pagados del plan actual que todavía no se usaron (0 si ya venció).</summary>
        public static int DiasRestantes(BE.Cliente cliente, DateTime hoy)
        {
            var vence = VencimientoPagado(cliente, hoy);
            return vence.HasValue ? Math.Max(0, (vence.Value - hoy.Date).Days) : 0;
        }

        /// <summary>¿Contratar <paramref name="idPlanNuevo"/> es pasar a un plan superior con período vigente?</summary>
        public static bool EsUpgrade(BE.Cliente cliente, BE.PlanSuscripcion planActual, int idPlanNuevo,
                                     decimal precioMensualNuevo, DateTime hoy)
        {
            return cliente?.IdPlan != null
                && planActual != null
                && cliente.IdPlan.Value == planActual.IdPlan
                && planActual.IdPlan != idPlanNuevo
                && precioMensualNuevo > planActual.Precio
                && DiasRestantes(cliente, hoy) > 0;
        }

        /// <summary>
        /// ¿Contratar <paramref name="idPlanNuevo"/> es pasar a un plan IGUAL O MÁS BARATO mientras el actual
        /// sigue vigente? Esa contratación se rechaza: la regla dice que rige al vencer el período pagado, y
        /// el sistema no guarda un "plan siguiente"; se registra cuando venza (o al renovar).
        /// </summary>
        public static bool EsCambioSinUpgradeConPeriodoVigente(BE.Cliente cliente, BE.PlanSuscripcion planActual,
                                                                int idPlanNuevo, decimal precioMensualNuevo, DateTime hoy)
        {
            return cliente?.IdPlan != null
                && planActual != null
                && cliente.IdPlan.Value == planActual.IdPlan
                && planActual.IdPlan != idPlanNuevo
                && precioMensualNuevo <= planActual.Precio
                && DiasRestantes(cliente, hoy) > 0;
        }

        /// <summary>
        /// Crédito por los días no usados del plan actual (0 si no es upgrade). Nunca supera
        /// <paramref name="tope"/> (lo que queda por cobrar), para que el total no sea negativo.
        /// </summary>
        public static decimal Credito(BE.Cliente cliente, BE.PlanSuscripcion planActual, int idPlanNuevo,
                                      decimal precioMensualNuevo, DateTime hoy, decimal tope)
        {
            if (tope <= 0 || !EsUpgrade(cliente, planActual, idPlanNuevo, precioMensualNuevo, hoy)) return 0m;
            decimal credito = Math.Round(planActual.Precio * DiasRestantes(cliente, hoy) / DiasPorMes, 2,
                                         MidpointRounding.AwayFromZero);
            return Math.Min(credito, tope);
        }
    }
}
