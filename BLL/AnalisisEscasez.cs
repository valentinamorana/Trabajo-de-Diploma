using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Detección de Escasez por Talle/Categoría (PdN12). Compara el
    /// stock Disponible agrupado por combinación Talle+Categoría contra un umbral mínimo
    /// configurable por el GerenteInventario, para planificar compras antes de un faltante.
    /// </summary>
    public class AnalisisEscasez : Interfaces.IAnalisisEscasezService
    {
        public const int UmbralPorDefecto = 3;

        private readonly DAL.Interfaces.IPrendaDAL dalPrenda;

        public AnalisisEscasez() : this(new DAL.Prenda()) { }

        public AnalisisEscasez(DAL.Interfaces.IPrendaDAL dalPrenda)
        {
            this.dalPrenda = dalPrenda ?? throw new ArgumentNullException(nameof(dalPrenda));
        }

        public List<BE.EscasezStock> Detectar(int umbralMinimo)
        {
            if (umbralMinimo < 0) throw new ArgumentOutOfRangeException(nameof(umbralMinimo));

            var resultado = new List<BE.EscasezStock>();
            foreach (var stock in dalPrenda.ObtenerConteoDisponiblesPorTalleCategoria())
            {
                if (stock.CantidadDisponible >= umbralMinimo) continue;

                resultado.Add(new BE.EscasezStock
                {
                    Talle = stock.Talle,
                    Categoria = stock.Categoria,
                    CantidadDisponible = stock.CantidadDisponible,
                    Umbral = umbralMinimo,
                    Motivo = $"Talle {stock.Talle} de {stock.Categoria}: quedan {stock.CantidadDisponible} disponible(s), por debajo del mínimo de {umbralMinimo}.",
                    Clave = "escasez.motivo",
                    Args = new object[] { stock.Talle, stock.Categoria, stock.CantidadDisponible, umbralMinimo }
                });
            }

            return resultado.OrderBy(r => r.CantidadDisponible).ToList();
        }
    }
}
