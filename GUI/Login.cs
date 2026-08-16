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
    /// PATRÓN OBSERVER — T05 Gestión de Múltiples Idiomas:
    ///   Implementa IIdiomaObserver. Se suscribe al GestorIdioma en Load
    ///   y se desuscribe en FormClosing. Al recibir UpdateLanguage() aplica
    ///   las traducciones del nuevo idioma a todos sus controles.
    ///   El selector de idioma vive en la franja superior (pnlHeader).
    ///   Al cambiar el idioma acá, el Menu ya abre traducido cuando el usuario ingresa.
    ///
    /// Toda la paleta de color usada acá viene de <see cref="Tema"/> — no se agregan
    /// literales nuevos; las variantes hover/pressed se derivan en runtime con
    /// ControlPaint.Dark/Light sobre los mismos tokens.
    /// </summary>
    public partial class Login : Form, IIdiomaObserver
    {
        // ── Arrastre de ventana sin borde nativo (FormBorderStyle.None) ──────────────
        // Reenvía el click a Windows como si hubiera pasado en la barra de título nativa
        // (HTCAPTION). Es el mecanismo estándar para mover un form sin bordes; no cambia
        // ningún comportamiento de negocio.
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void IniciarArrastreVentana(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private readonly Usuario usuarioBLL = new Usuario();

        // Selector de idioma (dropdown) — vive en pnlHeader. Soporta idiomas dinámicos de BD.
        private ComboBox _cmbIdiomaLogin;
        private bool _suprimirIdiomaChange = false;
        // Etiquetas de marca (creadas en código para ser traducibles)
        private Label _lblTagline;
        private Label _lblBrandDesc;
        // RF-10 — link a autodesbloqueo con clave de emergencia (admin bloqueado)
        private LinkLabel _lnkEmergencia;

        public Login()
        {
            InitializeComponent();

            // ── Card flotante: esquinas redondeadas (Region) + sombra propia ──────
            // BringToFront es necesario: en el orden de InitializeComponent pnlCardShadow
            // queda "delante" de pnlCard en el z-order (el primer control agregado al form
            // es el que se pinta al frente), así que sin esto la sombra tapa la card entera.
            pnlCard.Region = new Region(BuildRoundedRect(new Rectangle(0, 0, pnlCard.Width, pnlCard.Height), Tema.RadioCard));
            pnlCardShadow.Paint += PnlCardShadow_Paint;
            pnlCard.BringToFront();

            // ── Botones grandes (Ingresar/Salir) con esquinas redondeadas tipo "pill" ─
            btnIngresar.Region = new Region(BuildRoundedRect(new Rectangle(0, 0, btnIngresar.Width, btnIngresar.Height), Tema.RadioBotonGrande));
            btnSalir.Region    = new Region(BuildRoundedRect(new Rectangle(0, 0, btnSalir.Width, btnSalir.Height), Tema.RadioBotonGrande));

            // ── Arrastre de la ventana desde el área vacía del header (sin borde nativo) ─
            pnlHeader.MouseDown    += IniciarArrastreVentana;
            picLogo.MouseDown      += IniciarArrastreVentana;
            lblSubtitulo.MouseDown += IniciarArrastreVentana;

            // ── Decoraciones dibujadas mediante eventos Paint ─────────────────────
            // Evita BackgroundImage bitmaps y permite que los controles transparentes
            // muestren correctamente el fondo del panel que los contiene.
            pnlLeft.Paint += PnlLeft_Paint;

            // ── Elementos de marca en el panel izquierdo + selector de idioma ─────
            AgregarBrandElements();
            AgregarComboIdioma();

            // ── Bordes redondeados propios de las cajas de usuario/contraseña ────
            pnlUsuarioBox.Paint    += DibujarBordeCampo;
            pnlContraseñaBox.Paint += DibujarBordeCampo;

            // ── Íconos dentro de los campos (persona / candado) ───────────────────
            AgregarIconoCampo(pnlUsuarioBox, "👤");
            AgregarIconoCampo(pnlContraseñaBox, "🔒");

            // ── Ojito mostrar/ocultar contraseña ─────────────────────────────────
            // Se achica el textbox para que el botón quede en el borde derecho de la caja.
            txtContraseña.Width -= 24;
            var btnOjo = new Button
            {
                Text      = "👁",
                Font      = new Font("Segoe UI Emoji", 9f),
                Size      = new Size(24, txtContraseña.Height + 4),
                Location  = new Point(txtContraseña.Right + 2, txtContraseña.Top - 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Tema.Papel,
                ForeColor = Tema.TextoMuted,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnOjo.FlatAppearance.BorderSize = 0;
            btnOjo.Click += (s, e) =>
            {
                var b = (Button)s;
                if (txtContraseña.PasswordChar == '\0')
                {
                    txtContraseña.PasswordChar = '●';
                    b.Font = new Font("Segoe UI Emoji", 9f);
                }
                else
                {
                    txtContraseña.PasswordChar = '\0';
                    b.Font = new Font("Segoe UI Emoji", 9f, FontStyle.Strikeout);
                }
            };
            pnlContraseñaBox.Controls.Add(btnOjo);
            btnOjo.BringToFront();

            // ── Líneas del separador "o" dibujadas vía Paint ──────────────────────
            lblDivider.Paint += LblDivider_Paint;

            // ── Link de autodesbloqueo con clave de emergencia (RF-10) ────────────
            // Fila propia entre "¿Olvidaste tu contraseña?" y el cartel de error,
            // para que el link NO se superponga con el mensaje "cuenta bloqueada".
            _lnkEmergencia = new LinkLabel
            {
                Text            = "¿Cuenta bloqueada? Usar clave de emergencia",
                AutoSize        = false,
                TextAlign       = ContentAlignment.MiddleLeft,
                Location        = new Point(lnkOlvidaste.Left, lnkOlvidaste.Bottom + 4),
                Size            = new Size(lnkOlvidaste.Width, 18),
                BackColor       = Color.Transparent,
                Font            = new Font("Segoe UI", 8.25f),
                LinkColor       = Tema.RosaOscuro,
                ActiveLinkColor = Tema.RosaOscuro,
                Tag             = "emg.link"
            };
            _lnkEmergencia.LinkClicked += LnkEmergencia_LinkClicked;
            (lnkOlvidaste.Parent ?? (Control)this).Controls.Add(_lnkEmergencia);
            _lnkEmergencia.BringToFront();

            this.AcceptButton = btnIngresar;
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

        // Ícono no interactivo (persona / candado) a la izquierda de una caja de campo.
        private void AgregarIconoCampo(Panel box, string glifo)
        {
            var icono = new Label
            {
                Text      = glifo,
                Font      = new Font("Segoe UI Emoji", 11f),
                ForeColor = Tema.TextoMuted,
                BackColor = Tema.Papel,
                Location  = new Point(10, 10),
                Size      = new Size(26, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            box.Controls.Add(icono);
            icono.SendToBack();
        }

        // ── Elementos de marca en pnlLeft ─────────────────────────────────────────

        private void AgregarBrandElements()
        {
            // 1. Isotipo + wordmark "WardrobeFlow" — asset 01 (picLogo, declarado en el Designer).
            //    Reemplaza al ícono de percha dibujado a mano; lblTitle queda oculto pero intacto.
            picLogo.BringToFront();

            // 2. Tagline "ORGANIZÁ • GESTIONÁ • POTENCIÁ"
            _lblTagline = new Label
            {
                Location  = new Point(32, 76),
                Size      = new Size(340, 20),
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Tema.RosaOscuro,
                BackColor = Tema.RosaPalido,
                Text      = "ORGANIZÁ • GESTIONÁ • POTENCIÁ",
                Tag       = "lbl.tagline"
            };
            pnlLeft.Controls.Add(_lblTagline);

            // 3. Wordmark "Wardrobe" / "Flow" en dos líneas y dos colores (labels simples)
            var lblWordmarkDark = new Label
            {
                Location  = new Point(28, 112),
                Size      = new Size(340, 58),
                Font      = new Font("Segoe UI", 34f),
                ForeColor = Tema.Tinta,
                BackColor = Tema.RosaPalido,
                Text      = "Wardrobe",
                AutoSize  = false
            };
            var lblWordmarkVino = new Label
            {
                Location  = new Point(28, 172),
                Size      = new Size(340, 58),
                Font      = new Font("Segoe UI", 34f),
                ForeColor = Tema.RosaOscuro,
                BackColor = Tema.RosaPalido,
                Text      = "Flow",
                AutoSize  = false
            };
            pnlLeft.Controls.Add(lblWordmarkDark);
            pnlLeft.Controls.Add(lblWordmarkVino);
            lblWordmarkDark.BringToFront();
            lblWordmarkVino.BringToFront();

            // 4. Descripción de marca (traducible vía tag)
            _lblBrandDesc = new Label
            {
                Location  = new Point(32, 246),
                Size      = new Size(340, 66),
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Tema.TextoMuted,
                BackColor = Tema.RosaPalido,
                Text      = "Gestioná tu flujo de\nprendas e información\nde manera simple y eficiente.",
                Tag       = "lbl.brand.desc",
                AutoSize  = false
            };
            pnlLeft.Controls.Add(_lblBrandDesc);
            _lblBrandDesc.BringToFront();

            // 5. Ilustración (percha con prendas, cartel de precio, planta y bolso) — assets
            //    03/04/05, declarados en el Designer como picClothingRack/picPriceTag/picPlantAndBag.
            picClothingRack.BringToFront();
            picPriceTag.BringToFront();
            picPlantAndBag.BringToFront();
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

        private void AgregarComboIdioma()
        {
            _cmbIdiomaLogin = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size          = new Size(150, 26),
                Location      = new Point(729, 19),
                FlatStyle     = FlatStyle.Flat,
                Font          = new Font("Segoe UI", 9f),
                ForeColor     = Tema.RosaOscuro,
                BackColor     = Tema.Papel,
                TabStop       = false
            };
            _cmbIdiomaLogin.SelectedIndexChanged += CmbIdiomaLogin_Changed;
            pnlHeader.Controls.Add(_cmbIdiomaLogin);
            _cmbIdiomaLogin.BringToFront();
            ConstruirComboIdioma(Traductor.ObtenerIdiomas());
        }

        // Llena el combo con los idiomas (de BD o fallback). Un idioma nuevo aparece solo.
        private void ConstruirComboIdioma(IList<Idioma> idiomas)
        {
            _suprimirIdiomaChange = true;
            _cmbIdiomaLogin.DataSource    = null;
            _cmbIdiomaLogin.DisplayMember = "Nombre";
            _cmbIdiomaLogin.ValueMember   = "Id";
            _cmbIdiomaLogin.DataSource    = new List<Idioma>(idiomas);

            string cod = GestorIdioma.IdiomaActual?.Id ?? "ES";
            for (int i = 0; i < idiomas.Count; i++)
                if (string.Equals(idiomas[i].Id, cod, StringComparison.OrdinalIgnoreCase))
                {
                    _cmbIdiomaLogin.SelectedIndex = i;
                    break;
                }
            _suprimirIdiomaChange = false;
        }

        private void CmbIdiomaLogin_Changed(object sender, EventArgs e)
        {
            if (_suprimirIdiomaChange) return;
            var idioma = _cmbIdiomaLogin.SelectedItem as Idioma;
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
            var tagline = Tx(_lblTagline?.Tag?.ToString());
            if (tagline != null && _lblTagline != null) _lblTagline.Text = tagline;

            var desc = Tx(_lblBrandDesc?.Tag?.ToString());
            if (desc != null && _lblBrandDesc != null) _lblBrandDesc.Text = desc;

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

            var emg = Tx(_lnkEmergencia?.Tag?.ToString());
            if (emg != null && _lnkEmergencia != null) _lnkEmergencia.Text = emg;

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
