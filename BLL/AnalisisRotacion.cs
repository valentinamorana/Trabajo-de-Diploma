using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Análisis de Rotación de Prendas (PdN9). Cruza el catálogo activo
    /// (BE.Prenda, excluye Baja) con la cantidad de pedidos que incluyó a cada prenda
    /// (DAL.Pedido) para identificar candidatas a baja (sin movimiento) o a reposición
    /// (demanda alta) — mismo criterio de cruce de datos que BLL.AnalisisAbandono (PdN10).
    /// </summary>
    public class AnalisisRotacion : Interfaces.IAnalisisRotacionService
    {
        private readonly DAL.Interfaces.IPrendaDAL dalPrenda;
        private readonly DAL.Interfaces.IPedidoDAL dalPedido;

        // Una prenda sin ningún pedido recién es "candidata a baja" si ya lleva un tiempo
        // en catálogo — evita marcar como sin movimiento a una prenda recién ingresada.
        public const int DiasAntiguedadMinimaParaBajaDemanda = 30;
        public const int CantidadPedidosParaAltaDemanda = 5;

        public AnalisisRotacion() : this(new DAL.Prenda(), new DAL.Pedido()) { }

        public AnalisisRotacion(DAL.Interfaces.IPrendaDAL dalPrenda, DAL.Interfaces.IPedidoDAL dalPedido)
        {
            this.dalPrenda = dalPrenda ?? throw new ArgumentNullException(nameof(dalPrenda));
            this.dalPedido = dalPedido ?? throw new ArgumentNullException(nameof(dalPedido));
        }

        public List<BE.RotacionPrenda> Detectar()
        {
            var cantidadPorPrenda = dalPedido.ObtenerCantidadPedidosPorPrenda();

            var resultado = new List<BE.RotacionPrenda>();
            foreach (var prenda in dalPrenda.ObtenerTodos().Where(p => p.Estado != BE.EstadoPrenda.Baja))
            {
                int cantidad = cantidadPorPrenda.TryGetValue(prenda.IdPrenda, out var c) ? c : 0;
                int diasEnCatalogo = (int)(DateTime.Today - prenda.FechaAlta.Date).TotalDays;

                if (cantidad == 0 && diasEnCatalogo >= DiasAntiguedadMinimaParaBajaDemanda)
                {
                    resultado.Add(new BE.RotacionPrenda
                    {
                        IdPrenda = prenda.IdPrenda,
                        NombrePrenda = prenda.Nombre,
                        Categoria = prenda.Categoria,
                        CantidadPedidos = 0,
                        Motivo = $"{prenda.Nombre} no registra pedidos en {diasEnCatalogo} día(s) desde su alta — candidata a baja.",
                        Clave = "rotacion.motivo.bajademanda",
                        Args = new object[] { prenda.Nombre, diasEnCatalogo }
                    });
                }
                else if (cantidad >= CantidadPedidosParaAltaDemanda)
                {
                    resultado.Add(new BE.RotacionPrenda
                    {
                        IdPrenda = prenda.IdPrenda,
                        NombrePrenda = prenda.Nombre,
                        Categoria = prenda.Categoria,
                        CantidadPedidos = cantidad,
                        Motivo = $"{prenda.Nombre} fue pedida {cantidad} veces — candidata a reposición en su categoría.",
                        Clave = "rotacion.motivo.altademanda",
                        Args = new object[] { prenda.Nombre, cantidad }
                    });
                }
            }

            return resultado.OrderByDescending(r => r.CantidadPedidos).ToList();
        }
    }
}
