namespace BE
{
    /// <summary>
    /// PN03 — Métrica de la que surgió una sugerencia de promoción ("Analizar métricas"):
    /// el análisis de abandono por plan, el de rotación por categoría, o una idea de Gerencia
    /// cargada a mano (sin un dato de los reportes).
    /// </summary>
    public enum OrigenMetrica
    {
        Abandono = 0,
        Rotacion = 1,
        Manual = 2
    }
}
