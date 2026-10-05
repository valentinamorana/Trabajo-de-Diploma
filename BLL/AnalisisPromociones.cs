using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// PN03 — carril Gerencia del diagrama (ver el mapeo completo en BLL.Promocion):
    ///
    ///   Analizar métricas («Reporte de métricas») ........ AnalizarMetricas
    ///   ¿Hay oportunidad? (No → fin "Sin promoción") ..... HayOportunidad
    ///
    /// Cruza los reportes del negocio con la sugerencia de promociones:
    ///   · Abandono por plan: clientes en riesgo de irse → promoción de retención por plan.
    ///   · Rotación por categoría: varias prendas sin pedidos → promoción por categoría (informativa).
    /// Es de solo lectura: el reporte se calcula sobre los datos guardados y se puede imprimir;
    /// Gerencia decide qué oportunidad convertir en sugerencia (SugerenciaPromocion.RegistrarSugerencia).
    /// </summary>
    public class AnalisisPromociones
    {
        public const int MinimoPrendasPorCategoria = 2;

        /// <summary>Valor de referencia (editable por Gerencia) por prenda parada, para la estimación inicial.</summary>
        public const decimal ValorReferenciaPorPrenda = 1000m;

        private const string ClaveBajaDemanda = "rotacion.motivo.bajademanda";
        private const string ModuloGerencia = "Promociones";

        private readonly Interfaces.IAnalisisRotacionService rotacion;
        private readonly Interfaces.IAnalisisAbandonoService abandono;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();

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

        // "Analizar métricas": abandono por plan y rotación por categoría → «Reporte de métricas»
        // con las oportunidades detectadas. Si no hay ninguna, el flujo termina "Sin promoción".
        public BE.ReporteMetricas AnalizarMetricas(string modulo)
        {
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

            var rot = rotacion.Detectar();
            var ab = abandono.Detectar();
            var planes = dalPlan.ObtenerTodos();

            var reporte = new BE.ReporteMetricas
            {
                Fecha = DateTime.Now,
                AbandonoPorPlan = AbandonoPorPlan(ab, planes),
                RotacionPorCategoria = rot
                    .Where(r => !string.IsNullOrWhiteSpace(r.Categoria))
                    .GroupBy(r => r.Categoria.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new BE.MetricaRotacionCategoria
                    {
                        Categoria = g.Key,
                        PrendasBajaDemanda = g.Count(r => r.Clave == ClaveBajaDemanda),
                        PrendasAltaDemanda = g.Count(r => r.Clave != ClaveBajaDemanda)
                    })
                    .OrderByDescending(m => m.PrendasBajaDemanda).ToList(),
                Oportunidades = Oportunidades(rot, ab, planes)
            };

            try
            {
                bitacora.Registrar(modulo, HayOportunidad(reporte)
                        ? $"Analizar métricas: {reporte.Oportunidades.Count} oportunidad(es) de promoción detectada(s)"
                        : "Analizar métricas: sin oportunidad de promoción (fin sin promoción)",
                    BE.Criticidad.Baja);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[BLL.AnalisisPromociones] {ex.Message}"); }
            return reporte;
        }

        // "¿Hay oportunidad?": el reporte detectó al menos un caso para sugerir.
        public bool HayOportunidad(BE.ReporteMetricas reporte) => reporte != null && reporte.HayOportunidad();

        // Oportunidades detectadas (sin exigir permisos: lo usa AnalizarMetricas y las pruebas).
        public List<BE.CandidataSugerencia> Detectar()
            => Oportunidades(rotacion.Detectar(), abandono.Detectar(), dalPlan.ObtenerTodos());

        private static List<BE.MetricaAbandonoPlan> AbandonoPorPlan(List<BE.ClienteEnRiesgo> enRiesgo, List<BE.PlanSuscripcion> planes)
        {
            var lista = new List<BE.MetricaAbandonoPlan>();
            foreach (var g in enRiesgo.Where(c => !string.IsNullOrWhiteSpace(c.NombrePlan)).GroupBy(c => c.NombrePlan))
            {
                var plan = planes.Find(p => p.Estado && p.Nombre == g.Key);
                if (plan == null) continue;
                lista.Add(new BE.MetricaAbandonoPlan
                {
                    IdPlan = plan.IdPlan,
                    NombrePlan = plan.Nombre,
                    ClientesEnRiesgo = g.Count(),
                    IngresoMensualEnRiesgo = g.Count() * plan.Precio
                });
            }
            return lista.OrderByDescending(m => m.IngresoMensualEnRiesgo).ToList();
        }

        private static List<BE.CandidataSugerencia> Oportunidades(List<BE.RotacionPrenda> rot, List<BE.ClienteEnRiesgo> ab,
                                                                  List<BE.PlanSuscripcion> planes)
        {
            var candidatas = new List<BE.CandidataSugerencia>();

            // Rotación: prendas de baja demanda agrupadas por categoría.
            var bajaDemanda = rot
                .Where(r => r.Clave == ClaveBajaDemanda && !string.IsNullOrWhiteSpace(r.Categoria))
                .GroupBy(r => r.Categoria.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() >= MinimoPrendasPorCategoria);
            foreach (var g in bajaDemanda)
            {
                string ejemplos = string.Join(", ", g.Select(r => r.NombrePrenda).Take(3));
                candidatas.Add(new BE.CandidataSugerencia
                {
                    Origen = BE.OrigenMetrica.Rotacion,
                    CategoriaPrenda = g.Key,
                    TipoSugerido = BE.TipoDescuento.MontoFijo,
                    BeneficioEstimado = g.Count() * ValorReferenciaPorPrenda,
                    Motivo = $"Análisis de rotación: {g.Count()} prenda(s) de la categoría {g.Key} sin pedidos ({ejemplos})."
                });
            }

            // Abandono: clientes en riesgo agrupados por plan (el ingreso mensual en riesgo es el beneficio a proteger).
            foreach (var m in AbandonoPorPlan(ab, planes))
            {
                candidatas.Add(new BE.CandidataSugerencia
                {
                    Origen = BE.OrigenMetrica.Abandono,
                    IdPlan = m.IdPlan,
                    NombrePlan = m.NombrePlan,
                    TipoSugerido = BE.TipoDescuento.Porcentaje,
                    BeneficioEstimado = m.IngresoMensualEnRiesgo,
                    Motivo = $"Análisis de abandono: {m.ClientesEnRiesgo} cliente(s) del plan {m.NombrePlan} en riesgo de abandono " +
                             $"(${m.IngresoMensualEnRiesgo} de ingreso mensual en riesgo)."
                });
            }

            return candidatas.OrderByDescending(c => c.BeneficioEstimado).ToList();
        }
    }
}
