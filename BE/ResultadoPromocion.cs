using System;
using System.Collections.Generic;
using System.Linq;

namespace BE
{
    /// <summary>
    /// PN03 — "Evaluar resultado de la promoción" (sección del «Reporte de métricas», carril Gerencia):
    /// compara los movimientos de suscripción del plan de una promoción Vigente, Dada de baja o Vencida
    /// DURANTE su vigencia contra un período ANTERIOR de la misma duración, y mide cuántos de los
    /// clientes que tuvieron la renovación en juego durante la vigencia se retuvieron. Cierra el ciclo
    /// medir → decidir → aplicar → medir: es el dato que justifica sostener o dar de baja la promoción.
    /// </summary>
    public class ResultadoPromocion
    {
        public int IdPromocion { get; set; }
        public string Nombre { get; set; }
        public EstadoPromocion Estado { get; set; }
        public int IdPlan { get; set; }
        public string NombrePlan { get; set; }

        /// <summary>Vigencia real evaluada [Desde, Hasta): desde la aprobación (o la fecha de inicio)
        /// hasta la baja, el vencimiento o hoy.</summary>
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        /// <summary>Período anterior de la misma duración: [AnteriorDesde, Desde).</summary>
        public DateTime AnteriorDesde => Desde.AddDays(-Dias);
        public int Dias => Math.Max(1, (int)Math.Ceiling((Hasta - Desde).TotalDays));

        public ConteoMovimientos Durante { get; set; } = new ConteoMovimientos();
        public ConteoMovimientos Antes { get; set; } = new ConteoMovimientos();

        /// <summary>Variación durante − antes (negativa en bajas = la promoción ayudó).</summary>
        public int VariacionBajas => Durante.Bajas - Antes.Bajas;
        public int VariacionCambiosDePlan => Durante.CambiosDePlan - Antes.CambiosDePlan;
        public int VariacionPausas => Durante.Pausas - Antes.Pausas;
        public int VariacionRenovaciones => Durante.Renovaciones - Antes.Renovaciones;

        /// <summary>Clientes del plan con la renovación en juego durante la vigencia (los que estaban
        /// en riesgo de irse en ese tramo) y cuántos se retuvieron (su última decisión fue renovar o
        /// pausar: ni la baja ni el cambio a otro plan). Lo mismo para el período anterior, que es la
        /// base: sin ella, un plan que siempre renueva mucho parecería un éxito de la promoción.</summary>
        public int ClientesEnJuego { get; set; }
        public int ClientesRetenidos { get; set; }
        public int ClientesEnJuegoAntes { get; set; }
        public int ClientesRetenidosAntes { get; set; }

        /// <summary>Retención real (%) durante la vigencia; null si nadie tuvo la renovación en juego.</summary>
        public decimal? TasaRetencionReal => Porcentaje(ClientesRetenidos, ClientesEnJuego);

        /// <summary>Retención (%) del período anterior: la que hubo sin la promoción.</summary>
        public decimal? TasaRetencionAntes => Porcentaje(ClientesRetenidosAntes, ClientesEnJuegoAntes);

        /// <summary>Mejora de la retención en puntos porcentuales (durante − antes): la parte que se le
        /// puede atribuir a la promoción. null si falta alguno de los dos tramos.</summary>
        public decimal? MejoraRetencion =>
            TasaRetencionReal.HasValue && TasaRetencionAntes.HasValue
                ? TasaRetencionReal.Value - TasaRetencionAntes.Value
                : (decimal?)null;

        /// <summary>Tasa de retención que supuso Gerencia en la sugerencia de origen (null en un alta manual):
        /// el % de clientes en riesgo que la promoción retendría GRACIAS al descuento.</summary>
        public decimal? TasaRetencionEsperada { get; set; }

        public bool RetuvoClientes() => ClientesRetenidos > 0;

        /// <summary>¿La mejora de la retención (durante − antes) alcanzó la retención esperada?
        /// Es una aproximación: compara puntos porcentuales de mejora con el % que Gerencia esperaba
        /// retener por la promoción. null si falta alguno de los datos.</summary>
        public bool? CumplioRetencionEsperada() =>
            MejoraRetencion.HasValue && TasaRetencionEsperada.HasValue
                ? MejoraRetencion.Value >= TasaRetencionEsperada.Value
                : (bool?)null;

        private static decimal? Porcentaje(int parte, int total) =>
            total > 0 ? Math.Round(parte * 100m / total, 2) : (decimal?)null;

        /// <summary>
        /// Cuenta los movimientos de un tramo y, para el tramo de la vigencia, los clientes en juego y
        /// retenidos (por cliente vale su última decisión del tramo).
        /// </summary>
        public static ConteoMovimientos Contar(IEnumerable<DecisionRenovacion> decisiones) =>
            new ConteoMovimientos
            {
                Bajas        = decisiones.Count(d => d.Resultado == EstadoRenovacion.Baja),
                CambiosDePlan = decisiones.Count(d => d.Resultado == EstadoRenovacion.CambioPlan),
                Pausas       = decisiones.Count(d => d.Resultado == EstadoRenovacion.Pausada),
                Renovaciones = decisiones.Count(d => d.Resultado == EstadoRenovacion.Renovada)
            };

        public void CalcularRetencion(IEnumerable<DecisionRenovacion> decisionesDurante,
                                      IEnumerable<DecisionRenovacion> decisionesAntes = null)
        {
            (ClientesEnJuego, ClientesRetenidos) = Retencion(decisionesDurante);
            (ClientesEnJuegoAntes, ClientesRetenidosAntes) = Retencion(decisionesAntes ?? Enumerable.Empty<DecisionRenovacion>());
        }

        // Por cliente vale su última decisión del tramo; se retuvo si renovó o pausó.
        private static (int enJuego, int retenidos) Retencion(IEnumerable<DecisionRenovacion> decisiones)
        {
            var ultimaPorCliente = decisiones
                .GroupBy(d => d.IdCliente)
                .Select(g => g.OrderBy(d => d.Fecha).Last())
                .ToList();
            return (ultimaPorCliente.Count, ultimaPorCliente.Count(d => d.EsRetencion()));
        }
    }

    /// <summary>Bajas, pausas y renovaciones de un plan en un tramo.</summary>
    public class ConteoMovimientos
    {
        public int Bajas { get; set; }
        /// <summary>Clientes que se pasaron a otro plan: para el plan de la promoción son una pérdida.</summary>
        public int CambiosDePlan { get; set; }
        public int Pausas { get; set; }
        public int Renovaciones { get; set; }
    }

    /// <summary>
    /// Una decisión de suscripción de un cliente sobre un plan: la de la renovación de N01
    /// (HistorialRenovacion: Renovada, Baja, Pausada o CambioPlan a OTRO plan) o una recontratación
    /// del mismo plan cobrada en Caja (PN02), que cuenta como Renovada.
    /// </summary>
    public class DecisionRenovacion
    {
        public int IdCliente { get; set; }
        public DateTime Fecha { get; set; }
        public EstadoRenovacion Resultado { get; set; }

        /// <summary>El cliente siguió en el plan: renovó o pausó (la baja y el cambio de plan son pérdidas).</summary>
        public bool EsRetencion() => Resultado == EstadoRenovacion.Renovada || Resultado == EstadoRenovacion.Pausada;
    }
}
