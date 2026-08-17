namespace BE.Estados
{
    /// <summary>Estado concreto: la prenda está en mantenimiento/limpieza tras una devolución.</summary>
    public sealed class EstadoEnLimpieza : Estado
    {
        public override bool EsTransicionValida(EstadoPrenda destino) =>
            destino == EstadoPrenda.EnLimpieza ||
            destino == EstadoPrenda.Disponible ||
            destino == EstadoPrenda.Baja;
    }
}
