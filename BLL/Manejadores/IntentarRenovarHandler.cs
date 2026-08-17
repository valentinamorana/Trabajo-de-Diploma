using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Segundo eslabón: atiende el caso en que el cliente decide renovar con el mismo
    /// plan. Si la decisión pedida no es Renovar, delega al siguiente eslabón.
    /// Reutiliza el patrón Builder (PdN1) para calcular la nueva vigencia.
    /// </summary>
    public sealed class IntentarRenovarHandler : ManejadorRenovacion
    {
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.IRenovacionDAL dalRenovacion;

        public IntentarRenovarHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.IRenovacionDAL dalRenovacion)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalRenovacion = dalRenovacion ?? throw new ArgumentNullException(nameof(dalRenovacion));
        }

        public override ResultadoRenovacion Procesar(ContextoRenovacion contexto)
        {
            if (contexto.Decision != DecisionRenovacion.Renovar)
                return _sucesor.Procesar(contexto);

            var cliente = contexto.Cliente;
            int? idPlanAnterior = cliente.IdPlan;

            var plan = new BE.PlanSuscripcion
            {
                IdPlan = cliente.IdPlan ?? 0,
                Nombre = cliente.NombrePlan,
                LimitePrendas = cliente.LimitePrendas
            };
            var builder = BE.Builders.SuscripcionBuilderFactory.Crear(contexto.Modalidad);
            var suscripcion = BE.Builders.DirectorSuscripcion.Construir(builder, cliente, plan);

            cliente.FechaVencimiento = suscripcion.FechaVencimiento;
            dalCliente.Modificar(cliente);

            var ahora = DateTime.Now;
            int idRenovacion = dalRenovacion.Alta(new BE.Renovacion
            {
                IdCliente = cliente.IdCliente,
                IdPlanAnterior = idPlanAnterior,
                IdPlanNuevo = idPlanAnterior,
                FechaDeteccion = ahora,
                FechaResolucion = ahora,
                Resultado = BE.EstadoRenovacion.Renovada,
                Actor = contexto.Actor
            });

            return new ResultadoRenovacion
            {
                Resuelto = true,
                Estado = BE.EstadoRenovacion.Renovada,
                IdRenovacion = idRenovacion,
                Mensaje = $"Suscripción renovada. Nueva vigencia hasta {suscripcion.FechaVencimiento:d}.",
                Clave = "renov.msg.renovada",
                Args = new object[] { suscripcion.FechaVencimiento }
            };
        }
    }
}
