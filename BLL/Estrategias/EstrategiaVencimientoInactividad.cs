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
