using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// PN03 — Lo que Contabilidad ve en "Analizar margen e impacto": la promoción, el beneficio
    /// estimado de la sugerencia que le dio origen (si existe) y las otras promociones Vigentes
    /// del mismo plan que se superponen en fechas (advertencia: no impide aprobar).
    /// </summary>
    public class AnalisisImpactoPromocion
    {
        public Promocion Promocion { get; set; }
        public decimal? BeneficioEstimadoSugerencia { get; set; }
        public OrigenMetrica? OrigenSugerencia { get; set; }
        public List<Promocion> Superpuestas { get; set; } = new List<Promocion>();

        /// <summary>false si el usuario en sesión es quien creó la promoción (no puede dictaminarla).</summary>
        public bool UsuarioPuedeDictaminar { get; set; }

        public bool TieneSuperposicion() => Superpuestas != null && Superpuestas.Count > 0;
    }
}
