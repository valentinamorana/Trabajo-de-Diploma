using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IMantenimientoPrendaDAL (sin base de datos).</summary>
    public class FakeMantenimientoPrendaDAL : IMantenimientoPrendaDAL
    {
        public List<BE.MantenimientoPrenda> Todos { get; set; } = new List<BE.MantenimientoPrenda>();

        public int IniciarMantenimientoVeces { get; private set; }
        public int CerrarMantenimientoVeces { get; private set; }

        public void IniciarMantenimiento(int idPrenda, string actor) => IniciarMantenimientoVeces++;
        public void CerrarMantenimiento(int idPrenda) => CerrarMantenimientoVeces++;
        public List<BE.MantenimientoPrenda> ObtenerPorPrenda(int idPrenda) => Todos.FindAll(m => m.IdPrenda == idPrenda);
        public List<BE.MantenimientoPrenda> ObtenerTodos() => Todos;
        public BE.MantenimientoPrenda ObtenerPorId(int id) => Todos.Find(m => m.IdMantenimiento == id);
    }
}
