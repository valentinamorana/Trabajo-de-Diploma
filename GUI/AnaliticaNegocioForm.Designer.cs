using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class AnaliticaNegocioForm
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
            this.lblTitulo      = new Label();
            this.lblReporte     = new Label();
            this.cmbReporte     = new ComboBox();
            this.btnGenerar     = new Button();
            this.pnlFiltros     = new Panel();
            this.lblResultado   = new Label();
            this.dgv            = new DataGridView();
            this.btnExportarPdf = new Button();
            this.btnExportarCsv = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font     = new Font(this.Font, FontStyle.Bold);
            this.lblTitulo.Location = new Point(15, 12);
            this.lblTitulo.Name     = "lblTitulo";
            this.lblTitulo.TabIndex = 0;

            // ── lblReporte / cmbReporte — selector de reporte (solo los permitidos) ──
            this.lblReporte.AutoSize = true;
            this.lblReporte.Location = new Point(15, 48);
            this.lblReporte.Name     = "lblReporte";
            this.lblReporte.TabIndex = 1;

            this.cmbReporte.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbReporte.Location      = new Point(150, 45);
            this.cmbReporte.Name          = "cmbReporte";
            this.cmbReporte.TabIndex      = 2;
            this.cmbReporte.Width         = 340;
            this.cmbReporte.SelectedIndexChanged += new System.EventHandler(this.CmbReporte_SelectedIndexChanged);

            // ── btnGenerar ─────────────────────────────────────────────────────
            this.btnGenerar.Height   = 28;
            this.btnGenerar.Location = new Point(500, 44);
            this.btnGenerar.Name     = "btnGenerar";
            this.btnGenerar.TabIndex = 4;
            this.btnGenerar.Width    = 110;
            this.btnGenerar.UseVisualStyleBackColor = true;
            this.btnGenerar.Click += new System.EventHandler(this.BtnGenerar_Click);

            // ── pnlFiltros — filtros propios del reporte elegido (criterio, umbral, cliente) ──
            this.pnlFiltros.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.pnlFiltros.Location = new Point(15, 80);
            this.pnlFiltros.Name     = "pnlFiltros";
            this.pnlFiltros.Size     = new Size(840, 30);
            this.pnlFiltros.TabIndex = 3;

            // ── lblResultado ───────────────────────────────────────────────────
            this.lblResultado.AutoSize  = true;
            this.lblResultado.ForeColor = Color.DimGray;
            this.lblResultado.Location  = new Point(15, 115);
            this.lblResultado.Name      = "lblResultado";
            this.lblResultado.TabIndex  = 5;

            // ── dgv ────────────────────────────────────────────────────────────
            this.dgv.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.dgv.AllowUserToAddRows    = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv.Location   = new Point(15, 140);
            this.dgv.Name       = "dgv";
            this.dgv.ReadOnly   = true;
            this.dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgv.Size       = new Size(840, 345);
            this.dgv.TabIndex   = 6;

            // ── btnExportarPdf / btnExportarCsv ───────────────────────────────
            this.btnExportarPdf.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnExportarPdf.Height   = 30;
            this.btnExportarPdf.Location = new Point(15, 495);
            this.btnExportarPdf.Name     = "btnExportarPdf";
            this.btnExportarPdf.TabIndex = 7;
            this.btnExportarPdf.Width    = 150;
            this.btnExportarPdf.UseVisualStyleBackColor = true;
            this.btnExportarPdf.Click += new System.EventHandler(this.BtnExportarPdf_Click);

            this.btnExportarCsv.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnExportarCsv.Height   = 30;
            this.btnExportarCsv.Location = new Point(175, 495);
            this.btnExportarCsv.Name     = "btnExportarCsv";
            this.btnExportarCsv.TabIndex = 8;
            this.btnExportarCsv.Width    = 150;
            this.btnExportarCsv.UseVisualStyleBackColor = true;
            this.btnExportarCsv.Click += new System.EventHandler(this.BtnExportarCsv_Click);

            // ── AnaliticaNegocioForm ───────────────────────────────────────────
            this.ClientSize = new Size(880, 560);
            this.MinimumSize = new Size(660, 400);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblReporte);
            this.Controls.Add(this.cmbReporte);
            this.Controls.Add(this.btnGenerar);
            this.Controls.Add(this.pnlFiltros);
            this.Controls.Add(this.lblResultado);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.btnExportarPdf);
            this.Controls.Add(this.btnExportarCsv);
            this.Name          = "AnaliticaNegocioForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text          = "Analítica de Negocio";

            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label        lblTitulo;
        private Label        lblReporte;
        private ComboBox     cmbReporte;
        private Button       btnGenerar;
        private Panel        pnlFiltros;
        private Label        lblResultado;
        private DataGridView dgv;
        private Button       btnExportarPdf;
        private Button       btnExportarCsv;
    }
}
