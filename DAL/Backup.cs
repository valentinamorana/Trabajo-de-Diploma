using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DAL
{
    public class Backup : BaseDAL, Interfaces.IBackupDAL
    {
        private readonly string _cadenaConexionMaster;

        public Backup()
        {
            var entrada = ConfigurationManager.ConnectionStrings["WardrobeFlowDB"];
            if (entrada != null)
            {
                var builder = new SqlConnectionStringBuilder(entrada.ConnectionString);
                builder.InitialCatalog = "master";
                _cadenaConexionMaster = builder.ConnectionString;
            }
        }

        // Directorio temporal que la CUENTA DE SERVICIO de SQL Server pueda LEER (para BACKUP/RESTORE
        // y RESTORE VERIFYONLY). C:\Users\Public es legible por todos los servicios; el %TEMP% del
        // usuario (AppData\Local\Temp) NO lo es → causaba "Operating system error 5 (Access is denied)"
        // al restaurar. Por eso se usa Public como ubicación principal.
        public static string DirectorioTempSeguro()
        {
            string publico = Environment.GetEnvironmentVariable("PUBLIC");
            if (string.IsNullOrEmpty(publico)) publico = @"C:\Users\Public";
            string dir = Path.Combine(publico, "WardrobeFlow_Temp");
            try { if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); return dir; }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "[Backup.DirectorioTempSeguro] No se pudo usar la carpeta Public, se prueba el fallback: " + ex.Message);
            }

            // Fallback: carpeta junto al ejecutable con permiso de lectura para Todos.
            string alt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempBackups");
            try
            {
                if (!Directory.Exists(alt)) Directory.CreateDirectory(alt);
                var dInfo = new DirectoryInfo(alt);
                var sec   = dInfo.GetAccessControl();
                sec.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None, AccessControlType.Allow));
                dInfo.SetAccessControl(sec);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "[Backup.DirectorioTempSeguro] No se pudo fijar la ACL en la carpeta de respaldo temporal: " + ex.Message);
            }
            return alt;
        }

        private string ObtenerDirectorioTemp() => DirectorioTempSeguro();

        /// <summary>
        /// Borra las copias SIN CIFRAR que hayan quedado de un backup o una restauración que se
        /// cortó (corte de luz, proceso terminado): cada operación borra la suya al terminar, pero un
        /// corte deja el .bak plano en la carpeta temporal, legible por otros usuarios de la PC. Solo
        /// borra las de más de una hora (una operación en curso de otra sesión sigue intacta).
        /// Devuelve cuántas borró; nunca lanza.
        /// </summary>
        public static int LimpiarTemporalesHuerfanos()
        {
            int borrados = 0;
            var carpetas = new List<string> { Path.GetTempPath() };
            string publico = Environment.GetEnvironmentVariable("PUBLIC");
            carpetas.Add(Path.Combine(string.IsNullOrEmpty(publico) ? @"C:\Users\Public" : publico, "WardrobeFlow_Temp"));
            carpetas.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempBackups"));
            // Parciales de la copia final (~WF_*.partial): en la carpeta Backups (automáticos) o en
            // el temporal del usuario (manuales, antes de cifrar).
            carpetas.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"));
            DateTime limite = DateTime.Now.AddHours(-1);
            foreach (string dir in carpetas)
            {
                try
                {
                    if (!Directory.Exists(dir)) continue;
                    foreach (string patron in new[] { "WF_bak_*.bak", "WF_restore_*.bak", "WardrobeFlow_Temp_*.bak", "WardrobeFlow_Restore_*.bak", "~WF_*.partial" })
                        foreach (string archivo in Directory.GetFiles(dir, patron))
                            try
                            {
                                // File.Copy conserva la fecha de modificación del original: se toma la
                                // más reciente entre creación y modificación (una copia recién hecha de un
                                // .bak viejo no cuenta como huérfana).
                                DateTime ultima = File.GetCreationTime(archivo) > File.GetLastWriteTime(archivo)
                                    ? File.GetCreationTime(archivo) : File.GetLastWriteTime(archivo);
                                if (ultima >= limite) continue;
                                File.Delete(archivo);
                                borrados++;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceWarning($"[DAL.Backup] No se pudo borrar el temporal huérfano '{archivo}': {ex.Message}");
                            }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"[DAL.Backup] No se pudo revisar '{dir}': {ex.Message}");
                }
            }
            return borrados;
        }

        public void RealizarBackup(string rutaDestino)
        {
            if (string.IsNullOrEmpty(_cadenaConexionMaster))
                throw new InvalidOperationException("Cadena de conexión no configurada.");

            string dirTemp  = ObtenerDirectorioTemp();
            string rutaTemp = Path.Combine(dirTemp, $"WardrobeFlow_Temp_{Guid.NewGuid():N}.bak");
            string rutaParcial = null;

            try
            {
                using (var conn = new SqlConnection(_cadenaConexionMaster))
                using (var cmd  = new SqlCommand(
                    "BACKUP DATABASE WardrobeFlowDB TO DISK = @Ruta WITH FORMAT, INIT, NAME = 'WardrobeFlowBackup';",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@Ruta", rutaTemp);
                    cmd.CommandTimeout = 120;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                // Se copia a un parcial en la carpeta de destino y recién al final se reemplaza:
                // si la copia falla (disco lleno, corte), el archivo anterior queda intacto. Importa
                // en los automáticos, que pisan siempre el mismo auto_<día>.bak.
                rutaParcial = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(rutaDestino)),
                                           $"~WF_{Guid.NewGuid():N}.partial");
                File.Copy(rutaTemp, rutaParcial);
                // Replace cambia el archivo en un solo paso: si falla, el anterior sigue ahí.
                if (File.Exists(rutaDestino)) File.Replace(rutaParcial, rutaDestino, null);
                else                          File.Move(rutaParcial, rutaDestino);
            }
            finally
            {
                if (rutaParcial != null && File.Exists(rutaParcial))
                    try { File.Delete(rutaParcial); } catch (Exception exParcial) { System.Diagnostics.Trace.TraceWarning($"[DAL.Backup] No se pudo borrar el parcial '{rutaParcial}': {exParcial.Message}"); }
                if (File.Exists(rutaTemp))
                    try { File.Delete(rutaTemp); } catch (Exception exTemp) { System.Diagnostics.Trace.TraceWarning($"[DAL.Backup] No se pudo borrar el temporal '{rutaTemp}': {exTemp.Message}"); }
            }
        }

        // RNF-02 — Verifica que un archivo .bak sea un backup VÁLIDO y no esté corrupto,
        // usando RESTORE VERIFYONLY (chequea integridad del conjunto de backup sin restaurar).
        // Lanza excepción si el backup es ilegible/corrupto. Se llama ANTES de cualquier restauración.
        public void VerificarBackup(string rutaArchivo)
        {
            if (string.IsNullOrEmpty(_cadenaConexionMaster))
                throw new InvalidOperationException("Cadena de conexión no configurada.");
            if (!File.Exists(rutaArchivo))
                throw new FileNotFoundException("El archivo de backup no existe.", rutaArchivo);

            try
            {
                using (var conn = new SqlConnection(_cadenaConexionMaster))
                using (var cmd  = new SqlCommand("RESTORE VERIFYONLY FROM DISK = @Ruta;", conn))
                {
                    cmd.Parameters.AddWithValue("@Ruta", rutaArchivo);
                    cmd.CommandTimeout = 120;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                // VERIFYONLY falla cuando el .bak está corrupto, truncado o no es un backup válido.
                throw new BE.AppException("err.dal.backup_corrupto",
                    "El archivo de backup es inválido o está corrupto y no puede restaurarse.\n\nDetalle: " + ex.Message);
            }
        }

        // RF-08 — Devuelve la fecha en que se completó el backup (BackupFinishDate del header),
        // para informar al administrador el alcance de la pérdida antes de restaurar.
        // Retorna null si no puede leerse el header.
        public DateTime? ObtenerFechaBackup(string rutaArchivo)
        {
            if (string.IsNullOrEmpty(_cadenaConexionMaster) || !File.Exists(rutaArchivo))
                return null;
            try
            {
                using (var conn = new SqlConnection(_cadenaConexionMaster))
                using (var cmd  = new SqlCommand("RESTORE HEADERONLY FROM DISK = @Ruta;", conn))
                {
                    cmd.Parameters.AddWithValue("@Ruta", rutaArchivo);
                    cmd.CommandTimeout = 120;
                    conn.Open();
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            int col = rd.GetOrdinal("BackupFinishDate");
                            if (!rd.IsDBNull(col)) return rd.GetDateTime(col);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Header ilegible → null (la verificación de corrupción se hace en VerificarBackup),
                // pero se deja constancia por si en realidad es un problema de conectividad real.
                System.Diagnostics.Trace.TraceWarning(
                    $"[DAL.Backup] No se pudo leer el header del backup '{rutaArchivo}': {ex.Message}");
            }
            return null;
        }

        // RF-08 — Cuenta, sobre la base de datos ACTUAL (en vivo), los registros con fecha
        // POSTERIOR a la del backup: son exactamente los que se perderían al restaurar.
        // Se recorren las tablas de negocio que llevan marca temporal. Cada tabla se cuenta
        // de forma independiente y tolerante a fallos (si una no existe en una BD legacy,
        // se omite esa línea en vez de abortar todo el preview).
        public List<BE.CambioPosterior> ContarCambiosPosterioresA(DateTime fecha)
        {
            // (Entidad legible, Tabla, Columna de fecha). El orden define el del resumen.
            var fuentes = new (string Entidad, string Tabla, string Columna)[]
            {
                ("Pedidos",                       "Pedido",             "FechaPedido"),
                ("Clientes nuevos",               "Cliente",            "FechaAlta"),
                ("Prendas nuevas",                "Prenda",             "FechaAlta"),
                ("Cambios de estado de pedidos",  "PedidoHistorial",    "Fecha"),
                ("Mantenimientos de prendas",     "MantenimientoPrenda","FechaEntrada"),
                ("Cambios sobre usuarios",        "HistorialUsuario",   "Fecha"),
                ("Movimientos de negocio",        "BitacoraNegocio",    "Fecha"),
                ("Registros de bitácora",         "Bitacora",           "fecha"),
            };

            var resultado = new List<BE.CambioPosterior>();

            foreach (var f in fuentes)
            {
                try
                {
                    // Tabla/columna son CONSTANTES del sistema (no entrada de usuario) y se encierran
                    // en corchetes; la fecha SÍ va parametrizada. Defensa en profundidad igual que el DV.
                    string sql = "SELECT COUNT(*) AS Cantidad FROM " + SqlIdentificador.Validar(f.Tabla) +
                                 " WHERE " + SqlIdentificador.Validar(f.Columna) + " > @fecha";
                    DataTable dt = acceso.Leer(sql, new[] { new SqlParameter("@fecha", fecha) });
                    int cant = (dt != null && dt.Rows.Count > 0) ? Convert.ToInt32(dt.Rows[0]["Cantidad"]) : 0;
                    if (cant > 0)
                        resultado.Add(new BE.CambioPosterior { Entidad = f.Entidad, Cantidad = cant });
                }
                catch (Exception ex)
                {
                    // Tabla inexistente / columna ausente en una BD vieja → se omite esa fuente,
                    // pero se deja constancia por si en realidad es un problema de conectividad real.
                    System.Diagnostics.Trace.TraceWarning(
                        $"[DAL.Backup] No se pudo contar cambios posteriores en '{f.Tabla}': {ex.Message}");
                }
            }

            return resultado;
        }

        public void RestaurarBackup(string rutaOrigen)
        {
            if (string.IsNullOrEmpty(_cadenaConexionMaster))
                throw new InvalidOperationException("Cadena de conexión no configurada.");

            string dirTemp  = ObtenerDirectorioTemp();
            string rutaTemp = Path.Combine(dirTemp, $"WardrobeFlow_Restore_{Guid.NewGuid():N}.bak");

            try
            {
                // Copiar PRIMERO a una ubicación legible por la cuenta de SQL Server, y recién ahí
                // verificar/restaurar. (La app puede leer el origen aunque SQL no — ej. %TEMP% del
                // usuario o Documentos. Antes se hacía VERIFYONLY sobre el origen → Access denied.)
                File.Copy(rutaOrigen, rutaTemp, true);

                // RNF-02 — No se restaura un backup corrupto: validar integridad de la copia accesible.
                VerificarBackup(rutaTemp);

                // El RESTORE va en TRY/CATCH: si falla (disco lleno, corrupción que VERIFYONLY no
                // detectó, etc.) DESPUÉS de que SINGLE_USER ya se aplicó, sin este guard la base
                // queda bloqueada en modo single-user para siempre, sin ningún camino de código
                // para recuperarla. El CATCH fuerza MULTI_USER de vuelta y relanza el error real.
                const string sql =
                    "ALTER DATABASE WardrobeFlowDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    "BEGIN TRY " +
                    "    RESTORE DATABASE WardrobeFlowDB FROM DISK = @Ruta WITH REPLACE; " +
                    "END TRY " +
                    "BEGIN CATCH " +
                    "    ALTER DATABASE WardrobeFlowDB SET MULTI_USER; " +
                    "    THROW; " +
                    "END CATCH " +
                    "ALTER DATABASE WardrobeFlowDB SET MULTI_USER;";

                using (var conn = new SqlConnection(_cadenaConexionMaster))
                using (var cmd  = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ruta", rutaTemp);
                    cmd.CommandTimeout = 240;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (File.Exists(rutaTemp))
                    try { File.Delete(rutaTemp); } catch (Exception exTemp) { System.Diagnostics.Trace.TraceWarning($"[DAL.Backup] No se pudo borrar el temporal '{rutaTemp}': {exTemp.Message}"); }
            }
        }
    }
}
