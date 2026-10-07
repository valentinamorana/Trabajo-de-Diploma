using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// PN03 — «Reporte de métricas» (Gerencia → "Analizar métricas"): abandono por plan y
    /// rotación por categoría, más las oportunidades de promoción que surgen de esos datos.
    /// Se calcula sobre los datos guardados (clientes, pedidos, prendas) cada vez que se analiza,
    /// para un período (Desde–Hasta): la rotación cuenta los pedidos de ese período y el impacto de
    /// las promociones, los cobros (PN02 y N01) que las usaron en él.
    /// </summary>
    public class ReporteMetricas
    {
        public DateTime Fecha { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<MetricaImpactoPromocion> ImpactoPromociones { get; set; } = new List<MetricaImpactoPromocion>();
        public List<MetricaAbandonoPlan> AbandonoPorPlan { get; set; } = new List<MetricaAbandonoPlan>();
        public List<MetricaRotacionCategoria> RotacionPorCategoria { get; set; } = new List<MetricaRotacionCategoria>();
        public List<CandidataSugerencia> Oportunidades { get; set; } = new List<CandidataSugerencia>();

        /// <summary>Guarda de "¿Hay oportunidad?".</summary>
        public bool HayOportunidad() => Oportunidades != null && Oportunidades.Count > 0;
    }

    /// <summary>Impacto de una promoción en el período: cuántos cobros la usaron, cuánto se
    /// descontó y cuánto se cobró con ella (Contratacion de PN02 + HistorialCobro de N01).</summary>
    public class MetricaImpactoPromocion
    {
        public int IdPromocion { get; set; }
        public string Nombre { get; set; }
        public EstadoPromocion Estado { get; set; }
        public int Cobros { get; set; }
        public decimal TotalDescontado { get; set; }
        public decimal TotalCobrado { get; set; }
    }

    /// <summary>Abandono de un plan: clientes en riesgo y el ingreso mensual que representan.</summary>
    public class MetricaAbandonoPlan
    {
        public int IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public int ClientesEnRiesgo { get; set; }
        public decimal IngresoMensualEnRiesgo { get; set; }
    }

    /// <summary>Rotación de una categoría: prendas de baja y de alta demanda.</summary>
    public class MetricaRotacionCategoria
    {
        public string Categoria { get; set; }
        public int PrendasBajaDemanda { get; set; }
        public int PrendasAltaDemanda { get; set; }
    }
}
