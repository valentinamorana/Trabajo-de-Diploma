namespace GUI.Exportacion
{
    /// <summary>
    /// Patrón FACTORY METHOD — rol "ConcreteCreator".
    ///
    /// Creador de exportadores para los documentos de PN03 (Métricas, promociones y toma de
    /// decisiones): reporte de métricas, sugerencia de promoción, constancia de descarte, ficha
    /// de promoción, dictamen contable, solicitud de baja y resolución de baja.
    /// </summary>
    public class GeneradorDocumentoPromocion : GeneradorReporte
    {
        private readonly string _origen;

        public GeneradorDocumentoPromocion(string origen)
        {
            _origen = origen;
        }

        public override Exportador CrearExportador(string formato)
        {
            if (formato == "pdf")
                return new ExportadorPdf(_origen);
            else if (formato == "txt")
                return new ExportadorTxt(_origen);
            else
                return null;
        }
    }
}
