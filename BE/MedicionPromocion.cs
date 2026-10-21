namespace BE
{
    /// <summary>Veredicto automático sobre una promoción medida (pestaña "Medir resultado").</summary>
    public enum VeredictoPromocion
    {
        /// <summary>No hay datos suficientes para opinar (sin clientes en juego o sin base).</summary>
        SinDatos = 0,
        Mantener = 1,
        Extender = 2,
        DarDeBaja = 3
    }

    /// <summary>
    /// PN03 «Medir resultado» — El «Resultado de la promoción» (BE.ResultadoPromocion, de
    /// BLL.AnalisisPromociones) más su costo real, el beneficio real atribuible, el ROI real contra el
    /// estimado y un veredicto (Mantener / Extender / Dar de baja) que se puede ejecutar con un botón.
    /// </summary>
    public class MedicionPromocion
    {
        public ResultadoPromocion Resultado { get; set; }
        public Promocion Promocion { get; set; }
        public decimal PrecioPlan { get; set; }
        /// <summary>Descontado en los cobros de la vigencia (costo real de la promoción).</summary>
        public decimal TotalDescontado { get; set; }
        public int Cobros { get; set; }
        /// <summary>Beneficio que estimó Gerencia (ingreso en riesgo × retención esperada × horizonte); null en un alta manual.</summary>
        public decimal? BeneficioEstimado { get; set; }
        /// <summary>Clientes retenidos atribuibles a la promoción (en juego × mejora de retención).</summary>
        public decimal? RetenidosAtribuibles { get; set; }
        public decimal? BeneficioReal { get; set; }
        public decimal? RoiReal { get; set; }
        public decimal? RoiEstimado { get; set; }
        public VeredictoPromocion Veredicto { get; set; }
        public TextoTraducible Explicacion { get; set; }

        public int IdPromocion => Resultado?.IdPromocion ?? Promocion?.IdPromocion ?? 0;
        public string Nombre => Resultado?.Nombre ?? Promocion?.Nombre;
    }
}
