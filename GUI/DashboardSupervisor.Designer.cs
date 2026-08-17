using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class DashboardSupervisor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        // Nota: las tarjetas de las 3 columnas (pedidos, mantenimiento, bitácora) y el cartel
        // "— sin elementos —" se arman en runtime en ActualizarKanban() porque su cantidad y
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
            this.cardPrendas  = new Panel();
            this.numPrendas   = new Label();
            this.txtPrendas   = new Label();
            this.cardClientes = new Panel();
            this.numClientes  = new Label();
            this.txtClientes  = new Label();
            this.cardPedidos  = new Panel();
            this.numPedidos   = new Label();
            this.txtPedidos   = new Label();
            this.wrapper      = new Panel();
            this.tbl          = new TableLayoutPanel();
            this.col1          = new Panel();
            this.colPedidos    = new FlowLayoutPanel();
            this.lblColPed     = new Label();
            this.col2           = new Panel();
            this.colMant        = new FlowLayoutPanel();
            this.lblColMant     = new Label();
            this.col3            = new Panel();
            this.colBitacora     = new FlowLayoutPanel();
            this.lblColBit       = new Label();
            this.panelHeader.SuspendLayout();
            this.panelSbar.SuspendLayout();
            this.flowCards.SuspendLayout();
            this.cardPrendas.SuspendLayout();
            this.cardClientes.SuspendLayout();
            this.cardPedidos.SuspendLayout();
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
            this.lblTitulo.Text      = "Panel de Supervisor";

            this.lblSub.AutoSize  = true;
            this.lblSub.BackColor = Color.Transparent;
            this.lblSub.Font      = new Font("Segoe UI", 8F, FontStyle.Italic);
            this.lblSub.ForeColor = Color.FromArgb(200, 255, 200, 220);
            this.lblSub.Location  = new Point(14, 36);
            this.lblSub.Name      = "lblSub";
            this.lblSub.TabIndex  = 1;
            this.lblSub.Text      = "WardrobeFlow  —  Supervisión";

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
            this.flowCards.Controls.Add(this.cardPrendas);
            this.flowCards.Controls.Add(this.cardClientes);
            this.flowCards.Controls.Add(this.cardPedidos);
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
            this.cardPrendas.BackColor = Color.FromArgb(215, 240, 220);
            this.cardPrendas.Controls.Add(this.numPrendas);
            this.cardPrendas.Controls.Add(this.txtPrendas);
            this.cardPrendas.Margin  = new Padding(0, 0, 8, 0);
            this.cardPrendas.Name    = "cardPrendas";
            this.cardPrendas.Size    = new Size(148, 160);
            this.cardPrendas.TabIndex = 0;
            this.cardPrendas.Paint  += new PaintEventHandler(this.TarjetaKpi_Paint);
            this.cardPrendas.Resize += new System.EventHandler(this.CardPrendas_Resize);

            this.numPrendas.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.numPrendas.BackColor = Color.Transparent;
            this.numPrendas.Font      = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.numPrendas.ForeColor = Color.FromArgb(15, 85, 35);
            this.numPrendas.Location  = new Point(0, 20);
            this.numPrendas.Name      = "numPrendas";
            this.numPrendas.Size      = new Size(148, 78);
            this.numPrendas.TabIndex  = 0;
            this.numPrendas.Text      = "…";
            this.numPrendas.TextAlign = ContentAlignment.BottomCenter;

            this.txtPrendas.Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtPrendas.BackColor = Color.Transparent;
            this.txtPrendas.Font      = new Font("Segoe UI", 8F);
            this.txtPrendas.ForeColor = Color.FromArgb(65, 135, 85);
            this.txtPrendas.Location  = new Point(0, 102);
            this.txtPrendas.Name      = "txtPrendas";
            this.txtPrendas.Size      = new Size(148, 44);
            this.txtPrendas.TabIndex  = 1;
            this.txtPrendas.TextAlign = ContentAlignment.TopCenter;

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

            this.cardPedidos.BackColor = Color.FromArgb(252, 228, 235);
            this.cardPedidos.Controls.Add(this.numPedidos);
            this.cardPedidos.Controls.Add(this.txtPedidos);
            this.cardPedidos.Margin  = new Padding(0, 0, 8, 0);
            this.cardPedidos.Name    = "cardPedidos";
            this.cardPedidos.Size    = new Size(148, 160);
            this.cardPedidos.TabIndex = 2;
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

            this.col1.Controls.Add(this.colPedidos);
            this.col1.Controls.Add(this.lblColPed);
            this.col1.Dock     = DockStyle.Fill;
            this.col1.Location = new Point(3, 3);
            this.col1.Margin   = new Padding(0, 0, 3, 0);
            this.col1.Name     = "col1";
            this.col1.Size     = new Size(283, 296);
            this.col1.TabIndex = 0;

            this.colPedidos.AutoScroll     = true;
            this.colPedidos.BackColor      = Color.FromArgb(250, 244, 248);
            this.colPedidos.Dock           = DockStyle.Fill;
            this.colPedidos.FlowDirection  = FlowDirection.TopDown;
            this.colPedidos.Location       = new Point(0, 30);
            this.colPedidos.Name           = "colPedidos";
            this.colPedidos.Padding        = new Padding(6);
            this.colPedidos.Size           = new Size(283, 266);
            this.colPedidos.TabIndex       = 1;
            this.colPedidos.WrapContents   = false;
            this.colPedidos.Resize += new System.EventHandler(this.ColPedidos_Resize);

            this.lblColPed.BackColor = Color.FromArgb(252, 228, 235);
            this.lblColPed.Dock      = DockStyle.Top;
            this.lblColPed.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColPed.ForeColor = Color.FromArgb(176, 62, 96);
            this.lblColPed.Height    = 30;
            this.lblColPed.Location  = new Point(0, 0);
            this.lblColPed.Name      = "lblColPed";
            this.lblColPed.Padding   = new Padding(8, 6, 0, 0);
            this.lblColPed.Size      = new Size(283, 30);
            this.lblColPed.TabIndex  = 0;
            this.lblColPed.Text      = "Pedidos pendientes";

            this.col2.Controls.Add(this.colMant);
            this.col2.Controls.Add(this.lblColMant);
            this.col2.Dock     = DockStyle.Fill;
            this.col2.Location = new Point(289, 3);
            this.col2.Margin   = new Padding(3, 0, 3, 0);
            this.col2.Name     = "col2";
            this.col2.Size     = new Size(280, 296);
            this.col2.TabIndex = 1;

            this.colMant.AutoScroll    = true;
            this.colMant.BackColor     = Color.FromArgb(252, 250, 240);
            this.colMant.Dock          = DockStyle.Fill;
            this.colMant.FlowDirection = FlowDirection.TopDown;
            this.colMant.Location      = new Point(0, 30);
            this.colMant.Name          = "colMant";
            this.colMant.Padding       = new Padding(6);
            this.colMant.Size          = new Size(280, 266);
            this.colMant.TabIndex      = 1;
            this.colMant.WrapContents  = false;
            this.colMant.Resize += new System.EventHandler(this.ColMant_Resize);

            this.lblColMant.BackColor = Color.FromArgb(255, 248, 210);
            this.lblColMant.Dock      = DockStyle.Top;
            this.lblColMant.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColMant.ForeColor = Color.FromArgb(120, 80, 0);
            this.lblColMant.Height    = 30;
            this.lblColMant.Location  = new Point(0, 0);
            this.lblColMant.Name      = "lblColMant";
            this.lblColMant.Padding   = new Padding(8, 6, 0, 0);
            this.lblColMant.Size      = new Size(280, 30);
            this.lblColMant.TabIndex  = 0;
            this.lblColMant.Text      = "En mantenimiento";

            this.col3.Controls.Add(this.colBitacora);
            this.col3.Controls.Add(this.lblColBit);
            this.col3.Dock     = DockStyle.Fill;
            this.col3.Location = new Point(575, 3);
            this.col3.Margin   = new Padding(3, 0, 0, 0);
            this.col3.Name     = "col3";
            this.col3.Size     = new Size(280, 296);
            this.col3.TabIndex = 2;

            this.colBitacora.AutoScroll    = true;
            this.colBitacora.BackColor     = Color.FromArgb(244, 242, 252);
            this.colBitacora.Dock          = DockStyle.Fill;
            this.colBitacora.FlowDirection = FlowDirection.TopDown;
            this.colBitacora.Location      = new Point(0, 30);
            this.colBitacora.Name          = "colBitacora";
            this.colBitacora.Padding       = new Padding(6);
            this.colBitacora.Size          = new Size(280, 266);
            this.colBitacora.TabIndex      = 1;
            this.colBitacora.WrapContents  = false;
            this.colBitacora.Resize += new System.EventHandler(this.ColBitacora_Resize);

            this.lblColBit.BackColor = Color.FromArgb(230, 225, 248);
            this.lblColBit.Dock      = DockStyle.Top;
            this.lblColBit.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblColBit.ForeColor = Color.FromArgb(100, 80, 160);
            this.lblColBit.Height    = 30;
            this.lblColBit.Location  = new Point(0, 0);
            this.lblColBit.Name      = "lblColBit";
            this.lblColBit.Padding   = new Padding(8, 6, 0, 0);
            this.lblColBit.Size      = new Size(280, 30);
            this.lblColBit.TabIndex  = 0;
            this.lblColBit.Text      = "Actividad reciente";

            // ── DashboardSupervisor ────────────────────────────────────────────
            this.BackColor       = Color.FromArgb(240, 240, 245);
            this.Controls.Add(this.wrapper);
            this.Controls.Add(this.flowCards);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelSbar);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Location        = new Point(10, 10);
            this.MinimumSize     = new Size(600, 400);
            this.Name            = "DashboardSupervisor";
            this.Size            = new Size(870, 570);
            this.StartPosition   = FormStartPosition.Manual;
            this.Text             = "Panel de Supervisor";

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelSbar.ResumeLayout(false);
            this.flowCards.ResumeLayout(false);
            this.cardPrendas.ResumeLayout(false);
            this.cardClientes.ResumeLayout(false);
            this.cardPedidos.ResumeLayout(false);
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
        private Panel    cardPrendas;
        private Label    numPrendas;
        private Label    txtPrendas;
        private Panel    cardClientes;
        private Label    numClientes;
        private Label    txtClientes;
        private Panel    cardPedidos;
        private Label    numPedidos;
        private Label    txtPedidos;
        private Panel    wrapper;
        private TableLayoutPanel tbl;
        private Panel    col1;
        private FlowLayoutPanel colPedidos;
        private Label    lblColPed;
        private Panel    col2;
        private FlowLayoutPanel colMant;
        private Label    lblColMant;
        private Panel    col3;
        private FlowLayoutPanel colBitacora;
        private Label    lblColBit;
    }
}
