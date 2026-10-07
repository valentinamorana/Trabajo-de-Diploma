namespace BE
{
    /// <summary>
    /// PdN6 — El próximo cobro de un cliente, calculado por BLL.Cobro.PrevisualizarCobro para
    /// mostrarlo ANTES de procesarlo: importe del período, el descuento que corresponde, los
    /// cargos por daño/pérdida pendientes y el total (la pantalla solo lo muestra, no suma nada).
    /// </summary>
    public class PrevisualizacionCobro
    {
        public int     CantidadCargosPendientes { get; set; }
        public decimal TotalCargosPendientes    { get; set; }

        public bool TieneCargosPendientes => CantidadCargosPendientes > 0;

        /// <summary>Precio del plan × meses de la modalidad (0 si el cliente no tiene plan).</summary>
        public decimal Bruto { get; set; }
        /// <summary>Único descuento del ciclo: promoción vigente o crédito por referido.</summary>
        public decimal Descuento { get; set; }
        public string NombrePromocion { get; set; }
        public bool UsaCreditoReferido { get; set; }

        /// <summary>Lo que se va a cobrar: período con descuento + cargos pendientes.</summary>
        public decimal Total => System.Math.Max(0, Bruto - Descuento) + TotalCargosPendientes;
    }
}
