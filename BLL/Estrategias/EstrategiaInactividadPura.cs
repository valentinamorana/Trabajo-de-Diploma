namespace BLL.Estrategias
{
    /// <summary>
    /// Criterio de "abandono silencioso": en riesgo si no tiene actividad hace mucho
    /// tiempo, SIN mirar el vencimiento — a diferencia de EstrategiaVencimientoInactividad,
    /// detecta al cliente que sigue pagando (su suscripción está vigente) pero dejó de usar
    /// el servicio, antes de que llegue el momento de renovar.
    /// </summary>
    public sealed class EstrategiaInactividadPura : EstrategiaRiesgo
    {
        // 60 días (vs. los 30 de EstrategiaVencimientoInactividad) a propósito: acá la
        // inactividad es la ÚNICA señal de riesgo (no se mira el vencimiento), así que necesita
        // un umbral más largo para no marcar en riesgo a alguien que solo se tomó un respiro
        // corto. La otra estrategia solo evalúa inactividad como refuerzo de un vencimiento ya
        // próximo/vencido, por eso le alcanza con un umbral más corto.
        public const int DiasSinActividadParaRiesgo = 60;

        public override BE.ResultadoRiesgo Evaluar(BE.DatosClienteRiesgo datos)
        {
            var cliente = datos.Cliente;

            bool sinActividad = datos.FechaUltimoPedido.HasValue && datos.DiasSinActividad >= DiasSinActividadParaRiesgo;

            if (!sinActividad)
                return new BE.ResultadoRiesgo { EnRiesgo = false };

            return new BE.ResultadoRiesgo
            {
                EnRiesgo = true,
                Motivo = $"{cliente.NombreCompleto} no tiene actividad hace {datos.DiasSinActividad} día(s), independientemente del vencimiento.",
                Clave = "abandono.motivo.inactividad",
                Args = new object[] { cliente.NombreCompleto, datos.DiasSinActividad }
            };
        }
    }
}
