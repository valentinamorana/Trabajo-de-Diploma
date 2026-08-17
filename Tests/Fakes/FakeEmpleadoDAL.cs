using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IEmpleadoDAL (sin base de datos).</summary>
    public class FakeEmpleadoDAL : IEmpleadoDAL
    {
        public BE.Empleado EmpleadoPorUsuario { get; set; }

        public List<BE.Empleado> ObtenerTodos() => new List<BE.Empleado>();
        public BE.Empleado ObtenerPorId(int idEmpleado) => null;
        public BE.Empleado ObtenerPorUsuario(int idUsuario) => EmpleadoPorUsuario;
        public bool ExisteDNI(string dni) => false;
        public int Alta(BE.Empleado empleado) => 0;
        public void Modificar(BE.Empleado empleado) { }
    }
}
