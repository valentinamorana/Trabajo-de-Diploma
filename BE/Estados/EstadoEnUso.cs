namespace BE.Estados
{
    /// <summary>
    /// Estado concreto: la prenda está en poder de un cliente. No admite transición manual
    /// general desde el módulo de Prendas — el pasaje a/desde EnUso lo maneja exclusivamente
    /// el flujo de Pedido (alta, devolución, cancelación, des-cancelación), documentado en
    /// <see cref="Prenda.TransicionPermitida"/>. La única excepción es <c>EnUso → Baja</c>
    /// (PN04, CU-DEP-02 Reportar Prenda Perdida): una prenda que nunca vuelve físicamente
    /// (perdida) no puede pasar primero por EnLimpieza para inspeccionarse, así que necesita
    /// darse de baja directo desde EnUso.
    /// </summary>
    public sealed class EstadoEnUso : Estado
    {
        public override bool EsTransicionValida(EstadoPrenda destino) =>
            destino == EstadoPrenda.EnUso || destino == EstadoPrenda.Baja;
    }
}
