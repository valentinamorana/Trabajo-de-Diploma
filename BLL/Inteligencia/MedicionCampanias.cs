using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia.Reglas;

namespace BLL.Inteligencia
{
    /// <summary>Resultado medido de una campaña de contacto (ver MedicionCampanias).</summary>
    public sealed class ResultadoCampania
    {
        /// <summary>Resultado incremental ($): lo que la campaña logró por encima de lo que pasa sin ella.</summary>
        public decimal Valor { get; set; }
        /// <summary>true mientras la ventana de medición no cerró (el valor puede cambiar).</summary>
        public bool Parcial { get; set; }
        public int Contactados { get; set; }
        public int Respondieron { get; set; }
        /// <summary>Tasa base (0 a 1): cómo se comportaron esos planes en la ventana anterior a la campaña.</summary>
        public decimal TasaBase { get; set; }
        public decimal ValorPorRespuesta { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
    }

    /// <summary>
    /// PN03 «Medir resultado» — Resultado real de una campaña de contacto (opción "Exportar contactos" de
    /// las reglas Renovaciones, ChurnPlan y UpgradePlan). Al ejecutarse, la decisión guarda a quiénes se
    /// contactó (DecisionComercialDetalle). La medición es INCREMENTAL, para no acreditarle a la campaña las
    /// renovaciones de rutina:
    ///   ventana      = los <see cref="DiasVentana"/> días desde la EJECUCIÓN de la decisión (fija); hasta que
    ///                  cierra, el resultado es parcial
    ///   respondieron = contactados con una respuesta en la ventana: Renovaciones y ChurnPlan = renovó o cambió
    ///                  de plan (el primer movimiento); UpgradePlan = pasó a un plan más caro (cualquier cambio
    ///                  de plan en la ventana, aunque antes haya renovado)
    ///   tasa base    = en los <see cref="DiasVentana"/> días ANTERIORES a la decisión, de las renovaciones
    ///                  resueltas (renovada, cambio de plan o baja) de los planes de los contactados, la parte
    ///                  que hubiera contado como respuesta (sin historia: 0)
    ///   resultado    = (respondieron − tasa base × contactados) × valor por respuesta
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

            DateTime desde = decision.FechaVigencia, hasta = desde.AddDays(DiasVentana);
            var r = new ResultadoCampania { Contactados = ids.Count, Desde = desde, Hasta = hasta, Parcial = hoy < hasta };

            // Plan de cada contactado: el de hoy o, si ya no está, el del movimiento que lo dejó.
            int? PlanDe(int id) => planDeCliente != null && planDeCliente.TryGetValue(id, out var p) && p.HasValue ? p
                : movs.Where(m => m.IdCliente == id).OrderBy(m => m.Fecha).Select(m => m.IdPlanAnterior).FirstOrDefault(x => x.HasValue);
            var planes = new HashSet<int>(ids.Select(PlanDe).Where(p => p.HasValue).Select(p => p.Value));

            var enVentana = movs.Where(m => ids.Contains(m.IdCliente) && m.Fecha >= desde && m.Fecha < hasta).ToList();
            var respuestas = upgrade
                ? enVentana.Where(Responde).GroupBy(m => m.IdCliente).Select(g => g.OrderBy(m => m.Fecha).First()).ToList()
                : enVentana.Where(Resuelta).GroupBy(m => m.IdCliente).Select(g => g.OrderBy(m => m.Fecha).First()).Where(Responde).ToList();
            r.Respondieron = respuestas.Count;

            var previos = movs.Where(m => m.Fecha >= desde.AddDays(-DiasVentana) && m.Fecha < desde && Resuelta(m)
                                          && m.IdPlanAnterior.HasValue && planes.Contains(m.IdPlanAnterior.Value)).ToList();
            r.TasaBase = previos.Count > 0 ? Math.Round((decimal)previos.Count(Responde) / previos.Count, 4) : 0m;

            r.ValorPorRespuesta = respuestas.Count > 0
                ? respuestas.Average(Valor)
                : upgrade || planes.Count == 0 ? 0m : planes.Average(p => Precio(p)) * (porHorizonte ? horizonte : 1m);
            r.Valor = Math.Round((r.Respondieron - r.TasaBase * r.Contactados) * r.ValorPorRespuesta, 2);
            r.ValorPorRespuesta = Math.Round(r.ValorPorRespuesta, 2);
            return r;
        }
    }
}
