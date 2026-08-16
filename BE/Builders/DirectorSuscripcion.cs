using System;

namespace BE.Builders
{
    /// <summary>
    /// Director del patrón Builder: fija el orden en que se cargan los datos de entrada
    /// y dispara el armado del producto. El diagrama de clase de cátedra (Pizzeria) sí
    /// muestra un Director separado del Builder, aunque el código de ejemplo fusiona ese
    /// rol dentro de PizzaBuilder.BuildPizza(); acá se mantiene como clase aparte para
    /// respetar el diagrama.
    /// </summary>
    public static class DirectorSuscripcion
    {
        public static Suscripcion Construir(SuscripcionBuilder builder, Cliente cliente, PlanSuscripcion plan)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            builder.AsignarCliente(cliente);
            builder.AsignarPlan(plan);
            return builder.BuildSuscripcion();
        }
    }
}
