namespace BLL.Estrategias
{
    /// <summary>
    /// Criterio por defecto de PdN10 — literalmente "cruza último pedido y vencimiento"
    /// como describe el proceso de negocio: en riesgo si la suscripción está vencida o
    /// vence pronto Y, además, el cliente no tiene actividad reciente (o nunca la tuvo).
    /// Un cliente que paga pero no usa el servicio es el candidato más claro a no renovar.
    /// </summary>
    public sealed class EstrategiaVencimientoInactividad : EstrategiaRiesgo
    {
        public const int DiasParaConsiderarVencimientoProximo = 15;

        // 30 días (vs. los 60 de EstrategiaInactividadPura) a propósito: acá la inactividad es
        // una señal SECUNDARIA que solo se evalúa cuando la suscripción YA está vencida o por
        // vencer, así que un umbral más corto alcanza para reforzar ese riesgo. La otra
        // estrategia usa la inactividad como ÚNICA señal (sin mirar vencimiento) y necesita un
        // umbral más largo para no marcar en riesgo a alguien que solo se tomó un respiro corto.
        public const int DiasSinActividadParaRiesgo = 30;

        public override BE.ResultadoRiesgo Evaluar(BE.DatosClienteRiesgo datos)
        {
            var cliente = datos.Cliente;

            bool venceOVencido = cliente.VencimientoExpirado || cliente.SuscripcionProximaAVencer(DiasParaConsiderarVencimientoProximo);
            bool sinActividad = !datos.FechaUltimoPedido.HasValue || datos.DiasSinActividad >= DiasSinActividadParaRiesgo;

            if (!venceOVencido || !sinActividad)
                return new BE.ResultadoRiesgo { EnRiesgo = false };

            if (!datos.FechaUltimoPedido.HasValue)
                return new BE.ResultadoRiesgo
                {
                    EnRiesgo = true,
                    Motivo = $"La suscripción de {cliente.NombreCompleto} vence pronto y nunca generó un pedido.",
                    Clave = "abandono.motivo.vencinactividad_nunca",
                    Args = new object[] { cliente.NombreCompleto }
                };

            return new BE.ResultadoRiesgo
            {
                EnRiesgo = true,
                Motivo = $"La suscripción de {cliente.NombreCompleto} vence pronto y no tiene actividad hace {datos.DiasSinActividad} día(s).",
                Clave = "abandono.motivo.vencinactividad",
                Args = new object[] { cliente.NombreCompleto, datos.DiasSinActividad }
            };
        }
    }
}
