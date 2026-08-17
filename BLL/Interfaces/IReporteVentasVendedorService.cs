using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>PdN8 — Reporte de ventas por vendedor: pedidos, entregas y cancelaciones
    /// agregados por Empleado, para evaluar desempeño comercial.</summary>
    public interface IReporteVentasVendedorService
    {
        /// <summary>Devuelve el desempeño de todos los vendedores con al menos un pedido.</summary>
        List<BE.DesempenoVendedor> Obtener();
    }
}
