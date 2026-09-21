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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblSub = new System.Windows.Forms.Label();
            this.btnRefrescar = new System.Windows.Forms.Button();
            this.flowCards = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAviso = new System.Windows.Forms.Label();
            this.panelCentro = new System.Windows.Forms.Panel();
            this.panelMiniStats = new System.Windows.Forms.Panel();
            this.flStats = new System.Windows.Forms.FlowLayoutPanel();
            this.lblStTitulo = new System.Windows.Forms.Label();
            this.panelTareas = new System.Windows.Forms.Panel();
            this.dgvTareas = new System.Windows.Forms.DataGridView();
            this.colTipo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFecha = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblTareasTitulo = new System.Windows.Forms.Label();
            this.panelSbar = new System.Windows.Forms.Panel();
            this.lblSesion = new System.Windows.Forms.Label();
            this.panelHeader.SuspendLayout();
            this.panelCentro.SuspendLayout();
            this.panelMiniStats.SuspendLayout();
            this.panelTareas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTareas)).BeginInit();
            this.panelSbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.panelHeader.Controls.Add(this.lblTitulo);
            this.panelHeader.Controls.Add(this.lblSub);
            this.panelHeader.Controls.Add(this.btnRefrescar);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(854, 62);
            this.panelHeader.TabIndex = 0;
            this.panelHeader.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelHeader_Paint);
            this.panelHeader.Resize += new System.EventHandler(this.PanelHeader_Resize);
            // 
            // lblTitulo
            // 
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.BackColor = System.Drawing.Color.Transparent;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.White;
            this.lblTitulo.Location = new System.Drawing.Point(14, 8);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(161, 25);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Panel de Control";
            // 
            // lblSub
            // 
            this.lblSub.AutoSize = true;
            this.lblSub.BackColor = System.Drawing.Color.Transparent;
            this.lblSub.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(255)))), ((int)(((byte)(200)))), ((int)(((byte)(220)))));
            this.lblSub.Location = new System.Drawing.Point(14, 36);
            this.lblSub.Name = "lblSub";
            this.lblSub.Size = new System.Drawing.Size(76, 13);
            this.lblSub.TabIndex = 1;
            this.lblSub.Text = "WardrobeFlow";
            // 
            // btnRefrescar
            // 
            this.btnRefrescar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefrescar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnRefrescar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefrescar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(230)))), ((int)(((byte)(140)))), ((int)(((byte)(170)))));
            this.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefrescar.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnRefrescar.ForeColor = System.Drawing.Color.White;
            this.btnRefrescar.Location = new System.Drawing.Point(746, 21);
            this.btnRefrescar.Name = "btnRefrescar";
            this.btnRefrescar.Size = new System.Drawing.Size(100, 28);
            this.btnRefrescar.TabIndex = 2;
            this.btnRefrescar.Text = "↻  Actualizar";
            this.btnRefrescar.UseVisualStyleBackColor = false;
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);
            // 
            // flowCards
            // 
            this.flowCards.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.flowCards.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowCards.Location = new System.Drawing.Point(0, 62);
            this.flowCards.Name = "flowCards";
            this.flowCards.Padding = new System.Windows.Forms.Padding(10, 10, 10, 4);
            this.flowCards.Size = new System.Drawing.Size(854, 168);
            this.flowCards.TabIndex = 1;
            this.flowCards.WrapContents = false;
            this.flowCards.Resize += new System.EventHandler(this.FlowCards_Resize);
            // 
            // lblAviso
            // 
            this.lblAviso.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAviso.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblAviso.Location = new System.Drawing.Point(0, 230);
            this.lblAviso.Name = "lblAviso";
            this.lblAviso.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblAviso.Size = new System.Drawing.Size(854, 0);
            this.lblAviso.TabIndex = 2;
            this.lblAviso.Visible = false;
            // 
            // panelCentro
            // 
            this.panelCentro.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.panelCentro.Controls.Add(this.panelMiniStats);
            this.panelCentro.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCentro.Location = new System.Drawing.Point(0, 370);
            this.panelCentro.Name = "panelCentro";
            this.panelCentro.Size = new System.Drawing.Size(854, 135);
            this.panelCentro.TabIndex = 3;
            this.panelCentro.Resize += new System.EventHandler(this.PanelCentro_Resize);
            // 
            // panelMiniStats
            // 
            this.panelMiniStats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(244)))), ((int)(((byte)(250)))));
            this.panelMiniStats.Controls.Add(this.flStats);
            this.panelMiniStats.Controls.Add(this.lblStTitulo);
            this.panelMiniStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMiniStats.Location = new System.Drawing.Point(0, 0);
            this.panelMiniStats.Name = "panelMiniStats";
            this.panelMiniStats.Padding = new System.Windows.Forms.Padding(8);
            this.panelMiniStats.Size = new System.Drawing.Size(854, 135);
            this.panelMiniStats.TabIndex = 0;
            // 
            // flStats
            // 
            this.flStats.BackColor = System.Drawing.Color.Transparent;
            this.flStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flStats.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flStats.Location = new System.Drawing.Point(8, 36);
            this.flStats.Name = "flStats";
            this.flStats.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.flStats.Size = new System.Drawing.Size(838, 91);
            this.flStats.TabIndex = 1;
            this.flStats.WrapContents = false;
            // 
            // lblStTitulo
            // 
            this.lblStTitulo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(240)))), ((int)(((byte)(246)))));
            this.lblStTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStTitulo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblStTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblStTitulo.Location = new System.Drawing.Point(8, 8);
            this.lblStTitulo.Name = "lblStTitulo";
            this.lblStTitulo.Padding = new System.Windows.Forms.Padding(4, 6, 0, 0);
            this.lblStTitulo.Size = new System.Drawing.Size(838, 28);
            this.lblStTitulo.TabIndex = 0;
            this.lblStTitulo.Text = "Resumen de eventos";
            // 
            // panelTareas
            // 
            this.panelTareas.BackColor = System.Drawing.Color.White;
            this.panelTareas.Controls.Add(this.dgvTareas);
            this.panelTareas.Controls.Add(this.lblTareasTitulo);
            this.panelTareas.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTareas.Location = new System.Drawing.Point(0, 230);
            this.panelTareas.Name = "panelTareas";
            this.panelTareas.Size = new System.Drawing.Size(854, 140);
            this.panelTareas.TabIndex = 4;
            // 
            // dgvTareas
            // 
            this.dgvTareas.AllowUserToAddRows = false;
            this.dgvTareas.AllowUserToResizeRows = false;
            this.dgvTareas.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvTareas.BackgroundColor = System.Drawing.Color.White;
            this.dgvTareas.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvTareas.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvTareas.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvTareas.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTipo,
            this.colDesc,
            this.colFecha});
            this.dgvTareas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTareas.EnableHeadersVisualStyles = false;
            this.dgvTareas.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.dgvTareas.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(225)))), ((int)(((byte)(232)))));
            this.dgvTareas.Location = new System.Drawing.Point(0, 26);
            this.dgvTareas.Name = "dgvTareas";
            this.dgvTareas.ReadOnly = true;
            this.dgvTareas.RowHeadersVisible = false;
            this.dgvTareas.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTareas.Size = new System.Drawing.Size(854, 114);
            this.dgvTareas.TabIndex = 1;
            this.dgvTareas.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvTareas_CellClick);
            // 
            // colTipo
            // 
            this.colTipo.FillWeight = 22F;
            this.colTipo.HeaderText = "Tipo";
            this.colTipo.Name = "colTipo";
            this.colTipo.ReadOnly = true;
            // 
            // colDesc
            // 
            this.colDesc.FillWeight = 56F;
            this.colDesc.HeaderText = "Descripción";
            this.colDesc.Name = "colDesc";
            this.colDesc.ReadOnly = true;
            // 
            // colFecha
            // 
            this.colFecha.FillWeight = 22F;
            this.colFecha.HeaderText = "Desde";
            this.colFecha.Name = "colFecha";
            this.colFecha.ReadOnly = true;
            // 
            // lblTareasTitulo
            // 
            this.lblTareasTitulo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(240)))), ((int)(((byte)(248)))));
            this.lblTareasTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTareasTitulo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTareasTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblTareasTitulo.Location = new System.Drawing.Point(0, 0);
            this.lblTareasTitulo.Name = "lblTareasTitulo";
            this.lblTareasTitulo.Padding = new System.Windows.Forms.Padding(10, 5, 0, 0);
            this.lblTareasTitulo.Size = new System.Drawing.Size(854, 26);
            this.lblTareasTitulo.TabIndex = 0;
            this.lblTareasTitulo.Text = "Mis Tareas Pendientes";
            // 
            // panelSbar
            // 
            this.panelSbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.panelSbar.Controls.Add(this.lblSesion);
            this.panelSbar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelSbar.Location = new System.Drawing.Point(0, 505);
            this.panelSbar.Name = "panelSbar";
            this.panelSbar.Size = new System.Drawing.Size(854, 26);
            this.panelSbar.TabIndex = 5;
            // 
            // lblSesion
            // 
            this.lblSesion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSesion.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblSesion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(212)))), ((int)(((byte)(226)))));
            this.lblSesion.Location = new System.Drawing.Point(0, 0);
            this.lblSesion.Name = "lblSesion";
            this.lblSesion.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSesion.Size = new System.Drawing.Size(854, 26);
            this.lblSesion.TabIndex = 0;
            this.lblSesion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // DashboardForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.ClientSize = new System.Drawing.Size(854, 531);
            this.Controls.Add(this.panelCentro);
            this.Controls.Add(this.panelTareas);
            this.Controls.Add(this.lblAviso);
            this.Controls.Add(this.flowCards);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelSbar);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.Location = new System.Drawing.Point(10, 10);
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "DashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Panel de Control";
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
