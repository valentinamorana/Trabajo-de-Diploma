using System;
using System.Reflection;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// USABILIDAD centralizada en el formulario base (todo formulario la hereda sin escribir código):
    ///
    ///   • Tooltips desde las traducciones: por cada campo del formulario que sea un control (o un
    ///     ítem de ToolStrip) se busca la clave tip.&lt;Formulario&gt;.&lt;campo&gt; (BLL.MapaNavegacion.ClaveTooltip)
    ///     y, si existe, se asigna como tooltip. Agregar un tooltip = agregar la clave al .tsv en los
    ///     4 idiomas. Se re-aplican al cambiar de idioma (observador propio del GestorIdioma, así no
    ///     depende de que el hijo implemente IIdiomaObserver).
    ///   • Barra "Usted está en" del Menú: un diálogo modal abierto desde el Menú (ShowDialog(menu))
    ///     se informa al abrirse y al cerrarse, para que la barra muestre esa pantalla mientras dure.
    /// </summary>
    public partial class FormBase
    {
        private ToolTip _tipControles;
        private ObservadorTooltips _observadorTooltips;

        // Llamado al final de OnLoad. Nunca lanza: la usabilidad no puede impedir que se abra una pantalla.
        private void IniciarUsabilidad()
        {
            try { AplicarTooltips(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning($"[FormBase] Tooltips de {GetType().Name}: {ex.Message}"); }

            try
            {
                if (_observadorTooltips == null)
                {
                    _observadorTooltips = new ObservadorTooltips(this);
                    GestorIdioma.SuscribirObservador(_observadorTooltips);
                }
                // El Menú filtra: solo cuentan los diálogos que se abren desde el menú.
                if (!IsMdiChild && Owner is Menu menu) menu.InformarDialogo(this, abierto: true);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning($"[FormBase] Usabilidad de {GetType().Name}: {ex.Message}"); }
        }

        // Llamado en OnFormClosed (antes de base.OnFormClosed, que corre igual: try/finally en FormBase).
        private void FinalizarUsabilidad()
        {
            try
            {
                if (_observadorTooltips != null)
                {
                    GestorIdioma.DesuscribirObservador(_observadorTooltips);
                    _observadorTooltips = null;
                }
                if (!IsMdiChild && Owner is Menu menu) menu.InformarDialogo(this, abierto: false);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning($"[FormBase] Usabilidad de {GetType().Name}: {ex.Message}"); }
        }

        /// <summary>
        /// Asigna los tooltips tip.&lt;Formulario&gt;.&lt;campo&gt; del idioma activo. Se busca por el nombre del
        /// CAMPO (el identificador del Designer), no por Control.Name, que varios formularios no asignan.
        /// </summary>
        protected void AplicarTooltips()
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string formulario = GetType().Name;
            const BindingFlags campos = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            for (Type tipo = GetType(); tipo != null && tipo != typeof(FormBase); tipo = tipo.BaseType)
            {
                foreach (FieldInfo campo in tipo.GetFields(campos))
                {
                    if (!typeof(Control).IsAssignableFrom(campo.FieldType) &&
                        !typeof(ToolStripItem).IsAssignableFrom(campo.FieldType)) continue;

                    string clave = BLL.MapaNavegacion.ClaveTooltip(formulario, campo.Name);
                    if (!t.TryGetValue(clave, out Traduccion tr) || string.IsNullOrWhiteSpace(tr?.Texto)) continue;

                    switch (campo.GetValue(this))
                    {
                        case ToolStripItem item:
                            item.ToolTipText = tr.Texto;
                            if (item.Owner != null) item.Owner.ShowItemToolTips = true;
                            break;
                        case Control control:
                            if (_tipControles == null)
                            {
                                _tipControles = new ToolTip { AutoPopDelay = 15000, InitialDelay = 400, ReshowDelay = 200, ShowAlways = true };
                                Disposed += (s, e) => _tipControles.Dispose();
                            }
                            _tipControles.SetToolTip(control, tr.Texto);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Observador del GestorIdioma solo para los tooltips. Referencia débil al formulario: si un
        /// formulario se descarta sin cerrarse (Dispose sin Close), no queda retenido por la lista de
        /// observadores.
        /// </summary>
        private sealed class ObservadorTooltips : IIdiomaObserver
        {
            private readonly WeakReference<FormBase> _formulario;
            public ObservadorTooltips(FormBase formulario) { _formulario = new WeakReference<FormBase>(formulario); }

            public void UpdateLanguage(Idioma idioma)
            {
                if (!_formulario.TryGetTarget(out FormBase f) || f.IsDisposed)
                {
                    GestorIdioma.DesuscribirObservador(this);
                    return;
                }
                if (f.InvokeRequired) f.InvocarSeguro(f.AplicarTooltips);
                else f.AplicarTooltips();
            }
        }
    }
}
