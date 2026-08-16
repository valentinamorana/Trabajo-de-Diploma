using BLL;
using Servicios.Multiidioma;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GUI
{
    public partial class ReporteJornadaForm : Form, IIdiomaObserver
    {
        private readonly ReporteJornada _servicio = new ReporteJornada();

        private bool _esComparacion = false;

        public ReporteJornadaForm(List<BE.Permiso> permisos)
        {
            InitializeComponent();
        }

        private void ReporteJornadaForm_Load(object sender, EventArgs e)
        {
            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new System.Drawing.Icon(ico); } catch { }
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);

            dtpJornada.Value  = DateTime.Today;
            dtpJornada2.Value = DateTime.Today.AddDays(-1);

            btnExportarComp.Visible = false;
            GenerarReporte();
        }

        // Degradado del panel superior.
        private void PanelTop_Paint(object sender, PaintEventArgs e)
        {
            using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                panelTop.ClientRectangle,
                Color.FromArgb(176, 62, 96),
                Color.FromArgb(242, 114, 153),
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                e.Graphics.FillRectangle(br, panelTop.ClientRectangle);
        }

        // Línea inferior + separadores verticales del banner de KPIs.
        private void PanelKpiBanner_Paint(object sender, PaintEventArgs e)
        {
            const int panelH = 66;
            const int n      = 4;
            int cellW = panelKpiBanner.Width / n;

            using (var br = new SolidBrush(Color.FromArgb(176, 62, 96)))
                e.Graphics.FillRectangle(br, 0, panelH - 3, panelKpiBanner.Width, 3);
            using (var pen = new Pen(Color.FromArgb(220, 180, 200), 1))
                for (int i = 1; i < n; i++)
                    e.Graphics.DrawLine(pen, i * cellW, 8, i * cellW, panelH - 10);
        }

        private void BtnTendencia_Click(object sender, EventArgs e) => MostrarTendencia();

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        // ── IIdiomaObserver ───────────────────────────────────────────────────

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            if (_esComparacion)
                btnComparar_Click(null, EventArgs.Empty);
            else
                GenerarReporte();
        }

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text          = T("frm.reportejornada", "Reporte de Jornada — WardrobeFlow");
            lblTitulo.Text     = T("frm.reportejornada", "Reporte de Jornada — WardrobeFlow");
            lblSubtitulo.Text  = T("rpt.subtitulo",      "Eventos de negocio por jornada con exportación a TXT");
            lblJornada.Text    = T("rpt.fecha",          "Jornada:");
            lblComparar.Text   = T("rpt.fecha2",         "Comparar con:");
            btnGenerar.Text    = "↻  " + T("rpt.generar",     "Generar");
            btnComparar.Text   = "⚖  " + T("rpt.comparar",    "Comparar jornadas");
            btnExportar.Text   = "⬇  " + T("rpt.exportartxt", "Exportar TXT") + "...";
            btnExportarComp.Text = "⬇  " + T("rpt.exportartxt", "Exportar TXT") + "...";
            btnLimpiar.Text    = "↩  " + T("rpt.limpiar",     "Limpiar");
            btnTendencia.Text  = "📈  " + T("rpt.tendencia", "Tendencia (rango)");

            kpiPrendasLbl.Text  = T("rpt.kpi.prendas",  "Prendas disponibles");
            kpiClientesLbl.Text = T("rpt.kpi.clientes", "Clientes registrados");
            kpiEventosLbl.Text  = T("rpt.kpi.eventos",  "Eventos del día");
            kpiBackupLbl.Text   = T("rpt.kpi.backup",   "días sin backup");

            mnuGuardarTxt.Text  = T("rpt.menu.guardartxt",  "Guardar como .TXT");
            mnuImprimir.Text    = T("rpt.menu.imprimir",    "Imprimir / Exportar PDF");
            mnuGuardarCsv.Text  = T("rpt.menu.guardarcsv",  "Guardar eventos como .CSV");
            mnuGuardarComp.Text = T("rpt.menu.guardarcmp",  "Guardar comparación como .TXT");
            mnuImprimirComp.Text = T("rpt.menu.imprimir",   "Imprimir / Exportar PDF");
        }

        // ── KPI Banner ────────────────────────────────────────────────────────

        private void ActualizarKPIs(DateTime fecha)
        {
            try
            {
                kpiPrendasVal.Text  = _servicio.ContarPrendasDisponibles().ToString();
                kpiClientesVal.Text = _servicio.ContarClientes().ToString();
                kpiEventosVal.Text  = _servicio.ContarEventosDia(fecha).ToString();

                int dias = _servicio.ObtenerDiasSinBackup();
                kpiBackupVal.Text = dias < 0 ? "!" : dias.ToString();
            }
            catch { /* no interrumpir el reporte */ }
        }

        // ── Menús de exportación ──────────────────────────────────────────────

        private void MnuGuardarTxt_Click(object sender, EventArgs e) => ExportarContenido(esComparacion: false);

        private void MnuImprimir_Click(object sender, EventArgs e) => ImprimirReporte();

        private void MnuGuardarCsv_Click(object sender, EventArgs e) => ExportarEventosCsv();

        private void MnuGuardarComp_Click(object sender, EventArgs e) => ExportarContenido(esComparacion: true);

        private void ExportarContenido(bool esComparacion)
        {
            try
            {
                var lbl = ConstruirLblReporte();
                string nombreBase = esComparacion
                    ? $"Comparacion_{dtpJornada.Value:yyyyMMdd}_vs_{dtpJornada2.Value:yyyyMMdd}"
                    : $"ReporteJornada_{dtpJornada.Value:yyyyMMdd}";

                var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                string Tk(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

                var reporte = new Exportacion.ReporteExportable
                {
                    Titulo        = $"{Tk("frm.reportejornada", "Reporte de Jornada")} — {dtpJornada.Value:dd/MM/yyyy}",
                    NombreArchivo = nombreBase,
                    TextoPlano    = rtbReporte.Text
                };

                // Creator → Factory Method → Product (Exportador concreto a .TXT)
                Exportacion.GeneradorReporte generador  = new Exportacion.GeneradorJornada();
                Exportacion.Exportador       exportador = generador.CrearExportador("txt");
                string ruta = exportador.Exportar(reporte, this);

                if (ruta != null)
                    lblStatus.Text = $"{lbl["rptoegenerado"]}: {Path.GetFileName(ruta)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirReporte()
        {
            try
            {
                var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                string Tk(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

                var reporte = new Exportacion.ReporteExportable
                {
                    Titulo        = $"{Tk("frm.reportejornada", "Reporte de Jornada")} — {dtpJornada.Value:dd/MM/yyyy}",
                    NombreArchivo = $"ReporteJornada_{dtpJornada.Value:yyyyMMdd}",
                    TextoPlano    = rtbReporte.Text
                };

                // Creator → Factory Method → Product (Exportador concreto a PDF)
                Exportacion.GeneradorReporte generador  = new Exportacion.GeneradorJornada();
                Exportacion.Exportador       exportador = generador.CrearExportador("pdf");
                exportador.Exportar(reporte, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al imprimir", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Genera la TENDENCIA de actividad en el rango [Comparar con … Jornada].
        // La agregación por día vive en BLL.ReporteJornada; la GUI solo la muestra.
        private void MostrarTendencia()
        {
            try
            {
                var lbl = ConstruirLblReporte();
                rtbReporte.Text = _servicio.GenerarTendencia(
                    dtpJornada2.Value.Date, dtpJornada.Value.Date, lbl);
                lblStatus.Text  = $"{lbl["rptoegenerado"]} — {DateTime.Now:HH:mm:ss}";
                _esComparacion  = false;
                btnExportarComp.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Exporta los eventos de negocio de la jornada como CSV tabular (Factory Method).
        // Los datos los provee BLL.ReporteJornada; acá solo se arma el reporte y se exporta.
        private void ExportarEventosCsv()
        {
            try
            {
                var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                string Tk(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

                DateTime fecha    = dtpJornada.Value.Date;
                DataTable eventos = _servicio.ObtenerEventosDelDia(fecha);

                if (eventos == null || eventos.Rows.Count == 0)
                {
                    MessageBox.Show(
                        Tk("err.pdf.sinDatos", "No hay datos para exportar."),
                        Tk("rpt.menu.guardarcsv", "Guardar eventos como .CSV"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var headers = new string[eventos.Columns.Count];
                for (int i = 0; i < eventos.Columns.Count; i++)
                    headers[i] = eventos.Columns[i].ColumnName;

                var reporte = new Exportacion.ReporteExportable
                {
                    Titulo        = $"{Tk("frm.reportejornada", "Reporte de Jornada")} — {fecha:dd/MM/yyyy}",
                    NombreArchivo = $"EventosJornada_{fecha:yyyyMMdd}",
                    Encabezados   = headers,
                    Datos         = eventos
                };

                Exportacion.GeneradorReporte generador  = new Exportacion.GeneradorJornada();
                Exportacion.Exportador       exportador = generador.CrearExportador("csv");
                exportador?.Exportar(reporte, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void btnGenerar_Click(object sender, EventArgs e) => GenerarReporte();

        private void dtpJornada_ValueChanged(object sender, EventArgs e) => GenerarReporte();

        private void btnExportar_Click(object sender, EventArgs e)
        {
            menuExportar.Show(btnExportar, new Point(0, btnExportar.Height));
        }

        private void btnExportarComp_Click(object sender, EventArgs e)
        {
            menuExportarComp.Show(btnExportarComp, new Point(0, btnExportarComp.Height));
        }

        private void btnLimpiar_Click(object sender, EventArgs e) => GenerarReporte();

        private void btnComparar_Click(object sender, EventArgs e)
        {
            try
            {
                var lbl = ConstruirLblReporte();
                string texto = _servicio.GenerarComparacion(
                    dtpJornada.Value.Date, dtpJornada2.Value.Date, lbl);
                rtbReporte.Text = texto;
                lblStatus.Text  = $"{lbl["compgenerada"]} — {DateTime.Now:HH:mm:ss}";
                _esComparacion = true;
                btnExportarComp.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IDictionary<string, string> ConstruirLblReporte()
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string Tv(string key, string fb) => t.ContainsKey(key) ? t[key].Texto : fb;
            return new Dictionary<string, string>
            {
                { "titulo",        Tv("rpt.txt.titulo",       "REPORTE DE JORNADA")                                    },
                { "resumen",       Tv("rpt.txt.resumen",      "RESUMEN DEL SISTEMA")                                   },
                { "prendas",       Tv("rpt.txt.prendas",      "Prendas disponibles")                                   },
                { "clientes",      Tv("rpt.txt.clientes",     "Clientes registrados")                                  },
                { "diassinbkp",    Tv("rpt.txt.diassinbkp",   "Días sin backup")                                       },
                { "sinbackups",    Tv("rpt.txt.sinbackups",   "Sin backups")                                           },
                { "eventos",       Tv("rpt.txt.eventos",      "EVENTOS DE NEGOCIO DEL DÍA")                            },
                { "sinevt",        Tv("rpt.txt.sinevt",       "(sin eventos registrados para esta jornada)")            },
                { "usuario",       Tv("rpt.txt.usuario",      "Usuario")                                               },
                { "cliente",       Tv("rpt.txt.cliente",      "Cliente")                                               },
                { "totalevt",      Tv("rpt.txt.totalevt",     "TOTAL EVENTOS")                                         },
                { "generado",      Tv("rpt.txt.generado",     "Generado")                                              },
                { "comparacion",   Tv("rpt.txt.comparacion",  "COMPARACIÓN DE JORNADAS")                               },
                { "jornada",       Tv("rpt.txt.jornada",      "JORNADA")                                               },
                { "sinevtjorn",    Tv("rpt.txt.sinevtjorn",   "(sin eventos registrados en esta jornada)")              },
                { "comparfinal",   Tv("rpt.txt.comparfinal",  "COMPARATIVO FINAL")                                     },
                { "fecha",         Tv("rpt.txt.fecha",        "Fecha")                                                 },
                { "eventostot",    Tv("rpt.txt.eventostot",   "Eventos totales")                                       },
                { "masmasa",       Tv("rpt.txt.masmasa",      "tuvo más actividad")                                    },
                { "ninguna",       Tv("rpt.txt.ninguna",      "Ninguna jornada tuvo eventos registrados.")              },
                { "iguales",       Tv("rpt.txt.iguales",      "Ambas jornadas tuvieron la misma cantidad de eventos.") },
                { "rptoegenerado", Tv("rpt.txt.rptoegenerado","Reporte generado")                                      },
                { "compgenerada",  Tv("rpt.txt.compgenerada", "Comparación generada")                                  },
                { "impresionenv",  Tv("rpt.txt.impresionenv", "Impresión enviada")                                     },
                { "tend.titulo",   Tv("rpt.txt.tend.titulo",  "TENDENCIA DE ACTIVIDAD")                                },
                { "tend.dias",     Tv("rpt.txt.tend.dias",    "Días analizados")                                       },
                { "tend.total",    Tv("rpt.txt.tend.total",   "Total de eventos")                                      },
                { "tend.promedio", Tv("rpt.txt.tend.promedio","Promedio diario")                                       },
                { "tend.diapico",  Tv("rpt.txt.tend.diapico", "Día de mayor actividad")                                },
                { "tend.diavalle", Tv("rpt.txt.tend.diavalle","Día de menor actividad")                                },
                { "tend.detalle",  Tv("rpt.txt.tend.detalle", "DETALLE POR DÍA")                                       },
            };
        }

        private void GenerarReporte()
        {
            try
            {
                DateTime fecha = dtpJornada.Value.Date;
                var lbl = ConstruirLblReporte();
                rtbReporte.Text = _servicio.Generar(fecha, lbl);
                lblStatus.Text  = $"{lbl["rptoegenerado"]} — {DateTime.Now:HH:mm:ss}";
                _esComparacion = false;
                btnExportarComp.Visible = false;
                ActualizarKPIs(fecha);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
