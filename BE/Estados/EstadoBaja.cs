namespace BE.Estados
{
    /// <summary>Estado concreto y final: la prenda fue dada de baja y no vuelve a circular.</summary>
    public sealed class EstadoBaja : Estado
    {
        public override bool ControlarEstado(Prenda prenda, EstadoPrenda destino)
        {
            if (destino != EstadoPrenda.Baja) return false;
            prenda.Estado = destino;
            return true;
        }
    }
}
