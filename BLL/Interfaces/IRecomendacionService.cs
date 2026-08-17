using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>PdN13 — Recomendación de prendas para un cliente, a partir de su historial
    /// de pedidos (categoría y color preferidos), sugiriendo prendas Disponibles afines.</summary>
    public interface IRecomendacionService
    {
        /// <summary>Devuelve prendas Disponibles sugeridas para el cliente, ordenadas por afinidad.
        /// Lista vacía si el cliente no tiene historial de pedidos suficiente.</summary>
        List<BE.PrendaRecomendada> Recomendar(int idCliente);
    }
}
