namespace BE
{
    /// <summary>
    /// PN01 — momento del proceso en que el cliente desiste del pedido. Coincide con las dos
    /// ramas "Comunicar desistimiento → Asentar desistimiento" del diagrama de actividad.
    /// Se persiste como texto en Pedido.EtapaDesistimiento (CHECK: 'Cupo' / 'Disponibilidad').
    /// </summary>
    public enum EtapaDesistimiento
    {
        // La selección excede el cupo del plan y el cliente no la ajusta.
        Cupo = 0,

        // Depósito informó faltantes y el cliente no ajusta la selección.
        Disponibilidad = 1
    }
}
