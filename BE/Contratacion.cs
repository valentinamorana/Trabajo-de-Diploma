using System;

namespace BE
{
    /// <summary>
    /// PN02 — Comercialización de la suscripción. Estado intermedio entre "el cliente eligió
    /// un plan" (Venta) y "la suscripción quedó vigente" (Caja confirma el pago y se dispara
    /// BLL.Cliente.ActivarSuscripcion). El Comprobante (CU02-CAJ) se guarda como columnas
    /// propias en vez de una entidad aparte: alcanza con un número y una fecha de emisión.
    /// </summary>
    public class Contratacion
    {
        public int IdContratacion { get; set; }
        public int IdCliente { get; set; }
        public int IdPlan { get; set; }
        public int IdVendedor { get; set; }
        public int? IdCaja { get; set; }
        public Builders.ModalidadCobro Modalidad { get; set; }
        public EstadoContratacion Estado { get; set; } = EstadoContratacion.PendientePago;
        public int IntentosPago { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string MedioPago { get; set; }
        public string NumeroComprobante { get; set; }
        public DateTime? FechaComprobante { get; set; }

        /// <summary>Cargado por JOIN, no persiste.</summary>
        public string NombreCliente { get; set; }

        /// <summary>Cargado por JOIN, no persiste.</summary>
        public string NombrePlan { get; set; }

        public bool PuedeCobrarse() => Estado == EstadoContratacion.PendientePago;
    }
}
