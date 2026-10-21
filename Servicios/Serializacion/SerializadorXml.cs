using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Servicios.Serializacion
{
    /// <summary>
    /// A02 Serialización — formato XML (el XmlSerializator de la cátedra), con
    /// System.Xml.Serialization.XmlSerializer: serializa las propiedades públicas y respeta los
    /// atributos [XmlRoot], [XmlElement], [XmlAttribute] y [XmlIgnore] de las clases de BE.
    /// Escribe UTF-8 sin BOM e indentado; al leer no procesa DTD ni entidades externas (XXE), porque
    /// el archivo puede venir de otra PC. Es el formato del registro interno de errores
    /// (errores_AAAAMM.xml) y uno de los dos de exportación/importación.
    /// </summary>
    public sealed class SerializadorXml<T> : SerializadorAbstracto<T> where T : class
    {
        // XmlSerializer genera su código la primera vez que se usa un tipo; la instancia es
        // thread-safe para Serialize/Deserialize, así que se reutiliza una por tipo.
        private static readonly XmlSerializer Serializador = new XmlSerializer(typeof(T));

        public override string Extension => FabricaSerializadores.ExtensionXml;
        public override string Formato   => FabricaSerializadores.FormatoXml;

        public override void Serializar(T objeto, Stream destino)
        {
            if (objeto == null) throw new ArgumentNullException(nameof(objeto));
            if (destino == null) throw new ArgumentNullException(nameof(destino));
            var opciones = new XmlWriterSettings { Indent = true, Encoding = Utf8SinBom, CloseOutput = false };
            using (var escritor = XmlWriter.Create(destino, opciones))
            {
                Serializador.Serialize(escritor, objeto);
                escritor.Flush();
            }
        }

        public override T Deserializar(Stream origen)
        {
            if (origen == null) throw new ArgumentNullException(nameof(origen));
            // Sin DTD ni entidades externas (XXE): el archivo puede venir de otra PC.
            var opciones = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, CloseInput = false };
            try
            {
                using (var lector = XmlReader.Create(origen, opciones))
                    return (T)Serializador.Deserialize(lector);
            }
            catch (InvalidOperationException ex)
            {
                throw ErrorDeFormato(ex.InnerException?.Message ?? ex.Message);
            }
            catch (XmlException ex)
            {
                throw ErrorDeFormato(ex.Message);
            }
        }
    }
}
