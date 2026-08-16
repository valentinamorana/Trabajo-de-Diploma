using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Último eslabón de la cadena: siempre atiende lo que le llega (Decision.Baja, o
    /// cualquier caso que ningún eslabón anterior haya resuelto), sin condición ni
    /// delegación — igual que DirectorGeneral del ejemplo de cátedra, que aprueba
    /// cualquier compra sin evaluar nada porque es el último de la cadena. Da de baja el
    /// plan del cliente — lo que automáticamente bloquea nuevos pedidos, porque
    /// BLL.Pedido.CrearPedido exige Cliente.TienePlan() — y señala si hay prendas en
    /// uso para solicitar la devolución (el pedido de devolución en sí es manual: el
    /// cliente es externo al sistema).
    /// </summary>
    public sealed class BajaSuscripcionHandler : ManejadorRenovacion
    {
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.IRenovacionDAL dalRenovacion;
        private readonly DAL.Prenda dalPrenda;

        public BajaSuscripcionHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.IRenovacionDAL dalRenovacion,
                                       DAL.Prenda dalPrenda)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalRenovacion = dalRenovacion ?? throw new ArgumentNullException(nameof(dalRenovacion));
            this.dalPrenda = dalPrenda ?? throw new ArgumentNullException(nameof(dalPrenda));
        }

        public override ResultadoRenovacion Procesar(ContextoRenovacion contexto)
        {
            var cliente = contexto.Cliente;
            int? idPlanAnterior = cliente.IdPlan;

            cliente.IdPlan           = null;
            cliente.NombrePlan       = null;
            cliente.FechaVencimiento = null;
            dalCliente.Modificar(cliente);

            var ahora = DateTime.Now;
            int idRenovacion = dalRenovacion.Alta(new BE.Renovacion
            {
                IdCliente = cliente.IdCliente,
                IdPlanAnterior = idPlanAnterior,
                IdPlanNuevo = null,
                FechaDeteccion = ahora,
                FechaResolucion = ahora,
                Resultado = BE.EstadoRenovacion.Baja,
                Actor = contexto.Actor
            });

            var prendasEnUso = dalPrenda.ObtenerPorCliente(cliente.IdCliente);
            string mensaje = "Suscripción dada de baja. El cliente no podrá generar nuevos pedidos.";
            if (prendasEnUso.Count > 0)
                mensaje += $" Tiene {prendasEnUso.Count} prenda(s) en uso — solicitar la devolución.";

            return new ResultadoRenovacion
            {
                Resuelto = true,
                Estado = BE.EstadoRenovacion.Baja,
                IdRenovacion = idRenovacion,
                Mensaje = mensaje
            };
        }
    }
}
