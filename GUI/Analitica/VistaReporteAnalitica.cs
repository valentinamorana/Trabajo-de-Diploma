using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI.Analitica
{
    /// <summary>
    /// Un reporte de "Analítica de Negocio" tal como lo muestra AnaliticaNegocioForm. Antes cada
    /// reporte era un formulario propio (AnalisisAbandonoForm, ReporteVentasVendedorForm, ...) con
    /// la misma plantilla: BLL + Generar + Exportar. Ahora el formulario es uno solo y lo propio de
    /// cada reporte vive acá: su título, sus columnas, sus filtros (si tiene), cómo arma la grilla
    /// a partir del resultado de su BLL y qué generador de exportación usa (Factory Method de
    /// GUI.Exportacion). Las reglas siguen en la BLL de cada reporte; esto es solo presentación.
    /// </summary>
    internal abstract class VistaReporteAnalitica
    {
        private Control _filtros;
        private bool _filtrosCreados;

        /// <summary>Reporte de la BLL que representa (y cuya patente lo habilita).</summary>
        public abstract BE.ReporteAnalitica Tipo { get; }

        /// <summary>Título traducido: se muestra en el selector y encabeza la exportación.</summary>
        public abstract string Titulo { get; }

        /// <summary>Nombre base del archivo exportado (sin extensión).</summary>
        public abstract string NombreArchivo { get; }

        /// <summary>Encabezados de columna traducidos, en el orden de la grilla.</summary>
        public abstract string[] Encabezados { get; }

        /// <summary>Creador concreto del Factory Method de exportación de este reporte.</summary>
        public abstract Exportacion.GeneradorReporte CrearGenerador();

        /// <summary>
        /// Ejecuta el reporte con los filtros actuales y arma la tabla para la grilla. Devuelve
        /// null si todavía no hay nada que generar (p. ej. no hay cliente elegido).
        /// </summary>
        public abstract ResultadoVistaReporte Generar();

        /// <summary>Controles de filtro del reporte (se crean una sola vez), o null si no tiene.</summary>
        public Control Filtros
        {
            get
            {
                if (!_filtrosCreados)
                {
                    _filtros = CrearFiltros();
                    _filtrosCreados = true;
                    Traducir();
                }
                return _filtros;
            }
        }

        protected virtual Control CrearFiltros() => null;

        /// <summary>Ya se crearon sus filtros (Traducir los puede tocar).</summary>
        public bool FiltrosCreados => _filtrosCreados;

        /// <summary>Los datos de sus filtros se cargaron bien (si Cargar falló, queda en false).</summary>
        public bool Cargada { get; private set; }

        /// <summary>Carga los filtros si todavía no se cargaron. Devuelve true si los cargó AHORA.
        /// Si Cargar lanza, la vista sigue sin cargar y se reintenta la próxima vez.</summary>
        public bool AsegurarCargada()
        {
            if (Cargada) return false;
            _ = Filtros;   // Cargar llena los controles de filtro: tienen que existir antes
            Cargar();
            Cargada = true;
            return true;
        }

        /// <summary>Re-traduce los textos de los filtros (cambio de idioma en vivo).</summary>
        public virtual void Traducir() { }

        /// <summary>Carga los datos que necesitan los filtros (p. ej. la lista de clientes).</summary>
        public virtual void Cargar() { }

        public override string ToString() => Titulo;

        protected static string Tr(string clave, string fallback, object[] args = null)
            => Traductor.Resolver(clave, fallback, args, GestorIdioma.IdiomaActual);

        /// <summary>Tabla vacía con una columna por encabezado.</summary>
        protected DataTable NuevaTabla()
        {
            var tabla = new DataTable();
            foreach (var h in Encabezados) tabla.Columns.Add(h);
            return tabla;
        }

        /// <summary>
        /// Fila de filtro con el mismo diseño que tenían los formularios individuales: la
        /// etiqueta a la izquierda y el control alineado en x = 135 (debajo del selector).
        /// </summary>
        protected static Panel FilaFiltro(Label etiqueta, Control control)
        {
            var panel = new Panel { Height = 30, Width = 840, Margin = Padding.Empty };
            etiqueta.AutoSize = true;
            etiqueta.Location = new Point(0, 3);
            control.Location = new Point(135, 0);
            panel.Controls.Add(etiqueta);
            panel.Controls.Add(control);
            return panel;
        }
    }

    /// <summary>Resultado de generar un reporte: la tabla para la grilla y el mensaje de resumen.</summary>
    internal sealed class ResultadoVistaReporte
    {
        public DataTable Tabla { get; set; }
        public int Cantidad { get; set; }
        public string Mensaje { get; set; }
        public Color ColorMensaje { get; set; }
    }
}
