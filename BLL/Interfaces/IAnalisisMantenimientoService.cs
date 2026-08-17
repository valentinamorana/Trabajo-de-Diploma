using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>PdN11 — Detección de prendas con tiempos o cantidad de mantenimientos
    /// excesivos, a partir del historial de MantenimientoPrenda.</summary>
    public interface IAnalisisMantenimientoService
    {
        /// <summary>Devuelve las prendas cuyo historial de mantenimiento supera el umbral aceptable.</summary>
        List<BE.TiempoMantenimientoPrenda> Detectar();
    }
}
