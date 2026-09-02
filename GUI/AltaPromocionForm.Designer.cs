namespace GUI
{
    partial class AltaPromocionForm
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
            this.lblSugerencia = new System.Windows.Forms.Label();
            this.lblNombre = new System.Windows.Forms.Label();
            this.txtNombre = new System.Windows.Forms.TextBox();
            this.lblDescripcion = new System.Windows.Forms.Label();
            this.txtDescripcion = new System.Windows.Forms.TextBox();
            this.rbPlan = new System.Windows.Forms.RadioButton();
            this.cmbPlan = new System.Windows.Forms.ComboBox();
            this.rbCategoria = new System.Windows.Forms.RadioButton();
            this.txtCategoria = new System.Windows.Forms.TextBox();
            this.lblTipoDescuento = new System.Windows.Forms.Label();
            this.cmbTipoDescuento = new System.Windows.Forms.ComboBox();
            this.lblValor = new System.Windows.Forms.Label();
            this.numValor = new System.Windows.Forms.NumericUpDown();
            this.lblInicio = new System.Windows.Forms.Label();
            this.dtpInicio = new System.Windows.Forms.DateTimePicker();
            this.lblFin = new System.Windows.Forms.Label();
            this.dtpFin = new System.Windows.Forms.DateTimePicker();
            this.lblMargenEstimado = new System.Windows.Forms.Label();
            this.numMargenEstimado = new System.Windows.Forms.NumericUpDown();
            this.lblImpactoEconomico = new System.Windows.Forms.Label();
            this.txtImpactoEconomico = new System.Windows.Forms.TextBox();
            this.btnConfirmar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numValor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargenEstimado)).BeginInit();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // lblSugerencia
            //
            this.lblSugerencia.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic);
            this.lblSugerencia.ForeColor = System.Drawing.Color.DimGray;
            this.lblSugerencia.Location = new System.Drawing.Point(12, 10);
            this.lblSugerencia.Name = "lblSugerencia";
            this.lblSugerencia.Size = new System.Drawing.Size(420, 32);
            this.lblSugerencia.TabIndex = 0;
            //
            // lblNombre
            //
            this.lblNombre.Location = new System.Drawing.Point(12, 48);
            this.lblNombre.Name = "lblNombre";
            this.lblNombre.Size = new System.Drawing.Size(90, 23);
            this.lblNombre.TabIndex = 1;
            this.lblNombre.Tag = "promocion.nombre";
            this.lblNombre.Text = "Nombre:";
            //
            // txtNombre
            //
            this.txtNombre.Location = new System.Drawing.Point(108, 46);
            this.txtNombre.Name = "txtNombre";
            this.txtNombre.Size = new System.Drawing.Size(320, 20);
            this.txtNombre.TabIndex = 2;
            //
            // lblDescripcion
            //
            this.lblDescripcion.Location = new System.Drawing.Point(12, 74);
            this.lblDescripcion.Name = "lblDescripcion";
            this.lblDescripcion.Size = new System.Drawing.Size(90, 23);
            this.lblDescripcion.TabIndex = 3;
            this.lblDescripcion.Tag = "promocion.descripcion";
            this.lblDescripcion.Text = "Descripción:";
            //
            // txtDescripcion
            //
            this.txtDescripcion.Location = new System.Drawing.Point(108, 72);
            this.txtDescripcion.Name = "txtDescripcion";
            this.txtDescripcion.Size = new System.Drawing.Size(320, 20);
            this.txtDescripcion.TabIndex = 4;
            //
            // rbPlan
            //
            this.rbPlan.Checked = true;
            this.rbPlan.TabStop = true;
            this.rbPlan.Location = new System.Drawing.Point(12, 100);
            this.rbPlan.Name = "rbPlan";
            this.rbPlan.Size = new System.Drawing.Size(90, 24);
            this.rbPlan.TabIndex = 5;
            this.rbPlan.Tag = "promocion.plan";
            this.rbPlan.Text = "Plan:";
            this.rbPlan.CheckedChanged += new System.EventHandler(this.RbPlan_CheckedChanged);
            //
            // cmbPlan
            //
            this.cmbPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlan.Location = new System.Drawing.Point(108, 100);
            this.cmbPlan.Name = "cmbPlan";
            this.cmbPlan.Size = new System.Drawing.Size(220, 21);
            this.cmbPlan.TabIndex = 6;
            //
            // rbCategoria
            //
            this.rbCategoria.Location = new System.Drawing.Point(12, 128);
            this.rbCategoria.Name = "rbCategoria";
            this.rbCategoria.Size = new System.Drawing.Size(90, 24);
            this.rbCategoria.TabIndex = 7;
            this.rbCategoria.Tag = "promocion.categoria";
            this.rbCategoria.Text = "Categoría:";
            //
            // txtCategoria
            //
            this.txtCategoria.Enabled = false;
            this.txtCategoria.Location = new System.Drawing.Point(108, 130);
            this.txtCategoria.Name = "txtCategoria";
            this.txtCategoria.Size = new System.Drawing.Size(220, 20);
            this.txtCategoria.TabIndex = 8;
            //
            // lblTipoDescuento
            //
            this.lblTipoDescuento.Location = new System.Drawing.Point(12, 160);
            this.lblTipoDescuento.Name = "lblTipoDescuento";
            this.lblTipoDescuento.Size = new System.Drawing.Size(90, 23);
            this.lblTipoDescuento.TabIndex = 9;
            this.lblTipoDescuento.Tag = "promocion.tipo";
            this.lblTipoDescuento.Text = "Tipo:";
            //
            // cmbTipoDescuento
            //
            this.cmbTipoDescuento.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTipoDescuento.Location = new System.Drawing.Point(108, 158);
            this.cmbTipoDescuento.Name = "cmbTipoDescuento";
            this.cmbTipoDescuento.Size = new System.Drawing.Size(150, 21);
            this.cmbTipoDescuento.TabIndex = 10;
            //
            // lblValor
            //
            this.lblValor.Location = new System.Drawing.Point(268, 160);
            this.lblValor.Name = "lblValor";
            this.lblValor.Size = new System.Drawing.Size(50, 23);
            this.lblValor.TabIndex = 11;
            this.lblValor.Tag = "promocion.valor";
            this.lblValor.Text = "Valor:";
            //
            // numValor
            //
            this.numValor.DecimalPlaces = 2;
            this.numValor.Location = new System.Drawing.Point(322, 159);
            this.numValor.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numValor.Name = "numValor";
            this.numValor.Size = new System.Drawing.Size(106, 20);
            this.numValor.TabIndex = 12;
            //
            // lblInicio
            //
            this.lblInicio.Location = new System.Drawing.Point(12, 188);
            this.lblInicio.Name = "lblInicio";
            this.lblInicio.Size = new System.Drawing.Size(90, 23);
            this.lblInicio.TabIndex = 13;
            this.lblInicio.Tag = "promocion.vigenciadesde";
            this.lblInicio.Text = "Vigencia desde:";
            //
            // dtpInicio
            //
            this.dtpInicio.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpInicio.Location = new System.Drawing.Point(108, 186);
            this.dtpInicio.Name = "dtpInicio";
            this.dtpInicio.Size = new System.Drawing.Size(120, 20);
            this.dtpInicio.TabIndex = 14;
            //
            // lblFin
            //
            this.lblFin.Location = new System.Drawing.Point(240, 188);
            this.lblFin.Name = "lblFin";
            this.lblFin.Size = new System.Drawing.Size(40, 23);
            this.lblFin.TabIndex = 15;
            this.lblFin.Tag = "promocion.hasta";
            this.lblFin.Text = "hasta:";
            //
            // dtpFin
            //
            this.dtpFin.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFin.Location = new System.Drawing.Point(284, 186);
            this.dtpFin.Name = "dtpFin";
            this.dtpFin.Size = new System.Drawing.Size(120, 20);
            this.dtpFin.TabIndex = 16;
            //
            // lblMargenEstimado
            //
            this.lblMargenEstimado.Location = new System.Drawing.Point(12, 216);
            this.lblMargenEstimado.Name = "lblMargenEstimado";
            this.lblMargenEstimado.Size = new System.Drawing.Size(90, 23);
            this.lblMargenEstimado.TabIndex = 17;
            this.lblMargenEstimado.Tag = "promocion.margenestimado";
            this.lblMargenEstimado.Text = "Margen est.:";
            //
            // numMargenEstimado
            //
            this.numMargenEstimado.DecimalPlaces = 2;
            this.numMargenEstimado.Location = new System.Drawing.Point(108, 214);
            this.numMargenEstimado.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numMargenEstimado.Name = "numMargenEstimado";
            this.numMargenEstimado.Size = new System.Drawing.Size(120, 20);
            this.numMargenEstimado.TabIndex = 18;
            //
            // lblImpactoEconomico
            //
            this.lblImpactoEconomico.Location = new System.Drawing.Point(12, 244);
            this.lblImpactoEconomico.Name = "lblImpactoEconomico";
            this.lblImpactoEconomico.Size = new System.Drawing.Size(90, 23);
            this.lblImpactoEconomico.TabIndex = 19;
            this.lblImpactoEconomico.Tag = "promocion.impactoeconomico";
            this.lblImpactoEconomico.Text = "Impacto econ.:";
            //
            // txtImpactoEconomico
            //
            this.txtImpactoEconomico.Location = new System.Drawing.Point(108, 242);
            this.txtImpactoEconomico.Multiline = true;
            this.txtImpactoEconomico.Name = "txtImpactoEconomico";
            this.txtImpactoEconomico.Size = new System.Drawing.Size(320, 44);
            this.txtImpactoEconomico.TabIndex = 20;
            //
            // btnConfirmar
            //
            this.btnConfirmar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(140)))), ((int)(((byte)(60)))));
            this.btnConfirmar.FlatAppearance.BorderSize = 0;
            this.btnConfirmar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirmar.ForeColor = System.Drawing.Color.White;
            this.btnConfirmar.Location = new System.Drawing.Point(108, 296);
            this.btnConfirmar.Name = "btnConfirmar";
            this.btnConfirmar.Size = new System.Drawing.Size(120, 30);
            this.btnConfirmar.TabIndex = 21;
            this.btnConfirmar.Tag = "promocion.btn.registrar";
            this.btnConfirmar.Text = "Registrar";
            this.btnConfirmar.UseVisualStyleBackColor = false;
            this.btnConfirmar.Click += new System.EventHandler(this.BtnConfirmar_Click);
            //
            // btnCancelar
            //
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.Location = new System.Drawing.Point(236, 296);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(100, 30);
            this.btnCancelar.TabIndex = 22;
            this.btnCancelar.Tag = "btn.cancelar";
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.Click += new System.EventHandler(this.BtnCancelar_Click);
            //
            // panelStatus
            //
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 344);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.panelStatus.Size = new System.Drawing.Size(452, 26);
            this.panelStatus.TabIndex = 23;
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
            // AltaPromocionForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 370);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.btnConfirmar);
            this.AcceptButton = this.btnConfirmar;
            this.CancelButton = this.btnCancelar;
            this.Controls.Add(this.txtImpactoEconomico);
            this.Controls.Add(this.lblImpactoEconomico);
            this.Controls.Add(this.numMargenEstimado);
            this.Controls.Add(this.lblMargenEstimado);
            this.Controls.Add(this.dtpFin);
            this.Controls.Add(this.lblFin);
            this.Controls.Add(this.dtpInicio);
            this.Controls.Add(this.lblInicio);
            this.Controls.Add(this.numValor);
            this.Controls.Add(this.lblValor);
            this.Controls.Add(this.cmbTipoDescuento);
            this.Controls.Add(this.lblTipoDescuento);
            this.Controls.Add(this.txtCategoria);
            this.Controls.Add(this.rbCategoria);
            this.Controls.Add(this.cmbPlan);
            this.Controls.Add(this.rbPlan);
            this.Controls.Add(this.txtDescripcion);
            this.Controls.Add(this.lblDescripcion);
            this.Controls.Add(this.txtNombre);
            this.Controls.Add(this.lblNombre);
            this.Controls.Add(this.lblSugerencia);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AltaPromocionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag = "frm.altapromocion";
            this.Text = "Alta de Promoción";
            this.Load += new System.EventHandler(this.AltaPromocionForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numValor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargenEstimado)).EndInit();
            this.panelStatus.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblSugerencia;
        private System.Windows.Forms.Label lblNombre;
        private System.Windows.Forms.TextBox txtNombre;
        private System.Windows.Forms.Label lblDescripcion;
        private System.Windows.Forms.TextBox txtDescripcion;
        private System.Windows.Forms.RadioButton rbPlan;
        private System.Windows.Forms.ComboBox cmbPlan;
        private System.Windows.Forms.RadioButton rbCategoria;
        private System.Windows.Forms.TextBox txtCategoria;
        private System.Windows.Forms.Label lblTipoDescuento;
        private System.Windows.Forms.ComboBox cmbTipoDescuento;
        private System.Windows.Forms.Label lblValor;
        private System.Windows.Forms.NumericUpDown numValor;
        private System.Windows.Forms.Label lblInicio;
        private System.Windows.Forms.DateTimePicker dtpInicio;
        private System.Windows.Forms.Label lblFin;
        private System.Windows.Forms.DateTimePicker dtpFin;
        private System.Windows.Forms.Label lblMargenEstimado;
        private System.Windows.Forms.NumericUpDown numMargenEstimado;
        private System.Windows.Forms.Label lblImpactoEconomico;
        private System.Windows.Forms.TextBox txtImpactoEconomico;
        private System.Windows.Forms.Button btnConfirmar;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblMensaje;
    }
}
