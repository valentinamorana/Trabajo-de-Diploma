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
    public partial class ReporteJornadaForm : FormBase, IIdiomaObserver
    {
        private readonly ReporteJornada _servicio = new ReporteJornada();

        private bool _esComparacion = false;

        // Mismo criterio de visibilidad que GUI.DashboardForm para estos 3 KPIs: "Días sin
        // backup" es exclusivo de Administrador (mnuUsuarios); Prendas/Clientes requieren su
        // propio permiso de módulo. El parámetro `permisos` se recibía pero no se usaba —
        // este reporte mostraba esos datos a cualquier rol con acceso a Auditoría (mnuAuditoria),
        // aunque no tuviera el permiso que el propio Dashboard exige para lo mismo.
        private readonly bool _verPrendas, _verClientes, _verBackup;

        public ReporteJornadaForm(List<BE.Permiso> permisos)
        {
            InitializeComponent();

            var nombres = new HashSet<string>();
            if (permisos != null)
                foreach (var p in permisos)
                    if (p.NombreMenu != null) nombres.Add(p.NombreMenu);

            _verPrendas  = nombres.Contains("mnuPrendas");
            _verClientes = nombres.Contains("mnuClientes");
            _verBackup   = nombres.Contains("mnuUsuarios");

            if (!_verPrendas)  { kpiPrendasLbl.Visible  = false; kpiPrendasVal.Visible  = false; }
            if (!_verClientes) { kpiClientesLbl.Visible = false; kpiClientesVal.Visible = false; }
            if (!_verBackup)   { kpiBackupLbl.Visible   = false; kpiBackupVal.Visible   = false; }
        }

        private void ReporteJornadaForm_Load(object sender, EventArgs e)
        {
            // El ícono ahora lo aplica FormBase.OnLoad (esta clase no lo sobreescribe, así que
            // corre alrededor de este handler del Load del Designer) — no hace falta duplicarlo acá.
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
            this.Text          = Tr("frm.reportejornada", "Reporte de Jornada — WardrobeFlow");
            lblTitulo.Text     = Tr("frm.reportejornada", "Reporte de Jornada — WardrobeFlow");
            lblSubtitulo.Text  = Tr("rpt.subtitulo",      "Eventos de negocio por jornada con exportación a TXT");
            lblJornada.Text    = Tr("rpt.fecha",          "Jornada:");
            lblComparar.Text   = Tr("rpt.fecha2",         "Comparar con:");
            btnGenerar.Text    = "↻  " + Tr("rpt.generar",     "Generar");
            btnComparar.Text   = "⚖  " + Tr("rpt.comparar",    "Comparar jornadas");
            btnExportar.Text   = "⬇  " + Tr("rpt.exportartxt", "Exportar TXT") + "...";
            btnExportarComp.Text = "⬇  " + Tr("rpt.exportartxt", "Exportar TXT") + "...";
            btnLimpiar.Text    = "↩  " + Tr("rpt.limpiar",     "Limpiar");
            btnTendencia.Text  = "📈  " + Tr("rpt.tendencia", "Tendencia (rango)");

            kpiPrendasLbl.Text  = Tr("rpt.kpi.prendas",  "Prendas disponibles");
            kpiClientesLbl.Text = Tr("rpt.kpi.clientes", "Clientes registrados");
            kpiEventosLbl.Text  = Tr("rpt.kpi.eventos",  "Eventos del día");
            kpiBackupLbl.Text   = Tr("rpt.kpi.backup",   "días sin backup");

            mnuGuardarTxt.Text  = Tr("rpt.menu.guardartxt",  "Guardar como .TXT");
            mnuImprimir.Text    = Tr("rpt.menu.imprimir",    "Imprimir / Exportar PDF");
            mnuGuardarCsv.Text  = Tr("rpt.menu.guardarcsv",  "Guardar eventos como .CSV");
            mnuGuardarComp.Text = Tr("rpt.menu.guardarcmp",  "Guardar comparación como .TXT");
            mnuImprimirComp.Text = Tr("rpt.menu.imprimir",   "Imprimir / Exportar PDF");
        }

        // ── KPI Banner ────────────────────────────────────────────────────────

        private void ActualizarKPIs(DateTime fecha)
        {
            try
            {
                if (_verPrendas)  kpiPrendasVal.Text  = _servicio.ContarPrendasDisponibles().ToString();
                if (_verClientes) kpiClientesVal.Text = _servicio.ContarClientes().ToString();
                kpiEventosVal.Text = _servicio.ContarEventosDia(fecha).ToString();

                if (_verBackup)
                {
                    int dias = _servicio.ObtenerDiasSinBackup();
                    kpiBackupVal.Text = dias < 0 ? "!" : dias.ToString();
                }
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

                var reporte = new Exportacion.ReporteExportable
                {
                    Titulo        = $"{Tr("frm.reportejornada", "Reporte de Jornada")} — {dtpJornada.Value:dd/MM/yyyy}",
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
                MostrarError(ex, "err.exportar.titulo", "Error al exportar");
            }
        }

        private void ImprimirReporte()
        {
            try
            {
                var reporte = new Exportacion.ReporteExportable
                {
                    Titulo        = $"{Tr("frm.reportejornada", "Reporte de Jornada")} — {dtpJornada.Value:dd/MM/yyyy}",
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
                MostrarError(ex, "err.imprimir.titulo", "Error al imprimir");
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
                MostrarError(ex, "msg.error.titulo", "Error");
            }
        }

        // Exporta los eventos de negocio de la jornada como CSV tabular (Factory Method).
        // Los datos los provee BLL.ReporteJornada; acá solo se arma el reporte y se exporta.
        private void ExportarEventosCsv()
        {
            try
            {
                DateTime fecha    = dtpJornada.Value.Date;
                DataTable eventos = _servicio.ObtenerEventosDelDia(fecha);

                if (eventos == null || eventos.Rows.Count == 0)
                {
                    MessageBox.Show(
                        Tr("err.pdf.sinDatos", "No hay datos para exportar."),
                        Tr("rpt.menu.guardarcsv", "Guardar eventos como .CSV"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var headers = new string[eventos.Columns.Count];
                for (int i = 0; i < eventos.Columns.Count; i++)
                    headers[i] = eventos.Columns[i].ColumnName;

                var reporte = new Exportacion.ReporteExportable
                {
                    Titulo        = $"{Tr("frm.reportejornada", "Reporte de Jornada")} — {fecha:dd/MM/yyyy}",
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
                MostrarError(ex, "err.exportar.titulo", "Error al exportar");
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
                MostrarError(ex, "msg.error.titulo", "Error");
            }
        }

        private IDictionary<string, string> ConstruirLblReporte()
        {
            return new Dictionary<string, string>
            {
                { "titulo",        Tr("rpt.txt.titulo",       "REPORTE DE JORNADA")                                    },
                { "resumen",       Tr("rpt.txt.resumen",      "RESUMEN DEL SISTEMA")                                   },
                { "prendas",       Tr("rpt.txt.prendas",      "Prendas disponibles")                                   },
                { "clientes",      Tr("rpt.txt.clientes",     "Clientes registrados")                                  },
                { "diassinbkp",    Tr("rpt.txt.diassinbkp",   "Días sin backup")                                       },
                { "sinbackups",    Tr("rpt.txt.sinbackups",   "Sin backups")                                           },
                { "eventos",       Tr("rpt.txt.eventos",      "EVENTOS DE NEGOCIO DEL DÍA")                            },
                { "sinevt",        Tr("rpt.txt.sinevt",       "(sin eventos registrados para esta jornada)")            },
                { "usuario",       Tr("rpt.txt.usuario",      "Usuario")                                               },
                { "cliente",       Tr("rpt.txt.cliente",      "Cliente")                                               },
                { "totalevt",      Tr("rpt.txt.totalevt",     "TOTAL EVENTOS")                                         },
                { "generado",      Tr("rpt.txt.generado",     "Generado")                                              },
                { "comparacion",   Tr("rpt.txt.comparacion",  "COMPARACIÓN DE JORNADAS")                               },
                { "jornada",       Tr("rpt.txt.jornada",      "JORNADA")                                               },
                { "sinevtjorn",    Tr("rpt.txt.sinevtjorn",   "(sin eventos registrados en esta jornada)")              },
                { "comparfinal",   Tr("rpt.txt.comparfinal",  "COMPARATIVO FINAL")                                     },
                { "fecha",         Tr("rpt.txt.fecha",        "Fecha")                                                 },
                { "eventostot",    Tr("rpt.txt.eventostot",   "Eventos totales")                                       },
                { "masmasa",       Tr("rpt.txt.masmasa",      "tuvo más actividad")                                    },
                { "ninguna",       Tr("rpt.txt.ninguna",      "Ninguna jornada tuvo eventos registrados.")              },
                { "iguales",       Tr("rpt.txt.iguales",      "Ambas jornadas tuvieron la misma cantidad de eventos.") },
                { "rptoegenerado", Tr("rpt.txt.rptoegenerado","Reporte generado")                                      },
                { "compgenerada",  Tr("rpt.txt.compgenerada", "Comparación generada")                                  },
                { "impresionenv",  Tr("rpt.txt.impresionenv", "Impresión enviada")                                     },
                { "tend.titulo",   Tr("rpt.txt.tend.titulo",  "TENDENCIA DE ACTIVIDAD")                                },
                { "tend.dias",     Tr("rpt.txt.tend.dias",    "Días analizados")                                       },
                { "tend.total",    Tr("rpt.txt.tend.total",   "Total de eventos")                                      },
                { "tend.promedio", Tr("rpt.txt.tend.promedio","Promedio diario")                                       },
                { "tend.diapico",  Tr("rpt.txt.tend.diapico", "Día de mayor actividad")                                },
                { "tend.diavalle", Tr("rpt.txt.tend.diavalle","Día de menor actividad")                                },
                { "tend.detalle",  Tr("rpt.txt.tend.detalle", "DETALLE POR DÍA")                                       },
            };
        }

        // BE.AppException lleva una clave de traducción — ex.Message es el fallback hardcodeado en
        // español fijado en el throw. Este form no hereda FormBase (no tiene lblMensaje propio), así
        // que replica acá la misma resolución que FormBase.MostrarError(Exception) hace para el resto
        // de la app, y también traduce el título del MessageBox.
        private static void MostrarError(Exception ex, string claveTitulo, string tituloFallback)
        {
            string mensaje = ex is BE.AppException appEx
                ? Traductor.Resolver(appEx.Clave, ex.Message, appEx.Args, GestorIdioma.IdiomaActual)
                : ex.Message;
            string titulo = Traductor.Resolver(claveTitulo, tituloFallback, null, GestorIdioma.IdiomaActual);
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MostrarError(ex, "msg.error.titulo", "Error");
            }
        }
    }
}
