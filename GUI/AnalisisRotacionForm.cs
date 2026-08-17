using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN9 — Análisis de Rotación de Prendas. El GerenteInventario genera el listado de
    /// prendas de baja demanda (candidatas a baja) o alta demanda (candidatas a
    /// reposición), cruzando el catálogo activo con la cantidad de pedidos por prenda.
    /// Exporta a PDF (vista previa) o CSV (archivo) reutilizando el Factory Method de
    /// GUI.Exportacion.
    /// </summary>
    public partial class AnalisisRotacionForm : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IAnalisisRotacionService _bll = new BLL.AnalisisRotacion();

        private string[] _encabezados;

        public AnalisisRotacionForm()
        {
            InitializeComponent();
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

            this.Text          = Tr("rotacion.titulo", "Rotación de Prendas");
            lblTitulo.Text      = Tr("rotacion.titulo", "Rotación de Prendas");
            btnGenerar.Text     = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            _encabezados = new[]
            {
                Tr("rotacion.col.prenda", "Prenda"),
                Tr("rotacion.col.categoria", "Categoría"),
                Tr("rotacion.col.cantidad", "Cant. pedidos"),
                Tr("rotacion.col.motivo", "Motivo")
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
                    tabla.Rows.Add(r.NombrePrenda, r.Categoria ?? "", r.CantidadPedidos,
                        T(r.Clave, r.Motivo, r.Args));

                dgv.DataSource = tabla;
                for (int i = 0; i < _encabezados.Length && i < dgv.Columns.Count; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];

                lblResultado.ForeColor = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen;
                lblResultado.Text = string.Format(
                    T("rotacion.resultado", "{0} prenda(s) marcada(s) por rotación."),
                    new object[] { resultados.Count });
            }
            catch (Exception ex)
            {
                lblResultado.ForeColor = Color.DarkRed;
                lblResultado.Text = ex.Message;
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
                NombreArchivo = "RotacionPrendas",
                Encabezados = _encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = new Exportacion.GeneradorRotacion();
            Exportacion.Exportador exportador = generador.CrearExportador(formato);
            exportador?.Exportar(reporte, this);
        }
    }
}
