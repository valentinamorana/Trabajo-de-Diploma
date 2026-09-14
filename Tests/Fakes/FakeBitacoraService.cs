using System;
using System.Data;
using BLL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IBitacoraService (sin base de datos). BuscarPorFiltrosNegocio
    /// devuelve TablaNegocio (configurable por test) y cuenta invocaciones; el resto del
    /// contrato no lo ejercita ReporteJornada, cuerpos mínimos.
    /// </summary>
    public class FakeBitacoraService : IBitacoraService
    {
        public DataTable TablaNegocio { get; set; } = NuevaTablaNegocio();
        public int BuscarPorFiltrosNegocioVeces { get; private set; }

        public static DataTable NuevaTablaNegocio()
        {
            var dt = new DataTable();
            dt.Columns.Add("Tipo", typeof(string));
            dt.Columns.Add("Descripcion", typeof(string));
            dt.Columns.Add("Fecha", typeof(DateTime));
            dt.Columns.Add("UsernameUsuario", typeof(string));
            dt.Columns.Add("NombreCliente", typeof(string));
            return dt;
        }

        public DataTable BuscarPorFiltrosNegocio(DateTime? desde, DateTime? hasta, string tipo, int? idCliente, int? idPedido)
        {
            BuscarPorFiltrosNegocioVeces++;
            return TablaNegocio;
        }

        public void RegistrarSinSesion(string modulo, string actividad, BE.Criticidad criticidad, int? idUsuario = null, string detalle = null) { }
        public DataTable ObtenerTodosSistema() => NuevaTablaNegocio();
        public DataTable ObtenerUltimosNDiasSistema(int dias) => NuevaTablaNegocio();
        public DataTable BuscarPorFiltrosSistema(DateTime? desde, DateTime? hasta, int idUsuario, string actividad, int criticidad) => NuevaTablaNegocio();
        public DataTable ObtenerTodosNegocio() => NuevaTablaNegocio();
        public bool UsuarioPuedeVerSistema() => true;
    }
}
