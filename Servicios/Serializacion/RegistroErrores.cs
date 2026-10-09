using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — cada error inesperado de la aplicación queda serializado en XML
    /// (un archivo por mes: Errores\errores_AAAAMM.xml en la carpeta de datos local de WardrobeFlow),
    /// además del renglón que ya va a la bitácora. El archivo se puede abrir, importar desde otra
    /// PC y exportar desde la pantalla de Bitácora (GUI.ErroresXmlForm, vía BLL.ErroresSerializados).
    /// Nunca lanza: si no puede escribir, el error original igual se muestra.
    /// </summary>
    public static class RegistroErrores
    {
        /// <summary>Errores que se conservan por archivo mensual (los más viejos se descartan).</summary>
        public const int MaximoPorArchivo = 500;

        private static readonly object Candado = new object();

        /// <summary>Carpeta de los archivos. Se puede cambiar (por ejemplo, en los tests).</summary>
        public static string Carpeta { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WardrobeFlow", "Errores");

        public static string RutaDelMes(DateTime fecha) => Path.Combine(Carpeta, $"errores_{fecha:yyyyMM}.xml");

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
                    SerializadorXml.GuardarArchivo(libro, ruta);
                }
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("[Servicios.RegistroErrores] " + e.Message);
                return false;
            }
        }

        /// <summary>Todos los errores de los archivos de la carpeta, del más nuevo al más viejo.</summary>
        public static List<BE.RegistroError> ObtenerTodos()
        {
            var todos = new List<BE.RegistroError>();
            if (!Directory.Exists(Carpeta)) return todos;
            lock (Candado)
            {
                foreach (var ruta in Directory.GetFiles(Carpeta, "errores_*.xml"))
                    todos.AddRange(LeerSinFallar(ruta).Errores);
            }
            return todos.OrderByDescending(e => e.Fecha).ToList();
        }

        // Un archivo dañado no impide registrar los errores nuevos: se aparta y se empieza otro.
        private static BE.LibroErrores LeerSinFallar(string ruta)
        {
            try { return SerializadorXml.LeerArchivo<BE.LibroErrores>(ruta) ?? new BE.LibroErrores(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Servicios.RegistroErrores] Archivo dañado " + ruta + ": " + ex.Message);
                try { File.Move(ruta, ruta + ".danado_" + DateTime.Now.ToString("yyyyMMddHHmmss")); } catch { }
                return new BE.LibroErrores();
            }
        }
    }
}
