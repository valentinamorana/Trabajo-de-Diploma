namespace GUI
{
    partial class PromocionesAdministracionForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnUsarSugerencia = new System.Windows.Forms.Button();
            this.btnNuevaManual = new System.Windows.Forms.Button();
            this.btnDesactivar = new System.Windows.Forms.Button();
            this.btnAprobarBaja = new System.Windows.Forms.Button();
            this.btnRechazarBaja = new System.Windows.Forms.Button();
            this.btnRefrescar = new System.Windows.Forms.Button();
            this.lblSugerenciasTitulo = new System.Windows.Forms.Label();
            this.dgvSugerencias = new System.Windows.Forms.DataGridView();
            this.lblPromocionesTitulo = new System.Windows.Forms.Label();
            this.lblConteo = new System.Windows.Forms.Label();
            this.dgvPromociones = new System.Windows.Forms.DataGridView();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.splitPrincipal = new System.Windows.Forms.SplitContainer();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSugerencias)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPromociones)).BeginInit();
            this.panelStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitPrincipal)).BeginInit();
            this.splitPrincipal.Panel1.SuspendLayout();
            this.splitPrincipal.Panel2.SuspendLayout();
            this.splitPrincipal.SuspendLayout();
            this.SuspendLayout();
            //
            // panelTop
            //
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelTop.Controls.Add(this.btnUsarSugerencia);
            this.panelTop.Controls.Add(this.btnNuevaManual);
            this.panelTop.Controls.Add(this.btnDesactivar);
            this.panelTop.Controls.Add(this.btnAprobarBaja);
            this.panelTop.Controls.Add(this.btnRechazarBaja);
            this.panelTop.Controls.Add(this.btnRefrescar);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 4);
            this.panelTop.Size = new System.Drawing.Size(1040, 46);
            this.panelTop.TabIndex = 0;
            //
            // btnUsarSugerencia
            //
            this.btnUsarSugerencia.Enabled = false;
            this.btnUsarSugerencia.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUsarSugerencia.Location = new System.Drawing.Point(8, 8);
            this.btnUsarSugerencia.Name = "btnUsarSugerencia";
            this.btnUsarSugerencia.Size = new System.Drawing.Size(160, 28);
            this.btnUsarSugerencia.TabIndex = 0;
            this.btnUsarSugerencia.Tag = "promocion.btn.altadesdesugerencia";
            this.btnUsarSugerencia.Text = "Alta desde Sugerencia";
            this.btnUsarSugerencia.Click += new System.EventHandler(this.BtnUsarSugerencia_Click);
            //
            // btnNuevaManual
            //
            this.btnNuevaManual.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNuevaManual.Location = new System.Drawing.Point(176, 8);
            this.btnNuevaManual.Name = "btnNuevaManual";
            this.btnNuevaManual.Size = new System.Drawing.Size(130, 28);
            this.btnNuevaManual.TabIndex = 1;
            this.btnNuevaManual.Tag = "promocion.btn.altamanual";
            this.btnNuevaManual.Text = "Alta Manual";
            this.btnNuevaManual.Click += new System.EventHandler(this.BtnNuevaManual_Click);
            //
            // btnDesactivar
            //
            this.btnDesactivar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnDesactivar.Enabled = false;
            this.btnDesactivar.FlatAppearance.BorderSize = 0;
            this.btnDesactivar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDesactivar.ForeColor = System.Drawing.Color.White;
            this.btnDesactivar.Location = new System.Drawing.Point(320, 8);
            this.btnDesactivar.Name = "btnDesactivar";
            this.btnDesactivar.Size = new System.Drawing.Size(110, 28);
            this.btnDesactivar.TabIndex = 2;
            this.btnDesactivar.Tag = "promocion.btn.desactivar";
            this.btnDesactivar.Text = "Desactivar";
            this.btnDesactivar.UseVisualStyleBackColor = false;
            this.btnDesactivar.Click += new System.EventHandler(this.BtnDesactivar_Click);
            //
            // btnAprobarBaja
            //
            this.btnAprobarBaja.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(140)))), ((int)(((byte)(60)))));
            this.btnAprobarBaja.Enabled = false;
            this.btnAprobarBaja.FlatAppearance.BorderSize = 0;
            this.btnAprobarBaja.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAprobarBaja.ForeColor = System.Drawing.Color.White;
            this.btnAprobarBaja.Location = new System.Drawing.Point(436, 8);
            this.btnAprobarBaja.Name = "btnAprobarBaja";
            this.btnAprobarBaja.Size = new System.Drawing.Size(130, 28);
            this.btnAprobarBaja.TabIndex = 3;
            this.btnAprobarBaja.Tag = "promocion.btn.aprobarbaja";
            this.btnAprobarBaja.Text = "Aprobar Baja";
            this.btnAprobarBaja.UseVisualStyleBackColor = false;
            this.btnAprobarBaja.Click += new System.EventHandler(this.BtnAprobarBaja_Click);
            //
            // btnRechazarBaja
            //
            this.btnRechazarBaja.Enabled = false;
            this.btnRechazarBaja.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRechazarBaja.Location = new System.Drawing.Point(572, 8);
            this.btnRechazarBaja.Name = "btnRechazarBaja";
            this.btnRechazarBaja.Size = new System.Drawing.Size(130, 28);
            this.btnRechazarBaja.TabIndex = 4;
            this.btnRechazarBaja.Tag = "promocion.btn.rechazarbaja";
            this.btnRechazarBaja.Text = "Rechazar Baja";
            this.btnRechazarBaja.Click += new System.EventHandler(this.BtnRechazarBaja_Click);
            //
            // btnRefrescar
            //
            this.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefrescar.Location = new System.Drawing.Point(710, 8);
            this.btnRefrescar.Name = "btnRefrescar";
            this.btnRefrescar.Size = new System.Drawing.Size(32, 28);
            this.btnRefrescar.TabIndex = 5;
            this.btnRefrescar.Text = "↻";
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);
            //
            // splitPrincipal
            //
            this.splitPrincipal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitPrincipal.Location = new System.Drawing.Point(0, 46);
            this.splitPrincipal.Name = "splitPrincipal";
            this.splitPrincipal.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitPrincipal.Size = new System.Drawing.Size(1040, 484);
            this.splitPrincipal.SplitterDistance = 200;
            this.splitPrincipal.TabIndex = 1;
            //
            // splitPrincipal.Panel1
            //
            this.splitPrincipal.Panel1.Controls.Add(this.dgvSugerencias);
            this.splitPrincipal.Panel1.Controls.Add(this.lblSugerenciasTitulo);
            //
            // splitPrincipal.Panel2
            //
            this.splitPrincipal.Panel2.Controls.Add(this.dgvPromociones);
            this.splitPrincipal.Panel2.Controls.Add(this.lblConteo);
            this.splitPrincipal.Panel2.Controls.Add(this.lblPromocionesTitulo);
            //
            // lblSugerenciasTitulo
            //
            this.lblSugerenciasTitulo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.lblSugerenciasTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSugerenciasTitulo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSugerenciasTitulo.Location = new System.Drawing.Point(0, 0);
            this.lblSugerenciasTitulo.Name = "lblSugerenciasTitulo";
            this.lblSugerenciasTitulo.Padding = new System.Windows.Forms.Padding(6, 3, 0, 0);
            this.lblSugerenciasTitulo.Size = new System.Drawing.Size(1040, 22);
            this.lblSugerenciasTitulo.TabIndex = 0;
            this.lblSugerenciasTitulo.Tag = "promocion.titulosugerencias";
            this.lblSugerenciasTitulo.Text = "Sugerencias de Promoción pendientes (Gerencia)";
            //
            // dgvSugerencias
            //
            this.dgvSugerencias.AllowUserToAddRows = false;
            this.dgvSugerencias.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(248)))), ((int)(((byte)(252)))));
            this.dgvSugerencias.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvSugerencias.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvSugerencias.BackgroundColor = System.Drawing.Color.White;
            this.dgvSugerencias.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(182)))), ((int)(((byte)(193)))));
            this.dgvSugerencias.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvSugerencias.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSugerencias.Location = new System.Drawing.Point(0, 22);
            this.dgvSugerencias.Name = "dgvSugerencias";
            this.dgvSugerencias.ReadOnly = true;
            this.dgvSugerencias.RowHeadersVisible = false;
            this.dgvSugerencias.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSugerencias.Size = new System.Drawing.Size(1040, 178);
            this.dgvSugerencias.TabIndex = 1;
            this.dgvSugerencias.SelectionChanged += new System.EventHandler(this.DgvSugerencias_SelectionChanged);
            //
            // lblPromocionesTitulo
            //
            this.lblPromocionesTitulo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.lblPromocionesTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPromocionesTitulo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPromocionesTitulo.Location = new System.Drawing.Point(0, 0);
            this.lblPromocionesTitulo.Name = "lblPromocionesTitulo";
            this.lblPromocionesTitulo.Padding = new System.Windows.Forms.Padding(6, 3, 0, 0);
            this.lblPromocionesTitulo.Size = new System.Drawing.Size(1040, 22);
            this.lblPromocionesTitulo.TabIndex = 0;
            this.lblPromocionesTitulo.Tag = "promocion.titulotodas";
            this.lblPromocionesTitulo.Text = "Todas las promociones";
            //
            // lblConteo
            //
            this.lblConteo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblConteo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblConteo.ForeColor = System.Drawing.Color.DimGray;
            this.lblConteo.Location = new System.Drawing.Point(0, 258);
            this.lblConteo.Name = "lblConteo";
            this.lblConteo.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblConteo.Size = new System.Drawing.Size(1040, 20);
            this.lblConteo.TabIndex = 2;
            //
            // dgvPromociones
            //
            this.dgvPromociones.AllowUserToAddRows = false;
            this.dgvPromociones.AllowUserToDeleteRows = false;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(248)))), ((int)(((byte)(252)))));
            this.dgvPromociones.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvPromociones.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPromociones.BackgroundColor = System.Drawing.Color.White;
            this.dgvPromociones.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(182)))), ((int)(((byte)(193)))));
            this.dgvPromociones.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvPromociones.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPromociones.Location = new System.Drawing.Point(0, 22);
            this.dgvPromociones.Name = "dgvPromociones";
            this.dgvPromociones.ReadOnly = true;
            this.dgvPromociones.RowHeadersVisible = false;
            this.dgvPromociones.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPromociones.Size = new System.Drawing.Size(1040, 236);
            this.dgvPromociones.TabIndex = 1;
            this.dgvPromociones.SelectionChanged += new System.EventHandler(this.DgvPromociones_SelectionChanged);
            //
            // panelStatus
            //
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 530);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.panelStatus.Size = new System.Drawing.Size(1040, 26);
            this.panelStatus.TabIndex = 2;
            //
            // lblMensaje
            //
            this.lblMensaje.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensaje.Location = new System.Drawing.Point(8, 4);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(1024, 18);
            this.lblMensaje.TabIndex = 0;
            //
            // PromocionesAdministracionForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1040, 556);
            this.Controls.Add(this.splitPrincipal);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new System.Drawing.Size(880, 480);
            this.Name = "PromocionesAdministracionForm";
            this.Tag = "frm.promoadmin";
            this.Text = "Gestión de Promociones (Administración)";
            this.Load += new System.EventHandler(this.PromocionesAdministracionForm_Load);
            this.panelTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSugerencias)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPromociones)).EndInit();
            this.panelStatus.ResumeLayout(false);
            this.splitPrincipal.Panel1.ResumeLayout(false);
            this.splitPrincipal.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPrincipal)).EndInit();
            this.splitPrincipal.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnUsarSugerencia;
        private System.Windows.Forms.Button btnNuevaManual;
        private System.Windows.Forms.Button btnDesactivar;
        private System.Windows.Forms.Button btnAprobarBaja;
        private System.Windows.Forms.Button btnRechazarBaja;
        private System.Windows.Forms.Button btnRefrescar;
        private System.Windows.Forms.SplitContainer splitPrincipal;
        private System.Windows.Forms.Label lblSugerenciasTitulo;
        private System.Windows.Forms.DataGridView dgvSugerencias;
        private System.Windows.Forms.Label lblPromocionesTitulo;
        private System.Windows.Forms.Label lblConteo;
        private System.Windows.Forms.DataGridView dgvPromociones;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblMensaje;
    }
}
