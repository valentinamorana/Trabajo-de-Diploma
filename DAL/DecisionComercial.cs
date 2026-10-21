using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// PN03 — Persistencia de la «Decisión comercial» (tabla DecisionComercial, sección 20o del script).
    /// Cada cambio de estado es un claim atómico (UPDATE ... WHERE Estado = @Esperado): dos sesiones no
    /// pueden decidir dos veces sobre la misma decisión.
    /// </summary>
    public class DecisionComercial : BaseDAL, Interfaces.IDecisionComercialDAL
    {
        // Largos de las columnas: los textos se recortan antes de escribir para no perder la decisión.
        private const int LargoTitulo = 300, LargoDiagnostico = 1000, LargoAccion = 500, LargoMotivo = 500;

        private const string SELECT_BASE =
            "SELECT d.IdDecision, d.Fecha, d.IdUsuario, u.Username AS NombreUsuario, d.TipoRegla, d.ClaveRecomendacion, " +
            "       d.Prioridad, d.Titulo, d.Diagnostico, d.AccionElegida, d.EsAlternativa, d.ImpactoEstimado, d.Estado, " +
            "       d.Motivo, d.TipoReferencia, d.Referencia, d.PospuestaHasta, d.FechaEjecucion, d.FechaEvaluacion, d.ResultadoReal " +
            "FROM DecisionComercial d LEFT JOIN Usuario u ON u.IdUsuario = d.IdUsuario ";

        private static string Recortar(string texto, int largo) =>
            texto == null ? null : (texto.Length <= largo ? texto : texto.Substring(0, largo - 1) + "…");

        private static object Nulo(object v) => v ?? DBNull.Value;

        public int Alta(BE.DecisionComercial d)
        {
            try
            {
                DataTable t = acceso.Leer(
                    "INSERT INTO DecisionComercial (Fecha, IdUsuario, TipoRegla, ClaveRecomendacion, Prioridad, Titulo, Diagnostico, " +
                    "  AccionElegida, EsAlternativa, ImpactoEstimado, Estado, Motivo, TipoReferencia, Referencia, PospuestaHasta, FechaEjecucion) " +
                    "VALUES (@Fecha, @IdUsuario, @TipoRegla, @Clave, @Prioridad, @Titulo, @Diagnostico, @Accion, @EsAlternativa, " +
                    "  @Impacto, @Estado, @Motivo, @TipoReferencia, @Referencia, @PospuestaHasta, @FechaEjecucion); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;",
                    new[]
                    {
                        new SqlParameter("@Fecha", SqlDbType.DateTime) { Value = d.Fecha },
                        new SqlParameter("@IdUsuario", d.IdUsuario),
                        new SqlParameter("@TipoRegla", SqlDbType.NVarChar, 50) { Value = d.TipoRegla ?? "" },
                        new SqlParameter("@Clave", SqlDbType.NVarChar, 100) { Value = Recortar(d.ClaveRecomendacion ?? "", 100) },
                        new SqlParameter("@Prioridad", (int)d.Prioridad),
                        new SqlParameter("@Titulo", SqlDbType.NVarChar, LargoTitulo) { Value = Recortar(d.Titulo ?? "", LargoTitulo) },
                        new SqlParameter("@Diagnostico", SqlDbType.NVarChar, LargoDiagnostico) { Value = Nulo(Recortar(d.Diagnostico, LargoDiagnostico)) },
                        new SqlParameter("@Accion", SqlDbType.NVarChar, LargoAccion) { Value = Nulo(Recortar(d.AccionElegida, LargoAccion)) },
                        new SqlParameter("@EsAlternativa", d.EsAlternativa),
                        new SqlParameter("@Impacto", SqlDbType.Decimal) { Precision = 14, Scale = 2, Value = d.ImpactoEstimado },
                        new SqlParameter("@Estado", (int)d.Estado),
                        new SqlParameter("@Motivo", SqlDbType.NVarChar, LargoMotivo) { Value = Nulo(Recortar(d.Motivo, LargoMotivo)) },
                        new SqlParameter("@TipoReferencia", SqlDbType.NVarChar, 30) { Value = Nulo(d.TipoReferencia) },
                        new SqlParameter("@Referencia", SqlDbType.Int) { Value = Nulo(d.Referencia) },
                        new SqlParameter("@PospuestaHasta", SqlDbType.Date) { Value = Nulo(d.PospuestaHasta?.Date) },
                        new SqlParameter("@FechaEjecucion", SqlDbType.DateTime) { Value = Nulo(d.FechaEjecucion) }
                    });
                return Convert.ToInt32(t.Rows[0]["Id"]);
            }
            catch (Exception ex) { throw new Exception("Error al registrar la decisión comercial.", ex); }
        }

        private const string UPDATE_ESTADO =
            "UPDATE DecisionComercial SET Estado = @Estado, Motivo = @Motivo, PospuestaHasta = @PospuestaHasta, " +
            "  AccionElegida = @Accion, EsAlternativa = @EsAlternativa, ImpactoEstimado = @Impacto, " +
            "  TipoReferencia = @TipoReferencia, Referencia = @Referencia, FechaEjecucion = @FechaEjecucion " +
            "WHERE IdDecision = @Id AND Estado = @Esperado";

        public bool CambiarEstado(BE.DecisionComercial d, BE.EstadoDecision estadoEsperado)
        {
            try
            {
                int filas = acceso.Escribir(UPDATE_ESTADO, ParametrosEstado(d, estadoEsperado));
                return filas == 1;
            }
            catch (Exception ex) { throw new Exception("Error al cambiar el estado de la decisión comercial.", ex); }
        }

        private static SqlParameter[] ParametrosEstado(BE.DecisionComercial d, BE.EstadoDecision estadoEsperado) => new[]
                    {
                        new SqlParameter("@Id", d.IdDecision),
                        new SqlParameter("@Esperado", (int)estadoEsperado),
                        new SqlParameter("@Estado", (int)d.Estado),
                        new SqlParameter("@Motivo", SqlDbType.NVarChar, LargoMotivo) { Value = Nulo(Recortar(d.Motivo, LargoMotivo)) },
                        new SqlParameter("@PospuestaHasta", SqlDbType.Date) { Value = Nulo(d.PospuestaHasta?.Date) },
                        new SqlParameter("@Accion", SqlDbType.NVarChar, LargoAccion) { Value = Nulo(Recortar(d.AccionElegida, LargoAccion)) },
                        new SqlParameter("@EsAlternativa", d.EsAlternativa),
                        new SqlParameter("@Impacto", SqlDbType.Decimal) { Precision = 14, Scale = 2, Value = d.ImpactoEstimado },
                        new SqlParameter("@TipoReferencia", SqlDbType.NVarChar, 30) { Value = Nulo(d.TipoReferencia) },
                        new SqlParameter("@Referencia", SqlDbType.Int) { Value = Nulo(d.Referencia) },
                        new SqlParameter("@FechaEjecucion", SqlDbType.DateTime) { Value = Nulo(d.FechaEjecucion) }
                    };

        // El claim no afectó filas: otra sesión cambió la decisión. Se lanza para revertir la transacción.
        private sealed class DecisionCambiadaException : Exception { }

        public bool EjecutarConDetalle(BE.DecisionComercial d, BE.EstadoDecision estadoEsperado, string tipo, IEnumerable<int> ids)
        {
            var lista = new List<int>(new HashSet<int>(ids ?? new int[0]));
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    using (var cmd = new SqlCommand(UPDATE_ESTADO, cn, tx))
                    {
                        cmd.Parameters.AddRange(ParametrosEstado(d, estadoEsperado));
                        if (cmd.ExecuteNonQuery() != 1) throw new DecisionCambiadaException();
                    }
                    foreach (int id in lista)
                        using (var cmd = new SqlCommand(
                            "IF NOT EXISTS (SELECT 1 FROM DecisionComercialDetalle WHERE IdDecision = @IdDecision AND Tipo = @Tipo AND IdObjeto = @IdObjeto) " +
                            "INSERT INTO DecisionComercialDetalle (IdDecision, Tipo, IdObjeto) VALUES (@IdDecision, @Tipo, @IdObjeto);", cn, tx))
                        {
                            cmd.Parameters.Add(new SqlParameter("@IdDecision", SqlDbType.Int) { Value = d.IdDecision });
                            cmd.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.NVarChar, 20) { Value = tipo });
                            cmd.Parameters.Add(new SqlParameter("@IdObjeto", SqlDbType.Int) { Value = id });
                            cmd.ExecuteNonQuery();
                        }
                });
                return true;
            }
            catch (DecisionCambiadaException) { return false; }
            catch (Exception ex) { throw new Exception("Error al ejecutar la decisión comercial con su detalle.", ex); }
        }

        public bool RegistrarResultado(int idDecision, decimal resultadoReal, DateTime fechaEvaluacion)
        {
            try
            {
                // Solo una decisión Aceptada o Ejecutada tiene resultado (1, 4: BE.EstadoDecision).
                return acceso.Escribir(
                    "UPDATE DecisionComercial SET ResultadoReal = @Resultado, FechaEvaluacion = @Fecha " +
                    "WHERE IdDecision = @Id AND Estado IN (1, 4)",
                    new[]
                    {
                        new SqlParameter("@Id", idDecision),
                        new SqlParameter("@Resultado", SqlDbType.Decimal) { Precision = 14, Scale = 2, Value = resultadoReal },
                        new SqlParameter("@Fecha", SqlDbType.DateTime) { Value = fechaEvaluacion }
                    }) == 1;
            }
            catch (Exception ex) { throw new Exception("Error al registrar el resultado de la decisión comercial.", ex); }
        }

        public BE.DecisionComercial ObtenerPorId(int idDecision)
        {
            var lista = Listar(SELECT_BASE + "WHERE d.IdDecision = @Id", new[] { new SqlParameter("@Id", idDecision) });
            return lista.Count > 0 ? lista[0] : null;
        }

        public List<BE.DecisionComercial> Obtener(DateTime? desde, DateTime? hastaExcl)
        {
            var p = new List<SqlParameter>();
            string where = "WHERE 1 = 1 ";
            if (desde.HasValue) { where += "AND d.Fecha >= @Desde "; p.Add(new SqlParameter("@Desde", SqlDbType.DateTime) { Value = desde.Value }); }
            if (hastaExcl.HasValue) { where += "AND d.Fecha < @HastaExcl "; p.Add(new SqlParameter("@HastaExcl", SqlDbType.DateTime) { Value = hastaExcl.Value }); }
            return Listar(SELECT_BASE + where + "ORDER BY d.Fecha DESC, d.IdDecision DESC", p.ToArray());
        }

        // ── Detalle: qué tocó la decisión (tabla DecisionComercialDetalle, sección 20p del script) ──

        public Dictionary<int, List<int>> ObtenerDetalle(IEnumerable<int> idsDecision, string tipo)
        {
            var resultado = new Dictionary<int, List<int>>();
            var ids = new List<int>(new HashSet<int>(idsDecision ?? new int[0]));
            if (ids.Count == 0) return resultado;
            try
            {
                // Un parámetro por ID (sin concatenar valores en el SQL); en tandas para no pasar el límite.
                for (int inicio = 0; inicio < ids.Count; inicio += 500)
                {
                    var tanda = ids.GetRange(inicio, Math.Min(500, ids.Count - inicio));
                    var p = new List<SqlParameter> { new SqlParameter("@Tipo", SqlDbType.NVarChar, 20) { Value = tipo } };
                    var nombres = new List<string>();
                    for (int i = 0; i < tanda.Count; i++)
                    {
                        nombres.Add("@D" + i);
                        p.Add(new SqlParameter("@D" + i, SqlDbType.Int) { Value = tanda[i] });
                    }
                    DataTable t = acceso.Leer(
                        "SELECT IdDecision, IdObjeto FROM DecisionComercialDetalle WHERE Tipo = @Tipo AND IdDecision IN (" +
                        string.Join(", ", nombres) + ") ORDER BY IdDecision, IdObjeto", p.ToArray());
                    foreach (DataRow r in t.Rows)
                    {
                        int idDecision = Convert.ToInt32(r["IdDecision"]);
                        if (!resultado.TryGetValue(idDecision, out var lista)) resultado[idDecision] = lista = new List<int>();
                        lista.Add(Convert.ToInt32(r["IdObjeto"]));
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Error al obtener el detalle de las decisiones comerciales.", ex); }
            return resultado;
        }

        private List<BE.DecisionComercial> Listar(string sql, SqlParameter[] p)
        {
            var lista = new List<BE.DecisionComercial>();
            try
            {
                foreach (DataRow r in acceso.Leer(sql, p).Rows)
                    lista.Add(new BE.DecisionComercial
                    {
                        IdDecision = Convert.ToInt32(r["IdDecision"]),
                        Fecha = Convert.ToDateTime(r["Fecha"]),
                        IdUsuario = Convert.ToInt32(r["IdUsuario"]),
                        NombreUsuario = r["NombreUsuario"] == DBNull.Value ? null : r["NombreUsuario"].ToString(),
                        TipoRegla = r["TipoRegla"].ToString(),
                        ClaveRecomendacion = r["ClaveRecomendacion"].ToString(),
                        Prioridad = (BE.PrioridadRecomendacion)Convert.ToInt32(r["Prioridad"]),
                        Titulo = r["Titulo"].ToString(),
                        Diagnostico = r["Diagnostico"] == DBNull.Value ? null : r["Diagnostico"].ToString(),
                        AccionElegida = r["AccionElegida"] == DBNull.Value ? null : r["AccionElegida"].ToString(),
                        EsAlternativa = Convert.ToBoolean(r["EsAlternativa"]),
                        ImpactoEstimado = Convert.ToDecimal(r["ImpactoEstimado"]),
                        Estado = (BE.EstadoDecision)Convert.ToInt32(r["Estado"]),
                        Motivo = r["Motivo"] == DBNull.Value ? null : r["Motivo"].ToString(),
                        TipoReferencia = r["TipoReferencia"] == DBNull.Value ? null : r["TipoReferencia"].ToString(),
                        Referencia = r["Referencia"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Referencia"]),
                        PospuestaHasta = r["PospuestaHasta"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PospuestaHasta"]),
                        FechaEjecucion = r["FechaEjecucion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaEjecucion"]),
                        FechaEvaluacion = r["FechaEvaluacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaEvaluacion"]),
                        ResultadoReal = r["ResultadoReal"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["ResultadoReal"])
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener las decisiones comerciales.", ex); }
            return lista;
        }
    }
}
