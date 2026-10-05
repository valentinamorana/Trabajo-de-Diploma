namespace GUI
{
    partial class NuevaContratacionForm
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
            this.lblIdentificacion = new System.Windows.Forms.Label();
            this.txtIdentificacion = new System.Windows.Forms.TextBox();
            this.btnBuscar = new System.Windows.Forms.Button();
            this.btnRegistrarCliente = new System.Windows.Forms.Button();
            this.lstCoincidencias = new System.Windows.Forms.ListBox();
            this.lblFicha = new System.Windows.Forms.Label();
            this.lblPlanes = new System.Windows.Forms.Label();
            this.btnImprimirPlanes = new System.Windows.Forms.Button();
            this.dgvPlanes = new System.Windows.Forms.DataGridView();
            this.lblModalidad = new System.Windows.Forms.Label();
            this.cmbModalidad = new System.Windows.Forms.ComboBox();
            this.lblImporte = new System.Windows.Forms.Label();
            this.btnConfirmar = new System.Windows.Forms.Button();
            this.btnDesistir = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlanes)).BeginInit();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // lblIdentificacion
            //
            this.lblIdentificacion.AutoSize = true;
            this.lblIdentificacion.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblIdentificacion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblIdentificacion.Location = new System.Drawing.Point(20, 16);
            this.lblIdentificacion.Name = "lblIdentificacion";
            this.lblIdentificacion.Tag = "lbl.ped.identificacion";
            this.lblIdentificacion.Text = "Identificación del cliente (DNI, nombre o apellido):";
            //
            // txtIdentificacion
            //
            this.txtIdentificacion.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtIdentificacion.Location = new System.Drawing.Point(20, 42);
            this.txtIdentificacion.Name = "txtIdentificacion";
            this.txtIdentificacion.Size = new System.Drawing.Size(300, 25);
            this.txtIdentificacion.TabIndex = 0;
            this.txtIdentificacion.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtIdentificacion_KeyDown);
            //
            // btnBuscar
            //
            this.btnBuscar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnBuscar.FlatAppearance.BorderSize = 0;
            this.btnBuscar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuscar.ForeColor = System.Drawing.Color.White;
            this.btnBuscar.Location = new System.Drawing.Point(330, 41);
            this.btnBuscar.Name = "btnBuscar";
            this.btnBuscar.Size = new System.Drawing.Size(100, 28);
            this.btnBuscar.TabIndex = 1;
            this.btnBuscar.Tag = "btn.ped.buscar";
            this.btnBuscar.Text = "Buscar";
            this.btnBuscar.UseVisualStyleBackColor = false;
            this.btnBuscar.Click += new System.EventHandler(this.BtnBuscar_Click);
            //
            // btnRegistrarCliente
            //
            this.btnRegistrarCliente.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.btnRegistrarCliente.FlatAppearance.BorderSize = 0;
            this.btnRegistrarCliente.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegistrarCliente.ForeColor = System.Drawing.Color.White;
            this.btnRegistrarCliente.Location = new System.Drawing.Point(440, 41);
            this.btnRegistrarCliente.Name = "btnRegistrarCliente";
            this.btnRegistrarCliente.Size = new System.Drawing.Size(170, 28);
            this.btnRegistrarCliente.TabIndex = 2;
            this.btnRegistrarCliente.Tag = "btn.contr.registrarcliente";
            this.btnRegistrarCliente.Text = "Registrar cliente";
            this.btnRegistrarCliente.UseVisualStyleBackColor = false;
            this.btnRegistrarCliente.Visible = false;
            this.btnRegistrarCliente.Click += new System.EventHandler(this.BtnRegistrarCliente_Click);
            //
            // lstCoincidencias
            //
            this.lstCoincidencias.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lstCoincidencias.Location = new System.Drawing.Point(20, 74);
            this.lstCoincidencias.Name = "lstCoincidencias";
            this.lstCoincidencias.Size = new System.Drawing.Size(590, 64);
            this.lstCoincidencias.TabIndex = 3;
            this.lstCoincidencias.Visible = false;
            this.lstCoincidencias.SelectedIndexChanged += new System.EventHandler(this.LstCoincidencias_SelectedIndexChanged);
            //
            // lblFicha
            //
            this.lblFicha.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.lblFicha.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFicha.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFicha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.lblFicha.Location = new System.Drawing.Point(20, 144);
            this.lblFicha.Name = "lblFicha";
            this.lblFicha.Padding = new System.Windows.Forms.Padding(6);
            this.lblFicha.Size = new System.Drawing.Size(700, 62);
            this.lblFicha.Visible = false;
            //
            // lblPlanes
            //
            this.lblPlanes.AutoSize = true;
            this.lblPlanes.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblPlanes.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblPlanes.Location = new System.Drawing.Point(20, 216);
            this.lblPlanes.Name = "lblPlanes";
            this.lblPlanes.Tag = "lbl.contr.planes";
            this.lblPlanes.Text = "Planes disponibles:";
            //
            // btnImprimirPlanes
            //
            this.btnImprimirPlanes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(110)))));
            this.btnImprimirPlanes.FlatAppearance.BorderSize = 0;
            this.btnImprimirPlanes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImprimirPlanes.ForeColor = System.Drawing.Color.White;
            this.btnImprimirPlanes.Location = new System.Drawing.Point(560, 211);
            this.btnImprimirPlanes.Name = "btnImprimirPlanes";
            this.btnImprimirPlanes.Size = new System.Drawing.Size(160, 26);
            this.btnImprimirPlanes.TabIndex = 4;
            this.btnImprimirPlanes.Tag = "btn.contr.imprimirplanes";
            this.btnImprimirPlanes.Text = "Imprimir planes";
            this.btnImprimirPlanes.UseVisualStyleBackColor = false;
            this.btnImprimirPlanes.Click += new System.EventHandler(this.BtnImprimirPlanes_Click);
            //
            // dgvPlanes
            //
            this.dgvPlanes.AllowUserToAddRows = false;
            this.dgvPlanes.AllowUserToDeleteRows = false;
            this.dgvPlanes.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPlanes.BackgroundColor = System.Drawing.Color.White;
            this.dgvPlanes.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dgvPlanes.Enabled = false;
            this.dgvPlanes.Location = new System.Drawing.Point(20, 242);
            this.dgvPlanes.MultiSelect = false;
            this.dgvPlanes.Name = "dgvPlanes";
            this.dgvPlanes.ReadOnly = true;
            this.dgvPlanes.RowHeadersVisible = false;
            this.dgvPlanes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlanes.Size = new System.Drawing.Size(700, 140);
            this.dgvPlanes.TabIndex = 5;
            this.dgvPlanes.SelectionChanged += new System.EventHandler(this.DgvPlanes_SelectionChanged);
            //
            // lblModalidad
            //
            this.lblModalidad.AutoSize = true;
            this.lblModalidad.Location = new System.Drawing.Point(20, 397);
            this.lblModalidad.Name = "lblModalidad";
            this.lblModalidad.Tag = "lbl.contratacion.modalidad";
            this.lblModalidad.Text = "Modalidad de cobro:";
            //
            // cmbModalidad
            //
            this.cmbModalidad.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModalidad.Enabled = false;
            this.cmbModalidad.Location = new System.Drawing.Point(140, 393);
            this.cmbModalidad.Name = "cmbModalidad";
            this.cmbModalidad.Size = new System.Drawing.Size(170, 21);
            this.cmbModalidad.TabIndex = 6;
            this.cmbModalidad.SelectedIndexChanged += new System.EventHandler(this.CmbModalidad_SelectedIndexChanged);
            //
            // lblImporte
            //
            this.lblImporte.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblImporte.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(70)))));
            this.lblImporte.Location = new System.Drawing.Point(330, 388);
            this.lblImporte.Name = "lblImporte";
            this.lblImporte.Size = new System.Drawing.Size(390, 48);
            //
            // btnConfirmar
            //
            this.btnConfirmar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(70)))));
            this.btnConfirmar.Enabled = false;
            this.btnConfirmar.FlatAppearance.BorderSize = 0;
            this.btnConfirmar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirmar.ForeColor = System.Drawing.Color.White;
            this.btnConfirmar.Location = new System.Drawing.Point(20, 446);
            this.btnConfirmar.Name = "btnConfirmar";
            this.btnConfirmar.Size = new System.Drawing.Size(220, 34);
            this.btnConfirmar.TabIndex = 7;
            this.btnConfirmar.Tag = "btn.contr.registrar";
            this.btnConfirmar.Text = "Registrar contratación";
            this.btnConfirmar.UseVisualStyleBackColor = false;
            this.btnConfirmar.Click += new System.EventHandler(this.BtnConfirmar_Click);
            //
            // btnDesistir
            //
            this.btnDesistir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(178)))), ((int)(((byte)(58)))), ((int)(((byte)(58)))));
            this.btnDesistir.Enabled = false;
            this.btnDesistir.FlatAppearance.BorderSize = 0;
            this.btnDesistir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDesistir.ForeColor = System.Drawing.Color.White;
            this.btnDesistir.Location = new System.Drawing.Point(250, 446);
            this.btnDesistir.Name = "btnDesistir";
            this.btnDesistir.Size = new System.Drawing.Size(200, 34);
            this.btnDesistir.TabIndex = 8;
            this.btnDesistir.Tag = "btn.contr.desistir";
            this.btnDesistir.Text = "El cliente desiste";
            this.btnDesistir.UseVisualStyleBackColor = false;
            this.btnDesistir.Click += new System.EventHandler(this.BtnDesistir_Click);
            //
            // btnCancelar
            //
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.Location = new System.Drawing.Point(600, 446);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(120, 34);
            this.btnCancelar.TabIndex = 9;
            this.btnCancelar.Tag = "btn.cerrar";
            this.btnCancelar.Text = "Cerrar";
            this.btnCancelar.Click += new System.EventHandler(this.BtnCancelar_Click);
            //
            // panelStatus
            //
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 494);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.panelStatus.Size = new System.Drawing.Size(740, 46);
            //
            // lblMensaje
            //
            this.lblMensaje.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMensaje.Name = "lblMensaje";
            //
            // NuevaContratacionForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new System.Drawing.Size(740, 540);
            this.Controls.Add(this.lblIdentificacion);
            this.Controls.Add(this.txtIdentificacion);
            this.Controls.Add(this.btnBuscar);
            this.Controls.Add(this.btnRegistrarCliente);
            this.Controls.Add(this.lstCoincidencias);
            this.Controls.Add(this.lblFicha);
            this.Controls.Add(this.lblPlanes);
            this.Controls.Add(this.btnImprimirPlanes);
            this.Controls.Add(this.dgvPlanes);
            this.Controls.Add(this.lblModalidad);
            this.Controls.Add(this.cmbModalidad);
            this.Controls.Add(this.lblImporte);
            this.Controls.Add(this.btnConfirmar);
            this.Controls.Add(this.btnDesistir);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.panelStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NuevaContratacionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag = "frm.nuevacontratacion";
            this.Text = "Nueva Contratación";
            this.Load += new System.EventHandler(this.NuevaContratacionForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlanes)).EndInit();
            this.panelStatus.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblIdentificacion;
        private System.Windows.Forms.TextBox txtIdentificacion;
        private System.Windows.Forms.Button btnBuscar;
        private System.Windows.Forms.Button btnRegistrarCliente;
        private System.Windows.Forms.ListBox lstCoincidencias;
        private System.Windows.Forms.Label lblFicha;
        private System.Windows.Forms.Label lblPlanes;
        private System.Windows.Forms.Button btnImprimirPlanes;
        private System.Windows.Forms.DataGridView dgvPlanes;
        private System.Windows.Forms.Label lblModalidad;
        private System.Windows.Forms.ComboBox cmbModalidad;
        private System.Windows.Forms.Label lblImporte;
        private System.Windows.Forms.Button btnConfirmar;
        private System.Windows.Forms.Button btnDesistir;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblMensaje;
    }
}
