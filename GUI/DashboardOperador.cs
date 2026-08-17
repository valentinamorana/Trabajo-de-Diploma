using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    public partial class DashboardOperador : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IPedidoService _bllPedido  = new BLL.Pedido();
        private readonly BLL.Usuario                   _bllUsuario = new BLL.Usuario();

        private System.Windows.Forms.Timer _timer;

        public DashboardOperador()
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

            this.Text          = Tr("dash.operador.titulo", "Panel de Operaciones");
            lblTitulo.Text     = Tr("dash.operador.titulo", "Panel de Operaciones");
            btnRefrescar.Text  = Tr("dash.btn.refrescar",   "↻ Actualizar");
            txtPend.Text = Tr("dash.pedidos",   "Pedidos\npendientes");
            txtDesp.Text = Tr("dash.despachados", "Pedidos\ndespachados");
            txtEntr.Text = Tr("dash.entregados",  "Pedidos\nentregados");
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
                    var pedidos = _bllPedido.ObtenerTodos();
                    this.BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        ActualizarCards(pedidos);
                        ActualizarKanban(pedidos);
                        ActualizarSesion();
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"[DashboardOperador] No se pudo cargar pedidos: {ex.Message}");
                }
            });
        }

        private void ActualizarCards(List<BE.Pedido> pedidos)
        {
            int pend = 0, desp = 0, entr = 0;
            foreach (var p in pedidos)
            {
                if (p.Estado == BE.EstadoPedido.Pendiente)  pend++;
                else if (p.Estado == BE.EstadoPedido.Despachado) desp++;
                else if (p.Estado == BE.EstadoPedido.Entregado)  entr++;
            }
            numPend.Text = pend.ToString();
            numDesp.Text = desp.ToString();
            numEntr.Text = entr.ToString();
        }

        private void ActualizarKanban(List<BE.Pedido> pedidos)
        {
            colPendiente.Controls.Clear();
            colDespachado.Controls.Clear();
            colEntregado.Controls.Clear();

            foreach (var p in pedidos)
            {
                int    dias = p.DiasDesdeAlta;
                string tit  = $"Pedido #{p.IdPedido}";
                string sub  = p.NombreCliente ?? $"Cliente {p.IdCliente}";

                // Las tres columnas abren la misma pantalla (Pedidos Realizados): es el único
                // permiso que tiene OperadorLogistico, y ahí se despacha, se marca entregado y
                // se ve el historial — no hay riesgo de exponer una pantalla sin permiso.
                switch (p.Estado)
                {
                    case BE.EstadoPedido.Pendiente:
                        var cPend = CrearCard(tit, sub, dias,
                            p.EsUrgentePorAntiguedad ? Color.FromArgb(255, 205, 200) : Color.FromArgb(255, 242, 200));
                        HabilitarClicAbrirPedidosRealizados(cPend);
                        colPendiente.Controls.Add(cPend);
                        break;
                    case BE.EstadoPedido.Despachado:
                        var cDesp = CrearCard(tit, sub, dias, Color.FromArgb(205, 225, 255));
                        HabilitarClicAbrirPedidosRealizados(cDesp);
                        colDespachado.Controls.Add(cDesp);
                        break;
                    case BE.EstadoPedido.Entregado:
                        var cEntr = CrearCard(tit, sub, dias, Color.FromArgb(210, 240, 220));
                        HabilitarClicAbrirPedidosRealizados(cEntr);
                        colEntregado.Controls.Add(cEntr);
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
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"[DashboardOperador] No se pudo cargar la sesión: {ex.Message}");
            }
        }

        // Abre (o enfoca) Pedidos Realizados. Todavía no deja seleccionado el pedido puntual
        // dentro de la grilla — esa pantalla no tiene esa capacidad hoy.
        private void HabilitarClicAbrirPedidosRealizados(Panel card)
        {
            EventHandler abrir = (s, e) => AbrirPedidosRealizados();
            card.Cursor = Cursors.Hand;
            card.Click += abrir;
            foreach (Control c in card.Controls)
            {
                c.Cursor = Cursors.Hand;
                c.Click += abrir;
            }
        }

        private void AbrirPedidosRealizados()
        {
            var menu = this.MdiParent;
            if (menu == null) return;
            foreach (Form hijo in menu.MdiChildren)
                if (hijo is PedidosRealizados) { hijo.BringToFront(); return; }
            new PedidosRealizados { MdiParent = menu }.Show();
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
            int w = Math.Max(100, (flowCards.ClientSize.Width - flowCards.Padding.Horizontal - cnt * 8) / cnt);
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

        private void CardPend_Resize(object sender, EventArgs e) { numPend.Width = cardPend.Width; txtPend.Width = cardPend.Width; }
        private void CardDesp_Resize(object sender, EventArgs e) { numDesp.Width = cardDesp.Width; txtDesp.Width = cardDesp.Width; }
        private void CardEntr_Resize(object sender, EventArgs e) { numEntr.Width = cardEntr.Width; txtEntr.Width = cardEntr.Width; }

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
            card.Controls.Add(new Label { Text = dStr, Font = new Font("Segoe UI", 7f, FontStyle.Italic), AutoSize = true, Location = new Point(8, 44), BackColor = Color.Transparent, ForeColor = Color.FromArgb(110, 100, 100) });
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
