using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// PESTAÑAS y PANTALLA DIVIDIDA del Menú principal (parte separada de Menu.cs):
    ///
    ///   • Cada pantalla que se abre dentro del Menú (hijo MDI) ocupa todo el espacio de trabajo,
    ///     sin barra de título propia, y tiene su pestaña en tabsVentanas, debajo del menú. Se cambia
    ///     de pantalla con un clic en la pestaña y se cierra con la × (o con el botón del medio).
    ///     No importa desde dónde se abra la pantalla (Menu.AbrirUnico, el Panel de Control, un
    ///     tablero): se engancha al agregarse al área MDI (MdiClient.ControlAdded).
    ///   • Pantalla dividida, como en los navegadores: clic derecho en una pestaña → «Fijar a la
    ///     izquierda». Esa pantalla queda en la mitad izquierda y la mitad derecha muestra la pestaña
    ///     que se elija. También desde Ventanas Abiertas → Dividir pantalla (la activa a la izquierda).
    ///
    /// Las pantallas siguen siendo hijos MDI: ActiveMdiChild, la barra «Usted está en», F1 y la
    /// lista de Ventanas Abiertas funcionan igual que antes.
    /// </summary>
    public partial class Menu
    {
        // Ancho de la franja que separa las dos mitades (se ve el fondo del área MDI).
        private const int SeparacionDivision = 4;

        private MdiClient _areaMdi;
        private bool _sincronizandoPestanias;
        private Form _fijadaIzquierda;
        private TabPage _pestaniaDelMenu;   // pestaña sobre la que se hizo clic derecho

        /// <summary>Engancha las pestañas al área MDI. Se llama una vez desde el constructor.</summary>
        private void InicializarPestanias()
        {
            _areaMdi = Controls.OfType<MdiClient>().FirstOrDefault();
            if (_areaMdi == null) return;

            // Orden de acoplado: el último del z-order se acopla primero. Las pestañas van justo
            // debajo del menú y el área MDI ocupa lo que queda.
            tabsVentanas.BringToFront();
            _areaMdi.BringToFront();

            _areaMdi.ControlAdded += (s, e) => { if (e.Control is Form hijo) AgregarPestania(hijo); };
            _areaMdi.Resize += (s, e) => Acomodar();
            MdiChildActivate += (s, e) =>
            {
                SeleccionarPestaniaActiva();
                Acomodar();
            };
        }

        // ── Pestañas ────────────────────────────────────────────────────────────

        private void AgregarPestania(Form hijo)
        {
            if (tabsVentanas.TabPages.Cast<TabPage>().Any(p => p.Tag == hijo)) return;

            // Sin borde ni tamaños mínimos: la posición y el tamaño los decide Acomodar.
            hijo.FormBorderStyle = FormBorderStyle.None;
            hijo.MinimumSize = Size.Empty;
            hijo.MaximumSize = Size.Empty;
            hijo.AutoScroll = true;   // una pantalla diseñada más grande que el espacio se desplaza

            var pestania = new TabPage(TituloPestania(hijo)) { Tag = hijo };
            tabsVentanas.TabPages.Add(pestania);
            tabsVentanas.Visible = true;

            hijo.TextChanged += (s, e) => { pestania.Text = TituloPestania(hijo); tabsVentanas.Invalidate(); };
            hijo.FormClosed += (s, e) => QuitarPestania(hijo, pestania);
            // Si la pantalla se mueve o cambia de tamaño sola (en su Load, por ejemplo), vuelve a su lugar.
            hijo.SizeChanged += (s, e) => Acomodar();
            hijo.LocationChanged += (s, e) => Acomodar();
            Acomodar();
        }

        private void QuitarPestania(Form hijo, TabPage pestania)
        {
            if (_fijadaIzquierda == hijo) _fijadaIzquierda = null;
            _sincronizandoPestanias = true;
            try
            {
                tabsVentanas.TabPages.Remove(pestania);
                pestania.Dispose();
            }
            finally { _sincronizandoPestanias = false; }
            tabsVentanas.Visible = tabsVentanas.TabCount > 0;
            Acomodar();
        }

        private static string TituloPestania(Form hijo) =>
            string.IsNullOrWhiteSpace(hijo.Text) ? hijo.GetType().Name : hijo.Text.Trim();

        private TabPage PestaniaDe(Form hijo) =>
            tabsVentanas.TabPages.Cast<TabPage>().FirstOrDefault(p => p.Tag == hijo);

        private void SeleccionarPestaniaActiva()
        {
            var pestania = ActiveMdiChild == null ? null : PestaniaDe(ActiveMdiChild);
            if (pestania == null || tabsVentanas.SelectedTab == pestania) { tabsVentanas.Invalidate(); return; }
            _sincronizandoPestanias = true;
            try { tabsVentanas.SelectedTab = pestania; }
            finally { _sincronizandoPestanias = false; }
        }

        private void TabsVentanas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_sincronizandoPestanias) return;
            if (tabsVentanas.SelectedTab?.Tag is Form hijo && !hijo.IsDisposed)
                hijo.Activate();
        }

        private void TabsVentanas_MouseDown(object sender, MouseEventArgs e)
        {
            _pestaniaDelMenu = null;
            for (int i = 0; i < tabsVentanas.TabCount; i++)
            {
                Rectangle r = tabsVentanas.GetTabRect(i);
                if (!r.Contains(e.Location)) continue;
                var pestania = tabsVentanas.TabPages[i];
                if ((e.Button == MouseButtons.Left && RectanguloCerrar(r).Contains(e.Location)) || e.Button == MouseButtons.Middle)
                    (pestania.Tag as Form)?.Close();
                else if (e.Button == MouseButtons.Right)
                    _pestaniaDelMenu = pestania;
                return;
            }
        }

        private static Rectangle RectanguloCerrar(Rectangle pestania) =>
            new Rectangle(pestania.Right - 20, pestania.Top + (pestania.Height - 16) / 2, 16, 16);

        private void TabsVentanas_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabsVentanas.TabCount) return;
            var pestania = tabsVentanas.TabPages[e.Index];
            Rectangle r = tabsVentanas.GetTabRect(e.Index);
            bool activa = e.Index == tabsVentanas.SelectedIndex;

            using (var fondo = new SolidBrush(activa ? Color.White : Tema.RosaMuyClara))
                e.Graphics.FillRectangle(fondo, r);
            // La pantalla fijada a la izquierda se marca con una franja abajo.
            if (_fijadaIzquierda != null && pestania.Tag == _fijadaIzquierda)
                using (var marca = new SolidBrush(Tema.RosaPrimario))
                    e.Graphics.FillRectangle(marca, r.Left, r.Bottom - 3, r.Width, 3);

            var areaTexto = new Rectangle(r.Left + 6, r.Top, r.Width - 28, r.Height);
            TextRenderer.DrawText(e.Graphics, pestania.Text, tabsVentanas.Font, areaTexto, activa ? Tema.RosaOscuro : Tema.Tinta,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, "×", tabsVentanas.Font, RectanguloCerrar(r), Tema.TextoMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        // ── Pantalla dividida ───────────────────────────────────────────────────

        /// <summary>
        /// Deja <paramref name="izquierda"/> en la mitad izquierda; la derecha muestra la pestaña
        /// activa entre las demás (si la fijada era la activa, pasa a la derecha la última usada).
        /// </summary>
        private void Dividir(Form izquierda)
        {
            if (izquierda == null || izquierda.IsDisposed || MdiChildren.Length < 2) return;
            _fijadaIzquierda = izquierda;
            if (ActiveMdiChild == izquierda)
                MdiChildren.Where(f => f != izquierda && !f.IsDisposed)
                           .OrderBy(f => tabsVentanas.TabPages.IndexOf(PestaniaDe(f)))
                           .LastOrDefault()?.Activate();
            Acomodar();
        }

        private void QuitarDivision()
        {
            _fijadaIzquierda = null;
            Acomodar();
        }

        private bool _acomodando;

        // Ubica cada pantalla: sin división, todas ocupan el área completa (se ve la activa, que está
        // al frente); con división, la fijada ocupa la mitad izquierda y las demás la derecha. Se usan
        // coordenadas y no Dock porque el Dock depende del z-order, y reordenar un hijo MDI lo activa.
        private void Acomodar()
        {
            if (_areaMdi == null || _acomodando) return;
            if (_fijadaIzquierda != null && _fijadaIzquierda.IsDisposed) _fijadaIzquierda = null;
            _acomodando = true;
            try
            {
                Rectangle area = _areaMdi.ClientRectangle;
                int mitad = area.Width / 2;
                var izquierda = new Rectangle(0, 0, Math.Max(0, mitad - SeparacionDivision / 2), area.Height);
                var derecha = new Rectangle(mitad + SeparacionDivision / 2, 0, Math.Max(0, area.Width - mitad - SeparacionDivision / 2), area.Height);
                foreach (Form hijo in MdiChildren)
                {
                    if (hijo.IsDisposed || hijo.WindowState != FormWindowState.Normal) continue;
                    Rectangle destino = _fijadaIzquierda == null ? area : hijo == _fijadaIzquierda ? izquierda : derecha;
                    if (hijo.Bounds != destino) hijo.Bounds = destino;
                }
            }
            finally { _acomodando = false; }
            tabsVentanas.Invalidate();
        }

        private void VentanaMenu_DropDownOpening(object sender, EventArgs e)
        {
            dividirPantallaToolStripMenuItem.Enabled = MdiChildren.Length >= 2 && ActiveMdiChild != null;
            quitarDivisionToolStripMenuItem.Enabled = _fijadaIzquierda != null;
        }

        private void DividirPantalla_Click(object sender, EventArgs e) => Dividir(ActiveMdiChild);

        private void QuitarDivision_Click(object sender, EventArgs e) => QuitarDivision();

        // ── Menú contextual de la pestaña ───────────────────────────────────────

        private void MenuPestania_Opening(object sender, CancelEventArgs e)
        {
            if (_pestaniaDelMenu == null) { e.Cancel = true; return; }
            fijarIzquierdaPestaniaItem.Text = Tx("mnu.pestania.fijar", "Fijar a la izquierda (pantalla dividida)");
            quitarDivisionPestaniaItem.Text = Tx("mnu.ventana.quitardivision", "Quitar pantalla dividida");
            cerrarPestaniaItem.Text = Tx("mnu.pestania.cerrar", "Cerrar");
            cerrarOtrasPestaniaItem.Text = Tx("mnu.pestania.cerrarotras", "Cerrar las demás");

            fijarIzquierdaPestaniaItem.Enabled = MdiChildren.Length >= 2 && _pestaniaDelMenu.Tag != _fijadaIzquierda;
            quitarDivisionPestaniaItem.Enabled = _fijadaIzquierda != null;
            cerrarOtrasPestaniaItem.Enabled = tabsVentanas.TabCount > 1;
        }

        private void FijarIzquierdaPestania_Click(object sender, EventArgs e) => Dividir(_pestaniaDelMenu?.Tag as Form);

        private void CerrarPestania_Click(object sender, EventArgs e) => (_pestaniaDelMenu?.Tag as Form)?.Close();

        private void CerrarOtrasPestanias_Click(object sender, EventArgs e)
        {
            var conservar = _pestaniaDelMenu?.Tag as Form;
            foreach (Form hijo in MdiChildren)
                if (hijo != conservar) hijo.Close();
        }
    }
}
