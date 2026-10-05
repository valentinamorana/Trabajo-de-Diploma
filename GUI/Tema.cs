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
        public static readonly Color Info   = Color.FromArgb(30, 100, 170);

        // ── Fondos de estado (cards de alertas, tarjetas de kanban, celdas resaltadas) ──────
        // Antes había 5 verdes de "éxito", varios rojos y amarillos distintos repartidos por
        // los formularios; ahora cada estado tiene un único fondo claro que combina con su
        // color de texto (Exito/Error/Alerta/Info).
        public static readonly Color FondoExito  = Color.FromArgb(215, 240, 220);
        public static readonly Color FondoError  = Color.FromArgb(255, 218, 218);
        public static readonly Color FondoAlerta = Color.FromArgb(255, 248, 210);
        public static readonly Color FondoInfo   = Color.FromArgb(225, 240, 255);

        // ── Variantes de marca y neutros ────────────────────────────────────────────────────
        public static readonly Color RosaClara       = Color.FromArgb(245, 222, 230);   // encabezado de grillas
        public static readonly Color RosaMuyClara    = Color.FromArgb(250, 244, 246);   // filas alternadas
        public static readonly Color RosaTinta       = Color.FromArgb(120, 30, 55);     // texto sobre RosaPalido
        public static readonly Color TextoSecundario = Color.FromArgb(70, 70, 80);
        public static readonly Color Dorado          = Color.FromArgb(255, 235, 130);   // badge de alertas del menú

        // ── Tema oscuro (Mi Perfil → Tema: Oscuro) ──────────────────────────────────────────
        public static readonly Color OscuroFondo      = Color.FromArgb(37, 37, 45);
        public static readonly Color OscuroControl    = Color.FromArgb(50, 50, 60);
        public static readonly Color OscuroGrilla     = Color.FromArgb(45, 45, 55);
        public static readonly Color OscuroEncabezado = Color.FromArgb(60, 60, 72);

        // ── Escala tipográfica (Segoe UI, 4 tamaños) ─────────────────────────────────────
        // Antes convivían "Microsoft Sans Serif 8.25" (default de WinForms) con varios Segoe UI
        // sueltos. Todo control nuevo o normalizado usa una de estas cuatro.
        public const string FamiliaFuente = "Segoe UI";
        public static readonly System.Drawing.Font FuentePequena  = new System.Drawing.Font(FamiliaFuente, 8.25F);
        public static readonly System.Drawing.Font FuenteNormal   = new System.Drawing.Font(FamiliaFuente, 9F);
        public static readonly System.Drawing.Font FuenteSubtitulo = new System.Drawing.Font(FamiliaFuente, 10F, FontStyle.Bold);
        public static readonly System.Drawing.Font FuenteTitulo   = new System.Drawing.Font(FamiliaFuente, 12F, FontStyle.Bold);

        /// <summary>Radio de esquina para inputs con borde propio (cajas de usuario/contraseña).</summary>
        public const int RadioCampo = 10;

        /// <summary>Radio de esquina para botones grandes tipo "pill" (Ingresar/Salir del Login).</summary>
        public const int RadioBotonGrande = 14;

        /// <summary>Radio de esquina para paneles flotantes tipo card (Login, modales).</summary>
        public const int RadioCard = 22;
    }
}
