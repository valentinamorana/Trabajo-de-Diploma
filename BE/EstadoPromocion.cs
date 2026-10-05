namespace BE
{
    /// <summary>
    /// PN03 — Estado de una Promocion (diagrama de actividad aprobado):
    ///   EnRevisionContable    → estado inicial tras "Validar" (alta o reformulación).
    ///   Vigente               → Contabilidad aprobó; se aplica en el cobro de PN02.
    ///   RechazadaContabilidad → Contabilidad rechazó; Administración decide si reformula.
    ///   BajaSolicitada        → Vendedor pidió la baja; Administración la resuelve.
    ///   Desactivada           → fin: baja aprobada o desactivación directa.
    ///   Descartada            → fin: Administración no reformula una promoción rechazada.
    ///   Vencida               → fin: llegó la FechaFin (BLL.Promocion.CerrarVencidas).
    /// </summary>
    public enum EstadoPromocion
    {
        EnRevisionContable = 0,
        Vigente = 1,
        RechazadaContabilidad = 2,
        BajaSolicitada = 3,
        Desactivada = 4,
        Descartada = 5,
        Vencida = 6
    }
}
