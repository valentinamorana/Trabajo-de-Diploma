using BLL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// WardrobeFlow — Formulario de Login para Empleados.
    ///
    /// Todos los controles visuales están declarados en Login.Designer.cs (InitializeComponent),
    /// igual que en el resto del proyecto (ClienteForm, Usuarios, PrendaForm, etc.): nada de
    /// crear controles "a mano" en el constructor. Esto es lo que le permite al Diseñador de
    /// Windows Forms de Visual Studio mostrar la preview del formulario y navegar al código con
    /// doble click sobre cada control.
    ///
    /// PATRÓN OBSERVER — T05 Gestión de Múltiples Idiomas:
    ///   Implementa IIdiomaObserver. Se suscribe al GestorIdioma en Load
    ///   y se desuscribe en FormClosing. Al recibir UpdateLanguage() aplica
    ///   las traducciones del nuevo idioma a todos sus controles.
    ///   El selector de idioma (cmbIdioma) vive en la franja superior (pnlHeader).
    ///   Al cambiar el idioma acá, el Menu ya abre traducido cuando el usuario ingresa.
    ///
    /// Toda la paleta de color usada acá viene de <see cref="Tema"/> — no se agregan
    /// literales nuevos; las variantes hover/pressed se derivan en runtime con
    /// ControlPaint.Dark/Light sobre los mismos tokens.
    /// </summary>
    public partial class Login : Form, IIdiomaObserver
    {
        private readonly Usuario usuarioBLL = new Usuario();
        private bool suprimirIdiomaChange = false;

        public Login()
        {
            InitializeComponent();

            // ── Card flotante: esquinas redondeadas (Region) + sombra propia ──────
            // BringToFront es necesario: en el orden de InitializeComponent pnlCardShadow
            // queda "delante" de pnlCard en el z-order (el primer control agregado al form
            // es el que se pinta al frente), así que sin esto la sombra tapa la card entera.
            pnlCard.Region = new Region(BuildRoundedRect(new Rectangle(0, 0, pnlCard.Width, pnlCard.Height), Tema.RadioCard));
            pnlCard.BringToFront();

            // ── Botones grandes (Ingresar/Salir) con esquinas redondeadas tipo "pill" ─
            btnIngresar.Region = new Region(BuildRoundedRect(new Rectangle(0, 0, btnIngresar.Width, btnIngresar.Height), Tema.RadioBotonGrande));
            btnSalir.Region    = new Region(BuildRoundedRect(new Rectangle(0, 0, btnSalir.Width, btnSalir.Height), Tema.RadioBotonGrande));

            ConstruirComboIdioma(Traductor.ObtenerIdiomas());
        }

        // ── Arrastre de ventana sin borde nativo (FormBorderStyle.None) ──────────────
        // Reenvía el click a Windows como si hubiera pasado en la barra de título nativa
        // (HTCAPTION). Es el mecanismo estándar para mover un form sin bordes; no cambia
        // ningún comportamiento de negocio. Enganchado desde el Designer al MouseDown de
        // pnlHeader, picLogo y lblSubtitulo (el resto del header son controles interactivos
        // propios — combo/botones — que no deben disparar el arrastre).
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void PnlHeader_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        // ── Pintura decorativa del panel izquierdo (fondo rosa + textura sutil) ───

        private void PnlLeft_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Manchas circulares suaves, en un tono más oscuro que el fondo rosa pálido.
            using (var b = new SolidBrush(Color.FromArgb(40, Tema.RosaPrimario.R, Tema.RosaPrimario.G, Tema.RosaPrimario.B)))
                g.FillEllipse(b, 210, -50, 300, 300);

            using (var b = new SolidBrush(Color.FromArgb(35, Tema.RosaPrimario.R, Tema.RosaPrimario.G, Tema.RosaPrimario.B)))
                g.FillEllipse(b, -90, 460, 340, 340);

            using (var pen = new Pen(Color.FromArgb(70, Tema.RosaOscuro.R, Tema.RosaOscuro.G, Tema.RosaOscuro.B), 1f))
                g.DrawEllipse(pen, 240, 320, 150, 150);
        }

        // ── Sombra suave detrás de la card flotante del formulario ───────────────
        // Varias capas de un rectángulo redondeado, cada vez más grande y más transparente,
        // desplazadas levemente hacia abajo (luz desde arriba) — truco clásico de GDI+ para
        // simular blur sin necesitar procesamiento de imagen.

        private void PnlCardShadow_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int offsetX = pnlCard.Left - pnlCardShadow.Left;
            int offsetY = pnlCard.Top  - pnlCardShadow.Top;
            var cardRect = new Rectangle(offsetX, offsetY, pnlCard.Width, pnlCard.Height);

            const int capas = 6;
            for (int i = capas; i >= 1; i--)
            {
                int crecimiento = i * 3;
                int alpha = 4 + (capas - i) * 2;
                var rect = Rectangle.Inflate(cardRect, crecimiento, crecimiento);
                rect.Offset(0, 3);
                using (var path = BuildRoundedRect(rect, Tema.RadioCard + 10))
                using (var br = new SolidBrush(Color.FromArgb(alpha, Tema.Tinta.R, Tema.Tinta.G, Tema.Tinta.B)))
                    g.FillPath(br, path);
            }
        }

        // ── Líneas horizontales del separador "o" ─────────────────────────────────

        private void LblDivider_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var lbl = (Label)sender;
            int midY = lbl.Height / 2;
            using (var pen = new Pen(Tema.Borde, 0.8f))
            {
                var sz = e.Graphics.MeasureString(lbl.Text, lbl.Font);
                float cx = lbl.Width / 2f;
                float hw = sz.Width / 2f + 10;
                e.Graphics.DrawLine(pen, 0, midY, cx - hw, midY);
                e.Graphics.DrawLine(pen, cx + hw, midY, lbl.Width, midY);
            }
        }

        // ── Borde redondeado propio de las cajas de usuario/contraseña ────────────

        private void DibujarBordeCampo(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using (var path = BuildRoundedRect(rect, Tema.RadioCampo))
            using (var pen = new Pen(Tema.Borde, 1f))
                e.Graphics.DrawPath(pen, path);
        }

        // ── Ojito mostrar/ocultar contraseña ──────────────────────────────────────

        private void btnMostrarClave_Click(object sender, EventArgs e)
        {
            if (txtContraseña.PasswordChar == '\0')
            {
                txtContraseña.PasswordChar = '●';
                btnMostrarClave.Font = new Font("Segoe UI Emoji", 9f);
            }
            else
            {
                txtContraseña.PasswordChar = '\0';
                btnMostrarClave.Font = new Font("Segoe UI Emoji", 9f, FontStyle.Strikeout);
            }
        }

        // Construye un GraphicsPath de rectángulo redondeado.
        private static GraphicsPath BuildRoundedRect(Rectangle rect, int r)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.Left,            rect.Top,             r * 2, r * 2, 180, 90);
            path.AddArc(rect.Right - r * 2,   rect.Top,             r * 2, r * 2, 270, 90);
            path.AddArc(rect.Right - r * 2,   rect.Bottom - r * 2, r * 2, r * 2, 0,   90);
            path.AddArc(rect.Left,            rect.Bottom - r * 2, r * 2, r * 2, 90,  90);
            path.CloseFigure();
            return path;
        }

        // ── Selector de idioma (dropdown) en pnlHeader ────────────────────────────

        // Llena el combo con los idiomas (de BD o fallback). Un idioma nuevo aparece solo.
        private void ConstruirComboIdioma(IList<Idioma> idiomas)
        {
            suprimirIdiomaChange = true;
            cmbIdioma.DataSource    = null;
            cmbIdioma.DisplayMember = "Nombre";
            cmbIdioma.ValueMember   = "Id";
            cmbIdioma.DataSource    = new List<Idioma>(idiomas);

            string cod = GestorIdioma.IdiomaActual?.Id ?? "ES";
            for (int i = 0; i < idiomas.Count; i++)
                if (string.Equals(idiomas[i].Id, cod, StringComparison.OrdinalIgnoreCase))
                {
                    cmbIdioma.SelectedIndex = i;
                    break;
                }
            suprimirIdiomaChange = false;
        }

        private void CmbIdioma_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suprimirIdiomaChange) return;
            var idioma = cmbIdioma.SelectedItem as Idioma;
            if (idioma == null) return;
            try
            {
                var dict = new BLL.IdiomaService().CargarTraducciones(idioma.Id);
                GestorIdioma.CambiarIdioma(idioma, dict);
            }
            catch { GestorIdioma.CambiarIdioma(idioma); }
        }

        // ── Ciclo de vida ─────────────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);

            try
            {
                var idiomas = new BLL.IdiomaService().ObtenerIdiomasActivosComoIdioma();
                if (idiomas.Count > 0)
                {
                    GestorIdioma.SetIdiomasDisponibles(idiomas);
                    ConstruirComboIdioma(idiomas);
                }
            }
            catch { /* sin conexión: usa el combo con los idiomas del constructor */ }

            Traducir(GestorIdioma.IdiomaActual);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        // ══════════════════════════════════════════════════════════════════════════
        // PATRÓN OBSERVER — T05 Gestión de Múltiples Idiomas
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Recibe la notificación del GestorIdioma cuando el idioma cambia.
        /// Equivalente a UpdateLanguage(IIdioma idioma) del ejemplo de cátedra.
        /// </summary>
        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
        }

        /// <summary>
        /// Reasigna el .Text de cada control leyendo su propiedad Tag como clave
        /// de traducción — exactamente igual que en el ejemplo de cátedra (frmLogin).
        /// </summary>
        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string Tx(string tag) => (tag != null && t.ContainsKey(tag)) ? t[tag].Texto : null;

            // Título del formulario
            var tituloForm = Tx(this.Tag?.ToString());
            if (tituloForm != null) this.Text = tituloForm;

            // Header
            var sub = Tx(lblSubtitulo.Tag?.ToString());
            if (sub != null) lblSubtitulo.Text = sub;

            // Panel izquierdo
            var tagline = Tx(lblTagline.Tag?.ToString());
            if (tagline != null) lblTagline.Text = tagline;

            var desc = Tx(lblBrandDesc.Tag?.ToString());
            if (desc != null) lblBrandDesc.Text = desc;

            // Panel derecho — título y subtítulo
            var bienvenido = Tx(lblAccent.Tag?.ToString());
            if (bienvenido != null) lblAccent.Text = bienvenido;

            var cred = Tx(lblLoginSub.Tag?.ToString());
            if (cred != null) lblLoginSub.Text = cred;

            // Campos
            var usr = Tx(lblUsuario.Tag?.ToString());
            if (usr != null) lblUsuario.Text = usr;

            var pwd = Tx(lblContraseña.Tag?.ToString());
            if (pwd != null) lblContraseña.Text = pwd;

            // Botones y link
            var ingresar = Tx(btnIngresar.Tag?.ToString());
            if (ingresar != null) btnIngresar.Text = ingresar;

            var salir = Tx(btnSalir.Tag?.ToString());
            if (salir != null) btnSalir.Text = salir;

            var olvide = Tx(lnkOlvidaste.Tag?.ToString());
            if (olvide != null) lnkOlvidaste.Text = olvide;

            var emg = Tx(lnkEmergencia.Tag?.ToString());
            if (emg != null) lnkEmergencia.Text = emg;

            // Separador
            var divider = Tx(lblDivider.Tag?.ToString());
            if (divider != null) lblDivider.Text = divider;
        }

        // ── Eventos de negocio ────────────────────────────────────────────────────

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string Tx(string key, string fallback) => t.ContainsKey(key) ? t[key].Texto : fallback;

            try
            {
                // La BLL decide TODO: si las credenciales no son válidas lanza LoginException
                // (mismo mensaje exista o no el usuario). Acá solo reaccionamos al resultado.
                if (usuarioBLL.Login(this.Text, txtUsuario.Text, txtContraseña.Text))
                {
                    // Si la cuenta tiene una clave temporal/generada (alta o reset), forzar el
                    // cambio ANTES de abrir el sistema. Sin cambio no se permite ingresar.
                    var u = usuarioBLL.ObtenerUsuarioActivo();
                    if (u != null && u.RequiereCambioClave)
                    {
                        using (var dlg = new CambioClaveObligatorioForm())
                        {
                            if (dlg.ShowDialog(this) != DialogResult.OK)
                            {
                                usuarioBLL.Logout(this.Text);
                                MostrarErrorLogin(
                                    Tx("err.login.debecambiarclave",
                                       "Debés cambiar tu contraseña temporal para ingresar."),
                                    bloqueado: false);
                                return;
                            }
                        }
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (BE.LoginException ex) when (ex.Tipo == BE.LoginException.TipoError.LimiteAlcanzado)
            {
                string titulo = Tx("dlg.login.sesion.titulo", "Sesión terminada");
                string cuerpo = Tx("err.login.limitesesion",  "Demasiados intentos fallidos en esta sesión.");
                string cierre = Tx("dlg.login.sesion.cierre", "La aplicación se cerrará.");
                MessageBox.Show(cuerpo + "\n\n" + cierre, titulo, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.Exit();
            }
            catch (BE.LoginException ex) when (ex.Tipo == BE.LoginException.TipoError.CuentaBloqueada)
            {
                MostrarErrorLogin(Tx("err.login.bloqueada", ex.Message), bloqueado: true);
            }
            catch (BE.LoginException ex) when (ex.Tipo == BE.LoginException.TipoError.CredencialesInvalidas)
            {
                string msg = ex.IntentosRestantes.HasValue
                    ? string.Format(Tx("err.login.intentos", "Usuario o contraseña incorrectos.\nIntentos restantes: {0}."), ex.IntentosRestantes.Value)
                    : Tx("err.login.credenciales", "Usuario o contraseña incorrectos.");
                MostrarErrorLogin(msg, bloqueado: false);
            }
            catch (BE.LoginException ex) when (ex.Tipo == BE.LoginException.TipoError.CamposVacios)
            {
                MostrarErrorLogin(Tx("err.login.camposvacio", ex.Message), bloqueado: false);
            }
            catch (BE.LoginException ex)
            {
                MostrarErrorLogin(ex.Message, bloqueado: false);
            }
        }

        private void MostrarErrorLogin(string mensaje, bool bloqueado)
        {
            lblError.ForeColor = bloqueado
                ? ControlPaint.Dark(Tema.Error, 0.3f)
                : Tema.Error;
            lblError.Text = mensaje;
            lblError.Refresh();

            if (bloqueado)
            {
                txtUsuario.Enabled    = false;
                txtContraseña.Enabled = false;
                btnIngresar.Enabled   = false;
                this.AcceptButton     = null;
            }
            else
            {
                txtContraseña.Clear();
                txtContraseña.Focus();
            }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // ── Controles de ventana propios (FormBorderStyle.None no trae los nativos) ──

        private void btnMinimizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        // Mismo resultado que cerrar con la X nativa de un ShowDialog: DialogResult.Cancel,
        // que Program.cs interpreta como "no continuar" (ver GUI/Program.cs). No es un cambio
        // de comportamiento, es la reposición del control que se pierde al sacar el borde nativo.
        private void btnCerrarVentana_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void lnkOlvidaste_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var form = new OlvideContrasenaForm())
                form.ShowDialog(this);
        }

        // RF-10 — Abre el diálogo de autodesbloqueo con clave de emergencia. Si la cuenta queda
        // desbloqueada, se reactivan los campos del login para que el admin ingrese normalmente.
        private void LnkEmergencia_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var form = new DesbloqueoEmergenciaForm(txtUsuario.Text))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    txtUsuario.Enabled    = true;
                    txtContraseña.Enabled = true;
                    btnIngresar.Enabled   = true;
                    this.AcceptButton     = btnIngresar;
                    lblError.Text         = string.Empty;
                    txtContraseña.Focus();
                }
            }
        }
    }
}
