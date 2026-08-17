using System;
using System.Collections.Generic;
using System.Data;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de PedidoHistorial (permite inyección y dobles de prueba).</summary>
    public interface IPedidoHistorialDAL
    {
        void RegistrarCambios(List<BE.PedidoHistorial> cambios);
        int ObtenerSiguienteIdOperacion(int idPedido);
        DataTable ObtenerPorPedido(int idPedido, string accion = null, DateTime? desde = null, DateTime? hasta = null);
        List<BE.PedidoHistorial> ObtenerPorOperacion(int idPedido, int idOperacion);
    }
}
