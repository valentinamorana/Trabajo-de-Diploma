namespace GUI.Exportacion
{
    /// <summary>
    /// Patrón FACTORY METHOD — rol "ConcreteCreator".
    ///
    /// Creador de exportadores para los documentos de PN02 (Comercialización de la
    /// suscripción): planes disponibles, aviso de desistimiento, orden de cobro, liquidación,
    /// comprobante, constancia de suscripción y constancia de cancelación.
    /// </summary>
    public class GeneradorDocumentoContratacion : GeneradorReporte
    {
        private readonly string _origen;

        public GeneradorDocumentoContratacion(string origen)
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
