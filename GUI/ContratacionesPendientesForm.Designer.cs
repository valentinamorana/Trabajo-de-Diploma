namespace GUI
{
    partial class ContratacionesPendientesForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblMedioPago = new System.Windows.Forms.Label();
            this.cmbMedioPago = new System.Windows.Forms.ComboBox();
            this.btnCobrar = new System.Windows.Forms.Button();
            this.btnIntentoFallido = new System.Windows.Forms.Button();
            this.btnRefrescar = new System.Windows.Forms.Button();
            this.lblConteo = new System.Windows.Forms.Label();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.dgvContrataciones = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.panelStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvContrataciones)).BeginInit();
            this.SuspendLayout();
            //
            // panelTop
            //
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelTop.Controls.Add(this.lblMedioPago);
            this.panelTop.Controls.Add(this.cmbMedioPago);
            this.panelTop.Controls.Add(this.btnCobrar);
            this.panelTop.Controls.Add(this.btnIntentoFallido);
            this.panelTop.Controls.Add(this.btnRefrescar);
            this.panelTop.Controls.Add(this.lblConteo);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 4);
            this.panelTop.Size = new System.Drawing.Size(900, 60);
            this.panelTop.TabIndex = 0;
            //
            // lblMedioPago
            //
            this.lblMedioPago.Location = new System.Drawing.Point(8, 18);
            this.lblMedioPago.Name = "lblMedioPago";
            this.lblMedioPago.Size = new System.Drawing.Size(90, 23);
            this.lblMedioPago.TabIndex = 0;
            this.lblMedioPago.Text = "Medio de pago:";
            this.lblMedioPago.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbMedioPago
            //
            this.cmbMedioPago.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMedioPago.Items.AddRange(new object[] {
            "Efectivo",
            "Tarjeta",
            "Transferencia"});
            this.cmbMedioPago.Location = new System.Drawing.Point(102, 16);
            this.cmbMedioPago.Name = "cmbMedioPago";
            this.cmbMedioPago.Size = new System.Drawing.Size(140, 21);
            this.cmbMedioPago.TabIndex = 1;
            //
            // btnCobrar
            //
            this.btnCobrar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(140)))), ((int)(((byte)(60)))));
            this.btnCobrar.Enabled = false;
            this.btnCobrar.FlatAppearance.BorderSize = 0;
            this.btnCobrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCobrar.ForeColor = System.Drawing.Color.White;
            this.btnCobrar.Location = new System.Drawing.Point(252, 15);
            this.btnCobrar.Name = "btnCobrar";
            this.btnCobrar.Size = new System.Drawing.Size(100, 28);
            this.btnCobrar.TabIndex = 2;
            this.btnCobrar.Text = "💲 Cobrar";
            this.btnCobrar.UseVisualStyleBackColor = false;
            this.btnCobrar.Click += new System.EventHandler(this.BtnCobrar_Click);
            //
            // btnIntentoFallido
            //
            this.btnIntentoFallido.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnIntentoFallido.Enabled = false;
            this.btnIntentoFallido.FlatAppearance.BorderSize = 0;
            this.btnIntentoFallido.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIntentoFallido.ForeColor = System.Drawing.Color.White;
            this.btnIntentoFallido.Location = new System.Drawing.Point(360, 15);
            this.btnIntentoFallido.Name = "btnIntentoFallido";
            this.btnIntentoFallido.Size = new System.Drawing.Size(160, 28);
            this.btnIntentoFallido.TabIndex = 3;
            this.btnIntentoFallido.Text = "✗ Intento Fallido";
            this.btnIntentoFallido.UseVisualStyleBackColor = false;
            this.btnIntentoFallido.Click += new System.EventHandler(this.BtnIntentoFallido_Click);
            //
            // btnRefrescar
            //
            this.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefrescar.Location = new System.Drawing.Point(528, 15);
            this.btnRefrescar.Name = "btnRefrescar";
            this.btnRefrescar.Size = new System.Drawing.Size(32, 28);
            this.btnRefrescar.TabIndex = 4;
            this.btnRefrescar.Text = "↻";
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);
            //
            // lblConteo
            //
            this.lblConteo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblConteo.ForeColor = System.Drawing.Color.DimGray;
            this.lblConteo.Location = new System.Drawing.Point(568, 18);
            this.lblConteo.Name = "lblConteo";
            this.lblConteo.Size = new System.Drawing.Size(320, 23);
            this.lblConteo.TabIndex = 5;
            this.lblConteo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // panelStatus
            //
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 474);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.panelStatus.Size = new System.Drawing.Size(900, 26);
            this.panelStatus.TabIndex = 2;
            //
            // lblMensaje
            //
            this.lblMensaje.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.Location = new System.Drawing.Point(8, 4);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(884, 18);
            this.lblMensaje.TabIndex = 0;
            //
            // dgvContrataciones
            //
            this.dgvContrataciones.AllowUserToAddRows = false;
            this.dgvContrataciones.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(248)))), ((int)(((byte)(252)))));
            this.dgvContrataciones.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvContrataciones.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvContrataciones.BackgroundColor = System.Drawing.Color.White;
            this.dgvContrataciones.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(182)))), ((int)(((byte)(193)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvContrataciones.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvContrataciones.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvContrataciones.Location = new System.Drawing.Point(0, 60);
            this.dgvContrataciones.Name = "dgvContrataciones";
            this.dgvContrataciones.ReadOnly = true;
            this.dgvContrataciones.RowHeadersVisible = false;
            this.dgvContrataciones.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvContrataciones.Size = new System.Drawing.Size(900, 414);
            this.dgvContrataciones.TabIndex = 1;
            this.dgvContrataciones.SelectionChanged += new System.EventHandler(this.DgvContrataciones_SelectionChanged);
            //
            // ContratacionesPendientesForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 500);
            this.Controls.Add(this.dgvContrataciones);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new System.Drawing.Size(720, 400);
            this.Name = "ContratacionesPendientesForm";
            this.Text = "Contrataciones Pendientes de Pago";
            this.Load += new System.EventHandler(this.ContratacionesPendientesForm_Load);
            this.panelTop.ResumeLayout(false);
            this.panelStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvContrataciones)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblMedioPago;
        private System.Windows.Forms.ComboBox cmbMedioPago;
        private System.Windows.Forms.Button btnCobrar;
        private System.Windows.Forms.Button btnIntentoFallido;
        private System.Windows.Forms.Button btnRefrescar;
        private System.Windows.Forms.Label lblConteo;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblMensaje;
        private System.Windows.Forms.DataGridView dgvContrataciones;
    }
}
