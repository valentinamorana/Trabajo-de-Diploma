using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN12 — Detección de Escasez por Talle/Categoría. El GerenteInventario configura un
    /// umbral mínimo de stock Disponible y genera las combinaciones Talle+Categoría que
    /// caen por debajo, para planificar compras antes de un faltante. Exporta a PDF (vista
    /// previa) o CSV (archivo) reutilizando el Factory Method de GUI.Exportacion.
    /// </summary>
    public partial class AnalisisEscasezForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IAnalisisEscasezService _bll = new BLL.AnalisisEscasez();

        private string[] _encabezados;

        protected override Label MensajeLabel => lblResultado;

        public AnalisisEscasezForm()
        {
            InitializeComponent();
            Estilos.EstiloFormulario.BotonPrimario(btnGenerar);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarPdf);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarCsv);
            Estilos.EstiloFormulario.Grilla(dgv);
            btnExportarPdf.Enabled = btnExportarCsv.Enabled = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
            numUmbral.Value = BLL.AnalisisEscasez.UmbralPorDefecto;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private void Traducir(Idioma idioma)
        {

            this.Text          = Tr("escasez.titulo", "Escasez de Stock");
            lblTitulo.Text      = Tr("escasez.titulo", "Escasez de Stock");
            lblUmbral.Text      = Tr("escasez.umbral", "Umbral mínimo:");
            btnGenerar.Text     = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            _encabezados = new[]
            {
                Tr("escasez.col.talle", "Talle"),
                Tr("escasez.col.categoria", "Categoría"),
                Tr("escasez.col.disponible", "Disponible"),
                Tr("escasez.col.motivo", "Motivo")
            };
            if (dgv.Columns.Count == _encabezados.Length)
                for (int i = 0; i < _encabezados.Length; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            try
            {
                var resultados = _bll.Detectar((int)numUmbral.Value);

                var tabla = new DataTable();
                foreach (var h in _encabezados) tabla.Columns.Add(h);
                foreach (var r in resultados)
                    tabla.Rows.Add(r.Talle, r.Categoria, r.CantidadDisponible, Tr(r.Clave, r.Motivo, r.Args));

                dgv.DataSource = tabla;
                for (int i = 0; i < _encabezados.Length && i < dgv.Columns.Count; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];
                btnExportarPdf.Enabled = btnExportarCsv.Enabled = resultados.Count > 0;

                lblResultado.ForeColor = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen;
                lblResultado.Text = string.Format(
                    Tr("escasez.resultado", "{0} combinación(es) por debajo del umbral."),
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
                    Tr("err.pdf.sinDatos", "No hay datos para exportar."),
                    this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var reporte = new Exportacion.ReporteExportable
            {
                Titulo = this.Text,
                NombreArchivo = "EscasezStock",
                Encabezados = _encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = new Exportacion.GeneradorEscasez();
            Exportacion.Exportador exportador = generador.CrearExportador(formato);
            exportador?.Exportar(reporte, this);
        }
    }
}
