using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Tercer eslabón: atiende el caso en que el cliente decide cambiar de plan al
    /// renovar. Si la decisión pedida no es CambiarPlan, delega al siguiente eslabón.
    /// Valida que el nuevo plan alcance para las prendas que ya tiene en uso.
    /// </summary>
    public sealed class CambioPlanHandler : ManejadorRenovacion
    {
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.PlanSuscripcion dalPlan;
        private readonly DAL.Interfaces.IRenovacionDAL dalRenovacion;

        public CambioPlanHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.PlanSuscripcion dalPlan,
                                  DAL.Interfaces.IRenovacionDAL dalRenovacion)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalPlan = dalPlan ?? throw new ArgumentNullException(nameof(dalPlan));
            this.dalRenovacion = dalRenovacion ?? throw new ArgumentNullException(nameof(dalRenovacion));
        }

        public override ResultadoRenovacion Procesar(ContextoRenovacion contexto)
        {
            if (contexto.Decision != DecisionRenovacion.CambiarPlan)
                return _sucesor.Procesar(contexto);

            if (!contexto.IdPlanNuevo.HasValue)
                throw new BE.AppException("err.bll.renovacion.plan_requerido",
                    "Debe indicar el nuevo plan para cambiar la suscripción.");

            var cliente = contexto.Cliente;
            int? idPlanAnterior = cliente.IdPlan;

            var planNuevo = dalPlan.ObtenerPorId(contexto.IdPlanNuevo.Value);
            if (planNuevo == null)
                throw new BE.AppException("err.bll.renovacion.plan_inexistente",
                    "El plan indicado no existe.");

            if (cliente.StockUtilizado > planNuevo.LimitePrendas)
                throw new BE.AppException("err.bll.renovacion.plan_insuficiente",
                    "No se puede cambiar al plan '{0}': el cliente tiene {1} prenda(s) en uso y ese plan solo permite {2}.",
                    planNuevo.Nombre, cliente.StockUtilizado, planNuevo.LimitePrendas);

            var builder = BE.Builders.SuscripcionBuilderFactory.Crear(contexto.Modalidad);
            var suscripcion = BE.Builders.DirectorSuscripcion.Construir(builder, cliente, planNuevo);

            cliente.IdPlan           = planNuevo.IdPlan;
            cliente.NombrePlan       = planNuevo.Nombre;
            cliente.LimitePrendas    = planNuevo.LimitePrendas;
            cliente.FechaVencimiento = suscripcion.FechaVencimiento;

            // UPDATE de Cliente + INSERT del historial en una única transacción (ver
            // IntentarRenovarHandler para el porqué).
            var ahora = DateTime.Now;
            int idRenovacion = 0;
            dalCliente.EjecutarTransaccion((conexion, tx) =>
            {
                dalCliente.ModificarEnTx(conexion, tx, cliente);
                idRenovacion = dalRenovacion.AltaEnTx(conexion, tx, new BE.Renovacion
                {
                    IdCliente = cliente.IdCliente,
                    IdPlanAnterior = idPlanAnterior,
                    IdPlanNuevo = planNuevo.IdPlan,
                    FechaDeteccion = ahora,
                    FechaResolucion = ahora,
                    Resultado = BE.EstadoRenovacion.CambioPlan,
                    Actor = contexto.Actor
                });
            });
            dalCliente.RecalcularDV();

            return new ResultadoRenovacion
            {
                Resuelto = true,
                Estado = BE.EstadoRenovacion.CambioPlan,
                IdRenovacion = idRenovacion,
                Mensaje = $"Plan cambiado a '{planNuevo.Nombre}'. Nueva vigencia hasta {suscripcion.FechaVencimiento:d}.",
                Clave = "renov.msg.cambioplan",
                Args = new object[] { planNuevo.Nombre, suscripcion.FechaVencimiento }
            };
        }
    }
}
