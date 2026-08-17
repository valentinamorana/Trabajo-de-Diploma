using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class DashboardOperador
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        // Nota: las tarjetas del Kanban (una por pedido) y el cartel "— sin tareas —" se arman
        // en runtime en ActualizarKanban() porque su cantidad y contenido dependen de los
        // pedidos cargados desde la BLL — no son representables como controles fijos del
        // Diseñador. El resto del formulario es estático y vive acá.
        private void InitializeComponent()
        {
            this.panelHeader   = new Panel();
            this.lblTitulo     = new Label();
            this.lblSub        = new Label();
            this.btnRefrescar  = new Button();
            this.panelSbar     = new Panel();
            this.lblSesion     = new Label();
            this.flowCards     = new FlowLayoutPanel();
            this.cardPend      = new Panel();
            this.numPend       = new Label();
            this.txtPend       = new Label();
            this.cardDesp      = new Panel();
            this.numDesp       = new Label();
            this.txtDesp       = new Label();
            this.cardEntr      = new Panel();
            this.numEntr       = new Label();
            this.txtEntr       = new Label();
            this.wrapper       = new Panel();
            this.tbl           = new TableLayoutPanel();
            this.col1           = new Panel();
            this.colPendiente   = new FlowLayoutPanel();
            this.lblColPend     = new Label();
            this.col2            = new Panel();
            this.colDespachado   = new FlowLayoutPanel();
            this.lblColDesp      = new Label();
            this.col3             = new Panel();
            this.colEntregado     = new FlowLayoutPanel();
            this.lblColEntr       = new Label();
            this.panelHeader.SuspendLayout();
            this.panelSbar.SuspendLayout();
            this.flowCards.SuspendLayout();
            this.cardPend.SuspendLayout();
            this.cardDesp.SuspendLayout();
            this.cardEntr.SuspendLayout();
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
            this.lblTitulo.Text      = "Panel de Operaciones";

            this.lblSub.AutoSize  = true;
            this.lblSub.BackColor = Color.Transparent;
            this.lblSub.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
            this.lblSub.ForeColor = Color.FromArgb(200, 255, 200, 220);
            this.lblSub.Location  = new Point(14, 36);
            this.lblSub.Name      = "lblSub";
            this.lblSub.TabIndex  = 1;
            this.lblSub.Text      = "WardrobeFlow  —  Operaciones";

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
            this.flowCards.Controls.Add(this.cardPend);
            this.flowCards.Controls.Add(this.cardDesp);
            this.flowCards.Controls.Add(this.cardEntr);
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
            this.cardPend.BackColor = Color.FromArgb(255, 242, 200);
            this.cardPend.Controls.Add(this.numPend);
            this.cardPend.Controls.Add(this.txtPend);
            this.cardPend.Margin  = new Padding(0, 0, 8, 0);
            this.cardPend.Name    = "cardPend";
            this.cardPend.Size    = new Size(148, 160);
            this.cardPend.TabIndex = 0;
            this.cardPend.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardPend.Resize += new System.EventHandler(this.CardPend_Resize);

            this.numPend.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numPend.BackColor = Color.Transparent;
            this.numPend.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numPend.ForeColor = Color.FromArgb(120, 80, 0);
            this.numPend.Location  = new Point(0, 20);
            this.numPend.Name      = "numPend";
            this.numPend.Size      = new Size(148, 78);
            this.numPend.TabIndex  = 0;
            this.numPend.Text      = "…";
            this.numPend.TextAlign = ContentAlignment.BottomCenter;

            this.txtPend.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtPend.BackColor = Color.Transparent;
            this.txtPend.Font      = new Font("Segoe UI", 8F);
            this.txtPend.ForeColor = Color.FromArgb(170, 130, 50);
            this.txtPend.Location  = new Point(0, 102);
            this.txtPend.Name      = "txtPend";
            this.txtPend.Size      = new Size(148, 44);
            this.txtPend.TabIndex  = 1;
            this.txtPend.TextAlign = ContentAlignment.TopCenter;

            this.cardDesp.BackColor = Color.FromArgb(205, 225, 255);
            this.cardDesp.Controls.Add(this.numDesp);
            this.cardDesp.Controls.Add(this.txtDesp);
            this.cardDesp.Margin  = new Padding(0, 0, 8, 0);
            this.cardDesp.Name    = "cardDesp";
            this.cardDesp.Size    = new Size(148, 160);
            this.cardDesp.TabIndex = 1;
            this.cardDesp.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardDesp.Resize += new System.EventHandler(this.CardDesp_Resize);

            this.numDesp.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numDesp.BackColor = Color.Transparent;
            this.numDesp.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numDesp.ForeColor = Color.FromArgb(20, 60, 130);
            this.numDesp.Location  = new Point(0, 20);
            this.numDesp.Name      = "numDesp";
            this.numDesp.Size      = new Size(148, 78);
            this.numDesp.TabIndex  = 0;
            this.numDesp.Text      = "…";
            this.numDesp.TextAlign = ContentAlignment.BottomCenter;

            this.txtDesp.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtDesp.BackColor = Color.Transparent;
            this.txtDesp.Font      = new Font("Segoe UI", 8F);
            this.txtDesp.ForeColor = Color.FromArgb(70, 110, 180);
            this.txtDesp.Location  = new Point(0, 102);
            this.txtDesp.Name      = "txtDesp";
            this.txtDesp.Size      = new Size(148, 44);
            this.txtDesp.TabIndex  = 1;
            this.txtDesp.TextAlign = ContentAlignment.TopCenter;

            this.cardEntr.BackColor = Color.FromArgb(210, 240, 220);
            this.cardEntr.Controls.Add(this.numEntr);
            this.cardEntr.Controls.Add(this.txtEntr);
            this.cardEntr.Margin  = new Padding(0, 0, 8, 0);
            this.cardEntr.Name    = "cardEntr";
            this.cardEntr.Size    = new Size(148, 160);
            this.cardEntr.TabIndex = 2;
            this.cardEntr.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardEntr.Resize += new System.EventHandler(this.CardEntr_Resize);

            this.numEntr.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numEntr.BackColor = Color.Transparent;
            this.numEntr.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numEntr.ForeColor = Color.FromArgb(15, 85, 35);
            this.numEntr.Location  = new Point(0, 20);
            this.numEntr.Name      = "numEntr";
            this.numEntr.Size      = new Size(148, 78);
            this.numEntr.TabIndex  = 0;
            this.numEntr.Text      = "…";
            this.numEntr.TextAlign = ContentAlignment.BottomCenter;

            this.txtEntr.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtEntr.BackColor = Color.Transparent;
            this.txtEntr.Font      = new Font("Segoe UI", 8F);
            this.txtEntr.ForeColor = Color.FromArgb(65, 135, 85);
            this.txtEntr.Location  = new Point(0, 102);
            this.txtEntr.Name      = "txtEntr";
            this.txtEntr.Size      = new Size(148, 44);
            this.txtEntr.TabIndex  = 1;
            this.txtEntr.TextAlign = ContentAlignment.TopCenter;

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

            this.col1.Controls.Add(this.colPendiente);
            this.col1.Controls.Add(this.lblColPend);
            this.col1.Dock     = DockStyle.Fill;
            this.col1.Location = new Point(3, 3);
            this.col1.Margin   = new Padding(0, 0, 3, 0);
            this.col1.Name     = "col1";
            this.col1.Size     = new Size(283, 296);
            this.col1.TabIndex = 0;

            this.colPendiente.AutoScroll     = true;
            this.colPendiente.BackColor      = Color.FromArgb(252, 250, 242);
            this.colPendiente.Dock           = DockStyle.Fill;
            this.colPendiente.FlowDirection  = FlowDirection.TopDown;
            this.colPendiente.Location       = new Point(0, 30);
            this.colPendiente.Name           = "colPendiente";
            this.colPendiente.Padding        = new Padding(6);
            this.colPendiente.Size           = new Size(283, 266);
            this.colPendiente.TabIndex       = 1;
            this.colPendiente.WrapContents   = false;
            this.colPendiente.Resize += new System.EventHandler(this.ColPendiente_Resize);

            this.lblColPend.BackColor = Color.FromArgb(255, 248, 210);
            this.lblColPend.Dock      = DockStyle.Top;
            this.lblColPend.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColPend.ForeColor = Color.FromArgb(120, 80, 0);
            this.lblColPend.Height    = 30;
            this.lblColPend.Location  = new Point(0, 0);
            this.lblColPend.Name      = "lblColPend";
            this.lblColPend.Padding   = new Padding(8, 6, 0, 0);
            this.lblColPend.Size      = new Size(283, 30);
            this.lblColPend.TabIndex  = 0;
            this.lblColPend.Text      = "Pendiente";

            this.col2.Controls.Add(this.colDespachado);
            this.col2.Controls.Add(this.lblColDesp);
            this.col2.Dock     = DockStyle.Fill;
            this.col2.Location = new Point(289, 3);
            this.col2.Margin   = new Padding(3, 0, 3, 0);
            this.col2.Name     = "col2";
            this.col2.Size     = new Size(280, 296);
            this.col2.TabIndex = 1;

            this.colDespachado.AutoScroll    = true;
            this.colDespachado.BackColor     = Color.FromArgb(242, 246, 252);
            this.colDespachado.Dock          = DockStyle.Fill;
            this.colDespachado.FlowDirection = FlowDirection.TopDown;
            this.colDespachado.Location      = new Point(0, 30);
            this.colDespachado.Name          = "colDespachado";
            this.colDespachado.Padding       = new Padding(6);
            this.colDespachado.Size          = new Size(280, 266);
            this.colDespachado.TabIndex      = 1;
            this.colDespachado.WrapContents  = false;
            this.colDespachado.Resize += new System.EventHandler(this.ColDespachado_Resize);

            this.lblColDesp.BackColor = Color.FromArgb(220, 230, 252);
            this.lblColDesp.Dock      = DockStyle.Top;
            this.lblColDesp.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColDesp.ForeColor = Color.FromArgb(20, 60, 130);
            this.lblColDesp.Height    = 30;
            this.lblColDesp.Location  = new Point(0, 0);
            this.lblColDesp.Name      = "lblColDesp";
            this.lblColDesp.Padding   = new Padding(8, 6, 0, 0);
            this.lblColDesp.Size      = new Size(280, 30);
            this.lblColDesp.TabIndex  = 0;
            this.lblColDesp.Text      = "Despachado";

            this.col3.Controls.Add(this.colEntregado);
            this.col3.Controls.Add(this.lblColEntr);
            this.col3.Dock     = DockStyle.Fill;
            this.col3.Location = new Point(575, 3);
            this.col3.Margin   = new Padding(3, 0, 0, 0);
            this.col3.Name     = "col3";
            this.col3.Size     = new Size(280, 296);
            this.col3.TabIndex = 2;

            this.colEntregado.AutoScroll    = true;
            this.colEntregado.BackColor     = Color.FromArgb(244, 252, 246);
            this.colEntregado.Dock          = DockStyle.Fill;
            this.colEntregado.FlowDirection = FlowDirection.TopDown;
            this.colEntregado.Location      = new Point(0, 30);
            this.colEntregado.Name          = "colEntregado";
            this.colEntregado.Padding       = new Padding(6);
            this.colEntregado.Size          = new Size(280, 266);
            this.colEntregado.TabIndex      = 1;
            this.colEntregado.WrapContents  = false;
            this.colEntregado.Resize += new System.EventHandler(this.ColEntregado_Resize);

            this.lblColEntr.BackColor = Color.FromArgb(215, 240, 220);
            this.lblColEntr.Dock      = DockStyle.Top;
            this.lblColEntr.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColEntr.ForeColor = Color.FromArgb(15, 85, 35);
            this.lblColEntr.Height    = 30;
            this.lblColEntr.Location  = new Point(0, 0);
            this.lblColEntr.Name      = "lblColEntr";
            this.lblColEntr.Padding   = new Padding(8, 6, 0, 0);
            this.lblColEntr.Size      = new Size(280, 30);
            this.lblColEntr.TabIndex  = 0;
            this.lblColEntr.Text      = "Entregado";

            // ── DashboardOperador ──────────────────────────────────────────────
            this.BackColor       = Color.FromArgb(240, 240, 245);
            this.Controls.Add(this.wrapper);
            this.Controls.Add(this.flowCards);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelSbar);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Location        = new Point(10, 10);
            this.MinimumSize     = new Size(600, 400);
            this.Name            = "DashboardOperador";
            this.Size            = new Size(870, 570);
            this.StartPosition   = FormStartPosition.Manual;
            this.Text             = "Panel de Operaciones";

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelSbar.ResumeLayout(false);
            this.flowCards.ResumeLayout(false);
            this.cardPend.ResumeLayout(false);
            this.cardDesp.ResumeLayout(false);
            this.cardEntr.ResumeLayout(false);
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
        private Panel    cardPend;
        private Label    numPend;
        private Label    txtPend;
        private Panel    cardDesp;
        private Label    numDesp;
        private Label    txtDesp;
        private Panel    cardEntr;
        private Label    numEntr;
        private Label    txtEntr;
        private Panel    wrapper;
        private TableLayoutPanel tbl;
        private Panel    col1;
        private FlowLayoutPanel colPendiente;
        private Label    lblColPend;
        private Panel    col2;
        private FlowLayoutPanel colDespachado;
        private Label    lblColDesp;
        private Panel    col3;
        private FlowLayoutPanel colEntregado;
        private Label    lblColEntr;
    }
}
