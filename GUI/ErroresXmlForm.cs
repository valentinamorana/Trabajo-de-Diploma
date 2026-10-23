using GUI.Estilos;
using Servicios.Multiidioma;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// A02 Serialización — muestra los errores inesperados serializados en XML
    /// (Servicios.Serializacion.RegistroErrores), permite importar un archivo XML o JSON de errores
    /// (deserializar, por ejemplo traído de otra PC) y exportar la lista que se está viendo
    /// (serializar, en XML o JSON según la extensión elegida). Se abre desde la Bitácora; exige la
    /// patente de Auditoría (BLL.ErroresSerializados). El nombre de la clase conserva "Xml" (formato
    /// original) para no renombrar el formulario. Hecho con el Diseñador (ErroresXmlForm.Designer.cs).
    /// </summary>
    public partial class ErroresXmlForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.ErroresSerializados _bll = new BLL.ErroresSerializados();
        private List<BE.RegistroError> _errores = new List<BE.RegistroError>();
        private string _origen;   // null = errores de esta PC; si no, el archivo importado

        protected override Label MensajeLabel => lblMensaje;

        public ErroresXmlForm()
        {
            InitializeComponent();
            // Estilo de la aplicación (encabezado rosa, filas alternadas); la disposición está en el Diseñador.
            EstiloFormulario.Grilla(dgvErrores);
            EstiloFormulario.BotonSecundario(btnRecargar);
            EstiloFormulario.BotonSecundario(btnImportar);
            EstiloFormulario.BotonPrimario(btnExportar);
            EstiloFormulario.BotonSecundario(btnCerrar);
        }

        private void DgvErrores_SelectionChanged(object sender, EventArgs e) => MostrarDetalle();
        private void BtnRecargar_Click(object sender, EventArgs e) => CargarDeEstaPc();
        private void BtnImportar_Click(object sender, EventArgs e) => Importar();
        private void BtnExportar_Click(object sender, EventArgs e) => Exportar();
        private void BtnCerrar_Click(object sender, EventArgs e) => Close();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir();
            CargarDeEstaPc();
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir();
            Mostrar();
        }

        private void Traducir()
        {
            Text             = Tr("frm.errxml", "Errores (XML/JSON)");
            lblTitulo.Text   = Tr("frm.errxml.titulo", "Errores inesperados serializados (XML/JSON)");
            btnRecargar.Text = Tr("btn.errxml.estapc", "Ver los de esta PC");
            btnImportar.Text = Tr("btn.errxml.importar", "Importar XML/JSON…");
            btnExportar.Text = Tr("btn.errxml.exportar", "Exportar XML/JSON…");
            btnCerrar.Text   = Tr("btn.cerrar", "Cerrar");
        }

        private void CargarDeEstaPc()
        {
            try
            {
                _errores = _bll.ObtenerRegistrados();
                _origen = null;
                Mostrar();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // Carpeta inicial de los diálogos: %LOCALAPPDATA%\WardrobeFlow\Errores\Exportados (la crea si
        // falta), aparte del registro mensual errores_AAAAMM.xml, así una exportación nunca se mezcla
        // con los errores de esta PC. La importación igual puede abrir un archivo de cualquier carpeta.
        // Si no se pudiera crear, el diálogo abre en su carpeta por defecto.
        private string CarpetaInicial()
        {
            try { return Servicios.Serializacion.RegistroErrores.PrepararCarpetaExportados(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[ErroresXmlForm.CarpetaInicial] {ex.Message}");
                return null;
            }
        }

        private void Importar()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = Tr("dlg.filtro.serial.abrir", "Archivos XML o JSON (*.xml;*.json)|*.xml;*.json"),
                InitialDirectory = CarpetaInicial()
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    _errores = _bll.Importar(Text, dlg.FileName);
                    _origen = dlg.FileName;
                    Mostrar();
                    MostrarOk(Tr("msg.errxml.importado", "Se importaron {0} error(es).", new object[] { _errores.Count }));
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        private void Exportar()
        {
            using (var dlg = new SaveFileDialog
            {
                // XML o JSON: el formato lo decide la extensión elegida (BLL → FabricaSerializadores).
                // DefaultExt: si se escribe un nombre sin extensión, toma la del tipo elegido.
                Filter = Tr("dlg.filtro.serial.guardar", "Archivos XML (*.xml)|*.xml|Archivos JSON (*.json)|*.json"),
                DefaultExt = "xml",
                AddExtension = true,
                InitialDirectory = CarpetaInicial(),
                FileName = Servicios.Serializacion.GestorArchivos.Instancia.NombreArchivoSugerido(
                    "errores_wardrobeflow", null, DateTime.Now, Servicios.Serializacion.FabricaSerializadores.ExtensionXml)
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string formato = _bll.Exportar(Text, _errores, dlg.FileName);
                    MostrarOk(Tr("msg.errxml.exportado", "Se exportaron {0} error(es) a {1}.", new object[] { _errores.Count, formato }));
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        private void Mostrar()
        {
            lblOrigen.Text = _origen == null
                ? Tr("lbl.errxml.origen.pc", "Errores registrados en esta PC: {0}", new object[] { _errores.Count })
                : Tr("lbl.errxml.origen.archivo", "Archivo importado: {0} — {1} error(es)",
                     new object[] { System.IO.Path.GetFileName(_origen), _errores.Count });

            var tabla = new DataTable();
            tabla.Columns.Add(Tr("col.errxml.fecha", "Fecha"));
            tabla.Columns.Add(Tr("col.errxml.usuario", "Usuario"));
            tabla.Columns.Add(Tr("col.errxml.modulo", "Pantalla"));
            tabla.Columns.Add(Tr("col.errxml.tipo", "Tipo"));
            tabla.Columns.Add(Tr("col.errxml.mensaje", "Mensaje"));
            foreach (var e in _errores)
                tabla.Rows.Add(e.Fecha.ToString("g"), e.Usuario, e.Modulo, NombreCorto(e.Tipo), e.Mensaje);
            dgvErrores.DataSource = tabla;
            // Sin ordenar por columna: la fila se mapea por índice a _errores.
            foreach (DataGridViewColumn col in dgvErrores.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (_errores.Count == 0) txtDetalle.Text = Tr("lbl.errxml.vacio", "No hay errores registrados.");
            btnExportar.Enabled = _errores.Count > 0;
        }

        private void MostrarDetalle()
        {
            if (dgvErrores.SelectedRows.Count == 0) return;
            int i = dgvErrores.SelectedRows[0].Index;
            if (i < 0 || i >= _errores.Count) return;
            var e = _errores[i];
            txtDetalle.Text =
                $"{Tr("col.errxml.fecha", "Fecha")}: {e.Fecha:G}   {Tr("lbl.errxml.equipo", "Equipo")}: {e.Equipo}\r\n" +
                $"{Tr("col.errxml.tipo", "Tipo")}: {e.Tipo}\r\n" +
                $"{Tr("col.errxml.mensaje", "Mensaje")}: {e.Mensaje}" +
                (string.IsNullOrEmpty(e.Causa) ? "" : $"\r\n{Tr("lbl.errxml.causa", "Causa")}: {e.Causa}");
        }

        private static string NombreCorto(string tipo)
        {
            if (string.IsNullOrEmpty(tipo)) return tipo;
            int punto = tipo.LastIndexOf('.');
            return punto >= 0 ? tipo.Substring(punto + 1) : tipo;
        }
    }
}
