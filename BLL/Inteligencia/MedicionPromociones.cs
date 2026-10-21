using System;

namespace BLL.Inteligencia
{
    /// <summary>Parámetros del veredicto de una promoción (salen de los umbrales configurables).</summary>
    public class CriterioMedicion
    {
        public decimal HorizonteMeses { get; set; } = CatalogoUmbrales.PorDefecto(CatalogoUmbrales.HorizonteMeses);
        public decimal RendimientoMinimoPct { get; set; } = CatalogoUmbrales.PorDefecto(CatalogoUmbrales.RendimientoMinimoPct);
        public int DiasMinimosEvaluacion { get; set; } = (int)CatalogoUmbrales.PorDefecto(CatalogoUmbrales.DiasMinimosEvaluacion);
        public int DiasAvisoVencimiento { get; set; } = (int)CatalogoUmbrales.PorDefecto(CatalogoUmbrales.DiasAvisoVencimientoPromo);

        public static CriterioMedicion Desde(System.Collections.Generic.IDictionary<string, decimal> umbrales)
        {
            decimal V(string k) => umbrales != null && umbrales.TryGetValue(k, out var v) ? v : CatalogoUmbrales.PorDefecto(k);
            return new CriterioMedicion
            {
                HorizonteMeses = V(CatalogoUmbrales.HorizonteMeses),
                RendimientoMinimoPct = V(CatalogoUmbrales.RendimientoMinimoPct),
                DiasMinimosEvaluacion = (int)V(CatalogoUmbrales.DiasMinimosEvaluacion),
                DiasAvisoVencimiento = (int)V(CatalogoUmbrales.DiasAvisoVencimientoPromo)
            };
        }
    }

    /// <summary>
    /// PN03 «Medir resultado» — Convierte el «Resultado de la promoción» (BLL.AnalisisPromociones) en
    /// plata y en una decisión. Cálculo puro (Tests/MedicionPromocionesTests):
    ///
    ///   retenidos atribuibles = clientes en juego × mejora de retención (pp) / 100
    ///   beneficio real        = retenidos atribuibles × precio del plan × horizonte − total descontado
    ///   ROI real              = beneficio real / total descontado × 100
    ///   beneficio estimado    = ingreso en riesgo × retención esperada / 100 × horizonte (de la sugerencia)
    ///   ROI esperado          = (beneficio estimado − total descontado) / total descontado × 100
    ///   cumplimiento          = mejora lograda / retención esperada × 100
    ///
    /// Veredicto (en este orden): sin clientes en juego o sin base → Sin datos; menos días que el mínimo →
    /// Mantener (todavía es pronto); la retención no mejoró → Dar de baja; cumplimiento menor al mínimo →
    /// Dar de baja; beneficio real negativo → Dar de baja; vigente, cumple y vence pronto → Extender;
    /// si no, Mantener.
    /// </summary>
    public static class MedicionPromociones
    {
        public static BE.MedicionPromocion Evaluar(BE.ResultadoPromocion resultado, BE.Promocion promocion, decimal precioPlan,
                                                   BE.MetricaImpactoPromocion impacto, BE.SugerenciaPromocion sugerencia,
                                                   CriterioMedicion criterio, DateTime hoy)
        {
            if (resultado == null) throw new ArgumentNullException(nameof(resultado));
            criterio = criterio ?? new CriterioMedicion();
            var m = new BE.MedicionPromocion
            {
                Resultado = resultado,
                Promocion = promocion,
                PrecioPlan = precioPlan,
                TotalDescontado = impacto?.TotalDescontado ?? 0m,
                Cobros = impacto?.Cobros ?? 0
            };

            if (sugerencia != null)
                m.BeneficioEstimado = Math.Round(sugerencia.IngresoEnRiesgo() * sugerencia.TasaRetencion / 100m * criterio.HorizonteMeses, 2);

            if (!resultado.NoSePudoCalcular && resultado.MejoraRetencion.HasValue && resultado.ClientesEnJuego > 0)
            {
                m.RetenidosAtribuibles = Math.Round(Math.Max(0m, resultado.ClientesEnJuego * resultado.MejoraRetencion.Value / 100m), 2);
                m.BeneficioReal = Math.Round(m.RetenidosAtribuibles.Value * precioPlan * criterio.HorizonteMeses - m.TotalDescontado, 2);
                if (m.TotalDescontado > 0)
                    m.RoiReal = Math.Round(m.BeneficioReal.Value * 100m / m.TotalDescontado, 2);
            }
            if (m.BeneficioEstimado.HasValue && m.TotalDescontado > 0)
                m.RoiEstimado = Math.Round((m.BeneficioEstimado.Value - m.TotalDescontado) * 100m / m.TotalDescontado, 2);

            Dictaminar(m, criterio, hoy);
            return m;
        }

        private static void Dictaminar(BE.MedicionPromocion m, CriterioMedicion c, DateTime hoy)
        {
            var r = m.Resultado;
            bool vigente = m.Promocion?.Estado == BE.EstadoPromocion.Vigente || (m.Promocion == null && r.Estado == BE.EstadoPromocion.Vigente);
            if (r.NoSePudoCalcular || r.ClientesEnJuego == 0 || !r.MejoraRetencion.HasValue)
            {
                m.Veredicto = BE.VeredictoPromocion.SinDatos;
                m.Explicacion = new BE.TextoTraducible("med.ver.sindatos",
                    "Todavía no hay clientes con la renovación en juego o falta la base del período anterior: no se puede opinar.");
                return;
            }
            decimal mejora = r.MejoraRetencion.Value;
            if (vigente && r.Dias < c.DiasMinimosEvaluacion)
            {
                m.Veredicto = BE.VeredictoPromocion.Mantener;
                m.Explicacion = new BE.TextoTraducible("med.ver.pocosdias",
                    "Lleva {0} día(s) vigente: espere al menos {1} días para decidir.", r.Dias, c.DiasMinimosEvaluacion);
                return;
            }
            if (mejora <= 0)
            {
                m.Veredicto = BE.VeredictoPromocion.DarDeBaja;
                m.Explicacion = new BE.TextoTraducible("med.ver.sinmejora",
                    "La retención no mejoró ({0:+0.#;-0.#;0} pp contra el período anterior) y la promoción ya costó {1:C2}: conviene darla de baja.",
                    mejora, m.TotalDescontado);
                return;
            }
            decimal? cumplimiento = r.TasaRetencionEsperada.HasValue && r.TasaRetencionEsperada.Value > 0
                ? Math.Round(mejora * 100m / r.TasaRetencionEsperada.Value, 2) : (decimal?)null;
            if (cumplimiento.HasValue && cumplimiento.Value < c.RendimientoMinimoPct)
            {
                m.Veredicto = BE.VeredictoPromocion.DarDeBaja;
                m.Explicacion = new BE.TextoTraducible("med.ver.bajorendimiento",
                    "Mejoró {0:0.#} pp, apenas el {1:0} % de lo esperado ({2:0.#} %): conviene darla de baja.",
                    mejora, cumplimiento.Value, r.TasaRetencionEsperada.Value);
                return;
            }
            if (m.BeneficioReal.HasValue && m.BeneficioReal.Value < 0)
            {
                m.Veredicto = BE.VeredictoPromocion.DarDeBaja;
                m.Explicacion = new BE.TextoTraducible("med.ver.perdida",
                    "Retiene clientes, pero el descuento cuesta más de lo que retiene: beneficio real {0:C2}.", m.BeneficioReal.Value);
                return;
            }
            if (vigente && m.Promocion != null && (cumplimiento == null || cumplimiento.Value >= 100m)
                && (m.Promocion.FechaFin.Date - hoy.Date).TotalDays <= c.DiasAvisoVencimiento)
            {
                m.Veredicto = BE.VeredictoPromocion.Extender;
                m.Explicacion = new BE.TextoTraducible("med.ver.extender",
                    "Cumple lo esperado (mejora de {0:0.#} pp) y vence el {1:d}: conviene extenderla.", mejora, m.Promocion.FechaFin);
                return;
            }
            m.Veredicto = BE.VeredictoPromocion.Mantener;
            m.Explicacion = new BE.TextoTraducible("med.ver.mantener",
                "Rinde dentro de lo esperado (mejora de {0:0.#} pp, beneficio real {1:C2}): conviene mantenerla.",
                mejora, m.BeneficioReal ?? 0m);
        }
    }
}
