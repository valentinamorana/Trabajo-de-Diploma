namespace GUI
{
    partial class SugerirPromocionForm
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
            this.rbPlan = new System.Windows.Forms.RadioButton();
            this.rbCategoria = new System.Windows.Forms.RadioButton();
            this.cmbPlan = new System.Windows.Forms.ComboBox();
            this.txtCategoria = new System.Windows.Forms.TextBox();
            this.lblTipoDescuento = new System.Windows.Forms.Label();
            this.cmbTipoDescuento = new System.Windows.Forms.ComboBox();
            this.lblBeneficioEstimado = new System.Windows.Forms.Label();
            this.numBeneficioEstimado = new System.Windows.Forms.NumericUpDown();
            this.lblMotivo = new System.Windows.Forms.Label();
            this.txtMotivo = new System.Windows.Forms.TextBox();
            this.btnEnviar = new System.Windows.Forms.Button();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numBeneficioEstimado)).BeginInit();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // rbPlan
            //
            this.rbPlan.Checked = true;
            this.rbPlan.TabStop = true;
            this.rbPlan.Location = new System.Drawing.Point(16, 16);
            this.rbPlan.Name = "rbPlan";
            this.rbPlan.Size = new System.Drawing.Size(90, 24);
            this.rbPlan.TabIndex = 0;
            this.rbPlan.Tag = "promocion.plan";
            this.rbPlan.Text = "Plan:";
            this.rbPlan.CheckedChanged += new System.EventHandler(this.RbPlan_CheckedChanged);
            //
            // cmbPlan
            //
            this.cmbPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlan.Location = new System.Drawing.Point(112, 16);
            this.cmbPlan.Name = "cmbPlan";
            this.cmbPlan.Size = new System.Drawing.Size(220, 21);
            this.cmbPlan.TabIndex = 1;
            //
            // rbCategoria
            //
            this.rbCategoria.Location = new System.Drawing.Point(16, 46);
            this.rbCategoria.Name = "rbCategoria";
            this.rbCategoria.Size = new System.Drawing.Size(90, 24);
            this.rbCategoria.TabIndex = 2;
            this.rbCategoria.Tag = "promocion.categoria";
            this.rbCategoria.Text = "Categoría:";
            //
            // txtCategoria
            //
            this.txtCategoria.Enabled = false;
            this.txtCategoria.Location = new System.Drawing.Point(112, 48);
            this.txtCategoria.Name = "txtCategoria";
            this.txtCategoria.Size = new System.Drawing.Size(220, 20);
            this.txtCategoria.TabIndex = 3;
            //
            // lblTipoDescuento
            //
            this.lblTipoDescuento.Location = new System.Drawing.Point(16, 80);
            this.lblTipoDescuento.Name = "lblTipoDescuento";
            this.lblTipoDescuento.Size = new System.Drawing.Size(90, 23);
            this.lblTipoDescuento.TabIndex = 4;
            this.lblTipoDescuento.Tag = "promocion.tiposugerido";
            this.lblTipoDescuento.Text = "Tipo sugerido:";
            this.lblTipoDescuento.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbTipoDescuento
            //
            this.cmbTipoDescuento.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTipoDescuento.Location = new System.Drawing.Point(112, 78);
            this.cmbTipoDescuento.Name = "cmbTipoDescuento";
            this.cmbTipoDescuento.Size = new System.Drawing.Size(160, 21);
            this.cmbTipoDescuento.TabIndex = 5;
            //
            // lblBeneficioEstimado
            //
            this.lblBeneficioEstimado.Location = new System.Drawing.Point(16, 110);
            this.lblBeneficioEstimado.Name = "lblBeneficioEstimado";
            this.lblBeneficioEstimado.Size = new System.Drawing.Size(90, 23);
            this.lblBeneficioEstimado.TabIndex = 6;
            this.lblBeneficioEstimado.Tag = "promocion.beneficioestimado";
            this.lblBeneficioEstimado.Text = "Beneficio est.:";
            this.lblBeneficioEstimado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numBeneficioEstimado
            //
            this.numBeneficioEstimado.DecimalPlaces = 2;
            this.numBeneficioEstimado.Location = new System.Drawing.Point(112, 108);
            this.numBeneficioEstimado.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numBeneficioEstimado.Name = "numBeneficioEstimado";
            this.numBeneficioEstimado.Size = new System.Drawing.Size(120, 20);
            this.numBeneficioEstimado.TabIndex = 7;
            //
            // lblMotivo
            //
            this.lblMotivo.Location = new System.Drawing.Point(16, 140);
            this.lblMotivo.Name = "lblMotivo";
            this.lblMotivo.Size = new System.Drawing.Size(90, 23);
            this.lblMotivo.TabIndex = 8;
            this.lblMotivo.Tag = "lbl.motivo";
            this.lblMotivo.Text = "Motivo:";
            //
            // txtMotivo
            //
            this.txtMotivo.Location = new System.Drawing.Point(112, 140);
            this.txtMotivo.Multiline = true;
            this.txtMotivo.Name = "txtMotivo";
            this.txtMotivo.Size = new System.Drawing.Size(320, 60);
            this.txtMotivo.TabIndex = 9;
            //
            // btnEnviar
            //
            this.btnEnviar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(140)))), ((int)(((byte)(60)))));
            this.btnEnviar.FlatAppearance.BorderSize = 0;
            this.btnEnviar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEnviar.ForeColor = System.Drawing.Color.White;
            this.btnEnviar.Location = new System.Drawing.Point(112, 210);
            this.btnEnviar.Name = "btnEnviar";
            this.btnEnviar.Size = new System.Drawing.Size(160, 30);
            this.btnEnviar.TabIndex = 10;
            this.btnEnviar.Tag = "promocion.btn.enviarsugerencia";
            this.btnEnviar.Text = "Enviar Sugerencia";
            this.btnEnviar.UseVisualStyleBackColor = false;
            this.btnEnviar.Click += new System.EventHandler(this.BtnEnviar_Click);
            //
            // panelStatus
            //
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 258);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.panelStatus.Size = new System.Drawing.Size(452, 26);
            this.panelStatus.TabIndex = 11;
            //
            // lblMensaje
            //
            this.lblMensaje.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.Location = new System.Drawing.Point(8, 4);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(436, 18);
            this.lblMensaje.TabIndex = 0;
            //
            // SugerirPromocionForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 284);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.btnEnviar);
            this.AcceptButton = this.btnEnviar;
            this.Controls.Add(this.txtMotivo);
            this.Controls.Add(this.lblMotivo);
            this.Controls.Add(this.numBeneficioEstimado);
            this.Controls.Add(this.lblBeneficioEstimado);
            this.Controls.Add(this.cmbTipoDescuento);
            this.Controls.Add(this.lblTipoDescuento);
            this.Controls.Add(this.txtCategoria);
            this.Controls.Add(this.rbCategoria);
            this.Controls.Add(this.cmbPlan);
            this.Controls.Add(this.rbPlan);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SugerirPromocionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag = "frm.sugerirpromocion";
            this.Text = "Sugerir Promoción a Administración";
            this.Load += new System.EventHandler(this.SugerirPromocionForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numBeneficioEstimado)).EndInit();
            this.panelStatus.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton rbPlan;
        private System.Windows.Forms.RadioButton rbCategoria;
        private System.Windows.Forms.ComboBox cmbPlan;
        private System.Windows.Forms.TextBox txtCategoria;
        private System.Windows.Forms.Label lblTipoDescuento;
        private System.Windows.Forms.ComboBox cmbTipoDescuento;
        private System.Windows.Forms.Label lblBeneficioEstimado;
        private System.Windows.Forms.NumericUpDown numBeneficioEstimado;
        private System.Windows.Forms.Label lblMotivo;
        private System.Windows.Forms.TextBox txtMotivo;
        private System.Windows.Forms.Button btnEnviar;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblMensaje;
    }
}
