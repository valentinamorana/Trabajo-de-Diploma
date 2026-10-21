using System;
using System.IO;
using System.Text;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — base de todos los serializadores (el AbstractSerializator del material de
    /// la cátedra). Cada formato es una subclase: <see cref="SerializadorXml{T}"/> y
    /// <see cref="SerializadorJson{T}"/>. Esta parte, sin tipo genérico, reúne lo que no depende del
    /// objeto serializado: la extensión y el nombre del formato y la validación de la versión de
    /// formato (que también se usa sobre un objeto ya leído, sin serializador a mano).
    /// </summary>
    public abstract class SerializadorAbstracto
    {
        /// <summary>
        /// UTF-8 sin BOM: lo usa el XML al escribir y las variantes con texto (string). El escritor
        /// JSON de .NET (JsonReaderWriterFactory) recibe Encoding.UTF8 y tampoco escribe BOM.
        /// </summary>
        protected static readonly Encoding Utf8SinBom = new UTF8Encoding(false);

        /// <summary>Extensión de los archivos de este formato, con el punto (".xml", ".json").</summary>
        public abstract string Extension { get; }

        /// <summary>Nombre del formato para los mensajes y la bitácora ("XML", "JSON").</summary>
        public abstract string Formato { get; }

        /// <summary>
        /// Valida el atributo de versión de formato de un archivo leído. Un archivo de un formato más
        /// nuevo (generado por una versión posterior de la aplicación) o más viejo no se interpreta a
        /// medias: se rechaza con un mensaje claro. 0 = el archivo no trae la versión.
        /// </summary>
        public static void ExigirVersionFormato(int leida, int admitida)
        {
            if (leida <= 0)
                throw new BE.AppException("err.serial.sin_version",
                    "El archivo no indica su versión de formato: no fue generado por WardrobeFlow.");
            if (leida > admitida)
                throw new BE.AppException("err.serial.version_nueva",
                    "El archivo usa un formato más nuevo (versión {0}) que el que admite esta aplicación (versión {1}). Actualice WardrobeFlow para abrirlo.",
                    leida, admitida);
            if (leida < admitida)
                throw new BE.AppException("err.serial.version_vieja",
                    "El archivo usa un formato anterior (versión {0}) que esta aplicación ya no admite (versión {1}).",
                    leida, admitida);
        }

        /// <summary>Archivo vacío (o sin contenido): mismo error para todos los formatos.</summary>
        protected static BE.AppException ErrorArchivoVacio() =>
            new BE.AppException("err.serial.vacio", "El archivo está vacío.");

        /// <summary>El contenido no respeta el formato: {0} = detalle técnico, {1} = formato.</summary>
        protected BE.AppException ErrorDeFormato(string detalle) =>
            new BE.AppException("err.serial.formato", "El archivo no tiene el formato {1} esperado: {0}", detalle, Formato);
    }

    /// <summary>
    /// A02 Serialización — serializador de objetos de tipo <typeparamref name="T"/>. Las subclases
    /// solo implementan cómo se escribe y se lee el objeto en un Stream (Serializar / Deserializar);
    /// el resto es común a todos los formatos (método plantilla):
    ///   • GuardarArchivo: crea la carpeta, escribe a un ".tmp" y después lo reemplaza, para que un
    ///     corte a mitad de camino no deje un archivo truncado.
    ///   • LeerArchivo: archivo inexistente o vacío con el mismo mensaje en cualquier formato.
    ///   • Serializar / Deserializar a texto (string), para las pruebas y para mostrar el contenido.
    /// </summary>
    public abstract class SerializadorAbstracto<T> : SerializadorAbstracto where T : class
    {
        /// <summary>Escribe el objeto en el stream (no lo cierra).</summary>
        public abstract void Serializar(T objeto, Stream destino);

        /// <summary>
        /// Lee el objeto del stream (no lo cierra). Si el contenido no respeta el formato lanza
        /// AppException "err.serial.formato".
        /// </summary>
        public abstract T Deserializar(Stream origen);

        public string Serializar(T objeto)
        {
            if (objeto == null) throw new ArgumentNullException(nameof(objeto));
            using (var memoria = new MemoryStream())
            {
                Serializar(objeto, memoria);
                return Utf8SinBom.GetString(memoria.ToArray());
            }
        }

        public T Deserializar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) throw ErrorArchivoVacio();
            using (var memoria = new MemoryStream(Utf8SinBom.GetBytes(texto)))
                return Deserializar(memoria);
        }

        public void GuardarArchivo(T objeto, string ruta)
        {
            if (objeto == null) throw new ArgumentNullException(nameof(objeto));
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentNullException(nameof(ruta));
            string carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta));
            if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

            string temporal = ruta + ".tmp";
            try
            {
                using (var archivo = new FileStream(temporal, FileMode.Create, FileAccess.Write, FileShare.None))
                    Serializar(objeto, archivo);
                if (File.Exists(ruta)) File.Replace(temporal, ruta, null);
                else File.Move(temporal, ruta);
            }
            catch
            {
                // Falle la escritura o el reemplazo, no queda un .tmp suelto (el archivo anterior, si
                // existía, sigue intacto).
                try { if (File.Exists(temporal)) File.Delete(temporal); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Lee el archivo tal como está en el disco: el lector del formato detecta la codificación por
        /// los bytes (BOM y, en XML, la declaración encoding="..."), así un XML en UTF-16 también se lee.
        /// </summary>
        public T LeerArchivo(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                throw new BE.AppException("err.serial.no_existe", "No se encontró el archivo '{0}'.", ruta ?? "");
            byte[] contenido = File.ReadAllBytes(ruta);
            if (SinContenido(contenido)) throw ErrorArchivoVacio();

            // Un BOM UTF-8 se saltea (el lector JSON no lo espera; para XML es indistinto).
            int inicio = contenido.Length >= 3 && contenido[0] == 0xEF && contenido[1] == 0xBB && contenido[2] == 0xBF ? 3 : 0;
            using (var memoria = new MemoryStream(contenido, inicio, contenido.Length - inicio, writable: false))
                return Deserializar(memoria);
        }

        // Archivo vacío o solo con espacios (en cualquier codificación con BOM, o UTF-8 sin BOM).
        private static bool SinContenido(byte[] contenido)
        {
            if (contenido.Length == 0) return true;
            using (var lector = new StreamReader(new MemoryStream(contenido), Utf8SinBom, detectEncodingFromByteOrderMarks: true))
                return string.IsNullOrWhiteSpace(lector.ReadToEnd());
        }
    }
}
