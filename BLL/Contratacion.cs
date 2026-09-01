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
        private readonly Servicios.Bitacora        bitacora    = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();

        // BLL.Cliente es quien formaliza la suscripción (ActivarSuscripcion, PdN1) — composición
        // lazy, mismo criterio que BLL.Pedido.prendaBLL / BLL.Prenda.listaEsperaBLL.
        private Interfaces.IClienteService _clienteBLLLazy;
        private Interfaces.IClienteService clienteBLL => _clienteBLLLazy ?? (_clienteBLLLazy = new Cliente());

        // DI: el constructor por defecto usa los DAL reales; el otro permite inyectar dobles
        // de prueba (mismo criterio que BLL.Pedido/BLL.Cliente).
        public Contratacion() : this(new DAL.Contratacion(), new DAL.Cliente(), new DAL.Empleado(), new DAL.PlanSuscripcion()) { }

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
        public void ConfirmarPago(string modulo, BE.Contratacion contratacion, string medioPago)
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

            int idCaja = BLLHelper.ResolverEmpleadoActivo(dalEmpleado);
            string numeroComprobante = GenerarNumeroComprobante(contratacion.IdContratacion);

            // Activar la suscripción PRIMERO: si esto falla (plan dado de baja entre la
            // contratación y el cobro, etc.) la Contratacion sigue PendientePago y se puede
            // reintentar. Si confirmáramos el pago antes y esto fallara después, quedaría
            // Pagada sin suscripción activada y sin forma de reintentar (PuedeCobrarse()
            // exige PendientePago).
            //
            // Riesgo residual aceptado: estos dos pasos no corren en una única transacción de
            // BD (Contratacion y Cliente/Suscripcion son tablas distintas). Si el proceso cae
            // justo entre ambas líneas, un reintento manual de Caja re-ejecuta la activación
            // (extiende la vigencia de nuevo) porque la Contratacion sigue viéndose
            // PendientePago. Es una ventana angosta (falla exactamente ahí) y de bajo impacto
            // (a lo sumo días de suscripción de más, nunca de menos ni cobro duplicado real);
            // una solución completa requeriría una transacción cruzando ambos DAL o una marca
            // de idempotencia dedicada, fuera de alcance de este TP.
            var cliente = dalCliente.ObtenerPorId(actual.IdCliente);
            clienteBLL.ActivarSuscripcionDesdeContratacion(modulo, cliente, actual.IdPlan, actual.Modalidad);

            dalContratacion.ConfirmarPago(actual.IdContratacion, idCaja, medioPago, numeroComprobante);

            bitacora.Registrar(modulo,
                $"Cobro Contratación #{contratacion.IdContratacion} — Cliente: {contratacion.NombreCliente} — " +
                $"Medio: {medioPago} — Comprobante: {numeroComprobante}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.CobroSuscripcion,
                $"Contratación #{contratacion.IdContratacion} cobrada y suscripción formalizada — " +
                $"{contratacion.NombreCliente} — Plan {contratacion.NombrePlan} — Comprobante {numeroComprobante}",
                idCliente: contratacion.IdCliente);
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
