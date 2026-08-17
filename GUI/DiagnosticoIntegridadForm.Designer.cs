using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class DiagnosticoIntegridadForm
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
            this.tabs               = new TabControl();
            this.tabDiagnostico     = new TabPage();
            this.panelEstado        = new Panel();
            this.lblEstadoDVV       = new Label();
            this.lblDVVDetalle      = new Label();
            this.panelBotonesDiag   = new FlowLayoutPanel();
            this.btnRecalcularTodo  = new Button();
            this.btnEspejo          = new Button();
            this.btnActualizar      = new Button();
            this.contenedorDiag     = new Panel();
            this.gridRotas          = new DataGridView();
            this.colId              = new DataGridViewTextBoxColumn();
            this.colUsuario         = new DataGridViewTextBoxColumn();
            this.colDVHAlm          = new DataGridViewTextBoxColumn();
            this.colDVHCalc         = new DataGridViewTextBoxColumn();
            this.colEstado          = new DataGridViewTextBoxColumn();
            this.lblGridVacio       = new Label();
            this.lblFilasRotas      = new Label();
            this.tabHistorial       = new TabPage();
            this.gridHistorial      = new DataGridView();
            this.hFecha             = new DataGridViewTextBoxColumn();
            this.hTabla             = new DataGridViewTextBoxColumn();
            this.hDVVAlm            = new DataGridViewTextBoxColumn();
            this.hDVVCalc           = new DataGridViewTextBoxColumn();
            this.hRotas             = new DataGridViewTextBoxColumn();
            this.hResultado         = new DataGridViewTextBoxColumn();
            this.hOrigen            = new DataGridViewTextBoxColumn();
            this.panelBotonesHist   = new FlowLayoutPanel();
            this.btnActualizarHist  = new Button();
            this.tabs.SuspendLayout();
            this.tabDiagnostico.SuspendLayout();
            this.panelEstado.SuspendLayout();
            this.panelBotonesDiag.SuspendLayout();
            this.contenedorDiag.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRotas)).BeginInit();
            this.tabHistorial.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridHistorial)).BeginInit();
            this.panelBotonesHist.SuspendLayout();
            this.SuspendLayout();

            // ── tabs ───────────────────────────────────────────────────────────
            this.tabs.Controls.Add(this.tabDiagnostico);
            this.tabs.Controls.Add(this.tabHistorial);
            this.tabs.Dock     = DockStyle.Fill;
            this.tabs.Location = new Point(0, 0);
            this.tabs.Name     = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size     = new Size(820, 560);
            this.tabs.TabIndex = 0;

            // ── tabDiagnostico ─────────────────────────────────────────────────
            this.tabDiagnostico.Controls.Add(this.contenedorDiag);
            this.tabDiagnostico.Controls.Add(this.panelBotonesDiag);
            this.tabDiagnostico.Controls.Add(this.panelEstado);
            this.tabDiagnostico.Location = new Point(4, 24);
            this.tabDiagnostico.Name     = "tabDiagnostico";
            this.tabDiagnostico.Padding  = new Padding(3);
            this.tabDiagnostico.Size     = new Size(812, 532);
            this.tabDiagnostico.TabIndex = 0;
            this.tabDiagnostico.Text     = "Diagnóstico";
            this.tabDiagnostico.UseVisualStyleBackColor = true;

            // ── panelEstado ────────────────────────────────────────────────────
            this.panelEstado.BackColor = Color.FromArgb(245, 245, 250);
            this.panelEstado.Controls.Add(this.lblEstadoDVV);
            this.panelEstado.Controls.Add(this.lblDVVDetalle);
            this.panelEstado.Dock     = DockStyle.Top;
            this.panelEstado.Location = new Point(3, 3);
            this.panelEstado.Name     = "panelEstado";
            this.panelEstado.Padding  = new Padding(12, 8, 12, 8);
            this.panelEstado.Size     = new Size(806, 90);
            this.panelEstado.TabIndex = 0;

            this.lblEstadoDVV.AutoSize = true;
            this.lblEstadoDVV.Font     = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.lblEstadoDVV.Location = new Point(12, 10);
            this.lblEstadoDVV.Name     = "lblEstadoDVV";
            this.lblEstadoDVV.TabIndex = 0;

            this.lblDVVDetalle.AutoSize  = true;
            this.lblDVVDetalle.Font      = new Font("Segoe UI", 9F);
            this.lblDVVDetalle.ForeColor = Color.FromArgb(80, 80, 100);
            this.lblDVVDetalle.Location  = new Point(12, 38);
            this.lblDVVDetalle.Name      = "lblDVVDetalle";
            this.lblDVVDetalle.TabIndex  = 1;

            // ── panelBotonesDiag ──────────────────────────────────────────────
            this.panelBotonesDiag.BackColor = Color.FromArgb(245, 245, 250);
            this.panelBotonesDiag.Controls.Add(this.btnRecalcularTodo);
            this.panelBotonesDiag.Controls.Add(this.btnEspejo);
            this.panelBotonesDiag.Controls.Add(this.btnActualizar);
            this.panelBotonesDiag.Dock          = DockStyle.Bottom;
            this.panelBotonesDiag.FlowDirection = FlowDirection.RightToLeft;
            this.panelBotonesDiag.Location      = new Point(3, 465);
            this.panelBotonesDiag.Name          = "panelBotonesDiag";
            this.panelBotonesDiag.Padding       = new Padding(4);
            this.panelBotonesDiag.Size          = new Size(806, 44);
            this.panelBotonesDiag.TabIndex      = 1;

            this.btnRecalcularTodo.BackColor = Color.FromArgb(180, 80, 80);
            this.btnRecalcularTodo.FlatAppearance.BorderSize = 0;
            this.btnRecalcularTodo.FlatStyle = FlatStyle.Flat;
            this.btnRecalcularTodo.ForeColor = Color.White;
            this.btnRecalcularTodo.Height    = 32;
            this.btnRecalcularTodo.Name      = "btnRecalcularTodo";
            this.btnRecalcularTodo.TabIndex  = 0;
            this.btnRecalcularTodo.Text      = "Recalcular Todo";
            this.btnRecalcularTodo.Width     = 130;
            this.btnRecalcularTodo.Click += new System.EventHandler(this.BtnRecalcularTodo_Click);

            this.btnEspejo.BackColor = Color.FromArgb(80, 150, 90);
            this.btnEspejo.FlatAppearance.BorderSize = 0;
            this.btnEspejo.FlatStyle = FlatStyle.Flat;
            this.btnEspejo.ForeColor = Color.White;
            this.btnEspejo.Height    = 32;
            this.btnEspejo.Name      = "btnEspejo";
            this.btnEspejo.TabIndex  = 1;
            this.btnEspejo.Text      = "Recuperación (Espejo)...";
            this.btnEspejo.Width     = 180;
            this.btnEspejo.Click += new System.EventHandler(this.BtnEspejo_Click);

            this.btnActualizar.BackColor = Color.FromArgb(100, 160, 100);
            this.btnActualizar.FlatAppearance.BorderSize = 0;
            this.btnActualizar.FlatStyle = FlatStyle.Flat;
            this.btnActualizar.ForeColor = Color.White;
            this.btnActualizar.Height    = 32;
            this.btnActualizar.Name      = "btnActualizar";
            this.btnActualizar.TabIndex  = 2;
            this.btnActualizar.Text      = "Actualizar";
            this.btnActualizar.Width     = 100;
            this.btnActualizar.Click += new System.EventHandler(this.BtnActualizar_Click);

            // ── contenedorDiag ─────────────────────────────────────────────────
            this.contenedorDiag.Controls.Add(this.gridRotas);
            this.contenedorDiag.Controls.Add(this.lblGridVacio);
            this.contenedorDiag.Controls.Add(this.lblFilasRotas);
            this.contenedorDiag.Dock     = DockStyle.Fill;
            this.contenedorDiag.Location = new Point(3, 93);
            this.contenedorDiag.Name     = "contenedorDiag";
            this.contenedorDiag.Size     = new Size(806, 372);
            this.contenedorDiag.TabIndex = 2;

            // ── gridRotas ──────────────────────────────────────────────────────
            this.gridRotas.AllowUserToAddRows    = false;
            this.gridRotas.AllowUserToDeleteRows = false;
            this.gridRotas.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.gridRotas.BackgroundColor       = Color.White;
            this.gridRotas.BorderStyle           = BorderStyle.None;
            this.gridRotas.Columns.AddRange(new DataGridViewColumn[] {
                this.colId, this.colUsuario, this.colDVHAlm, this.colDVHCalc, this.colEstado });
            this.gridRotas.Dock              = DockStyle.Fill;
            this.gridRotas.Font              = new Font("Segoe UI", 9F);
            this.gridRotas.Name              = "gridRotas";
            this.gridRotas.ReadOnly          = true;
            this.gridRotas.RowHeadersVisible = false;
            this.gridRotas.SelectionMode     = DataGridViewSelectionMode.FullRowSelect;
            this.gridRotas.TabIndex          = 0;

            this.colId.FillWeight = 8F;
            this.colId.HeaderText = "ID";
            this.colId.Name       = "colId";
            this.colId.ReadOnly   = true;

            this.colUsuario.FillWeight = 25F;
            this.colUsuario.HeaderText = "Usuario";
            this.colUsuario.Name       = "colUsuario";
            this.colUsuario.ReadOnly   = true;

            this.colDVHAlm.FillWeight = 20F;
            this.colDVHAlm.HeaderText = "DVH Almacenado";
            this.colDVHAlm.Name       = "colDVHAlm";
            this.colDVHAlm.ReadOnly   = true;

            this.colDVHCalc.FillWeight = 20F;
            this.colDVHCalc.HeaderText = "DVH Calculado";
            this.colDVHCalc.Name       = "colDVHCalc";
            this.colDVHCalc.ReadOnly   = true;

            this.colEstado.FillWeight = 27F;
            this.colEstado.HeaderText = "Estado";
            this.colEstado.Name       = "colEstado";
            this.colEstado.ReadOnly   = true;

            // ── lblGridVacio ───────────────────────────────────────────────────
            this.lblGridVacio.BackColor  = Color.White;
            this.lblGridVacio.Dock       = DockStyle.Fill;
            this.lblGridVacio.Font       = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.lblGridVacio.ForeColor  = Color.FromArgb(40, 140, 60);
            this.lblGridVacio.Name       = "lblGridVacio";
            this.lblGridVacio.TabIndex   = 1;
            this.lblGridVacio.TextAlign  = ContentAlignment.MiddleCenter;
            this.lblGridVacio.Visible    = false;

            // ── lblFilasRotas ──────────────────────────────────────────────────
            this.lblFilasRotas.Dock     = DockStyle.Top;
            this.lblFilasRotas.Font     = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblFilasRotas.Height   = 22;
            this.lblFilasRotas.Name     = "lblFilasRotas";
            this.lblFilasRotas.Padding  = new Padding(4, 2, 0, 0);
            this.lblFilasRotas.TabIndex = 2;
            this.lblFilasRotas.Text     = "Filas con DVH inválido:";

            // ── tabHistorial ───────────────────────────────────────────────────
            this.tabHistorial.Controls.Add(this.gridHistorial);
            this.tabHistorial.Controls.Add(this.panelBotonesHist);
            this.tabHistorial.Location = new Point(4, 24);
            this.tabHistorial.Name     = "tabHistorial";
            this.tabHistorial.Padding  = new Padding(3);
            this.tabHistorial.Size     = new Size(812, 532);
            this.tabHistorial.TabIndex = 1;
            this.tabHistorial.Text     = "Historial de Verificaciones";
            this.tabHistorial.UseVisualStyleBackColor = true;

            // ── gridHistorial ──────────────────────────────────────────────────
            this.gridHistorial.AllowUserToAddRows    = false;
            this.gridHistorial.AllowUserToDeleteRows = false;
            this.gridHistorial.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.gridHistorial.BackgroundColor       = Color.White;
            this.gridHistorial.BorderStyle           = BorderStyle.None;
            this.gridHistorial.Columns.AddRange(new DataGridViewColumn[] {
                this.hFecha, this.hTabla, this.hDVVAlm, this.hDVVCalc, this.hRotas, this.hResultado, this.hOrigen });
            this.gridHistorial.Dock              = DockStyle.Fill;
            this.gridHistorial.Font              = new Font("Segoe UI", 9F);
            this.gridHistorial.Name              = "gridHistorial";
            this.gridHistorial.ReadOnly          = true;
            this.gridHistorial.RowHeadersVisible = false;
            this.gridHistorial.SelectionMode     = DataGridViewSelectionMode.FullRowSelect;
            this.gridHistorial.TabIndex          = 0;
            this.gridHistorial.CellFormatting += new DataGridViewCellFormattingEventHandler(this.GridHistorial_CellFormatting);

            this.hFecha.FillWeight = 22F;
            this.hFecha.HeaderText = "Fecha";
            this.hFecha.Name       = "hFecha";
            this.hFecha.ReadOnly   = true;

            this.hTabla.FillWeight = 15F;
            this.hTabla.HeaderText = "Tabla";
            this.hTabla.Name       = "hTabla";
            this.hTabla.ReadOnly   = true;

            this.hDVVAlm.FillWeight = 16F;
            this.hDVVAlm.HeaderText = "DVV Almacenado";
            this.hDVVAlm.Name       = "hDVVAlm";
            this.hDVVAlm.ReadOnly   = true;

            this.hDVVCalc.FillWeight = 16F;
            this.hDVVCalc.HeaderText = "DVV Calculado";
            this.hDVVCalc.Name       = "hDVVCalc";
            this.hDVVCalc.ReadOnly   = true;

            this.hRotas.FillWeight = 14F;
            this.hRotas.HeaderText = "Filas Corruptas";
            this.hRotas.Name       = "hRotas";
            this.hRotas.ReadOnly   = true;

            this.hResultado.FillWeight = 10F;
            this.hResultado.HeaderText = "Resultado";
            this.hResultado.Name       = "hResultado";
            this.hResultado.ReadOnly   = true;

            this.hOrigen.FillWeight = 12F;
            this.hOrigen.HeaderText = "Disparado por";
            this.hOrigen.Name       = "hOrigen";
            this.hOrigen.ReadOnly   = true;

            // ── panelBotonesHist ───────────────────────────────────────────────
            this.panelBotonesHist.BackColor = Color.FromArgb(245, 245, 250);
            this.panelBotonesHist.Controls.Add(this.btnActualizarHist);
            this.panelBotonesHist.Dock          = DockStyle.Bottom;
            this.panelBotonesHist.FlowDirection = FlowDirection.RightToLeft;
            this.panelBotonesHist.Location      = new Point(3, 465);
            this.panelBotonesHist.Name          = "panelBotonesHist";
            this.panelBotonesHist.Padding       = new Padding(4);
            this.panelBotonesHist.Size          = new Size(806, 44);
            this.panelBotonesHist.TabIndex      = 1;

            this.btnActualizarHist.BackColor = Color.FromArgb(100, 160, 100);
            this.btnActualizarHist.FlatAppearance.BorderSize = 0;
            this.btnActualizarHist.FlatStyle = FlatStyle.Flat;
            this.btnActualizarHist.ForeColor = Color.White;
            this.btnActualizarHist.Height    = 32;
            this.btnActualizarHist.Name      = "btnActualizarHist";
            this.btnActualizarHist.TabIndex  = 0;
            this.btnActualizarHist.Text      = "Actualizar";
            this.btnActualizarHist.Width     = 100;
            this.btnActualizarHist.Click += new System.EventHandler(this.BtnActualizarHist_Click);

            // ── DiagnosticoIntegridadForm ────────────────────────────────────────
            this.ClientSize    = new Size(820, 560);
            this.Controls.Add(this.tabs);
            this.Font          = new Font("Segoe UI", 9F);
            this.MinimumSize   = new Size(700, 460);
            this.Name          = "DiagnosticoIntegridadForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text          = "Diagnóstico de Integridad";

            this.tabs.ResumeLayout(false);
            this.tabDiagnostico.ResumeLayout(false);
            this.panelEstado.ResumeLayout(false);
            this.panelEstado.PerformLayout();
            this.panelBotonesDiag.ResumeLayout(false);
            this.contenedorDiag.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridRotas)).EndInit();
            this.tabHistorial.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridHistorial)).EndInit();
            this.panelBotonesHist.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private TabControl tabs;
        private TabPage    tabDiagnostico;
        private Panel      panelEstado;
        private Label      lblEstadoDVV;
        private Label      lblDVVDetalle;
        private FlowLayoutPanel panelBotonesDiag;
        private Button     btnRecalcularTodo;
        private Button     btnEspejo;
        private Button     btnActualizar;
        private Panel      contenedorDiag;
        private DataGridView gridRotas;
        private DataGridViewTextBoxColumn colId;
        private DataGridViewTextBoxColumn colUsuario;
        private DataGridViewTextBoxColumn colDVHAlm;
        private DataGridViewTextBoxColumn colDVHCalc;
        private DataGridViewTextBoxColumn colEstado;
        private Label      lblGridVacio;
        private Label      lblFilasRotas;
        private TabPage    tabHistorial;
        private DataGridView gridHistorial;
        private DataGridViewTextBoxColumn hFecha;
        private DataGridViewTextBoxColumn hTabla;
        private DataGridViewTextBoxColumn hDVVAlm;
        private DataGridViewTextBoxColumn hDVVCalc;
        private DataGridViewTextBoxColumn hRotas;
        private DataGridViewTextBoxColumn hResultado;
        private DataGridViewTextBoxColumn hOrigen;
        private FlowLayoutPanel panelBotonesHist;
        private Button     btnActualizarHist;
    }
}
