using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class DashboardControlStock
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        // Nota: las tarjetas del Kanban (una por prenda en mantenimiento) y el cartel
        // "— sin tareas —" se arman en runtime en ActualizarKanban() porque su cantidad y
        // contenido dependen de los datos cargados desde la BLL — no son representables como
        // controles fijos del Diseñador. El resto del formulario es estático y vive acá.
        private void InitializeComponent()
        {
            this.panelHeader  = new Panel();
            this.lblTitulo    = new Label();
            this.lblSub       = new Label();
            this.btnRefrescar = new Button();
            this.panelSbar    = new Panel();
            this.lblSesion    = new Label();
            this.flowCards    = new FlowLayoutPanel();
            this.cardDisp     = new Panel();
            this.numDisp      = new Label();
            this.txtDisp      = new Label();
            this.cardMant     = new Panel();
            this.numMant      = new Label();
            this.txtMant      = new Label();
            this.cardOcup     = new Panel();
            this.numOcup      = new Label();
            this.txtOcup      = new Label();
            this.wrapper      = new Panel();
            this.tbl          = new TableLayoutPanel();
            this.col1          = new Panel();
            this.colReciente   = new FlowLayoutPanel();
            this.lblColRec     = new Label();
            this.col2           = new Panel();
            this.colEnCurso     = new FlowLayoutPanel();
            this.lblColCur      = new Label();
            this.col3            = new Panel();
            this.colUrgente      = new FlowLayoutPanel();
            this.lblColUrg       = new Label();
            this.panelHeader.SuspendLayout();
            this.panelSbar.SuspendLayout();
            this.flowCards.SuspendLayout();
            this.cardDisp.SuspendLayout();
            this.cardMant.SuspendLayout();
            this.cardOcup.SuspendLayout();
            this.wrapper.SuspendLayout();
            this.tbl.SuspendLayout();
            this.col1.SuspendLayout();
            this.col2.SuspendLayout();
            this.col3.SuspendLayout();
            this.SuspendLayout();

            // ── panelHeader ────────────────────────────────────────────────────
            this.panelHeader.BackColor = Color.FromArgb(176, 62, 96);
            this.panelHeader.Controls.Add(this.lblTitulo);
            this.panelHeader.Controls.Add(this.lblSub);
            this.panelHeader.Controls.Add(this.btnRefrescar);
            this.panelHeader.Dock     = DockStyle.Top;
            this.panelHeader.Location = new Point(0, 0);
            this.panelHeader.Name     = "panelHeader";
            this.panelHeader.Size     = new Size(870, 62);
            this.panelHeader.TabIndex = 0;
            this.panelHeader.Paint  += new PaintEventHandler(this.PanelHeader_Paint);
            this.panelHeader.Resize += new System.EventHandler(this.PanelHeader_Resize);

            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.BackColor = Color.Transparent;
            this.lblTitulo.Font      = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitulo.ForeColor = Color.White;
            this.lblTitulo.Location  = new Point(14, 8);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Text      = "Panel de Stock";

            this.lblSub.AutoSize  = true;
            this.lblSub.BackColor = Color.Transparent;
            this.lblSub.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
            this.lblSub.ForeColor = Color.FromArgb(200, 255, 200, 220);
            this.lblSub.Location  = new Point(14, 36);
            this.lblSub.Name      = "lblSub";
            this.lblSub.TabIndex  = 1;
            this.lblSub.Text      = "WardrobeFlow  —  Stock";

            this.btnRefrescar.Anchor    = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRefrescar.BackColor = Color.FromArgb(210, 100, 135);
            this.btnRefrescar.Cursor    = Cursors.Hand;
            this.btnRefrescar.FlatAppearance.BorderColor = Color.FromArgb(180, 230, 140, 170);
            this.btnRefrescar.FlatAppearance.BorderSize  = 1;
            this.btnRefrescar.FlatStyle = FlatStyle.Flat;
            this.btnRefrescar.Font      = new Font("Segoe UI", 8.5F);
            this.btnRefrescar.ForeColor = Color.White;
            this.btnRefrescar.Location  = new Point(756, 17);
            this.btnRefrescar.Name      = "btnRefrescar";
            this.btnRefrescar.Size      = new Size(100, 28);
            this.btnRefrescar.TabIndex  = 2;
            this.btnRefrescar.Text      = "↻  Actualizar";
            this.btnRefrescar.UseVisualStyleBackColor = false;
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);

            // ── panelSbar ──────────────────────────────────────────────────────
            this.panelSbar.BackColor = Color.FromArgb(176, 62, 96);
            this.panelSbar.Controls.Add(this.lblSesion);
            this.panelSbar.Dock     = DockStyle.Bottom;
            this.panelSbar.Height   = 26;
            this.panelSbar.Location = new Point(0, 544);
            this.panelSbar.Name     = "panelSbar";
            this.panelSbar.Size     = new Size(870, 26);
            this.panelSbar.TabIndex = 3;

            this.lblSesion.Dock      = DockStyle.Fill;
            this.lblSesion.Font      = new Font("Segoe UI", 8F);
            this.lblSesion.ForeColor = Color.FromArgb(244, 212, 226);
            this.lblSesion.Location  = new Point(0, 0);
            this.lblSesion.Name      = "lblSesion";
            this.lblSesion.Padding   = new Padding(10, 0, 0, 0);
            this.lblSesion.Size      = new Size(870, 26);
            this.lblSesion.TabIndex  = 0;
            this.lblSesion.TextAlign = ContentAlignment.MiddleLeft;

            // ── flowCards ──────────────────────────────────────────────────────
            this.flowCards.BackColor = Color.FromArgb(240, 240, 245);
            this.flowCards.Controls.Add(this.cardDisp);
            this.flowCards.Controls.Add(this.cardMant);
            this.flowCards.Controls.Add(this.cardOcup);
            this.flowCards.Dock          = DockStyle.Top;
            this.flowCards.FlowDirection = FlowDirection.LeftToRight;
            this.flowCards.Height        = 168;
            this.flowCards.Location      = new Point(0, 62);
            this.flowCards.Name          = "flowCards";
            this.flowCards.Padding       = new Padding(10, 10, 10, 4);
            this.flowCards.Size          = new Size(870, 168);
            this.flowCards.TabIndex      = 1;
            this.flowCards.WrapContents  = false;
            this.flowCards.Resize += new System.EventHandler(this.FlowCards_Resize);

            // ── tarjetas KPI ───────────────────────────────────────────────────
            this.cardDisp.BackColor = Color.FromArgb(215, 240, 220);
            this.cardDisp.Controls.Add(this.numDisp);
            this.cardDisp.Controls.Add(this.txtDisp);
            this.cardDisp.Margin  = new Padding(0, 0, 8, 0);
            this.cardDisp.Name    = "cardDisp";
            this.cardDisp.Size    = new Size(148, 160);
            this.cardDisp.TabIndex = 0;
            this.cardDisp.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardDisp.Resize += new System.EventHandler(this.CardDisp_Resize);

            this.numDisp.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numDisp.BackColor = Color.Transparent;
            this.numDisp.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numDisp.ForeColor = Color.FromArgb(15, 85, 35);
            this.numDisp.Location  = new Point(0, 20);
            this.numDisp.Name      = "numDisp";
            this.numDisp.Size      = new Size(148, 78);
            this.numDisp.TabIndex  = 0;
            this.numDisp.Text      = "…";
            this.numDisp.TextAlign = ContentAlignment.BottomCenter;

            this.txtDisp.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtDisp.BackColor = Color.Transparent;
            this.txtDisp.Font      = new Font("Segoe UI", 8F);
            this.txtDisp.ForeColor = Color.FromArgb(65, 135, 85);
            this.txtDisp.Location  = new Point(0, 102);
            this.txtDisp.Name      = "txtDisp";
            this.txtDisp.Size      = new Size(148, 44);
            this.txtDisp.TabIndex  = 1;
            this.txtDisp.TextAlign = ContentAlignment.TopCenter;

            this.cardMant.BackColor = Color.FromArgb(215, 240, 220);
            this.cardMant.Controls.Add(this.numMant);
            this.cardMant.Controls.Add(this.txtMant);
            this.cardMant.Margin  = new Padding(0, 0, 8, 0);
            this.cardMant.Name    = "cardMant";
            this.cardMant.Size    = new Size(148, 160);
            this.cardMant.TabIndex = 1;
            this.cardMant.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardMant.Resize += new System.EventHandler(this.CardMant_Resize);

            this.numMant.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numMant.BackColor = Color.Transparent;
            this.numMant.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numMant.ForeColor = Color.FromArgb(15, 85, 35);
            this.numMant.Location  = new Point(0, 20);
            this.numMant.Name      = "numMant";
            this.numMant.Size      = new Size(148, 78);
            this.numMant.TabIndex  = 0;
            this.numMant.Text      = "…";
            this.numMant.TextAlign = ContentAlignment.BottomCenter;

            this.txtMant.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtMant.BackColor = Color.Transparent;
            this.txtMant.Font      = new Font("Segoe UI", 8F);
            this.txtMant.ForeColor = Color.FromArgb(65, 135, 85);
            this.txtMant.Location  = new Point(0, 102);
            this.txtMant.Name      = "txtMant";
            this.txtMant.Size      = new Size(148, 44);
            this.txtMant.TabIndex  = 1;
            this.txtMant.TextAlign = ContentAlignment.TopCenter;

            this.cardOcup.BackColor = Color.FromArgb(215, 240, 220);
            this.cardOcup.Controls.Add(this.numOcup);
            this.cardOcup.Controls.Add(this.txtOcup);
            this.cardOcup.Margin  = new Padding(0, 0, 8, 0);
            this.cardOcup.Name    = "cardOcup";
            this.cardOcup.Size    = new Size(148, 160);
            this.cardOcup.TabIndex = 2;
            this.cardOcup.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardOcup.Resize += new System.EventHandler(this.CardOcup_Resize);

            this.numOcup.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numOcup.BackColor = Color.Transparent;
            this.numOcup.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numOcup.ForeColor = Color.FromArgb(15, 85, 35);
            this.numOcup.Location  = new Point(0, 20);
            this.numOcup.Name      = "numOcup";
            this.numOcup.Size      = new Size(148, 78);
            this.numOcup.TabIndex  = 0;
            this.numOcup.Text      = "…";
            this.numOcup.TextAlign = ContentAlignment.BottomCenter;

            this.txtOcup.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtOcup.BackColor = Color.Transparent;
            this.txtOcup.Font      = new Font("Segoe UI", 8F);
            this.txtOcup.ForeColor = Color.FromArgb(65, 135, 85);
            this.txtOcup.Location  = new Point(0, 102);
            this.txtOcup.Name      = "txtOcup";
            this.txtOcup.Size      = new Size(148, 44);
            this.txtOcup.TabIndex  = 1;
            this.txtOcup.TextAlign = ContentAlignment.TopCenter;

            // ── wrapper / tbl / columnas ───────────────────────────────────────
            this.wrapper.BackColor = Color.FromArgb(240, 240, 245);
            this.wrapper.Controls.Add(this.tbl);
            this.wrapper.Dock     = DockStyle.Fill;
            this.wrapper.Location = new Point(0, 230);
            this.wrapper.Name     = "wrapper";
            this.wrapper.Padding  = new Padding(6);
            this.wrapper.Size     = new Size(870, 314);
            this.wrapper.TabIndex = 2;

            this.tbl.ColumnCount = 3;
            this.tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            this.tbl.Controls.Add(this.col1, 0, 0);
            this.tbl.Controls.Add(this.col2, 1, 0);
            this.tbl.Controls.Add(this.col3, 2, 0);
            this.tbl.Dock     = DockStyle.Fill;
            this.tbl.Location = new Point(6, 6);
            this.tbl.Name     = "tbl";
            this.tbl.RowCount = 1;
            this.tbl.Size     = new Size(858, 302);
            this.tbl.TabIndex = 0;

            this.col1.Controls.Add(this.colReciente);
            this.col1.Controls.Add(this.lblColRec);
            this.col1.Dock     = DockStyle.Fill;
            this.col1.Location = new Point(3, 3);
            this.col1.Margin   = new Padding(0, 0, 3, 0);
            this.col1.Name     = "col1";
            this.col1.Size     = new Size(283, 296);
            this.col1.TabIndex = 0;

            this.colReciente.AutoScroll     = true;
            this.colReciente.BackColor      = Color.FromArgb(244, 252, 246);
            this.colReciente.Dock           = DockStyle.Fill;
            this.colReciente.FlowDirection  = FlowDirection.TopDown;
            this.colReciente.Location       = new Point(0, 30);
            this.colReciente.Name           = "colReciente";
            this.colReciente.Padding        = new Padding(6);
            this.colReciente.Size           = new Size(283, 266);
            this.colReciente.TabIndex       = 1;
            this.colReciente.WrapContents   = false;
            this.colReciente.Resize += new System.EventHandler(this.ColReciente_Resize);

            this.lblColRec.BackColor = Color.FromArgb(215, 240, 220);
            this.lblColRec.Dock      = DockStyle.Top;
            this.lblColRec.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColRec.ForeColor = Color.FromArgb(15, 85, 35);
            this.lblColRec.Height    = 30;
            this.lblColRec.Location  = new Point(0, 0);
            this.lblColRec.Name      = "lblColRec";
            this.lblColRec.Padding   = new Padding(8, 6, 0, 0);
            this.lblColRec.Size      = new Size(283, 30);
            this.lblColRec.TabIndex  = 0;
            this.lblColRec.Text      = "Reciente (< 2d)";

            this.col2.Controls.Add(this.colEnCurso);
            this.col2.Controls.Add(this.lblColCur);
            this.col2.Dock     = DockStyle.Fill;
            this.col2.Location = new Point(289, 3);
            this.col2.Margin   = new Padding(3, 0, 3, 0);
            this.col2.Name     = "col2";
            this.col2.Size     = new Size(280, 296);
            this.col2.TabIndex = 1;

            this.colEnCurso.AutoScroll    = true;
            this.colEnCurso.BackColor     = Color.FromArgb(252, 250, 240);
            this.colEnCurso.Dock          = DockStyle.Fill;
            this.colEnCurso.FlowDirection = FlowDirection.TopDown;
            this.colEnCurso.Location      = new Point(0, 30);
            this.colEnCurso.Name          = "colEnCurso";
            this.colEnCurso.Padding       = new Padding(6);
            this.colEnCurso.Size          = new Size(280, 266);
            this.colEnCurso.TabIndex      = 1;
            this.colEnCurso.WrapContents  = false;
            this.colEnCurso.Resize += new System.EventHandler(this.ColEnCurso_Resize);

            this.lblColCur.BackColor = Color.FromArgb(255, 248, 210);
            this.lblColCur.Dock      = DockStyle.Top;
            this.lblColCur.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColCur.ForeColor = Color.FromArgb(120, 90, 0);
            this.lblColCur.Height    = 30;
            this.lblColCur.Location  = new Point(0, 0);
            this.lblColCur.Name      = "lblColCur";
            this.lblColCur.Padding   = new Padding(8, 6, 0, 0);
            this.lblColCur.Size      = new Size(280, 30);
            this.lblColCur.TabIndex  = 0;
            this.lblColCur.Text      = "En curso (2-7d)";

            this.col3.Controls.Add(this.colUrgente);
            this.col3.Controls.Add(this.lblColUrg);
            this.col3.Dock     = DockStyle.Fill;
            this.col3.Location = new Point(575, 3);
            this.col3.Margin   = new Padding(3, 0, 0, 0);
            this.col3.Name     = "col3";
            this.col3.Size     = new Size(280, 296);
            this.col3.TabIndex = 2;

            this.colUrgente.AutoScroll    = true;
            this.colUrgente.BackColor     = Color.FromArgb(252, 244, 244);
            this.colUrgente.Dock          = DockStyle.Fill;
            this.colUrgente.FlowDirection = FlowDirection.TopDown;
            this.colUrgente.Location      = new Point(0, 30);
            this.colUrgente.Name          = "colUrgente";
            this.colUrgente.Padding       = new Padding(6);
            this.colUrgente.Size          = new Size(280, 266);
            this.colUrgente.TabIndex      = 1;
            this.colUrgente.WrapContents  = false;
            this.colUrgente.Resize += new System.EventHandler(this.ColUrgente_Resize);

            this.lblColUrg.BackColor = Color.FromArgb(255, 218, 218);
            this.lblColUrg.Dock      = DockStyle.Top;
            this.lblColUrg.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColUrg.ForeColor = Color.FromArgb(160, 20, 20);
            this.lblColUrg.Height    = 30;
            this.lblColUrg.Location  = new Point(0, 0);
            this.lblColUrg.Name      = "lblColUrg";
            this.lblColUrg.Padding   = new Padding(8, 6, 0, 0);
            this.lblColUrg.Size      = new Size(280, 30);
            this.lblColUrg.TabIndex  = 0;
            this.lblColUrg.Text      = "Urgente (> 7d)";

            // ── DashboardControlStock ──────────────────────────────────────────
            this.BackColor       = Color.FromArgb(240, 240, 245);
            this.Controls.Add(this.wrapper);
            this.Controls.Add(this.flowCards);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelSbar);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Location        = new Point(10, 10);
            this.MinimumSize     = new Size(600, 400);
            this.Name            = "DashboardControlStock";
            this.Size            = new Size(870, 570);
            this.StartPosition   = FormStartPosition.Manual;
            this.Text             = "Panel de Stock";

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelSbar.ResumeLayout(false);
            this.flowCards.ResumeLayout(false);
            this.cardDisp.ResumeLayout(false);
            this.cardMant.ResumeLayout(false);
            this.cardOcup.ResumeLayout(false);
            this.wrapper.ResumeLayout(false);
            this.tbl.ResumeLayout(false);
            this.col1.ResumeLayout(false);
            this.col2.ResumeLayout(false);
            this.col3.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private Panel    panelHeader;
        private Label    lblTitulo;
        private Label    lblSub;
        private Button   btnRefrescar;
        private Panel    panelSbar;
        private Label    lblSesion;
        private FlowLayoutPanel flowCards;
        private Panel    cardDisp;
        private Label    numDisp;
        private Label    txtDisp;
        private Panel    cardMant;
        private Label    numMant;
        private Label    txtMant;
        private Panel    cardOcup;
        private Label    numOcup;
        private Label    txtOcup;
        private Panel    wrapper;
        private TableLayoutPanel tbl;
        private Panel    col1;
        private FlowLayoutPanel colReciente;
        private Label    lblColRec;
        private Panel    col2;
        private FlowLayoutPanel colEnCurso;
        private Label    lblColCur;
        private Panel    col3;
        private FlowLayoutPanel colUrgente;
        private Label    lblColUrg;
    }
}
