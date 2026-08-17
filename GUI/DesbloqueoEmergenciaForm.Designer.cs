namespace GUI
{
    partial class DesbloqueoEmergenciaForm
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
            this.lblTitulo      = new System.Windows.Forms.Label();
            this.lblInfo        = new System.Windows.Forms.Label();
            this.lblUsuario     = new System.Windows.Forms.Label();
            this.txtUsuario     = new System.Windows.Forms.TextBox();
            this.lblClave       = new System.Windows.Forms.Label();
            this.txtClave       = new System.Windows.Forms.TextBox();
            this.lblError       = new System.Windows.Forms.Label();
            this.btnDesbloquear = new System.Windows.Forms.Button();
            this.btnCancelar    = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.Font      = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.lblTitulo.Location  = new System.Drawing.Point(20, 18);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Tag       = "emg.encabezado";
            this.lblTitulo.Text      = "Cuenta de Administrador bloqueada";

            // ── lblInfo ────────────────────────────────────────────────────────
            this.lblInfo.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblInfo.Location  = new System.Drawing.Point(22, 50);
            this.lblInfo.Name      = "lblInfo";
            this.lblInfo.Size      = new System.Drawing.Size(386, 40);
            this.lblInfo.TabIndex  = 1;
            this.lblInfo.Tag       = "emg.info";
            this.lblInfo.Text      = "Ingresá tu usuario y una de tus claves de emergencia de un solo uso para desbloquear la cuenta.";

            // ── lblUsuario ─────────────────────────────────────────────────────
            this.lblUsuario.AutoSize = true;
            this.lblUsuario.Location = new System.Drawing.Point(22, 100);
            this.lblUsuario.Name     = "lblUsuario";
            this.lblUsuario.TabIndex = 2;
            this.lblUsuario.Tag      = "emg.usuario";
            this.lblUsuario.Text     = "Usuario:";

            // ── txtUsuario ─────────────────────────────────────────────────────
            this.txtUsuario.Location = new System.Drawing.Point(24, 120);
            this.txtUsuario.Name     = "txtUsuario";
            this.txtUsuario.Size     = new System.Drawing.Size(382, 24);
            this.txtUsuario.TabIndex = 3;

            // ── lblClave ───────────────────────────────────────────────────────
            this.lblClave.AutoSize = true;
            this.lblClave.Location = new System.Drawing.Point(22, 150);
            this.lblClave.Name     = "lblClave";
            this.lblClave.TabIndex = 4;
            this.lblClave.Tag      = "emg.clave";
            this.lblClave.Text     = "Clave de emergencia:";

            // ── txtClave ───────────────────────────────────────────────────────
            this.txtClave.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtClave.Font            = new System.Drawing.Font("Consolas", 11F);
            this.txtClave.Location        = new System.Drawing.Point(24, 170);
            this.txtClave.Name            = "txtClave";
            this.txtClave.Size            = new System.Drawing.Size(382, 29);
            this.txtClave.TabIndex        = 5;

            // ── lblError ───────────────────────────────────────────────────────
            this.lblError.ForeColor = System.Drawing.Color.FromArgb(180, 50, 50);
            this.lblError.Location  = new System.Drawing.Point(22, 200);
            this.lblError.Name      = "lblError";
            this.lblError.Size      = new System.Drawing.Size(386, 32);
            this.lblError.TabIndex  = 6;

            // ── btnDesbloquear ─────────────────────────────────────────────────
            this.btnDesbloquear.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnDesbloquear.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnDesbloquear.FlatAppearance.BorderSize = 0;
            this.btnDesbloquear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDesbloquear.ForeColor = System.Drawing.Color.White;
            this.btnDesbloquear.Location  = new System.Drawing.Point(214, 240);
            this.btnDesbloquear.Name      = "btnDesbloquear";
            this.btnDesbloquear.Size      = new System.Drawing.Size(120, 34);
            this.btnDesbloquear.TabIndex  = 7;
            this.btnDesbloquear.Tag       = "emg.btn.desbloquear";
            this.btnDesbloquear.Text      = "Desbloquear";
            this.btnDesbloquear.UseVisualStyleBackColor = false;
            this.btnDesbloquear.Click += new System.EventHandler(this.BtnDesbloquear_Click);

            // ── btnCancelar ────────────────────────────────────────────────────
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.FlatStyle    = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.Location     = new System.Drawing.Point(340, 240);
            this.btnCancelar.Name         = "btnCancelar";
            this.btnCancelar.Size         = new System.Drawing.Size(66, 34);
            this.btnCancelar.TabIndex     = 8;
            this.btnCancelar.Tag          = "btn.cancelar";
            this.btnCancelar.Text         = "Cancelar";

            // ── DesbloqueoEmergenciaForm ───────────────────────────────────────
            this.AcceptButton    = this.btnDesbloquear;
            this.BackColor       = System.Drawing.Color.White;
            this.CancelButton    = this.btnCancelar;
            this.ClientSize      = new System.Drawing.Size(430, 286);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblUsuario);
            this.Controls.Add(this.txtUsuario);
            this.Controls.Add(this.lblClave);
            this.Controls.Add(this.txtClave);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.btnDesbloquear);
            this.Controls.Add(this.btnCancelar);
            this.Font            = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.Name            = "DesbloqueoEmergenciaForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag             = "emg.titulo";
            this.Text            = "Desbloqueo con clave de emergencia";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label   lblTitulo;
        private System.Windows.Forms.Label   lblInfo;
        private System.Windows.Forms.Label   lblUsuario;
        private System.Windows.Forms.TextBox txtUsuario;
        private System.Windows.Forms.Label   lblClave;
        private System.Windows.Forms.TextBox txtClave;
        private System.Windows.Forms.Label   lblError;
        private System.Windows.Forms.Button  btnDesbloquear;
        private System.Windows.Forms.Button  btnCancelar;
    }
}
