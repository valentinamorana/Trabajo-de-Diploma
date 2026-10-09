using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// PN03 — Lo que Contabilidad ve en "Analizar margen e impacto": la promoción, el beneficio
    /// estimado de la sugerencia que le dio origen (si existe), las otras promociones Vigentes
    /// del mismo plan que se superponen en fechas (advertencia: no impide aprobar) y el cálculo
    /// del margen proyectado.
    ///
    /// Cálculo (todo en importes MENSUALES, la misma base que el beneficio de Gerencia, que es el
    /// ingreso mensual en riesgo del plan):
    ///   descuento por cliente  = lo que la promoción descuenta sobre el precio mensual del plan
    ///                            (misma regla que el cobro: BLL.Politicas.PoliticaDescuento.DescuentoDe)
    ///   costo del descuento    = descuento por cliente × clientes activos del plan
    ///   beneficio estimado     = el de la sugerencia de Gerencia; en un alta manual, el margen
    ///                            estimado que cargó Administración
    ///   margen proyectado      = beneficio estimado − costo del descuento
    ///   ¿conviene?             = margen proyectado &gt; 0
    /// Las promociones por categoría son informativas (no descuentan en el cobro): no tienen costo
    /// proyectado y el resultado no se calcula. Es una recomendación: la decisión es de Contabilidad.
    /// </summary>
    public class AnalisisImpactoPromocion
    {
        public Promocion Promocion { get; set; }
        public decimal? BeneficioEstimadoSugerencia { get; set; }
        public OrigenMetrica? OrigenSugerencia { get; set; }
        public List<Promocion> Superpuestas { get; set; } = new List<Promocion>();

        /// <summary>false si el usuario en sesión es quien creó la promoción (no puede dictaminarla).</summary>
        public bool UsuarioPuedeDictaminar { get; set; }

        public bool TieneSuperposicion() => Superpuestas != null && Superpuestas.Count > 0;

        // ── Margen proyectado ─────────────────────────────────────────────────

        /// <summary>Promoción por categoría de prenda: no descuenta en el cobro, el análisis es informativo.</summary>
        public bool EsInformativa { get; set; }

        /// <summary>Precio mensual del plan destino. Null si es por categoría o el plan ya no existe.</summary>
        public decimal? PrecioPlan { get; set; }

        /// <summary>Clientes activos asignados al plan destino (a quienes se les aplicaría el descuento).</summary>
        public int ClientesActivosPlan { get; set; }

        /// <summary>Descuento mensual que recibe cada cliente del plan.</summary>
        public decimal DescuentoPorCliente { get; set; }

        /// <summary>Costo mensual proyectado del descuento: descuento por cliente × clientes activos.</summary>
        public decimal CostoDescuentoProyectado => DescuentoPorCliente * ClientesActivosPlan;

        /// <summary>true si el beneficio sale de la sugerencia de Gerencia; false si es el margen que cargó Administración.</summary>
        public bool BeneficioDeSugerencia => BeneficioEstimadoSugerencia.HasValue;

        /// <summary>Beneficio mensual contra el que se compara el costo del descuento.</summary>
        public decimal BeneficioEstimado =>
            BeneficioEstimadoSugerencia ?? (Promocion != null ? Promocion.MargenEstimado : 0m);

        /// <summary>Hay datos para calcular el margen: promoción por plan con su precio.</summary>
        public bool MargenCalculable => !EsInformativa && PrecioPlan.HasValue && !SugerenciaOrigenPerdida;

        /// <summary>La promoción nació de una sugerencia que ya no se encuentra: no hay beneficio de
        /// referencia confiable (el margen que carga Administración no aplica a ese caso).</summary>
        public bool SugerenciaOrigenPerdida =>
            Promocion?.IdSugerenciaOrigen != null && !BeneficioEstimadoSugerencia.HasValue;

        /// <summary>Beneficio estimado − costo proyectado del descuento (mensual).</summary>
        public decimal MargenProyectado => MargenCalculable ? BeneficioEstimado - CostoDescuentoProyectado : 0m;

        /// <summary>Recomendación: true si el margen proyectado es positivo; null si no se puede calcular.</summary>
        public bool? Conviene => MargenCalculable ? MargenProyectado > 0 : (bool?)null;
    }
}
