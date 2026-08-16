namespace BE
{
    /// <summary>PdN6 — Resultado de un intento de cobro de suscripción.</summary>
    public enum EstadoCobro
    {
        Pendiente = 0,
        Cobrado = 1,
        Gracia = 2,
        Suspendido = 3
    }
}
