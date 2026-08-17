namespace GUI
{
    partial class MiPerfilForm
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
            this.lblUsuarioCap = new System.Windows.Forms.Label();
            this.lblUsuarioVal = new System.Windows.Forms.Label();
            this.lblPerfilCap  = new System.Windows.Forms.Label();
            this.lblPerfilVal  = new System.Windows.Forms.Label();
            this.lblSeccion    = new System.Windows.Forms.Label();
            this.lblIdiomaCap  = new System.Windows.Forms.Label();
            this.cmbIdioma     = new System.Windows.Forms.ComboBox();
            this.lblFuenteCap  = new System.Windows.Forms.Label();
            this.cmbFuente     = new System.Windows.Forms.ComboBox();
            this.lblTamanoCap  = new System.Windows.Forms.Label();
            this.cmbTamano     = new System.Windows.Forms.ComboBox();
            this.lblTemaCap    = new System.Windows.Forms.Label();
            this.cmbTema       = new System.Windows.Forms.ComboBox();
            this.lblFechaCap   = new System.Windows.Forms.Label();
            this.cmbFecha      = new System.Windows.Forms.ComboBox();
            this.chkNotif      = new System.Windows.Forms.CheckBox();
            this.btnGuardar    = new System.Windows.Forms.Button();
            this.btnDefault    = new System.Windows.Forms.Button();
            this.lblEstado     = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.lblTitulo.Location  = new System.Drawing.Point(20, 14);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Tag       = "perfil.frm.titulo";
            this.lblTitulo.Text      = "Mi Perfil";

            // ── lblUsuarioCap ──────────────────────────────────────────────────
            this.lblUsuarioCap.AutoSize  = true;
            this.lblUsuarioCap.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblUsuarioCap.Location  = new System.Drawing.Point(24, 52);
            this.lblUsuarioCap.Name      = "lblUsuarioCap";
            this.lblUsuarioCap.TabIndex  = 1;
            this.lblUsuarioCap.Tag       = "perfil.usuario";
            this.lblUsuarioCap.Text      = "Usuario:";

            // ── lblUsuarioVal — el texto final (username) se completa en el constructor ─
            this.lblUsuarioVal.AutoSize = true;
            this.lblUsuarioVal.Font     = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUsuarioVal.Location = new System.Drawing.Point(170, 52);
            this.lblUsuarioVal.Name     = "lblUsuarioVal";
            this.lblUsuarioVal.TabIndex = 2;

            // ── lblPerfilCap ───────────────────────────────────────────────────
            this.lblPerfilCap.AutoSize  = true;
            this.lblPerfilCap.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblPerfilCap.Location  = new System.Drawing.Point(24, 76);
            this.lblPerfilCap.Name      = "lblPerfilCap";
            this.lblPerfilCap.TabIndex  = 3;
            this.lblPerfilCap.Tag       = "perfil.perfil";
            this.lblPerfilCap.Text      = "Perfil / Rol:";

            // ── lblPerfilVal — el texto final (perfil) se completa en el constructor ─
            this.lblPerfilVal.AutoSize = true;
            this.lblPerfilVal.Font     = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPerfilVal.Location = new System.Drawing.Point(170, 76);
            this.lblPerfilVal.Name     = "lblPerfilVal";
            this.lblPerfilVal.TabIndex = 4;

            // ── lblSeccion ─────────────────────────────────────────────────────
            this.lblSeccion.AutoSize  = true;
            this.lblSeccion.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSeccion.ForeColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.lblSeccion.Location  = new System.Drawing.Point(20, 108);
            this.lblSeccion.Name      = "lblSeccion";
            this.lblSeccion.TabIndex  = 5;
            this.lblSeccion.Tag       = "perfil.seccion";
            this.lblSeccion.Text      = "Preferencias";

            // ── lblIdiomaCap / cmbIdioma — Items se cargan en vivo desde BD ─────
            this.lblIdiomaCap.AutoSize  = true;
            this.lblIdiomaCap.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblIdiomaCap.Location  = new System.Drawing.Point(24, 141);
            this.lblIdiomaCap.Name      = "lblIdiomaCap";
            this.lblIdiomaCap.TabIndex  = 6;
            this.lblIdiomaCap.Tag       = "perfil.idioma";
            this.lblIdiomaCap.Text      = "Idioma preferido:";

            this.cmbIdioma.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbIdioma.Location      = new System.Drawing.Point(170, 138);
            this.cmbIdioma.Name          = "cmbIdioma";
            this.cmbIdioma.Size          = new System.Drawing.Size(244, 21);
            this.cmbIdioma.TabIndex      = 7;

            // ── lblFuenteCap / cmbFuente ───────────────────────────────────────
            this.lblFuenteCap.AutoSize  = true;
            this.lblFuenteCap.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblFuenteCap.Location  = new System.Drawing.Point(24, 173);
            this.lblFuenteCap.Name      = "lblFuenteCap";
            this.lblFuenteCap.TabIndex  = 8;
            this.lblFuenteCap.Tag       = "perfil.fuente";
            this.lblFuenteCap.Text      = "Tipografía:";

            this.cmbFuente.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFuente.Items.AddRange(new object[] { "Segoe UI", "Verdana", "Calibri", "Tahoma", "Arial" });
            this.cmbFuente.Location      = new System.Drawing.Point(170, 170);
            this.cmbFuente.Name          = "cmbFuente";
            this.cmbFuente.Size          = new System.Drawing.Size(244, 21);
            this.cmbFuente.TabIndex      = 9;

            // ── lblTamanoCap / cmbTamano ───────────────────────────────────────
            this.lblTamanoCap.AutoSize  = true;
            this.lblTamanoCap.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblTamanoCap.Location  = new System.Drawing.Point(24, 205);
            this.lblTamanoCap.Name      = "lblTamanoCap";
            this.lblTamanoCap.TabIndex  = 10;
            this.lblTamanoCap.Tag       = "perfil.tamano";
            this.lblTamanoCap.Text      = "Tamaño de letra:";

            this.cmbTamano.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTamano.Items.AddRange(new object[] { "Chico", "Normal", "Grande" });
            this.cmbTamano.Location      = new System.Drawing.Point(170, 202);
            this.cmbTamano.Name          = "cmbTamano";
            this.cmbTamano.Size          = new System.Drawing.Size(244, 21);
            this.cmbTamano.TabIndex      = 11;

            // ── lblTemaCap / cmbTema ───────────────────────────────────────────
            this.lblTemaCap.AutoSize  = true;
            this.lblTemaCap.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblTemaCap.Location  = new System.Drawing.Point(24, 237);
            this.lblTemaCap.Name      = "lblTemaCap";
            this.lblTemaCap.TabIndex  = 12;
            this.lblTemaCap.Tag       = "perfil.tema";
            this.lblTemaCap.Text      = "Tema:";

            this.cmbTema.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTema.Items.AddRange(new object[] { "Claro", "Oscuro" });
            this.cmbTema.Location      = new System.Drawing.Point(170, 234);
            this.cmbTema.Name          = "cmbTema";
            this.cmbTema.Size          = new System.Drawing.Size(244, 21);
            this.cmbTema.TabIndex      = 13;

            // ── lblFechaCap / cmbFecha ─────────────────────────────────────────
            this.lblFechaCap.AutoSize  = true;
            this.lblFechaCap.ForeColor = System.Drawing.Color.FromArgb(90, 90, 100);
            this.lblFechaCap.Location  = new System.Drawing.Point(24, 269);
            this.lblFechaCap.Name      = "lblFechaCap";
            this.lblFechaCap.TabIndex  = 14;
            this.lblFechaCap.Tag       = "perfil.fecha";
            this.lblFechaCap.Text      = "Formato de fecha:";

            this.cmbFecha.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFecha.Items.AddRange(new object[] { "dd/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "MM/dd/yyyy" });
            this.cmbFecha.Location      = new System.Drawing.Point(170, 266);
            this.cmbFecha.Name          = "cmbFecha";
            this.cmbFecha.Size          = new System.Drawing.Size(244, 21);
            this.cmbFecha.TabIndex      = 15;

            // ── chkNotif ───────────────────────────────────────────────────────
            this.chkNotif.AutoSize = true;
            this.chkNotif.Location = new System.Drawing.Point(170, 298);
            this.chkNotif.Name     = "chkNotif";
            this.chkNotif.TabIndex = 16;
            this.chkNotif.Tag      = "perfil.notif";
            this.chkNotif.Text     = "Recibir notificaciones";

            // ── btnGuardar ─────────────────────────────────────────────────────
            this.btnGuardar.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnGuardar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnGuardar.FlatAppearance.BorderSize = 0;
            this.btnGuardar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGuardar.ForeColor = System.Drawing.Color.White;
            this.btnGuardar.Location  = new System.Drawing.Point(170, 334);
            this.btnGuardar.Name      = "btnGuardar";
            this.btnGuardar.Size      = new System.Drawing.Size(244, 34);
            this.btnGuardar.TabIndex  = 17;
            this.btnGuardar.Tag       = "perfil.btn.guardar";
            this.btnGuardar.Text      = "Guardar preferencias";
            this.btnGuardar.UseVisualStyleBackColor = false;
            this.btnGuardar.Click += new System.EventHandler(this.BtnGuardar_Click);

            // ── btnDefault ─────────────────────────────────────────────────────
            this.btnDefault.BackColor = System.Drawing.Color.White;
            this.btnDefault.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnDefault.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(210, 180, 195);
            this.btnDefault.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDefault.ForeColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnDefault.Location  = new System.Drawing.Point(170, 372);
            this.btnDefault.Name      = "btnDefault";
            this.btnDefault.Size      = new System.Drawing.Size(244, 30);
            this.btnDefault.TabIndex  = 18;
            this.btnDefault.Tag       = "perfil.btn.default";
            this.btnDefault.Text      = "Restaurar valores de fábrica";
            this.btnDefault.UseVisualStyleBackColor = false;
            this.btnDefault.Click += new System.EventHandler(this.BtnDefault_Click);

            // ── lblEstado ──────────────────────────────────────────────────────
            this.lblEstado.ForeColor = System.Drawing.Color.FromArgb(40, 140, 60);
            this.lblEstado.Location  = new System.Drawing.Point(24, 410);
            this.lblEstado.Name      = "lblEstado";
            this.lblEstado.Size      = new System.Drawing.Size(392, 28);
            this.lblEstado.TabIndex  = 19;

            // ── MiPerfilForm ───────────────────────────────────────────────────
            this.BackColor       = System.Drawing.Color.White;
            this.ClientSize      = new System.Drawing.Size(440, 474);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblUsuarioCap);
            this.Controls.Add(this.lblUsuarioVal);
            this.Controls.Add(this.lblPerfilCap);
            this.Controls.Add(this.lblPerfilVal);
            this.Controls.Add(this.lblSeccion);
            this.Controls.Add(this.lblIdiomaCap);
            this.Controls.Add(this.cmbIdioma);
            this.Controls.Add(this.lblFuenteCap);
            this.Controls.Add(this.cmbFuente);
            this.Controls.Add(this.lblTamanoCap);
            this.Controls.Add(this.cmbTamano);
            this.Controls.Add(this.lblTemaCap);
            this.Controls.Add(this.cmbTema);
            this.Controls.Add(this.lblFechaCap);
            this.Controls.Add(this.cmbFecha);
            this.Controls.Add(this.chkNotif);
            this.Controls.Add(this.btnGuardar);
            this.Controls.Add(this.btnDefault);
            this.Controls.Add(this.lblEstado);
            this.Font            = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.Name            = "MiPerfilForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag             = "perfil.frm.titulo";
            this.Text            = "Mi Perfil";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label    lblTitulo;
        private System.Windows.Forms.Label    lblUsuarioCap;
        private System.Windows.Forms.Label    lblUsuarioVal;
        private System.Windows.Forms.Label    lblPerfilCap;
        private System.Windows.Forms.Label    lblPerfilVal;
        private System.Windows.Forms.Label    lblSeccion;
        private System.Windows.Forms.Label    lblIdiomaCap;
        private System.Windows.Forms.ComboBox cmbIdioma;
        private System.Windows.Forms.Label    lblFuenteCap;
        private System.Windows.Forms.ComboBox cmbFuente;
        private System.Windows.Forms.Label    lblTamanoCap;
        private System.Windows.Forms.ComboBox cmbTamano;
        private System.Windows.Forms.Label    lblTemaCap;
        private System.Windows.Forms.ComboBox cmbTema;
        private System.Windows.Forms.Label    lblFechaCap;
        private System.Windows.Forms.ComboBox cmbFecha;
        private System.Windows.Forms.CheckBox chkNotif;
        private System.Windows.Forms.Button   btnGuardar;
        private System.Windows.Forms.Button   btnDefault;
        private System.Windows.Forms.Label    lblEstado;
    }
}
