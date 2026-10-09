using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Empleado (permite inyección y dobles de prueba).</summary>
    public interface IEmpleadoDAL
    {
        List<BE.Empleado> ObtenerTodos();
        BE.Empleado ObtenerPorId(int idEmpleado);
        BE.Empleado ObtenerPorUsuario(int idUsuario);

        // Crea el Empleado vinculado a un usuario del sistema (si no tiene uno) y devuelve su Id.
        int CrearParaUsuario(int idUsuario, string nombre, string apellido, string email, string puesto);
    }
}
