namespace BE
{
    /// <summary>
    /// PN03 — Idea de promoción detectada a partir de los reportes del negocio (rotación, abandono).
    /// Es una PROPUESTA de datos para que Gerencia arme su sugerencia con un dato concreto; no se
    /// persiste: Gerencia la revisa, la ajusta y recién ahí se crea la SugerenciaPromocion.
    /// </summary>
    public class CandidataSugerencia
    {
        /// <summary>Reporte del que sale el dato: "Rotación" o "Abandono".</summary>
        public string Origen { get; set; }

        public int? IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public string CategoriaPrenda { get; set; }

        /// <summary>Motivo redactado con el dato concreto (cantidades, categoría o plan afectado).</summary>
        public string Motivo { get; set; }

        public TipoDescuento TipoSugerido { get; set; }

        /// <summary>Estimación inicial editable: ingreso mensual en riesgo (abandono) o un valor de
        /// referencia por prenda parada (rotación). Gerencia la ajusta antes de enviar.</summary>
        public decimal BeneficioEstimado { get; set; }

        /// <summary>Texto corto para listar la candidata.</summary>
        public string Resumen => IdPlan.HasValue
            ? $"[{Origen}] Plan {NombrePlan}"
            : $"[{Origen}] Categoría {CategoriaPrenda}";
    }
}
