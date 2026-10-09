using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Cobro de suscripción (PdN6). Arma y expone la cadena de
    /// manejadores (Chain of Responsibility) que resuelve si un cobro se confirma
    /// (y renueva la vigencia), entra en período de gracia, o suspende nuevos pedidos.
    /// </summary>
    public class Cobro : Interfaces.ICobroService
    {
        private readonly DAL.Interfaces.ICobroDAL dalCobro;
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.ICargoPrendaDAL dalCargoPrenda;
        private readonly DAL.Interfaces.IPromocionDAL dalPromocion;   // null: cobro sin promociones
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();
        private readonly Servicios.IRegistroBitacoraNegocio bitacoraNeg = Servicios.FabricaBitacora.CrearNegocio();
        private readonly Manejadores.ManejadorCobro cadena;

        public Cobro() : this(new DAL.Cliente(), new DAL.Cobro(), new DAL.CargoPrenda(), new DAL.Promocion()) { }

        public Cobro(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.ICobroDAL dalCobro,
                      DAL.Interfaces.ICargoPrendaDAL dalCargoPrenda,
                      DAL.Interfaces.IPromocionDAL dalPromocion = null)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalCobro = dalCobro ?? throw new ArgumentNullException(nameof(dalCobro));
            this.dalCargoPrenda = dalCargoPrenda ?? throw new ArgumentNullException(nameof(dalCargoPrenda));
            this.dalPromocion = dalPromocion;

            // Arma la cadena de cola a cabeza, con sentencias sueltas — igual que el
            // Program.cs del ejemplo de cátedra (director.AgregarSiguiente(directorGeneral);
            // gerente.AgregarSiguiente(director); comprador.AgregarSiguiente(gerente);).
            var detectar  = new Manejadores.DetectarCobroHandler();
            var procesar  = new Manejadores.ProcesarPagoHandler(dalCliente, dalCobro, dalCargoPrenda, dalPromocion);
            var gracia    = new Manejadores.AplicarGraciaHandler(dalCliente, dalCobro);
            var suspender = new Manejadores.SuspenderHandler(dalCobro);

            gracia.AgregarSiguiente(suspender);
            procesar.AgregarSiguiente(gracia);
            detectar.AgregarSiguiente(procesar);
            cadena = detectar;
        }

        public Manejadores.ResultadoCobro Procesar(
            string modulo, BE.Cliente cliente, Manejadores.DecisionCobro decision,
            BE.Builders.ModalidadCobro modalidad, string actor, int? idMedioPago = null)
        {
            // N01 — El cobro recurrente de la suscripción lo registra CAJA (decisión del proceso:
            // quien vende no cobra). Se gobierna por la patente de edición de Caja; la de
            // mnuCobroSuscripcion solo controla que el ítem de menú/pantalla sea visible.
            PermisosAccion.Exigir(BE.Patentes.CajaEditar, BE.Patentes.Caja);
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));

            // Guarda de entrada única para toda la cadena: sin plan asignado no hay
            // suscripción que cobrar (mismo criterio que BLL.Renovacion.Procesar).
            if (!cliente.TienePlan())
                throw new BE.AppException("err.bll.cobro.sin_plan",
                    "{0} no tiene un plan de suscripción asignado. No corresponde procesar un cobro.",
                    cliente.NombreCompleto);

            // PN02: con una contratación pendiente de pago, el plan y el vencimiento los define el
            // cobro de Caja. Procesar un cobro recurrente a la vez extendería o cambiaría la suscripción dos veces.
            if (dalCliente.TieneContratacionPendiente(cliente.IdCliente))
                throw new BE.AppException("err.bll.cobro.contratacion_pendiente",
                    "{0} tiene una contratación pendiente de pago: la suscripción se define cuando Caja la cobre o la cancele.",
                    cliente.NombreCompleto);

            // N01 — como en PN02, un cobro exitoso registra con qué medio pagó el cliente.
            if (decision == Manejadores.DecisionCobro.Cobrado)
            {
                if (!idMedioPago.HasValue)
                    throw new BE.AppException("err.bll.cobro.medio_requerido",
                        "Indicá el medio de pago con el que abonó el cliente.");
                if (!ObtenerMediosPago().Exists(m => m.IdMedioPago == idMedioPago.Value))
                    throw new BE.AppException("err.bll.contratacion.medio_invalido",
                        "El medio de pago indicado no existe.");
            }

            var contexto = new Manejadores.ContextoCobro
            {
                Cliente = cliente,
                Decision = decision,
                Modalidad = modalidad,
                Actor = actor,
                Modulo = modulo,
                IdMedioPago = decision == Manejadores.DecisionCobro.Cobrado ? idMedioPago : null
            };

            var resultado = cadena.Procesar(contexto);

            bitacora.Registrar(modulo,
                $"Cobro Cliente ID {cliente.IdCliente} ({cliente.NombreCompleto}): {resultado.Estado} — {resultado.Mensaje}",
                resultado.Estado == BE.EstadoCobro.Pendiente ? BE.Criticidad.Baja : BE.Criticidad.Media);

            if (resultado.Estado != BE.EstadoCobro.Pendiente)
                bitacoraNeg.Registrar(BE.TipoEventoNegocio.CobroSuscripcion,
                    $"Cobro de suscripción: {cliente.NombreCompleto} — {resultado.Estado} — {resultado.Mensaje}",
                    idCliente: cliente.IdCliente);

            return resultado;
        }

        // Un cobro (para reimprimir su comprobante).
        public BE.Cobro ObtenerCobro(int idCobro) => dalCobro.ObtenerPorId(idCobro);

        // Medios de pago vigentes (el mismo catálogo que Caja usa en PN02).
        public List<BE.MedioPago> ObtenerMediosPago() =>
            (dalCobro.ObtenerMediosPago() ?? new List<BE.MedioPago>()).Where(m => m.Activo).ToList();

        // Clientes a los que hoy corresponde procesarles un cobro: con plan, con la suscripción
        // vencida o próxima a vencer (mismo criterio que DetectarCobroHandler) y sin una
        // contratación PN02 pendiente de pago (Procesar los rechaza). Antes este filtro lo
        // armaba CobroSuscripcionForm y no excluía las contrataciones pendientes.
        public List<BE.Cliente> ObtenerElegibles()
        {
            var pendientes = dalCliente.ObtenerIdsConContratacionPendiente();   // una sola consulta
            return dalCliente.ObtenerTodos()
                .Where(c => c.TienePlan() && c.RequiereGestionDeVencimiento() && !pendientes.Contains(c.IdCliente))
                .ToList();
        }

        // Anticipa el próximo cobro del cliente con la misma cuenta que ProcesarPagoHandler:
        // período (precio × meses) con el único descuento del ciclo + cargos por daño/pérdida
        // pendientes. Así el operador ve el total ANTES de procesar.
        public BE.PrevisualizacionCobro PrevisualizarCobro(int idCliente,
            BE.Builders.ModalidadCobro modalidad = BE.Builders.ModalidadCobro.Mensual)
        {
            var pendientes = dalCargoPrenda.ObtenerPendientesPorCliente(idCliente) ?? new List<BE.CargoPrenda>();
            var previa = new BE.PrevisualizacionCobro
            {
                CantidadCargosPendientes = pendientes.Count,
                TotalCargosPendientes    = pendientes.Sum(c => c.Monto)
            };

            var cliente = dalCliente.ObtenerPorId(idCliente);
            if (cliente != null && cliente.TienePlan())
            {
                var r = Manejadores.ProcesarPagoHandler.CalcularDescuento(cliente, modalidad, ObtenerPromocionesVigentes());
                previa.Bruto = r.Bruto;
                previa.Descuento = r.Descuento;
                previa.NombrePromocion = r.Promocion?.Nombre;
                previa.UsaCreditoReferido = r.UsaCreditoReferido;
            }
            return previa;
        }

        // Igual que ProcesarPagoHandler: sin DAL de promociones o si falla la consulta, sin promociones.
        private List<BE.Promocion> ObtenerPromocionesVigentes()
        {
            if (dalPromocion == null) return new List<BE.Promocion>();
            try { return dalPromocion.ObtenerVigentes(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[BLL.Cobro] Promociones no disponibles: " + ex.Message);
                return new List<BE.Promocion>();
            }
        }
    }
}
