using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// PN03 — Persistencia de las «Metas comerciales» (tabla MetaComercial, sección 20p del script): el
    /// objetivo mensual de Gerencia para cada indicador (MRR, % de bajas, ocupación del stock…). Consultas
    /// parametrizadas; sin dígito verificador, como DecisionComercial.
    /// </summary>
    public class MetaComercial : BaseDAL, Interfaces.IMetaComercialDAL
    {
        private const string SELECT_BASE =
            "SELECT m.IdMeta, m.Periodo, m.Indicador, m.ValorObjetivo, m.Sentido, m.IdUsuario, u.Username AS NombreUsuario, m.Fecha " +
            "FROM MetaComercial m LEFT JOIN Usuario u ON u.IdUsuario = m.IdUsuario ";

        public List<BE.MetaComercial> Obtener(DateTime desdePeriodo, DateTime hastaPeriodo)
        {
            var lista = new List<BE.MetaComercial>();
            try
            {
                DataTable t = acceso.Leer(SELECT_BASE + "WHERE m.Periodo >= @Desde AND m.Periodo <= @Hasta ORDER BY m.Periodo, m.Indicador",
                    new[]
                    {
                        new SqlParameter("@Desde", SqlDbType.Date) { Value = desdePeriodo.Date },
                        new SqlParameter("@Hasta", SqlDbType.Date) { Value = hastaPeriodo.Date }
                    });
                foreach (DataRow r in t.Rows)
                    lista.Add(new BE.MetaComercial
                    {
                        IdMeta = Convert.ToInt32(r["IdMeta"]),
                        Periodo = Convert.ToDateTime(r["Periodo"]),
                        Indicador = (BE.IndicadorMeta)Convert.ToInt32(r["Indicador"]),
                        ValorObjetivo = Convert.ToDecimal(r["ValorObjetivo"]),
                        Sentido = (BE.SentidoMeta)Convert.ToInt32(r["Sentido"]),
                        IdUsuario = Convert.ToInt32(r["IdUsuario"]),
                        NombreUsuario = r["NombreUsuario"] == DBNull.Value ? null : r["NombreUsuario"].ToString(),
                        Fecha = Convert.ToDateTime(r["Fecha"])
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener las metas comerciales.", ex); }
            return lista;
        }

        public int Alta(BE.MetaComercial meta)
        {
            try
            {
                DataTable t = acceso.Leer(
                    "INSERT INTO MetaComercial (Periodo, Indicador, ValorObjetivo, Sentido, IdUsuario, Fecha) " +
                    "VALUES (@Periodo, @Indicador, @Valor, @Sentido, @IdUsuario, @Fecha); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;",
                    Parametros(meta));
                return Convert.ToInt32(t.Rows[0]["Id"]);
            }
            catch (Exception ex) { throw new Exception("Error al registrar la meta comercial.", ex); }
        }

        public bool Modificar(BE.MetaComercial meta)
        {
            try
            {
                var p = new List<SqlParameter>(Parametros(meta)) { new SqlParameter("@IdMeta", SqlDbType.Int) { Value = meta.IdMeta } };
                return acceso.Escribir(
                    "UPDATE MetaComercial SET ValorObjetivo = @Valor, Sentido = @Sentido, IdUsuario = @IdUsuario, Fecha = @Fecha " +
                    "WHERE IdMeta = @IdMeta AND Periodo = @Periodo AND Indicador = @Indicador", p.ToArray()) == 1;
            }
            catch (Exception ex) { throw new Exception("Error al modificar la meta comercial.", ex); }
        }

        public bool Eliminar(int idMeta)
        {
            try
            {
                return acceso.Escribir("DELETE FROM MetaComercial WHERE IdMeta = @IdMeta",
                    new[] { new SqlParameter("@IdMeta", SqlDbType.Int) { Value = idMeta } }) == 1;
            }
            catch (Exception ex) { throw new Exception("Error al eliminar la meta comercial.", ex); }
        }

        private static SqlParameter[] Parametros(BE.MetaComercial meta) => new[]
        {
            new SqlParameter("@Periodo", SqlDbType.Date) { Value = meta.Periodo.Date },
            new SqlParameter("@Indicador", SqlDbType.Int) { Value = (int)meta.Indicador },
            new SqlParameter("@Valor", SqlDbType.Decimal) { Precision = 14, Scale = 2, Value = meta.ValorObjetivo },
            new SqlParameter("@Sentido", SqlDbType.Int) { Value = (int)meta.Sentido },
            new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = meta.IdUsuario },
            new SqlParameter("@Fecha", SqlDbType.DateTime) { Value = meta.Fecha }
        };
    }
}
