using System;
using System.IO;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — elige el serializador según la extensión del archivo (.xml o .json), así
    /// la BLL y la GUI no deciden el formato con un if por cada caso. Cualquier otra extensión se
    /// rechaza con un mensaje claro en vez de intentar leer el archivo.
    /// </summary>
    public static class FabricaSerializadores
    {
        public const string ExtensionXml  = ".xml";
        public const string ExtensionJson = ".json";
        public const string FormatoXml    = "XML";
        public const string FormatoJson   = "JSON";

        /// <summary>Serializador para la ruta (o la extensión, con el punto) indicada.</summary>
        public static SerializadorAbstracto<T> Crear<T>(string rutaOExtension) where T : class
        {
            if (EsXml(rutaOExtension))  return new SerializadorXml<T>();
            if (EsJson(rutaOExtension)) return new SerializadorJson<T>();
            throw ExtensionNoAdmitida(rutaOExtension);
        }

        /// <summary>Nombre del formato ("XML" o "JSON") de la ruta indicada, para mensajes y bitácora.</summary>
        public static string Formato(string ruta)
        {
            if (EsXml(ruta))  return FormatoXml;
            if (EsJson(ruta)) return FormatoJson;
            throw ExtensionNoAdmitida(ruta);
        }

        private static bool EsXml(string ruta)  => string.Equals(Extension(ruta), ExtensionXml,  StringComparison.OrdinalIgnoreCase);
        private static bool EsJson(string ruta) => string.Equals(Extension(ruta), ExtensionJson, StringComparison.OrdinalIgnoreCase);

        private static BE.AppException ExtensionNoAdmitida(string ruta) =>
            new BE.AppException("err.serial.extension",
                "El archivo '{0}' no tiene un formato admitido: use la extensión .xml o .json.",
                NombreArchivo(ruta));

        private static string Extension(string ruta)
        {
            try { return Path.GetExtension(ruta ?? "") ?? ""; }
            catch (ArgumentException) { return ""; }   // ruta con caracteres inválidos
        }

        private static string NombreArchivo(string ruta)
        {
            try { return Path.GetFileName(ruta ?? ""); }
            catch (ArgumentException) { return ruta; }
        }
    }
}
