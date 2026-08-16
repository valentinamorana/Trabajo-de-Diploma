using System;
using System.Collections.Generic;

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
        private readonly Servicios.Bitacora bitacora = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();
        private readonly Manejadores.ManejadorCobro cadena;

        public Cobro() : this(new DAL.Cliente(), new DAL.Cobro()) { }

        public Cobro(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.ICobroDAL dalCobro)
        {
            this.dalCobro = dalCobro ?? throw new ArgumentNullException(nameof(dalCobro));

            // Arma la cadena de cola a cabeza, con sentencias sueltas — igual que el
            // Program.cs del ejemplo de cátedra (director.AgregarSiguiente(directorGeneral);
            // gerente.AgregarSiguiente(director); comprador.AgregarSiguiente(gerente);).
            var detectar  = new Manejadores.DetectarCobroHandler();
            var procesar  = new Manejadores.ProcesarPagoHandler(dalCliente, dalCobro);
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
    }
}
