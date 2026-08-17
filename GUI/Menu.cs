using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — Formulario Menú Principal (MDI Container).
    ///
    /// Al iniciarse, construye el menú dinámicamente según los permisos EFECTIVOS
    /// del usuario logueado, resueltos en el Login desde el árbol Composite
    /// (tabla PermisoRelacion — la única fuente de verdad de autorización).
    ///
    /// Roles del sistema (documento G04 — WardrobeFlow_Iteracion1.docx):
    ///
    ///   Administrador     → TODO: Inventario | Ventas | Administrar | Bitácora
    ///   Supervisor        → Bitácora
    ///   OperadorLogistico → Inventario (Prendas, Outfits, Categorias, Pedidos Realizados)
    ///
    /// Roles adicionales (implementación, no están en G04):
    ///   Vendedor             → Ventas (Clientes, Planes, Pedidos de Venta)
    ///   ControladorDeStock   → Inventario (Prendas, Stock)
    ///   OperadorDeInventario → Ventas (Pedidos Realizados)
    ///
    /// Los permisos se leen de BE.Usuario.Permisos via BLL.ObtenerUsuarioActivo().
    /// La GUI nunca accede directamente a Seguridad ni a DAL.
    ///
    /// PATRÓN OBSERVER — T05 Gestión de Múltiples Idiomas:
    ///   Implementa IIdiomaObserver. Se suscribe al GestorIdioma en Load
    ///   y se desuscribe en FormClosing. Al recibir UpdateLanguage() llama
    ///   a Traducir() que reasigna el .Text de todos los ítems del menú.
    ///   La barra de idiomas (ToolStrip con 3 botones) se construye en el constructor.
    /// </summary>
    public partial class Menu : Form, IIdiomaObserver
    {
        // Timer de verificación periódica de integridad (C — background check)
        private System.Windows.Forms.Timer _timerIntegridad;

        private bool _suprimirIdiomaMenu = false;
        private int _alertasCount = -1;

        // Helper de traducción con fallback (para ítems creados por código).
        private static string Tx(string key, string fallback)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(key) ? t[key].Texto : fallback;
        }
        // Usuario cargado en el constructor; reutilizado en OnLoad para no hacer dos SELECT
        private BE.Usuario _usuarioActivo;

        public Menu()
        {
            InitializeComponent();

            ReconstruirComboIdioma(Traductor.ObtenerIdiomas());

            // Obtener usuario activo via BLL (GUI nunca toca SessionManager directamente)
            _usuarioActivo = new BLL.Usuario().ObtenerUsuarioActivo();

            if (_usuarioActivo != null)
            {
                this.Text = "WardrobeFlow  —  " + _usuarioActivo.Username +
                            (_usuarioActivo.Perfil != null ? "  [" + _usuarioActivo.Perfil + "]" : "");

                // Cargar y aplicar las preferencias de UI del usuario (fuente/tamaño/tema).
                PreferenciasUI.Cargar(_usuarioActivo.Id);
                PreferenciasUI.Aplicar(this);
            }

            RefrescarTextoAlertas();

            // Construir menú dinámico según permisos del rol
            RegistroControles.Registrar(this);   // Etapa 4 (C1) — registra los ítems del menú para la pantalla de mapeo
            AplicarPermisos(_usuarioActivo?.Permisos);
        }

        // Abre el Centro de Alertas como hijo MDI (reusa la instancia abierta si existe).
        private void AlertasItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
                if (hijo is AlertasForm) { hijo.BringToFront(); return; }
            new AlertasForm { MdiParent = this }.Show();
            ActualizarBadgeAlertas(); // refrescar el badge tras consultar
        }

        // Compone el texto del ítem: icono + etiqueta traducida + (N) si hay alertas.
        private void RefrescarTextoAlertas()
        {
            string baseTxt = "🔔 " + Tx("mnu.alertas", "Alertas");
            alertasItem.Text      = _alertasCount > 0 ? $"{baseTxt} ({_alertasCount})" : baseTxt;
            alertasItem.ForeColor = _alertasCount > 0 ? Color.FromArgb(255, 235, 130) : Color.White;
        }

        // Calcula la cantidad de alertas en background (la lógica vive en BLL.PanelAlertas)
        // y actualiza el badge sin bloquear la UI.
        private void ActualizarBadgeAlertas()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                int n;
                try { n = new BLL.PanelAlertas().Contar(); } catch { n = 0; }
                try
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        _alertasCount = n;
                        RefrescarTextoAlertas();
                    }));
                }
                catch { }
            });
        }

        // Abre "Mi Perfil" como diálogo modal (preferencias del usuario en sesión).
        private void MiPerfil_Click(object sender, EventArgs e)
        {
            using (var f = new MiPerfilForm(_usuarioActivo))
                f.ShowDialog(this);
        }

        /// <summary>
        /// Genera un tile 160×148 con el monograma WF en patrón de ladrillos
        /// (filas alternadas desplazadas medio tile) sobre fondo rosa claro.
        /// El tile repite perfectamente en ambas direcciones sin cortes visibles.
        /// </summary>
        private static Bitmap GenerarTileWF()
        {
            const int TW = 80, TH = 74;
            var bmp = new Bitmap(TW * 2, TH * 2);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(252, 228, 235));
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                var tinta = Color.FromArgb(40, 180, 70, 100);

                using (var fontW = new Font("Georgia", 22, FontStyle.Bold, GraphicsUnit.Point))
                using (var fontF = new Font("Georgia", 18, FontStyle.Bold, GraphicsUnit.Point))
                using (var br    = new SolidBrush(tinta))
                {
                    // Fila 0 — sin offset (y = 0)
                    g.DrawString("W", fontW, br, 0,      0);
                    g.DrawString("F", fontF, br, 24,     20);
                    g.DrawString("W", fontW, br, TW,     0);
                    g.DrawString("F", fontF, br, TW + 24, 20);

                    // Fila 1 — offset medio tile (y = TH)
                    g.DrawString("W", fontW, br, TW / 2f,       TH);
                    g.DrawString("F", fontF, br, TW / 2f + 24,  TH + 20);
                    g.DrawString("W", fontW, br, TW / 2f + TW,  TH);
                    g.DrawString("F", fontF, br, TW / 2f + TW + 24, TH + 20);
                }
            }

            return bmp;
        }

        /// <summary>
        /// Muestra u oculta los ítems del menú según los permisos del usuario. La DECISIÓN
        /// (qué .Name queda visible) vive en BLL.MenuVisibilidad.Resolver — pura, testeable
        /// sin instanciar esta UI (Tests/MenuVisibilidadTests.cs) — este método solo la
        /// APLICA a los ToolStripMenuItem reales, buscándolos por .Name. Mismo criterio de
        /// separación que BLL.PanelAlertas (EvaluarAlertas puro / ObtenerAlertas con BD).
        ///
        /// Administrador ve todo el menú por bypass explícito (esAdmin), no porque la BD
        /// tenga las 12 patentes bien asignadas — mismo criterio que ManejadorSeguridad y
        /// BLL.PermisosAccion, para que los 3 puntos donde se chequea permiso sean consistentes.
        /// </summary>
        private void AplicarPermisos(List<BE.Permiso> permisos)
        {
            bool esAdmin = _usuarioActivo?.EsAdministrador == true;
            var nombresMenu = (permisos ?? new List<BE.Permiso>())
                .Where(p => !string.IsNullOrEmpty(p.NombreMenu))
                .Select(p => p.NombreMenu);

            var visibilidad = BLL.MenuVisibilidad.Resolver(nombresMenu, esAdmin);

            // Todo ToolStripMenuItem que BLL.MenuVisibilidad conoce por nombre — se busca por
            // .Name para no duplicar acá el mapeo patente→ítem (única fuente de verdad).
            var items = new ToolStripMenuItem[]
            {
                panelControlToolStripMenuItem, inventarioToolStripMenuItem,
                prendasToolStripMenuItem, outfitsToolStripMenuItem, categoriasToolStripMenuItem,
                clientesToolStripMenuItem, planesToolStripMenuItem,
                renovacionSuscripcionToolStripMenuItem, cobroSuscripcionToolStripMenuItem,
                pedidosVentaToolStripMenuItem, pedidosRealizadosToolStripMenuItem,
                usuariosToolStripMenuItem, perfilesToolStripMenuItem, idiomasToolStripMenuItem,
                historialUsuariosToolStripMenuItem, backupToolStripMenuItem, integridadToolStripMenuItem,
                adminUsuariosItem,
                bitSistemaToolStripMenuItem, bitNegocioToolStripMenuItem, reporteJornadaToolStripMenuItem,
                analisisAbandonoToolStripMenuItem,
                suscriptoresToolStripMenuItem, ventasToolStripMenuItem, bitacoraToolStripMenuItem,
                grpUsuarios, grpSistema, gestionToolStripMenuItem
            };
            // Fail-closed: si un .Name no está en la resolución (typo, ítem renombrado por el
            // Designer sin actualizar BLL.MenuVisibilidad), se OCULTA en vez de quedar con el
            // Visible=true default de WinForms — un ítem gobernado por permiso que desaparece
            // por error se nota enseguida; uno que queda expuesto sin permiso, no.
            foreach (var item in items)
                if (item != null)
                    item.Visible = visibilidad.TryGetValue(item.Name, out bool v) && v;
        }

        // ── Etapa 2 — Re-aplicación de seguridad EN VIVO (patrón ManejadorSeguridad de Stach) ──

        /// <summary>
        /// Re-resuelve los permisos efectivos del usuario en sesión desde la BD y re-aplica la
        /// visibilidad del menú, sin necesidad de cerrar sesión. Útil tras editar roles/permisos
        /// en "Gestión de Permisos": si se modificó el rol del usuario actual, su menú se actualiza
        /// al instante (los cambios sobre OTROS roles impactan a esos usuarios en su próximo login).
        /// </summary>
        public void RefrescarSeguridad()
        {
            if (!Seguridad.SessionManager.IsLoggedIn) return;
            try
            {
                var usuario  = Seguridad.SessionManager.GetInstance().Usuario;
                var permisos = new BLL.Familia().ObtenerPermisosEfectivos(usuario.Rol ?? usuario.Perfil);
                usuario.Permisos = permisos;   // _usuarioActivo es la MISMA referencia que la sesión
                AplicarPermisos(permisos);
                // Etapa 4 — re-aplicar también la seguridad a nivel de control en los forms abiertos.
                ManejadorSeguridad.ActualizarSeguridadFormulariosAbiertos(usuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Menu.RefrescarSeguridad] " + ex.Message);
            }
        }

        /// <summary>
        /// Localiza el Menú abierto (formulario MDI padre) y le re-aplica la seguridad. Seguro de
        /// llamar desde cualquier formulario hijo tras un cambio de permisos.
        /// </summary>
        public static void RefrescarSeguridadAbierta()
        {
            foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
            {
                if (f is Menu m) { m.RefrescarSeguridad(); return; }
            }
        }

        /// <summary>
        /// Cierra la sesión y reinicia la aplicación para volver al Login con estado limpio.
        /// </summary>
        private void cerrarSesionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ConfirmarCerrarSesion())
            {
                new BLL.Usuario().Logout(this.Text);
                Application.Restart();
            }
        }

        /// <summary>
        /// Diálogo de confirmación de cierre de sesión con botones traducidos al idioma activo.
        /// Reemplaza MessageBox.Show() cuyo "Yes"/"No" es siempre en inglés (Windows).
        /// </summary>
        private bool ConfirmarCerrarSesion()
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string key, string fallback) => t.ContainsKey(key) ? t[key].Texto : fallback;

            using (var dlg = new Form())
            {
                dlg.Text            = T("dlg.cerrarsesion.titulo", "Cerrar Sesión");
                dlg.ClientSize      = new Size(340, 126);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;

                var lbl = new Label
                {
                    Text      = T("dlg.cerrarsesion.msg", "¿Está seguro que desea cerrar la sesión?"),
                    Left = 16, Top = 20, Width = 308, Height = 44,
                    Font      = new System.Drawing.Font("Segoe UI", 9.5f),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };

                var btnSi = new Button
                {
                    Text         = T("btn.si", "Sí"),
                    Left = 84, Top = 76, Width = 76, Height = 30,
                    DialogResult = DialogResult.Yes,
                    BackColor    = Color.FromArgb(210, 100, 135),
                    ForeColor    = Color.White,
                    FlatStyle    = FlatStyle.Flat
                };
                btnSi.FlatAppearance.BorderSize = 0;

                var btnNo = new Button
                {
                    Text         = T("btn.no", "No"),
                    Left = 176, Top = 76, Width = 76, Height = 30,
                    DialogResult = DialogResult.No,
                    FlatStyle    = FlatStyle.Flat
                };

                dlg.Controls.AddRange(new Control[] { lbl, btnSi, btnNo });
                dlg.AcceptButton = btnSi;
                dlg.CancelButton = btnNo;

                return dlg.ShowDialog(this) == DialogResult.Yes;
            }
        }

        private void panelControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is DashboardForm || hijo is DashboardVendedor ||
                    hijo is DashboardControlStock || hijo is DashboardOperador || hijo is DashboardSupervisor)
                { hijo.BringToFront(); return; }
            }
            CrearDashboardDelRol().Show();
        }

        // Factory de dashboards — para agregar un rol nuevo insertá una entrada en el diccionario,
        // sin tocar CrearDashboardDelRol() (Open/Closed Principle).
        private static readonly Dictionary<string, Func<Form>> _dashboardFactory =
            new Dictionary<string, Func<Form>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Vendedor",             () => new DashboardVendedor()     },
                // OperadorDeInventario = mantenimiento de prendas/stock
                { "OperadorDeInventario", () => new DashboardControlStock() },
                // OperadorLogistico = pedidos/despacho
                { "OperadorLogistico",    () => new DashboardOperador()     },
            };

        private Form CrearDashboardDelRol()
        {
            string perfil   = _usuarioActivo?.Perfil ?? "";
            var    permisos = _usuarioActivo?.Permisos;
            // Gerentes / Auditor / Administrador caen al DashboardForm genérico (se adapta por permisos).
            Form dash = _dashboardFactory.TryGetValue(perfil, out Func<Form> factory)
                ? factory()
                : new DashboardForm(permisos);
            dash.MdiParent = this;
            return dash;
        }

        private void bitSistemaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is Bitacora b) { b.SeleccionarTab("sistema"); b.BringToFront(); return; }
            }
            new Bitacora("sistema") { MdiParent = this }.Show();
        }

        private void bitNegocioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is Bitacora b) { b.SeleccionarTab("negocio"); b.BringToFront(); return; }
            }
            new Bitacora("negocio") { MdiParent = this }.Show();
        }

        private void reporteJornadaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is ReporteJornadaForm) { hijo.BringToFront(); return; }
            }
            new ReporteJornadaForm(_usuarioActivo?.Permisos) { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el módulo de Análisis de Abandono (PdN10) como hijo MDI.
        /// </summary>
        private void analisisAbandonoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is AnalisisAbandonoForm) { hijo.BringToFront(); return; }
            }
            new AnalisisAbandonoForm { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre Gestión de Usuarios como hijo MDI. Accesible solo para Administrador.
        /// </summary>
        private void usuariosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is Usuarios) { hijo.BringToFront(); return; }
            }
            new Usuarios { MdiParent = this }.Show();
        }

        // Abre el panel de Administración de Usuarios (ABM de datos no sensibles + cambiar rol + historial).
        private void AdminUsuarios_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is AdministracionUsuariosForm) { hijo.BringToFront(); return; }
            }
            new AdministracionUsuariosForm { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el Gestor de Perfiles y Permisos como hijo MDI — T04 Composite Pattern.
        /// Accesible solo para Administrador.
        /// </summary>
        private void perfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is GestorPermisos) { hijo.BringToFront(); return; }
            }
            new GestorPermisos { MdiParent = this }.Show();
        }

        private void idiomasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is FormIdiomas) { hijo.BringToFront(); return; }
            }
            new FormIdiomas { MdiParent = this }.Show();
        }

        private void historialUsuariosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is VersionHistorialForm) { hijo.BringToFront(); return; }
            }
            new VersionHistorialForm { MdiParent = this }.Show();
        }

        private void backupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new BackupForm())
                form.ShowDialog(this);
        }

        /// <summary>
        /// Abre el módulo de Prendas como hijo MDI.
        /// </summary>
        private void prendasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is Prendas) { hijo.BringToFront(); return; }
            }
            new Prendas { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el módulo de Outfits como hijo MDI.
        /// TODO: implementar cuando se cree el formulario Outfits.
        /// </summary>
        private void outfitsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tM = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T_m(string k, string fb) => tM.ContainsKey(k) ? tM[k].Texto : fb;
            MessageBox.Show(
                T_m("msg.modulo.outfits",    "El módulo de Outfits aún no está disponible."),
                T_m("lbl.proximamente",       "Próximamente"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Abre el módulo de Categorías como hijo MDI.
        /// TODO: implementar cuando se cree el formulario Categorias.
        /// </summary>
        private void categoriasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tM = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T_m(string k, string fb) => tM.ContainsKey(k) ? tM[k].Texto : fb;
            MessageBox.Show(
                T_m("msg.modulo.categorias", "El módulo de Categorías aún no está disponible."),
                T_m("lbl.proximamente",       "Próximamente"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Abre el módulo de Clientes como hijo MDI. Accesible para Vendedor.
        /// </summary>
        private void clientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is Clientes) { hijo.BringToFront(); return; }
            }
            new Clientes { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el módulo de Planes de Suscripción como hijo MDI.
        /// </summary>
        private void planesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is Planes) { hijo.BringToFront(); return; }
            }
            new Planes { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el módulo de Renovación de Suscripción (PdN5) como hijo MDI.
        /// </summary>
        private void renovacionSuscripcionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is RenovacionSuscripcionForm) { hijo.BringToFront(); return; }
            }
            new RenovacionSuscripcionForm { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el módulo de Cobro de Suscripción (PdN6) como hijo MDI.
        /// </summary>
        private void cobroSuscripcionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is CobroSuscripcionForm) { hijo.BringToFront(); return; }
            }
            new CobroSuscripcionForm { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el módulo de Pedidos de Venta como hijo MDI. Accesible para Vendedor.
        /// </summary>
        private void pedidosVentaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is PedidosVenta) { hijo.BringToFront(); return; }
            }
            new PedidosVenta { MdiParent = this }.Show();
        }

        /// <summary>
        /// Abre el módulo de Pedidos Realizados como hijo MDI.
        /// Accesible para OperadorDeInventario (mnuPedidosRealizados).
        /// </summary>
        private void pedidosRealizadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is PedidosRealizados) { hijo.BringToFront(); return; }
            }
            new PedidosRealizados { MdiParent = this }.Show();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PATRÓN OBSERVER — T05 Gestión de Múltiples Idiomas
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Suscribe este formulario al GestorIdioma al abrirse.
        /// Equivalente a frmMain_Load → ManejadorDeSesion.SuscribirObservador(this)
        /// del ejemplo de cátedra.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 1. Fondo MDI (solo GDI, sin BD)
            foreach (Control c in Controls)
            {
                if (c.GetType().Name == "MdiClient")
                {
                    c.BackColor             = Color.FromArgb(252, 228, 235);
                    c.BackgroundImage       = GenerarTileWF();
                    c.BackgroundImageLayout = ImageLayout.Tile;
                    break;
                }
            }

            // 2. Observer + timer de integridad
            GestorIdioma.SuscribirObservador(this);
            _timerIntegridad = new System.Windows.Forms.Timer { Interval = 30 * 60 * 1000 };
            _timerIntegridad.Tick += TimerIntegridad_Tick;
            _timerIntegridad.Start();

            // 3. Abrir dashboard inmediatamente — sus datos cargan en background
            var dash = CrearDashboardDelRol();
            dash.Show();
            try { PreferenciasUI.Aplicar(dash); } catch { }

            // Calcular el badge del Centro de Alertas (en background, vía BLL).
            ActualizarBadgeAlertas();

            // 4. Cargar traducciones de BD en background (no bloquea la UI)
            // RF-22/23 — Al ingresar se aplica el idioma PREFERIDO del usuario (persistido en BD).
            // El login queda en el idioma elegido en esa pantalla; el Menú ya abre en el del usuario.
            string codigoPref = _usuarioActivo?.IdIdioma ?? GestorIdioma.IdiomaActual?.Id ?? "ES";
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var svc      = new BLL.IdiomaService();
                    var dictTrad = svc.CargarTraducciones(codigoPref);
                    var idiomas  = svc.ObtenerIdiomasActivosComoIdioma();

                    this.BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        if (idiomas.Count > 0)
                        {
                            GestorIdioma.SetIdiomasDisponibles(idiomas);
                            ReconstruirComboIdioma(idiomas);
                        }
                        foreach (var idm in Traductor.ObtenerIdiomas())
                        {
                            if (idm.Id != codigoPref) continue;
                            GestorIdioma.CambiarIdioma(idm, dictTrad);
                            SeleccionarIdiomaEnCombo(codigoPref);
                            break;
                        }
                    }));
                }
                catch
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (!IsDisposed) Traducir(GestorIdioma.IdiomaActual);
                    }));
                }
            });
        }

        /// <summary>
        /// Desuscribe este formulario del GestorIdioma al cerrarse.
        /// Equivalente a frmMain_FormClosing → ManejadorDeSesion.DesuscribirObservador(this)
        /// del ejemplo de cátedra.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timerIntegridad?.Stop();
            _timerIntegridad?.Dispose();
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        private void TimerIntegridad_Tick(object sender, EventArgs e)
        {
            try
            {
                var diag = BLL.Configuracion.ObtenerDiagnostico();

                // Loguear resultado via BLL (GUI no accede a DAL directamente)
                BLL.Configuracion.RegistrarVerificacionPeriodica(diag);

                if (!diag.Integro)
                {
                    var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                    string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

                    MessageBox.Show(
                        string.Format(
                            T("alerta.integridad.msg",
                              "ALERTA: Se detectaron problemas de integridad en la tabla Usuario.\n\n" +
                              "Filas con DVH inválido: {0}\n" +
                              "DVV almacenado: {1}  |  DVV calculado: {2}\n\n" +
                              "Vaya a Administrar → Diagnóstico de Integridad para reparar."),
                            diag.FilasRotas.Count,
                            diag.DVVAlmacenado?.ToString() ?? "—",
                            diag.DVVCalculado),
                        T("alerta.integridad.titulo", "Alerta de Integridad"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Menu.TimerIntegridad] Error: {ex.Message}");
            }
        }

        private void integridadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form hijo in this.MdiChildren)
            {
                if (hijo is DiagnosticoIntegridadForm) { hijo.BringToFront(); return; }
            }
            new DiagnosticoIntegridadForm { MdiParent = this }.Show();
        }

        /// <summary>
        /// Recibe la notificación del GestorIdioma cuando el idioma cambia.
        /// Equivalente a UpdateLanguage(IIdioma idioma) del ejemplo de cátedra.
        /// </summary>
        public void UpdateLanguage(Idioma idioma)
        {
            // Reconstruir el combo por si cambió el CONJUNTO de idiomas disponibles
            // (p. ej. el admin activó/creó un idioma en Gestión de Idiomas): así el nuevo
            // idioma aparece al instante en el dropdown, sin reiniciar la aplicación.
            if (GestorIdioma.IdiomasDisponibles != null && GestorIdioma.IdiomasDisponibles.Count > 0)
                ReconstruirComboIdioma(GestorIdioma.IdiomasDisponibles);
            Traducir(idioma);
            SeleccionarIdiomaEnCombo(idioma.Id);
        }

        /// <summary>
        /// Reasigna el .Text de cada ítem del menú leyendo su propiedad Tag como
        /// clave de traducción — exactamente igual que en el ejemplo de cátedra (frmMain).
        /// Los Tags se asignan en el Designer; el código no hardcodea ninguna clave.
        /// </summary>
        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);

            // Label dinámico "Idioma:" / "Language:" / "Язык:"
            if (t.ContainsKey("lbl.idioma"))
                lblIdioma.Text = t["lbl.idioma"].Texto;

            Aplicar(usuarioToolStripMenuItem,           t);
            Aplicar(miPerfilItem,                       t);
            Aplicar(panelControlToolStripMenuItem,      t);
            Aplicar(inventarioToolStripMenuItem,        t);
            Aplicar(prendasToolStripMenuItem,           t);
            Aplicar(suscriptoresToolStripMenuItem,      t);
            Aplicar(ventasToolStripMenuItem,            t);
            Aplicar(clientesToolStripMenuItem,          t);
            Aplicar(planesToolStripMenuItem,            t);
            Aplicar(cobroSuscripcionToolStripMenuItem,  t);
            Aplicar(pedidosVentaToolStripMenuItem,      t);
            Aplicar(pedidosRealizadosToolStripMenuItem, t);
            Aplicar(gestionToolStripMenuItem,           t);
            Aplicar(grpUsuarios,                        t);
            Aplicar(grpSistema,                         t);
            Aplicar(usuariosToolStripMenuItem,          t);
            Aplicar(adminUsuariosItem,                  t);
            Aplicar(perfilesToolStripMenuItem,          t);
            Aplicar(idiomasToolStripMenuItem,           t);
            Aplicar(historialUsuariosToolStripMenuItem, t);
            Aplicar(backupToolStripMenuItem,            t);
            Aplicar(integridadToolStripMenuItem,        t);
            Aplicar(bitacoraToolStripMenuItem,          t);
            Aplicar(bitSistemaToolStripMenuItem,        t);
            Aplicar(bitNegocioToolStripMenuItem,        t);
            Aplicar(reporteJornadaToolStripMenuItem,    t);
            Aplicar(analisisAbandonoToolStripMenuItem,  t);
            Aplicar(cerrarSesionToolStripMenuItem,      t);
            Aplicar(ventanaToolStripMenuItem,           t);

            // Ítem de Alertas: tiene icono + badge, se compone aparte (no por Tag directo).
            RefrescarTextoAlertas();
        }

        /// <summary>
        /// Lee el Tag del ítem para obtener la clave y aplica la traducción.
        /// Equivalente al patrón if (item.Tag != null && traducciones.ContainsKey(...))
        /// del ejemplo de cátedra — el Tag actúa como clave del diccionario.
        /// </summary>
        private static void Aplicar(ToolStripMenuItem item,
            IDictionary<string, Traduccion> t)
        {
            if (item != null && item.Tag != null && t.ContainsKey(item.Tag.ToString()))
                item.Text = t[item.Tag.ToString()].Texto;
        }

        // ── Helpers de la barra de idioma (dropdown) ──────────────────────────

        /// <summary>
        /// Llena el combo del ToolStrip con los idiomas (de BD o fallback). Un idioma nuevo
        /// aparece solo, sin tocar este código.
        /// </summary>
        private void ReconstruirComboIdioma(IList<Idioma> idiomas)
        {
            _suprimirIdiomaMenu = true;
            cmbIdiomaMenu.ComboBox.DataSource    = null;
            cmbIdiomaMenu.ComboBox.DisplayMember = "Nombre";
            cmbIdiomaMenu.ComboBox.ValueMember   = "Id";
            cmbIdiomaMenu.ComboBox.DataSource    = new List<Idioma>(idiomas);
            _suprimirIdiomaMenu = false;
            SeleccionarIdiomaEnCombo(GestorIdioma.IdiomaActual?.Id ?? "ES");
        }

        /// <summary>Selecciona en el combo el idioma indicado, SIN disparar el cambio (uso interno).</summary>
        private void SeleccionarIdiomaEnCombo(string codigo)
        {
            if (cmbIdiomaMenu.ComboBox?.Items == null) return;
            for (int i = 0; i < cmbIdiomaMenu.ComboBox.Items.Count; i++)
                if (cmbIdiomaMenu.ComboBox.Items[i] is Idioma idm &&
                    string.Equals(idm.Id, codigo, StringComparison.OrdinalIgnoreCase))
                {
                    if (cmbIdiomaMenu.SelectedIndex != i)
                    {
                        bool prev = _suprimirIdiomaMenu;
                        _suprimirIdiomaMenu = true;
                        cmbIdiomaMenu.SelectedIndex = i;
                        _suprimirIdiomaMenu = prev;
                    }
                    break;
                }
        }

        /// <summary>
        /// Cambio de idioma desde el combo: aplica vía Observer (todos los forms se traducen)
        /// y persiste la preferencia del usuario en BD.
        /// </summary>
        private void CmbIdiomaMenu_Changed(object sender, EventArgs e)
        {
            if (_suprimirIdiomaMenu) return;
            var idioma = cmbIdiomaMenu.SelectedItem as Idioma;
            if (idioma == null) return;

            try
            {
                var dictTrad = new BLL.IdiomaService().CargarTraducciones(idioma.Id);
                GestorIdioma.CambiarIdioma(idioma, dictTrad);
            }
            catch { GestorIdioma.CambiarIdioma(idioma); }

            try
            {
                if (_usuarioActivo != null)
                    new BLL.Usuario().GuardarPreferenciaIdioma(_usuarioActivo.Id, idioma.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Menu] Error al guardar preferencia de idioma: {ex.Message}");
            }
        }

    }
}
