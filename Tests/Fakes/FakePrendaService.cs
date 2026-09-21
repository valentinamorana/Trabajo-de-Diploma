using System.Collections.Generic;
using BLL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IPrendaService (sin base de datos). ObtenerDisponibles devuelve
    /// Disponibles (configurable por test); el resto del contrato no lo ejercita
    /// ReporteJornada, cuerpos mínimos.
    /// </summary>
    public class FakePrendaService : IPrendaService
    {
        public List<BE.Prenda> Disponibles { get; set; } = new List<BE.Prenda>();

        public List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null) => Disponibles;

        public List<BE.Prenda> ObtenerTodos() => new List<BE.Prenda>();
        // Prendas EnUso del cliente (pendientes de devolución) que devuelve ObtenerPorCliente.
        public List<BE.Prenda> EnUsoPorCliente { get; set; } = new List<BE.Prenda>();
        public List<BE.Prenda> ObtenerPorCliente(int idCliente) => EnUsoPorCliente;
        public BE.Prenda ObtenerPorId(int idPrenda) => null;
        public void Alta(string modulo, BE.Prenda prenda) { }
        public void Modificar(string modulo, BE.Prenda prenda) { }
        public void CambiarEstado(string modulo, BE.Prenda prenda, BE.EstadoPrenda nuevoEstado, string actor = null, bool viaFlujoPerdida = false, bool viaInspeccion = false) { }
        public (bool Disponible, List<BE.Prenda> NoDisponibles) VerificarDisponibilidad(List<BE.Prenda> seleccion) => (true, new List<BE.Prenda>());
        public List<BE.MantenimientoPrenda> ObtenerHistorialMantenimiento(int idPrenda) => new List<BE.MantenimientoPrenda>();
        public List<BE.MantenimientoPrenda> ObtenerEnMantenimiento() => new List<BE.MantenimientoPrenda>();
        public BE.OcupacionStock ObtenerOcupacion() => new BE.OcupacionStock();
        public List<BE.Prenda> ObtenerEnLimpieza() => new List<BE.Prenda>();
    }
}
