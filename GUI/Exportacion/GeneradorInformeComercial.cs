namespace GUI.Exportacion
{
    /// <summary>
    /// Patrón Factory Method — creador de exportadores del Centro de decisiones comerciales (PN03):
    /// Informe ejecutivo (PDF compuesto), recomendaciones y decisiones (CSV), orden de compra sugerida
    /// (PDF) y lista de contactos de una campaña (CSV).
    /// </summary>
    public class GeneradorInformeComercial : GeneradorReporte
    {
        private const string Origen = "Centro de decisiones comerciales";

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
