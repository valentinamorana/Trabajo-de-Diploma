using System;

namespace BE.Builders
{
    /// <summary>
    /// Producto del patrón Builder (PdN1): resultado de armar una suscripción para un
    /// cliente — plan elegido, modalidad de cobro y período de vigencia calculado.
    /// No se persiste como tabla propia; sus valores se vuelcan sobre BE.Cliente
    /// (IdPlan, FechaVencimiento) al confirmar la activación.
    /// Construcción por constructor con todas las partes — igual que Pizza(Masa, Salsa,
    /// Agregado, string) del ejemplo de cátedra.
    /// </summary>
    public class Suscripcion
    {
        public Cliente Cliente { get; }
        public PlanSuscripcion Plan { get; }
        public ModalidadCobro Modalidad { get; }
        public DateTime FechaActivacion { get; }
        public DateTime FechaVencimiento { get; }

        public Suscripcion(Cliente cliente, PlanSuscripcion plan, ModalidadCobro modalidad,
                            DateTime fechaActivacion, DateTime fechaVencimiento)
        {
            Cliente = cliente;
            Plan = plan;
            Modalidad = modalidad;
            FechaActivacion = fechaActivacion;
            FechaVencimiento = fechaVencimiento;
        }
    }
}
