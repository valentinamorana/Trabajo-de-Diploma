namespace GUI.Exportacion
{
    /// <summary>Patrón Factory Method — creador de exportadores para la Recomendación de Prendas (PdN13).</summary>
    public class GeneradorRecomendacion : GeneradorReporte
    {
        private const string Origen = "Recomendación de Prendas";

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
