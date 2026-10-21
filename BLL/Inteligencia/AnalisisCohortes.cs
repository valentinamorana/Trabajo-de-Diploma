using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Análisis de cohortes» (Centro de decisiones comerciales, pestaña Cohortes): retención de los
    /// clientes agrupados por el mes en que se suscribieron.
    ///
    ///   Cohorte .............. clientes cuya PRIMERA suscripción cae en el mismo mes calendario. La fecha es
    ///                          la de DAL ObtenerAltas (la misma de las altas y el MRR del Centro): el primer
    ///                          pago de una contratación y, solo si no tiene ninguna, la fecha de alta del
    ///                          cliente. Limitación: un cliente anterior a PN02 que después pagó una
    ///                          contratación (p. ej. un upgrade) cae en la cohorte de ese pago. Si llegaran
    ///                          varias altas de un cliente, cuenta la primera. Con filtro de plan, la cohorte
    ///                          son los que se suscribieron a ese plan (un cambio de plan posterior no los saca).
    ///   Activo en el mes k ... el cliente no tiene una BAJA registrada (HistorialRenovacion, Resultado =
    ///                          Baja, la misma fuente que el % de bajas) antes del cierre del mes k desde su
    ///                          alta (k = 0 es el mes del alta). Igual que en el % de bajas, una pausa, una
    ///                          suscripción vencida o una suspensión por falta de pago NO son bajas: el
    ///                          cliente sigue en la cohorte. La primera baja corta la permanencia (si
    ///                          después se reactiva, no vuelve a contar).
    ///   Retención ............ activos en el mes k / tamaño de la cohorte × 100.
    ///   Mes parcial .......... si el mes k todavía no terminó al corte (el fin del período analizado,
    ///                          como máximo hoy), la celda se calcula con las bajas registradas hasta el
    ///                          corte y se marca con *: se muestra, pero no entra en la curva promedio ni
    ///                          en la elección de la peor cohorte (solo se comparan meses completos). La
    ///                          tendencia del Centro hace algo parecido con su mes parcial (lo grafica y no
    ///                          lo usa en la regresión), pero allí solo afecta a la proyección.
    ///   Curva promedio ....... para cada k, Σ activos / Σ tamaño de las cohortes con el mes k completo
    ///                          (promedio ponderado: una cohorte chica pesa poco).
    ///   Peor cohorte ......... entre las cohortes de al menos <see cref="MinimoClientesCohorte"/> clientes
    ///                          (para no exagerar el ruido de cohortes chicas), la de menor retención en el
    ///                          mes de comparación k* = el mayor k ≤ <see cref="MesComparacionPorDefecto"/>
    ///                          que tienen completo al menos 2 de esas cohortes (solo se compara lo
    ///                          comparable: una cohorte que todavía no vivió k meses no entra). Se compara
    ///                          la proporción exacta (activos / tamaño), no el % redondeado que se muestra.
    ///                          Empate: la de más clientes y, si sigue, la más antigua. Si retiene a todos
    ///                          sus clientes, no hay peor cohorte.
    ///   Plan que la explica .. el plan que dejaron más clientes de esa cohorte hasta el cierre del mes k*:
    ///                          el IdPlanAnterior de la baja, el mismo dato con que el % de bajas y
    ///                          ReglaChurnPlan cuentan las bajas de cada plan. Una baja sin IdPlanAnterior
    ///                          baja la retención, pero no se atribuye a ningún plan (tampoco cuenta en el %
    ///                          de bajas de ningún plan); si ninguna lo tiene, no hay plan. Empate: el de
    ///                          mayor proporción de bajas sobre sus altas en la cohorte, después el de menor
    ///                          ID. Su recomendación en el motor es la de ReglaChurnPlan: "ChurnPlan:&lt;IdPlan&gt;".
    ///   Colores .............. el % se muestra redondeado a entero (<see cref="RetencionMostrada"/>) y el
    ///                          tramo del mapa de calor (<see cref="Nivel"/>) sale de ese mismo valor.
    ///
    /// <see cref="Calcular"/> es pura (se prueba con datos armados a mano); <see cref="Analizar"/> lee
    /// altas y bajas de 12 meses con el DAL del Centro y exige la patente de Gerencia de PN03.
    /// </summary>
    public class AnalisisCohortes
    {
        /// <summary>Meses de alta que muestra la matriz (el del corte y los 11 anteriores).</summary>
        public const int MaximoCohortes = 12;
        /// <summary>Tamaño mínimo de una cohorte para competir por "la peor" (menos clientes = ruido).</summary>
        public const int MinimoClientesCohorte = 4;
        /// <summary>Mes desde el alta en que se comparan las cohortes (si hay datos; si no, el último común).</summary>
        public const int MesComparacionPorDefecto = 3;
        /// <summary>Cohortes comparables que hacen falta para elegir la peor.</summary>
        public const int CohortesParaComparar = 2;

        private readonly DAL.Interfaces.IInteligenciaComercialDAL dal;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;
        private readonly Func<DateTime> hoy;

        public AnalisisCohortes() : this(new DAL.InteligenciaComercial(), new DAL.PlanSuscripcion()) { }

        public AnalisisCohortes(DAL.Interfaces.IInteligenciaComercialDAL dal, DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                                Func<DateTime> hoy = null)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
            this.dalPlan = dalPlan;
            this.hoy = hoy ?? (() => DateTime.Today);
        }

        private static void Exigir() =>
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

        /// <summary>
        /// Matriz de cohortes al fin del período analizado (como máximo hoy), con el mismo filtro de plan
        /// que el Centro. Lee las altas y las bajas de los <see cref="MaximoCohortes"/> meses que terminan en
        /// el mes del corte.
        /// </summary>
        public BE.MatrizCohortes Analizar(DateTime hasta, int? idPlan = null)
        {
            Exigir();
            DateTime hoyDia = hoy().Date;
            DateTime corte = hasta.Date > hoyDia ? hoyDia : hasta.Date;
            DateTime desde = PrimerDia(corte).AddMonths(-(MaximoCohortes - 1));
            DateTime hastaExcl = corte.AddDays(1);
            var altas = dal.ObtenerAltas(desde, hastaExcl) ?? new List<BE.AltaSuscripcion>();
            var movimientos = dal.ObtenerMovimientos(desde, hastaExcl) ?? new List<BE.MovimientoSuscripcion>();
            var nombres = (dalPlan?.ObtenerTodos() ?? new List<BE.PlanSuscripcion>())
                .GroupBy(p => p.IdPlan).ToDictionary(g => g.Key, g => g.First().Nombre);
            return Calcular(altas, movimientos, corte, idPlan, nombres);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Cálculo puro
        // ══════════════════════════════════════════════════════════════════════

        private sealed class Miembro
        {
            public BE.AltaSuscripcion Alta;
            public BE.MovimientoSuscripcion Baja;
            // Mismo dato que el % de bajas por plan y ReglaChurnPlan: el plan que dejó (null = sin plan).
            public int? PlanDeBaja => Baja?.IdPlanAnterior;
        }

        // ── Tramos del mapa de calor (sobre el % que se muestra, redondeado a entero) ──
        public const decimal TramoAlto = 90m, TramoMedio = 75m, TramoBajo = 50m;

        /// <summary>El % de retención como se muestra (entero, redondeo comercial): grilla, gráfico e informe.</summary>
        public static decimal RetencionMostrada(decimal retencion) => Math.Round(retencion, 0, MidpointRounding.AwayFromZero);

        /// <summary>Tramo del mapa de calor según el % mostrado: así el color y el número de la celda coinciden.</summary>
        public static BE.NivelRetencion Nivel(decimal retencion)
        {
            decimal v = RetencionMostrada(retencion);
            return v >= TramoAlto ? BE.NivelRetencion.Alta
                 : v >= TramoMedio ? BE.NivelRetencion.Media
                 : v >= TramoBajo ? BE.NivelRetencion.Baja
                 : BE.NivelRetencion.Critica;
        }

        public static DateTime PrimerDia(DateTime fecha) => new DateTime(fecha.Year, fecha.Month, 1);

        private static int MesesEntre(DateTime desdeMes, DateTime hastaMes) =>
            (hastaMes.Year - desdeMes.Year) * 12 + hastaMes.Month - desdeMes.Month;

        private static decimal Porcentaje(int parte, int total) =>
            total > 0 ? Math.Round(parte * 100m / total, 1) : 0m;

        /// <summary>
        /// Arma la matriz: una fila por mes de alta (de los <see cref="MaximoCohortes"/> que terminan en el
        /// mes del corte, solo los que tienen altas), una celda por mes transcurrido, la curva promedio y la
        /// peor cohorte con el plan que más explica sus bajas. Ver las definiciones en el encabezado.
        /// </summary>
        public static BE.MatrizCohortes Calcular(IEnumerable<BE.AltaSuscripcion> altas, IEnumerable<BE.MovimientoSuscripcion> movimientos,
                                                 DateTime corte, int? idPlan = null, IDictionary<int, string> nombresPlan = null,
                                                 int minimoClientes = MinimoClientesCohorte, int mesComparacion = MesComparacionPorDefecto)
        {
            DateTime corteDia = corte.Date, finCorte = corteDia.AddDays(1);
            DateTime mesCorte = PrimerDia(corteDia);
            DateTime primerMes = mesCorte.AddMonths(-(MaximoCohortes - 1));
            string NombrePlan(int? id) => id.HasValue && nombresPlan != null && nombresPlan.TryGetValue(id.Value, out var n) ? n
                                         : id.HasValue ? "#" + id.Value : null;
            var matriz = new BE.MatrizCohortes
            {
                Corte = corteDia, IdPlan = idPlan, NombrePlan = idPlan.HasValue ? NombrePlan(idPlan) : null,
                MinimoClientes = Math.Max(1, minimoClientes), MesComparacion = Math.Max(0, mesComparacion)
            };

            // Una sola alta por cliente: la primera (el DAL ya devuelve la primera suscripción).
            var primeras = (altas ?? Enumerable.Empty<BE.AltaSuscripcion>())
                .Where(a => a != null)
                .GroupBy(a => a.IdCliente)
                .Select(g => g.OrderBy(a => a.Fecha).First())
                .Where(a => a.Fecha >= primerMes && a.Fecha < finCorte && (!idPlan.HasValue || a.IdPlan == idPlan))
                .ToList();
            var bajas = (movimientos ?? Enumerable.Empty<BE.MovimientoSuscripcion>())
                .Where(m => m != null && m.Resultado == BE.EstadoRenovacion.Baja && m.Fecha < finCorte)
                .GroupBy(m => m.IdCliente)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Fecha).ToList());

            var miembros = new Dictionary<DateTime, List<Miembro>>();
            foreach (var g in primeras.GroupBy(a => PrimerDia(a.Fecha)).OrderBy(g => g.Key))
            {
                var lista = g.Select(a => new Miembro
                {
                    Alta = a,
                    // La baja de ESTA suscripción: la primera registrada desde el alta.
                    Baja = bajas.TryGetValue(a.IdCliente, out var bs) ? bs.FirstOrDefault(b => b.Fecha >= a.Fecha) : null
                }).ToList();
                miembros[g.Key] = lista;

                var fila = new BE.FilaCohorte
                {
                    Mes = g.Key, Tamanio = lista.Count,
                    AltasPorPlan = lista.Where(m => m.Alta.IdPlan.HasValue).GroupBy(m => m.Alta.IdPlan.Value).ToDictionary(x => x.Key, x => x.Count()),
                    BajasPorPlan = lista.Where(m => m.PlanDeBaja.HasValue).GroupBy(m => m.PlanDeBaja.Value).ToDictionary(x => x.Key, x => x.Count()),
                    Comparable = lista.Count >= matriz.MinimoClientes
                };
                int meses = MesesEntre(g.Key, mesCorte);
                for (int k = 0; k <= meses; k++)
                {
                    DateTime finMes = g.Key.AddMonths(k + 1);
                    bool parcial = finMes > finCorte;
                    DateTime limite = parcial ? finCorte : finMes;
                    int activos = lista.Count(m => m.Baja == null || m.Baja.Fecha >= limite);
                    fila.Celdas.Add(new BE.CeldaCohorte
                    {
                        Desplazamiento = k, Activos = activos, Retencion = Porcentaje(activos, lista.Count), EsParcial = parcial
                    });
                }
                matriz.Cohortes.Add(fila);
            }
            matriz.MaximoDesplazamiento = matriz.Cohortes.Count == 0 ? 0 : matriz.Cohortes.Max(c => c.Celdas.Count - 1);

            // ── Curva promedio (solo meses completos, ponderada por tamaño) ──
            for (int k = 0; matriz.Cohortes.Count > 0 && k <= matriz.MaximoDesplazamiento; k++)
            {
                var completas = matriz.Cohortes.Where(c => Completa(c, k)).ToList();
                int clientes = completas.Sum(c => c.Tamanio);
                matriz.Curva.Add(new BE.PuntoCurvaRetencion
                {
                    Desplazamiento = k, Cohortes = completas.Count, Clientes = clientes,
                    Retencion = clientes > 0 ? Porcentaje(completas.Sum(c => c.Celda(k).Activos), clientes) : (decimal?)null
                });
            }

            ElegirPeor(matriz, miembros, NombrePlan);
            matriz.Resumen = Resumen(matriz);
            return matriz;
        }

        private static bool Completa(BE.FilaCohorte fila, int k)
        {
            var c = fila.Celda(k);
            return c != null && !c.EsParcial;
        }

        private static void ElegirPeor(BE.MatrizCohortes matriz, Dictionary<DateTime, List<Miembro>> miembros, Func<int?, string> nombrePlan)
        {
            if (matriz.Cohortes.Count == 0)
            {
                matriz.MotivoSinPeor = new BE.TextoTraducible("cdc.cohorte.sinpeor.sindatos",
                    "No hay altas de suscriptores en los últimos 12 meses: no hay cohortes que comparar.");
                return;
            }
            int kMax = Math.Min(matriz.MesComparacion, matriz.MaximoDesplazamiento);
            int kComparacion = -1;
            for (int k = kMax; k >= 0 && kComparacion < 0; k--)
                if (matriz.Cohortes.Count(c => c.Comparable && Completa(c, k)) >= CohortesParaComparar)
                    kComparacion = k;
            if (kComparacion < 0)
            {
                matriz.MotivoSinPeor = new BE.TextoTraducible("cdc.cohorte.sinpeor.pocas",
                    "Para elegir la peor cohorte hacen falta al menos 2 cohortes de {0} o más clientes con un mes completo; todavía no las hay.",
                    matriz.MinimoClientes);
                return;
            }

            // Se compara la proporción exacta: el % redondeado (Retencion) es solo para mostrar.
            var peor = matriz.Cohortes
                .Where(c => c.Comparable && Completa(c, kComparacion))
                .OrderBy(c => (decimal)c.Celda(kComparacion).Activos / c.Tamanio)
                .ThenByDescending(c => c.Tamanio)
                .ThenBy(c => c.Mes)
                .First();
            var celda = peor.Celda(kComparacion);
            if (celda.Activos >= peor.Tamanio)
            {
                matriz.MotivoSinPeor = new BE.TextoTraducible("cdc.cohorte.sinpeor.todas",
                    "Todas las cohortes comparables retienen el 100 % al mes {0}: no hay una cohorte peor que las demás.", kComparacion);
                return;
            }

            // Las bajas que explican esa retención: las de la cohorte hasta el cierre del mes de comparación.
            DateTime limite = peor.Mes.AddMonths(kComparacion + 1);
            var bajasCohorte = miembros[peor.Mes].Where(m => m.Baja != null && m.Baja.Fecha < limite).ToList();
            var plan = bajasCohorte.Where(m => m.PlanDeBaja.HasValue)
                .GroupBy(m => m.PlanDeBaja.Value)
                .Select(g => new
                {
                    IdPlan = g.Key, Bajas = g.Count(),
                    Proporcion = (decimal)g.Count() / (peor.AltasPorPlan.TryGetValue(g.Key, out var a) && a > 0 ? a : peor.Tamanio)
                })
                .OrderByDescending(x => x.Bajas).ThenByDescending(x => x.Proporcion).ThenBy(x => x.IdPlan)
                .FirstOrDefault();

            peor.EsPeor = true;
            matriz.Peor = new BE.PeorCohorte
            {
                Mes = peor.Mes, Tamanio = peor.Tamanio, Desplazamiento = kComparacion, Retencion = celda.Retencion,
                RetencionPromedio = matriz.Curva.FirstOrDefault(p => p.Desplazamiento == kComparacion)?.Retencion,
                Bajas = bajasCohorte.Count,
                IdPlan = plan?.IdPlan, NombrePlan = plan != null ? nombrePlan(plan.IdPlan) : null, BajasDelPlan = plan?.Bajas ?? 0,
                ClaveRecomendacion = plan != null ? Reglas.ReglaChurnPlan.Codigo + ":" + plan.IdPlan : null
            };
        }

        private static BE.TextoTraducible Resumen(BE.MatrizCohortes m)
        {
            var p = m.Peor;
            if (p == null) return m.MotivoSinPeor;
            object promedio = p.RetencionPromedio.HasValue
                ? (object)(p.RetencionPromedio.Value.ToString("0.#") + " %")
                : new BE.TextoTraducible("cdc.cohorte.sindato", "sin dato");
            if (p.IdPlan == null)
                return new BE.TextoTraducible("cdc.cohorte.peor.sinplan",
                    "La peor cohorte es la de {0:yyyy-MM} ({1} clientes): retiene el {2:0.#} % al mes {3}, contra el {4} del promedio. No se pudo identificar el plan de sus bajas.",
                    p.Mes, p.Tamanio, p.Retencion, p.Desplazamiento, promedio);
            return new BE.TextoTraducible("cdc.cohorte.peor",
                "La peor cohorte es la de {0:yyyy-MM} ({1} clientes): retiene el {2:0.#} % al mes {3}, contra el {4} del promedio. El plan {5} explica {6} de sus {7} baja(s).",
                p.Mes, p.Tamanio, p.Retencion, p.Desplazamiento, promedio, p.NombrePlan, p.BajasDelPlan, p.Bajas);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Movimiento: de la peor cohorte a la recomendación (o al simulador)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>La recomendación de bajas del plan que explica la peor cohorte ("ChurnPlan:&lt;IdPlan&gt;"), o null.</summary>
        public static BE.Recomendacion RecomendacionDeLaPeor(BE.PeorCohorte peor, IEnumerable<BE.Recomendacion> recomendaciones)
        {
            if (peor?.ClaveRecomendacion == null || recomendaciones == null) return null;
            return recomendaciones.FirstOrDefault(r => r != null && string.Equals(r.Clave, peor.ClaveRecomendacion, StringComparison.Ordinal));
        }

        /// <summary>
        /// Por qué el motor no propuso la recomendación de bajas del plan de la peor cohorte (mismos criterios
        /// que ReglaChurnPlan): el plan no se identificó, no está en el análisis (filtro de plan o sin precio),
        /// la regla de bajas falló en este análisis (<paramref name="reglasConError"/>, ResultadoMotor.ReglasConError),
        /// ya tiene una promoción vigente, no tenía clientes al inicio del período (sin % de bajas) o su % de
        /// bajas del período no llega a los umbrales.
        /// </summary>
        public static BE.TextoTraducible ExplicarSinRecomendacion(BE.PeorCohorte peor, BE.ContextoComercial ctx,
                                                                  IEnumerable<string> reglasConError = null)
        {
            if (peor?.IdPlan == null)
                return new BE.TextoTraducible("cdc.cohorte.sinrec.sinplan",
                    "No se pudo identificar el plan que explica las bajas de la cohorte: no hay una recomendación de bajas a la cual ir.");
            var plan = ctx?.Planes?.FirstOrDefault(x => x.IdPlan == peor.IdPlan.Value);
            if (plan == null || plan.Precio <= 0)
                return new BE.TextoTraducible("cdc.cohorte.sinrec.fueraanalisis",
                    "El plan {0} no está en el análisis actual (revise el filtro de plan): el motor no lo evaluó.", peor.NombrePlan);
            if (reglasConError != null && reglasConError.Any(r => string.Equals(r, Reglas.ReglaChurnPlan.Codigo, StringComparison.OrdinalIgnoreCase)))
                return new BE.TextoTraducible("cdc.cohorte.sinrec.errorregla",
                    "La regla de bajas por plan no se pudo evaluar en este análisis: no se sabe si el motor propondría una recomendación para el plan {0}. Vuelva a analizar.",
                    plan.Nombre);
            if (plan.TienePromocionVigente)
                return new BE.TextoTraducible("cdc.cohorte.sinrec.promovigente",
                    "El plan {0} ya tiene una promoción vigente: el motor no propone otra (un solo descuento por ciclo) y su rendimiento se evalúa en «Medir resultado».",
                    plan.Nombre);
            if (!plan.Churn.HasValue)
                return new BE.TextoTraducible("cdc.cohorte.sinrec.sinclientes",
                    "El plan {0} no tenía clientes al inicio del período analizado: no hay un % de bajas con el que el motor pueda evaluarlo.",
                    plan.Nombre);
            return new BE.TextoTraducible("cdc.cohorte.sinrec.umbral",
                "El % de bajas del plan {0} en el período analizado ({1}) no llega al umbral de {2:0.#} % ni subió {3:0.#} pp: el motor no generó la recomendación de bajas.",
                plan.Nombre, plan.Churn.Value.ToString("0.#") + " %",
                CatalogoUmbrales.Valor(ctx, CatalogoUmbrales.ChurnAltoPct), CatalogoUmbrales.Valor(ctx, CatalogoUmbrales.SubidaChurnPp));
        }
    }
}
