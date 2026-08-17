using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IPrendaDAL (sin base de datos).</summary>
    public class FakePrendaDAL : IPrendaDAL
    {
        public List<BE.Prenda> Todas { get; set; } = new List<BE.Prenda>();
        public List<BE.Prenda> Disponibles { get; set; } = new List<BE.Prenda>();
        public List<BE.StockPorTalleCategoria> ConteoDisponiblesPorTalleCategoria { get; set; } = new List<BE.StockPorTalleCategoria>();

        public int CambiarEstadoVeces { get; private set; }

        public List<BE.Prenda> ObtenerTodos() => Todas;
        public List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null) => Disponibles;
        public BE.Prenda ObtenerPorId(int idPrenda) => Todas.Find(p => p.IdPrenda == idPrenda);
        public List<BE.Prenda> ObtenerPorCliente(int idCliente) => new List<BE.Prenda>();
        public int Alta(BE.Prenda prenda) => 0;
        public void Modificar(BE.Prenda prenda) { }
        public void CambiarEstado(int idPrenda, BE.EstadoPrenda nuevoEstado, int? idClienteActual = null) => CambiarEstadoVeces++;
        public List<BE.StockPorTalleCategoria> ObtenerConteoDisponiblesPorTalleCategoria() => ConteoDisponiblesPorTalleCategoria;
    }
}
