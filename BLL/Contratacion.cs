using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para PN02 — Comercialización de la suscripción.
    ///
    /// Cada método público corresponde a una actividad del diagrama de actividad de PN02
    /// (adaptado a WardrobeFlow; carriles Cliente, Vendedor y Caja):
    ///
    ///   Vendedor  Identificar cliente → ¿Registrado? ............... IdentificarCliente
    ///             Presentar planes («Planes disponibles») ............ PresentarPlanes
    ///             ¿Elige plan y modalidad? No → Asentar desistimiento . AsentarDesistimiento
    ///             Registrar contratación → ¿Contratación válida? ..... ValidarContratacion / RegistrarContratacion
    ///   Caja      Consultar cola ..................................... ObtenerPendientesDePago
    ///             Calcular importe («Liquidación») ................... CalcularImporte
    ///             ¿Se concreta el pago? Sí → Confirmar cobro →
    ///               Emitir comprobante → Activar suscripción →
    ///               ¿Referido? Sí → Acreditar crédito ................ ConfirmarCobro
    ///             ¿Se concreta el pago? No → Registrar intento →
    ///               ¿Alcanzó el máximo de 3 intentos? Sí → Cancelar .. RegistrarIntentoFallido
    ///
    /// Caja es un rol propio, separado de Vendedor (separación de funciones): Vendedor registra
    /// la contratación y Caja cobra; recién con el cobro la suscripción queda vigente.
    /// </summary>
    public class Contratacion : Interfaces.IContratacionService
    {
        private const string ModuloContratacion = "Contratación";

        private readonly DAL.Interfaces.IContratacionDAL    dalContratacion;
        private readonly DAL.Interfaces.IClienteDAL         dalCliente;
        private readonly DAL.Interfaces.IEmpleadoDAL        dalEmpleado;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;
        // PN03: promociones vigentes que se aplican al importe del cobro. Opcional (null = sin promociones).
        private DAL.Interfaces.IPromocionDAL dalPromocion;
        // PN04: cargos por daño o pérdida pendientes que se suman al cobro. Opcional (null = sin cargos).
        internal DAL.Interfaces.ICargoPrendaDAL DalCargos { get; set; }
        private readonly Servicios.IRegistroBitacora        bitacora    = Servicios.FabricaBitacora.CrearSistema();
        private readonly Servicios.IRegistroBitacoraNegocio bitacoraNeg = Servicios.FabricaBitacora.CrearNegocio();

        // BLL.Cliente es quien activa la suscripción (Builder) y acredita el referido — composición
        // lazy, mismo criterio que BLL.Pedido.prendaBLL / BLL.Prenda.listaEsperaBLL.
        private Interfaces.IClienteService _clienteBLLLazy;
        private Interfaces.IClienteService clienteBLL => _clienteBLLLazy ?? (_clienteBLLLazy = new Cliente());

        // DI: el constructor por defecto usa los DAL reales; los otros permiten inyectar dobles.
        public Contratacion() : this(new DAL.Contratacion(), new DAL.Cliente(), new DAL.Empleado(), new DAL.PlanSuscripcion())
        {
            dalPromocion = new DAL.Promocion();
            DalCargos    = new DAL.CargoPrenda();
        }

        public Contratacion(DAL.Interfaces.IContratacionDAL dalContratacion, DAL.Interfaces.IClienteDAL dalCliente,
                             DAL.Interfaces.IEmpleadoDAL dalEmpleado, DAL.Interfaces.IPlanSuscripcionDAL dalPlan)
        {
            this.dalContratacion = dalContratacion ?? throw new ArgumentNullException(nameof(dalContratacion));
            this.dalCliente      = dalCliente      ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalEmpleado     = dalEmpleado     ?? throw new ArgumentNullException(nameof(dalEmpleado));
            this.dalPlan         = dalPlan         ?? throw new ArgumentNullException(nameof(dalPlan));
        }

        public Contratacion(DAL.Interfaces.IContratacionDAL dalContratacion, DAL.Interfaces.IClienteDAL dalCliente,
                             DAL.Interfaces.IEmpleadoDAL dalEmpleado, DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                             Interfaces.IClienteService clienteBLL)
            : this(dalContratacion, dalCliente, dalEmpleado, dalPlan)
        {
            _clienteBLLLazy = clienteBLL ?? throw new ArgumentNullException(nameof(clienteBLL));
        }

        public Contratacion(DAL.Interfaces.IContratacionDAL dalContratacion, DAL.Interfaces.IClienteDAL dalCliente,
                             DAL.Interfaces.IEmpleadoDAL dalEmpleado, DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                             Interfaces.IClienteService clienteBLL, DAL.Interfaces.IPromocionDAL dalPromocion)
            : this(dalContratacion, dalCliente, dalEmpleado, dalPlan, clienteBLL)
        {
            this.dalPromocion = dalPromocion;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Carril Vendedor
        // ══════════════════════════════════════════════════════════════════════

        // "Identificar cliente" → ¿Registrado? Busca por DNI exacto o por nombre/apellido parcial.
        // Lista vacía = No registrado (el Vendedor lo registra en el ABM de Clientes).
        public List<BE.Cliente> IdentificarCliente(string identificacion)
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            return clienteBLL.BuscarPorIdentificacion(identificacion);
        }

        // "Presentar planes" («Planes disponibles»: nombre, precio mensual y límite de prendas).
        public List<BE.PlanSuscripcion> PresentarPlanes()
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            return dalPlan.ObtenerActivos();
        }

        // "¿Elige plan y modalidad? No → Asentar desistimiento" («Aviso de desistimiento»). El
        // cliente ya fue identificado; el plan y la modalidad son los que estaba considerando,
        // si llegó a elegirlos. No genera contratación. Devuelve el ID del desistimiento.
        // DesistimientoContratacion.Motivo y ContratacionIntentoPago.Motivo son NVARCHAR(200)
        // (BD/00_Instalacion_Completa.sql): un texto más largo haría fallar la escritura.
        public const int LargoMaximoMotivo = 200;

        private static void ValidarLargoMotivo(string motivo)
        {
            if (motivo != null && motivo.Trim().Length > LargoMaximoMotivo)
                throw new BE.AppException("err.bll.contratacion.motivo_largo",
                    "El motivo no puede superar los {0} caracteres.", LargoMaximoMotivo);
        }

        public int AsentarDesistimiento(string modulo, int idCliente, int? idPlan,
                                        BE.Builders.ModalidadCobro? modalidad, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.contratacion.desistir_sin_motivo",
                    "Es obligatorio indicar el motivo del desistimiento que comunicó el cliente.");
            ValidarLargoMotivo(motivo);

            var cliente = dalCliente.ObtenerPorId(idCliente)
                ?? throw new BE.AppException("err.bll.contratacion.cliente_inexistente", "El cliente seleccionado no existe.");
            BE.PlanSuscripcion plan = idPlan.HasValue ? dalPlan.ObtenerPorId(idPlan.Value) : null;

            var desistimiento = new BE.DesistimientoContratacion
            {
                IdCliente  = idCliente,
                IdPlan     = plan?.IdPlan,
                Modalidad  = plan != null ? modalidad : null,
                Motivo     = motivo.Trim(),
                Fecha      = DateTime.Now,
                IdVendedor = BLLHelper.ResolverEmpleadoActivo(dalEmpleado)
            };
            int id = dalContratacion.AltaDesistimiento(desistimiento);

            bitacora.Registrar(modulo,
                $"Desistimiento de contratación #{id} — Cliente: {cliente.NombreCompleto} — " +
                $"Plan: {plan?.Nombre ?? "—"} — Motivo: {desistimiento.Motivo}",
                BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Desistimiento,
                $"El cliente {cliente.NombreCompleto} no contrató — Plan considerado: {plan?.Nombre ?? "ninguno"} — {desistimiento.Motivo}",
                idCliente: idCliente);
            return id;
        }

        // "¿Contratación válida?": el cliente existe y está activo, el plan está activo, el plan
        // alcanza para las prendas que el cliente ya tiene en uso y no hay otra contratación
        // pendiente. Si no, lanza el motivo ("Informar motivo"), que queda en la bitácora.
        // Es una consulta: la pantalla la usa para informar el motivo mientras el Vendedor elige el
        // plan, así que no escribe en la bitácora (el intento fallido se registra en RegistrarContratacion).
        public BE.PlanSuscripcion ValidarContratacion(int idCliente, int idPlan)
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            {
                var cliente = dalCliente.ObtenerPorId(idCliente);
                if (cliente == null)
                    throw new BE.AppException("err.bll.contratacion.cliente_inexistente",
                        "El cliente seleccionado no existe.");

                var plan = dalPlan.ObtenerPorId(idPlan);
                if (plan == null || !plan.Estado)
                    throw new BE.AppException("err.bll.contratacion.plan_inexistente",
                        "El plan seleccionado no existe o no está activo.");

                ValidarCupo(cliente, plan);

                // Plan igual o más barato con el período vigente: rige al vencer, así que no se registra
                // ahora (antes el plan y su límite cambiaban el mismo día del cobro).
                var planActual = cliente.IdPlan.HasValue && cliente.IdPlan.Value != idPlan
                    ? dalPlan.ObtenerPorId(cliente.IdPlan.Value) : null;
                if (Politicas.PoliticaCambioPlan.EsCambioSinUpgradeConPeriodoVigente(cliente, planActual, idPlan, plan.Precio, DateTime.Today))
                    throw new BE.AppException("err.bll.contratacion.cambio_plan_vigente",
                        "El cambio a un plan igual o más barato rige al vencer el período pagado de '{0}' (el {1:d}). Registralo a partir de esa fecha.",
                        planActual.Nombre, Politicas.PoliticaCambioPlan.VencimientoPagado(cliente, DateTime.Today));

                // Un cliente no puede tener dos contrataciones pendientes de pago a la vez (además
                // lo garantiza el índice único UX_Contratacion_UnaPendientePorCliente).
                if (dalContratacion.ObtenerPendientesDePago().Exists(c => c.IdCliente == idCliente))
                    throw new BE.AppException("err.bll.contratacion.pendiente_existente",
                        "Este cliente ya tiene una contratación pendiente de pago. " +
                        "Hay que resolverla (cobrarla o cancelarla) antes de registrar una nueva.");
                return plan;
            }
        }

        // "Registrar contratación" → ¿Contratación válida? Sí: «Orden de cobro» (la contratación
        // queda Pendiente de pago en la cola de Caja). La suscripción NO queda vigente todavía.
        public int RegistrarContratacion(string modulo, int idCliente, int idPlan, BE.Builders.ModalidadCobro modalidad)
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            BE.PlanSuscripcion plan;
            try { plan = ValidarContratacion(idCliente, idPlan); }
            catch (BE.AppException ex)
            {
                // ¿Contratación válida? No → "Informar motivo": queda asentado el intento.
                try
                {
                    bitacora.Registrar(modulo,
                        $"Contratación no válida — Cliente #{idCliente}, Plan #{idPlan} — {ex.Clave}: {ex.Message}",
                        BE.Criticidad.Baja);
                }
                catch (Exception e) { System.Diagnostics.Trace.TraceError($"[BLL.Contratacion] Aviso: {e.Message}"); }
                throw;
            }
            var cliente = dalCliente.ObtenerPorId(idCliente);

            var contratacion = new BE.Contratacion
            {
                IdCliente  = idCliente,
                IdPlan     = idPlan,
                IdVendedor = BLLHelper.ResolverEmpleadoActivo(dalEmpleado),
                Modalidad  = modalidad,
                Estado     = BE.EstadoContratacion.PendientePago,
                FechaAlta  = DateTime.Now,
                // Precio pactado en la «Orden de cobro»: Caja cobra este aunque el plan cambie.
                PrecioMensual = plan.Precio
            };
            int idNuevo = dalContratacion.Alta(contratacion);

            bitacora.Registrar(modulo,
                $"Nueva contratación #{idNuevo} — Cliente: {cliente?.NombreCompleto} — Plan: {plan.Nombre} — Modalidad: {modalidad}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Contratación #{idNuevo} pendiente de pago — {cliente?.NombreCompleto} — Plan {plan.Nombre}",
                idCliente: idCliente);
            return idNuevo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Carril Caja
        // ══════════════════════════════════════════════════════════════════════

        // "Consultar cola": contrataciones pendientes de pago, de la más antigua a la más nueva.
        public List<BE.Contratacion> ObtenerPendientesDePago()
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);
            return dalContratacion.ObtenerPendientesDePago();
        }

        // Cantidad de contrataciones en la cola de Caja (panel de alertas, común a todos los roles).
        public int ContarPendientesDePago() => dalContratacion.ObtenerPendientesDePago().Count;

        // Contrataciones ya resueltas (Pagadas o Canceladas), para volver a imprimir sus constancias.
        public List<BE.Contratacion> ObtenerResueltas()
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);
            return dalContratacion.ObtenerResueltas();
        }

        public BE.Contratacion ObtenerPorId(int idContratacion) => dalContratacion.ObtenerPorId(idContratacion);

        // Medios que se ofrecen en Caja: los activos (un medio histórico solo aparece en cobros viejos).
        public List<BE.MedioPago> ObtenerMediosPago() => dalContratacion.ObtenerMediosPago().Where(m => m.Activo).ToList();

        // "Ofrecer planes de cuotas": con tarjeta de crédito, los planes activos que no superan los
        // meses que cubre la modalidad (Mensual: 1; Trimestral: hasta 3; Anual: hasta 12).
        public List<BE.PlanCuotas> ObtenerPlanesCuotas(BE.Builders.ModalidadCobro modalidad)
            => Politicas.PoliticaCuotas.Disponibles(dalContratacion.ObtenerPlanesCuotas(), modalidad);

        public List<BE.IntentoPago> ObtenerIntentos(int idContratacion)
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);
            return dalContratacion.ObtenerIntentos(idContratacion);
        }

        public BE.DesistimientoContratacion ObtenerDesistimiento(int idDesistimiento) => dalContratacion.ObtenerDesistimiento(idDesistimiento);

        // "Calcular importe" («Liquidación»): precio mensual × meses de la modalidad menos UN solo
        // descuento (la promoción vigente del plan o el crédito por referidos, el mayor). Si se indica
        // el medio de pago y es tarjeta de crédito, "Calcular recargo y valor de cuota" con el plan de
        // cuotas elegido («Detalle de financiación»).
        public BE.LiquidacionContratacion CalcularImporte(BE.Contratacion contratacion, int? idMedioPago = null, int? idPlanCuotas = null)
        {
            var plan = dalPlan.ObtenerPorId(contratacion.IdPlan);
            var cliente = dalCliente.ObtenerPorId(contratacion.IdCliente);
            var r = ResolverDescuento(contratacion, plan, cliente, ObtenerPromocionesVigentes());
            var liq = new BE.LiquidacionContratacion
            {
                Bruto = r.Bruto, Descuento = r.Descuento, CreditoCambioPlan = r.CreditoCambioPlan,
                NombrePromocion = r.Promocion?.Nombre, UsaCreditoReferido = r.UsaCreditoReferido
            };
            SumarCargos(liq, CargosPendientes(contratacion.IdCliente));
            if (idMedioPago.HasValue)
            {
                var medio = ValidarMedioPago(idMedioPago, requerido: true);
                liq.AplicarFinanciacion(Politicas.PoliticaCuotas.Financiar(liq.Total, ResolverCuotas(medio, idPlanCuotas, contratacion.Modalidad)));
            }
            return liq;
        }

        // Importe que el Vendedor informa al presentar el plan elegido (antes de registrar la
        // contratación): misma regla que el cobro, para que el cliente sepa cuánto va a abonar.
        public BE.LiquidacionContratacion EstimarImporte(int idCliente, int idPlan, BE.Builders.ModalidadCobro modalidad)
        {
            var plan = dalPlan.ObtenerPorId(idPlan);
            if (plan == null) return null;
            return CalcularImporte(new BE.Contratacion
            {
                IdCliente = idCliente, IdPlan = idPlan, Modalidad = modalidad, MontoPlan = plan.Precio
            });
        }

        // Igual que CalcularImporte pero para toda la cola de Caja: las promociones se leen UNA vez.
        public Dictionary<int, BE.LiquidacionContratacion> CalcularImportes(List<BE.Contratacion> contrataciones)
        {
            var resultado = new Dictionary<int, BE.LiquidacionContratacion>();
            if (contrataciones == null || contrataciones.Count == 0) return resultado;

            var promos = ObtenerPromocionesVigentes();
            foreach (var c in contrataciones)
            {
                var r = ResolverDescuento(c, dalPlan.ObtenerPorId(c.IdPlan), dalCliente.ObtenerPorId(c.IdCliente), promos);
                var liq = new BE.LiquidacionContratacion
                {
                    Bruto = r.Bruto, Descuento = r.Descuento, CreditoCambioPlan = r.CreditoCambioPlan,
                    NombrePromocion = r.Promocion?.Nombre, UsaCreditoReferido = r.UsaCreditoReferido
                };
                SumarCargos(liq, CargosPendientes(c.IdCliente));
                resultado[c.IdContratacion] = liq;
            }
            return resultado;
        }

        // "¿Se concreta el pago? Sí" → "Confirmar cobro" → "Emitir comprobante" («Comprobante») →
        // "Activar suscripción" («Constancia de suscripción») → "¿Referido? Sí → Acreditar crédito".
        public BE.LiquidacionContratacion ConfirmarCobro(string modulo, BE.Contratacion contratacion, int idMedioPago,
                                                         decimal? importeConfirmado = null, int? idPlanCuotas = null)
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);

            // Revalida contra el estado fresco de la BD (doble clic / dos sesiones de Caja).
            var actual = dalContratacion.ObtenerPorId(contratacion.IdContratacion);
            if (actual == null)
                throw new BE.AppException("err.bll.contratacion.inexistente", "La contratación ya no existe.");
            if (!actual.PuedeCobrarse() || !actual.TransicionValida(BE.EstadoContratacion.Pagada))
                throw new BE.AppException("err.bll.contratacion.cobrar_estado",
                    "Solo se pueden cobrar contrataciones Pendientes de pago. Esta contratación está '{0}'.",
                    actual.Estado);

            var medio = ValidarMedioPago(idMedioPago, requerido: true);
            // ¿Paga con tarjeta de crédito en cuotas? El plan de cuotas se valida ANTES de tocar nada.
            var planCuotas = ResolverCuotas(medio, idPlanCuotas, actual.Modalidad);

            // El plan fue dado de baja entre que el Vendedor registró la contratación y que Caja la
            // cobra: se rechaza ANTES de tocar nada y la contratación sigue Pendiente de pago.
            var plan = dalPlan.ObtenerPorId(actual.IdPlan);
            if (plan == null || !plan.Estado)
                throw new BE.AppException("err.bll.contratacion.plan_baja",
                    "El plan '{0}' fue dado de baja: no se puede formalizar la suscripción. " +
                    "La contratación queda pendiente de pago.",
                    actual.NombrePlan ?? plan?.Nombre ?? actual.IdPlan.ToString());

            var cliente = dalCliente.ObtenerPorId(actual.IdCliente)
                ?? throw new BE.AppException("err.bll.contratacion.cliente_inexistente", "El cliente seleccionado no existe.");
            ValidarCupo(cliente, plan);

            int idCaja = BLLHelper.ResolverEmpleadoActivo(dalEmpleado);
            // Separación de funciones POR PERSONA: quien vendió la contratación no puede cobrarla.
            // Excepción: el Administrador puede hacer las dos cosas (decisión de la alumna).
            if (idCaja == actual.IdVendedor && !(Sesion.Usuario?.EsAdministrador ?? false))
                throw new BE.AppException("err.bll.contratacion.cobra_el_vendedor",
                    "Quien registró la contratación no puede cobrarla: el cobro lo confirma otra persona de Caja.");
            var descuento = ResolverDescuento(actual, plan, cliente, ObtenerPromocionesVigentes());
            // PN04: los cargos por daño o pérdida pendientes se cobran junto con el período (igual que
            // el cobro de N01); se liquidan en la misma transacción del cobro.
            var cargos = CargosPendientes(cliente.IdCliente);
            decimal totalCargos = cargos.Sum(x => x.Monto);
            decimal totalACobrar = descuento.Total + totalCargos;
            // Caja confirmó el importe de la «Liquidación» que vio: si cambió (venció una promoción,
            // se consumió el crédito, apareció un cargo), no se cobra un monto distinto del confirmado.
            if (importeConfirmado.HasValue && totalACobrar != importeConfirmado.Value)
                throw new BE.AppException("err.bll.contratacion.importe_cambiado",
                    "El importe a cobrar cambió desde la liquidación ({0:C2} → {1:C2}). Volvé a calcular el importe y confirmá de nuevo.",
                    importeConfirmado.Value, totalACobrar);

            // "Calcular recargo y valor de cuota" («Detalle de financiación»): el recargo se suma al total
            // confirmado; con un medio que no financia, financiacion queda en 1 pago sin recargo.
            var financiacion = Politicas.PoliticaCuotas.Financiar(totalACobrar, planCuotas);
            var idsCargo = cargos.Select(x => x.IdCargo).ToList();

            // "Emitir comprobante".
            string numeroComprobante = EmitirComprobante(actual.IdContratacion);

            // "Confirmar cobro": claim atómico (el UPDATE exige que siga PendientePago).
            if (!dalContratacion.ConfirmarCobro(actual.IdContratacion, idCaja, medio.IdMedioPago, numeroComprobante,
                    totalACobrar, descuento.Descuento, descuento.Promocion?.IdPromocion,
                    planCuotas?.IdPlanCuotas, planCuotas != null ? financiacion.Recargo : (decimal?)null,
                    descuento.CreditoCambioPlan > 0 ? descuento.CreditoCambioPlan : (decimal?)null, idsCargo))
                throw new BE.AppException("err.bll.contratacion.cobrar_concurrente",
                    "Otra sesión de Caja ya resolvió esta contratación. Actualizá la cola de Caja.");

            // "¿Referido?": el beneficio se acredita una sola vez, al activar la suscripción del referido.
            BE.Cliente referente = null;
            if (cliente.IdClienteReferente.HasValue && !cliente.BeneficioReferidoOtorgado)
                referente = dalCliente.ObtenerPorId(cliente.IdClienteReferente.Value);

            // "Activar suscripción" (+ "Acreditar crédito" si corresponde). Si falla, se compensa
            // devolviendo la contratación a Pendiente de pago (nunca queda Pagada sin suscripción).
            // Riesgo residual aceptado: el claim y la activación no comparten una transacción (son
            // tablas de dos DAL distintos); si el proceso cae justo entre ambos pasos, se corrige con
            // el alta administrativa del plan.
            BE.Builders.Suscripcion suscripcion;
            try
            {
                // El crédito consumido se descuenta en la BD (ConsumirCreditoEnTx); acá se refleja en el objeto.
                if (descuento.UsaCreditoReferido)
                    cliente.DescuentoProximoCobro = Math.Max(0, cliente.DescuentoProximoCobro - descuento.Descuento);
                // Upgrade: el período del plan nuevo arranca hoy (aunque el crédito sea 0 porque el total ya era 0).
                suscripcion = clienteBLL.ActivarSuscripcionDesdeContratacion(modulo, cliente, actual.IdPlan, actual.Modalidad,
                    descuento.UsaCreditoReferido ? descuento.Descuento : 0m, iniciarHoy: descuento.EsUpgrade);
            }
            catch
            {
                Compensar(modulo, actual, idsCargo);
                throw;
            }

            DateTime? hasta = suscripcion?.FechaVencimiento;
            DateTime? desde = suscripcion?.InicioPeriodo;
            if (hasta.HasValue)
            {
                try { dalContratacion.RegistrarVigencia(actual.IdContratacion, desde.Value, hasta.Value, referente?.IdCliente); }
                catch (Exception ex)
                {
                    // La suscripción ya quedó activa: no se revierte el cobro, pero queda constancia para
                    // corregir la Constancia de suscripción (que saldría sin período).
                    try
                    {
                        bitacora.Registrar(modulo,
                            $"Contratación #{actual.IdContratacion} cobrada y activada, pero no se pudo guardar la vigencia " +
                            $"({desde:d} - {hasta:d}): {ex.Message}", BE.Criticidad.Alta);
                    }
                    catch { }
                }
            }

            bitacora.Registrar(modulo,
                $"Cobro Contratación #{actual.IdContratacion} — Cliente: {actual.NombreCliente} — " +
                $"Medio: {medio.Nombre}" +
                (planCuotas != null ? $" en {financiacion.CantidadCuotas} cuota(s) de ${financiacion.ValorCuota} (recargo ${financiacion.Recargo})" : "") +
                $" — Comprobante: {numeroComprobante}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.CobroSuscripcion,
                $"Contratación #{actual.IdContratacion} cobrada y suscripción activada — " +
                $"{actual.NombreCliente} — Plan {actual.NombrePlan} — Comprobante {numeroComprobante} — " +
                $"Importe ${totalACobrar}" +
                (cargos.Count > 0 ? $" (incluye {cargos.Count} cargo(s) por daño o pérdida: ${totalCargos})" : "") +
                (planCuotas != null && financiacion.CantidadCuotas > 1
                    ? $" en {financiacion.CantidadCuotas} cuotas con tarjeta de crédito (recargo ${financiacion.Recargo})" : "") +
                (descuento.Descuento > 0
                    ? $" (descuento ${descuento.Descuento}: {(descuento.Promocion != null ? $"promoción '{descuento.Promocion.Nombre}'" : "crédito por referido")})"
                    : "") +
                (descuento.CreditoCambioPlan > 0
                    ? $" (cambio a un plan superior: crédito ${descuento.CreditoCambioPlan} por los días no usados del plan anterior)"
                    : ""),
                idCliente: actual.IdCliente);

            var resultado = new BE.LiquidacionContratacion
            {
                Bruto = descuento.Bruto,
                Descuento = descuento.Descuento,
                CreditoCambioPlan = descuento.CreditoCambioPlan,
                Cargos = totalCargos,
                CantidadCargos = cargos.Count,
                NombrePromocion = descuento.Promocion?.Nombre,
                UsaCreditoReferido = descuento.UsaCreditoReferido,
                NumeroComprobante = numeroComprobante,
                VigenciaDesde = desde,
                VigenciaHasta = hasta,
                ReferenteAcreditado = referente?.NombreCompleto
            };
            resultado.AplicarFinanciacion(financiacion);
            return resultado;
        }

        // "¿Se concreta el pago? No" → "Registrar intento" («Intento») → "¿Alcanzó el máximo de 3
        // intentos?" No: vuelve a Calcular importe (sigue en la cola); Sí: "Cancelar contratación"
        // («Constancia de cancelación»). Todo en una transacción del DAL.
        public BE.ResultadoIntentoPago RegistrarIntentoFallido(string modulo, BE.Contratacion contratacion,
                                                               int? idMedioPago, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);

            var actual = dalContratacion.ObtenerPorId(contratacion.IdContratacion);
            if (actual == null)
                throw new BE.AppException("err.bll.contratacion.inexistente", "La contratación ya no existe.");
            if (!actual.PuedeCobrarse() || !actual.TransicionValida(BE.EstadoContratacion.Cancelada))
                throw new BE.AppException("err.bll.contratacion.cobrar_estado",
                    "Solo se pueden registrar intentos sobre contrataciones Pendientes de pago. Esta contratación está '{0}'.",
                    actual.Estado);
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.contratacion.intento_sin_motivo",
                    "Indicá el motivo por el que no se concretó el pago.");
            ValidarLargoMotivo(motivo);
            ValidarMedioPago(idMedioPago, requerido: false);

            var resultado = dalContratacion.RegistrarIntentoFallido(actual.IdContratacion, idMedioPago, motivo.Trim(),
                BLLHelper.ResolverEmpleadoActivo(dalEmpleado), BE.Contratacion.MaxIntentosPago);
            if (resultado == null)
                throw new BE.AppException("err.bll.contratacion.cobrar_concurrente",
                    "Otra sesión de Caja ya resolvió esta contratación. Actualizá la cola de Caja.");

            bitacora.Registrar(modulo,
                $"Intento de pago fallido en Contratación #{actual.IdContratacion} — Cliente: {actual.NombreCliente} — " +
                $"Intento {resultado.NroIntento} de {resultado.Maximo} — Motivo: {motivo.Trim()}",
                BE.Criticidad.Baja);

            if (resultado.Cancelada)
            {
                bitacora.Registrar(modulo,
                    $"Cancelar Contratación #{actual.IdContratacion} — Cliente: {actual.NombreCliente} — " +
                    "Máximo de intentos de pago agotado",
                    BE.Criticidad.Media);
                bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                    $"Contratación #{actual.IdContratacion} cancelada — {actual.NombreCliente} — " +
                    $"máximo de {resultado.Maximo} intentos de pago agotado sin concretarse",
                    idCliente: actual.IdCliente);
            }
            return resultado;
        }

        // "Anular contratación": el cliente se arrepintió antes de pagar o hubo un error de carga. Caja
        // la cancela con motivo, sin registrar intentos de pago que no existieron. Queda Cancelada
        // («Constancia de cancelación» con el motivo) y el cliente puede volver a contratar.
        public void Anular(string modulo, BE.Contratacion contratacion, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);

            var actual = dalContratacion.ObtenerPorId(contratacion.IdContratacion);
            if (actual == null)
                throw new BE.AppException("err.bll.contratacion.inexistente", "La contratación ya no existe.");
            if (!actual.PuedeCobrarse() || !actual.TransicionValida(BE.EstadoContratacion.Cancelada))
                throw new BE.AppException("err.bll.contratacion.anular_estado",
                    "Solo se pueden anular contrataciones Pendientes de pago. Esta contratación está '{0}'.",
                    actual.Estado);
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.contratacion.anular_sin_motivo",
                    "Indicá el motivo por el que se anula la contratación.");
            ValidarLargoMotivo(motivo);

            if (!dalContratacion.Anular(actual.IdContratacion, motivo.Trim(), BLLHelper.ResolverEmpleadoActivo(dalEmpleado)))
                throw new BE.AppException("err.bll.contratacion.cobrar_concurrente",
                    "Otra sesión de Caja ya resolvió esta contratación. Actualizá la cola de Caja.");

            bitacora.Registrar(modulo,
                $"Anular Contratación #{actual.IdContratacion} — Cliente: {actual.NombreCliente} — Motivo: {motivo.Trim()}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                $"Contratación #{actual.IdContratacion} anulada por Caja — {actual.NombreCliente} — {motivo.Trim()}",
                idCliente: actual.IdCliente);
        }

        // ── Auxiliares ─────────────────────────────────────────────────────────

        // "Emitir comprobante": número simple y legible (prefijo + ID de contratación + fecha).
        private static string EmitirComprobante(int idContratacion)
            => $"CMP-{idContratacion:D6}-{DateTime.Now:yyyyMMdd}";

        private BE.MedioPago ValidarMedioPago(int? idMedioPago, bool requerido)
        {
            if (!idMedioPago.HasValue)
            {
                if (requerido)
                    throw new BE.AppException("err.bll.contratacion.medio_pago_requerido",
                        "Debe indicar el medio de pago (efectivo, tarjeta de débito, tarjeta de crédito o transferencia).");
                return null;
            }
            return ObtenerMediosPago().FirstOrDefault(m => m.IdMedioPago == idMedioPago.Value)
                ?? throw new BE.AppException("err.bll.contratacion.medio_invalido",
                    "El medio de pago indicado no existe.");
        }

        // "¿Paga con tarjeta de crédito en cuotas?": devuelve el plan de cuotas a aplicar, o null si el
        // medio no financia (se cobra en un solo pago). Con tarjeta de crédito y sin plan elegido, un
        // solo pago (el plan de 1 cuota). Reglas: solo los medios que permiten cuotas; el plan tiene
        // que existir y estar activo; no más cuotas que los meses que cubre la modalidad.
        private BE.PlanCuotas ResolverCuotas(BE.MedioPago medio, int? idPlanCuotas, BE.Builders.ModalidadCobro modalidad)
        {
            var planes = dalContratacion.ObtenerPlanesCuotas() ?? new List<BE.PlanCuotas>();
            var plan = idPlanCuotas.HasValue
                ? planes.FirstOrDefault(p => p.IdPlanCuotas == idPlanCuotas.Value)
                : null;

            if (!medio.PermiteCuotas)
            {
                if (idPlanCuotas.HasValue && (plan == null || plan.CantidadCuotas > 1))
                    throw new BE.AppException("err.bll.contratacion.cuotas_medio",
                        "El medio de pago '{0}' no permite pagar en cuotas: las cuotas son solo con tarjeta de crédito.",
                        medio.Nombre);
                return null;
            }

            if (!idPlanCuotas.HasValue)
                plan = planes.Where(p => p.Activo && p.CantidadCuotas == 1).FirstOrDefault();
            if (plan == null || !plan.Activo)
                throw new BE.AppException("err.bll.contratacion.cuotas_invalidas",
                    "El plan de cuotas elegido no existe o no está disponible.");
            if (!Politicas.PoliticaCuotas.PermiteModalidad(plan, modalidad))
                throw new BE.AppException("err.bll.contratacion.cuotas_modalidad",
                    "Con la modalidad {0} se puede pagar en hasta {1} cuota(s); se eligieron {2}.",
                    modalidad, BE.Builders.ModalidadCobroExtensiones.Meses(modalidad), plan.CantidadCuotas);
            return plan;
        }

        private void Compensar(string modulo, BE.Contratacion actual, IList<int> idsCargo = null)
        {
            try { dalContratacion.ReabrirPago(actual.IdContratacion, idsCargo); }
            catch (Exception ex)
            {
                // Peor caso: el cobro quedó registrado y no se pudo reabrir. Se deja constancia de
                // alta criticidad y se avisa a Caja con un error propio.
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
        }

        // El plan elegido debe tener capacidad para las prendas que el cliente ya tiene en uso.
        private static void ValidarCupo(BE.Cliente cliente, BE.PlanSuscripcion plan)
        {
            if (cliente != null && plan != null && cliente.StockUtilizado > plan.LimitePrendas)
                throw new BE.AppException("err.bll.cliente.plan_insuficiente",
                    "No se puede asignar el plan '{0}': el cliente tiene {1} prenda(s) en uso y ese plan solo permite {2}. Registrá las devoluciones primero.",
                    plan.Nombre, cliente.StockUtilizado, plan.LimitePrendas);
        }

        // Precio mensual del plan × meses de la modalidad (NUULY cobra por mes) menos un único
        // descuento: promoción vigente del plan o crédito por referido.
        private BE.ResultadoDescuento ResolverDescuento(BE.Contratacion c, BE.PlanSuscripcion plan,
                                                        BE.Cliente cliente, List<BE.Promocion> promos)
        {
            int meses = BE.Builders.ModalidadCobroExtensiones.Meses(c.Modalidad);
            // Precio pactado al registrar la contratación; si no lo tiene (datos previos), el del plan.
            decimal precio = c.PrecioMensual ?? (plan != null ? plan.Precio : c.MontoPlan);
            decimal bruto = precio * meses;
            var r = Politicas.PoliticaDescuento.Resolver(bruto, c.IdPlan, promos, cliente?.DescuentoProximoCobro ?? 0m, meses);

            // Upgrade: pasa a un plan más caro con el período vigente → el plan nuevo rige desde hoy y
            // los días no usados del plan actual se descuentan del cobro (BLL.Politicas.PoliticaCambioPlan).
            var planActual = cliente?.IdPlan != null && cliente.IdPlan.Value != c.IdPlan
                ? dalPlan.ObtenerPorId(cliente.IdPlan.Value) : null;
            r.EsUpgrade         = Politicas.PoliticaCambioPlan.EsUpgrade(cliente, planActual, c.IdPlan, precio, DateTime.Today);
            r.CreditoCambioPlan = Politicas.PoliticaCambioPlan.Credito(cliente, planActual, c.IdPlan, precio, DateTime.Today, r.Total);
            return r;
        }

        // PN04: cargos por daño o pérdida pendientes del cliente (vacío si no hay DAL de cargos).
        private List<BE.CargoPrenda> CargosPendientes(int idCliente) =>
            DalCargos?.ObtenerPendientesPorCliente(idCliente) ?? new List<BE.CargoPrenda>();

        private static void SumarCargos(BE.LiquidacionContratacion liq, List<BE.CargoPrenda> cargos)
        {
            liq.Cargos         = cargos.Sum(x => x.Monto);
            liq.CantidadCargos = cargos.Count;
        }

        // Best-effort: sin DAL de promociones, o si la tabla aún no existe, se cobra sin promociones.
        private List<BE.Promocion> ObtenerPromocionesVigentes()
        {
            if (dalPromocion == null) return new List<BE.Promocion>();
            try { return dalPromocion.ObtenerVigentes(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[BLL.Contratacion] No se pudieron leer las promociones: {ex.Message}");
                try { bitacora.Registrar(ModuloContratacion, $"No se pudieron leer las promociones vigentes; se cobró sin descuento: {ex.Message}", BE.Criticidad.Media); }
                catch { }
                return new List<BE.Promocion>();
            }
        }
    }
}
