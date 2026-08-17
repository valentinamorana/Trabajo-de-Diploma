namespace BLL.Manejadores
{
    /// <summary>
    /// Primer eslabón: si la suscripción todavía no venció ni está próxima a vencer, no
    /// corresponde procesar ningún cobro — la atiende devolviendo Pendiente sin tocar
    /// nada, sea cual sea la decisión pedida. Si está vencida o próxima a vencer, delega
    /// al siguiente eslabón — igual que Comprador.ProcesarCompra delega a _sucesor
    /// cuando el importe supera lo que puede aprobar.
    /// </summary>
    public sealed class DetectarCobroHandler : ManejadorCobro
    {
        public override ResultadoCobro Procesar(ContextoCobro contexto)
        {
            var cliente = contexto.Cliente;

            if (!cliente.VencimientoExpirado && !cliente.SuscripcionProximaAVencer())
            {
                return new ResultadoCobro
                {
                    Resuelto = true,
                    Estado = BE.EstadoCobro.Pendiente,
                    Mensaje = $"La suscripción de {cliente.NombreCompleto} no está vencida ni próxima a vencer. " +
                              "Todavía no corresponde procesar el cobro.",
                    Clave = "cobro.msg.pendiente",
                    Args = new object[] { cliente.NombreCompleto }
                };
            }

            return _sucesor.Procesar(contexto);
        }
    }
}
