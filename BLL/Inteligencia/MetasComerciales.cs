using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>Datos crudos con los que se miden las metas (los arma InteligenciaComercial.SeguirMetas).</summary>
    public sealed class DatosMetas
    {
        public DateTime Hoy { get; set; }
        public List<BE.ClienteComercial> Clientes { get; set; } = new List<BE.ClienteComercial>();
        /// <summary>Renovaciones resueltas desde el inicio del primer mes que se mide hasta hoy.</summary>
        public List<BE.MovimientoSuscripcion> Movimientos { get; set; } = new List<BE.MovimientoSuscripcion>();
        /// <summary>Altas desde el inicio del primer mes que se mide hasta hoy.</summary>
        public List<BE.AltaSuscripcion> Altas { get; set; } = new List<BE.AltaSuscripcion>();
        /// <summary>Precio mensual de cada plan.</summary>
        public Dictionary<int, decimal> Precios { get; set; } = new Dictionary<int, decimal>();
        /// <summary>Ocupación del stock hoy (foto: prendas en uso / activas × 100); null si no se pudo leer.</summary>
        public decimal? OcupacionHoy { get; set; }
    }

    /// <summary>
    /// PN03 «Metas comerciales» — Gerencia fija un objetivo mensual por indicador (MRR a alcanzar, % de bajas
    /// máximo, ocupación del stock…; ver CatalogoIndicadores) y el sistema lo sigue: valor real, avance,
    /// proyección al cierre del mes y semáforo. Cuando una meta del mes en curso está en riesgo, las
    /// recomendaciones que la mueven suben de prioridad (ElevarPrioridad): el análisis se convierte en acción.
    ///
    /// Reglas (puras, probadas en Tests/MetasComercialesTests):
    ///   · Una meta por indicador y mes (guardar dos veces la misma modifica la existente); el objetivo dentro
    ///     del rango del indicador; solo se crea, modifica o elimina para el mes en curso o los futuros (hasta
    ///     <see cref="MesesFuturosEditables"/> meses): un mes cerrado es historia.
    ///   · Valor real: con las mismas fórmulas que los KPI de "Analizar" (CalculoKpi), medido al cierre del mes
    ///     (o a hoy en el mes en curso). La ocupación es una foto del día: solo se mide en el mes en curso.
    ///   · Avance: mayor es mejor = real / objetivo × 100; menor es mejor = real / máximo permitido × 100
    ///     (debajo de 100 % se cumple).
    ///   · Proyección al cierre (mes en curso, indicadores con historia, desde el día <see cref="DiasMinimosProyeccion"/>):
    ///     valor de hoy + pendiente × días que faltan, con la pendiente de la regresión lineal por mínimos
    ///     cuadrados (ProyeccionSerie, la misma de la tendencia) sobre el valor de cada día transcurrido del mes.
    ///   · Semáforo: mes futuro → Pendiente; sin dato → Sin datos; mes cerrado → Cumplida o No cumplida;
    ///     mes en curso → estimado = proyección (o el valor actual si no hay proyección):
    ///       - un máximo acumulativo (% de bajas) ya superado → No cumplida (en lo que queda del mes no mejora);
    ///       - el actual y el estimado cumplen (y no es acumulativo) → Cumplida;
    ///       - el estimado cumple → En camino; si no → En riesgo.
    /// Escribir exige la patente de Gerencia (mnuSugerenciaPromocion), la misma que los umbrales del motor;
    /// cada cambio queda en la bitácora del sistema y en la de negocio (evento DecisionComercial).
    /// </summary>
    public class MetasComerciales
    {
        public const int DiasMinimosProyeccion = 7;
        public const int MesesHistoria = 6;
        public const int MesesFuturosEditables = 12;

        private readonly DAL.Interfaces.IMetaComercialDAL dal;
        private readonly Func<DateTime> ahora;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();
        private readonly Servicios.IRegistroBitacoraNegocio bitacoraNeg = Servicios.FabricaBitacora.CrearNegocio();

        public MetasComerciales() : this(new DAL.MetaComercial()) { }

        public MetasComerciales(DAL.Interfaces.IMetaComercialDAL dal, Func<DateTime> ahora = null)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
            this.ahora = ahora ?? (() => DateTime.Now);
        }

        private static void Exigir() =>
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

        // ── Consultas ─────────────────────────────────────────────────────────────

        /// <summary>Metas de los meses entre <paramref name="desdeMes"/> y <paramref name="hastaMes"/> (inclusivos).</summary>
        public List<BE.MetaComercial> Obtener(DateTime desdeMes, DateTime hastaMes)
        {
            Exigir();
            return dal.Obtener(BE.MetaComercial.InicioDeMes(desdeMes), BE.MetaComercial.InicioDeMes(hastaMes))
                   ?? new List<BE.MetaComercial>();
        }

        /// <summary>¿Se puede crear, modificar o eliminar una meta de ese mes? (mes en curso o futuro cercano).</summary>
        public static bool EsMesEditable(DateTime mes, DateTime hoy)
        {
            DateTime m = BE.MetaComercial.InicioDeMes(mes), actual = BE.MetaComercial.InicioDeMes(hoy);
            return m >= actual && m <= actual.AddMonths(MesesFuturosEditables);
        }

        // ── Escritura ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida una meta (pura): indicador conocido, mes editable y objetivo dentro del rango (con dos
        /// decimales como máximo). Lanza AppException traducible.
        /// </summary>
        public static void Validar(BE.IndicadorMeta indicador, DateTime mes, decimal valor, DateTime hoy)
        {
            if (!CatalogoIndicadores.Existe(indicador))
                throw new BE.AppException("err.bll.meta.indicador", "El indicador de la meta no existe.");
            var def = CatalogoIndicadores.De(indicador);
            if (BE.MetaComercial.InicioDeMes(mes) < BE.MetaComercial.InicioDeMes(hoy))
                throw new BE.AppException("err.bll.meta.mescerrado",
                    "El mes {0:MM/yyyy} ya cerró: sus metas no se pueden cambiar.", mes);
            if (!EsMesEditable(mes, hoy))
                throw new BE.AppException("err.bll.meta.mesfuturo",
                    "Solo se pueden fijar metas hasta {0} meses hacia adelante.", MesesFuturosEditables);
            if (valor < def.Minimo || valor > def.Maximo)
                throw new BE.AppException("err.bll.meta.rango",
                    "El objetivo de '{0}' debe estar entre {1} y {2}.", def.Nombre, def.Minimo, def.Maximo);
            if (valor != Math.Round(valor, 2))
                throw new BE.AppException("err.bll.meta.decimales", "El objetivo admite como máximo dos decimales.");
        }

        /// <summary>
        /// Fija el objetivo del indicador para el mes: si ya hay una meta de ese indicador en ese mes, la
        /// modifica (una sola meta por indicador y mes); si no, la crea.
        /// </summary>
        public BE.MetaComercial Guardar(string modulo, BE.IndicadorMeta indicador, DateTime mes, decimal valor)
        {
            Exigir();
            DateTime periodo = BE.MetaComercial.InicioDeMes(mes);
            DateTime cuando = ahora();
            Validar(indicador, periodo, valor, cuando);
            var def = CatalogoIndicadores.De(indicador);

            var existente = (dal.Obtener(periodo, periodo) ?? new List<BE.MetaComercial>()).FirstOrDefault(m => m.Indicador == indicador);
            decimal? anterior = existente?.ValorObjetivo;
            var meta = existente ?? new BE.MetaComercial { Periodo = periodo, Indicador = indicador };
            meta.ValorObjetivo = valor;
            meta.Sentido = def.Sentido;
            meta.IdUsuario = BLLHelper.ResolverUsuarioActivo();
            meta.Fecha = cuando;

            if (existente == null)
                meta.IdMeta = dal.Alta(meta);
            else if (!dal.Modificar(meta))
                throw new BE.AppException("err.bll.meta.concurrente", "La meta cambió desde otra sesión. Actualice la pantalla.");

            Registrar(modulo, existente == null
                ? $"Meta comercial #{meta.IdMeta} fijada: {def.Nombre} {periodo:MM/yyyy} = {valor:0.##} ({def.Sentido})"
                : $"Meta comercial #{meta.IdMeta} modificada: {def.Nombre} {periodo:MM/yyyy} de {anterior:0.##} a {valor:0.##}");
            return meta;
        }

        /// <summary>Elimina una meta del mes en curso o de un mes futuro.</summary>
        public void Eliminar(string modulo, BE.MetaComercial meta)
        {
            Exigir();
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (!EsMesEditable(meta.Periodo, ahora()))
                throw new BE.AppException("err.bll.meta.mescerrado",
                    "El mes {0:MM/yyyy} ya cerró: sus metas no se pueden cambiar.", meta.Periodo);
            if (!dal.Eliminar(meta.IdMeta))
                throw new BE.AppException("err.bll.meta.concurrente", "La meta cambió desde otra sesión. Actualice la pantalla.");
            Registrar(modulo, $"Meta comercial #{meta.IdMeta} eliminada: {CatalogoIndicadores.De(meta.Indicador).Nombre} {meta.Periodo:MM/yyyy}");
        }

        private void Registrar(string modulo, string texto)
        {
            try { bitacora.Registrar(modulo, texto, BE.Criticidad.Baja); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.MetasComerciales] " + ex.Message); }
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.DecisionComercial, texto);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Seguimiento (puro)
        // ══════════════════════════════════════════════════════════════════════

        public static bool Cumple(BE.SentidoMeta sentido, decimal valor, decimal objetivo) =>
            sentido == BE.SentidoMeta.MayorEsMejor ? valor >= objetivo : valor <= objetivo;

        /// <summary>Avance (%): mayor es mejor = real / objetivo; menor es mejor = real / máximo permitido.</summary>
        public static decimal? Avance(BE.SentidoMeta sentido, decimal? actual, decimal objetivo)
        {
            if (!actual.HasValue) return null;
            if (objetivo == 0) return actual.Value == 0 ? 0m : (decimal?)null;
            return Math.Round(actual.Value * 100m / objetivo, 1);
        }

        /// <summary>Semáforo de la meta (ver el resumen de la clase).</summary>
        public static BE.EstadoMeta Semaforo(BE.IndicadorMeta indicador, DateTime mes, DateTime hoy, decimal objetivo,
                                             decimal? actual, decimal? proyeccion)
        {
            var def = CatalogoIndicadores.De(indicador);
            DateTime m = BE.MetaComercial.InicioDeMes(mes), actualMes = BE.MetaComercial.InicioDeMes(hoy);
            if (m > actualMes) return BE.EstadoMeta.Pendiente;
            if (!actual.HasValue) return BE.EstadoMeta.SinDatos;
            if (m < actualMes)
                return Cumple(def.Sentido, actual.Value, objetivo) ? BE.EstadoMeta.Cumplida : BE.EstadoMeta.NoCumplida;

            decimal estimado = proyeccion ?? actual.Value;
            if (def.EsAcumulativo && def.Sentido == BE.SentidoMeta.MenorEsMejor && actual.Value > objetivo)
                return BE.EstadoMeta.NoCumplida;
            if (!def.EsAcumulativo && Cumple(def.Sentido, actual.Value, objetivo) && Cumple(def.Sentido, estimado, objetivo))
                return BE.EstadoMeta.Cumplida;
            return Cumple(def.Sentido, estimado, objetivo) ? BE.EstadoMeta.EnCamino : BE.EstadoMeta.EnRiesgo;
        }

        /// <summary>
        /// Proyección al cierre, ANCLADA en el valor de hoy: estimado = valor de hoy + pendiente × días que faltan,
        /// con la pendiente de la regresión lineal (ProyeccionSerie) sobre el valor de cada día transcurrido. El
        /// último día del mes el estimado es el valor real; un salto reciente no se diluye en el promedio de la
        /// recta. null con menos de DiasMinimosProyeccion días. Nunca negativa; un porcentaje no pasa de 100.
        /// </summary>
        public static decimal? Proyectar(IList<decimal> serieDiaria, int diasDelMes, bool esPorcentaje)
        {
            if (serieDiaria == null || serieDiaria.Count < DiasMinimosProyeccion || diasDelMes < 1) return null;
            var ajuste = ProyeccionSerie.RegresionLineal(serieDiaria);
            int faltan = Math.Max(0, diasDelMes - serieDiaria.Count);
            decimal valor = Math.Max(0m, serieDiaria[serieDiaria.Count - 1] + ajuste.Pendiente * faltan);
            if (esPorcentaje) valor = Math.Min(100m, valor);
            return Math.Round(valor, 2);
        }

        /// <summary>
        /// Valor del indicador del mes <paramref name="mes"/> al cierre del día <paramref name="dia"/> (dentro
        /// del mes, no posterior a hoy), con las fórmulas de los KPI de "Analizar":
        ///   MRR = MRR de hoy − MRR de las altas posteriores a ese día + MRR de las bajas posteriores;
        ///   % de bajas = bajas del mes hasta ese día / clientes al inicio del mes × 100 (por plan, como la regla);
        ///   ocupación = la de hoy (solo si el día es hoy). null si no se puede medir.
        /// </summary>
        public static decimal? ValorAl(BE.IndicadorMeta indicador, DateTime mes, DateTime dia, DatosMetas d)
        {
            if (d == null) return null;
            DateTime hoy = d.Hoy.Date, inicio = BE.MetaComercial.InicioDeMes(mes), finExcl = dia.Date.AddDays(1);
            if (dia.Date > hoy || dia.Date < inicio) return null;
            decimal Precio(int? id) => id.HasValue && d.Precios.TryGetValue(id.Value, out var v) ? v : 0m;
            var bajas = d.Movimientos.Where(x => x.Resultado == BE.EstadoRenovacion.Baja).ToList();

            switch (indicador)
            {
                case BE.IndicadorMeta.Mrr:
                {
                    decimal mrrHoy = d.Clientes.Where(c => c.IdPlan.HasValue && c.PagaAl(hoy)).Sum(c => Precio(c.IdPlan));
                    decimal altasPost = d.Altas.Where(a => a.Fecha >= finExcl).Sum(a => Precio(a.IdPlan));
                    decimal bajasPost = bajas.Where(b => b.Fecha >= finExcl).Sum(b => Precio(b.IdPlanAnterior));
                    return CalculoKpi.MrrAlFinal(mrrHoy, altasPost, bajasPost);
                }
                case BE.IndicadorMeta.PorcentajeBajas:
                {
                    var planes = d.Clientes.Where(c => c.IdPlan.HasValue).Select(c => c.IdPlan.Value)
                        .Concat(d.Altas.Where(a => a.IdPlan.HasValue).Select(a => a.IdPlan.Value))
                        .Concat(bajas.Where(b => b.IdPlanAnterior.HasValue).Select(b => b.IdPlanAnterior.Value))
                        .Distinct();
                    int totalBajas = 0, totalInicio = 0;
                    foreach (int p in planes)
                    {
                        int activos = d.Clientes.Count(c => c.IdPlan == p);
                        int altasPost = d.Altas.Where(a => a.IdPlan == p && a.Fecha >= finExcl).Select(a => a.IdCliente).Distinct().Count();
                        int bajasPost = bajas.Where(b => b.IdPlanAnterior == p && b.Fecha >= finExcl).Select(b => b.IdCliente).Distinct().Count();
                        int altasMes = d.Altas.Where(a => a.IdPlan == p && a.Fecha >= inicio && a.Fecha < finExcl).Select(a => a.IdCliente).Distinct().Count();
                        int bajasMes = bajas.Where(b => b.IdPlanAnterior == p && b.Fecha >= inicio && b.Fecha < finExcl).Select(b => b.IdCliente).Distinct().Count();
                        int fin = CalculoKpi.ClientesAlFinal(activos, altasPost, bajasPost);
                        totalInicio += CalculoKpi.ClientesAlInicio(fin, altasMes, bajasMes);
                        totalBajas += bajasMes;
                    }
                    return CalculoKpi.TasaBajas(totalBajas, totalInicio);
                }
                case BE.IndicadorMeta.OcupacionStock:
                    return dia.Date == hoy ? d.OcupacionHoy : null;
                default:
                    return null;
            }
        }

        /// <summary>Valor al cierre de cada día del mes hasta hoy (o hasta el fin del mes), sin los días sin dato.</summary>
        public static List<decimal> SerieDiaria(BE.IndicadorMeta indicador, DateTime mes, DatosMetas d)
        {
            var serie = new List<decimal>();
            DateTime inicio = BE.MetaComercial.InicioDeMes(mes), fin = inicio.AddMonths(1).AddDays(-1);
            DateTime hasta = d.Hoy.Date < fin ? d.Hoy.Date : fin;
            for (DateTime dia = inicio; dia <= hasta; dia = dia.AddDays(1))
            {
                var v = ValorAl(indicador, inicio, dia, d);
                if (v.HasValue) serie.Add(v.Value);
            }
            return serie;
        }

        /// <summary>Valor real del mes: al cierre si ya terminó, a hoy si es el mes en curso; null si es futuro.</summary>
        public static decimal? ValorDelMes(BE.IndicadorMeta indicador, DateTime mes, DatosMetas d)
        {
            DateTime inicio = BE.MetaComercial.InicioDeMes(mes), fin = inicio.AddMonths(1).AddDays(-1);
            if (inicio > d.Hoy.Date) return null;
            return ValorAl(indicador, inicio, fin < d.Hoy.Date ? fin : d.Hoy.Date, d);
        }

        /// <summary>Seguimiento completo de una meta: real, proyección, avance, semáforo, explicación y serie.</summary>
        public static BE.SeguimientoMeta Seguir(BE.MetaComercial meta, IEnumerable<BE.MetaComercial> todas, DatosMetas d)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            var def = CatalogoIndicadores.De(meta.Indicador);
            DateTime hoy = d.Hoy.Date, mes = BE.MetaComercial.InicioDeMes(meta.Periodo), mesActual = BE.MetaComercial.InicioDeMes(hoy);
            bool enCurso = mes == mesActual;
            int diasMes = DateTime.DaysInMonth(mes.Year, mes.Month);
            decimal? actual = ValorDelMes(meta.Indicador, mes, d);
            decimal? proyeccion = enCurso && def.TieneHistoria
                ? Proyectar(SerieDiaria(meta.Indicador, mes, d), diasMes, def.Unidad == BE.UnidadKpi.Porcentaje)
                : null;
            var estado = Semaforo(meta.Indicador, mes, hoy, meta.ValorObjetivo, actual, proyeccion);

            var s = new BE.SeguimientoMeta
            {
                Meta = meta,
                NombreIndicador = def.NombreTraducible,
                Unidad = def.Unidad,
                ValorActual = actual,
                Proyeccion = proyeccion,
                AvancePorcentaje = Avance(def.Sentido, actual, meta.ValorObjetivo),
                Estado = estado,
                EsMesEnCurso = enCurso,
                DiasDelMes = diasMes,
                DiasTranscurridos = enCurso ? hoy.Day : (mes < mesActual ? diasMes : 0),
                Explicacion = Explicar(def, estado, enCurso, actual, proyeccion, meta.ValorObjetivo)
            };

            // Serie real contra meta de los últimos meses (hasta el de la meta o el actual, el que sea anterior).
            var lista = (todas ?? Enumerable.Empty<BE.MetaComercial>()).Where(x => x.Indicador == meta.Indicador).ToList();
            DateTime ultimo = mes < mesActual ? mes : mesActual;
            for (int i = MesesHistoria - 1; i >= 0; i--)
            {
                DateTime m = ultimo.AddMonths(-i);
                s.Serie.Add(new BE.PuntoMeta
                {
                    Mes = m,
                    Real = ValorDelMes(meta.Indicador, m, d),
                    Objetivo = lista.FirstOrDefault(x => BE.MetaComercial.InicioDeMes(x.Periodo) == m)?.ValorObjetivo,
                    Proyeccion = m == mesActual ? proyeccion : null,
                    EsMesEnCurso = m == mesActual
                });
            }
            return s;
        }

        private static BE.TextoTraducible Valor(BE.UnidadKpi unidad, decimal? v)
        {
            if (!v.HasValue) return new BE.TextoTraducible("cdc.meta.valor.sindato", "sin dato");
            return unidad == BE.UnidadKpi.Moneda
                ? new BE.TextoTraducible("cdc.meta.valor.moneda", "{0:C0}", v.Value)
                : new BE.TextoTraducible("cdc.meta.valor.pct", "{0:0.##} %", v.Value);
        }

        private static BE.TextoTraducible T(string clave, string fallback, params object[] args) => new BE.TextoTraducible(clave, fallback, args);

        private static BE.TextoTraducible Explicar(CatalogoIndicadores.Definicion def, BE.EstadoMeta estado, bool enCurso,
                                                  decimal? actual, decimal? proyeccion, decimal objetivo)
        {
            var real = Valor(def.Unidad, actual);
            var obj = Valor(def.Unidad, objetivo);
            var proy = Valor(def.Unidad, proyeccion);
            var nota = !def.TieneHistoria
                ? T("cdc.meta.nota.foto", "Es una foto del día: no tiene historia para proyectar el cierre.")
                : T("cdc.meta.nota.pocosdias", "Con menos de {0} días del mes todavía no se proyecta el cierre.", DiasMinimosProyeccion);
            switch (estado)
            {
                case BE.EstadoMeta.Pendiente:
                    return T("cdc.meta.exp.pendiente", "El mes todavía no empezó.");
                case BE.EstadoMeta.SinDatos:
                    return !def.TieneHistoria && !enCurso
                        ? T("cdc.meta.exp.foto", "Es una foto del día: no se puede reconstruir para un mes cerrado.")
                        : T("cdc.meta.exp.sindatos", "No hay datos para medir este indicador en el mes.");
                case BE.EstadoMeta.Cumplida:
                    return enCurso
                        ? T("cdc.meta.exp.cumplida", "Ya está en {0} (objetivo {1}) y la proyección al cierre ({2}) lo sostiene.", real, obj, proyeccion.HasValue ? proy : real)
                        : T("cdc.meta.exp.cerrada.cumplida", "Cerró el mes en {0} contra un objetivo de {1}: meta cumplida.", real, obj);
                case BE.EstadoMeta.NoCumplida:
                    return enCurso
                        ? T("cdc.meta.exp.superada", "Ya está en {0} y el máximo era {1}: en lo que queda del mes no puede mejorar.", real, obj)
                        : T("cdc.meta.exp.cerrada.nocumplida", "Cerró el mes en {0} contra un objetivo de {1}: no se cumplió.", real, obj);
                case BE.EstadoMeta.EnCamino:
                    return proyeccion.HasValue
                        ? T("cdc.meta.exp.encamino", "Está en {0} (objetivo {1}); al ritmo actual cerraría el mes en {2}.", real, obj, proy)
                        : T("cdc.meta.exp.encamino.sinproy", "Está en {0} (objetivo {1}): hoy se cumple. {2}", real, obj, nota);
                default:
                    return proyeccion.HasValue
                        ? T("cdc.meta.exp.enriesgo", "Está en {0} (objetivo {1}); al ritmo actual cerraría el mes en {2}: hay que actuar.", real, obj, proy)
                        : T("cdc.meta.exp.enriesgo.sinproy", "Está en {0} contra un objetivo de {1}: hoy no se cumple. {2}", real, obj, nota);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Movimiento: las metas en riesgo suben la prioridad de lo que las mueve
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Por cada meta del mes en curso que requiere acción (en riesgo o ya perdida), las recomendaciones
        /// de las reglas que mueven ese indicador (CatalogoIndicadores.Reglas) y que ganan plata (impacto
        /// &gt; 0) suben un nivel de prioridad (Baja → Media → Alta) y explican por qué. Una recomendación sube
        /// una sola vez aunque mueva varias metas en riesgo. Devuelve la lista reordenada como el motor.
        /// </summary>
        public static List<BE.Recomendacion> ElevarPrioridad(IEnumerable<BE.Recomendacion> recomendaciones,
                                                             IEnumerable<BE.SeguimientoMeta> metas)
        {
            var recs = (recomendaciones ?? Enumerable.Empty<BE.Recomendacion>()).Where(r => r != null).ToList();
            var enRiesgo = (metas ?? Enumerable.Empty<BE.SeguimientoMeta>())
                .Where(m => m?.Meta != null && m.EsMesEnCurso && m.RequiereAccion).ToList();
            if (enRiesgo.Count == 0) return recs;

            foreach (var r in recs.Where(x => x.ImpactoEstimado > 0 && x.PrioridadElevada == null))
            {
                var meta = enRiesgo.FirstOrDefault(m => CatalogoIndicadores.De(m.Meta.Indicador).Reglas
                    .Contains(r.TipoRegla ?? "", StringComparer.OrdinalIgnoreCase));
                if (meta == null) continue;
                var estimado = Valor(meta.Unidad, meta.Proyeccion ?? meta.ValorActual);
                var objetivo = Valor(meta.Unidad, meta.Meta.ValorObjetivo);
                if (r.Prioridad != BE.PrioridadRecomendacion.Alta)
                {
                    r.Prioridad = r.Prioridad - 1;
                    r.PrioridadElevada = T("cdc.meta.prioridad",
                        "Prioridad elevada: la meta de {0} está en riesgo ({1} contra un objetivo de {2}) y esta recomendación la mueve.",
                        meta.NombreIndicador, estimado, objetivo);
                }
                else
                    r.PrioridadElevada = T("cdc.meta.prioridad.yaalta",
                        "Ayuda a la meta de {0}, que está en riesgo ({1} contra un objetivo de {2}).",
                        meta.NombreIndicador, estimado, objetivo);
            }
            return MotorRecomendaciones.Ordenar(recs);
        }
    }
}
