using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    public partial class DashboardControlStock : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IPrendaService _bllPrenda  = new BLL.Prenda();
        private readonly BLL.Usuario                   _bllUsuario = new BLL.Usuario();

        private System.Windows.Forms.Timer _timer;

        public DashboardControlStock()
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

            this.Text          = Tr("dash.stock.titulo",   "Panel de Stock");
            lblTitulo.Text     = Tr("dash.stock.titulo",   "Panel de Stock");
            btnRefrescar.Text  = Tr("dash.btn.refrescar",  "↻ Actualizar");
            txtDisp.Text = Tr("dash.prendas",    "Prendas\ndisponibles");
            txtMant.Text = Tr("dash.mant.activo", "En\nmantenimiento");
            txtOcup.Text = Tr("dash.ocupacion",  "ocupación\ndel stock");
            lblColRec.Text = Tr("dash.mant.reciente", "Reciente (< 2d)");
            lblColCur.Text = Tr("dash.mant.encurso",  "En curso (2-7d)");
            lblColUrg.Text = Tr("dash.mant.urgente",  "Urgente (> 7d)");
        }

        private void CargarEnBackground()
        {
            Task.Run(() =>
            {
                try
                {
                    var disponibles  = _bllPrenda.ObtenerDisponibles();
                    var enMant       = _bllPrenda.ObtenerEnMantenimiento();
                    var ocupacion    = _bllPrenda.ObtenerOcupacion();
                    this.BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        ActualizarCards(disponibles, enMant, ocupacion);
                        ActualizarKanban(enMant);
                        ActualizarSesion();
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"[DashboardControlStock] No se pudo cargar el dashboard: {ex.Message}");
                }
            });
        }

        private void ActualizarCards(List<BE.Prenda> disponibles, List<BE.MantenimientoPrenda> enMant, BE.OcupacionStock ocup)
        {
            numDisp.Text = disponibles.Count.ToString();

            numMant.Text = enMant.Count.ToString();
            Color fondo = enMant.Count == 0
                ? Color.FromArgb(215, 240, 220)
                : enMant.Count > 5 ? Color.FromArgb(255, 218, 218) : Color.FromArgb(255, 248, 210);
            cardMant.BackColor = fondo;

            if (ocup != null)
            {
                numOcup.Text = $"{ocup.PorcentajeOcupacion}%";
                numOcup.Font = new Font("Segoe UI", 24f, FontStyle.Bold);
                txtOcup.Text = $"{ocup.EnUso} en uso · {ocup.Disponibles} libres";
            }
        }

        private void ActualizarKanban(List<BE.MantenimientoPrenda> enMant)
        {
            colReciente.Controls.Clear();
            colEnCurso.Controls.Clear();
            colUrgente.Controls.Clear();

            foreach (var m in enMant)
            {
                int    dias  = m.DiasTranscurridos;
                string tit   = m.NombrePrenda ?? $"Prenda #{m.IdPrenda}";
                string sub   = $"Entrada: {m.FechaEntrada:dd/MM/yyyy}";
                var nivel    = m.NivelUrgencia;

                // Las tres columnas abren Prendas: OperadorDeInventario siempre tiene ese
                // permiso (mnuPrendas), así que no hay riesgo de exponer una pantalla sin acceso.
                Panel card = nivel == BE.NivelUrgencia.Reciente
                    ? CrearCard(tit, sub, dias, Color.FromArgb(210, 240, 220))
                    : nivel == BE.NivelUrgencia.Normal
                        ? CrearCard(tit, sub, dias, Color.FromArgb(255, 248, 210))
                        : CrearCard(tit, sub, dias, Color.FromArgb(255, 205, 200));
                HabilitarClicAbrirPrendas(card);

                if (nivel == BE.NivelUrgencia.Reciente)     colReciente.Controls.Add(card);
                else if (nivel == BE.NivelUrgencia.Normal)  colEnCurso.Controls.Add(card);
                else                                        colUrgente.Controls.Add(card);
            }

            if (colReciente.Controls.Count == 0) colReciente.Controls.Add(CrearVacio());
            if (colEnCurso.Controls.Count  == 0) colEnCurso.Controls.Add(CrearVacio());
            if (colUrgente.Controls.Count  == 0) colUrgente.Controls.Add(CrearVacio());
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
                System.Diagnostics.Trace.TraceWarning($"[DashboardControlStock] No se pudo cargar la sesión: {ex.Message}");
            }
        }

        // Abre (o enfoca) Prendas. Todavía no deja seleccionada la prenda puntual dentro de la
        // grilla — esa pantalla no tiene esa capacidad hoy.
        private void HabilitarClicAbrirPrendas(Panel card)
        {
            EventHandler abrir = (s, e) => AbrirPrendas();
            card.Cursor = Cursors.Hand;
            card.Click += abrir;
            foreach (Control c in card.Controls)
            {
                c.Cursor = Cursors.Hand;
                c.Click += abrir;
            }
        }

        private void AbrirPrendas()
        {
            var menu = this.MdiParent;
            if (menu == null) return;
            foreach (Form hijo in menu.MdiChildren)
                if (hijo is Prendas) { hijo.BringToFront(); return; }
            new Prendas { MdiParent = menu }.Show();
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

        private void CardDisp_Resize(object sender, EventArgs e) { numDisp.Width = cardDisp.Width; txtDisp.Width = cardDisp.Width; }
        private void CardMant_Resize(object sender, EventArgs e) { numMant.Width = cardMant.Width; txtMant.Width = cardMant.Width; }
        private void CardOcup_Resize(object sender, EventArgs e) { numOcup.Width = cardOcup.Width; txtOcup.Width = cardOcup.Width; }

        private void ColReciente_Resize(object sender, EventArgs e) => AjustarAnchosCards(colReciente);
        private void ColEnCurso_Resize(object sender, EventArgs e) => AjustarAnchosCards(colEnCurso);
        private void ColUrgente_Resize(object sender, EventArgs e) => AjustarAnchosCards(colUrgente);

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
            card.Controls.Add(new Label { Text = dStr, Font = new Font("Segoe UI", 7f, FontStyle.Italic), AutoSize = true, Location = new Point(8, 44), BackColor = Color.Transparent, ForeColor = Color.FromArgb(110, 100, 80) });
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
