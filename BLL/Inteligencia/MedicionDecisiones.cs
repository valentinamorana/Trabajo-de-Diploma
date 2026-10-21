using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Medir resultado» — Totales del historial de decisiones (pantalla e Informe ejecutivo). Puro:
    ///   aceptadas  = las que admiten un resultado (aceptadas o ejecutadas) y su impacto estimado
    ///   con real   = las que tienen un resultado real registrado a mano (estimado → real)
    ///   medidas    = las que tienen un resultado FINAL medido por el sistema (estimado → medido); puede ser
    ///                negativo si la campaña quedó debajo de la rutina
    ///   en medición = campañas con la ventana abierta: se cuentan aparte y NO entran en ninguna suma
    ///                (todavía no tienen importe; ver MedicionCampanias)
    /// </summary>
    public static class MedicionDecisiones
    {
        public static BE.ResumenMediciones Resumir(IEnumerable<BE.DecisionComercial> lista, IDictionary<int, BE.MedicionDecision> medidos)
        {
            var todas = (lista ?? Enumerable.Empty<BE.DecisionComercial>()).Where(d => d != null).ToList();
            var aceptadas = todas.Where(d => d.AdmiteResultado()).ToList();
            var conReal = aceptadas.Where(d => d.ResultadoReal.HasValue).ToList();
            BE.MedicionDecision Medicion(BE.DecisionComercial d) =>
                medidos != null && medidos.TryGetValue(d.IdDecision, out var m) ? m : null;
            var medidas = aceptadas.Where(d => Medicion(d)?.Estado == BE.EstadoMedicion.Medido && Medicion(d).Valor.HasValue).ToList();
            return new BE.ResumenMediciones
            {
                Decisiones = todas.Count,
                Aceptadas = aceptadas.Count,
                EstimadoAceptadas = aceptadas.Sum(d => d.ImpactoEstimado),
                ConReal = conReal.Count,
                EstimadoConReal = conReal.Sum(d => d.ImpactoEstimado),
                Real = conReal.Sum(d => d.ResultadoReal.Value),
                Medidas = medidas.Count,
                EstimadoMedidas = medidas.Sum(d => d.ImpactoEstimado),
                Medido = medidas.Sum(d => Medicion(d).Valor.Value),
                EnMedicion = aceptadas.Count(d => Medicion(d)?.Estado == BE.EstadoMedicion.EnMedicion)
            };
        }
    }
}
