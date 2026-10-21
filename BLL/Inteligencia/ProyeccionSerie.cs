using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>Recta ajustada por mínimos cuadrados: y = Ordenada + Pendiente · x (x = 0, 1, 2…).</summary>
    public sealed class AjusteLineal
    {
        public decimal Pendiente { get; internal set; }
        public decimal Ordenada { get; internal set; }
        /// <summary>Coeficiente de determinación (0 a 1): qué parte de la variación explica la recta.
        /// null con menos de 2 puntos (no hay variación que explicar).</summary>
        public decimal? R2 { get; internal set; }
        public int Puntos { get; internal set; }

        public decimal Estimar(int x) => Ordenada + Pendiente * x;
    }

    /// <summary>
    /// PN03 «Analizar» — Proyección de series mensuales (altas, bajas, ingresos). Matemática pura, sin
    /// base de datos ni UI, probada en Tests/InteligenciaComercialTests:
    ///   · Regresión lineal por mínimos cuadrados:
    ///       pendiente b = Σ(x − x̄)(y − ȳ) / Σ(x − x̄)²,  ordenada a = ȳ − b·x̄,
    ///       R² = 1 − Σ(y − ŷ)² / Σ(y − ȳ)²  (si todos los valores son iguales, R² = 1: la recta es exacta).
    ///   · Promedio móvil: cada mes proyectado es el promedio de los últimos N meses (reales o proyectados).
    /// Casos borde: serie vacía → proyección en 0; un solo punto → se repite ese valor (pendiente 0);
    /// una proyección negativa se lleva a 0 (no hay altas, bajas ni ingresos negativos).
    /// </summary>
    public static class ProyeccionSerie
    {
        public static AjusteLineal RegresionLineal(IList<decimal> serie)
        {
            var ajuste = new AjusteLineal { Puntos = serie?.Count ?? 0 };
            if (serie == null || serie.Count == 0) return ajuste;
            if (serie.Count == 1)
            {
                ajuste.Ordenada = serie[0];
                return ajuste;
            }

            int n = serie.Count;
            decimal xMedia = (n - 1) / 2m;
            decimal yMedia = serie.Average();
            decimal sxx = 0, sxy = 0;
            for (int x = 0; x < n; x++)
            {
                sxx += (x - xMedia) * (x - xMedia);
                sxy += (x - xMedia) * (serie[x] - yMedia);
            }
            ajuste.Pendiente = sxx == 0 ? 0 : sxy / sxx;
            ajuste.Ordenada = yMedia - ajuste.Pendiente * xMedia;

            decimal ssTotal = 0, ssResidual = 0;
            for (int x = 0; x < n; x++)
            {
                ssTotal += (serie[x] - yMedia) * (serie[x] - yMedia);
                decimal error = serie[x] - ajuste.Estimar(x);
                ssResidual += error * error;
            }
            ajuste.R2 = ssTotal == 0 ? 1m : Math.Round(Math.Max(0m, 1m - ssResidual / ssTotal), 4);
            return ajuste;
        }

        /// <summary>Próximos <paramref name="periodos"/> valores de la recta (no negativos, redondeados a 2 decimales).</summary>
        public static List<decimal> Proyectar(IList<decimal> serie, int periodos) => Proyectar(serie, periodos, 0);

        /// <summary>
        /// Igual que <see cref="Proyectar(IList{decimal}, int)"/>, pero la proyección empieza
        /// <paramref name="saltear"/> períodos después del último punto ajustado. Se usa cuando el último
        /// mes real está incompleto (mes en curso): se excluye de la regresión, pero sigue ocupando su
        /// lugar en el eje, así que el primer mes proyectado es x = n + 1 y no x = n.
        /// </summary>
        public static List<decimal> Proyectar(IList<decimal> serie, int periodos, int saltear)
        {
            var resultado = new List<decimal>();
            if (periodos <= 0) return resultado;
            var ajuste = RegresionLineal(serie);
            int n = (serie?.Count ?? 0) + Math.Max(0, saltear);
            for (int i = 0; i < periodos; i++)
                resultado.Add(Math.Round(Math.Max(0m, ajuste.Estimar(n + i)), 2));
            return resultado;
        }

        /// <summary>Proyección por promedio móvil de <paramref name="ventana"/> meses.</summary>
        public static List<decimal> PromedioMovil(IList<decimal> serie, int ventana, int periodos)
        {
            if (ventana < 1) throw new ArgumentOutOfRangeException(nameof(ventana));
            var resultado = new List<decimal>();
            if (periodos <= 0) return resultado;
            var valores = new List<decimal>(serie ?? new List<decimal>());
            for (int i = 0; i < periodos; i++)
            {
                decimal siguiente = valores.Count == 0 ? 0m
                    : Math.Round(valores.Skip(Math.Max(0, valores.Count - ventana)).Average(), 2);
                resultado.Add(siguiente);
                valores.Add(siguiente);
            }
            return resultado;
        }
    }
}
