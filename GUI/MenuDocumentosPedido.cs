using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// PN01 — Menú "Documentos ▾" de Control de stock y Pedidos de venta (mismo esquema que
    /// MenuDocumentosPromocion). Los documentos del pedido ya no se ofrecen con una pregunta después
    /// de cada acción: se imprimen desde acá cuando hacen falta, habilitando solo los que existen
    /// según el estado del pedido. Solo consulta a la BLL y manda a imprimir; no decide nada del negocio.
    /// </summary>
    internal sealed class MenuDocumentosPedido
    {
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly ToolStripMenuItem itPlanilla = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itFaltantes = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itConfirmadas = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itSeparacion = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itConfirmacion = new ToolStripMenuItem();
        private readonly ToolStripMenuItem itDesistimiento = new ToolStripMenuItem();

        private readonly BLL.Interfaces.IPedidoService pedidoBLL;
        private readonly BLL.Interfaces.IClienteService clienteBLL;
        private readonly Func<BE.Pedido> seleccionado;
        private readonly Func<List<BE.LineaControlStock>> lineasControl;   // null fuera de Control de stock
        private readonly Func<string> modulo;
        private readonly IWin32Window propietario;
        private readonly Action<Exception> alFallar;
        private readonly Func<string, string, string> tr;

        private BE.Pedido _pedido;
        private List<BE.PedidoFaltante> _faltantes;

        public MenuDocumentosPedido(BLL.Interfaces.IPedidoService pedidoBLL, BLL.Interfaces.IClienteService clienteBLL,
                                    Func<BE.Pedido> seleccionado, Func<string> modulo, IWin32Window propietario, Action<Exception> alFallar,
                                    Func<string, string, string> tr, Func<List<BE.LineaControlStock>> lineasControl = null)
        {
            this.pedidoBLL = pedidoBLL;
            this.clienteBLL = clienteBLL;
            this.seleccionado = seleccionado;
            this.lineasControl = lineasControl;
            this.modulo = modulo;
            this.propietario = propietario;
            this.alFallar = alFallar;
            this.tr = tr;

            menu.Items.AddRange(new ToolStripItem[] { itPlanilla, itFaltantes, itConfirmadas, itSeparacion, itConfirmacion, itDesistimiento });
            itPlanilla.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPedido.PlanillaControl(
                _pedido, lineasControl?.Invoke(), clienteBLL.ObtenerPorId(_pedido.IdCliente)));
            itFaltantes.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPedido.InformeFaltantes(_pedido, _faltantes));
            itConfirmadas.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPedido.DetallePrendasConfirmadas(_pedido));
            itSeparacion.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPedido.ConstanciaSeparacion(_pedido));
            // "Preparar la confirmación" pasa por la BLL: valida el estado y lo asienta en la bitácora.
            itConfirmacion.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPedido.ConfirmacionPedido(
                pedidoBLL.PrepararConfirmacion(modulo(), _pedido.IdPedido)));
            itDesistimiento.Click += (s, e) => Imprimir(() => Exportacion.DocumentosPedido.AvisoDesistimiento(_pedido));
            Traducir();
        }

        public void Traducir()
        {
            itPlanilla.Text = tr("doc.planilla.titulo", "Planilla de control de existencias");
            itFaltantes.Text = tr("doc.faltantes.titulo", "Informe de disponibilidad (faltantes y alternativas)");
            itConfirmadas.Text = tr("doc.confirmadas.titulo", "Detalle de prendas confirmadas");
            itSeparacion.Text = tr("doc.separacion.titulo", "Constancia de prendas separadas");
            itConfirmacion.Text = tr("doc.confirmacion.titulo", "Confirmación y constancia del pedido");
            itDesistimiento.Text = tr("doc.desist.titulo", "Aviso de desistimiento");
        }

        // Muestra el menú debajo del botón con el pedido fresco de la BD (con sus prendas).
        public void Mostrar(Control boton)
        {
            var sel = seleccionado();
            if (sel == null) return;
            try
            {
                _pedido = pedidoBLL.ObtenerPorId(sel.IdPedido) ?? sel;
                _faltantes = pedidoBLL.ObtenerInformeFaltantes(_pedido.IdPedido) ?? new List<BE.PedidoFaltante>();
            }
            catch (Exception ex) { alFallar(ex); return; }

            itPlanilla.Enabled = _pedido.FechaEnvioControl.HasValue;
            itFaltantes.Enabled = _faltantes.Count > 0;
            itConfirmadas.Enabled = _pedido.FechaSeparacion.HasValue
                                    || (_pedido.Estado == BE.EstadoPedido.EnControlStock && _pedido.TodasConfirmadas);
            itSeparacion.Enabled = _pedido.FechaSeparacion.HasValue;
            itConfirmacion.Enabled = _pedido.EstaFormalizado();
            itDesistimiento.Enabled = _pedido.Estado == BE.EstadoPedido.Desistido;
            menu.Show(boton, 0, boton.Height);
        }

        private void Imprimir(Func<Exportacion.ReporteExportable> armar)
        {
            try { Exportacion.DocumentosPedido.Imprimir(armar(), propietario); }
            catch (Exception ex) { alFallar(ex); }
        }
    }
}
