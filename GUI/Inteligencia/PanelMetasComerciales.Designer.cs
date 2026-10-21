using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace GUI.Inteligencia
{
    partial class PanelMetasComerciales
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
            this.components = new System.ComponentModel.Container();
            this.pnlFiltros = new Panel();
            this.lblMes = new Label();
            this.dtpMes = new DateTimePicker();
            this.btnActualizar = new Button();
            this.lblInfo = new Label();
            this.split = new SplitContainer();
            this.dgvMetas = new DataGridView();
            this.chartMetas = new Chart();
            this.pnlEditor = new Panel();
            this.lblIndicador = new Label();
            this.cmbIndicador = new ComboBox();
            this.lblObjetivo = new Label();
            this.numObjetivo = new NumericUpDown();
            this.btnGuardar = new Button();
            this.btnEliminar = new Button();
            this.lblMensaje = new Label();
            this.tip = new ToolTip(this.components);
            this.pnlFiltros.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMetas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartMetas)).BeginInit();
            this.pnlEditor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numObjetivo)).BeginInit();
            this.SuspendLayout();

            // ── pnlFiltros: mes y actualizar ──────────────────────────────────
            this.pnlFiltros.Controls.Add(this.lblMes);
            this.pnlFiltros.Controls.Add(this.dtpMes);
            this.pnlFiltros.Controls.Add(this.btnActualizar);
            this.pnlFiltros.Dock = DockStyle.Top;
            this.pnlFiltros.Height = 40;
            this.pnlFiltros.Name = "pnlFiltros";

            this.lblMes.AutoSize = true;
            this.lblMes.Location = new Point(6, 12);
            this.lblMes.Name = "lblMes";
            this.lblMes.Text = "Mes:";

            this.dtpMes.CustomFormat = "MMMM yyyy";
            this.dtpMes.Format = DateTimePickerFormat.Custom;
            this.dtpMes.Location = new Point(60, 8);
            this.dtpMes.Name = "dtpMes";
            this.dtpMes.ShowUpDown = true;
            this.dtpMes.Size = new Size(170, 23);
            this.dtpMes.ValueChanged += new System.EventHandler(this.DtpMes_ValueChanged);

            this.btnActualizar.Location = new Point(240, 6);
            this.btnActualizar.Name = "btnActualizar";
            this.btnActualizar.Size = new Size(120, 28);
            this.btnActualizar.Text = "Actualizar";
            this.btnActualizar.Click += new System.EventHandler(this.BtnActualizar_Click);

            // ── lblInfo ───────────────────────────────────────────────────────
            this.lblInfo.Dock = DockStyle.Top;
            this.lblInfo.ForeColor = Tema.TextoMuted;
            this.lblInfo.Height = 36;
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Padding = new Padding(6, 0, 6, 0);

            // ── split: grilla arriba, gráfico abajo ───────────────────────────
            this.split.Dock = DockStyle.Fill;
            this.split.Name = "split";
            this.split.Orientation = Orientation.Horizontal;
            this.split.Panel1.Controls.Add(this.dgvMetas);
            this.split.Panel2.Controls.Add(this.chartMetas);

            this.dgvMetas.AllowUserToAddRows = false;
            this.dgvMetas.AllowUserToDeleteRows = false;
            this.dgvMetas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvMetas.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvMetas.Dock = DockStyle.Fill;
            this.dgvMetas.MultiSelect = false;
            this.dgvMetas.Name = "dgvMetas";
            this.dgvMetas.ReadOnly = true;
            this.dgvMetas.RowHeadersVisible = false;
            this.dgvMetas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvMetas.CellFormatting += new DataGridViewCellFormattingEventHandler(this.DgvMetas_CellFormatting);
            this.dgvMetas.SelectionChanged += new System.EventHandler(this.DgvMetas_SelectionChanged);

            this.chartMetas.ChartAreas.Add(new ChartArea("principal"));
            this.chartMetas.Legends.Add(new Legend("leyenda"));
            this.chartMetas.Dock = DockStyle.Fill;
            this.chartMetas.Name = "chartMetas";

            // ── pnlEditor: fijar o quitar la meta ─────────────────────────────
            this.pnlEditor.Controls.Add(this.lblIndicador);
            this.pnlEditor.Controls.Add(this.cmbIndicador);
            this.pnlEditor.Controls.Add(this.lblObjetivo);
            this.pnlEditor.Controls.Add(this.numObjetivo);
            this.pnlEditor.Controls.Add(this.btnGuardar);
            this.pnlEditor.Controls.Add(this.btnEliminar);
            this.pnlEditor.Dock = DockStyle.Bottom;
            this.pnlEditor.Height = 44;
            this.pnlEditor.Name = "pnlEditor";

            this.lblIndicador.AutoSize = true;
            this.lblIndicador.Location = new Point(6, 14);
            this.lblIndicador.Name = "lblIndicador";
            this.lblIndicador.Text = "Indicador:";

            this.cmbIndicador.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbIndicador.Location = new Point(90, 10);
            this.cmbIndicador.Name = "cmbIndicador";
            this.cmbIndicador.Size = new Size(240, 23);
            this.cmbIndicador.SelectedIndexChanged += new System.EventHandler(this.CmbIndicador_SelectedIndexChanged);

            this.lblObjetivo.AutoSize = true;
            this.lblObjetivo.Location = new Point(345, 14);
            this.lblObjetivo.Name = "lblObjetivo";
            this.lblObjetivo.Text = "Objetivo:";

            this.numObjetivo.DecimalPlaces = 2;
            this.numObjetivo.Location = new Point(420, 10);
            this.numObjetivo.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            this.numObjetivo.Name = "numObjetivo";
            this.numObjetivo.Size = new Size(130, 23);
            this.numObjetivo.ThousandsSeparator = true;

            this.btnGuardar.Location = new Point(565, 7);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.Size = new Size(130, 30);
            this.btnGuardar.Text = "Guardar meta";
            this.btnGuardar.Click += new System.EventHandler(this.BtnGuardar_Click);

            this.btnEliminar.Location = new Point(705, 7);
            this.btnEliminar.Name = "btnEliminar";
            this.btnEliminar.Size = new Size(130, 30);
            this.btnEliminar.Text = "Eliminar meta";
            this.btnEliminar.Click += new System.EventHandler(this.BtnEliminar_Click);

            // ── lblMensaje ────────────────────────────────────────────────────
            this.lblMensaje.Dock = DockStyle.Bottom;
            this.lblMensaje.Height = 24;
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Padding = new Padding(6, 4, 6, 0);

            // ── PanelMetasComerciales ─────────────────────────────────────────
            this.BackColor = Tema.Papel;
            this.Controls.Add(this.split);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.pnlFiltros);
            this.Controls.Add(this.pnlEditor);
            this.Controls.Add(this.lblMensaje);
            this.Name = "PanelMetasComerciales";
            this.Size = new Size(900, 560);

            this.pnlFiltros.ResumeLayout(false);
            this.pnlFiltros.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvMetas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartMetas)).EndInit();
            this.pnlEditor.ResumeLayout(false);
            this.pnlEditor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numObjetivo)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private Panel pnlFiltros;
        private Label lblMes;
        private DateTimePicker dtpMes;
        private Button btnActualizar;
        private Label lblInfo;
        private SplitContainer split;
        private DataGridView dgvMetas;
        private Chart chartMetas;
        private Panel pnlEditor;
        private Label lblIndicador;
        private ComboBox cmbIndicador;
        private Label lblObjetivo;
        private NumericUpDown numObjetivo;
        private Button btnGuardar;
        private Button btnEliminar;
        private Label lblMensaje;
        private ToolTip tip;
    }
}
