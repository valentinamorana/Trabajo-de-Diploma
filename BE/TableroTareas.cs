using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Tareas pendientes del Vendedor según los diagramas de actividad de PN01 y PN02:
    /// pedidos esperando a Depósito (informativo), pedidos con faltantes para comunicar al
    /// cliente, pedidos separados para formalizar y contrataciones esperando el cobro de Caja.
    /// </summary>
    public class TableroVendedor
    {
        public List<Pedido> EnControlStock { get; set; } = new List<Pedido>();
        public List<Pedido> ConFaltantes   { get; set; } = new List<Pedido>();
        public List<Pedido> Separados      { get; set; } = new List<Pedido>();
        public int ContratacionesEnCaja    { get; set; }

        /// <summary>Lo que el Vendedor tiene que atender él: comunicar faltantes y formalizar.</summary>
        public int PedidosParaAtender => ConFaltantes.Count + Separados.Count;
    }
}
