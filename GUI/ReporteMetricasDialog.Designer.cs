namespace GUI
{
    partial class ReporteMetricasDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtReporte = new System.Windows.Forms.TextBox();
            this.lstOportunidades = new System.Windows.Forms.ListBox();
            this.pnlBotones = new System.Windows.Forms.Panel();
            this.btnUsar = new System.Windows.Forms.Button();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.btnImprimir = new System.Windows.Forms.Button();
            this.pnlBotones.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtReporte
            // 
            this.txtReporte.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.txtReporte.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtReporte.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.txtReporte.Location = new System.Drawing.Point(0, 0);
            this.txtReporte.Multiline = true;
            this.txtReporte.Name = "txtReporte";
            this.txtReporte.ReadOnly = true;
            this.txtReporte.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtReporte.Size = new System.Drawing.Size(640, 304);
            this.txtReporte.TabIndex = 0;
            this.txtReporte.Text = "Reporte de métricas";
            // 
            // lstOportunidades
            // 
            this.lstOportunidades.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lstOportunidades.FormattingEnabled = true;
            this.lstOportunidades.Location = new System.Drawing.Point(0, 304);
            this.lstOportunidades.Name = "lstOportunidades";
            this.lstOportunidades.Size = new System.Drawing.Size(640, 120);
            this.lstOportunidades.TabIndex = 1;
            // 
            // pnlBotones
            // 
            this.pnlBotones.Controls.Add(this.btnUsar);
            this.pnlBotones.Controls.Add(this.btnCerrar);
            this.pnlBotones.Controls.Add(this.btnImprimir);
            this.pnlBotones.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBotones.Location = new System.Drawing.Point(0, 424);
            this.pnlBotones.Name = "pnlBotones";
            this.pnlBotones.Padding = new System.Windows.Forms.Padding(4);
            this.pnlBotones.Size = new System.Drawing.Size(640, 36);
            this.pnlBotones.TabIndex = 2;
            // 
            // btnUsar
            // 
            this.btnUsar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(70)))));
            this.btnUsar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnUsar.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnUsar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUsar.ForeColor = System.Drawing.Color.White;
            this.btnUsar.Location = new System.Drawing.Point(386, 4);
            this.btnUsar.Name = "btnUsar";
            this.btnUsar.Size = new System.Drawing.Size(150, 28);
            this.btnUsar.TabIndex = 0;
            this.btnUsar.Text = "Usar esta idea";
            this.btnUsar.UseVisualStyleBackColor = false;
            // 
            // btnCerrar
            // 
            this.btnCerrar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCerrar.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCerrar.Location = new System.Drawing.Point(536, 4);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(100, 28);
            this.btnCerrar.TabIndex = 1;
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = true;
            // 
            // btnImprimir
            // 
            this.btnImprimir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.btnImprimir.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnImprimir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImprimir.ForeColor = System.Drawing.Color.White;
            this.btnImprimir.Location = new System.Drawing.Point(4, 4);
            this.btnImprimir.Name = "btnImprimir";
            this.btnImprimir.Size = new System.Drawing.Size(150, 28);
            this.btnImprimir.TabIndex = 2;
            this.btnImprimir.Text = "Imprimir reporte";
            this.btnImprimir.UseVisualStyleBackColor = false;
            this.btnImprimir.Click += new System.EventHandler(this.BtnImprimir_Click);
            // 
            // ReporteMetricasDialog
            // 
            this.AcceptButton = this.btnUsar;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.CancelButton = this.btnCerrar;
            this.ClientSize = new System.Drawing.Size(640, 460);
            this.Controls.Add(this.txtReporte);
            this.Controls.Add(this.lstOportunidades);
            this.Controls.Add(this.pnlBotones);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReporteMetricasDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Reporte de métricas";
            this.pnlBotones.ResumeLayout(false);
            this.pnlBotones.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtReporte;
        private System.Windows.Forms.ListBox lstOportunidades;
        private System.Windows.Forms.Panel pnlBotones;
        private System.Windows.Forms.Button btnUsar;
        private System.Windows.Forms.Button btnCerrar;
        private System.Windows.Forms.Button btnImprimir;
    }
}
