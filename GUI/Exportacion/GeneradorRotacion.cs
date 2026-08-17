namespace GUI.Exportacion
{
    /// <summary>Patrón Factory Method — creador de exportadores para el Análisis de Rotación de Prendas (PdN9).</summary>
    public class GeneradorRotacion : GeneradorReporte
    {
        private const string Origen = "Rotación de Prendas";

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
