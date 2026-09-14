using System;
using System.Data;

namespace BLL.Interfaces
{
    /// <summary>
    /// Consultas de auditoría (Bitácora del sistema y de negocio).
    /// </summary>
    public interface IBitacoraService
    {
        void RegistrarSinSesion(string modulo, string actividad, BE.Criticidad criticidad,
                                 int? idUsuario = null, string detalle = null);

        DataTable ObtenerTodosSistema();

        DataTable ObtenerUltimosNDiasSistema(int dias);

        DataTable BuscarPorFiltrosSistema(
            DateTime? desde, DateTime? hasta,
            int idUsuario, string actividad, int criticidad);

        DataTable ObtenerTodosNegocio();

        DataTable BuscarPorFiltrosNegocio(
            DateTime? desde, DateTime? hasta,
            string tipo, int? idCliente, int? idPedido);

        bool UsuarioPuedeVerSistema();
    }
}
