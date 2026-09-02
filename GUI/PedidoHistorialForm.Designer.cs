namespace GUI
{
    partial class PedidoHistorialForm
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
            this.lblPedidoInfo = new System.Windows.Forms.Label();
            this.lblMensaje    = new System.Windows.Forms.Label();
            this.grpFiltros    = new System.Windows.Forms.GroupBox();
            this.chkDesde      = new System.Windows.Forms.CheckBox();
            this.lblDesde      = new System.Windows.Forms.Label();
            this.dtpDesde      = new System.Windows.Forms.DateTimePicker();
            this.chkHasta      = new System.Windows.Forms.CheckBox();
            this.lblHasta      = new System.Windows.Forms.Label();
            this.dtpHasta      = new System.Windows.Forms.DateTimePicker();
            this.lblAccion     = new System.Windows.Forms.Label();
            this.cmbAccion     = new System.Windows.Forms.ComboBox();
            this.btnBuscar     = new System.Windows.Forms.Button();
            this.dgv           = new System.Windows.Forms.DataGridView();
            this.btnRestaurar  = new System.Windows.Forms.Button();
            this.btnCerrar     = new System.Windows.Forms.Button();
            this.grpFiltros.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();

            // ── lblPedidoInfo — el texto final ("Pedido #N") se completa en el constructor ─
            this.lblPedidoInfo.AutoSize  = true;
            this.lblPedidoInfo.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblPedidoInfo.ForeColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.lblPedidoInfo.Location  = new System.Drawing.Point(12, 12);
            this.lblPedidoInfo.Name      = "lblPedidoInfo";
            this.lblPedidoInfo.TabIndex  = 0;

            // ── lblMensaje ─────────────────────────────────────────────────────
            this.lblMensaje.AutoSize  = false;
            this.lblMensaje.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.ForeColor = System.Drawing.Color.DimGray;
            this.lblMensaje.Location  = new System.Drawing.Point(12, 40);
            this.lblMensaje.Name      = "lblMensaje";
            this.lblMensaje.Size      = new System.Drawing.Size(820, 20);
            this.lblMensaje.TabIndex  = 1;

            // ── grpFiltros ─────────────────────────────────────────────────────
            this.grpFiltros.Controls.Add(this.chkDesde);
            this.grpFiltros.Controls.Add(this.lblDesde);
            this.grpFiltros.Controls.Add(this.dtpDesde);
            this.grpFiltros.Controls.Add(this.chkHasta);
            this.grpFiltros.Controls.Add(this.lblHasta);
            this.grpFiltros.Controls.Add(this.dtpHasta);
            this.grpFiltros.Controls.Add(this.lblAccion);
            this.grpFiltros.Controls.Add(this.cmbAccion);
            this.grpFiltros.Controls.Add(this.btnBuscar);
            this.grpFiltros.Font     = new System.Drawing.Font("Segoe UI", 8.5F);
            this.grpFiltros.Location = new System.Drawing.Point(12, 68);
            this.grpFiltros.Name     = "grpFiltros";
            this.grpFiltros.Size     = new System.Drawing.Size(820, 64);
            this.grpFiltros.TabIndex = 2;
            this.grpFiltros.TabStop  = false;
            this.grpFiltros.Tag      = "lbl.hist.filtros";
            this.grpFiltros.Text     = "Filtros";

            // ── chkDesde ───────────────────────────────────────────────────────
            this.chkDesde.Checked  = false;
            this.chkDesde.Location = new System.Drawing.Point(8, 30);
            this.chkDesde.Name     = "chkDesde";
            this.chkDesde.Size     = new System.Drawing.Size(20, 20);
            this.chkDesde.TabIndex = 0;
            this.chkDesde.CheckedChanged += new System.EventHandler(this.ChkDesde_CheckedChanged);

            // ── lblDesde ───────────────────────────────────────────────────────
            this.lblDesde.AutoSize = true;
            this.lblDesde.Location = new System.Drawing.Point(28, 32);
            this.lblDesde.Name     = "lblDesde";
            this.lblDesde.TabIndex = 1;
            this.lblDesde.Tag      = "lbl.hist.desde";
            this.lblDesde.Text     = "Desde:";

            // ── dtpDesde — Value se fija en el constructor (DateTime.Today.AddMonths(-1)) ─
            this.dtpDesde.Enabled  = false;
            this.dtpDesde.Format   = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDesde.Location = new System.Drawing.Point(80, 28);
            this.dtpDesde.Name     = "dtpDesde";
            this.dtpDesde.Size     = new System.Drawing.Size(105, 22);
            this.dtpDesde.TabIndex = 2;

            // ── chkHasta ───────────────────────────────────────────────────────
            this.chkHasta.Checked  = false;
            this.chkHasta.Location = new System.Drawing.Point(200, 30);
            this.chkHasta.Name     = "chkHasta";
            this.chkHasta.Size     = new System.Drawing.Size(20, 20);
            this.chkHasta.TabIndex = 3;
            this.chkHasta.CheckedChanged += new System.EventHandler(this.ChkHasta_CheckedChanged);

            // ── lblHasta ───────────────────────────────────────────────────────
            this.lblHasta.AutoSize = true;
            this.lblHasta.Location = new System.Drawing.Point(220, 32);
            this.lblHasta.Name     = "lblHasta";
            this.lblHasta.TabIndex = 4;
            this.lblHasta.Tag      = "lbl.hist.hasta";
            this.lblHasta.Text     = "Hasta:";

            // ── dtpHasta — Value se fija en el constructor (DateTime.Today) ─────
            this.dtpHasta.Enabled  = false;
            this.dtpHasta.Format   = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpHasta.Location = new System.Drawing.Point(272, 28);
            this.dtpHasta.Name     = "dtpHasta";
            this.dtpHasta.Size     = new System.Drawing.Size(105, 22);
            this.dtpHasta.TabIndex = 5;

            // ── lblAccion ──────────────────────────────────────────────────────
            this.lblAccion.AutoSize = true;
            this.lblAccion.Location = new System.Drawing.Point(394, 32);
            this.lblAccion.Name     = "lblAccion";
            this.lblAccion.TabIndex = 6;
            this.lblAccion.Tag      = "lbl.hist.accion";
            this.lblAccion.Text     = "Acción:";

            // ── cmbAccion ──────────────────────────────────────────────────────
            this.cmbAccion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAccion.Font          = new System.Drawing.Font("Segoe UI", 8.5F);
            this.cmbAccion.Location      = new System.Drawing.Point(446, 28);
            this.cmbAccion.Name          = "cmbAccion";
            this.cmbAccion.Size          = new System.Drawing.Size(148, 22);
            this.cmbAccion.TabIndex      = 7;

            // ── btnBuscar ──────────────────────────────────────────────────────
            this.btnBuscar.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnBuscar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnBuscar.FlatAppearance.BorderSize = 0;
            this.btnBuscar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuscar.Font      = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnBuscar.ForeColor = System.Drawing.Color.White;
            this.btnBuscar.Location  = new System.Drawing.Point(610, 26);
            this.btnBuscar.Name      = "btnBuscar";
            this.btnBuscar.Size      = new System.Drawing.Size(100, 28);
            this.btnBuscar.TabIndex  = 8;
            this.btnBuscar.Tag       = "btn.hist.buscar";
            this.btnBuscar.Text      = "🔍 Buscar";
            this.btnBuscar.UseVisualStyleBackColor = false;
            this.btnBuscar.Click += new System.EventHandler(this.BtnBuscar_Click);

            // ── dgv ────────────────────────────────────────────────────────────
            this.dgv.AllowUserToAddRows = false;
            this.dgv.Anchor = ((System.Windows.Forms.AnchorStyles)(
                System.Windows.Forms.AnchorStyles.Top |
                System.Windows.Forms.AnchorStyles.Bottom |
                System.Windows.Forms.AnchorStyles.Left |
                System.Windows.Forms.AnchorStyles.Right));
            this.dgv.BackgroundColor = System.Drawing.Color.WhiteSmoke;
            this.dgv.BorderStyle     = System.Windows.Forms.BorderStyle.None;
            this.dgv.ColumnHeadersHeight = 28;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv.EnableHeadersVisualStyles = false;
            this.dgv.Font        = new System.Drawing.Font("Segoe UI", 8.5F);
            this.dgv.GridColor   = System.Drawing.Color.FromArgb(220, 220, 220);
            this.dgv.Location    = new System.Drawing.Point(12, 142);
            this.dgv.MultiSelect = false;
            this.dgv.Name        = "dgv";
            this.dgv.ReadOnly    = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.SelectionMode     = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv.Size        = new System.Drawing.Size(820, 320);
            this.dgv.TabIndex    = 9;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.dgv.ColumnHeadersDefaultCellStyle.Font      = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgv.DefaultCellStyle.SelectionBackColor     = System.Drawing.Color.FromArgb(255, 182, 193);
            this.dgv.DefaultCellStyle.SelectionForeColor     = System.Drawing.Color.Black;
            this.dgv.SelectionChanged += new System.EventHandler(this.Dgv_SelectionChanged);

            // ── btnRestaurar ───────────────────────────────────────────────────
            this.btnRestaurar.Anchor = ((System.Windows.Forms.AnchorStyles)(
                System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            this.btnRestaurar.BackColor = System.Drawing.Color.FromArgb(210, 100, 135);
            this.btnRestaurar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnRestaurar.Enabled   = false;
            this.btnRestaurar.FlatAppearance.BorderSize = 0;
            this.btnRestaurar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestaurar.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRestaurar.ForeColor = System.Drawing.Color.White;
            this.btnRestaurar.Location  = new System.Drawing.Point(12, 472);
            this.btnRestaurar.Name      = "btnRestaurar";
            this.btnRestaurar.Size      = new System.Drawing.Size(130, 32);
            this.btnRestaurar.TabIndex  = 10;
            this.btnRestaurar.Tag       = "btn.hist.restaurar";
            this.btnRestaurar.Text      = "⟲ Restaurar";
            this.btnRestaurar.UseVisualStyleBackColor = false;
            this.btnRestaurar.Click += new System.EventHandler(this.BtnRestaurar_Click);

            // ── btnCerrar ──────────────────────────────────────────────────────
            this.btnCerrar.Anchor = ((System.Windows.Forms.AnchorStyles)(
                System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right));
            this.btnCerrar.BackColor = System.Drawing.Color.FromArgb(210, 210, 210);
            this.btnCerrar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.Font      = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCerrar.ForeColor = System.Drawing.Color.Black;
            this.btnCerrar.Location  = new System.Drawing.Point(714, 472);
            this.btnCerrar.Name      = "btnCerrar";
            this.btnCerrar.Size      = new System.Drawing.Size(118, 32);
            this.btnCerrar.TabIndex  = 11;
            this.btnCerrar.Tag       = "btn.hist.cerrar";
            this.btnCerrar.Text      = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);

            // ── PedidoHistorialForm ────────────────────────────────────────────
            this.BackColor       = System.Drawing.Color.White;
            this.ClientSize      = new System.Drawing.Size(860, 560);
            this.Controls.Add(this.lblPedidoInfo);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.grpFiltros);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.btnRestaurar);
            this.Controls.Add(this.btnCerrar);
            this.CancelButton = this.btnCerrar;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MinimumSize     = new System.Drawing.Size(760, 460);
            this.Name            = "PedidoHistorialForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag             = "frm.historial";
            this.Text            = "Historial de Cambios — Pedido";
            this.grpFiltros.ResumeLayout(false);
            this.grpFiltros.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label          lblPedidoInfo;
        private System.Windows.Forms.Label          lblMensaje;
        private System.Windows.Forms.GroupBox       grpFiltros;
        private System.Windows.Forms.CheckBox       chkDesde;
        private System.Windows.Forms.Label          lblDesde;
        private System.Windows.Forms.DateTimePicker dtpDesde;
        private System.Windows.Forms.CheckBox       chkHasta;
        private System.Windows.Forms.Label          lblHasta;
        private System.Windows.Forms.DateTimePicker dtpHasta;
        private System.Windows.Forms.Label          lblAccion;
        private System.Windows.Forms.ComboBox       cmbAccion;
        private System.Windows.Forms.Button         btnBuscar;
        private System.Windows.Forms.DataGridView   dgv;
        private System.Windows.Forms.Button         btnRestaurar;
        private System.Windows.Forms.Button         btnCerrar;
    }
}
