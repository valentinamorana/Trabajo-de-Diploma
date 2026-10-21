using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// Regla «Prendas de baja rotación» → darlas de baja para liquidarlas (capital inmovilizado).
    ///
    /// Una prenda es de baja rotación si está Disponible, no tuvo pedidos en el período, nadie la espera y
    /// lleva al menos DiasSinRotacion días en el catálogo. Se recomienda recién con MinPrendasLiquidar o más.
    ///   capital inmovilizado = Σ precio de reposición (o el promedio del catálogo si falta)
    ///   impacto              = capital × RecuperoLiquidacionPct / 100
    /// Al aceptarla, las prendas pasan de Disponible a Baja con la operación existente del catálogo
    /// (BLL.Prenda.CambiarEstado: patente de edición de stock, bitácora); la GUI confirma la lista antes.
    /// </summary>
    public class ReglaBajaRotacion : ReglaBase
    {
        public const string Codigo = "BajaRotacion";
        public override string Tipo => Codigo;

        public override IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx)
        {
            int diasMinimos = (int)U(ctx, CatalogoUmbrales.DiasSinRotacion);
            int minimoPrendas = (int)U(ctx, CatalogoUmbrales.MinPrendasLiquidar);
            decimal promedio = PrecioReposicionPromedio(ctx);

            var quietas = ctx.Prendas
                .Where(p => p.Estado == BE.EstadoPrenda.Disponible && p.PedidosPeriodo == 0 && p.EnListaEspera == 0
                            && (ctx.Hoy.Date - p.FechaAlta.Date).TotalDays >= diasMinimos)
                .OrderBy(p => p.FechaAlta).ToList();
            if (quietas.Count == 0 || quietas.Count < minimoPrendas) yield break;

            decimal capital = quietas.Sum(p => p.PrecioReposicion ?? promedio);
            decimal recupero = U(ctx, CatalogoUmbrales.RecuperoLiquidacionPct);
            decimal impacto = Math.Round(capital * recupero / 100m, 2);
            var ids = quietas.Select(p => p.IdPrenda).ToList();

            var rec = new BE.Recomendacion
            {
                TipoRegla = Codigo,
                Clave = Codigo,
                Prioridad = BE.PrioridadRecomendacion.Media,
                Confianza = ctx.DiasPeriodo >= 60 ? BE.NivelConfianza.Alta : BE.NivelConfianza.Media,
                Titulo = T("rec.rotacion.titulo", "Liquidar o dar de baja {0} prenda(s) sin rotación", quietas.Count),
                Diagnostico = T("rec.rotacion.diag",
                    "{0} prenda(s) disponibles no tuvieron pedidos en los {1} días del período y llevan más de {2} días en el catálogo. Inmovilizan {3:C2} de capital.",
                    quietas.Count, ctx.DiasPeriodo, diasMinimos, capital),
                Motivo = T("rec.rotacion.motivo",
                    "Una prenda que nadie pide ocupa lugar y capital; liquidarla libera fondos para reponer lo que sí tiene demanda."),
                Detalle = quietas.Select(p => $"#{p.IdPrenda} {p.Nombre} — {p.Categoria} {p.Talle} — {(p.PrecioReposicion ?? promedio):C2}").ToList()
            };
            rec.Principal = Opcion(T("rec.rotacion.opcion", "Dar de baja las {0} prenda(s) para liquidarlas (pasan de Disponible a Baja)", quietas.Count),
                impacto, T("rec.formula.rotacion", "{0:C2} de capital × {1:0} % de recupero = {2:C2}", capital, recupero, impacto),
                BE.AccionRecomendacion.DarDeBajaPrendas, new BE.ParametrosAccion { Ids = ids });
            yield return rec;
        }
    }
}
