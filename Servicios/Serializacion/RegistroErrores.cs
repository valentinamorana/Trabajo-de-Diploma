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
