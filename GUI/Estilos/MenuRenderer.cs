using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GUI.Estilos
{
    /// <summary>
    /// Renderer visual del menú principal (GUI.Menu). En vez del MenuStrip plano por
    /// defecto de WinForms, cada ítem de primer nivel se pinta como un "chip" redondeado
    /// con fondo propio por área funcional — separados entre sí por el propio fondo rosa
    /// de la barra (vía Margin en cada ítem, ver Menu.AplicarEstiloMenu), así se leen como
    /// botones independientes en vez de una fila continua.
    ///
    /// Los ítems dentro de un dropdown mantienen fondo blanco con una franja de acento a
    /// la izquierda heredada del color del menú padre (buscada subiendo por OwnerItem) y
    /// un resaltado redondeado suave al pasar el mouse. Los separadores se dibujan como
    /// 3 puntos centrados en vez de la línea clásica.
    ///
    /// Pura presentación (colores/formas de GDI+) — no hay decisiones de negocio acá.
    /// </summary>
    public class MenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color AcentoPorDefecto = Color.FromArgb(210, 100, 135);
        private static readonly Color ResaltadoDropdown = Color.FromArgb(252, 228, 235);
        private static readonly Color ColorResaltoSistema = Color.FromArgb(224, 231, 245);

        // Paleta por Name del ítem de primer nivel — un color por área funcional. Los
        // ítems que ya gestionan su propio texto dinámicamente (Sesión/Alertas) reciben
        // un fondo neutro/oscuro acorde para que ese texto siga siendo legible.
        private static readonly Dictionary<string, Color> _paleta =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["panelControlToolStripMenuItem"]     = Color.FromArgb(255, 255, 255),
            ["ventanaToolStripMenuItem"]           = Color.FromArgb(238, 238, 242),
            ["suscriptoresToolStripMenuItem"]      = Color.FromArgb(255, 205, 205),
            ["inventarioToolStripMenuItem"]        = Color.FromArgb(255, 219, 173),
            ["ventasToolStripMenuItem"]            = Color.FromArgb(190, 230, 200),
            ["auditoriaToolStripMenuItem"]         = Color.FromArgb(190, 213, 240),
            ["analiticaNegocioToolStripMenuItem"]  = Color.FromArgb(215, 200, 240),
            ["gestionToolStripMenuItem"]           = Color.FromArgb(220, 216, 226),
            ["usuarioToolStripMenuItem"]           = Color.FromArgb(250, 250, 252),
            ["alertasItem"]                        = Color.FromArgb(176, 62, 96),
        };

        private static Color Aclarar(Color c, int cantidad) => Color.FromArgb(
            Math.Min(255, c.R + cantidad), Math.Min(255, c.G + cantidad), Math.Min(255, c.B + cantidad));

        private static Color Oscurecer(Color c, int cantidad) => Color.FromArgb(
            Math.Max(0, c.R - cantidad), Math.Max(0, c.G - cantidad), Math.Max(0, c.B - cantidad));

        // Busca el color de categoría subiendo por la cadena de OwnerItem hasta encontrar
        // un ítem de primer nivel conocido — así un ítem dentro de un submenú anidado
        // (ej. "Cuentas de Usuario" dentro de Usuarios ▸ dentro de Administrar) hereda el
        // color de "Administrar", no un default genérico.
        private static Color ColorCategoria(ToolStripItem item)
        {
            var actual = item;
            while (actual != null)
            {
                if (_paleta.TryGetValue(actual.Name ?? string.Empty, out var color)) return color;
                actual = actual.OwnerItem;
            }
            return AcentoPorDefecto;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (!item.IsOnDropDown)
            {
                // ── Ítem de primer nivel: chip redondeado con color de categoría ──────
                Color baseColor = ColorCategoria(item);
                Color fondo = item.Pressed ? Oscurecer(baseColor, 25)
                            : item.Selected ? Aclarar(baseColor, 20)
                            : baseColor;

                var rect = new Rectangle(0, 0, item.Width - 1, item.Height - 1);
                using (var path = Redondeado(rect, 6))
                using (var br = new SolidBrush(fondo))
                    g.FillPath(br, path);
                return;
            }

            // ── Ítem dentro de un dropdown: fondo blanco + franja de acento a la izquierda ──
            // Excepción: los 3 ítems de "Sistema" (grpSistema) traen su propio BackColor
            // celeste desde el Designer (resalte distintivo ya existente) — se respeta en
            // vez de taparlo con blanco, para no perder esa señal visual.
            var rectItem = new Rectangle(Point.Empty, item.Size);
            Color fondoDropdown = item.BackColor.ToArgb() == ColorResaltoSistema.ToArgb()
                ? item.BackColor
                : Color.White;
            using (var br = new SolidBrush(fondoDropdown))
                g.FillRectangle(br, rectItem);

            if (item.Selected || item.Pressed)
            {
                var rectResalte = new Rectangle(2, 1, Math.Max(1, item.Width - 4), Math.Max(1, item.Height - 2));
                using (var path = Redondeado(rectResalte, 4))
                using (var br = new SolidBrush(ResaltadoDropdown))
                    g.FillPath(br, path);
            }

            using (var br = new SolidBrush(ColorCategoria(item)))
                g.FillRectangle(br, 0, 1, 4, Math.Max(0, item.Height - 2));
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics;
            var item = e.Item;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cy = item.Height / 2;
            int cx = item.Width / 2;
            using (var br = new SolidBrush(Color.FromArgb(200, 190, 200)))
            {
                for (int i = -1; i <= 1; i++)
                    g.FillEllipse(br, cx + i * 10 - 2, cy - 2, 4, 4);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Color.FromArgb(140, 90, 115);
            base.OnRenderArrow(e);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // La barra en sí mantiene el color de marca fijado en el Designer (menuStrip1);
            // los dropdown quedan blancos, sin el degradé celeste del renderer profesional
            // default — cada ítem pinta su propio fondo en OnRenderMenuItemBackground.
            Color fondo = e.ToolStrip is MenuStrip ? e.ToolStrip.BackColor : Color.White;
            using (var br = new SolidBrush(fondo))
                e.Graphics.FillRectangle(br, e.AffectedBounds);
        }

        private static GraphicsPath Redondeado(Rectangle b, int r)
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
