namespace GUI
{
    partial class ErroresXmlForm
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
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblOrigen = new System.Windows.Forms.Label();
            this.dgvErrores = new System.Windows.Forms.DataGridView();
            this.txtDetalle = new System.Windows.Forms.TextBox();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.btnRecargar = new System.Windows.Forms.Button();
            this.btnImportar = new System.Windows.Forms.Button();
            this.btnExportar = new System.Windows.Forms.Button();
            this.btnCerrar = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvErrores)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitulo
            // 
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblTitulo.Location = new System.Drawing.Point(16, 12);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(860, 28);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Errores inesperados serializados (XML/JSON)";
            // 
            // lblOrigen
            // 
            this.lblOrigen.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblOrigen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(116)))), ((int)(((byte)(128)))));
            this.lblOrigen.Location = new System.Drawing.Point(16, 42);
            this.lblOrigen.Name = "lblOrigen";
            this.lblOrigen.Size = new System.Drawing.Size(860, 20);
            this.lblOrigen.TabIndex = 1;
            this.lblOrigen.Text = "Errores registrados en esta PC: 0";
            // 
            // dgvErrores
            // 
            this.dgvErrores.AllowUserToAddRows = false;
            this.dgvErrores.AllowUserToDeleteRows = false;
            this.dgvErrores.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dgvErrores.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvErrores.BackgroundColor = System.Drawing.Color.White;
            this.dgvErrores.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvErrores.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvErrores.Location = new System.Drawing.Point(16, 68);
            this.dgvErrores.MultiSelect = false;
            this.dgvErrores.Name = "dgvErrores";
            this.dgvErrores.ReadOnly = true;
            this.dgvErrores.RowHeadersVisible = false;
            this.dgvErrores.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvErrores.Size = new System.Drawing.Size(868, 300);
            this.dgvErrores.TabIndex = 2;
            this.dgvErrores.SelectionChanged += new System.EventHandler(this.DgvErrores_SelectionChanged);
            // 
            // txtDetalle
            // 
            this.txtDetalle.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtDetalle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.txtDetalle.Location = new System.Drawing.Point(16, 376);
            this.txtDetalle.Multiline = true;
            this.txtDetalle.Name = "txtDetalle";
            this.txtDetalle.ReadOnly = true;
            this.txtDetalle.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetalle.Size = new System.Drawing.Size(868, 110);
            this.txtDetalle.TabIndex = 3;
            // 
            // lblMensaje
            // 
            this.lblMensaje.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblMensaje.Location = new System.Drawing.Point(16, 492);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(868, 20);
            this.lblMensaje.TabIndex = 4;
            // 
            // btnRecargar
            // 
            this.btnRecargar.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnRecargar.Location = new System.Drawing.Point(16, 516);
            this.btnRecargar.Name = "btnRecargar";
            this.btnRecargar.Size = new System.Drawing.Size(170, 32);
            this.btnRecargar.TabIndex = 5;
            this.btnRecargar.Text = "Ver los de esta PC";
            this.btnRecargar.BackColor = System.Drawing.Color.White;
            this.btnRecargar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnRecargar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecargar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.btnRecargar.UseVisualStyleBackColor = false;
            this.btnRecargar.Click += new System.EventHandler(this.BtnRecargar_Click);
            // 
            // btnImportar
            // 
            this.btnImportar.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnImportar.Location = new System.Drawing.Point(194, 516);
            this.btnImportar.Name = "btnImportar";
            this.btnImportar.Size = new System.Drawing.Size(170, 32);
            this.btnImportar.TabIndex = 6;
            this.btnImportar.Text = "Importar XML/JSON…";
            this.btnImportar.BackColor = System.Drawing.Color.White;
            this.btnImportar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnImportar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImportar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.btnImportar.UseVisualStyleBackColor = false;
            this.btnImportar.Click += new System.EventHandler(this.BtnImportar_Click);
            // 
            // btnExportar
            // 
            this.btnExportar.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnExportar.Location = new System.Drawing.Point(372, 516);
            this.btnExportar.Name = "btnExportar";
            this.btnExportar.Size = new System.Drawing.Size(170, 32);
            this.btnExportar.TabIndex = 7;
            this.btnExportar.Text = "Exportar XML/JSON…";
            this.btnExportar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnExportar.FlatAppearance.BorderSize = 0;
            this.btnExportar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportar.ForeColor = System.Drawing.Color.White;
            this.btnExportar.UseVisualStyleBackColor = false;
            this.btnExportar.Click += new System.EventHandler(this.BtnExportar_Click);
            // 
            // btnCerrar
            // 
            this.btnCerrar.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnCerrar.Location = new System.Drawing.Point(764, 516);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(120, 32);
            this.btnCerrar.TabIndex = 8;
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.BackColor = System.Drawing.Color.White;
            this.btnCerrar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);
            // 
            // ErroresXmlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCerrar;
            this.ClientSize = new System.Drawing.Size(900, 560);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblOrigen);
            this.Controls.Add(this.dgvErrores);
            this.Controls.Add(this.txtDetalle);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.btnRecargar);
            this.Controls.Add(this.btnImportar);
            this.Controls.Add(this.btnExportar);
            this.Controls.Add(this.btnCerrar);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(760, 460);
            this.Name = "ErroresXmlForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Errores (XML/JSON)";
            ((System.ComponentModel.ISupportInitialize)(this.dgvErrores)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblOrigen;
        private System.Windows.Forms.DataGridView dgvErrores;
        private System.Windows.Forms.TextBox txtDetalle;
        private System.Windows.Forms.Label lblMensaje;
        private System.Windows.Forms.Button btnRecargar;
        private System.Windows.Forms.Button btnImportar;
        private System.Windows.Forms.Button btnExportar;
        private System.Windows.Forms.Button btnCerrar;
    }
}
