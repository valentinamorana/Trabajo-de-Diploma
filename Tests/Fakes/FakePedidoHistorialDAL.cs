using System;
using System.Collections.Generic;
using System.Data;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IPedidoHistorialDAL (sin base de datos).</summary>
    public class FakePedidoHistorialDAL : IPedidoHistorialDAL
    {
        public List<BE.PedidoHistorial> CambiosParaOperacion { get; set; } = new List<BE.PedidoHistorial>();
        public int RegistrarCambiosVeces { get; private set; }

        public void RegistrarCambios(List<BE.PedidoHistorial> cambios) => RegistrarCambiosVeces++;
        public int ObtenerSiguienteIdOperacion(int idPedido) => 1;
        public DataTable ObtenerPorPedido(int idPedido, string accion = null, DateTime? desde = null, DateTime? hasta = null) => new DataTable();
        public List<BE.PedidoHistorial> ObtenerPorOperacion(int idPedido, int idOperacion) => CambiosParaOperacion;
    }
}
