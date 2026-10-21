using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Servicios.Multiidioma;
using static GUI.Inteligencia.TextosComerciales;
using static GUI.Inteligencia.TextosMetas;

namespace GUI.Inteligencia
{
    /// <summary>
    /// PN03 «Metas comerciales» — Pestaña "Metas" del Centro de decisiones: Gerencia fija el objetivo del mes
    /// para cada indicador (MRR, % de bajas, ocupación del stock) y ve el real contra el objetivo, el avance,
    /// la proyección al cierre, el semáforo y el gráfico de los últimos meses. Toda la lógica (validación,
    /// medición, proyección, semáforo) es de BLL.Inteligencia.MetasComerciales; este control solo pide y dibuja.
    /// Al guardar o eliminar una meta avisa (MetasCambiaron): el Centro vuelve a analizar, porque una meta
    /// en riesgo sube la prioridad de las recomendaciones que la mueven.
    /// </summary>
    public partial class PanelMetasComerciales : UserControl, IIdiomaObserver
    {
        public event EventHandler MetasCambiaron;

        private BLL.Inteligencia.InteligenciaComercial _bll;
        private string _modulo;
        private List<BE.SeguimientoMeta> _seguimiento = new List<BE.SeguimientoMeta>();
        private bool _cargando;
        // Generación de la carga: si el mes cambia mientras se calcula, el resultado viejo se ignora.
        private int _generacion;

        private sealed class ItemIndicador
        {
            public BE.IndicadorMeta Valor { get; set; }
            public override string ToString() => Indicador(Valor);
        }

        public PanelMetasComerciales()
        {
            InitializeComponent();
            // Un objetivo ambiguo ("2.5") no se lee como 25: vuelve al valor anterior y se explica.
            numObjetivo.EntradaInvalida += (s, mensaje) => Mensaje(mensaje, true);
            Estilos.EstiloFormulario.Grilla(dgvMetas);
            Estilos.EstiloFormulario.BotonSecundario(btnActualizar);
            Estilos.EstiloFormulario.BotonPrimario(btnGuardar);
            Estilos.EstiloFormulario.BotonSecundario(btnEliminar);
            dgvMetas.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _cargando = true;
            dtpMes.Value = DateTime.Today;
            _cargando = false;
        }

        /// <summary>true cuando ya se mostró al menos una vez (el Informe usa lo que se ve).</summary>
        public bool Cargado { get; private set; }

        /// <summary>Lo que muestra la pestaña (copia).</summary>
        public List<BE.SeguimientoMeta> Seguimiento => _seguimiento.ToList();

        public void Inicializar(BLL.Inteligencia.InteligenciaComercial bll, string modulo)
        {
            _bll = bll ?? throw new ArgumentNullException(nameof(bll));
            _modulo = modulo;
            Traducir();
        }

        public void CargarSiHaceFalta()
        {
            if (!Cargado) _ = CargarAsync();
        }

        // ── Idioma ─────────────────────────────────────────────────────────────

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            GestorIdioma.SuscribirObservador(this);
            if (split.Height > 120) split.SplitterDistance = Math.Max(90, split.Height * 2 / 5);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnHandleDestroyed(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            if (IsDisposed) return;
            Traducir();
            Mostrar();
        }

        private void Traducir()
        {
            if (Parent is TabPage pestana) pestana.Text = Tr("cdc.tab.metas", "Metas");
            lblMes.Text = Tr("cdc.meta.mes", "Mes:");
            btnActualizar.Text = Tr("cdc.meta.actualizar", "Actualizar");
            lblInfo.Text = Tr("cdc.meta.info",
                "Fije el objetivo del mes de cada indicador. El semáforo compara la proyección al cierre con el objetivo; una meta en riesgo sube la prioridad de las recomendaciones que la mueven.");
            lblIndicador.Text = Tr("cdc.meta.indicador", "Indicador:");
            lblObjetivo.Text = Tr("cdc.meta.objetivo", "Objetivo:");
            btnGuardar.Text = Tr("cdc.meta.guardar", "Guardar meta");
            btnEliminar.Text = Tr("cdc.meta.eliminar", "Eliminar meta");
            tip.SetToolTip(btnGuardar, Tr("cdc.meta.tip.guardar", "Fija el objetivo del indicador para el mes elegido (si ya tenía uno, lo reemplaza)."));
            tip.SetToolTip(dtpMes, Tr("cdc.meta.tip.mes", "Las metas se editan para el mes en curso y los siguientes; los meses cerrados solo se consultan."));
            var elegido = (cmbIndicador.SelectedItem as ItemIndicador)?.Valor;
            cmbIndicador.Items.Clear();
            foreach (var d in BLL.Inteligencia.CatalogoIndicadores.Todas)
                cmbIndicador.Items.Add(new ItemIndicador { Valor = d.Indicador });
            SeleccionarIndicador(elegido ?? BE.IndicadorMeta.Mrr);
        }

        // ── Carga ──────────────────────────────────────────────────────────────

        private DateTime MesElegido => BE.MetaComercial.InicioDeMes(dtpMes.Value);

        private void DtpMes_ValueChanged(object sender, EventArgs e)
        {
            if (_cargando || _bll == null) return;
            _ = CargarAsync();
        }

        private void BtnActualizar_Click(object sender, EventArgs e) => _ = CargarAsync();

        public async Task CargarAsync()
        {
            if (_bll == null) return;
            DateTime mes = MesElegido;
            int generacion = ++_generacion;
            var anterior = Cursor;
            Cursor = Cursors.WaitCursor;
            btnActualizar.Enabled = false;
            try
            {
                var lista = await Task.Run(() => _bll.SeguirMetas(mes));
                if (IsDisposed || generacion != _generacion) return;   // llegó tarde: ya se pidió otro mes
                _seguimiento = lista ?? new List<BE.SeguimientoMeta>();
                Cargado = true;
                Mostrar();
                if (_seguimiento.Count == 0)
                    Mensaje(Tr("cdc.meta.ninguna", "No hay metas para {0:yyyy-MM}. Elija un indicador, escriba el objetivo y pulse Guardar meta.", mes), false);
                else
                    Mensaje(string.Empty, false);
            }
            catch (Exception ex) { if (!IsDisposed && generacion == _generacion) Mensaje(Error(ex), true); }
            finally
            {
                if (!IsDisposed && generacion == _generacion)
                {
                    Cursor = anterior;
                    btnActualizar.Enabled = true;
                }
            }
        }

        private void Mostrar()
        {
            var t = new DataTable();
            foreach (var c in new[] { "Indicador", "Objetivo", "Real", "Avance", "Proyeccion", "Estado", "Explicacion" })
                t.Columns.Add(c, typeof(string));
            foreach (var s in _seguimiento)
                t.Rows.Add(R(s.NombreIndicador), ValorMeta(s.Unidad, s.Meta.ValorObjetivo) + (s.Meta.Sentido == BE.SentidoMeta.MenorEsMejor ? " " + Tr("cdc.meta.maximo", "(máx.)") : ""),
                    ValorMeta(s.Unidad, s.ValorActual), s.AvancePorcentaje.HasValue ? s.AvancePorcentaje.Value.ToString("0.#") + " %" : "—",
                    ValorMeta(s.Unidad, s.Proyeccion), EstadoMeta(s.Estado), R(s.Explicacion));
            dgvMetas.DataSource = t;
            if (dgvMetas.Columns.Count == 7)
            {
                string[] titulos =
                {
                    Tr("cdc.col.indicador", "Indicador"), Tr("cdc.meta.col.objetivo", "Objetivo"), Tr("cdc.meta.col.real", "Real"),
                    Tr("cdc.meta.col.avance", "Avance"), Tr("cdc.meta.col.proyeccion", "Proyección al cierre"),
                    Tr("cdc.meta.col.estado", "Estado"), Tr("cdc.meta.col.explicacion", "Por qué")
                };
                int[] pesos = { 22, 14, 14, 10, 16, 12, 60 };
                for (int i = 0; i < 7; i++)
                {
                    dgvMetas.Columns[i].HeaderText = titulos[i];
                    dgvMetas.Columns[i].FillWeight = pesos[i];
                    dgvMetas.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
            bool editable = BLL.Inteligencia.MetasComerciales.EsMesEditable(MesElegido, DateTime.Today);
            btnGuardar.Enabled = editable;
            numObjetivo.Enabled = editable;
            ActualizarSeleccion();
            if (!editable)
                Mensaje(Tr("cdc.meta.soloconsulta", "Mes cerrado (o demasiado lejano): sus metas solo se consultan."), false);
        }

        private BE.SeguimientoMeta Seleccionada()
        {
            if (dgvMetas.SelectedRows.Count == 0) return null;
            var drv = dgvMetas.SelectedRows[0].DataBoundItem as DataRowView;
            int i = drv != null ? drv.Row.Table.Rows.IndexOf(drv.Row) : -1;
            return i >= 0 && i < _seguimiento.Count ? _seguimiento[i] : null;
        }

        private void DgvMetas_SelectionChanged(object sender, EventArgs e) => ActualizarSeleccion();

        private void ActualizarSeleccion()
        {
            var s = Seleccionada() ?? _seguimiento.FirstOrDefault();
            GraficoMetas.Dibujar(chartMetas, s);
            btnEliminar.Enabled = s != null && BLL.Inteligencia.MetasComerciales.EsMesEditable(s.Meta.Periodo, DateTime.Today);
            if (s == null) return;
            SeleccionarIndicador(s.Meta.Indicador);
            Asignar(s.Meta.ValorObjetivo);
        }

        private void DgvMetas_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 5) return;
            var drv = dgvMetas.Rows[e.RowIndex].DataBoundItem as DataRowView;
            int i = drv != null ? drv.Row.Table.Rows.IndexOf(drv.Row) : -1;
            if (i < 0 || i >= _seguimiento.Count) return;
            var (fondo, texto) = ColorEstado(_seguimiento[i].Estado);
            e.CellStyle.BackColor = fondo;
            e.CellStyle.ForeColor = texto;
            e.CellStyle.Font = Tema.FuenteSubtitulo;
        }

        // ── Editor ─────────────────────────────────────────────────────────────

        private void SeleccionarIndicador(BE.IndicadorMeta indicador)
        {
            var item = cmbIndicador.Items.Cast<ItemIndicador>().FirstOrDefault(x => x.Valor == indicador);
            if (item != null && !ReferenceEquals(cmbIndicador.SelectedItem, item)) cmbIndicador.SelectedItem = item;
        }

        // Rango y decimales según el indicador (el rango válido es de la BLL: CatalogoIndicadores).
        private void CmbIndicador_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(cmbIndicador.SelectedItem is ItemIndicador item)) return;
            var def = BLL.Inteligencia.CatalogoIndicadores.De(item.Valor);
            numObjetivo.Minimum = def.Minimo;
            numObjetivo.Maximum = def.Maximo;
            numObjetivo.DecimalPlaces = def.Unidad == BE.UnidadKpi.Moneda ? 0 : 1;
            numObjetivo.Increment = def.Unidad == BE.UnidadKpi.Moneda ? 1000 : 1;
            var existente = _seguimiento.FirstOrDefault(s => s.Meta.Indicador == item.Valor);
            Asignar(existente?.Meta.ValorObjetivo ?? def.Minimo);
        }

        private void Asignar(decimal valor) =>
            numObjetivo.Value = Math.Min(numObjetivo.Maximum, Math.Max(numObjetivo.Minimum, valor));

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (_bll?.Metas == null || !(cmbIndicador.SelectedItem is ItemIndicador item)) return;
            try
            {
                var meta = _bll.Metas.Guardar(_modulo, item.Valor, MesElegido, numObjetivo.Value);
                Mensaje(Tr("cdc.meta.guardada", "Meta guardada: {0} para {1:yyyy-MM}.", Indicador(meta.Indicador), meta.Periodo), false);
                Recargar();
            }
            catch (Exception ex) { Mensaje(Error(ex), true); }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            var s = Seleccionada() ?? _seguimiento.FirstOrDefault();
            if (_bll?.Metas == null || s == null) return;
            if (!FormBase.MostrarConfirmacionSiNo(FindForm(),
                    Tr("cdc.meta.confirmareliminar", "¿Eliminar la meta de {0} para {1:yyyy-MM}?", R(s.NombreIndicador), s.Meta.Periodo),
                    Tr("cdc.meta.eliminar", "Eliminar meta")))
                return;
            try
            {
                _bll.Metas.Eliminar(_modulo, s.Meta);
                Mensaje(Tr("cdc.meta.eliminada", "Meta eliminada."), false);
                Recargar();
            }
            catch (Exception ex) { Mensaje(Error(ex), true); }
        }

        private async void Recargar()
        {
            await CargarAsync();
            MetasCambiaron?.Invoke(this, EventArgs.Empty);
        }

        // ── Mensajes y exportación ─────────────────────────────────────────────

        private void Mensaje(string texto, bool esError)
        {
            lblMensaje.Text = texto ?? string.Empty;
            lblMensaje.ForeColor = esError ? Tema.Error : Tema.Exito;
        }

        // Mismo criterio que FormBase.MensajeDeError: el error de negocio, traducido; cualquier otro, un
        // mensaje genérico (el detalle técnico va al registro de seguimiento, no a la pantalla).
        private static string Error(Exception ex)
        {
            if (ex is BE.AppException app)
                return Traductor.Resolver(app.Clave, app.Message, app.Args, GestorIdioma.IdiomaActual);
            System.Diagnostics.Trace.TraceError("[PanelMetasComerciales] " + ex);
            return Tr("msg.error.inesperado", "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.");
        }

        /// <summary>El gráfico como PNG para el Informe ejecutivo.</summary>
        public byte[] GraficoPng() => _seguimiento.Count == 0 ? null : GraficosComerciales.Png(chartMetas);
    }
}
