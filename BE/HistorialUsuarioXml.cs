using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BE
{
    /// <summary>
    /// A02 Serialización — raíz del archivo (XML o JSON) con el historial de versiones (Mementos) de
    /// UN usuario. Lo arma y lo valida BLL.VersionUsuario (Exportar / Importar) y lo escribe/lee el
    /// serializador del formato elegido (Servicios.Serializacion.SerializadorXml / SerializadorJson).
    /// Desde un archivo importado se puede restaurar al usuario a una de sus versiones con el mismo
    /// flujo de restauración del historial. La clase conserva "Xml" en el nombre porque nació con ese
    /// formato; los nombres de los elementos del XML no cambiaron, así los archivos ya exportados se
    /// siguen importando.
    ///
    /// Qué se serializa: cabecera (formato, equipo, fecha, quién exportó, Id y nombre del usuario)
    /// y, por cada versión, Id, IdUsuario, Fecha, Actor, Detalle, Username, Nombre, Apellido, fecha
    /// de nacimiento y Email. Qué NO: la clave y el estado de bloqueo/intentos (ver [XmlIgnore] e
    /// [IgnoreDataMember] en <see cref="VersionUsuario"/>).
    /// </summary>
    [Serializable]
    [XmlRoot("HistorialUsuarioWardrobeFlow")]
    [DataContract(Name = "HistorialUsuarioWardrobeFlow", Namespace = "")]
    public class HistorialUsuarioXml
    {
        /// <summary>Versión del formato que escribe y admite esta aplicación.</summary>
        public const int FormatoActual = 1;

        /// <summary>
        /// Versión del formato. Sin valor por defecto a propósito: si el atributo falta en el archivo
        /// queda en 0 y la importación lo rechaza (no es un historial exportado por WardrobeFlow).
        /// </summary>
        [XmlAttribute("version")]
        [DataMember(Order = 1)]
        public int Version { get; set; }

        /// <summary>Equipo donde se exportó (el archivo se puede llevar a otra PC).</summary>
        [XmlElement("Equipo")]
        [DataMember(Order = 2)]
        public string Equipo { get; set; }

        [XmlElement("FechaExportacion")]
        [DataMember(Order = 3)]
        public DateTime FechaExportacion { get; set; }

        /// <summary>Usuario (Administrador) que hizo la exportación.</summary>
        [XmlElement("ExportadoPor")]
        [DataMember(Order = 4)]
        public string ExportadoPor { get; set; }

        /// <summary>Usuario dueño del historial: Id y nombre de acceso al momento de exportar.</summary>
        [XmlElement("IdUsuario")]
        [DataMember(Order = 5)]
        public int IdUsuario { get; set; }

        [XmlElement("Username")]
        [DataMember(Order = 6)]
        public string Username { get; set; }

        /// <summary>
        /// IsRequired (solo JSON): el JSON no lleva el nombre de la raíz como el XML; exigir la lista
        /// evita tomar por historial otro archivo de WardrobeFlow (por ejemplo, uno de errores, que
        /// también trae "Version").
        /// </summary>
        [XmlArray("Versiones")]
        [XmlArrayItem("Version")]
        [DataMember(Order = 7, IsRequired = true)]
        public List<VersionUsuario> Versiones { get; set; } = new List<VersionUsuario>();

        /// <summary>
        /// Formato del archivo del que se leyó o al que se exportó ("XML" o "JSON"). Dato de la sesión,
        /// para los mensajes y la bitácora: no se serializa.
        /// </summary>
        [XmlIgnore, IgnoreDataMember]
        public string FormatoArchivo { get; set; }
    }
}
