using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — Contratacion (PN02, Comercialización de la suscripción).
    /// Opera sobre la tabla [Contratacion] de WardrobeFlowDB.
    /// </summary>
    public class Contratacion : BaseDAL, Interfaces.IContratacionDAL
    {
        private const string SELECT_BASE =
            "SELECT c.IdContratacion, c.IdCliente, c.IdPlan, c.IdVendedor, c.IdCaja, c.Modalidad, " +
            "c.Estado, c.IntentosPago, c.FechaAlta, c.FechaResolucion, c.MedioPago, " +
            "c.NumeroComprobante, c.FechaComprobante, c.Importe, c.DescuentoAplicado, c.IdPromocion, " +
            "cli.Nombre + ' ' + cli.Apellido AS NombreCliente, pl.Nombre AS NombrePlan, pl.Precio AS MontoPlan " +
            "FROM Contratacion c " +
            "JOIN Cliente cli ON cli.IdCliente = c.IdCliente " +
            "JOIN PlanSuscripcion pl ON pl.IdPlan = c.IdPlan ";

        public List<BE.Contratacion> ObtenerPendientesDePago()
        {
            var lista = new List<BE.Contratacion>();
            try
            {
                DataTable tabla = acceso.Leer(
                    SELECT_BASE + "WHERE c.Estado = 0 ORDER BY c.FechaAlta", null);

                foreach (DataRow row in tabla.Rows)
                    lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las contrataciones pendientes de pago.", ex);
            }
            return lista;
        }

        public BE.Contratacion ObtenerPorId(int idContratacion)
        {
            SqlParameter[] p = { new SqlParameter("@IdContratacion", idContratacion) };
            try
            {
                DataTable tabla = acceso.Leer(
                    SELECT_BASE + "WHERE c.IdContratacion = @IdContratacion", p);

                if (tabla == null || tabla.Rows.Count == 0) return null;
                return Mapear(tabla.Rows[0]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la contratación.", ex);
            }
        }

        public int Alta(BE.Contratacion contratacion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdCliente",  contratacion.IdCliente),
                new SqlParameter("@IdPlan",     contratacion.IdPlan),
                new SqlParameter("@IdVendedor", contratacion.IdVendedor),
                new SqlParameter("@Modalidad",  (int)contratacion.Modalidad),
                new SqlParameter("@FechaAlta",  contratacion.FechaAlta)
            };
            try
            {
                DataTable tabla = acceso.Leer(
                    "INSERT INTO Contratacion (IdCliente, IdPlan, IdVendedor, Modalidad, Estado, IntentosPago, FechaAlta) " +
                    "VALUES (@IdCliente, @IdPlan, @IdVendedor, @Modalidad, 0, 0, @FechaAlta); " +
                    "SELECT SCOPE_IDENTITY() AS IdNuevo",
                    p);

                return tabla != null && tabla.Rows.Count > 0
                    ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"])
                    : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al registrar la contratación.", ex);
            }
        }

        public int IncrementarIntento(int idContratacion)
        {
            SqlParameter[] p = { new SqlParameter("@IdContratacion", idContratacion) };
            try
            {
                DataTable tabla = acceso.Leer(
                    "UPDATE Contratacion SET IntentosPago = IntentosPago + 1 " +
                    "WHERE IdContratacion = @IdContratacion AND Estado = 0; " +
                    "IF @@ROWCOUNT = 0 SELECT -1 AS Intentos " +
                    "ELSE SELECT IntentosPago AS Intentos FROM Contratacion WHERE IdContratacion = @IdContratacion",
                    p);

                return tabla != null && tabla.Rows.Count > 0
                    ? Convert.ToInt32(tabla.Rows[0]["Intentos"])
                    : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al registrar el intento de pago.", ex);
            }
        }

        public bool ConfirmarPago(int idContratacion, int idCaja, string medioPago, string numeroComprobante,
                                  decimal importe, decimal descuento, int? idPromocion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdContratacion",    idContratacion),
                new SqlParameter("@Estado",             (int)BE.EstadoContratacion.Pagada),
                new SqlParameter("@IdCaja",             idCaja),
                new SqlParameter("@MedioPago",          medioPago),
                new SqlParameter("@NumeroComprobante",  numeroComprobante),
                new SqlParameter("@FechaResolucion",    DateTime.Now),
                new SqlParameter("@FechaComprobante",   DateTime.Now),
                new SqlParameter("@Importe",            importe),
                new SqlParameter("@Descuento",          descuento),
                new SqlParameter("@IdPromocion",        (object)idPromocion ?? DBNull.Value)
            };
            try
            {
                // "Claim" atómico: solo una sesión puede pasar de PendientePago (0) a Pagada.
                int filas = acceso.Escribir(
                    "UPDATE Contratacion SET Estado = @Estado, IdCaja = @IdCaja, MedioPago = @MedioPago, " +
                    "NumeroComprobante = @NumeroComprobante, FechaComprobante = @FechaComprobante, " +
                    "Importe = @Importe, DescuentoAplicado = @Descuento, IdPromocion = @IdPromocion, " +
                    "FechaResolucion = @FechaResolucion WHERE IdContratacion = @IdContratacion AND Estado = 0",
                    p);
                return filas > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al confirmar el pago de la contratación.", ex);
            }
        }

        public void ReabrirPago(int idContratacion)
        {
            SqlParameter[] p = { new SqlParameter("@IdContratacion", idContratacion) };
            try
            {
                acceso.Escribir(
                    "UPDATE Contratacion SET Estado = 0, IdCaja = NULL, MedioPago = NULL, NumeroComprobante = NULL, " +
                    "FechaComprobante = NULL, Importe = NULL, DescuentoAplicado = NULL, IdPromocion = NULL, FechaResolucion = NULL WHERE IdContratacion = @IdContratacion AND Estado = 1",
                    p);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al reabrir la contratación tras un cobro fallido.", ex);
            }
        }

        public void Cancelar(int idContratacion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdContratacion", idContratacion),
                new SqlParameter("@Estado", (int)BE.EstadoContratacion.Cancelada),
                new SqlParameter("@FechaResolucion", DateTime.Now)
            };
            try
            {
                acceso.Escribir(
                    "UPDATE Contratacion SET Estado = @Estado, FechaResolucion = @FechaResolucion " +
                    "WHERE IdContratacion = @IdContratacion AND Estado = 0",
                    p);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cancelar la contratación.", ex);
            }
        }

        private BE.Contratacion Mapear(DataRow row)
        {
            return new BE.Contratacion
            {
                IdContratacion    = Convert.ToInt32(row["IdContratacion"]),
                IdCliente         = Convert.ToInt32(row["IdCliente"]),
                IdPlan            = Convert.ToInt32(row["IdPlan"]),
                IdVendedor        = Convert.ToInt32(row["IdVendedor"]),
                IdCaja            = row["IdCaja"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdCaja"]) : null,
                Modalidad         = (BE.Builders.ModalidadCobro)Convert.ToInt32(row["Modalidad"]),
                Estado            = (BE.EstadoContratacion)Convert.ToInt32(row["Estado"]),
                IntentosPago      = Convert.ToInt32(row["IntentosPago"]),
                FechaAlta         = Convert.ToDateTime(row["FechaAlta"]),
                FechaResolucion   = row["FechaResolucion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaResolucion"]) : null,
                MedioPago         = row["MedioPago"] != DBNull.Value ? row["MedioPago"].ToString() : null,
                NumeroComprobante = row["NumeroComprobante"] != DBNull.Value ? row["NumeroComprobante"].ToString() : null,
                FechaComprobante  = row["FechaComprobante"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaComprobante"]) : null,
                Importe           = row["Importe"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["Importe"]) : null,
                DescuentoAplicado = row["DescuentoAplicado"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["DescuentoAplicado"]) : null,
                IdPromocion       = row["IdPromocion"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdPromocion"]) : null,
                NombreCliente     = row["NombreCliente"].ToString(),
                NombrePlan        = row["NombrePlan"].ToString(),
                MontoPlan         = Convert.ToDecimal(row["MontoPlan"])
            };
        }
    }
}
