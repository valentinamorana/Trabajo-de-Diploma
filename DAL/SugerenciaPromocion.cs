using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — SugerenciaPromocion (PN03).
    /// Opera sobre la tabla [SugerenciaPromocion] de WardrobeFlowDB.
    /// </summary>
    public class SugerenciaPromocion : Interfaces.ISugerenciaPromocionDAL
    {
        private readonly Acceso acceso = Acceso.GetInstance();

        private const string SELECT_BASE =
            "SELECT s.IdSugerencia, s.IdPlan, s.CategoriaPrenda, s.Motivo, s.TipoDescuentoSugerido, " +
            "s.BeneficioEstimado, s.Estado, s.FechaAlta, pl.Nombre AS NombrePlan " +
            "FROM SugerenciaPromocion s " +
            "LEFT JOIN PlanSuscripcion pl ON pl.IdPlan = s.IdPlan ";

        public List<BE.SugerenciaPromocion> ObtenerPendientes()
        {
            var lista = new List<BE.SugerenciaPromocion>();
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "WHERE s.Estado = 0 ORDER BY s.FechaAlta", null);
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las sugerencias de promoción pendientes.", ex);
            }
            return lista;
        }

        public BE.SugerenciaPromocion ObtenerPorId(int idSugerencia)
        {
            SqlParameter[] p = { new SqlParameter("@IdSugerencia", idSugerencia) };
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "WHERE s.IdSugerencia = @IdSugerencia", p);
                if (tabla == null || tabla.Rows.Count == 0) return null;
                return Mapear(tabla.Rows[0]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la sugerencia de promoción.", ex);
            }
        }

        public int Alta(BE.SugerenciaPromocion sugerencia)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPlan",               (object)sugerencia.IdPlan ?? DBNull.Value),
                new SqlParameter("@CategoriaPrenda",       (object)sugerencia.CategoriaPrenda ?? DBNull.Value),
                new SqlParameter("@Motivo",                sugerencia.Motivo),
                new SqlParameter("@TipoDescuentoSugerido", (int)sugerencia.TipoDescuentoSugerido),
                new SqlParameter("@BeneficioEstimado",     sugerencia.BeneficioEstimado),
                new SqlParameter("@FechaAlta",             sugerencia.FechaAlta)
            };
            try
            {
                DataTable tabla = acceso.Leer(
                    "INSERT INTO SugerenciaPromocion (IdPlan, CategoriaPrenda, Motivo, TipoDescuentoSugerido, BeneficioEstimado, Estado, FechaAlta) " +
                    "VALUES (@IdPlan, @CategoriaPrenda, @Motivo, @TipoDescuentoSugerido, @BeneficioEstimado, 0, @FechaAlta); " +
                    "SELECT SCOPE_IDENTITY() AS IdNuevo",
                    p);
                return tabla != null && tabla.Rows.Count > 0 ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"]) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al registrar la sugerencia de promoción.", ex);
            }
        }

        public void MarcarEvaluada(int idSugerencia)
        {
            SqlParameter[] p = { new SqlParameter("@IdSugerencia", idSugerencia) };
            try
            {
                acceso.Escribir("UPDATE SugerenciaPromocion SET Estado = 1 WHERE IdSugerencia = @IdSugerencia", p);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al marcar la sugerencia como evaluada.", ex);
            }
        }

        private BE.SugerenciaPromocion Mapear(DataRow row)
        {
            return new BE.SugerenciaPromocion
            {
                IdSugerencia          = Convert.ToInt32(row["IdSugerencia"]),
                IdPlan                = row["IdPlan"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdPlan"]) : null,
                CategoriaPrenda       = row["CategoriaPrenda"] != DBNull.Value ? row["CategoriaPrenda"].ToString() : null,
                Motivo                = row["Motivo"].ToString(),
                TipoDescuentoSugerido = (BE.TipoDescuento)Convert.ToInt32(row["TipoDescuentoSugerido"]),
                BeneficioEstimado     = Convert.ToDecimal(row["BeneficioEstimado"]),
                Estado                = (BE.EstadoSugerencia)Convert.ToInt32(row["Estado"]),
                FechaAlta             = Convert.ToDateTime(row["FechaAlta"]),
                NombrePlan            = row["NombrePlan"] != DBNull.Value ? row["NombrePlan"].ToString() : null
            };
        }
    }
}
