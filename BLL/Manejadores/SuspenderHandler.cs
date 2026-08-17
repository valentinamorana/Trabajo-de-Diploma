using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Último eslabón de la cadena: siempre atiende lo que le llega (venció el período
    /// de gracia sin regularizar) — igual que BajaSuscripcionHandler / DirectorGeneral
    /// del ejemplo de cátedra, que resuelve sin condición ni delegación por ser el
    /// último de la cadena. El cliente ya quedó con FechaLimiteGracia vencida (la fijó
    /// AplicarGraciaHandler en un intento anterior), así que Cliente.EstaSuspendidoPorPago
    /// ya es true sin tocar la BD de nuevo: acá solo se deja constancia en el historial.
    /// El bloqueo real de nuevos pedidos lo hace BLL.Pedido.CrearPedido consultando esa
    /// propiedad, igual que ya hace con SuscripcionVigente().
    /// </summary>
    public sealed class SuspenderHandler : ManejadorCobro
    {
        private readonly DAL.Interfaces.ICobroDAL dalCobro;

        public SuspenderHandler(DAL.Interfaces.ICobroDAL dalCobro)
        {
            this.dalCobro = dalCobro ?? throw new ArgumentNullException(nameof(dalCobro));
        }

        public override ResultadoCobro Procesar(ContextoCobro contexto)
        {
            var cliente = contexto.Cliente;

            var ahora = DateTime.Now;
            int idCobro = dalCobro.Alta(new BE.Cobro
            {
                IdCliente = cliente.IdCliente,
                Importe = 0,
                FechaDeteccion = ahora,
                FechaResolucion = ahora,
                Resultado = BE.EstadoCobro.Suspendido,
                Actor = contexto.Actor
            });

            return new ResultadoCobro
            {
                Resuelto = true,
                Estado = BE.EstadoCobro.Suspendido,
                IdCobro = idCobro,
                Mensaje = "Venció el período de gracia sin regularizar el cobro. " +
                          "El cliente no podrá generar nuevos pedidos hasta registrar un cobro exitoso.",
                Clave = "cobro.msg.suspendido",
                Args = null
            };
        }
    }
}
