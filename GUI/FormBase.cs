using Servicios.Multiidioma;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// Formulario base de WardrobeFlow.
    ///
    /// PATRÓN HERENCIA (igual al ejemplo Vehículo/Auto/Moto de la cátedra):
    ///   Esta clase es como "Vehículo": define atributos y métodos comunes
    ///   que todos los formularios hijos heredan automáticamente.
    ///
    ///   Jerarquía:
    ///     FormBase : Form          ← como Vehiculo
    ///       ├── Clientes           ← como Auto
    ///       ├── Prendas            ← como Auto
    ///       ├── Planes             ← como Auto
    ///       ├── PedidosVenta       ← como Moto
    ///       ├── PedidosRealizados  ← como Moto
    ///       ├── NuevoPedidoForm    ← como Moto
    ///       └── Bitacora           ← como Moto
    ///
    /// MÉTODOS HEREDADOS (equivalentes a "acelerar()" en Vehículo):
    ///   MostrarOk(msg)    → feedback verde en el label del formulario
    ///   MostrarError(msg) → feedback rojo (o MessageBox si no hay label)
    ///
    /// PROPIEDAD VIRTUAL (equivalente a una propiedad sobreescribible):
    ///   MensajeLabel → cada hijo sobreescribe para devolver su lblMensaje.
    ///   Si no sobreescribe (como Bitacora), MostrarError usa MessageBox.
    /// </summary>
    public class FormBase : Form
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // PATRÓN OBSERVER (T05) centralizado: todo formulario que implementa
            // IIdiomaObserver queda suscripto al GestorIdioma al cargarse y se desuscribe en
            // OnFormClosed. Antes cada uno de los ~45 formularios repetía el par
            // Suscribir/Desuscribir, y desuscribir en OnFormClosing se perdía si el cierre se
            // cancelaba. Cada hijo sigue llamando a su propio Traducir() en su OnLoad.
            if (this is IIdiomaObserver observador)
                GestorIdioma.SuscribirObservador(observador);

            try
            {
                string ico = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(ico))
                    this.Icon = new Icon(ico);
            }
            catch { }

            // Aplica las preferencias de UI del usuario (fuente/tamaño/tema) a este formulario.
            try { PreferenciasUI.Aplicar(this); } catch { }

            // Etapa 4 — Registrar los controles de este form (C1) y aplicar la seguridad a nivel de
            // control: muestra/oculta los mapeados a patentes que el usuario no tiene. Sin mapeos, no hace nada.
            try
            {
                RegistroControles.Registrar(this);
                if (Seguridad.SessionManager.IsLoggedIn)
                    ManejadorSeguridad.AplicarSeguridad(this, Seguridad.SessionManager.GetInstance().Usuario);
            }
            catch (Exception ex)
            {
                // Sin mapeos no pasa nada (comportamiento normal), pero si ROMPE por otra razón
                // conviene que quede rastro: esto corre en OnLoad de CADA formulario de la app.
                System.Diagnostics.Trace.TraceWarning(
                    $"[FormBase.OnLoad] No se pudo aplicar seguridad de controles en {this.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Label donde se muestra el feedback al usuario.
        /// Cada formulario hijo sobreescribe esta propiedad devolviendo
        /// su propio control lblMensaje declarado en el Designer.
        ///
        /// Ejemplo en cada hijo:
        ///   protected override Label MensajeLabel => lblMensaje;
        ///
        /// Si un formulario no tiene lblMensaje (como Bitácora),
        /// MostrarError usa MessageBox como fallback automático.
        /// </summary>
        protected virtual Label MensajeLabel => null;

        /// <summary>
        /// Traduce una clave al idioma activo, con fallback si no existe. Único punto de acceso
        /// al diccionario de traducciones — reemplaza los ~84 helpers locales casi idénticos
        /// (`T`, `Tx`, `T_ce`, `Tv`...) que antes reinventaba cada formulario/método por su cuenta
        ///. Heredado por todos los formularios
        /// hijos — no necesitan redefinirlo.
        /// </summary>
        protected string Tr(string clave, string fallback, object[] args = null)
            => Traductor.Resolver(clave, fallback, args, GestorIdioma.IdiomaActual);

        /// <summary>
        /// Muestra un mensaje de operación exitosa (en verde).
        /// Heredado por todos los formularios hijos — no necesitan redefinirlo.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this is IIdiomaObserver observador)
                GestorIdioma.DesuscribirObservador(observador);
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Confirmación Sí/No con los botones traducidos al idioma activo (MessageBox usa
        /// el idioma de Windows: "Yes"/"No"). Disponible para todos los formularios hijos.
        /// </summary>
        protected bool ConfirmarSiNo(string texto, string titulo, bool porDefectoNo = false)
            => MostrarConfirmacionSiNo(this, texto, titulo, porDefectoNo);

        /// <summary>
        /// Implementación compartida de <see cref="ConfirmarSiNo"/>; estática para que la use
        /// también el Menú (MDI), que no hereda de FormBase.
        /// </summary>
        internal static bool MostrarConfirmacionSiNo(IWin32Window owner, string texto, string titulo,
                                                      bool porDefectoNo = false)
        {
            string T(string clave, string fallback)
                => Traductor.Resolver(clave, fallback, null, GestorIdioma.IdiomaActual);

            // El alto se adapta al texto: varias confirmaciones (cobro, cancelación) traen
            // un detalle de 5-6 líneas que no entraba en un cuadro fijo.
            const int anchoTexto = 380;
            Size medida = TextRenderer.MeasureText(texto ?? string.Empty, Tema.FuenteNormal,
                new Size(anchoTexto, int.MaxValue), TextFormatFlags.WordBreak);
            int altoTexto = Math.Max(44, Math.Min(medida.Height + 8, 420));

            using (var dlg = new Form())
            {
                dlg.Text            = titulo;
                dlg.ClientSize      = new Size(anchoTexto + 32, altoTexto + 76);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition   = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.ShowInTaskbar   = false;
                dlg.BackColor       = Color.White;

                var lbl = new Label
                {
                    Text      = texto,
                    Left = 16, Top = 14, Width = anchoTexto, Height = altoTexto,
                    Font      = Tema.FuenteNormal,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                int topBotones = altoTexto + 28;
                int centro     = (anchoTexto + 32) / 2;
                var btnSi = new Button
                {
                    Text         = T("btn.si", "Sí"),
                    Left = centro - 84, Top = topBotones, Width = 76, Height = 30,
                    DialogResult = DialogResult.Yes,
                    BackColor    = Tema.RosaPrimario,
                    ForeColor    = Color.White,
                    FlatStyle    = FlatStyle.Flat
                };
                btnSi.FlatAppearance.BorderSize = 0;

                var btnNo = new Button
                {
                    Text         = T("btn.no", "No"),
                    Left = centro + 8, Top = topBotones, Width = 76, Height = 30,
                    DialogResult = DialogResult.No,
                    FlatStyle    = FlatStyle.Flat
                };

                dlg.Controls.AddRange(new Control[] { lbl, btnSi, btnNo });
                // porDefectoNo: para acciones destructivas, Enter elige "No" (equivale al
                // MessageBoxDefaultButton.Button2 que usaban los MessageBox reemplazados).
                dlg.AcceptButton  = porDefectoNo ? btnNo : btnSi;
                dlg.CancelButton  = btnNo;
                dlg.ActiveControl = porDefectoNo ? btnNo : btnSi;

                return (owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog()) == DialogResult.Yes;
            }
        }

        protected void MostrarOk(string msg)
        {
            if (MensajeLabel == null) return;
            MensajeLabel.ForeColor = Tema.Exito;
            MensajeLabel.Text      = $"{msg}";
        }

        /// <summary>
        /// Muestra un mensaje de error (en rojo).
        /// Si el formulario no tiene lblMensaje, usa MessageBox como fallback.
        /// Heredado por todos los formularios hijos — no necesitan redefinirlo.
        /// </summary>
        protected void MostrarError(string msg)
        {
            if (MensajeLabel == null)
            {
                var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                string titulo = t.ContainsKey("msg.error.titulo") ? t["msg.error.titulo"].Texto : "Error";
                MessageBox.Show(msg, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MensajeLabel.ForeColor = Tema.Error;
            MensajeLabel.Text      = $"{msg}";
        }

        /// <summary>
        /// Sobrecarga que traduce AppException al idioma activo antes de mostrar.
        /// Para otras excepciones muestra ex.Message directamente.
        /// </summary>
        protected void MostrarError(Exception ex)
        {
            if (ex is BE.AppException appEx)
            {
                // AppException = error de negocio esperado (validación, permiso…): se
                // muestra traducido y NO se registra en bitácora para no generar ruido.
                string msg = Traductor.Resolver(appEx.Clave, ex.Message, appEx.Args, GestorIdioma.IdiomaActual);
                MostrarError(msg);
                return;
            }

            // Excepción INESPERADA: se registra en la bitácora (con el detalle técnico) y al
            // usuario se le muestra SOLO un mensaje GENÉRICO, sin exponer información técnica (#6).
            RegistrarExcepcion(ex);
            var tg = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string generico = tg.ContainsKey("msg.error.inesperado")
                ? tg["msg.error.inesperado"].Texto
                : "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.";
            MostrarError(generico);
        }

        // Registra una excepción inesperada en la bitácora con criticidad Alta.
        // Nunca propaga: si el logueo falla, no debe tapar el error original. Protected (no
        // private) para que formularios con una necesidad real de título de MessageBox
        // personalizado (ej. ReporteJornadaForm) puedan reusar el mismo criterio de auditoría
        // que MostrarError(Exception) sin reimplementarlo de cero.
        protected void RegistrarExcepcion(Exception ex)
        {
            try
            {
                var bitacora = new BLL.Bitacora();
                string modulo = this.GetType().Name;
                int?   idUsuario = Seguridad.SessionManager.IsLoggedIn
                                   ? (int?)Seguridad.SessionManager.GetInstance().Usuario.Id : null;
                string usuario = Seguridad.SessionManager.IsLoggedIn
                                   ? Seguridad.SessionManager.GetInstance().Usuario.Username : "(sin sesión)";
                bitacora.RegistrarSinSesion(
                    modulo,
                    "Excepción: " + ex.GetType().Name,
                    BE.Criticidad.Alta,
                    idUsuario,
                    $"Usuario '{usuario}' — {modulo}: {ex.Message}" +
                    (ex.InnerException != null ? $" | Causa: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : ""));
            }
            catch { /* el fallo al registrar no debe romper el manejo del error */ }
        }
    }
}
