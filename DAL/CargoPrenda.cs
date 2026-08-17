using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de la tabla [CargoPrenda] (cargo por daño/pérdida — Bloque 1).</summary>
    public class CargoPrenda : BaseDAL<BE.CargoPrenda>, Interfaces.ICargoPrendaDAL
    {
        private const string SELECT_BASE =
            "SELECT g.IdCargo, g.IdPrenda, p.Nombre AS NombrePrenda, g.IdCliente, " +
            "       cl.Nombre + ' ' + cl.Apellido AS NombreCliente, " +
            "       g.Motivo, g.Monto, g.FechaRegistro, g.FechaCobro, g.Actor, g.Estado " +
            "FROM CargoPrenda g " +
            "INNER JOIN Prenda p  ON p.IdPrenda  = g.IdPrenda " +
            "INNER JOIN Cliente cl ON cl.IdCliente = g.IdCliente";

        public override List<BE.CargoPrenda> ObtenerTodos()
        {
            var lista = new List<BE.CargoPrenda>();
            DataTable tabla = acceso.Leer(SELECT_BASE + " ORDER BY g.FechaRegistro DESC", null);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        public override BE.CargoPrenda ObtenerPorId(int id)
        {
            SqlParameter[] p = { new SqlParameter("@Id", id) };
            DataTable tabla = acceso.Leer(SELECT_BASE + " WHERE g.IdCargo = @Id", p);
            return tabla != null && tabla.Rows.Count > 0 ? Mapear(tabla.Rows[0]) : null;
        }

        public List<BE.CargoPrenda> ObtenerPendientesPorCliente(int idCliente)
        {
            var lista = new List<BE.CargoPrenda>();
            SqlParameter[] p =
            {
                new SqlParameter("@IdCliente", idCliente),
                new SqlParameter("@Estado", (int)BE.EstadoCargo.Pendiente)
            };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE g.IdCliente = @IdCliente AND g.Estado = @Estado " +
                "ORDER BY g.FechaRegistro", p);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        public int Alta(BE.CargoPrenda cargo)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPrenda",      cargo.IdPrenda),
                new SqlParameter("@IdCliente",     cargo.IdCliente),
                new SqlParameter("@Motivo",        cargo.Motivo),
                new SqlParameter("@Monto",         cargo.Monto),
                new SqlParameter("@FechaRegistro", cargo.FechaRegistro),
                new SqlParameter("@Actor",         (object)cargo.Actor ?? DBNull.Value),
                new SqlParameter("@Estado",        (int)BE.EstadoCargo.Pendiente)
            };

            DataTable tabla = acceso.Leer(
                "INSERT INTO CargoPrenda (IdPrenda, IdCliente, Motivo, Monto, FechaRegistro, Actor, Estado) " +
                "VALUES (@IdPrenda, @IdCliente, @Motivo, @Monto, @FechaRegistro, @Actor, @Estado); " +
                "SELECT SCOPE_IDENTITY() AS IdNuevo",
                p);

            return tabla != null && tabla.Rows.Count > 0
                ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"])
                : 0;
        }

        // Igual que MarcarCobrado individual, pero para todos los cargos Pendientes que
        // ProcesarPagoHandler suma a un mismo cobro — sobre la transacción ya abierta por
        // DAL.Cliente.EjecutarTransaccion, junto con el INSERT de HistorialCobro (ver DAL.Cobro.AltaEnTx).
        public void MarcarCobradosEnTx(SqlConnection conexion, SqlTransaction tx, List<int> idsCargo, DateTime fechaCobro)
        {
            if (idsCargo == null || idsCargo.Count == 0) return;

            foreach (var idCargo in idsCargo)
            {
                using (var cmd = new SqlCommand(
                    "UPDATE CargoPrenda SET Estado=@Estado, FechaCobro=@FechaCobro WHERE IdCargo=@IdCargo",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Estado", (int)BE.EstadoCargo.Cobrado);
                    cmd.Parameters.AddWithValue("@FechaCobro", fechaCobro);
                    cmd.Parameters.AddWithValue("@IdCargo", idCargo);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private BE.CargoPrenda Mapear(DataRow row)
        {
            return new BE.CargoPrenda
            {
                IdCargo       = Convert.ToInt32(row["IdCargo"]),
                IdPrenda      = Convert.ToInt32(row["IdPrenda"]),
                NombrePrenda  = row["NombrePrenda"].ToString(),
                IdCliente     = Convert.ToInt32(row["IdCliente"]),
                NombreCliente = row["NombreCliente"].ToString(),
                Motivo        = row["Motivo"].ToString(),
                Monto         = Convert.ToDecimal(row["Monto"]),
                FechaRegistro = Convert.ToDateTime(row["FechaRegistro"]),
                FechaCobro    = row["FechaCobro"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaCobro"]) : null,
                Actor         = row["Actor"] != DBNull.Value ? row["Actor"].ToString() : null,
                Estado        = (BE.EstadoCargo)Convert.ToInt32(row["Estado"])
            };
        }
    }
}
