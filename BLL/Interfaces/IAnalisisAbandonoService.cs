using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PdN10 — Detección de clientes en riesgo de abandono. Contexto del patrón Strategy
    /// (BLL.Estrategias): mantiene el criterio de riesgo activo y lo aplica a todos los
    /// clientes con plan asignado.
    /// </summary>
    public interface IAnalisisAbandonoService
    {
        /// <summary>Cambia el criterio de riesgo a aplicar en la próxima llamada a Detectar().</summary>
        void CambiarEstrategia(Estrategias.EstrategiaRiesgo estrategia);

        /// <summary>Evalúa todos los clientes con plan y devuelve los que el criterio activo marca en riesgo.</summary>
        List<BE.ClienteEnRiesgo> Detectar();
    }
}
