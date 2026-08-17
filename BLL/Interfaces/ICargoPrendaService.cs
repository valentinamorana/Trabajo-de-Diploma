using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// Bloque 1 — Cargo por daño/pérdida de prenda. Se registra sobre el último cliente
    /// que tuvo la prenda (BE.Prenda.IdUltimoCliente) y se liquida junto con su próxima
    /// renovación (ver BLL.Manejadores.ProcesarPagoHandler).
    /// </summary>
    public interface ICargoPrendaService
    {
        // Registra un cargo Pendiente para la prenda indicada, contra su último cliente conocido.
        void RegistrarCargo(string modulo, BE.Prenda prenda, string motivo, decimal monto, string actor = null);

        List<BE.CargoPrenda> ObtenerPendientesPorCliente(int idCliente);
        List<BE.CargoPrenda> ObtenerTodos();
    }
}
