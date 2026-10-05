using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — T07 Dígitos Verificadores.
    ///
    /// Opera sobre:
    ///   [Tabla].DVH        — dígito verificador horizontal por fila de cada tabla protegida
    ///   [DVVertical]       — dígito verificador vertical por tabla
    ///   [ParametroSistema] — versión del FORMATO de los DV ('FormatoDV') y la marca de
    ///                        inicialización pendiente que deja el script de instalación.
    ///
    /// REGLAS (formato 2):
    ///   • Tras cada escritura legítima se recalcula SOLO el DVH de la fila afectada y el DVV se
    ///     recalcula a partir de los DVH ALMACENADOS (no de los recalculados): así una fila
    ///     manipulada por fuera del sistema sigue detectándose aunque después se escriba otra.
    ///   • El recálculo se serializa con sp_getapplock (un bloqueo de aplicación por tabla) dentro
    ///     de su propia transacción, para que dos escrituras simultáneas no se pisen el DVV.
    ///   • Los valores se normalizan con <see cref="Formatear"/> (formato invariante), de modo que
    ///     el DVH no dependa de la cultura regional del equipo.
    ///   • Recalcular TODA una tabla queda reservado al recálculo administrativo explícito
    ///     ("Recalcular todo"/"Asumir pérdida") y a la inicialización tras instalar/actualizar.
    /// </summary>
    public class DigitoVerificador : BaseDAL
    {
        // ── Versión del formato y marcas de instalación ────────────────────────────
        // Formato 2: Usuario incluye Activo/RequiereCambioClave/CantidadBloqueos/FechaBloqueo;
        // Cliente incluye plan, fechas de suscripción, crédito, referente y Activo; Pedido incluye
        // las columnas de PN01 y la confirmación de cada línea; se protegen además Contratacion y
        // PermisoRelacion; fechas y decimales en formato invariante.
        public const int    FormatoActual       = 2;
        public const string ClaveFormato        = "FormatoDV";
        public const string ClavePendiente      = "DVInicializacionPendiente";

        // Prefijo del recurso de sp_getapplock.
        private const string PrefijoBloqueo = "WardrobeFlow.DV.";

        // ── Normalización de valores (fuente única) ───────────────────────────────
        public const string FormatoFecha = "yyyy-MM-ddTHH:mm:ss.fff";

        public static string Formatear(object valor)
        {
            if (valor == null || valor == DBNull.Value) return "";
            switch (valor)
            {
                case DateTime d: return d.ToString(FormatoFecha, CultureInfo.InvariantCulture);
                case decimal m:  return m.ToString(CultureInfo.InvariantCulture);
                case double x:   return x.ToString("R", CultureInfo.InvariantCulture);
                case float x:    return x.ToString("R", CultureInfo.InvariantCulture);
                case bool b:     return b ? "1" : "0";
                default:         return Convert.ToString(valor, CultureInfo.InvariantCulture);
            }
        }

        // ── DVVertical ────────────────────────────────────────────────────────────
        // Lee el DVV almacenado para una tabla. Retorna null si no existe registro.
        public int? ObtenerDVV(string nombreTabla)
        {
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT DVV FROM DVVertical WHERE NombreTabla = @tabla",
                    new SqlParameter[] { new SqlParameter("@tabla", nombreTabla) });

                if (tabla == null || tabla.Rows.Count == 0) return null;
                return Convert.ToInt32(tabla.Rows[0]["DVV"]);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener DVV de la tabla '{nombreTabla}'.", ex);
            }
        }

        internal static void GuardarDVVEnTx(SqlConnection cn, SqlTransaction tx, string nombreTabla, int dvv)
        {
            using (var cmd = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM DVVertical WHERE NombreTabla = @tabla) " +
                "    UPDATE DVVertical SET DVV = @dvv, FechaCalculo = GETDATE() WHERE NombreTabla = @tabla " +
                "ELSE " +
                "    INSERT INTO DVVertical (NombreTabla, DVV, FechaCalculo) VALUES (@tabla, @dvv, GETDATE())",
                cn, tx))
            {
                cmd.Parameters.AddWithValue("@tabla", nombreTabla);
                cmd.Parameters.AddWithValue("@dvv", dvv);
                cmd.ExecuteNonQuery();
            }
        }

        // ── ParametroSistema ──────────────────────────────────────────────────────
        public string ObtenerParametro(string clave)
        {
            DataTable dt = acceso.Leer(
                "SELECT Valor FROM ParametroSistema WHERE Clave = @c",
                new SqlParameter[] { new SqlParameter("@c", clave) });
            return dt == null || dt.Rows.Count == 0 || dt.Rows[0]["Valor"] == DBNull.Value
                ? null : dt.Rows[0]["Valor"].ToString();
        }

        public void GuardarParametro(string clave, string valor)
        {
            acceso.Escribir(
                "IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = @c) " +
                "    UPDATE ParametroSistema SET Valor = @v, Fecha = GETDATE() WHERE Clave = @c " +
                "ELSE " +
                "    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (@c, @v, GETDATE())",
                new SqlParameter[]
                {
                    new SqlParameter("@c", clave),
                    new SqlParameter("@v", (object)valor ?? DBNull.Value)
                });
        }

        // ── Bloqueo de aplicación por tabla ─────────────────────────────────────────
        // Toma un bloqueo EXCLUSIVO de aplicación sobre el DV de la tabla, ligado a la
        // transacción (se libera solo al confirmar o revertir).
        internal static void Bloquear(SqlConnection cn, SqlTransaction tx, string tabla)
        {
            using (var cmd = new SqlCommand("sp_getapplock", cn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Resource", PrefijoBloqueo + tabla);
                cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
                cmd.Parameters.AddWithValue("@LockOwner", "Transaction");
                cmd.Parameters.AddWithValue("@LockTimeout", 15000);
                var ret = cmd.Parameters.Add("@ret", SqlDbType.Int);
                ret.Direction = ParameterDirection.ReturnValue;
                cmd.ExecuteNonQuery();
                if ((int)ret.Value < 0)
                    throw new Exception($"No se pudo obtener el bloqueo del dígito verificador de '{tabla}'.");
            }
        }

        // Ejecuta 'accion' en una transacción propia, con el bloqueo del DV de la tabla tomado
        // ANTES de leer nada (evita bloqueos mutuos entre recálculos simultáneos).
        public void EjecutarConBloqueo(string tabla, Action<SqlConnection, SqlTransaction> accion)
        {
            acceso.EjecutarTransaccion((cn, tx) =>
            {
                Bloquear(cn, tx, tabla);
                accion(cn, tx);
            });
        }

        internal static DataTable LeerEnTx(SqlConnection cn, SqlTransaction tx, string sql, params SqlParameter[] p)
        {
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                if (p != null) cmd.Parameters.AddRange(p);
                var dt = new DataTable();
                using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
                return dt;
            }
        }

        // Recalcula el DVV de una tabla con los DVH ALMACENADOS (NULL cuenta como 0), en el orden
        // de 'ordenarPor' (el mismo que usa la verificación). Debe correr con el bloqueo tomado.
        internal static void GuardarDVVDesdeAlmacenadosEnTx(SqlConnection cn, SqlTransaction tx,
                                                            string tabla, string ordenarPor)
        {
            var dt = LeerEnTx(cn, tx,
                "SELECT ISNULL(DVH, 0) AS DVH FROM " + SqlIdentificador.Validar(tabla) + " ORDER BY " + ordenarPor);
            var dvhs = new List<int>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) dvhs.Add(Convert.ToInt32(r["DVH"]));
            GuardarDVVEnTx(cn, tx, tabla, Seguridad.CalculadorDV.Crear().CalcularDVV(dvhs));
        }

        // Arma el ORDER BY validado a partir de las columnas de la clave.
        internal static string OrdenPor(params string[] columnasClave)
        {
            var partes = new string[columnasClave.Length];
            for (int i = 0; i < columnasClave.Length; i++) partes[i] = SqlIdentificador.Validar(columnasClave[i]);
            return string.Join(", ", partes);
        }

        // ── Usuario ───────────────────────────────────────────────────────────────
        internal const string SelectFilasUsuario =
            "SELECT IdUsuario, Username, Clave, Rol, Perfil, Estado, IntentosFallidos, " +
            "       Activo, RequiereCambioClave, CantidadBloqueos, FechaBloqueo, DVH FROM Usuario";

        internal static BE.FilaUsuarioDV MapearFilaUsuario(DataRow row)
        {
            return new BE.FilaUsuarioDV
            {
                Id                  = Convert.ToInt32(row["IdUsuario"]),
                Username            = row["Username"].ToString(),
                Clave               = row["Clave"].ToString(),
                Rol                 = row["Rol"] != DBNull.Value ? row["Rol"].ToString() : "",
                Perfil              = row["Perfil"] != DBNull.Value ? row["Perfil"].ToString() : "",
                Estado              = row["Estado"] != DBNull.Value ? Convert.ToInt32(row["Estado"]).ToString() : "0",
                IntentosFallidos    = row["IntentosFallidos"] != DBNull.Value ? Convert.ToInt32(row["IntentosFallidos"]).ToString() : "0",
                Activo              = row["Activo"] != DBNull.Value ? Convert.ToInt32(row["Activo"]).ToString() : "0",
                RequiereCambioClave = row["RequiereCambioClave"] != DBNull.Value ? Convert.ToInt32(row["RequiereCambioClave"]).ToString() : "0",
                CantidadBloqueos    = row["CantidadBloqueos"] != DBNull.Value ? Convert.ToInt32(row["CantidadBloqueos"]).ToString() : "0",
                FechaBloqueo        = Formatear(row["FechaBloqueo"]),
                DVHAlmacenado       = row["DVH"] != DBNull.Value ? (int?)Convert.ToInt32(row["DVH"]) : null
            };
        }

        // Lee todas las filas de Usuario con sus DVH almacenados, ordenadas por IdUsuario.
        public List<BE.FilaUsuarioDV> ObtenerFilasUsuario()
        {
            var lista = new List<BE.FilaUsuarioDV>();
            try
            {
                DataTable tabla = acceso.Leer(SelectFilasUsuario + " ORDER BY IdUsuario", null);
                if (tabla == null) return lista;
                foreach (DataRow row in tabla.Rows) lista.Add(MapearFilaUsuario(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener filas de Usuario para verificación DV.", ex);
            }
            return lista;
        }

        // Recalcula TODOS los DVH de Usuario y su DVV con el bloqueo del DV tomado y devuelve las
        // filas ya recalculadas (para reconstruir el espejo). Solo para la inicialización tras
        // instalar y el recálculo administrativo explícito.
        public List<BE.FilaUsuarioDV> RecalcularTablaUsuario()
        {
            var filas = new List<BE.FilaUsuarioDV>();
            EjecutarConBloqueo("Usuario", (cn, tx) =>
            {
                var svc = Seguridad.CalculadorDV.Crear();
                var dt = LeerEnTx(cn, tx, SelectFilasUsuario + " ORDER BY IdUsuario");
                var dvhs = new List<int>();
                foreach (DataRow r in dt.Rows)
                {
                    var fila = MapearFilaUsuario(r);
                    int dvh = svc.CalcularDVH(fila.CamposParaDVH());
                    using (var cmd = new SqlCommand("UPDATE Usuario SET DVH=@dvh WHERE IdUsuario=@id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@dvh", dvh);
                        cmd.Parameters.AddWithValue("@id", fila.Id);
                        cmd.ExecuteNonQuery();
                    }
                    fila.DVHAlmacenado = dvh;
                    filas.Add(fila);
                    dvhs.Add(dvh);
                }
                GuardarDVVEnTx(cn, tx, "Usuario", svc.CalcularDVV(dvhs));
            });
            return filas;
        }

        // ── DV GENÉRICO (reutilizable para cualquier tabla protegida) ───────────
        // tabla / pkCol / columnas son CONSTANTES del sistema (no entradas de usuario).
        // Como acá los identificadores NO pueden ir parametrizados (SQL no admite @param
        // para nombres de tabla/columna), se aplica defensa en profundidad: cada identificador
        // se VALIDA contra una lista blanca de identificadores simples y se encierra en
        // corchetes [ ], de modo que sea imposible inyectar SQL aunque una constante cambie.

        private static string SelectGenerico(string tabla, string pkCol, string[] columnas)
        {
            var colsQuoted = new string[columnas.Length];
            for (int i = 0; i < columnas.Length; i++) colsQuoted[i] = SqlIdentificador.Validar(columnas[i]);
            return "SELECT " + SqlIdentificador.Validar(pkCol) + ", " + string.Join(", ", colsQuoted) +
                   ", DVH FROM " + SqlIdentificador.Validar(tabla);
        }

        private static BE.FilaDV MapearGenerico(DataRow row, string tabla, string pkCol, string[] columnas)
        {
            var campos = new string[columnas.Length + 1];
            campos[0] = Formatear(row[pkCol]);                // la PK entra al DVH
            for (int i = 0; i < columnas.Length; i++) campos[i + 1] = Formatear(row[columnas[i]]);
            return new BE.FilaDV
            {
                Id            = Convert.ToInt32(row[pkCol]),
                Campos        = campos,
                DVHAlmacenado = row["DVH"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DVH"]),
                Descripcion   = tabla + " #" + row[pkCol]
            };
        }

        // Lee las filas de una tabla con sus campos relevantes para el DVH y el DVH almacenado.
        public List<BE.FilaDV> ObtenerFilas(string tabla, string pkCol, string[] columnas)
        {
            DataTable dt = acceso.Leer(
                SelectGenerico(tabla, pkCol, columnas) + " ORDER BY " + SqlIdentificador.Validar(pkCol), null);
            var lista = new List<BE.FilaDV>();
            if (dt == null) return lista;
            foreach (DataRow row in dt.Rows) lista.Add(MapearGenerico(row, tabla, pkCol, columnas));
            return lista;
        }

        /// <summary>
        /// Recalcula el DVH de UNA fila (si todavía existe) y el DVV de la tabla a partir de los DVH
        /// almacenados, con el bloqueo del DV tomado. Es lo que corresponde llamar después de cada
        /// escritura legítima (alta, modificación, baja física) sobre la tabla protegida.
        /// </summary>
        public void ActualizarFila(string tabla, string pkCol, string[] columnas, int id)
        {
            EjecutarConBloqueo(tabla, (cn, tx) =>
            {
                ActualizarFilaEnTx(cn, tx, tabla, pkCol, columnas, id);
                GuardarDVVDesdeAlmacenadosEnTx(cn, tx, tabla, OrdenPor(pkCol));
            });
        }

        internal static void ActualizarFilaEnTx(SqlConnection cn, SqlTransaction tx,
                                                string tabla, string pkCol, string[] columnas, int id)
        {
            var dt = LeerEnTx(cn, tx,
                SelectGenerico(tabla, pkCol, columnas) + " WHERE " + SqlIdentificador.Validar(pkCol) + " = @id",
                new SqlParameter("@id", id));
            if (dt.Rows.Count == 0) return;   // la fila ya no existe (baja física): solo cambia el DVV
            var fila = MapearGenerico(dt.Rows[0], tabla, pkCol, columnas);
            int dvh = Seguridad.CalculadorDV.Crear().CalcularDVH(fila.Campos);
            using (var cmd = new SqlCommand(
                "UPDATE " + SqlIdentificador.Validar(tabla) + " SET DVH = @dvh WHERE " + SqlIdentificador.Validar(pkCol) + " = @id",
                cn, tx))
            {
                cmd.Parameters.AddWithValue("@dvh", dvh);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Recalcula y persiste el DVH de TODAS las filas y el DVV de la tabla. Acepta los datos
        /// actuales como legítimos: se usa SOLO en el recálculo administrativo explícito
        /// ("Recalcular todo"/"Asumir pérdida") y en la inicialización tras instalar/actualizar.
        /// </summary>
        public void RecalcularTabla(string tabla, string pkCol, string[] columnas)
        {
            EjecutarConBloqueo(tabla, (cn, tx) =>
            {
                var svc = Seguridad.CalculadorDV.Crear();
                var dt = LeerEnTx(cn, tx,
                    SelectGenerico(tabla, pkCol, columnas) + " ORDER BY " + SqlIdentificador.Validar(pkCol));
                var dvhs = new List<int>();
                foreach (DataRow row in dt.Rows)
                {
                    var f = MapearGenerico(row, tabla, pkCol, columnas);
                    int dvh = svc.CalcularDVH(f.Campos);
                    using (var cmd = new SqlCommand(
                        "UPDATE " + SqlIdentificador.Validar(tabla) + " SET DVH = @dvh WHERE " + SqlIdentificador.Validar(pkCol) + " = @id",
                        cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@dvh", dvh);
                        cmd.Parameters.AddWithValue("@id", f.Id);
                        cmd.ExecuteNonQuery();
                    }
                    dvhs.Add(dvh);
                }
                GuardarDVVEnTx(cn, tx, tabla, svc.CalcularDVV(dvhs));
            });
        }
    }
}
