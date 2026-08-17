using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN13 — Recomendación de Prendas para un Cliente. El Vendedor elige un cliente y
    /// genera prendas Disponibles afines a su historial de pedidos (categoría/color más
    /// pedidos), para ofrecerlas en el próximo pedido. Exporta a PDF (vista previa) o CSV
    /// (archivo) reutilizando el Factory Method de GUI.Exportacion.
    /// </summary>
    public partial class RecomendacionPrendasForm : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService _bllCliente = new BLL.Cliente();
        private readonly BLL.Interfaces.IRecomendacionService _bll = new BLL.RecomendacionPrendas();

        private string[] _encabezados;

        public RecomendacionPrendasForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
            CargarClientes();
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

            this.Text          = Tr("recom.titulo", "Recomendación de Prendas");
            lblTitulo.Text      = Tr("recom.titulo", "Recomendación de Prendas");
            lblCliente.Text     = Tr("renov.cliente", "Cliente:");
            btnGenerar.Text     = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            _encabezados = new[]
            {
                Tr("recom.col.prenda", "Prenda"),
                Tr("recom.col.categoria", "Categoría"),
                Tr("recom.col.color", "Color"),
                Tr("recom.col.motivo", "Motivo")
            };
            if (dgv.Columns.Count == _encabezados.Length)
                for (int i = 0; i < _encabezados.Length; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];
        }

        private void CargarClientes()
        {
            try
            {
                cmbCliente.Items.Clear();
                foreach (var c in _bllCliente.ObtenerTodos())
                    cmbCliente.Items.Add(new ClienteItem(c));
                if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                lblResultado.ForeColor = Color.DarkRed;
                lblResultado.Text = ex.Message;
            }
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            if (!(cmbCliente.SelectedItem is ClienteItem item)) return;

            try
            {
                var resultados = _bll.Recomendar(item.Cliente.IdCliente);

                var tabla = new DataTable();
                foreach (var h in _encabezados) tabla.Columns.Add(h);
                foreach (var r in resultados)
                    tabla.Rows.Add(r.Prenda.Nombre, r.Prenda.Categoria ?? "", r.Prenda.Color ?? "",
                        T(r.Clave, r.Motivo, r.Args));

                dgv.DataSource = tabla;
                for (int i = 0; i < _encabezados.Length && i < dgv.Columns.Count; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];

                lblResultado.ForeColor = resultados.Count > 0 ? Color.DarkGreen : Color.DarkOrange;
                lblResultado.Text = resultados.Count > 0
                    ? string.Format(T("recom.resultado", "{0} prenda(s) recomendada(s)."), new object[] { resultados.Count })
                    : T("recom.sinhistorial", "El cliente no tiene historial de pedidos suficiente para recomendar.");
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
                NombreArchivo = "RecomendacionPrendas",
                Encabezados = _encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = new Exportacion.GeneradorRecomendacion();
            Exportacion.Exportador exportador = generador.CrearExportador(formato);
            exportador?.Exportar(reporte, this);
        }

        private sealed class ClienteItem
        {
            public BE.Cliente Cliente { get; }
            public ClienteItem(BE.Cliente c) => Cliente = c;
            public override string ToString() => $"{Cliente.NombreCompleto} (DNI {Cliente.DNI})";
        }
    }
}
