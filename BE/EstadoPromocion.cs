namespace BE
{
    /// <summary>
    /// PN03 — Estado de una Promocion (diagrama de actividad aprobado):
    ///   EnRevisionContable    → estado inicial tras "Validar" (alta o reformulación).
    ///   Vigente               → Contabilidad aprobó; se aplica en el cobro de PN02.
    ///   RechazadaContabilidad → Contabilidad rechazó; Administración la reformula o la da de baja.
    ///   DadaDeBaja            → fin: Administración la dio de baja (una Vigente o una Rechazada).
    ///   Vencida               → fin: llegó la FechaFin (BLL.Promocion.CerrarVencidas).
    /// La «Solicitud de baja» del Vendedor no es un estado: la promoción sigue Vigente hasta que
    /// Administración la atiende (BE.SolicitudBajaPromocion). Los valores 3 y 5 (BajaSolicitada y
    /// Descartada, de la versión anterior) ya no existen: la base los migra a 1 y 4.
    /// </summary>
    public enum EstadoPromocion
    {
        EnRevisionContable = 0,
        Vigente = 1,
        RechazadaContabilidad = 2,
        DadaDeBaja = 4,
        Vencida = 6
    }
}
