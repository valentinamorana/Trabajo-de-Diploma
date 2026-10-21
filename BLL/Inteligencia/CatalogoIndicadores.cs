using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia.Reglas;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Metas comerciales» — Catálogo de los indicadores que admiten una meta mensual. Cada uno dice:
    /// nombre (traducible), unidad, sentido (mayor o menor es mejor), rango válido del objetivo, si es
    /// ACUMULATIVO dentro del mes (solo puede empeorar: el % de bajas), si tiene historia diaria para
    /// proyectar y qué reglas del motor lo mueven (cuando la meta está en riesgo, esas recomendaciones
    /// suben de prioridad). Para sumar un indicador: un valor en BE.IndicadorMeta, una entrada acá y cómo
    /// se mide en MetasComerciales.ValorDiario.
    /// </summary>
    public static class CatalogoIndicadores
    {
        public sealed class Definicion
        {
            public BE.IndicadorMeta Indicador { get; internal set; }
            public string Clave { get; internal set; }
            public string Nombre { get; internal set; }
            public BE.UnidadKpi Unidad { get; internal set; }
            public BE.SentidoMeta Sentido { get; internal set; }
            public decimal Minimo { get; internal set; }
            public decimal Maximo { get; internal set; }
            /// <summary>Dentro del mes solo crece (bajas acumuladas): con "menor es mejor", pasarse es definitivo.</summary>
            public bool EsAcumulativo { get; internal set; }
            /// <summary>Se puede reconstruir día por día (y por lo tanto proyectar y medir meses cerrados).</summary>
            public bool TieneHistoria { get; internal set; }
            /// <summary>Reglas del motor cuyas recomendaciones mueven el indicador.</summary>
            public IReadOnlyList<string> Reglas { get; internal set; }

            public BE.TextoTraducible NombreTraducible => new BE.TextoTraducible("cdc.meta.ind." + Clave, Nombre);
        }

        private static readonly List<Definicion> Definiciones = new List<Definicion>
        {
            new Definicion
            {
                Indicador = BE.IndicadorMeta.Mrr, Clave = "mrr", Nombre = "Ingreso recurrente mensual (MRR)",
                Unidad = BE.UnidadKpi.Moneda, Sentido = BE.SentidoMeta.MayorEsMejor, Minimo = 1m, Maximo = 1000000000m,
                EsAcumulativo = false, TieneHistoria = true,
                Reglas = new[] { ReglaChurnPlan.Codigo, ReglaRenovaciones.Codigo, ReglaUpgradePlan.Codigo, ReglaRendimientoPromocion.Codigo }
            },
            new Definicion
            {
                Indicador = BE.IndicadorMeta.PorcentajeBajas, Clave = "churn", Nombre = "% de bajas",
                Unidad = BE.UnidadKpi.Porcentaje, Sentido = BE.SentidoMeta.MenorEsMejor, Minimo = 0m, Maximo = 100m,
                EsAcumulativo = true, TieneHistoria = true,
                Reglas = new[] { ReglaChurnPlan.Codigo, ReglaRenovaciones.Codigo, ReglaRendimientoPromocion.Codigo }
            },
            new Definicion
            {
                Indicador = BE.IndicadorMeta.OcupacionStock, Clave = "ocupacion", Nombre = "Ocupación del stock",
                Unidad = BE.UnidadKpi.Porcentaje, Sentido = BE.SentidoMeta.MayorEsMejor, Minimo = 1m, Maximo = 100m,
                EsAcumulativo = false, TieneHistoria = false,
                // Sube si se retira el stock ocioso (menos prendas activas) o si los clientes llevan más prendas.
                Reglas = new[] { ReglaBajaRotacion.Codigo, ReglaUpgradePlan.Codigo, ReglaMantenimiento.Codigo }
            }
        };

        public static IReadOnlyList<Definicion> Todas => Definiciones;

        public static Definicion De(BE.IndicadorMeta indicador) =>
            Definiciones.FirstOrDefault(d => d.Indicador == indicador)
            ?? throw new BE.AppException("err.bll.meta.indicador", "El indicador de la meta no existe.");

        public static bool Existe(BE.IndicadorMeta indicador) => Definiciones.Any(d => d.Indicador == indicador);

        /// <summary>Indicadores cuyas metas mueve una regla (para elevar la prioridad de su recomendación).</summary>
        public static IEnumerable<BE.IndicadorMeta> MovidosPor(string tipoRegla) =>
            Definiciones.Where(d => d.Reglas.Contains(tipoRegla, StringComparer.OrdinalIgnoreCase)).Select(d => d.Indicador);
    }
}
