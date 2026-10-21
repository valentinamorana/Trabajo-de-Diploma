using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>Unidad con la que se muestra un KPI (y cómo se expresa su variación).</summary>
    public enum UnidadKpi
    {
        Moneda = 0,
        /// <summary>Porcentaje: la variación se expresa en puntos porcentuales (pp).</summary>
        Porcentaje = 1,
        Cantidad = 2
    }

    /// <summary>Sentido de la variación de un KPI contra el período anterior de igual duración.</summary>
    public enum TendenciaKpi
    {
        /// <summary>No hay dato del período anterior (o el KPI es una foto actual, sin historia).</summary>
        SinDato = 0,
        Sube = 1,
        Baja = 2,
        Estable = 3
    }

    /// <summary>
    /// PN03 «Inteligencia comercial» — Un indicador de la pestaña Analizar: valor del período, valor
    /// del período anterior de la misma duración, variación (▲/▼) y una interpretación de una línea
    /// ("Las bajas subieron 4 pp respecto del período anterior"). Lo calcula BLL.Inteligencia.CalculoKpi.
    /// </summary>
    public class KpiComercial
    {
        /// <summary>Identificador estable: mrr, churn, riesgo, ingresoriesgo, descontado, ocupacion, ticket, ingresos.</summary>
        public string Clave { get; set; }
        public TextoTraducible Nombre { get; set; }
        public UnidadKpi Unidad { get; set; }
        public decimal Valor { get; set; }
        /// <summary>Valor del período anterior; null si el KPI no tiene historia (foto actual).</summary>
        public decimal? ValorAnterior { get; set; }
        /// <summary>true si que el valor suba es bueno para el negocio (ingresos); false si es malo (bajas).</summary>
        public bool SubirEsBueno { get; set; }

        /// <summary>Variación absoluta (en pp si la unidad es Porcentaje).</summary>
        public decimal? Variacion { get; set; }
        /// <summary>Variación relativa (%) contra el valor anterior; null si el anterior es 0 o no hay.</summary>
        public decimal? VariacionPorcentual { get; set; }
        public TendenciaKpi Tendencia { get; set; }
        /// <summary>true si la variación es favorable, false si es desfavorable, null si es estable o sin dato.</summary>
        public bool? EsFavorable { get; set; }
        public TextoTraducible Interpretacion { get; set; }
    }

    /// <summary>Un mes de la tendencia (altas, bajas e ingresos); los meses proyectados se dibujan punteados.</summary>
    public class PuntoMensual
    {
        public DateTime Mes { get; set; }
        public decimal Altas { get; set; }
        public decimal Bajas { get; set; }
        public decimal Ingresos { get; set; }
        public bool EsProyeccion { get; set; }
        /// <summary>Mes real incompleto (el período termina antes de fin de mes, p. ej. el mes en curso):
        /// se grafica distinto y NO entra en la regresión de la proyección.</summary>
        public bool EsParcial { get; set; }
    }

    /// <summary>Stock de una categoría por estado (gráfico de barras apiladas).</summary>
    public class StockCategoria
    {
        public string Categoria { get; set; }
        public int Disponibles { get; set; }
        public int EnUso { get; set; }
        public int EnLimpieza { get; set; }
        public int Total => Disponibles + EnUso + EnLimpieza;
    }

    /// <summary>
    /// Resultado de "Analizar" del Centro de decisiones comerciales: los KPI, la tendencia mensual con
    /// su proyección, los datos de los gráficos y el contexto con el que el motor de recomendaciones
    /// decide. Avisos: partes que no se pudieron calcular (el resto del panel igual se muestra).
    /// </summary>
    public class PanelComercial
    {
        public DateTime FechaGeneracion { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public DateTime AnteriorDesde { get; set; }
        public DateTime AnteriorHasta { get; set; }
        /// <summary>Filtro de plan (null = todos los planes).</summary>
        public int? IdPlan { get; set; }
        public string NombrePlan { get; set; }

        public List<KpiComercial> Kpis { get; set; } = new List<KpiComercial>();
        public List<PuntoMensual> Tendencia { get; set; } = new List<PuntoMensual>();
        /// <summary>Coeficiente de determinación (R²) de la regresión de ingresos: qué tan confiable es la proyección.</summary>
        public decimal? R2Ingresos { get; set; }
        public List<MetricaAbandonoPlan> RiesgoPorPlan { get; set; } = new List<MetricaAbandonoPlan>();
        public List<StockCategoria> StockPorCategoria { get; set; } = new List<StockCategoria>();
        public List<RotacionPrenda> MasPedidas { get; set; } = new List<RotacionPrenda>();
        public List<RotacionPrenda> MenosPedidas { get; set; } = new List<RotacionPrenda>();

        public ContextoComercial Contexto { get; set; }
        public List<TextoTraducible> Avisos { get; set; } = new List<TextoTraducible>();

        public KpiComercial Kpi(string clave) => Kpis.Find(k => k.Clave == clave);
    }
}
