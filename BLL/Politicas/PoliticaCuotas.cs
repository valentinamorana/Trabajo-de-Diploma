using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Politicas
{
    /// <summary>
    /// PN02 — Reglas del pago en cuotas (sin acceso a datos: se prueban solas):
    ///   · solo los medios de pago que permiten cuotas (Tarjeta de crédito) ofrecen planes;
    ///   · no se puede financiar en más cuotas que los meses que cubre la modalidad
    ///     (Mensual: 1; Trimestral: hasta 3; Anual: hasta 12);
    ///   · recargo = total × % del plan, redondeado a 2 decimales; valor de cuota = total financiado / cuotas.
    /// </summary>
    public static class PoliticaCuotas
    {
        /// <summary>¿El plan se puede usar con esta modalidad? (activo y sin superar los meses que cubre el cobro).</summary>
        public static bool PermiteModalidad(BE.PlanCuotas plan, BE.Builders.ModalidadCobro modalidad)
            => plan != null && plan.Activo && plan.CantidadCuotas >= 1
               && plan.CantidadCuotas <= BE.Builders.ModalidadCobroExtensiones.Meses(modalidad);

        /// <summary>Planes de cuotas que se le ofrecen al cliente para esta modalidad, de menos a más cuotas.</summary>
        public static List<BE.PlanCuotas> Disponibles(IEnumerable<BE.PlanCuotas> planes, BE.Builders.ModalidadCobro modalidad)
            => (planes ?? Enumerable.Empty<BE.PlanCuotas>())
               .Where(p => PermiteModalidad(p, modalidad))
               .OrderBy(p => p.CantidadCuotas)
               .ToList();

        /// <summary>Financia el total del cobro con el plan elegido (null = pago en un solo pago, sin recargo).</summary>
        public static BE.FinanciacionCuotas Financiar(decimal total, BE.PlanCuotas plan)
        {
            if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
            int cuotas = plan?.CantidadCuotas ?? 1;
            if (cuotas < 1) throw new ArgumentOutOfRangeException(nameof(plan));
            decimal porcentaje = plan?.RecargoPorcentaje ?? 0m;
            decimal recargo = Math.Round(total * porcentaje / 100m, 2, MidpointRounding.AwayFromZero);
            decimal financiado = total + recargo;
            return new BE.FinanciacionCuotas
            {
                CantidadCuotas = cuotas,
                RecargoPorcentaje = porcentaje,
                Recargo = recargo,
                TotalFinanciado = financiado,
                ValorCuota = Math.Round(financiado / cuotas, 2, MidpointRounding.AwayFromZero)
            };
        }
    }
}
