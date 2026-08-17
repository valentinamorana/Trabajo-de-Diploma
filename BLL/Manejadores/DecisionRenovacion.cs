namespace BLL.Manejadores
{
    /// <summary>
    /// PdN5 — Qué resolvió el Vendedor/Supervisor tras contactar al cliente (fuera del
    /// sistema, por teléfono/WhatsApp/mail) sobre su suscripción próxima a vencer o vencida.
    ///
    /// ⚠ BajaSuscripcionHandler es el último eslabón de la cadena y trata CUALQUIER
    /// decisión que llegue hasta él como Baja, sin validar cuál es (mismo criterio que
    /// DirectorGeneral en el ejemplo de cátedra: el último eslabón resuelve sin condición).
    /// Si agregás un valor nuevo acá (ej. "Pausar"), tenés que insertar su propio Handler
    /// en la cadena ANTES de BajaSuscripcionHandler (ver BLL.Renovacion) — si te olvidás,
    /// esa decisión nueva se va a tratar como una baja completa sin ningún error visible.
    /// </summary>
    public enum DecisionRenovacion
    {
        Renovar,
        CambiarPlan,
        Baja
    }
}
