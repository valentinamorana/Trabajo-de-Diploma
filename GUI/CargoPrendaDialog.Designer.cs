namespace GUI
{
    partial class CargoPrendaDialog
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
            this.lblPrendaInfo = new System.Windows.Forms.Label();
            this.lblMotivo     = new System.Windows.Forms.Label();
            this.txtMotivo     = new System.Windows.Forms.TextBox();
            this.lblMonto      = new System.Windows.Forms.Label();
            this.numMonto      = new System.Windows.Forms.NumericUpDown();
            this.lblMensaje    = new System.Windows.Forms.Label();
            this.btnConfirmar  = new System.Windows.Forms.Button();
            this.btnCancelar   = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numMonto)).BeginInit();
            this.SuspendLayout();
            //
            // lblPrendaInfo
            //
            this.lblPrendaInfo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPrendaInfo.Location = new System.Drawing.Point(16, 16);
            this.lblPrendaInfo.Name = "lblPrendaInfo";
            this.lblPrendaInfo.Size = new System.Drawing.Size(340, 40);
            this.lblPrendaInfo.TabIndex = 0;
            this.lblPrendaInfo.Text = "Prenda: —";
            //
            // lblMotivo
            //
            this.lblMotivo.Location = new System.Drawing.Point(16, 62);
            this.lblMotivo.Name = "lblMotivo";
            this.lblMotivo.Size = new System.Drawing.Size(340, 15);
            this.lblMotivo.TabIndex = 1;
            this.lblMotivo.Text = "Motivo (daño o pérdida) *";
            //
            // txtMotivo
            //
            this.txtMotivo.Location = new System.Drawing.Point(16, 80);
            this.txtMotivo.MaxLength = 200;
            this.txtMotivo.Name = "txtMotivo";
            this.txtMotivo.Size = new System.Drawing.Size(340, 20);
            this.txtMotivo.TabIndex = 2;
            //
            // lblMonto
            //
            this.lblMonto.Location = new System.Drawing.Point(16, 114);
            this.lblMonto.Name = "lblMonto";
            this.lblMonto.Size = new System.Drawing.Size(340, 15);
            this.lblMonto.TabIndex = 3;
            this.lblMonto.Text = "Monto a cobrar *";
            //
            // numMonto
            //
            this.numMonto.DecimalPlaces = 2;
            this.numMonto.Location = new System.Drawing.Point(16, 132);
            this.numMonto.Maximum = new decimal(new int[] { 9999999, 0, 0, 0 });
            this.numMonto.Name = "numMonto";
            this.numMonto.Size = new System.Drawing.Size(160, 20);
            this.numMonto.TabIndex = 4;
            this.numMonto.ThousandsSeparator = true;
            //
            // lblMensaje
            //
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.ForeColor = System.Drawing.Color.DarkRed;
            this.lblMensaje.Location = new System.Drawing.Point(16, 160);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(340, 30);
            this.lblMensaje.TabIndex = 5;
            //
            // btnConfirmar
            //
            this.btnConfirmar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnConfirmar.FlatAppearance.BorderSize = 0;
            this.btnConfirmar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirmar.ForeColor = System.Drawing.Color.White;
            this.btnConfirmar.Location = new System.Drawing.Point(16, 196);
            this.btnConfirmar.Name = "btnConfirmar";
            this.btnConfirmar.Size = new System.Drawing.Size(160, 32);
            this.btnConfirmar.TabIndex = 6;
            this.btnConfirmar.Text = "Registrar Cargo";
            this.btnConfirmar.UseVisualStyleBackColor = false;
            this.btnConfirmar.Click += new System.EventHandler(this.BtnConfirmar_Click);
            //
            // btnCancelar
            //
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.Location = new System.Drawing.Point(184, 196);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(100, 32);
            this.btnCancelar.TabIndex = 7;
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.Click += new System.EventHandler(this.BtnCancelar_Click);
            //
            // CargoPrendaDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(380, 246);
            this.Controls.Add(this.lblPrendaInfo);
            this.Controls.Add(this.lblMotivo);
            this.Controls.Add(this.txtMotivo);
            this.Controls.Add(this.lblMonto);
            this.Controls.Add(this.numMonto);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.btnConfirmar);
            this.Controls.Add(this.btnCancelar);
            this.AcceptButton = this.btnConfirmar;
            this.CancelButton = this.btnCancelar;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CargoPrendaDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cargo por Daño/Pérdida";
            ((System.ComponentModel.ISupportInitialize)(this.numMonto)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label         lblPrendaInfo;
        private System.Windows.Forms.Label         lblMotivo;
        private System.Windows.Forms.TextBox       txtMotivo;
        private System.Windows.Forms.Label         lblMonto;
        private System.Windows.Forms.NumericUpDown numMonto;
        private System.Windows.Forms.Label         lblMensaje;
        private System.Windows.Forms.Button        btnConfirmar;
        private System.Windows.Forms.Button        btnCancelar;
    }
}
