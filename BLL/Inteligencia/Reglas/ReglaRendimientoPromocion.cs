using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// Regla «Promoción vigente que rinde mal / bien» → dar de baja o extender.
    ///
    /// Toma el veredicto de "Medir resultado" (MedicionPromociones) de cada promoción VIGENTE:
    ///   · Dar de baja: ahorro = descuento diario actual × 30 (lo que se deja de regalar por mes)
    ///       = total descontado / días de vigencia × 30. Al aceptarla se registra la «Solicitud de baja»
    ///       (camino existente de PN03: la atiende Administración).
    ///   · Extender: beneficio mensual = beneficio real / días de vigencia × 30. Al aceptarla se registra una
    ///       nueva sugerencia para el mismo plan (vuelve a pasar por Administración y Contabilidad).
    /// Sin alternativas "esperar" / "dejarla vencer": no hacer nada no es una decisión que ejecutar (para
    /// eso está Descartar o Posponer). Una promoción que rinde lo esperado (Mantener) no genera recomendación.
    /// </summary>
    public class ReglaRendimientoPromocion : ReglaBase
    {
        public const string Codigo = "RendimientoPromocion";
        public override string Tipo => Codigo;

        /// <summary>
        /// "Medir resultado → Aplicar veredicto": la misma recomendación que daría el motor para una
        /// promoción vigente (dar de baja o extender). null si la promoción no está vigente, si no hay datos
        /// para opinar o si el veredicto es Mantener (no hay nada que ejecutar: sigue como está).
        /// </summary>
        public static BE.Recomendacion DesdeMedicion(BE.MedicionPromocion m, DateTime hoy, Dictionary<string, decimal> umbrales)
        {
            if (m?.Promocion == null || m.Promocion.Estado != BE.EstadoPromocion.Vigente || m.Veredicto == BE.VeredictoPromocion.SinDatos)
                return null;
            var ctx = new BE.ContextoComercial
            {
                Hoy = hoy, Desde = hoy, Hasta = hoy,
                Umbrales = umbrales ?? CatalogoUmbrales.ValoresPorDefecto()
            };
            ctx.Promociones.Add(m);
            return new ReglaRendimientoPromocion().Evaluar(ctx).FirstOrDefault();
        }

        public override IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx)
        {
            foreach (var m in ctx.Promociones.Where(x => x.Promocion != null && x.Promocion.Estado == BE.EstadoPromocion.Vigente))
            {
                var r = m.Resultado;
                int dias = Math.Max(1, r.Dias);
                var confianza = r.ClientesEnJuego >= 10 ? BE.NivelConfianza.Alta
                              : r.ClientesEnJuego >= 3 ? BE.NivelConfianza.Media : BE.NivelConfianza.Baja;
                string mejora = r.MejoraRetencion.HasValue ? r.MejoraRetencion.Value.ToString("+0.#;-0.#;0") + " pp" : "—";

                if (m.Veredicto == BE.VeredictoPromocion.DarDeBaja)
                {
                    decimal porDia = m.TotalDescontado / dias;
                    decimal ahorro = Math.Round(porDia * 30m, 2);
                    var rec = new BE.Recomendacion
                    {
                        TipoRegla = Codigo,
                        Clave = Codigo + ":" + m.IdPromocion + ":Baja",
                        Prioridad = BE.PrioridadRecomendacion.Alta,
                        Confianza = confianza,
                        Titulo = T("rec.promo.baja.titulo", "Dar de baja la promoción \"{0}\": rinde por debajo de lo esperado", m.Nombre),
                        Diagnostico = T("rec.promo.diag",
                            "En {0} día(s) de vigencia: {1} cliente(s) con la renovación en juego, retención {2} contra el período anterior, {3:C2} descontados en {4} cobro(s).",
                            dias, r.ClientesEnJuego, mejora, m.TotalDescontado, m.Cobros),
                        Motivo = m.Explicacion
                    };
                    var parametros = new BE.ParametrosAccion { IdPromocion = m.IdPromocion, IdPlan = r.IdPlan };
                    rec.Principal = Opcion(T("rec.promo.baja.opcion", "Solicitar la baja de la promoción (la atiende Administración)"),
                        ahorro, T("rec.formula.promo.baja", "{0:C2} descontados / {1} días × 30 = {2:C2} por mes que se dejan de descontar", m.TotalDescontado, dias, ahorro),
                        BE.AccionRecomendacion.SolicitarBajaPromocion, parametros);

                    yield return rec;
                }
                else if (m.Veredicto == BE.VeredictoPromocion.Extender)
                {
                    decimal mensual = Math.Round((m.BeneficioReal ?? 0m) / dias * 30m, 2);
                    var parametros = new BE.ParametrosAccion
                    {
                        IdPromocion = m.IdPromocion,
                        IdPlan = r.IdPlan,
                        DescuentoPorcentaje = m.Promocion.TipoDescuento == BE.TipoDescuento.Porcentaje ? m.Promocion.Valor : 0m,
                        DuracionDias = Math.Max(1, (int)(m.Promocion.FechaFin.Date - m.Promocion.FechaInicio.Date).TotalDays + 1),
                        TasaRetencion = Math.Min(100m, Math.Max(1m, r.MejoraRetencion ?? CatalogoUmbrales.Valor(ctx, CatalogoUmbrales.TasaRetencionDefecto))),
                        ClientesObjetivo = r.ClientesEnJuego,
                        IngresoEnRiesgo = Math.Max(m.PrecioPlan, r.ClientesEnJuego * m.PrecioPlan)
                    };
                    var rec = new BE.Recomendacion
                    {
                        TipoRegla = Codigo,
                        Clave = Codigo + ":" + m.IdPromocion + ":Extender",
                        Prioridad = BE.PrioridadRecomendacion.Media,
                        Confianza = confianza,
                        Titulo = T("rec.promo.ext.titulo", "Extender la promoción \"{0}\": cumple lo esperado", m.Nombre),
                        Diagnostico = T("rec.promo.diag",
                            "En {0} día(s) de vigencia: {1} cliente(s) con la renovación en juego, retención {2} contra el período anterior, {3:C2} descontados en {4} cobro(s).",
                            dias, r.ClientesEnJuego, mejora, m.TotalDescontado, m.Cobros),
                        Motivo = m.Explicacion
                    };
                    rec.Principal = Opcion(T("rec.promo.ext.opcion", "Sugerir una extensión de la promoción para el plan {0} (pasa por Administración y Contabilidad)", r.NombrePlan),
                        mensual, T("rec.formula.promo.ext", "{0:C2} de beneficio real / {1} días × 30 = {2:C2} por mes", m.BeneficioReal ?? 0m, dias, mensual),
                        BE.AccionRecomendacion.ExtenderPromocion, parametros);

                    yield return rec;
                }
            }
        }
    }
}
