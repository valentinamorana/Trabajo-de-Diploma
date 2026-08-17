namespace GUI
{
    partial class ExploradorCompositeForm
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
            this.panelHeader     = new System.Windows.Forms.Panel();
            this.lblTitulo       = new System.Windows.Forms.Label();
            this.lblDescripcion  = new System.Windows.Forms.Label();
            this.panelLeyenda    = new System.Windows.Forms.Panel();
            this.lblLeyenda      = new System.Windows.Forms.Label();
            this.treeView        = new System.Windows.Forms.TreeView();
            this.panelBotones    = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCerrar       = new System.Windows.Forms.Button();
            this.btnColapsar     = new System.Windows.Forms.Button();
            this.btnExpandir     = new System.Windows.Forms.Button();
            this.btnActualizar   = new System.Windows.Forms.Button();
            this.panelHeader.SuspendLayout();
            this.panelLeyenda.SuspendLayout();
            this.panelBotones.SuspendLayout();
            this.SuspendLayout();

            // ── panelHeader ────────────────────────────────────────────────────
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.panelHeader.Controls.Add(this.lblTitulo);
            this.panelHeader.Controls.Add(this.lblDescripcion);
            this.panelHeader.Dock     = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height   = 80;
            this.panelHeader.Name     = "panelHeader";
            this.panelHeader.Padding  = new System.Windows.Forms.Padding(14, 10, 14, 10);
            this.panelHeader.TabIndex = 3;

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.Font      = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.White;
            this.lblTitulo.Location  = new System.Drawing.Point(14, 10);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Tag       = "lbl.explorador.titulo";
            this.lblTitulo.Text      = "Vista completa del sistema";

            // ── lblDescripcion ─────────────────────────────────────────────────
            this.lblDescripcion.AutoSize  = true;
            this.lblDescripcion.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblDescripcion.ForeColor = System.Drawing.Color.FromArgb(244, 212, 226);
            this.lblDescripcion.Location  = new System.Drawing.Point(16, 42);
            this.lblDescripcion.Name      = "lblDescripcion";
            this.lblDescripcion.TabIndex  = 1;
            this.lblDescripcion.Tag       = "lbl.explorador.descripcion";
            this.lblDescripcion.Text      = "Estructura organizacional de WardrobeFlow — Solo lectura";

            // ── panelLeyenda ───────────────────────────────────────────────────
            this.panelLeyenda.BackColor = System.Drawing.Color.FromArgb(252, 240, 246);
            this.panelLeyenda.Controls.Add(this.lblLeyenda);
            this.panelLeyenda.Dock     = System.Windows.Forms.DockStyle.Top;
            this.panelLeyenda.Height   = 30;
            this.panelLeyenda.Name     = "panelLeyenda";
            this.panelLeyenda.Padding  = new System.Windows.Forms.Padding(14, 5, 0, 0);
            this.panelLeyenda.TabIndex = 2;

            // ── lblLeyenda ─────────────────────────────────────────────────────
            this.lblLeyenda.AutoSize  = true;
            this.lblLeyenda.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblLeyenda.ForeColor = System.Drawing.Color.FromArgb(110, 42, 74);
            this.lblLeyenda.Location  = new System.Drawing.Point(14, 6);
            this.lblLeyenda.Name      = "lblLeyenda";
            this.lblLeyenda.TabIndex  = 0;
            this.lblLeyenda.Tag       = "lbl.explorador.leyenda";
            this.lblLeyenda.Text      = "📁 Familia (nodo compuesto — Área o Rol)    🔑 Patente (hoja — permiso atómico)";

            // ── treeView ───────────────────────────────────────────────────────
            this.treeView.BackColor     = System.Drawing.Color.FromArgb(252, 250, 252);
            this.treeView.BorderStyle   = System.Windows.Forms.BorderStyle.None;
            this.treeView.CheckBoxes    = false;
            this.treeView.Dock          = System.Windows.Forms.DockStyle.Fill;
            this.treeView.Font          = new System.Drawing.Font("Segoe UI", 9.5F);
            this.treeView.FullRowSelect = true;
            this.treeView.HideSelection = false;
            this.treeView.Indent        = 20;
            this.treeView.ItemHeight    = 24;
            this.treeView.Name          = "treeView";
            this.treeView.ShowLines     = true;
            this.treeView.ShowPlusMinus = true;
            this.treeView.TabIndex      = 0;

            // ── panelBotones ───────────────────────────────────────────────────
            this.panelBotones.BackColor      = System.Drawing.Color.FromArgb(252, 240, 246);
            this.panelBotones.Controls.Add(this.btnCerrar);
            this.panelBotones.Controls.Add(this.btnColapsar);
            this.panelBotones.Controls.Add(this.btnExpandir);
            this.panelBotones.Controls.Add(this.btnActualizar);
            this.panelBotones.Dock          = System.Windows.Forms.DockStyle.Bottom;
            this.panelBotones.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.panelBotones.Height        = 46;
            this.panelBotones.Name          = "panelBotones";
            this.panelBotones.Padding       = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.panelBotones.TabIndex      = 1;

            // ── btnCerrar ──────────────────────────────────────────────────────
            this.btnCerrar.BackColor = System.Drawing.Color.FromArgb(210, 200, 220);
            this.btnCerrar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.Font      = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCerrar.ForeColor = System.Drawing.Color.Black;
            this.btnCerrar.Name      = "btnCerrar";
            this.btnCerrar.Size      = new System.Drawing.Size(100, 32);
            this.btnCerrar.TabIndex  = 3;
            this.btnCerrar.Tag       = "btn.explorador.cerrar";
            this.btnCerrar.Text      = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);

            // ── btnColapsar ────────────────────────────────────────────────────
            this.btnColapsar.BackColor = System.Drawing.Color.FromArgb(210, 100, 135);
            this.btnColapsar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnColapsar.FlatAppearance.BorderSize = 0;
            this.btnColapsar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColapsar.Font      = new System.Drawing.Font("Segoe UI", 9F);
            this.btnColapsar.ForeColor = System.Drawing.Color.White;
            this.btnColapsar.Name      = "btnColapsar";
            this.btnColapsar.Size      = new System.Drawing.Size(130, 32);
            this.btnColapsar.TabIndex  = 2;
            this.btnColapsar.Tag       = "btn.explorador.colapsar";
            this.btnColapsar.Text      = "⊟ Colapsar todo";
            this.btnColapsar.UseVisualStyleBackColor = false;
            this.btnColapsar.Click += new System.EventHandler(this.BtnColapsar_Click);

            // ── btnExpandir ────────────────────────────────────────────────────
            this.btnExpandir.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnExpandir.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnExpandir.FlatAppearance.BorderSize = 0;
            this.btnExpandir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExpandir.Font      = new System.Drawing.Font("Segoe UI", 9F);
            this.btnExpandir.ForeColor = System.Drawing.Color.White;
            this.btnExpandir.Name      = "btnExpandir";
            this.btnExpandir.Size      = new System.Drawing.Size(130, 32);
            this.btnExpandir.TabIndex  = 1;
            this.btnExpandir.Tag       = "btn.explorador.expandir";
            this.btnExpandir.Text      = "⊞ Expandir todo";
            this.btnExpandir.UseVisualStyleBackColor = false;
            this.btnExpandir.Click += new System.EventHandler(this.BtnExpandir_Click);

            // ── btnActualizar ──────────────────────────────────────────────────
            this.btnActualizar.BackColor = System.Drawing.Color.White;
            this.btnActualizar.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnActualizar.FlatAppearance.BorderSize  = 1;
            this.btnActualizar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnActualizar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActualizar.Font      = new System.Drawing.Font("Segoe UI", 9F);
            this.btnActualizar.ForeColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.btnActualizar.Name      = "btnActualizar";
            this.btnActualizar.Size      = new System.Drawing.Size(120, 32);
            this.btnActualizar.TabIndex  = 0;
            this.btnActualizar.Tag       = "btn.permisos.actualizar";
            this.btnActualizar.Text      = "↻ Actualizar";
            this.btnActualizar.UseVisualStyleBackColor = false;
            this.btnActualizar.Click += new System.EventHandler(this.BtnActualizar_Click);

            // ── ExploradorCompositeForm ────────────────────────────────────────
            this.BackColor       = System.Drawing.Color.White;
            this.ClientSize      = new System.Drawing.Size(680, 660);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.panelBotones);
            this.Controls.Add(this.panelLeyenda);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MinimumSize     = new System.Drawing.Size(500, 500);
            this.Name            = "ExploradorCompositeForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Tag             = "frm.explorador";
            this.Text            = "Vista completa del sistema";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelLeyenda.ResumeLayout(false);
            this.panelLeyenda.PerformLayout();
            this.panelBotones.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel          panelHeader;
        private System.Windows.Forms.Label          lblTitulo;
        private System.Windows.Forms.Label          lblDescripcion;
        private System.Windows.Forms.Panel          panelLeyenda;
        private System.Windows.Forms.Label          lblLeyenda;
        private System.Windows.Forms.TreeView       treeView;
        private System.Windows.Forms.FlowLayoutPanel panelBotones;
        private System.Windows.Forms.Button         btnCerrar;
        private System.Windows.Forms.Button         btnColapsar;
        private System.Windows.Forms.Button         btnExpandir;
        private System.Windows.Forms.Button         btnActualizar;
    }
}
