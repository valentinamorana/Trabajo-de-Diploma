using Servicios.Multiidioma;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GUI
{
    public partial class VersionHistorialForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.VersionUsuario _bll         = new BLL.VersionUsuario();
        private readonly BLL.Usuario        _bllUsuario  = new BLL.Usuario();

        private List<BE.VersionUsuario> _versiones = new List<BE.VersionUsuario>();
        private int _preseleccionarId = 0;   // si >0, se preselecciona ese usuario al abrir

        // A02 Serialización: si no es null, la grilla muestra las versiones de un archivo XML o JSON
        // importado (no las de la base) y "Restaurar" usa el flujo de versión importada.
        private BE.HistorialUsuarioXml _importado;
        private string _archivoImportado;

        public VersionHistorialForm()
        {
            InitializeComponent();
        }

        // Abre el historial ya filtrado a un usuario concreto (desde Administración de Usuarios).
        public VersionHistorialForm(int idUsuario) : this()
        {
            _preseleccionarId = idUsuario;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);   // FormBase: ícono + tema/fuente del usuario + seguridad de controles
            Traducir(GestorIdioma.IdiomaActual);
            CargarUsuarios();

            // Si se abrió para un usuario concreto, preseleccionarlo y cargar su historial.
            if (_preseleccionarId > 0)
            {
                cboUsuario.SelectedValue = _preseleccionarId;
                if (cboUsuario.SelectedValue != null) btnCargar_Click(null, EventArgs.Empty);
            }
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private void Traducir(Idioma idioma)
        {
            this.Text           = Tr("frm.historialusr",    "Historial de Cambios de Usuarios");
            lblTitulo.Text      = Tr("frm.historialusr",    "Historial de Cambios de Usuarios");
            lblUsuario.Text     = Tr("lbl.ver.usuario",     "Usuario:");
            btnCargar.Text      = Tr("btn.ver.cargar",      "Cargar");
            btnRestaurar.Text   = Tr("btn.ver.restaurar",   "Restaurar Versión Seleccionada");
            btnCerrar.Text      = Tr("btn.ver.cerrar",      "Cerrar");
            btnExportar.Text    = Tr("btn.ver.exportar",    "Exportar versiones…");
            btnImportar.Text    = Tr("btn.ver.importar",    "Importar versiones…");
            MostrarOrigen();

            if (dgv.Columns.Count > 0)
            {
                dgv.Columns["colRegistro"].HeaderText = Tr("col.ver.registro", "Usuario (ID)");
                dgv.Columns["colFecha"].HeaderText    = Tr("col.ver.fecha",    "Fecha");
                dgv.Columns["colActor"].HeaderText    = Tr("col.ver.actor",    "Modificado por");
                dgv.Columns["colDetalle"].HeaderText  = Tr("col.ver.detalle",  "Cambios realizados");
            }
        }

        private void CargarUsuarios()
        {
            try
            {
                var usuarios = _bllUsuario.ObtenerTodos();
                cboUsuario.DisplayMember = "Username";
                cboUsuario.ValueMember   = "Id";
                cboUsuario.DataSource    = usuarios;
                cboUsuario.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[VersionHistorialForm.CargarUsuarios] {ex.Message}");
            }
        }

        // Deshabilitado por diseño (ver Designer); solo se habilita con una fila seleccionada —
        // mismo patrón que PedidoHistorialForm.Dgv_SelectionChanged.
        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            btnRestaurar.Enabled = dgv.SelectedRows.Count > 0;
        }

        private void BtnCerrar_Click(object sender, EventArgs e) => this.Close();

        private void btnCargar_Click(object sender, EventArgs e)
        {
            if (cboUsuario.SelectedValue == null) return;

            try
            {
                int idUsuario = (int)cboUsuario.SelectedValue;
                _versiones = _bll.ObtenerPorUsuario(idUsuario);
                _importado = null;            // vuelve a mostrar el historial de la base
                _archivoImportado = null;
                MostrarOrigen();
                CargarGrilla();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // Muestra el historial como VERSIONES (snapshots) del usuario: una fila por cada GUARDADO.
        // Cada fila es un estado completo en un instante dado (modelo snapshot/Memento), con su Id de
        // versión, fecha, autor y el detalle de qué cambió en ese guardado. "Restaurar" deja al usuario
        // EXACTAMENTE en el estado de la versión elegida (punto en el tiempo), sin arrastrar ni perder
        // los demás guardados. Solo datos administrativos NO sensibles (la contraseña nunca se versiona).
        private void CargarGrilla()
        {
            dgv.Rows.Clear();

            // Más reciente primero (el DAL ya devuelve ORDER BY Fecha DESC; se reordena por las dudas).
            var orden = new List<BE.VersionUsuario>(_versiones);
            orden.Sort((a, b) =>
            {
                int c = b.Fecha.CompareTo(a.Fecha);
                return c != 0 ? c : b.Id.CompareTo(a.Id);
            });

            foreach (var v in orden)
            {
                string fechaTxt    = v.Fecha.ToString("dd/MM/yyyy HH:mm:ss");
                string registroTxt = $"{v.UsernameSnapshot} (ID {v.IdUsuario})";
                // Si esta versión proviene de una restauración, se marca en "Modificado por".
                string actorTxt = EsRestauracion(v)
                    ? $"{v.Actor} · {Tr("hist.rollback", "rollback")}"
                    : v.Actor;
                // Versión leída de un archivo XML o JSON (no está en la base): se marca como "importada".
                if (_importado != null)
                    actorTxt = $"{actorTxt} · {Tr("hist.importada", "importada")}";

                // colId = Id de ESTA versión: "Restaurar" deja al usuario en el estado de este snapshot.
                dgv.Rows.Add(new object[] { v.Id, registroTxt, fechaTxt, actorTxt, v.Detalle ?? "" });
            }
        }

        // Una versión proviene de un rollback si su detalle es una restauración (formato nuevo
        // "Restauración a versión..." o el legacy "...antes de restaurar..."). Marcador interno
        // en español: no es texto de UI, solo se usa para detectar el origen.
        private static bool EsRestauracion(BE.VersionUsuario v)
        {
            if (string.IsNullOrEmpty(v.Detalle)) return false;
            return v.Detalle.IndexOf("Restauración a versión", StringComparison.OrdinalIgnoreCase) >= 0
                || v.Detalle.IndexOf("antes de restaurar",     StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void btnRestaurar_Click(object sender, EventArgs e)
        {
            if (dgv.CurrentRow == null)
            {
                MessageBox.Show(
                    Tr("msg.historial.sinseleccion", "Seleccione una versión de la grilla."),
                    Tr("msg.historial.atencion",     "Atención"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // colId = Id de la versión seleccionada: restaurarla deja al usuario EXACTAMENTE en el
            // estado de ese snapshot (punto en el tiempo), sin tocar los demás guardados del historial.
            int idVersion = Convert.ToInt32(dgv.CurrentRow.Cells["colId"].Value);
            var version = _versiones.Find(v => v.Id == idVersion);
            if (version == null) return;

            string fechaTxt = version.Fecha.ToString("dd/MM/yyyy HH:mm:ss");
            string fnac     = version.FechaNacSnapshot?.ToString("d") ?? "—";
            // Estado completo al que volverá el usuario (datos administrativos no sensibles).
            string estado =
                $"{Tr("lbl.ver.campo.usuario", "Usuario")}: {version.UsernameSnapshot}\n" +
                $"{Tr("lbl.ver.campo.nombre", "Nombre")}: {version.NombreSnapshot}\n" +
                $"{Tr("lbl.ver.campo.apellido", "Apellido")}: {version.ApellidoSnapshot}\n" +
                $"{Tr("lbl.ver.campo.email", "Email")}: {version.EmailSnapshot}\n" +
                $"{Tr("lbl.ver.campo.fechanac", "Fecha nac.")}: {fnac}";

            string msg = Tr("msg.historial.confirmar.restaurar",
                "¿Restaurar al usuario al estado del {0}?\n\n{1}\n\nEl usuario quedará exactamente en este estado. Es reversible (queda registrado como un nuevo cambio en el historial).",
                new object[] { fechaTxt, estado });
            if (_importado != null)
                msg += "\n\n" + Tr("msg.ver.xml.confirmar.importada",
                    "Esta versión proviene del archivo importado; no está guardada en el historial de la base.");

            if (!ConfirmarSiNo(msg, Tr("msg.backup.titulorestaura", "Confirmar Restauración"), porDefectoNo: true))
                return;

            try
            {
                // Mismo flujo de restauración (permiso, nombre en uso, DV, snapshot y bitácora):
                // la BLL solo cambia de dónde toma el Memento (la base o el archivo importado).
                if (_importado != null)
                    _bll.RestaurarVersionImportada(this.Text, _importado, idVersion);
                else
                    _bll.RestaurarVersion(this.Text, idVersion);
                MessageBox.Show(
                    Tr("msg.historial.restaurado",  "Versión restaurada correctamente."),
                    Tr("rpt.dlg.exito.titulo",       "Éxito"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Se vuelve al historial de la base del usuario restaurado (allí aparece el rollback).
                cboUsuario.SelectedValue = version.IdUsuario;
                if (cboUsuario.SelectedValue != null)
                    btnCargar_Click(null, EventArgs.Empty);
                else
                {
                    // Usuario fuera de la lista (p. ej. archivado): se sale del modo "importado".
                    _importado = null;
                    _archivoImportado = null;
                    _versiones.Clear();
                    CargarGrilla();
                    MostrarOrigen();
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ══ A02 Serialización — exportar / importar las versiones (Mementos) a XML o JSON ═════════

        // Carpeta inicial de los diálogos: %LOCALAPPDATA%\WardrobeFlow\Versiones (la crea si falta).
        // Si no se pudiera crear, el diálogo abre en su carpeta por defecto.
        private string CarpetaInicial()
        {
            try { return BLL.VersionUsuario.PrepararCarpetaVersiones(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[VersionHistorialForm.CarpetaInicial] {ex.Message}");
                return null;
            }
        }

        private void btnExportar_Click(object sender, EventArgs e)
        {
            var usuario = cboUsuario.SelectedItem as BE.Usuario;
            if (usuario == null)
            {
                MessageBox.Show(
                    Tr("msg.ver.xml.sinusuario", "Seleccione un usuario para exportar sus versiones."),
                    Tr("msg.historial.atencion", "Atención"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new SaveFileDialog
            {
                // XML o JSON: el formato lo decide la extensión elegida (BLL → FabricaSerializadores).
                // DefaultExt: si se escribe un nombre sin extensión, toma la del tipo elegido.
                Filter           = Tr("dlg.filtro.serial.guardar", "Archivos XML (*.xml)|*.xml|Archivos JSON (*.json)|*.json"),
                DefaultExt       = "xml",
                AddExtension     = true,
                InitialDirectory = CarpetaInicial(),
                FileName         = BLL.VersionUsuario.NombreArchivoSugerido(usuario.Username, DateTime.Now)
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var archivo = _bll.Exportar(this.Text, usuario.Id, dlg.FileName);
                    MessageBox.Show(
                        Tr("msg.ver.xml.exportado", "Se exportaron {0} versión(es) del usuario '{1}' a:\n{2}",
                           new object[] { archivo.Versiones.Count, archivo.Username, dlg.FileName }),
                        Tr("rpt.dlg.exito.titulo", "Éxito"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        private void btnImportar_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Filter           = Tr("dlg.filtro.serial.abrir", "Archivos XML o JSON (*.xml;*.json)|*.xml;*.json"),
                InitialDirectory = CarpetaInicial()
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    // La BLL deserializa y valida (formato, coherencia y que el usuario exista).
                    _importado        = _bll.Importar(this.Text, dlg.FileName);
                    _archivoImportado = dlg.FileName;
                    _versiones        = new List<BE.VersionUsuario>(_importado.Versiones);
                    cboUsuario.SelectedValue = _importado.IdUsuario;
                    MostrarOrigen();
                    CargarGrilla();
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        // Indica de dónde salen las versiones de la grilla: la base o un archivo importado.
        private void MostrarOrigen()
        {
            if (_importado == null)
            {
                lblOrigen.Text = dgv.Rows.Count > 0 || cboUsuario.SelectedValue != null
                    ? Tr("lbl.ver.origen.base", "Versiones guardadas en la base de datos.")
                    : "";
                return;
            }
            lblOrigen.Text = Tr("lbl.ver.origen.archivo",
                "Versiones importadas de '{0}' — usuario '{1}' (ID {2}), exportadas el {3} en el equipo {4}. Elija una y presione Restaurar.",
                new object[]
                {
                    System.IO.Path.GetFileName(_archivoImportado), _importado.Username, _importado.IdUsuario,
                    _importado.FechaExportacion.ToString("dd/MM/yyyy HH:mm"), _importado.Equipo
                });
        }
    }
}
