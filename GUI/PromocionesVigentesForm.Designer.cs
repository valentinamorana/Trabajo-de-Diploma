namespace GUI
{
    partial class PromocionesVigentesForm
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
            this.btnSugerirBaja = new System.Windows.Forms.Button();
            this.btnRefrescar = new System.Windows.Forms.Button();
            this.lblConteo = new System.Windows.Forms.Label();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.dgvPromociones = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.panelStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPromociones)).BeginInit();
            this.SuspendLayout();
            //
            // panelTop
            //
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelTop.Controls.Add(this.btnSugerirBaja);
            this.panelTop.Controls.Add(this.btnRefrescar);
            this.panelTop.Controls.Add(this.lblConteo);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 4);
            this.panelTop.Size = new System.Drawing.Size(900, 50);
            this.panelTop.TabIndex = 0;
            //
            // btnSugerirBaja
            //
            this.btnSugerirBaja.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnSugerirBaja.Enabled = false;
            this.btnSugerirBaja.FlatAppearance.BorderSize = 0;
            this.btnSugerirBaja.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSugerirBaja.ForeColor = System.Drawing.Color.White;
            this.btnSugerirBaja.Location = new System.Drawing.Point(8, 10);
            this.btnSugerirBaja.Name = "btnSugerirBaja";
            this.btnSugerirBaja.Size = new System.Drawing.Size(150, 28);
            this.btnSugerirBaja.TabIndex = 0;
            this.btnSugerirBaja.Text = "Sugerir Baja";
            this.btnSugerirBaja.UseVisualStyleBackColor = false;
            this.btnSugerirBaja.Click += new System.EventHandler(this.BtnSugerirBaja_Click);
            //
            // btnRefrescar
            //
            this.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefrescar.Location = new System.Drawing.Point(168, 10);
            this.btnRefrescar.Name = "btnRefrescar";
            this.btnRefrescar.Size = new System.Drawing.Size(32, 28);
            this.btnRefrescar.TabIndex = 1;
            this.btnRefrescar.Text = "↻";
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);
            //
            // lblConteo
            //
            this.lblConteo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblConteo.ForeColor = System.Drawing.Color.DimGray;
            this.lblConteo.Location = new System.Drawing.Point(208, 14);
            this.lblConteo.Name = "lblConteo";
            this.lblConteo.Size = new System.Drawing.Size(300, 23);
            this.lblConteo.TabIndex = 2;
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
            this.panelStatus.Size = new System.Drawing.Size(900, 26);
            this.panelStatus.TabIndex = 2;
            //
            // lblMensaje
            //
            this.lblMensaje.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.Location = new System.Drawing.Point(8, 4);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(884, 18);
            this.lblMensaje.TabIndex = 0;
            //
            // dgvPromociones
            //
            this.dgvPromociones.AllowUserToAddRows = false;
            this.dgvPromociones.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(248)))), ((int)(((byte)(252)))));
            this.dgvPromociones.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvPromociones.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPromociones.BackgroundColor = System.Drawing.Color.White;
            this.dgvPromociones.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(182)))), ((int)(((byte)(193)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvPromociones.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvPromociones.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPromociones.Location = new System.Drawing.Point(0, 50);
            this.dgvPromociones.Name = "dgvPromociones";
            this.dgvPromociones.ReadOnly = true;
            this.dgvPromociones.RowHeadersVisible = false;
            this.dgvPromociones.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPromociones.Size = new System.Drawing.Size(900, 424);
            this.dgvPromociones.TabIndex = 1;
            this.dgvPromociones.SelectionChanged += new System.EventHandler(this.DgvPromociones_SelectionChanged);
            //
            // PromocionesVigentesForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 500);
            this.Controls.Add(this.dgvPromociones);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new System.Drawing.Size(720, 400);
            this.Name = "PromocionesVigentesForm";
            this.Text = "Promociones Vigentes";
            this.Load += new System.EventHandler(this.PromocionesVigentesForm_Load);
            this.panelTop.ResumeLayout(false);
            this.panelStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPromociones)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnSugerirBaja;
        private System.Windows.Forms.Button btnRefrescar;
        private System.Windows.Forms.Label lblConteo;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblMensaje;
        private System.Windows.Forms.DataGridView dgvPromociones;
    }
}
