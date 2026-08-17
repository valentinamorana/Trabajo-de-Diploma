using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>PdN12 — Detección de escasez de stock por combinación Talle+Categoría,
    /// comparando el stock Disponible contra un umbral mínimo configurable.</summary>
    public interface IAnalisisEscasezService
    {
        /// <summary>Devuelve las combinaciones Talle+Categoría cuyo stock Disponible está por debajo del umbral.</summary>
        List<BE.EscasezStock> Detectar(int umbralMinimo);
    }
}
