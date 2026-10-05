using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Tareas de cada rol para su panel de inicio, tomadas de los diagramas de PN01 y PN02. La
    /// clasificación (qué estado es tarea de quién) vive acá; los paneles solo la muestran.
    /// </summary>
    public class PanelTareas
    {
        private readonly Interfaces.IPedidoService _pedido;
        private readonly Func<int> _contarContrataciones;

        public PanelTareas() : this(new Pedido(), () => new Contratacion().ContarPendientesDePago()) { }

        public PanelTareas(Interfaces.IPedidoService pedido, Func<int> contarContrataciones)
        {
            _pedido = pedido ?? throw new ArgumentNullException(nameof(pedido));
            _contarContrataciones = contarContrataciones ?? (() => 0);
        }

        // Vendedor: "Comunicar faltantes" (ConFaltantes) y "Formalizar el pedido" (Separado) son
        // suyas; EnControlStock se muestra para seguimiento (espera a Depósito). PN02: las
        // contrataciones que registró y esperan el cobro de Caja.
        public BE.TableroVendedor ObtenerTareasVendedor()
        {
            var pedidos = _pedido.ObtenerTodos();
            List<BE.Pedido> En(BE.EstadoPedido e) =>
                pedidos.Where(p => p.Estado == e).OrderBy(p => p.FechaPedido).ToList();

            int contrataciones = 0;
            try { contrataciones = _contarContrataciones(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning($"[BLL.PanelTareas] Contrataciones: {ex.Message}"); }

            return new BE.TableroVendedor
            {
                EnControlStock       = En(BE.EstadoPedido.EnControlStock),
                ConFaltantes         = En(BE.EstadoPedido.ConFaltantes),
                Separados            = En(BE.EstadoPedido.Separado),
                ContratacionesEnCaja = contrataciones
            };
        }

        // Depósito: "Revisar stock de las prendas" — pedidos en la cola de control de stock.
        public int ContarPedidosAControlar() => _pedido.ObtenerColaControlStock().Count;
    }
}
