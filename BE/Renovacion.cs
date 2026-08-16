using System;

namespace BE
{
    /// <summary>
    /// Entidad — Registro de un intento de renovación de suscripción (PdN5).
    /// Mapea la tabla [HistorialRenovacion]. Es el rastro que deja la cadena de
    /// manejadores de BLL.Manejadores al resolver (o dejar pendiente) un vencimiento.
    /// </summary>
    public class Renovacion
    {
        public int IdRenovacion { get; set; }
        public int IdCliente { get; set; }

        /// <summary>Nombre del cliente (cargado por JOIN, no persiste).</summary>
        public string NombreCliente { get; set; }

        public int? IdPlanAnterior { get; set; }
        public int? IdPlanNuevo { get; set; }
        public DateTime FechaDeteccion { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public EstadoRenovacion Resultado { get; set; }
        public string Actor { get; set; }
    }
}
