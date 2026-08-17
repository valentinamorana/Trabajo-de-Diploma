using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de la tabla [ListaEspera] (mejora opcional, no requerida por la cátedra).</summary>
    public class ListaEspera : BaseDAL<BE.ListaEspera>, Interfaces.IListaEsperaDAL
    {
        private const string SELECT_BASE =
            "SELECT le.IdListaEspera, le.IdPrenda, le.IdCliente, le.FechaAlta, le.Estado, " +
            "       le.FechaLimiteReserva, le.FechaResolucion, le.Actor, " +
            "       p.Nombre AS NombrePrenda, " +
            "       c.Nombre + ' ' + c.Apellido AS NombreCliente " +
            "FROM ListaEspera le " +
            "INNER JOIN Prenda p  ON p.IdPrenda  = le.IdPrenda " +
            "INNER JOIN Cliente c ON c.IdCliente = le.IdCliente";

        public override List<BE.ListaEspera> ObtenerTodos()
        {
            var lista = new List<BE.ListaEspera>();
            DataTable tabla = acceso.Leer(SELECT_BASE + " ORDER BY le.FechaAlta DESC", null);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        public override BE.ListaEspera ObtenerPorId(int id)
        {
            SqlParameter[] p = { new SqlParameter("@Id", id) };
            DataTable tabla = acceso.Leer(SELECT_BASE + " WHERE le.IdListaEspera = @Id", p);
            return tabla != null && tabla.Rows.Count > 0 ? Mapear(tabla.Rows[0]) : null;
        }

        public List<BE.ListaEspera> ObtenerActivas()
        {
            var lista = new List<BE.ListaEspera>();
            SqlParameter[] p =
            {
                new SqlParameter("@Pendiente", (int)BE.EstadoListaEspera.Pendiente),
                new SqlParameter("@Reservada",  (int)BE.EstadoListaEspera.Reservada)
            };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE le.Estado IN (@Pendiente, @Reservada) ORDER BY le.FechaAlta", p);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        public List<BE.ListaEspera> ObtenerPorPrenda(int idPrenda)
        {
            var lista = new List<BE.ListaEspera>();
            SqlParameter[] p = { new SqlParameter("@IdPrenda", idPrenda) };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE le.IdPrenda = @IdPrenda ORDER BY le.FechaAlta", p);
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            return lista;
        }

        public BE.ListaEspera ObtenerPendienteMasAntigua(int idPrenda)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPrenda", idPrenda),
                new SqlParameter("@Pendiente", (int)BE.EstadoListaEspera.Pendiente)
            };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE le.IdPrenda = @IdPrenda AND le.Estado = @Pendiente " +
                "ORDER BY le.FechaAlta ASC", p);
            return tabla != null && tabla.Rows.Count > 0 ? Mapear(tabla.Rows[0]) : null;
        }

        public BE.ListaEspera ObtenerReservaVigenteDeOtro(int idPrenda, int idClienteSolicitante)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPrenda", idPrenda),
                new SqlParameter("@IdCliente", idClienteSolicitante),
                new SqlParameter("@Reservada", (int)BE.EstadoListaEspera.Reservada)
            };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE le.IdPrenda = @IdPrenda AND le.IdCliente <> @IdCliente " +
                "AND le.Estado = @Reservada AND le.FechaLimiteReserva > GETDATE()", p);
            return tabla != null && tabla.Rows.Count > 0 ? Mapear(tabla.Rows[0]) : null;
        }

        public BE.ListaEspera ObtenerReservaVigenteDeCliente(int idPrenda, int idCliente)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPrenda", idPrenda),
                new SqlParameter("@IdCliente", idCliente),
                new SqlParameter("@Reservada", (int)BE.EstadoListaEspera.Reservada)
            };
            DataTable tabla = acceso.Leer(
                SELECT_BASE + " WHERE le.IdPrenda = @IdPrenda AND le.IdCliente = @IdCliente " +
                "AND le.Estado = @Reservada AND le.FechaLimiteReserva > GETDATE()", p);
            return tabla != null && tabla.Rows.Count > 0 ? Mapear(tabla.Rows[0]) : null;
        }

        public int ContarReservadasVigentes()
        {
            SqlParameter[] p = { new SqlParameter("@Reservada", (int)BE.EstadoListaEspera.Reservada) };
            DataTable tabla = acceso.Leer(
                "SELECT COUNT(*) AS Cant FROM ListaEspera " +
                "WHERE Estado = @Reservada AND FechaLimiteReserva > GETDATE()", p);
            return tabla != null && tabla.Rows.Count > 0 ? Convert.ToInt32(tabla.Rows[0]["Cant"]) : 0;
        }

        public int Alta(BE.ListaEspera fila)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPrenda",  fila.IdPrenda),
                new SqlParameter("@IdCliente", fila.IdCliente),
                new SqlParameter("@FechaAlta", fila.FechaAlta),
                new SqlParameter("@Estado",    (int)fila.Estado)
            };

            DataTable tabla = acceso.Leer(
                "INSERT INTO ListaEspera (IdPrenda, IdCliente, FechaAlta, Estado) " +
                "VALUES (@IdPrenda, @IdCliente, @FechaAlta, @Estado); " +
                "SELECT SCOPE_IDENTITY() AS IdNuevo",
                p);

            return tabla != null && tabla.Rows.Count > 0
                ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"])
                : 0;
        }

        public void CambiarEstado(int idListaEspera, BE.EstadoListaEspera nuevoEstado,
                                   DateTime? fechaLimiteReserva, string actor)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Estado",             (int)nuevoEstado),
                new SqlParameter("@FechaLimiteReserva",  (object)fechaLimiteReserva ?? DBNull.Value),
                new SqlParameter("@FechaResolucion",     nuevoEstado == BE.EstadoListaEspera.Reservada ? (object)DBNull.Value : DateTime.Now),
                new SqlParameter("@Actor",               (object)actor ?? DBNull.Value),
                new SqlParameter("@IdListaEspera",       idListaEspera)
            };
            acceso.Escribir(
                "UPDATE ListaEspera SET Estado = @Estado, FechaLimiteReserva = @FechaLimiteReserva, " +
                "FechaResolucion = @FechaResolucion, Actor = @Actor WHERE IdListaEspera = @IdListaEspera",
                p);
        }

        private BE.ListaEspera Mapear(DataRow row)
        {
            return new BE.ListaEspera
            {
                IdListaEspera      = Convert.ToInt32(row["IdListaEspera"]),
                IdPrenda           = Convert.ToInt32(row["IdPrenda"]),
                IdCliente          = Convert.ToInt32(row["IdCliente"]),
                NombrePrenda       = row["NombrePrenda"].ToString(),
                NombreCliente      = row["NombreCliente"].ToString(),
                FechaAlta          = Convert.ToDateTime(row["FechaAlta"]),
                Estado             = (BE.EstadoListaEspera)Convert.ToInt32(row["Estado"]),
                FechaLimiteReserva = row["FechaLimiteReserva"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaLimiteReserva"]) : null,
                FechaResolucion    = row["FechaResolucion"]    != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaResolucion"])    : null,
                Actor              = row["Actor"] != DBNull.Value ? row["Actor"].ToString() : null
            };
        }
    }
}
