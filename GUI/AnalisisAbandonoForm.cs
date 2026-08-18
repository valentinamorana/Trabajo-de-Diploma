using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN10 — Análisis de Abandono. El Gerente Comercial elige el criterio de riesgo
    /// (patrón Strategy, BLL.Estrategias) y genera la lista de clientes que ese criterio
    /// marca en riesgo, cruzando su último pedido contra el vencimiento de la suscripción.
    /// Exporta a PDF (vista previa) o CSV (archivo) reutilizando el Factory Method de
    /// GUI.Exportacion (mismo mecanismo que Bitácora / Reporte de Jornada).
    /// </summary>
    public partial class AnalisisAbandonoForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IAnalisisAbandonoService _bll = new BLL.AnalisisAbandono();

        private string[] _encabezados;

        protected override Label MensajeLabel => lblResultado;

        public AnalisisAbandonoForm()
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
            Traducir(GestorIdioma.IdiomaActual); // ya incluye CargarEstrategias()
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

            this.Text            = Tr("abandono.titulo", "Análisis de Abandono");
            lblTitulo.Text        = Tr("abandono.titulo", "Análisis de Abandono");
            lblEstrategia.Text    = Tr("abandono.estrategia", "Criterio de riesgo:");
            btnGenerar.Text       = Tr("abandono.generar", "Generar");
            btnExportarPdf.Text   = Tr("bit.menu.exportarpdf", "Exportar a PDF");
            btnExportarCsv.Text   = Tr("rpt.menu.guardarcsv", "Guardar como .CSV");

            _encabezados = new[]
            {
                Tr("abandono.col.cliente", "Cliente"),
                Tr("abandono.col.plan", "Plan"),
                Tr("abandono.col.vencimiento", "Vencimiento"),
                Tr("abandono.col.ultimopedido", "Último pedido"),
                Tr("abandono.col.motivo", "Motivo")
            };
            if (dgv.Columns.Count == _encabezados.Length)
                for (int i = 0; i < _encabezados.Length; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];

            CargarEstrategias();
        }

        /// <summary>
        /// Rellena el combo de estrategias con los 3 criterios traducidos, respetando el
        /// índice previamente seleccionado — mismo criterio que
        /// Bitacora.RellenarComboCriticidad para que un cambio de idioma no pierda la
        /// selección del usuario. El orden de las 3 estrategias es fijo, así que
        /// preservar por índice (no por instancia) es seguro.
        /// </summary>
        private void CargarEstrategias()
        {
            int idx = cmbEstrategia.SelectedIndex;
            if (idx < 0) idx = 0;

            cmbEstrategia.Items.Clear();
            cmbEstrategia.Items.Add(new EstrategiaItem(new BLL.Estrategias.EstrategiaVencimientoInactividad(),
                T("abandono.estr.vencinactividad", "Vence pronto y sin actividad reciente")));
            cmbEstrategia.Items.Add(new EstrategiaItem(new BLL.Estrategias.EstrategiaInactividadPura(),
                T("abandono.estr.inactividad", "Sin actividad hace tiempo (aunque esté vigente)")));
            cmbEstrategia.Items.Add(new EstrategiaItem(new BLL.Estrategias.EstrategiaClienteNuevoInactivo(),
                T("abandono.estr.nuncaactivo", "Se dio de alta y nunca hizo un pedido")));

            cmbEstrategia.SelectedIndex = (idx >= 0 && idx < cmbEstrategia.Items.Count) ? idx : 0;
        }

        private void BtnGenerar_Click(object sender, EventArgs e)
        {
            if (!(cmbEstrategia.SelectedItem is EstrategiaItem item)) return;

            try
            {
                _bll.CambiarEstrategia(item.Estrategia);
                var resultados = _bll.Detectar();

                var tabla = new DataTable();
                foreach (var h in _encabezados) tabla.Columns.Add(h);
                foreach (var r in resultados)
                {
                    tabla.Rows.Add(
                        r.NombreCliente,
                        r.NombrePlan ?? "",
                        r.FechaVencimiento.HasValue ? r.FechaVencimiento.Value.ToString("dd/MM/yyyy") : T("susc.sinfecha", "sin fecha"),
                        r.FechaUltimoPedido.HasValue ? r.FechaUltimoPedido.Value.ToString("dd/MM/yyyy") : T("abandono.nuncapidio", "nunca"),
                        T(r.Clave, r.Motivo, r.Args));
                }
                dgv.DataSource = tabla;
                for (int i = 0; i < _encabezados.Length && i < dgv.Columns.Count; i++)
                    dgv.Columns[i].HeaderText = _encabezados[i];

                lblResultado.ForeColor = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen;
                lblResultado.Text = string.Format(
                    T("abandono.resultado", "{0} cliente(s) en riesgo según '{1}'."),
                    new object[] { resultados.Count, item.ToString() });
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
            // pero Encabezados no (se llena en Traducir/OnLoad) — ReporteExportable.EsTabular
            // exige AMBOS no-null, así que sin este guard cae en la rama "de texto" de los
            // exportadores (TextoPlano vacío) y muestra éxito con un CSV vacío / PDF en blanco
            // en vez del aviso "no hay datos para exportar".
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
                NombreArchivo = "AnalisisAbandono",
                Encabezados = _encabezados,
                Datos = datos
            };

            Exportacion.GeneradorReporte generador = new Exportacion.GeneradorAnalisisAbandono();
            Exportacion.Exportador exportador = generador.CrearExportador(formato);
            exportador?.Exportar(reporte, this);
        }

        // Envuelve una estrategia con un nombre amigable para el combo (igual criterio
        // que ClienteItem/PlanItem en RenovacionSuscripcionForm).
        private sealed class EstrategiaItem
        {
            public BLL.Estrategias.EstrategiaRiesgo Estrategia { get; }
            private readonly string _nombre;
            public EstrategiaItem(BLL.Estrategias.EstrategiaRiesgo estrategia, string nombre)
            {
                Estrategia = estrategia;
                _nombre = nombre;
            }
            public override string ToString() => _nombre;
        }
    }
}
