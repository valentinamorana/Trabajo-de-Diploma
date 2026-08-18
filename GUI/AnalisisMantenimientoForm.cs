using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN11 — Análisis de Tiempos de Mantenimiento. El GerenteInventario genera el listado
    /// de prendas cuyo historial de MantenimientoPrenda supera el umbral aceptable de
    /// cantidad o duración promedio — señal de una prenda problemática a revisar. Exporta a
    /// PDF (vista previa) o CSV (archivo) reutilizando el Factory Method de GUI.Exportacion.
    /// </summary>
    public partial class AnalisisMantenimientoForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IAnalisisMantenimientoService _bll = new BLL.AnalisisMantenimiento();

        private string[] _encabezados;

        protected override Label MensajeLabel => lblResultado;

        public AnalisisMantenimientoForm()
        {
            InitializeComponent();
            Estilos.EstiloFormulario.BotonPrimario(btnGenerar);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarPdf);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarCsv);
            Estilos.EstiloFormulario.Grilla(dgv);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private string T(string clave, string fallback, object[] args = null)
            => Traductor.Resolver(clave, fallback, args, GestorIdioma.IdiomaActual);

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string Tr(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text          = Tr("mant.analisis.titulo", "Tiempos de Mantenimiento");
            lblTitulo.Text      = Tr("mant.analisis.titulo", "Tiempos de Mantenimiento");
            btnGenerar.Text     = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            _encabezados = new[]
            {
                Tr("mant.analisis.col.prenda", "Prenda"),
                Tr("mant.analisis.col.cantidad", "Cant. mantenimientos"),
                Tr("mant.analisis.col.promedio", "Prom. días"),
                Tr("mant.analisis.col.maximo", "Máx. días"),
                Tr("mant.analisis.col.motivo", "Motivo")
            };
            if (dgv.Columns.Count == _encabezados.Length)
                for (int i = 0; i < _encabezados.Length; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            try
            {
                var resultados = _bll.Detectar();

                var tabla = new DataTable();
                foreach (var h in _encabezados) tabla.Columns.Add(h);
                foreach (var r in resultados)
                    tabla.Rows.Add(
                        r.NombrePrenda,
                        r.CantidadMantenimientos,
                        r.DuracionPromedioDias.HasValue ? r.DuracionPromedioDias.Value.ToString("F1") : "—",
                        r.DuracionMaximaDias.HasValue ? r.DuracionMaximaDias.Value.ToString() : "—",
                        T(r.Clave, r.Motivo, r.Args));

                dgv.DataSource = tabla;
                for (int i = 0; i < _encabezados.Length && i < dgv.Columns.Count; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];

                lblResultado.ForeColor = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen;
                lblResultado.Text = string.Format(
                    T("mant.analisis.resultado", "{0} prenda(s) con mantenimiento excesivo."),
                    new object[] { resultados.Count });
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void BtnExportarPdf_Click(object sender, EventArgs e) => Exportar("pdf");

        private void BtnExportarCsv_Click(object sender, EventArgs e) => Exportar("csv");

        private void Exportar(string formato)
        {
            var datos = dgv.DataSource as DataTable;
            if (datos == null || datos.Rows.Count == 0)
            {
                MessageBox.Show(
                    T("err.pdf.sinDatos", "No hay datos para exportar."),
                    this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var reporte = new Exportacion.ReporteExportable
            {
                Titulo = this.Text,
                NombreArchivo = "TiemposMantenimiento",
                Encabezados = _encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = new Exportacion.GeneradorMantenimiento();
            Exportacion.Exportador exportador = generador.CrearExportador(formato);
            exportador?.Exportar(reporte, this);
        }
    }
}
