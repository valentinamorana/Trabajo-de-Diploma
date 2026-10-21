using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// USABILIDAD del Menú principal (parte separada de Menu.cs para no mezclarla con permisos e idioma):
    ///
    ///   • Barra de estado "¿Dónde estoy?" al pie de la ventana: la ruta del menú de la pantalla
    ///     activa (Promociones → Centro de decisiones comerciales), una pista de una línea con lo que
    ///     se puede hacer ahí, el usuario con su rol y "F1: Ayuda". Se actualiza al activar una
    ///     ventana hija, al abrir/cerrar un diálogo del menú, al cerrar la última ventana (Inicio) y
    ///     al cambiar de idioma.
    ///     La ruta NO está escrita a mano: BLL.MapaNavegacion dice qué ítem abre cada formulario y
    ///     acá se sube por los OwnerItem del ToolStripMenuItem real, leyendo el texto traducido de
    ///     cada nivel. Si el Designer mueve un ítem de submenú, la ruta lo sigue sola.
    ///   • Guía de primer uso (GuiaPrimerUsoForm): se abre sola en el primer ingreso del usuario,
    ///     después del Panel de Control y de los avisos de arranque, y desde Sesión → Ver guía de inicio.
    /// </summary>
    public partial class Menu
    {
        private StatusStrip barraUbicacion;
        private ToolStripStatusLabel lblUbicacion;
        private ToolStripStatusLabel lblPista;
        private ToolStripStatusLabel lblUsuarioBarra;
        private ToolStripStatusLabel lblAyudaBarra;

        // Sesión → "Ver guía de inicio" (lo ven todos los roles, como Ayuda (F1)).
        private readonly ToolStripMenuItem guiaItem = new ToolStripMenuItem { Name = "guiaItem", Tag = "ux.mnu.guia", Text = "Ver guía de inicio" };

        // Diálogos modales del menú abiertos (Nueva contratación, Backup, Mi Perfil…), en orden de
        // apertura: la barra muestra el último y, al cerrarse, vuelve al anterior o a la ventana activa.
        // Los informa FormBase (InformarDialogo); los diálogos de las ventanas hijas no entran.
        private readonly List<Form> _dialogosAbiertos = new List<Form>();
        private readonly HashSet<Form> _hijosObservados = new HashSet<Form>();
        private Timer _timerGuia;

        // Lo que tiene que mostrarse DESPUÉS de la guía de primer uso (ver DespuesDeLaGuia).
        private BLL.SecuenciaDespuesDeLaGuia _secuenciaGuia;

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        /// <summary>Arma la barra de estado y el ítem de la guía. Se llama una vez desde el constructor.</summary>
        private void InicializarUsabilidad()
        {
            lblUbicacion = new ToolStripStatusLabel
            {
                Name = "lblUbicacion", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Tema.RosaTinta,
                Margin = new Padding(6, 3, 6, 2)
            };
            lblPista = new ToolStripStatusLabel
            {
                Name = "lblPista", Spring = true, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Tema.Tinta,
                BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched
            };
            lblUsuarioBarra = new ToolStripStatusLabel
            {
                Name = "lblUsuarioBarra", ForeColor = Tema.TextoSecundario,
                BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched
            };
            lblAyudaBarra = new ToolStripStatusLabel
            {
                Name = "lblAyudaBarra", IsLink = true, LinkColor = Tema.RosaOscuro, ActiveLinkColor = Tema.RosaPrimario,
                LinkBehavior = LinkBehavior.HoverUnderline, BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched, Margin = new Padding(0, 3, 6, 2)
            };
            lblAyudaBarra.Click += (s, e) => Ayuda.AyudaEnLinea.Mostrar(this,
                ActiveMdiChild?.GetType().Name ?? "General", (ActiveMdiChild as FormBase)?.TextoAyudaAdicional);

            barraUbicacion = new StatusStrip
            {
                Name = "barraUbicacion", Dock = DockStyle.Bottom, SizingGrip = false, ShowItemToolTips = true,
                BackColor = Tema.RosaMuyClara
            };
            barraUbicacion.Items.AddRange(new ToolStripItem[] { lblUbicacion, lblPista, lblUsuarioBarra, lblAyudaBarra });
            Controls.Add(barraUbicacion);

            guiaItem.Click += (s, e) => MostrarGuia(new BLL.GuiaPrimerUso());
            int idxAyuda = usuarioToolStripMenuItem.DropDownItems.IndexOf(ayudaItem);
            usuarioToolStripMenuItem.DropDownItems.Insert(idxAyuda >= 0 ? idxAyuda + 1 : 0, guiaItem);

            _secuenciaGuia = new BLL.SecuenciaDespuesDeLaGuia(accion =>
            {
                if (IsDisposed) return;
                if (IsHandleCreated) BeginInvoke(accion);
                else accion();
            });

            MdiChildActivate += (s, e) =>
            {
                try
                {
                    var hijo = ActiveMdiChild;
                    if (hijo != null && _hijosObservados.Add(hijo))
                        hijo.FormClosed += (s2, e2) =>
                        {
                            _hijosObservados.Remove(hijo);
                            // Al cerrarse la última ventana, ActiveMdiChild recién queda en null después.
                            ActualizarBarraUbicacionDiferida();
                        };
                }
                catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[Menu] Barra de ubicación: " + ex.Message); }
                ActualizarBarraUbicacion();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ActualizarBarraUbicacion();
            ProgramarGuiaDePrimerUso();
        }

        /// <summary>
        /// SECUENCIA DE INGRESO — ejecuta <paramref name="accion"/> justo DESPUÉS de la guía de primer
        /// uso: cuando el usuario la cierra o, si la guía no se va a mostrar (ya la descartó, no hay
        /// interfaz, /silent…), apenas se decide, siempre por BeginInvoke en el hilo de UI. Si se
        /// llama cuando la guía ya se resolvió, la acción se encola de inmediato.
        /// Pensado para los avisos de ingreso no modales (p. ej. el aviso comercial de PN03, que se
        /// engancha en OnLoad): <c>DespuesDeLaGuia(() =&gt; MostrarAvisoComercial());</c> así, en el
        /// primer ingreso, primero se lee la guía y después aparece el aviso. Se puede llamar desde
        /// el constructor u OnLoad: lo encolado espera a OnShown.
        /// </summary>
        internal void DespuesDeLaGuia(Action accion) => _secuenciaGuia?.Agregar(accion);

        /// <summary>
        /// Lo llama FormBase cuando un diálogo modal con este Menú como dueño se abre o se cierra.
        /// Solo cuentan los diálogos que se abren desde el menú (BLL.MapaNavegacion.EsDialogoDelMenu):
        /// uno abierto desde una ventana hija también tiene al Menú como dueño, pero no cambia la
        /// ubicación. Se apilan, así un diálogo dentro de otro restaura bien la barra al cerrarse.
        /// </summary>
        internal void InformarDialogo(Form dialogo, bool abierto)
        {
            try
            {
                if (dialogo == null || !BLL.MapaNavegacion.EsDialogoDelMenu(dialogo.GetType().Name)) return;
                _dialogosAbiertos.Remove(dialogo);
                if (abierto) _dialogosAbiertos.Add(dialogo);
                ActualizarBarraUbicacion();
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[Menu] Barra de ubicación: " + ex.Message); }
        }

        /// <summary>Recalcula la barra después de que todos los observadores del idioma terminaron.</summary>
        private void ActualizarBarraUbicacionDiferida()
        {
            try
            {
                if (IsHandleCreated && !IsDisposed) BeginInvoke(new Action(ActualizarBarraUbicacion));
                else ActualizarBarraUbicacion();
            }
            catch (InvalidOperationException) { /* el Menú se está cerrando */ }
        }

        /// <summary>
        /// Muestra dónde está el usuario, qué puede hacer ahí, quién es y cómo pedir ayuda. Nunca
        /// lanza: se llama en cada cambio de ventana y con textos editables desde Gestión de Idiomas.
        /// </summary>
        internal void ActualizarBarraUbicacion()
        {
            try { ActualizarBarraUbicacionSinProteger(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[Menu] No se pudo actualizar la barra de ubicación: " + ex.Message);
            }
        }

        private void ActualizarBarraUbicacionSinProteger()
        {
            if (barraUbicacion == null || IsDisposed) return;

            _dialogosAbiertos.RemoveAll(d => d == null || d.IsDisposed);
            Form activa = _dialogosAbiertos.Count > 0 ? _dialogosAbiertos[_dialogosAbiertos.Count - 1] : ActiveMdiChild;
            string formulario = activa?.GetType().Name;

            // Un solo diccionario para toda la barra (ObtenerTraducciones arma el merge BD + corpus).
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string clave, string fallback) => Traductor.Resolver(clave, fallback, null, t);

            string ruta;
            if (activa == null)
                ruta = T("ux.barra.inicio", "Inicio");
            else
            {
                var recorridos = BLL.MapaNavegacion.EntradasDe(formulario)
                    .Select(p => RecorridoDelItem(p.ItemMenu, t))
                    .Where(r => r != null)
                    .ToList();
                ruta = BLL.MapaNavegacion.ArmarRuta(recorridos, activa.Text);
            }

            string pista = T(BLL.MapaNavegacion.ClavePista(formulario), BLL.MapaNavegacion.PistaEs(formulario));

            const string UbicacionDeFabrica = "Usted está en: {0}", UsuarioDeFabrica = "{0} · {1}";
            lblUbicacion.Text        = BLL.MapaNavegacion.FormatoSeguro(T("ux.barra.ubicacion", UbicacionDeFabrica), UbicacionDeFabrica, ruta);
            lblUbicacion.ToolTipText = lblUbicacion.Text;
            lblPista.Text            = pista;
            lblPista.ToolTipText     = pista;

            if (_usuarioActivo != null)
            {
                string rol = RolTraducido(_usuarioActivo);
                lblUsuarioBarra.Text = string.IsNullOrEmpty(rol)
                    ? _usuarioActivo.Username
                    : BLL.MapaNavegacion.FormatoSeguro(T("ux.barra.usuario", UsuarioDeFabrica), UsuarioDeFabrica, _usuarioActivo.Username, rol);
            }
            else lblUsuarioBarra.Text = string.Empty;

            lblAyudaBarra.Text        = T("ux.barra.f1", "F1: Ayuda");
            lblAyudaBarra.ToolTipText = T("ux.barra.f1.tip", "Abre la ayuda de la pantalla activa.");
        }

        /// <summary>
        /// Niveles del menú, de la raíz a la hoja, del ítem con ese .Name (null si no está). Cada
        /// nivel usa la traducción de su Tag (sin el contador de Alertas ni el '&amp;' de acceso rápido).
        /// </summary>
        private IList<string> RecorridoDelItem(string nombreItem, IDictionary<string, Traduccion> t)
        {
            ToolStripItem encontrado = BuscarItem(menuStrip1.Items, nombreItem);
            if (encontrado == null) return null;

            var niveles = new List<string>();
            for (ToolStripItem it = encontrado; it != null; it = it.OwnerItem)
            {
                string tag   = it.Tag as string;
                string texto = !string.IsNullOrEmpty(tag) ? Traductor.Resolver(tag, it.Text, null, t) : it.Text;
                niveles.Insert(0, (texto ?? string.Empty).Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&"));
            }
            return niveles;
        }

        /// <summary>
        /// Ítem del menú con ese .Name. Cada ítem es un campo del Menú con el mismo nombre (Designer o
        /// creado por código, como ayudaItem/guiaItem): se toma de ahí. Items.Find no sirve como única
        /// vía porque no entra en los submenús cuyos ítems están ocultos por permisos.
        /// </summary>
        private ToolStripItem BuscarItem(ToolStripItemCollection items, string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            var campo = typeof(Menu).GetField(nombre,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (campo?.GetValue(this) is ToolStripItem item && string.Equals(item.Name, nombre, StringComparison.Ordinal))
                return item;
            ToolStripItem[] encontrados = items.Find(nombre, true);
            return encontrados.Length > 0 ? encontrados[0] : null;
        }

        /// <summary>Rol del usuario traducido (clave perm.rol.&lt;rol&gt;, la del título de la ventana).</summary>
        internal static string RolTraducido(BE.Usuario usuario)
        {
            string rol = usuario?.Perfil;
            if (string.IsNullOrEmpty(rol)) return string.Empty;
            string clave = "perm.rol." + rol.ToLowerInvariant().Replace(" ", "").Replace("(", "").Replace(")", "");
            return Tx(clave, rol);
        }

        // ── Guía de primer uso ──────────────────────────────────────────────────

        /// <summary>
        /// Programa la guía para cuando la ventana quede libre: si al arrancar hay un aviso modal
        /// abierto (backup del login, integridad…), la ventana principal está deshabilitada y se
        /// espera a que se cierre, así la guía nunca se superpone a otro diálogo.
        /// </summary>
        /// <remarks>
        /// Toda salida que NO muestra la guía resuelve la secuencia (DespuesDeLaGuia) en el acto; si
        /// la guía se muestra, la secuencia se resuelve al cerrarla.
        /// </remarks>
        private void ProgramarGuiaDePrimerUso()
        {
            bool programada = false;
            try
            {
                if (_usuarioActivo == null || !Environment.UserInteractive) return;
                if (BLL.GuiaPrimerUso.SuprimidaPorEntorno(Environment.GetCommandLineArgs().Skip(1),
                        Environment.GetEnvironmentVariable("WARDROBEFLOW_SIN_GUIA"))) return;

                var preferencia = new BLL.GuiaPrimerUso();
                if (!preferencia.DebeMostrarse(_usuarioActivo.Username)) return;

                _timerGuia = new Timer { Interval = 400 };
                _timerGuia.Tick += (s, e) =>
                {
                    if (IsDisposed) { _timerGuia.Stop(); return; }
                    if (!IsHandleCreated || !IsWindowEnabled(Handle) || OwnedForms.Any(f => f.Visible && f.Modal)) return;
                    _timerGuia.Stop();
                    _timerGuia.Dispose();
                    _timerGuia = null;
                    try { MostrarGuia(preferencia); }
                    finally { _secuenciaGuia.Resolver(); }
                };
                _timerGuia.Start();
                programada = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[Menu] Guía de primer uso: " + ex.Message);
            }
            finally
            {
                if (!programada) _secuenciaGuia.Resolver();
            }
        }

        private void MostrarGuia(BLL.GuiaPrimerUso preferencia)
        {
            if (_usuarioActivo == null) return;
            try
            {
                using (var guia = new GuiaPrimerUsoForm(_usuarioActivo, preferencia))
                    guia.ShowDialog(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[Menu] Guía de primer uso: " + ex.Message);
            }
        }
    }
}
