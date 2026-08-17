using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class AnalisisEscasezForm
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
            this.lblUmbral      = new Label();
            this.numUmbral      = new NumericUpDown();
            this.btnGenerar     = new Button();
            this.lblResultado   = new Label();
            this.dgv            = new DataGridView();
            this.btnExportarPdf = new Button();
            this.btnExportarCsv = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.numUmbral)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font     = new Font(this.Font, FontStyle.Bold);
            this.lblTitulo.Location = new Point(15, 12);
            this.lblTitulo.Name     = "lblTitulo";
            this.lblTitulo.TabIndex = 0;

            // ── lblUmbral / numUmbral ──────────────────────────────────────────
            this.lblUmbral.AutoSize = true;
            this.lblUmbral.Location = new Point(15, 48);
            this.lblUmbral.Name     = "lblUmbral";
            this.lblUmbral.TabIndex = 1;

            this.numUmbral.Location = new Point(150, 45);
            this.numUmbral.Name     = "numUmbral";
            this.numUmbral.TabIndex = 2;
            this.numUmbral.Width    = 80;
            this.numUmbral.Minimum  = 0;
            this.numUmbral.Maximum  = 1000;

            // ── btnGenerar ─────────────────────────────────────────────────────
            this.btnGenerar.Height   = 28;
            this.btnGenerar.Location = new Point(250, 44);
            this.btnGenerar.Name     = "btnGenerar";
            this.btnGenerar.TabIndex = 3;
            this.btnGenerar.Width    = 150;
            this.btnGenerar.UseVisualStyleBackColor = true;
            this.btnGenerar.Click += new System.EventHandler(this.BtnGenerar_Click);

            // ── lblResultado ───────────────────────────────────────────────────
            this.lblResultado.AutoSize  = true;
            this.lblResultado.ForeColor = Color.DimGray;
            this.lblResultado.Location  = new Point(15, 80);
            this.lblResultado.Name      = "lblResultado";
            this.lblResultado.TabIndex  = 4;

            // ── dgv ────────────────────────────────────────────────────────────
            this.dgv.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.dgv.AllowUserToAddRows    = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv.Location   = new Point(15, 105);
            this.dgv.Name       = "dgv";
            this.dgv.ReadOnly   = true;
            this.dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgv.Size       = new Size(840, 380);
            this.dgv.TabIndex   = 5;

            // ── btnExportarPdf / btnExportarCsv ───────────────────────────────
            this.btnExportarPdf.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnExportarPdf.Height   = 30;
            this.btnExportarPdf.Location = new Point(15, 495);
            this.btnExportarPdf.Name     = "btnExportarPdf";
            this.btnExportarPdf.TabIndex = 6;
            this.btnExportarPdf.Width    = 150;
            this.btnExportarPdf.UseVisualStyleBackColor = true;
            this.btnExportarPdf.Click += new System.EventHandler(this.BtnExportarPdf_Click);

            this.btnExportarCsv.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnExportarCsv.Height   = 30;
            this.btnExportarCsv.Location = new Point(175, 495);
            this.btnExportarCsv.Name     = "btnExportarCsv";
            this.btnExportarCsv.TabIndex = 7;
            this.btnExportarCsv.Width    = 150;
            this.btnExportarCsv.UseVisualStyleBackColor = true;
            this.btnExportarCsv.Click += new System.EventHandler(this.BtnExportarCsv_Click);

            // ── AnalisisEscasezForm ────────────────────────────────────────────
            this.ClientSize = new Size(880, 560);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblUmbral);
            this.Controls.Add(this.numUmbral);
            this.Controls.Add(this.btnGenerar);
            this.Controls.Add(this.lblResultado);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.btnExportarPdf);
            this.Controls.Add(this.btnExportarCsv);
            this.Name          = "AnalisisEscasezForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text          = "Escasez de Stock";

            ((System.ComponentModel.ISupportInitialize)(this.numUmbral)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label          lblTitulo;
        private Label          lblUmbral;
        private NumericUpDown  numUmbral;
        private Button         btnGenerar;
        private Label          lblResultado;
        private DataGridView   dgv;
        private Button         btnExportarPdf;
        private Button         btnExportarCsv;
    }
}
