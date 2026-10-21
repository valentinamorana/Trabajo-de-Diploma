using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using static GUI.Inteligencia.TextosComerciales;

namespace GUI.Inteligencia
{
    /// <summary>
    /// Dibujo de los gráficos del Centro de decisiones comerciales con MSChart
    /// (System.Windows.Forms.DataVisualization). Solo presentación: recibe los datos ya calculados por
    /// BLL.Inteligencia y los pinta con la paleta de GUI.Tema, con tooltips, leyendas y un mensaje claro
    /// cuando no hay datos (en vez de un gráfico vacío). Las proyecciones se dibujan punteadas.
    /// </summary>
    internal static partial class GraficosComerciales
    {
        private static readonly Color ColorAltas = Tema.Exito;
        private static readonly Color ColorBajas = Tema.Error;
        private static readonly Color ColorIngresos = Tema.RosaOscuro;

        private static ChartArea Preparar(Chart chart, string titulo)
        {
            chart.Series.Clear();
            chart.Titles.Clear();
            chart.Annotations.Clear();
            chart.BackColor = Color.White;
            if (chart.ChartAreas.Count == 0) chart.ChartAreas.Add(new ChartArea("principal"));
            var area = chart.ChartAreas[0];
            area.Visible = true;
            area.BackColor = Color.White;
            area.AxisX.MajorGrid.LineColor = Tema.Borde;
            area.AxisY.MajorGrid.LineColor = Tema.Borde;
            area.AxisY2.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.Font = Tema.FuentePequena;
            area.AxisY.LabelStyle.Font = Tema.FuentePequena;
            area.AxisY2.LabelStyle.Font = Tema.FuentePequena;
            area.AxisX.Interval = 0;
            area.AxisX.StripLines.Clear();
            area.AxisY.StripLines.Clear();
            area.AxisY2.Enabled = AxisEnabled.False;
            if (chart.Legends.Count == 0) chart.Legends.Add(new Legend("leyenda"));
            chart.Legends[0].Docking = Docking.Bottom;
            chart.Legends[0].Font = Tema.FuentePequena;
            chart.Legends[0].Enabled = true;
            chart.Legends[0].CustomItems.Clear();
            chart.Titles.Add(new Title(titulo, Docking.Top, Tema.FuenteSubtitulo, Tema.RosaOscuro));
            return area;
        }

        /// <summary>Mensaje en lugar del gráfico cuando no hay datos.</summary>
        public static void SinDatos(Chart chart, string titulo, string mensaje)
        {
            var area = Preparar(chart, titulo);
            area.Visible = false;
            chart.Legends[0].Enabled = false;
            chart.Titles.Add(new Title(mensaje, Docking.Top, Tema.FuenteNormal, Tema.TextoMuted)
            {
                Position = new ElementPosition(5, 40, 90, 20)
            });
        }

        private static string MensajeVacio() => Tr("cdc.grafico.sindatos", "No hay datos para graficar en el período elegido.");

        /// <summary>(a) Tendencia mensual de altas, bajas e ingresos con 3 meses proyectados (punteado).</summary>
        public static void Tendencia(Chart chart, List<BE.PuntoMensual> puntos, decimal? r2)
        {
            string titulo = Tr("cdc.grafico.tendencia", "Tendencia de 6 meses y proyección de 3");
            if (puntos == null || puntos.Count == 0 || puntos.Where(p => !p.EsProyeccion).All(p => p.Altas == 0 && p.Bajas == 0 && p.Ingresos == 0))
            {
                SinDatos(chart, titulo, MensajeVacio());
                return;
            }
            var area = Preparar(chart, titulo);
            area.AxisY2.Enabled = AxisEnabled.True;
            area.AxisY.Title = Tr("cdc.grafico.clientes", "Clientes");
            area.AxisY2.Title = Tr("cdc.grafico.ingresos", "Ingresos");
            area.AxisY2.LabelStyle.Format = "C0";
            area.AxisX.Interval = 1;

            void Serie(string nombre, Color color, System.Func<BE.PuntoMensual, decimal> valor, bool eje2)
            {
                var real = new Series(nombre) { ChartType = SeriesChartType.Line, Color = color, BorderWidth = 3, MarkerStyle = MarkerStyle.Circle,
                                                YAxisType = eje2 ? AxisType.Secondary : AxisType.Primary,
                                                ToolTip = "#SERIESNAME — #VALX: " + (eje2 ? "#VAL{C0}" : "#VAL") };
                var proy = new Series(nombre + " · " + Tr("cdc.grafico.proyeccion", "proyección"))
                {
                    ChartType = SeriesChartType.Line, Color = color, BorderWidth = 2, BorderDashStyle = ChartDashStyle.Dash,
                    MarkerStyle = MarkerStyle.Diamond, YAxisType = eje2 ? AxisType.Secondary : AxisType.Primary,
                    IsVisibleInLegend = false, ToolTip = "#SERIESNAME — #VALX: " + (eje2 ? "#VAL{C0}" : "#VAL")
                };
                // X numérica (posición del mes) con el mes como etiqueta: así la serie proyectada queda
                // alineada con la real (con X de texto cada serie se ubicaría por su propio índice).
                BE.PuntoMensual ultimoReal = null;
                int xUltimoReal = 0;
                for (int i = 0; i < puntos.Count; i++)
                {
                    var p = puntos[i];
                    int x = i + 1;
                    string mes = Mes(p.Mes);
                    if (!p.EsProyeccion)
                    {
                        int k = real.Points.AddXY(x, (double)valor(p));
                        real.Points[k].AxisLabel = mes;
                        if (p.EsParcial)
                        {
                            // Mes parcial (el período termina antes de fin de mes, p. ej. el mes en curso):
                            // marcador hueco y aviso; no entra en la regresión (BLL InteligenciaComercial.AgregarProyeccion).
                            var pt = real.Points[k];
                            pt.AxisLabel = mes + "*";
                            pt.MarkerColor = Color.White;
                            pt.MarkerBorderColor = color;
                            pt.MarkerBorderWidth = 2;
                            pt.MarkerSize = 9;
                            pt.ToolTip = "#SERIESNAME — " + mes + " (" + Tr("cdc.grafico.mesparcial", "mes parcial: no entra en la proyección") + "): " + (eje2 ? "#VAL{C0}" : "#VAL");
                        }
                        ultimoReal = p;
                        xUltimoReal = x;
                    }
                    else
                    {
                        if (proy.Points.Count == 0 && ultimoReal != null)
                        {
                            int k0 = proy.Points.AddXY(xUltimoReal, (double)valor(ultimoReal));
                            proy.Points[k0].AxisLabel = Mes(ultimoReal.Mes) +(ultimoReal.EsParcial ? "*" : "");
                        }
                        int k = proy.Points.AddXY(x, (double)valor(p));
                        proy.Points[k].AxisLabel = mes;
                    }
                }
                chart.Series.Add(real);
                chart.Series.Add(proy);
            }
            Serie(Tr("cdc.grafico.altas", "Altas"), ColorAltas, p => p.Altas, false);
            Serie(Tr("cdc.grafico.bajas", "Bajas"), ColorBajas, p => p.Bajas, false);
            Serie(Tr("cdc.grafico.ingresos", "Ingresos"), ColorIngresos, p => p.Ingresos, true);
            // Una entrada de leyenda que explica el punteado.
            chart.Legends[0].CustomItems.Clear();
            chart.Legends[0].CustomItems.Add(new LegendItem
            {
                Name = Tr("cdc.grafico.leyendaproyeccion", "Punteado: proyección (regresión lineal)") +
                       (r2.HasValue ? $" · R² {r2.Value:0.00}" : ""),
                ImageStyle = LegendImageStyle.Line, Color = Tema.TextoMuted, BorderDashStyle = ChartDashStyle.Dash
            });
            if (puntos.Any(p => p.EsParcial && !p.EsProyeccion))
                chart.Legends[0].CustomItems.Add(new LegendItem
                {
                    Name = Tr("cdc.grafico.leyendaparcial", "* Mes parcial (p. ej. el mes en curso): se muestra, pero no entra en la proyección"),
                    ImageStyle = LegendImageStyle.Marker, MarkerStyle = MarkerStyle.Circle, MarkerColor = Color.White,
                    MarkerBorderColor = Tema.TextoMuted, MarkerSize = 8
                });
        }

        /// <summary>(b) Ingreso mensual en riesgo por plan.</summary>
        public static void RiesgoPorPlan(Chart chart, List<BE.MetricaAbandonoPlan> datos)
        {
            string titulo = Tr("cdc.grafico.riesgo", "Ingreso mensual en riesgo por plan");
            if (chart.Legends.Count > 0) chart.Legends[0].CustomItems.Clear();
            if (datos == null || datos.Count == 0)
            {
                SinDatos(chart, titulo, Tr("cdc.grafico.sinriesgo", "No hay clientes en riesgo: no hay ingreso en riesgo que graficar."));
                return;
            }
            var area = Preparar(chart, titulo);
            area.AxisY.LabelStyle.Format = "C0";
            area.AxisX.Interval = 1;
            var s = new Series(Tr("kpi.ingresoriesgo", "Ingreso en riesgo"))
            {
                ChartType = SeriesChartType.Column, Color = Tema.RosaPrimario, IsValueShownAsLabel = true, LabelFormat = "C0",
                Font = Tema.FuentePequena
            };
            foreach (var m in datos)
            {
                int i = s.Points.AddXY(m.NombrePlan, (double)m.IngresoMensualEnRiesgo);
                s.Points[i].ToolTip = Tr("cdc.grafico.tiporiesgo", "{0}: {1} cliente(s), {2:C2} por mes", m.NombrePlan, m.ClientesEnRiesgo, m.IngresoMensualEnRiesgo);
            }
            chart.Series.Add(s);
            chart.Legends[0].Enabled = false;
        }

        /// <summary>(c) Stock por categoría y estado (barras apiladas).</summary>
        public static void Stock(Chart chart, List<BE.StockCategoria> datos)
        {
            string titulo = Tr("cdc.grafico.stock", "Stock por categoría");
            if (chart.Legends.Count > 0) chart.Legends[0].CustomItems.Clear();
            if (datos == null || datos.Count == 0 || datos.All(d => d.Total == 0))
            {
                SinDatos(chart, titulo, Tr("cdc.grafico.sinstock", "No hay prendas activas en el catálogo."));
                return;
            }
            var area = Preparar(chart, titulo);
            area.AxisX.Interval = 1;
            void Serie(string nombre, Color color, System.Func<BE.StockCategoria, int> valor)
            {
                var s = new Series(nombre) { ChartType = SeriesChartType.StackedColumn, Color = color, ToolTip = "#VALX — #SERIESNAME: #VAL" };
                foreach (var d in datos) s.Points.AddXY(d.Categoria, valor(d));
                chart.Series.Add(s);
            }
            Serie(Tr("cdc.grafico.disponibles", "Disponibles"), Tema.Exito, d => d.Disponibles);
            Serie(Tr("cdc.grafico.enuso", "En uso"), Tema.RosaPrimario, d => d.EnUso);
            Serie(Tr("cdc.grafico.enlimpieza", "En limpieza"), Tema.Alerta, d => d.EnLimpieza);
        }

        /// <summary>(d) Rotación: prendas más y menos pedidas en el período.</summary>
        public static void Rotacion(Chart chart, List<BE.RotacionPrenda> mas, List<BE.RotacionPrenda> menos)
        {
            string titulo = Tr("cdc.grafico.rotacion", "Rotación: más y menos pedidas");
            if (chart.Legends.Count > 0) chart.Legends[0].CustomItems.Clear();
            if ((mas == null || mas.Count == 0) && (menos == null || menos.Count == 0))
            {
                SinDatos(chart, titulo, MensajeVacio());
                return;
            }
            var area = Preparar(chart, titulo);
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Font = Tema.FuentePequena;
            area.AxisX.IsLabelAutoFit = true;
            area.AxisX.LabelAutoFitMinFontSize = 6;
            area.AxisY.Title = Tr("cdc.grafico.pedidos", "Pedidos del período");
            area.AxisY.Minimum = 0;
            // Una sola serie (barras anchas): arriba las más pedidas en verde, abajo las menos pedidas en
            // rojo. Una prenda sin pedidos se ve como una barra mínima con la etiqueta "0".
            var s = new Series("rotacion") { ChartType = SeriesChartType.Bar, IsValueShownAsLabel = true, Font = Tema.FuentePequena, IsVisibleInLegend = false };
            s["PointWidth"] = "0.7";
            void Agregar(BE.RotacionPrenda p, Color color)
            {
                int i = s.Points.AddXY(p.NombrePrenda, p.CantidadPedidos);
                s.Points[i].Color = color;
                s.Points[i].ToolTip = $"{p.NombrePrenda} ({p.Categoria}): {p.CantidadPedidos}";
                if (p.CantidadPedidos == 0) { s.Points[i].YValues[0] = 0.02; s.Points[i].Label = "0"; }
            }
            // En un gráfico de barras el primer punto queda abajo: se cargan primero las menos pedidas.
            foreach (var p in (menos ?? new List<BE.RotacionPrenda>()).AsEnumerable().Reverse()) Agregar(p, Tema.Error);
            foreach (var p in (mas ?? new List<BE.RotacionPrenda>()).AsEnumerable().Reverse()) Agregar(p, Tema.Exito);
            chart.Series.Add(s);
            chart.Legends[0].CustomItems.Add(new LegendItem { Name = Tr("cdc.grafico.maspedidas", "Más pedidas"), Color = Tema.Exito });
            chart.Legends[0].CustomItems.Add(new LegendItem { Name = Tr("cdc.grafico.menospedidas", "Menos pedidas"), Color = Tema.Error });
        }

        /// <summary>Simulador: beneficio neto según la retención, con el equilibrio y el supuesto marcados.</summary>
        public static void Sensibilidad(Chart chart, BE.ResultadoSimulacion actual, IList<BE.ResultadoSimulacion> guardados)
        {
            string titulo = Tr("cdc.grafico.sensibilidad", "Beneficio neto según la retención");
            if (chart.Legends.Count > 0) chart.Legends[0].CustomItems.Clear();
            if (actual == null)
            {
                SinDatos(chart, titulo, Tr("cdc.grafico.sinescenario", "Complete el escenario para ver la curva."));
                return;
            }
            var area = Preparar(chart, titulo);
            area.AxisX.Title = Tr("cdc.grafico.retencion", "Retención (%)");
            area.AxisY.Title = Tr("cdc.grafico.beneficio", "Beneficio neto");
            area.AxisY.LabelStyle.Format = "C0";
            area.AxisX.Minimum = 0;
            area.AxisX.Maximum = 100;
            area.AxisX.Interval = 10;
            area.AxisY.StripLines.Add(new StripLine { IntervalOffset = 0, StripWidth = 0, BorderColor = Tema.Tinta, BorderWidth = 1 });

            Color[] colores = { Tema.Info, Tema.Alerta, Tema.TextoMuted };
            for (int i = 0; guardados != null && i < guardados.Count; i++)
                chart.Series.Add(Curva(Tr("cdc.sim.escenario", "Escenario {0}", (char)('A' + i)), guardados[i], colores[i % colores.Length], 2, ChartDashStyle.Dash));
            chart.Series.Add(Curva(Tr("cdc.sim.actual", "Escenario actual"), actual, Tema.RosaOscuro, 3, ChartDashStyle.Solid));

            if (actual.TasaEquilibrio.HasValue && actual.TasaEquilibrio.Value <= 100)
            {
                area.AxisX.StripLines.Add(new StripLine
                {
                    IntervalOffset = (double)actual.TasaEquilibrio.Value, StripWidth = 0, BorderColor = Tema.Error, BorderWidth = 2,
                    BorderDashStyle = ChartDashStyle.Dot, ForeColor = Tema.Error, Font = Tema.FuentePequena,
                    Text = Tr("cdc.grafico.equilibrio", "Equilibrio {0:0.#} %", actual.TasaEquilibrio.Value), TextOrientation = TextOrientation.Rotated270
                });
            }
            area.AxisX.StripLines.Add(new StripLine
            {
                IntervalOffset = (double)actual.Escenario.TasaRetencion, StripWidth = 0, BorderColor = Tema.Exito, BorderWidth = 2,
                ForeColor = Tema.Exito, Font = Tema.FuentePequena, TextOrientation = TextOrientation.Rotated270,
                Text = Tr("cdc.grafico.supuesto", "Supuesto {0:0.#} %", actual.Escenario.TasaRetencion)
            });
        }

        private static Series Curva(string nombre, BE.ResultadoSimulacion r, Color color, int grosor, ChartDashStyle estilo)
        {
            var s = new Series(nombre) { ChartType = SeriesChartType.Line, Color = color, BorderWidth = grosor, BorderDashStyle = estilo,
                                         ToolTip = "#SERIESNAME — #VALX %: #VAL{C0}" };
            foreach (var p in r.Curva) s.Points.AddXY((double)p.TasaRetencion, (double)p.BeneficioNeto);
            return s;
        }

        /// <summary>Medir resultado: movimientos del plan antes vs durante la promoción.</summary>
        public static void Medicion(Chart chart, BE.MedicionPromocion m)
        {
            string titulo = m == null ? Tr("cdc.grafico.medicion", "Antes vs durante la promoción")
                                      : Tr("cdc.grafico.medicionde", "Antes vs durante: {0}", m.Nombre);
            if (chart.Legends.Count > 0) chart.Legends[0].CustomItems.Clear();
            var r = m?.Resultado;
            if (r == null || r.NoSePudoCalcular)
            {
                SinDatos(chart, titulo, Tr("cdc.grafico.sinmedicion", "Elija una promoción medida para ver la comparación."));
                return;
            }
            if (r.Antes.Bajas + r.Antes.Pausas + r.Antes.CambiosDePlan + r.Antes.Renovaciones +
                r.Durante.Bajas + r.Durante.Pausas + r.Durante.CambiosDePlan + r.Durante.Renovaciones == 0)
            {
                SinDatos(chart, titulo, Tr("cdc.grafico.sinmovimientos", "No hubo bajas, pausas, cambios de plan ni renovaciones del plan en estos tramos."));
                return;
            }
            var area = Preparar(chart, titulo);
            area.AxisX.Interval = 1;
            var antes = new Series(Tr("cdc.grafico.antes", "Período anterior")) { ChartType = SeriesChartType.Column, Color = Tema.TextoMuted, IsValueShownAsLabel = true, ToolTip = "#SERIESNAME — #VALX: #VAL" };
            var durante = new Series(Tr("cdc.grafico.durante", "Durante la promoción")) { ChartType = SeriesChartType.Column, Color = Tema.RosaPrimario, IsValueShownAsLabel = true, ToolTip = "#SERIESNAME — #VALX: #VAL" };
            void Par(string nombre, int a, int d) { antes.Points.AddXY(nombre, a); durante.Points.AddXY(nombre, d); }
            Par(Tr("cdc.grafico.bajas", "Bajas"), r.Antes.Bajas, r.Durante.Bajas);
            Par(Tr("cdc.grafico.pausas", "Pausas"), r.Antes.Pausas, r.Durante.Pausas);
            Par(Tr("cdc.grafico.cambios", "Cambios de plan"), r.Antes.CambiosDePlan, r.Durante.CambiosDePlan);
            Par(Tr("cdc.grafico.renovaciones", "Renovaciones"), r.Antes.Renovaciones, r.Durante.Renovaciones);
            chart.Series.Add(antes);
            chart.Series.Add(durante);
        }

        /// <summary>Imagen PNG del gráfico (para el Informe ejecutivo en PDF).</summary>
        public static byte[] Png(Chart chart, int ancho = 900, int alto = 420)
        {
            // El gráfico está acoplado (Dock): se suelta un instante para guardarlo con un tamaño fijo, igual
            // para todos, aunque su pestaña no se haya mostrado nunca.
            var tamanio = chart.Size;
            var dock = chart.Dock;
            var padre = chart.Parent;
            padre?.SuspendLayout();
            try
            {
                chart.Dock = System.Windows.Forms.DockStyle.None;
                chart.Size = new Size(ancho, alto);
                using (var ms = new MemoryStream())
                {
                    chart.SaveImage(ms, ChartImageFormat.Png);
                    return ms.ToArray();
                }
            }
            finally
            {
                chart.Size = tamanio;
                chart.Dock = dock;
                padre?.ResumeLayout();
            }
        }
    }
}
