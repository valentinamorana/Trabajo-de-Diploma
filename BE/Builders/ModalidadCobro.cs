namespace BE.Builders
{
    /// <summary>PdN1 — Periodicidad de cobro elegida al activar una suscripción.</summary>
    public enum ModalidadCobro
    {
        Mensual = 0,
        Trimestral = 1,
        Anual = 2
    }

    public static class ModalidadCobroExtensiones
    {
        /// <summary>
        /// Meses que cubre cada cobro. NUULY cobra por mes (USD 98/mes): PlanSuscripcion.Precio es el
        /// precio de UN mes y el importe de un cobro es Precio × Meses. Sin descuento por modalidad:
        /// los descuentos salen solo de las promociones (PN03).
        /// </summary>
        public static int Meses(this ModalidadCobro modalidad)
        {
            switch (modalidad)
            {
                case ModalidadCobro.Trimestral: return 3;
                case ModalidadCobro.Anual: return 12;
                default: return 1;
            }
        }
    }
}
