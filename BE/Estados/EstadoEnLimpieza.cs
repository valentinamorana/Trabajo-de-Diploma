namespace BE.Estados
{
    /// <summary>Estado concreto: la prenda está en mantenimiento/limpieza tras una devolución.</summary>
    public sealed class EstadoEnLimpieza : Estado
    {
        public override bool ControlarEstado(Prenda prenda, EstadoPrenda destino)
        {
            if (destino != EstadoPrenda.EnLimpieza &&
                destino != EstadoPrenda.Disponible &&
                destino != EstadoPrenda.Baja)
                return false;

            prenda.Estado = destino;
            return true;
        }
    }
}
