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

        // Operador Logístico: pedidos formalizados por etapa del despacho. Antes el panel
        // contaba los estados por su cuenta (DashboardLogistica.ActualizarCards).
        public BE.TableroLogistica ObtenerTableroLogistica() => ClasificarLogistica(_pedido.ObtenerTodos());

        /// <summary>
        /// Regla PURA del tablero logístico: solo pedidos formalizados (Pendiente de despacho,
        /// Despachado, Entregado), en orden de antigüedad. Los de armado y los cancelados quedan afuera.
        /// </summary>
        public static BE.TableroLogistica ClasificarLogistica(IEnumerable<BE.Pedido> pedidos)
        {
            var lista = (pedidos ?? Enumerable.Empty<BE.Pedido>()).Where(p => p != null).ToList();
            List<BE.Pedido> En(BE.EstadoPedido e) =>
                lista.Where(p => p.Estado == e).OrderBy(p => p.FechaPedido).ToList();

            return new BE.TableroLogistica
            {
                Pendientes  = En(BE.EstadoPedido.Pendiente),
                Despachados = En(BE.EstadoPedido.Despachado),
                Entregados  = En(BE.EstadoPedido.Entregado)
            };
        }

        /// <summary>
        /// Vendedor: suscripciones a gestionar (vencidas o que vencen en los próximos 7 días).
        /// La regla es el predicado BE.Cliente.RequiereGestionDeVencimiento — el mismo criterio
        /// de las alertas de suscripción (BLL.PanelAlertas); antes el panel lo repetía a mano.
        /// </summary>
        public static int ContarSuscripcionesAGestionar(IEnumerable<BE.Cliente> clientes) =>
            (clientes ?? Enumerable.Empty<BE.Cliente>()).Count(c => c != null && c.RequiereGestionDeVencimiento());
    }
}
