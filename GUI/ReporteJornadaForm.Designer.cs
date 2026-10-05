namespace GUI
{
    partial class ReporteJornadaForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuExportar = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuGuardarTxt = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuImprimir = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuGuardarCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExportarComp = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuGuardarComp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuImprimirComp = new System.Windows.Forms.ToolStripMenuItem();
            this.panelKpiBanner = new System.Windows.Forms.Panel();
            this.kpiPrendasVal = new System.Windows.Forms.Label();
            this.kpiClientesVal = new System.Windows.Forms.Label();
            this.kpiEventosVal = new System.Windows.Forms.Label();
            this.kpiBackupVal = new System.Windows.Forms.Label();
            this.kpiPrendasLbl = new System.Windows.Forms.Label();
            this.kpiClientesLbl = new System.Windows.Forms.Label();
            this.kpiEventosLbl = new System.Windows.Forms.Label();
            this.kpiBackupLbl = new System.Windows.Forms.Label();
            this.btnTendencia = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblSubtitulo = new System.Windows.Forms.Label();
            this.panelControles = new System.Windows.Forms.Panel();
            this.lblJornada = new System.Windows.Forms.Label();
            this.dtpJornada = new System.Windows.Forms.DateTimePicker();
            this.btnGenerar = new System.Windows.Forms.Button();
            this.btnExportar = new System.Windows.Forms.Button();
            this.lblComparar = new System.Windows.Forms.Label();
            this.dtpJornada2 = new System.Windows.Forms.DateTimePicker();
            this.btnComparar = new System.Windows.Forms.Button();
            this.btnExportarComp = new System.Windows.Forms.Button();
            this.btnLimpiar = new System.Windows.Forms.Button();
            this.rtbReporte = new System.Windows.Forms.RichTextBox();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.menuExportar.SuspendLayout();
            this.menuExportarComp.SuspendLayout();
            this.panelKpiBanner.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.panelControles.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuExportar
            // 
            this.menuExportar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuGuardarTxt,
            this.mnuImprimir,
            this.mnuGuardarCsv});
            this.menuExportar.Name = "menuExportar";
            this.menuExportar.Size = new System.Drawing.Size(222, 70);
            // 
            // mnuGuardarTxt
            // 
            this.mnuGuardarTxt.Name = "mnuGuardarTxt";
            this.mnuGuardarTxt.Size = new System.Drawing.Size(221, 22);
            this.mnuGuardarTxt.Text = "Guardar como .TXT";
            this.mnuGuardarTxt.Click += new System.EventHandler(this.MnuGuardarTxt_Click);
            // 
            // mnuImprimir
            // 
            this.mnuImprimir.Name = "mnuImprimir";
            this.mnuImprimir.Size = new System.Drawing.Size(221, 22);
            this.mnuImprimir.Text = "Imprimir / Exportar PDF";
            this.mnuImprimir.Click += new System.EventHandler(this.MnuImprimir_Click);
            // 
            // mnuGuardarCsv
            // 
            this.mnuGuardarCsv.Name = "mnuGuardarCsv";
            this.mnuGuardarCsv.Size = new System.Drawing.Size(221, 22);
            this.mnuGuardarCsv.Text = "Guardar eventos como .CSV";
            this.mnuGuardarCsv.Click += new System.EventHandler(this.MnuGuardarCsv_Click);
            // 
            // menuExportarComp
            // 
            this.menuExportarComp.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuGuardarComp,
            this.mnuImprimirComp});
            this.menuExportarComp.Name = "menuExportarComp";
            this.menuExportarComp.Size = new System.Drawing.Size(251, 48);
            // 
            // mnuGuardarComp
            // 
            this.mnuGuardarComp.Name = "mnuGuardarComp";
            this.mnuGuardarComp.Size = new System.Drawing.Size(250, 22);
            this.mnuGuardarComp.Text = "Guardar comparación como .TXT";
            this.mnuGuardarComp.Click += new System.EventHandler(this.MnuGuardarComp_Click);
            // 
            // mnuImprimirComp
            // 
            this.mnuImprimirComp.Name = "mnuImprimirComp";
            this.mnuImprimirComp.Size = new System.Drawing.Size(250, 22);
            this.mnuImprimirComp.Text = "Imprimir / Exportar PDF";
            this.mnuImprimirComp.Click += new System.EventHandler(this.MnuImprimir_Click);
            // 
            // panelKpiBanner
            // 
            this.panelKpiBanner.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(236)))), ((int)(((byte)(244)))));
            this.panelKpiBanner.Controls.Add(this.kpiPrendasVal);
            this.panelKpiBanner.Controls.Add(this.kpiClientesVal);
            this.panelKpiBanner.Controls.Add(this.kpiEventosVal);
            this.panelKpiBanner.Controls.Add(this.kpiBackupVal);
            this.panelKpiBanner.Controls.Add(this.kpiPrendasLbl);
            this.panelKpiBanner.Controls.Add(this.kpiClientesLbl);
            this.panelKpiBanner.Controls.Add(this.kpiEventosLbl);
            this.panelKpiBanner.Controls.Add(this.kpiBackupLbl);
            this.panelKpiBanner.Location = new System.Drawing.Point(10, 160);
            this.panelKpiBanner.Name = "panelKpiBanner";
            this.panelKpiBanner.Size = new System.Drawing.Size(940, 66);
            this.panelKpiBanner.TabIndex = 9;
            this.panelKpiBanner.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelKpiBanner_Paint);
            // 
            // kpiPrendasVal
            // 
            this.kpiPrendasVal.BackColor = System.Drawing.Color.Transparent;
            this.kpiPrendasVal.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.kpiPrendasVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(30)))), ((int)(((byte)(55)))));
            this.kpiPrendasVal.Location = new System.Drawing.Point(0, 5);
            this.kpiPrendasVal.Name = "kpiPrendasVal";
            this.kpiPrendasVal.Size = new System.Drawing.Size(235, 34);
            this.kpiPrendasVal.TabIndex = 0;
            this.kpiPrendasVal.Text = "—";
            this.kpiPrendasVal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // kpiClientesVal
            // 
            this.kpiClientesVal.BackColor = System.Drawing.Color.Transparent;
            this.kpiClientesVal.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.kpiClientesVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(30)))), ((int)(((byte)(55)))));
            this.kpiClientesVal.Location = new System.Drawing.Point(235, 5);
            this.kpiClientesVal.Name = "kpiClientesVal";
            this.kpiClientesVal.Size = new System.Drawing.Size(235, 34);
            this.kpiClientesVal.TabIndex = 1;
            this.kpiClientesVal.Text = "—";
            this.kpiClientesVal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // kpiEventosVal
            // 
            this.kpiEventosVal.BackColor = System.Drawing.Color.Transparent;
            this.kpiEventosVal.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.kpiEventosVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(30)))), ((int)(((byte)(55)))));
            this.kpiEventosVal.Location = new System.Drawing.Point(470, 5);
            this.kpiEventosVal.Name = "kpiEventosVal";
            this.kpiEventosVal.Size = new System.Drawing.Size(235, 34);
            this.kpiEventosVal.TabIndex = 2;
            this.kpiEventosVal.Text = "—";
            this.kpiEventosVal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // kpiBackupVal
            // 
            this.kpiBackupVal.BackColor = System.Drawing.Color.Transparent;
            this.kpiBackupVal.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.kpiBackupVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(30)))), ((int)(((byte)(55)))));
            this.kpiBackupVal.Location = new System.Drawing.Point(705, 5);
            this.kpiBackupVal.Name = "kpiBackupVal";
            this.kpiBackupVal.Size = new System.Drawing.Size(235, 34);
            this.kpiBackupVal.TabIndex = 3;
            this.kpiBackupVal.Text = "—";
            this.kpiBackupVal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // kpiPrendasLbl
            // 
            this.kpiPrendasLbl.BackColor = System.Drawing.Color.Transparent;
            this.kpiPrendasLbl.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.kpiPrendasLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.kpiPrendasLbl.Location = new System.Drawing.Point(0, 39);
            this.kpiPrendasLbl.Name = "kpiPrendasLbl";
            this.kpiPrendasLbl.Size = new System.Drawing.Size(235, 20);
            this.kpiPrendasLbl.TabIndex = 4;
            this.kpiPrendasLbl.Text = "Prendas disponibles";
            this.kpiPrendasLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // kpiClientesLbl
            // 
            this.kpiClientesLbl.BackColor = System.Drawing.Color.Transparent;
            this.kpiClientesLbl.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.kpiClientesLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.kpiClientesLbl.Location = new System.Drawing.Point(235, 39);
            this.kpiClientesLbl.Name = "kpiClientesLbl";
            this.kpiClientesLbl.Size = new System.Drawing.Size(235, 20);
            this.kpiClientesLbl.TabIndex = 5;
            this.kpiClientesLbl.Text = "Clientes registrados";
            this.kpiClientesLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // kpiEventosLbl
            // 
            this.kpiEventosLbl.BackColor = System.Drawing.Color.Transparent;
            this.kpiEventosLbl.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.kpiEventosLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.kpiEventosLbl.Location = new System.Drawing.Point(470, 39);
            this.kpiEventosLbl.Name = "kpiEventosLbl";
            this.kpiEventosLbl.Size = new System.Drawing.Size(235, 20);
            this.kpiEventosLbl.TabIndex = 6;
            this.kpiEventosLbl.Text = "Eventos del día";
            this.kpiEventosLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // kpiBackupLbl
            // 
            this.kpiBackupLbl.BackColor = System.Drawing.Color.Transparent;
            this.kpiBackupLbl.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.kpiBackupLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.kpiBackupLbl.Location = new System.Drawing.Point(705, 39);
            this.kpiBackupLbl.Name = "kpiBackupLbl";
            this.kpiBackupLbl.Size = new System.Drawing.Size(235, 20);
            this.kpiBackupLbl.TabIndex = 7;
            this.kpiBackupLbl.Text = "días sin backup";
            this.kpiBackupLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnTendencia
            // 
            this.btnTendencia.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(70)))), ((int)(((byte)(105)))));
            this.btnTendencia.FlatAppearance.BorderSize = 0;
            this.btnTendencia.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTendencia.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnTendencia.ForeColor = System.Drawing.Color.White;
            this.btnTendencia.Location = new System.Drawing.Point(545, 12);
            this.btnTendencia.Name = "btnTendencia";
            this.btnTendencia.Size = new System.Drawing.Size(175, 28);
            this.btnTendencia.TabIndex = 9;
            this.btnTendencia.Text = "Tendencia (rango)";
            this.btnTendencia.UseVisualStyleBackColor = false;
            this.btnTendencia.Click += new System.EventHandler(this.BtnTendencia_Click);
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.panelTop.Controls.Add(this.lblTitulo);
            this.panelTop.Controls.Add(this.lblSubtitulo);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(960, 56);
            this.panelTop.TabIndex = 0;
            this.panelTop.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelTop_Paint);
            // 
            // lblTitulo
            // 
            this.lblTitulo.BackColor = System.Drawing.Color.Transparent;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.White;
            this.lblTitulo.Location = new System.Drawing.Point(12, 6);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(700, 26);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Reporte de Jornada";
            // 
            // lblSubtitulo
            // 
            this.lblSubtitulo.BackColor = System.Drawing.Color.Transparent;
            this.lblSubtitulo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblSubtitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(200)))), ((int)(((byte)(218)))));
            this.lblSubtitulo.Location = new System.Drawing.Point(12, 34);
            this.lblSubtitulo.Name = "lblSubtitulo";
            this.lblSubtitulo.Size = new System.Drawing.Size(900, 16);
            this.lblSubtitulo.TabIndex = 1;
            this.lblSubtitulo.Text = "Eventos de negocio por jornada con exportación a TXT";
            // 
            // panelControles
            // 
            this.panelControles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(220)))), ((int)(((byte)(230)))));
            this.panelControles.Controls.Add(this.lblJornada);
            this.panelControles.Controls.Add(this.dtpJornada);
            this.panelControles.Controls.Add(this.btnGenerar);
            this.panelControles.Controls.Add(this.btnExportar);
            this.panelControles.Controls.Add(this.lblComparar);
            this.panelControles.Controls.Add(this.dtpJornada2);
            this.panelControles.Controls.Add(this.btnComparar);
            this.panelControles.Controls.Add(this.btnExportarComp);
            this.panelControles.Controls.Add(this.btnLimpiar);
            this.panelControles.Controls.Add(this.btnTendencia);
            this.panelControles.Location = new System.Drawing.Point(10, 66);
            this.panelControles.Name = "panelControles";
            this.panelControles.Size = new System.Drawing.Size(940, 90);
            this.panelControles.TabIndex = 1;
            // 
            // lblJornada
            // 
            this.lblJornada.AutoSize = true;
            this.lblJornada.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblJornada.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(20)))), ((int)(((byte)(42)))));
            this.lblJornada.Location = new System.Drawing.Point(10, 16);
            this.lblJornada.Name = "lblJornada";
            this.lblJornada.Size = new System.Drawing.Size(53, 15);
            this.lblJornada.TabIndex = 0;
            this.lblJornada.Text = "Jornada:";
            // 
            // dtpJornada
            // 
            this.dtpJornada.CustomFormat = "dd\'/\'MM\'/\'yyyy";
            this.dtpJornada.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dtpJornada.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpJornada.Location = new System.Drawing.Point(78, 13);
            this.dtpJornada.Name = "dtpJornada";
            this.dtpJornada.Size = new System.Drawing.Size(120, 23);
            this.dtpJornada.TabIndex = 1;
            this.dtpJornada.ValueChanged += new System.EventHandler(this.dtpJornada_ValueChanged);
            // 
            // btnGenerar
            // 
            this.btnGenerar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.btnGenerar.FlatAppearance.BorderSize = 0;
            this.btnGenerar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGenerar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnGenerar.ForeColor = System.Drawing.Color.White;
            this.btnGenerar.Location = new System.Drawing.Point(212, 12);
            this.btnGenerar.Name = "btnGenerar";
            this.btnGenerar.Size = new System.Drawing.Size(145, 28);
            this.btnGenerar.TabIndex = 2;
            this.btnGenerar.Text = "Generar";
            this.btnGenerar.UseVisualStyleBackColor = false;
            this.btnGenerar.Click += new System.EventHandler(this.btnGenerar_Click);
            // 
            // btnExportar
            // 
            this.btnExportar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(30)))), ((int)(((byte)(55)))));
            this.btnExportar.FlatAppearance.BorderSize = 0;
            this.btnExportar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnExportar.ForeColor = System.Drawing.Color.White;
            this.btnExportar.Location = new System.Drawing.Point(368, 12);
            this.btnExportar.Name = "btnExportar";
            this.btnExportar.Size = new System.Drawing.Size(165, 28);
            this.btnExportar.TabIndex = 3;
            this.btnExportar.Text = "Exportar reporte...";
            this.btnExportar.UseVisualStyleBackColor = false;
            this.btnExportar.Click += new System.EventHandler(this.btnExportar_Click);
            // 
            // lblComparar
            // 
            this.lblComparar.AutoSize = true;
            this.lblComparar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblComparar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(20)))), ((int)(((byte)(42)))));
            this.lblComparar.Location = new System.Drawing.Point(10, 57);
            this.lblComparar.Name = "lblComparar";
            this.lblComparar.Size = new System.Drawing.Size(87, 15);
            this.lblComparar.TabIndex = 4;
            this.lblComparar.Text = "Comparar con:";
            // 
            // dtpJornada2
            // 
            this.dtpJornada2.CustomFormat = "dd\'/\'MM\'/\'yyyy";
            this.dtpJornada2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dtpJornada2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpJornada2.Location = new System.Drawing.Point(112, 53);
            this.dtpJornada2.Name = "dtpJornada2";
            this.dtpJornada2.Size = new System.Drawing.Size(120, 23);
            this.dtpJornada2.TabIndex = 5;
            // 
            // btnComparar
            // 
            this.btnComparar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(85)))), ((int)(((byte)(120)))));
            this.btnComparar.FlatAppearance.BorderSize = 0;
            this.btnComparar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnComparar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnComparar.ForeColor = System.Drawing.Color.White;
            this.btnComparar.Location = new System.Drawing.Point(246, 51);
            this.btnComparar.Name = "btnComparar";
            this.btnComparar.Size = new System.Drawing.Size(185, 28);
            this.btnComparar.TabIndex = 6;
            this.btnComparar.Text = "Comparar Jornadas";
            this.btnComparar.UseVisualStyleBackColor = false;
            this.btnComparar.Click += new System.EventHandler(this.btnComparar_Click);
            // 
            // btnExportarComp
            // 
            this.btnExportarComp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(30)))), ((int)(((byte)(55)))));
            this.btnExportarComp.FlatAppearance.BorderSize = 0;
            this.btnExportarComp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportarComp.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnExportarComp.ForeColor = System.Drawing.Color.White;
            this.btnExportarComp.Location = new System.Drawing.Point(442, 51);
            this.btnExportarComp.Name = "btnExportarComp";
            this.btnExportarComp.Size = new System.Drawing.Size(185, 28);
            this.btnExportarComp.TabIndex = 7;
            this.btnExportarComp.Text = "Exportar comparación...";
            this.btnExportarComp.UseVisualStyleBackColor = false;
            this.btnExportarComp.Click += new System.EventHandler(this.btnExportarComp_Click);
            // 
            // btnLimpiar
            // 
            this.btnLimpiar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(215)))), ((int)(((byte)(185)))), ((int)(((byte)(200)))));
            this.btnLimpiar.FlatAppearance.BorderSize = 0;
            this.btnLimpiar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLimpiar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnLimpiar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(72)))), ((int)(((byte)(28)))), ((int)(((byte)(50)))));
            this.btnLimpiar.Location = new System.Drawing.Point(640, 51);
            this.btnLimpiar.Name = "btnLimpiar";
            this.btnLimpiar.Size = new System.Drawing.Size(175, 28);
            this.btnLimpiar.TabIndex = 8;
            this.btnLimpiar.Text = "Volver al reporte";
            this.btnLimpiar.UseVisualStyleBackColor = false;
            this.btnLimpiar.Click += new System.EventHandler(this.btnLimpiar_Click);
            // 
            // rtbReporte
            // 
            this.rtbReporte.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbReporte.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(250)))), ((int)(((byte)(253)))));
            this.rtbReporte.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbReporte.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.rtbReporte.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.rtbReporte.Location = new System.Drawing.Point(10, 230);
            this.rtbReporte.Name = "rtbReporte";
            this.rtbReporte.ReadOnly = true;
            this.rtbReporte.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbReporte.Size = new System.Drawing.Size(940, 330);
            this.rtbReporte.TabIndex = 0;
            this.rtbReporte.Text = "";
            // 
            // panelStatus
            // 
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(220)))), ((int)(((byte)(228)))));
            this.panelStatus.Controls.Add(this.lblStatus);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 564);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(960, 28);
            this.panelStatus.TabIndex = 2;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(45)))), ((int)(((byte)(68)))));
            this.lblStatus.Location = new System.Drawing.Point(8, 6);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(35, 15);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Listo.";
            // 
            // ReporteJornadaForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(240)))), ((int)(((byte)(246)))));
            this.ClientSize = new System.Drawing.Size(960, 592);
            this.Controls.Add(this.rtbReporte);
            this.Controls.Add(this.panelKpiBanner);
            this.Controls.Add(this.panelControles);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.Name = "ReporteJornadaForm";
            this.Text = "Reporte de Jornada";
            this.Load += new System.EventHandler(this.ReporteJornadaForm_Load);
            this.menuExportar.ResumeLayout(false);
            this.menuExportarComp.ResumeLayout(false);
            this.panelKpiBanner.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelControles.ResumeLayout(false);
            this.panelControles.PerformLayout();
            this.panelStatus.ResumeLayout(false);
            this.panelStatus.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel          panelTop;
        private System.Windows.Forms.Label          lblTitulo;
        private System.Windows.Forms.Label          lblSubtitulo;
        private System.Windows.Forms.Panel          panelControles;
        private System.Windows.Forms.Label          lblJornada;
        private System.Windows.Forms.DateTimePicker dtpJornada;
        private System.Windows.Forms.Button         btnGenerar;
        private System.Windows.Forms.Button         btnExportar;
        private System.Windows.Forms.Label          lblComparar;
        private System.Windows.Forms.DateTimePicker dtpJornada2;
        private System.Windows.Forms.Button         btnComparar;
        private System.Windows.Forms.Button         btnExportarComp;
        private System.Windows.Forms.Button         btnLimpiar;
        private System.Windows.Forms.RichTextBox    rtbReporte;
        private System.Windows.Forms.Panel          panelStatus;
        private System.Windows.Forms.Label          lblStatus;
        private System.Windows.Forms.ContextMenuStrip menuExportar;
        private System.Windows.Forms.ToolStripMenuItem mnuGuardarTxt;
        private System.Windows.Forms.ToolStripMenuItem mnuImprimir;
        private System.Windows.Forms.ToolStripMenuItem mnuGuardarCsv;
        private System.Windows.Forms.ContextMenuStrip menuExportarComp;
        private System.Windows.Forms.ToolStripMenuItem mnuGuardarComp;
        private System.Windows.Forms.ToolStripMenuItem mnuImprimirComp;
        private System.Windows.Forms.Panel          panelKpiBanner;
        private System.Windows.Forms.Label          kpiPrendasVal;
        private System.Windows.Forms.Label          kpiClientesVal;
        private System.Windows.Forms.Label          kpiEventosVal;
        private System.Windows.Forms.Label          kpiBackupVal;
        private System.Windows.Forms.Label          kpiPrendasLbl;
        private System.Windows.Forms.Label          kpiClientesLbl;
        private System.Windows.Forms.Label          kpiEventosLbl;
        private System.Windows.Forms.Label          kpiBackupLbl;
        private System.Windows.Forms.Button         btnTendencia;
    }
}
