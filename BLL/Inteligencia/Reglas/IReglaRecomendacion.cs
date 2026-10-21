using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// PN03 «Recomendar / decidir» — Patrón STRATEGY: cada regla de negocio del motor es una estrategia
    /// intercambiable que mira el contexto comercial y devuelve cero o más recomendaciones. El motor
    /// (BLL.Inteligencia.MotorRecomendaciones) las recorre todas, como una cadena de evaluadores
    /// independientes: agregar una regla es agregar una clase, sin tocar las demás. Cada regla es pura
    /// sobre BE.ContextoComercial y se prueba sola (Tests/MotorRecomendacionesTests).
    /// </summary>
    public interface IReglaRecomendacion
    {
        /// <summary>Identificador de la regla (se guarda en DecisionComercial.TipoRegla).</summary>
        string Tipo { get; }

        IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx);
    }

    /// <summary>Utilidades comunes de las reglas (umbrales, textos, armado de opciones).</summary>
    public abstract class ReglaBase : IReglaRecomendacion
    {
        public abstract string Tipo { get; }
        public abstract IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx);

        protected static decimal U(BE.ContextoComercial ctx, string clave) => CatalogoUmbrales.Valor(ctx, clave);

        protected static BE.TextoTraducible T(string clave, string fallback, params object[] args)
            => new BE.TextoTraducible(clave, fallback, args);

        protected static BE.TextoTraducible SinDato() => T("rec.sindato", "sin datos");

        /// <summary>
        /// Ordena las opciones por impacto estimado: la de mayor impacto pasa a ser la principal (la que
        /// el motor recomienda) y las demás quedan como alternativas para que el usuario elija.
        /// </summary>
        protected static void ElegirMejorOpcion(BE.Recomendacion rec, params BE.OpcionRecomendacion[] opciones)
        {
            var ordenadas = opciones.Where(o => o != null).OrderByDescending(o => o.ImpactoEstimado).ToList();
            rec.Principal = ordenadas.FirstOrDefault();
            rec.Alternativas = ordenadas.Skip(1).ToList();
        }

        protected static BE.OpcionRecomendacion Opcion(BE.TextoTraducible descripcion, decimal impacto, BE.TextoTraducible formula,
                                                       BE.AccionRecomendacion accion, BE.ParametrosAccion parametros = null)
            => new BE.OpcionRecomendacion
            {
                Descripcion = descripcion,
                ImpactoEstimado = System.Math.Round(impacto, 2),
                Formula = formula,
                Accion = accion,
                Parametros = parametros ?? new BE.ParametrosAccion()
            };

        /// <summary>Lleva un descuento al múltiplo de 5 más cercano dentro de [mínimo, máximo].</summary>
        protected static decimal RedondearA5(decimal valor, decimal minimo, decimal maximo)
        {
            decimal r = System.Math.Round(valor / 5m, System.MidpointRounding.AwayFromZero) * 5m;
            return System.Math.Min(maximo, System.Math.Max(minimo, r));
        }

        /// <summary>Precio de reposición promedio del catálogo (para prendas sin precio cargado).</summary>
        protected static decimal PrecioReposicionPromedio(BE.ContextoComercial ctx)
        {
            var conPrecio = ctx.Prendas.Where(p => p.PrecioReposicion.HasValue && p.PrecioReposicion.Value > 0).ToList();
            return conPrecio.Count > 0 ? System.Math.Round(conPrecio.Average(p => p.PrecioReposicion.Value), 2) : 0m;
        }
    }
}
