using System;

namespace BE
{
    /// <summary>
    /// Entidad — Registro de un intento de cobro de suscripción (PdN6).
    /// Mapea la tabla [HistorialCobro]. Es el rastro que deja la cadena de
    /// manejadores de BLL.Manejadores al resolver (o dejar pendiente) un cobro.
    /// </summary>
    public class Cobro
    {
        public int IdCobro { get; set; }
        public int IdCliente { get; set; }

        /// <summary>Nombre del cliente (cargado por JOIN, no persiste).</summary>
        public string NombreCliente { get; set; }

        public decimal Importe { get; set; }
        public DateTime FechaDeteccion { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public EstadoCobro Resultado { get; set; }
        public string Actor { get; set; }
    }
}
