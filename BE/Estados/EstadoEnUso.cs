namespace BE.Estados
{
    /// <summary>
    /// Estado concreto: la prenda está en poder de un cliente. No admite transición
    /// manual desde el módulo de Prendas — el pasaje a/desde EnUso lo maneja
    /// exclusivamente el flujo de Pedido (alta, devolución, cancelación,
    /// des-cancelación), documentado en <see cref="Prenda.TransicionPermitida"/>.
    /// </summary>
    public sealed class EstadoEnUso : Estado
    {
        public override bool EsTransicionValida(EstadoPrenda destino) => destino == EstadoPrenda.EnUso;
    }
}
