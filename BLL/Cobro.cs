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
            BE.Builders.ModalidadCobro modalidad, string actor)
        {
            // El cobro modifica datos del cliente (vencimiento / gracia): se gobierna por
            // el mismo permiso de edición que Renovación (BLL.Renovacion.Procesar), no por
            // mnuCobroSuscripcion — esa patente solo controla si el ítem de menú/pantalla
            // es visible, igual que mnuRenovacionSuscripcion.
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
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

            var contexto = new Manejadores.ContextoCobro
            {
                Cliente = cliente,
                Decision = decision,
                Modalidad = modalidad,
                Actor = actor,
                Modulo = modulo
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

        public List<BE.Cobro> ObtenerHistorial(int idCliente) => dalCobro.ObtenerPorCliente(idCliente);

        // Clientes a los que hoy corresponde procesarles un cobro: con plan, con la suscripción
        // vencida o próxima a vencer (mismo criterio que DetectarCobroHandler) y sin una
        // contratación PN02 pendiente de pago (Procesar los rechaza). Antes este filtro lo
        // armaba CobroSuscripcionForm y no excluía las contrataciones pendientes.
        public List<BE.Cliente> ObtenerElegibles() =>
            dalCliente.ObtenerTodos()
                .Where(c => c.TienePlan() && c.RequiereGestionDeVencimiento())
                // Va al final para consultar la contratación pendiente solo de los candidatos.
                .Where(c => !dalCliente.TieneContratacionPendiente(c.IdCliente))
                .ToList();

        // Anticipa lo que el próximo cobro del cliente va a sumar por cargos de daño/pérdida
        // pendientes (los mismos que ProcesarPagoHandler suma al cobrar). Antes la suma la
        // hacía CobroSuscripcionForm.
        public BE.PrevisualizacionCobro PrevisualizarCobro(int idCliente)
        {
            var pendientes = dalCargoPrenda.ObtenerPendientesPorCliente(idCliente) ?? new List<BE.CargoPrenda>();
            return new BE.PrevisualizacionCobro
            {
                CantidadCargosPendientes = pendientes.Count,
                TotalCargosPendientes    = pendientes.Sum(c => c.Monto)
            };
        }
    }
}
