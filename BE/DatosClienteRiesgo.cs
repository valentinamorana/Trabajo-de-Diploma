using System;

namespace BE
{
    /// <summary>
    /// PdN10 — Datos cruzados de un cliente para evaluar riesgo de abandono: la
    /// suscripción (Cliente, ya trae FechaVencimiento) + el último pedido registrado
    /// (fuera de Cliente, viene de la tabla Pedido). Es el INPUT que recibe cada
    /// BLL.Estrategias.EstrategiaRiesgo — la "Compra" del ejemplo de cátedra Strategy.
    /// </summary>
    public class DatosClienteRiesgo
    {
        public Cliente Cliente { get; set; }

        /// <summary>Null si el cliente nunca generó un pedido.</summary>
        public DateTime? FechaUltimoPedido { get; set; }

        /// <summary>Días desde el último pedido; null si nunca tuvo uno.</summary>
        public int? DiasSinActividad =>
            FechaUltimoPedido.HasValue
                ? (int)(DateTime.Today - FechaUltimoPedido.Value.Date).TotalDays
                : (int?)null;

        /// <summary>Días desde el alta del cliente (para detectar altas que nunca activaron el servicio).</summary>
        public int DiasDesdeAlta => (int)(DateTime.Today - Cliente.FechaAlta.Date).TotalDays;
    }
}
