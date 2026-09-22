using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para PN02 — Comercialización de la suscripción.
    /// Venta capta al cliente y el plan elegido (CrearContratacion); Caja cobra y recién
    /// ahí se formaliza la suscripción (ConfirmarPago llama a BLL.Cliente.ActivarSuscripcion).
    /// Caja es un rol real y propio, separado de Vendedor — ver decisión de diseño en el
    /// roadmap de procesos de negocio nuevos.
    /// </summary>
    public class Contratacion : Interfaces.IContratacionService
    {
        private const int MaxIntentosPago = 3;

        private readonly DAL.Interfaces.IContratacionDAL    dalContratacion;
        private readonly DAL.Interfaces.IClienteDAL         dalCliente;
        private readonly DAL.Interfaces.IEmpleadoDAL        dalEmpleado;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;
        // PN03: promociones vigentes que se aplican al importe del cobro. Opcional (null = sin promociones).
        private DAL.Interfaces.IPromocionDAL dalPromocion;
        private readonly Servicios.Bitacora        bitacora    = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();

        // BLL.Cliente es quien formaliza la suscripción (ActivarSuscripcion, PdN1) — composición
        // lazy, mismo criterio que BLL.Pedido.prendaBLL / BLL.Prenda.listaEsperaBLL.
        private Interfaces.IClienteService _clienteBLLLazy;
        private Interfaces.IClienteService clienteBLL => _clienteBLLLazy ?? (_clienteBLLLazy = new Cliente());

        // DI: el constructor por defecto usa los DAL reales; el otro permite inyectar dobles
        // de prueba (mismo criterio que BLL.Pedido/BLL.Cliente).
        public Contratacion() : this(new DAL.Contratacion(), new DAL.Cliente(), new DAL.Empleado(), new DAL.PlanSuscripcion())
        {
            dalPromocion = new DAL.Promocion();
        }

        public Contratacion(DAL.Interfaces.IContratacionDAL dalContratacion, DAL.Interfaces.IClienteDAL dalCliente,
                             DAL.Interfaces.IEmpleadoDAL dalEmpleado, DAL.Interfaces.IPlanSuscripcionDAL dalPlan)
        {
            this.dalContratacion = dalContratacion ?? throw new ArgumentNullException(nameof(dalContratacion));
            this.dalCliente      = dalCliente      ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalEmpleado     = dalEmpleado     ?? throw new ArgumentNullException(nameof(dalEmpleado));
            this.dalPlan         = dalPlan         ?? throw new ArgumentNullException(nameof(dalPlan));
        }

        // Overload para inyectar un doble de prueba de BLL.Cliente sin tocar el constructor
        // de 4 parámetros usado en el resto de los tests.
        public Contratacion(DAL.Interfaces.IContratacionDAL dalContratacion, DAL.Interfaces.IClienteDAL dalCliente,
                             DAL.Interfaces.IEmpleadoDAL dalEmpleado, DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                             Interfaces.IClienteService clienteBLL)
            : this(dalContratacion, dalCliente, dalEmpleado, dalPlan)
        {
            _clienteBLLLazy = clienteBLL ?? throw new ArgumentNullException(nameof(clienteBLL));
        }

        // Overload que además inyecta el DAL de promociones (pruebas del descuento aplicado al cobro).
        public Contratacion(DAL.Interfaces.IContratacionDAL dalContratacion, DAL.Interfaces.IClienteDAL dalCliente,
                             DAL.Interfaces.IEmpleadoDAL dalEmpleado, DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                             Interfaces.IClienteService clienteBLL, DAL.Interfaces.IPromocionDAL dalPromocion)
            : this(dalContratacion, dalCliente, dalEmpleado, dalPlan, clienteBLL)
        {
            this.dalPromocion = dalPromocion;
        }

        // Cola de contrataciones a cobrar (pantalla de Caja).
        public List<BE.Contratacion> ObtenerPendientesDePago() => dalContratacion.ObtenerPendientesDePago();

        public BE.Contratacion ObtenerPorId(int idContratacion) => dalContratacion.ObtenerPorId(idContratacion);

        // CU01-VTA-Gestionar Suscripción (PN02): Venta capta al cliente y el plan elegido y
        // deja la contratación pendiente de pago. La suscripción NO queda vigente todavía —
        // recién se formaliza cuando Caja confirma el pago (ConfirmarPago).
        public int CrearContratacion(string modulo, int idCliente, int idPlan, BE.Builders.ModalidadCobro modalidad)
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);

            var cliente = dalCliente.ObtenerPorId(idCliente);
            if (cliente == null)
                throw new BE.AppException("err.bll.contratacion.cliente_inexistente",
                    "El cliente seleccionado no existe.");

            var plan = dalPlan.ObtenerPorId(idPlan);
            if (plan == null || !plan.Estado)
                throw new BE.AppException("err.bll.contratacion.plan_inexistente",
                    "El plan seleccionado no existe o no está activo.");

            ValidarCupo(cliente, plan);

            // Evita duplicar la contratación si Venta reenvía el formulario (doble click, reintento
            // tras no ver confirmación, etc.): un cliente no puede tener dos contrataciones pendientes
            // de pago a la vez.
            if (dalContratacion.ObtenerPendientesDePago().Exists(c => c.IdCliente == idCliente))
                throw new BE.AppException("err.bll.contratacion.pendiente_existente",
                    "Este cliente ya tiene una contratación pendiente de pago. " +
                    "Hay que resolverla (cobrarla o cancelarla) antes de registrar una nueva.");

            var contratacion = new BE.Contratacion
            {
                IdCliente  = idCliente,
                IdPlan     = idPlan,
                IdVendedor = BLLHelper.ResolverEmpleadoActivo(dalEmpleado),
                Modalidad  = modalidad,
                Estado     = BE.EstadoContratacion.PendientePago,
                FechaAlta  = DateTime.Now
            };

            int idNuevo = dalContratacion.Alta(contratacion);

            bitacora.Registrar(modulo,
                $"Nueva contratación #{idNuevo} — Cliente: {cliente.NombreCompleto} — Plan: {plan.Nombre} — Modalidad: {modalidad}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Contratación #{idNuevo} pendiente de pago — {cliente.NombreCompleto} — Plan {plan.Nombre}",
                idCliente: idCliente);

            return idNuevo;
        }

        // CU01-CAJ-Gestionar Cobro + CU02-CAJ-Emitir Comprobante (PN02): Caja confirma el
        // pago, emite el comprobante y formaliza la suscripción del cliente.
        public BE.LiquidacionContratacion ConfirmarPago(string modulo, BE.Contratacion contratacion, string medioPago)
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);

            // Revalida contra el estado fresco de la BD, no el objeto que trae el caller: cubre
            // doble clic / dos sesiones de Caja cobrando la misma contratación al mismo tiempo.
            var actual = dalContratacion.ObtenerPorId(contratacion.IdContratacion);
            if (actual == null)
                throw new BE.AppException("err.bll.contratacion.inexistente", "La contratación ya no existe.");
            if (!actual.PuedeCobrarse())
                throw new BE.AppException("err.bll.contratacion.cobrar_estado",
                    "Solo se pueden cobrar contrataciones Pendientes de pago. Esta contratación está '{0}'.",
                    actual.Estado);

            if (string.IsNullOrWhiteSpace(medioPago))
                throw new BE.AppException("err.bll.contratacion.medio_pago_requerido",
                    "Debe indicar el medio de pago (efectivo, tarjeta o transferencia).");

            // Flujo alternativo 4.1 (PN02): el plan fue dado de baja entre que Venta generó la
            // contratación y que Caja la cobra. Se rechaza ANTES de tocar nada: la contratación
            // permanece Pendiente de pago y se puede reintentar una vez resuelta la causa.
            var plan = dalPlan.ObtenerPorId(actual.IdPlan);
            if (plan == null || !plan.Estado)
                throw new BE.AppException("err.bll.contratacion.plan_baja",
                    "El plan '{0}' fue dado de baja: no se puede formalizar la suscripción. " +
                    "La contratación queda pendiente de pago.",
                    actual.NombrePlan ?? plan?.Nombre ?? actual.IdPlan.ToString());

            int idCaja = BLLHelper.ResolverEmpleadoActivo(dalEmpleado);
            string numeroComprobante = GenerarNumeroComprobante(contratacion.IdContratacion);

            // PN03 — importe a cobrar: precio del plan menos UN solo descuento (la promoción vigente
            // del plan o el crédito por referidos, el mayor; ver BE.PoliticaDescuento).
            var cliente = dalCliente.ObtenerPorId(actual.IdCliente);
            if (cliente == null)
                throw new BE.AppException("err.bll.contratacion.cliente_inexistente",
                    "El cliente seleccionado no existe.");
            ValidarCupo(cliente, plan);
            var descuento = ResolverDescuento(actual, plan, cliente);

            // 1) "Claim" atómico: el UPDATE exige que la contratación siga PendientePago, así que
            //    entre dos sesiones de Caja (o un doble clic) solo UNA pasa. Sin esto, las dos
            //    superaban la revalidación de arriba y activaban la suscripción dos veces.
            if (!dalContratacion.ConfirmarPago(actual.IdContratacion, idCaja, medioPago, numeroComprobante,
                    descuento.Total, descuento.Descuento, descuento.Promocion?.IdPromocion))
                throw new BE.AppException("err.bll.contratacion.cobrar_concurrente",
                    "Otra sesión de Caja ya resolvió esta contratación. Actualizá la cola de Caja.");

            // 2) Activar la suscripción. Si falla, se compensa devolviendo la contratación a
            //    Pendiente de pago para que Caja pueda reintentar (nunca queda Pagada sin
            //    suscripción activada).
            //
            // Riesgo residual aceptado: el claim y la activación no comparten una única
            // transacción de BD (Contratacion y Cliente son tablas distintas). Si el proceso
            // cae justo entre ambos pasos la contratación queda Pagada sin suscripción activa:
            // se detecta en la cola de Caja/Renovación y se corrige con el alta administrativa
            // del plan. Ventana angosta; una solución completa requeriría una transacción que
            // cruce ambos DAL, fuera de alcance de este TP.
            try
            {
                // Si el descuento aplicado fue el crédito por referidos, se consume; si fue una
                // promoción, el crédito queda acumulado (un solo descuento por ciclo). La activación
                // persiste al cliente completo, así que el consumo se guarda junto con el alta.
                if (descuento.UsaCreditoReferido)
                    cliente.DescuentoProximoCobro = Math.Max(0, cliente.DescuentoProximoCobro - descuento.Descuento);
                clienteBLL.ActivarSuscripcionDesdeContratacion(modulo, cliente, actual.IdPlan, actual.Modalidad,
                    descuento.UsaCreditoReferido ? descuento.Descuento : 0m);
            }
            catch
            {
                try { dalContratacion.ReabrirPago(actual.IdContratacion); }
                catch (Exception ex)
                {
                    // Peor caso: el cobro quedó registrado y NO se pudo reabrir (por ejemplo Venta
                    // creó otra contratación pendiente para el cliente y el índice único lo impide).
                    // No se puede tragar en silencio: se deja constancia de alta criticidad en la
                    // bitácora y se avisa a Caja con un error propio, en vez del de la activación.
                    System.Diagnostics.Trace.TraceError(
                        $"[BLL.Contratacion] No se pudo reabrir la contratación #{actual.IdContratacion}: {ex.Message}");
                    try
                    {
                        bitacora.Registrar(modulo,
                            $"CRÍTICO: Contratación #{actual.IdContratacion} cobrada pero la suscripción NO se activó " +
                            $"y no se pudo reabrir el cobro — Cliente: {actual.NombreCliente}. Requiere alta administrativa del plan.",
                            BE.Criticidad.Alta);
                    }
                    catch { /* la bitácora no puede impedir el aviso a Caja */ }
                    throw new BE.AppException("err.bll.contratacion.cobro_sin_activar",
                        "El cobro de la contratación #{0} quedó registrado pero la suscripción NO se activó. " +
                        "Avisá al Administrador para completar el alta del plan.",
                        actual.IdContratacion);
                }
                throw;
            }

            bitacora.Registrar(modulo,
                $"Cobro Contratación #{contratacion.IdContratacion} — Cliente: {contratacion.NombreCliente} — " +
                $"Medio: {medioPago} — Comprobante: {numeroComprobante}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.CobroSuscripcion,
                $"Contratación #{contratacion.IdContratacion} cobrada y suscripción formalizada — " +
                $"{contratacion.NombreCliente} — Plan {contratacion.NombrePlan} — Comprobante {numeroComprobante} — " +
                $"Importe ${descuento.Total}" +
                (descuento.Descuento > 0
                    ? $" (descuento ${descuento.Descuento}: {(descuento.Promocion != null ? $"promoción '{descuento.Promocion.Nombre}'" : "crédito por referido")})"
                    : ""),
                idCliente: contratacion.IdCliente);

            return new BE.LiquidacionContratacion
            {
                Bruto = descuento.Bruto,
                Descuento = descuento.Descuento,
                NombrePromocion = descuento.Promocion?.Nombre,
                UsaCreditoReferido = descuento.UsaCreditoReferido,
                NumeroComprobante = numeroComprobante
            };
        }

        // Importe que Caja debe cobrar por una contratación pendiente, con el descuento aplicable
        // (para mostrarlo en la cola de Caja y en la confirmación, antes de cobrar).
        public BE.LiquidacionContratacion CalcularImporte(BE.Contratacion contratacion)
        {
            var plan = dalPlan.ObtenerPorId(contratacion.IdPlan);
            var cliente = dalCliente.ObtenerPorId(contratacion.IdCliente);
            var r = ResolverDescuento(contratacion, plan, cliente);
            return new BE.LiquidacionContratacion
            {
                Bruto = r.Bruto,
                Descuento = r.Descuento,
                NombrePromocion = r.Promocion?.Nombre,
                UsaCreditoReferido = r.UsaCreditoReferido
            };
        }

        // Igual que CalcularImporte pero para toda la cola de Caja: las promociones vigentes se leen UNA vez
        // (antes eran 3 consultas por fila en cada refresco de la grilla).
        public Dictionary<int, BE.LiquidacionContratacion> CalcularImportes(List<BE.Contratacion> contrataciones)
        {
            var resultado = new Dictionary<int, BE.LiquidacionContratacion>();
            if (contrataciones == null || contrataciones.Count == 0) return resultado;

            var promos = ObtenerPromocionesVigentes();
            foreach (var c in contrataciones)
            {
                var plan = dalPlan.ObtenerPorId(c.IdPlan);
                var cliente = dalCliente.ObtenerPorId(c.IdCliente);
                var r = BE.PoliticaDescuento.Resolver(
                    (plan != null ? plan.Precio : c.MontoPlan) * BE.Builders.ModalidadCobroExtensiones.Meses(c.Modalidad), c.IdPlan, promos, cliente?.DescuentoProximoCobro ?? 0m, BE.Builders.ModalidadCobroExtensiones.Meses(c.Modalidad));
                resultado[c.IdContratacion] = new BE.LiquidacionContratacion
                {
                    Bruto = r.Bruto, Descuento = r.Descuento,
                    NombrePromocion = r.Promocion?.Nombre, UsaCreditoReferido = r.UsaCreditoReferido
                };
            }
            return resultado;
        }

        // El plan elegido debe tener capacidad para las prendas que el cliente ya tiene en uso
        // (misma regla que BLL.Cliente.Modificar): sin esto, contratar un plan más chico dejaba
        // al cliente por encima de su cupo.
        private static void ValidarCupo(BE.Cliente cliente, BE.PlanSuscripcion plan)
        {
            if (cliente != null && plan != null && cliente.StockUtilizado > plan.LimitePrendas)
                throw new BE.AppException("err.bll.cliente.plan_insuficiente",
                    "No se puede asignar el plan '{0}': el cliente tiene {1} prenda(s) en uso y ese plan solo permite {2}. Registrá las devoluciones primero.",
                    plan.Nombre, cliente.StockUtilizado, plan.LimitePrendas);
        }

        // Precio mensual del plan × meses de la modalidad (NUULY cobra por mes; sin descuento por modalidad)
        // menos un único descuento: promoción vigente del plan o crédito por referido.
        private BE.ResultadoDescuento ResolverDescuento(BE.Contratacion c, BE.PlanSuscripcion plan, BE.Cliente cliente)
        {
            decimal bruto = (plan != null ? plan.Precio : c.MontoPlan) * BE.Builders.ModalidadCobroExtensiones.Meses(c.Modalidad);
            return BE.PoliticaDescuento.Resolver(
                bruto, c.IdPlan, ObtenerPromocionesVigentes(), cliente?.DescuentoProximoCobro ?? 0m, BE.Builders.ModalidadCobroExtensiones.Meses(c.Modalidad));
        }

        // Best-effort: sin DAL de promociones, o si la tabla aún no existe, se cobra sin promociones.
        private List<BE.Promocion> ObtenerPromocionesVigentes()
        {
            if (dalPromocion == null) return new List<BE.Promocion>();
            try { return dalPromocion.ObtenerVigentes(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[BLL.Contratacion] No se pudieron leer las promociones: {ex.Message}");
                try { bitacora.Registrar("Contratación", $"No se pudieron leer las promociones vigentes; se cobró sin descuento: {ex.Message}", BE.Criticidad.Media); }
                catch { }
                return new List<BE.Promocion>();
            }
        }

        // CU03-CAJ-Cancelar Contratación (PN02): un intento de pago que no se concretó. Al
        // llegar al máximo permitido, cancela automáticamente la contratación.
        public void RegistrarIntentoFallido(string modulo, BE.Contratacion contratacion)
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);

            // Mismo motivo que en ConfirmarPago: revalida contra el estado fresco de la BD, no
            // el objeto que trae el caller. Sin esto, una grilla de Caja desactualizada podría
            // registrar un intento fallido (y hasta cancelar) sobre una contratación que otra
            // sesión de Caja ya cobró en el ínterin.
            var actual = dalContratacion.ObtenerPorId(contratacion.IdContratacion);
            if (actual == null)
                throw new BE.AppException("err.bll.contratacion.inexistente", "La contratación ya no existe.");
            if (!actual.PuedeCobrarse())
                throw new BE.AppException("err.bll.contratacion.cobrar_estado",
                    "Solo se pueden registrar intentos sobre contrataciones Pendientes de pago. Esta contratación está '{0}'.",
                    actual.Estado);

            int intentos = dalContratacion.IncrementarIntento(actual.IdContratacion);
            if (intentos < 0)
                throw new BE.AppException("err.bll.contratacion.cobrar_concurrente",
                    "Otra sesión de Caja ya resolvió esta contratación. Actualizá la cola de Caja.");

            bitacora.Registrar(modulo,
                $"Intento de pago fallido en Contratación #{contratacion.IdContratacion} — Cliente: {contratacion.NombreCliente} — " +
                $"Intento {intentos} de {MaxIntentosPago}",
                BE.Criticidad.Baja);

            if (intentos >= MaxIntentosPago)
            {
                dalContratacion.Cancelar(actual.IdContratacion);

                bitacora.Registrar(modulo,
                    $"Cancelar Contratación #{contratacion.IdContratacion} — Cliente: {contratacion.NombreCliente} — " +
                    "Máximo de intentos de pago agotado",
                    BE.Criticidad.Media);
                bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                    $"Contratación #{contratacion.IdContratacion} cancelada — {contratacion.NombreCliente} — " +
                    $"máximo de {MaxIntentosPago} intentos de pago agotado sin concretarse",
                    idCliente: contratacion.IdCliente);
            }
        }

        // Número de comprobante simple y legible: prefijo + ID de contratación + fecha.
        // No requiere una entidad Comprobante propia (ver BE.Contratacion).
        private string GenerarNumeroComprobante(int idContratacion)
            => $"CMP-{idContratacion:D6}-{DateTime.Now:yyyyMMdd}";

    }
}
