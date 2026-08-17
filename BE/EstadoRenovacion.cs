namespace BE
{
    /// <summary>PdN5 — Resultado de un intento de renovación de suscripción.</summary>
    public enum EstadoRenovacion
    {
        Pendiente = 0,
        Renovada = 1,
        CambioPlan = 2,
        Baja = 3,
        Pausada = 4
    }
}
