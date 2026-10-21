using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia.Reglas;

namespace BLL.Inteligencia
{
    /// <summary>Resultado medido de una campaña de contacto (ver MedicionCampanias).</summary>
    public sealed class ResultadoCampania
    {
        /// <summary>Resultado incremental ($): lo que la campaña logró por encima de lo que pasa sin ella.
        /// null mientras la ventana de medición está abierta (<see cref="EnMedicion"/>): recién al cerrar hay importe.</summary>
        public decimal? Valor { get; set; }
        /// <summary>true mientras la ventana de medición no cerró: solo hay avance (respondieron de contactados).</summary>
        public bool EnMedicion { get; set; }
        public int Contactados { get; set; }
        public int Respondieron { get; set; }
        /// <summary>Población comparable de la campaña: los contactados que resolvieron una renovación en la
        /// ventana (renovaron, cambiaron de plan o se dieron de baja). Es el denominador al que se aplica la tasa base.</summary>
        public int Comparables { get; set; }
        /// <summary>Tasa base (0 a 1): en la ventana anterior, de los clientes de esos planes que resolvieron una
        /// renovación, la parte que respondió (la misma población que <see cref="Comparables"/>).</summary>
        public decimal TasaBase { get; set; }
        public decimal ValorPorRespuesta { get; set; }
        public DateTime Desde { get; set; }
        /// <summary>Fin de la ventana (exclusivo) y día en que cierra la medición: el día de la ejecución + <see cref="MedicionCampanias.DiasVentana"/>.</summary>
        public DateTime Hasta { get; set; }

        /// <summary>El resultado con su estado explícito: en medición (avance y cierre) o medido (importe).</summary>
        public BE.MedicionDecision ComoMedicion() => EnMedicion || !Valor.HasValue
            ? BE.MedicionDecision.EnCurso(Respondieron, Contactados, Hasta)
            : BE.MedicionDecision.Medida(Valor.Value);
    }

    /// <summary>
    /// PN03 «Medir resultado» — Resultado real de una campaña de contacto (opción "Exportar contactos" de
    /// las reglas Renovaciones, ChurnPlan y UpgradePlan). Al ejecutarse, la decisión guarda a quiénes se
    /// contactó (DecisionComercialDetalle). La medición es INCREMENTAL, para no acreditarle a la campaña las
    /// renovaciones de rutina:
    ///   ventana      = los <see cref="DiasVentana"/> días desde la EJECUCIÓN de la decisión (fija): desde la
    ///                  ejecución hasta el comienzo del día ejecución + <see cref="DiasVentana"/>, que es el día en
    ///                  que cierra. Mientras está abierta NO hay importe (EnMedicion: solo respondieron de
    ///                  contactados y la fecha de cierre); restar la tasa base antes de que nadie haya podido
    ///                  responder daría un resultado negativo sin sentido. Sin FechaEjecucion (decisiones
    ///                  anteriores a ese dato) se usa la fecha de creación (FechaVigencia).
    ///   población    = la MISMA definición en las dos ventanas: los clientes que resolvieron una renovación en
    ///   comparable     la ventana (renovaron, cambiaron de plan o se dieron de baja; HistorialRenovacion). Es la
    ///                  única forma observable de «tener un vencimiento»: un cliente sin movimiento no tiene
    ///                  resultado que comparar y no cuenta ni a favor ni en contra (antes la base salía de los
    ///                  resueltos y se restaba sobre TODOS los contactados: una campaña igual a la rutina daba
    ///                  negativo por los contactados sin movimiento).
    ///   respondieron = de los comparables, los que respondieron: Renovaciones y ChurnPlan = el primer movimiento
    ///                  resuelto es renovación o cambio de plan; UpgradePlan = pasó a un plan más caro (cualquier
    ///                  cambio de plan en la ventana, aunque antes haya renovado)
    ///   tasa base    = en los <see cref="DiasVentana"/> días ANTERIORES a la decisión, de los clientes de los
    ///                  planes de los contactados que resolvieron una renovación (población comparable de esa
    ///                  ventana, por cliente), la parte que respondió con el mismo criterio (sin historia: 0)
    ///   resultado    = (respondieron − tasa base × comparables) × valor por respuesta, al cerrar la ventana;
    ///                  es decir (tasa de la campaña − tasa base) × comparables × valor. Una campaña igual a la
    ///                  rutina da 0; peor, negativo (se muestra igual)
    ///   valor        = Renovaciones: precio mensual del plan; ChurnPlan: precio × horizonte; UpgradePlan:
    ///                  (precio del plan nuevo − del anterior) × horizonte; promedio de los que respondieron
    ///                  (sin respuestas: el precio promedio de los planes contactados; en upgrade, 0)
    /// Se usa el precio ACTUAL de cada plan: la base no guarda el historial de precios. Cada cliente se le
    /// atribuye a una sola campaña: la primera (por fecha de ejecución) que lo contactó (<see cref="Atribuir"/>).
    /// </summary>
    public static class MedicionCampanias
    {
        public const int DiasVentana = 30;

        /// <summary>Cada cliente, a la primera campaña ejecutada que lo contactó (las siguientes no lo cuentan).</summary>
        public static Dictionary<int, List<int>> Atribuir(IEnumerable<BE.DecisionComercial> decisiones, IDictionary<int, List<int>> contactos)
        {
            var resultado = new Dictionary<int, List<int>>();
            if (contactos == null) return resultado;
            var asignados = new HashSet<int>();
            foreach (var d in (decisiones ?? Enumerable.Empty<BE.DecisionComercial>())
                         .Where(x => x != null && contactos.ContainsKey(x.IdDecision))
                         .OrderBy(x => x.FechaVigencia).ThenBy(x => x.IdDecision))
                resultado[d.IdDecision] = contactos[d.IdDecision].Distinct().Where(c => asignados.Add(c)).ToList();
            return resultado;
        }

        private static bool Resuelta(BE.MovimientoSuscripcion m) =>
            m.Resultado == BE.EstadoRenovacion.Renovada || m.Resultado == BE.EstadoRenovacion.CambioPlan || m.Resultado == BE.EstadoRenovacion.Baja;

        public static ResultadoCampania Medir(BE.DecisionComercial decision, IEnumerable<int> contactados,
                                              IEnumerable<BE.MovimientoSuscripcion> movimientos, IDictionary<int, decimal> precios,
                                              IDictionary<int, int?> planDeCliente, decimal horizonteMeses, DateTime hoy)
        {
            if (decision == null || !decision.AdmiteResultado()) return null;
            var ids = new HashSet<int>(contactados ?? Enumerable.Empty<int>());
            if (ids.Count == 0) return null;
            var movs = (movimientos ?? Enumerable.Empty<BE.MovimientoSuscripcion>()).ToList();
            decimal Precio(int? id) => id.HasValue && precios != null && precios.TryGetValue(id.Value, out var v) ? v : 0m;
            decimal horizonte = Math.Max(1m, horizonteMeses);
            bool upgrade = decision.TipoRegla == ReglaUpgradePlan.Codigo;
            bool porHorizonte = upgrade || decision.TipoRegla == ReglaChurnPlan.Codigo;
            bool Responde(BE.MovimientoSuscripcion m) => upgrade
                ? m.Resultado == BE.EstadoRenovacion.CambioPlan && Precio(m.IdPlanNuevo) > Precio(m.IdPlanAnterior)
                : m.Resultado == BE.EstadoRenovacion.Renovada || m.Resultado == BE.EstadoRenovacion.CambioPlan;
            decimal Valor(BE.MovimientoSuscripcion m) => upgrade
                ? (Precio(m.IdPlanNuevo) - Precio(m.IdPlanAnterior)) * horizonte
                : Precio(m.IdPlanNuevo ?? m.IdPlanAnterior) * (porHorizonte ? horizonte : 1m);

            // La ventana cierra al empezar el día ejecución + DiasVentana: ese día ya se muestra el importe
            // (y ningún movimiento posterior lo cambia).
            DateTime desde = decision.FechaVigencia, hasta = desde.Date.AddDays(DiasVentana);
            var r = new ResultadoCampania { Contactados = ids.Count, Desde = desde, Hasta = hasta, EnMedicion = hoy.Date < hasta };

            // Plan de cada contactado: el de hoy o, si ya no está, el del movimiento que lo dejó.
            int? PlanDe(int id) => planDeCliente != null && planDeCliente.TryGetValue(id, out var p) && p.HasValue ? p
                : movs.Where(m => m.IdCliente == id).OrderBy(m => m.Fecha).Select(m => m.IdPlanAnterior).FirstOrDefault(x => x.HasValue);
            var planes = new HashSet<int>(ids.Select(PlanDe).Where(p => p.HasValue).Select(p => p.Value));

            // Población comparable de una ventana, por cliente: los que resolvieron una renovación en ella; de
            // cada uno, su primer movimiento resuelto y su respuesta (si respondió). La misma regla para la
            // campaña (contactados, ventana desde la ejecución) y para la base (planes, ventana anterior).
            List<(BE.MovimientoSuscripcion Primero, BE.MovimientoSuscripcion Respuesta)> Poblacion(IEnumerable<BE.MovimientoSuscripcion> ventana) =>
                ventana.GroupBy(m => m.IdCliente)
                       .Where(g => g.Any(Resuelta))
                       .Select(g =>
                       {
                           var orden = g.OrderBy(m => m.Fecha).ToList();
                           var primero = orden.First(Resuelta);
                           var respuesta = upgrade ? orden.FirstOrDefault(Responde) : (Responde(primero) ? primero : null);
                           return (primero, respuesta);
                       }).ToList();

            var comparables = Poblacion(movs.Where(m => ids.Contains(m.IdCliente) && m.Fecha >= desde && m.Fecha < hasta));
            var respuestas = comparables.Where(c => c.Respuesta != null).Select(c => c.Respuesta).ToList();
            r.Comparables = comparables.Count;
            r.Respondieron = respuestas.Count;

            var previos = Poblacion(movs.Where(m => m.Fecha >= desde.AddDays(-DiasVentana) && m.Fecha < desde))
                .Where(c => c.Primero.IdPlanAnterior.HasValue && planes.Contains(c.Primero.IdPlanAnterior.Value)).ToList();
            r.TasaBase = previos.Count > 0 ? Math.Round((decimal)previos.Count(c => c.Respuesta != null) / previos.Count, 4) : 0m;

            r.ValorPorRespuesta = respuestas.Count > 0
                ? respuestas.Average(Valor)
                : upgrade || planes.Count == 0 ? 0m : planes.Average(p => Precio(p)) * (porHorizonte ? horizonte : 1m);
            if (!r.EnMedicion)
                r.Valor = Math.Round((r.Respondieron - r.TasaBase * r.Comparables) * r.ValorPorRespuesta, 2);
            r.ValorPorRespuesta = Math.Round(r.ValorPorRespuesta, 2);
            return r;
        }
    }
}
