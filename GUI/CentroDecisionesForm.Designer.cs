using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace GUI
{
    partial class CentroDecisionesForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            ChartArea areaTendencia = new ChartArea();
            Legend leyendaTendencia = new Legend();
            ChartArea areaRiesgo = new ChartArea();
            Legend leyendaRiesgo = new Legend();
            ChartArea areaStock = new ChartArea();
            Legend leyendaStock = new Legend();
            ChartArea areaRotacion = new ChartArea();
            Legend leyendaRotacion = new Legend();
            ChartArea areaSensibilidad = new ChartArea();
            Legend leyendaSensibilidad = new Legend();
            ChartArea areaMedicion = new ChartArea();
            Legend leyendaMedicion = new Legend();
            this.tip = new ToolTip(this.components);
            // Encabezado: filtros del análisis
            this.pnlEncabezado = new Panel();
            this.lblTitulo = new Label();
            this.lblDesde = new Label();
            this.dtpDesde = new DateTimePicker();
            this.lblHasta = new Label();
            this.dtpHasta = new DateTimePicker();
            this.cmbPreset = new ComboBox();
            this.lblPlan = new Label();
            this.cmbPlan = new ComboBox();
            this.btnAnalizar = new Button();
            this.btnUmbrales = new Button();
            this.btnGlosario = new Button();
            this.btnInforme = new Button();
            this.lblComparacion = new Label();
            this.lblMensaje = new Label();
            this.tabs = new TabControl();
            // Pestaña Analizar
            this.tabAnalizar = new TabPage();
            this.flpKpis = new FlowLayoutPanel();
            this.lblAvisos = new Label();
            this.tlpGraficos = new TableLayoutPanel();
            this.chartTendencia = new Chart();
            this.chartRiesgo = new Chart();
            this.chartStock = new Chart();
            this.chartRotacion = new Chart();
            // Pestaña Recomendaciones
            this.tabRecomendaciones = new TabPage();
            this.pnlRecsBarra = new Panel();
            this.lblRecsResumen = new Label();
            this.chkMostrarOcultas = new CheckBox();
            this.btnRecsCsv = new Button();
            this.splitRecs = new SplitContainer();
            this.dgvRecomendaciones = new DataGridView();
            this.txtDetalle = new RichTextBox();
            this.pnlOpciones = new Panel();
            this.lblOpciones = new Label();
            this.dgvOpciones = new DataGridView();
            this.pnlAcciones = new Panel();
            this.btnAceptar = new Button();
            this.btnDescartar = new Button();
            this.btnPosponer = new Button();
            this.numDiasPosponer = new NumericUpDownLocal();
            this.lblDias = new Label();
            this.btnSimularOpcion = new Button();
            // Pestaña Historial de decisiones
            this.tabHistorial = new TabPage();
            this.pnlHistFiltros = new Panel();
            this.lblHistDesde = new Label();
            this.dtpHistDesde = new DateTimePicker();
            this.lblHistHasta = new Label();
            this.dtpHistHasta = new DateTimePicker();
            this.lblHistEstado = new Label();
            this.cmbHistEstado = new ComboBox();
            this.btnHistFiltrar = new Button();
            this.btnHistCsv = new Button();
            this.dgvHistorial = new DataGridView();
            this.pnlHistAcciones = new Panel();
            this.btnRegistrarResultado = new Button();
            this.btnHistDescartar = new Button();
            this.lblHistResumen = new Label();
            // Pestaña Simulador
            this.tabSimulador = new TabPage();
            this.pnlSimEntradas = new Panel();
            this.lblSimPlan = new Label();
            this.cmbSimPlan = new ComboBox();
            this.lblSimClientesPlan = new Label();
            this.numSimClientesPlan = new NumericUpDownLocal();
            this.lblSimObjetivo = new Label();
            this.numSimObjetivo = new NumericUpDownLocal();
            this.lblSimDescuento = new Label();
            this.numSimDescuento = new NumericUpDownLocal();
            this.lblSimDuracion = new Label();
            this.numSimDuracion = new NumericUpDownLocal();
            this.lblSimRetencion = new Label();
            this.numSimRetencion = new NumericUpDownLocal();
            this.lblSimHorizonte = new Label();
            this.numSimHorizonte = new NumericUpDownLocal();
            this.pnlVeredicto = new Panel();
            this.lblVeredicto = new Label();
            this.lblVeredictoExplicacion = new Label();
            this.lblSimResultados = new Label();
            this.btnSimAgregar = new Button();
            this.btnSimLimpiar = new Button();
            this.btnSimCrearSugerencia = new Button();
            this.splitSim = new SplitContainer();
            this.chartSensibilidad = new Chart();
            this.dgvEscenarios = new DataGridView();
            // Pestaña Medir resultado
            this.tabMedir = new TabPage();
            this.pnlMedirFiltros = new Panel();
            this.lblMedirDesde = new Label();
            this.dtpMedirDesde = new DateTimePicker();
            this.lblMedirHasta = new Label();
            this.dtpMedirHasta = new DateTimePicker();
            this.btnMedir = new Button();
            this.lblMedirResumen = new Label();
            this.splitMedir = new SplitContainer();
            this.dgvMedicion = new DataGridView();
            this.chartMedicion = new Chart();
            this.pnlMedirVeredicto = new Panel();
            this.lblMedirVeredicto = new Label();
            this.lblMedirExplicacion = new Label();
            this.btnAplicarVeredicto = new Button();

            this.pnlEncabezado.SuspendLayout();
            this.tabs.SuspendLayout();
            this.tabAnalizar.SuspendLayout();
            this.tlpGraficos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartTendencia)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartRiesgo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartStock)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartRotacion)).BeginInit();
            this.tabRecomendaciones.SuspendLayout();
            this.pnlRecsBarra.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRecs)).BeginInit();
            this.splitRecs.Panel1.SuspendLayout();
            this.splitRecs.Panel2.SuspendLayout();
            this.splitRecs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecomendaciones)).BeginInit();
            this.pnlOpciones.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOpciones)).BeginInit();
            this.pnlAcciones.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDiasPosponer)).BeginInit();
            this.tabHistorial.SuspendLayout();
            this.pnlHistFiltros.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistorial)).BeginInit();
            this.pnlHistAcciones.SuspendLayout();
            this.tabSimulador.SuspendLayout();
            this.pnlSimEntradas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSimClientesPlan)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimObjetivo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimDescuento)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimDuracion)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimRetencion)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimHorizonte)).BeginInit();
            this.pnlVeredicto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitSim)).BeginInit();
            this.splitSim.Panel1.SuspendLayout();
            this.splitSim.Panel2.SuspendLayout();
            this.splitSim.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartSensibilidad)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEscenarios)).BeginInit();
            this.tabMedir.SuspendLayout();
            this.pnlMedirFiltros.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMedir)).BeginInit();
            this.splitMedir.Panel1.SuspendLayout();
            this.splitMedir.Panel2.SuspendLayout();
            this.splitMedir.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMedicion)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartMedicion)).BeginInit();
            this.pnlMedirVeredicto.SuspendLayout();
            this.SuspendLayout();

            // ══ Encabezado ═════════════════════════════════════════════════════
            this.pnlEncabezado.Dock = DockStyle.Top;
            this.pnlEncabezado.Height = 96;
            this.pnlEncabezado.BackColor = Tema.Papel;
            this.pnlEncabezado.Name = "pnlEncabezado";
            this.pnlEncabezado.Controls.Add(this.lblTitulo);
            this.pnlEncabezado.Controls.Add(this.lblDesde);
            this.pnlEncabezado.Controls.Add(this.dtpDesde);
            this.pnlEncabezado.Controls.Add(this.lblHasta);
            this.pnlEncabezado.Controls.Add(this.dtpHasta);
            this.pnlEncabezado.Controls.Add(this.cmbPreset);
            this.pnlEncabezado.Controls.Add(this.lblPlan);
            this.pnlEncabezado.Controls.Add(this.cmbPlan);
            this.pnlEncabezado.Controls.Add(this.btnAnalizar);
            this.pnlEncabezado.Controls.Add(this.btnUmbrales);
            this.pnlEncabezado.Controls.Add(this.btnGlosario);
            this.pnlEncabezado.Controls.Add(this.btnInforme);
            this.pnlEncabezado.Controls.Add(this.lblComparacion);

            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font = Tema.FuenteTitulo;
            this.lblTitulo.ForeColor = Tema.RosaOscuro;
            this.lblTitulo.Location = new Point(12, 8);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Text = "Centro de decisiones comerciales";

            this.lblDesde.AutoSize = true;
            this.lblDesde.Location = new Point(12, 47);
            this.lblDesde.Name = "lblDesde";
            this.lblDesde.Text = "Desde:";

            this.dtpDesde.Format = DateTimePickerFormat.Short;
            this.dtpDesde.Location = new Point(62, 43);
            this.dtpDesde.Name = "dtpDesde";
            this.dtpDesde.Size = new Size(105, 23);
            this.dtpDesde.TabIndex = 1;
            this.dtpDesde.ValueChanged += new System.EventHandler(this.DtpPeriodo_ValueChanged);

            this.lblHasta.AutoSize = true;
            this.lblHasta.Location = new Point(176, 47);
            this.lblHasta.Name = "lblHasta";
            this.lblHasta.Text = "Hasta:";

            this.dtpHasta.Format = DateTimePickerFormat.Short;
            this.dtpHasta.Location = new Point(224, 43);
            this.dtpHasta.Name = "dtpHasta";
            this.dtpHasta.Size = new Size(105, 23);
            this.dtpHasta.TabIndex = 2;
            this.dtpHasta.ValueChanged += new System.EventHandler(this.DtpPeriodo_ValueChanged);

            this.cmbPreset.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbPreset.Location = new Point(338, 43);
            this.cmbPreset.Name = "cmbPreset";
            this.cmbPreset.Size = new Size(170, 23);
            this.cmbPreset.TabIndex = 3;
            this.cmbPreset.SelectedIndexChanged += new System.EventHandler(this.CmbPreset_SelectedIndexChanged);

            this.lblPlan.AutoSize = true;
            this.lblPlan.Location = new Point(518, 47);
            this.lblPlan.Name = "lblPlan";
            this.lblPlan.Text = "Plan:";

            this.cmbPlan.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbPlan.Location = new Point(558, 43);
            this.cmbPlan.Name = "cmbPlan";
            this.cmbPlan.Size = new Size(160, 23);
            this.cmbPlan.TabIndex = 4;

            this.btnAnalizar.Location = new Point(728, 40);
            this.btnAnalizar.Name = "btnAnalizar";
            this.btnAnalizar.Size = new Size(110, 30);
            this.btnAnalizar.TabIndex = 5;
            this.btnAnalizar.Text = "Analizar";
            this.btnAnalizar.UseVisualStyleBackColor = true;
            this.btnAnalizar.Click += new System.EventHandler(this.BtnAnalizar_Click);

            this.btnUmbrales.Location = new Point(846, 40);
            this.btnUmbrales.Name = "btnUmbrales";
            this.btnUmbrales.Size = new Size(100, 30);
            this.btnUmbrales.TabIndex = 6;
            this.btnUmbrales.Text = "Umbrales";
            this.btnUmbrales.UseVisualStyleBackColor = true;
            this.btnUmbrales.Click += new System.EventHandler(this.BtnUmbrales_Click);

            this.btnGlosario.Location = new Point(954, 40);
            this.btnGlosario.Name = "btnGlosario";
            this.btnGlosario.Size = new Size(95, 30);
            this.btnGlosario.TabIndex = 7;
            this.btnGlosario.Text = "Glosario";
            this.btnGlosario.UseVisualStyleBackColor = true;
            this.btnGlosario.Click += new System.EventHandler(this.BtnGlosario_Click);

            this.btnInforme.Location = new Point(1057, 40);
            this.btnInforme.Name = "btnInforme";
            this.btnInforme.Size = new Size(170, 30);
            this.btnInforme.TabIndex = 8;
            this.btnInforme.Text = "Informe ejecutivo (PDF)";
            this.btnInforme.UseVisualStyleBackColor = true;
            this.btnInforme.Click += new System.EventHandler(this.BtnInforme_Click);

            this.lblComparacion.AutoSize = true;
            this.lblComparacion.Font = Tema.FuentePequena;
            this.lblComparacion.ForeColor = Tema.TextoMuted;
            this.lblComparacion.Location = new Point(12, 74);
            this.lblComparacion.Name = "lblComparacion";

            // ══ Mensaje (barra inferior) ═══════════════════════════════════════
            this.lblMensaje.Dock = DockStyle.Bottom;
            this.lblMensaje.Height = 24;
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Padding = new Padding(8, 4, 8, 0);

            // ══ Pestañas ═══════════════════════════════════════════════════════
            this.tabs.Controls.Add(this.tabAnalizar);
            this.tabs.Controls.Add(this.tabRecomendaciones);
            this.tabs.Controls.Add(this.tabHistorial);
            this.tabs.Controls.Add(this.tabSimulador);
            this.tabs.Controls.Add(this.tabMedir);
            this.tabs.Dock = DockStyle.Fill;
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.TabIndex = 10;

            // ── 1. Analizar ────────────────────────────────────────────────────
            this.tabAnalizar.Controls.Add(this.tlpGraficos);
            this.tabAnalizar.Controls.Add(this.lblAvisos);
            this.tabAnalizar.Controls.Add(this.flpKpis);
            this.tabAnalizar.Name = "tabAnalizar";
            this.tabAnalizar.Padding = new Padding(4);
            this.tabAnalizar.Text = "1. Analizar";
            this.tabAnalizar.BackColor = Tema.PanelClaro;

            this.flpKpis.AutoScroll = true;
            this.flpKpis.Dock = DockStyle.Top;
            this.flpKpis.Height = 206;
            this.flpKpis.Name = "flpKpis";
            this.flpKpis.Padding = new Padding(2);

            this.lblAvisos.Dock = DockStyle.Top;
            this.lblAvisos.ForeColor = Tema.Alerta;
            this.lblAvisos.Height = 20;
            this.lblAvisos.Name = "lblAvisos";

            this.tlpGraficos.ColumnCount = 2;
            this.tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpGraficos.RowCount = 2;
            this.tlpGraficos.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.tlpGraficos.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.tlpGraficos.Controls.Add(this.chartTendencia, 0, 0);
            this.tlpGraficos.Controls.Add(this.chartRiesgo, 1, 0);
            this.tlpGraficos.Controls.Add(this.chartStock, 0, 1);
            this.tlpGraficos.Controls.Add(this.chartRotacion, 1, 1);
            this.tlpGraficos.Dock = DockStyle.Fill;
            this.tlpGraficos.Name = "tlpGraficos";

            areaTendencia.Name = "principal";
            leyendaTendencia.Name = "leyenda";
            this.chartTendencia.ChartAreas.Add(areaTendencia);
            this.chartTendencia.Legends.Add(leyendaTendencia);
            this.chartTendencia.Dock = DockStyle.Fill;
            // MSChart no admite ancho o alto 0 (pasa al escalar una pestaña que todavía no se mostró).
            this.chartTendencia.MinimumSize = new Size(120, 90);
            this.chartTendencia.Name = "chartTendencia";

            areaRiesgo.Name = "principal";
            leyendaRiesgo.Name = "leyenda";
            this.chartRiesgo.ChartAreas.Add(areaRiesgo);
            this.chartRiesgo.Legends.Add(leyendaRiesgo);
            this.chartRiesgo.Dock = DockStyle.Fill;
            // MSChart no admite ancho o alto 0 (pasa al escalar una pestaña que todavía no se mostró).
            this.chartRiesgo.MinimumSize = new Size(120, 90);
            this.chartRiesgo.Name = "chartRiesgo";

            areaStock.Name = "principal";
            leyendaStock.Name = "leyenda";
            this.chartStock.ChartAreas.Add(areaStock);
            this.chartStock.Legends.Add(leyendaStock);
            this.chartStock.Dock = DockStyle.Fill;
            // MSChart no admite ancho o alto 0 (pasa al escalar una pestaña que todavía no se mostró).
            this.chartStock.MinimumSize = new Size(120, 90);
            this.chartStock.Name = "chartStock";

            areaRotacion.Name = "principal";
            leyendaRotacion.Name = "leyenda";
            this.chartRotacion.ChartAreas.Add(areaRotacion);
            this.chartRotacion.Legends.Add(leyendaRotacion);
            this.chartRotacion.Dock = DockStyle.Fill;
            // MSChart no admite ancho o alto 0 (pasa al escalar una pestaña que todavía no se mostró).
            this.chartRotacion.MinimumSize = new Size(120, 90);
            this.chartRotacion.Name = "chartRotacion";

            // ── 2. Recomendaciones ─────────────────────────────────────────────
            this.tabRecomendaciones.Controls.Add(this.splitRecs);
            this.tabRecomendaciones.Controls.Add(this.pnlRecsBarra);
            this.tabRecomendaciones.Name = "tabRecomendaciones";
            this.tabRecomendaciones.Padding = new Padding(4);
            this.tabRecomendaciones.Text = "2. Recomendaciones";

            this.pnlRecsBarra.Dock = DockStyle.Top;
            this.pnlRecsBarra.Height = 38;
            this.pnlRecsBarra.Name = "pnlRecsBarra";
            this.pnlRecsBarra.Controls.Add(this.lblRecsResumen);
            this.pnlRecsBarra.Controls.Add(this.chkMostrarOcultas);
            this.pnlRecsBarra.Controls.Add(this.btnRecsCsv);

            this.lblRecsResumen.AutoSize = true;
            this.lblRecsResumen.Font = Tema.FuenteSubtitulo;
            this.lblRecsResumen.ForeColor = Tema.RosaOscuro;
            this.lblRecsResumen.Location = new Point(6, 10);
            this.lblRecsResumen.Name = "lblRecsResumen";

            this.chkMostrarOcultas.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.chkMostrarOcultas.AutoSize = true;
            this.chkMostrarOcultas.Location = new Point(760, 10);
            this.chkMostrarOcultas.Name = "chkMostrarOcultas";
            this.chkMostrarOcultas.Text = "Mostrar pospuestas y descartadas";
            this.chkMostrarOcultas.CheckedChanged += new System.EventHandler(this.ChkMostrarOcultas_CheckedChanged);

            this.btnRecsCsv.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRecsCsv.Location = new Point(1030, 4);
            this.btnRecsCsv.Name = "btnRecsCsv";
            this.btnRecsCsv.Size = new Size(170, 30);
            this.btnRecsCsv.Text = "Exportar a .CSV";
            this.btnRecsCsv.UseVisualStyleBackColor = true;
            this.btnRecsCsv.Click += new System.EventHandler(this.BtnRecsCsv_Click);

            this.splitRecs.Dock = DockStyle.Fill;
            this.splitRecs.Name = "splitRecs";
            this.splitRecs.Size = new Size(1224, 590);
            this.splitRecs.SplitterDistance = 560;
            this.splitRecs.Panel1.Controls.Add(this.dgvRecomendaciones);
            this.splitRecs.Panel2.Controls.Add(this.txtDetalle);
            this.splitRecs.Panel2.Controls.Add(this.pnlOpciones);

            this.dgvRecomendaciones.AllowUserToAddRows = false;
            this.dgvRecomendaciones.AllowUserToDeleteRows = false;
            this.dgvRecomendaciones.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvRecomendaciones.Dock = DockStyle.Fill;
            this.dgvRecomendaciones.MultiSelect = false;
            this.dgvRecomendaciones.Name = "dgvRecomendaciones";
            this.dgvRecomendaciones.ReadOnly = true;
            this.dgvRecomendaciones.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvRecomendaciones.SelectionChanged += new System.EventHandler(this.DgvRecomendaciones_SelectionChanged);

            this.txtDetalle.BackColor = Color.White;
            this.txtDetalle.BorderStyle = BorderStyle.None;
            this.txtDetalle.Dock = DockStyle.Fill;
            this.txtDetalle.Name = "txtDetalle";
            this.txtDetalle.ReadOnly = true;

            this.pnlOpciones.Dock = DockStyle.Bottom;
            this.pnlOpciones.Height = 230;
            this.pnlOpciones.Name = "pnlOpciones";
            this.pnlOpciones.Controls.Add(this.dgvOpciones);
            this.pnlOpciones.Controls.Add(this.lblOpciones);
            this.pnlOpciones.Controls.Add(this.pnlAcciones);

            this.lblOpciones.Dock = DockStyle.Top;
            this.lblOpciones.Font = Tema.FuenteSubtitulo;
            this.lblOpciones.ForeColor = Tema.RosaOscuro;
            this.lblOpciones.Height = 22;
            this.lblOpciones.Name = "lblOpciones";
            this.lblOpciones.Text = "Opciones (elija una)";

            this.dgvOpciones.AllowUserToAddRows = false;
            this.dgvOpciones.AllowUserToDeleteRows = false;
            this.dgvOpciones.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvOpciones.Dock = DockStyle.Fill;
            this.dgvOpciones.MultiSelect = false;
            this.dgvOpciones.Name = "dgvOpciones";
            this.dgvOpciones.ReadOnly = true;
            this.dgvOpciones.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvOpciones.SelectionChanged += new System.EventHandler(this.DgvOpciones_SelectionChanged);

            this.pnlAcciones.Dock = DockStyle.Bottom;
            this.pnlAcciones.Height = 44;
            this.pnlAcciones.Name = "pnlAcciones";
            this.pnlAcciones.Controls.Add(this.btnAceptar);
            this.pnlAcciones.Controls.Add(this.btnDescartar);
            this.pnlAcciones.Controls.Add(this.btnPosponer);
            this.pnlAcciones.Controls.Add(this.numDiasPosponer);
            this.pnlAcciones.Controls.Add(this.lblDias);
            this.pnlAcciones.Controls.Add(this.btnSimularOpcion);

            this.btnAceptar.Location = new Point(4, 7);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new Size(190, 30);
            this.btnAceptar.Text = "Aceptar y ejecutar";
            this.btnAceptar.UseVisualStyleBackColor = true;
            this.btnAceptar.Click += new System.EventHandler(this.BtnAceptar_Click);

            this.btnDescartar.Location = new Point(200, 7);
            this.btnDescartar.Name = "btnDescartar";
            this.btnDescartar.Size = new Size(110, 30);
            this.btnDescartar.Text = "Descartar";
            this.btnDescartar.UseVisualStyleBackColor = true;
            this.btnDescartar.Click += new System.EventHandler(this.BtnDescartar_Click);

            this.btnPosponer.Location = new Point(316, 7);
            this.btnPosponer.Name = "btnPosponer";
            this.btnPosponer.Size = new Size(110, 30);
            this.btnPosponer.Text = "Posponer";
            this.btnPosponer.UseVisualStyleBackColor = true;
            this.btnPosponer.Click += new System.EventHandler(this.BtnPosponer_Click);

            this.numDiasPosponer.Location = new Point(432, 11);
            this.numDiasPosponer.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
            this.numDiasPosponer.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numDiasPosponer.Name = "numDiasPosponer";
            this.numDiasPosponer.Size = new Size(55, 23);
            this.numDiasPosponer.Value = new decimal(new int[] { 7, 0, 0, 0 });

            this.lblDias.AutoSize = true;
            this.lblDias.Location = new Point(492, 14);
            this.lblDias.Name = "lblDias";
            this.lblDias.Text = "días";

            this.btnSimularOpcion.Location = new Point(540, 7);
            this.btnSimularOpcion.Name = "btnSimularOpcion";
            this.btnSimularOpcion.Size = new Size(170, 30);
            this.btnSimularOpcion.Text = "Simular esta opción";
            this.btnSimularOpcion.UseVisualStyleBackColor = true;
            this.btnSimularOpcion.Click += new System.EventHandler(this.BtnSimularOpcion_Click);

            // ── 3. Historial de decisiones ─────────────────────────────────────
            this.tabHistorial.Controls.Add(this.dgvHistorial);
            this.tabHistorial.Controls.Add(this.pnlHistFiltros);
            this.tabHistorial.Controls.Add(this.pnlHistAcciones);
            this.tabHistorial.Name = "tabHistorial";
            this.tabHistorial.Padding = new Padding(4);
            this.tabHistorial.Text = "3. Historial de decisiones";

            this.pnlHistFiltros.Dock = DockStyle.Top;
            this.pnlHistFiltros.Height = 40;
            this.pnlHistFiltros.Name = "pnlHistFiltros";
            this.pnlHistFiltros.Controls.Add(this.lblHistDesde);
            this.pnlHistFiltros.Controls.Add(this.dtpHistDesde);
            this.pnlHistFiltros.Controls.Add(this.lblHistHasta);
            this.pnlHistFiltros.Controls.Add(this.dtpHistHasta);
            this.pnlHistFiltros.Controls.Add(this.lblHistEstado);
            this.pnlHistFiltros.Controls.Add(this.cmbHistEstado);
            this.pnlHistFiltros.Controls.Add(this.btnHistFiltrar);
            this.pnlHistFiltros.Controls.Add(this.btnHistCsv);

            this.lblHistDesde.AutoSize = true;
            this.lblHistDesde.Location = new Point(4, 12);
            this.lblHistDesde.Name = "lblHistDesde";
            this.lblHistDesde.Text = "Desde:";

            this.dtpHistDesde.Format = DateTimePickerFormat.Short;
            this.dtpHistDesde.Location = new Point(54, 8);
            this.dtpHistDesde.Name = "dtpHistDesde";
            this.dtpHistDesde.Size = new Size(105, 23);

            this.lblHistHasta.AutoSize = true;
            this.lblHistHasta.Location = new Point(168, 12);
            this.lblHistHasta.Name = "lblHistHasta";
            this.lblHistHasta.Text = "Hasta:";

            this.dtpHistHasta.Format = DateTimePickerFormat.Short;
            this.dtpHistHasta.Location = new Point(216, 8);
            this.dtpHistHasta.Name = "dtpHistHasta";
            this.dtpHistHasta.Size = new Size(105, 23);

            this.lblHistEstado.AutoSize = true;
            this.lblHistEstado.Location = new Point(332, 12);
            this.lblHistEstado.Name = "lblHistEstado";
            this.lblHistEstado.Text = "Estado:";

            this.cmbHistEstado.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbHistEstado.Location = new Point(386, 8);
            this.cmbHistEstado.Name = "cmbHistEstado";
            this.cmbHistEstado.Size = new Size(150, 23);

            this.btnHistFiltrar.Location = new Point(546, 5);
            this.btnHistFiltrar.Name = "btnHistFiltrar";
            this.btnHistFiltrar.Size = new Size(110, 30);
            this.btnHistFiltrar.Text = "Filtrar";
            this.btnHistFiltrar.UseVisualStyleBackColor = true;
            this.btnHistFiltrar.Click += new System.EventHandler(this.BtnHistFiltrar_Click);

            this.btnHistCsv.Location = new Point(664, 5);
            this.btnHistCsv.Name = "btnHistCsv";
            this.btnHistCsv.Size = new Size(170, 30);
            this.btnHistCsv.Text = "Exportar a .CSV";
            this.btnHistCsv.UseVisualStyleBackColor = true;
            this.btnHistCsv.Click += new System.EventHandler(this.BtnHistCsv_Click);

            this.dgvHistorial.AllowUserToAddRows = false;
            this.dgvHistorial.AllowUserToDeleteRows = false;
            this.dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistorial.Dock = DockStyle.Fill;
            this.dgvHistorial.MultiSelect = false;
            this.dgvHistorial.Name = "dgvHistorial";
            this.dgvHistorial.ReadOnly = true;
            this.dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistorial.SelectionChanged += new System.EventHandler(this.DgvHistorial_SelectionChanged);

            this.pnlHistAcciones.Dock = DockStyle.Bottom;
            this.pnlHistAcciones.Height = 44;
            this.pnlHistAcciones.Name = "pnlHistAcciones";
            this.pnlHistAcciones.Controls.Add(this.btnRegistrarResultado);
            this.pnlHistAcciones.Controls.Add(this.btnHistDescartar);
            this.pnlHistAcciones.Controls.Add(this.lblHistResumen);

            this.btnRegistrarResultado.Location = new Point(4, 7);
            this.btnRegistrarResultado.Name = "btnRegistrarResultado";
            this.btnRegistrarResultado.Size = new Size(200, 30);
            this.btnRegistrarResultado.Text = "Registrar resultado real";
            this.btnRegistrarResultado.UseVisualStyleBackColor = true;
            this.btnRegistrarResultado.Click += new System.EventHandler(this.BtnRegistrarResultado_Click);

            this.btnHistDescartar.Location = new Point(210, 7);
            this.btnHistDescartar.Name = "btnHistDescartar";
            this.btnHistDescartar.Size = new Size(170, 30);
            this.btnHistDescartar.Text = "Descartar decisión";
            this.btnHistDescartar.UseVisualStyleBackColor = true;
            this.btnHistDescartar.Click += new System.EventHandler(this.BtnHistDescartar_Click);

            this.lblHistResumen.AutoSize = true;
            this.lblHistResumen.Location = new Point(392, 14);
            this.lblHistResumen.Name = "lblHistResumen";

            // ── 4. Simulador ───────────────────────────────────────────────────
            this.tabSimulador.Controls.Add(this.splitSim);
            this.tabSimulador.Controls.Add(this.pnlSimEntradas);
            this.tabSimulador.Name = "tabSimulador";
            this.tabSimulador.Padding = new Padding(4);
            this.tabSimulador.Text = "4. Simulador";

            this.pnlSimEntradas.AutoScroll = true;
            this.pnlSimEntradas.Dock = DockStyle.Left;
            this.pnlSimEntradas.Name = "pnlSimEntradas";
            this.pnlSimEntradas.Width = 360;
            this.pnlSimEntradas.Controls.Add(this.lblSimPlan);
            this.pnlSimEntradas.Controls.Add(this.cmbSimPlan);
            this.pnlSimEntradas.Controls.Add(this.lblSimClientesPlan);
            this.pnlSimEntradas.Controls.Add(this.numSimClientesPlan);
            this.pnlSimEntradas.Controls.Add(this.lblSimObjetivo);
            this.pnlSimEntradas.Controls.Add(this.numSimObjetivo);
            this.pnlSimEntradas.Controls.Add(this.lblSimDescuento);
            this.pnlSimEntradas.Controls.Add(this.numSimDescuento);
            this.pnlSimEntradas.Controls.Add(this.lblSimDuracion);
            this.pnlSimEntradas.Controls.Add(this.numSimDuracion);
            this.pnlSimEntradas.Controls.Add(this.lblSimRetencion);
            this.pnlSimEntradas.Controls.Add(this.numSimRetencion);
            this.pnlSimEntradas.Controls.Add(this.lblSimHorizonte);
            this.pnlSimEntradas.Controls.Add(this.numSimHorizonte);
            this.pnlSimEntradas.Controls.Add(this.pnlVeredicto);
            this.pnlSimEntradas.Controls.Add(this.lblSimResultados);
            this.pnlSimEntradas.Controls.Add(this.btnSimAgregar);
            this.pnlSimEntradas.Controls.Add(this.btnSimLimpiar);
            this.pnlSimEntradas.Controls.Add(this.btnSimCrearSugerencia);

            this.lblSimPlan.Location = new Point(6, 10);
            this.lblSimPlan.Name = "lblSimPlan";
            this.lblSimPlan.Size = new Size(200, 20);
            this.lblSimPlan.Text = "Plan:";
            this.cmbSimPlan.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbSimPlan.Location = new Point(210, 7);
            this.cmbSimPlan.Name = "cmbSimPlan";
            this.cmbSimPlan.Size = new Size(130, 23);
            this.cmbSimPlan.SelectedIndexChanged += new System.EventHandler(this.CmbSimPlan_SelectedIndexChanged);

            this.lblSimClientesPlan.Location = new Point(6, 41);
            this.lblSimClientesPlan.Name = "lblSimClientesPlan";
            this.lblSimClientesPlan.Size = new Size(200, 20);
            this.lblSimClientesPlan.Text = "Clientes del plan:";
            this.numSimClientesPlan.Location = new Point(210, 38);
            this.numSimClientesPlan.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numSimClientesPlan.Name = "numSimClientesPlan";
            this.numSimClientesPlan.Size = new Size(130, 23);
            this.numSimClientesPlan.ValueChanged += new System.EventHandler(this.Simulacion_ValueChanged);

            this.lblSimObjetivo.Location = new Point(6, 72);
            this.lblSimObjetivo.Name = "lblSimObjetivo";
            this.lblSimObjetivo.Size = new Size(200, 20);
            this.lblSimObjetivo.Text = "Clientes objetivo (en riesgo):";
            this.numSimObjetivo.Location = new Point(210, 69);
            this.numSimObjetivo.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numSimObjetivo.Name = "numSimObjetivo";
            this.numSimObjetivo.Size = new Size(130, 23);
            this.numSimObjetivo.ValueChanged += new System.EventHandler(this.Simulacion_ValueChanged);

            this.lblSimDescuento.Location = new Point(6, 103);
            this.lblSimDescuento.Name = "lblSimDescuento";
            this.lblSimDescuento.Size = new Size(200, 20);
            this.lblSimDescuento.Text = "Descuento (%):";
            this.numSimDescuento.DecimalPlaces = 1;
            this.numSimDescuento.Location = new Point(210, 100);
            this.numSimDescuento.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numSimDescuento.Name = "numSimDescuento";
            this.numSimDescuento.Size = new Size(130, 23);
            this.numSimDescuento.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.numSimDescuento.ValueChanged += new System.EventHandler(this.Simulacion_ValueChanged);

            this.lblSimDuracion.Location = new Point(6, 134);
            this.lblSimDuracion.Name = "lblSimDuracion";
            this.lblSimDuracion.Size = new Size(200, 20);
            this.lblSimDuracion.Text = "Duración (días):";
            this.numSimDuracion.Location = new Point(210, 131);
            this.numSimDuracion.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            this.numSimDuracion.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numSimDuracion.Name = "numSimDuracion";
            this.numSimDuracion.Size = new Size(130, 23);
            this.numSimDuracion.Value = new decimal(new int[] { 30, 0, 0, 0 });
            this.numSimDuracion.ValueChanged += new System.EventHandler(this.Simulacion_ValueChanged);

            this.lblSimRetencion.Location = new Point(6, 165);
            this.lblSimRetencion.Name = "lblSimRetencion";
            this.lblSimRetencion.Size = new Size(200, 20);
            this.lblSimRetencion.Text = "Retención esperada (%):";
            this.numSimRetencion.DecimalPlaces = 1;
            this.numSimRetencion.Location = new Point(210, 162);
            this.numSimRetencion.Name = "numSimRetencion";
            this.numSimRetencion.Size = new Size(130, 23);
            this.numSimRetencion.Value = new decimal(new int[] { 30, 0, 0, 0 });
            this.numSimRetencion.ValueChanged += new System.EventHandler(this.Simulacion_ValueChanged);

            this.lblSimHorizonte.Location = new Point(6, 196);
            this.lblSimHorizonte.Name = "lblSimHorizonte";
            this.lblSimHorizonte.Size = new Size(200, 20);
            this.lblSimHorizonte.Text = "Meses que paga un retenido:";
            this.numSimHorizonte.Location = new Point(210, 193);
            this.numSimHorizonte.Maximum = new decimal(new int[] { 24, 0, 0, 0 });
            this.numSimHorizonte.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numSimHorizonte.Name = "numSimHorizonte";
            this.numSimHorizonte.Size = new Size(130, 23);
            this.numSimHorizonte.Value = new decimal(new int[] { 6, 0, 0, 0 });
            this.numSimHorizonte.ValueChanged += new System.EventHandler(this.Simulacion_ValueChanged);

            this.pnlVeredicto.Location = new Point(6, 228);
            this.pnlVeredicto.Name = "pnlVeredicto";
            this.pnlVeredicto.Size = new Size(334, 92);
            this.pnlVeredicto.BackColor = Tema.PanelClaro;
            this.pnlVeredicto.Controls.Add(this.lblVeredictoExplicacion);
            this.pnlVeredicto.Controls.Add(this.lblVeredicto);

            this.lblVeredicto.Dock = DockStyle.Top;
            this.lblVeredicto.Font = Tema.FuenteTitulo;
            this.lblVeredicto.Height = 28;
            this.lblVeredicto.Name = "lblVeredicto";
            this.lblVeredicto.Padding = new Padding(6, 4, 0, 0);

            this.lblVeredictoExplicacion.Dock = DockStyle.Fill;
            this.lblVeredictoExplicacion.Font = Tema.FuentePequena;
            this.lblVeredictoExplicacion.Name = "lblVeredictoExplicacion";
            this.lblVeredictoExplicacion.Padding = new Padding(6, 2, 6, 2);

            this.lblSimResultados.Location = new Point(6, 326);
            this.lblSimResultados.Name = "lblSimResultados";
            this.lblSimResultados.Size = new Size(334, 112);
            this.lblSimResultados.Font = Tema.FuenteNormal;

            this.btnSimAgregar.Location = new Point(6, 444);
            this.btnSimAgregar.Name = "btnSimAgregar";
            this.btnSimAgregar.Size = new Size(334, 30);
            this.btnSimAgregar.Text = "Agregar a la comparación (máx. 3)";
            this.btnSimAgregar.UseVisualStyleBackColor = true;
            this.btnSimAgregar.Click += new System.EventHandler(this.BtnSimAgregar_Click);

            this.btnSimLimpiar.Location = new Point(6, 478);
            this.btnSimLimpiar.Name = "btnSimLimpiar";
            this.btnSimLimpiar.Size = new Size(334, 30);
            this.btnSimLimpiar.Text = "Limpiar la comparación";
            this.btnSimLimpiar.UseVisualStyleBackColor = true;
            this.btnSimLimpiar.Click += new System.EventHandler(this.BtnSimLimpiar_Click);

            this.btnSimCrearSugerencia.Location = new Point(6, 512);
            this.btnSimCrearSugerencia.Name = "btnSimCrearSugerencia";
            this.btnSimCrearSugerencia.Size = new Size(334, 30);
            this.btnSimCrearSugerencia.Text = "Crear sugerencia con este escenario";
            this.btnSimCrearSugerencia.UseVisualStyleBackColor = true;
            this.btnSimCrearSugerencia.Click += new System.EventHandler(this.BtnSimCrearSugerencia_Click);

            this.splitSim.Dock = DockStyle.Fill;
            this.splitSim.Name = "splitSim";
            this.splitSim.Orientation = Orientation.Horizontal;
            this.splitSim.Size = new Size(864, 630);
            this.splitSim.SplitterDistance = 330;
            this.splitSim.Panel1.Controls.Add(this.chartSensibilidad);
            this.splitSim.Panel2.Controls.Add(this.dgvEscenarios);

            areaSensibilidad.Name = "principal";
            leyendaSensibilidad.Name = "leyenda";
            this.chartSensibilidad.ChartAreas.Add(areaSensibilidad);
            this.chartSensibilidad.Legends.Add(leyendaSensibilidad);
            this.chartSensibilidad.Dock = DockStyle.Fill;
            // MSChart no admite ancho o alto 0 (pasa al escalar una pestaña que todavía no se mostró).
            this.chartSensibilidad.MinimumSize = new Size(120, 90);
            this.chartSensibilidad.Name = "chartSensibilidad";

            this.dgvEscenarios.AllowUserToAddRows = false;
            this.dgvEscenarios.AllowUserToDeleteRows = false;
            this.dgvEscenarios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvEscenarios.Dock = DockStyle.Fill;
            this.dgvEscenarios.Name = "dgvEscenarios";
            this.dgvEscenarios.ReadOnly = true;

            // ── 5. Medir resultado ─────────────────────────────────────────────
            this.tabMedir.Controls.Add(this.splitMedir);
            this.tabMedir.Controls.Add(this.pnlMedirFiltros);
            this.tabMedir.Name = "tabMedir";
            this.tabMedir.Padding = new Padding(4);
            this.tabMedir.Text = "5. Medir resultado";

            this.pnlMedirFiltros.Dock = DockStyle.Top;
            this.pnlMedirFiltros.Height = 40;
            this.pnlMedirFiltros.Name = "pnlMedirFiltros";
            this.pnlMedirFiltros.Controls.Add(this.lblMedirDesde);
            this.pnlMedirFiltros.Controls.Add(this.dtpMedirDesde);
            this.pnlMedirFiltros.Controls.Add(this.lblMedirHasta);
            this.pnlMedirFiltros.Controls.Add(this.dtpMedirHasta);
            this.pnlMedirFiltros.Controls.Add(this.btnMedir);
            this.pnlMedirFiltros.Controls.Add(this.lblMedirResumen);

            this.lblMedirDesde.AutoSize = true;
            this.lblMedirDesde.Location = new Point(4, 12);
            this.lblMedirDesde.Name = "lblMedirDesde";
            this.lblMedirDesde.Text = "Desde:";

            this.dtpMedirDesde.Format = DateTimePickerFormat.Short;
            this.dtpMedirDesde.Location = new Point(54, 8);
            this.dtpMedirDesde.Name = "dtpMedirDesde";
            this.dtpMedirDesde.Size = new Size(105, 23);

            this.lblMedirHasta.AutoSize = true;
            this.lblMedirHasta.Location = new Point(168, 12);
            this.lblMedirHasta.Name = "lblMedirHasta";
            this.lblMedirHasta.Text = "Hasta:";

            this.dtpMedirHasta.Format = DateTimePickerFormat.Short;
            this.dtpMedirHasta.Location = new Point(216, 8);
            this.dtpMedirHasta.Name = "dtpMedirHasta";
            this.dtpMedirHasta.Size = new Size(105, 23);

            this.btnMedir.Location = new Point(332, 5);
            this.btnMedir.Name = "btnMedir";
            this.btnMedir.Size = new Size(130, 30);
            this.btnMedir.Text = "Medir";
            this.btnMedir.UseVisualStyleBackColor = true;
            this.btnMedir.Click += new System.EventHandler(this.BtnMedir_Click);

            this.lblMedirResumen.AutoSize = true;
            this.lblMedirResumen.Location = new Point(474, 12);
            this.lblMedirResumen.Name = "lblMedirResumen";

            this.splitMedir.Dock = DockStyle.Fill;
            this.splitMedir.Name = "splitMedir";
            this.splitMedir.Orientation = Orientation.Horizontal;
            this.splitMedir.Size = new Size(1224, 590);
            this.splitMedir.SplitterDistance = 220;
            this.splitMedir.Panel1.Controls.Add(this.dgvMedicion);
            this.splitMedir.Panel2.Controls.Add(this.chartMedicion);
            this.splitMedir.Panel2.Controls.Add(this.pnlMedirVeredicto);

            this.dgvMedicion.AllowUserToAddRows = false;
            this.dgvMedicion.AllowUserToDeleteRows = false;
            this.dgvMedicion.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvMedicion.Dock = DockStyle.Fill;
            this.dgvMedicion.MultiSelect = false;
            this.dgvMedicion.Name = "dgvMedicion";
            this.dgvMedicion.ReadOnly = true;
            this.dgvMedicion.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvMedicion.SelectionChanged += new System.EventHandler(this.DgvMedicion_SelectionChanged);

            areaMedicion.Name = "principal";
            leyendaMedicion.Name = "leyenda";
            this.chartMedicion.ChartAreas.Add(areaMedicion);
            this.chartMedicion.Legends.Add(leyendaMedicion);
            this.chartMedicion.Dock = DockStyle.Fill;
            // MSChart no admite ancho o alto 0 (pasa al escalar una pestaña que todavía no se mostró).
            this.chartMedicion.MinimumSize = new Size(120, 90);
            this.chartMedicion.Name = "chartMedicion";

            this.pnlMedirVeredicto.Dock = DockStyle.Right;
            this.pnlMedirVeredicto.Name = "pnlMedirVeredicto";
            this.pnlMedirVeredicto.Width = 380;
            this.pnlMedirVeredicto.BackColor = Tema.PanelClaro;
            this.pnlMedirVeredicto.Controls.Add(this.lblMedirExplicacion);
            this.pnlMedirVeredicto.Controls.Add(this.lblMedirVeredicto);
            this.pnlMedirVeredicto.Controls.Add(this.btnAplicarVeredicto);

            this.lblMedirVeredicto.Dock = DockStyle.Top;
            this.lblMedirVeredicto.Font = Tema.FuenteTitulo;
            this.lblMedirVeredicto.Height = 34;
            this.lblMedirVeredicto.Name = "lblMedirVeredicto";
            this.lblMedirVeredicto.Padding = new Padding(8, 8, 0, 0);

            this.lblMedirExplicacion.Dock = DockStyle.Fill;
            this.lblMedirExplicacion.Name = "lblMedirExplicacion";
            this.lblMedirExplicacion.Padding = new Padding(8, 4, 8, 4);

            this.btnAplicarVeredicto.Dock = DockStyle.Bottom;
            this.btnAplicarVeredicto.Height = 36;
            this.btnAplicarVeredicto.Name = "btnAplicarVeredicto";
            this.btnAplicarVeredicto.Text = "Aplicar veredicto";
            this.btnAplicarVeredicto.UseVisualStyleBackColor = true;
            this.btnAplicarVeredicto.Click += new System.EventHandler(this.BtnAplicarVeredicto_Click);

            // ══ CentroDecisionesForm ═══════════════════════════════════════════
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1240, 780);
            this.MinimumSize = new Size(1000, 620);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.pnlEncabezado);
            this.Font = Tema.FuenteNormal;
            this.Name = "CentroDecisionesForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Centro de decisiones comerciales";

            this.pnlEncabezado.ResumeLayout(false);
            this.pnlEncabezado.PerformLayout();
            this.tlpGraficos.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartTendencia)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartRiesgo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartStock)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartRotacion)).EndInit();
            this.tabAnalizar.ResumeLayout(false);
            this.pnlRecsBarra.ResumeLayout(false);
            this.pnlRecsBarra.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecomendaciones)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOpciones)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiasPosponer)).EndInit();
            this.pnlAcciones.ResumeLayout(false);
            this.pnlAcciones.PerformLayout();
            this.pnlOpciones.ResumeLayout(false);
            this.splitRecs.Panel1.ResumeLayout(false);
            this.splitRecs.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRecs)).EndInit();
            this.splitRecs.ResumeLayout(false);
            this.tabRecomendaciones.ResumeLayout(false);
            this.pnlHistFiltros.ResumeLayout(false);
            this.pnlHistFiltros.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistorial)).EndInit();
            this.pnlHistAcciones.ResumeLayout(false);
            this.pnlHistAcciones.PerformLayout();
            this.tabHistorial.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numSimClientesPlan)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimObjetivo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimDescuento)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimDuracion)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimRetencion)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimHorizonte)).EndInit();
            this.pnlVeredicto.ResumeLayout(false);
            this.pnlSimEntradas.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartSensibilidad)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEscenarios)).EndInit();
            this.splitSim.Panel1.ResumeLayout(false);
            this.splitSim.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitSim)).EndInit();
            this.splitSim.ResumeLayout(false);
            this.tabSimulador.ResumeLayout(false);
            this.pnlMedirFiltros.ResumeLayout(false);
            this.pnlMedirFiltros.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMedicion)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartMedicion)).EndInit();
            this.pnlMedirVeredicto.ResumeLayout(false);
            this.splitMedir.Panel1.ResumeLayout(false);
            this.splitMedir.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMedir)).EndInit();
            this.splitMedir.ResumeLayout(false);
            this.tabMedir.ResumeLayout(false);
            this.tabs.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ToolTip tip;
        private Panel pnlEncabezado;
        private Label lblTitulo;
        private Label lblDesde;
        private DateTimePicker dtpDesde;
        private Label lblHasta;
        private DateTimePicker dtpHasta;
        private ComboBox cmbPreset;
        private Label lblPlan;
        private ComboBox cmbPlan;
        private Button btnAnalizar;
        private Button btnUmbrales;
        private Button btnGlosario;
        private Button btnInforme;
        private Label lblComparacion;
        private Label lblMensaje;
        private TabControl tabs;
        private TabPage tabAnalizar;
        private FlowLayoutPanel flpKpis;
        private Label lblAvisos;
        private TableLayoutPanel tlpGraficos;
        private Chart chartTendencia;
        private Chart chartRiesgo;
        private Chart chartStock;
        private Chart chartRotacion;
        private TabPage tabRecomendaciones;
        private Panel pnlRecsBarra;
        private Label lblRecsResumen;
        private CheckBox chkMostrarOcultas;
        private Button btnRecsCsv;
        private SplitContainer splitRecs;
        private DataGridView dgvRecomendaciones;
        private RichTextBox txtDetalle;
        private Panel pnlOpciones;
        private Label lblOpciones;
        private DataGridView dgvOpciones;
        private Panel pnlAcciones;
        private Button btnAceptar;
        private Button btnDescartar;
        private Button btnPosponer;
        private NumericUpDownLocal numDiasPosponer;
        private Label lblDias;
        private Button btnSimularOpcion;
        private TabPage tabHistorial;
        private Panel pnlHistFiltros;
        private Label lblHistDesde;
        private DateTimePicker dtpHistDesde;
        private Label lblHistHasta;
        private DateTimePicker dtpHistHasta;
        private Label lblHistEstado;
        private ComboBox cmbHistEstado;
        private Button btnHistFiltrar;
        private Button btnHistCsv;
        private DataGridView dgvHistorial;
        private Panel pnlHistAcciones;
        private Button btnRegistrarResultado;
        private Button btnHistDescartar;
        private Label lblHistResumen;
        private TabPage tabSimulador;
        private Panel pnlSimEntradas;
        private Label lblSimPlan;
        private ComboBox cmbSimPlan;
        private Label lblSimClientesPlan;
        private NumericUpDownLocal numSimClientesPlan;
        private Label lblSimObjetivo;
        private NumericUpDownLocal numSimObjetivo;
        private Label lblSimDescuento;
        private NumericUpDownLocal numSimDescuento;
        private Label lblSimDuracion;
        private NumericUpDownLocal numSimDuracion;
        private Label lblSimRetencion;
        private NumericUpDownLocal numSimRetencion;
        private Label lblSimHorizonte;
        private NumericUpDownLocal numSimHorizonte;
        private Panel pnlVeredicto;
        private Label lblVeredicto;
        private Label lblVeredictoExplicacion;
        private Label lblSimResultados;
        private Button btnSimAgregar;
        private Button btnSimLimpiar;
        private Button btnSimCrearSugerencia;
        private SplitContainer splitSim;
        private Chart chartSensibilidad;
        private DataGridView dgvEscenarios;
        private TabPage tabMedir;
        private Panel pnlMedirFiltros;
        private Label lblMedirDesde;
        private DateTimePicker dtpMedirDesde;
        private Label lblMedirHasta;
        private DateTimePicker dtpMedirHasta;
        private Button btnMedir;
        private Label lblMedirResumen;
        private SplitContainer splitMedir;
        private DataGridView dgvMedicion;
        private Chart chartMedicion;
        private Panel pnlMedirVeredicto;
        private Label lblMedirVeredicto;
        private Label lblMedirExplicacion;
        private Button btnAplicarVeredicto;
    }
}
