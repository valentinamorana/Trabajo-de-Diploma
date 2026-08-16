using System;

namespace BE.Builders
{
    /// <summary>
    /// Patrón Builder (PdN1 — Suscripción de cliente). Separa la construcción de una
    /// Suscripción de su representación final, para que el mismo proceso produzca
    /// resultados distintos según la modalidad de cobro elegida (Mensual/Trimestral/Anual).
    /// Mismo esquema que el ejemplo de cátedra (PizzaBuilder): un paso abstracto que cada
    /// builder concreto resuelve devolviendo un valor (CalcularVencimiento, equivalente a
    /// BuildMasa/BuildSalsa/BuildAgregado), y un método concreto (no abstracto) que llama
    /// al paso y arma el producto vía su constructor (BuildSuscripcion, equivalente a
    /// PizzaBuilder.BuildPizza()). AsignarCliente/AsignarPlan no varían por modalidad —
    /// son datos de entrada, no un paso de construcción — así que quedan como métodos
    /// concretos, no abstractos.
    /// </summary>
    public abstract class SuscripcionBuilder
    {
        protected Cliente _cliente;
        protected PlanSuscripcion _plan;

        /// <summary>Modalidad de cobro que representa este builder concreto.</summary>
        public abstract ModalidadCobro Modalidad { get; }

        public virtual void AsignarCliente(Cliente cliente) => _cliente = cliente;

        public virtual void AsignarPlan(PlanSuscripcion plan) => _plan = plan;

        /// <summary>Único paso que varía por modalidad: calcula el vencimiento a partir de la activación.</summary>
        protected abstract DateTime CalcularVencimiento(DateTime fechaActivacion);

        public Suscripcion BuildSuscripcion()
        {
            var fechaActivacion = DateTime.Today;
            var fechaVencimiento = CalcularVencimiento(fechaActivacion);
            return new Suscripcion(_cliente, _plan, Modalidad, fechaActivacion, fechaVencimiento);
        }
    }
}
