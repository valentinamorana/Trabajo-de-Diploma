namespace BE.Estados
{
    /// <summary>Estado concreto: la prenda está libre para asignarse a un pedido.</summary>
    public sealed class EstadoDisponible : Estado
    {
        public override bool ControlarEstado(Prenda prenda, EstadoPrenda destino)
        {
            if (destino != EstadoPrenda.Disponible &&
                destino != EstadoPrenda.EnLimpieza &&
                destino != EstadoPrenda.Baja)
                return false;

            prenda.Estado = destino;
            return true;
        }
    }
}
