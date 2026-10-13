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
    ///   Evaluar resultado de la promoción (sección del reporte) .. ResultadoPromociones
    ///
    /// Cruza los reportes del negocio con la sugerencia de promociones:
    ///   · Abandono por plan: clientes en riesgo de irse → promoción de retención por plan.
    ///   · Rotación por categoría: varias prendas sin pedidos → queda en el reporte como oportunidad
    ///     informativa; no sirve de base para una sugerencia (las promociones son por plan).
    /// Es de solo lectura: el reporte se calcula sobre los datos guardados y se puede imprimir;
    /// Gerencia decide qué oportunidad convertir en sugerencia (SugerenciaPromocion.RegistrarSugerencia).
    /// </summary>
    public class AnalisisPromociones
    {
        public const int MinimoPrendasPorCategoria = 2;

        /// <summary>Período por defecto de "Analizar métricas" (días hacia atrás desde hoy).</summary>
        public const int DiasPeriodoPorDefecto = 90;

        // Baja demanda: sin pedidos desde el alta (reporte PdN9) o en el período analizado (PN03).
        private static bool EsBajaDemanda(BE.RotacionPrenda r) =>
            r.Clave != null && r.Clave.StartsWith("rotacion.motivo.bajademanda", StringComparison.Ordinal);

        private readonly Interfaces.IAnalisisRotacionService rotacion;
        private readonly Interfaces.IAnalisisAbandonoService abandono;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;
        private readonly DAL.Interfaces.IPromocionDAL dalPromocion;   // null: sin impacto ni resultado de promociones
        private readonly DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia;   // null: sin tasa esperada
        private readonly Func<DateTime> hoy;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();

        public AnalisisPromociones()
            : this(new AnalisisRotacion(), new AnalisisAbandono(), new DAL.PlanSuscripcion(), new DAL.Promocion(),
                   new DAL.SugerenciaPromocion()) { }

        public AnalisisPromociones(Interfaces.IAnalisisRotacionService rotacion,
                                   Interfaces.IAnalisisAbandonoService abandono,
                                   DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                                   DAL.Interfaces.IPromocionDAL dalPromocion = null,
                                   DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia = null,
                                   Func<DateTime> hoy = null)
        {
            this.dalPromocion = dalPromocion;
            this.dalSugerencia = dalSugerencia;
            this.hoy = hoy ?? (() => DateTime.Today);
            this.rotacion = rotacion ?? throw new ArgumentNullException(nameof(rotacion));
            this.abandono = abandono ?? throw new ArgumentNullException(nameof(abandono));
            this.dalPlan  = dalPlan  ?? throw new ArgumentNullException(nameof(dalPlan));
        }

        // "Analizar métricas": abandono por plan, rotación por categoría e impacto de las promociones
        // en el período [desde, hasta] (por defecto, los últimos 90 días) → «Reporte de métricas» con
        // las oportunidades detectadas. Si no hay ninguna, el flujo termina "Sin promoción".
        public BE.ReporteMetricas AnalizarMetricas(string modulo, DateTime? desde = null, DateTime? hasta = null)
        {
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);
            DateTime fin = (hasta ?? DateTime.Today).Date;
            DateTime inicio = (desde ?? fin.AddDays(-DiasPeriodoPorDefecto)).Date;
            if (inicio > fin)
                throw new BE.AppException("err.bll.promocion.rango_fechas_invalido",
                    "La fecha de fin no puede ser anterior a la fecha de inicio.");

            var rot = rotacion.Detectar(inicio);
            var ab = abandono.Detectar();
            var planes = dalPlan.ObtenerTodos();

            var resultados = ResultadoPromociones(inicio, fin);
            var reporte = new BE.ReporteMetricas
            {
                ResultadoNoDisponible = resultados == null,
                Fecha = DateTime.Now,
                Desde = inicio,
                Hasta = fin,
                ImpactoPromociones = ImpactoPromociones(inicio, fin),
                ResultadoPromociones = resultados ?? new List<BE.ResultadoPromocion>(),
                AbandonoPorPlan = AbandonoPorPlan(ab, planes),
                RotacionPorCategoria = RotacionPorCategoria(rot),
                Oportunidades = Oportunidades(ab, planes)
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

        // Best-effort: sin DAL de promociones o si la consulta falla, el reporte sale sin esta sección.
        private List<BE.MetricaImpactoPromocion> ImpactoPromociones(DateTime desde, DateTime hasta)
        {
            if (dalPromocion == null) return new List<BE.MetricaImpactoPromocion>();
            try { return dalPromocion.ObtenerImpacto(desde, hasta) ?? new List<BE.MetricaImpactoPromocion>(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[BLL.AnalisisPromociones] Impacto de promociones: {ex.Message}");
                return new List<BE.MetricaImpactoPromocion>();
            }
        }

        // "Evaluar resultado de la promoción": por cada promoción Vigente, Dada de baja o Vencida cuya
        // vigencia toca el período, compara las bajas, pausas y renovaciones de su plan DURANTE la
        // vigencia contra un período ANTERIOR de la misma duración, y mide cuántos clientes con la
        // renovación en juego durante la vigencia se retuvieron (contra la tasa que supuso Gerencia).
        // Devuelve: lista vacía si no hay DAL de promociones; NULL si no se pudieron leer las
        // promociones (AnalizarMetricas lo informa como ReporteMetricas.ResultadoNoDisponible); y, si
        // falla una sola promoción, su fila con NoSePudoCalcular = true.
        public List<BE.ResultadoPromocion> ResultadoPromociones(DateTime desde, DateTime hasta)
        {
            var lista = new List<BE.ResultadoPromocion>();
            if (dalPromocion == null) return lista;
            List<BE.Promocion> promociones;
            try { promociones = dalPromocion.ObtenerTodas(); }
            catch (Exception ex)
            {
                // Falla total: null, para que el reporte diga que no se pudo leer (no "no hay promociones").
                System.Diagnostics.Trace.TraceError($"[BLL.AnalisisPromociones] Resultado de promociones: {ex.Message}");
                return null;
            }
            DateTime hastaExcl = hasta.Date.AddDays(1);
            // Las fechas planificadas ya descartan, sin consultar la BD, las que no tocan el período (la
            // vigencia real siempre cae dentro de [FechaInicio, FechaFin]).
            foreach (var p in promociones.Where(p => EsEvaluable(p) && p.FechaInicio.Date < hastaExcl && p.FechaFin.Date >= desde.Date))
            {
                // Una promoción que falla no se lleva puestas a las demás: queda marcada en el reporte.
                try
                {
                    var (inicio, fin) = VigenciaReal(p);
                    if (fin <= inicio || inicio >= hastaExcl || fin <= desde.Date) continue;

                    var r = new BE.ResultadoPromocion
                    {
                        IdPromocion = p.IdPromocion, Nombre = p.Nombre, Estado = p.Estado,
                        IdPlan = p.IdPlan, NombrePlan = p.NombrePlan, Desde = inicio, Hasta = fin,
                        TasaRetencionEsperada = TasaEsperada(p)
                    };
                    var durante = dalPromocion.ObtenerDecisionesDelPlan(p.IdPlan, r.Desde, r.Hasta);
                    var antes   = dalPromocion.ObtenerDecisionesDelPlan(p.IdPlan, r.AnteriorDesde, r.Desde);
                    r.Durante = BE.ResultadoPromocion.Contar(durante);
                    r.Antes   = BE.ResultadoPromocion.Contar(antes);
                    r.CalcularRetencion(durante, antes);
                    lista.Add(r);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"[BLL.AnalisisPromociones] Resultado de la promoción #{p.IdPromocion}: {ex.Message}");
                    lista.Add(new BE.ResultadoPromocion
                    {
                        IdPromocion = p.IdPromocion, Nombre = p.Nombre, Estado = p.Estado,
                        IdPlan = p.IdPlan, NombrePlan = p.NombrePlan, NoSePudoCalcular = true
                    });
                }
            }
            return lista;
        }

        private static bool EsEvaluable(BE.Promocion p) =>
            p.Estado == BE.EstadoPromocion.Vigente || p.Estado == BE.EstadoPromocion.DadaDeBaja
            || p.Estado == BE.EstadoPromocion.Vencida;

        // Vigencia real [inicio, fin): empieza al aprobarla Contabilidad (no antes de su FechaInicio) y
        // termina al darla de baja o al vencer (no después de su FechaFin); si sigue Vigente, hoy.
        private (DateTime inicio, DateTime fin) VigenciaReal(BE.Promocion p)
        {
            var historial = dalPromocion.ObtenerHistorial(p.IdPromocion) ?? new List<BE.PromocionHistorial>();
            var aprobada = historial.Where(h => h.EstadoNuevo == BE.EstadoPromocion.Vigente).Select(h => (DateTime?)h.Fecha.Date).Min();
            DateTime inicio = aprobada.HasValue && aprobada.Value > p.FechaInicio.Date ? aprobada.Value : p.FechaInicio.Date;

            DateTime finPlanificado = p.FechaFin.Date.AddDays(1);
            DateTime fin = finPlanificado;
            if (p.Estado == BE.EstadoPromocion.DadaDeBaja)
            {
                var baja = historial.Where(h => h.EstadoNuevo == BE.EstadoPromocion.DadaDeBaja).Select(h => (DateTime?)h.Fecha.Date).Max();
                if (baja.HasValue) fin = baja.Value.AddDays(1);
            }
            DateTime manana = hoy().Date.AddDays(1);
            if (fin > manana) fin = manana;
            if (fin > finPlanificado) fin = finPlanificado;
            return (inicio, fin);
        }

        private decimal? TasaEsperada(BE.Promocion p)
        {
            if (dalSugerencia == null || !p.IdSugerenciaOrigen.HasValue) return null;
            return dalSugerencia.ObtenerPorId(p.IdSugerenciaOrigen.Value)?.TasaRetencion;
        }

        // "¿Hay oportunidad?": el reporte detectó al menos un caso para sugerir.
        public bool HayOportunidad(BE.ReporteMetricas reporte) => reporte != null && reporte.HayOportunidad();

        // Oportunidades detectadas (sin exigir permisos: lo usa AnalizarMetricas y las pruebas).
        public List<BE.CandidataSugerencia> Detectar()
            => Oportunidades(abandono.Detectar(), dalPlan.ObtenerTodos());

        // Rotación por categoría del reporte. Una categoría con varias prendas de baja demanda se
        // marca como oportunidad informativa (con ejemplos), pero no genera una CandidataSugerencia.
        public List<BE.MetricaRotacionCategoria> RotacionPorCategoria()
            => RotacionPorCategoria(rotacion.Detectar());

        private static List<BE.MetricaRotacionCategoria> RotacionPorCategoria(List<BE.RotacionPrenda> rot) =>
            rot.Where(r => !string.IsNullOrWhiteSpace(r.Categoria))
               .GroupBy(r => r.Categoria.Trim(), StringComparer.OrdinalIgnoreCase)
               .Select(g => new BE.MetricaRotacionCategoria
               {
                   Categoria = g.Key,
                   PrendasBajaDemanda = g.Count(EsBajaDemanda),
                   PrendasAltaDemanda = g.Count(r => !EsBajaDemanda(r)),
                   EjemplosBajaDemanda = string.Join(", ", g.Where(EsBajaDemanda).Select(r => r.NombrePrenda).Take(3)),
                   EsOportunidad = g.Count(EsBajaDemanda) >= MinimoPrendasPorCategoria
               })
               .OrderByDescending(m => m.PrendasBajaDemanda).ToList();

        private static List<BE.MetricaAbandonoPlan> AbandonoPorPlan(List<BE.ClienteEnRiesgo> enRiesgo, List<BE.PlanSuscripcion> planes)
        {
            var lista = new List<BE.MetricaAbandonoPlan>();
            // Por IdPlan: dos planes pueden llamarse igual (el nombre no es único en la base).
            foreach (var g in enRiesgo.Where(c => c.IdPlan.HasValue).GroupBy(c => c.IdPlan.Value))
            {
                var plan = planes.Find(p => p.Estado && p.IdPlan == g.Key);
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

        // Oportunidades que Gerencia puede usar de base para una sugerencia: solo las por plan.
        private static List<BE.CandidataSugerencia> Oportunidades(List<BE.ClienteEnRiesgo> ab, List<BE.PlanSuscripcion> planes)
        {
            var candidatas = new List<BE.CandidataSugerencia>();

            // Abandono: clientes en riesgo agrupados por plan. El beneficio a proteger es la parte del
            // ingreso mensual en riesgo que la promoción retendría: ingreso × tasa de retención (por
            // defecto 30 %, Gerencia la ajusta). Antes se tomaba el 100 %.
            foreach (var m in AbandonoPorPlan(ab, planes))
            {
                candidatas.Add(new BE.CandidataSugerencia
                {
                    Origen = BE.OrigenMetrica.Abandono,
                    IdPlan = m.IdPlan,
                    NombrePlan = m.NombrePlan,
                    TipoSugerido = BE.TipoDescuento.Porcentaje,
                    ClientesEnRiesgo = m.ClientesEnRiesgo,
                    IngresoMensualEnRiesgo = m.IngresoMensualEnRiesgo,
                    Motivo = $"Análisis de abandono: {m.ClientesEnRiesgo} cliente(s) del plan {m.NombrePlan} en riesgo de abandono " +
                             $"({m.IngresoMensualEnRiesgo:C2} de ingreso mensual en riesgo).",
                    ClaveMotivo = "promo.cand.abandono",
                    ArgsMotivo = new object[] { m.ClientesEnRiesgo, m.NombrePlan, m.IngresoMensualEnRiesgo }
                });
            }

            return candidatas.OrderByDescending(c => c.BeneficioEstimado).ToList();
        }
    }
}
