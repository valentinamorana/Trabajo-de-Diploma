using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Recomendación de Prendas para un Cliente (PdN13). Construye el
    /// perfil de preferencia del cliente (categoría y color más pedidos en su historial) y
    /// sugiere prendas Disponibles afines, para que el Vendedor las ofrezca en el próximo
    /// pedido. Sin historial de pedidos, no hay base para recomendar: devuelve lista vacía.
    /// </summary>
    public class RecomendacionPrendas : Interfaces.IRecomendacionService
    {
        private readonly DAL.Pedido dalPedido;
        private readonly DAL.Prenda dalPrenda;

        public RecomendacionPrendas() : this(new DAL.Pedido(), new DAL.Prenda()) { }

        public RecomendacionPrendas(DAL.Pedido dalPedido, DAL.Prenda dalPrenda)
        {
            this.dalPedido = dalPedido ?? throw new ArgumentNullException(nameof(dalPedido));
            this.dalPrenda = dalPrenda ?? throw new ArgumentNullException(nameof(dalPrenda));
        }

        public List<BE.PrendaRecomendada> Recomendar(int idCliente)
        {
            var historial = dalPedido.ObtenerPrendasHistoricasPorCliente(idCliente);
            if (historial.Count == 0) return new List<BE.PrendaRecomendada>();

            string categoriaFavorita = historial
                .Where(p => !string.IsNullOrEmpty(p.Categoria))
                .GroupBy(p => p.Categoria)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            string colorFavorito = historial
                .Where(p => !string.IsNullOrEmpty(p.Color))
                .GroupBy(p => p.Color)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            var idsYaPedidos = new HashSet<int>(historial.Select(p => p.IdPrenda));

            var resultado = new List<BE.PrendaRecomendada>();
            foreach (var disponible in dalPrenda.ObtenerDisponibles())
            {
                if (idsYaPedidos.Contains(disponible.IdPrenda)) continue;

                bool coincideCategoria = categoriaFavorita != null && disponible.Categoria == categoriaFavorita;
                bool coincideColor = colorFavorito != null && disponible.Color == colorFavorito;
                if (!coincideCategoria && !coincideColor) continue;

                int coincidencias = (coincideCategoria ? 1 : 0) + (coincideColor ? 1 : 0);
                string motivo = coincideCategoria && coincideColor
                    ? $"{disponible.Nombre} coincide con la categoría ({categoriaFavorita}) y el color ({colorFavorito}) preferidos del cliente."
                    : coincideCategoria
                        ? $"{disponible.Nombre} coincide con la categoría preferida del cliente ({categoriaFavorita})."
                        : $"{disponible.Nombre} coincide con el color preferido del cliente ({colorFavorito}).";
                string clave = coincideCategoria && coincideColor ? "recom.motivo.ambos"
                             : coincideCategoria ? "recom.motivo.categoria"
                             : "recom.motivo.color";
                object[] args = coincideCategoria && coincideColor
                    ? new object[] { disponible.Nombre, categoriaFavorita, colorFavorito }
                    : coincideCategoria ? new object[] { disponible.Nombre, categoriaFavorita }
                                         : new object[] { disponible.Nombre, colorFavorito };

                resultado.Add(new BE.PrendaRecomendada
                {
                    Prenda = disponible,
                    Coincidencias = coincidencias,
                    Motivo = motivo,
                    Clave = clave,
                    Args = args
                });
            }

            return resultado.OrderByDescending(r => r.Coincidencias).ThenBy(r => r.Prenda.Nombre).ToList();
        }
    }
}
