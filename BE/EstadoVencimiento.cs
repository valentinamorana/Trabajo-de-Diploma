namespace BE
{
    /// <summary>
    /// Situación de la suscripción de un cliente respecto de su vencimiento. No se persiste:
    /// se DERIVA de FechaVencimiento y FechaPausaHasta (ver <see cref="Cliente.ObtenerEstadoVencimiento"/>
    /// y <see cref="Cliente.ObtenerEstadoSuscripcion"/>), así las pantallas no repiten la regla.
    /// </summary>
    public enum EstadoVencimiento
    {
        SinVencimiento = 0,

        Vigente = 1,

        ProximaAVencer = 2,

        Vencida = 3,

        Pausada = 4
    }
}
