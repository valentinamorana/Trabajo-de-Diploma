using System;
using System.IO;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — punto único (Singleton, el FileStreamManager de la cátedra) que arma las
    /// rutas de los archivos serializados: %LOCALAPPDATA%\WardrobeFlow\&lt;subcarpeta&gt;, con
    /// Environment.SpecialFolder (nunca una ruta fija), así funciona en cualquier PC y con un usuario
    /// de Windows sin permisos de administrador. Crea la carpeta cuando hace falta y sugiere nombres
    /// de archivo con la extensión del formato elegido. No abre el archivo: la escritura la hace el
    /// serializador (SerializadorAbstracto.GuardarArchivo), que escribe a un temporal y lo reemplaza.
    /// </summary>
    public sealed class GestorArchivos
    {
        public const string NombreAplicacion    = "WardrobeFlow";
        public const string SubcarpetaVersiones = "Versiones";
        public const string SubcarpetaErrores   = "Errores";

        /// <summary>
        /// Errores\Exportados: carpeta por defecto de las exportaciones de errores, aparte del registro
        /// mensual (Errores\errores_AAAAMM.xml) para que una exportación nunca se lea como registro.
        /// </summary>
        public static readonly string SubcarpetaErroresExportados = Path.Combine(SubcarpetaErrores, "Exportados");

        private static readonly GestorArchivos _instancia = new GestorArchivos();

        /// <summary>La única instancia.</summary>
        public static GestorArchivos Instancia => _instancia;

        private GestorArchivos() { }

        /// <summary>Raíz por defecto: %LOCALAPPDATA%\WardrobeFlow del usuario de Windows actual.</summary>
        public static string CarpetaRaizPorDefecto => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), NombreAplicacion);

        private string _carpetaRaiz;

        /// <summary>
        /// Carpeta raíz de los archivos. Solo las pruebas la cambian (a una carpeta temporal);
        /// null o vacío vuelve a la raíz por defecto.
        /// </summary>
        public string CarpetaRaiz
        {
            get => _carpetaRaiz ?? CarpetaRaizPorDefecto;
            internal set => _carpetaRaiz = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>Ruta de la subcarpeta (sin crearla).</summary>
        public string Carpeta(string subcarpeta)
        {
            if (string.IsNullOrWhiteSpace(subcarpeta)) throw new ArgumentNullException(nameof(subcarpeta));
            return Path.Combine(CarpetaRaiz, subcarpeta);
        }

        /// <summary>Crea la subcarpeta si no existe y devuelve su ruta.</summary>
        public string PrepararCarpeta(string subcarpeta)
        {
            string carpeta = Carpeta(subcarpeta);
            Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        /// <summary>
        /// Nombre sugerido: &lt;prefijo&gt;[_&lt;detalle&gt;]_&lt;aaaaMMdd_HHmmss&gt;&lt;extensión&gt;,
        /// sin caracteres inválidos para un nombre de archivo (se reemplazan por '_').
        /// </summary>
        public string NombreArchivoSugerido(string prefijo, string detalle, DateTime fecha, string extension)
        {
            string nombre = string.IsNullOrWhiteSpace(detalle)
                ? $"{prefijo}_{fecha:yyyyMMdd_HHmmss}"
                : $"{prefijo}_{detalle.Trim()}_{fecha:yyyyMMdd_HHmmss}";
            foreach (char c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '_');
            string ext = string.IsNullOrWhiteSpace(extension) ? "" : extension.Trim();
            if (ext.Length > 0 && ext[0] != '.') ext = "." + ext;
            return nombre + ext;
        }
    }
}
