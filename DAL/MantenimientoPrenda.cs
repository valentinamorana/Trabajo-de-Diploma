using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class MantenimientoPrenda : BaseDAL<BE.MantenimientoPrenda>, Interfaces.IMantenimientoPrendaDAL
    {
        // Los mantenimientos se abren y se cierran junto con el cambio de estado de la prenda, en la
        // misma transacción: DAL.Prenda.CambiarEstado (manuales) y DAL.Pedido.RegistrarDevolucion
        // (los de una devolución, Origen = 1).
        public List<BE.MantenimientoPrenda> ObtenerPorPrenda(int idPrenda)
        {
            var lista = new List<BE.MantenimientoPrenda>();
            SqlParameter[] p = { new SqlParameter("@IdPrenda", idPrenda) };
            DataTable tabla = acceso.Leer(
                "SELECT m.IdMantenimiento, m.IdPrenda, m.FechaEntrada, m.FechaSalida, m.Actor, m.Origen, " +
                "       pr.Nombre AS NombrePrenda " +
                "FROM MantenimientoPrenda m " +
                "INNER JOIN Prenda pr ON pr.IdPrenda = m.IdPrenda " +
                "WHERE m.IdPrenda = @IdPrenda " +
                "ORDER BY m.FechaEntrada DESC",
                p);

            if (tabla == null) return lista;
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public override List<BE.MantenimientoPrenda> ObtenerTodos()
        {
            var lista = new List<BE.MantenimientoPrenda>();
            DataTable tabla = acceso.Leer(
                "SELECT m.IdMantenimiento, m.IdPrenda, m.FechaEntrada, m.FechaSalida, m.Actor, m.Origen, " +
                "       pr.Nombre AS NombrePrenda " +
                "FROM MantenimientoPrenda m " +
                "INNER JOIN Prenda pr ON pr.IdPrenda = m.IdPrenda " +
                "ORDER BY m.FechaEntrada DESC",
                null);

            if (tabla == null) return lista;
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public override BE.MantenimientoPrenda ObtenerPorId(int id)
        {
            SqlParameter[] p = { new SqlParameter("@IdMantenimiento", id) };
            DataTable tabla = acceso.Leer(
                "SELECT m.IdMantenimiento, m.IdPrenda, m.FechaEntrada, m.FechaSalida, m.Actor, m.Origen, " +
                "       pr.Nombre AS NombrePrenda " +
                "FROM MantenimientoPrenda m " +
                "INNER JOIN Prenda pr ON pr.IdPrenda = m.IdPrenda " +
                "WHERE m.IdMantenimiento = @IdMantenimiento",
                p);

            if (tabla == null || tabla.Rows.Count == 0) return null;
            return Mapear(tabla.Rows[0]);
        }

        private BE.MantenimientoPrenda Mapear(DataRow row)
        {
            return new BE.MantenimientoPrenda
            {
                IdMantenimiento = Convert.ToInt32(row["IdMantenimiento"]),
                IdPrenda        = Convert.ToInt32(row["IdPrenda"]),
                NombrePrenda    = row["NombrePrenda"].ToString(),
                FechaEntrada    = Convert.ToDateTime(row["FechaEntrada"]),
                FechaSalida     = row["FechaSalida"] != DBNull.Value
                                      ? (DateTime?)Convert.ToDateTime(row["FechaSalida"])
                                      : null,
                Actor           = row["Actor"] != DBNull.Value ? row["Actor"].ToString() : null,
                Origen          = row["Origen"] != DBNull.Value
                                      ? (BE.OrigenMantenimiento)Convert.ToInt32(row["Origen"])
                                      : BE.OrigenMantenimiento.Manual
            };
        }
    }
}
