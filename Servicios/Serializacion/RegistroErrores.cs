using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — cada error inesperado de la aplicación queda serializado en XML
    /// (un archivo por mes: Errores\errores_AAAAMM.xml en la carpeta de datos local de WardrobeFlow,
    /// que arma GestorArchivos), además del renglón que ya va a la bitácora. El archivo mensual es
    /// siempre XML; desde la pantalla de Bitácora (GUI.ErroresXmlForm, vía BLL.ErroresSerializados)
    /// los errores se exportan e importan en XML o JSON (por defecto en Errores\Exportados, aparte
    /// del registro mensual).
    /// Registrar nunca lanza: si no puede escribir, el error original igual se muestra.
    /// </summary>
    public static class RegistroErrores
    {
        /// <summary>Errores que se conservan por archivo mensual (los más viejos se descartan).</summary>
        public const int MaximoPorArchivo = 500;

        private static readonly object Candado = new object();

        // Solo el nombre exacto que arma RutaDelMes: una exportación u otro archivo de la carpeta
        // (por ejemplo, errores_wardrobeflow_20261020_101010.xml) nunca se lee como registro mensual.
        private static readonly Regex NombreMensual = new Regex(@"^errores_\d{6}\.xml$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Carpeta de los archivos: %LOCALAPPDATA%\WardrobeFlow\Errores (GestorArchivos).</summary>
        public static string Carpeta => GestorArchivos.Instancia.Carpeta(GestorArchivos.SubcarpetaErrores);

        /// <summary>Crea la carpeta de errores si no existe y devuelve su ruta.</summary>
        public static string PrepararCarpeta() => GestorArchivos.Instancia.PrepararCarpeta(GestorArchivos.SubcarpetaErrores);

        /// <summary>
        /// Crea la carpeta por defecto de las exportaciones (Errores\Exportados) si no existe y devuelve
        /// su ruta.
        /// </summary>
        public static string PrepararCarpetaExportados() =>
            GestorArchivos.Instancia.PrepararCarpeta(GestorArchivos.SubcarpetaErroresExportados);

        public static string RutaDelMes(DateTime fecha) => Path.Combine(Carpeta, $"errores_{fecha:yyyyMM}.xml");

        /// <summary>¿Es el nombre de un archivo mensual del registro (errores_AAAAMM.xml)?</summary>
        public static bool EsArchivoMensual(string ruta) => NombreMensual.IsMatch(Path.GetFileName(ruta ?? ""));

        /// <summary>Arma el registro a partir de la excepción (sin el ToString completo).</summary>
        public static BE.RegistroError Crear(Exception ex, string modulo, string usuario, DateTime fecha)
        {
            return new BE.RegistroError
            {
                Fecha   = fecha,
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "(sin sesión)" : usuario,
                Modulo  = modulo,
                Tipo    = ex?.GetType().FullName,
                Mensaje = ex?.Message,
                Causa   = ex?.InnerException != null
                    ? ex.InnerException.GetType().Name + ": " + ex.InnerException.Message : null,
                Equipo  = Environment.MachineName
            };
        }

        /// <summary>Agrega el error al archivo del mes. Devuelve false si no pudo (nunca lanza).</summary>
        public static bool Registrar(Exception ex, string modulo, string usuario)
        {
            if (ex == null) return false;
            try
            {
                var registro = Crear(ex, modulo, usuario, DateTime.Now);
                lock (Candado)
                {
                    string ruta = RutaDelMes(registro.Fecha);
                    var libro = File.Exists(ruta) ? LeerSinFallar(ruta) : new BE.LibroErrores();
                    libro.Errores.Add(registro);
                    if (libro.Errores.Count > MaximoPorArchivo)
                        libro.Errores = libro.Errores.Skip(libro.Errores.Count - MaximoPorArchivo).ToList();
                    // El registro interno es siempre XML. Se crea acá adentro (y no en un campo estático)
                    // para que ni siquiera una falla al preparar el XmlSerializer escape de este try.
                    new SerializadorXml<BE.LibroErrores>().GuardarArchivo(libro, ruta);
                }
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("[Servicios.RegistroErrores] " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Todos los errores de los archivos mensuales de la carpeta (solo errores_AAAAMM.xml), del más
        /// nuevo al más viejo.
        /// </summary>
        public static List<BE.RegistroError> ObtenerTodos()
        {
            var todos = new List<BE.RegistroError>();
            if (!Directory.Exists(Carpeta)) return todos;
            lock (Candado)
            {
                foreach (var ruta in Directory.GetFiles(Carpeta, "errores_*.xml").Where(EsArchivoMensual))
                    todos.AddRange(LeerSinFallar(ruta).Errores);
            }
            return todos.OrderByDescending(e => e.Fecha).ToList();
        }

        /// <summary>
        /// Lee un archivo de errores en XML o JSON (el formato sale de la extensión) y valida su versión
        /// de formato (un archivo de un formato que esta aplicación no conoce se rechaza con un mensaje
        /// claro en vez de interpretarse a medias). Un archivo sin contenido (por ejemplo, una raíz
        /// xsi:nil) se toma como un libro vacío, igual que antes.
        /// </summary>
        public static BE.LibroErrores LeerArchivo(string ruta)
        {
            var libro = FabricaSerializadores.Crear<BE.LibroErrores>(ruta).LeerArchivo(ruta) ?? new BE.LibroErrores();
            SerializadorAbstracto.ExigirVersionFormato(libro.Version, BE.LibroErrores.FormatoActual);
            if (libro.Errores == null) libro.Errores = new List<BE.RegistroError>();
            return libro;
        }

        /// <summary>
        /// Exporta los errores a un archivo XML o JSON (el formato sale de la extensión de la ruta) y
        /// devuelve el nombre del formato usado ("XML" o "JSON").
        /// </summary>
        public static string Exportar(List<BE.RegistroError> errores, string ruta)
        {
            var serializador = FabricaSerializadores.Crear<BE.LibroErrores>(ruta);
            serializador.GuardarArchivo(new BE.LibroErrores { Version = BE.LibroErrores.FormatoActual, Errores = errores }, ruta);
            return serializador.Formato;
        }

        /// <summary>
        /// Datos de demostración del registro de errores, igual que la demo de la base: la primera vez
        /// que la aplicación se abre con este usuario de Windows (todavía no existe la carpeta Errores),
        /// deja 6 errores de ejemplo en los archivos mensuales de esta PC y, en Errores\Exportados, un
        /// archivo de errores "traído de otra PC" en XML y en JSON para probar la importación. Así la
        /// pantalla Errores (XML/JSON) muestra datos desde la instalación. Las fechas son relativas a
        /// hoy. Nunca lanza ni pisa nada: si la carpeta ya existe, no hace nada.
        /// </summary>
        public static void SembrarEjemplos()
        {
            try
            {
                if (Directory.Exists(Carpeta)) return;
                PrepararCarpeta();
                DateTime hoy = DateTime.Now;
                string equipo = Environment.MachineName;

                var deEstaPc = new List<BE.RegistroError>
                {
                    Ejemplo(hoy.AddDays(-26).Date.AddHours(10).AddMinutes(12), "deposito", "ControlStockForm",
                        "System.Data.SqlClient.SqlException",
                        "Se agotó el tiempo de espera de la conexión con el servidor SQL al consultar el stock.",
                        "Win32Exception: Tiempo de espera de la operación agotado", equipo),
                    Ejemplo(hoy.AddDays(-18).Date.AddHours(18).AddMinutes(40), "admin", "BackupForm",
                        "System.IO.IOException",
                        "No se pudo escribir el archivo de backup: no hay espacio suficiente en el disco.", null, equipo),
                    Ejemplo(hoy.AddDays(-11).Date.AddHours(11).AddMinutes(5), "vendedor", "PedidosVenta",
                        "System.InvalidOperationException",
                        "La prenda seleccionada dejó de estar disponible mientras se armaba el pedido.", null, equipo),
                    Ejemplo(hoy.AddDays(-6).Date.AddHours(9).AddMinutes(31), "gcomercial", "CentroDecisionesForm",
                        "System.Data.SqlClient.SqlException",
                        "Error de red al ejecutar la consulta del análisis comercial.",
                        "Win32Exception: Se forzó la interrupción de una conexión existente por el host remoto", equipo),
                    Ejemplo(hoy.AddDays(-3).Date.AddHours(20).AddMinutes(2), "admin", "Aplicación",
                        "System.UnauthorizedAccessException",
                        "Acceso denegado a la ruta de la carpeta de backups.", null, equipo),
                    Ejemplo(hoy.AddDays(-1).Date.AddHours(16).AddMinutes(48), "caja", "ContratacionesPendientesForm",
                        "System.FormatException",
                        "El importe ingresado no tiene un formato numérico válido.", null, equipo),
                };
                lock (Candado)
                {
                    foreach (var mes in deEstaPc.GroupBy(e => new DateTime(e.Fecha.Year, e.Fecha.Month, 1)))
                        new SerializadorXml<BE.LibroErrores>().GuardarArchivo(
                            new BE.LibroErrores { Errores = mes.ToList() }, RutaDelMes(mes.Key));
                }

                var deOtraPc = new List<BE.RegistroError>
                {
                    Ejemplo(hoy.AddDays(-9).Date.AddHours(8).AddMinutes(55), "deposito", "InspeccionDevolucionForm",
                        "System.Data.SqlClient.SqlException",
                        "No se pudo registrar la inspección: la conexión con la base se cerró.",
                        "Win32Exception: El nombre de red especificado ya no está disponible", "PC-DEPOSITO"),
                    Ejemplo(hoy.AddDays(-4).Date.AddHours(15).AddMinutes(20), "logistico", "PedidosRealizados",
                        "System.TimeoutException",
                        "La operación de despacho excedió el tiempo de espera.", null, "PC-DEPOSITO"),
                    Ejemplo(hoy.AddDays(-2).Date.AddHours(12).AddMinutes(7), "deposito", "ControlStockForm",
                        "System.IO.IOException",
                        "No se pudo abrir el PDF del informe de faltantes: el archivo está en uso.", null, "PC-DEPOSITO"),
                };
                string exportados = PrepararCarpetaExportados();
                Exportar(deOtraPc, Path.Combine(exportados, "errores_PC-DEPOSITO_ejemplo.xml"));
                Exportar(deOtraPc, Path.Combine(exportados, "errores_PC-DEPOSITO_ejemplo.json"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Servicios.RegistroErrores] No se pudieron crear los ejemplos: " + ex.Message);
            }
        }

        private static BE.RegistroError Ejemplo(DateTime fecha, string usuario, string modulo, string tipo,
                                                string mensaje, string causa, string equipo) =>
            new BE.RegistroError
            {
                Fecha = fecha, Usuario = usuario, Modulo = modulo, Tipo = tipo,
                Mensaje = mensaje, Causa = causa, Equipo = equipo
            };

        // Un archivo dañado (o de un formato desconocido) no impide registrar los errores nuevos: se
        // aparta y se empieza otro. Por las dudas, un registro nulo se descarta (XmlSerializer no los
        // produce, pero la lista no debe llegar con nulos a la pantalla).
        private static BE.LibroErrores LeerSinFallar(string ruta)
        {
            try
            {
                var libro = LeerArchivo(ruta);
                libro.Errores.RemoveAll(e => e == null);
                return libro;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Servicios.RegistroErrores] Archivo dañado " + ruta + ": " + ex.Message);
                try { File.Move(ruta, ruta + ".danado_" + DateTime.Now.ToString("yyyyMMddHHmmss")); } catch { }
                return new BE.LibroErrores();
            }
        }
    }
}
