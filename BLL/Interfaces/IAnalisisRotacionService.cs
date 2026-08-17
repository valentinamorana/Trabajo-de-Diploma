using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>PdN9 — Detección de prendas de alta o baja demanda cruzando el catálogo
    /// activo contra la cantidad de pedidos que incluyó a cada prenda.</summary>
    public interface IAnalisisRotacionService
    {
        /// <summary>Devuelve las prendas marcadas de alta o baja demanda.</summary>
        List<BE.RotacionPrenda> Detectar();
    }
}
