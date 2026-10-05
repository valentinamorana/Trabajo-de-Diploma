namespace BE
{
    /// <summary>
    /// Estado de un pedido de alquiler. Sigue el diagrama de actividad de PN01 (Armar pedido):
    ///
    ///   EnControlStock → (Depósito revisa el stock) → ConFaltantes ⇄ EnControlStock (ajuste)
    ///                                              → Separado → Pendiente (formalizado)
    ///   ConFaltantes / selección que excede el cupo → Desistido
    ///   Pendiente → Despachado → Entregado        (ciclo logístico, posterior a PN01)
    ///   Pendiente → Cancelado → Pendiente         (cancelar / des-cancelar)
    ///
    /// Los valores numéricos se persisten en Pedido.Estado: los existentes (0-3) no cambian.
    /// </summary>
    public enum EstadoPedido
    {
        // Formalizado por el Vendedor: selección cerrada, prendas separadas y en uso,
        // pendiente de despacho.
        Pendiente = 0,
        Despachado = 1,
        Entregado = 2,
        Cancelado = 3,

        // PN01 — el Vendedor envió la selección para control de stock (sin reservar prendas).
        EnControlStock = 4,

        // PN01 — Depósito emitió el informe de prendas faltantes (con alternativas).
        ConFaltantes = 5,

        // PN01 — Depósito confirmó y separó las prendas (quedan En uso); falta formalizar.
        Separado = 6,

        // PN01 — el cliente desistió (por exceso de cupo o por falta de disponibilidad).
        Desistido = 7
    }
}
