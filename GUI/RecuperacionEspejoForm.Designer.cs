namespace GUI
{
    partial class RecuperacionEspejoForm
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
            this.panelEstado  = new System.Windows.Forms.Panel();
            this.lblEstado    = new System.Windows.Forms.Label();
            this.contenedor   = new System.Windows.Forms.Panel();
            this.grid         = new System.Windows.Forms.DataGridView();
            this.colId        = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colUsuario   = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTipo      = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCampo     = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colActual    = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEsperado  = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtResumen   = new System.Windows.Forms.TextBox();
            this.panelBotones = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCerrar    = new System.Windows.Forms.Button();
            this.btnBackup    = new System.Windows.Forms.Button();
            this.btnAsumir    = new System.Windows.Forms.Button();
            this.btnReparar   = new System.Windows.Forms.Button();
            this.panelEstado.SuspendLayout();
            this.contenedor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.panelBotones.SuspendLayout();
            this.SuspendLayout();

            // ── panelEstado ────────────────────────────────────────────────────
            this.panelEstado.BackColor = System.Drawing.Color.FromArgb(40, 40, 55);
            this.panelEstado.Controls.Add(this.lblEstado);
            this.panelEstado.Dock     = System.Windows.Forms.DockStyle.Top;
            this.panelEstado.Height   = 58;
            this.panelEstado.Name     = "panelEstado";
            this.panelEstado.Padding  = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.panelEstado.TabIndex = 2;

            // ── lblEstado ──────────────────────────────────────────────────────
            this.lblEstado.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblEstado.Font      = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblEstado.ForeColor = System.Drawing.Color.White;
            this.lblEstado.Name      = "lblEstado";
            this.lblEstado.TabIndex  = 0;
            this.lblEstado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ── contenedor ─────────────────────────────────────────────────────
            this.contenedor.Controls.Add(this.grid);
            this.contenedor.Controls.Add(this.txtResumen);
            this.contenedor.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.contenedor.Name     = "contenedor";
            this.contenedor.TabIndex = 0;

            // ── grid ───────────────────────────────────────────────────────────
            this.grid.AllowUserToAddRows    = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode   = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor       = System.Drawing.Color.White;
            this.grid.BorderStyle           = System.Windows.Forms.BorderStyle.None;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colId,
            this.colUsuario,
            this.colTipo,
            this.colCampo,
            this.colActual,
            this.colEsperado});
            this.grid.Dock          = System.Windows.Forms.DockStyle.Fill;
            this.grid.Font          = new System.Drawing.Font("Segoe UI", 9F);
            this.grid.Name          = "grid";
            this.grid.ReadOnly      = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.TabIndex      = 1;

            // ── columnas de grid ───────────────────────────────────────────────
            this.colId.Name = "cId";
            this.colId.HeaderText = "ID";
            this.colId.FillWeight = 7F;
            this.colId.Tag = "rec.col.id";

            this.colUsuario.Name = "cUsuario";
            this.colUsuario.HeaderText = "Usuario";
            this.colUsuario.FillWeight = 18F;
            this.colUsuario.Tag = "rec.col.usuario";

            this.colTipo.Name = "cTipo";
            this.colTipo.HeaderText = "Tipo";
            this.colTipo.FillWeight = 18F;
            this.colTipo.Tag = "rec.col.tipo";

            this.colCampo.Name = "cCampo";
            this.colCampo.HeaderText = "Campo";
            this.colCampo.FillWeight = 17F;
            this.colCampo.Tag = "rec.col.campo";

            this.colActual.Name = "cActual";
            this.colActual.HeaderText = "Actual";
            this.colActual.FillWeight = 20F;
            this.colActual.Tag = "rec.col.actual";

            this.colEsperado.Name = "cEsperado";
            this.colEsperado.HeaderText = "Esperado (espejo)";
            this.colEsperado.FillWeight = 20F;
            this.colEsperado.Tag = "rec.col.esperado";

            // ── txtResumen ─────────────────────────────────────────────────────
            this.txtResumen.BackColor    = System.Drawing.Color.FromArgb(248, 244, 250);
            this.txtResumen.BorderStyle  = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtResumen.Dock         = System.Windows.Forms.DockStyle.Top;
            this.txtResumen.Font         = new System.Drawing.Font("Consolas", 8.5F);
            this.txtResumen.ForeColor    = System.Drawing.Color.FromArgb(60, 30, 50);
            this.txtResumen.Height       = 110;
            this.txtResumen.Multiline    = true;
            this.txtResumen.Name         = "txtResumen";
            this.txtResumen.ReadOnly     = true;
            this.txtResumen.ScrollBars   = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResumen.TabIndex     = 0;

            // ── panelBotones ───────────────────────────────────────────────────
            this.panelBotones.BackColor      = System.Drawing.Color.FromArgb(245, 245, 250);
            this.panelBotones.Controls.Add(this.btnCerrar);
            this.panelBotones.Controls.Add(this.btnBackup);
            this.panelBotones.Controls.Add(this.btnAsumir);
            this.panelBotones.Controls.Add(this.btnReparar);
            // Sin AcceptButton a propósito: las otras 3 acciones del panel son irreversibles
            // (reparar/asumir pérdida/restaurar backup) — Enter no debe dispararlas por accidente.
            this.CancelButton = this.btnCerrar;
            this.panelBotones.Dock          = System.Windows.Forms.DockStyle.Bottom;
            this.panelBotones.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.panelBotones.Height        = 52;
            this.panelBotones.Name          = "panelBotones";
            this.panelBotones.Padding       = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.panelBotones.TabIndex      = 1;

            // ── btnCerrar ──────────────────────────────────────────────────────
            this.btnCerrar.BackColor = System.Drawing.Color.FromArgb(120, 120, 135);
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.ForeColor = System.Drawing.Color.White;
            this.btnCerrar.Height    = 34;
            this.btnCerrar.Name      = "btnCerrar";
            this.btnCerrar.TabIndex  = 3;
            this.btnCerrar.Tag       = "rec.btn.cerrar";
            this.btnCerrar.Text      = "Cerrar";
            this.btnCerrar.Width     = 90;
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);

            // ── btnBackup ──────────────────────────────────────────────────────
            this.btnBackup.BackColor = System.Drawing.Color.FromArgb(210, 100, 135);
            this.btnBackup.FlatAppearance.BorderSize = 0;
            this.btnBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBackup.ForeColor = System.Drawing.Color.White;
            this.btnBackup.Height    = 34;
            this.btnBackup.Name      = "btnBackup";
            this.btnBackup.TabIndex  = 2;
            this.btnBackup.Tag       = "rec.btn.backup";
            this.btnBackup.Text      = "Restaurar Backup...";
            this.btnBackup.Width     = 160;
            this.btnBackup.Click += new System.EventHandler(this.BtnBackup_Click);

            // ── btnAsumir ──────────────────────────────────────────────────────
            this.btnAsumir.BackColor = System.Drawing.Color.FromArgb(180, 120, 60);
            this.btnAsumir.FlatAppearance.BorderSize = 0;
            this.btnAsumir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAsumir.ForeColor = System.Drawing.Color.White;
            this.btnAsumir.Height    = 34;
            this.btnAsumir.Name      = "btnAsumir";
            this.btnAsumir.TabIndex  = 1;
            this.btnAsumir.Tag       = "rec.btn.asumir";
            this.btnAsumir.Text      = "Asumir Pérdida";
            this.btnAsumir.Width     = 140;
            this.btnAsumir.Click += new System.EventHandler(this.BtnAsumir_Click);

            // ── btnReparar ─────────────────────────────────────────────────────
            this.btnReparar.BackColor = System.Drawing.Color.FromArgb(80, 150, 90);
            this.btnReparar.FlatAppearance.BorderSize = 0;
            this.btnReparar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReparar.ForeColor = System.Drawing.Color.White;
            this.btnReparar.Height    = 34;
            this.btnReparar.Name      = "btnReparar";
            this.btnReparar.TabIndex  = 0;
            this.btnReparar.Tag       = "rec.btn.reparar";
            this.btnReparar.Text      = "Reparar desde Espejo";
            this.btnReparar.Width     = 170;
            this.btnReparar.Click += new System.EventHandler(this.BtnReparar_Click);

            // ── RecuperacionEspejoForm ─────────────────────────────────────────
            this.ClientSize    = new System.Drawing.Size(860, 600);
            this.Controls.Add(this.contenedor);
            this.Controls.Add(this.panelBotones);
            this.Controls.Add(this.panelEstado);
            this.Font          = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize   = new System.Drawing.Size(720, 500);
            this.Name          = "RecuperacionEspejoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Tag           = "rec.frm.titulo";
            this.Text          = "Recuperación de Integridad (Espejo)";
            this.panelEstado.ResumeLayout(false);
            this.contenedor.ResumeLayout(false);
            this.contenedor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.panelBotones.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel          panelEstado;
        private System.Windows.Forms.Label          lblEstado;
        private System.Windows.Forms.Panel          contenedor;
        private System.Windows.Forms.DataGridView   grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colId;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUsuario;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTipo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCampo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colActual;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEsperado;
        private System.Windows.Forms.TextBox        txtResumen;
        private System.Windows.Forms.FlowLayoutPanel panelBotones;
        private System.Windows.Forms.Button         btnCerrar;
        private System.Windows.Forms.Button         btnBackup;
        private System.Windows.Forms.Button         btnAsumir;
        private System.Windows.Forms.Button         btnReparar;
    }
}
