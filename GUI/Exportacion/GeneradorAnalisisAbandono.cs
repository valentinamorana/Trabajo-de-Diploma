namespace GUI.Exportacion
{
    /// <summary>
    /// Patrón FACTORY METHOD — rol "ConcreteCreator".
    ///
    /// Creador de exportadores para el Análisis de Abandono (PdN10, reporte tabular).
    /// Mismo criterio que GeneradorBitacora: decide qué producto concreto instanciar
    /// según el formato pedido y lo marca con su origen.
    /// </summary>
    public class GeneradorAnalisisAbandono : GeneradorReporte
    {
        private const string Origen = "Análisis de Abandono";

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
