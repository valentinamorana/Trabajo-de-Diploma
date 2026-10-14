namespace BE
{
    /// <summary>
    /// PN03 — Oportunidad de promoción detectada en el «Reporte de métricas» (abandono por plan).
    /// Es una PROPUESTA de datos para que Gerencia arme su sugerencia con un dato concreto; no se
    /// persiste: Gerencia la revisa, la ajusta y recién ahí se registra la SugerenciaPromocion
    /// (que guarda el origen en OrigenMetrica). Siempre apunta a un plan: las oportunidades por
    /// categoría de prenda quedan en el reporte como dato (MetricaRotacionCategoria.EsOportunidad)
    /// pero no sirven de base para una sugerencia.
    /// </summary>
    public class CandidataSugerencia
    {
        /// <summary>Reporte del que sale el dato (abandono por plan).</summary>
        public OrigenMetrica Origen { get; set; }

        public int IdPlan { get; set; }
        public string NombrePlan { get; set; }

        /// <summary>Motivo redactado con el dato concreto (cantidades y plan afectado).</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción del motivo (formato con {0}, {1}…) y sus argumentos: la
        /// pantalla y el PDF lo muestran en el idioma activo; Motivo queda como texto en español.</summary>
        public string ClaveMotivo { get; set; }
        public object[] ArgsMotivo { get; set; }

        public TipoDescuento TipoSugerido { get; set; }

        /// <summary>Clientes del plan en riesgo de abandono y el ingreso mensual que representan
        /// (clientes × precio del plan).</summary>
        public int ClientesEnRiesgo { get; set; }
        public decimal IngresoMensualEnRiesgo { get; set; }

        /// <summary>Tasa de retención esperada (%) con la que se estima el beneficio; Gerencia la
        /// ajusta antes de enviar la sugerencia.</summary>
        public decimal TasaRetencion { get; set; } = SugerenciaPromocion.TasaRetencionPorDefecto;

        /// <summary>Beneficio estimado: ingreso mensual en riesgo × tasa de retención / 100.</summary>
        public decimal BeneficioEstimado => SugerenciaPromocion.CalcularBeneficio(IngresoMensualEnRiesgo, TasaRetencion);
    }
}
