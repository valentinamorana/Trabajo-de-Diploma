namespace BE
{
    /// <summary>
    /// PN03 — Estado de una «Solicitud de baja» de promoción (Vendedor → Administración).
    ///   Pendiente → Administración todavía no la atendió.
    ///   Atendida  → Administración respondió (SolicitudBajaPromocion.Respuesta); si estuvo de
    ///               acuerdo, dio de baja la promoción (SolicitudBajaPromocion.DioDeBaja).
    /// </summary>
    public enum EstadoSolicitudBaja
    {
        Pendiente = 0,
        Atendida = 1
    }
}
