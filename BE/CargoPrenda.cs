using System;

namespace BE
{
    /// <summary>
    /// Cargo por daño o pérdida de una prenda, registrado en Mantenimiento (PdN4) sobre
    /// el último cliente que la tuvo (Prenda.IdUltimoCliente) y cobrado junto con su
    /// próxima renovación (PdN6, BLL.Manejadores.ProcesarPagoHandler).
    /// </summary>
    public class CargoPrenda
    {
        public int IdCargo { get; set; }
        public int IdPrenda { get; set; }
        public string NombrePrenda { get; set; }
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string Motivo { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaCobro { get; set; }
        public string Actor { get; set; }
        public EstadoCargo Estado { get; set; }
    }
}
