using System;
using System.Linq;

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
    ///
    /// Bloque 1 — este es también el único punto de cobro real del sistema, así que acá
    /// se liquidan el descuento por referido (Cliente.DescuentoProximoCobro, se consume
    /// completo en este cobro) y los cargos por daño/pérdida pendientes (CargoPrenda,
    /// Estado=Pendiente) del cliente: se suman al importe y se marcan Cobrados en la
    /// misma transacción.
    /// </summary>
    public sealed class ProcesarPagoHandler : ManejadorCobro
    {
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.ICobroDAL dalCobro;
        private readonly DAL.Interfaces.ICargoPrendaDAL dalCargoPrenda;

        public ProcesarPagoHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.ICobroDAL dalCobro,
                                    DAL.Interfaces.ICargoPrendaDAL dalCargoPrenda)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalCobro = dalCobro ?? throw new ArgumentNullException(nameof(dalCobro));
            this.dalCargoPrenda = dalCargoPrenda ?? throw new ArgumentNullException(nameof(dalCargoPrenda));
        }

        public override ResultadoCobro Procesar(ContextoCobro contexto)
        {
            if (contexto.Decision != DecisionCobro.Cobrado)
                return DelegarASucesor(contexto);

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

            decimal descuento = cliente.DescuentoProximoCobro;
            var cargosPendientes = dalCargoPrenda.ObtenerPendientesPorCliente(cliente.IdCliente);
            decimal totalCargos = cargosPendientes.Sum(c => c.Monto);
            decimal importeFinal = Math.Max(0, plan.Precio - descuento) + totalCargos;

            cliente.DescuentoProximoCobro = 0;

            // UPDATE de Cliente + INSERT del historial + liquidación de cargos pendientes, todo
            // en una única transacción: antes eran round-trips independientes, y un crash entre
            // medio podía dejar el historial de auditoría desincronizado del estado real.
            var ahora = DateTime.Now;
            int idCobro = 0;
            dalCliente.EjecutarTransaccion((conexion, tx) =>
            {
                dalCliente.ModificarEnTx(conexion, tx, cliente);
                idCobro = dalCobro.AltaEnTx(conexion, tx, new BE.Cobro
                {
                    IdCliente = cliente.IdCliente,
                    Importe = importeFinal,
                    FechaDeteccion = ahora,
                    FechaResolucion = ahora,
                    Resultado = BE.EstadoCobro.Cobrado,
                    Actor = contexto.Actor
                });
                if (cargosPendientes.Count > 0)
                    dalCargoPrenda.MarcarCobradosEnTx(conexion, tx,
                        cargosPendientes.Select(c => c.IdCargo).ToList(), ahora);
            });
            dalCliente.RecalcularDV();

            // Clave (y Mensaje de respaldo) distinta por combinación, igual que
            // BajaSuscripcionHandler con renov.msg.baja/renov.msg.baja_conprendas: el Mensaje
            // fijo en español es solo el respaldo si el corpus de traducciones no cargó — la
            // GUI siempre resuelve por Clave+Args primero (ver Traductor.Resolver), así que
            // concatenar texto extra sobre un Mensaje ya traducido lo perdería en los otros 3 idiomas.
            bool conDescuento = descuento > 0;
            bool conCargos = cargosPendientes.Count > 0;

            string clave;
            string mensaje;
            object[] args;

            if (conDescuento && conCargos)
            {
                clave = "cobro.msg.cobrado.descuentoycargos";
                mensaje = $"Cobro registrado (${importeFinal}). Renovación confirmada: nueva vigencia hasta {suscripcion.FechaVencimiento:d}. " +
                          $"Incluye descuento por referido de ${descuento} y {cargosPendientes.Count} cargo(s) por daño/pérdida (${totalCargos}).";
                args = new object[] { importeFinal, suscripcion.FechaVencimiento, descuento, cargosPendientes.Count, totalCargos };
            }
            else if (conDescuento)
            {
                clave = "cobro.msg.cobrado.descuento";
                mensaje = $"Cobro registrado (${importeFinal}). Renovación confirmada: nueva vigencia hasta {suscripcion.FechaVencimiento:d}. " +
                          $"Incluye descuento por referido de ${descuento}.";
                args = new object[] { importeFinal, suscripcion.FechaVencimiento, descuento };
            }
            else if (conCargos)
            {
                clave = "cobro.msg.cobrado.cargos";
                mensaje = $"Cobro registrado (${importeFinal}). Renovación confirmada: nueva vigencia hasta {suscripcion.FechaVencimiento:d}. " +
                          $"Incluye {cargosPendientes.Count} cargo(s) por daño/pérdida (${totalCargos}).";
                args = new object[] { importeFinal, suscripcion.FechaVencimiento, cargosPendientes.Count, totalCargos };
            }
            else
            {
                clave = "cobro.msg.cobrado";
                mensaje = $"Cobro registrado (${importeFinal}). Renovación confirmada: nueva vigencia hasta {suscripcion.FechaVencimiento:d}.";
                args = new object[] { importeFinal, suscripcion.FechaVencimiento };
            }

            return new ResultadoCobro
            {
                Resuelto = true,
                Estado = BE.EstadoCobro.Cobrado,
                IdCobro = idCobro,
                Mensaje = mensaje,
                Clave = clave,
                Args = args
            };
        }
    }
}
