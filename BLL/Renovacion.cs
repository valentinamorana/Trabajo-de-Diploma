using System;
using System.Collections.Generic;

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

        public Renovacion(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.IRenovacionDAL dalRenovacion,
                           DAL.PlanSuscripcion dalPlan, DAL.Prenda dalPrenda)
        {
            this.dalRenovacion = dalRenovacion ?? throw new ArgumentNullException(nameof(dalRenovacion));

            // Arma la cadena de cola a cabeza, con sentencias sueltas — igual que el
            // Program.cs del ejemplo de cátedra (director.AgregarSiguiente(directorGeneral);
            // gerente.AgregarSiguiente(director); comprador.AgregarSiguiente(gerente);).
            var verificar = new Manejadores.VerificarVencimientoHandler();
            var renovar   = new Manejadores.IntentarRenovarHandler(dalCliente, dalRenovacion);
            var cambio    = new Manejadores.CambioPlanHandler(dalCliente, dalPlan, dalRenovacion);
            var baja      = new Manejadores.BajaSuscripcionHandler(dalCliente, dalRenovacion, dalPrenda);

            cambio.AgregarSiguiente(baja);
            renovar.AgregarSiguiente(cambio);
            verificar.AgregarSiguiente(renovar);
            cadena = verificar;
        }

        public Manejadores.ResultadoRenovacion Procesar(
            string modulo, BE.Cliente cliente, Manejadores.DecisionRenovacion decision,
            int? idPlanNuevo, BE.Builders.ModalidadCobro modalidad, string actor)
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

            var contexto = new Manejadores.ContextoRenovacion
            {
                Cliente = cliente,
                Decision = decision,
                IdPlanNuevo = idPlanNuevo,
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
    }
}
