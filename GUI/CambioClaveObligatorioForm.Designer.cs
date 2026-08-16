namespace GUI
{
    partial class CambioClaveObligatorioForm
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
            this.lblInfo     = new System.Windows.Forms.Label();
            this.lblNueva    = new System.Windows.Forms.Label();
            this.txtNueva    = new System.Windows.Forms.TextBox();
            this.lblRepetir  = new System.Windows.Forms.Label();
            this.txtRepetir  = new System.Windows.Forms.TextBox();
            this.lblReglas   = new System.Windows.Forms.Label();
            this.lblError    = new System.Windows.Forms.Label();
            this.btnCambiar  = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // ── lblInfo ────────────────────────────────────────────────────────
            this.lblInfo.Location = new System.Drawing.Point(16, 14);
            this.lblInfo.Name     = "lblInfo";
            this.lblInfo.Size     = new System.Drawing.Size(398, 36);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Tag      = "lbl.cambioclave.info";
            this.lblInfo.Text     = "Tu contraseña es temporal. Definí una nueva para continuar.";

            // ── lblNueva ───────────────────────────────────────────────────────
            this.lblNueva.Location = new System.Drawing.Point(16, 58);
            this.lblNueva.Name     = "lblNueva";
            this.lblNueva.Size     = new System.Drawing.Size(398, 18);
            this.lblNueva.TabIndex = 1;
            this.lblNueva.Tag      = "lbl.cambioclave.nueva";
            this.lblNueva.Text     = "Nueva contraseña:";

            // ── txtNueva ───────────────────────────────────────────────────────
            this.txtNueva.Location = new System.Drawing.Point(18, 78);
            this.txtNueva.Name     = "txtNueva";
            this.txtNueva.Size     = new System.Drawing.Size(394, 24);
            this.txtNueva.TabIndex = 2;
            this.txtNueva.UseSystemPasswordChar = true;

            // ── lblRepetir ─────────────────────────────────────────────────────
            this.lblRepetir.Location = new System.Drawing.Point(16, 110);
            this.lblRepetir.Name     = "lblRepetir";
            this.lblRepetir.Size     = new System.Drawing.Size(398, 18);
            this.lblRepetir.TabIndex = 3;
            this.lblRepetir.Tag      = "lbl.cambioclave.repetir";
            this.lblRepetir.Text     = "Repetir contraseña:";

            // ── txtRepetir ─────────────────────────────────────────────────────
            this.txtRepetir.Location = new System.Drawing.Point(18, 130);
            this.txtRepetir.Name     = "txtRepetir";
            this.txtRepetir.Size     = new System.Drawing.Size(394, 24);
            this.txtRepetir.TabIndex = 4;
            this.txtRepetir.UseSystemPasswordChar = true;

            // ── lblReglas ──────────────────────────────────────────────────────
            this.lblReglas.Font      = new System.Drawing.Font("Segoe UI", 8F);
            this.lblReglas.ForeColor = System.Drawing.Color.FromArgb(150, 130, 142);
            this.lblReglas.Location  = new System.Drawing.Point(16, 158);
            this.lblReglas.Name      = "lblReglas";
            this.lblReglas.Size      = new System.Drawing.Size(398, 18);
            this.lblReglas.TabIndex  = 5;
            this.lblReglas.Tag       = "lbl.cambioclave.reglas";
            this.lblReglas.Text      = "Mínimo 8 caracteres, con al menos un número y un carácter especial.";

            // ── lblError ───────────────────────────────────────────────────────
            this.lblError.ForeColor = System.Drawing.Color.FromArgb(180, 50, 50);
            this.lblError.Location  = new System.Drawing.Point(16, 178);
            this.lblError.Name      = "lblError";
            this.lblError.Size      = new System.Drawing.Size(398, 18);
            this.lblError.TabIndex  = 6;

            // ── btnCambiar ─────────────────────────────────────────────────────
            this.btnCambiar.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnCambiar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCambiar.FlatAppearance.BorderSize = 0;
            this.btnCambiar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCambiar.ForeColor = System.Drawing.Color.White;
            this.btnCambiar.Location  = new System.Drawing.Point(238, 206);
            this.btnCambiar.Name      = "btnCambiar";
            this.btnCambiar.Size      = new System.Drawing.Size(84, 30);
            this.btnCambiar.TabIndex  = 7;
            this.btnCambiar.Tag       = "btn.cambioclave.cambiar";
            this.btnCambiar.Text      = "Cambiar";
            this.btnCambiar.UseVisualStyleBackColor = false;
            this.btnCambiar.Click += new System.EventHandler(this.BtnCambiar_Click);

            // ── btnCancelar ────────────────────────────────────────────────────
            this.btnCancelar.BackColor = System.Drawing.Color.White;
            this.btnCancelar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(210, 180, 195);
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.btnCancelar.Location  = new System.Drawing.Point(328, 206);
            this.btnCancelar.Name      = "btnCancelar";
            this.btnCancelar.Size      = new System.Drawing.Size(84, 30);
            this.btnCancelar.TabIndex  = 8;
            this.btnCancelar.Tag       = "btn.cambioclave.cancelar";
            this.btnCancelar.Text      = "Cancelar";
            this.btnCancelar.UseVisualStyleBackColor = false;

            // ── CambioClaveObligatorioForm ─────────────────────────────────────
            this.AcceptButton    = this.btnCambiar;
            this.BackColor       = System.Drawing.Color.White;
            this.CancelButton    = this.btnCancelar;
            this.ClientSize      = new System.Drawing.Size(430, 250);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblNueva);
            this.Controls.Add(this.txtNueva);
            this.Controls.Add(this.lblRepetir);
            this.Controls.Add(this.txtRepetir);
            this.Controls.Add(this.lblReglas);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.btnCambiar);
            this.Controls.Add(this.btnCancelar);
            this.Font            = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.Name            = "CambioClaveObligatorioForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag             = "frm.cambioclave.titulo";
            this.Text            = "Cambio de contraseña obligatorio";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label   lblInfo;
        private System.Windows.Forms.Label   lblNueva;
        private System.Windows.Forms.TextBox txtNueva;
        private System.Windows.Forms.Label   lblRepetir;
        private System.Windows.Forms.TextBox txtRepetir;
        private System.Windows.Forms.Label   lblReglas;
        private System.Windows.Forms.Label   lblError;
        private System.Windows.Forms.Button  btnCambiar;
        private System.Windows.Forms.Button  btnCancelar;
    }
}
