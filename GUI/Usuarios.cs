using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — Administración de Usuarios (Menú → Administrar → Usuarios).
    ///
    /// Una sola pantalla con dos pestañas (antes eran dos formularios, "Cuentas de Usuario" y
    /// "Administración de Usuarios", y los dos podían dar de alta):
    ///   • Cuentas: operaciones sobre la cuenta — resetear contraseña, desbloquear, archivar
    ///     (baja lógica), ver archivados y purgar los archivados hace más de un año.
    ///   • Datos administrativos: el ÚNICO alta de usuarios ("Nuevo usuario"), la edición de los
    ///     datos no sensibles (nombre, apellido, usuario, nacimiento, email), el cambio de rol
    ///     (con la validación de "último administrador") y el Historial de Cambios (Memento).
    ///
    /// Las dos pestañas están gobernadas por la misma patente (mnuUsuarios), igual que lo estaban
    /// los dos formularios: la fusión no amplía el acceso de ningún rol.
    ///
    /// CUMPLE REQUISITOS T02:
    ///   Las contraseñas se generan y hashean en la BLL (nunca se ingresan ni se muestran acá)
    ///   Se registra la actividad en la bitácora y en el Historial de Cambios (vía BLL)
    ///   Al dar de alta, la BLL vincula al usuario con su legajo de Empleado
    ///
    /// Hereda de <see cref="FormBase"/>: MostrarOk/MostrarError/MostrarCarga escriben en la
    /// barra de resultado común a las dos pestañas (lblMensaje).
    /// </summary>
    public partial class Usuarios : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Usuario usuarioBLL  = new BLL.Usuario();
        private readonly BLL.Familia _familiaBLL = new BLL.Familia();

        // Idioma activo — sincronizado en Traducir() para usar en CargarUsuarios
        private Idioma _idioma = GestorIdioma.IdiomaActual;

        // Pestaña "Cuentas"
        private bool _viendoArchivados = false;

        // Pestaña "Datos administrativos"
        private List<BE.Usuario> _usuarios = new List<BE.Usuario>();
        private int  _idSeleccionado = 0;
        private bool _modoAlta = false;   // true mientras se cargan los datos de un usuario NUEVO

        /// <summary>Constructor: inicializa el formulario y el estilo de las dos grillas.</summary>
        public Usuarios()
        {
            InitializeComponent();
            // Estilo de grilla compartido (encabezado rosa, filas alternadas) — EstiloFormulario.
            Estilos.EstiloFormulario.Grilla(dgvUsuarios);
            Estilos.EstiloFormulario.Grilla(dgvDatos);
            HabilitarEdicion(false);
        }

        // ── Ciclo de vida y Observer de idioma ────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
            CargarRoles();
            CargarDatos();
            CargarUsuarios();   // último: el mensaje de carga es el de la pestaña visible (Cuentas)
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            // Refrescar grillas para que headers y valores de estado reflejen el nuevo idioma
            // Con cambios sin guardar, Traducir ya re-tradujo los encabezados; los valores se refrescan
            // al guardar o al volver a la pestaña.
            if (!HayEdicionEnCurso()) CargarDatos(txtBuscar.Text);
            CargarUsuarios();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            lblTitulo.Text  = Tr("lbl.adminusr.titulo", "Administración de Usuarios");
            tabCuentas.Text = Tr("usr.tab.cuentas",     "Cuentas");
            tabDatos.Text   = Tr("usr.tab.datos",       "Datos administrativos");

            // Pestaña "Cuentas" (claves en el Tag de cada control)
            Aplicar(btnRefrescar,         t);
            Aplicar(lblResetTitulo,       t);
            Aplicar(lblResetInfo,         t);
            Aplicar(btnResetearClave,     t);
            Aplicar(lblDesbloquearTitulo, t);
            Aplicar(lblDesbloquearInfo,   t);
            Aplicar(btnDesbloquear,       t);
            Aplicar(lblListaTitulo,       t);
            Aplicar(btnArchivar,          t);
            Aplicar(btnVerArchivados,     t);
            Aplicar(btnPurgar,            t);
            if (_viendoArchivados) btnVerArchivados.Text = Tr("btn.usr.veractivos", "Ver activos");
            TraducirHeadersGrilla();

            // Pestaña "Datos administrativos"
            lblBuscar.Text     = Tr("lbl.adminusr.buscar",    "Buscar (nombre, apellido o email):");
            btnBuscar.Text     = Tr("btn.adminusr.buscar",    "Buscar");
            btnVerTodos.Text   = Tr("btn.adminusr.refrescar", "Ver todos");
            btnNuevo.Text      = Tr("btn.adminusr.nuevo",     "Nuevo usuario");
            lblDatos.Text      = Tr("lbl.adminusr.datos",     "Datos del usuario seleccionado");
            lblNombre.Text     = Tr("lbl.adminusr.nombre",    "Nombre:");
            lblApellido.Text   = Tr("lbl.adminusr.apellido",  "Apellido:");
            lblUsername.Text   = Tr("lbl.adminusr.username",  "Nombre de usuario:");
            lblEmail.Text      = Tr("lbl.adminusr.email",     "Email:");
            lblNacimiento.Text = Tr("lbl.adminusr.nacimiento","Fecha de nacimiento:");
            lblRol.Text        = Tr("lbl.adminusr.rol",       "Rol:");
            btnGuardar.Text    = _modoAlta ? Tr("btn.adminusr.crear", "Crear usuario")
                                           : Tr("btn.adminusr.guardar", "Guardar cambios");
            btnCambiarRol.Text = Tr("btn.adminusr.cambiarrol","Cambiar rol");
            btnHistorial.Text  = Tr("btn.adminusr.historial", "Ver historial de cambios");
            TraducirHeadersDatos();
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        // Cada pestaña recarga su grilla al mostrarse: lo que se hizo en la otra (un alta, un
        // archivado, un cambio de rol) se ve sin tener que refrescar a mano.
        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabs.SelectedTab == tabDatos) { if (!HayEdicionEnCurso()) CargarDatos(txtBuscar.Text); }
            else CargarUsuarios();
        }

        // Un alta o cambios sin guardar sobre el usuario seleccionado no se descartan al cambiar de
        // pestaña o de idioma (recargar la grilla limpia los campos). Tener una fila seleccionada sin
        // tocar NO cuenta: la grilla se recarga para no operar sobre datos viejos (un usuario archivado
        // desde Cuentas, por ejemplo).
        private bool HayEdicionEnCurso() =>
            _modoAlta || (_idSeleccionado != 0 && FotoEdicion() != _fotoAlSeleccionar);

        // Lo que muestran los campos de edición, tal como quedaron al seleccionar el usuario: así un
        // rol que no está en el combo (o un combo que no cargó) no cuenta como "cambio sin guardar".
        private string _fotoAlSeleccionar;
        private string FotoEdicion() => string.Join("\u001f",   // separador: un cambio que cruce campos no se confunde
            txtNombre.Text, txtApellido.Text, txtUsername.Text, txtEmail.Text,
            dtpNacimiento.Checked ? dtpNacimiento.Value.Date.ToString("yyyyMMdd") : "",
            (cmbRol.SelectedItem as RolItem)?.Value ?? "");

        // ══ Pestaña "Cuentas" ══════════════════════════════════════════════════

        /// <summary>Traduce los HeaderText de la grilla de cuentas según el idioma activo.</summary>
        private void TraducirHeadersGrilla()
        {
            var t = Traductor.ObtenerTraducciones(_idioma);
            void RH(string col, string clave, string fallback)
            {
                if (dgvUsuarios.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvUsuarios.Columns[col].HeaderText = t[clave].Texto;
                else if (dgvUsuarios.Columns.Contains(col))
                    dgvUsuarios.Columns[col].HeaderText = fallback;
            }
            RH("Username", "col.usr.username", "Usuario");
            RH("Perfil",   "col.usr.perfil",   "Perfil");
            RH("Estado",   "col.usr.estado",   "Estado");

            // Ocultar columna interna de clave de bloqueo
            if (dgvUsuarios.Columns.Contains("_BloqueadoKey"))
                dgvUsuarios.Columns["_BloqueadoKey"].Visible = false;
        }

        private void BtnRefrescar_Click(object sender, EventArgs e)
        {
            CargarUsuarios();
        }

        private void DgvUsuarios_SelectionChanged(object sender, EventArgs e)
        {
            bool haySeleccion = dgvUsuarios.SelectedRows.Count > 0;

            // En la vista de archivados no hay acciones individuales (solo purga masiva).
            if (_viendoArchivados)
            {
                btnResetearClave.Enabled = false;
                btnDesbloquear.Enabled   = false;
                btnArchivar.Enabled      = false;
                return;
            }

            btnResetearClave.Enabled = haySeleccion;
            btnArchivar.Enabled      = haySeleccion;

            // Desbloquear solo se habilita si el usuario seleccionado está bloqueado.
            // Usamos la columna interna _BloqueadoKey (int) para ser independientes del idioma.
            if (haySeleccion && dgvUsuarios.Columns.Contains("_BloqueadoKey"))
            {
                var bloqCell = dgvUsuarios.SelectedRows[0].Cells["_BloqueadoKey"];
                btnDesbloquear.Enabled = bloqCell?.Value?.ToString() == "1";
            }
            else
            {
                btnDesbloquear.Enabled = false;
            }
        }

        /// <summary>
        /// Carga la lista de cuentas (activas o archivadas) desde la BLL → DAL y la muestra en la grilla.
        /// Las contraseñas NO se muestran por seguridad.
        /// </summary>
        private void CargarUsuarios()
        {
            try
            {
                List<BE.Usuario> usuarios = _viendoArchivados
                    ? usuarioBLL.ObtenerArchivados()
                    : usuarioBLL.ObtenerTodos();

                string lblActivo    = Tr("usr.activo",    "Activo");
                string lblBloqueada = Tr("usr.bloqueada", "Bloqueada");
                string lblArchivado = Tr("usr.archivado", "Archivado");

                var tabla = new DataTable();
                tabla.Columns.Add("ID",           typeof(int));
                tabla.Columns.Add("Username",     typeof(string));
                tabla.Columns.Add("Perfil",       typeof(string));
                tabla.Columns.Add("Estado",       typeof(string));
                // Columna interna: 1 = bloqueado/archivado, 0 = activo — independiente del idioma
                tabla.Columns.Add("_BloqueadoKey", typeof(int));

                foreach (var u in usuarios)
                {
                    string estado = _viendoArchivados
                        ? (u.FechaBaja.HasValue ? $"{lblArchivado} ({u.FechaBaja.Value:d})" : lblArchivado)
                        : (u.Bloqueado ? lblBloqueada : lblActivo);
                    tabla.Rows.Add(
                        u.Id,
                        u.Username,
                        TraductorPerfil.Nombre(u.Perfil),
                        estado,
                        (_viendoArchivados || u.Bloqueado) ? 1 : 0);
                }

                dgvUsuarios.DataSource = tabla;
                TraducirHeadersGrilla();

                // Colorear filas bloqueadas/archivadas usando la columna interna (independiente del idioma)
                foreach (DataGridViewRow fila in dgvUsuarios.Rows)
                {
                    if (fila.Cells["_BloqueadoKey"].Value?.ToString() == "1")
                    {
                        fila.DefaultCellStyle.BackColor = _viendoArchivados ? Tema.PanelClaro : Tema.FondoError;
                        fila.DefaultCellStyle.ForeColor = _viendoArchivados ? Color.DimGray : Color.DarkRed;
                    }
                }

                string fmt = _viendoArchivados
                    ? Tr("msg.usr.archivados", "{0} usuario(s) archivado(s).")
                    : Tr("msg.usr.cargados",   "{0} usuario(s) registrado(s).");
                MostrarCarga(string.Format(fmt, usuarios.Count));

                btnResetearClave.Enabled = false;
                btnDesbloquear.Enabled   = false;
                btnArchivar.Enabled      = false;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        /// <summary>
        /// Resetea la contraseña del usuario seleccionado generándola automáticamente.
        /// No solicita contraseña al administrador — la BLL la genera y exporta a archivo.
        /// Solo funciona si el usuario en sesión es Administrador.
        /// </summary>
        private void BtnResetearClave_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MostrarError(Tr("err.usr.selecciona", "Seleccioná un usuario de la lista."));
                return;
            }

            int    idUsuario = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);
            string username  = dgvUsuarios.SelectedRows[0].Cells["Username"].Value?.ToString() ?? "";

            if (!ConfirmarSiNo(Tr("conf.usr.resetclave",
                    "¿Generar una contraseña nueva para '{0}'?\n\nLa actual deja de funcionar y la nueva se exporta a un archivo.",
                    new object[] { username }),
                    Tr("conf.usr.resetclave.titulo", "Resetear contraseña"), porDefectoNo: true))
                return;

            try
            {
                string rutaArchivo = usuarioBLL.ResetearClave(this.Text, idUsuario, username);
                MostrarOk(string.Format(Tr("msg.usr.clave.exportada",
                    "Contraseña de '{0}' regenerada.\nCredenciales en: {1}"), username, rutaArchivo));
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        /// <summary>
        /// Desbloquea la cuenta del usuario seleccionado en la grilla.
        /// Solo funciona si el usuario en sesión es Administrador.
        /// </summary>
        private void BtnDesbloquear_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MostrarError(Tr("err.usr.sel.bloqueado", "Seleccioná un usuario bloqueado de la lista."));
                return;
            }

            int    idUsuario = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);
            string username  = dgvUsuarios.SelectedRows[0].Cells["Username"].Value?.ToString() ?? "";

            if (!ConfirmarSiNo(
                    string.Format(Tr("conf.desbloquear.body", "¿Desbloquear la cuenta de '{0}'?"), username),
                    Tr("conf.desbloquear.titulo", "Confirmar Desbloqueo"), porDefectoNo: true))
                return;

            try
            {
                usuarioBLL.Desbloquear(this.Text, idUsuario, username);
                CargarUsuarios();
                MostrarOk(string.Format(Tr("msg.usr.desbloqueada", "Cuenta '{0}' desbloqueada correctamente."), username));
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ── RF-10 — Archivar / Ver archivados / Purgar ────────────────────────

        // Archiva (baja lógica) el usuario seleccionado. La BLL protege al último admin y al self.
        private void BtnArchivar_Click(object sender, EventArgs e)
        {
            if (_viendoArchivados || dgvUsuarios.SelectedRows.Count == 0) return;

            int    idUsuario = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);
            string username  = dgvUsuarios.SelectedRows[0].Cells["Username"].Value?.ToString() ?? "";

            if (!ConfirmarSiNo(
                    string.Format(Tr("conf.usr.archivar.body",
                        "¿Archivar al usuario '{0}'?\n\nNo podrá iniciar sesión y saldrá de la lista, pero se conserva su historial.\nPodrá eliminarse definitivamente tras 1 año."), username),
                    Tr("conf.usr.archivar.titulo", "Confirmar Archivado"), porDefectoNo: true))
                return;

            try
            {
                usuarioBLL.Eliminar(this.Text, idUsuario, username);
                CargarUsuarios();
                MostrarOk(string.Format(Tr("msg.usr.archivado", "Usuario '{0}' archivado correctamente."), username));
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // Alterna entre la lista de usuarios activos y la de archivados.
        private void BtnVerArchivados_Click(object sender, EventArgs e)
        {
            _viendoArchivados = !_viendoArchivados;
            btnVerArchivados.Text = _viendoArchivados
                ? Tr("btn.usr.veractivos",    "Ver activos")
                : Tr("btn.usr.verarchivados", "Ver archivados");
            CargarUsuarios();
        }

        // Purga (eliminación física) de todos los usuarios archivados hace más de 1 año.
        private void BtnPurgar_Click(object sender, EventArgs e)
        {
            int elegibles;
            try { elegibles = usuarioBLL.ObtenerArchivadosParaPurga().Count; }
            catch (Exception ex) { MostrarError(ex); return; }

            if (elegibles == 0)
            {
                MostrarOk(Tr("msg.usr.purga.ninguno", "No hay usuarios archivados con más de 1 año para purgar."));
                return;
            }

            if (!ConfirmarSiNo(
                    string.Format(Tr("conf.usr.purgar.body",
                        "Se eliminarán DEFINITIVAMENTE {0} usuario(s) archivado(s) hace más de 1 año.\nEsta acción no se puede deshacer.\n\n¿Continuar?"), elegibles),
                    Tr("conf.usr.purgar.titulo", "Confirmar Purga"), porDefectoNo: true))
                return;

            try
            {
                int eliminados = usuarioBLL.PurgarArchivados(this.Text);
                CargarUsuarios();
                MostrarOk(string.Format(Tr("msg.usr.purga.ok", "{0} usuario(s) eliminado(s) definitivamente."), eliminados));
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ══ Pestaña "Datos administrativos" ════════════════════════════════════
        //   • busca/filtra por nombre, apellido o email (datos NO sensibles),
        //   • da de alta (único alta del sistema) y modifica nombre, apellido, usuario, nacimiento y email,
        //   • cambia el rol del usuario (con validación de "último administrador"),
        //   • consulta el Historial de Cambios (campo / valor anterior / valor nuevo / quién / cuándo).
        // La contraseña NUNCA se edita ni se muestra desde acá.

        private void TraducirHeadersDatos()
        {
            if (dgvDatos.Columns.Count == 0) return;
            void RH(string col, string clave, string fb)
            {
                if (dgvDatos.Columns.Contains(col)) dgvDatos.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("Username", "col.usr.username",      "Usuario");
            RH("Nombre",   "col.adminusr.nombre",   "Nombre");
            RH("Apellido", "col.adminusr.apellido", "Apellido");
            RH("Email",    "col.adminusr.email",    "Email");
            RH("Perfil",   "col.usr.perfil",        "Rol");
        }

        private void CargarRoles()
        {
            try
            {
                var items = new List<RolItem>();
                foreach (var r in _familiaBLL.ObtenerRoles()) items.Add(new RolItem(r, TraductorPerfil.Nombre(r)));
                cmbRol.DataSource    = items;
                cmbRol.DisplayMember = "Label";
                cmbRol.ValueMember   = "Value";
                cmbRol.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Usuarios.CargarRoles] " + ex.Message);
            }
        }

        private void CargarDatos(string filtro = null)
        {
            try
            {
                _usuarios = string.IsNullOrWhiteSpace(filtro)
                    ? usuarioBLL.ObtenerTodos()
                    : usuarioBLL.Buscar(filtro);

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

                dgvDatos.DataSource = tabla;
                if (dgvDatos.Columns.Contains("ID")) dgvDatos.Columns["ID"].Visible = false;
                // El email es más largo que el resto: darle más ancho relativo para que no se trunque.
                if (dgvDatos.Columns.Contains("Email")) dgvDatos.Columns["Email"].FillWeight = 170;
                TraducirHeadersDatos();

                MostrarCarga(string.Format(Tr("msg.adminusr.cargados", "{0} usuario(s)."), _usuarios.Count));
                LimpiarEdicion();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void DgvDatos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDatos.SelectedRows.Count == 0) { LimpiarEdicion(); return; }
            int id = Convert.ToInt32(dgvDatos.SelectedRows[0].Cells["ID"].Value);
            var u = _usuarios.Find(x => x.Id == id);
            if (u == null) { LimpiarEdicion(); return; }

            // Seleccionar un usuario existente sale del modo alta.
            _modoAlta = false;
            btnGuardar.Text  = Tr("btn.adminusr.guardar", "Guardar cambios");
            _idSeleccionado  = u.Id;
            txtNombre.Text   = u.Nombre   ?? "";
            txtApellido.Text = u.Apellido ?? "";
            txtUsername.Text = u.Username ?? "";
            txtEmail.Text    = u.Email    ?? "";
            if (u.FechaNacimiento.HasValue)
            {
                dtpNacimiento.Checked = true;
                dtpNacimiento.Value   = u.FechaNacimiento.Value;
            }
            else dtpNacimiento.Checked = false;

            // Preseleccionar el rol actual en el combo. Si no está en la lista, queda SIN selección
            // (antes quedaba el rol del usuario anterior y Guardar se lo asignaba sin querer).
            cmbRol.SelectedIndex = -1;
            for (int i = 0; i < cmbRol.Items.Count; i++)
                if (cmbRol.Items[i] is RolItem ri &&
                    string.Equals(ri.Value, u.Perfil, StringComparison.OrdinalIgnoreCase))
                { cmbRol.SelectedIndex = i; break; }

            HabilitarEdicion(true);
            _fotoAlSeleccionar = FotoEdicion();
        }

        private void LimpiarEdicion()
        {
            _idSeleccionado = 0;
            _modoAlta = false;
            txtNombre.Text = txtApellido.Text = txtUsername.Text = txtEmail.Text = "";
            dtpNacimiento.Checked = false;
            cmbRol.SelectedIndex = -1;
            HabilitarEdicion(false);
            btnGuardar.Text = Tr("btn.adminusr.guardar", "Guardar cambios");
        }

        private void HabilitarEdicion(bool on)
        {
            txtNombre.Enabled = txtApellido.Enabled = txtUsername.Enabled = on;
            txtEmail.Enabled  = dtpNacimiento.Enabled = cmbRol.Enabled = on;
            btnGuardar.Enabled = btnCambiarRol.Enabled = btnHistorial.Enabled = on;
        }

        private void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CargarDatos(txtBuscar.Text); }
        }

        private void BtnBuscar_Click(object sender, EventArgs e) => CargarDatos(txtBuscar.Text);

        private void BtnVerTodos_Click(object sender, EventArgs e) { txtBuscar.Clear(); CargarDatos(); }

        private void BtnNuevo_Click(object sender, EventArgs e) => ModoAlta();

        private void BtnGuardar_Click(object sender, EventArgs e) => Guardar();

        private void BtnCambiarRol_Click(object sender, EventArgs e) => CambiarRol();

        private void BtnHistorial_Click(object sender, EventArgs e) => VerHistorial();

        // Entra en modo "alta": habilita los campos vacíos para cargar un usuario NUEVO.
        // La contraseña la genera la BLL y se exporta a un archivo (no se ingresa acá).
        private void ModoAlta()
        {
            dgvDatos.ClearSelection();   // dispara LimpiarEdicion; luego activamos el modo alta
            _modoAlta = true;
            _idSeleccionado = 0;
            txtNombre.Text = txtApellido.Text = txtUsername.Text = txtEmail.Text = "";
            dtpNacimiento.Checked = false;
            // Sin rol preseleccionado: la lista está en orden alfabético y el primero puede ser
            // Administrador. Se elige a mano (la BLL rechaza el alta sin rol).
            cmbRol.SelectedIndex = -1;
            HabilitarEdicion(true);
            btnCambiarRol.Enabled = false;   // el rol se elige en el alta; "cambiar rol" es para existentes
            btnHistorial.Enabled  = false;
            btnGuardar.Text = Tr("btn.adminusr.crear", "Crear usuario");
            lblMensaje.ForeColor = Color.DimGray;
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
            bool huboCambio = false;
            try
            {
                DateTime? fnac = dtpNacimiento.Checked ? (DateTime?)dtpNacimiento.Value.Date : null;

                // Datos administrativos (si no cambió nada, la BLL lanza 'sin_cambios' y lo ignoramos).
                try
                {
                    usuarioBLL.Modificar(this.Text, _idSeleccionado,
                        txtNombre.Text, txtApellido.Text, txtUsername.Text, fnac, txtEmail.Text);
                    huboCambio = true;
                }
                catch (BE.AppException ex) when (ex.Clave == "err.bll.usuario.sin_cambios") { }

                // Rol (solo si el seleccionado difiere del actual).
                if (!string.IsNullOrWhiteSpace(rolSel) && actual != null
                    && !string.Equals(rolSel, actual.Perfil, StringComparison.OrdinalIgnoreCase))
                {
                    usuarioBLL.CambiarRol(this.Text, _idSeleccionado, rolSel);
                    GUI.Menu.RefrescarSeguridadAbierta();
                    huboCambio = true;
                }

                if (!huboCambio)
                { MostrarError(Tr("err.bll.usuario.sin_cambios", "No hay cambios para guardar.")); return; }

                MostrarOk(Tr("msg.adminusr.guardado", "Datos del usuario actualizados correctamente."));
                CargarDatos(txtBuscar.Text);
            }
            catch (Exception ex)
            {
                // Si una parte ya se guardó (los datos) y falló otra (el rol), la grilla se recarga para
                // no seguir mostrando datos viejos; si no se guardó nada, se conservan los campos.
                if (huboCambio)
                {
                    CargarDatos(txtBuscar.Text);
                    MostrarError(string.Format(Tr("err.adminusr.guardadoparcial",
                        "Se guardaron los datos del usuario, pero no se pudo cambiar el rol: {0}"),
                        MensajeDeError(ex)));
                }
                else MostrarError(ex);
            }
        }

        // Alta de un usuario nuevo con sus datos administrativos — el ÚNICO alta del sistema.
        // La BLL genera la contraseña, la exporta a un .txt, vincula el legajo de Empleado y
        // registra en bitácora + versión base del historial.
        private void Alta()
        {
            string rol = (cmbRol.SelectedItem as RolItem)?.Value;
            if (string.IsNullOrWhiteSpace(rol))
            { MostrarError(Tr("err.adminusr.rol_vacio", "Seleccioná un rol.")); return; }
            try
            {
                DateTime? fnac = dtpNacimiento.Checked ? (DateTime?)dtpNacimiento.Value.Date : null;
                string ruta = usuarioBLL.Alta(this.Text, txtUsername.Text, rol,
                    txtNombre.Text, txtApellido.Text, fnac, txtEmail.Text);
                string creado = txtUsername.Text.Trim();
                _modoAlta = false;
                btnGuardar.Text = Tr("btn.adminusr.guardar", "Guardar cambios");
                CargarDatos();
                MostrarOk(string.Format(
                    Tr("msg.adminusr.creado", "Usuario '{0}' creado. Credenciales en: {1}"), creado, ruta));
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void CambiarRol()
        {
            if (_idSeleccionado == 0) return;
            string rol = (cmbRol.SelectedItem as RolItem)?.Value;
            if (string.IsNullOrWhiteSpace(rol))
            { MostrarError(Tr("err.adminusr.rol_vacio", "Seleccioná un rol.")); return; }
            if (!ConfirmarSiNo(Tr("conf.adminusr.cambiarrol",
                    "¿Cambiar el rol de '{0}' a '{1}'?\n\nSus permisos cambian en el próximo ingreso.",
                    new object[] { txtUsername.Text.Trim(), cmbRol.SelectedItem?.ToString() }),
                    Tr("conf.adminusr.cambiarrol.titulo", "Cambiar rol"), porDefectoNo: true))
                return;
            try
            {
                usuarioBLL.CambiarRol(this.Text, _idSeleccionado, rol);
                GUI.Menu.RefrescarSeguridadAbierta();   // re-aplica seguridad en vivo si tocó al usuario en sesión
                MostrarOk(Tr("msg.adminusr.rolcambiado", "Rol del usuario actualizado correctamente."));
                CargarDatos(txtBuscar.Text);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // Historial de Cambios (Memento) del usuario seleccionado.
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
