using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — Prenda.
    /// Opera sobre la tabla [Prenda] de WardrobeFlowDB.
    /// </summary>
    /// <summary>
    /// Hereda de <see cref="BaseDAL{T}"/>:
    ///   - acceso  → Singleton de BD (heredado, no se redeclara)
    ///   - ObtenerTodos() y ObtenerPorId() → implementados con SQL de Prenda
    /// </summary>
    public class Prenda : BaseDAL<BE.Prenda>, Interfaces.IPrendaDAL
    {

        // Devuelve todas las prendas con nombre del cliente si están en uso. 
        public override List<BE.Prenda> ObtenerTodos()
        {
            var lista = new List<BE.Prenda>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT p.IdPrenda, p.Nombre, p.Descripcion, p.Talle, p.Color, " +
                    "       p.Categoria, p.Estado, p.IdClienteActual, p.IdUltimoCliente, p.FechaAlta, " +
                    "       c.Nombre + ' ' + c.Apellido AS NombreCliente, " +
                    "       cu.Nombre + ' ' + cu.Apellido AS NombreUltimoCliente " +
                    "FROM Prenda p " +
                    "LEFT JOIN Cliente c  ON c.IdCliente  = p.IdClienteActual " +
                    "LEFT JOIN Cliente cu ON cu.IdCliente = p.IdUltimoCliente " +
                    "ORDER BY p.Categoria, p.Nombre",
                    null);

                foreach (DataRow row in tabla.Rows)
                    lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la lista de prendas.", ex);
            }
            return lista;
        }

        // Devuelve solo las prendas con estado Disponible. El filtrado adicional por
        // reservas de Lista de Espera (mejora opcional) vive en BLL.Prenda — acá se ignora
        // el parámetro para no acoplar esta query, ya probada, a una tabla nueva y opcional.
        public List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null)
        {
            var lista = new List<BE.Prenda>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT p.IdPrenda, p.Nombre, p.Descripcion, p.Talle, p.Color, " +
                    "       p.Categoria, p.Estado, p.IdClienteActual, p.IdUltimoCliente, p.FechaAlta, " +
                    "       NULL AS NombreCliente, " +
                    "       cu.Nombre + ' ' + cu.Apellido AS NombreUltimoCliente " +
                    "FROM Prenda p " +
                    "LEFT JOIN Cliente cu ON cu.IdCliente = p.IdUltimoCliente " +
                    "WHERE p.Estado = @Estado " +
                    "ORDER BY p.Categoria, p.Nombre",
                    new[] { new SqlParameter("@Estado", (int)BE.EstadoPrenda.Disponible) });

                foreach (DataRow row in tabla.Rows)
                    lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener prendas disponibles.", ex);
            }
            return lista;
        }

        // Obtiene una prenda por ID.
        public override BE.Prenda ObtenerPorId(int idPrenda)
        {
            SqlParameter[] p = { new SqlParameter("@IdPrenda", idPrenda) };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT p.IdPrenda, p.Nombre, p.Descripcion, p.Talle, p.Color, " +
                    "       p.Categoria, p.Estado, p.IdClienteActual, p.IdUltimoCliente, p.FechaAlta, " +
                    "       c.Nombre + ' ' + c.Apellido AS NombreCliente, " +
                    "       cu.Nombre + ' ' + cu.Apellido AS NombreUltimoCliente " +
                    "FROM Prenda p " +
                    "LEFT JOIN Cliente c  ON c.IdCliente  = p.IdClienteActual " +
                    "LEFT JOIN Cliente cu ON cu.IdCliente = p.IdUltimoCliente " +
                    "WHERE p.IdPrenda = @IdPrenda",
                    p);

                if (tabla == null || tabla.Rows.Count == 0) return null;
                return Mapear(tabla.Rows[0]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la prenda.", ex);
            }
        }

        // Devuelve las prendas actualmente asignadas a un cliente.
        public List<BE.Prenda> ObtenerPorCliente(int idCliente)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdCliente", idCliente),
                new SqlParameter("@Estado",    (int)BE.EstadoPrenda.EnUso),
            };
            var lista = new List<BE.Prenda>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT p.IdPrenda, p.Nombre, p.Descripcion, p.Talle, p.Color, " +
                    "       p.Categoria, p.Estado, p.IdClienteActual, p.IdUltimoCliente, p.FechaAlta, " +
                    "       c.Nombre + ' ' + c.Apellido AS NombreCliente, " +
                    "       cu.Nombre + ' ' + cu.Apellido AS NombreUltimoCliente " +
                    "FROM Prenda p " +
                    "LEFT JOIN Cliente c  ON c.IdCliente  = p.IdClienteActual " +
                    "LEFT JOIN Cliente cu ON cu.IdCliente = p.IdUltimoCliente " +
                    "WHERE p.IdClienteActual = @IdCliente AND p.Estado = @Estado",
                    p);

                foreach (DataRow row in tabla.Rows)
                    lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener prendas del cliente.", ex);
            }
            return lista;
        }

        // PdN12 — Stock Disponible agrupado por Talle+Categoría. Usada por
        // BLL.AnalisisEscasez para detectar combinaciones por debajo del umbral mínimo.
        public List<BE.StockPorTalleCategoria> ObtenerConteoDisponiblesPorTalleCategoria()
        {
            var lista = new List<BE.StockPorTalleCategoria>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT ISNULL(Talle, '—') AS Talle, ISNULL(Categoria, '—') AS Categoria, " +
                    "       COUNT(*) AS Cantidad " +
                    "FROM Prenda " +
                    "WHERE Estado = @Estado " +
                    "GROUP BY Talle, Categoria " +
                    "ORDER BY Categoria, Talle",
                    new[] { new SqlParameter("@Estado", (int)BE.EstadoPrenda.Disponible) });

                if (tabla != null)
                    foreach (DataRow row in tabla.Rows)
                        lista.Add(new BE.StockPorTalleCategoria
                        {
                            Talle = row["Talle"].ToString(),
                            Categoria = row["Categoria"].ToString(),
                            CantidadDisponible = Convert.ToInt32(row["Cantidad"])
                        });
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el stock disponible por talle y categoría.", ex);
            }
            return lista;
        }

        // Inserta una nueva prenda. Devuelve el ID generado.
        public int Alta(BE.Prenda prenda)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Nombre", prenda.Nombre),
                new SqlParameter("@Descripcion", (object)prenda.Descripcion ?? DBNull.Value),
                new SqlParameter("@Talle", (object)prenda.Talle ?? DBNull.Value),
                new SqlParameter("@Color", (object)prenda.Color ?? DBNull.Value),
                new SqlParameter("@Categoria", (object)prenda.Categoria ?? DBNull.Value),
                new SqlParameter("@Estado", (int)prenda.Estado),
                new SqlParameter("@FechaAlta", prenda.FechaAlta)
            };

            DataTable tabla = acceso.Leer(
                "INSERT INTO Prenda (Nombre, Descripcion, Talle, Color, Categoria, Estado, FechaAlta) " +
                "VALUES (@Nombre, @Descripcion, @Talle, @Color, @Categoria, @Estado, @FechaAlta); " +
                "SELECT SCOPE_IDENTITY() AS IdNuevo",
                p);

            return tabla != null && tabla.Rows.Count > 0
                ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"])
                : 0;
        }

        // Actualiza datos descriptivos de una prenda.
        public void Modificar(BE.Prenda prenda)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Nombre", prenda.Nombre),
                new SqlParameter("@Descripcion", (object)prenda.Descripcion ?? DBNull.Value),
                new SqlParameter("@Talle", (object)prenda.Talle ?? DBNull.Value),
                new SqlParameter("@Color", (object)prenda.Color ?? DBNull.Value),
                new SqlParameter("@Categoria", (object)prenda.Categoria ?? DBNull.Value),
                new SqlParameter("@IdPrenda", prenda.IdPrenda)
            };
            acceso.Escribir(
                "UPDATE Prenda SET Nombre=@Nombre, Descripcion=@Descripcion, " +
                "Talle=@Talle, Color=@Color, Categoria=@Categoria " +
                "WHERE IdPrenda=@IdPrenda",
                p);
        }

        // Cambia el estado de una prenda (disponible, en uso, limpieza, baja).
        // IdUltimoCliente solo se pisa cuando se pasa un idClienteActual concreto (asignación);
        // en las demás transiciones (limpieza, baja) se conserva el último dueño conocido.
        public void CambiarEstado(int idPrenda, BE.EstadoPrenda nuevoEstado, int? idClienteActual = null)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Estado", (int)nuevoEstado),
                new SqlParameter("@IdClienteActual", (object)idClienteActual ?? DBNull.Value),
                new SqlParameter("@IdUltimoCliente", (object)idClienteActual ?? DBNull.Value),
                new SqlParameter("@IdPrenda", idPrenda)
            };
            acceso.Escribir(
                "UPDATE Prenda SET Estado=@Estado, IdClienteActual=@IdClienteActual, " +
                "IdUltimoCliente=COALESCE(@IdUltimoCliente, IdUltimoCliente) " +
                "WHERE IdPrenda=@IdPrenda",
                p);
        }

        private BE.Prenda Mapear(DataRow row)
        {
            return new BE.Prenda
            {
                IdPrenda = Convert.ToInt32(row["IdPrenda"]),
                Nombre = row["Nombre"].ToString(),
                Descripcion = row["Descripcion"] != DBNull.Value ? row["Descripcion"].ToString() : null,
                Talle = row["Talle"]  != DBNull.Value ? row["Talle"].ToString() : null,
                Color = row["Color"]  != DBNull.Value ? row["Color"].ToString() : null,
                Categoria = row["Categoria"] != DBNull.Value ? row["Categoria"].ToString() : null,
                Estado = (BE.EstadoPrenda)Convert.ToInt32(row["Estado"]),
                IdClienteActual = row["IdClienteActual"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdClienteActual"]) : null,
                NombreCliente = row["NombreCliente"] != DBNull.Value ? row["NombreCliente"].ToString() : null,
                IdUltimoCliente = row.Table.Columns.Contains("IdUltimoCliente") && row["IdUltimoCliente"] != DBNull.Value
                    ? (int?)Convert.ToInt32(row["IdUltimoCliente"]) : null,
                NombreUltimoCliente = row.Table.Columns.Contains("NombreUltimoCliente") && row["NombreUltimoCliente"] != DBNull.Value
                    ? row["NombreUltimoCliente"].ToString() : null,
                FechaAlta = Convert.ToDateTime(row["FechaAlta"])
            };
        }
    }
}
