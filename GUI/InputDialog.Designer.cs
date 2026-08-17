namespace GUI
{
    partial class InputDialog
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
            this.lblPrompt   = new System.Windows.Forms.Label();
            this.txtInput    = new System.Windows.Forms.TextBox();
            this.btnAceptar  = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // ── lblPrompt ──────────────────────────────────────────────────────
            this.lblPrompt.AutoSize  = false;
            this.lblPrompt.Location  = new System.Drawing.Point(16, 16);
            this.lblPrompt.Name      = "lblPrompt";
            this.lblPrompt.Size      = new System.Drawing.Size(388, 40);
            this.lblPrompt.TabIndex  = 0;

            // ── txtInput ───────────────────────────────────────────────────────
            this.txtInput.Location = new System.Drawing.Point(18, 60);
            this.txtInput.Name     = "txtInput";
            this.txtInput.Size     = new System.Drawing.Size(384, 24);
            this.txtInput.TabIndex = 1;

            // ── btnAceptar ─────────────────────────────────────────────────────
            this.btnAceptar.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnAceptar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnAceptar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAceptar.FlatAppearance.BorderSize = 0;
            this.btnAceptar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Location  = new System.Drawing.Point(228, 100);
            this.btnAceptar.Name      = "btnAceptar";
            this.btnAceptar.Size      = new System.Drawing.Size(84, 30);
            this.btnAceptar.TabIndex  = 2;
            this.btnAceptar.Text      = "Aceptar";
            this.btnAceptar.UseVisualStyleBackColor = false;

            // ── btnCancelar ────────────────────────────────────────────────────
            this.btnCancelar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(210, 180, 195);
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.btnCancelar.Location  = new System.Drawing.Point(318, 100);
            this.btnCancelar.Name      = "btnCancelar";
            this.btnCancelar.Size      = new System.Drawing.Size(84, 30);
            this.btnCancelar.TabIndex  = 3;
            this.btnCancelar.Text      = "Cancelar";
            this.btnCancelar.UseVisualStyleBackColor = true;

            // ── InputDialog ────────────────────────────────────────────────────
            this.AcceptButton    = this.btnAceptar;
            this.BackColor       = System.Drawing.Color.White;
            this.CancelButton    = this.btnCancelar;
            this.ClientSize      = new System.Drawing.Size(420, 150);
            this.Controls.Add(this.lblPrompt);
            this.Controls.Add(this.txtInput);
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.btnCancelar);
            this.Font            = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.Name            = "InputDialog";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label   lblPrompt;
        private System.Windows.Forms.TextBox txtInput;
        private System.Windows.Forms.Button  btnAceptar;
        private System.Windows.Forms.Button  btnCancelar;
    }
}
