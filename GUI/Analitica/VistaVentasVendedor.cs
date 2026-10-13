using System.Drawing;

namespace GUI.Analitica
{
    /// <summary>
    /// PdN8 — Ventas por Vendedor. El Gerente Comercial genera el desempeño de cada vendedor
    /// (pedidos totales, entregados y cancelados) para evaluar rendimiento. Sin filtros.
    /// </summary>
    internal sealed class VistaVentasVendedor : VistaReporteAnalitica
    {
        private readonly BLL.Interfaces.IReporteVentasVendedorService _bll = new BLL.ReporteVentasVendedor();

        public override BE.ReporteAnalitica Tipo => BE.ReporteAnalitica.VentasVendedor;
        public override string Titulo => Tr("ventasvend.titulo", "Ventas por Vendedor");
        public override string NombreArchivo => "VentasPorVendedor";

        public override string[] Encabezados => new[]
        {
            Tr("ventasvend.col.vendedor", "Vendedor"),
            Tr("ventasvend.col.total", "Total pedidos"),
            Tr("ventasvend.col.entregados", "Entregados"),
            Tr("ventasvend.col.cancelados", "Cancelados"),
            Tr("ventasvend.col.tasacancel", "% Cancelación")
        };

        public override Exportacion.GeneradorReporte CrearGenerador() => new Exportacion.GeneradorVentasVendedor();

        public override ResultadoVistaReporte Generar()
        {
            var resultados = _bll.Obtener();

            var tabla = NuevaTabla();
            foreach (var r in resultados)
                tabla.Rows.Add(r.NombreEmpleado, r.TotalPedidos, r.Entregados, r.Cancelados,
                    r.TasaCancelacion.ToString("F1") + "%");

            return new ResultadoVistaReporte
            {
                Tabla = tabla,
                Cantidad = resultados.Count,
                ColorMensaje = Color.DarkGreen,
                Mensaje = string.Format(
                    Tr("ventasvend.resultado", "{0} vendedor(es) con pedidos registrados."),
                    new object[] { resultados.Count })
            };
        }
    }
}
