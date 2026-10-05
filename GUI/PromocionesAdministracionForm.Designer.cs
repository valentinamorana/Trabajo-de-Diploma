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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnRefrescar = new System.Windows.Forms.Button();
            this.flowSugerencias = new System.Windows.Forms.FlowLayoutPanel();
            this.btnUsarSugerencia = new System.Windows.Forms.Button();
            this.btnDescartarSugerencia = new System.Windows.Forms.Button();
            this.btnImprimirSugerencia = new System.Windows.Forms.Button();
            this.flowPromociones = new System.Windows.Forms.FlowLayoutPanel();
            this.btnNuevaManual = new System.Windows.Forms.Button();
            this.btnReformular = new System.Windows.Forms.Button();
            this.btnDescartarPromocion = new System.Windows.Forms.Button();
            this.btnDesactivar = new System.Windows.Forms.Button();
            this.btnAprobarBaja = new System.Windows.Forms.Button();
            this.btnRechazarBaja = new System.Windows.Forms.Button();
            this.btnHistorial = new System.Windows.Forms.Button();
            this.btnImprimir = new System.Windows.Forms.Button();
            this.lblSugerenciasTitulo = new System.Windows.Forms.Label();
            this.dgvSugerencias = new System.Windows.Forms.DataGridView();
            this.lblPromocionesTitulo = new System.Windows.Forms.Label();
            this.lblConteo = new System.Windows.Forms.Label();
            this.dgvPromociones = new System.Windows.Forms.DataGridView();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.splitPrincipal = new System.Windows.Forms.SplitContainer();
            this.tip = new System.Windows.Forms.ToolTip(this.components);
            this.panelTop.SuspendLayout();
            this.flowSugerencias.SuspendLayout();
            this.flowPromociones.SuspendLayout();
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
            this.panelTop.Controls.Add(this.btnRefrescar);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 4);
            this.panelTop.Size = new System.Drawing.Size(1040, 40);
            this.panelTop.TabIndex = 0;
            //
            // btnRefrescar
            //
            this.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefrescar.Location = new System.Drawing.Point(8, 6);
            this.btnRefrescar.Name = "btnRefrescar";
            this.btnRefrescar.Size = new System.Drawing.Size(96, 28);
            this.btnRefrescar.TabIndex = 0;
            this.btnRefrescar.Text = "Actualizar";
            this.tip.SetToolTip(this.btnRefrescar, "Actualizar");
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);
            //
            // flowSugerencias
            //
            this.flowSugerencias.Controls.Add(this.btnUsarSugerencia);
            this.flowSugerencias.Controls.Add(this.btnDescartarSugerencia);
            this.flowSugerencias.Controls.Add(this.btnImprimirSugerencia);
            this.flowSugerencias.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowSugerencias.Location = new System.Drawing.Point(0, 22);
            this.flowSugerencias.Name = "flowSugerencias";
            this.flowSugerencias.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.flowSugerencias.Size = new System.Drawing.Size(1040, 36);
            this.flowSugerencias.TabIndex = 1;
            //
            // btnUsarSugerencia
            //
            this.btnUsarSugerencia.Enabled = false;
            this.btnUsarSugerencia.FlatAppearance.BorderSize = 0;
            this.btnUsarSugerencia.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUsarSugerencia.ForeColor = System.Drawing.Color.White;
            this.btnUsarSugerencia.Name = "btnUsarSugerencia";
            this.btnUsarSugerencia.Size = new System.Drawing.Size(170, 28);
            this.btnUsarSugerencia.TabIndex = 0;
            this.btnUsarSugerencia.Tag = "promocion.btn.altadesdesugerencia";
            this.btnUsarSugerencia.Text = "Alta desde Sugerencia";
            this.btnUsarSugerencia.UseVisualStyleBackColor = false;
            this.btnUsarSugerencia.Click += new System.EventHandler(this.BtnUsarSugerencia_Click);
            //
            // btnDescartarSugerencia
            //
            this.btnDescartarSugerencia.Enabled = false;
            this.btnDescartarSugerencia.FlatAppearance.BorderSize = 0;
            this.btnDescartarSugerencia.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDescartarSugerencia.ForeColor = System.Drawing.Color.White;
            this.btnDescartarSugerencia.Name = "btnDescartarSugerencia";
            this.btnDescartarSugerencia.Size = new System.Drawing.Size(170, 28);
            this.btnDescartarSugerencia.TabIndex = 1;
            this.btnDescartarSugerencia.Tag = "promocion.btn.descartarsugerencia";
            this.btnDescartarSugerencia.Text = "Descartar sugerencia";
            this.btnDescartarSugerencia.UseVisualStyleBackColor = false;
            this.btnDescartarSugerencia.Click += new System.EventHandler(this.BtnDescartarSugerencia_Click);
            //
            // btnImprimirSugerencia
            //
            this.btnImprimirSugerencia.Enabled = false;
            this.btnImprimirSugerencia.FlatAppearance.BorderSize = 0;
            this.btnImprimirSugerencia.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImprimirSugerencia.ForeColor = System.Drawing.Color.White;
            this.btnImprimirSugerencia.Name = "btnImprimirSugerencia";
            this.btnImprimirSugerencia.Size = new System.Drawing.Size(170, 28);
            this.btnImprimirSugerencia.TabIndex = 2;
            this.btnImprimirSugerencia.Tag = "promocion.btn.imprimirsugerencia";
            this.btnImprimirSugerencia.Text = "Imprimir sugerencia";
            this.btnImprimirSugerencia.UseVisualStyleBackColor = false;
            this.btnImprimirSugerencia.Click += new System.EventHandler(this.BtnImprimirSugerencia_Click);
            //
            // flowPromociones
            //
            this.flowPromociones.Controls.Add(this.btnNuevaManual);
            this.flowPromociones.Controls.Add(this.btnReformular);
            this.flowPromociones.Controls.Add(this.btnDescartarPromocion);
            this.flowPromociones.Controls.Add(this.btnDesactivar);
            this.flowPromociones.Controls.Add(this.btnAprobarBaja);
            this.flowPromociones.Controls.Add(this.btnRechazarBaja);
            this.flowPromociones.Controls.Add(this.btnHistorial);
            this.flowPromociones.Controls.Add(this.btnImprimir);
            this.flowPromociones.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowPromociones.Location = new System.Drawing.Point(0, 22);
            this.flowPromociones.Name = "flowPromociones";
            this.flowPromociones.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.flowPromociones.Size = new System.Drawing.Size(1040, 36);
            this.flowPromociones.TabIndex = 1;
            //
            // btnNuevaManual
            //
            this.btnNuevaManual.FlatAppearance.BorderSize = 0;
            this.btnNuevaManual.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNuevaManual.ForeColor = System.Drawing.Color.White;
            this.btnNuevaManual.Name = "btnNuevaManual";
            this.btnNuevaManual.Size = new System.Drawing.Size(110, 28);
            this.btnNuevaManual.TabIndex = 0;
            this.btnNuevaManual.Tag = "promocion.btn.altamanual";
            this.btnNuevaManual.Text = "Alta Manual";
            this.btnNuevaManual.UseVisualStyleBackColor = false;
            this.btnNuevaManual.Click += new System.EventHandler(this.BtnNuevaManual_Click);
            //
            // btnReformular
            //
            this.btnReformular.Enabled = false;
            this.btnReformular.FlatAppearance.BorderSize = 0;
            this.btnReformular.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReformular.ForeColor = System.Drawing.Color.White;
            this.btnReformular.Name = "btnReformular";
            this.btnReformular.Size = new System.Drawing.Size(105, 28);
            this.btnReformular.TabIndex = 1;
            this.btnReformular.Tag = "promocion.btn.reformular";
            this.btnReformular.Text = "Reformular";
            this.btnReformular.UseVisualStyleBackColor = false;
            this.btnReformular.Click += new System.EventHandler(this.BtnReformular_Click);
            //
            // btnDescartarPromocion
            //
            this.btnDescartarPromocion.Enabled = false;
            this.btnDescartarPromocion.FlatAppearance.BorderSize = 0;
            this.btnDescartarPromocion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDescartarPromocion.ForeColor = System.Drawing.Color.White;
            this.btnDescartarPromocion.Name = "btnDescartarPromocion";
            this.btnDescartarPromocion.Size = new System.Drawing.Size(105, 28);
            this.btnDescartarPromocion.TabIndex = 2;
            this.btnDescartarPromocion.Tag = "promocion.btn.descartar";
            this.btnDescartarPromocion.Text = "Descartar";
            this.btnDescartarPromocion.UseVisualStyleBackColor = false;
            this.btnDescartarPromocion.Click += new System.EventHandler(this.BtnDescartarPromocion_Click);
            //
            // btnDesactivar
            //
            this.btnDesactivar.Enabled = false;
            this.btnDesactivar.FlatAppearance.BorderSize = 0;
            this.btnDesactivar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDesactivar.ForeColor = System.Drawing.Color.White;
            this.btnDesactivar.Name = "btnDesactivar";
            this.btnDesactivar.Size = new System.Drawing.Size(105, 28);
            this.btnDesactivar.TabIndex = 3;
            this.btnDesactivar.Tag = "promocion.btn.desactivar";
            this.btnDesactivar.Text = "Desactivar";
            this.btnDesactivar.UseVisualStyleBackColor = false;
            this.btnDesactivar.Click += new System.EventHandler(this.BtnDesactivar_Click);
            //
            // btnAprobarBaja
            //
            this.btnAprobarBaja.Enabled = false;
            this.btnAprobarBaja.FlatAppearance.BorderSize = 0;
            this.btnAprobarBaja.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAprobarBaja.ForeColor = System.Drawing.Color.White;
            this.btnAprobarBaja.Name = "btnAprobarBaja";
            this.btnAprobarBaja.Size = new System.Drawing.Size(115, 28);
            this.btnAprobarBaja.TabIndex = 4;
            this.btnAprobarBaja.Tag = "promocion.btn.aprobarbaja";
            this.btnAprobarBaja.Text = "Aprobar Baja";
            this.btnAprobarBaja.UseVisualStyleBackColor = false;
            this.btnAprobarBaja.Click += new System.EventHandler(this.BtnAprobarBaja_Click);
            //
            // btnRechazarBaja
            //
            this.btnRechazarBaja.Enabled = false;
            this.btnRechazarBaja.FlatAppearance.BorderSize = 0;
            this.btnRechazarBaja.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRechazarBaja.ForeColor = System.Drawing.Color.White;
            this.btnRechazarBaja.Name = "btnRechazarBaja";
            this.btnRechazarBaja.Size = new System.Drawing.Size(115, 28);
            this.btnRechazarBaja.TabIndex = 5;
            this.btnRechazarBaja.Tag = "promocion.btn.rechazarbaja";
            this.btnRechazarBaja.Text = "Rechazar Baja";
            this.btnRechazarBaja.UseVisualStyleBackColor = false;
            this.btnRechazarBaja.Click += new System.EventHandler(this.BtnRechazarBaja_Click);
            //
            // btnHistorial
            //
            this.btnHistorial.Enabled = false;
            this.btnHistorial.FlatAppearance.BorderSize = 0;
            this.btnHistorial.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHistorial.ForeColor = System.Drawing.Color.White;
            this.btnHistorial.Name = "btnHistorial";
            this.btnHistorial.Size = new System.Drawing.Size(95, 28);
            this.btnHistorial.TabIndex = 6;
            this.btnHistorial.Tag = "promocion.btn.historial";
            this.btnHistorial.Text = "Historial";
            this.btnHistorial.UseVisualStyleBackColor = false;
            this.btnHistorial.Click += new System.EventHandler(this.BtnHistorial_Click);
            //
            // btnImprimir
            //
            this.btnImprimir.Enabled = false;
            this.btnImprimir.FlatAppearance.BorderSize = 0;
            this.btnImprimir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImprimir.ForeColor = System.Drawing.Color.White;
            this.btnImprimir.Name = "btnImprimir";
            this.btnImprimir.Size = new System.Drawing.Size(105, 28);
            this.btnImprimir.TabIndex = 7;
            this.btnImprimir.Tag = "promocion.btn.imprimir";
            this.btnImprimir.Text = "Imprimir ▾";
            this.btnImprimir.UseVisualStyleBackColor = false;
            this.btnImprimir.Click += new System.EventHandler(this.BtnImprimir_Click);
            //
            // splitPrincipal
            //
            this.splitPrincipal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitPrincipal.Location = new System.Drawing.Point(0, 40);
            this.splitPrincipal.Name = "splitPrincipal";
            this.splitPrincipal.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitPrincipal.Size = new System.Drawing.Size(1040, 514);
            this.splitPrincipal.SplitterDistance = 220;
            this.splitPrincipal.TabIndex = 1;
            //
            // splitPrincipal.Panel1 (Fill primero; los Top se apilan en orden inverso)
            //
            this.splitPrincipal.Panel1.Controls.Add(this.dgvSugerencias);
            this.splitPrincipal.Panel1.Controls.Add(this.flowSugerencias);
            this.splitPrincipal.Panel1.Controls.Add(this.lblSugerenciasTitulo);
            //
            // splitPrincipal.Panel2
            //
            this.splitPrincipal.Panel2.Controls.Add(this.dgvPromociones);
            this.splitPrincipal.Panel2.Controls.Add(this.lblConteo);
            this.splitPrincipal.Panel2.Controls.Add(this.flowPromociones);
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
            this.dgvSugerencias.Location = new System.Drawing.Point(0, 58);
            this.dgvSugerencias.MultiSelect = false;
            this.dgvSugerencias.Name = "dgvSugerencias";
            this.dgvSugerencias.ReadOnly = true;
            this.dgvSugerencias.RowHeadersVisible = false;
            this.dgvSugerencias.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSugerencias.Size = new System.Drawing.Size(1040, 162);
            this.dgvSugerencias.TabIndex = 2;
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
            this.lblConteo.Location = new System.Drawing.Point(0, 270);
            this.lblConteo.Name = "lblConteo";
            this.lblConteo.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblConteo.Size = new System.Drawing.Size(1040, 20);
            this.lblConteo.TabIndex = 3;
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
            this.dgvPromociones.Location = new System.Drawing.Point(0, 58);
            this.dgvPromociones.MultiSelect = false;
            this.dgvPromociones.Name = "dgvPromociones";
            this.dgvPromociones.ReadOnly = true;
            this.dgvPromociones.RowHeadersVisible = false;
            this.dgvPromociones.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPromociones.Size = new System.Drawing.Size(1040, 212);
            this.dgvPromociones.TabIndex = 2;
            this.dgvPromociones.SelectionChanged += new System.EventHandler(this.DgvPromociones_SelectionChanged);
            //
            // panelStatus
            //
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(235)))), ((int)(((byte)(245)))));
            this.panelStatus.Controls.Add(this.lblMensaje);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 554);
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
            this.ClientSize = new System.Drawing.Size(1040, 580);
            this.Controls.Add(this.splitPrincipal);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new System.Drawing.Size(960, 500);
            this.Name = "PromocionesAdministracionForm";
            this.Tag = "frm.promoadmin";
            this.Text = "Gestión de Promociones (Administración)";
            this.Load += new System.EventHandler(this.PromocionesAdministracionForm_Load);
            this.panelTop.ResumeLayout(false);
            this.flowSugerencias.ResumeLayout(false);
            this.flowPromociones.ResumeLayout(false);
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
        private System.Windows.Forms.Button btnRefrescar;
        private System.Windows.Forms.FlowLayoutPanel flowSugerencias;
        private System.Windows.Forms.Button btnUsarSugerencia;
        private System.Windows.Forms.Button btnDescartarSugerencia;
        private System.Windows.Forms.Button btnImprimirSugerencia;
        private System.Windows.Forms.FlowLayoutPanel flowPromociones;
        private System.Windows.Forms.Button btnNuevaManual;
        private System.Windows.Forms.Button btnReformular;
        private System.Windows.Forms.Button btnDescartarPromocion;
        private System.Windows.Forms.Button btnDesactivar;
        private System.Windows.Forms.Button btnAprobarBaja;
        private System.Windows.Forms.Button btnRechazarBaja;
        private System.Windows.Forms.Button btnHistorial;
        private System.Windows.Forms.Button btnImprimir;
        private System.Windows.Forms.ToolTip tip;
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
