using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — Formulario de Gestión de Usuarios.
    ///
    /// Permite administrar los usuarios del sistema: ver la lista completa,
    /// crear nuevos usuarios y resetear contraseñas (solo Administrador).
    ///
    /// Se abre como formulario hijo MDI desde el Menú → Administrar → Usuarios.
    ///
    /// CUMPLE REQUISITOS T02:
    ///   Gestión de usuarios del sistema
    ///   Las contraseñas se hashean con PBKDF2 antes de guardarse
    ///   Se registra la actividad en la bitácora (via BLL)
    ///   Solo un Administrador puede resetear contraseñas ajenas
    /// </summary>
    /// <summary>
    /// Hereda de <see cref="FormBase"/>:
    ///   - MostrarOk() y MostrarError() → heredados, no se redeclaran
    ///   - MensajeLabel → sobreescrito para devolver el lblMensaje de este formulario
    /// </summary>
    public partial class Usuarios : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        // BLL de usuarios para operaciones de negocio
        private readonly BLL.Usuario usuarioBLL = new BLL.Usuario();

        // Idioma activo — sincronizado en Traducir() para usar en CargarUsuarios
        private Idioma _idioma = GestorIdioma.IdiomaActual;

        private bool   _viendoArchivados = false;

        /// <summary>
        /// Constructor: inicializa el formulario y construye la interfaz de gestión de usuarios.
        /// </summary>
        public Usuarios()
        {
            InitializeComponent();
        }

        // ── Observer de idioma ────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            // Refrescar grilla para que headers y valores de estado reflejen el nuevo idioma
            CargarUsuarios();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(lblTitulo,            t);
            Aplicar(lblUser,              t);
            Aplicar(lblPerfil,            t);
            Aplicar(btnAgregar,           t);
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
            RellenarComboPerfil();
            TraducirHeadersGrilla();
        }

        // Recarga cmbPerfil con etiquetas traducidas manteniendo los valores internos (DB keys).
        private void RellenarComboPerfil()
        {
            var items = new[]
            {
                // Jerarquía consolidada (2da entrega):
                //   Comercial:   GerenteComercial ⊃ Vendedor
                //   Inventario:  GerenteInventario ⊃ OperadorLogistico + Deposito
                //   Transversal: Auditor (solo lectura) · Administrador (todo)
                new PerfilItem("Administrador",       Tr("perfil.administrador",       "Administrador")),
                new PerfilItem("Auditor",             Tr("perfil.auditor",             "Auditor")),
                new PerfilItem("GerenteComercial",    Tr("perfil.gerentecomercial",    "Gerente Comercial")),
                new PerfilItem("Vendedor",            Tr("perfil.vendedor",            "Vendedor")),
                new PerfilItem("GerenteInventario",   Tr("perfil.gerenteinventario",   "Gerente de Inventario")),
                new PerfilItem("Deposito",          Tr("perfil.deposito",            "Depósito")),
                new PerfilItem("OperadorLogistico",   Tr("perfil.operadorlogistico",   "Operador Logístico")),
                // PN02 — Caja: separado de Vendedor a propósito (Vendedor es "operador" de la
                // venta, Caja cobra).
                new PerfilItem("Caja",                Tr("perfil.caja",                "Caja")),
                // PN03 — Administración y Contabilidad: roles nuevos, separados de Gerencia
                // (que reusa GerenteComercial) y de Administrador (superusuario técnico).
                new PerfilItem("AdministracionComercial", Tr("perfil.administracioncomercial", "Administración")),
                new PerfilItem("Contabilidad",             Tr("perfil.contabilidad",            "Contabilidad")),
            };

            int prevIdx = cmbPerfil.SelectedIndex < 0 ? 2 : cmbPerfil.SelectedIndex;
            cmbPerfil.DataSource    = null;
            cmbPerfil.DisplayMember = "Label";
            cmbPerfil.ValueMember   = "Value";
            cmbPerfil.DataSource    = items;
            cmbPerfil.SelectedIndex = prevIdx < items.Length ? prevIdx : 2;
        }

        private class PerfilItem
        {
            public string Value { get; }
            public string Label { get; }
            public PerfilItem(string value, string label) { Value = value; Label = label; }
            public override string ToString() => Label;
        }

        /// <summary>Traduce los HeaderText de la grilla de usuarios según el idioma activo.</summary>
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

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        // ── Eventos del Designer ──────────────────────────────────────────────

        private void Usuarios_Load(object sender, EventArgs e)
        {
            CargarUsuarios();
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

        // ── Carga ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Carga la lista de usuarios desde la BLL → DAL y la muestra en la grilla.
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
                        ? (u.FechaBaja.HasValue ? $"{lblArchivado} ({u.FechaBaja.Value:dd/MM/yyyy})" : lblArchivado)
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
                        fila.DefaultCellStyle.BackColor = _viendoArchivados ? Color.FromArgb(235, 235, 235) : Color.FromArgb(255, 220, 220);
                        fila.DefaultCellStyle.ForeColor = _viendoArchivados ? Color.DimGray : Color.DarkRed;
                    }
                }

                string fmt = _viendoArchivados
                    ? Tr("msg.usr.archivados", "{0} usuario(s) archivado(s).")
                    : Tr("msg.usr.cargados",   "{0} usuario(s) registrado(s).");
                lblMensaje.ForeColor = Color.DarkGreen;
                lblMensaje.Text      = string.Format(fmt, usuarios.Count);

                // En vista de archivados, las acciones sobre activos no aplican.
                btnAgregar.Enabled       = !_viendoArchivados;
                btnResetearClave.Enabled = false;
                btnDesbloquear.Enabled   = false;
                btnArchivar.Enabled      = false;
            }
            catch (Exception ex)
            {
                var te = Traductor.ObtenerTraducciones(_idioma);
                MostrarError(string.Format(te.ContainsKey("err.generico.cargar") ? te["err.generico.cargar"].Texto : "Error al cargar: {0}", ex.Message));
            }
        }

        // ── Eventos de botones ────────────────────────────────────────────────

        /// <summary>Crea un nuevo usuario. La contraseña se genera automáticamente en la BLL.</summary>
        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string perfil   = (cmbPerfil.SelectedItem as PerfilItem)?.Value ?? cmbPerfil.SelectedItem?.ToString() ?? "";

            try
            {
                string rutaArchivo = usuarioBLL.Alta(this.Text, username, perfil);

                txtUsername.Clear();
                cmbPerfil.SelectedIndex = 2;

                CargarUsuarios();
                var tCr = Traductor.ObtenerTraducciones(_idioma);
                string fmt = tCr.ContainsKey("msg.usr.creado.exportado")
                    ? tCr["msg.usr.creado.exportado"].Texto
                    : "Usuario '{0}' [{1}] creado.\nCredenciales en: {2}";
                MostrarOk(string.Format(fmt, username, perfil, rutaArchivo));
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
                var tRSel = Traductor.ObtenerTraducciones(_idioma);
                MostrarError(tRSel.ContainsKey("err.usr.selecciona") ? tRSel["err.usr.selecciona"].Texto : "Seleccioná un usuario de la lista.");
                return;
            }

            int    idUsuario = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["ID"].Value);
            string username  = dgvUsuarios.SelectedRows[0].Cells["Username"].Value?.ToString() ?? "";

            try
            {
                string rutaArchivo = usuarioBLL.ResetearClave(this.Text, idUsuario, username);
                var tR = Traductor.ObtenerTraducciones(_idioma);
                string fmt = tR.ContainsKey("msg.usr.clave.exportada")
                    ? tR["msg.usr.clave.exportada"].Texto
                    : "Contraseña de '{0}' regenerada.\nCredenciales en: {1}";
                MostrarOk(string.Format(fmt, username, rutaArchivo));
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

            var confirm = MessageBox.Show(
                string.Format(Tr("conf.desbloquear.body",  "¿Desbloquear la cuenta de '{0}'?"), username),
                Tr("conf.desbloquear.titulo", "Confirmar Desbloqueo"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

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

            var confirm = MessageBox.Show(
                string.Format(Tr("conf.usr.archivar.body",
                    "¿Archivar al usuario '{0}'?\n\nNo podrá iniciar sesión y saldrá de la lista, pero se conserva su historial.\nPodrá eliminarse definitivamente tras 1 año."), username),
                Tr("conf.usr.archivar.titulo", "Confirmar Archivado"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

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

            var confirm = MessageBox.Show(
                string.Format(Tr("conf.usr.purgar.body",
                    "Se eliminarán DEFINITIVAMENTE {0} usuario(s) archivado(s) hace más de 1 año.\nEsta acción no se puede deshacer.\n\n¿Continuar?"), elegibles),
                Tr("conf.usr.purgar.titulo", "Confirmar Purga"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

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

        // ── Helpers ───────────────────────────────────────────────────────────

    }
}
