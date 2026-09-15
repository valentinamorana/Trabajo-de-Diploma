using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace DbInstaller
{
    // Cliente SQL embebido para el instalador (PPT, "A01: Instalación de Base
    // Datos": "Creación de Tablas y Esquema: Automatización mediante cliente SQL
    // embebido..."). Reemplaza la dependencia de sqlcmd.exe / herramientas
    // externas por ADO.NET + ServiceController, que ya viajan con el .NET
    // Framework que la propia app requiere.
    //
    // Uso:
    //   DbInstaller.exe run-script      <servidor> <script.sql> <log.txt>
    //   DbInstaller.exe check-service   <nombreServicio> <log.txt>
    //   DbInstaller.exe test-connection <servidor> <log.txt>
    //
    // Códigos de salida (comunes a los tres subcomandos):
    //   0 = OK   1 = uso incorrecto   2/3 = falló la operación (ver log)
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine(Uso);
                return 1;
            }

            string logPath = args[args.Length - 1];
            var log = new StringBuilder();
            Action<string> registrar = mensaje =>
                log.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}");

            int resultado;
            try
            {
                switch (args[0])
                {
                    case "run-script" when args.Length == 4:
                        resultado = EjecutarScript(args[1], args[2], registrar);
                        break;
                    case "check-service" when args.Length == 3:
                        resultado = ChequearYArrancarServicio(args[1], registrar);
                        break;
                    case "test-connection" when args.Length == 3:
                        resultado = ProbarConexion(args[1], registrar);
                        break;
                    case "drop-database" when args.Length == 4:
                        resultado = BorrarBaseDeDatos(args[1], args[2], registrar);
                        break;
                    default:
                        Console.Error.WriteLine(Uso);
                        return 1;
                }
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

        private const string Uso =
            "Uso:\n" +
            "  DbInstaller.exe run-script      <servidor> <script.sql> <log.txt>\n" +
            "  DbInstaller.exe check-service   <nombreServicio> <log.txt>\n" +
            "  DbInstaller.exe test-connection <servidor> <log.txt>\n" +
            "  DbInstaller.exe drop-database   <servidor> <nombreBD> <log.txt>";

        private static int EjecutarScript(string servidor, string scriptPath, Action<string> registrar)
        {
            if (!File.Exists(scriptPath))
            {
                registrar($"ERROR: no se encontró el script '{scriptPath}'.");
                return 2;
            }

            string script = File.ReadAllText(scriptPath, Encoding.UTF8);
            string[] batches = SplitEnBatches(script);

            registrar($"Conectando a '{servidor}'...");
            using (var conexion = new SqlConnection(CadenaConexion(servidor)))
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

        // Pre-flight check (PPT, slide "Resiliencia y Flujo UX"): probar que se
        // puede conectar SIN ejecutar nada, usado para validar un servidor/
        // instancia ingresado a mano antes de intentar crear la base de datos.
        private static int ProbarConexion(string servidor, Action<string> registrar)
        {
            registrar($"Probando conexión a '{servidor}'...");
            try
            {
                using (var conexion = new SqlConnection(CadenaConexion(servidor)))
                {
                    conexion.Open();
                }
                registrar("Conexión OK.");
                return 0;
            }
            catch (Exception ex)
            {
                registrar("No se pudo conectar: " + ex.Message);
                return 2;
            }
        }

        // Verifica el servicio de Windows del motor SQL y lo arranca si está
        // detenido (PPT, slide "Servicio SQL Detenido": "Verificación: Consultar
        // ServiceController en Windows. Arranque Automático: Intentar iniciar el
        // servicio (service.Start())."). Códigos: 0 = corriendo (ya lo estaba o
        // se arrancó ahora), 2 = el servicio no existe, 3 = existe pero no arrancó.
        private static int ChequearYArrancarServicio(string nombreServicio, Action<string> registrar)
        {
            ServiceController servicio;
            try
            {
                servicio = new ServiceController(nombreServicio);
                var status = servicio.Status; // fuerza la lectura; tira si el servicio no existe
                registrar($"Servicio '{nombreServicio}' encontrado. Estado: {status}.");
            }
            catch (Exception)
            {
                registrar($"ERROR: el servicio '{nombreServicio}' no existe en este equipo.");
                return 2;
            }

            if (servicio.Status == ServiceControllerStatus.Running)
            {
                registrar("El servicio ya estaba en ejecución.");
                return 0;
            }

            registrar("El servicio está detenido. Intentando iniciarlo...");
            try
            {
                servicio.Start();
                servicio.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                registrar("Servicio iniciado correctamente.");
                return 0;
            }
            catch (Exception ex)
            {
                registrar("No se pudo iniciar el servicio (¿faltan permisos de administrador?): " + ex.Message);
                return 3;
            }
        }

        // Usada al desinstalar, solo si el usuario confirma explícitamente que
        // quiere borrar también los datos (ver [Code] del .iss, opción "No" por
        // defecto). nombreBD siempre es la constante fija de la app (no viene
        // de input del usuario), así que la interpolación directa es segura acá.
        private static int BorrarBaseDeDatos(string servidor, string nombreBD, Action<string> registrar)
        {
            registrar($"Conectando a '{servidor}' para eliminar la base '{nombreBD}'...");
            try
            {
                using (var conexion = new SqlConnection(CadenaConexion(servidor)))
                {
                    conexion.Open();
                    string sql =
                        $"IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{nombreBD}') " +
                        $"BEGIN ALTER DATABASE [{nombreBD}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nombreBD}]; END";
                    using (var cmd = new SqlCommand(sql, conexion))
                    {
                        cmd.CommandTimeout = 60;
                        cmd.ExecuteNonQuery();
                    }
                }
                registrar("Base de datos eliminada (si existía).");
                return 0;
            }
            catch (Exception ex)
            {
                registrar("No se pudo eliminar la base de datos: " + ex.Message);
                return 2;
            }
        }

        private static string CadenaConexion(string servidor) =>
            $"Data Source={servidor};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15";

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
