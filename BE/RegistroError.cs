using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BE
{
    /// <summary>
    /// A02 Serialización — un error inesperado de la aplicación, tal como queda serializado en XML
    /// (Servicios.Serializacion.RegistroErrores). Solo datos técnicos mínimos: nunca contraseñas ni
    /// el ToString() completo de la excepción (puede arrastrar valores de parámetros).
    /// </summary>
    [Serializable]
    [XmlType("Error")]
    public class RegistroError
    {
        [XmlAttribute("fecha")]
        public DateTime Fecha { get; set; }

        [XmlElement("Usuario")]
        public string Usuario { get; set; }

        /// <summary>Pantalla o componente donde ocurrió (nombre del formulario o "Aplicación").</summary>
        [XmlElement("Modulo")]
        public string Modulo { get; set; }

        /// <summary>Tipo de la excepción (nombre completo).</summary>
        [XmlElement("Tipo")]
        public string Tipo { get; set; }

        [XmlElement("Mensaje")]
        public string Mensaje { get; set; }

        /// <summary>Tipo y mensaje de la excepción interna, si la hay.</summary>
        [XmlElement("Causa")]
        public string Causa { get; set; }

        /// <summary>Equipo donde ocurrió (útil cuando se juntan archivos de varias PC).</summary>
        [XmlElement("Equipo")]
        public string Equipo { get; set; }
    }

    /// <summary>Raíz del archivo XML: la lista de errores registrados.</summary>
    [Serializable]
    [XmlRoot("ErroresWardrobeFlow")]
    public class LibroErrores
    {
        [XmlAttribute("version")]
        public int Version { get; set; } = 1;

        [XmlElement("Error")]
        public List<RegistroError> Errores { get; set; } = new List<RegistroError>();
    }
}
