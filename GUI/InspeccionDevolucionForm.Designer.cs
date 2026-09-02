namespace GUI
{
    partial class InspeccionDevolucionForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnAprobarReingreso = new System.Windows.Forms.Button();
            this.btnDarDeBajaConCargo = new System.Windows.Forms.Button();
            this.btnRefrescar = new System.Windows.Forms.Button();
            this.lblConteo = new System.Windows.Forms.Label();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.dgvPrendas = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.panelStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPrendas)).BeginInit();
            this.SuspendLayout();
            //
            // panelTop
            //
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelTop.Controls.Add(this.btnAprobarReingreso);
            this.panelTop.Controls.Add(this.btnDarDeBajaConCargo);
            this.panelTop.Controls.Add(this.btnRefrescar);
            this.panelTop.Controls.Add(this.lblConteo);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 4);
            this.panelTop.Size = new System.Drawing.Size(960, 50);
            this.panelTop.TabIndex = 0;
            //
            // btnAprobarReingreso
            //
            this.btnAprobarReingreso.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(140)))), ((int)(((byte)(60)))));
            this.btnAprobarReingreso.Enabled = false;
            this.btnAprobarReingreso.FlatAppearance.BorderSize = 0;
            this.btnAprobarReingreso.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAprobarReingreso.ForeColor = System.Drawing.Color.White;
            this.btnAprobarReingreso.Location = new System.Drawing.Point(8, 10);
            this.btnAprobarReingreso.Name = "btnAprobarReingreso";
            this.btnAprobarReingreso.Size = new System.Drawing.Size(170, 28);
            this.btnAprobarReingreso.TabIndex = 0;
            this.btnAprobarReingreso.Tag = "insp.btn.aprobarreingreso";
            this.btnAprobarReingreso.Text = "Aprobar Reingreso";
            this.btnAprobarReingreso.UseVisualStyleBackColor = false;
            this.btnAprobarReingreso.Click += new System.EventHandler(this.BtnAprobarReingreso_Click);
            //
            // btnDarDeBajaConCargo
            //
            this.btnDarDeBajaConCargo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnDarDeBajaConCargo.Enabled = false;
            this.btnDarDeBajaConCargo.FlatAppearance.BorderSize = 0;
            this.btnDarDeBajaConCargo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDarDeBajaConCargo.ForeColor = System.Drawing.Color.White;
            this.btnDarDeBajaConCargo.Location = new System.Drawing.Point(186, 10);
            this.btnDarDeBajaConCargo.Name = "btnDarDeBajaConCargo";
            this.btnDarDeBajaConCargo.Size = new System.Drawing.Size(170, 28);
            this.btnDarDeBajaConCargo.TabIndex = 1;
            this.btnDarDeBajaConCargo.Tag = "insp.btn.darbajaconcargo";
            this.btnDarDeBajaConCargo.Text = "Dar de Baja y Cobrar";
            this.btnDarDeBajaConCargo.UseVisualStyleBackColor = false;
            this.btnDarDeBajaConCargo.Click += new System.EventHandler(this.BtnDarDeBajaConCargo_Click);
            //
            // btnRefrescar
            //
            this.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefrescar.Location = new System.Drawing.Point(364, 10);
            this.btnRefrescar.Name = "btnRefrescar";
            this.btnRefrescar.Size = new System.Drawing.Size(32, 28);
            this.btnRefrescar.TabIndex = 2;
            this.btnRefrescar.Text = "↻";
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);
            //
            // lblConteo
            //
            this.lblConteo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblConteo.ForeColor = System.Drawing.Color.DimGray;
            this.lblConteo.Location = new System.Drawing.Point(404, 14);
            this.lblConteo.Name = "lblConteo";
            this.lblConteo.Size = new System.Drawing.Size(300, 23);
            this.lblConteo.TabIndex = 3;
            this.lblConteo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // panelStatus
            //
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 474);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.panelStatus.Size = new System.Drawing.Size(960, 26);
            this.panelStatus.TabIndex = 2;
            //
            // lblMensaje
            //
            this.lblMensaje.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.Location = new System.Drawing.Point(8, 4);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(944, 18);
            this.lblMensaje.TabIndex = 0;
            //
            // dgvPrendas
            //
            this.dgvPrendas.AllowUserToAddRows = false;
            this.dgvPrendas.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(248)))), ((int)(((byte)(252)))));
            this.dgvPrendas.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvPrendas.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPrendas.BackgroundColor = System.Drawing.Color.White;
            this.dgvPrendas.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(182)))), ((int)(((byte)(193)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvPrendas.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvPrendas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPrendas.Location = new System.Drawing.Point(0, 50);
            this.dgvPrendas.Name = "dgvPrendas";
            this.dgvPrendas.ReadOnly = true;
            this.dgvPrendas.RowHeadersVisible = false;
            this.dgvPrendas.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPrendas.Size = new System.Drawing.Size(960, 424);
            this.dgvPrendas.TabIndex = 1;
            this.dgvPrendas.SelectionChanged += new System.EventHandler(this.DgvPrendas_SelectionChanged);
            //
            // InspeccionDevolucionForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(960, 500);
            this.Controls.Add(this.dgvPrendas);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new System.Drawing.Size(780, 400);
            this.Name = "InspeccionDevolucionForm";
            this.Tag = "frm.inspecciondevolucion";
            this.Text = "Inspección de Devolución";
            this.Load += new System.EventHandler(this.InspeccionDevolucionForm_Load);
            this.panelTop.ResumeLayout(false);
            this.panelStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPrendas)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnAprobarReingreso;
        private System.Windows.Forms.Button btnDarDeBajaConCargo;
        private System.Windows.Forms.Button btnRefrescar;
        private System.Windows.Forms.Label lblConteo;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblMensaje;
        private System.Windows.Forms.DataGridView dgvPrendas;
    }
}
