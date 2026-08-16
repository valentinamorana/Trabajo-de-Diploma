namespace GUI
{
    partial class AlertasForm
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
            this.header         = new System.Windows.Forms.Panel();
            this.lblTitulo      = new System.Windows.Forms.Label();
            this.btnActualizar  = new System.Windows.Forms.Button();
            this.flow           = new System.Windows.Forms.FlowLayoutPanel();
            this.header.SuspendLayout();
            this.SuspendLayout();

            // ── header ─────────────────────────────────────────────────────────
            this.header.BackColor = System.Drawing.Color.FromArgb(176, 62, 96);
            this.header.Controls.Add(this.lblTitulo);
            this.header.Controls.Add(this.btnActualizar);
            this.header.Dock     = System.Windows.Forms.DockStyle.Top;
            this.header.Height   = 52;
            this.header.Name     = "header";
            this.header.TabIndex = 1;

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.Font      = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.White;
            this.lblTitulo.Location  = new System.Drawing.Point(14, 12);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Tag       = "frm.alertas";
            this.lblTitulo.Text      = "🔔  Centro de Alertas";

            // ── btnActualizar ──────────────────────────────────────────────────
            this.btnActualizar.Anchor = ((System.Windows.Forms.AnchorStyles)(
                System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
            this.btnActualizar.BackColor = System.Drawing.Color.FromArgb(150, 45, 78);
            this.btnActualizar.FlatAppearance.BorderSize = 0;
            this.btnActualizar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActualizar.Font      = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnActualizar.ForeColor = System.Drawing.Color.White;
            this.btnActualizar.Location  = new System.Drawing.Point(508, 10);
            this.btnActualizar.Name      = "btnActualizar";
            this.btnActualizar.Size      = new System.Drawing.Size(40, 32);
            this.btnActualizar.TabIndex  = 1;
            this.btnActualizar.Text      = "↻";
            this.btnActualizar.UseVisualStyleBackColor = false;
            this.btnActualizar.Click += new System.EventHandler(this.BtnActualizar_Click);

            // ── flow ───────────────────────────────────────────────────────────
            this.flow.AutoScroll    = true;
            this.flow.Dock          = System.Windows.Forms.DockStyle.Fill;
            this.flow.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flow.Name          = "flow";
            this.flow.Padding       = new System.Windows.Forms.Padding(12);
            this.flow.TabIndex      = 0;
            this.flow.WrapContents  = false;

            // ── AlertasForm ────────────────────────────────────────────────────
            this.BackColor       = System.Drawing.Color.FromArgb(252, 240, 245);
            this.ClientSize      = new System.Drawing.Size(560, 460);
            this.Controls.Add(this.flow);
            this.Controls.Add(this.header);
            this.MinimumSize     = new System.Drawing.Size(420, 300);
            this.Name            = "AlertasForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Tag             = "frm.alertas";
            this.Text            = "Centro de Alertas";
            this.header.ResumeLayout(false);
            this.header.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel          header;
        private System.Windows.Forms.Label          lblTitulo;
        private System.Windows.Forms.Button         btnActualizar;
        private System.Windows.Forms.FlowLayoutPanel flow;
    }
}
