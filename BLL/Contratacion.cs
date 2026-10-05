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
        private readonly Servicios.Bitacora        bitacora    = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();

        // BLL.Cliente es quien activa la suscripción (Builder) y acredita el referido — composición
        // lazy, mismo criterio que BLL.Pedido.prendaBLL / BLL.Prenda.listaEsperaBLL.
        private Interfaces.IClienteService _clienteBLLLazy;
        private Interfaces.IClienteService clienteBLL => _clienteBLLLazy ?? (_clienteBLLLazy = new Cliente());

        // DI: el constructor por defecto usa los DAL reales; los otros permiten inyectar dobles.
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
        public int AsentarDesistimiento(string modulo, int idCliente, int? idPlan,
                                        BE.Builders.ModalidadCobro? modalidad, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.contratacion.desistir_sin_motivo",
                    "Es obligatorio indicar el motivo del desistimiento que comunicó el cliente.");

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

        public List<BE.MedioPago> ObtenerMediosPago() => dalContratacion.ObtenerMediosPago();

        public List<BE.IntentoPago> ObtenerIntentos(int idContratacion)
        {
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);
            return dalContratacion.ObtenerIntentos(idContratacion);
        }

        public BE.DesistimientoContratacion ObtenerDesistimiento(int idDesistimiento) => dalContratacion.ObtenerDesistimiento(idDesistimiento);

        // "Calcular importe" («Liquidación»): precio mensual × meses de la modalidad menos UN solo
        // descuento (la promoción vigente del plan o el crédito por referidos, el mayor).
        public BE.LiquidacionContratacion CalcularImporte(BE.Contratacion contratacion)
        {
            var plan = dalPlan.ObtenerPorId(contratacion.IdPlan);
            var cliente = dalCliente.ObtenerPorId(contratacion.IdCliente);
            var r = ResolverDescuento(contratacion, plan, cliente, ObtenerPromocionesVigentes());
            return new BE.LiquidacionContratacion
            {
                Bruto = r.Bruto, Descuento = r.Descuento,
                NombrePromocion = r.Promocion?.Nombre, UsaCreditoReferido = r.UsaCreditoReferido
            };
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
                resultado[c.IdContratacion] = new BE.LiquidacionContratacion
                {
                    Bruto = r.Bruto, Descuento = r.Descuento,
                    NombrePromocion = r.Promocion?.Nombre, UsaCreditoReferido = r.UsaCreditoReferido
                };
            }
            return resultado;
        }

        // "¿Se concreta el pago? Sí" → "Confirmar cobro" → "Emitir comprobante" («Comprobante») →
        // "Activar suscripción" («Constancia de suscripción») → "¿Referido? Sí → Acreditar crédito".
        public BE.LiquidacionContratacion ConfirmarCobro(string modulo, BE.Contratacion contratacion, int idMedioPago,
                                                         decimal? importeConfirmado = null)
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
            var descuento = ResolverDescuento(actual, plan, cliente, ObtenerPromocionesVigentes());
            // Caja confirmó el importe de la «Liquidación» que vio: si cambió (venció una promoción,
            // se consumió el crédito), no se cobra un monto distinto del confirmado.
            if (importeConfirmado.HasValue && descuento.Total != importeConfirmado.Value)
                throw new BE.AppException("err.bll.contratacion.importe_cambiado",
                    "El importe a cobrar cambió desde la liquidación ({0:C2} → {1:C2}). Volvé a calcular el importe y confirmá de nuevo.",
                    importeConfirmado.Value, descuento.Total);

            // "Emitir comprobante".
            string numeroComprobante = EmitirComprobante(actual.IdContratacion);

            // "Confirmar cobro": claim atómico (el UPDATE exige que siga PendientePago).
            if (!dalContratacion.ConfirmarCobro(actual.IdContratacion, idCaja, medio.IdMedioPago, numeroComprobante,
                    descuento.Total, descuento.Descuento, descuento.Promocion?.IdPromocion))
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
                suscripcion = clienteBLL.ActivarSuscripcionDesdeContratacion(modulo, cliente, actual.IdPlan, actual.Modalidad,
                    descuento.UsaCreditoReferido ? descuento.Descuento : 0m);
            }
            catch
            {
                Compensar(modulo, actual);
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
                $"Medio: {medio.Nombre} — Comprobante: {numeroComprobante}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.CobroSuscripcion,
                $"Contratación #{actual.IdContratacion} cobrada y suscripción activada — " +
                $"{actual.NombreCliente} — Plan {actual.NombrePlan} — Comprobante {numeroComprobante} — " +
                $"Importe ${descuento.Total}" +
                (descuento.Descuento > 0
                    ? $" (descuento ${descuento.Descuento}: {(descuento.Promocion != null ? $"promoción '{descuento.Promocion.Nombre}'" : "crédito por referido")})"
                    : ""),
                idCliente: actual.IdCliente);

            return new BE.LiquidacionContratacion
            {
                Bruto = descuento.Bruto,
                Descuento = descuento.Descuento,
                NombrePromocion = descuento.Promocion?.Nombre,
                UsaCreditoReferido = descuento.UsaCreditoReferido,
                NumeroComprobante = numeroComprobante,
                VigenciaDesde = desde,
                VigenciaHasta = hasta,
                ReferenteAcreditado = referente?.NombreCompleto
            };
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
                        "Debe indicar el medio de pago (efectivo, tarjeta o transferencia).");
                return null;
            }
            return dalContratacion.ObtenerMediosPago().FirstOrDefault(m => m.IdMedioPago == idMedioPago.Value)
                ?? throw new BE.AppException("err.bll.contratacion.medio_invalido",
                    "El medio de pago indicado no existe.");
        }

        private void Compensar(string modulo, BE.Contratacion actual)
        {
            try { dalContratacion.ReabrirPago(actual.IdContratacion); }
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
        private static BE.ResultadoDescuento ResolverDescuento(BE.Contratacion c, BE.PlanSuscripcion plan,
                                                               BE.Cliente cliente, List<BE.Promocion> promos)
        {
            int meses = BE.Builders.ModalidadCobroExtensiones.Meses(c.Modalidad);
            // Precio pactado al registrar la contratación; si no lo tiene (datos previos), el del plan.
            decimal precio = c.PrecioMensual ?? (plan != null ? plan.Precio : c.MontoPlan);
            decimal bruto = precio * meses;
            return BE.PoliticaDescuento.Resolver(bruto, c.IdPlan, promos, cliente?.DescuentoProximoCobro ?? 0m, meses);
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
