using System;
using System.Collections.Generic;
using BLL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IClienteService (sin base de datos). Implementa todos los
    /// miembros del contrato con cuerpos mínimos y deja un espía sobre ActivarSuscripcion,
    /// que es la que ejercita BLL.Contratacion.ConfirmarPago (PN02) al formalizar la
    /// suscripción tras el cobro.
    /// </summary>
    public class FakeClienteService : IClienteService
    {
        public int ActivarSuscripcionVeces { get; private set; }
        public BE.Cliente UltimoCliente { get; private set; }
        public int UltimoIdPlan { get; private set; }
        public BE.Builders.ModalidadCobro UltimaModalidad { get; private set; }

        public BE.Builders.Suscripcion ActivarSuscripcion(
            string modulo, BE.Cliente cliente, int idPlan, BE.Builders.ModalidadCobro modalidad)
        {
            ActivarSuscripcionVeces++;
            UltimoCliente = cliente;
            UltimoIdPlan = idPlan;
            UltimaModalidad = modalidad;

            return new BE.Builders.Suscripcion(
                cliente, new BE.PlanSuscripcion { IdPlan = idPlan }, modalidad,
                DateTime.Today, DateTime.Today.AddMonths(1));
        }

        // PN02 — mismo espía: a los tests de ConfirmarPago les alcanza con saber que
        // "algo tipo ActivarSuscripcion" fue invocado, sin importar cuál de los dos overloads.
        public BE.Builders.Suscripcion ActivarSuscripcionDesdeContratacion(
            string modulo, BE.Cliente cliente, int idPlan, BE.Builders.ModalidadCobro modalidad)
            => ActivarSuscripcion(modulo, cliente, idPlan, modalidad);

        // Resto del contrato: no ejercitado por estos tests, cuerpos mínimos.
        public List<BE.Cliente> ObtenerTodos() => new List<BE.Cliente>();
        public BE.Cliente ObtenerPorId(int idCliente) => null;
        public void Alta(string modulo, BE.Cliente cliente) { }
        public void Modificar(string modulo, BE.Cliente cliente) { }
        public void Baja(string modulo, BE.Cliente cliente) { }
        public BE.EstadoComercialCliente ObtenerEstadoComercial(BE.Cliente cliente, int prendasSolicitadas) => null;
        public void ReanudarPausa(string modulo, BE.Cliente cliente) { }
    }
}
