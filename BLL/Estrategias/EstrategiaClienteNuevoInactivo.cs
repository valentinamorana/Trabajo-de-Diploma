namespace BLL.Estrategias
{
    /// <summary>
    /// Criterio de "abandono temprano": en riesgo si el cliente se dio de alta hace más
    /// de un umbral de días y JAMÁS generó un pedido — activó la suscripción pero nunca
    /// llegó a usar el servicio. Caso distinto a los otros dos: no depende de vencimiento
    /// ni de haber tenido actividad alguna vez.
    /// </summary>
    public sealed class EstrategiaClienteNuevoInactivo : EstrategiaRiesgo
    {
        public const int DiasDeGraciaInicial = 20;

        public override BE.ResultadoRiesgo Evaluar(BE.DatosClienteRiesgo datos)
        {
            var cliente = datos.Cliente;

            bool nuncaPidio = !datos.FechaUltimoPedido.HasValue;
            bool pasoElPeriodoInicial = datos.DiasDesdeAlta >= DiasDeGraciaInicial;

            if (!nuncaPidio || !pasoElPeriodoInicial)
                return new BE.ResultadoRiesgo { EnRiesgo = false };

            return new BE.ResultadoRiesgo
            {
                EnRiesgo = true,
                Motivo = $"{cliente.NombreCompleto} se dio de alta hace {datos.DiasDesdeAlta} día(s) y nunca generó un pedido.",
                Clave = "abandono.motivo.nuncaactivo",
                Args = new object[] { cliente.NombreCompleto, datos.DiasDesdeAlta }
            };
        }
    }
}
