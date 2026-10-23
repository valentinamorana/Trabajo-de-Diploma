using System;
using System.IO;
using System.Reflection;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// «Acerca de WardrobeFlow» (Sesión → Acerca de WardrobeFlow). Lo ve cualquier usuario con sesión,
    /// sin patente (BLL.MapaNavegacion.ItemsSiempreVisibles), como Ayuda (F1) y la guía de inicio.
    ///
    /// Muestra el nombre del producto y la versión LEÍDOS DEL ENSAMBLADO en tiempo de ejecución
    /// (AssemblyProduct / AssemblyVersion de GUI/Properties/AssemblyInfo.cs, la misma versión que
    /// MyAppVersion del instalador), la fecha del ejecutable, el servidor y la base de datos de la
    /// cadena de conexión en uso (BLL.Configuracion: la GUI no lee la configuración de la base por su
    /// cuenta) y los datos del Trabajo de Diploma. Solo presenta: no escribe nada.
    ///
    /// Textos: claves ux.acercade.* (traducciones.tsv, 4 idiomas) y ayuda F1 ayuda.AcercaDeForm.
    /// </summary>
    public partial class AcercaDeForm : FormBase, IIdiomaObserver
    {
        private const string Autora = "Valentina Morana";

        public AcercaDeForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir();
            ActiveControl = btnCerrar;
        }

        public void UpdateLanguage(Idioma idioma) => Traducir();

        private void Traducir()
        {
            Text                     = Tr("ux.acercade.titulo", "Acerca de WardrobeFlow");
            lblProducto.Text         = NombreDelProducto();
            lblDescripcion.Text      = Tr("ux.acercade.descripcion", "Gestión de suscripciones de alquiler de indumentaria.");

            lblVersion.Text          = Tr("ux.acercade.version", "Versión");
            lblCompilacion.Text      = Tr("ux.acercade.compilacion", "Compilación");
            lblServidor.Text         = Tr("ux.acercade.servidor", "Servidor");
            lblBaseDatos.Text        = Tr("ux.acercade.basedatos", "Base de datos");
            lblAutora.Text           = Tr("ux.acercade.autora", "Autora");

            string noDisponible      = Tr("ux.acercade.nodisponible", "No disponible");
            DateTime? compilacion    = FechaDelEjecutable();
            lblVersionValor.Text     = VersionDelProducto();
            lblCompilacionValor.Text = compilacion.HasValue ? compilacion.Value.ToShortDateString() : noDisponible;
            lblServidorValor.Text    = ValorODefecto(BLL.Configuracion.ServidorConfigurado(), noDisponible);
            lblBaseDatosValor.Text   = ValorODefecto(BLL.Configuracion.BaseDeDatosConfigurada(), noDisponible);
            lblAutoraValor.Text      = Autora;

            lblTrabajo.Text          = Tr("ux.acercade.trabajo", "Trabajo de Diploma – Ingeniería en Sistemas – UAI – 2026");
            btnCerrar.Text           = Tr("btn.cerrar", "Cerrar");
        }

        // "?" es lo que devuelve BLL.Configuracion cuando no puede leer la cadena de conexión.
        private static string ValorODefecto(string valor, string porDefecto)
            => string.IsNullOrWhiteSpace(valor) || valor == "?" ? porDefecto : valor.Trim();

        /// <summary>AssemblyProduct de GUI.exe ("WardrobeFlow"); el nombre fijo si el atributo falta.</summary>
        private static string NombreDelProducto()
        {
            try
            {
                var producto = (AssemblyProductAttribute)Attribute.GetCustomAttribute(
                    typeof(AcercaDeForm).Assembly, typeof(AssemblyProductAttribute));
                if (producto != null && !string.IsNullOrWhiteSpace(producto.Product)) return producto.Product.Trim();
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[AcercaDeForm] Producto: " + ex.Message); }
            return "WardrobeFlow";
        }

        /// <summary>
        /// Versión del ensamblado en ejecución (AssemblyVersion) con tres números, p. ej. "1.6.0". No se
        /// escribe a mano en la pantalla: sale de GUI/Properties/AssemblyInfo.cs.
        /// </summary>
        private static string VersionDelProducto()
        {
            try
            {
                Version version = typeof(AcercaDeForm).Assembly.GetName().Version;
                if (version != null) return version.ToString(3);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[AcercaDeForm] Versión: " + ex.Message); }
            return "?";
        }

        /// <summary>
        /// Fecha de compilación aproximada: la última modificación de GUI.exe (la compilación lo escribe y
        /// el instalador conserva la fecha del archivo). El encabezado PE no sirve: los proyectos compilan
        /// con Deterministic, que no guarda la hora real. Null si no se puede leer.
        /// </summary>
        private static DateTime? FechaDelEjecutable()
        {
            try
            {
                string ruta = typeof(AcercaDeForm).Assembly.Location;
                if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta)) return File.GetLastWriteTime(ruta);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[AcercaDeForm] Fecha del ejecutable: " + ex.Message); }
            return null;
        }
    }
}
