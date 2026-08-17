namespace BLL.Manejadores
{
    /// <summary>
    /// PdN6 — Qué resolvió el Vendedor tras intentar cobrar la suscripción (fuera del
    /// sistema: efectivo, transferencia, etc. — sin pasarela de pago, ver alcance).
    ///
    /// ⚠ SuspenderHandler es el último eslabón de la cadena y no mira esta propiedad en
    /// absoluto: cualquier decisión que no sea Cobrado avanza a través de AplicarGraciaHandler
    /// y, si el período de gracia ya venció, termina tratada como Suspendido sin validar
    /// cuál era la decisión real. Si agregás un valor nuevo acá, tenés que insertar su
    /// propio Handler en la cadena ANTES de SuspenderHandler (ver BLL.Cobro) — si te
    /// olvidás, esa decisión nueva se va a tratar como una suspensión sin error visible.
    /// </summary>
    public enum DecisionCobro
    {
        Cobrado,
        PagoFallido
    }
}
