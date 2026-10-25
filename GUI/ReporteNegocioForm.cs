using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using GUI.Analitica;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Un reporte de decisión (Bloque 3 + PdN10) en su propia pantalla: Análisis de Abandono, Ventas
    /// por Vendedor, Rotación de Prendas, Tiempos de Mantenimiento, Escasez de Stock o Recomendación de
    /// Prendas. Cada uno tiene su ítem en el menú de su rol (Promociones, Inventario o Ventas) y su
    /// patente (BLL.MenuVisibilidad.Hojas); antes convivían en «Analítica de Negocio» con un selector.
    /// Lo propio de cada reporte (filtros, columnas, generador de exportación) vive en GUI.Analitica;
    /// cada uno exporta a PDF (se guarda y se abre con el visor) o CSV con su propio Factory Method de
    /// GUI.Exportacion.
    /// </summary>
    public partial class ReporteNegocioForm : FormBase, IIdiomaObserver
    {
        // Un creador por reporte.
        private static readonly Dictionary<BE.ReporteAnalitica, Func<VistaReporteAnalitica>> Vistas =
            new Dictionary<BE.ReporteAnalitica, Func<VistaReporteAnalitica>>
            {
                [BE.ReporteAnalitica.AnalisisAbandono]     = () => new VistaAbandono(),
                [BE.ReporteAnalitica.VentasVendedor]       = () => new VistaVentasVendedor(),
                [BE.ReporteAnalitica.RotacionPrendas]      = () => new VistaRotacion(),
                [BE.ReporteAnalitica.TiemposMantenimiento] = () => new VistaMantenimiento(),
                [BE.ReporteAnalitica.EscasezStock]         = () => new VistaEscasez(),
                [BE.ReporteAnalitica.RecomendacionPrendas] = () => new VistaRecomendacion(),
            };

        private readonly VistaReporteAnalitica _vista;

        /// <summary>Reporte que muestra la pantalla (el Menú reusa la que ya está abierta con el mismo).</summary>
        public BE.ReporteAnalitica Reporte { get; }

        protected override Label MensajeLabel => lblResultado;

        public ReporteNegocioForm(BE.ReporteAnalitica reporte)
        {
            InitializeComponent();
            Estilos.EstiloFormulario.BotonPrimario(btnGenerar);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarPdf);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarCsv);
            Estilos.EstiloFormulario.Grilla(dgv);
            btnExportarPdf.Enabled = btnExportarCsv.Enabled = false;

            Reporte = reporte;
            _vista = Vistas[reporte]();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var filtros = _vista.Filtros;
            if (filtros != null) pnlFiltros.Controls.Add(filtros);
            Traducir(GestorIdioma.IdiomaActual);

            // Si Cargar falla (p. ej. la BD), se reintenta al pulsar Generar.
            try { _vista.AsegurarCargada(); }
            catch (Exception ex) { MostrarError(ex); }
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private void Traducir(Idioma idioma)
        {
            this.Text           = _vista.Titulo;
            lblTitulo.Text      = _vista.Titulo;
            btnGenerar.Text     = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            if (_vista.FiltrosCreados) _vista.Traducir();

            var encabezados = _vista.Encabezados;
            if (dgv.Columns.Count == encabezados.Length)
                for (int i = 0; i < encabezados.Length; i++)
                    dgv.Columns[i].HeaderText = encabezados[i];
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            try
            {
                if (_vista.AsegurarCargada())
                {
                    // Se reintentó la carga de los filtros que había fallado, pero NO se genera: el
                    // usuario tiene que ver y elegir los filtros (p. ej. el cliente) antes del reporte.
                    lblResultado.ForeColor = System.Drawing.Color.DimGray;
                    lblResultado.Text = Tr("analitica.filtroscargados", "Se cargaron los filtros del reporte: revíselos y pulse Generar.");
                    return;
                }
                var resultado = _vista.Generar();
                if (resultado == null)
                {
                    // Falta un filtro obligatorio: no se eligió o no hay opciones (p. ej. no hay clientes).
                    dgv.DataSource = null;
                    btnExportarPdf.Enabled = btnExportarCsv.Enabled = false;
                    lblResultado.ForeColor = System.Drawing.Color.DimGray;
                    lblResultado.Text = Tr("analitica.faltafiltro", "Falta elegir un filtro del reporte, o no hay opciones para elegir (por ejemplo, no hay clientes).");
                    return;
                }

                var encabezados = _vista.Encabezados;
                dgv.DataSource = resultado.Tabla;
                for (int i = 0; i < encabezados.Length && i < dgv.Columns.Count; i++)
                    dgv.Columns[i].HeaderText = encabezados[i];
                btnExportarPdf.Enabled = btnExportarCsv.Enabled = resultado.Cantidad > 0;

                lblResultado.ForeColor = resultado.ColorMensaje;
                lblResultado.Text = resultado.Mensaje;
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
            // Si todavía no se generó el reporte, dgv.DataSource es null: Datos queda null
            // pero Encabezados no — ReporteExportable.EsTabular exige AMBOS no-null, así que sin
            // este guard cae en la rama "de texto" de los exportadores (TextoPlano vacío) y
            // muestra éxito con un CSV vacío / PDF en blanco en vez del aviso "no hay datos".
            var datos = dgv.DataSource as DataTable;
            if (datos == null || datos.Rows.Count == 0)
            {
                MessageBox.Show(
                    Tr("err.pdf.sinDatos", "No hay datos para exportar."),
                    _vista.Titulo,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var reporte = new Exportacion.ReporteExportable
            {
                Titulo = _vista.Titulo,
                NombreArchivo = _vista.NombreArchivo,
                Encabezados = _vista.Encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = _vista.CrearGenerador();
            generador.Exportar(formato, reporte, this);
        }
    }
}
