using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Inteligencia comercial y promociones» — servicio del Centro de decisiones comerciales:
    ///
    ///   Analizar ......... KPI con variación contra el período anterior, tendencia de 6 meses con
    ///                      proyección de 3 (ProyeccionSerie), riesgo por plan, stock y rotación   → Analizar
    ///   Recomendar/decidir motor de reglas sobre el contexto (MotorRecomendaciones) + la última
    ///                      decisión de cada recomendación (DecisionesComerciales)                  → Recomendar
    ///   Simular .......... "¿Qué pasa si…?" con los umbrales vigentes (SimuladorPromocion)          → Simular
    ///   Aplicar .......... la GUI abre el flujo existente (Sugerir → Administración → Contabilidad)
    ///   Medir resultado .. «Resultado de la promoción» (AnalisisPromociones) en plata y con
    ///                      veredicto (MedicionPromociones); cierra el ciclo de las decisiones          → MedirPromociones / MedirDecisiones
    ///   Metas ............ objetivo mensual por indicador, real, proyección al cierre y semáforo; una
    ///                      meta en riesgo sube la prioridad de lo que la mueve (MetasComerciales)        → SeguirMetas
    ///   Avisos ........... qué hay para decidir hoy, al iniciar sesión (AvisosComerciales)               → ResumirAvisos
    ///
    /// Toda la lógica vive acá y en las clases puras que usa; la GUI solo dibuja. Consultar exige la
    /// patente de Gerencia de PN03 (mnuSugerenciaPromocion), como "Analizar métricas". Robustez: si una
    /// fuente secundaria falla (abandono, mantenimiento, promociones, prendas), el panel sale
    /// igual con un aviso; solo planes, clientes, movimientos y cobros son imprescindibles.
    /// </summary>
    public class InteligenciaComercial
    {
        public const int MesesTendencia = 6;
        public const int MesesProyeccion = 3;
        public const int TopRotacion = 4;
        /// <summary>Ventana con la que se mide la retención histórica de las promociones de cada plan.</summary>
        public const int DiasHistoriaPromociones = 365;
        /// <summary>Suscripciones vencidas hace más de estos días no se consideran en juego.</summary>
        public const int DiasVencimientoConsiderados = 30;

        private readonly DAL.Interfaces.IInteligenciaComercialDAL dal;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;
        private readonly Interfaces.IAnalisisAbandonoService abandono;
        private readonly Interfaces.IAnalisisMantenimientoService mantenimiento;
        private readonly DAL.Interfaces.IPromocionDAL dalPromocion;
        private readonly DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia;
        private readonly UmbralesComerciales umbrales;
        private readonly DecisionesComerciales decisiones;
        private readonly MotorRecomendaciones motor;
        private readonly AnalisisPromociones analisisPromociones;
        private readonly MetasComerciales metas;
        private readonly Func<DateTime> hoy;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();

        public InteligenciaComercial()
            : this(new DAL.InteligenciaComercial(), new DAL.PlanSuscripcion(), new AnalisisAbandono(), new AnalisisMantenimiento(),
                   new DAL.Promocion(), new DAL.SugerenciaPromocion(), new DAL.DecisionComercial(),
                   dalMetas: new DAL.MetaComercial()) { }

        public InteligenciaComercial(DAL.Interfaces.IInteligenciaComercialDAL dal, DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                                     Interfaces.IAnalisisAbandonoService abandono, Interfaces.IAnalisisMantenimientoService mantenimiento,
                                     DAL.Interfaces.IPromocionDAL dalPromocion, DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia,
                                     DAL.Interfaces.IDecisionComercialDAL dalDecision, Func<DateTime> hoy = null,
                                     MotorRecomendaciones motor = null, DAL.Interfaces.IMetaComercialDAL dalMetas = null)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
            this.dalPlan = dalPlan ?? throw new ArgumentNullException(nameof(dalPlan));
            this.abandono = abandono;
            this.mantenimiento = mantenimiento;
            this.dalPromocion = dalPromocion;
            this.dalSugerencia = dalSugerencia;
            this.hoy = hoy ?? (() => DateTime.Today);
            umbrales = new UmbralesComerciales(dal);
            decisiones = dalDecision != null ? new DecisionesComerciales(dalDecision) : null;
            this.motor = motor ?? new MotorRecomendaciones();
            metas = dalMetas != null ? new MetasComerciales(dalMetas, () => this.hoy().Date.Add(DateTime.Now.TimeOfDay)) : null;
            // AnalisisPromociones reutiliza el «Resultado de la promoción» existente; acá no se usa su
            // rotación ni su abandono (los pide el constructor), por eso van vacíos.
            analisisPromociones = dalPromocion != null
                ? new AnalisisPromociones(new SinRotacion(), new SinAbandono(), dalPlan, dalPromocion, dalSugerencia, this.hoy)
                : null;
        }

        public UmbralesComerciales Umbrales => umbrales;
        public DecisionesComerciales Decisiones => decisiones;
        /// <summary>Metas comerciales mensuales (null si el servicio se armó sin su DAL).</summary>
        public MetasComerciales Metas => metas;

        /// <summary>
        /// ¿El usuario de la sesión tiene la patente de edición? Decide qué opciones de las recomendaciones ve
        /// (AccionesRecomendacion.FiltrarPorPermiso). Por defecto, la sesión real; las pruebas lo reemplazan.
        /// </summary>
        public Func<string, bool> TienePermiso { get; set; } = PermisosAccion.Puede;

        private static void Exigir() =>
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

        // ══════════════════════════════════════════════════════════════════════
        // Analizar
        // ══════════════════════════════════════════════════════════════════════

        public BE.PanelComercial Analizar(string modulo, DateTime desde, DateTime hasta, int? idPlan = null, bool registrarEnBitacora = true)
        {
            Exigir();
            DateTime inicio = desde.Date, fin = hasta.Date;
            if (inicio > fin)
                throw new BE.AppException("err.bll.promocion.rango_fechas_invalido",
                    "La fecha de fin no puede ser anterior a la fecha de inicio.");

            int dias = (int)(fin - inicio).TotalDays + 1;
            DateTime finExcl = fin.AddDays(1);
            DateTime antDesde = inicio.AddDays(-dias);
            DateTime mesFin = new DateTime(fin.Year, fin.Month, 1);
            DateTime mesInicio = mesFin.AddMonths(-(MesesTendencia - 1));
            DateTime lecturaDesde = antDesde < mesInicio ? antDesde : mesInicio;
            DateTime hoyDia = hoy().Date;
            // Altas y bajas se leen hasta HOY (aunque el período termine antes): con las posteriores al
            // período se reconstruye el estado al fin del período (clientes y MRR).
            DateTime lecturaHasta = hoyDia.AddDays(1) > finExcl ? hoyDia.AddDays(1) : finExcl;

            var panel = new BE.PanelComercial
            {
                FechaGeneracion = DateTime.Now, Desde = inicio, Hasta = fin,
                AnteriorDesde = antDesde, AnteriorHasta = inicio.AddDays(-1), IdPlan = idPlan
            };
            var valoresUmbral = umbrales.ObtenerValores();

            // ── Fuentes imprescindibles ────────────────────────────────────────
            var planes = dalPlan.ObtenerTodos() ?? new List<BE.PlanSuscripcion>();
            var clientes = dal.ObtenerClientes() ?? new List<BE.ClienteComercial>();
            var movimientos = dal.ObtenerMovimientos(lecturaDesde, lecturaHasta) ?? new List<BE.MovimientoSuscripcion>();
            var altas = dal.ObtenerAltas(lecturaDesde, lecturaHasta) ?? new List<BE.AltaSuscripcion>();
            var cobros = dal.ObtenerCobros(lecturaDesde, finExcl) ?? new List<BE.CobroComercial>();
            panel.NombrePlan = idPlan.HasValue ? planes.FirstOrDefault(p => p.IdPlan == idPlan)?.Nombre : null;

            bool EnPlan(int? id) => !idPlan.HasValue || id == idPlan;
            bool EnPeriodo(DateTime f) => f >= inicio && f < finExcl;
            bool EnAnterior(DateTime f) => f >= antDesde && f < inicio;
            bool Posterior(DateTime f) => f >= finExcl;   // después del período, hasta hoy
            var precio = planes.GroupBy(p => p.IdPlan).ToDictionary(g => g.Key, g => g.First().Precio);
            decimal Precio(int? id) => id.HasValue && precio.TryGetValue(id.Value, out var v) ? v : 0m;

            var suscriptores = clientes.Where(c => c.IdPlan.HasValue).ToList();
            var bajas = movimientos.Where(m => m.Resultado == BE.EstadoRenovacion.Baja).ToList();

            // ── Fuentes secundarias (si fallan, aviso y sigue) ─────────────────
            var enRiesgo = Intentar(() => abandono?.Detectar(), panel, "aviso.riesgo",
                "No se pudo calcular el análisis de abandono: los clientes en riesgo figuran en 0.") ?? new List<BE.ClienteEnRiesgo>();
            var prendas = Intentar(() => dal.ObtenerPrendas(inicio, finExcl), panel, "aviso.prendas",
                "No se pudo leer el stock: los gráficos de stock y rotación quedan vacíos.") ?? new List<BE.PrendaComercial>();
            var excesivas = Intentar(() => mantenimiento?.Detectar(), panel, "aviso.mantenimiento",
                "No se pudo leer el historial de mantenimiento.") ?? new List<BE.TiempoMantenimientoPrenda>();
            DateTime historiaDesde = hoyDia.AddDays(-DiasHistoriaPromociones) < inicio ? hoyDia.AddDays(-DiasHistoriaPromociones) : inicio;
            var mediciones = Intentar(() => MedirInterno(historiaDesde, fin, valoresUmbral, planes), panel, "aviso.promociones",
                "No se pudo medir el resultado de las promociones.") ?? new List<BE.MedicionPromocion>();
            var vigentes = Intentar(() => dalPromocion?.ObtenerTodas()?.Where(p => p.EstaVigente()).ToList(), panel, "aviso.promociones",
                "No se pudo medir el resultado de las promociones.") ?? new List<BE.Promocion>();

            // ── Métricas por plan ──────────────────────────────────────────────
            var metricas = new List<BE.MetricaPlanComercial>();
            foreach (var plan in planes.Where(p => p.Estado || suscriptores.Any(c => c.IdPlan == p.IdPlan)))
            {
                int activos = suscriptores.Count(c => c.IdPlan == plan.IdPlan);
                int altasP = altas.Where(a => a.IdPlan == plan.IdPlan && EnPeriodo(a.Fecha)).Select(a => a.IdCliente).Distinct().Count();
                int bajasP = bajas.Where(b => b.IdPlanAnterior == plan.IdPlan && EnPeriodo(b.Fecha)).Select(b => b.IdCliente).Distinct().Count();
                int altasA = altas.Where(a => a.IdPlan == plan.IdPlan && EnAnterior(a.Fecha)).Select(a => a.IdCliente).Distinct().Count();
                int bajasA = bajas.Where(b => b.IdPlanAnterior == plan.IdPlan && EnAnterior(b.Fecha)).Select(b => b.IdCliente).Distinct().Count();
                int altasPost = altas.Where(a => a.IdPlan == plan.IdPlan && Posterior(a.Fecha)).Select(a => a.IdCliente).Distinct().Count();
                int bajasPost = bajas.Where(b => b.IdPlanAnterior == plan.IdPlan && Posterior(b.Fecha)).Select(b => b.IdCliente).Distinct().Count();
                // Fin del período reconstruido desde hoy (si el período termina hoy, fin = activos).
                int finPeriodo = CalculoKpi.ClientesAlFinal(activos, altasPost, bajasPost);
                int ini = CalculoKpi.ClientesAlInicio(finPeriodo, altasP, bajasP);
                int iniA = CalculoKpi.ClientesAlInicio(ini, altasA, bajasA);
                var medidas = mediciones.Where(m => m.Resultado.IdPlan == plan.IdPlan && m.Resultado.MejoraRetencion.HasValue
                                                    && m.Resultado.MejoraRetencion.Value > 0).ToList();
                metricas.Add(new BE.MetricaPlanComercial
                {
                    IdPlan = plan.IdPlan, Nombre = plan.Nombre, Precio = plan.Precio, LimitePrendas = plan.LimitePrendas,
                    ClientesActivos = activos, ClientesFinPeriodo = finPeriodo, ClientesInicio = ini, Bajas = bajasP,
                    ClientesInicioAnterior = iniA, BajasAnterior = bajasA,
                    Churn = CalculoKpi.TasaBajas(bajasP, ini), ChurnAnterior = CalculoKpi.TasaBajas(bajasA, iniA),
                    ClientesEnRiesgo = enRiesgo.Count(c => c.IdPlan == plan.IdPlan),
                    RetencionHistorica = medidas.Count > 0 ? Math.Round(medidas.Average(m => m.Resultado.MejoraRetencion.Value), 2) : (decimal?)null,
                    PromocionesMedidas = medidas.Count,
                    TienePromocionVigente = vigentes.Any(p => p.IdPlan == plan.IdPlan)
                });
            }
            var metricasFiltradas = metricas.Where(m => EnPlan(m.IdPlan)).ToList();

            // ── KPI ────────────────────────────────────────────────────────────
            var cobrosP = cobros.Where(c => EnPlan(c.IdPlan) && EnPeriodo(c.Fecha)).ToList();
            var cobrosA = cobros.Where(c => EnPlan(c.IdPlan) && EnAnterior(c.Fecha)).ToList();
            // MRR = lo que pagan HOY los clientes con plan que no están pausados, vencidos ni suspendidos
            // (ClienteComercial.PagaAl). Al fin del período se reconstruye restando las altas y sumando
            // las bajas posteriores; al inicio, restando las altas y sumando las bajas del período.
            decimal mrrHoy = suscriptores.Where(c => EnPlan(c.IdPlan) && c.PagaAl(hoyDia)).Sum(c => Precio(c.IdPlan));
            decimal mrrAltasPost = altas.Where(a => EnPlan(a.IdPlan) && Posterior(a.Fecha)).Sum(a => Precio(a.IdPlan));
            decimal mrrBajasPost = bajas.Where(b => EnPlan(b.IdPlanAnterior) && Posterior(b.Fecha)).Sum(b => Precio(b.IdPlanAnterior));
            decimal mrrFin = CalculoKpi.MrrAlFinal(mrrHoy, mrrAltasPost, mrrBajasPost);
            decimal mrrAltas = altas.Where(a => EnPlan(a.IdPlan) && EnPeriodo(a.Fecha)).Sum(a => Precio(a.IdPlan));
            decimal mrrBajas = bajas.Where(b => EnPlan(b.IdPlanAnterior) && EnPeriodo(b.Fecha)).Sum(b => Precio(b.IdPlanAnterior));
            panel.Kpis.Add(CalculoKpi.Crear("mrr", Nombre("mrr"), BE.UnidadKpi.Moneda, mrrFin,
                CalculoKpi.MrrAlInicio(mrrFin, mrrAltas, mrrBajas), true));

            decimal? churn = CalculoKpi.TasaBajas(metricasFiltradas.Sum(m => m.Bajas), metricasFiltradas.Sum(m => m.ClientesInicio));
            decimal? churnA = CalculoKpi.TasaBajas(metricasFiltradas.Sum(m => m.BajasAnterior), metricasFiltradas.Sum(m => m.ClientesInicioAnterior));
            panel.Kpis.Add(CalculoKpi.Crear("churn", Nombre("churn"), BE.UnidadKpi.Porcentaje, churn ?? 0m, churnA, false));

            var riesgoFiltrado = enRiesgo.Where(c => EnPlan(c.IdPlan)).ToList();
            int activosFiltrados = suscriptores.Count(c => EnPlan(c.IdPlan));
            var kRiesgo = CalculoKpi.Crear("riesgo", Nombre("riesgo"), BE.UnidadKpi.Cantidad, riesgoFiltrado.Count, null, false);
            kRiesgo.Interpretacion = new BE.TextoTraducible("kpi.interp.riesgo",
                "Son el {0:0.#} % de los clientes activos (foto de hoy).", CalculoKpi.Porcentaje(riesgoFiltrado.Count, activosFiltrados));
            panel.Kpis.Add(kRiesgo);

            decimal ingresoRiesgo = riesgoFiltrado.Sum(c => Precio(c.IdPlan));
            var kIngresoRiesgo = CalculoKpi.Crear("ingresoriesgo", Nombre("ingresoriesgo"), BE.UnidadKpi.Moneda, ingresoRiesgo, null, false);
            kIngresoRiesgo.Interpretacion = new BE.TextoTraducible("kpi.interp.ingresoriesgo",
                "Es el {0:0.#} % del ingreso recurrente mensual (foto de hoy).", CalculoKpi.Porcentaje(ingresoRiesgo, mrrHoy));
            panel.Kpis.Add(kIngresoRiesgo);

            panel.Kpis.Add(CalculoKpi.Crear("descontado", Nombre("descontado"), BE.UnidadKpi.Moneda,
                cobrosP.Sum(c => c.Descuento), cobrosA.Sum(c => c.Descuento), false));

            var activas = prendas.Where(p => p.Estado != BE.EstadoPrenda.Baja).ToList();
            int enUso = activas.Count(p => p.Estado == BE.EstadoPrenda.EnUso);
            decimal ocupacion = CalculoKpi.Porcentaje(enUso, activas.Count);
            var kOcupacion = CalculoKpi.Crear("ocupacion", Nombre("ocupacion"), BE.UnidadKpi.Porcentaje, ocupacion, null, true);
            kOcupacion.Interpretacion = new BE.TextoTraducible("kpi.interp.ocupacion",
                "{0} de {1} prendas activas están en uso (foto de hoy).", enUso, activas.Count);
            panel.Kpis.Add(kOcupacion);

            decimal ticket = cobrosP.Count > 0 ? Math.Round(cobrosP.Sum(c => c.Importe) / cobrosP.Count, 2) : 0m;
            decimal? ticketA = cobrosA.Count > 0 ? Math.Round(cobrosA.Sum(c => c.Importe) / cobrosA.Count, 2) : (decimal?)null;
            panel.Kpis.Add(CalculoKpi.Crear("ticket", Nombre("ticket"), BE.UnidadKpi.Moneda, ticket, ticketA, true));
            panel.Kpis.Add(CalculoKpi.Crear("ingresos", Nombre("ingresos"), BE.UnidadKpi.Moneda,
                cobrosP.Sum(c => c.Importe), cobrosA.Sum(c => c.Importe), true));

            // ── Tendencia mensual + proyección ─────────────────────────────────
            // Cada mes cuenta hasta el fin del período (no hasta hoy). Si el período termina antes de fin
            // de mes (p. ej. el mes en curso), el último mes es PARCIAL: se muestra, pero no entra en la
            // regresión (contarlo como un mes completo tiraría la proyección para abajo).
            for (int i = 0; i < MesesTendencia; i++)
            {
                DateTime m = mesInicio.AddMonths(i), mSig = m.AddMonths(1);
                DateTime corte = mSig < finExcl ? mSig : finExcl;
                panel.Tendencia.Add(new BE.PuntoMensual
                {
                    Mes = m,
                    Altas = altas.Count(a => EnPlan(a.IdPlan) && a.Fecha >= m && a.Fecha < corte),
                    Bajas = bajas.Count(b => EnPlan(b.IdPlanAnterior) && b.Fecha >= m && b.Fecha < corte),
                    Ingresos = cobros.Where(c => EnPlan(c.IdPlan) && c.Fecha >= m && c.Fecha < corte).Sum(c => c.Importe),
                    EsParcial = corte < mSig
                });
            }
            panel.R2Ingresos = AgregarProyeccion(panel.Tendencia, mesFin);

            // ── Gráficos ───────────────────────────────────────────────────────
            panel.RiesgoPorPlan = metricasFiltradas.Where(m => m.ClientesEnRiesgo > 0)
                .Select(m => new BE.MetricaAbandonoPlan
                {
                    IdPlan = m.IdPlan, NombrePlan = m.Nombre, ClientesEnRiesgo = m.ClientesEnRiesgo, IngresoMensualEnRiesgo = m.IngresoEnRiesgo
                })
                .OrderByDescending(m => m.IngresoMensualEnRiesgo).ToList();
            panel.StockPorCategoria = activas
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Categoria) ? "—" : p.Categoria.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new BE.StockCategoria
                {
                    Categoria = g.Key,
                    Disponibles = g.Count(p => p.Estado == BE.EstadoPrenda.Disponible),
                    EnUso = g.Count(p => p.Estado == BE.EstadoPrenda.EnUso),
                    EnLimpieza = g.Count(p => p.Estado == BE.EstadoPrenda.EnLimpieza)
                })
                .OrderByDescending(s => s.Total).ToList();
            BE.RotacionPrenda Rot(BE.PrendaComercial p) => new BE.RotacionPrenda
            {
                IdPrenda = p.IdPrenda, NombrePrenda = p.Nombre, Categoria = p.Categoria, CantidadPedidos = p.PedidosPeriodo
            };
            panel.MasPedidas = activas.Where(p => p.PedidosPeriodo > 0).OrderByDescending(p => p.PedidosPeriodo).ThenBy(p => p.Nombre)
                .Take(TopRotacion).Select(Rot).ToList();
            panel.MenosPedidas = activas.OrderBy(p => p.PedidosPeriodo).ThenBy(p => p.FechaAlta)
                .Where(p => !panel.MasPedidas.Any(m => m.IdPrenda == p.IdPrenda)).Take(TopRotacion).Select(Rot).ToList();

            // ── Contexto para el motor ─────────────────────────────────────────
            var nombrePlan = planes.GroupBy(p => p.IdPlan).ToDictionary(g => g.Key, g => g.First().Nombre);
            panel.Contexto = new BE.ContextoComercial
            {
                Hoy = hoyDia, Desde = inicio, Hasta = fin,
                Umbrales = valoresUmbral,
                Planes = metricasFiltradas,
                Clientes = suscriptores.Where(c => EnPlan(c.IdPlan)).ToList(),
                Vencimientos = suscriptores
                    .Where(c => EnPlan(c.IdPlan) && c.FechaVencimiento.HasValue
                                && !(c.FechaPausaHasta.HasValue && c.FechaPausaHasta.Value.Date >= hoyDia)
                                && Math.Abs((c.FechaVencimiento.Value.Date - hoyDia).TotalDays) <= DiasVencimientoConsiderados)
                    .Select(c => new BE.ClienteVencimiento
                    {
                        IdCliente = c.IdCliente, Nombre = c.NombreCompleto, Email = c.Email, IdPlan = c.IdPlan,
                        NombrePlan = c.IdPlan.HasValue && nombrePlan.TryGetValue(c.IdPlan.Value, out var n) ? n : null,
                        Precio = Precio(c.IdPlan), FechaVencimiento = c.FechaVencimiento.Value,
                        DiasRestantes = (int)(c.FechaVencimiento.Value.Date - hoyDia).TotalDays
                    }).ToList(),
                Prendas = prendas,
                Mantenimientos = excesivas,
                Promociones = mediciones.Where(m => EnPlan(m.Resultado.IdPlan)).ToList(),
                // Sin cobros en el período, el ticket se estima con el precio promedio de los planes activos.
                TicketPromedio = ticket > 0 ? ticket
                    : (planes.Any(p => p.Estado && p.Precio > 0) ? Math.Round(planes.Where(p => p.Estado && p.Precio > 0).Average(p => p.Precio), 2) : 0m),
                OcupacionPorcentaje = ocupacion
            };

            if (registrarEnBitacora)
            try
            {
                bitacora.Registrar(modulo, $"Centro de decisiones comerciales: análisis del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}" +
                    (idPlan.HasValue ? $" (plan ID {idPlan})" : "") + $" — MRR {mrrFin:C2}, bajas {churn ?? 0m:0.##} %",
                    BE.Criticidad.Baja);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.InteligenciaComercial] " + ex.Message); }
            return panel;
        }

        /// <summary>
        /// Agrega a la tendencia los <see cref="MesesProyeccion"/> meses proyectados después de
        /// <paramref name="mesFin"/> y devuelve el R² de los ingresos. La regresión usa solo los meses
        /// completos: un último mes parcial (EsParcial) se excluye, pero conserva su lugar en el eje x,
        /// así que la proyección arranca un mes después de él.
        /// </summary>
        public static decimal? AgregarProyeccion(List<BE.PuntoMensual> tendencia, DateTime mesFin)
        {
            var reales = tendencia.Where(p => !p.EsProyeccion).ToList();
            var completos = reales.Where(p => !p.EsParcial).ToList();
            int saltear = reales.Count - completos.Count;
            var pAltas = ProyeccionSerie.Proyectar(completos.Select(p => p.Altas).ToList(), MesesProyeccion, saltear);
            var pBajas = ProyeccionSerie.Proyectar(completos.Select(p => p.Bajas).ToList(), MesesProyeccion, saltear);
            var pIngresos = ProyeccionSerie.Proyectar(completos.Select(p => p.Ingresos).ToList(), MesesProyeccion, saltear);
            for (int i = 0; i < MesesProyeccion; i++)
                tendencia.Add(new BE.PuntoMensual
                {
                    Mes = mesFin.AddMonths(i + 1), Altas = pAltas[i], Bajas = pBajas[i], Ingresos = pIngresos[i], EsProyeccion = true
                });
            return ProyeccionSerie.RegresionLineal(completos.Select(p => p.Ingresos).ToList()).R2;
        }

        private static BE.TextoTraducible Nombre(string clave)
        {
            switch (clave)
            {
                case "mrr": return new BE.TextoTraducible("kpi.mrr", "Ingreso recurrente mensual (MRR)");
                case "churn": return new BE.TextoTraducible("kpi.churn", "% de bajas");
                case "riesgo": return new BE.TextoTraducible("kpi.riesgo", "Clientes en riesgo");
                case "ingresoriesgo": return new BE.TextoTraducible("kpi.ingresoriesgo", "Ingreso en riesgo");
                case "descontado": return new BE.TextoTraducible("kpi.descontado", "Descontado por promociones");
                case "ocupacion": return new BE.TextoTraducible("kpi.ocupacion", "Ocupación del stock");
                case "ticket": return new BE.TextoTraducible("kpi.ticket", "Ticket promedio");
                default: return new BE.TextoTraducible("kpi.ingresos", "Ingresos cobrados");
            }
        }

        // Ejecuta una lectura secundaria: si falla, deja un aviso en el panel (una sola vez por clave) y devuelve null.
        private static T Intentar<T>(Func<T> lectura, BE.PanelComercial panel, string clave, string fallback) where T : class
        {
            try { return lectura(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[BLL.InteligenciaComercial] {clave}: {ex.Message}");
                if (!panel.Avisos.Any(a => a.Clave == clave))
                    panel.Avisos.Add(new BE.TextoTraducible(clave, fallback));
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Recomendar / decidir
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Corre el motor sobre el contexto del panel y asocia la última decisión de cada recomendación.</summary>
        public ResultadoMotor Recomendar(BE.PanelComercial panel)
        {
            Exigir();
            if (panel?.Contexto == null)
                throw new BE.AppException("err.bll.inteligencia.sinanalisis", "Primero analice un período.");
            var resultado = motor.Evaluar(panel.Contexto);
            decisiones?.AsociarDecisiones(resultado.Recomendaciones);
            // Solo las opciones que este usuario puede ejecutar; sin ninguna, la recomendación no se le muestra
            // (ni cuenta en sus avisos ni recibe la prioridad de las metas, que se aplica después).
            resultado.Recomendaciones = AccionesRecomendacion.FiltrarPorPermiso(resultado.Recomendaciones, TienePermiso);
            // Movimiento: una meta del mes en curso en riesgo sube la prioridad de las recomendaciones que la
            // mueven. Si las metas no se pueden leer (base sin migrar), las recomendaciones salen igual.
            try
            {
                resultado.Metas = SeguirMetas(hoy());
                resultado.Recomendaciones = MetasComerciales.ElevarPrioridad(resultado.Recomendaciones, resultado.Metas);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.InteligenciaComercial] Metas: " + ex.Message); }
            return resultado;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Metas comerciales
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Seguimiento de las metas del mes (real, avance, proyección al cierre, semáforo y la serie real
        /// contra meta de los últimos meses). Sin metas en ese mes no lee nada más.
        /// </summary>
        public List<BE.SeguimientoMeta> SeguirMetas(DateTime mes)
        {
            Exigir();
            var resultado = new List<BE.SeguimientoMeta>();
            if (metas == null) return resultado;
            DateTime periodo = BE.MetaComercial.InicioDeMes(mes);
            DateTime desde = periodo.AddMonths(-(MetasComerciales.MesesHistoria - 1));
            var lista = metas.Obtener(desde, periodo);
            var delMes = lista.Where(m => BE.MetaComercial.InicioDeMes(m.Periodo) == periodo).OrderBy(m => m.Indicador).ToList();
            if (delMes.Count == 0) return resultado;
            var datos = LeerDatosMetas(desde);
            foreach (var m in delMes) resultado.Add(MetasComerciales.Seguir(m, lista, datos));
            return resultado;
        }

        private DatosMetas LeerDatosMetas(DateTime desde)
        {
            DateTime hoyDia = hoy().Date, hastaExcl = hoyDia.AddDays(1);
            DateTime inicio = desde.Date < hoyDia ? desde.Date : hoyDia;
            var datos = new DatosMetas
            {
                Hoy = hoyDia,
                Clientes = dal.ObtenerClientes() ?? new List<BE.ClienteComercial>(),
                Movimientos = dal.ObtenerMovimientos(inicio, hastaExcl) ?? new List<BE.MovimientoSuscripcion>(),
                Altas = dal.ObtenerAltas(inicio, hastaExcl) ?? new List<BE.AltaSuscripcion>(),
                Precios = (dalPlan.ObtenerTodos() ?? new List<BE.PlanSuscripcion>()).GroupBy(p => p.IdPlan).ToDictionary(g => g.Key, g => g.First().Precio)
            };
            try
            {
                var activas = (dal.ObtenerPrendas(hoyDia, hastaExcl) ?? new List<BE.PrendaComercial>()).Where(p => p.Estado != BE.EstadoPrenda.Baja).ToList();
                datos.OcupacionHoy = activas.Count > 0 ? CalculoKpi.Porcentaje(activas.Count(p => p.Estado == BE.EstadoPrenda.EnUso), activas.Count) : (decimal?)null;
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.InteligenciaComercial] Ocupación para metas: " + ex.Message); }
            return datos;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Avisos al iniciar sesión
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Qué hay para decidir hoy (avisos al iniciar sesión): analiza los últimos 90 días como la pantalla
        /// (sin dejar el análisis en la bitácora), corre el motor con las metas y cuenta lo urgente.
        /// </summary>
        public BE.ResumenAvisosComerciales ResumirAvisos(string modulo)
        {
            Exigir();
            DateTime hoyDia = hoy().Date;
            var panel = Analizar(modulo, hoyDia.AddDays(-89), hoyDia, null, registrarEnBitacora: false);
            var resultado = Recomendar(panel);
            List<BE.DecisionComercial> historial;
            try { historial = decisiones?.Obtener() ?? new List<BE.DecisionComercial>(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.InteligenciaComercial] Avisos: " + ex.Message);
                historial = new List<BE.DecisionComercial>();
            }
            int silencio = (int)CatalogoUmbrales.Valor(panel.Contexto, CatalogoUmbrales.DiasSilencioDescarte);
            return AvisosComerciales.Resumir(resultado.Recomendaciones, historial, resultado.Metas, hoyDia, silencio, panel.Desde);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Simular
        // ══════════════════════════════════════════════════════════════════════

        public BE.ResultadoSimulacion Simular(BE.EscenarioPromocion escenario)
        {
            Exigir();
            var valores = umbrales.ObtenerValores();
            return SimuladorPromocion.Simular(escenario,
                valores[CatalogoUmbrales.RoiMinimoPct], valores[CatalogoUmbrales.MargenSeguridadPp]);
        }

        /// <summary>¿La opción es una promoción que se puede llevar al simulador ("Simular esta opción")?
        /// Sí, si sugiere o extiende una promoción y trae plan, % de descuento y duración.</summary>
        public static bool EsSimulable(BE.OpcionRecomendacion opcion)
        {
            var p = opcion?.Parametros;
            return p != null && p.IdPlan.HasValue && p.DescuentoPorcentaje > 0 && p.DuracionDias > 0
                   && (opcion.Accion == BE.AccionRecomendacion.SugerirPromocion || opcion.Accion == BE.AccionRecomendacion.ExtenderPromocion);
        }

        /// <summary>
        /// Escenario del simulador precargado con una opción de promoción: parte del escenario base del
        /// plan (clientes del plan, horizonte) y toma de la opción el %, los días, la retención supuesta y
        /// los clientes objetivo. Así el simulador reproduce el impacto que calculó la regla. null si la
        /// opción no es simulable o el plan no está en el análisis.
        /// </summary>
        public static BE.EscenarioPromocion EscenarioDesdeOpcion(BE.PanelComercial panel, BE.OpcionRecomendacion opcion)
        {
            if (!EsSimulable(opcion)) return null;
            var p = opcion.Parametros;
            var escenario = EscenarioBase(panel, p.IdPlan.Value);
            if (escenario == null) return null;
            escenario.DescuentoPorcentaje = p.DescuentoPorcentaje;
            escenario.DuracionDias = p.DuracionDias;
            if (p.TasaRetencion > 0) escenario.TasaRetencion = p.TasaRetencion;
            if (p.ClientesObjetivo > 0) escenario.ClientesObjetivo = p.ClientesObjetivo;
            return escenario;
        }

        /// <summary>Escenario inicial del simulador para un plan, con los datos del último análisis.</summary>
        public static BE.EscenarioPromocion EscenarioBase(BE.PanelComercial panel, int idPlan)
        {
            var ctx = panel?.Contexto;
            var plan = ctx?.Planes.FirstOrDefault(p => p.IdPlan == idPlan);
            if (plan == null) return null;
            return new BE.EscenarioPromocion
            {
                IdPlan = plan.IdPlan, NombrePlan = plan.Nombre, PrecioPlan = plan.Precio,
                ClientesPlan = plan.ClientesActivos,
                ClientesObjetivo = Math.Max(plan.ClientesEnRiesgo, plan.Bajas),
                DescuentoPorcentaje = CatalogoUmbrales.Valor(ctx, CatalogoUmbrales.DescuentoBasePct),
                DuracionDias = 30,
                TasaRetencion = plan.RetencionHistorica ?? CatalogoUmbrales.Valor(ctx, CatalogoUmbrales.TasaRetencionDefecto),
                HorizonteMeses = CatalogoUmbrales.Valor(ctx, CatalogoUmbrales.HorizonteMeses)
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        // Medir resultado
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Mide cada promoción Vigente, Dada de baja o Vencida cuya vigencia toca el período.</summary>
        public List<BE.MedicionPromocion> MedirPromociones(DateTime desde, DateTime hasta)
        {
            Exigir();
            if (desde.Date > hasta.Date)
                throw new BE.AppException("err.bll.promocion.rango_fechas_invalido",
                    "La fecha de fin no puede ser anterior a la fecha de inicio.");
            return MedirInterno(desde.Date, hasta.Date, umbrales.ObtenerValores(), dalPlan.ObtenerTodos() ?? new List<BE.PlanSuscripcion>());
        }

        private List<BE.MedicionPromocion> MedirInterno(DateTime desde, DateTime hasta, Dictionary<string, decimal> valores,
                                                        List<BE.PlanSuscripcion> planes)
        {
            var lista = new List<BE.MedicionPromocion>();
            if (analisisPromociones == null) return lista;
            var resultados = analisisPromociones.ResultadoPromociones(desde, hasta)
                ?? throw new BE.AppException("err.bll.inteligencia.promociones",
                    "No se pudieron leer las promociones: vuelva a intentarlo.");
            if (resultados.Count == 0) return lista;

            var promociones = (dalPromocion.ObtenerTodas() ?? new List<BE.Promocion>()).ToDictionary(p => p.IdPromocion);
            var conDatos = resultados.Where(r => !r.NoSePudoCalcular).ToList();
            var impacto = new Dictionary<int, BE.MetricaImpactoPromocion>();
            if (conDatos.Count > 0)
                foreach (var i in dalPromocion.ObtenerImpacto(conDatos.Min(r => r.Desde), conDatos.Max(r => r.Hasta)) ?? new List<BE.MetricaImpactoPromocion>())
                    impacto[i.IdPromocion] = i;
            var criterio = CriterioMedicion.Desde(valores);

            foreach (var r in resultados)
            {
                promociones.TryGetValue(r.IdPromocion, out var promo);
                BE.SugerenciaPromocion sugerencia = null;
                if (promo?.IdSugerenciaOrigen != null && dalSugerencia != null)
                {
                    try { sugerencia = dalSugerencia.ObtenerPorId(promo.IdSugerenciaOrigen.Value); }
                    catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.InteligenciaComercial] " + ex.Message); }
                }
                decimal precioPlan = planes.FirstOrDefault(p => p.IdPlan == r.IdPlan)?.Precio ?? 0m;
                impacto.TryGetValue(r.IdPromocion, out var imp);
                lista.Add(MedicionPromociones.Evaluar(r, promo, precioPlan, imp, sugerencia, criterio, hoy()));
            }
            return lista;
        }

        /// <summary>
        /// Cierra el ciclo: para cada decisión ejecutada o aceptada que generó una sugerencia o tocó una
        /// promoción, el beneficio real medido de esa promoción (SinDato si todavía no se puede medir); para
        /// cada campaña de contacto ejecutada, su resultado (EnMedicion con el avance mientras la ventana
        /// está abierta, Medido con el importe al cerrar). Las decisiones sin nada que medir no aparecen.
        /// </summary>
        public Dictionary<int, BE.MedicionDecision> MedirDecisiones(IEnumerable<BE.DecisionComercial> lista)
        {
            Exigir();
            var todas = (lista ?? Enumerable.Empty<BE.DecisionComercial>()).ToList();
            var resultado = MedirPorPromocion(todas);
            foreach (var kv in MedirCampanias(todas)) resultado[kv.Key] = kv.Value;
            return resultado;
        }

        /// <summary>
        /// Campañas de contacto ejecutadas (Renovaciones, ChurnPlan, UpgradePlan): qué hicieron los clientes
        /// contactados después de la decisión (MedicionCampanias). Best-effort: si algo falla, sin dato.
        /// </summary>
        private Dictionary<int, BE.MedicionDecision> MedirCampanias(List<BE.DecisionComercial> lista)
        {
            var resultado = new Dictionary<int, BE.MedicionDecision>();
            bool EsCampania(BE.DecisionComercial d) => d != null && d.Estado == BE.EstadoDecision.Ejecutada && !d.Referencia.HasValue;
            var candidatas = lista.Where(EsCampania).ToList();
            if (candidatas.Count == 0 || decisiones == null) return resultado;
            try
            {
                // La atribución se hace sobre TODAS las campañas ejecutadas, no solo las que pasan el filtro
                // del Historial (fechas, estado): el resultado de una decisión no cambia con el filtro.
                var todas = (decisiones.Obtener() ?? new List<BE.DecisionComercial>()).Where(EsCampania).ToList();
                var idsTodas = new HashSet<int>(todas.Select(d => d.IdDecision));
                todas.AddRange(candidatas.Where(d => idsTodas.Add(d.IdDecision)));
                var contactos = decisiones.ObtenerDetalle(todas.Select(d => d.IdDecision), BE.DecisionComercial.DetalleCliente);
                var conContactos = candidatas.Where(d => contactos.ContainsKey(d.IdDecision)).ToList();
                if (conContactos.Count == 0) return resultado;
                // Cada cliente cuenta para una sola campaña: la primera que lo contactó (entre todas).
                var atribuidos = MedicionCampanias.Atribuir(todas, contactos);
                DateTime hoyDia = hoy().Date;
                DateTime desde = conContactos.Min(d => d.FechaVigencia).Date.AddDays(-MedicionCampanias.DiasVentana);
                var movimientos = dal.ObtenerMovimientos(desde, hoyDia.AddDays(1)) ?? new List<BE.MovimientoSuscripcion>();
                var precios = (dalPlan.ObtenerTodos() ?? new List<BE.PlanSuscripcion>()).GroupBy(p => p.IdPlan).ToDictionary(g => g.Key, g => g.First().Precio);
                var planDeCliente = (dal.ObtenerClientes() ?? new List<BE.ClienteComercial>())
                    .GroupBy(c => c.IdCliente).ToDictionary(g => g.Key, g => g.First().IdPlan);
                decimal horizonte = umbrales.ObtenerValores()[CatalogoUmbrales.HorizonteMeses];
                foreach (var d in conContactos)
                {
                    var r = MedicionCampanias.Medir(d, atribuidos[d.IdDecision], movimientos, precios, planDeCliente, horizonte, hoyDia);
                    resultado[d.IdDecision] = r?.ComoMedicion() ?? BE.MedicionDecision.SinDatos();
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.InteligenciaComercial] Medir campañas: " + ex.Message); }
            return resultado;
        }

        private Dictionary<int, BE.MedicionDecision> MedirPorPromocion(List<BE.DecisionComercial> lista)
        {
            var resultado = new Dictionary<int, BE.MedicionDecision>();
            var medibles = lista
                .Where(d => d.AdmiteResultado() && d.Referencia.HasValue
                            && (d.TipoReferencia == BE.DecisionComercial.RefPromocion || d.TipoReferencia == BE.DecisionComercial.RefSugerencia))
                .ToList();
            if (medibles.Count == 0 || dalPromocion == null) return resultado;

            List<BE.Promocion> promociones;
            List<BE.MedicionPromocion> mediciones;
            try
            {
                promociones = dalPromocion.ObtenerTodas() ?? new List<BE.Promocion>();
                DateTime desde = medibles.Min(d => d.Fecha).Date.AddDays(-DiasHistoriaPromociones);
                mediciones = MedirInterno(desde, hoy(), umbrales.ObtenerValores(), dalPlan.ObtenerTodos() ?? new List<BE.PlanSuscripcion>());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.InteligenciaComercial] Medir decisiones: " + ex.Message);
                return resultado;
            }

            foreach (var d in medibles)
            {
                var promo = d.TipoReferencia == BE.DecisionComercial.RefPromocion
                    ? promociones.FirstOrDefault(p => p.IdPromocion == d.Referencia.Value)
                    : promociones.FirstOrDefault(p => p.IdSugerenciaOrigen == d.Referencia.Value);
                var medicion = promo != null ? mediciones.FirstOrDefault(m => m.IdPromocion == promo.IdPromocion) : null;
                resultado[d.IdDecision] = medicion?.BeneficioReal is decimal beneficio
                    ? BE.MedicionDecision.Medida(beneficio) : BE.MedicionDecision.SinDatos();
            }
            return resultado;
        }

        // Dobles internos para reutilizar AnalisisPromociones.ResultadoPromociones sin su reporte completo.
        private sealed class SinRotacion : Interfaces.IAnalisisRotacionService
        {
            public List<BE.RotacionPrenda> Detectar(DateTime? desde = null) => new List<BE.RotacionPrenda>();
        }

        private sealed class SinAbandono : Interfaces.IAnalisisAbandonoService
        {
            public void CambiarEstrategia(Estrategias.EstrategiaRiesgo estrategia) { }
            public List<BE.ClienteEnRiesgo> Detectar() => new List<BE.ClienteEnRiesgo>();
        }
    }
}
