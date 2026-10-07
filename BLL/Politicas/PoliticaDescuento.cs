using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Politicas
{
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
        public static decimal DescuentoDe(BE.Promocion promocion, decimal bruto, int meses = 1)
        {
            if (promocion == null) throw new ArgumentNullException(nameof(promocion));
            if (bruto <= 0) return 0;

            decimal d;
            switch (promocion.TipoDescuento)
            {
                case BE.TipoDescuento.Porcentaje:
                    d = Math.Round(bruto * promocion.Valor / 100m, 2);
                    break;
                case BE.TipoDescuento.MontoFijo:
                    d = promocion.Valor;
                    break;
                case BE.TipoDescuento.PrecioPromocional:
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
        /// vigentes hoy. Una promoción Vencida (PN03: llegó su FechaFin) nunca se aplica.
        /// </summary>
        public static BE.ResultadoDescuento Resolver(
            decimal bruto, int? idPlan, IEnumerable<BE.Promocion> promocionesDelPlan, decimal creditoReferido, int meses = 1)
        {
            var resultado = new BE.ResultadoDescuento { Bruto = bruto };
            if (bruto <= 0) return resultado;

            BE.Promocion mejor = null;
            decimal descuentoMejor = 0;
            if (idPlan.HasValue && promocionesDelPlan != null)
            {
                foreach (var p in promocionesDelPlan.Where(x => x != null && x.AplicaAPlan()
                                                                && x.IdPlan == idPlan && !x.EstaVencida()
                                                                && x.EstaVigente()))
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
