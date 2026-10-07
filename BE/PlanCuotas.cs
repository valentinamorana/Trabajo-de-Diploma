using System;
using System.Collections.Generic;
using System.Linq;

namespace BE
{
    /// <summary>
    /// PN02 — Plan de cuotas con el que el cliente puede pagar con TARJETA DE CRÉDITO (catálogo
    /// PlanCuotas). La tarjeta financia: Caja cobra el total en un solo cobro y el sistema registra
    /// cuántas cuotas eligió el cliente y el recargo por financiación que se le suma al importe.
    /// Recargos vigentes (seed de la BD, decisión de la alumna): 1 cuota sin interés, 3 cuotas 5 %,
    /// 6 cuotas 10 %, 12 cuotas 20 %.
    /// </summary>
    public class PlanCuotas
    {
        public int     IdPlanCuotas      { get; set; }
        public int     CantidadCuotas    { get; set; }
        /// <summary>Recargo por financiación, en % sobre el total a cobrar (0 = sin interés).</summary>
        public decimal RecargoPorcentaje { get; set; }
        public bool    Activo            { get; set; } = true;

        public bool SinInteres => RecargoPorcentaje == 0;

        public override string ToString() => SinInteres
            ? $"{CantidadCuotas} cuota(s) sin interés"
            : $"{CantidadCuotas} cuotas (recargo {RecargoPorcentaje:0.##} %)";
    }

    /// <summary>Resultado de financiar un importe en cuotas («Detalle de financiación»).</summary>
    public class FinanciacionCuotas
    {
        public int     CantidadCuotas    { get; set; } = 1;
        public decimal RecargoPorcentaje { get; set; }
        /// <summary>Recargo por financiación en pesos (se suma al total del cobro).</summary>
        public decimal Recargo           { get; set; }
        /// <summary>Total que abona el cliente: total del cobro + recargo.</summary>
        public decimal TotalFinanciado   { get; set; }
        /// <summary>Valor de cada cuota (redondeado a 2 decimales).</summary>
        public decimal ValorCuota        { get; set; }
    }

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
        public static bool PermiteModalidad(PlanCuotas plan, Builders.ModalidadCobro modalidad)
            => plan != null && plan.Activo && plan.CantidadCuotas >= 1
               && plan.CantidadCuotas <= Builders.ModalidadCobroExtensiones.Meses(modalidad);

        /// <summary>Planes de cuotas que se le ofrecen al cliente para esta modalidad, de menos a más cuotas.</summary>
        public static List<PlanCuotas> Disponibles(IEnumerable<PlanCuotas> planes, Builders.ModalidadCobro modalidad)
            => (planes ?? Enumerable.Empty<PlanCuotas>())
               .Where(p => PermiteModalidad(p, modalidad))
               .OrderBy(p => p.CantidadCuotas)
               .ToList();

        /// <summary>Financia el total del cobro con el plan elegido (null = pago en un solo pago, sin recargo).</summary>
        public static FinanciacionCuotas Financiar(decimal total, PlanCuotas plan)
        {
            if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
            int cuotas = plan?.CantidadCuotas ?? 1;
            if (cuotas < 1) throw new ArgumentOutOfRangeException(nameof(plan));
            decimal porcentaje = plan?.RecargoPorcentaje ?? 0m;
            decimal recargo = Math.Round(total * porcentaje / 100m, 2, MidpointRounding.AwayFromZero);
            decimal financiado = total + recargo;
            return new FinanciacionCuotas
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
