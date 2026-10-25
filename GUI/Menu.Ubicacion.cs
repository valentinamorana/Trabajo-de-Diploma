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
    ///   • Sesión → Acerca de WardrobeFlow (AcercaDeForm): versión, servidor y base de datos en uso.
    ///     Lo ve cualquier usuario con sesión, sin patente (BLL.MapaNavegacion.ItemsSiempreVisibles).
    /// </summary>
    public partial class Menu
    {

        // Diálogos modales del menú abiertos (Nueva contratación, Backup, Mi Perfil…), en orden de
        // apertura: la barra muestra el último y, al cerrarse, vuelve al anterior o a la ventana activa.
        // Los informa FormBase (InformarDialogo); los diálogos de las ventanas hijas no entran.
        private readonly List<Form> _dialogosAbiertos = new List<Form>();
        private readonly HashSet<Form> _hijosObservados = new HashSet<Form>();
        private Timer _timerGuia;

        // Lo que tiene que mostrarse DESPUÉS de la guía de primer uso (ver DespuesDeLaGuia).
        private BLL.SecuenciaDespuesDeLaGuia _secuenciaGuia;

        // Lo que espera a que no haya un diálogo modal abierto (ver CuandoLaVentanaEsteLibre).
        private BLL.ColaHastaVentanaLibre _colaVentanaLibre;
        private Timer _timerVentanaLibre;

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        /// <summary>Arma la barra de estado y el ítem de la guía. Se llama una vez desde el constructor.</summary>
        private void InicializarUsabilidad()
        {
            // La barra «Usted está en» (barraUbicacion y sus cuatro etiquetas) y los ítems de Sesión
            // Ayuda (F1) · Ver guía de inicio · Acerca de WardrobeFlow están en el Diseñador (Menu.Designer.cs).

            // Lo encolado tras la guía tampoco se superpone a un aviso modal (p. ej. el del backup del
            // login, abierto cuando la guía no se muestra y la secuencia se resuelve en el acto).
            _colaVentanaLibre = new BLL.ColaHastaVentanaLibre(VentanaLibre);
            _secuenciaGuia = new BLL.SecuenciaDespuesDeLaGuia(CuandoLaVentanaEsteLibre);

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

        private void LblAyudaBarra_Click(object sender, EventArgs e)
            => Ayuda.AyudaEnLinea.Mostrar(this, ActiveMdiChild?.GetType().Name ?? "General", (ActiveMdiChild as FormBase)?.TextoAyudaAdicional);

        private void GuiaItem_Click(object sender, EventArgs e) => MostrarGuia(new BLL.GuiaPrimerUso());

        private void AcercaDeItem_Click(object sender, EventArgs e) => MostrarAcercaDe();

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
        /// Ejecuta <paramref name="accion"/> en el hilo de UI cuando la ventana principal quede libre:
        /// habilitada y sin un diálogo modal propio abierto (MessageBox del backup del login, la guía,
        /// Backup, Mi Perfil…); la misma condición con la que espera la guía. Si ya está libre, corre
        /// enseguida (por BeginInvoke); si no, se revisa cada 400 ms. Lo encolado respeta el orden.
        /// Con la ventana ya creada se puede llamar desde cualquier hilo; nunca lanza.
        /// </summary>
        internal void CuandoLaVentanaEsteLibre(Action accion)
        {
            if (accion == null || IsDisposed) return;
            try
            {
                if (IsHandleCreated) BeginInvoke(new Action(() => EncolarHastaVentanaLibre(accion)));
                else EncolarHastaVentanaLibre(accion);
            }
            catch (InvalidOperationException) { /* el menú se cerró mientras tanto */ }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[Menu] Acción tras los avisos: " + ex.Message); }
        }

        private void EncolarHastaVentanaLibre(Action accion)
        {
            if (IsDisposed) return;
            _colaVentanaLibre.Agregar(accion);
            if (_colaVentanaLibre.Procesar() || _timerVentanaLibre != null) return;
            _timerVentanaLibre = new Timer { Interval = 400 };
            _timerVentanaLibre.Tick += (s, e) =>
            {
                if (!IsDisposed && !_colaVentanaLibre.Procesar()) return;
                _timerVentanaLibre?.Stop();
                _timerVentanaLibre?.Dispose();
                _timerVentanaLibre = null;
            };
            _timerVentanaLibre.Start();
        }

        // Libre = la ventana está habilitada (un MessageBox o un ShowDialog la deshabilitan) y no hay
        // un formulario modal propio visible. La usan la guía y CuandoLaVentanaEsteLibre.
        private bool VentanaLibre() =>
            IsHandleCreated && IsWindowEnabled(Handle) && !OwnedForms.Any(f => f.Visible && f.Modal);

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
                // Los 6 reportes de decisión abren el mismo formulario: la ruta sale del ítem de SU reporte.
                var entradas = BLL.MapaNavegacion.EntradasDe(formulario);
                if (activa is ReporteNegocioForm reporte)
                    entradas = entradas.Where(p => p.ItemMenu == BLL.MenuVisibilidad.ItemDelReporte(reporte.Reporte)).ToList();
                var recorridos = entradas
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
        /// incluidos ayudaItem, guiaItem y acercaDeItem): se toma de ahí. Items.Find no sirve como única
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
                    if (!VentanaLibre()) return;
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

        // ── Acerca de WardrobeFlow ──────────────────────────────────────────────

        /// <summary>
        /// Sesión → Acerca de WardrobeFlow: diálogo modal con el Menú como dueño (así la barra "Usted
        /// está en" lo muestra mientras está abierto). Solo lectura: no necesita patente.
        /// </summary>
        private void MostrarAcercaDe()
        {
            try
            {
                using (var acercaDe = new AcercaDeForm())
                    acercaDe.ShowDialog(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[Menu] Acerca de WardrobeFlow: " + ex.Message);
            }
        }
    }
}
