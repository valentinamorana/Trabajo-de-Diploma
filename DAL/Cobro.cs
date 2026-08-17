using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de la tabla [HistorialCobro] (PdN6).</summary>
    public class Cobro : BaseDAL<BE.Cobro>, Interfaces.ICobroDAL
    {
        private const string SELECT_BASE =
            "SELECT c.IdCobro, c.IdCliente, c.Importe, " +
            "       c.FechaDeteccion, c.FechaResolucion, c.Resultado, c.Actor, " +
            "       cl.Nombre + ' ' + cl.Apellido AS NombreCliente " +
            "FROM HistorialCobro c " +
            "INNER JOIN Cliente cl ON cl.IdCliente = c.IdCliente";

        public override List<BE.Cobro> ObtenerTodos()
        {
            var lista = new List<BE.Cobro>();
            DataTable tabla = acceso.Leer(SELECT_BASE + " ORDER BY c.FechaDeteccion DESC", null);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        public override BE.Cobro ObtenerPorId(int id)
        {
            SqlParameter[] p = { new SqlParameter("@Id", id) };
            DataTable tabla = acceso.Leer(SELECT_BASE + " WHERE c.IdCobro = @Id", p);
            return tabla != null && tabla.Rows.Count > 0 ? Mapear(tabla.Rows[0]) : null;
        }

        public List<BE.Cobro> ObtenerPorCliente(int idCliente)
        {
            var lista = new List<BE.Cobro>();
            SqlParameter[] p = { new SqlParameter("@IdCliente", idCliente) };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE c.IdCliente = @IdCliente ORDER BY c.FechaDeteccion DESC", p);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        // Inserta el intento de cobro con su resultado ya definido (los tres manejadores
        // que persisten — Cobrado/Gracia/Suspendido — resuelven en el mismo paso en que
        // detectan el caso, no hay un estado "Pendiente" intermedio que requiera un
        // UPDATE posterior). FechaResolucion se completa acá directamente para evitar
        // un segundo round-trip a la base solo para timestampear.
        public int Alta(BE.Cobro cobro)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdCliente",      cobro.IdCliente),
                new SqlParameter("@Importe",         cobro.Importe),
                new SqlParameter("@FechaDeteccion",  cobro.FechaDeteccion),
                new SqlParameter("@FechaResolucion", (object)cobro.FechaResolucion ?? DBNull.Value),
                new SqlParameter("@Resultado",       (int)cobro.Resultado),
                new SqlParameter("@Actor",           (object)cobro.Actor ?? DBNull.Value)
            };

            DataTable tabla = acceso.Leer(
                "INSERT INTO HistorialCobro " +
                "(IdCliente, Importe, FechaDeteccion, FechaResolucion, Resultado, Actor) " +
                "VALUES (@IdCliente, @Importe, @FechaDeteccion, @FechaResolucion, @Resultado, @Actor); " +
                "SELECT SCOPE_IDENTITY() AS IdNuevo",
                p);

            return tabla != null && tabla.Rows.Count > 0
                ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"])
                : 0;
        }

        // Igual que Alta, pero sobre una transacción ya abierta por el caller — usada por los
        // manejadores de Cobro para que el UPDATE de Cliente y este INSERT sean atómicos
        // (ver DAL.Cliente.EjecutarTransaccion/ModificarEnTx).
        public int AltaEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cobro cobro)
        {
            using (var cmd = new SqlCommand(
                "INSERT INTO HistorialCobro " +
                "(IdCliente, Importe, FechaDeteccion, FechaResolucion, Resultado, Actor) " +
                "VALUES (@IdCliente, @Importe, @FechaDeteccion, @FechaResolucion, @Resultado, @Actor); " +
                "SELECT SCOPE_IDENTITY();",
                conexion, tx))
            {
                cmd.Parameters.AddWithValue("@IdCliente", cobro.IdCliente);
                cmd.Parameters.AddWithValue("@Importe", cobro.Importe);
                cmd.Parameters.AddWithValue("@FechaDeteccion", cobro.FechaDeteccion);
                cmd.Parameters.AddWithValue("@FechaResolucion", (object)cobro.FechaResolucion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Resultado", (int)cobro.Resultado);
                cmd.Parameters.AddWithValue("@Actor", (object)cobro.Actor ?? DBNull.Value);

                var resultadoId = cmd.ExecuteScalar();
                return resultadoId == null || resultadoId == DBNull.Value ? 0 : Convert.ToInt32(resultadoId);
            }
        }

        private BE.Cobro Mapear(DataRow row)
        {
            return new BE.Cobro
            {
                IdCobro         = Convert.ToInt32(row["IdCobro"]),
                IdCliente       = Convert.ToInt32(row["IdCliente"]),
                NombreCliente   = row["NombreCliente"].ToString(),
                Importe         = Convert.ToDecimal(row["Importe"]),
                FechaDeteccion  = Convert.ToDateTime(row["FechaDeteccion"]),
                FechaResolucion = row["FechaResolucion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaResolucion"]) : null,
                Resultado       = (BE.EstadoCobro)Convert.ToInt32(row["Resultado"]),
                Actor           = row["Actor"] != DBNull.Value ? row["Actor"].ToString() : null
            };
        }
    }
}
