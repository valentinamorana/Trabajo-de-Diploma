using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// PN03 — Métricas → decisión. Cruza los reportes del negocio con la sugerencia de promociones:
    /// en vez de que Gerencia parta de una hoja en blanco, el sistema le propone ideas basadas en un
    /// dato concreto.
    ///   · Rotación: categorías con varias prendas sin pedidos → promoción por categoría (informativa).
    ///   · Abandono: planes con clientes en riesgo de irse → promoción de retención por plan.
    /// Es de solo lectura: las candidatas no se guardan; Gerencia decide cuál convertir en sugerencia
    /// (BLL.SugerenciaPromocion.Crear), que sigue el circuito Administración → Contabilidad → Vigente.
    /// </summary>
    public class AnalisisPromociones
    {
        public const int MinimoPrendasPorCategoria = 2;

        /// <summary>Valor de referencia (editable por Gerencia) por prenda parada, para la estimación inicial.</summary>
        public const decimal ValorReferenciaPorPrenda = 1000m;

        private readonly Interfaces.IAnalisisRotacionService rotacion;
        private readonly Interfaces.IAnalisisAbandonoService abandono;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;

        public AnalisisPromociones()
            : this(new AnalisisRotacion(), new AnalisisAbandono(), new DAL.PlanSuscripcion()) { }

        public AnalisisPromociones(Interfaces.IAnalisisRotacionService rotacion,
                                   Interfaces.IAnalisisAbandonoService abandono,
                                   DAL.Interfaces.IPlanSuscripcionDAL dalPlan)
        {
            this.rotacion = rotacion ?? throw new ArgumentNullException(nameof(rotacion));
            this.abandono = abandono ?? throw new ArgumentNullException(nameof(abandono));
            this.dalPlan  = dalPlan  ?? throw new ArgumentNullException(nameof(dalPlan));
        }

        public List<BE.CandidataSugerencia> Detectar()
        {
            var candidatas = new List<BE.CandidataSugerencia>();

            // Rotación: prendas de baja demanda agrupadas por categoría.
            var bajaDemanda = rotacion.Detectar()
                .Where(r => r.Clave == "rotacion.motivo.bajademanda" && !string.IsNullOrWhiteSpace(r.Categoria))
                .GroupBy(r => r.Categoria.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() >= MinimoPrendasPorCategoria);
            foreach (var g in bajaDemanda)
            {
                string ejemplos = string.Join(", ", g.Select(r => r.NombrePrenda).Take(3));
                candidatas.Add(new BE.CandidataSugerencia
                {
                    Origen = "Rotación",
                    CategoriaPrenda = g.Key,
                    TipoSugerido = BE.TipoDescuento.MontoFijo,
                    BeneficioEstimado = g.Count() * ValorReferenciaPorPrenda,
                    Motivo = $"Análisis de rotación: {g.Count()} prenda(s) de la categoría {g.Key} sin pedidos ({ejemplos})."
                });
            }

            // Abandono: clientes en riesgo agrupados por plan (el ingreso mensual en riesgo es el beneficio a proteger).
            var planes = dalPlan.ObtenerTodos();
            foreach (var g in abandono.Detectar()
                                      .Where(c => !string.IsNullOrWhiteSpace(c.NombrePlan))
                                      .GroupBy(c => c.NombrePlan))
            {
                var plan = planes.Find(p => p.Estado && p.Nombre == g.Key);
                if (plan == null) continue;
                candidatas.Add(new BE.CandidataSugerencia
                {
                    Origen = "Abandono",
                    IdPlan = plan.IdPlan,
                    NombrePlan = plan.Nombre,
                    TipoSugerido = BE.TipoDescuento.Porcentaje,
                    BeneficioEstimado = g.Count() * plan.Precio,
                    Motivo = $"Análisis de abandono: {g.Count()} cliente(s) del plan {plan.Nombre} en riesgo de abandono " +
                             $"(${g.Count() * plan.Precio} de ingreso mensual en riesgo)."
                });
            }

            return candidatas.OrderByDescending(c => c.BeneficioEstimado).ToList();
        }
    }
}
