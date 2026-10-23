namespace GUI.Ayuda
{
    partial class AyudaDialog
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
            this.lblEncabezado = new System.Windows.Forms.Label();
            this.pnlMargen = new System.Windows.Forms.Panel();
            this.txtCuerpo = new System.Windows.Forms.TextBox();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.pnlMargen.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblEncabezado
            // 
            this.lblEncabezado.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblEncabezado.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblEncabezado.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEncabezado.ForeColor = System.Drawing.Color.White;
            this.lblEncabezado.Location = new System.Drawing.Point(0, 0);
            this.lblEncabezado.Name = "lblEncabezado";
            this.lblEncabezado.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblEncabezado.Size = new System.Drawing.Size(560, 40);
            this.lblEncabezado.TabIndex = 0;
            this.lblEncabezado.Text = "Título del tema";
            this.lblEncabezado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlMargen
            // 
            this.pnlMargen.Controls.Add(this.txtCuerpo);
            this.pnlMargen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMargen.Location = new System.Drawing.Point(0, 40);
            this.pnlMargen.Name = "pnlMargen";
            this.pnlMargen.Padding = new System.Windows.Forms.Padding(12);
            this.pnlMargen.Size = new System.Drawing.Size(560, 348);
            this.pnlMargen.TabIndex = 1;
            // 
            // txtCuerpo
            // 
            this.txtCuerpo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.txtCuerpo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtCuerpo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtCuerpo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCuerpo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.txtCuerpo.Location = new System.Drawing.Point(12, 12);
            this.txtCuerpo.Multiline = true;
            this.txtCuerpo.Name = "txtCuerpo";
            this.txtCuerpo.ReadOnly = true;
            this.txtCuerpo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCuerpo.Size = new System.Drawing.Size(536, 324);
            this.txtCuerpo.TabIndex = 0;
            this.txtCuerpo.TabStop = false;
            this.txtCuerpo.Text = "Texto de la ayuda de la pantalla.";
            // 
            // btnCerrar
            // 
            this.btnCerrar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCerrar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnCerrar.Location = new System.Drawing.Point(0, 388);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(560, 32);
            this.btnCerrar.TabIndex = 2;
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = true;
            // 
            // AyudaDialog
            // 
            this.AcceptButton = this.btnCerrar;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.CancelButton = this.btnCerrar;
            this.ClientSize = new System.Drawing.Size(560, 420);
            this.Controls.Add(this.pnlMargen);
            this.Controls.Add(this.btnCerrar);
            this.Controls.Add(this.lblEncabezado);
            this.MinimizeBox = false;
            this.Name = "AyudaDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Ayuda";
            this.pnlMargen.ResumeLayout(false);
            this.pnlMargen.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblEncabezado;
        private System.Windows.Forms.Panel pnlMargen;
        private System.Windows.Forms.TextBox txtCuerpo;
        private System.Windows.Forms.Button btnCerrar;
    }
}
