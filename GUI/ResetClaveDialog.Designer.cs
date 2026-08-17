namespace GUI
{
    partial class ResetClaveDialog
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
            this.lblTitulo     = new System.Windows.Forms.Label();
            this.lblUsuario    = new System.Windows.Forms.Label();
            this.lblNueva      = new System.Windows.Forms.Label();
            this.txtNuevaClave = new System.Windows.Forms.TextBox();
            this.lblConfirmar  = new System.Windows.Forms.Label();
            this.txtConfirmar  = new System.Windows.Forms.TextBox();
            this.lblError      = new System.Windows.Forms.Label();
            this.btnAceptar    = new System.Windows.Forms.Button();
            this.btnCancelar   = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.Font      = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(30, 30, 60);
            this.lblTitulo.Location  = new System.Drawing.Point(20, 18);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.Size      = new System.Drawing.Size(300, 24);
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Tag       = "frm.resetclave";
            this.lblTitulo.Text      = "Resetear Contraseña";

            // ── lblUsuario — nombre del usuario afectado (se completa en el ctor) ─
            this.lblUsuario.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblUsuario.ForeColor = System.Drawing.Color.DimGray;
            this.lblUsuario.Location  = new System.Drawing.Point(20, 50);
            this.lblUsuario.Name      = "lblUsuario";
            this.lblUsuario.Size      = new System.Drawing.Size(300, 20);
            this.lblUsuario.TabIndex  = 1;

            // ── lblNueva ───────────────────────────────────────────────────────
            this.lblNueva.Location  = new System.Drawing.Point(20, 82);
            this.lblNueva.Name      = "lblNueva";
            this.lblNueva.Size      = new System.Drawing.Size(300, 18);
            this.lblNueva.TabIndex  = 2;
            this.lblNueva.Tag       = "lbl.nueva.clave";
            this.lblNueva.Text      = "Nueva contraseña (mín. 6 caracteres):";

            // ── txtNuevaClave ──────────────────────────────────────────────────
            this.txtNuevaClave.Location     = new System.Drawing.Point(20, 102);
            this.txtNuevaClave.Name         = "txtNuevaClave";
            this.txtNuevaClave.PasswordChar = '●';
            this.txtNuevaClave.Size         = new System.Drawing.Size(300, 24);
            this.txtNuevaClave.TabIndex     = 3;

            // ── lblConfirmar ───────────────────────────────────────────────────
            this.lblConfirmar.Location  = new System.Drawing.Point(20, 136);
            this.lblConfirmar.Name      = "lblConfirmar";
            this.lblConfirmar.Size      = new System.Drawing.Size(300, 18);
            this.lblConfirmar.TabIndex  = 4;
            this.lblConfirmar.Tag       = "lbl.confirmar.clave";
            this.lblConfirmar.Text      = "Confirmar contraseña:";

            // ── txtConfirmar ───────────────────────────────────────────────────
            this.txtConfirmar.Location     = new System.Drawing.Point(20, 156);
            this.txtConfirmar.Name         = "txtConfirmar";
            this.txtConfirmar.PasswordChar = '●';
            this.txtConfirmar.Size         = new System.Drawing.Size(300, 24);
            this.txtConfirmar.TabIndex     = 5;

            // ── lblError — oculto hasta que falle la validación (texto vacío) ────
            this.lblError.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblError.ForeColor = System.Drawing.Color.Crimson;
            this.lblError.Location  = new System.Drawing.Point(20, 186);
            this.lblError.Name      = "lblError";
            this.lblError.Size      = new System.Drawing.Size(300, 18);
            this.lblError.TabIndex  = 6;

            // ── btnAceptar — DialogResult.None: la validación la maneja el handler ─
            this.btnAceptar.BackColor = System.Drawing.Color.FromArgb(180, 100, 30);
            this.btnAceptar.FlatAppearance.BorderSize = 0;
            this.btnAceptar.FlatStyle   = System.Windows.Forms.FlatStyle.Flat;
            this.btnAceptar.ForeColor   = System.Drawing.Color.White;
            this.btnAceptar.Location    = new System.Drawing.Point(20, 214);
            this.btnAceptar.Name        = "btnAceptar";
            this.btnAceptar.Size        = new System.Drawing.Size(145, 32);
            this.btnAceptar.TabIndex    = 7;
            this.btnAceptar.Tag         = "btn.confirmar.reset";
            this.btnAceptar.Text        = "Confirmar Reset";
            this.btnAceptar.DialogResult = System.Windows.Forms.DialogResult.None;
            this.btnAceptar.Click      += new System.EventHandler(this.BtnAceptar_Click);

            // ── btnCancelar ────────────────────────────────────────────────────
            this.btnCancelar.Location     = new System.Drawing.Point(175, 214);
            this.btnCancelar.Name         = "btnCancelar";
            this.btnCancelar.Size         = new System.Drawing.Size(145, 32);
            this.btnCancelar.TabIndex     = 8;
            this.btnCancelar.Tag          = "btn.cancelar";
            this.btnCancelar.Text         = "Cancelar";
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // ── ResetClaveDialog ───────────────────────────────────────────────
            this.AcceptButton     = this.btnAceptar;
            this.AutoScaleMode    = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor        = System.Drawing.Color.White;
            this.CancelButton     = this.btnCancelar;
            this.ClientSize       = new System.Drawing.Size(340, 260);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblUsuario);
            this.Controls.Add(this.lblNueva);
            this.Controls.Add(this.txtNuevaClave);
            this.Controls.Add(this.lblConfirmar);
            this.Controls.Add(this.txtConfirmar);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.btnCancelar);
            this.FormBorderStyle  = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox      = false;
            this.MinimizeBox      = false;
            this.Name             = "ResetClaveDialog";
            this.StartPosition    = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag              = "frm.resetclave";
            this.Text             = "Resetear Contraseña";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label   lblTitulo;
        private System.Windows.Forms.Label   lblUsuario;
        private System.Windows.Forms.Label   lblNueva;
        private System.Windows.Forms.TextBox txtNuevaClave;
        private System.Windows.Forms.Label   lblConfirmar;
        private System.Windows.Forms.TextBox txtConfirmar;
        private System.Windows.Forms.Label   lblError;
        private System.Windows.Forms.Button  btnAceptar;
        private System.Windows.Forms.Button  btnCancelar;
    }
}
