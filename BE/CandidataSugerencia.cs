namespace BE
{
    /// <summary>
    /// PN03 — Oportunidad de promoción detectada en el «Reporte de métricas» (rotación, abandono).
    /// Es una PROPUESTA de datos para que Gerencia arme su sugerencia con un dato concreto; no se
    /// persiste: Gerencia la revisa, la ajusta y recién ahí se registra la SugerenciaPromocion
    /// (que guarda el origen en OrigenMetrica).
    /// </summary>
    public class CandidataSugerencia
    {
        /// <summary>Reporte del que sale el dato: abandono por plan o rotación por categoría.</summary>
        public OrigenMetrica Origen { get; set; }

        public int? IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public string CategoriaPrenda { get; set; }

        /// <summary>Motivo redactado con el dato concreto (cantidades, categoría o plan afectado).</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción del motivo (formato con {0}, {1}…) y sus argumentos: la
        /// pantalla y el PDF lo muestran en el idioma activo; Motivo queda como texto en español.</summary>
        public string ClaveMotivo { get; set; }
        public object[] ArgsMotivo { get; set; }

        public TipoDescuento TipoSugerido { get; set; }

        /// <summary>Estimación inicial editable: ingreso mensual en riesgo (abandono) o un valor de
        /// referencia por prenda parada (rotación). Gerencia la ajusta antes de enviar.</summary>
        public decimal BeneficioEstimado { get; set; }
    }
}
