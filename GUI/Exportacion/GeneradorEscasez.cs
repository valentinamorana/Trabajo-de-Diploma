namespace GUI.Exportacion
{
    /// <summary>Patrón Factory Method — creador de exportadores para la Detección de Escasez de Stock (PdN12).</summary>
    public class GeneradorEscasez : GeneradorReporte
    {
        private const string Origen = "Escasez de Stock";

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
