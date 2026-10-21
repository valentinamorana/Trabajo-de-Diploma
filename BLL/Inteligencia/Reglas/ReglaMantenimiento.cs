using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// Regla «Mantenimiento excesivo» → revisar o dar de baja y reponer.
    ///
    /// Usa las prendas que el Análisis de Tiempos de Mantenimiento (PdN11) marca con demasiados
    /// mantenimientos o con mantenimientos demasiado largos (sin las que ya están de baja).
    ///   días fuera de servicio = Σ cantidad de mantenimientos × duración promedio
    ///   valor de un día        = ticket promedio / prendas promedio por plan / 30
    ///   costo de seguir igual  = días fuera de servicio × valor de un día
    ///   opción "dar de baja y reponer": costo de seguir igual − Σ precio de reposición
    ///       → al aceptarla, las prendas pasan a Baja (BLL.Prenda.CambiarEstado).
    ///   opción "mandarlas a revisión": sin costo ni ahorro directo (0)
    ///       → al aceptarla, las prendas disponibles pasan a En limpieza y se abre su mantenimiento
    ///         (la transición manual de PN04, BLL.Prenda.CambiarEstado), para revisarlas con el proveedor.
    /// Gana la de mayor impacto: si reponer cuesta más de lo que se pierde, el motor propone revisar.
    /// Las dos exigen la patente de edición de stock; una prenda en uso no se toca (la GUI lo avisa).
    /// </summary>
    public class ReglaMantenimiento : ReglaBase
    {
        public const string Codigo = "Mantenimiento";
        public override string Tipo => Codigo;

        public override IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx)
        {
            var activas = ctx.Prendas.Where(p => p.Estado != BE.EstadoPrenda.Baja).ToDictionary(p => p.IdPrenda);
            var excesivas = ctx.Mantenimientos.Where(m => activas.ContainsKey(m.IdPrenda)).ToList();
            if (excesivas.Count == 0) yield break;

            decimal promedioCatalogo = PrecioReposicionPromedio(ctx);
            decimal diasFuera = Math.Round(excesivas.Sum(m => m.CantidadMantenimientos * (decimal)(m.DuracionPromedioDias ?? 0d)), 1);
            var planesConLimite = ctx.Planes.Where(p => p.LimitePrendas > 0).ToList();
            decimal prendasPorPlan = planesConLimite.Count > 0 ? (decimal)planesConLimite.Average(p => p.LimitePrendas) : 1m;
            decimal valorDia = Math.Round(ctx.TicketPromedio / Math.Max(1m, prendasPorPlan) / 30m, 2);
            decimal perdida = Math.Round(diasFuera * valorDia, 2);
            decimal reposicion = excesivas.Sum(m => activas[m.IdPrenda].PrecioReposicion ?? promedioCatalogo);
            var ids = excesivas.Select(m => m.IdPrenda).ToList();

            var rec = new BE.Recomendacion
            {
                TipoRegla = Codigo,
                Clave = Codigo,
                Prioridad = excesivas.Count >= 3 ? BE.PrioridadRecomendacion.Media : BE.PrioridadRecomendacion.Baja,
                Confianza = BE.NivelConfianza.Media,
                Titulo = T("rec.mant.titulo", "Revisar o dar de baja {0} prenda(s) con mantenimiento excesivo", excesivas.Count),
                Diagnostico = T("rec.mant.diag",
                    "{0} prenda(s) acumulan {1} mantenimiento(s) y unos {2:0.#} días fuera de servicio; cada día de una prenda vale unos {3:C2}.",
                    excesivas.Count, excesivas.Sum(m => m.CantidadMantenimientos), diasFuera, valorDia),
                Motivo = T("rec.mant.motivo",
                    "Una prenda que vive en limpieza no se alquila: conviene reponerla si lo que se pierde supera su costo; si no, revisar el proceso de limpieza."),
                Detalle = excesivas.Select(m => $"#{m.IdPrenda} {m.NombrePrenda} — {m.CantidadMantenimientos} mant. — {(m.DuracionPromedioDias ?? 0d):0.#} d prom.").ToList()
            };
            ElegirMejorOpcion(rec,
                Opcion(T("rec.mant.opcion.baja", "Dar de baja y reponer las {0} prenda(s)", excesivas.Count),
                    perdida - reposicion,
                    T("rec.formula.mant", "{0:0.#} días × {1:C2} − {2:C2} de reposición = {3:C2}", diasFuera, valorDia, reposicion, perdida - reposicion),
                    BE.AccionRecomendacion.DarDeBajaPrendas, new BE.ParametrosAccion { Ids = ids }),
                Opcion(T("rec.mant.opcion.revisar", "Mandar las prendas disponibles a revisión (En limpieza) y revisar el proceso con el proveedor de limpieza"),
                    0m, T("rec.formula.sincosto", "Sin costo ni ingreso directo: se vuelve a evaluar con los datos nuevos."),
                    BE.AccionRecomendacion.EnviarAMantenimiento, new BE.ParametrosAccion { Ids = ids }));
            yield return rec;
        }
    }
}
