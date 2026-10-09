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
    /// (Servicios.Serializacion.RegistroErrores), permite importar un archivo XML de errores
    /// (deserializar, por ejemplo traído de otra PC) y exportar la lista que se está viendo
    /// (serializar). Se abre desde la Bitácora; exige la patente de Auditoría (BLL.ErroresSerializados).
    /// </summary>
    public class ErroresXmlForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.ErroresSerializados _bll = new BLL.ErroresSerializados();
        private List<BE.RegistroError> _errores = new List<BE.RegistroError>();
        private string _origen;   // null = errores de esta PC; si no, el archivo importado

        private readonly Label        lblTitulo    = new Label();
        private readonly Label        lblOrigen    = new Label();
        private readonly Label        lblMensaje   = new Label();
        private readonly DataGridView dgvErrores   = new DataGridView();
        private readonly TextBox      txtDetalle   = new TextBox();
        private readonly Button       btnRecargar  = new Button();
        private readonly Button       btnImportar  = new Button();
        private readonly Button       btnExportar  = new Button();
        private readonly Button       btnCerrar    = new Button();

        protected override Label MensajeLabel => lblMensaje;

        public ErroresXmlForm()
        {
            Text = "Errores (XML)";
            ClientSize = new Size(900, 560);
            MinimumSize = new Size(760, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = Tema.FuenteNormal;

            lblTitulo.SetBounds(16, 12, 860, 28);
            lblTitulo.Font = Tema.FuenteTitulo;
            lblTitulo.ForeColor = Tema.RosaOscuro;

            lblOrigen.SetBounds(16, 42, 860, 20);
            lblOrigen.ForeColor = Tema.TextoMuted;
            lblOrigen.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvErrores.SetBounds(16, 68, 868, 300);
            dgvErrores.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            dgvErrores.ReadOnly = true;
            dgvErrores.AllowUserToAddRows = false;
            dgvErrores.AllowUserToDeleteRows = false;
            dgvErrores.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvErrores.MultiSelect = false;
            dgvErrores.RowHeadersVisible = false;
            dgvErrores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvErrores.SelectionChanged += (s, e) => MostrarDetalle();
            EstiloFormulario.Grilla(dgvErrores);

            txtDetalle.SetBounds(16, 376, 868, 110);
            txtDetalle.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            txtDetalle.Multiline = true;
            txtDetalle.ReadOnly = true;
            txtDetalle.ScrollBars = ScrollBars.Vertical;
            txtDetalle.BackColor = Tema.Papel;

            lblMensaje.SetBounds(16, 492, 868, 20);
            lblMensaje.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            int y = 516;
            ConfigurarBoton(btnRecargar, 16, y, 170, false, (s, e) => CargarDeEstaPc());
            ConfigurarBoton(btnImportar, 194, y, 170, false, (s, e) => Importar());
            ConfigurarBoton(btnExportar, 372, y, 170, true, (s, e) => Exportar());
            ConfigurarBoton(btnCerrar, 764, y, 120, false, (s, e) => Close());
            btnCerrar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            CancelButton = btnCerrar;

            Controls.AddRange(new Control[] { lblTitulo, lblOrigen, dgvErrores, txtDetalle, lblMensaje,
                                              btnRecargar, btnImportar, btnExportar, btnCerrar });
        }

        private void ConfigurarBoton(Button btn, int x, int y, int ancho, bool primario, EventHandler click)
        {
            btn.SetBounds(x, y, ancho, 32);
            btn.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            if (primario) EstiloFormulario.BotonPrimario(btn); else EstiloFormulario.BotonSecundario(btn);
            btn.Click += click;
        }

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
            Text             = Tr("frm.errxml", "Errores (XML)");
            lblTitulo.Text   = Tr("frm.errxml.titulo", "Errores inesperados serializados en XML");
            btnRecargar.Text = Tr("btn.errxml.estapc", "Ver los de esta PC");
            btnImportar.Text = Tr("btn.errxml.importar", "Importar XML…");
            btnExportar.Text = Tr("btn.errxml.exportar", "Exportar XML…");
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

        private void Importar()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = Tr("dlg.filtro.xml", "Archivos XML (*.xml)|*.xml"),
                InitialDirectory = Servicios.Serializacion.RegistroErrores.Carpeta
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
                Filter = Tr("dlg.filtro.xml", "Archivos XML (*.xml)|*.xml"),
                FileName = $"errores_wardrobeflow_{DateTime.Now:yyyyMMdd_HHmm}.xml"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    _bll.Exportar(Text, _errores, dlg.FileName);
                    MostrarOk(Tr("msg.errxml.exportado", "Se exportaron {0} error(es) a XML.", new object[] { _errores.Count }));
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
