using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — SugerenciaPromocion (PN03, «Sugerencia de promoción»).
    /// Las decisiones de Administración (aceptar → Evaluada, descartar → Descartada) son claims
    /// atómicos sobre el estado Pendiente.
    /// </summary>
    public class SugerenciaPromocion : BaseDAL, Interfaces.ISugerenciaPromocionDAL
    {
        private const string SELECT_BASE =
            "SELECT s.IdSugerencia, s.IdPlan, s.CategoriaPrenda, s.Motivo, s.TipoDescuentoSugerido, " +
            "s.BeneficioEstimado, s.Estado, s.FechaAlta, s.OrigenMetrica, s.IdUsuarioAlta, s.MotivoDescarte, " +
            "s.FechaEvaluacion, pl.Nombre AS NombrePlan, " +
            "COALESCE(NULLIF(LTRIM(RTRIM(ISNULL(u.Nombre, '') + ' ' + ISNULL(u.Apellido, ''))), ''), u.Username) AS NombreUsuarioAlta " +
            "FROM SugerenciaPromocion s " +
            "LEFT JOIN PlanSuscripcion pl ON pl.IdPlan = s.IdPlan " +
            "LEFT JOIN Usuario u ON u.IdUsuario = s.IdUsuarioAlta ";

        public List<BE.SugerenciaPromocion> ObtenerPendientes() =>
            Listar(SELECT_BASE + "WHERE s.Estado = 0 ORDER BY s.FechaAlta",
                   "Error al obtener las sugerencias de promoción pendientes.");

        public List<BE.SugerenciaPromocion> ObtenerTodas() =>
            Listar(SELECT_BASE + "ORDER BY s.FechaAlta DESC", "Error al obtener las sugerencias de promoción.");

        private List<BE.SugerenciaPromocion> Listar(string sql, string error)
        {
            var lista = new List<BE.SugerenciaPromocion>();
            try
            {
                foreach (DataRow row in acceso.Leer(sql, null).Rows) lista.Add(Mapear(row));
            }
            catch (Exception ex) { throw new Exception(error, ex); }
            return lista;
        }

        public BE.SugerenciaPromocion ObtenerPorId(int idSugerencia)
        {
            SqlParameter[] p = { new SqlParameter("@IdSugerencia", SqlDbType.Int) { Value = idSugerencia } };
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "WHERE s.IdSugerencia = @IdSugerencia", p);
                return tabla == null || tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
            }
            catch (Exception ex) { throw new Exception("Error al obtener la sugerencia de promoción.", ex); }
        }

        public int Alta(BE.SugerenciaPromocion sugerencia)
        {
            // { Value = ... }: TipoDescuento/OrigenMetrica pueden valer 0 (Porcentaje/Abandono).
            SqlParameter[] p =
            {
                new SqlParameter("@IdPlan",               SqlDbType.Int) { Value = (object)sugerencia.IdPlan ?? DBNull.Value },
                new SqlParameter("@CategoriaPrenda",       SqlDbType.NVarChar, 100) { Value = (object)sugerencia.CategoriaPrenda ?? DBNull.Value },
                new SqlParameter("@Motivo",                SqlDbType.NVarChar, 500) { Value = sugerencia.Motivo },
                new SqlParameter("@TipoDescuentoSugerido", SqlDbType.Int) { Value = (int)sugerencia.TipoDescuentoSugerido },
                new SqlParameter("@BeneficioEstimado",     SqlDbType.Decimal) { Value = sugerencia.BeneficioEstimado, Precision = 10, Scale = 2 },
                new SqlParameter("@Estado",                SqlDbType.Int) { Value = (int)BE.EstadoSugerencia.Pendiente },
                new SqlParameter("@FechaAlta",             SqlDbType.DateTime) { Value = sugerencia.FechaAlta },
                new SqlParameter("@OrigenMetrica",         SqlDbType.Int) { Value = (int)sugerencia.OrigenMetrica },
                new SqlParameter("@IdUsuarioAlta",         SqlDbType.Int) { Value = (object)sugerencia.IdUsuarioAlta ?? DBNull.Value }
            };
            try
            {
                DataTable tabla = acceso.Leer(
                    "INSERT INTO SugerenciaPromocion (IdPlan, CategoriaPrenda, Motivo, TipoDescuentoSugerido, BeneficioEstimado, " +
                    "Estado, FechaAlta, OrigenMetrica, IdUsuarioAlta) " +
                    "VALUES (@IdPlan, @CategoriaPrenda, @Motivo, @TipoDescuentoSugerido, @BeneficioEstimado, " +
                    "@Estado, @FechaAlta, @OrigenMetrica, @IdUsuarioAlta); " +
                    "SELECT SCOPE_IDENTITY() AS IdNuevo", p);
                return tabla != null && tabla.Rows.Count > 0 ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"]) : 0;
            }
            catch (Exception ex) { throw new Exception("Error al registrar la sugerencia de promoción.", ex); }
        }

        public bool MarcarEvaluada(int idSugerencia, DateTime fecha)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdSugerencia", SqlDbType.Int) { Value = idSugerencia },
                new SqlParameter("@Estado",       SqlDbType.Int) { Value = (int)BE.EstadoSugerencia.Evaluada },
                new SqlParameter("@Pendiente",    SqlDbType.Int) { Value = (int)BE.EstadoSugerencia.Pendiente },
                new SqlParameter("@Fecha",        SqlDbType.DateTime) { Value = fecha }
            };
            try
            {
                return acceso.Escribir(
                    "UPDATE SugerenciaPromocion SET Estado = @Estado, FechaEvaluacion = @Fecha " +
                    "WHERE IdSugerencia = @IdSugerencia AND Estado = @Pendiente", p) > 0;
            }
            catch (Exception ex) { throw new Exception("Error al marcar la sugerencia como evaluada.", ex); }
        }

        public void ReabrirEvaluacion(int idSugerencia)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdSugerencia", SqlDbType.Int) { Value = idSugerencia },
                new SqlParameter("@Pendiente",    SqlDbType.Int) { Value = (int)BE.EstadoSugerencia.Pendiente },
                new SqlParameter("@Evaluada",     SqlDbType.Int) { Value = (int)BE.EstadoSugerencia.Evaluada }
            };
            try
            {
                acceso.Escribir(
                    "UPDATE SugerenciaPromocion SET Estado = @Pendiente, FechaEvaluacion = NULL " +
                    "WHERE IdSugerencia = @IdSugerencia AND Estado = @Evaluada", p);
            }
            catch (Exception ex) { throw new Exception("Error al reabrir la sugerencia.", ex); }
        }

        public bool Descartar(int idSugerencia, string motivo, DateTime fecha)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdSugerencia", SqlDbType.Int) { Value = idSugerencia },
                new SqlParameter("@Estado",       SqlDbType.Int) { Value = (int)BE.EstadoSugerencia.Descartada },
                new SqlParameter("@Pendiente",    SqlDbType.Int) { Value = (int)BE.EstadoSugerencia.Pendiente },
                new SqlParameter("@Motivo",       SqlDbType.NVarChar, 500) { Value = motivo },
                new SqlParameter("@Fecha",        SqlDbType.DateTime) { Value = fecha }
            };
            try
            {
                return acceso.Escribir(
                    "UPDATE SugerenciaPromocion SET Estado = @Estado, MotivoDescarte = @Motivo, FechaEvaluacion = @Fecha " +
                    "WHERE IdSugerencia = @IdSugerencia AND Estado = @Pendiente", p) > 0;
            }
            catch (Exception ex) { throw new Exception("Error al descartar la sugerencia.", ex); }
        }

        private static BE.SugerenciaPromocion Mapear(DataRow row)
        {
            string Texto(string col) => row[col] != DBNull.Value ? row[col].ToString() : null;
            return new BE.SugerenciaPromocion
            {
                IdSugerencia          = Convert.ToInt32(row["IdSugerencia"]),
                IdPlan                = row["IdPlan"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdPlan"]) : null,
                CategoriaPrenda       = Texto("CategoriaPrenda"),
                Motivo                = row["Motivo"].ToString(),
                TipoDescuentoSugerido = (BE.TipoDescuento)Convert.ToInt32(row["TipoDescuentoSugerido"]),
                BeneficioEstimado     = Convert.ToDecimal(row["BeneficioEstimado"]),
                Estado                = (BE.EstadoSugerencia)Convert.ToInt32(row["Estado"]),
                FechaAlta             = Convert.ToDateTime(row["FechaAlta"]),
                OrigenMetrica         = (BE.OrigenMetrica)Convert.ToInt32(row["OrigenMetrica"]),
                IdUsuarioAlta         = row["IdUsuarioAlta"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdUsuarioAlta"]) : null,
                MotivoDescarte        = Texto("MotivoDescarte"),
                FechaEvaluacion       = row["FechaEvaluacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaEvaluacion"]) : null,
                NombrePlan            = Texto("NombrePlan"),
                NombreUsuarioAlta     = Texto("NombreUsuarioAlta")
            };
        }
    }
}
