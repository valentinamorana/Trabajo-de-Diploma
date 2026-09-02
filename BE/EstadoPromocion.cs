namespace BE
{
    /// <summary>
    /// PN03 — Estado de una Promocion. EnRevisionContable es el estado inicial (Administración
    /// la crea, desde una sugerencia de Gerencia o manual). Contabilidad aprueba (Vigente) o
    /// rechaza (RechazadaContabilidad, vuelve a Administración). Desde Vigente, Vendedor puede
    /// sugerir la baja (BajaSolicitada); Administración la aprueba (Desactivada) o la rechaza
    /// (vuelve a Vigente). Administración también puede desactivar una Vigente directamente.
    /// </summary>
    public enum EstadoPromocion
    {
        EnRevisionContable = 0,
        Vigente = 1,
        RechazadaContabilidad = 2,
        BajaSolicitada = 3,
        Desactivada = 4
    }
}
