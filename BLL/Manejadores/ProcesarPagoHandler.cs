using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Segundo eslabón: atiende el caso en que el cobro se realizó con éxito. Si la
    /// decisión pedida no es Cobrado, delega al siguiente eslabón. Un cobro exitoso
    /// CONFIRMA LA RENOVACIÓN (PdN6 dice explícitamente "procesar el pago y confirmar
    /// la renovación"): reutiliza el mismo patrón Builder (PdN1) que usa
    /// IntentarRenovarHandler para extender la vigencia, con el mismo criterio de no
    /// consultar PlanSuscripcion de nuevo — arma el plan a partir de los datos ya
    /// cacheados en Cliente (NombrePlan/LimitePrendas/PrecioPlan, cargados por JOIN).
    /// </summary>
    public sealed class ProcesarPagoHandler : ManejadorCobro
    {
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.ICobroDAL dalCobro;

        public ProcesarPagoHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.ICobroDAL dalCobro)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalCobro = dalCobro ?? throw new ArgumentNullException(nameof(dalCobro));
        }

        public override ResultadoCobro Procesar(ContextoCobro contexto)
        {
            if (contexto.Decision != DecisionCobro.Cobrado)
                return _sucesor.Procesar(contexto);

            var cliente = contexto.Cliente;

            var plan = new BE.PlanSuscripcion
            {
                IdPlan = cliente.IdPlan ?? 0,
                Nombre = cliente.NombrePlan,
                LimitePrendas = cliente.LimitePrendas,
                Precio = cliente.PrecioPlan
            };
            var builder = BE.Builders.SuscripcionBuilderFactory.Crear(contexto.Modalidad);
            var suscripcion = BE.Builders.DirectorSuscripcion.Construir(builder, cliente, plan);

            cliente.FechaVencimiento = suscripcion.FechaVencimiento;
            cliente.FechaLimiteGracia = null;
            dalCliente.Modificar(cliente);

            var ahora = DateTime.Now;
            int idCobro = dalCobro.Alta(new BE.Cobro
            {
                IdCliente = cliente.IdCliente,
                Importe = plan.Precio,
                FechaDeteccion = ahora,
                FechaResolucion = ahora,
                Resultado = BE.EstadoCobro.Cobrado,
                Actor = contexto.Actor
            });

            return new ResultadoCobro
            {
                Resuelto = true,
                Estado = BE.EstadoCobro.Cobrado,
                IdCobro = idCobro,
                Mensaje = $"Cobro registrado (${plan.Precio}). Renovación confirmada: nueva vigencia hasta {suscripcion.FechaVencimiento:d}."
            };
        }
    }
}
