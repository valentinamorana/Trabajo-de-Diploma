using System.Drawing;

namespace GUI
{
    /// <summary>
    /// Paleta y tokens visuales centralizados de WardrobeFlow (rediseño UX/UI — hallazgo #4:
    /// antes cada formulario repetía <c>Color.FromArgb(...)</c> suelto; algunos incluso
    /// definían sus propias constantes locales, como <c>AdministracionUsuariosForm.RosaPrimario</c>).
    /// Los valores son los mismos que ya estaban en uso — no se inventó paleta nueva, solo se
    /// les dio un único lugar de origen. Los formularios existentes se migran de a poco;
    /// todo control nuevo debería usar esta clase en vez de un literal.
    /// </summary>
    internal static class Tema
    {
        public static readonly Color RosaPrimario = Color.FromArgb(210, 100, 135);
        public static readonly Color RosaOscuro   = Color.FromArgb(176, 62, 96);
        public static readonly Color RosaPalido   = Color.FromArgb(252, 228, 235);
        public static readonly Color Papel        = Color.FromArgb(251, 247, 248);
        public static readonly Color PanelClaro   = Color.FromArgb(245, 245, 250);
        public static readonly Color Tinta        = Color.FromArgb(36, 26, 32);
        public static readonly Color TextoMuted   = Color.FromArgb(138, 116, 128);
        public static readonly Color Borde        = Color.FromArgb(228, 211, 217);

        public static readonly Color Exito  = Color.FromArgb(46, 125, 70);
        public static readonly Color Alerta = Color.FromArgb(166, 101, 14);
        public static readonly Color Error  = Color.FromArgb(178, 58, 58);

        /// <summary>Radio de esquina estándar para botones/cards/inputs (sección 15 del rediseño).</summary>
        public const int RadioBoton = 6;
    }
}
