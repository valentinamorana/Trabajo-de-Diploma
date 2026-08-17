namespace GUI
{
    partial class MapeoControlesForm
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
            this.lblPat         = new System.Windows.Forms.Label();
            this.cmbPatente     = new System.Windows.Forms.ComboBox();
            this.lblForm        = new System.Windows.Forms.Label();
            this.cmbFormulario  = new System.Windows.Forms.ComboBox();
            this.lblDisp        = new System.Windows.Forms.Label();
            this.lstDisponibles = new System.Windows.Forms.ListBox();
            this.lblAsoc        = new System.Windows.Forms.Label();
            this.lstAsociados   = new System.Windows.Forms.ListBox();
            this.btnAgregar     = new System.Windows.Forms.Button();
            this.btnQuitar      = new System.Windows.Forms.Button();
            this.btnGuardar     = new System.Windows.Forms.Button();
            this.btnCerrar      = new System.Windows.Forms.Button();
            this.lblEstado      = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // ── lblPat / cmbPatente ────────────────────────────────────────────
            this.lblPat.AutoSize = true;
            this.lblPat.Location = new System.Drawing.Point(16, 18);
            this.lblPat.Name     = "lblPat";
            this.lblPat.TabIndex = 0;
            this.lblPat.Tag      = "map.patente";
            this.lblPat.Text     = "Patente:";

            this.cmbPatente.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatente.Location      = new System.Drawing.Point(110, 14);
            this.cmbPatente.Name          = "cmbPatente";
            this.cmbPatente.Size          = new System.Drawing.Size(454, 24);
            this.cmbPatente.TabIndex      = 1;
            this.cmbPatente.SelectedIndexChanged += new System.EventHandler(this.CmbPatente_SelectedIndexChanged);

            // ── lblForm / cmbFormulario ────────────────────────────────────────
            this.lblForm.AutoSize = true;
            this.lblForm.Location = new System.Drawing.Point(16, 50);
            this.lblForm.Name     = "lblForm";
            this.lblForm.TabIndex = 2;
            this.lblForm.Tag      = "map.formulario";
            this.lblForm.Text     = "Formulario:";

            this.cmbFormulario.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFormulario.Location      = new System.Drawing.Point(110, 46);
            this.cmbFormulario.Name          = "cmbFormulario";
            this.cmbFormulario.Size          = new System.Drawing.Size(454, 24);
            this.cmbFormulario.TabIndex      = 3;
            this.cmbFormulario.SelectedIndexChanged += new System.EventHandler(this.CmbFormulario_SelectedIndexChanged);

            // ── lblDisp / lstDisponibles ───────────────────────────────────────
            this.lblDisp.AutoSize  = true;
            this.lblDisp.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDisp.Location  = new System.Drawing.Point(16, 84);
            this.lblDisp.Name      = "lblDisp";
            this.lblDisp.TabIndex  = 4;
            this.lblDisp.Tag       = "map.disponibles";
            this.lblDisp.Text      = "Controles disponibles";

            this.lstDisponibles.DisplayMember        = "Display";
            this.lstDisponibles.HorizontalScrollbar   = true;
            this.lstDisponibles.Location              = new System.Drawing.Point(16, 104);
            this.lstDisponibles.Name                  = "lstDisponibles";
            this.lstDisponibles.Size                  = new System.Drawing.Size(230, 280);
            this.lstDisponibles.TabIndex              = 5;

            // ── lblAsoc / lstAsociados ─────────────────────────────────────────
            this.lblAsoc.AutoSize  = true;
            this.lblAsoc.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAsoc.ForeColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.lblAsoc.Location  = new System.Drawing.Point(334, 84);
            this.lblAsoc.Name      = "lblAsoc";
            this.lblAsoc.TabIndex  = 6;
            this.lblAsoc.Tag       = "map.asociados";
            this.lblAsoc.Text      = "Asociados a la patente";

            this.lstAsociados.DisplayMember      = "Display";
            this.lstAsociados.HorizontalScrollbar = true;
            this.lstAsociados.Location            = new System.Drawing.Point(334, 104);
            this.lstAsociados.Name                = "lstAsociados";
            this.lstAsociados.Size                = new System.Drawing.Size(230, 280);
            this.lstAsociados.TabIndex            = 7;

            // ── btnAgregar ─────────────────────────────────────────────────────
            this.btnAgregar.BackColor = System.Drawing.Color.FromArgb(210, 100, 135);
            this.btnAgregar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnAgregar.FlatAppearance.BorderSize = 0;
            this.btnAgregar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAgregar.Font      = new System.Drawing.Font("Segoe UI", 8F);
            this.btnAgregar.ForeColor = System.Drawing.Color.White;
            this.btnAgregar.Location  = new System.Drawing.Point(258, 170);
            this.btnAgregar.Name      = "btnAgregar";
            this.btnAgregar.Size      = new System.Drawing.Size(64, 30);
            this.btnAgregar.TabIndex  = 8;
            this.btnAgregar.Text      = "Asociar ➜";
            this.btnAgregar.UseVisualStyleBackColor = false;
            this.btnAgregar.Click += new System.EventHandler(this.BtnAgregar_Click);

            // ── btnQuitar ──────────────────────────────────────────────────────
            this.btnQuitar.BackColor = System.Drawing.Color.FromArgb(210, 100, 135);
            this.btnQuitar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnQuitar.FlatAppearance.BorderSize = 0;
            this.btnQuitar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnQuitar.Font      = new System.Drawing.Font("Segoe UI", 8F);
            this.btnQuitar.ForeColor = System.Drawing.Color.White;
            this.btnQuitar.Location  = new System.Drawing.Point(258, 210);
            this.btnQuitar.Name      = "btnQuitar";
            this.btnQuitar.Size      = new System.Drawing.Size(64, 30);
            this.btnQuitar.TabIndex  = 9;
            this.btnQuitar.Text      = "⬅ Quitar";
            this.btnQuitar.UseVisualStyleBackColor = false;
            this.btnQuitar.Click += new System.EventHandler(this.BtnQuitar_Click);

            // ── btnGuardar ─────────────────────────────────────────────────────
            this.btnGuardar.BackColor = System.Drawing.Color.FromArgb(60, 140, 80);
            this.btnGuardar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnGuardar.FlatAppearance.BorderSize = 0;
            this.btnGuardar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGuardar.ForeColor = System.Drawing.Color.White;
            this.btnGuardar.Location  = new System.Drawing.Point(334, 400);
            this.btnGuardar.Name      = "btnGuardar";
            this.btnGuardar.Size      = new System.Drawing.Size(140, 32);
            this.btnGuardar.TabIndex  = 10;
            this.btnGuardar.Tag       = "map.guardar";
            this.btnGuardar.Text      = "💾 Guardar";
            this.btnGuardar.UseVisualStyleBackColor = false;
            this.btnGuardar.Click += new System.EventHandler(this.BtnGuardar_Click);

            // ── btnCerrar ──────────────────────────────────────────────────────
            this.btnCerrar.BackColor = System.Drawing.Color.FromArgb(220, 215, 225);
            this.btnCerrar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.ForeColor = System.Drawing.Color.Black;
            this.btnCerrar.Location  = new System.Drawing.Point(484, 400);
            this.btnCerrar.Name      = "btnCerrar";
            this.btnCerrar.Size      = new System.Drawing.Size(80, 32);
            this.btnCerrar.TabIndex  = 11;
            this.btnCerrar.Tag       = "btn.cerrar";
            this.btnCerrar.Text      = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);

            // ── lblEstado ──────────────────────────────────────────────────────
            this.lblEstado.ForeColor = System.Drawing.Color.FromArgb(40, 140, 60);
            this.lblEstado.Location  = new System.Drawing.Point(16, 405);
            this.lblEstado.Name      = "lblEstado";
            this.lblEstado.Size      = new System.Drawing.Size(300, 40);
            this.lblEstado.TabIndex  = 12;

            // ── MapeoControlesForm ─────────────────────────────────────────────
            this.BackColor       = System.Drawing.Color.White;
            this.ClientSize      = new System.Drawing.Size(580, 470);
            this.Controls.Add(this.lblPat);
            this.Controls.Add(this.cmbPatente);
            this.Controls.Add(this.lblForm);
            this.Controls.Add(this.cmbFormulario);
            this.Controls.Add(this.lblDisp);
            this.Controls.Add(this.lstDisponibles);
            this.Controls.Add(this.lblAsoc);
            this.Controls.Add(this.lstAsociados);
            this.Controls.Add(this.btnAgregar);
            this.Controls.Add(this.btnQuitar);
            this.Controls.Add(this.btnGuardar);
            this.Controls.Add(this.btnCerrar);
            this.Controls.Add(this.lblEstado);
            this.Font            = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.Name            = "MapeoControlesForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag             = "frm.mapeocontroles";
            this.Text            = "Mapeo de controles por permiso";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label    lblPat;
        private System.Windows.Forms.ComboBox cmbPatente;
        private System.Windows.Forms.Label    lblForm;
        private System.Windows.Forms.ComboBox cmbFormulario;
        private System.Windows.Forms.Label    lblDisp;
        private System.Windows.Forms.ListBox  lstDisponibles;
        private System.Windows.Forms.Label    lblAsoc;
        private System.Windows.Forms.ListBox  lstAsociados;
        private System.Windows.Forms.Button   btnAgregar;
        private System.Windows.Forms.Button   btnQuitar;
        private System.Windows.Forms.Button   btnGuardar;
        private System.Windows.Forms.Button   btnCerrar;
        private System.Windows.Forms.Label    lblEstado;
    }
}
