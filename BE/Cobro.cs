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

        // N01 — lo que se registra de un cobro exitoso (igual que Contratacion en PN02).
        public int? IdMedioPago { get; set; }
        /// <summary>Cargado por JOIN con MedioPago, no persiste.</summary>
        public string NombreMedioPago { get; set; }
        public string NumeroComprobante { get; set; }
        public Builders.ModalidadCobro? Modalidad { get; set; }
        /// <summary>Descuento aplicado (promoción vigente o crédito por referido); null si no hubo.</summary>
        public decimal? DescuentoAplicado { get; set; }
        /// <summary>Promoción vigente aplicada (PN03: mide su impacto); null si no hubo o fue el crédito por referido.</summary>
        public int? IdPromocion { get; set; }
    }
}
