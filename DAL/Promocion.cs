using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — Promocion (PN03, Métricas, promociones y toma de decisiones).
    /// Tablas: Promocion, PromocionHistorial, DictamenContable y SolicitudBajaPromocion.
    ///
    /// Cada transición es un claim atómico (UPDATE ... WHERE Estado = @Esperado) que, en la misma
    /// transacción, inserta su fila de PromocionHistorial y el objeto del flujo que genera
    /// («Dictamen contable», «Solicitud de baja», «Resolución de baja»).
    /// </summary>
    public class Promocion : BaseDAL, Interfaces.IPromocionDAL
    {
        // Nombre visible de un usuario: "Nombre Apellido" o, si no tiene datos administrativos, el Username.
        private static string NombreUsuario(string alias) =>
            $"COALESCE(NULLIF(LTRIM(RTRIM(ISNULL({alias}.Nombre, '') + ' ' + ISNULL({alias}.Apellido, ''))), ''), {alias}.Username)";

        private static readonly string SELECT_BASE =
            "SELECT p.IdPromocion, p.Nombre, p.Descripcion, p.TipoDescuento, p.Valor, " +
            "p.FechaInicio, p.FechaFin, p.Estado, p.IdPlan, p.CategoriaPrenda, p.MargenEstimado, " +
            "p.ImpactoEconomico, p.IdSugerenciaOrigen, p.IdUsuarioAlta, p.FechaAlta, " +
            "pl.Nombre AS NombrePlan, " + NombreUsuario("ua") + " AS NombreUsuarioAlta, " +
            "dc.Observacion, sb.Motivo AS MotivoBaja " +
            "FROM Promocion p " +
            "LEFT JOIN PlanSuscripcion pl ON pl.IdPlan = p.IdPlan " +
            "LEFT JOIN Usuario ua ON ua.IdUsuario = p.IdUsuarioAlta " +
            // Observación del último «Dictamen contable» y motivo de la «Solicitud de baja» pendiente.
            "OUTER APPLY (SELECT TOP 1 d.Observacion FROM DictamenContable d WHERE d.IdPromocion = p.IdPromocion " +
            "             ORDER BY d.Fecha DESC, d.IdDictamen DESC) dc " +
            "OUTER APPLY (SELECT TOP 1 s.Motivo FROM SolicitudBajaPromocion s WHERE s.IdPromocion = p.IdPromocion " +
            "             AND s.Estado = 0 ORDER BY s.IdSolicitud DESC) sb ";

        public List<BE.Promocion> ObtenerTodas() =>
            Listar(SELECT_BASE + "ORDER BY p.FechaAlta DESC", "Error al obtener las promociones.");

        // Vigente exige, además de Estado = 1, estar dentro del rango de fechas: coincide con
        // BE.Promocion.EstaVigente(). Una promoción Vencida (Estado = 6) nunca entra.
        public List<BE.Promocion> ObtenerVigentes() =>
            Listar(SELECT_BASE + "WHERE p.Estado = 1 AND CAST(GETDATE() AS DATE) BETWEEN p.FechaInicio AND p.FechaFin " +
                   "ORDER BY p.FechaFin", "Error al obtener las promociones vigentes.");

        public List<BE.MetricaImpactoPromocion> ObtenerImpacto(DateTime desde, DateTime hasta)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Desde", desde.Date),
                new SqlParameter("@HastaExcl", hasta.Date.AddDays(1))
            };
            var lista = new List<BE.MetricaImpactoPromocion>();
            try
            {
                DataTable t = acceso.Leer(
                    "SELECT pr.IdPromocion, pr.Nombre, pr.Estado, COUNT(*) AS Cobros, " +
                    "       SUM(ISNULL(x.Descuento, 0)) AS TotalDescontado, SUM(x.Importe) AS TotalCobrado " +
                    "FROM (" +
                    "  SELECT IdPromocion, DescuentoAplicado AS Descuento, Importe FROM Contratacion " +
                    "  WHERE Estado = 1 AND IdPromocion IS NOT NULL AND FechaComprobante >= @Desde AND FechaComprobante < @HastaExcl " +
                    "  UNION ALL " +
                    "  SELECT IdPromocion, DescuentoAplicado, Importe FROM HistorialCobro " +
                    "  WHERE Resultado = 1 AND IdPromocion IS NOT NULL AND FechaResolucion >= @Desde AND FechaResolucion < @HastaExcl" +
                    ") x INNER JOIN Promocion pr ON pr.IdPromocion = x.IdPromocion " +
                    "GROUP BY pr.IdPromocion, pr.Nombre, pr.Estado " +
                    "ORDER BY TotalDescontado DESC", p);
                foreach (DataRow r in t.Rows)
                    lista.Add(new BE.MetricaImpactoPromocion
                    {
                        IdPromocion     = Convert.ToInt32(r["IdPromocion"]),
                        Nombre          = r["Nombre"].ToString(),
                        Estado          = (BE.EstadoPromocion)Convert.ToInt32(r["Estado"]),
                        Cobros          = Convert.ToInt32(r["Cobros"]),
                        TotalDescontado = Convert.ToDecimal(r["TotalDescontado"]),
                        TotalCobrado    = r["TotalCobrado"] != DBNull.Value ? Convert.ToDecimal(r["TotalCobrado"]) : 0m
                    });
            }
            catch (Exception ex) { throw new Exception("Error al calcular el impacto de las promociones.", ex); }
            return lista;
        }

        public List<BE.Promocion> ObtenerPendientesRevisionContable() =>
            Listar(SELECT_BASE + "WHERE p.Estado = 0 ORDER BY p.FechaAlta",
                   "Error al obtener las promociones pendientes de revisión contable.");

        private List<BE.Promocion> Listar(string sql, string error)
        {
            var lista = new List<BE.Promocion>();
            try
            {
                foreach (DataRow row in acceso.Leer(sql, null).Rows) lista.Add(Mapear(row));
            }
            catch (Exception ex) { throw new Exception(error, ex); }
            return lista;
        }

        public BE.Promocion ObtenerPorId(int idPromocion)
        {
            SqlParameter[] p = { new SqlParameter("@IdPromocion", SqlDbType.Int) { Value = idPromocion } };
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "WHERE p.IdPromocion = @IdPromocion", p);
                return tabla == null || tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
            }
            catch (Exception ex) { throw new Exception("Error al obtener la promoción.", ex); }
        }

        // ── Transiciones ──────────────────────────────────────────────────────

        public int Alta(BE.Promocion promocion, BE.PromocionHistorial historial)
        {
            int idNuevo = 0;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO Promocion (Nombre, Descripcion, TipoDescuento, Valor, FechaInicio, FechaFin, " +
                        "Estado, IdPlan, CategoriaPrenda, MargenEstimado, ImpactoEconomico, IdSugerenciaOrigen, IdUsuarioAlta, FechaAlta) " +
                        "VALUES (@Nombre, @Descripcion, @TipoDescuento, @Valor, @FechaInicio, @FechaFin, " +
                        "@Estado, @IdPlan, @CategoriaPrenda, @MargenEstimado, @ImpactoEconomico, @IdSugerenciaOrigen, @IdUsuarioAlta, @FechaAlta); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT)", cn, tx))
                    {
                        AgregarCondiciones(cmd, promocion);
                        cmd.Parameters.AddWithValue("@Estado", (int)BE.EstadoPromocion.EnRevisionContable);
                        cmd.Parameters.AddWithValue("@IdSugerenciaOrigen", (object)promocion.IdSugerenciaOrigen ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IdUsuarioAlta", (object)promocion.IdUsuarioAlta ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FechaAlta", promocion.FechaAlta);
                        idNuevo = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    historial.IdPromocion = idNuevo;
                    InsertarHistorial(cn, tx, historial);
                });
            }
            catch (Exception ex) { throw new Exception("Error al registrar la promoción.", ex); }
            return idNuevo;
        }

        public bool Reformular(BE.Promocion promocion, BE.PromocionHistorial historial)
        {
            bool ok = false;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    using (var cmd = new SqlCommand(
                        "UPDATE Promocion SET Nombre=@Nombre, Descripcion=@Descripcion, TipoDescuento=@TipoDescuento, " +
                        "Valor=@Valor, FechaInicio=@FechaInicio, FechaFin=@FechaFin, IdPlan=@IdPlan, " +
                        "CategoriaPrenda=@CategoriaPrenda, MargenEstimado=@MargenEstimado, ImpactoEconomico=@ImpactoEconomico, " +
                        "Estado=@Nuevo WHERE IdPromocion=@IdPromocion AND Estado=@Esperado", cn, tx))
                    {
                        AgregarCondiciones(cmd, promocion);
                        cmd.Parameters.AddWithValue("@IdPromocion", promocion.IdPromocion);
                        cmd.Parameters.AddWithValue("@Nuevo", (int)historial.EstadoNuevo);
                        cmd.Parameters.AddWithValue("@Esperado", (int)historial.EstadoAnterior.GetValueOrDefault());
                        ok = cmd.ExecuteNonQuery() == 1;
                    }
                    if (ok) InsertarHistorial(cn, tx, historial);
                });
            }
            catch (Exception ex) { throw new Exception("Error al reformular la promoción.", ex); }
            return ok;
        }

        public bool CambiarEstado(int idPromocion, BE.EstadoPromocion estadoEsperado, BE.PromocionHistorial historial)
        {
            bool ok = false;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    ok = Reclamar(cn, tx, idPromocion, estadoEsperado, historial.EstadoNuevo);
                    if (ok) InsertarHistorial(cn, tx, historial);
                });
            }
            catch (Exception ex) { throw new Exception("Error al cambiar el estado de la promoción.", ex); }
            return ok;
        }

        public int Dictaminar(BE.DictamenContable dictamen, BE.PromocionHistorial historial)
        {
            int idDictamen = 0;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    if (!Reclamar(cn, tx, dictamen.IdPromocion, BE.EstadoPromocion.EnRevisionContable, historial.EstadoNuevo))
                        return;
                    using (var cmd = new SqlCommand(
                        "INSERT INTO DictamenContable (IdPromocion, IdUsuario, Aprobada, Observacion, Fecha) " +
                        "VALUES (@IdPromocion, @IdUsuario, @Aprobada, @Observacion, @Fecha); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT)", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@IdPromocion", dictamen.IdPromocion);
                        cmd.Parameters.AddWithValue("@IdUsuario", dictamen.IdUsuario);
                        cmd.Parameters.AddWithValue("@Aprobada", dictamen.Aprobada);
                        cmd.Parameters.AddWithValue("@Observacion", dictamen.Observacion);
                        cmd.Parameters.AddWithValue("@Fecha", dictamen.Fecha);
                        idDictamen = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    InsertarHistorial(cn, tx, historial);
                });
            }
            catch (Exception ex) { throw new Exception("Error al registrar el dictamen contable.", ex); }
            return idDictamen;
        }

        public int SolicitarBaja(BE.SolicitudBajaPromocion solicitud, BE.PromocionHistorial historial)
        {
            int idSolicitud = 0;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    if (!Reclamar(cn, tx, solicitud.IdPromocion, BE.EstadoPromocion.Vigente, BE.EstadoPromocion.BajaSolicitada))
                        return;
                    using (var cmd = new SqlCommand(
                        "INSERT INTO SolicitudBajaPromocion (IdPromocion, IdUsuarioSolicita, Motivo, FechaSolicitud, Estado) " +
                        "VALUES (@IdPromocion, @IdUsuario, @Motivo, @Fecha, @Estado); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT)", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@IdPromocion", solicitud.IdPromocion);
                        cmd.Parameters.AddWithValue("@IdUsuario", solicitud.IdUsuarioSolicita);
                        cmd.Parameters.AddWithValue("@Motivo", solicitud.Motivo);
                        cmd.Parameters.AddWithValue("@Fecha", solicitud.FechaSolicitud);
                        cmd.Parameters.AddWithValue("@Estado", (int)BE.EstadoSolicitudBaja.Pendiente);
                        idSolicitud = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    InsertarHistorial(cn, tx, historial);
                });
            }
            catch (Exception ex) { throw new Exception("Error al registrar la solicitud de baja.", ex); }
            return idSolicitud;
        }

        public bool ResolverBaja(BE.SolicitudBajaPromocion resolucion, BE.PromocionHistorial historial)
        {
            bool ok = false;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    if (!Reclamar(cn, tx, resolucion.IdPromocion, BE.EstadoPromocion.BajaSolicitada, historial.EstadoNuevo))
                        return;
                    using (var cmd = new SqlCommand(
                        "UPDATE SolicitudBajaPromocion SET Estado = @Estado, IdUsuarioResuelve = @IdUsuario, " +
                        "MotivoResolucion = @Motivo, FechaResolucion = @Fecha " +
                        "WHERE IdSolicitud = @IdSolicitud AND IdPromocion = @IdPromocion AND Estado = @Pendiente", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Estado", (int)resolucion.Estado);
                        cmd.Parameters.AddWithValue("@IdUsuario", (object)resolucion.IdUsuarioResuelve ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Motivo", (object)resolucion.MotivoResolucion ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Fecha", (object)resolucion.FechaResolucion ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IdSolicitud", resolucion.IdSolicitud);
                        cmd.Parameters.AddWithValue("@IdPromocion", resolucion.IdPromocion);
                        cmd.Parameters.AddWithValue("@Pendiente", (int)BE.EstadoSolicitudBaja.Pendiente);
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("La solicitud de baja ya no está pendiente.");
                    }
                    InsertarHistorial(cn, tx, historial);
                    ok = true;
                });
            }
            catch (Exception ex) { throw new Exception("Error al resolver la solicitud de baja.", ex); }
            return ok;
        }

        // Claim atómico: solo una sesión puede sacar a la promoción del estado esperado.
        private static bool Reclamar(SqlConnection cn, SqlTransaction tx, int idPromocion,
                                     BE.EstadoPromocion esperado, BE.EstadoPromocion nuevo)
        {
            using (var cmd = new SqlCommand(
                "UPDATE Promocion SET Estado = @Nuevo WHERE IdPromocion = @IdPromocion AND Estado = @Esperado", cn, tx))
            {
                cmd.Parameters.AddWithValue("@Nuevo", (int)nuevo);
                cmd.Parameters.AddWithValue("@IdPromocion", idPromocion);
                cmd.Parameters.AddWithValue("@Esperado", (int)esperado);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        private static void InsertarHistorial(SqlConnection cn, SqlTransaction tx, BE.PromocionHistorial h)
        {
            using (var cmd = new SqlCommand(
                "INSERT INTO PromocionHistorial (IdPromocion, EstadoAnterior, EstadoNuevo, IdUsuario, Fecha, Observacion) " +
                "VALUES (@IdPromocion, @Anterior, @Nuevo, @IdUsuario, @Fecha, @Observacion)", cn, tx))
            {
                cmd.Parameters.AddWithValue("@IdPromocion", h.IdPromocion);
                cmd.Parameters.AddWithValue("@Anterior", h.EstadoAnterior.HasValue ? (object)(int)h.EstadoAnterior.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Nuevo", (int)h.EstadoNuevo);
                cmd.Parameters.AddWithValue("@IdUsuario", (object)h.IdUsuario ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fecha", h.Fecha);
                cmd.Parameters.AddWithValue("@Observacion", (object)h.Observacion ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        // Condiciones de la promoción (comunes al alta y a la reformulación). AddWithValue: los
        // enums en 0 (Porcentaje) no deben caer en la sobrecarga SqlParameter(string, SqlDbType).
        private static void AgregarCondiciones(SqlCommand cmd, BE.Promocion p)
        {
            cmd.Parameters.AddWithValue("@Nombre", p.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", (object)p.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TipoDescuento", (int)p.TipoDescuento);
            cmd.Parameters.AddWithValue("@Valor", p.Valor);
            cmd.Parameters.AddWithValue("@FechaInicio", p.FechaInicio.Date);
            cmd.Parameters.AddWithValue("@FechaFin", p.FechaFin.Date);
            cmd.Parameters.AddWithValue("@IdPlan", (object)p.IdPlan ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoriaPrenda", (object)p.CategoriaPrenda ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MargenEstimado", p.MargenEstimado);
            cmd.Parameters.AddWithValue("@ImpactoEconomico", (object)p.ImpactoEconomico ?? DBNull.Value);
        }

        // ── Objetos del flujo ────────────────────────────────────────────────

        public List<BE.PromocionHistorial> ObtenerHistorial(int idPromocion)
        {
            var lista = new List<BE.PromocionHistorial>();
            SqlParameter[] p = { new SqlParameter("@Id", SqlDbType.Int) { Value = idPromocion } };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT h.IdHistorial, h.IdPromocion, h.EstadoAnterior, h.EstadoNuevo, h.IdUsuario, h.Fecha, h.Observacion, " +
                    NombreUsuario("u") + " AS NombreUsuario " +
                    "FROM PromocionHistorial h LEFT JOIN Usuario u ON u.IdUsuario = h.IdUsuario " +
                    "WHERE h.IdPromocion = @Id ORDER BY h.Fecha, h.IdHistorial", p);
                foreach (DataRow r in tabla.Rows)
                    lista.Add(new BE.PromocionHistorial
                    {
                        IdHistorial    = Convert.ToInt32(r["IdHistorial"]),
                        IdPromocion    = Convert.ToInt32(r["IdPromocion"]),
                        EstadoAnterior = r["EstadoAnterior"] != DBNull.Value ? (BE.EstadoPromocion?)Convert.ToInt32(r["EstadoAnterior"]) : null,
                        EstadoNuevo    = (BE.EstadoPromocion)Convert.ToInt32(r["EstadoNuevo"]),
                        IdUsuario      = r["IdUsuario"] != DBNull.Value ? (int?)Convert.ToInt32(r["IdUsuario"]) : null,
                        Fecha          = Convert.ToDateTime(r["Fecha"]),
                        Observacion    = Texto(r, "Observacion"),
                        NombreUsuario  = Texto(r, "NombreUsuario")
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener el historial de la promoción.", ex); }
            return lista;
        }

        public List<BE.DictamenContable> ObtenerDictamenes(int idPromocion)
        {
            var lista = new List<BE.DictamenContable>();
            SqlParameter[] p = { new SqlParameter("@Id", SqlDbType.Int) { Value = idPromocion } };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT d.IdDictamen, d.IdPromocion, d.IdUsuario, d.Aprobada, d.Observacion, d.Fecha, " +
                    NombreUsuario("u") + " AS NombreUsuario " +
                    "FROM DictamenContable d JOIN Usuario u ON u.IdUsuario = d.IdUsuario " +
                    "WHERE d.IdPromocion = @Id ORDER BY d.Fecha, d.IdDictamen", p);
                foreach (DataRow r in tabla.Rows)
                    lista.Add(new BE.DictamenContable
                    {
                        IdDictamen    = Convert.ToInt32(r["IdDictamen"]),
                        IdPromocion   = Convert.ToInt32(r["IdPromocion"]),
                        IdUsuario     = Convert.ToInt32(r["IdUsuario"]),
                        Aprobada      = Convert.ToBoolean(r["Aprobada"]),
                        Observacion   = r["Observacion"].ToString(),
                        Fecha         = Convert.ToDateTime(r["Fecha"]),
                        NombreUsuario = Texto(r, "NombreUsuario")
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener los dictámenes contables.", ex); }
            return lista;
        }

        public List<BE.SolicitudBajaPromocion> ObtenerSolicitudesBaja(int idPromocion)
        {
            var lista = new List<BE.SolicitudBajaPromocion>();
            SqlParameter[] p = { new SqlParameter("@Id", SqlDbType.Int) { Value = idPromocion } };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT s.IdSolicitud, s.IdPromocion, s.IdUsuarioSolicita, s.Motivo, s.FechaSolicitud, s.Estado, " +
                    "s.IdUsuarioResuelve, s.MotivoResolucion, s.FechaResolucion, " +
                    NombreUsuario("us") + " AS NombreUsuarioSolicita, " + NombreUsuario("ur") + " AS NombreUsuarioResuelve " +
                    "FROM SolicitudBajaPromocion s " +
                    "JOIN Usuario us ON us.IdUsuario = s.IdUsuarioSolicita " +
                    "LEFT JOIN Usuario ur ON ur.IdUsuario = s.IdUsuarioResuelve " +
                    "WHERE s.IdPromocion = @Id ORDER BY s.FechaSolicitud, s.IdSolicitud", p);
                foreach (DataRow r in tabla.Rows)
                    lista.Add(new BE.SolicitudBajaPromocion
                    {
                        IdSolicitud           = Convert.ToInt32(r["IdSolicitud"]),
                        IdPromocion           = Convert.ToInt32(r["IdPromocion"]),
                        IdUsuarioSolicita     = Convert.ToInt32(r["IdUsuarioSolicita"]),
                        Motivo                = r["Motivo"].ToString(),
                        FechaSolicitud        = Convert.ToDateTime(r["FechaSolicitud"]),
                        Estado                = (BE.EstadoSolicitudBaja)Convert.ToInt32(r["Estado"]),
                        IdUsuarioResuelve     = r["IdUsuarioResuelve"] != DBNull.Value ? (int?)Convert.ToInt32(r["IdUsuarioResuelve"]) : null,
                        MotivoResolucion      = Texto(r, "MotivoResolucion"),
                        FechaResolucion       = r["FechaResolucion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["FechaResolucion"]) : null,
                        NombreUsuarioSolicita = Texto(r, "NombreUsuarioSolicita"),
                        NombreUsuarioResuelve = Texto(r, "NombreUsuarioResuelve")
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener las solicitudes de baja.", ex); }
            return lista;
        }

        private static string Texto(DataRow r, string col) => r[col] != DBNull.Value ? r[col].ToString() : null;

        private static BE.Promocion Mapear(DataRow row)
        {
            return new BE.Promocion
            {
                IdPromocion         = Convert.ToInt32(row["IdPromocion"]),
                Nombre              = row["Nombre"].ToString(),
                Descripcion         = Texto(row, "Descripcion"),
                TipoDescuento       = (BE.TipoDescuento)Convert.ToInt32(row["TipoDescuento"]),
                Valor               = Convert.ToDecimal(row["Valor"]),
                FechaInicio         = Convert.ToDateTime(row["FechaInicio"]),
                FechaFin            = Convert.ToDateTime(row["FechaFin"]),
                Estado              = (BE.EstadoPromocion)Convert.ToInt32(row["Estado"]),
                IdPlan              = row["IdPlan"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdPlan"]) : null,
                CategoriaPrenda     = Texto(row, "CategoriaPrenda"),
                MargenEstimado      = Convert.ToDecimal(row["MargenEstimado"]),
                ImpactoEconomico    = Texto(row, "ImpactoEconomico"),
                IdSugerenciaOrigen  = row["IdSugerenciaOrigen"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdSugerenciaOrigen"]) : null,
                IdUsuarioAlta       = row["IdUsuarioAlta"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdUsuarioAlta"]) : null,
                FechaAlta           = Convert.ToDateTime(row["FechaAlta"]),
                NombrePlan          = Texto(row, "NombrePlan"),
                NombreUsuarioAlta   = Texto(row, "NombreUsuarioAlta"),
                Observacion         = Texto(row, "Observacion"),
                MotivoBaja          = Texto(row, "MotivoBaja")
            };
        }
    }
}
