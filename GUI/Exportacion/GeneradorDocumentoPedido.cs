namespace GUI.Exportacion
{
    /// <summary>
    /// Patrón FACTORY METHOD — rol "ConcreteCreator".
    ///
    /// Creador de exportadores para los documentos de PN01 (Armar pedido): planilla de
    /// control de existencias, informe de disponibilidad, constancia de prendas separadas,
    /// confirmación y constancia del pedido y aviso de desistimiento. Todos son reportes de
    /// texto; el origen identifica de qué documento se trata.
    /// </summary>
    public class GeneradorDocumentoPedido : GeneradorReporte
    {
        private readonly string _origen;

        public GeneradorDocumentoPedido(string origen)
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
