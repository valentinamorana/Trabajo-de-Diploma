using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — Promocion (PN03, Métricas y Promociones).
    /// Opera sobre la tabla [Promocion] de WardrobeFlowDB.
    /// </summary>
    public class Promocion : Interfaces.IPromocionDAL
    {
        private readonly Acceso acceso = Acceso.GetInstance();

        private const string SELECT_BASE =
            "SELECT p.IdPromocion, p.Nombre, p.Descripcion, p.TipoDescuento, p.Valor, " +
            "p.FechaInicio, p.FechaFin, p.Estado, p.IdPlan, p.CategoriaPrenda, p.MargenEstimado, " +
            "p.ImpactoEconomico, p.Observacion, p.MotivoBaja, p.IdSugerenciaOrigen, p.FechaAlta, " +
            "pl.Nombre AS NombrePlan " +
            "FROM Promocion p " +
            "LEFT JOIN PlanSuscripcion pl ON pl.IdPlan = p.IdPlan ";

        public List<BE.Promocion> ObtenerTodas()
        {
            var lista = new List<BE.Promocion>();
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "ORDER BY p.FechaAlta DESC", null);
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las promociones.", ex);
            }
            return lista;
        }

        public List<BE.Promocion> ObtenerVigentes()
        {
            var lista = new List<BE.Promocion>();
            try
            {
                // Vigente exige, ademas de Estado=1, estar dentro del rango de fechas — coincide
                // con BE.Promocion.EstaVigente(). Sin esto, una promocion aprobada pero ya vencida
                // seguia apareciendo como vigente para el Vendedor indefinidamente.
                DataTable tabla = acceso.Leer(
                    SELECT_BASE + "WHERE p.Estado = 1 AND CAST(GETDATE() AS DATE) BETWEEN p.FechaInicio AND p.FechaFin " +
                    "ORDER BY p.FechaFin", null);
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las promociones vigentes.", ex);
            }
            return lista;
        }

        public List<BE.Promocion> ObtenerPendientesRevisionContable()
        {
            var lista = new List<BE.Promocion>();
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "WHERE p.Estado = 0 ORDER BY p.FechaAlta", null);
                foreach (DataRow row in tabla.Rows) lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las promociones pendientes de revisión contable.", ex);
            }
            return lista;
        }

        public BE.Promocion ObtenerPorId(int idPromocion)
        {
            SqlParameter[] p = { new SqlParameter("@IdPromocion", idPromocion) };
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "WHERE p.IdPromocion = @IdPromocion", p);
                if (tabla == null || tabla.Rows.Count == 0) return null;
                return Mapear(tabla.Rows[0]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la promoción.", ex);
            }
        }

        public int Alta(BE.Promocion promocion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Nombre",             promocion.Nombre),
                new SqlParameter("@Descripcion",         (object)promocion.Descripcion ?? DBNull.Value),
                new SqlParameter("@TipoDescuento",       (int)promocion.TipoDescuento),
                new SqlParameter("@Valor",               promocion.Valor),
                new SqlParameter("@FechaInicio",         promocion.FechaInicio),
                new SqlParameter("@FechaFin",            promocion.FechaFin),
                new SqlParameter("@IdPlan",              (object)promocion.IdPlan ?? DBNull.Value),
                new SqlParameter("@CategoriaPrenda",     (object)promocion.CategoriaPrenda ?? DBNull.Value),
                new SqlParameter("@MargenEstimado",      promocion.MargenEstimado),
                new SqlParameter("@ImpactoEconomico",    (object)promocion.ImpactoEconomico ?? DBNull.Value),
                new SqlParameter("@IdSugerenciaOrigen",  (object)promocion.IdSugerenciaOrigen ?? DBNull.Value),
                new SqlParameter("@FechaAlta",           promocion.FechaAlta)
            };
            try
            {
                DataTable tabla = acceso.Leer(
                    "INSERT INTO Promocion (Nombre, Descripcion, TipoDescuento, Valor, FechaInicio, FechaFin, " +
                    "Estado, IdPlan, CategoriaPrenda, MargenEstimado, ImpactoEconomico, IdSugerenciaOrigen, FechaAlta) " +
                    "VALUES (@Nombre, @Descripcion, @TipoDescuento, @Valor, @FechaInicio, @FechaFin, " +
                    "0, @IdPlan, @CategoriaPrenda, @MargenEstimado, @ImpactoEconomico, @IdSugerenciaOrigen, @FechaAlta); " +
                    "SELECT SCOPE_IDENTITY() AS IdNuevo",
                    p);
                return tabla != null && tabla.Rows.Count > 0 ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"]) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al registrar la promoción.", ex);
            }
        }

        public void Modificar(BE.Promocion promocion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPromocion",     promocion.IdPromocion),
                new SqlParameter("@Nombre",           promocion.Nombre),
                new SqlParameter("@Descripcion",      (object)promocion.Descripcion ?? DBNull.Value),
                new SqlParameter("@TipoDescuento",    (int)promocion.TipoDescuento),
                new SqlParameter("@Valor",            promocion.Valor),
                new SqlParameter("@FechaInicio",      promocion.FechaInicio),
                new SqlParameter("@FechaFin",         promocion.FechaFin),
                new SqlParameter("@IdPlan",           (object)promocion.IdPlan ?? DBNull.Value),
                new SqlParameter("@CategoriaPrenda",  (object)promocion.CategoriaPrenda ?? DBNull.Value),
                new SqlParameter("@MargenEstimado",   promocion.MargenEstimado),
                new SqlParameter("@ImpactoEconomico", (object)promocion.ImpactoEconomico ?? DBNull.Value)
            };
            try
            {
                acceso.Escribir(
                    "UPDATE Promocion SET Nombre=@Nombre, Descripcion=@Descripcion, TipoDescuento=@TipoDescuento, " +
                    "Valor=@Valor, FechaInicio=@FechaInicio, FechaFin=@FechaFin, IdPlan=@IdPlan, " +
                    "CategoriaPrenda=@CategoriaPrenda, MargenEstimado=@MargenEstimado, ImpactoEconomico=@ImpactoEconomico " +
                    "WHERE IdPromocion=@IdPromocion",
                    p);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al modificar la promoción.", ex);
            }
        }

        public void CambiarEstado(int idPromocion, BE.EstadoPromocion nuevoEstado, string observacionOMotivo)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPromocion", idPromocion),
                new SqlParameter("@Estado",      (int)nuevoEstado),
                new SqlParameter("@Observacion", (object)observacionOMotivo ?? DBNull.Value)
            };
            try
            {
                // COALESCE: Desactivar()/AprobarBaja() llaman a esto con observacionOMotivo=null
                // (no tienen nada que agregar) — sin el COALESCE, ese null pisaba la observacion
                // que Contabilidad ya habia dejado en AprobarContable/RechazarContable.
                acceso.Escribir(
                    "UPDATE Promocion SET Estado=@Estado, Observacion=COALESCE(@Observacion, Observacion) " +
                    "WHERE IdPromocion=@IdPromocion",
                    p);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cambiar el estado de la promoción.", ex);
            }
        }

        public void SolicitarBaja(int idPromocion, string motivo)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPromocion", idPromocion),
                new SqlParameter("@MotivoBaja",  motivo)
            };
            try
            {
                acceso.Escribir(
                    "UPDATE Promocion SET Estado=3, MotivoBaja=@MotivoBaja WHERE IdPromocion=@IdPromocion",
                    p);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al solicitar la baja de la promoción.", ex);
            }
        }

        private BE.Promocion Mapear(DataRow row)
        {
            return new BE.Promocion
            {
                IdPromocion         = Convert.ToInt32(row["IdPromocion"]),
                Nombre              = row["Nombre"].ToString(),
                Descripcion         = row["Descripcion"] != DBNull.Value ? row["Descripcion"].ToString() : null,
                TipoDescuento       = (BE.TipoDescuento)Convert.ToInt32(row["TipoDescuento"]),
                Valor               = Convert.ToDecimal(row["Valor"]),
                FechaInicio         = Convert.ToDateTime(row["FechaInicio"]),
                FechaFin            = Convert.ToDateTime(row["FechaFin"]),
                Estado              = (BE.EstadoPromocion)Convert.ToInt32(row["Estado"]),
                IdPlan              = row["IdPlan"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdPlan"]) : null,
                CategoriaPrenda     = row["CategoriaPrenda"] != DBNull.Value ? row["CategoriaPrenda"].ToString() : null,
                MargenEstimado      = Convert.ToDecimal(row["MargenEstimado"]),
                ImpactoEconomico    = row["ImpactoEconomico"] != DBNull.Value ? row["ImpactoEconomico"].ToString() : null,
                Observacion         = row["Observacion"] != DBNull.Value ? row["Observacion"].ToString() : null,
                MotivoBaja          = row["MotivoBaja"] != DBNull.Value ? row["MotivoBaja"].ToString() : null,
                IdSugerenciaOrigen  = row["IdSugerenciaOrigen"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdSugerenciaOrigen"]) : null,
                FechaAlta           = Convert.ToDateTime(row["FechaAlta"]),
                NombrePlan          = row["NombrePlan"] != DBNull.Value ? row["NombrePlan"].ToString() : null
            };
        }
    }
}
