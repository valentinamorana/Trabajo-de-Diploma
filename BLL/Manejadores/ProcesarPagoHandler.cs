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
        // PN03: promociones vigentes que se aplican al cobro. Opcional (null = sin promociones).
        private readonly DAL.Interfaces.IPromocionDAL dalPromocion;

        public ProcesarPagoHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.ICobroDAL dalCobro,
                                    DAL.Interfaces.ICargoPrendaDAL dalCargoPrenda,
                                    DAL.Interfaces.IPromocionDAL dalPromocion = null)
        {
            this.dalPromocion = dalPromocion;
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

            // PN03 + NUULY 5.1: un solo descuento por ciclo — el mayor entre la promoción vigente del
            // plan y el crédito por referidos. Si gana la promoción, el crédito queda acumulado.
            var resDescuento = BE.PoliticaDescuento.Resolver(
                plan.Precio * BE.Builders.ModalidadCobroExtensiones.Meses(contexto.Modalidad), cliente.IdPlan, ObtenerPromocionesVigentes(), cliente.DescuentoProximoCobro, BE.Builders.ModalidadCobroExtensiones.Meses(contexto.Modalidad));
            decimal descuento = resDescuento.Descuento;
            var cargosPendientes = dalCargoPrenda.ObtenerPendientesPorCliente(cliente.IdCliente);
            decimal totalCargos = cargosPendientes.Sum(c => c.Monto);
            decimal importeFinal = resDescuento.Total + totalCargos;

            // El crédito por referido no usado queda acumulado: solo se descuenta lo aplicado.
            if (resDescuento.UsaCreditoReferido)
                cliente.DescuentoProximoCobro = Math.Max(0, cliente.DescuentoProximoCobro - resDescuento.Descuento);

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
            bool conDescuento = resDescuento.UsaCreditoReferido;
            bool conPromo = resDescuento.Promocion != null;
            bool conCargos = cargosPendientes.Count > 0;

            string clave;
            string mensaje;
            object[] args;

            if (conPromo && conCargos)
            {
                clave = "cobro.msg.cobrado.promoycargos";
                mensaje = $"Cobro registrado (${importeFinal}). Renovación confirmada: nueva vigencia hasta {suscripcion.FechaVencimiento:d}. " +
                          $"Incluye la promoción '{resDescuento.Promocion.Nombre}' (-${descuento}) y {cargosPendientes.Count} cargo(s) por daño/pérdida (${totalCargos}).";
                args = new object[] { importeFinal, suscripcion.FechaVencimiento, resDescuento.Promocion.Nombre, descuento, cargosPendientes.Count, totalCargos };
            }
            else if (conPromo)
            {
                clave = "cobro.msg.cobrado.promo";
                mensaje = $"Cobro registrado (${importeFinal}). Renovación confirmada: nueva vigencia hasta {suscripcion.FechaVencimiento:d}. " +
                          $"Incluye la promoción '{resDescuento.Promocion.Nombre}' (-${descuento}).";
                args = new object[] { importeFinal, suscripcion.FechaVencimiento, resDescuento.Promocion.Nombre, descuento };
            }
            else if (conDescuento && conCargos)
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

        // Best-effort: si la tabla Promocion no existe todavía (BD sin migrar) o falla la lectura, el
        // cobro sigue sin promociones en vez de romperse.
        private System.Collections.Generic.List<BE.Promocion> ObtenerPromocionesVigentes()
        {
            if (dalPromocion == null) return new System.Collections.Generic.List<BE.Promocion>();
            try { return dalPromocion.ObtenerVigentes(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[ProcesarPagoHandler] No se pudieron leer las promociones: {ex.Message}");
                try { new Servicios.Bitacora().Registrar("Cobro", $"No se pudieron leer las promociones vigentes; se cobró sin descuento: {ex.Message}", BE.Criticidad.Media); }
                catch { }
                return new System.Collections.Generic.List<BE.Promocion>();
            }
        }
    }
}
