using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// PN01 — una línea del "Informe de disponibilidad (prendas faltantes y alternativas)"
    /// que emite Depósito: la prenda del pedido que no está disponible y las prendas
    /// alternativas que propone el sistema (Disponibles, de la misma categoría y talle).
    /// Mapea la tabla [PedidoFaltante] (una fila por alternativa, o una sola sin alternativa).
    /// </summary>
    public class PedidoFaltante
    {
        public int IdPedido { get; set; }

        // Prenda del pedido que no está disponible.
        public Prenda Prenda { get; set; }

        // Estado en que estaba la prenda al revisar el stock (por qué falta).
        public EstadoPrenda EstadoAlRevisar { get; set; }

        // True si la prenda sigue Disponible pero está reservada por Lista de Espera para otro cliente.
        public bool ReservadaParaOtro { get; set; }

        public List<Prenda> Alternativas { get; set; } = new List<Prenda>();
    }
}
