using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    public partial class DashboardSupervisor : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IPedidoService  _bllPedido  = new BLL.Pedido();
        private readonly BLL.Interfaces.IPrendaService  _bllPrenda  = new BLL.Prenda();
        private readonly BLL.Interfaces.IClienteService _bllCliente = new BLL.Cliente();
        private readonly BLL.Bitacora                   _bllBitacora = new BLL.Bitacora();
        private readonly BLL.Usuario                    _bllUsuario = new BLL.Usuario();

        private System.Windows.Forms.Timer _timer;

        public DashboardSupervisor()
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

            this.Text          = Tr("dash.supervisor.titulo", "Panel de Supervisor");
            lblTitulo.Text     = Tr("dash.supervisor.titulo", "Panel de Supervisor");
            btnRefrescar.Text  = Tr("dash.btn.refrescar",     "↻ Actualizar");
            txtPrendas.Text  = Tr("dash.prendas",  "Prendas\ndisponibles");
            txtClientes.Text = Tr("dash.clientes", "Clientes\nregistrados");
            txtPedidos.Text  = Tr("dash.pedidos",  "Pedidos\npendientes");
            lblColPed.Text  = Tr("dash.sup.pedidos",  "Pedidos pendientes");
            lblColMant.Text = Tr("dash.sup.mant",     "En mantenimiento");
            lblColBit.Text  = Tr("dash.sup.bitacora", "Actividad reciente");
        }

        private void CargarEnBackground()
        {
            Task.Run(() =>
            {
                try
                {
                    var pedidos   = _bllPedido.ObtenerPendientes();
                    var prendas   = _bllPrenda.ObtenerDisponibles();
                    var clientes  = _bllCliente.ObtenerTodos();
                    var enMant    = _bllPrenda.ObtenerEnMantenimiento();
                    System.Data.DataTable actividad = null;
                    try { actividad = _bllBitacora.ObtenerUltimosNDiasSistema(7); }
                    catch (Exception ex) { System.Diagnostics.Trace.TraceWarning($"[DashboardSupervisor] No se pudo cargar la bitácora: {ex.Message}"); }

                    this.BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        ActualizarCards(prendas, clientes, pedidos);
                        ActualizarKanban(pedidos, enMant, actividad);
                        ActualizarSesion();
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"[DashboardSupervisor] No se pudo cargar el dashboard: {ex.Message}");
                }
            });
        }

        private void ActualizarCards(List<BE.Prenda> prendas, List<BE.Cliente> clientes, List<BE.Pedido> pedidos)
        {
            numPrendas.Text  = prendas.Count.ToString();
            numClientes.Text = clientes.Count.ToString();
            numPedidos.Text  = pedidos.Count.ToString();
        }

        private void ActualizarKanban(List<BE.Pedido> pedidos, List<BE.MantenimientoPrenda> enMant, System.Data.DataTable actividad)
        {
            colPedidos.Controls.Clear();
            colMant.Controls.Clear();
            colBitacora.Controls.Clear();

            foreach (var p in pedidos)
            {
                int    dias = p.DiasDesdeAlta;
                string tit  = $"Pedido #{p.IdPedido}";
                string sub  = p.NombreCliente ?? $"Cliente {p.IdCliente}";
                colPedidos.Controls.Add(CrearCard(tit, sub, dias,
                    p.EsUrgentePorAntiguedad ? Color.FromArgb(255, 205, 200) : Color.FromArgb(255, 240, 200)));
            }

            foreach (var m in enMant)
            {
                int    dias = m.DiasTranscurridos;
                string tit  = m.NombrePrenda ?? $"Prenda #{m.IdPrenda}";
                string sub  = $"Entrada: {m.FechaEntrada:dd/MM/yyyy}";
                colMant.Controls.Add(CrearCard(tit, sub, dias,
                    m.NivelUrgencia == BE.NivelUrgencia.Urgente ? Color.FromArgb(255, 205, 200)
                    : m.NivelUrgencia == BE.NivelUrgencia.Normal ? Color.FromArgb(255, 248, 210)
                    : Color.FromArgb(210, 240, 220)));
            }

            if (actividad != null)
            {
                int n = 0;
                foreach (System.Data.DataRow row in actividad.Rows)
                {
                    if (n >= 8) break;
                    string evento  = row["actividad"]?.ToString() ?? "";
                    string usuario = row["usuario"]?.ToString() ?? "";
                    string fecha   = row["fecha"]?.ToString() ?? "";
                    colBitacora.Controls.Add(CrearCardBitacora(evento, usuario, fecha));
                    n++;
                }
            }

            if (colPedidos.Controls.Count  == 0) colPedidos.Controls.Add(CrearVacio());
            if (colMant.Controls.Count     == 0) colMant.Controls.Add(CrearVacio());
            if (colBitacora.Controls.Count == 0) colBitacora.Controls.Add(CrearVacio());
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
                System.Diagnostics.Trace.TraceWarning($"[DashboardSupervisor] No se pudo cargar la sesión: {ex.Message}");
            }
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

        private void CardPrendas_Resize(object sender, EventArgs e) { numPrendas.Width = cardPrendas.Width; txtPrendas.Width = cardPrendas.Width; }
        private void CardClientes_Resize(object sender, EventArgs e) { numClientes.Width = cardClientes.Width; txtClientes.Width = cardClientes.Width; }
        private void CardPedidos_Resize(object sender, EventArgs e) { numPedidos.Width = cardPedidos.Width; txtPedidos.Width = cardPedidos.Width; }

        private void ColPedidos_Resize(object sender, EventArgs e) => AjustarAnchosCards(colPedidos);
        private void ColMant_Resize(object sender, EventArgs e) => AjustarAnchosCards(colMant);
        private void ColBitacora_Resize(object sender, EventArgs e) => AjustarAnchosCards(colBitacora);

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

        private static Panel CrearCardBitacora(string evento, string usuario, string fecha)
        {
            var card = new Panel { Width = 180, Height = 64, BackColor = Color.FromArgb(235, 230, 252), Margin = new Padding(0, 0, 0, 6) };
            card.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 6))
                using (var br   = new SolidBrush(card.BackColor))
                    pe.Graphics.FillPath(br, path);
            };
            card.Controls.Add(new Label { Text = evento, Font = new Font("Segoe UI", 8f, FontStyle.Bold), AutoSize = false, Size = new Size(164, 16), Location = new Point(8, 6), BackColor = Color.Transparent, ForeColor = Color.FromArgb(100, 80, 160) });
            card.Controls.Add(new Label { Text = usuario, Font = new Font("Segoe UI", 7.5f), AutoSize = false, Size = new Size(164, 16), Location = new Point(8, 24), BackColor = Color.Transparent, ForeColor = Color.FromArgb(70, 70, 80) });
            card.Controls.Add(new Label { Text = fecha, Font = new Font("Segoe UI", 7f, FontStyle.Italic), AutoSize = true, Location = new Point(8, 44), BackColor = Color.Transparent, ForeColor = Color.FromArgb(120, 110, 140) });
            return card;
        }

        private static Panel CrearVacio()
        {
            var p = new Panel { Width = 180, Height = 28, BackColor = Color.Transparent };
            p.Controls.Add(new Label { Text = "— sin elementos —", Font = new Font("Segoe UI", 8f, FontStyle.Italic), ForeColor = Color.Silver, AutoSize = true, Location = new Point(6, 6), BackColor = Color.Transparent });
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
