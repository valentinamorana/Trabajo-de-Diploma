namespace BLL.Manejadores
{
    /// <summary>
    /// PdN6 — Qué resolvió el Vendedor tras intentar cobrar la suscripción (fuera del
    /// sistema: efectivo, transferencia, etc. — sin pasarela de pago, ver alcance).
    /// </summary>
    public enum DecisionCobro
    {
        Cobrado,
        PagoFallido
    }
}
