using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        // Nota: las tarjetas KPI (flowCards), el panel de Actividad Reciente y el botón de
        // configuración de la tarjeta de backup se arman en runtime según los PERMISOS del
        // usuario (ver ConstruirElementosCondicionales() en DashboardForm.cs) — no son
        // representables como controles fijos del Diseñador porque su EXISTENCIA (no solo su
        // visibilidad) depende de datos de sesión. El resto del formulario (header, footer,
        // panel de tareas, mini-stats, sesión) es estático y vive acá.
        private void InitializeComponent()
        {
            this.panelHeader     = new Panel();
            this.lblTitulo       = new Label();
            this.lblSub          = new Label();
            this.btnRefrescar    = new Button();
            this.flowCards       = new FlowLayoutPanel();
            this.lblAviso        = new Label();
            this.panelCentro     = new Panel();
            this.panelMiniStats  = new Panel();
            this.flStats         = new FlowLayoutPanel();
            this.lblStTitulo     = new Label();
            this.panelTareas     = new Panel();
            this.dgvTareas       = new DataGridView();
            this.colTipo         = new DataGridViewTextBoxColumn();
            this.colDesc         = new DataGridViewTextBoxColumn();
            this.colFecha        = new DataGridViewTextBoxColumn();
            this.lblTareasTitulo = new Label();
            this.panelSbar       = new Panel();
            this.lblSesion       = new Label();
            this.panelHeader.SuspendLayout();
            this.panelCentro.SuspendLayout();
            this.panelMiniStats.SuspendLayout();
            this.panelTareas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTareas)).BeginInit();
            this.panelSbar.SuspendLayout();
            this.SuspendLayout();

            // ── panelHeader ────────────────────────────────────────────────────
            this.panelHeader.BackColor = Color.FromArgb(176, 62, 96);
            this.panelHeader.Controls.Add(this.lblTitulo);
            this.panelHeader.Controls.Add(this.lblSub);
            this.panelHeader.Controls.Add(this.btnRefrescar);
            this.panelHeader.Dock     = DockStyle.Top;
            this.panelHeader.Location = new Point(0, 0);
            this.panelHeader.Name     = "panelHeader";
            this.panelHeader.Size     = new Size(870, 62);
            this.panelHeader.TabIndex = 0;
            this.panelHeader.Paint  += new PaintEventHandler(this.PanelHeader_Paint);
            this.panelHeader.Resize += new System.EventHandler(this.PanelHeader_Resize);

            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.BackColor = Color.Transparent;
            this.lblTitulo.Font      = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitulo.ForeColor = Color.White;
            this.lblTitulo.Location  = new Point(14, 8);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Text      = "Panel de Control";

            this.lblSub.AutoSize  = true;
            this.lblSub.BackColor = Color.Transparent;
            this.lblSub.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
            this.lblSub.ForeColor = Color.FromArgb(200, 255, 200, 220);
            this.lblSub.Location  = new Point(14, 36);
            this.lblSub.Name      = "lblSub";
            this.lblSub.TabIndex  = 1;
            this.lblSub.Text      = "WardrobeFlow";

            this.btnRefrescar.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRefrescar.BackColor = Color.FromArgb(210, 100, 135);
            this.btnRefrescar.Cursor    = Cursors.Hand;
            this.btnRefrescar.FlatAppearance.BorderColor = Color.FromArgb(180, 230, 140, 170);
            this.btnRefrescar.FlatAppearance.BorderSize  = 1;
            this.btnRefrescar.FlatStyle = FlatStyle.Flat;
            this.btnRefrescar.Font      = new Font("Segoe UI", 8.5F);
            this.btnRefrescar.ForeColor = Color.White;
            this.btnRefrescar.Location  = new Point(88, 17);
            this.btnRefrescar.Name      = "btnRefrescar";
            this.btnRefrescar.Size      = new Size(100, 28);
            this.btnRefrescar.TabIndex  = 2;
            this.btnRefrescar.Text      = "↻  Actualizar";
            this.btnRefrescar.UseVisualStyleBackColor = false;
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);

            // ── flowCards ──────────────────────────────────────────────────────
            this.flowCards.BackColor     = Color.FromArgb(240, 240, 245);
            this.flowCards.Dock          = DockStyle.Top;
            this.flowCards.FlowDirection = FlowDirection.LeftToRight;
            this.flowCards.Height        = 168;
            this.flowCards.Location      = new Point(0, 62);
            this.flowCards.Name          = "flowCards";
            this.flowCards.Padding       = new Padding(10, 10, 10, 4);
            this.flowCards.Size          = new Size(870, 168);
            this.flowCards.TabIndex      = 1;
            this.flowCards.WrapContents  = false;
            this.flowCards.Resize += new System.EventHandler(this.FlowCards_Resize);

            // ── lblAviso ───────────────────────────────────────────────────────
            this.lblAviso.AutoSize = false;
            this.lblAviso.Dock     = DockStyle.Top;
            this.lblAviso.Font     = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.lblAviso.Height   = 0;
            this.lblAviso.Location = new Point(0, 230);
            this.lblAviso.Name     = "lblAviso";
            this.lblAviso.Padding  = new Padding(12, 0, 0, 0);
            this.lblAviso.Size     = new Size(870, 0);
            this.lblAviso.TabIndex = 2;
            this.lblAviso.Visible  = false;

            // ── panelCentro ────────────────────────────────────────────────────
            this.panelCentro.BackColor = Color.FromArgb(240, 240, 245);
            this.panelCentro.Controls.Add(this.panelMiniStats);
            this.panelCentro.Dock     = DockStyle.Fill;
            this.panelCentro.Location = new Point(0, 230);
            this.panelCentro.Name     = "panelCentro";
            this.panelCentro.Size     = new Size(870, 118);
            this.panelCentro.TabIndex = 3;
            this.panelCentro.Resize += new System.EventHandler(this.PanelCentro_Resize);

            // ── panelMiniStats ─────────────────────────────────────────────────
            this.panelMiniStats.BackColor = Color.FromArgb(248, 244, 250);
            this.panelMiniStats.Controls.Add(this.flStats);
            this.panelMiniStats.Controls.Add(this.lblStTitulo);
            this.panelMiniStats.Dock     = DockStyle.Fill;
            this.panelMiniStats.Location = new Point(0, 0);
            this.panelMiniStats.Name     = "panelMiniStats";
            this.panelMiniStats.Padding  = new Padding(8);
            this.panelMiniStats.Size     = new Size(870, 118);
            this.panelMiniStats.TabIndex = 0;

            this.flStats.BackColor     = Color.Transparent;
            this.flStats.Dock          = DockStyle.Fill;
            this.flStats.FlowDirection = FlowDirection.TopDown;
            this.flStats.Location      = new Point(8, 36);
            this.flStats.Name          = "flStats";
            this.flStats.Padding       = new Padding(6, 4, 6, 4);
            this.flStats.Size          = new Size(854, 74);
            this.flStats.TabIndex      = 1;
            this.flStats.WrapContents  = false;

            this.lblStTitulo.BackColor = Color.FromArgb(252, 240, 246);
            this.lblStTitulo.Dock      = DockStyle.Top;
            this.lblStTitulo.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblStTitulo.ForeColor = Color.FromArgb(176, 62, 96);
            this.lblStTitulo.Height    = 28;
            this.lblStTitulo.Location  = new Point(8, 8);
            this.lblStTitulo.Name      = "lblStTitulo";
            this.lblStTitulo.Padding   = new Padding(4, 6, 0, 0);
            this.lblStTitulo.Size      = new Size(854, 28);
            this.lblStTitulo.TabIndex  = 0;
            this.lblStTitulo.Text      = "Resumen de eventos";

            // ── panelTareas ────────────────────────────────────────────────────
            this.panelTareas.BackColor = Color.White;
            this.panelTareas.Controls.Add(this.dgvTareas);
            this.panelTareas.Controls.Add(this.lblTareasTitulo);
            this.panelTareas.Dock     = DockStyle.Top;
            this.panelTareas.Height   = 140;
            this.panelTareas.Location = new Point(0, 230);
            this.panelTareas.Name     = "panelTareas";
            this.panelTareas.Size     = new Size(870, 140);
            this.panelTareas.TabIndex = 4;

            this.dgvTareas.AllowUserToAddRows    = false;
            this.dgvTareas.AllowUserToResizeRows = false;
            this.dgvTareas.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvTareas.BackgroundColor       = Color.White;
            this.dgvTareas.BorderStyle           = BorderStyle.None;
            this.dgvTareas.CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvTareas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(176, 62, 96);
            this.dgvTareas.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8F, FontStyle.Bold);
            this.dgvTareas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            this.dgvTareas.Columns.AddRange(new DataGridViewColumn[] {
                this.colTipo, this.colDesc, this.colFecha });
            this.dgvTareas.Dock                      = DockStyle.Fill;
            this.dgvTareas.EnableHeadersVisualStyles  = false;
            this.dgvTareas.Font                       = new Font("Segoe UI", 8F);
            this.dgvTareas.GridColor                  = Color.FromArgb(235, 225, 232);
            this.dgvTareas.Location                   = new Point(0, 26);
            this.dgvTareas.Name                       = "dgvTareas";
            this.dgvTareas.ReadOnly                   = true;
            this.dgvTareas.RowHeadersVisible          = false;
            this.dgvTareas.SelectionMode              = DataGridViewSelectionMode.FullRowSelect;
            this.dgvTareas.Size                       = new Size(870, 114);
            this.dgvTareas.TabIndex                   = 1;
            this.dgvTareas.CellClick += new DataGridViewCellEventHandler(this.DgvTareas_CellClick);

            this.colTipo.FillWeight = 22F;
            this.colTipo.HeaderText = "Tipo";
            this.colTipo.Name       = "colTipo";
            this.colTipo.ReadOnly   = true;

            this.colDesc.FillWeight = 56F;
            this.colDesc.HeaderText = "Descripción";
            this.colDesc.Name       = "colDesc";
            this.colDesc.ReadOnly   = true;

            this.colFecha.FillWeight = 22F;
            this.colFecha.HeaderText = "Desde";
            this.colFecha.Name       = "colFecha";
            this.colFecha.ReadOnly   = true;

            this.lblTareasTitulo.BackColor = Color.FromArgb(252, 240, 248);
            this.lblTareasTitulo.Dock      = DockStyle.Top;
            this.lblTareasTitulo.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblTareasTitulo.ForeColor = Color.FromArgb(176, 62, 96);
            this.lblTareasTitulo.Height    = 26;
            this.lblTareasTitulo.Location  = new Point(0, 0);
            this.lblTareasTitulo.Name      = "lblTareasTitulo";
            this.lblTareasTitulo.Padding   = new Padding(10, 5, 0, 0);
            this.lblTareasTitulo.Size      = new Size(870, 26);
            this.lblTareasTitulo.TabIndex  = 0;
            this.lblTareasTitulo.Text      = "Mis Tareas Pendientes";

            // ── panelSbar ──────────────────────────────────────────────────────
            this.panelSbar.BackColor = Color.FromArgb(176, 62, 96);
            this.panelSbar.Controls.Add(this.lblSesion);
            this.panelSbar.Dock     = DockStyle.Bottom;
            this.panelSbar.Height   = 26;
            this.panelSbar.Location = new Point(0, 544);
            this.panelSbar.Name     = "panelSbar";
            this.panelSbar.Size     = new Size(870, 26);
            this.panelSbar.TabIndex = 5;

            this.lblSesion.Dock      = DockStyle.Fill;
            this.lblSesion.Font      = new Font("Segoe UI", 8F);
            this.lblSesion.ForeColor = Color.FromArgb(244, 212, 226);
            this.lblSesion.Location  = new Point(0, 0);
            this.lblSesion.Name      = "lblSesion";
            this.lblSesion.Padding   = new Padding(10, 0, 0, 0);
            this.lblSesion.Size      = new Size(870, 26);
            this.lblSesion.TabIndex  = 0;
            this.lblSesion.TextAlign = ContentAlignment.MiddleLeft;

            // ── DashboardForm ──────────────────────────────────────────────────
            this.BackColor       = Color.FromArgb(240, 240, 245);
            this.Controls.Add(this.panelCentro);
            this.Controls.Add(this.panelTareas);
            this.Controls.Add(this.lblAviso);
            this.Controls.Add(this.flowCards);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelSbar);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Location        = new Point(10, 10);
            this.MinimumSize     = new Size(600, 400);
            this.Name            = "DashboardForm";
            this.Size            = new Size(870, 570);
            this.StartPosition   = FormStartPosition.Manual;
            this.Text             = "Panel de Control";

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelCentro.ResumeLayout(false);
            this.panelMiniStats.ResumeLayout(false);
            this.panelTareas.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTareas)).EndInit();
            this.panelSbar.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private Panel    panelHeader;
        private Label    lblTitulo;
        private Label    lblSub;
        private Button   btnRefrescar;
        private FlowLayoutPanel flowCards;
        private Label    lblAviso;
        private Panel    panelCentro;
        private Panel    panelMiniStats;
        private FlowLayoutPanel flStats;
        private Label    lblStTitulo;
        private Panel    panelTareas;
        private DataGridView dgvTareas;
        private DataGridViewTextBoxColumn colTipo;
        private DataGridViewTextBoxColumn colDesc;
        private DataGridViewTextBoxColumn colFecha;
        private Label    lblTareasTitulo;
        private Panel    panelSbar;
        private Label    lblSesion;
    }
}
