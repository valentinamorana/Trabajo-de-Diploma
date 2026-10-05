namespace BE
{
    /// <summary>
    /// PN01 — Resultado de la decisión "¿Selección disponible?" de la planilla de control de
    /// stock y de qué acciones del diagrama de actividad quedan habilitadas para Depósito.
    /// Lo calcula BLL.EvaluacionControlStock; la GUI solo lo aplica a sus botones.
    /// </summary>
    public class EvaluacionControlStock
    {
        /// <summary>Todas las prendas del pedido están disponibles (y hay al menos una).</summary>
        public bool SeleccionDisponible     { get; set; }

        /// <summary>No → "Informe de prendas faltantes".</summary>
        public bool PuedeInformarFaltantes  { get; set; }

        /// <summary>Sí, todavía sin confirmar → "Confirmar prendas disponibles".</summary>
        public bool PuedeConfirmar          { get; set; }

        /// <summary>Sí y ya confirmadas → "Separar prendas del pedido".</summary>
        public bool PuedeSeparar            { get; set; }
    }
}
