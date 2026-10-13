using System;
using System.Windows.Forms;

namespace GUI.Exportacion
{
    /// <summary>
    /// Patrón FACTORY METHOD — rol "Creator" (Creador abstracto).
    ///
    /// Declara el método fábrica <see cref="CrearExportador"/>, que recibe el formato
    /// pedido ("pdf" / "txt") y devuelve el <see cref="Exportador"/> concreto. Las
    /// SUBCLASES (un creador por reporte) deciden con qué origen se etiqueta el
    /// producto, igual que Logistica/Pizzeria en el ejercicio de la cátedra.
    ///
    /// El cliente (los formularios) usa esta abstracción y no instancia productos
    /// concretos: llama a <see cref="Exportar"/>, la operación del creador que usa el método
    /// fábrica y trabaja con el producto polimórficamente.
    /// </summary>
    public abstract class GeneradorReporte
    {
        /// <summary>
        /// Método fábrica. Devuelve el exportador concreto para el formato dado,
        /// o null si el formato no se reconoce.
        /// </summary>
        public abstract Exportador CrearExportador(string formato);

        /// <summary>
        /// Operación del creador (la "AnOperation" del patrón): obtiene el producto con el método
        /// fábrica y exporta. Devuelve la ruta escrita, o null si no se generó archivo. Un formato
        /// que el creador no conoce es un error de programación: lanza en vez de no hacer nada.
        /// </summary>
        public string Exportar(string formato, ReporteExportable reporte, IWin32Window propietario)
        {
            Exportador exportador = CrearExportador(formato)
                ?? throw new ArgumentException($"Formato de exportación no soportado: '{formato}'.", nameof(formato));
            return exportador.Exportar(reporte, propietario);
        }
    }
}
