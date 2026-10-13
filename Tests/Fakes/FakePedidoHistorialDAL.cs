using System;
using System.Collections.Generic;
using System.Data;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IPedidoHistorialDAL (sin base de datos). El historial se escribe
    /// junto con cada cambio del pedido: lo espía FakePedidoDAL (HistorialRegistradoVeces / UltimoHistorial).</summary>
    public class FakePedidoHistorialDAL : IPedidoHistorialDAL
    {
        public List<BE.PedidoHistorial> CambiosParaOperacion { get; set; } = new List<BE.PedidoHistorial>();
        // IdOperacion de la última operación del pedido (0 = sin historial).
        public int UltimaIdOperacion { get; set; }
        public int ObtenerUltimaIdOperacion(int idPedido) => UltimaIdOperacion;
        public DataTable ObtenerPorPedido(int idPedido, string accion = null, DateTime? desde = null, DateTime? hasta = null) => new DataTable();
        public List<BE.PedidoHistorial> ObtenerPorOperacion(int idPedido, int idOperacion) => CambiosParaOperacion;
    }
}
