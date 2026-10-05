namespace GUI
{
    partial class ControlStockForm
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
            System.Windows.Forms.DataGridViewCellStyle estiloAlterno = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnInformarFaltantes = new System.Windows.Forms.Button();
            this.btnConfirmarPrendas = new System.Windows.Forms.Button();
            this.btnSepararPrendas = new System.Windows.Forms.Button();
            this.btnImprimirPlanilla = new System.Windows.Forms.Button();
            this.btnRefrescar = new System.Windows.Forms.Button();
            this.lblConteo = new System.Windows.Forms.Label();
            this.dgvCola = new System.Windows.Forms.DataGridView();
            this.panelPlanilla = new System.Windows.Forms.Panel();
            this.dgvPlanilla = new System.Windows.Forms.DataGridView();
            this.lblPlanillaTitulo = new System.Windows.Forms.Label();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCola)).BeginInit();
            this.panelPlanilla.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlanilla)).BeginInit();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // panelTop
            //
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(240)))));
            this.panelTop.Controls.Add(this.btnInformarFaltantes);
            this.panelTop.Controls.Add(this.btnConfirmarPrendas);
            this.panelTop.Controls.Add(this.btnSepararPrendas);
            this.panelTop.Controls.Add(this.btnImprimirPlanilla);
            this.panelTop.Controls.Add(this.btnRefrescar);
            this.panelTop.Controls.Add(this.lblConteo);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 8, 8, 4);
            this.panelTop.Size = new System.Drawing.Size(1000, 52);
            this.panelTop.TabIndex = 0;
            //
            // btnInformarFaltantes
            //
            this.btnInformarFaltantes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(166)))), ((int)(((byte)(101)))), ((int)(((byte)(14)))));
            this.btnInformarFaltantes.Enabled = false;
            this.btnInformarFaltantes.FlatAppearance.BorderSize = 0;
            this.btnInformarFaltantes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInformarFaltantes.ForeColor = System.Drawing.Color.White;
            this.btnInformarFaltantes.Location = new System.Drawing.Point(8, 11);
            this.btnInformarFaltantes.Name = "btnInformarFaltantes";
            this.btnInformarFaltantes.Size = new System.Drawing.Size(150, 28);
            this.btnInformarFaltantes.TabIndex = 0;
            this.btnInformarFaltantes.Tag = "btn.cs.faltantes";
            this.btnInformarFaltantes.Text = "Informar faltantes";
            this.btnInformarFaltantes.UseVisualStyleBackColor = false;
            this.btnInformarFaltantes.Click += new System.EventHandler(this.BtnInformarFaltantes_Click);
            //
            // btnConfirmarPrendas
            //
            this.btnConfirmarPrendas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(100)))), ((int)(((byte)(170)))));
            this.btnConfirmarPrendas.Enabled = false;
            this.btnConfirmarPrendas.FlatAppearance.BorderSize = 0;
            this.btnConfirmarPrendas.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirmarPrendas.ForeColor = System.Drawing.Color.White;
            this.btnConfirmarPrendas.Location = new System.Drawing.Point(166, 11);
            this.btnConfirmarPrendas.Name = "btnConfirmarPrendas";
            this.btnConfirmarPrendas.Size = new System.Drawing.Size(210, 28);
            this.btnConfirmarPrendas.TabIndex = 1;
            this.btnConfirmarPrendas.Tag = "btn.cs.confirmar";
            this.btnConfirmarPrendas.Text = "Confirmar prendas disponibles";
            this.btnConfirmarPrendas.UseVisualStyleBackColor = false;
            this.btnConfirmarPrendas.Click += new System.EventHandler(this.BtnConfirmarPrendas_Click);
            //
            // btnSepararPrendas
            //
            this.btnSepararPrendas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(70)))));
            this.btnSepararPrendas.Enabled = false;
            this.btnSepararPrendas.FlatAppearance.BorderSize = 0;
            this.btnSepararPrendas.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSepararPrendas.ForeColor = System.Drawing.Color.White;
            this.btnSepararPrendas.Location = new System.Drawing.Point(384, 11);
            this.btnSepararPrendas.Name = "btnSepararPrendas";
            this.btnSepararPrendas.Size = new System.Drawing.Size(150, 28);
            this.btnSepararPrendas.TabIndex = 2;
            this.btnSepararPrendas.Tag = "btn.cs.separar";
            this.btnSepararPrendas.Text = "Separar prendas";
            this.btnSepararPrendas.UseVisualStyleBackColor = false;
            this.btnSepararPrendas.Click += new System.EventHandler(this.BtnSepararPrendas_Click);
            //
            // btnImprimirPlanilla
            //
            this.btnImprimirPlanilla.Enabled = false;
            this.btnImprimirPlanilla.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImprimirPlanilla.Location = new System.Drawing.Point(542, 11);
            this.btnImprimirPlanilla.Name = "btnImprimirPlanilla";
            this.btnImprimirPlanilla.Size = new System.Drawing.Size(130, 28);
            this.btnImprimirPlanilla.TabIndex = 3;
            this.btnImprimirPlanilla.Tag = "btn.cs.planilla";
            this.btnImprimirPlanilla.Text = "Imprimir planilla";
            this.btnImprimirPlanilla.Click += new System.EventHandler(this.BtnImprimirPlanilla_Click);
            //
            // btnRefrescar
            //
            this.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefrescar.Location = new System.Drawing.Point(680, 11);
            this.btnRefrescar.Name = "btnRefrescar";
            this.btnRefrescar.Size = new System.Drawing.Size(90, 28);
            this.btnRefrescar.TabIndex = 4;
            this.btnRefrescar.Text = "Actualizar";
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);
            //
            // lblConteo
            //
            this.lblConteo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblConteo.ForeColor = System.Drawing.Color.DimGray;
            this.lblConteo.Location = new System.Drawing.Point(780, 16);
            this.lblConteo.Name = "lblConteo";
            this.lblConteo.Size = new System.Drawing.Size(210, 23);
            this.lblConteo.TabIndex = 5;
            //
            // dgvCola
            //
            this.dgvCola.AllowUserToAddRows = false;
            this.dgvCola.AllowUserToDeleteRows = false;
            estiloAlterno.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(248)))), ((int)(((byte)(252)))));
            this.dgvCola.AlternatingRowsDefaultCellStyle = estiloAlterno;
            this.dgvCola.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvCola.BackgroundColor = System.Drawing.Color.White;
            this.dgvCola.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCola.Location = new System.Drawing.Point(0, 52);
            this.dgvCola.MultiSelect = false;
            this.dgvCola.Name = "dgvCola";
            this.dgvCola.ReadOnly = true;
            this.dgvCola.RowHeadersVisible = false;
            this.dgvCola.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCola.Size = new System.Drawing.Size(1000, 222);
            this.dgvCola.TabIndex = 1;
            this.dgvCola.SelectionChanged += new System.EventHandler(this.DgvCola_SelectionChanged);
            //
            // panelPlanilla
            //
            this.panelPlanilla.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.panelPlanilla.Controls.Add(this.dgvPlanilla);
            this.panelPlanilla.Controls.Add(this.lblPlanillaTitulo);
            this.panelPlanilla.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelPlanilla.Location = new System.Drawing.Point(0, 274);
            this.panelPlanilla.Name = "panelPlanilla";
            this.panelPlanilla.Padding = new System.Windows.Forms.Padding(8);
            this.panelPlanilla.Size = new System.Drawing.Size(1000, 260);
            this.panelPlanilla.TabIndex = 2;
            //
            // dgvPlanilla
            //
            this.dgvPlanilla.AllowUserToAddRows = false;
            this.dgvPlanilla.AllowUserToDeleteRows = false;
            this.dgvPlanilla.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPlanilla.BackgroundColor = System.Drawing.Color.White;
            this.dgvPlanilla.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPlanilla.Location = new System.Drawing.Point(8, 34);
            this.dgvPlanilla.Name = "dgvPlanilla";
            this.dgvPlanilla.ReadOnly = true;
            this.dgvPlanilla.RowHeadersVisible = false;
            this.dgvPlanilla.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlanilla.Size = new System.Drawing.Size(984, 218);
            this.dgvPlanilla.TabIndex = 1;
            //
            // lblPlanillaTitulo
            //
            this.lblPlanillaTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPlanillaTitulo.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblPlanillaTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblPlanillaTitulo.Location = new System.Drawing.Point(8, 8);
            this.lblPlanillaTitulo.Name = "lblPlanillaTitulo";
            this.lblPlanillaTitulo.Size = new System.Drawing.Size(984, 26);
            this.lblPlanillaTitulo.TabIndex = 0;
            this.lblPlanillaTitulo.Tag = "lbl.cs.planilla";
            this.lblPlanillaTitulo.Text = "Planilla de control de existencias — seleccioná un pedido de la cola";
            //
            // panelStatus
            //
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(240)))));
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 534);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.panelStatus.Size = new System.Drawing.Size(1000, 26);
            this.panelStatus.TabIndex = 3;
            //
            // lblMensaje
            //
            this.lblMensaje.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.Location = new System.Drawing.Point(8, 4);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(984, 18);
            this.lblMensaje.TabIndex = 0;
            //
            // ControlStockForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 560);
            this.Controls.Add(this.dgvCola);
            this.Controls.Add(this.panelPlanilla);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelTop);
            this.Name = "ControlStockForm";
            this.Tag = "frm.controlstock";
            this.Text = "Control de Stock";
            this.Load += new System.EventHandler(this.ControlStockForm_Load);
            this.panelTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCola)).EndInit();
            this.panelPlanilla.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlanilla)).EndInit();
            this.panelStatus.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.Button       btnInformarFaltantes;
        private System.Windows.Forms.Button       btnConfirmarPrendas;
        private System.Windows.Forms.Button       btnSepararPrendas;
        private System.Windows.Forms.Button       btnImprimirPlanilla;
        private System.Windows.Forms.Button       btnRefrescar;
        private System.Windows.Forms.Label        lblConteo;
        private System.Windows.Forms.DataGridView dgvCola;
        private System.Windows.Forms.Panel        panelPlanilla;
        private System.Windows.Forms.DataGridView dgvPlanilla;
        private System.Windows.Forms.Label        lblPlanillaTitulo;
        private System.Windows.Forms.Panel        panelStatus;
        private System.Windows.Forms.Label        lblMensaje;
    }
}
