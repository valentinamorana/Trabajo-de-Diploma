using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de ICargoPrendaDAL (sin base de datos). En memoria, con espías.</summary>
    public class FakeCargoPrendaDAL : ICargoPrendaDAL
    {
        public readonly List<BE.CargoPrenda> Registros = new List<BE.CargoPrenda>();

        public int MarcarCobradosVeces { get; private set; }

        public int Alta(BE.CargoPrenda cargo)
        {
            cargo.IdCargo = Registros.Count + 1;
            cargo.Estado = BE.EstadoCargo.Pendiente;
            Registros.Add(cargo);
            return cargo.IdCargo;
        }

        public List<BE.CargoPrenda> ObtenerPendientesPorCliente(int idCliente) =>
            Registros.FindAll(c => c.IdCliente == idCliente && c.Estado == BE.EstadoCargo.Pendiente);

        public void MarcarCobradosEnTx(SqlConnection conexion, SqlTransaction tx, List<int> idsCargo, DateTime fechaCobro)
        {
            MarcarCobradosVeces++;
            foreach (var c in Registros.Where(c => idsCargo.Contains(c.IdCargo)))
            {
                c.Estado = BE.EstadoCargo.Cobrado;
                c.FechaCobro = fechaCobro;
            }
        }

        public List<BE.CargoPrenda> ObtenerTodos() => Registros;
    }
}
