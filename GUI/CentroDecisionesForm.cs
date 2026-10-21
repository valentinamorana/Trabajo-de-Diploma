using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GUI.Inteligencia;
using Servicios.Multiidioma;
using BLLI = BLL.Inteligencia;

namespace GUI
{
    /// <summary>
    /// PN03 «Inteligencia comercial y promociones» — Centro de decisiones comerciales (Gerencia,
    /// patente mnuSugerenciaPromocion). Recorre el ciclo completo del proceso:
    ///
    ///   1. Analizar ........ KPI con variación contra el período anterior + 4 gráficos (tendencia con
    ///                        proyección, ingreso en riesgo por plan, stock, rotación)
    ///   2. Recomendaciones . el motor de reglas propone qué hacer, con diagnóstico, motivo, impacto con
    ///                        su fórmula, confianza y alternativas; Aceptar ejecuta la acción (sugerencia,
    ///                        contactos, orden de compra, baja o revisión de prendas, solicitud de baja),
    ///                        Descartar pide motivo, Posponer oculta N días
    ///   3. Historial ....... las decisiones tomadas, con el impacto estimado contra el real (cierre del ciclo)
    ///   4. Simulador ....... "¿Qué pasa si…?": costo, beneficio, ROI, retención de equilibrio, curva de
    ///                        sensibilidad y hasta 3 escenarios comparados; crea la sugerencia (PN03)
    ///   5. Medir resultado . el «Resultado de la promoción» en tablas y gráfico, con veredicto y botón
    ///   Metas .............. objetivo mensual por indicador, real, proyección y semáforo
    ///                        (PanelMetasComerciales; una meta en riesgo sube la prioridad de lo que la mueve)
    ///
    /// Toda la lógica vive en BLL.Inteligencia; este formulario solo pide, dibuja y abre pantallas. Las
    /// lecturas pesadas corren en segundo plano (Task.Run + cursor de espera) sin congelar la interfaz.
    /// </summary>
    public partial class CentroDecisionesForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private const string Modulo = "Centro de decisiones comerciales";
        private const int MaximoEscenarios = 3;

        private readonly BLLI.InteligenciaComercial bll = new BLLI.InteligenciaComercial();
        private readonly BLL.Interfaces.IPlanSuscripcionService planBLL = new BLL.PlanSuscripcion();
        private BLLI.DecisionesComerciales Decisiones => bll.Decisiones;

        private BE.PanelComercial _panel;
        private List<BE.Recomendacion> _recs = new List<BE.Recomendacion>();
        private List<BE.Recomendacion> _recsVisibles = new List<BE.Recomendacion>();
        private List<string> _reglasConError = new List<string>();
        private List<BE.DecisionComercial> _historial = new List<BE.DecisionComercial>();
        private Dictionary<int, BE.MedicionDecision> _medidos = new Dictionary<int, BE.MedicionDecision>();
        private List<BE.MedicionPromocion> _mediciones = new List<BE.MedicionPromocion>();
        private BE.ResultadoSimulacion _simActual;
        private readonly List<BE.ResultadoSimulacion> _escenarios = new List<BE.ResultadoSimulacion>();
        private Dictionary<string, decimal> _umbrales = BLLI.CatalogoUmbrales.ValoresPorDefecto();
        private List<BE.PlanSuscripcion> _planes = new List<BE.PlanSuscripcion>();
        private bool _cargando;

        private enum Preset { Ultimos30, Ultimos90, Ultimos180, EsteMes, MesAnterior, Personalizado }

        private sealed class Item<T>
        {
            public T Valor { get; set; }
            public string Texto { get; set; }
            public override string ToString() => Texto;
        }

        public CentroDecisionesForm()
        {
            InitializeComponent();
            InicializarCohortes();   // pestaña «1b. Cohortes» (CentroDecisionesForm.Cohortes.cs)
            // Simulador y días de posponer: un número ambiguo ("2.5") no se lee como 25, se explica.
            foreach (var n in new[] { numDiasPosponer, numSimClientesPlan, numSimObjetivo, numSimDescuento, numSimDuracion, numSimRetencion, numSimHorizonte })
                n.EntradaInvalida += (s, mensaje) => MostrarError(mensaje);
            Estilos.EstiloFormulario.BotonPrimario(btnAnalizar);
            Estilos.EstiloFormulario.BotonSecundario(btnUmbrales);
            Estilos.EstiloFormulario.BotonSecundario(btnGlosario);
            Estilos.EstiloFormulario.BotonPrimario(btnInforme);
            Estilos.EstiloFormulario.BotonPrimario(btnAceptar);
            Estilos.EstiloFormulario.BotonSecundario(btnDescartar);
            Estilos.EstiloFormulario.BotonSecundario(btnPosponer);
            Estilos.EstiloFormulario.BotonSecundario(btnSimularOpcion);
            Estilos.EstiloFormulario.BotonSecundario(btnRecsCsv);
            Estilos.EstiloFormulario.BotonSecundario(btnHistFiltrar);
            Estilos.EstiloFormulario.BotonSecundario(btnHistCsv);
            Estilos.EstiloFormulario.BotonPrimario(btnRegistrarResultado);
            Estilos.EstiloFormulario.BotonSecundario(btnHistDescartar);
            Estilos.EstiloFormulario.BotonSecundario(btnSimAgregar);
            Estilos.EstiloFormulario.BotonSecundario(btnSimLimpiar);
            Estilos.EstiloFormulario.BotonPrimario(btnSimCrearSugerencia);
            Estilos.EstiloFormulario.BotonPrimario(btnMedir);
            Estilos.EstiloFormulario.BotonPrimario(btnAplicarVeredicto);
            foreach (var dgv in new[] { dgvRecomendaciones, dgvOpciones, dgvHistorial, dgvEscenarios, dgvMedicion })
                Estilos.EstiloFormulario.Grilla(dgv);
            // Textos largos (títulos, fórmulas): la fila crece en vez de cortar el texto.
            foreach (var dgv in new[] { dgvRecomendaciones, dgvOpciones, dgvHistorial })
            {
                dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }
            dgvRecomendaciones.CellFormatting += DgvRecomendaciones_CellFormatting;
            btnAceptar.Enabled = btnDescartar.Enabled = btnPosponer.Enabled = btnSimularOpcion.Enabled = false;
            btnRegistrarResultado.Enabled = btnHistDescartar.Enabled = false;
            btnAplicarVeredicto.Enabled = false;
            AgregarPestanaMetas();   // CentroDecisionesForm.Metas.cs
        }

        // ══ Ciclo de vida e idioma ══════════════════════════════════════════════

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _cargando = true;
            try
            {
                dtpHasta.Value = DateTime.Today;
                dtpDesde.Value = DateTime.Today.AddDays(-89);
                dtpHistDesde.Value = DateTime.Today.AddDays(-180);
                dtpHistHasta.Value = DateTime.Today;
                dtpMedirDesde.Value = DateTime.Today.AddDays(-180);
                dtpMedirHasta.Value = DateTime.Today;
                try { _planes = planBLL.ObtenerActivos() ?? new List<BE.PlanSuscripcion>(); }
                catch (Exception ex) { MostrarError(ex); }
            }
            finally { _cargando = false; }
            Traducir(GestorIdioma.IdiomaActual);
            if (cmbPreset.Items.Count > 1) cmbPreset.SelectedIndex = (int)Preset.Ultimos90;
            DibujarVacio();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Primera carga automática: el panel abre con los últimos 90 días ya analizados.
            BtnAnalizar_Click(this, EventArgs.Empty);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            RedibujarTodo();
        }

        /// <summary>F1: la ayuda de la pantalla más el glosario de métricas.</summary>
        protected override string AyudaAdicional() => TextosComerciales.GlosarioTexto();

        private void Traducir(Idioma idioma)
        {
            Text = Tr("mnu.centrodecisiones", "Centro de decisiones comerciales");
            lblTitulo.Text = Tr("cdc.titulo", "Centro de decisiones comerciales — Inteligencia comercial y promociones");
            lblDesde.Text = lblHistDesde.Text = lblMedirDesde.Text = Tr("cdc.desde", "Desde:");
            lblHasta.Text = lblHistHasta.Text = lblMedirHasta.Text = Tr("cdc.hasta", "Hasta:");
            lblPlan.Text = Tr("cdc.plan", "Plan:");
            btnAnalizar.Text = Tr("cdc.btn.analizar", "Analizar");
            btnUmbrales.Text = Tr("cdc.btn.umbrales", "Umbrales");
            btnGlosario.Text = Tr("cdc.btn.glosario", "Glosario");
            btnInforme.Text = Tr("cdc.btn.informe", "Informe ejecutivo (PDF)");
            tabAnalizar.Text = Tr("cdc.tab.analizar", "1. Analizar");
            tabRecomendaciones.Text = Tr("cdc.tab.recomendaciones", "2. Recomendaciones");
            tabHistorial.Text = Tr("cdc.tab.historial", "3. Historial de decisiones");
            tabSimulador.Text = Tr("cdc.tab.simulador", "4. Simulador");
            tabMedir.Text = Tr("cdc.tab.medir", "5. Medir resultado");
            chkMostrarOcultas.Text = Tr("cdc.recs.mostrarocultas", "Mostrar pospuestas y descartadas");
            btnRecsCsv.Text = btnHistCsv.Text = Tr("cdc.btn.csv", "Exportar a .CSV");
            lblOpciones.Text = Tr("cdc.recs.opciones", "Opciones (la primera es la que recomienda el motor; elija una)");
            btnAceptar.Text = Tr("cdc.btn.aceptar", "Aceptar y ejecutar");
            btnDescartar.Text = Tr("cdc.btn.descartar", "Descartar");
            btnPosponer.Text = Tr("cdc.btn.posponer", "Posponer");
            lblDias.Text = Tr("cdc.dias", "días");
            lblHistEstado.Text = Tr("cdc.hist.estado", "Estado:");
            btnHistFiltrar.Text = Tr("cdc.btn.filtrar", "Filtrar");
            btnRegistrarResultado.Text = Tr("cdc.btn.resultado", "Registrar resultado real");
            btnHistDescartar.Text = Tr("cdc.btn.descartardecision", "Descartar decisión");
            lblSimPlan.Text = Tr("cdc.plan", "Plan:");
            lblSimClientesPlan.Text = Tr("cdc.sim.clientesplan", "Clientes del plan:");
            lblSimObjetivo.Text = Tr("cdc.sim.objetivo", "Clientes objetivo (en riesgo):");
            lblSimDescuento.Text = Tr("cdc.sim.descuento", "Descuento (%):");
            lblSimDuracion.Text = Tr("cdc.sim.duracion", "Duración (días):");
            lblSimRetencion.Text = Tr("cdc.sim.retencion", "Retención esperada (%):");
            lblSimHorizonte.Text = Tr("cdc.sim.horizonte", "Meses que paga un retenido:");
            btnSimAgregar.Text = Tr("cdc.sim.agregar", "Agregar a la comparación (máx. 3)");
            btnSimLimpiar.Text = Tr("cdc.sim.limpiar", "Limpiar la comparación");
            btnSimCrearSugerencia.Text = Tr("cdc.sim.crearsugerencia", "Crear sugerencia con este escenario");
            btnMedir.Text = Tr("cdc.btn.medir", "Medir");
            btnSimularOpcion.Text = Tr("cdc.btn.simularopcion", "Simular esta opción");
            TraducirCohortes();

            CargarPresets();
            CargarPlanes();
            CargarEstadosHistorial();
            foreach (var c in new Control[] { btnAnalizar, btnUmbrales, btnGlosario, btnInforme })
                tip.SetToolTip(c, null);
            tip.SetToolTip(btnUmbrales, Tr("cdc.tip.umbrales", "Valores que disparan cada recomendación (editables por Gerencia)."));
            tip.SetToolTip(btnInforme, Tr("cdc.tip.informe", "PDF con filtros, indicadores, gráficos, recomendaciones, decisiones, escenarios y glosario."));
        }

        private void CargarPresets()
        {
            int elegido = cmbPreset.SelectedIndex;
            cmbPreset.SelectedIndexChanged -= CmbPreset_SelectedIndexChanged;
            cmbPreset.Items.Clear();
            cmbPreset.Items.Add(Tr("cdc.preset.30", "Últimos 30 días"));
            cmbPreset.Items.Add(Tr("cdc.preset.90", "Últimos 90 días"));
            cmbPreset.Items.Add(Tr("cdc.preset.180", "Últimos 180 días"));
            cmbPreset.Items.Add(Tr("cdc.preset.estemes", "Este mes"));
            cmbPreset.Items.Add(Tr("cdc.preset.mesanterior", "Mes anterior"));
            cmbPreset.Items.Add(Tr("cdc.preset.personalizado", "Personalizado"));
            cmbPreset.SelectedIndex = elegido >= 0 ? elegido : (int)Preset.Personalizado;
            cmbPreset.SelectedIndexChanged += CmbPreset_SelectedIndexChanged;
        }

        private void CargarPlanes()
        {
            int? elegido = (cmbPlan.SelectedItem as Item<int?>)?.Valor;
            cmbPlan.Items.Clear();
            cmbPlan.Items.Add(new Item<int?> { Valor = null, Texto = Tr("cdc.todoslosplanes", "Todos los planes") });
            foreach (var p in _planes) cmbPlan.Items.Add(new Item<int?> { Valor = p.IdPlan, Texto = p.Nombre });
            cmbPlan.SelectedItem = cmbPlan.Items.Cast<Item<int?>>().FirstOrDefault(i => i.Valor == elegido) ?? cmbPlan.Items[0];
        }

        private void CargarEstadosHistorial()
        {
            int elegido = Math.Max(0, cmbHistEstado.SelectedIndex);
            cmbHistEstado.Items.Clear();
            cmbHistEstado.Items.Add(new Item<BE.EstadoDecision?> { Valor = null, Texto = Tr("cdc.hist.todos", "Todos") });
            foreach (BE.EstadoDecision e in Enum.GetValues(typeof(BE.EstadoDecision)))
                if (e != BE.EstadoDecision.Pendiente)
                    cmbHistEstado.Items.Add(new Item<BE.EstadoDecision?> { Valor = e, Texto = TextosComerciales.Estado(e) });
            cmbHistEstado.SelectedIndex = Math.Min(elegido, cmbHistEstado.Items.Count - 1);
        }

        // ══ Período ══════════════════════════════════════════════════════════════

        private void CmbPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPreset.SelectedIndex < 0 || cmbPreset.SelectedIndex == (int)Preset.Personalizado) return;
            DateTime hoy = DateTime.Today, desde, hasta = hoy;
            var mes = new DateTime(hoy.Year, hoy.Month, 1);
            switch ((Preset)cmbPreset.SelectedIndex)
            {
                case Preset.Ultimos30: desde = hoy.AddDays(-29); break;
                case Preset.Ultimos180: desde = hoy.AddDays(-179); break;
                case Preset.EsteMes: desde = mes; break;
                case Preset.MesAnterior: desde = mes.AddMonths(-1); hasta = mes.AddDays(-1); break;
                default: desde = hoy.AddDays(-89); break;
            }
            _cargando = true;
            try { dtpDesde.Value = desde; dtpHasta.Value = hasta; }
            finally { _cargando = false; }
            MostrarComparacion();
        }

        // Si el usuario cambia una fecha a mano, el período pasa a "Personalizado".
        private void DtpPeriodo_ValueChanged(object sender, EventArgs e)
        {
            if (_cargando) return;
            cmbPreset.SelectedIndexChanged -= CmbPreset_SelectedIndexChanged;
            cmbPreset.SelectedIndex = (int)Preset.Personalizado;
            cmbPreset.SelectedIndexChanged += CmbPreset_SelectedIndexChanged;
            MostrarComparacion();
        }

        private void MostrarComparacion()
        {
            DateTime desde = dtpDesde.Value.Date, hasta = dtpHasta.Value.Date;
            int dias = Math.Max(1, (int)(hasta - desde).TotalDays + 1);
            lblComparacion.Text = desde > hasta
                ? Tr("err.bll.promocion.rango_fechas_invalido", "La fecha de fin no puede ser anterior a la fecha de inicio.")
                : Tr("cdc.comparacion", "{0} día(s). Se compara con el período anterior de igual duración: {1:d} al {2:d}.",
                     new object[] { dias, desde.AddDays(-dias), desde.AddDays(-1) });
        }

        // ══ Ejecución en segundo plano ═══════════════════════════════════════════

        private async Task<T> EjecutarConEsperaAsync<T>(Func<T> operacion)
        {
            Cursor = Cursors.WaitCursor;
            pnlEncabezado.Enabled = tabs.Enabled = false;
            try { return await Task.Run(operacion); }
            finally
            {
                pnlEncabezado.Enabled = tabs.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        // ══ 1. Analizar ══════════════════════════════════════════════════════════

        private async void BtnAnalizar_Click(object sender, EventArgs e)
        {
            DateTime desde = dtpDesde.Value.Date, hasta = dtpHasta.Value.Date;
            DateTime medirDesde = dtpMedirDesde.Value.Date, medirHasta = dtpMedirHasta.Value.Date;
            int? idPlan = (cmbPlan.SelectedItem as Item<int?>)?.Valor;
            try
            {
                BE.MatrizCohortes cohortes = null;
                var (panel, motor, umbrales, mediciones) = await EjecutarConEsperaAsync(() =>
                {
                    var p = bll.Analizar(Modulo, desde, hasta, idPlan);
                    cohortes = LeerCohortes(hasta, idPlan);   // pestaña Cohortes (si falla, avisa ahí)
                    // La pestaña Medir resultado también queda lista (si falla, el análisis sigue).
                    List<BE.MedicionPromocion> m;
                    try { m = bll.MedirPromociones(medirDesde, medirHasta); }
                    catch (Exception ex) { System.Diagnostics.Trace.TraceError("[CentroDecisiones] " + ex.Message); m = new List<BE.MedicionPromocion>(); }
                    return (p, bll.Recomendar(p), bll.Umbrales.ObtenerValores(), m);
                });
                if (IsDisposed) return;
                _mediciones = mediciones;
                _cohortes = cohortes;
                _panel = panel;
                _recs = motor.Recomendaciones;
                _reglasConError = motor.ReglasConError;
                _umbrales = umbrales;
                RedibujarTodo();
                PrepararSimulador();
                MostrarOk(Tr("cdc.msg.analizado", "Análisis listo: {0} recomendación(es) para decidir.",
                    new object[] { _recs.Count(r => BLLI.DecisionesComerciales.DebeMostrarse(r, DateTime.Today, DiasSilencio())) }));
                await CargarHistorialAsync(false);
            }
            catch (Exception ex) { if (!IsDisposed) MostrarError(ex); }
        }

        private int DiasSilencio() =>
            (int)(_umbrales.TryGetValue(BLLI.CatalogoUmbrales.DiasSilencioDescarte, out var d) ? d : 30m);

        // Inicio del período analizado: una decisión ejecutada antes de esa fecha ya no rige (situación nueva).
        private DateTime InicioAnalisis() => _panel?.Desde ?? DateTime.Today;

        // ── Grillas enlazadas a un DataTable ──
        // El usuario puede ordenar por cualquier encabezado: eso reordena la VISTA del DataTable, no sus
        // filas. Por eso el ítem se resuelve por la fila de origen (DataBoundItem), nunca por la posición
        // en pantalla; las filas del DataTable se cargan en el mismo orden que la lista que representan.
        private static int IndiceFuente(DataGridViewRow fila)
        {
            var drv = fila?.DataBoundItem as DataRowView;
            return drv != null ? drv.Row.Table.Rows.IndexOf(drv.Row) : -1;
        }

        private static int IndiceSeleccionado(DataGridView dgv) =>
            dgv.SelectedRows.Count > 0 ? IndiceFuente(dgv.SelectedRows[0]) : -1;

        private static void SeleccionarFuente(DataGridView dgv, int indice)
        {
            dgv.ClearSelection();
            var fila = dgv.Rows.Cast<DataGridViewRow>().FirstOrDefault(f => IndiceFuente(f) == indice)
                       ?? dgv.Rows.Cast<DataGridViewRow>().FirstOrDefault();
            if (fila != null) fila.Selected = true;
        }

        private void DibujarVacio()
        {
            string sin = Tr("cdc.grafico.analice", "Elija el período y pulse Analizar.");
            GraficosComerciales.SinDatos(chartTendencia, Tr("cdc.grafico.tendencia", "Tendencia de 6 meses y proyección de 3"), sin);
            GraficosComerciales.SinDatos(chartRiesgo, Tr("cdc.grafico.riesgo", "Ingreso mensual en riesgo por plan"), sin);
            GraficosComerciales.SinDatos(chartStock, Tr("cdc.grafico.stock", "Stock por categoría"), sin);
            GraficosComerciales.SinDatos(chartRotacion, Tr("cdc.grafico.rotacion", "Rotación: más y menos pedidas"), sin);
            GraficosComerciales.Sensibilidad(chartSensibilidad, null, null);
            GraficosComerciales.Medicion(chartMedicion, null);
        }

        private void RedibujarTodo()
        {
            MostrarComparacion();
            if (_panel != null)
            {
                MostrarPanel();
                MostrarRecomendaciones();
                MostrarCohortes();
            }
            MostrarHistorial();
            RecalcularSimulacion();
            MostrarMediciones();
        }

        private void MostrarPanel()
        {
            flpKpis.SuspendLayout();
            foreach (Control c in flpKpis.Controls.Cast<Control>().ToList()) c.Dispose();
            flpKpis.Controls.Clear();
            foreach (var k in _panel.Kpis) flpKpis.Controls.Add(new TarjetaKpi(k, tip));
            flpKpis.ResumeLayout();

            var avisos = _panel.Avisos.Select(TextosComerciales.R).ToList();
            lblAvisos.Text = avisos.Count > 0 ? "⚠ " + string.Join("  ·  ", avisos) : string.Empty;

            GraficosComerciales.Tendencia(chartTendencia, _panel.Tendencia, _panel.R2Ingresos);
            GraficosComerciales.RiesgoPorPlan(chartRiesgo, _panel.RiesgoPorPlan);
            GraficosComerciales.Stock(chartStock, _panel.StockPorCategoria);
            GraficosComerciales.Rotacion(chartRotacion, _panel.MasPedidas, _panel.MenosPedidas);
        }

        // ══ 2. Recomendaciones ═══════════════════════════════════════════════════

        private void ChkMostrarOcultas_CheckedChanged(object sender, EventArgs e) => MostrarRecomendaciones();

        private void MostrarRecomendaciones()
        {
            var seleccionada = RecomendacionSeleccionada()?.Clave;
            _recsVisibles = chkMostrarOcultas.Checked
                ? _recs.ToList()
                : _recs.Where(r => BLLI.DecisionesComerciales.DebeMostrarse(r, DateTime.Today, DiasSilencio())).ToList();

            var t = new DataTable();
            t.Columns.Add("Prioridad", typeof(string));
            t.Columns.Add("Recomendacion", typeof(string));
            t.Columns.Add("Impacto", typeof(decimal));
            t.Columns.Add("Confianza", typeof(string));
            t.Columns.Add("Estado", typeof(string));
            foreach (var r in _recsVisibles)
                t.Rows.Add(TextosComerciales.Prioridad(r.Prioridad), TextosComerciales.R(r.Titulo), r.ImpactoEstimado,
                           TextosComerciales.Confianza(r.Confianza),
                           TextosComerciales.EstadoRecomendacion(r,
                               BLLI.DecisionesComerciales.DecisionVigente(r.UltimaDecision, DateTime.Today, DiasSilencio(), InicioAnalisis())));
            dgvRecomendaciones.DataSource = t;
            if (dgvRecomendaciones.Columns.Count == 5)
            {
                dgvRecomendaciones.Columns[0].HeaderText = Tr("cdc.col.prioridad", "Prioridad");
                dgvRecomendaciones.Columns[1].HeaderText = Tr("cdc.col.recomendacion", "Recomendación");
                dgvRecomendaciones.Columns[2].HeaderText = Tr("cdc.col.impacto", "Impacto estimado");
                dgvRecomendaciones.Columns[3].HeaderText = Tr("cdc.col.confianza", "Confianza");
                dgvRecomendaciones.Columns[4].HeaderText = Tr("cdc.col.estado", "Estado");
                dgvRecomendaciones.Columns[0].FillWeight = 14;
                dgvRecomendaciones.Columns[1].FillWeight = 60;
                dgvRecomendaciones.Columns[2].FillWeight = 22;
                dgvRecomendaciones.Columns[2].DefaultCellStyle.Format = "C0";
                dgvRecomendaciones.Columns[3].FillWeight = 14;
                dgvRecomendaciones.Columns[4].FillWeight = 22;
            }

            int ocultas = _recs.Count - _recs.Count(r => BLLI.DecisionesComerciales.DebeMostrarse(r, DateTime.Today, DiasSilencio()));
            lblRecsResumen.Text = _recs.Count == 0
                ? Tr("cdc.recs.ninguna", "El motor no encontró situaciones que requieran una decisión con los umbrales actuales.")
                : Tr("cdc.recs.resumen", "{0} recomendación(es): {1} de prioridad alta · impacto total estimado {2:C0} · {3} oculta(s) por decisiones anteriores",
                     new object[] { _recs.Count, _recs.Count(r => r.Prioridad == BE.PrioridadRecomendacion.Alta),
                                    _recs.Where(r => r.ImpactoEstimado > 0).Sum(r => r.ImpactoEstimado), ocultas });
            if (_reglasConError.Count > 0)
                lblRecsResumen.Text += "  ⚠ " + Tr("cdc.recs.errores", "Reglas que no se pudieron evaluar: {0}",
                    new object[] { string.Join(", ", _reglasConError.Select(TextosComerciales.Regla)) });

            int indice = _recsVisibles.FindIndex(r => r.Clave == seleccionada);
            SeleccionarFuente(dgvRecomendaciones, indice >= 0 ? indice : 0);
            MostrarDetalle(RecomendacionSeleccionada());
        }

        private void DgvRecomendaciones_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
            int i = IndiceFuente(dgvRecomendaciones.Rows[e.RowIndex]);
            if (i < 0 || i >= _recsVisibles.Count) return;
            switch (_recsVisibles[i].Prioridad)
            {
                case BE.PrioridadRecomendacion.Alta: e.CellStyle.BackColor = Tema.FondoError; e.CellStyle.ForeColor = Tema.Error; break;
                case BE.PrioridadRecomendacion.Media: e.CellStyle.BackColor = Tema.FondoAlerta; e.CellStyle.ForeColor = Tema.Alerta; break;
                default: e.CellStyle.BackColor = Tema.FondoInfo; e.CellStyle.ForeColor = Tema.Info; break;
            }
        }

        private BE.Recomendacion RecomendacionSeleccionada()
        {
            int i = IndiceSeleccionado(dgvRecomendaciones);
            return i >= 0 && i < _recsVisibles.Count ? _recsVisibles[i] : null;
        }

        private void DgvRecomendaciones_SelectionChanged(object sender, EventArgs e) => MostrarDetalle(RecomendacionSeleccionada());

        // Detalle: qué pasa, por qué, qué hacer (con fórmula), confianza, detalle y la última decisión.
        private void MostrarDetalle(BE.Recomendacion r)
        {
            txtDetalle.Clear();
            var opciones = new DataTable();
            opciones.Columns.Add("Opcion", typeof(string));
            opciones.Columns.Add("Descripcion", typeof(string));
            opciones.Columns.Add("Impacto", typeof(decimal));
            opciones.Columns.Add("Formula", typeof(string));
            opciones.Columns.Add("Boton", typeof(string));
            bool hay = r != null;
            // La regla es de la BLL: una decisión final (descartada o ejecutada) bloquea solo mientras rige.
            btnAceptar.Enabled = btnDescartar.Enabled = btnPosponer.Enabled = hay
                && BLLI.DecisionesComerciales.PuedeDecidirse(r, DateTime.Today, DiasSilencio(), InicioAnalisis());
            if (!hay)
            {
                dgvOpciones.DataSource = opciones;
                ActualizarSimularOpcion();
                return;
            }

            void Seccion(string titulo, string texto, Color? color = null)
            {
                txtDetalle.SelectionFont = Tema.FuenteSubtitulo;
                txtDetalle.SelectionColor = Tema.RosaOscuro;
                txtDetalle.AppendText(titulo + Environment.NewLine);
                txtDetalle.SelectionFont = Tema.FuenteNormal;
                txtDetalle.SelectionColor = color ?? Tema.Tinta;
                txtDetalle.AppendText(texto + Environment.NewLine + Environment.NewLine);
            }
            txtDetalle.SelectionFont = Tema.FuenteTitulo;
            txtDetalle.SelectionColor = Tema.Tinta;
            txtDetalle.AppendText(TextosComerciales.R(r.Titulo) + Environment.NewLine);
            txtDetalle.SelectionFont = Tema.FuentePequena;
            txtDetalle.SelectionColor = Tema.TextoMuted;
            txtDetalle.AppendText(Tr("cdc.det.cabecera", "Prioridad {0} · Confianza {1} · Regla: {2}",
                new object[] { TextosComerciales.Prioridad(r.Prioridad), TextosComerciales.Confianza(r.Confianza), TextosComerciales.Regla(r.TipoRegla) })
                + Environment.NewLine + Environment.NewLine);
            if (r.PrioridadElevada != null)
                Seccion(Tr("cdc.det.metas", "Metas del mes"), TextosComerciales.R(r.PrioridadElevada), Tema.Error);
            Seccion(Tr("cdc.det.diagnostico", "Qué está pasando"), TextosComerciales.R(r.Diagnostico));
            Seccion(Tr("cdc.det.motivo", "Por qué se recomienda"), TextosComerciales.R(r.Motivo));
            Seccion(Tr("cdc.det.accion", "Qué hacer"), TextosComerciales.R(r.Principal.Descripcion));
            Seccion(Tr("cdc.det.impacto", "Impacto estimado"), r.ImpactoEstimado.ToString("C2") + "  =  " + TextosComerciales.R(r.Principal.Formula),
                    r.ImpactoEstimado >= 0 ? Tema.Exito : Tema.Error);
            if (r.Detalle.Count > 0)
                Seccion(Tr("cdc.det.detalle", "Detalle"), string.Join(Environment.NewLine, r.Detalle.Take(30))
                    + (r.Detalle.Count > 30 ? Environment.NewLine + "…" : ""));
            if (r.UltimaDecision != null)
            {
                var d = r.UltimaDecision;
                Seccion(Tr("cdc.det.decision", "Última decisión"),
                    $"#{d.IdDecision} · {d.Fecha:g} · {d.NombreUsuario} · {TextosComerciales.Estado(d.Estado)}" +
                    (d.PospuestaHasta.HasValue ? $" ({d.PospuestaHasta:d})" : "") +
                    (string.IsNullOrEmpty(d.AccionElegida) ? "" : " · " + d.AccionElegida) +
                    (string.IsNullOrEmpty(d.Motivo) ? "" : " · " + d.Motivo) +
                    (_medidos.TryGetValue(d.IdDecision, out var medicion) && medicion.Estado != BE.EstadoMedicion.SinDato
                        ? " · " + Tr("cdc.col.medido", "Medido por el sistema") + ": " + TextosComerciales.Medicion(medicion) : "") +
                    (d.EsFinal() && !BLLI.DecisionesComerciales.DecisionVigente(d, DateTime.Today, DiasSilencio(), InicioAnalisis())
                        ? Environment.NewLine + Tr("cdc.det.decisionnorige", "Esa decisión ya no rige para la situación actual: puede volver a decidir.")
                        : ""));
            }
            txtDetalle.SelectionStart = 0;
            txtDetalle.ScrollToCaret();

            var lista = r.Opciones();
            for (int i = 0; i < lista.Count; i++)
                opciones.Rows.Add(i == 0 ? "★ " + Tr("cdc.opcion.recomendada", "Recomendada") : Tr("cdc.opcion.alternativa", "Alternativa {0}", new object[] { i }),
                    TextosComerciales.R(lista[i].Descripcion), lista[i].ImpactoEstimado, TextosComerciales.R(lista[i].Formula),
                    TextosComerciales.Accion(lista[i].Accion));
            dgvOpciones.DataSource = opciones;
            if (dgvOpciones.Columns.Count == 5)
            {
                dgvOpciones.Columns[0].HeaderText = Tr("cdc.col.opcion", "Opción");
                dgvOpciones.Columns[1].HeaderText = Tr("cdc.col.propuesta", "Acción propuesta");
                dgvOpciones.Columns[2].HeaderText = Tr("cdc.col.impacto", "Impacto estimado");
                dgvOpciones.Columns[3].HeaderText = Tr("cdc.col.formula", "Fórmula y fuente");
                dgvOpciones.Columns[4].HeaderText = Tr("cdc.col.boton", "Al aceptar");
                dgvOpciones.Columns[2].DefaultCellStyle.Format = "C0";
                dgvOpciones.Columns[0].FillWeight = 16;
                dgvOpciones.Columns[1].FillWeight = 40;
                dgvOpciones.Columns[2].FillWeight = 16;
                dgvOpciones.Columns[3].FillWeight = 50;
                dgvOpciones.Columns[4].FillWeight = 24;
            }
            SeleccionarFuente(dgvOpciones, 0);
            ActualizarSimularOpcion();
        }

        private int OpcionSeleccionada() => Math.Max(0, IndiceSeleccionado(dgvOpciones));

        private BE.OpcionRecomendacion OpcionElegida()
        {
            var lista = RecomendacionSeleccionada()?.Opciones();
            int i = OpcionSeleccionada();
            return lista != null && i < lista.Count ? lista[i] : null;
        }

        private void DgvOpciones_SelectionChanged(object sender, EventArgs e) => ActualizarSimularOpcion();

        // "Simular esta opción": solo para opciones de promoción (plan, %, días y retención). La regla es de la BLL.
        private void ActualizarSimularOpcion() =>
            btnSimularOpcion.Enabled = BLLI.InteligenciaComercial.EsSimulable(OpcionElegida());

        // Lleva la opción al simulador con su plan, %, días, retención y clientes objetivo, y simula.
        private void BtnSimularOpcion_Click(object sender, EventArgs e)
        {
            var escenario = BLLI.InteligenciaComercial.EscenarioDesdeOpcion(_panel, OpcionElegida());
            var item = escenario == null ? null
                : cmbSimPlan.Items.Cast<Item<int?>>().FirstOrDefault(x => x.Valor == escenario.IdPlan);
            if (item == null)
            {
                MostrarError(Tr("cdc.sim.opcionnosimulable", "Esta opción no se puede simular: elija una opción de promoción de un plan del análisis."));
                return;
            }
            cmbSimPlan.SelectedItem = item;   // carga el escenario base del plan; abajo se pisa con la opción
            _cargando = true;
            try
            {
                Asignar(numSimClientesPlan, escenario.ClientesPlan);
                Asignar(numSimObjetivo, escenario.ClientesObjetivo);
                Asignar(numSimDescuento, escenario.DescuentoPorcentaje);
                Asignar(numSimDuracion, escenario.DuracionDias);
                Asignar(numSimRetencion, escenario.TasaRetencion);
                Asignar(numSimHorizonte, escenario.HorizonteMeses);
            }
            finally { _cargando = false; }
            RecalcularSimulacion();
            tabs.SelectedTab = tabSimulador;
            MostrarOk(Tr("cdc.msg.simulandoopcion", "Simulador cargado con la opción elegida: ajuste los supuestos para ver cómo cambia el resultado."));
        }

        private void BtnAceptar_Click(object sender, EventArgs e)
        {
            var rec = RecomendacionSeleccionada();
            if (rec == null || Decisiones == null) return;
            int indice = OpcionSeleccionada();
            var opcion = rec.Opciones()[indice];
            // Antes de registrar la aceptación: permiso para ejecutarla y, si toca prendas, la confirmación
            // con la lista. Así no queda una decisión aceptada que nadie puede ejecutar.
            if (!PuedeEjecutar(opcion)) return;
            BLLI.PlanAccionPrendas plan = null;
            if (BLLI.AccionesRecomendacion.TocaPrendas(opcion.Accion))
            {
                plan = PlanificarPrendas(opcion);
                if (plan == null) return;
            }
            try
            {
                var decision = Decisiones.Aceptar(Modulo, rec, indice);
                Ejecutar(rec, opcion, decision, plan);
            }
            catch (Exception ex) { MostrarError(ex); }
            RefrescarDecisiones();
        }

        private void BtnDescartar_Click(object sender, EventArgs e)
        {
            var rec = RecomendacionSeleccionada();
            if (rec == null || Decisiones == null) return;
            string motivo = PedirTexto(Tr("cdc.btn.descartar", "Descartar"),
                Tr("cdc.descartar.motivo", "¿Por qué descarta la recomendación? (obligatorio)"));
            if (motivo == null) return;
            try
            {
                var d = Decisiones.Descartar(Modulo, rec, motivo);
                MostrarOk(Tr("cdc.msg.descartada", "Recomendación descartada (decisión #{0}).", new object[] { d.IdDecision }));
            }
            catch (Exception ex) { MostrarError(ex); }
            RefrescarDecisiones();
        }

        private void BtnPosponer_Click(object sender, EventArgs e)
        {
            var rec = RecomendacionSeleccionada();
            if (rec == null || Decisiones == null) return;
            try
            {
                var d = Decisiones.Posponer(Modulo, rec, (int)numDiasPosponer.Value, null);
                MostrarOk(Tr("cdc.msg.pospuesta", "Recomendación pospuesta hasta el {0:d}.", new object[] { d.PospuestaHasta }));
            }
            catch (Exception ex) { MostrarError(ex); }
            RefrescarDecisiones();
        }

        private void RefrescarDecisiones()
        {
            try { Decisiones?.AsociarDecisiones(_recs); }
            catch (Exception ex) { MostrarError(ex); }
            MostrarRecomendaciones();
            _ = CargarHistorialAsync(false);
        }

        // ── Ejecución de la opción aceptada: cada acción hace algo con una operación existente ──
        // (la regla de qué patente pide cada acción y a qué estado van las prendas es de la BLL).
        private bool PuedeEjecutar(BE.OpcionRecomendacion opcion)
        {
            if (BLLI.AccionesRecomendacion.PuedeEjecutar(opcion, BLL.PermisosAccion.Puede)) return true;
            MostrarError(Tr("cdc.msg.sinpermisoaccion",
                "No tiene permiso para ejecutar esta opción. Elija otra opción o pida el permiso al Administrador (Perfiles y Permisos)."));
            return false;
        }

        private void Ejecutar(BE.Recomendacion rec, BE.OpcionRecomendacion opcion, BE.DecisionComercial decision, BLLI.PlanAccionPrendas plan = null)
        {
            var p = opcion.Parametros ?? new BE.ParametrosAccion();
            switch (opcion.Accion)
            {
                case BE.AccionRecomendacion.SugerirPromocion:
                case BE.AccionRecomendacion.ExtenderPromocion:
                {
                    string motivo = $"{TextosComerciales.R(rec.Titulo)}. {TextosComerciales.R(opcion.Descripcion)}. {TextosComerciales.R(rec.Diagnostico)}";
                    int? id = AbrirSugerencia(p.IdPlan, p.IngresoEnRiesgo, p.TasaRetencion, motivo, BE.OrigenMetrica.Abandono);
                    if (id.HasValue)
                    {
                        Decisiones.MarcarEjecutada(Modulo, decision.IdDecision, BE.DecisionComercial.RefSugerencia, id);
                        MostrarOk(Tr("cdc.msg.sugerencia", "Decisión #{0} ejecutada: sugerencia #{1} enviada a Administración (sigue a Contabilidad).",
                            new object[] { decision.IdDecision, id }));
                    }
                    else MostrarAceptadaSinEjecutar(decision);
                    break;
                }
                case BE.AccionRecomendacion.ExportarContactos:
                    if (ExportarContactos(p.Contactos))
                    {
                        // Guarda a quiénes se contactó: el historial mide después cuántos renovaron.
                        Decisiones.MarcarEjecutadaConDetalle(Modulo, decision.IdDecision, BE.DecisionComercial.DetalleCliente, p.Contactos.Select(c => c.IdCliente));
                        MostrarOk(Tr("cdc.msg.contactos", "Decisión #{0} ejecutada: {1} contacto(s) exportado(s). El historial medirá cuántos renuevan o cambian de plan.",
                            new object[] { decision.IdDecision, p.Contactos.Count }));
                    }
                    else MostrarAceptadaSinEjecutar(decision);
                    break;
                case BE.AccionRecomendacion.OrdenCompra:
                    if (new Exportacion.GeneradorInformeComercial().Exportar("pdf", InformeEjecutivo.OrdenCompra(p.Lineas, rec), this) != null)
                    {
                        Decisiones.MarcarEjecutada(Modulo, decision.IdDecision, null, null);
                        MostrarOk(Tr("cdc.msg.ejecutada", "Decisión #{0} ejecutada.", new object[] { decision.IdDecision }));
                    }
                    else MostrarAceptadaSinEjecutar(decision);
                    break;
                case BE.AccionRecomendacion.DarDeBajaPrendas:
                case BE.AccionRecomendacion.EnviarAMantenimiento:
                    EjecutarPrendas(decision, plan);
                    break;
                case BE.AccionRecomendacion.SolicitarBajaPromocion:
                    SolicitarBaja(rec, p.IdPromocion, decision);
                    break;
                default:
                    // Acción que esta versión ya no tiene: no queda una aceptación muda; se cierra con el motivo.
                    try { Decisiones.DescartarDecision(Modulo, decision.IdDecision, "Acción no disponible en esta versión."); }
                    catch (Exception ex) { System.Diagnostics.Trace.TraceError("[CentroDecisiones] " + ex.Message); }
                    MostrarError(Tr("cdc.msg.accionnodisponible", "La acción de esta opción ya no está disponible: la decisión #{0} se descartó. Vuelva a analizar.",
                        new object[] { decision.IdDecision }));
                    break;
            }
        }

        private void MostrarAceptadaSinEjecutar(BE.DecisionComercial decision) =>
            MostrarOk(Tr("cdc.msg.aceptadasinejecutar", "Decisión #{0} aceptada. Puede ejecutarla más tarde desde esta pantalla.", new object[] { decision.IdDecision }));

        // Abre "Sugerir promoción" precargada; devuelve el ID de la sugerencia registrada (o null).
        private int? AbrirSugerencia(int? idPlan, decimal ingresoEnRiesgo, decimal tasa, string motivo, BE.OrigenMetrica origen)
        {
            if (!BLL.Sesion.TienePermiso(BE.Patentes.SugerenciaPromocion))
            {
                MostrarError(Tr("cdc.msg.sinpermisosugerir", "No tiene permiso para sugerir promociones."));
                return null;
            }
            using (var form = new SugerirPromocionForm())
            {
                form.Precargar(idPlan, BE.TipoDescuento.Porcentaje, ingresoEnRiesgo, tasa, motivo, origen);
                form.ShowDialog(this);
                return form.UltimaSugerenciaRegistrada;
            }
        }

        private bool ExportarContactos(List<BE.ClienteVencimiento> contactos)
        {
            if (contactos == null || contactos.Count == 0)
            {
                MostrarError(Tr("err.pdf.sinDatos", "No hay datos para exportar."));
                return false;
            }
            return new Exportacion.GeneradorInformeComercial().Exportar("csv",
                InformeEjecutivo.Contactos(contactos, Tr("cdc.contactos.titulo", "Contactos para la campaña")), this) != null;
        }

        // Prendas: la BLL relee el catálogo y dice cuáles admiten hoy el cambio; acá solo se confirma la lista.
        private BLLI.PlanAccionPrendas PlanificarPrendas(BE.OpcionRecomendacion opcion)
        {
            BLLI.PlanAccionPrendas plan;
            try { plan = new BLLI.AccionesPrendas().Planificar(opcion.Accion, opcion.Parametros?.Ids); }
            catch (Exception ex) { MostrarError(ex); return null; }
            if (plan.Aplicables.Count == 0)
            {
                MostrarError(Tr("cdc.prendas.ninguna",
                    "Ninguna de las prendas de la recomendación admite hoy ese cambio (están en uso, ya de baja o pendientes de inspección). Vuelva a analizar."));
                return null;
            }
            var lineas = plan.Aplicables.Take(15).Select(x => $"  • #{x.IdPrenda} {x.Nombre} ({x.Categoria} {x.Talle})").ToList();
            if (plan.Aplicables.Count > 15) lineas.Add("  …");
            string texto = Tr("cdc.prendas.confirmar", "¿Pasar estas {0} prenda(s) a «{1}»?", new object[] { plan.Aplicables.Count, DestinoPrendas(plan) })
                           + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, lineas);
            int omitidas = plan.NoAplicables.Count + plan.NoEncontradas.Count;
            if (omitidas > 0)
                texto += Environment.NewLine + Environment.NewLine + Tr("cdc.prendas.omitidas",
                    "Se omiten {0} prenda(s) que hoy no admiten el cambio (en uso, ya de baja o pendientes de inspección).", new object[] { omitidas });
            return ConfirmarSiNo(texto, Tr("cdc.btn.aceptar", "Aceptar y ejecutar")) ? plan : null;
        }

        private string DestinoPrendas(BLLI.PlanAccionPrendas plan) =>
            plan.Destino == BE.EstadoPrenda.Baja ? Tr("cdc.prendas.destino.baja", "Baja") : Tr("cdc.prendas.destino.limpieza", "En limpieza");

        // La BLL aplica prenda por prenda y, pase lo que pase, deja la decisión Ejecutada con lo que sí cambió.
        private void EjecutarPrendas(BE.DecisionComercial decision, BLLI.PlanAccionPrendas plan)
        {
            if (plan == null) { MostrarAceptadaSinEjecutar(decision); return; }
            var r = new BLLI.AccionesPrendas().EjecutarDecision(Modulo, plan, decision.IdDecision, Decisiones);
            if (r.Aplicadas.Count > 0 && !r.Registrada)
            {
                MostrarError(Tr("cdc.msg.prendassinregistrar",
                    "Se cambiaron {0} prenda(s) ({1}), pero la decisión #{2} no se pudo marcar como ejecutada ({3}). Los cambios de las prendas quedaron en la bitácora.",
                    new object[] { r.Aplicadas.Count, string.Join(", ", r.Aplicadas.Select(id => "#" + id)), decision.IdDecision, MensajeDeError(r.ErrorRegistro) }));
                return;
            }
            if (r.Interrupcion != null && r.Aplicadas.Count == 0) { MostrarError(r.Interrupcion); return; }
            if (r.Fallidas.Count == 0)
                MostrarOk(Tr("cdc.msg.prendas", "Decisión #{0} ejecutada: {1} prenda(s) pasaron a «{2}».",
                    new object[] { decision.IdDecision, r.Aplicadas.Count, DestinoPrendas(plan) }));
            else if (r.Aplicadas.Count > 0)
                MostrarError(Tr("cdc.msg.prendasparcial", "Decisión #{0} ejecutada: {1} prenda(s) cambiaron y {2} no ({3}).",
                    new object[] { decision.IdDecision, r.Aplicadas.Count, r.Fallidas.Count, MensajeDeError(r.Fallidas[0].Error) }));
            else
                MostrarError(Tr("cdc.msg.prendasninguna", "No se pudo cambiar ninguna prenda ({0}). La decisión #{1} quedó aceptada: puede descartarla en el historial.",
                    new object[] { MensajeDeError(r.Fallidas[0].Error), decision.IdDecision }));
        }

        private void SolicitarBaja(BE.Recomendacion rec, int? idPromocion, BE.DecisionComercial decision)
        {
            var promocionBLL = new BLL.Promocion();
            var promo = idPromocion.HasValue ? promocionBLL.ObtenerParaVentas().FirstOrDefault(x => x.IdPromocion == idPromocion.Value) : null;
            if (promo == null)
            {
                MostrarError(Tr("cdc.msg.promonovigente", "La promoción ya no está vigente: actualice el análisis."));
                return;
            }
            if (!ConfirmarSiNo(Tr("cdc.baja.confirmar", "¿Registrar la solicitud de baja de la promoción \"{0}\"? Administración la atiende y decide la baja.",
                    new object[] { promo.Nombre }), Tr("cdc.btn.aceptar", "Aceptar y ejecutar")))
                return;
            string motivo = $"{TextosComerciales.R(rec.Titulo)}: {TextosComerciales.R(rec.Motivo)}";
            if (motivo.Length > 480) motivo = motivo.Substring(0, 479) + "…";
            int idSolicitud = promocionBLL.SolicitarBaja(Modulo, promo, motivo);
            Decisiones.MarcarEjecutada(Modulo, decision.IdDecision, BE.DecisionComercial.RefSolicitudBaja, idSolicitud);
            MostrarOk(Tr("cdc.msg.bajasolicitada", "Decisión #{0} ejecutada: solicitud de baja #{1} enviada a Administración.",
                new object[] { decision.IdDecision, idSolicitud }));
        }

        private void BtnRecsCsv_Click(object sender, EventArgs e)
        {
            var t = InformeEjecutivo.TablaRecomendaciones(_recsVisibles, out var enc);
            new Exportacion.GeneradorInformeComercial().Exportar("csv", new Exportacion.ReporteExportable
            {
                Titulo = Tr("cdc.informe.recomendaciones", "Recomendaciones del motor"),
                NombreArchivo = $"Recomendaciones_{DateTime.Today:yyyyMMdd}", Datos = t, Encabezados = enc
            }, this);
        }

        // ══ 3. Historial de decisiones ═══════════════════════════════════════════

        private async void BtnHistFiltrar_Click(object sender, EventArgs e) => await CargarHistorialAsync(true);

        private async Task CargarHistorialAsync(bool conEspera)
        {
            if (Decisiones == null) return;
            DateTime desde = dtpHistDesde.Value.Date, hasta = dtpHistHasta.Value.Date;
            try
            {
                Func<(List<BE.DecisionComercial>, Dictionary<int, BE.MedicionDecision>)> leer = () =>
                {
                    var lista = Decisiones.Obtener(desde, hasta);
                    return (lista, bll.MedirDecisiones(lista));
                };
                var (lista2, medidos) = conEspera ? await EjecutarConEsperaAsync(leer) : await Task.Run(leer);
                if (IsDisposed) return;
                _historial = lista2;
                _medidos = medidos;
                MostrarHistorial();
            }
            catch (Exception ex) { if (!IsDisposed) MostrarError(ex); }
        }

        private List<BE.DecisionComercial> HistorialFiltrado()
        {
            var estado = (cmbHistEstado.SelectedItem as Item<BE.EstadoDecision?>)?.Valor;
            return _historial.Where(d => !estado.HasValue || d.Estado == estado.Value).ToList();
        }

        private void MostrarHistorial()
        {
            var lista = HistorialFiltrado();
            var t = InformeEjecutivo.TablaDecisiones(lista, _medidos, out var enc);
            dgvHistorial.DataSource = t;
            for (int i = 0; i < enc.Length && i < dgvHistorial.Columns.Count; i++) dgvHistorial.Columns[i].HeaderText = enc[i];
            if (dgvHistorial.Columns.Count > 0) dgvHistorial.Columns[0].FillWeight = 20;

            // Los totales los arma la BLL: las campañas en medición no se suman (el texto lo aclara).
            lblHistResumen.Text = TextosComerciales.ResumenHistorial(BLLI.MedicionDecisiones.Resumir(lista, _medidos));
            DgvHistorial_SelectionChanged(this, EventArgs.Empty);
        }

        private BE.DecisionComercial DecisionSeleccionada()
        {
            int i = IndiceSeleccionado(dgvHistorial);
            var lista = HistorialFiltrado();
            return i >= 0 && i < lista.Count ? lista[i] : null;
        }

        private void DgvHistorial_SelectionChanged(object sender, EventArgs e)
        {
            var d = DecisionSeleccionada();
            btnRegistrarResultado.Enabled = d != null && d.AdmiteResultado();
            btnHistDescartar.Enabled = d != null && d.TransicionValida(BE.EstadoDecision.Descartada);
        }

        private void BtnRegistrarResultado_Click(object sender, EventArgs e)
        {
            var d = DecisionSeleccionada();
            if (d == null) return;
            var medicion = _medidos.TryGetValue(d.IdDecision, out var m) ? m : null;
            // Solo un resultado final se propone como valor; una campaña en medición muestra su avance, sin importe.
            decimal? medido = medicion?.Estado == BE.EstadoMedicion.Medido ? medicion.Valor : null;
            string nota = medido.HasValue ? " " + Tr("cdc.resultado.medido", "El sistema midió {0:C0}.", new object[] { medido.Value })
                        : medicion?.Estado == BE.EstadoMedicion.EnMedicion
                            ? " " + Tr("cdc.resultado.enmedicion", "{0}; el sistema calcula el importe al cierre.", new object[] { TextosComerciales.Medicion(medicion) })
                            : "";
            string valor = PedirTexto(Tr("cdc.btn.resultado", "Registrar resultado real"),
                Tr("cdc.resultado.pedir", "Resultado real en $ de la decisión #{0} (estimado {1:C0}).{2}",
                   new object[] { d.IdDecision, d.ImpactoEstimado, nota }),
                (medido ?? d.ResultadoReal ?? 0m).ToString("0.##"));
            if (valor == null) return;
            // Coma decimal y punto solo para miles: "21600.50" se rechaza (no se lee como 2.160.050).
            if (!EntradaNumerica.TryLeer(valor, Tr("cdc.resultado.invalido", "Ingrese un importe válido."), out var resultado, out var error))
            {
                MostrarError(error);
                return;
            }
            try
            {
                Decisiones.RegistrarResultado(Modulo, d.IdDecision, resultado);
                MostrarOk(Tr("cdc.msg.resultado", "Resultado real registrado en la decisión #{0}.", new object[] { d.IdDecision }));
            }
            catch (Exception ex) { MostrarError(ex); }
            _ = CargarHistorialAsync(false);
        }

        private void BtnHistDescartar_Click(object sender, EventArgs e)
        {
            var d = DecisionSeleccionada();
            if (d == null) return;
            string motivo = PedirTexto(Tr("cdc.btn.descartardecision", "Descartar decisión"),
                Tr("cdc.descartar.motivo", "¿Por qué descarta la recomendación? (obligatorio)"));
            if (motivo == null) return;
            try
            {
                Decisiones.DescartarDecision(Modulo, d.IdDecision, motivo);
                MostrarOk(Tr("cdc.msg.descartada", "Recomendación descartada (decisión #{0}).", new object[] { d.IdDecision }));
            }
            catch (Exception ex) { MostrarError(ex); }
            RefrescarDecisiones();
        }

        private void BtnHistCsv_Click(object sender, EventArgs e)
        {
            var t = InformeEjecutivo.TablaDecisiones(HistorialFiltrado(), _medidos, out var enc);
            new Exportacion.GeneradorInformeComercial().Exportar("csv", new Exportacion.ReporteExportable
            {
                Titulo = Tr("cdc.informe.decisiones", "Decisiones tomadas"),
                NombreArchivo = $"DecisionesComerciales_{DateTime.Today:yyyyMMdd}", Datos = t, Encabezados = enc
            }, this);
        }

        // ══ 4. Simulador ═════════════════════════════════════════════════════════

        private void PrepararSimulador()
        {
            int? elegido = (cmbSimPlan.SelectedItem as Item<int?>)?.Valor;
            cmbSimPlan.SelectedIndexChanged -= CmbSimPlan_SelectedIndexChanged;
            cmbSimPlan.Items.Clear();
            var planes = _panel?.Contexto?.Planes ?? new List<BE.MetricaPlanComercial>();
            foreach (var p in planes.Where(p => p.Precio > 0))
                cmbSimPlan.Items.Add(new Item<int?> { Valor = p.IdPlan, Texto = $"{p.Nombre} ({p.Precio:C0})" });
            cmbSimPlan.SelectedIndexChanged += CmbSimPlan_SelectedIndexChanged;
            if (cmbSimPlan.Items.Count == 0) { RecalcularSimulacion(); return; }
            var item = cmbSimPlan.Items.Cast<Item<int?>>().FirstOrDefault(i => i.Valor == elegido)
                       ?? cmbSimPlan.Items.Cast<Item<int?>>().OrderByDescending(i => planes.First(p => p.IdPlan == i.Valor).IngresoEnRiesgo).First();
            cmbSimPlan.SelectedItem = item;
        }

        // Al elegir el plan se cargan sus datos del último análisis (editables).
        private void CmbSimPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            var id = (cmbSimPlan.SelectedItem as Item<int?>)?.Valor;
            var escenario = id.HasValue ? BLLI.InteligenciaComercial.EscenarioBase(_panel, id.Value) : null;
            if (escenario == null) return;
            _cargando = true;
            try
            {
                Asignar(numSimClientesPlan, escenario.ClientesPlan);
                Asignar(numSimObjetivo, escenario.ClientesObjetivo);
                Asignar(numSimDescuento, escenario.DescuentoPorcentaje);
                Asignar(numSimDuracion, escenario.DuracionDias);
                Asignar(numSimRetencion, escenario.TasaRetencion);
                Asignar(numSimHorizonte, escenario.HorizonteMeses);
            }
            finally { _cargando = false; }
            RecalcularSimulacion();
        }

        private static void Asignar(NumericUpDown n, decimal v) => n.Value = Math.Min(n.Maximum, Math.Max(n.Minimum, v));

        private void Simulacion_ValueChanged(object sender, EventArgs e)
        {
            if (!_cargando) RecalcularSimulacion();
        }

        private BE.EscenarioPromocion EscenarioActual()
        {
            var id = (cmbSimPlan.SelectedItem as Item<int?>)?.Valor;
            var plan = _panel?.Contexto?.Planes.FirstOrDefault(p => p.IdPlan == id);
            if (plan == null) return null;
            return new BE.EscenarioPromocion
            {
                IdPlan = plan.IdPlan, NombrePlan = plan.Nombre, PrecioPlan = plan.Precio,
                ClientesPlan = (int)numSimClientesPlan.Value, ClientesObjetivo = (int)numSimObjetivo.Value,
                DescuentoPorcentaje = numSimDescuento.Value, DuracionDias = (int)numSimDuracion.Value,
                TasaRetencion = numSimRetencion.Value, HorizonteMeses = numSimHorizonte.Value
            };
        }

        // Cálculo en vivo con los umbrales vigentes (la matemática es BLL.Inteligencia.SimuladorPromocion).
        private void RecalcularSimulacion()
        {
            var escenario = EscenarioActual();
            _simActual = null;
            if (escenario == null)
            {
                MostrarVeredicto(null, Tr("cdc.sim.analice", "Analice un período para cargar los planes en el simulador."));
            }
            else
            {
                try
                {
                    _simActual = BLLI.SimuladorPromocion.Simular(escenario,
                        _umbrales[BLLI.CatalogoUmbrales.RoiMinimoPct], _umbrales[BLLI.CatalogoUmbrales.MargenSeguridadPp]);
                    MostrarVeredicto(_simActual, TextosComerciales.R(_simActual.Explicacion));
                }
                catch (BE.AppException ex) { MostrarVeredicto(null, MensajeDeError(ex)); }
            }
            GraficosComerciales.Sensibilidad(chartSensibilidad, _simActual, _escenarios);
            MostrarEscenarios();
            btnSimAgregar.Enabled = btnSimCrearSugerencia.Enabled = _simActual != null;
        }

        private void MostrarVeredicto(BE.ResultadoSimulacion r, string explicacion)
        {
            lblVeredictoExplicacion.Text = explicacion;
            if (r == null)
            {
                lblVeredicto.Text = "—";
                pnlVeredicto.BackColor = Tema.PanelClaro;
                lblVeredicto.ForeColor = Tema.TextoMuted;
                lblSimResultados.Text = string.Empty;
                return;
            }
            lblVeredicto.Text = TextosComerciales.VeredictoSimulacion(r.Veredicto);
            switch (r.Veredicto)
            {
                case BE.VeredictoSimulacion.Conviene: pnlVeredicto.BackColor = Tema.FondoExito; lblVeredicto.ForeColor = Tema.Exito; break;
                case BE.VeredictoSimulacion.Riesgoso: pnlVeredicto.BackColor = Tema.FondoAlerta; lblVeredicto.ForeColor = Tema.Alerta; break;
                case BE.VeredictoSimulacion.NoConviene: pnlVeredicto.BackColor = Tema.FondoError; lblVeredicto.ForeColor = Tema.Error; break;
                default: pnlVeredicto.BackColor = Tema.PanelClaro; lblVeredicto.ForeColor = Tema.TextoMuted; break;
            }
            lblSimResultados.Text = Tr("cdc.sim.resultados",
                "Costo del descuento: {0:C0}\nClientes retenidos: {1:0.#}\nIngreso retenido: {2:C0}\nBeneficio neto: {3:C0}\nROI: {4}\nConviene si retiene al menos: {5}",
                new object[] { r.CostoDescuento, r.ClientesRetenidos, r.IngresoRetenido, r.BeneficioNeto,
                               r.Roi.HasValue ? r.Roi.Value.ToString("0.#") + " %" : "—",
                               r.TasaEquilibrio.HasValue ? r.TasaEquilibrio.Value.ToString("0.#") + " %" : "—" }).Replace("\\n", "\n");
        }

        private void BtnSimAgregar_Click(object sender, EventArgs e)
        {
            if (_simActual == null) return;
            if (_escenarios.Count >= MaximoEscenarios)
            {
                MostrarError(Tr("cdc.sim.maximo", "Ya hay 3 escenarios en la comparación: límpiela para agregar otro."));
                return;
            }
            _escenarios.Add(_simActual);
            RecalcularSimulacion();
        }

        private void BtnSimLimpiar_Click(object sender, EventArgs e)
        {
            _escenarios.Clear();
            RecalcularSimulacion();
        }

        // Escenarios lado a lado: una fila por métrica, una columna por escenario (A, B, C y el actual).
        private void MostrarEscenarios()
        {
            var t = new DataTable();
            t.Columns.Add(Tr("cdc.col.metrica", "Métrica"), typeof(string));
            var todos = _escenarios.Select((s, i) => (Nombre: Tr("cdc.sim.escenario", "Escenario {0}", new object[] { (char)('A' + i) }), R: s)).ToList();
            if (_simActual != null) todos.Add((Tr("cdc.sim.actual", "Escenario actual"), _simActual));
            foreach (var x in todos) t.Columns.Add(x.Nombre, typeof(string));
            void Fila(string nombre, Func<BE.ResultadoSimulacion, string> valor)
            {
                var fila = new List<object> { nombre };
                fila.AddRange(todos.Select(x => (object)valor(x.R)));
                t.Rows.Add(fila.ToArray());
            }
            if (todos.Count > 0)
            {
                Fila(Tr("cdc.col.plan", "Plan"), r => r.Escenario.NombrePlan);
                Fila(Tr("cdc.col.descuento", "Descuento"), r => r.Escenario.DescuentoPorcentaje.ToString("0.#") + " %");
                Fila(Tr("cdc.col.dias", "Días"), r => r.Escenario.DuracionDias.ToString());
                Fila(Tr("cdc.col.retencion", "Retención"), r => r.Escenario.TasaRetencion.ToString("0.#") + " %");
                Fila(Tr("cdc.col.costo", "Costo"), r => r.CostoDescuento.ToString("C0"));
                Fila(Tr("cdc.col.ingresoretenido", "Ingreso retenido"), r => r.IngresoRetenido.ToString("C0"));
                Fila(Tr("cdc.col.neto", "Beneficio neto"), r => r.BeneficioNeto.ToString("C0"));
                Fila(Tr("cdc.col.roi", "ROI"), r => r.Roi.HasValue ? r.Roi.Value.ToString("0.#") + " %" : "—");
                Fila(Tr("cdc.col.equilibrio", "Equilibrio"), r => r.TasaEquilibrio.HasValue ? r.TasaEquilibrio.Value.ToString("0.#") + " %" : "—");
                Fila(Tr("cdc.col.veredicto", "Veredicto"), r => TextosComerciales.VeredictoSimulacion(r.Veredicto));
            }
            dgvEscenarios.DataSource = t;
            // Una fila por métrica en un orden fijo: ordenar por encabezado no tiene sentido acá.
            foreach (DataGridViewColumn c in dgvEscenarios.Columns) c.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void BtnSimCrearSugerencia_Click(object sender, EventArgs e)
        {
            var r = _simActual;
            if (r == null) return;
            string motivo = Tr("cdc.sim.motivo",
                "Escenario simulado en el Centro de decisiones: {0:0.#} % de descuento durante {1} días en el plan {2}; beneficio neto estimado {3:C0}, conviene si retiene al menos {4:0.#} %.",
                new object[] { r.Escenario.DescuentoPorcentaje, r.Escenario.DuracionDias, r.Escenario.NombrePlan, r.BeneficioNeto, r.TasaEquilibrio ?? 0m });
            decimal tasa = Math.Max(0.01m, r.Escenario.TasaRetencion);
            int? id = AbrirSugerencia(r.Escenario.IdPlan, r.Escenario.ClientesObjetivo * r.Escenario.PrecioPlan, tasa, motivo, BE.OrigenMetrica.Manual);
            if (id.HasValue)
                MostrarOk(Tr("cdc.msg.sugerenciasim", "Sugerencia #{0} enviada a Administración con este escenario.", new object[] { id }));
        }

        // ══ 5. Medir resultado ═══════════════════════════════════════════════════

        private async void BtnMedir_Click(object sender, EventArgs e)
        {
            DateTime desde = dtpMedirDesde.Value.Date, hasta = dtpMedirHasta.Value.Date;
            try
            {
                var lista = await EjecutarConEsperaAsync(() => bll.MedirPromociones(desde, hasta));
                if (IsDisposed) return;
                _mediciones = lista;
                MostrarMediciones();
                MostrarOk(Tr("cdc.msg.medidas", "{0} promoción(es) medida(s).", new object[] { lista.Count }));
            }
            catch (Exception ex) { if (!IsDisposed) MostrarError(ex); }
        }

        private void MostrarMediciones()
        {
            var t = InformeEjecutivo.TablaMediciones(_mediciones, out var enc);
            dgvMedicion.DataSource = t;
            for (int i = 0; i < enc.Length && i < dgvMedicion.Columns.Count; i++) dgvMedicion.Columns[i].HeaderText = enc[i];
            lblMedirResumen.Text = _mediciones.Count == 0
                ? Tr("cdc.medir.ninguna", "No hay promociones vigentes, dadas de baja o vencidas en el período.")
                : Tr("cdc.medir.resumen", "{0} promoción(es): {1} para mantener, {2} para extender, {3} para dar de baja.",
                     new object[] { _mediciones.Count, _mediciones.Count(m => m.Veredicto == BE.VeredictoPromocion.Mantener),
                                    _mediciones.Count(m => m.Veredicto == BE.VeredictoPromocion.Extender),
                                    _mediciones.Count(m => m.Veredicto == BE.VeredictoPromocion.DarDeBaja) });
            DgvMedicion_SelectionChanged(this, EventArgs.Empty);
        }

        private BE.MedicionPromocion MedicionSeleccionada()
        {
            int i = IndiceSeleccionado(dgvMedicion);
            return i >= 0 && i < _mediciones.Count ? _mediciones[i] : null;
        }

        private void DgvMedicion_SelectionChanged(object sender, EventArgs e)
        {
            var m = MedicionSeleccionada();
            GraficosComerciales.Medicion(chartMedicion, m);
            if (m == null)
            {
                lblMedirVeredicto.Text = string.Empty;
                lblMedirExplicacion.Text = string.Empty;
                btnAplicarVeredicto.Enabled = false;
                return;
            }
            lblMedirVeredicto.Text = TextosComerciales.Veredicto(m.Veredicto);
            lblMedirVeredicto.ForeColor = m.Veredicto == BE.VeredictoPromocion.DarDeBaja ? Tema.Error
                                        : m.Veredicto == BE.VeredictoPromocion.Extender ? Tema.Exito
                                        : m.Veredicto == BE.VeredictoPromocion.Mantener ? Tema.Info : Tema.TextoMuted;
            lblMedirExplicacion.Text = TextosComerciales.R(m.Explicacion) + Environment.NewLine + Environment.NewLine +
                Tr("cdc.medir.roi", "Beneficio real: {0} · ROI real: {1} · ROI esperado: {2}",
                   new object[] { m.BeneficioReal?.ToString("C0") ?? "—", m.RoiReal.HasValue ? m.RoiReal.Value.ToString("0.#") + " %" : "—",
                                  m.RoiEstimado.HasValue ? m.RoiEstimado.Value.ToString("0.#") + " %" : "—" });
            // Solo hay algo que ejecutar si el veredicto es dar de baja o extender (Mantener: sigue como está).
            bool vigente = m.Promocion?.Estado == BE.EstadoPromocion.Vigente;
            bool accionable = m.Veredicto == BE.VeredictoPromocion.DarDeBaja || m.Veredicto == BE.VeredictoPromocion.Extender;
            btnAplicarVeredicto.Enabled = vigente && accionable;
            btnAplicarVeredicto.Text = m.Veredicto == BE.VeredictoPromocion.Extender ? Tr("cdc.accion.extender", "Sugerir la extensión")
                                     : Tr("cdc.accion.baja", "Solicitar la baja de la promoción");
        }

        // El veredicto se aplica como cualquier recomendación: queda registrado como decisión y ejecuta su acción.
        private void BtnAplicarVeredicto_Click(object sender, EventArgs e)
        {
            var m = MedicionSeleccionada();
            if (m == null || Decisiones == null) return;
            var rec = BLLI.Reglas.ReglaRendimientoPromocion.DesdeMedicion(m, DateTime.Today, _umbrales);
            if (rec == null || !PuedeEjecutar(rec.Principal)) return;
            try
            {
                Decisiones.AsociarDecisiones(new[] { rec });
                var decision = Decisiones.Aceptar(Modulo, rec, 0);
                Ejecutar(rec, rec.Principal, decision);
            }
            catch (Exception ex) { MostrarError(ex); }
            RefrescarDecisiones();
        }

        // ══ Umbrales, glosario e informe ═════════════════════════════════════════

        private void BtnUmbrales_Click(object sender, EventArgs e)
        {
            List<BE.UmbralComercial> umbrales;
            try { umbrales = bll.Umbrales.Obtener(); }
            catch (Exception ex) { MostrarError(ex); return; }
            using (var dlg = new UmbralesComercialesDialog(umbrales, bll.Umbrales, Modulo))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
            }
            MostrarOk(Tr("cdc.msg.umbrales", "Umbrales guardados. Vuelva a analizar para aplicarlos."));
            BtnAnalizar_Click(this, EventArgs.Empty);
        }

        private void BtnGlosario_Click(object sender, EventArgs e) =>
            Ayuda.AyudaEnLinea.Mostrar(this, GetType().Name, TextosComerciales.GlosarioTexto());

        private void BtnInforme_Click(object sender, EventArgs e)
        {
            if (_panel == null)
            {
                MostrarError(Tr("err.bll.inteligencia.sinanalisis", "Primero analice un período."));
                return;
            }
            try
            {
                var datos = new InformeEjecutivo.Datos
                {
                    Panel = _panel,
                    Recomendaciones = _recsVisibles,
                    Decisiones = _historial.Where(d => d.Fecha.Date >= _panel.Desde && d.Fecha.Date <= DateTime.Today).ToList(),
                    Medidos = _medidos,
                    Escenarios = _escenarios.ToList(),
                    Mediciones = _mediciones,
                    Metas = MetasParaInforme(),
                    Usuario = BLL.Sesion.Actor
                };
                datos.Graficos.Add((Tr("cdc.grafico.tendencia", "Tendencia de 6 meses y proyección de 3"), GraficosComerciales.Png(chartTendencia)));
                datos.Graficos.Add((Tr("cdc.grafico.riesgo", "Ingreso mensual en riesgo por plan"), GraficosComerciales.Png(chartRiesgo)));
                datos.Graficos.Add((Tr("cdc.grafico.stock", "Stock por categoría"), GraficosComerciales.Png(chartStock)));
                datos.Graficos.Add((Tr("cdc.grafico.rotacion", "Rotación: más y menos pedidas"), GraficosComerciales.Png(chartRotacion)));
                if (_simActual != null || _escenarios.Count > 0)
                    datos.Graficos.Add((Tr("cdc.grafico.sensibilidad", "Beneficio neto según la retención"), GraficosComerciales.Png(chartSensibilidad)));
                AgregarGraficoMetas(datos);
                AgregarCohortesAlInforme(datos);
                var ruta = new Exportacion.GeneradorInformeComercial().Exportar("pdf", InformeEjecutivo.Armar(datos), this);
                if (ruta != null) MostrarOk(Tr("cdc.msg.informe", "Informe ejecutivo guardado: {0}", new object[] { ruta }));
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ══ Auxiliares de diálogo ════════════════════════════════════════════════

        private string PedirTexto(string titulo, string pregunta, string valorInicial = null)
        {
            using (var dlg = new InputDialog(titulo, pregunta, false))
            {
                if (valorInicial != null) dlg.EstablecerTexto(valorInicial);
                if (dlg.ShowDialog(this) != DialogResult.OK) return null;
                return dlg.InputText?.Trim();
            }
        }

    }
}
