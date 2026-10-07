using System.Collections.Generic;
using System.Data.SqlClient;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IRenovacionDAL (sin base de datos). En memoria, con espías.</summary>
    public class FakeRenovacionDAL : IRenovacionDAL
    {
        public readonly List<BE.Renovacion> Registros = new List<BE.Renovacion>();

        public int AltaVeces { get; private set; }

        public int Alta(BE.Renovacion renovacion)
        {
            AltaVeces++;
            renovacion.IdRenovacion = Registros.Count + 1;
            Registros.Add(renovacion);
            return renovacion.IdRenovacion;
        }

        public int AltaEnTx(SqlConnection conexion, SqlTransaction tx, BE.Renovacion renovacion) => Alta(renovacion);

        public List<BE.Renovacion> ObtenerPorCliente(int idCliente) => Registros.FindAll(r => r.IdCliente == idCliente);
        public List<BE.Renovacion> ObtenerTodos() => Registros;
    }
}
