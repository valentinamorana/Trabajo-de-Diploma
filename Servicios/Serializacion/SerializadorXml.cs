using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — serializa y deserializa cualquier objeto [Serializable] a XML con
    /// System.Xml.Serialization.XmlSerializer. Escribe primero a un archivo temporal y después lo
    /// reemplaza, para que un corte a mitad de camino no deje un XML truncado.
    /// </summary>
    public static class SerializadorXml
    {
        public static string Serializar<T>(T objeto)
        {
            if (objeto == null) throw new ArgumentNullException(nameof(objeto));
            var serializador = new XmlSerializer(typeof(T));
            var opciones = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
            using (var texto = new StringWriterUtf8())
            using (var escritor = XmlWriter.Create(texto, opciones))
            {
                serializador.Serialize(escritor, objeto);
                escritor.Flush();
                return texto.ToString();
            }
        }

        public static T Deserializar<T>(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new BE.AppException("err.serial.vacio", "El archivo XML está vacío.");
            var serializador = new XmlSerializer(typeof(T));
            // Sin DTD ni entidades externas (XXE): el archivo puede venir de otra PC.
            var opciones = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            try
            {
                using (var texto = new StringReader(xml))
                using (var lector = XmlReader.Create(texto, opciones))
                    return (T)serializador.Deserialize(lector);
            }
            catch (InvalidOperationException ex)
            {
                throw new BE.AppException("err.serial.formato",
                    "El archivo no tiene el formato XML esperado: {0}", ex.InnerException?.Message ?? ex.Message);
            }
            catch (XmlException ex)
            {
                throw new BE.AppException("err.serial.formato",
                    "El archivo no tiene el formato XML esperado: {0}", ex.Message);
            }
        }

        public static void GuardarArchivo<T>(T objeto, string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentNullException(nameof(ruta));
            string carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta));
            if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

            string temporal = ruta + ".tmp";
            File.WriteAllText(temporal, Serializar(objeto), new UTF8Encoding(false));
            if (File.Exists(ruta)) File.Replace(temporal, ruta, null);
            else File.Move(temporal, ruta);
        }

        public static T LeerArchivo<T>(string ruta)
        {
            if (!File.Exists(ruta))
                throw new BE.AppException("err.serial.no_existe", "No se encontró el archivo '{0}'.", ruta);
            return Deserializar<T>(File.ReadAllText(ruta, Encoding.UTF8));
        }

        // StringWriter informa UTF-16 en la declaración XML; el archivo se guarda en UTF-8.
        private sealed class StringWriterUtf8 : StringWriter
        {
            public override Encoding Encoding => new UTF8Encoding(false);
        }
    }
}
