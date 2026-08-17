using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de la tabla [HistorialRenovacion] (PdN5).</summary>
    public class Renovacion : BaseDAL<BE.Renovacion>, Interfaces.IRenovacionDAL
    {
        private const string SELECT_BASE =
            "SELECT r.IdRenovacion, r.IdCliente, r.IdPlanAnterior, r.IdPlanNuevo, " +
            "       r.FechaDeteccion, r.FechaResolucion, r.Resultado, r.Actor, " +
            "       c.Nombre + ' ' + c.Apellido AS NombreCliente " +
            "FROM HistorialRenovacion r " +
            "INNER JOIN Cliente c ON c.IdCliente = r.IdCliente";

        public override List<BE.Renovacion> ObtenerTodos()
        {
            var lista = new List<BE.Renovacion>();
            DataTable tabla = acceso.Leer(SELECT_BASE + " ORDER BY r.FechaDeteccion DESC", null);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        public override BE.Renovacion ObtenerPorId(int id)
        {
            SqlParameter[] p = { new SqlParameter("@Id", id) };
            DataTable tabla = acceso.Leer(SELECT_BASE + " WHERE r.IdRenovacion = @Id", p);
            return tabla != null && tabla.Rows.Count > 0 ? Mapear(tabla.Rows[0]) : null;
        }

        public List<BE.Renovacion> ObtenerPorCliente(int idCliente)
        {
            var lista = new List<BE.Renovacion>();
            SqlParameter[] p = { new SqlParameter("@IdCliente", idCliente) };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE r.IdCliente = @IdCliente ORDER BY r.FechaDeteccion DESC", p);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        // Inserta el intento de renovación con su resultado ya definido (los tres
        // manejadores que persisten — Renovar/CambioPlan/Baja — resuelven en el mismo
        // paso en que detectan el caso, no hay un estado "Pendiente" intermedio que
        // requiera un UPDATE posterior). FechaResolucion se completa acá directamente
        // para evitar un segundo round-trip a la base solo para timestampear.
        public int Alta(BE.Renovacion renovacion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdCliente",       renovacion.IdCliente),
                new SqlParameter("@IdPlanAnterior",   (object)renovacion.IdPlanAnterior ?? DBNull.Value),
                new SqlParameter("@IdPlanNuevo",      (object)renovacion.IdPlanNuevo    ?? DBNull.Value),
                new SqlParameter("@FechaDeteccion",   renovacion.FechaDeteccion),
                new SqlParameter("@FechaResolucion",  (object)renovacion.FechaResolucion ?? DBNull.Value),
                new SqlParameter("@Resultado",        (int)renovacion.Resultado),
                new SqlParameter("@Actor",            (object)renovacion.Actor ?? DBNull.Value)
            };

            DataTable tabla = acceso.Leer(
                "INSERT INTO HistorialRenovacion " +
                "(IdCliente, IdPlanAnterior, IdPlanNuevo, FechaDeteccion, FechaResolucion, Resultado, Actor) " +
                "VALUES (@IdCliente, @IdPlanAnterior, @IdPlanNuevo, @FechaDeteccion, @FechaResolucion, @Resultado, @Actor); " +
                "SELECT SCOPE_IDENTITY() AS IdNuevo",
                p);

            return tabla != null && tabla.Rows.Count > 0
                ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"])
                : 0;
        }

        // Igual que Alta, pero sobre una transacción ya abierta por el caller — usada por
        // los manejadores de Renovación para que el UPDATE de Cliente y este INSERT sean
        // atómicos (ver DAL.Cliente.EjecutarTransaccion/ModificarEnTx).
        public int AltaEnTx(SqlConnection conexion, SqlTransaction tx, BE.Renovacion renovacion)
        {
            using (var cmd = new SqlCommand(
                "INSERT INTO HistorialRenovacion " +
                "(IdCliente, IdPlanAnterior, IdPlanNuevo, FechaDeteccion, FechaResolucion, Resultado, Actor) " +
                "VALUES (@IdCliente, @IdPlanAnterior, @IdPlanNuevo, @FechaDeteccion, @FechaResolucion, @Resultado, @Actor); " +
                "SELECT SCOPE_IDENTITY();",
                conexion, tx))
            {
                cmd.Parameters.AddWithValue("@IdCliente", renovacion.IdCliente);
                cmd.Parameters.AddWithValue("@IdPlanAnterior", (object)renovacion.IdPlanAnterior ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IdPlanNuevo", (object)renovacion.IdPlanNuevo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaDeteccion", renovacion.FechaDeteccion);
                cmd.Parameters.AddWithValue("@FechaResolucion", (object)renovacion.FechaResolucion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Resultado", (int)renovacion.Resultado);
                cmd.Parameters.AddWithValue("@Actor", (object)renovacion.Actor ?? DBNull.Value);

                var resultadoId = cmd.ExecuteScalar();
                return resultadoId == null || resultadoId == DBNull.Value ? 0 : Convert.ToInt32(resultadoId);
            }
        }

        public void Resolver(int idRenovacion, BE.EstadoRenovacion resultado, int? idPlanNuevo)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Resultado",       (int)resultado),
                new SqlParameter("@IdPlanNuevo",      (object)idPlanNuevo ?? DBNull.Value),
                new SqlParameter("@FechaResolucion",  DateTime.Now),
                new SqlParameter("@IdRenovacion",     idRenovacion)
            };
            acceso.Escribir(
                "UPDATE HistorialRenovacion SET Resultado=@Resultado, IdPlanNuevo=@IdPlanNuevo, " +
                "FechaResolucion=@FechaResolucion WHERE IdRenovacion=@IdRenovacion",
                p);
        }

        private BE.Renovacion Mapear(DataRow row)
        {
            return new BE.Renovacion
            {
                IdRenovacion    = Convert.ToInt32(row["IdRenovacion"]),
                IdCliente       = Convert.ToInt32(row["IdCliente"]),
                NombreCliente   = row["NombreCliente"].ToString(),
                IdPlanAnterior  = row["IdPlanAnterior"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdPlanAnterior"]) : null,
                IdPlanNuevo     = row["IdPlanNuevo"]    != DBNull.Value ? (int?)Convert.ToInt32(row["IdPlanNuevo"])    : null,
                FechaDeteccion  = Convert.ToDateTime(row["FechaDeteccion"]),
                FechaResolucion = row["FechaResolucion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaResolucion"]) : null,
                Resultado       = (BE.EstadoRenovacion)Convert.ToInt32(row["Resultado"]),
                Actor           = row["Actor"] != DBNull.Value ? row["Actor"].ToString() : null
            };
        }
    }
}
