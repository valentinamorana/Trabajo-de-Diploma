using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Entidad — Pedido de prendas generado por el Vendedor para un Cliente.
    /// Mapea la tabla [Pedido]. Las prendas del pedido se cargan por separado
    /// desde la tabla intermedia [PedidoPrenda].
    /// </summary>
    public class Pedido
    {
        public int IdPedido { get; set; }

        public int IdCliente { get; set; }

        public int IdEmpleado { get; set; }

        public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

        public DateTime FechaPedido { get; set; }
        public DateTime? FechaDespacho { get; set; }

        public DateTime? FechaEntrega { get; set; }

        // PN04 — "Registrar devolución": cuándo volvieron las prendas. El pedido sigue Entregado
        // (no hay estado nuevo, la máquina de estados no cambia); esta fecha es la que distingue un
        // pedido devuelto de uno con las prendas todavía en poder del cliente.
        public DateTime? FechaDevolucion { get; set; }

        public string MotivoCancelacion { get; set; }

        // ── PN01 — circuito de control de stock (diagrama de actividad) ─────────
        // "Enviar selección para control stock".
        public DateTime? FechaEnvioControl { get; set; }

        // "Revisar stock de las prendas": quién (Depósito) y cuándo emitió el resultado
        // (informe de faltantes o confirmación de las prendas disponibles).
        public DateTime? FechaControl { get; set; }
        public int? IdEmpleadoControl { get; set; }
        public string NombreEmpleadoControl { get; set; }

        // "Separar prendas del pedido" (constancia de prendas separadas).
        public DateTime? FechaSeparacion { get; set; }

        // "Formalizar el pedido": desde acá la selección no admite modificaciones.
        public DateTime? FechaFormalizacion { get; set; }

        // "Asentar desistimiento".
        public string MotivoDesistimiento { get; set; }
        public EtapaDesistimiento? EtapaDesistimiento { get; set; }

        public string NombreCliente { get; set; }

        public string NombreEmpleado { get; set; }

        // Prendas asociadas al pedido (cargadas desde PedidoPrenda).
        public List<Prenda> Prendas { get; set; } = new List<Prenda>();

        // Ids de las prendas que Depósito confirmó como disponibles (PedidoPrenda.Confirmada).
        public List<int> PrendasConfirmadas { get; set; } = new List<int>();

        public int CantidadPrendas => Prendas?.Count ?? 0;

        // Todas las líneas del pedido fueron confirmadas por Depósito.
        public bool TodasConfirmadas =>
            CantidadPrendas > 0 && Prendas.TrueForAll(p => PrendasConfirmadas.Contains(p.IdPrenda));

        // Comportamiento
        // Días transcurridos desde que se generó el pedido — usado por los dashboards de
        // rol (Kanban) para mostrar antigüedad y decidir el color de la tarjeta, en vez de
        // que cada dashboard recalcule "DateTime.Today - FechaPedido" por su cuenta.
        public int DiasDesdeAlta => (int)(DateTime.Today - FechaPedido.Date).TotalDays;

        // Umbral de antigüedad que los dashboards usan para resaltar un pedido Pendiente
        // sin atender. Centralizado acá para que Vendedor/Logística/Gerencia coincidan
        // siempre en el mismo criterio (antes duplicado con el mismo número "mágico" en
        // los 3 dashboards, sin una única fuente de verdad).
        public bool EsUrgentePorAntiguedad => DiasDesdeAlta >= 2;

        // El pedido puede cancelarse si está Pendiente (formalizado, sin despachar), Separado
        // (Depósito ya reservó las prendas pero todavía no se formalizó: "Cancelar pedido separado")
        // o En Control de Stock (todavía sin prendas reservadas): si mientras espera a Depósito el
        // cliente vence, se suspende o se arrepiente, el pedido no queda trabado en la cola.
        public bool PuedeCancelarse() => Estado == EstadoPedido.Pendiente || Estado == EstadoPedido.Separado
                                      || Estado == EstadoPedido.EnControlStock;

        // El pedido puede despacharse solo si está Pendiente (formalizado).
        public bool PuedeDespachar() => Estado == EstadoPedido.Pendiente;

        // El pedido puede marcarse como entregado solo si está Despachado.
        public bool PuedeEntregarse() => Estado == EstadoPedido.Despachado;

        // El pedido puede des-cancelarse solo si está Cancelado.
        public bool PuedeDesCancelarse() => Estado == EstadoPedido.Cancelado;

        // Registrar la devolución (PN04): solo de un pedido entregado que todavía no se devolvió.
        public bool PuedeDevolverse() => Estado == EstadoPedido.Entregado && !FechaDevolucion.HasValue;

        // PN04 — el pedido ya se devolvió (sigue en estado Entregado, con la fecha de devolución).
        public bool FueDevuelto => FechaDevolucion.HasValue;

        // PN04 — entregado y sin devolver: las prendas siguen en poder del cliente.
        public bool EnPoderDelCliente => Estado == EstadoPedido.Entregado && !FechaDevolucion.HasValue;

        // Días que lleva el cliente con las prendas (desde la entrega). Null si no están en su poder.
        public int? DiasEnPoderDelCliente(DateTime hoy) =>
            EnPoderDelCliente && FechaEntrega.HasValue
                ? (int?)Math.Max(0, (int)(hoy.Date - FechaEntrega.Value.Date).TotalDays)
                : null;

        // Hay notificación de envío desde que se despachó.
        public bool TieneNotificacionEnvio() => Estado == EstadoPedido.Despachado || Estado == EstadoPedido.Entregado;

        // Ciclo logístico (Pedidos Realizados): pedidos formalizados en adelante. Los que siguen en
        // el armado de PN01 (control de stock, faltantes, separados) o desistidos no son para despachar.
        public bool EsDelCicloLogistico() =>
            Estado == EstadoPedido.Pendiente || Estado == EstadoPedido.Despachado ||
            Estado == EstadoPedido.Entregado || Estado == EstadoPedido.Cancelado;

        // ── PN01 ──────────────────────────────────────────────────────────────
        // Depósito revisa el stock, informa faltantes o confirma prendas: solo en control.
        public bool PuedeControlarse() => Estado == EstadoPedido.EnControlStock;

        // Separar prendas: en control y con todas las líneas confirmadas.
        public bool PuedeSepararse() => Estado == EstadoPedido.EnControlStock && TodasConfirmadas;

        // El cliente ajusta la selección por disponibilidad: solo con faltantes informados.
        public bool PuedeAjustarse() => Estado == EstadoPedido.ConFaltantes;

        // Asentar desistimiento de un pedido existente: solo con faltantes informados.
        public bool PuedeDesistirse() => Estado == EstadoPedido.ConFaltantes;

        // Formalizar: solo con las prendas ya separadas.
        public bool PuedeFormalizarse() => Estado == EstadoPedido.Separado;

        // "Pedido formalizado con selección cerrada": pasó por Formalizar y no fue cancelado.
        // Es la condición para preparar la confirmación (BLL y pantalla usan este mismo criterio).
        public bool EstaFormalizado() =>
            FechaFormalizacion.HasValue &&
            (Estado == EstadoPedido.Pendiente || Estado == EstadoPedido.Despachado ||
             Estado == EstadoPedido.Entregado);

        // "¿Posee pedido activo?": un pedido que todavía no terminó su ciclo bloquea al cliente.
        // (Entregado sin devolver se controla aparte, por las prendas en uso.)
        public bool EsActivo() =>
            Estado == EstadoPedido.EnControlStock || Estado == EstadoPedido.ConFaltantes ||
            Estado == EstadoPedido.Separado       || Estado == EstadoPedido.Pendiente    ||
            Estado == EstadoPedido.Despachado;

        /// <summary>
        /// Valida que la transición de estado sea permitida según el flujo definido:
        ///   EnControlStock → ConFaltantes | Separado
        ///   ConFaltantes   → EnControlStock (selección ajustada) | Desistido
        ///   Separado       → Pendiente (formalizar) | Cancelado (cancelar pedido separado)
        ///   Pendiente      → Despachado | Cancelado
        ///   Despachado     → Entregado
        ///   Cancelado      → EnControlStock (reactivar: vuelve a control de stock)
        /// Entregado y Desistido son estados finales.
        /// </summary>
        public bool TransicionValida(EstadoPedido destino)
        {
            switch (Estado)
            {
                case EstadoPedido.EnControlStock:
                    return destino == EstadoPedido.ConFaltantes
                        || destino == EstadoPedido.Separado
                        || destino == EstadoPedido.Cancelado;   // Cancelar pedido en control (sin prendas reservadas)

                case EstadoPedido.ConFaltantes:
                    return destino == EstadoPedido.EnControlStock
                        || destino == EstadoPedido.Desistido;

                case EstadoPedido.Separado:
                    return destino == EstadoPedido.Pendiente
                        || destino == EstadoPedido.Cancelado;   // Cancelar pedido separado (libera las prendas)

                case EstadoPedido.Pendiente:
                    return destino == EstadoPedido.Despachado
                        || destino == EstadoPedido.Cancelado;

                case EstadoPedido.Despachado:
                    return destino == EstadoPedido.Entregado;

                case EstadoPedido.Cancelado:
                    return destino == EstadoPedido.EnControlStock; // Reactivar: vuelve a control de stock

                case EstadoPedido.Entregado:
                case EstadoPedido.Desistido:
                    return false; // Estados finales — no admiten más transiciones

                default:
                    return false;
            }
        }
    }
}
