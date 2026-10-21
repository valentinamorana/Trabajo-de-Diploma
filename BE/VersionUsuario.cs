using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BE
{
    /// <summary>
    /// Patrón MEMENTO — Memento CONCRETO del Usuario.
    ///
    /// Guarda una "foto" del estado del Usuario (snapshot) en un instante dado y
    /// persiste en la tabla HistorialUsuario. Expone metadatos (Fecha/Actor/Detalle)
    /// vía <see cref="Memento.IMemento"/> para que el Caretaker liste el historial.
    /// El Originator (BE.Usuario) es el único que usa los *Snapshot para crear y
    /// restaurar el Memento; GUI.VersionHistorialForm también los lee, pero solo
    /// para MOSTRAR el contenido de cada versión al usuario (grilla e "confirmar
    /// restauración"), nunca para tomar decisiones de negocio con ellos.
    ///
    /// A02 Serialización: el estado del Memento también se serializa a XML o JSON (dentro de
    /// <see cref="HistorialUsuarioXml"/>) para exportar/importar el historial. Solo viajan
    /// los datos administrativos NO sensibles; el estado de seguridad lleva [XmlIgnore] (XML) e
    /// [IgnoreDataMember] (JSON). [DataContract] hace que el JSON lleve solo los [DataMember], con
    /// el nombre de la propiedad: sin él, por [Serializable], DataContractJsonSerializer
    /// serializaría los campos internos ("&lt;Id&gt;k__BackingField"), clave incluida.
    /// </summary>
    [Serializable]
    [XmlType("VersionUsuario")]
    [DataContract(Name = "VersionUsuario", Namespace = "")]
    public class VersionUsuario : Memento.IMemento
    {
        [XmlAttribute("id")]
        [DataMember(Order = 1)]
        public int       Id               { get; set; }
        [DataMember(Order = 2)]
        public int       IdUsuario        { get; set; }
        [DataMember(Order = 3)]
        public DateTime  Fecha            { get; set; }
        [DataMember(Order = 4)]
        public string    Actor            { get; set; }
        [DataMember(Order = 5)]
        public string    Detalle          { get; set; }
        [DataMember(Order = 6)]
        public string    UsernameSnapshot { get; set; }

        // Datos administrativos NO sensibles versionados (Historial de Cambios).
        [DataMember(Order = 7)]
        public string    NombreSnapshot   { get; set; }
        [DataMember(Order = 8)]
        public string    ApellidoSnapshot { get; set; }
        [DataMember(Order = 9)]
        public DateTime? FechaNacSnapshot { get; set; }
        [DataMember(Order = 10)]
        public string    EmailSnapshot    { get; set; }

        // Estado de seguridad: trazabilidad interna; NO se muestra ni se restaura.
        // [XmlIgnore] / [IgnoreDataMember]: NUNCA sale de la base en un archivo exportado, ni en XML
        // ni en JSON (ni el hash de la clave, aunque hoy se guarde vacío, ni el estado de bloqueo /
        // intentos fallidos). Al importar quedan en sus valores por defecto, lo que no afecta la
        // restauración (BE.Usuario.RestaurarDesde no los usa).
        [XmlIgnore, IgnoreDataMember]
        public string    ClaveSnapshot    { get; set; }
        [XmlIgnore, IgnoreDataMember]
        public bool      EstadoSnapshot   { get; set; } // true = activo, false = bloqueado
        [XmlIgnore, IgnoreDataMember]
        public int       IntentosSnapshot { get; set; }
    }
}
