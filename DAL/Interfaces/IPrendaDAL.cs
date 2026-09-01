using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Prenda (permite inyección y dobles de prueba).</summary>
    public interface IPrendaDAL
    {
        List<BE.Prenda> ObtenerTodos();

        /// <summary>
        /// Prendas Disponible. Si se indica <paramref name="idClienteSolicitante"/>, excluye las
        /// que estén Reservadas por Lista de Espera para OTRO cliente (mejora opcional) — ese
        /// cliente sigue viéndolas. Sin cliente en contexto, se excluyen todas las reservas activas.
        /// </summary>
        List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null);
        BE.Prenda ObtenerPorId(int idPrenda);
        List<BE.Prenda> ObtenerPorCliente(int idCliente);
        int Alta(BE.Prenda prenda);
        void Modificar(BE.Prenda prenda);
        void CambiarEstado(int idPrenda, BE.EstadoPrenda estadoAnterior, BE.EstadoPrenda nuevoEstado, int? idClienteActual = null);
        List<BE.StockPorTalleCategoria> ObtenerConteoDisponiblesPorTalleCategoria();
    }
}
