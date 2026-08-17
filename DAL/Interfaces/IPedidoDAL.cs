using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Pedido (permite inyección y dobles de prueba).</summary>
    public interface IPedidoDAL
    {
        List<BE.Pedido> ObtenerTodos();
        List<BE.Pedido> ObtenerPendientes();
        Dictionary<int, DateTime> ObtenerFechaUltimoPedidoPorCliente();
        List<BE.DesempenoVendedor> ObtenerEstadisticasPorEmpleado();
        Dictionary<int, int> ObtenerCantidadPedidosPorPrenda();
        List<BE.Prenda> ObtenerPrendasHistoricasPorCliente(int idCliente);
        BE.Pedido ObtenerPorId(int idPedido);
        int Alta(BE.Pedido pedido);
        void Despachar(int idPedido);
        void MarcarEntregado(int idPedido);
        int RegistrarDevolucion(int idPedido, int idCliente);
        void ReconciliarPrendasConEstado(int idPedido);
        void RestaurarOperacionAtomica(int idPedido, IList<(string Campo, string ValorAnterior)> campos);
        void Cancelar(int idPedido, string motivo);
        bool DesCancelar(int idPedido, int idCliente);
        List<BE.FilaDV> ObtenerFilasDV();
        void RecalcularDV();
    }
}
