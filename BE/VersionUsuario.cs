using System;

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
    /// </summary>
    public class VersionUsuario : Memento.IMemento
    {
        public int       Id               { get; set; }
        public int       IdUsuario        { get; set; }
        public DateTime  Fecha            { get; set; }
        public string    Actor            { get; set; }
        public string    Detalle          { get; set; }
        public string    UsernameSnapshot { get; set; }

        // Datos administrativos NO sensibles versionados (Historial de Cambios).
        public string    NombreSnapshot   { get; set; }
        public string    ApellidoSnapshot { get; set; }
        public DateTime? FechaNacSnapshot { get; set; }
        public string    EmailSnapshot    { get; set; }

        // Estado de seguridad: trazabilidad interna; NO se muestra ni se restaura.
        public string    ClaveSnapshot    { get; set; }
        public bool      EstadoSnapshot   { get; set; } // true = activo, false = bloqueado
        public int       IntentosSnapshot { get; set; }
    }
}
