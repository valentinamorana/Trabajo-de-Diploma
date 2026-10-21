using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// Regla «Oportunidad de upgrade» → ofrecer el plan superior a quienes usan todo su cupo.
    ///
    /// Por plan: clientes con tantas prendas en uso como el límite del plan, si existe un plan activo con
    /// más prendas y más caro (el más barato de esos). Con al menos un cliente:
    ///   impacto       = clientes × ConversionUpgradePct / 100 × diferencia de precio × horizonte
    ///   alternativa   = primer mes al precio actual: conversión × 1,5 pero la diferencia se cobra
    ///                   un mes menos (horizonte − 1)
    /// Botón: lista de contactos (CSV) para la campaña.
    /// </summary>
    public class ReglaUpgradePlan : ReglaBase
    {
        public const string Codigo = "UpgradePlan";
        public override string Tipo => Codigo;

        public override IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx)
        {
            decimal conversion = U(ctx, CatalogoUmbrales.ConversionUpgradePct);
            decimal horizonte = U(ctx, CatalogoUmbrales.HorizonteMeses);

            foreach (var plan in ctx.Planes.Where(p => p.LimitePrendas > 0))
            {
                var superior = ctx.Planes
                    .Where(p => p.LimitePrendas > plan.LimitePrendas && p.Precio > plan.Precio)
                    .OrderBy(p => p.Precio).FirstOrDefault();
                if (superior == null) continue;

                var intensivos = ctx.Clientes
                    .Where(c => c.IdPlan == plan.IdPlan && c.PrendasEnUso >= plan.LimitePrendas)
                    .OrderBy(c => c.Apellido).ToList();
                if (intensivos.Count == 0) continue;

                decimal diferencia = superior.Precio - plan.Precio;
                decimal impacto = Math.Round(intensivos.Count * conversion / 100m * diferencia * horizonte, 2);
                decimal conversionBonificada = Math.Min(100m, conversion * 1.5m);
                decimal impactoBonificado = Math.Round(intensivos.Count * conversionBonificada / 100m * diferencia * Math.Max(0m, horizonte - 1m), 2);

                var contactos = intensivos.Select(c => new BE.ClienteVencimiento
                {
                    IdCliente = c.IdCliente, Nombre = c.NombreCompleto, Email = c.Email, IdPlan = plan.IdPlan,
                    NombrePlan = plan.Nombre, Precio = plan.Precio,
                    FechaVencimiento = c.FechaVencimiento ?? ctx.Hoy,
                    DiasRestantes = c.FechaVencimiento.HasValue ? (int)(c.FechaVencimiento.Value.Date - ctx.Hoy.Date).TotalDays : 0
                }).ToList();
                var parametros = new BE.ParametrosAccion { IdPlan = superior.IdPlan, Ids = intensivos.Select(c => c.IdCliente).ToList(), Contactos = contactos };

                var rec = new BE.Recomendacion
                {
                    TipoRegla = Codigo,
                    Clave = Codigo + ":" + plan.IdPlan,
                    Prioridad = BE.PrioridadRecomendacion.Media,
                    Confianza = intensivos.Count >= 10 ? BE.NivelConfianza.Media : BE.NivelConfianza.Baja,
                    Titulo = T("rec.upgrade.titulo", "Ofrecer el plan {0} a {1} cliente(s) del plan {2}", superior.Nombre, intensivos.Count, plan.Nombre),
                    Diagnostico = T("rec.upgrade.diag",
                        "{0} cliente(s) del plan {1} usan las {2} prendas de su cupo. El plan {3} permite {4} prendas por {5:C2} más por mes.",
                        intensivos.Count, plan.Nombre, plan.LimitePrendas, superior.Nombre, superior.LimitePrendas, diferencia),
                    Motivo = T("rec.upgrade.motivo",
                        "Quien usa todo su cupo valora el servicio: es el cliente más propenso a pagar por más prendas."),
                    Detalle = intensivos.Select(c => $"{c.NombreCompleto} — {c.Email} — {c.PrendasEnUso}/{plan.LimitePrendas}").ToList()
                };
                ElegirMejorOpcion(rec,
                    Opcion(T("rec.upgrade.opcion", "Exportar los contactos y ofrecerles el plan {0}", superior.Nombre),
                        impacto, T("rec.formula.upgrade", "{0} clientes × {1:0.#} % × {2:C2} × {3:0} meses = {4:C2}",
                                   intensivos.Count, conversion, diferencia, horizonte, impacto),
                        BE.AccionRecomendacion.ExportarContactos, parametros),
                    Opcion(T("rec.upgrade.opcion.bonificado", "Ofrecer el plan {0} con el primer mes al precio actual", superior.Nombre),
                        impactoBonificado, T("rec.formula.upgrade", "{0} clientes × {1:0.#} % × {2:C2} × {3:0} meses = {4:C2}",
                                   intensivos.Count, conversionBonificada, diferencia, Math.Max(0m, horizonte - 1m), impactoBonificado),
                        BE.AccionRecomendacion.ExportarContactos, parametros));
                yield return rec;
            }
        }
    }
}
