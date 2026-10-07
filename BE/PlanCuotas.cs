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

}
