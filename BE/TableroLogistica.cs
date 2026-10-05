using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Tablero del Operador Logístico: pedidos YA FORMALIZADOS en cada etapa del despacho
    /// (pendiente de despacho → despachado → entregado). Lo arma BLL.PanelTareas; el panel
    /// solo lo muestra. Los pedidos en armado (control de stock, faltantes, separados) no
    /// son trabajo de logística y no aparecen.
    /// </summary>
    public class TableroLogistica
    {
        public List<Pedido> Pendientes  { get; set; } = new List<Pedido>();
        public List<Pedido> Despachados { get; set; } = new List<Pedido>();
        public List<Pedido> Entregados  { get; set; } = new List<Pedido>();

        public int CantidadPendientes  => Pendientes.Count;
        public int CantidadDespachados => Despachados.Count;
        public int CantidadEntregados  => Entregados.Count;
    }
}
