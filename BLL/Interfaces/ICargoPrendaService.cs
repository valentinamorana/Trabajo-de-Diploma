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
        void RegistrarCargo(string modulo, BE.Prenda prenda, string motivo, decimal monto);

        // Valida motivo (obligatorio) y monto (mayor a cero); lanza AppException si no cumplen.
        void ValidarDatos(string motivo, decimal monto);

        List<BE.CargoPrenda> ObtenerPendientesPorCliente(int idCliente);
    }
}
