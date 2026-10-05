using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// PN03 — «Reporte de métricas» (Gerencia → "Analizar métricas"): abandono por plan y
    /// rotación por categoría, más las oportunidades de promoción que surgen de esos datos.
    /// Se calcula sobre los datos guardados (clientes, pedidos, prendas) cada vez que se analiza.
    /// </summary>
    public class ReporteMetricas
    {
        public DateTime Fecha { get; set; }
        public List<MetricaAbandonoPlan> AbandonoPorPlan { get; set; } = new List<MetricaAbandonoPlan>();
        public List<MetricaRotacionCategoria> RotacionPorCategoria { get; set; } = new List<MetricaRotacionCategoria>();
        public List<CandidataSugerencia> Oportunidades { get; set; } = new List<CandidataSugerencia>();

        /// <summary>Guarda de "¿Hay oportunidad?".</summary>
        public bool HayOportunidad() => Oportunidades != null && Oportunidades.Count > 0;
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
