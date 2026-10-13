using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de MantenimientoPrenda (permite inyección y dobles de prueba).</summary>
    public interface IMantenimientoPrendaDAL
    {
        List<BE.MantenimientoPrenda> ObtenerPorPrenda(int idPrenda);
        List<BE.MantenimientoPrenda> ObtenerTodos();
        BE.MantenimientoPrenda ObtenerPorId(int id);
    }
}
