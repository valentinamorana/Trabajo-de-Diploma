using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace DbInstaller
{
    // Cliente SQL embebido para el instalador (PPT, "A01: Instalación de Base
    // Datos": "Creación de Tablas y Esquema: Automatización mediante cliente SQL
    // embebido..."). Reemplaza la dependencia de sqlcmd.exe (herramienta externa
    // que el cliente podía no tener instalada) por ADO.NET puro, que ya viaja
    // con el .NET Framework que la propia app requiere.
    //
    // Uso: DbInstaller.exe <servidor> <script.sql> <log.txt>
    // Códigos de salida: 0 = OK, 1 = error de uso/argumentos, 2 = falló el script.
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Uso: DbInstaller.exe <servidor> <script.sql> <log.txt>");
                return 1;
            }

            string servidor = args[0];
            string scriptPath = args[1];
            string logPath = args[2];
            var log = new StringBuilder();
            Action<string> registrar = mensaje =>
                log.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}");

            int resultado;
            try
            {
                resultado = EjecutarScript(servidor, scriptPath, registrar);
            }
            catch (Exception ex)
            {
                registrar("ERROR inesperado: " + ex);
                resultado = 2;
            }
            finally
            {
                try
                {
                    File.AppendAllText(logPath, log.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // Si no se puede escribir el log no hay nada más que hacer acá;
                    // el código de salida ya le informa al instalador si falló.
                }
            }

            return resultado;
        }

        private static int EjecutarScript(string servidor, string scriptPath, Action<string> registrar)
        {
            if (!File.Exists(scriptPath))
            {
                registrar($"ERROR: no se encontró el script '{scriptPath}'.");
                return 2;
            }

            string script = File.ReadAllText(scriptPath, Encoding.UTF8);
            string[] batches = SplitEnBatches(script);

            string connectionString =
                $"Data Source={servidor};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30";

            registrar($"Conectando a '{servidor}'...");
            using (var conexion = new SqlConnection(connectionString))
            {
                conexion.Open();
                registrar($"Conexión establecida. Ejecutando script ({batches.Length} batches)...");

                for (int i = 0; i < batches.Length; i++)
                {
                    string batch = batches[i].Trim();
                    if (batch.Length == 0) continue;

                    using (var cmd = new SqlCommand(batch, conexion))
                    {
                        cmd.CommandTimeout = 120;
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (SqlException ex)
                        {
                            registrar($"ERROR en batch {i + 1}/{batches.Length}: {ex.Message}");
                            registrar("--- Contenido del batch que falló ---");
                            registrar(batch);
                            return 2;
                        }
                    }
                }
            }

            registrar("Script ejecutado correctamente.");
            return 0;
        }

        // Separa el script en batches por líneas que son "GO" (igual que
        // sqlcmd/SSMS). El script del proyecto solo usa "GO" simple, sin
        // "GO N" (repetición), así que no hace falta soportar esa variante.
        private static string[] SplitEnBatches(string script)
        {
            string[] lineas = script.Replace("\r\n", "\n").Split('\n');
            var batches = new List<string>();
            var actual = new StringBuilder();

            foreach (string linea in lineas)
            {
                if (linea.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    batches.Add(actual.ToString());
                    actual.Clear();
                }
                else
                {
                    actual.AppendLine(linea);
                }
            }

            if (actual.Length > 0)
                batches.Add(actual.ToString());

            return batches.ToArray();
        }
    }
}
