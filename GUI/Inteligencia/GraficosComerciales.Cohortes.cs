using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using static GUI.Inteligencia.TextosComerciales;

namespace GUI.Inteligencia
{
    /// <summary>Gráfico de la pestaña Cohortes: curva de retención promedio y la de la peor cohorte.</summary>
    internal static partial class GraficosComerciales
    {
        /// <summary>
        /// Curva de retención: % de la cohorte que sigue activo en cada mes desde el alta. La línea llena es
        /// el promedio ponderado de las cohortes con ese mes completo; la punteada, la peor cohorte (con la
        /// línea vertical del mes en que se compararon). Los meses parciales de la peor cohorte se marcan huecos.
        /// </summary>
        public static void CurvaRetencion(Chart chart, BE.MatrizCohortes m)
        {
            string titulo = Tr("cdc.cohorte.grafico.titulo", "Curva de retención por mes desde el alta");
            if (chart.Legends.Count > 0) chart.Legends[0].CustomItems.Clear();
            if (m == null || m.SinDatos || m.SinMesesCompletos)
            {
                SinDatos(chart, titulo, m == null ? Tr("cdc.grafico.analice", "Elija el período y pulse Analizar.")
                    : m.SinDatos ? Tr("cdc.cohorte.sindatos", "No hay altas de suscriptores en los últimos 12 meses para armar cohortes.")
                    : Tr("cdc.cohorte.sinmesescompletos", "Aún no hay meses completos para comparar: las cohortes están en su primer mes (*), que se ve en la grilla."));
                return;
            }
            var area = Preparar(chart, titulo);
            area.AxisX.Title = Tr("cdc.cohorte.grafico.ejex", "Meses desde el alta");
            area.AxisY.Title = Tr("cdc.cohorte.grafico.ejey", "% de la cohorte activo");
            area.AxisX.Minimum = -0.3;
            area.AxisX.Maximum = m.MaximoDesplazamiento + 0.3;
            area.AxisX.Interval = 1;
            area.AxisY.Minimum = 0;
            area.AxisY.Maximum = 100;
            area.AxisY.Interval = 20;
            area.AxisY.LabelStyle.Format = "0' %'";

            string Etiqueta(int k) => Tr("cdc.cohorte.col.mes", "Mes {0}", k);

            var promedio = new Series(Tr("cdc.cohorte.grafico.promedio", "Promedio de las cohortes"))
            {
                ChartType = SeriesChartType.Line, Color = Tema.RosaOscuro, BorderWidth = 3, MarkerStyle = MarkerStyle.Circle, MarkerSize = 7,
                IsValueShownAsLabel = true, LabelFormat = "0' %'", Font = Tema.FuentePequena
            };
            foreach (var p in m.Curva.Where(p => p.Retencion.HasValue))
            {
                int i = promedio.Points.AddXY(p.Desplazamiento, (double)p.Retencion.Value);
                promedio.Points[i].AxisLabel = Etiqueta(p.Desplazamiento);
                promedio.Points[i].ToolTip = Tr("cdc.cohorte.grafico.tippromedio", "{0}: {1:0.#} % (promedio de {2} cohorte(s), {3} cliente(s))",
                    Etiqueta(p.Desplazamiento), p.Retencion.Value, p.Cohortes, p.Clientes);
            }
            chart.Series.Add(promedio);

            var fila = m.Peor == null ? null : m.Cohortes.FirstOrDefault(c => c.Mes == m.Peor.Mes);
            if (fila != null)
            {
                var peor = new Series(Tr("cdc.cohorte.grafico.peor", "Peor cohorte: {0:yyyy-MM}", fila.Mes))
                {
                    ChartType = SeriesChartType.Line, Color = Tema.Error, BorderWidth = 2, BorderDashStyle = ChartDashStyle.Dash,
                    MarkerStyle = MarkerStyle.Diamond, MarkerSize = 8
                };
                foreach (var c in fila.Celdas)
                {
                    int i = peor.Points.AddXY(c.Desplazamiento, (double)c.Retencion);
                    peor.Points[i].AxisLabel = Etiqueta(c.Desplazamiento);
                    peor.Points[i].ToolTip = Tr("cdc.cohorte.tip", "Cohorte {0:yyyy-MM}, mes {1}: {2} de {3} cliente(s) activos ({4:0.#} %)",
                        fila.Mes, c.Desplazamiento, c.Activos, fila.Tamanio, c.Retencion)
                        + (c.EsParcial ? " — " + Tr("cdc.cohorte.parcial", "mes en curso: no entra en el promedio ni en la comparación") : "");
                    if (c.EsParcial)
                    {
                        peor.Points[i].MarkerColor = Color.White;
                        peor.Points[i].MarkerBorderColor = Tema.Error;
                        peor.Points[i].MarkerBorderWidth = 2;
                    }
                }
                chart.Series.Add(peor);
                area.AxisX.StripLines.Add(new StripLine
                {
                    IntervalOffset = m.Peor.Desplazamiento, StripWidth = 0, BorderColor = Tema.TextoMuted, BorderWidth = 1,
                    BorderDashStyle = ChartDashStyle.Dot, ForeColor = Tema.TextoMuted, Font = Tema.FuentePequena,
                    TextOrientation = TextOrientation.Rotated270,
                    Text = Tr("cdc.cohorte.grafico.comparacion", "Mes de comparación")
                });
            }
        }
    }
}
