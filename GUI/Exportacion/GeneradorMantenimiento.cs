namespace GUI.Exportacion
{
    /// <summary>Patrón Factory Method — creador de exportadores para el Análisis de Tiempos de Mantenimiento (PdN11).</summary>
    public class GeneradorMantenimiento : GeneradorReporte
    {
        private const string Origen = "Tiempos de Mantenimiento";

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
