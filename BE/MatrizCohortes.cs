using System;
using System.Collections.Generic;
using System.Linq;

namespace BE
{
    // ══════════════════════════════════════════════════════════════════════════
    // PN03 «Análisis de cohortes» (Centro de decisiones comerciales, pestaña Cohortes).
    // Modelo ya calculado por BLL.Inteligencia.AnalisisCohortes: la GUI y el Informe solo lo dibujan.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Tramo del mapa de calor (BLL AnalisisCohortes.Nivel): ≥ 90 %, 75–89 %, 50–74 %, &lt; 50 %.</summary>
    public enum NivelRetencion { Alta, Media, Baja, Critica }

    /// <summary>Retención de una cohorte en el mes k desde su alta (k = 0 es el mes del alta).</summary>
    public class CeldaCohorte
    {
        public int Desplazamiento { get; set; }
        /// <summary>Clientes de la cohorte sin baja registrada al cierre del mes k (o al corte, si es parcial).</summary>
        public int Activos { get; set; }
        /// <summary>Activos / tamaño de la cohorte × 100, redondeado a un decimal (para mostrar; las
        /// comparaciones usan Activos / tamaño exacto).</summary>
        public decimal Retencion { get; set; }
        /// <summary>El mes k todavía no terminó al corte (p. ej. el mes en curso): se muestra con
        /// asterisco, pero no entra en la curva promedio ni en la comparación de la peor cohorte.</summary>
        public bool EsParcial { get; set; }
    }

    /// <summary>Una fila de la matriz: los clientes que se suscribieron en un mismo mes.</summary>
    public class FilaCohorte
    {
        /// <summary>Primer día del mes de alta.</summary>
        public DateTime Mes { get; set; }
        public int Tamanio { get; set; }
        /// <summary>Una celda por mes transcurrido desde el alta (0..), solo hasta el mes del corte.</summary>
        public List<CeldaCohorte> Celdas { get; set; } = new List<CeldaCohorte>();
        /// <summary>Altas de la cohorte por plan (IdPlan → clientes) y bajas por plan dejado.</summary>
        public Dictionary<int, int> AltasPorPlan { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> BajasPorPlan { get; set; } = new Dictionary<int, int>();
        /// <summary>Tiene al menos MinimoClientes clientes (entra en la comparación de la peor cohorte).</summary>
        public bool Comparable { get; set; }
        public bool EsPeor { get; set; }

        /// <summary>Celda del mes k (null si la cohorte todavía no llegó a ese mes).</summary>
        public CeldaCohorte Celda(int desplazamiento) => Celdas.FirstOrDefault(c => c.Desplazamiento == desplazamiento);
    }

    /// <summary>Un punto de la curva de retención promedio.</summary>
    public class PuntoCurvaRetencion
    {
        public int Desplazamiento { get; set; }
        /// <summary>Promedio ponderado por tamaño (Σ activos / Σ tamaño) de las cohortes con el mes k
        /// completo; null si ninguna lo completó todavía.</summary>
        public decimal? Retencion { get; set; }
        public int Cohortes { get; set; }
        public int Clientes { get; set; }
    }

    /// <summary>La cohorte que peor retiene en el mes de comparación y el plan que más explica sus bajas.</summary>
    public class PeorCohorte
    {
        public DateTime Mes { get; set; }
        public int Tamanio { get; set; }
        /// <summary>Mes desde el alta en que se compararon las cohortes.</summary>
        public int Desplazamiento { get; set; }
        public decimal Retencion { get; set; }
        /// <summary>Retención promedio de la curva en ese mismo mes.</summary>
        public decimal? RetencionPromedio { get; set; }
        /// <summary>Bajas de la cohorte hasta el cierre del mes de comparación.</summary>
        public int Bajas { get; set; }
        public int? IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public int BajasDelPlan { get; set; }
        /// <summary>Clave de la recomendación de bajas del plan en el motor (ReglaChurnPlan): "ChurnPlan:&lt;IdPlan&gt;".</summary>
        public string ClaveRecomendacion { get; set; }
    }

    /// <summary>Matriz de retención por cohorte de alta, con la curva promedio y la peor cohorte.</summary>
    public class MatrizCohortes
    {
        public DateTime Corte { get; set; }
        public int? IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public int MinimoClientes { get; set; }
        public int MesComparacion { get; set; }
        /// <summary>Último mes desde el alta que muestra la matriz (columnas 0..MaximoDesplazamiento).</summary>
        public int MaximoDesplazamiento { get; set; }
        /// <summary>Cohortes de la más antigua a la más reciente (solo meses con altas).</summary>
        public List<FilaCohorte> Cohortes { get; set; } = new List<FilaCohorte>();
        public List<PuntoCurvaRetencion> Curva { get; set; } = new List<PuntoCurvaRetencion>();
        /// <summary>null si no hay cohortes comparables o todas retienen el 100 % (ver MotivoSinPeor).</summary>
        public PeorCohorte Peor { get; set; }
        public TextoTraducible MotivoSinPeor { get; set; }
        /// <summary>Oración que resume la peor cohorte (o por qué no la hay).</summary>
        public TextoTraducible Resumen { get; set; }
        public bool HayParciales => Cohortes.Any(c => c.Celdas.Any(x => x.EsParcial));
        public int TotalClientes => Cohortes.Sum(c => c.Tamanio);
        public bool SinDatos => Cohortes.Count == 0;
        /// <summary>Hay cohortes, pero todavía ninguna completó un mes (todas en curso): no hay curva.</summary>
        public bool SinMesesCompletos => !SinDatos && !Curva.Any(p => p.Retencion.HasValue);
    }
}
