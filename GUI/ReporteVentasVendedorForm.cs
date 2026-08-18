using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN8 — Reporte de Ventas por Vendedor. El GerenteComercial genera el desempeño de
    /// cada vendedor (pedidos totales, entregados y cancelados) para evaluar rendimiento.
    /// Exporta a PDF (vista previa) o CSV (archivo) reutilizando el Factory Method de
    /// GUI.Exportacion (mismo mecanismo que Análisis de Abandono).
    /// </summary>
    public partial class ReporteVentasVendedorForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IReporteVentasVendedorService _bll = new BLL.ReporteVentasVendedor();

        private string[] _encabezados;

        protected override Label MensajeLabel => lblResultado;

        public ReporteVentasVendedorForm()
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

            this.Text          = Tr("ventasvend.titulo", "Ventas por Vendedor");
            lblTitulo.Text      = Tr("ventasvend.titulo", "Ventas por Vendedor");
            btnGenerar.Text     = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            _encabezados = new[]
            {
                Tr("ventasvend.col.vendedor", "Vendedor"),
                Tr("ventasvend.col.total", "Total pedidos"),
                Tr("ventasvend.col.entregados", "Entregados"),
                Tr("ventasvend.col.cancelados", "Cancelados"),
                Tr("ventasvend.col.tasacancel", "% Cancelación")
            };
            if (dgv.Columns.Count == _encabezados.Length)
                for (int i = 0; i < _encabezados.Length; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            try
            {
                var resultados = _bll.Obtener();

                var tabla = new DataTable();
                foreach (var h in _encabezados) tabla.Columns.Add(h);
                foreach (var r in resultados)
                    tabla.Rows.Add(r.NombreEmpleado, r.TotalPedidos, r.Entregados, r.Cancelados,
                        r.TasaCancelacion.ToString("F1") + "%");

                dgv.DataSource = tabla;
                for (int i = 0; i < _encabezados.Length && i < dgv.Columns.Count; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];

                lblResultado.ForeColor = Color.DarkGreen;
                lblResultado.Text = string.Format(
                    T("ventasvend.resultado", "{0} vendedor(es) con pedidos registrados."),
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
                NombreArchivo = "VentasPorVendedor",
                Encabezados = _encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = new Exportacion.GeneradorVentasVendedor();
            Exportacion.Exportador exportador = generador.CrearExportador(formato);
            exportador?.Exportar(reporte, this);
        }
    }
}
