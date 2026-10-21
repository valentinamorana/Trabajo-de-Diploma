using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// Regla «Bajas altas en un plan» → promoción de retención.
    ///
    /// Se dispara si el % de bajas del plan llega al umbral ChurnAltoPct, o si subió SubidaChurnPp puntos
    /// contra el período anterior (con al menos una baja). No se dispara si el plan ya tiene una promoción
    /// vigente (un solo descuento por ciclo, PoliticaDescuento): esa la evalúa ReglaRendimientoPromocion.
    ///
    /// Cálculo de la propuesta:
    ///   gravedad   = % de bajas / umbral (mínimo 1)
    ///   descuento  = DescuentoBasePct × gravedad, redondeado a 5, entre 5 y DescuentoMaximoPct
    ///   duración   = 60 días si la gravedad es 2 o más; si no, 30
    ///   retención  = mejora promedio de las promociones ya medidas del plan; sin historia, TasaRetencionDefecto
    ///   objetivo   = clientes en riesgo del plan (o las bajas del período, si el análisis no marcó ninguno)
    ///   impacto    = beneficio neto del simulador (SimuladorPromocion) con esos supuestos
    /// Opciones que compiten (gana la de mayor impacto): la propuesta, una promoción más suave (−5 % de
    /// descuento, se supone el 75 % de la retención) y una campaña de contacto sin descuento (un tercio de
    /// la retención, sin costo; exporta los contactos y después se mide cuántos renovaron).
    /// </summary>
    public class ReglaChurnPlan : ReglaBase
    {
        public const string Codigo = "ChurnPlan";
        public override string Tipo => Codigo;

        public const decimal FactorRetencionPromocionSuave = 0.75m;
        public const decimal FactorRetencionContacto = 1m / 3m;

        public override IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx)
        {
            decimal umbral = U(ctx, CatalogoUmbrales.ChurnAltoPct);
            decimal subida = U(ctx, CatalogoUmbrales.SubidaChurnPp);
            decimal horizonte = U(ctx, CatalogoUmbrales.HorizonteMeses);

            foreach (var plan in ctx.Planes.Where(p => p.Precio > 0 && p.Churn.HasValue && !p.TienePromocionVigente))
            {
                decimal churn = plan.Churn.Value;
                bool porNivel = churn >= umbral;
                bool porSuba = plan.ChurnAnterior.HasValue && churn - plan.ChurnAnterior.Value >= subida && plan.Bajas > 0;
                if (!porNivel && !porSuba) continue;

                decimal gravedad = umbral > 0 ? Math.Max(1m, churn / umbral) : 1m;
                decimal descuento = RedondearA5(U(ctx, CatalogoUmbrales.DescuentoBasePct) * gravedad, 5m,
                                                Math.Max(5m, U(ctx, CatalogoUmbrales.DescuentoMaximoPct)));
                int dias = gravedad >= 2m ? 60 : 30;
                decimal retencion = Math.Min(100m, Math.Max(1m, plan.RetencionHistorica ?? U(ctx, CatalogoUmbrales.TasaRetencionDefecto)));
                int objetivo = Math.Max(plan.ClientesEnRiesgo, plan.Bajas);

                var rec = new BE.Recomendacion
                {
                    TipoRegla = Codigo,
                    Clave = Codigo + ":" + plan.IdPlan,
                    Titulo = T("rec.churn.titulo", "Bajas altas en el plan {0}: actuar para retener a los clientes en riesgo", plan.Nombre),
                    Diagnostico = T("rec.churn.diag",
                        "El plan {0} perdió {1} cliente(s) de {2} en el período ({3:0.#} % de bajas; período anterior: {4}). Hay {5} cliente(s) en riesgo ({6:C2} de ingreso mensual).",
                        plan.Nombre, plan.Bajas, plan.ClientesInicio, churn,
                        plan.ChurnAnterior.HasValue ? (object)(plan.ChurnAnterior.Value.ToString("0.#") + " %") : SinDato(),
                        plan.ClientesEnRiesgo, plan.IngresoEnRiesgo),
                    Motivo = T(porNivel ? "rec.churn.motivo.nivel" : "rec.churn.motivo.suba",
                        porNivel
                            ? "El % de bajas llegó al umbral de {0:0.#} %. La retención supuesta ({1:0.#} %) sale {2}."
                            : "El % de bajas subió más de {0:0.#} pp contra el período anterior. La retención supuesta ({1:0.#} %) sale {2}.",
                        porNivel ? umbral : subida, retencion,
                        plan.RetencionHistorica.HasValue
                            ? T("rec.fuente.historica", "de las {0} promoción(es) ya medidas del plan", plan.PromocionesMedidas)
                            : T("rec.fuente.defecto", "del valor por defecto configurado (el plan no tiene promociones medidas)")),
                    Confianza = plan.PromocionesMedidas >= 2 && plan.ClientesInicio >= 20 ? BE.NivelConfianza.Alta
                              : plan.PromocionesMedidas >= 1 || plan.ClientesInicio >= 10 ? BE.NivelConfianza.Media
                              : BE.NivelConfianza.Baja
                };

                var propuesta = OpcionPromocion(plan, objetivo, descuento, dias, retencion, horizonte, "rec.churn.opcion",
                    "Lanzar una promoción de retención en el plan {0} con {1:0} % de descuento durante {2} días");
                decimal descuentoSuave = descuento > 5m ? descuento - 5m : Math.Max(1m, descuento / 2m);
                var suave = OpcionPromocion(plan, objetivo, descuentoSuave, dias, retencion * FactorRetencionPromocionSuave, horizonte,
                    "rec.churn.opcion.suave", "Promoción más suave en el plan {0}: {1:0} % durante {2} días (se supone el 75 % de la retención)");

                decimal retencionContacto = Math.Round(retencion * FactorRetencionContacto, 2);
                decimal impactoContacto = Math.Round(objetivo * retencionContacto / 100m * plan.Precio * horizonte, 2);
                // La campaña de contacto también se ejecuta: exporta los contactos del plan (primero los que
                // vencen o ya vencieron; si no hay, todos los clientes activos del plan). Sin contactos no
                // hay a quién llamar: la opción no se ofrece (toda opción hace algo al aceptarla).
                var contactos = ContactosDelPlan(ctx, plan);
                var contacto = contactos.Count == 0 ? null : Opcion(
                    T("rec.churn.opcion.contacto", "Sin descuento: campaña de contacto personal a los {0} cliente(s) en riesgo (se supone un tercio de la retención)", objetivo),
                    impactoContacto,
                    T("rec.formula.contacto", "{0} clientes × {1:C2} × {2:0.#} % × {3:0} meses = {4:C2}",
                      objetivo, plan.Precio, retencionContacto, horizonte, impactoContacto),
                    BE.AccionRecomendacion.ExportarContactos,
                    new BE.ParametrosAccion
                    {
                        IdPlan = plan.IdPlan,
                        Ids = contactos.Select(c => c.IdCliente).ToList(),
                        Contactos = contactos
                    });

                ElegirMejorOpcion(rec, propuesta, suave, contacto);
                rec.Prioridad = rec.ImpactoEstimado <= 0 ? BE.PrioridadRecomendacion.Baja
                              : gravedad >= 2m ? BE.PrioridadRecomendacion.Alta
                              : BE.PrioridadRecomendacion.Media;
                if (rec.ImpactoEstimado <= 0)
                    rec.Motivo = T("rec.churn.motivo.norentable",
                        "Las bajas están por encima del umbral, pero con los supuestos actuales ninguna opción recupera su costo: revise los supuestos en el simulador antes de decidir.");
                yield return rec;
            }
        }

        private static List<BE.ClienteVencimiento> ContactosDelPlan(BE.ContextoComercial ctx, BE.MetricaPlanComercial plan)
        {
            var porVencer = ctx.Vencimientos.Where(v => v.IdPlan == plan.IdPlan).ToList();
            if (porVencer.Count > 0) return porVencer;
            return ctx.Clientes
                .Where(c => c.IdPlan == plan.IdPlan)
                .Select(c => new BE.ClienteVencimiento
                {
                    IdCliente = c.IdCliente, Nombre = c.NombreCompleto, Email = c.Email,
                    IdPlan = c.IdPlan, NombrePlan = plan.Nombre, Precio = plan.Precio,
                    FechaVencimiento = c.FechaVencimiento ?? ctx.Hoy,
                    DiasRestantes = c.FechaVencimiento.HasValue ? (int)(c.FechaVencimiento.Value.Date - ctx.Hoy.Date).TotalDays : 0
                })
                .ToList();
        }

        private static BE.OpcionRecomendacion OpcionPromocion(BE.MetricaPlanComercial plan, int objetivo, decimal descuento, int dias,
                                                              decimal retencion, decimal horizonte, string clave, string fallback)
        {
            var escenario = new BE.EscenarioPromocion
            {
                IdPlan = plan.IdPlan, NombrePlan = plan.Nombre, PrecioPlan = plan.Precio,
                ClientesPlan = plan.ClientesActivos, ClientesObjetivo = objetivo,
                DescuentoPorcentaje = descuento, DuracionDias = dias,
                TasaRetencion = Math.Round(retencion, 2), HorizonteMeses = horizonte
            };
            decimal costo = SimuladorPromocion.CostoDescuento(escenario);
            decimal impacto = SimuladorPromocion.BeneficioNeto(escenario, escenario.TasaRetencion);
            return Opcion(T(clave, fallback, plan.Nombre, descuento, dias), impacto,
                T("rec.formula.promo",
                  "{0} clientes × {1:C2} × {2:0.#} % × {3:0} meses − {4} clientes × {1:C2} × {5:0} % × {6:0.##} meses = {7:C2} − {8:C2} = {9:C2}",
                  objetivo, plan.Precio, escenario.TasaRetencion, horizonte, Math.Max(plan.ClientesActivos, objetivo), descuento,
                  dias / 30m, impacto + costo, costo, impacto),
                BE.AccionRecomendacion.SugerirPromocion,
                new BE.ParametrosAccion
                {
                    IdPlan = plan.IdPlan, DescuentoPorcentaje = descuento, DuracionDias = dias,
                    TasaRetencion = escenario.TasaRetencion,
                    ClientesObjetivo = objetivo,
                    IngresoEnRiesgo = Math.Max(plan.IngresoEnRiesgo, objetivo * plan.Precio)
                });
        }
    }
}
