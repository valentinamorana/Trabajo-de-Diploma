using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Administración de Usuarios (ABM de datos administrativos NO sensibles).
    ///
    /// Panel de EDICIÓN de datos del usuario, complementario al panel "Gestión de Usuarios"
    /// (que concentra las operaciones de cuenta: alta, reset de contraseña, desbloqueo,
    /// archivado y purga). Acá se:
    ///   • busca/filtra por nombre, apellido o email (datos NO sensibles),
    ///   • modifica nombre, apellido, nombre de usuario, fecha de nacimiento y email,
    ///   • cambia el rol del usuario (con validación de "último administrador"),
    ///   • consulta el Historial de Cambios (campo / valor anterior / valor nuevo / quién / cuándo).
    ///
    /// La contraseña NUNCA se edita ni se muestra desde acá. Todas las operaciones quedan
    /// registradas en bitácora y en el Historial de Cambios (Memento) vía la BLL.
    /// </summary>
    public partial class AdministracionUsuariosForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Usuario _usuarioBLL = new BLL.Usuario();
        private readonly BLL.Familia _familiaBLL = new BLL.Familia();

        private List<BE.Usuario> _usuarios = new List<BE.Usuario>();
        private int  _idSeleccionado = 0;
        private bool _modoAlta = false;   // true mientras se cargan los datos de un usuario NUEVO

        private static readonly Color RosaOscuro   = Color.FromArgb(176, 62, 96);
        private static readonly Color PanelClaro   = Color.FromArgb(245, 245, 250);

        public AdministracionUsuariosForm()
        {
            InitializeComponent();
            HabilitarEdicion(false);
        }

        // ── Ciclo de vida ────────────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir();
            CargarRoles();
            CargarUsuarios();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma) { Traducir(); CargarUsuarios(); }

        private void Traducir()
        {
            this.Text            = Tr("frm.adminusuarios",      "Administración de Usuarios");
            lblTitulo.Text       = Tr("lbl.adminusr.titulo",    "Administración de Usuarios");
            lblBuscar.Text       = Tr("lbl.adminusr.buscar",    "Buscar (nombre, apellido o email):");
            btnBuscar.Text       = Tr("btn.adminusr.buscar",    "Buscar");
            btnRefrescar.Text    = Tr("btn.adminusr.refrescar", "Ver todos");
            btnNuevo.Text        = Tr("btn.adminusr.nuevo",     "Nuevo usuario");
            lblDatos.Text        = Tr("lbl.adminusr.datos",     "Datos del usuario seleccionado");
            lblNombre.Text       = Tr("lbl.adminusr.nombre",    "Nombre:");
            lblApellido.Text     = Tr("lbl.adminusr.apellido",  "Apellido:");
            lblUsername.Text     = Tr("lbl.adminusr.username",  "Nombre de usuario:");
            lblEmail.Text        = Tr("lbl.adminusr.email",     "Email:");
            lblNacimiento.Text   = Tr("lbl.adminusr.nacimiento","Fecha de nacimiento:");
            lblRol.Text          = Tr("lbl.adminusr.rol",       "Rol:");
            btnGuardar.Text      = Tr("btn.adminusr.guardar",   "Guardar cambios");
            btnCambiarRol.Text   = Tr("btn.adminusr.cambiarrol","Cambiar rol");
            btnHistorial.Text    = Tr("btn.adminusr.historial", "Ver historial de cambios");
            btnCerrar.Text       = Tr("btn.permisos.cerrar",    "Cerrar");
            TraducirHeaders();
        }

        private void TraducirHeaders()
        {
            if (dgv.Columns.Count == 0) return;
            void RH(string col, string clave, string fb)
            {
                if (dgv.Columns.Contains(col)) dgv.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("Username", "col.usr.username", "Usuario");
            RH("Nombre",   "col.adminusr.nombre",   "Nombre");
            RH("Apellido", "col.adminusr.apellido", "Apellido");
            RH("Email",    "col.adminusr.email",    "Email");
            RH("Perfil",   "col.usr.perfil",        "Rol");
        }

        // ── Carga ─────────────────────────────────────────────────────────────────
        private void CargarRoles()
        {
            try
            {
                var roles = _familiaBLL.ObtenerRoles();
                var items = new List<RolItem>();
                foreach (var r in roles) items.Add(new RolItem(r, TraductorPerfil.Nombre(r)));
                cmbRol.DataSource    = items;
                cmbRol.DisplayMember = "Label";
                cmbRol.ValueMember   = "Value";
                cmbRol.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[AdministracionUsuariosForm.CargarRoles] " + ex.Message);
            }
        }

        private void CargarUsuarios(string filtro = null)
        {
            try
            {
                _usuarios = string.IsNullOrWhiteSpace(filtro)
                    ? _usuarioBLL.ObtenerTodos()
                    : _usuarioBLL.Buscar(filtro);

                var tabla = new DataTable();
                tabla.Columns.Add("ID",       typeof(int));
                tabla.Columns.Add("Username", typeof(string));
                tabla.Columns.Add("Nombre",   typeof(string));
                tabla.Columns.Add("Apellido", typeof(string));
                tabla.Columns.Add("Email",    typeof(string));
                tabla.Columns.Add("Perfil",   typeof(string));
                foreach (var u in _usuarios)
                    tabla.Rows.Add(u.Id, u.Username, u.Nombre, u.Apellido, u.Email,
                                   TraductorPerfil.Nombre(u.Perfil));

                dgv.DataSource = tabla;
                if (dgv.Columns.Contains("ID")) dgv.Columns["ID"].Visible = false;
                // El email es más largo que el resto: darle más ancho relativo para que no se trunque.
                if (dgv.Columns.Contains("Email")) dgv.Columns["Email"].FillWeight = 170;
                TraducirHeaders();

                lblMensaje.ForeColor = Color.DarkGreen;
                lblMensaje.Text = string.Format(Tr("msg.adminusr.cargados", "{0} usuario(s)."), _usuarios.Count);
                LimpiarEdicion();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void DgvSelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) { LimpiarEdicion(); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
            var u = _usuarios.Find(x => x.Id == id);
            if (u == null) { LimpiarEdicion(); return; }

            // Seleccionar un usuario existente sale del modo alta.
            _modoAlta = false;
            btnGuardar.Text = Tr("btn.adminusr.guardar", "Guardar cambios");
            _idSeleccionado     = u.Id;
            txtNombre.Text     = u.Nombre   ?? "";
            txtApellido.Text   = u.Apellido ?? "";
            txtUsername.Text   = u.Username ?? "";
            txtEmail.Text      = u.Email    ?? "";
            if (u.FechaNacimiento.HasValue)
            {
                dtpNacimiento.Checked = true;
                dtpNacimiento.Value   = u.FechaNacimiento.Value;
            }
            else dtpNacimiento.Checked = false;

            // Preseleccionar el rol actual en el combo.
            for (int i = 0; i < cmbRol.Items.Count; i++)
                if (cmbRol.Items[i] is RolItem ri &&
                    string.Equals(ri.Value, u.Perfil, StringComparison.OrdinalIgnoreCase))
                { cmbRol.SelectedIndex = i; break; }

            HabilitarEdicion(true);
        }

        private void LimpiarEdicion()
        {
            _idSeleccionado = 0;
            _modoAlta = false;
            txtNombre.Text = txtApellido.Text = txtUsername.Text = txtEmail.Text = "";
            dtpNacimiento.Checked = false;
            cmbRol.SelectedIndex = -1;
            HabilitarEdicion(false);
            if (btnGuardar != null) btnGuardar.Text = Tr("btn.adminusr.guardar", "Guardar cambios");
        }

        private void HabilitarEdicion(bool on)
        {
            txtNombre.Enabled = txtApellido.Enabled = txtUsername.Enabled = on;
            txtEmail.Enabled  = dtpNacimiento.Enabled = cmbRol.Enabled = on;
            btnGuardar.Enabled = btnCambiarRol.Enabled = btnHistorial.Enabled = on;
        }

        // ── Acciones ────────────────────────────────────────────────────────────────

        private void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CargarUsuarios(txtBuscar.Text); }
        }

        private void BtnBuscar_Click(object sender, EventArgs e) => CargarUsuarios(txtBuscar.Text);

        private void BtnRefrescar_Click(object sender, EventArgs e) { txtBuscar.Clear(); CargarUsuarios(); }

        private void BtnNuevo_Click(object sender, EventArgs e) => ModoAlta();

        private void BtnGuardar_Click(object sender, EventArgs e) => Guardar();

        private void BtnCambiarRol_Click(object sender, EventArgs e) => CambiarRol();

        private void BtnHistorial_Click(object sender, EventArgs e) => VerHistorial();

        private void BtnCerrar_Click(object sender, EventArgs e) => this.Close();

        // Entra en modo "alta": habilita los campos vacíos para cargar un usuario NUEVO.
        // La contraseña la genera la BLL y se exporta a un archivo (no se ingresa acá).
        private void ModoAlta()
        {
            dgv.ClearSelection();   // dispara LimpiarEdicion; luego activamos el modo alta
            _modoAlta = true;
            _idSeleccionado = 0;
            txtNombre.Text = txtApellido.Text = txtUsername.Text = txtEmail.Text = "";
            dtpNacimiento.Checked = false;
            cmbRol.SelectedIndex = cmbRol.Items.Count > 0 ? 0 : -1;
            HabilitarEdicion(true);
            btnCambiarRol.Enabled = false;   // el rol se elige en el alta; "cambiar rol" es para existentes
            btnHistorial.Enabled  = false;
            btnGuardar.Text = Tr("btn.adminusr.crear", "Crear usuario");
            lblMensaje.ForeColor = System.Drawing.Color.DimGray;
            lblMensaje.Text = Tr("msg.adminusr.modoalta", "Cargá los datos del nuevo usuario y presioná Crear.");
            txtNombre.Focus();
        }

        // Guarda: crea (modo alta) o actualiza (usuario seleccionado). En edición, "Guardar cambios"
        // persiste TANTO los datos administrativos COMO el rol si cambió (el botón "Cambiar rol" queda
        // como atajo). Así no hay confusión de "cambié el rol pero no se guardó".
        private void Guardar()
        {
            if (_modoAlta) { Alta(); return; }
            if (_idSeleccionado == 0) return;

            var actual = _usuarios.Find(x => x.Id == _idSeleccionado);
            string rolSel = (cmbRol.SelectedItem as RolItem)?.Value;
            try
            {
                bool huboCambio = false;
                DateTime? fnac = dtpNacimiento.Checked ? (DateTime?)dtpNacimiento.Value.Date : null;

                // Datos administrativos (si no cambió nada, la BLL lanza 'sin_cambios' y lo ignoramos).
                try
                {
                    _usuarioBLL.Modificar(this.Text, _idSeleccionado,
                        txtNombre.Text, txtApellido.Text, txtUsername.Text, fnac, txtEmail.Text);
                    huboCambio = true;
                }
                catch (BE.AppException ex) when (ex.Clave == "err.bll.usuario.sin_cambios") { }

                // Rol (solo si el seleccionado difiere del actual).
                if (!string.IsNullOrWhiteSpace(rolSel) && actual != null
                    && !string.Equals(rolSel, actual.Perfil, StringComparison.OrdinalIgnoreCase))
                {
                    _usuarioBLL.CambiarRol(this.Text, _idSeleccionado, rolSel);
                    GUI.Menu.RefrescarSeguridadAbierta();
                    huboCambio = true;
                }

                if (!huboCambio)
                { MostrarError(Tr("err.bll.usuario.sin_cambios", "No hay cambios para guardar.")); return; }

                MostrarOk(Tr("msg.adminusr.guardado", "Datos del usuario actualizados correctamente."));
                CargarUsuarios(txtBuscar.Text);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // Alta de un usuario nuevo con sus datos administrativos. La BLL genera la contraseña,
        // la exporta a un .txt y registra en bitácora + versión base del historial.
        private void Alta()
        {
            string rol = (cmbRol.SelectedItem as RolItem)?.Value;
            if (string.IsNullOrWhiteSpace(rol))
            { MostrarError(Tr("err.adminusr.rol_vacio", "Seleccioná un rol.")); return; }
            try
            {
                DateTime? fnac = dtpNacimiento.Checked ? (DateTime?)dtpNacimiento.Value.Date : null;
                string ruta = _usuarioBLL.Alta(this.Text, txtUsername.Text, rol,
                    txtNombre.Text, txtApellido.Text, fnac, txtEmail.Text);
                _modoAlta = false;
                btnGuardar.Text = Tr("btn.adminusr.guardar", "Guardar cambios");
                CargarUsuarios();
                MostrarOk(string.Format(
                    Tr("msg.adminusr.creado", "Usuario '{0}' creado. Credenciales en: {1}"),
                    txtUsername.Text.Trim(), ruta));
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void CambiarRol()
        {
            if (_idSeleccionado == 0) return;
            string rol = (cmbRol.SelectedItem as RolItem)?.Value;
            if (string.IsNullOrWhiteSpace(rol))
            { MostrarError(Tr("err.adminusr.rol_vacio", "Seleccioná un rol.")); return; }
            try
            {
                _usuarioBLL.CambiarRol(this.Text, _idSeleccionado, rol);
                GUI.Menu.RefrescarSeguridadAbierta();   // re-aplica seguridad en vivo si tocó al usuario en sesión
                MostrarOk(Tr("msg.adminusr.rolcambiado", "Rol del usuario actualizado correctamente."));
                CargarUsuarios(txtBuscar.Text);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void VerHistorial()
        {
            if (_idSeleccionado == 0) return;
            try { new VersionHistorialForm(_idSeleccionado).ShowDialog(this); }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ── Combo de roles (valor técnico + etiqueta visible) ──────────────────────
        private class RolItem
        {
            public string Value { get; }
            public string Label { get; }
            public RolItem(string value, string label) { Value = value; Label = label; }
            public override string ToString() => Label;
        }
    }
}
