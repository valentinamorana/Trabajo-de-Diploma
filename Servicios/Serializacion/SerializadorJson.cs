using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — formato JSON (el JsonSerializator de la cátedra), con
    /// System.Runtime.Serialization.Json.DataContractJsonSerializer, que viene con .NET Framework
    /// (sin paquetes NuGet). Serializa los miembros marcados con [DataMember] de las clases de BE
    /// marcadas con [DataContract]; lo que lleva [IgnoreDataMember] (o no lleva [DataMember]) nunca
    /// sale en el archivo. Escribe UTF-8 indentado y las fechas en ISO 8601 legible.
    /// </summary>
    public sealed class SerializadorJson<T> : SerializadorAbstracto<T> where T : class
    {
        /// <summary>
        /// ISO 8601 legible ("2026-10-01T08:30:15"), con la fracción de segundo y la zona horaria solo
        /// si las hay ("2026-10-20T19:06:49.4773238-03:00"): el mismo detalle que escribe XmlSerializer,
        /// así una fecha (también DateTime?) hace el viaje de ida y vuelta exacta.
        /// </summary>
        public const string FormatoFecha = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK";

        // Cuotas del lector: los archivos de WardrobeFlow tienen pocos niveles y textos cortos; un
        // archivo armado para agotar memoria se corta acá (además del tamaño máximo de la BLL).
        private const int ProfundidadMaxima = 32;
        private const int LargoMaximoTexto  = 1024 * 1024;

        public override string Extension => FabricaSerializadores.ExtensionJson;
        public override string Formato   => FabricaSerializadores.FormatoJson;

        // DataContractJsonSerializer no garantiza ser thread-safe: una instancia por operación.
        private static DataContractJsonSerializer Crear()
        {
            var fechas = new DateTimeFormat(FormatoFecha, CultureInfo.InvariantCulture)
            {
                DateTimeStyles = DateTimeStyles.RoundtripKind
            };
            return new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings { DateTimeFormat = fechas });
        }

        public override void Serializar(T objeto, Stream destino)
        {
            if (objeto == null) throw new ArgumentNullException(nameof(objeto));
            if (destino == null) throw new ArgumentNullException(nameof(destino));
            using (var escritor = JsonReaderWriterFactory.CreateJsonWriter(destino, Encoding.UTF8, ownsStream: false, indent: true))
            {
                Crear().WriteObject(escritor, objeto);
                escritor.Flush();
            }
        }

        public override T Deserializar(Stream origen)
        {
            if (origen == null) throw new ArgumentNullException(nameof(origen));
            var cuotas = new XmlDictionaryReaderQuotas
            {
                MaxDepth               = ProfundidadMaxima,
                MaxStringContentLength = LargoMaximoTexto
            };
            T objeto;
            try
            {
                using (var lector = JsonReaderWriterFactory.CreateJsonReader(origen, cuotas))
                    objeto = (T)Crear().ReadObject(lector);
            }
            catch (SerializationException ex) { throw ErrorDeFormato(ex.Message); }
            catch (XmlException ex)           { throw ErrorDeFormato(ex.Message); }
            // El literal "null" es un JSON válido pero sin datos.
            if (objeto == null) throw ErrorArchivoVacio();
            return objeto;
        }
    }
}
