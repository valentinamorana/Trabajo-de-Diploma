namespace GUI.Exportacion
{
    /// <summary>Patrón Factory Method — creador de exportadores para el Reporte de Ventas por Vendedor (PdN8).</summary>
    public class GeneradorVentasVendedor : GeneradorReporte
    {
        private const string Origen = "Ventas por Vendedor";

        public override Exportador CrearExportador(string formato)
        {
            if (formato == "pdf")
                return new ExportadorPdf(Origen);
            else if (formato == "csv")
                return new ExportadorCsv(Origen);
            else
                return null;
        }
    }
}
