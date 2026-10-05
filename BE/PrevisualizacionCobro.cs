namespace BE
{
    /// <summary>
    /// PdN6 — Lo que el próximo cobro de un cliente va a sumar por cargos de daño/pérdida
    /// pendientes, calculado por BLL.Cobro.PrevisualizarCobro para mostrarlo ANTES de
    /// procesar el cobro (la pantalla solo lo muestra, no suma nada).
    /// </summary>
    public class PrevisualizacionCobro
    {
        public int     CantidadCargosPendientes { get; set; }
        public decimal TotalCargosPendientes    { get; set; }

        public bool TieneCargosPendientes => CantidadCargosPendientes > 0;
    }
}
