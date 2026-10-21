using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using static GUI.Inteligencia.TextosComerciales;

namespace GUI.Inteligencia
{
    /// <summary>
    /// Gráfico "real contra meta" de la pestaña Metas (MSChart): columnas con el valor real de cada mes,
    /// la línea del objetivo y, en el mes en curso, la proyección al cierre (rombo). Solo presentación:
    /// la serie la arma BLL.Inteligencia.MetasComerciales.Seguir.
    /// </summary>
    internal static class GraficoMetas
    {
        public static void Dibujar(Chart chart, BE.SeguimientoMeta s)
        {
            chart.Series.Clear();
            chart.Titles.Clear();
            chart.BackColor = Color.White;
            if (chart.ChartAreas.Count == 0) chart.ChartAreas.Add(new ChartArea("principal"));
            var area = chart.ChartAreas[0];
            area.BackColor = Color.White;
            area.AxisX.MajorGrid.LineColor = Tema.Borde;
            area.AxisY.MajorGrid.LineColor = Tema.Borde;
            area.AxisX.LabelStyle.Font = Tema.FuentePequena;
            area.AxisY.LabelStyle.Font = Tema.FuentePequena;
            area.AxisX.Interval = 1;
            if (chart.Legends.Count == 0) chart.Legends.Add(new Legend("leyenda"));
            chart.Legends[0].Docking = Docking.Bottom;
            chart.Legends[0].Font = Tema.FuentePequena;

            if (s == null || s.Serie.Count == 0)
            {
                area.Visible = false;
                chart.Legends[0].Enabled = false;
                chart.Titles.Add(new Title(Tr("cdc.meta.grafico.vacio", "Elija una meta para ver el real contra el objetivo de los últimos meses."),
                    Docking.Top, Tema.FuenteNormal, Tema.TextoMuted));
                return;
            }
            area.Visible = true;
            chart.Legends[0].Enabled = true;
            string nombre = R(s.NombreIndicador);
            chart.Titles.Add(new Title(Tr("cdc.meta.grafico.titulo", "{0}: real contra objetivo", nombre), Docking.Top, Tema.FuenteSubtitulo, Tema.RosaOscuro));
            bool moneda = s.Unidad == BE.UnidadKpi.Moneda;
            area.AxisY.LabelStyle.Format = moneda ? "C0" : "0.#";
            string formato = moneda ? "C0" : "0.#";

            var real = new Series(Tr("cdc.meta.col.real", "Real")) { ChartType = SeriesChartType.Column, Color = Tema.RosaPrimario, Font = Tema.FuentePequena };
            var objetivo = new Series(Tr("cdc.meta.col.objetivo", "Objetivo"))
            {
                ChartType = SeriesChartType.Line, Color = Tema.Info, BorderWidth = 2, BorderDashStyle = ChartDashStyle.Dash,
                MarkerStyle = MarkerStyle.Circle, MarkerSize = 6, MarkerColor = Tema.Info
            };
            var proyeccion = new Series(Tr("cdc.meta.col.proyeccion", "Proyección al cierre"))
            {
                ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Diamond, MarkerSize = 11, Color = Tema.Alerta
            };
            foreach (var p in s.Serie.OrderBy(x => x.Mes))
            {
                string etiqueta = p.Mes.ToString("MMM yy") + (p.EsMesEnCurso ? " *" : "");
                int i = real.Points.AddXY(etiqueta, (double)(p.Real ?? 0m));
                if (!p.Real.HasValue) real.Points[i].IsEmpty = true;
                else
                {
                    real.Points[i].ToolTip = $"{etiqueta}: {p.Real.Value.ToString(formato)}";
                    real.Points[i].Label = p.Real.Value.ToString(formato);
                }
                int j = objetivo.Points.AddXY(etiqueta, (double)(p.Objetivo ?? 0m));
                if (!p.Objetivo.HasValue) objetivo.Points[j].IsEmpty = true;
                else objetivo.Points[j].ToolTip = Tr("cdc.meta.col.objetivo", "Objetivo") + ": " + p.Objetivo.Value.ToString(formato);
                int k = proyeccion.Points.AddXY(etiqueta, (double)(p.Proyeccion ?? 0m));
                if (!p.Proyeccion.HasValue) proyeccion.Points[k].IsEmpty = true;
                else proyeccion.Points[k].ToolTip = Tr("cdc.meta.col.proyeccion", "Proyección al cierre") + ": " + p.Proyeccion.Value.ToString(formato);
            }
            objetivo.EmptyPointStyle.BorderWidth = 0;
            objetivo.EmptyPointStyle.MarkerStyle = MarkerStyle.None;
            proyeccion.EmptyPointStyle.MarkerStyle = MarkerStyle.None;
            chart.Series.Add(real);
            chart.Series.Add(objetivo);
            if (proyeccion.Points.Any(x => !x.IsEmpty)) chart.Series.Add(proyeccion);
        }
    }
}
