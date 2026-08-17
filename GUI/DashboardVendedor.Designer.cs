using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class DashboardVendedor
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
        // Diseñador. El resto del formulario (header, footer, tarjetas KPI, columnas del
        // Kanban) es estático y vive acá.
        private void InitializeComponent()
        {
            this.panelHeader       = new Panel();
            this.lblTitulo         = new Label();
            this.lblSub            = new Label();
            this.btnRefrescar      = new Button();
            this.panelSbar         = new Panel();
            this.lblSesion         = new Label();
            this.flowCards         = new FlowLayoutPanel();
            this.cardPedidos       = new Panel();
            this.numPedidos        = new Label();
            this.txtPedidos        = new Label();
            this.cardClientes      = new Panel();
            this.numClientes       = new Label();
            this.txtClientes       = new Label();
            this.cardPlanes        = new Panel();
            this.numPlanes         = new Label();
            this.txtPlanes         = new Label();
            this.cardSuscripciones = new Panel();
            this.numSuscripciones  = new Label();
            this.txtSuscripciones  = new Label();
            this.kanbanWrapper     = new Panel();
            this.tbl               = new TableLayoutPanel();
            this.col1               = new Panel();
            this.colPendiente       = new FlowLayoutPanel();
            this.lblColPend         = new Label();
            this.col2                = new Panel();
            this.colDespachado       = new FlowLayoutPanel();
            this.lblColDesp          = new Label();
            this.col3                = new Panel();
            this.colEntregado        = new FlowLayoutPanel();
            this.lblColEntr          = new Label();
            this.panelHeader.SuspendLayout();
            this.panelSbar.SuspendLayout();
            this.flowCards.SuspendLayout();
            this.cardPedidos.SuspendLayout();
            this.cardClientes.SuspendLayout();
            this.cardPlanes.SuspendLayout();
            this.cardSuscripciones.SuspendLayout();
            this.kanbanWrapper.SuspendLayout();
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
            this.lblTitulo.Text      = "Panel de Ventas";

            this.lblSub.AutoSize  = true;
            this.lblSub.BackColor = Color.Transparent;
            this.lblSub.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
            this.lblSub.ForeColor = Color.FromArgb(200, 255, 200, 220);
            this.lblSub.Location  = new Point(14, 36);
            this.lblSub.Name      = "lblSub";
            this.lblSub.TabIndex  = 1;
            this.lblSub.Text      = "WardrobeFlow  —  Ventas";

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
            this.flowCards.Controls.Add(this.cardPedidos);
            this.flowCards.Controls.Add(this.cardClientes);
            this.flowCards.Controls.Add(this.cardPlanes);
            this.flowCards.Controls.Add(this.cardSuscripciones);
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
            this.cardPedidos.BackColor = Color.FromArgb(252, 228, 235);
            this.cardPedidos.Controls.Add(this.numPedidos);
            this.cardPedidos.Controls.Add(this.txtPedidos);
            this.cardPedidos.Margin  = new Padding(0, 0, 8, 0);
            this.cardPedidos.Name    = "cardPedidos";
            this.cardPedidos.Size    = new Size(148, 160);
            this.cardPedidos.TabIndex = 0;
            this.cardPedidos.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardPedidos.Resize += new System.EventHandler(this.CardPedidos_Resize);

            this.numPedidos.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numPedidos.BackColor = Color.Transparent;
            this.numPedidos.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numPedidos.ForeColor = Color.FromArgb(80, 28, 52);
            this.numPedidos.Location  = new Point(0, 20);
            this.numPedidos.Name      = "numPedidos";
            this.numPedidos.Size      = new Size(148, 78);
            this.numPedidos.TabIndex  = 0;
            this.numPedidos.Text      = "…";
            this.numPedidos.TextAlign = ContentAlignment.BottomCenter;

            this.txtPedidos.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtPedidos.BackColor = Color.Transparent;
            this.txtPedidos.Font      = new Font("Segoe UI", 8F);
            this.txtPedidos.ForeColor = Color.FromArgb(130, 78, 102);
            this.txtPedidos.Location  = new Point(0, 102);
            this.txtPedidos.Name      = "txtPedidos";
            this.txtPedidos.Size      = new Size(148, 44);
            this.txtPedidos.TabIndex  = 1;
            this.txtPedidos.TextAlign = ContentAlignment.TopCenter;

            this.cardClientes.BackColor = Color.FromArgb(244, 212, 226);
            this.cardClientes.Controls.Add(this.numClientes);
            this.cardClientes.Controls.Add(this.txtClientes);
            this.cardClientes.Margin  = new Padding(0, 0, 8, 0);
            this.cardClientes.Name    = "cardClientes";
            this.cardClientes.Size    = new Size(148, 160);
            this.cardClientes.TabIndex = 1;
            this.cardClientes.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardClientes.Resize += new System.EventHandler(this.CardClientes_Resize);

            this.numClientes.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numClientes.BackColor = Color.Transparent;
            this.numClientes.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numClientes.ForeColor = Color.FromArgb(110, 42, 74);
            this.numClientes.Location  = new Point(0, 20);
            this.numClientes.Name      = "numClientes";
            this.numClientes.Size      = new Size(148, 78);
            this.numClientes.TabIndex  = 0;
            this.numClientes.Text      = "…";
            this.numClientes.TextAlign = ContentAlignment.BottomCenter;

            this.txtClientes.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtClientes.BackColor = Color.Transparent;
            this.txtClientes.Font      = new Font("Segoe UI", 8F);
            this.txtClientes.ForeColor = Color.FromArgb(160, 92, 124);
            this.txtClientes.Location  = new Point(0, 102);
            this.txtClientes.Name      = "txtClientes";
            this.txtClientes.Size      = new Size(148, 44);
            this.txtClientes.TabIndex  = 1;
            this.txtClientes.TextAlign = ContentAlignment.TopCenter;

            this.cardPlanes.BackColor = Color.FromArgb(236, 196, 215);
            this.cardPlanes.Controls.Add(this.numPlanes);
            this.cardPlanes.Controls.Add(this.txtPlanes);
            this.cardPlanes.Margin  = new Padding(0, 0, 8, 0);
            this.cardPlanes.Name    = "cardPlanes";
            this.cardPlanes.Size    = new Size(148, 160);
            this.cardPlanes.TabIndex = 2;
            this.cardPlanes.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardPlanes.Resize += new System.EventHandler(this.CardPlanes_Resize);

            this.numPlanes.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numPlanes.BackColor = Color.Transparent;
            this.numPlanes.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numPlanes.ForeColor = Color.FromArgb(176, 62, 96);
            this.numPlanes.Location  = new Point(0, 20);
            this.numPlanes.Name      = "numPlanes";
            this.numPlanes.Size      = new Size(148, 78);
            this.numPlanes.TabIndex  = 0;
            this.numPlanes.Text      = "…";
            this.numPlanes.TextAlign = ContentAlignment.BottomCenter;

            this.txtPlanes.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtPlanes.BackColor = Color.Transparent;
            this.txtPlanes.Font      = new Font("Segoe UI", 8F);
            this.txtPlanes.ForeColor = Color.FromArgb(226, 112, 146);
            this.txtPlanes.Location  = new Point(0, 102);
            this.txtPlanes.Name      = "txtPlanes";
            this.txtPlanes.Size      = new Size(148, 44);
            this.txtPlanes.TabIndex  = 1;
            this.txtPlanes.TextAlign = ContentAlignment.TopCenter;

            this.cardSuscripciones.BackColor = Color.FromArgb(255, 235, 210);
            this.cardSuscripciones.Controls.Add(this.numSuscripciones);
            this.cardSuscripciones.Controls.Add(this.txtSuscripciones);
            this.cardSuscripciones.Margin  = new Padding(0, 0, 8, 0);
            this.cardSuscripciones.Name    = "cardSuscripciones";
            this.cardSuscripciones.Size    = new Size(148, 160);
            this.cardSuscripciones.TabIndex = 3;
            this.cardSuscripciones.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardSuscripciones.Resize += new System.EventHandler(this.CardSuscripciones_Resize);

            this.numSuscripciones.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numSuscripciones.BackColor = Color.Transparent;
            this.numSuscripciones.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numSuscripciones.ForeColor = Color.FromArgb(166, 101, 14);
            this.numSuscripciones.Location  = new Point(0, 20);
            this.numSuscripciones.Name      = "numSuscripciones";
            this.numSuscripciones.Size      = new Size(148, 78);
            this.numSuscripciones.TabIndex  = 0;
            this.numSuscripciones.Text      = "…";
            this.numSuscripciones.TextAlign = ContentAlignment.BottomCenter;

            this.txtSuscripciones.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtSuscripciones.BackColor = Color.Transparent;
            this.txtSuscripciones.Font      = new Font("Segoe UI", 8F);
            this.txtSuscripciones.ForeColor = Color.FromArgb(216, 151, 64);
            this.txtSuscripciones.Location  = new Point(0, 102);
            this.txtSuscripciones.Name      = "txtSuscripciones";
            this.txtSuscripciones.Size      = new Size(148, 44);
            this.txtSuscripciones.TabIndex  = 1;
            this.txtSuscripciones.TextAlign = ContentAlignment.TopCenter;

            // ── kanbanWrapper / tbl / columnas ────────────────────────────────
            this.kanbanWrapper.BackColor = Color.FromArgb(240, 240, 245);
            this.kanbanWrapper.Controls.Add(this.tbl);
            this.kanbanWrapper.Dock     = DockStyle.Fill;
            this.kanbanWrapper.Location = new Point(0, 230);
            this.kanbanWrapper.Name     = "kanbanWrapper";
            this.kanbanWrapper.Padding  = new Padding(6);
            this.kanbanWrapper.Size     = new Size(870, 314);
            this.kanbanWrapper.TabIndex = 2;

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
            this.colPendiente.BackColor      = Color.FromArgb(250, 244, 248);
            this.colPendiente.Dock           = DockStyle.Fill;
            this.colPendiente.FlowDirection  = FlowDirection.TopDown;
            this.colPendiente.Location       = new Point(0, 30);
            this.colPendiente.Name           = "colPendiente";
            this.colPendiente.Padding        = new Padding(6);
            this.colPendiente.Size           = new Size(283, 266);
            this.colPendiente.TabIndex       = 1;
            this.colPendiente.WrapContents   = false;
            this.colPendiente.Resize += new System.EventHandler(this.ColPendiente_Resize);

            this.lblColPend.BackColor = Color.FromArgb(252, 228, 235);
            this.lblColPend.Dock      = DockStyle.Top;
            this.lblColPend.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColPend.ForeColor = Color.FromArgb(176, 62, 96);
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
            this.colDespachado.BackColor     = Color.FromArgb(244, 246, 252);
            this.colDespachado.Dock          = DockStyle.Fill;
            this.colDespachado.FlowDirection = FlowDirection.TopDown;
            this.colDespachado.Location      = new Point(0, 30);
            this.colDespachado.Name          = "colDespachado";
            this.colDespachado.Padding       = new Padding(6);
            this.colDespachado.Size          = new Size(280, 266);
            this.colDespachado.TabIndex      = 1;
            this.colDespachado.WrapContents  = false;
            this.colDespachado.Resize += new System.EventHandler(this.ColDespachado_Resize);

            this.lblColDesp.BackColor = Color.FromArgb(220, 230, 250);
            this.lblColDesp.Dock      = DockStyle.Top;
            this.lblColDesp.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColDesp.ForeColor = Color.FromArgb(30, 100, 170);
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

            // ── DashboardVendedor ──────────────────────────────────────────────
            this.BackColor       = Color.FromArgb(240, 240, 245);
            this.Controls.Add(this.kanbanWrapper);
            this.Controls.Add(this.flowCards);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelSbar);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Location        = new Point(10, 10);
            this.MinimumSize     = new Size(600, 400);
            this.Name            = "DashboardVendedor";
            this.Size            = new Size(870, 570);
            this.StartPosition   = FormStartPosition.Manual;
            this.Text             = "Panel de Ventas";

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelSbar.ResumeLayout(false);
            this.flowCards.ResumeLayout(false);
            this.cardPedidos.ResumeLayout(false);
            this.cardClientes.ResumeLayout(false);
            this.cardPlanes.ResumeLayout(false);
            this.cardSuscripciones.ResumeLayout(false);
            this.kanbanWrapper.ResumeLayout(false);
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
        private Panel    cardPedidos;
        private Label    numPedidos;
        private Label    txtPedidos;
        private Panel    cardClientes;
        private Label    numClientes;
        private Label    txtClientes;
        private Panel    cardPlanes;
        private Label    numPlanes;
        private Label    txtPlanes;
        private Panel    cardSuscripciones;
        private Label    numSuscripciones;
        private Label    txtSuscripciones;
        private Panel    kanbanWrapper;
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
