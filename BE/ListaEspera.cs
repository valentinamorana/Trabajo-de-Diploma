using System;

namespace BE
{
    /// <summary>
    /// Entidad — Lista de Espera de una prenda específica (mejora opcional, no
    /// requerida por la cátedra). Mapea la tabla [ListaEspera]. Un cliente se
    /// anota por una prenda EnUso; cuando se libera, la fila más antigua Pendiente
    /// pasa a Reservada con una ventana exclusiva para ese cliente.
    /// </summary>
    public class ListaEspera
    {
        public int IdListaEspera { get; set; }
        public int IdPrenda { get; set; }
        public int IdCliente { get; set; }

        /// <summary>Nombre de la prenda (cargado por JOIN, no persiste).</summary>
        public string NombrePrenda { get; set; }

        /// <summary>Nombre del cliente (cargado por JOIN, no persiste).</summary>
        public string NombreCliente { get; set; }

        public DateTime FechaAlta { get; set; }
        public EstadoListaEspera Estado { get; set; }
        public DateTime? FechaLimiteReserva { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string Actor { get; set; }

        // ── Comportamiento ────────────────────────────────────────────────────

        /// <summary>True si está Reservada y la ventana de retiro todavía no venció.</summary>
        public bool ReservaVigente =>
            Estado == EstadoListaEspera.Reservada &&
            FechaLimiteReserva.HasValue && FechaLimiteReserva.Value > DateTime.Now;

        /// <summary>True si estaba Reservada pero venció el plazo sin que el cliente la retirara.</summary>
        public bool ReservaExpirada =>
            Estado == EstadoListaEspera.Reservada &&
            FechaLimiteReserva.HasValue && FechaLimiteReserva.Value <= DateTime.Now;
    }
}
