namespace BLL.Manejadores
{
    /// <summary>
    /// Primer eslabón: si la suscripción todavía no venció ni está próxima a vencer, no
    /// corresponde procesar ninguna renovación — la atiende devolviendo Pendiente sin
    /// tocar nada, sea cual sea la decisión pedida (Renovar/CambiarPlan/Baja: PdN5 solo
    /// cubre la baja "el cliente no renueva al vencer", no una baja anticipada por otro
    /// motivo). Si está vencida o próxima a vencer, delega al siguiente eslabón — igual
    /// que Comprador.ProcesarCompra delega a _sucesor cuando el importe supera lo que
    /// puede aprobar.
    /// </summary>
    public sealed class VerificarVencimientoHandler : ManejadorRenovacion
    {
        public override ResultadoRenovacion Procesar(ContextoRenovacion contexto)
        {
            var cliente = contexto.Cliente;

            if (!cliente.VencimientoExpirado && !cliente.SuscripcionProximaAVencer())
            {
                return new ResultadoRenovacion
                {
                    Resuelto = true,
                    Estado = BE.EstadoRenovacion.Pendiente,
                    Mensaje = $"La suscripción de {cliente.NombreCompleto} no está vencida ni próxima a vencer. " +
                              $"No se procesó la decisión '{contexto.Decision}': todavía no corresponde renovar.",
                    Clave = "renov.msg.pendiente",
                    Args = new object[] { cliente.NombreCompleto, contexto.Decision }
                };
            }

            return DelegarASucesor(contexto);
        }
    }
}
