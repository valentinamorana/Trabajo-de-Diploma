using System;
using System.Collections.Generic;
using System.Linq;

namespace BE
{
    /// <summary>Resultado de resolver qué descuento corresponde a un cobro.</summary>
    public class ResultadoDescuento
    {
        /// <summary>Importe bruto sobre el que se calculó (precio del plan por cobro).</summary>
        public decimal Bruto { get; set; }

        /// <summary>Descuento aplicado (0 si no corresponde ninguno).</summary>
        public decimal Descuento { get; set; }

        /// <summary>Importe a cobrar sin cargos adicionales: Bruto - Descuento (nunca negativo).</summary>
        public decimal Total => Math.Max(0, Bruto - Descuento);

        /// <summary>Promoción vigente aplicada, o null si se aplicó el crédito por referido (o ninguno).</summary>
        public Promocion Promocion { get; set; }

        /// <summary>true si el descuento aplicado es el crédito acumulado por referidos.</summary>
        public bool UsaCreditoReferido { get; set; }
    }

    /// <summary>Liquidación de una contratación: qué se cobra, con qué descuento y qué comprobante se emitió.</summary>
    public class LiquidacionContratacion
    {
        public decimal Bruto { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total => System.Math.Max(0, Bruto - Descuento);
        public string NombrePromocion { get; set; }
        public bool UsaCreditoReferido { get; set; }
        /// <summary>Número de comprobante emitido (null mientras solo se está calculando el importe).</summary>
        public string NumeroComprobante { get; set; }
    }

    /// <summary>
    /// PN03 — Política de descuentos del cobro. Regla tomada de NUULY (5.1): "solo se puede aplicar
    /// UN descuento por ciclo de facturación; los no usados quedan acumulados para un ciclo futuro".
    ///
    /// Compiten dos fuentes:
    ///   · las promociones VIGENTES que aplican al plan del cliente (PN03: sugeridas por Gerencia,
    ///     definidas por Administración y aprobadas por Contabilidad);
    ///   · el crédito por referidos acumulado en el cliente (Cliente.DescuentoProximoCobro).
    /// Se aplica UNA sola: la de mayor descuento. Si gana la promoción, el crédito por referido NO se
    /// consume y sigue acumulado; si gana el crédito, se consume. En un empate se prefiere la promoción
    /// (así el crédito acumulado no se pierde). Las promociones por categoría de prenda son
    /// informativas: no tienen un importe sobre el cual aplicarse en el cobro de la suscripción.
    /// </summary>
    public static class PoliticaDescuento
    {
        /// <summary>Descuento que aporta una promoción sobre un importe bruto.</summary>
        public static decimal DescuentoDe(Promocion promocion, decimal bruto, int meses = 1)
        {
            if (promocion == null) throw new ArgumentNullException(nameof(promocion));
            if (bruto <= 0) return 0;

            decimal d;
            switch (promocion.TipoDescuento)
            {
                case TipoDescuento.Porcentaje:
                    d = Math.Round(bruto * promocion.Valor / 100m, 2);
                    break;
                case TipoDescuento.MontoFijo:
                    d = promocion.Valor;
                    break;
                case TipoDescuento.PrecioPromocional:
                    // Valor = precio promocional MENSUAL (como Plan.Precio): el descuento es la diferencia
                    // con el bruto, que cubre `meses` meses.
                    d = bruto - promocion.Valor * Math.Max(1, meses);
                    break;
                default:
                    d = 0;
                    break;
            }
            return Math.Min(Math.Max(0, d), bruto);
        }

        /// <summary>
        /// Resuelve el descuento de un cobro. <paramref name="promocionesDelPlan"/> puede traer
        /// cualquier promoción: se ignoran las que no aplican a <paramref name="idPlan"/> o no están
        /// vigentes hoy.
        /// </summary>
        public static ResultadoDescuento Resolver(
            decimal bruto, int? idPlan, IEnumerable<Promocion> promocionesDelPlan, decimal creditoReferido, int meses = 1)
        {
            var resultado = new ResultadoDescuento { Bruto = bruto };
            if (bruto <= 0) return resultado;

            Promocion mejor = null;
            decimal descuentoMejor = 0;
            if (idPlan.HasValue && promocionesDelPlan != null)
            {
                foreach (var p in promocionesDelPlan.Where(x => x != null && x.AplicaAPlan()
                                                                && x.IdPlan == idPlan && x.EstaVigente()))
                {
                    decimal d = DescuentoDe(p, bruto, meses);
                    if (d > descuentoMejor) { descuentoMejor = d; mejor = p; }
                }
            }

            decimal credito = Math.Min(Math.Max(0, creditoReferido), bruto);

            if (mejor != null && descuentoMejor >= credito)
            {
                resultado.Promocion = mejor;
                resultado.Descuento = descuentoMejor;
            }
            else if (credito > 0)
            {
                resultado.UsaCreditoReferido = true;
                resultado.Descuento = credito;
            }
            return resultado;
        }
    }
}
