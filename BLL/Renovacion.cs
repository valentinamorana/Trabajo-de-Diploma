using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Renovación de suscripción (PdN5). Arma y expone la cadena de
    /// manejadores (Chain of Responsibility) que resuelve si un cliente renueva, cambia
    /// de plan o se da de baja tras vencer o estar próximo a vencer.
    /// </summary>
    public class Renovacion : Interfaces.IRenovacionService
    {
        private readonly DAL.Interfaces.IRenovacionDAL dalRenovacion;
        private readonly Servicios.Bitacora bitacora = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();
        private readonly Manejadores.ManejadorRenovacion cadena;

        public Renovacion() : this(new DAL.Cliente(), new DAL.Renovacion(), new DAL.PlanSuscripcion(), new DAL.Prenda()) { }

        // dalPlan/dalPrenda tipados por interfaz (antes eran DAL.PlanSuscripcion/DAL.Prenda
        // concretos): con eso, ni esta fachada ni CambioPlanHandler/BajaSuscripcionHandler se
        // podían instanciar con un doble de prueba — la cadena real quedaba sin ningún test que
        // la ejercitara de punta a punta (los tests reconstruían su propio orden de cadena en vez
        // de usar el que arma este constructor).
        private readonly DAL.Interfaces.IClienteDAL dalCliente;

        public Renovacion(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.IRenovacionDAL dalRenovacion,
                           DAL.Interfaces.IPlanSuscripcionDAL dalPlan, DAL.Interfaces.IPrendaDAL dalPrenda)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalRenovacion = dalRenovacion ?? throw new ArgumentNullException(nameof(dalRenovacion));

            // Arma la cadena de cola a cabeza, con sentencias sueltas — igual que el
            // Program.cs del ejemplo de cátedra (director.AgregarSiguiente(directorGeneral);
            // gerente.AgregarSiguiente(director); comprador.AgregarSiguiente(gerente);).
            var verificar = new Manejadores.VerificarVencimientoHandler();
            var renovar   = new Manejadores.IntentarRenovarHandler(dalCliente, dalRenovacion);
            var cambio    = new Manejadores.CambioPlanHandler(dalCliente, dalPlan, dalRenovacion);
            var pausar    = new Manejadores.PausarSuscripcionHandler(dalCliente, dalRenovacion, dalPrenda);
            var baja      = new Manejadores.BajaSuscripcionHandler(dalCliente, dalRenovacion, dalPrenda);

            pausar.AgregarSiguiente(baja);
            cambio.AgregarSiguiente(pausar);
            renovar.AgregarSiguiente(cambio);
            verificar.AgregarSiguiente(renovar);
            cadena = verificar;
        }

        public Manejadores.ResultadoRenovacion Procesar(
            string modulo, BE.Cliente cliente, Manejadores.DecisionRenovacion decision,
            int? idPlanNuevo, BE.Builders.ModalidadCobro modalidad, string actor,
            DateTime? fechaPausaHasta = null)
        {
            PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));

            // Guarda de entrada única para toda la cadena: sin plan asignado no hay
            // suscripción que renovar, cambiar o dar de baja. Sin este chequeo, un cliente
            // con IdPlan=null pero FechaVencimiento vencida (estado inconsistente, pero no
            // imposible: datos legacy, edición directa en BD, etc.) haría que
            // IntentarRenovarHandler arme un plan sintético con IdPlan=0 y lo "renueve".
            if (!cliente.TienePlan())
                throw new BE.AppException("err.bll.renovacion.sin_plan",
                    "{0} no tiene un plan de suscripción asignado. No corresponde procesar una renovación.",
                    cliente.NombreCompleto);

            // PN02: con una contratación pendiente de pago, el plan y el vencimiento los define el
            // cobro de Caja. Procesar una renovación a la vez extendería o cambiaría la suscripción dos veces.
            if (dalCliente.TieneContratacionPendiente(cliente.IdCliente))
                throw new BE.AppException("err.bll.renovacion.contratacion_pendiente",
                    "{0} tiene una contratación pendiente de pago: la suscripción se define cuando Caja la cobre o la cancele.",
                    cliente.NombreCompleto);

            var contexto = new Manejadores.ContextoRenovacion
            {
                Cliente = cliente,
                Decision = decision,
                IdPlanNuevo = idPlanNuevo,
                FechaPausaHasta = fechaPausaHasta,
                Modalidad = modalidad,
                Actor = actor,
                Modulo = modulo
            };

            var resultado = cadena.Procesar(contexto);

            bitacora.Registrar(modulo,
                $"Renovación Cliente ID {cliente.IdCliente} ({cliente.NombreCompleto}): {resultado.Estado} — {resultado.Mensaje}",
                resultado.Estado == BE.EstadoRenovacion.Pendiente ? BE.Criticidad.Baja : BE.Criticidad.Media);

            if (resultado.Estado != BE.EstadoRenovacion.Pendiente)
                bitacoraNeg.Registrar(BE.TipoEventoNegocio.ModificacionCliente,
                    $"Renovación de suscripción: {cliente.NombreCompleto} — {resultado.Estado} — {resultado.Mensaje}",
                    idCliente: cliente.IdCliente);

            return resultado;
        }

        public List<BE.Renovacion> ObtenerHistorial(int idCliente) => dalRenovacion.ObtenerPorCliente(idCliente);

        // Clientes a los que se les puede procesar la decisión indicada, con el mismo criterio
        // que la cadena de manejadores (para no ofrecer una decisión que el sistema va a rechazar):
        //   - solo clientes con plan y sin contratación PN02 pendiente de pago (Procesar los rechaza;
        //     si el cliente estaba pausado, el cobro de Caja levanta la pausa al activar);
        //   - Renovar / Cambiar plan / Baja: suscripción vencida o próxima a vencer
        //     (VerificarVencimientoHandler);
        //   - Pausar: cualquiera que no esté ya pausado (PausarSuscripcionHandler no re-pausa);
        //   - SIEMPRE se suman los ya pausados: "Reanudar ahora" no depende de la decisión, y un
        //     cliente pausado suele tener el vencimiento corrido hacia adelante.
        // Antes este filtro lo armaba RenovacionSuscripcionForm.
        public List<BE.Cliente> ObtenerElegibles(Manejadores.DecisionRenovacion decision)
        {
            var conPlan = dalCliente.ObtenerTodos()
                .Where(c => c.TienePlan())
                .Where(c => !dalCliente.TieneContratacionPendiente(c.IdCliente))
                .ToList();

            var porDecision = decision == Manejadores.DecisionRenovacion.Pausar
                ? conPlan.Where(c => !c.EstaPausada)
                : conPlan.Where(c => c.RequiereGestionDeVencimiento());

            return porDecision.Union(conPlan.Where(c => c.EstaPausada)).ToList();
        }
    }
}
