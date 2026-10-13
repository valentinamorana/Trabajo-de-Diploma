using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using GUI.Analitica;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Analítica de Negocio — los 6 reportes de decisión (Bloque 3 + PdN10) en un solo
    /// formulario: Análisis de Abandono, Ventas por Vendedor, Rotación de Prendas, Tiempos de
    /// Mantenimiento, Escasez de Stock y Recomendación de Prendas. Antes eran 6 formularios con
    /// la misma plantilla (BLL + Generar + Exportar); ahora un selector elige el reporte y lo
    /// propio de cada uno (filtros, columnas, generador de exportación) vive en GUI.Analitica.
    ///
    /// El selector ofrece SOLO los reportes cuya patente tiene el usuario: la decisión es de la
    /// BLL (BLL.MenuVisibilidad.ReportesVisibles, la misma que decide si el ítem del menú se ve).
    /// Cada reporte exporta a PDF (vista previa) o CSV (archivo) con su propio Factory Method de
    /// GUI.Exportacion, igual que antes.
    /// </summary>
    public partial class AnaliticaNegocioForm : FormBase, IIdiomaObserver
    {
        // Un creador por reporte: la vista se instancia solo si el usuario puede verlo.
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

        private readonly List<VistaReporteAnalitica> _vistas;
        private readonly HashSet<VistaReporteAnalitica> _cargadas = new HashSet<VistaReporteAnalitica>();
        private VistaReporteAnalitica _actual;

        protected override Label MensajeLabel => lblResultado;

        /// <param name="permisos">Patentes efectivas del usuario en sesión.</param>
        /// <param name="esAdministrador">El Administrador ve todos los reportes (bypass).</param>
        public AnaliticaNegocioForm(List<BE.Permiso> permisos, bool esAdministrador)
        {
            InitializeComponent();
            Estilos.EstiloFormulario.BotonPrimario(btnGenerar);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarPdf);
            Estilos.EstiloFormulario.BotonSecundario(btnExportarCsv);
            Estilos.EstiloFormulario.Grilla(dgv);
            btnExportarPdf.Enabled = btnExportarCsv.Enabled = false;

            var patentes = (permisos ?? new List<BE.Permiso>())
                .Where(p => !string.IsNullOrEmpty(p.NombreMenu))
                .Select(p => p.NombreMenu);
            _vistas = BLL.MenuVisibilidad.ReportesVisibles(patentes, esAdministrador)
                .Select(r => Vistas[r]())
                .ToList();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);

            cmbReporte.Items.Clear();
            foreach (var v in _vistas) cmbReporte.Items.Add(v);
            if (cmbReporte.Items.Count > 0)
            {
                cmbReporte.SelectedIndex = 0; // dispara CmbReporte_SelectedIndexChanged
            }
            else
            {
                // No debería pasar (el menú no se ve sin al menos una patente), pero si se abre
                // igual, la pantalla queda inerte en vez de mostrar reportes no permitidos.
                cmbReporte.Enabled = btnGenerar.Enabled = false;
                lblResultado.Text = Tr("analitica.sinreportes", "No tenés permiso para ningún reporte de analítica.");
            }
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private void Traducir(Idioma idioma)
        {
            this.Text           = Tr("mnu.analiticanegocio", "Analítica de Negocio");
            lblTitulo.Text      = Tr("mnu.analiticanegocio", "Analítica de Negocio");
            lblReporte.Text     = Tr("analitica.reporte", "Reporte:");
            btnGenerar.Text     = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            // El combo cachea el texto de cada ítem: reasignarlos refresca el título traducido.
            for (int i = 0; i < cmbReporte.Items.Count; i++)
                cmbReporte.Items[i] = cmbReporte.Items[i];

            foreach (var v in _vistas.Where(v => _cargadas.Contains(v)))
                v.Traducir();

            if (_actual != null)
            {
                var encabezados = _actual.Encabezados;
                if (dgv.Columns.Count == encabezados.Length)
                    for (int i = 0; i < encabezados.Length; i++)
                        dgv.Columns[i].HeaderText = encabezados[i];
            }
        }

        /// <summary>
        /// Cambia de reporte: muestra sus filtros (los crea y carga la primera vez) y limpia el
        /// resultado anterior, que era de otro reporte.
        /// </summary>
        private void CmbReporte_SelectedIndexChanged(object sender, EventArgs e)
        {
            var vista = cmbReporte.SelectedItem as VistaReporteAnalitica;
            if (vista == null || vista == _actual) return;
            _actual = vista;

            pnlFiltros.Controls.Clear();
            var filtros = vista.Filtros;
            if (filtros != null) pnlFiltros.Controls.Add(filtros);

            dgv.DataSource = null;
            btnExportarPdf.Enabled = btnExportarCsv.Enabled = false;
            lblResultado.Text = string.Empty;

            if (_cargadas.Add(vista))
            {
                try
                {
                    vista.Cargar();
                }
                catch (Exception ex)
                {
                    MostrarError(ex);
                }
            }
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            if (_actual == null) return;

            try
            {
                var resultado = _actual.Generar();
                if (resultado == null) return;

                var encabezados = _actual.Encabezados;
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
            if (_actual == null) return;

            // Si todavía no se generó el reporte, dgv.DataSource es null: Datos queda null
            // pero Encabezados no — ReporteExportable.EsTabular exige AMBOS no-null, así que sin
            // este guard cae en la rama "de texto" de los exportadores (TextoPlano vacío) y
            // muestra éxito con un CSV vacío / PDF en blanco en vez del aviso "no hay datos".
            var datos = dgv.DataSource as DataTable;
            if (datos == null || datos.Rows.Count == 0)
            {
                MessageBox.Show(
                    Tr("err.pdf.sinDatos", "No hay datos para exportar."),
                    _actual.Titulo,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var reporte = new Exportacion.ReporteExportable
            {
                Titulo = _actual.Titulo,
                NombreArchivo = _actual.NombreArchivo,
                Encabezados = _actual.Encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = _actual.CrearGenerador();
            Exportacion.Exportador exportador = generador.CrearExportador(formato);
            exportador?.Exportar(reporte, this);
        }

        // Los filtros de los reportes que no están a la vista no pertenecen a ningún contenedor:
        // se liberan acá para no dejar handles colgados.
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            pnlFiltros.Controls.Clear();
            foreach (var v in _vistas.Where(v => _cargadas.Contains(v)))
                v.Filtros?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
