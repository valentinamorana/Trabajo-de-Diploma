namespace BE
{
    /// <summary>
    /// Estado de una fila de Lista de Espera de una prenda (mejora opcional,
    /// inspirada en la Lista de Espera de ExperienceHub). "Expirada" NO es un
    /// estado propio: se DERIVA de Reservada + FechaLimiteReserva vencida,
    /// mismo criterio que Cliente.EstaSuspendidoPorPago (PdN6) — así no hace
    /// falta un job en background que la recorra y actualice.
    /// </summary>
    public enum EstadoListaEspera
    {
        Pendiente = 0,

        Reservada = 1,

        Convertida = 2,

        Cancelada = 3
    }
}
