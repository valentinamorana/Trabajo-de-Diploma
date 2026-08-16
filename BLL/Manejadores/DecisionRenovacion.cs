namespace BLL.Manejadores
{
    /// <summary>
    /// PdN5 — Qué resolvió el Vendedor/Supervisor tras contactar al cliente (fuera del
    /// sistema, por teléfono/WhatsApp/mail) sobre su suscripción próxima a vencer o vencida.
    /// </summary>
    public enum DecisionRenovacion
    {
        Renovar,
        CambiarPlan,
        Baja
    }
}
