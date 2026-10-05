using System;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// PN03 — Menú "Imprimir" común a las pantallas de promociones: ofrece los documentos que
    /// la promoción seleccionada ya tiene guardados («Ficha de promoción», «Dictamen contable»,
    /// «Solicitud de baja», «Resolución de baja», «Constancia de descarte»). Solo consulta a la
    /// BLL qué objetos existen y los manda a imprimir; no decide nada del negocio.
    /// </summary>
    internal sealed class MenuDocumentosPromocion
    {
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly ToolStripMenuItem itFicha = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itDictamen = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itSolicitud = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itResolucion = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itDescarte = new ToolStripMenuItem();

        private readonly BLL.Interfaces.IPromocionService promocionBLL;
        private readonly Func<BE.Promocion> seleccionada;
        private readonly IWin32Window propietario;
        private readonly Action<Exception> alFallar;
        private readonly Func<string, string, string> tr;

        private BE.Promocion _promocion;
        private BE.DictamenContable _dictamen;
        private BE.SolicitudBajaPromocion _solicitud;
        private BE.PromocionHistorial _descarte;

        public MenuDocumentosPromocion(BLL.Interfaces.IPromocionService promocionBLL, Func<BE.Promocion> seleccionada,
                                       IWin32Window propietario, Action<Exception> alFallar, Func<string, string, string> tr)
        {
            this.promocionBLL = promocionBLL;
            this.seleccionada = seleccionada;
            this.propietario = propietario;
            this.alFallar = alFallar;
            this.tr = tr;

            menu.Items.AddRange(new ToolStripItem[] { itFicha, itDictamen, itSolicitud, itResolucion, itDescarte });
            itFicha.Click += (s, e) => Imprimir(() =>
                Exportacion.DocumentosPromocion.FichaPromocion(_promocion, promocionBLL.ObtenerHistorial(_promocion.IdPromocion)));
            itDictamen.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPromocion.DictamenContable(_promocion, _dictamen));
            itSolicitud.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPromocion.SolicitudBaja(_promocion, _solicitud));
            itResolucion.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPromocion.ResolucionBaja(_promocion, _solicitud));
            itDescarte.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPromocion.ConstanciaDescartePromocion(_promocion, _descarte));
            Traducir();
        }

        public void Traducir()
        {
            itFicha.Text = tr("doc.promo.ficha.titulo", "Ficha de promoción");
            itDictamen.Text = tr("doc.promo.dictamen.titulo", "Dictamen contable");
            itSolicitud.Text = tr("doc.promo.solicitud.titulo", "Solicitud de baja");
            itResolucion.Text = tr("menu.promo.resolucion", "Resolución de baja");
            itDescarte.Text = tr("doc.promo.constanciadescarte.titulo", "Constancia de descarte");
        }

        // Muestra el menú debajo del botón, habilitando solo los documentos que ya existen.
        public void Mostrar(Control boton)
        {
            _promocion = seleccionada();
            if (_promocion == null) return;
            try
            {
                _dictamen = promocionBLL.ObtenerUltimoDictamen(_promocion.IdPromocion);
                _solicitud = promocionBLL.ObtenerUltimaSolicitudBaja(_promocion.IdPromocion);
                _descarte = _promocion.Estado == BE.EstadoPromocion.Descartada
                    ? promocionBLL.ObtenerDescarte(_promocion.IdPromocion) : null;
            }
            catch (Exception ex) { alFallar(ex); return; }

            itDictamen.Enabled = _dictamen != null;
            itSolicitud.Enabled = _solicitud != null;
            itResolucion.Enabled = _solicitud != null && _solicitud.FueResuelta();
            itDescarte.Enabled = _descarte != null;
            menu.Show(boton, 0, boton.Height);
        }

        private void Imprimir(Func<Exportacion.ReporteExportable> armar)
        {
            try { Exportacion.DocumentosPromocion.Imprimir(armar(), propietario); }
            catch (Exception ex) { alFallar(ex); }
        }
    }
}
