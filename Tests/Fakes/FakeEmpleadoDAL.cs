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

        public readonly List<(int IdUsuario, string Puesto)> Creados = new List<(int, string)>();
        public int CrearParaUsuario(int idUsuario, string nombre, string apellido, string email, string puesto)
        {
            Creados.Add((idUsuario, puesto));
            EmpleadoPorUsuario = new BE.Empleado { IdEmpleado = 50 + idUsuario, IdUsuario = idUsuario, Puesto = puesto };
            return EmpleadoPorUsuario.IdEmpleado;
        }

        // Replica la regla del DAL: los datos vacíos del usuario no pisan los del empleado.
        public readonly List<(int IdUsuario, string Nombre, string Apellido, string Email)> Sincronizados
            = new List<(int, string, string, string)>();
        public int SincronizarDatosPersonales(int idUsuario, string nombre, string apellido, string email)
        {
            Sincronizados.Add((idUsuario, nombre, apellido, email));
            var e = EmpleadoPorUsuario;
            if (e == null || e.IdUsuario != idUsuario) return 0;
            if (!string.IsNullOrWhiteSpace(nombre))   e.Nombre   = nombre.Trim();
            if (!string.IsNullOrWhiteSpace(apellido)) e.Apellido = apellido.Trim();
            if (!string.IsNullOrWhiteSpace(email))    e.Email    = email.Trim();
            return e.IdEmpleado;
        }
    }
}
