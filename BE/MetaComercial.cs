using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Indicador que puede tener una meta mensual (tabla MetaComercial, columna Indicador). Para sumar
    /// uno nuevo: un valor acá (sin reutilizar números) y su definición en
    /// BLL.Inteligencia.CatalogoIndicadores (nombre, unidad, sentido, rango, cómo se mide y qué reglas lo mueven).
    /// </summary>
    public enum IndicadorMeta
    {
        /// <summary>Ingreso recurrente mensual (MRR, $) al cierre del mes.</summary>
        Mrr = 0,
        /// <summary>% de bajas del mes (bajas / clientes al inicio del mes × 100).</summary>
        PorcentajeBajas = 1,
        /// <summary>Ocupación del stock (% de prendas activas en uso; foto del día).</summary>
        OcupacionStock = 2
    }

    /// <summary>Qué significa cumplir la meta: llegar al valor (mayor es mejor) o no pasarlo (menor es mejor).</summary>
    public enum SentidoMeta
    {
        MayorEsMejor = 0,
        MenorEsMejor = 1
    }

    /// <summary>Semáforo de una meta (ver BLL.Inteligencia.MetasComerciales.Semaforo).</summary>
    public enum EstadoMeta
    {
        /// <summary>Mes cerrado que llegó al objetivo, o mes en curso que ya llegó y la proyección lo sostiene.</summary>
        Cumplida = 0,
        /// <summary>Mes en curso: la proyección al cierre llega al objetivo.</summary>
        EnCamino = 1,
        /// <summary>Mes en curso: la proyección al cierre no llega (todavía hay tiempo de actuar).</summary>
        EnRiesgo = 2,
        /// <summary>Mes cerrado que no llegó, o un máximo acumulativo que ya se pasó en el mes en curso.</summary>
        NoCumplida = 3,
        /// <summary>No se puede medir (sin datos del mes o el indicador no tiene historia).</summary>
        SinDatos = 4,
        /// <summary>Mes futuro: todavía no empezó.</summary>
        Pendiente = 5
    }

    /// <summary>
    /// PN03 — «Meta comercial»: el valor que Gerencia se propone para un indicador en un mes
    /// (tabla MetaComercial, una por indicador y mes). Solo se edita para el mes en curso o los futuros.
    /// Sin dígito verificador, como el resto de las tablas de PN03 (DecisionComercial, SugerenciaPromocion).
    /// </summary>
    public class MetaComercial
    {
        public int IdMeta { get; set; }
        /// <summary>Primer día del mes de la meta.</summary>
        public DateTime Periodo { get; set; }
        public IndicadorMeta Indicador { get; set; }
        public decimal ValorObjetivo { get; set; }
        public SentidoMeta Sentido { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; }
        /// <summary>Fecha del alta o de la última modificación.</summary>
        public DateTime Fecha { get; set; }

        public static DateTime InicioDeMes(DateTime dia) => new DateTime(dia.Year, dia.Month, 1);
    }

    /// <summary>Un mes de la serie real contra meta (gráfico de la pestaña Metas).</summary>
    public class PuntoMeta
    {
        public DateTime Mes { get; set; }
        /// <summary>Valor real al cierre del mes (o al día de hoy en el mes en curso); null sin dato.</summary>
        public decimal? Real { get; set; }
        /// <summary>Objetivo del mes; null si no se fijó meta.</summary>
        public decimal? Objetivo { get; set; }
        /// <summary>Proyección al cierre (solo el mes en curso).</summary>
        public decimal? Proyeccion { get; set; }
        public bool EsMesEnCurso { get; set; }
    }

    /// <summary>Seguimiento de una meta: real contra objetivo, avance, proyección al cierre y semáforo.</summary>
    public class SeguimientoMeta
    {
        public MetaComercial Meta { get; set; }
        public TextoTraducible NombreIndicador { get; set; }
        public UnidadKpi Unidad { get; set; }
        /// <summary>Valor real al cierre del mes (mes cerrado) o a hoy (mes en curso); null sin dato.</summary>
        public decimal? ValorActual { get; set; }
        /// <summary>Proyección al cierre del mes en curso; null si no corresponde o no hay datos suficientes.</summary>
        public decimal? Proyeccion { get; set; }
        /// <summary>Avance en %: mayor es mejor = real / objetivo; menor es mejor = real / máximo permitido.</summary>
        public decimal? AvancePorcentaje { get; set; }
        public EstadoMeta Estado { get; set; }
        public bool EsMesEnCurso { get; set; }
        public int DiasTranscurridos { get; set; }
        public int DiasDelMes { get; set; }
        /// <summary>Explicación del semáforo (con los números).</summary>
        public TextoTraducible Explicacion { get; set; }
        /// <summary>Real contra meta de los últimos meses (para el gráfico).</summary>
        public List<PuntoMeta> Serie { get; set; } = new List<PuntoMeta>();

        /// <summary>¿Hay que actuar? (en riesgo, o ya perdida en el mes en curso).</summary>
        public bool RequiereAccion => Estado == EstadoMeta.EnRiesgo
            || (Estado == EstadoMeta.NoCumplida && EsMesEnCurso);
    }

    /// <summary>
    /// PN03 — Resumen de los avisos al iniciar sesión (Gerencia): qué hay para decidir hoy en el Centro
    /// de decisiones. Lo calcula BLL.Inteligencia.AvisosComerciales.
    /// </summary>
    public class ResumenAvisosComerciales
    {
        /// <summary>Recomendaciones de prioridad Alta que se pueden decidir.</summary>
        public int RecomendacionesAlta { get; set; }
        /// <summary>Decisiones pospuestas cuya fecha de revisión es hoy o ya pasó.</summary>
        public int PospuestasVencidas { get; set; }
        /// <summary>Metas del mes en curso en riesgo (o ya perdidas).</summary>
        public int MetasEnRiesgo { get; set; }
        /// <summary>Títulos de lo más urgente (en español, para la bitácora y el tooltip).</summary>
        public List<TextoTraducible> Destacados { get; set; } = new List<TextoTraducible>();

        public bool HayAlgo => RecomendacionesAlta > 0 || PospuestasVencidas > 0 || MetasEnRiesgo > 0;
    }
}
