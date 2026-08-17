using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Reporte de Ventas por Vendedor (PdN8). Agrega los pedidos
    /// registrados por cada Empleado (Vendedor) para que el GerenteComercial evalúe
    /// desempeño: volumen de pedidos, entregas concretadas y tasa de cancelación.
    /// </summary>
    public class ReporteVentasVendedor : Interfaces.IReporteVentasVendedorService
    {
        private readonly DAL.Pedido dalPedido;

        public ReporteVentasVendedor() : this(new DAL.Pedido()) { }

        public ReporteVentasVendedor(DAL.Pedido dalPedido)
        {
            this.dalPedido = dalPedido ?? throw new System.ArgumentNullException(nameof(dalPedido));
        }

        public List<BE.DesempenoVendedor> Obtener() => dalPedido.ObtenerEstadisticasPorEmpleado();
    }
}
