using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    public partial class DashboardVendedor : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IPedidoService         _bllPedido  = new BLL.Pedido();
        private readonly BLL.Interfaces.IClienteService        _bllCliente = new BLL.Cliente();
        private readonly BLL.Interfaces.IPlanSuscripcionService _bllPlan    = new BLL.PlanSuscripcion();
        private readonly BLL.Usuario                            _bllUsuario = new BLL.Usuario();

        private System.Windows.Forms.Timer _timer;

        public DashboardVendedor()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new System.Drawing.Icon(ico); } catch { }
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
            CargarEnBackground();
            _timer = new System.Windows.Forms.Timer { Interval = 2 * 60 * 1000 };
            _timer.Tick += (s, ev) => CargarEnBackground();
            _timer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarEnBackground();
        }

        private string T(string k, string fb)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(k) ? t[k].Texto : fb;
        }

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string Tr(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text          = Tr("dash.vendedor.titulo", "Panel de Ventas");
            lblTitulo.Text     = Tr("dash.vendedor.titulo", "Panel de Ventas");
            btnRefrescar.Text  = Tr("dash.btn.refrescar",   "↻ Actualizar");
            txtPedidos.Text  = Tr("dash.pedidos",  "Pedidos\npendientes");
            txtClientes.Text = Tr("dash.clientes", "Clientes\nregistrados");
            txtPlanes.Text   = Tr("dash.planes",   "Planes\nactivos");
            txtSuscripciones.Text = Tr("dash.suscripciones", "Suscripciones\npor vencer");
            lblColPend.Text = Tr("dash.kan.pendiente",  "Pendiente");
            lblColDesp.Text = Tr("dash.kan.despachado", "Despachado");
            lblColEntr.Text = Tr("dash.kan.entregado",  "Entregado");
        }

        private void CargarEnBackground()
        {
            Task.Run(() =>
            {
                try
                {
                    var pedidos  = _bllPedido.ObtenerTodos();
                    var clientes = _bllCliente.ObtenerTodos();
                    var planes   = _bllPlan.ObtenerActivos();
                    this.BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        ActualizarCards(pedidos, clientes, planes);
                        ActualizarKanban(pedidos);
                        ActualizarSesion();
                    }));
                }
                catch { }
            });
        }

        private void ActualizarCards(List<BE.Pedido> pedidos, List<BE.Cliente> clientes, List<BE.PlanSuscripcion> planes)
        {
            numPedidos.Text  = pedidos.FindAll(p => p.Estado == BE.EstadoPedido.Pendiente).Count.ToString();
            numClientes.Text = clientes.Count.ToString();
            numPlanes.Text   = planes.Count.ToString();
            // Mismo criterio que BLL.PanelAlertas: vencida o vence en los próximos 7 días.
            numSuscripciones.Text = clientes
                .Count(c => c.VencimientoExpirado || c.SuscripcionProximaAVencer(7))
                .ToString();
        }

        private void ActualizarKanban(List<BE.Pedido> pedidos)
        {
            colPendiente.Controls.Clear();
            colDespachado.Controls.Clear();
            colEntregado.Controls.Clear();

            foreach (var p in pedidos)
            {
                int  dias  = p.DiasDesdeAlta;
                string tit = $"Pedido #{p.IdPedido}";
                string sub = p.NombreCliente ?? $"Cliente {p.IdCliente}";

                switch (p.Estado)
                {
                    case BE.EstadoPedido.Pendiente:
                        var cardPendiente = CrearCard(tit, sub, dias,
                            p.EsUrgentePorAntiguedad ? Color.FromArgb(255, 205, 200) : Color.FromArgb(255, 242, 200));
                        // Solo "Pendiente" es clickeable: abre Pedidos de Venta, permiso que el
                        // Vendedor siempre tiene (es el único rol que ve este dashboard).
                        // Despachado/Entregado viven en Pedidos Realizados — permiso que un
                        // Vendedor base NO tiene (solo lo hereda GerenteComercial) — habilitar
                        // el clic ahí sería un bypass de permisos, así que quedan solo informativas.
                        HabilitarClicAbrirPedidosVenta(cardPendiente);
                        colPendiente.Controls.Add(cardPendiente);
                        break;
                    case BE.EstadoPedido.Despachado:
                        colDespachado.Controls.Add(CrearCard(tit, sub, dias, Color.FromArgb(205, 225, 255)));
                        break;
                    case BE.EstadoPedido.Entregado:
                        colEntregado.Controls.Add(CrearCard(tit, sub, dias, Color.FromArgb(210, 240, 220)));
                        break;
                }
            }

            if (colPendiente.Controls.Count  == 0) colPendiente.Controls.Add(CrearVacio());
            if (colDespachado.Controls.Count == 0) colDespachado.Controls.Add(CrearVacio());
            if (colEntregado.Controls.Count  == 0) colEntregado.Controls.Add(CrearVacio());
        }

        private void ActualizarSesion()
        {
            try
            {
                var u = _bllUsuario.ObtenerUsuarioActivo();
                var h = _bllUsuario.ObtenerFechaInicioSesion();
                if (u != null)
                    lblSesion.Text = $"{u.Username}  ·  {u.Perfil ?? "—"}" + (h.HasValue ? $"  ·  {h.Value:HH:mm}" : "");
            }
            catch { }
        }

        // Pinta la tarjeta como clickeable y, al hacer clic, abre (o enfoca) Pedidos de Venta.
        // Todavía no deja seleccionado el pedido puntual dentro de la grilla — PedidosVenta no
        // tiene esa capacidad hoy — pero ya ahorra la navegación por menú (Suscriptores/Ventas
        // → Pedidos de Venta).
        private void HabilitarClicAbrirPedidosVenta(Panel card)
        {
            EventHandler abrir = (s, e) => AbrirPedidosVenta();
            card.Cursor = Cursors.Hand;
            card.Click += abrir;
            foreach (Control c in card.Controls)
            {
                c.Cursor = Cursors.Hand;
                c.Click += abrir;
            }
        }

        private void AbrirPedidosVenta()
        {
            var menu = this.MdiParent;
            if (menu == null) return;
            foreach (Form hijo in menu.MdiChildren)
                if (hijo is PedidosVenta) { hijo.BringToFront(); return; }
            new PedidosVenta { MdiParent = menu }.Show();
        }

        // ── Handlers de eventos estáticos (wireados desde el Diseñador) ─────────

        private void PanelHeader_Paint(object sender, PaintEventArgs pe)
        {
            using (var br = new LinearGradientBrush(panelHeader.ClientRectangle,
                Color.FromArgb(210, 100, 135), Color.FromArgb(176, 62, 96), LinearGradientMode.Horizontal))
                pe.Graphics.FillRectangle(br, panelHeader.ClientRectangle);
        }

        private void PanelHeader_Resize(object sender, EventArgs e) => btnRefrescar.Left = panelHeader.Width - 112;

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarEnBackground();

        private void FlowCards_Resize(object sender, EventArgs e)
        {
            int cnt = flowCards.Controls.Count;
            if (cnt == 0) return;
            int avail = flowCards.ClientSize.Width - flowCards.Padding.Horizontal - cnt * 8;
            int w     = Math.Max(100, avail / cnt);
            foreach (Control c in flowCards.Controls) c.Width = w;
        }

        private void TarjetaKpi_Paint(object sender, PaintEventArgs pe)
        {
            var card = (Panel)sender;
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10))
            using (var br   = new SolidBrush(card.BackColor))
                pe.Graphics.FillPath(br, path);
        }

        private void CardPedidos_Resize(object sender, EventArgs e) { numPedidos.Width = cardPedidos.Width; txtPedidos.Width = cardPedidos.Width; }
        private void CardClientes_Resize(object sender, EventArgs e) { numClientes.Width = cardClientes.Width; txtClientes.Width = cardClientes.Width; }
        private void CardPlanes_Resize(object sender, EventArgs e) { numPlanes.Width = cardPlanes.Width; txtPlanes.Width = cardPlanes.Width; }
        private void CardSuscripciones_Resize(object sender, EventArgs e) { numSuscripciones.Width = cardSuscripciones.Width; txtSuscripciones.Width = cardSuscripciones.Width; }

        private void ColPendiente_Resize(object sender, EventArgs e) => AjustarAnchosCards(colPendiente);
        private void ColDespachado_Resize(object sender, EventArgs e) => AjustarAnchosCards(colDespachado);
        private void ColEntregado_Resize(object sender, EventArgs e) => AjustarAnchosCards(colEntregado);

        private static void AjustarAnchosCards(FlowLayoutPanel col)
        {
            int w = Math.Max(100, col.ClientSize.Width - col.Padding.Horizontal - 2);
            foreach (Control c in col.Controls) c.Width = w;
        }

        private static Panel CrearCard(string titulo, string sub, int dias, Color fondo)
        {
            var card = new Panel { Width = 180, Height = 64, BackColor = fondo, Margin = new Padding(0, 0, 0, 6) };
            card.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 6))
                using (var br   = new SolidBrush(card.BackColor))
                    pe.Graphics.FillPath(br, path);
            };
            card.Controls.Add(new Label { Text = titulo, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), AutoSize = true, Location = new Point(8, 6), BackColor = Color.Transparent });
            card.Controls.Add(new Label { Text = sub, Font = new Font("Segoe UI", 7.5f), AutoSize = false, Size = new Size(164, 16), Location = new Point(8, 24), BackColor = Color.Transparent, ForeColor = Color.FromArgb(70, 70, 80) });
            string dStr = dias == 0 ? "hoy" : $"hace {dias}d";
            card.Controls.Add(new Label { Text = dStr, Font = new Font("Segoe UI", 7f, FontStyle.Italic), AutoSize = true, Location = new Point(8, 44), BackColor = Color.Transparent, ForeColor = Color.FromArgb(120, 100, 110) });
            return card;
        }

        private static Panel CrearVacio()
        {
            var p = new Panel { Width = 180, Height = 28, BackColor = Color.Transparent };
            p.Controls.Add(new Label { Text = "— sin tareas —", Font = new Font("Segoe UI", 8f, FontStyle.Italic), ForeColor = Color.Silver, AutoSize = true, Location = new Point(6, 6), BackColor = Color.Transparent });
            return p;
        }

        private static GraphicsPath RoundedRect(Rectangle b, int r)
        {
            int d = r * 2;
            var path = new GraphicsPath();
            path.AddArc(b.X,         b.Y,          d, d, 180, 90);
            path.AddArc(b.Right - d, b.Y,          d, d, 270, 90);
            path.AddArc(b.Right - d, b.Bottom - d, d, d,   0, 90);
            path.AddArc(b.X,         b.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
