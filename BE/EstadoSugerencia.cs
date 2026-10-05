namespace BE
{
    /// <summary>
    /// PN03 — Estado de una SugerenciaPromocion (Gerencia hacia Administración).
    ///   Pendiente  → Administración todavía no decidió (¿Acepta la sugerencia?).
    ///   Evaluada   → [Sí] se creó una promoción a partir de ella.
    ///   Descartada → [No] Administración la descartó con motivo (fin).
    /// </summary>
    public enum EstadoSugerencia
    {
        Pendiente = 0,
        Evaluada = 1,
        Descartada = 2
    }
}
