using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BE
{
    /// <summary>
    /// A02 Serialización — un error inesperado de la aplicación, tal como queda serializado en XML
    /// (Servicios.Serializacion.RegistroErrores) o se exporta en XML o JSON. Solo datos técnicos
    /// mínimos: nunca contraseñas ni el ToString() completo de la excepción (puede arrastrar valores
    /// de parámetros). [DataContract] / [DataMember]: el JSON lleva los nombres de las propiedades
    /// (sin él, por [Serializable], saldrían los campos internos "&lt;Fecha&gt;k__BackingField").
    /// </summary>
    [Serializable]
    [XmlType("Error")]
    [DataContract(Name = "Error", Namespace = "")]
    public class RegistroError
    {
        [XmlAttribute("fecha")]
        [DataMember(Order = 1)]
        public DateTime Fecha { get; set; }

        [XmlElement("Usuario")]
        [DataMember(Order = 2)]
        public string Usuario { get; set; }

        /// <summary>Pantalla o componente donde ocurrió (nombre del formulario o "Aplicación").</summary>
        [XmlElement("Modulo")]
        [DataMember(Order = 3)]
        public string Modulo { get; set; }

        /// <summary>Tipo de la excepción (nombre completo).</summary>
        [XmlElement("Tipo")]
        [DataMember(Order = 4)]
        public string Tipo { get; set; }

        [XmlElement("Mensaje")]
        [DataMember(Order = 5)]
        public string Mensaje { get; set; }

        /// <summary>Tipo y mensaje de la excepción interna, si la hay.</summary>
        [XmlElement("Causa")]
        [DataMember(Order = 6)]
        public string Causa { get; set; }

        /// <summary>Equipo donde ocurrió (útil cuando se juntan archivos de varias PC).</summary>
        [XmlElement("Equipo")]
        [DataMember(Order = 7)]
        public string Equipo { get; set; }
    }

    /// <summary>Raíz del archivo: la lista de errores registrados.</summary>
    [Serializable]
    [XmlRoot("ErroresWardrobeFlow")]
    [DataContract(Name = "ErroresWardrobeFlow", Namespace = "")]
    public class LibroErrores
    {
        /// <summary>Versión del formato que escribe y admite esta aplicación (se valida al leer).</summary>
        public const int FormatoActual = 1;

        // XML: por defecto 1, así los archivos escritos antes de validar la versión siguen siendo
        // legibles. JSON: DataContractJsonSerializer no ejecuta el constructor, así que un JSON sin
        // "Version" queda en 0 y se rechaza (no hay archivos JSON anteriores a la validación).
        [XmlAttribute("version")]
        [DataMember(Order = 1)]
        public int Version { get; set; } = FormatoActual;

        // IsRequired (solo JSON): el JSON no lleva el nombre de la raíz; exigir la lista evita tomar
        // por archivo de errores otro archivo de WardrobeFlow (por ejemplo, un historial de versiones).
        [XmlElement("Error")]
        [DataMember(Order = 2, IsRequired = true)]
        public List<RegistroError> Errores { get; set; } = new List<RegistroError>();
    }
}
