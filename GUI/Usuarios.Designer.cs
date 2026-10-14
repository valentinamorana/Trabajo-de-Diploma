using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class Usuarios
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelHeader          = new Panel();
            this.lblTitulo            = new Label();
            this.lblMensaje           = new Label();
            this.tabs                 = new TabControl();
            this.tabCuentas           = new TabPage();
            this.tabDatos             = new TabPage();
            // Pestaña "Cuentas"
            this.panelCuentas         = new Panel();
            this.btnRefrescar         = new Button();
            this.separador1           = new Label();
            this.lblResetTitulo       = new Label();
            this.lblResetInfo         = new Label();
            this.btnResetearClave     = new Button();
            this.separador2           = new Label();
            this.lblDesbloquearTitulo = new Label();
            this.lblDesbloquearInfo   = new Label();
            this.btnDesbloquear       = new Button();
            this.btnArchivar          = new Button();
            this.btnVerArchivados     = new Button();
            this.btnPurgar            = new Button();
            this.dgvUsuarios          = new DataGridView();
            this.lblListaTitulo       = new Label();
            // Pestaña "Datos administrativos"
            this.panelBusqueda        = new Panel();
            this.lblBuscar            = new Label();
            this.txtBuscar            = new TextBox();
            this.btnBuscar            = new Button();
            this.btnVerTodos          = new Button();
            this.btnNuevo             = new Button();
            this.dgvDatos             = new DataGridView();
            this.panelEdicion         = new Panel();
            this.lblDatos             = new Label();
            this.lblNombre            = new Label();
            this.txtNombre            = new TextBox();
            this.lblApellido          = new Label();
            this.txtApellido          = new TextBox();
            this.lblUsername          = new Label();
            this.txtUsername          = new TextBox();
            this.lblEmail             = new Label();
            this.txtEmail             = new TextBox();
            this.lblNacimiento        = new Label();
            this.dtpNacimiento        = new DateTimePicker();
            this.lblRol               = new Label();
            this.cmbRol               = new ComboBox();
            this.btnGuardar           = new Button();
            this.btnCambiarRol        = new Button();
            this.btnHistorial         = new Button();
            this.panelHeader.SuspendLayout();
            this.tabs.SuspendLayout();
            this.tabCuentas.SuspendLayout();
            this.tabDatos.SuspendLayout();
            this.panelCuentas.SuspendLayout();
            this.panelBusqueda.SuspendLayout();
            this.panelEdicion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsuarios)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDatos)).BeginInit();
            this.SuspendLayout();

            // ── panelHeader / lblTitulo ────────────────────────────────────────
            this.panelHeader.BackColor = Tema.RosaOscuro;
            this.panelHeader.Controls.Add(this.lblTitulo);
            this.panelHeader.Dock     = DockStyle.Top;
            this.panelHeader.Location = new Point(0, 0);
            this.panelHeader.Name     = "panelHeader";
            this.panelHeader.Size     = new Size(920, 48);
            this.panelHeader.TabIndex = 0;

            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.BackColor = Color.Transparent;
            this.lblTitulo.Font      = new Font("Segoe UI", 13F, FontStyle.Bold);
            this.lblTitulo.ForeColor = Color.White;
            this.lblTitulo.Location  = new Point(16, 10);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Text      = "Administración de Usuarios";

            // ── lblMensaje — barra de resultado común a las dos pestañas ───────
            this.lblMensaje.BackColor = Tema.PanelClaro;
            this.lblMensaje.Dock      = DockStyle.Bottom;
            this.lblMensaje.Font      = new Font("Segoe UI", 8.5F);
            this.lblMensaje.ForeColor = Color.DarkGreen;
            this.lblMensaje.Name      = "lblMensaje";
            this.lblMensaje.Padding   = new Padding(12, 0, 12, 0);
            this.lblMensaje.Size      = new Size(920, 30);
            this.lblMensaje.TabIndex  = 2;
            this.lblMensaje.TextAlign = ContentAlignment.MiddleLeft;

            // ── tabs ───────────────────────────────────────────────────────────
            this.tabs.Controls.Add(this.tabCuentas);
            this.tabs.Controls.Add(this.tabDatos);
            this.tabs.Dock          = DockStyle.Fill;
            this.tabs.Location      = new Point(0, 48);
            this.tabs.Name          = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size          = new Size(920, 582);
            this.tabs.TabIndex      = 1;
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.Tabs_SelectedIndexChanged);

            // ══ Pestaña "Cuentas": reset de contraseña, desbloqueo, archivado y purga ══
            this.tabCuentas.BackColor = Color.White;
            this.tabCuentas.Controls.Add(this.dgvUsuarios);
            this.tabCuentas.Controls.Add(this.lblListaTitulo);
            this.tabCuentas.Controls.Add(this.panelCuentas);
            this.tabCuentas.Location = new Point(4, 24);
            this.tabCuentas.Name     = "tabCuentas";
            this.tabCuentas.Size     = new Size(912, 554);
            this.tabCuentas.TabIndex = 0;
            this.tabCuentas.Text     = "Cuentas";

            this.panelCuentas.AutoScroll = true;
            this.panelCuentas.BackColor  = Tema.PanelClaro;
            this.panelCuentas.Controls.Add(this.btnRefrescar);
            this.panelCuentas.Controls.Add(this.separador1);
            this.panelCuentas.Controls.Add(this.lblResetTitulo);
            this.panelCuentas.Controls.Add(this.lblResetInfo);
            this.panelCuentas.Controls.Add(this.btnResetearClave);
            this.panelCuentas.Controls.Add(this.separador2);
            this.panelCuentas.Controls.Add(this.lblDesbloquearTitulo);
            this.panelCuentas.Controls.Add(this.lblDesbloquearInfo);
            this.panelCuentas.Controls.Add(this.btnDesbloquear);
            this.panelCuentas.Controls.Add(this.btnArchivar);
            this.panelCuentas.Controls.Add(this.btnVerArchivados);
            this.panelCuentas.Controls.Add(this.btnPurgar);
            this.panelCuentas.Dock     = DockStyle.Right;
            this.panelCuentas.Location = new Point(672, 0);
            this.panelCuentas.Name     = "panelCuentas";
            this.panelCuentas.Padding  = new Padding(12);
            this.panelCuentas.Size     = new Size(240, 554);
            this.panelCuentas.TabIndex = 0;

            this.btnRefrescar.Location = new Point(12, 12);
            this.btnRefrescar.Name     = "btnRefrescar";
            this.btnRefrescar.Size     = new Size(210, 28);
            this.btnRefrescar.TabIndex = 0;
            this.btnRefrescar.Tag      = "btn.refrescar";
            this.btnRefrescar.Text     = "Refrescar Lista";
            this.btnRefrescar.Click   += new System.EventHandler(this.BtnRefrescar_Click);

            this.separador1.BackColor = Color.Silver;
            this.separador1.Location  = new Point(12, 50);
            this.separador1.Name      = "separador1";
            this.separador1.Size      = new Size(210, 1);
            this.separador1.TabIndex  = 1;

            this.lblResetTitulo.Font     = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblResetTitulo.Location = new Point(12, 60);
            this.lblResetTitulo.Name     = "lblResetTitulo";
            this.lblResetTitulo.Size     = new Size(210, 23);
            this.lblResetTitulo.TabIndex = 2;
            this.lblResetTitulo.Tag      = "lbl.resettitulo";
            this.lblResetTitulo.Text     = "Resetear Contraseña";

            this.lblResetInfo.Font      = new Font("Segoe UI", 8.5F);
            this.lblResetInfo.ForeColor = Color.DimGray;
            this.lblResetInfo.Location  = new Point(12, 83);
            this.lblResetInfo.Name      = "lblResetInfo";
            this.lblResetInfo.Size      = new Size(210, 36);
            this.lblResetInfo.TabIndex  = 3;
            this.lblResetInfo.Tag       = "lbl.resetinfo";
            this.lblResetInfo.Text      = "Seleccione un usuario\nen la lista y presione:";

            this.btnResetearClave.BackColor = Color.FromArgb(60, 110, 180);
            this.btnResetearClave.Enabled   = false;
            this.btnResetearClave.FlatAppearance.BorderSize = 0;
            this.btnResetearClave.FlatStyle = FlatStyle.Flat;
            this.btnResetearClave.ForeColor = Color.White;
            this.btnResetearClave.Location  = new Point(12, 124);
            this.btnResetearClave.Name      = "btnResetearClave";
            this.btnResetearClave.Size      = new Size(210, 34);
            this.btnResetearClave.TabIndex  = 4;
            this.btnResetearClave.Tag       = "btn.resetclave";
            this.btnResetearClave.Text      = "Resetear Contraseña";
            this.btnResetearClave.UseVisualStyleBackColor = false;
            this.btnResetearClave.Click    += new System.EventHandler(this.BtnResetearClave_Click);

            this.separador2.BackColor = Color.Silver;
            this.separador2.Location  = new Point(12, 166);
            this.separador2.Name      = "separador2";
            this.separador2.Size      = new Size(210, 1);
            this.separador2.TabIndex  = 5;

            this.lblDesbloquearTitulo.Font     = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblDesbloquearTitulo.Location = new Point(12, 176);
            this.lblDesbloquearTitulo.Name     = "lblDesbloquearTitulo";
            this.lblDesbloquearTitulo.Size     = new Size(210, 23);
            this.lblDesbloquearTitulo.TabIndex = 6;
            this.lblDesbloquearTitulo.Tag      = "lbl.desbloqtitulo";
            this.lblDesbloquearTitulo.Text     = "Desbloquear Cuenta";

            this.lblDesbloquearInfo.Font      = new Font("Segoe UI", 8.5F);
            this.lblDesbloquearInfo.ForeColor = Color.DimGray;
            this.lblDesbloquearInfo.Location  = new Point(12, 199);
            this.lblDesbloquearInfo.Name      = "lblDesbloquearInfo";
            this.lblDesbloquearInfo.Size      = new Size(210, 36);
            this.lblDesbloquearInfo.TabIndex  = 7;
            this.lblDesbloquearInfo.Tag       = "lbl.desbloqinfo";
            this.lblDesbloquearInfo.Text      = "Seleccione un usuario\nbloqueado y presione:";

            this.btnDesbloquear.BackColor = Color.FromArgb(178, 58, 58);
            this.btnDesbloquear.Enabled   = false;
            this.btnDesbloquear.FlatAppearance.BorderSize = 0;
            this.btnDesbloquear.FlatStyle = FlatStyle.Flat;
            this.btnDesbloquear.ForeColor = Color.White;
            this.btnDesbloquear.Location  = new Point(12, 240);
            this.btnDesbloquear.Name      = "btnDesbloquear";
            this.btnDesbloquear.Size      = new Size(210, 34);
            this.btnDesbloquear.TabIndex  = 8;
            this.btnDesbloquear.Tag       = "btn.desbloquear";
            this.btnDesbloquear.Text      = "Desbloquear Cuenta";
            this.btnDesbloquear.UseVisualStyleBackColor = false;
            this.btnDesbloquear.Click    += new System.EventHandler(this.BtnDesbloquear_Click);

            // btnArchivar — RF-10, archivar (baja lógica) el usuario seleccionado.
            this.btnArchivar.BackColor = Color.FromArgb(170, 50, 50);
            this.btnArchivar.Cursor    = Cursors.Hand;
            this.btnArchivar.Enabled   = false;
            this.btnArchivar.FlatAppearance.BorderSize = 0;
            this.btnArchivar.FlatStyle = FlatStyle.Flat;
            this.btnArchivar.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.btnArchivar.ForeColor = Color.White;
            this.btnArchivar.Location  = new Point(12, 294);
            this.btnArchivar.Name      = "btnArchivar";
            this.btnArchivar.Size      = new Size(210, 30);
            this.btnArchivar.TabIndex  = 9;
            this.btnArchivar.Tag       = "btn.usr.eliminar";
            this.btnArchivar.Text      = "Archivar usuario";
            this.btnArchivar.UseVisualStyleBackColor = false;
            this.btnArchivar.Click    += new System.EventHandler(this.BtnArchivar_Click);

            this.btnVerArchivados.BackColor = Color.FromArgb(70, 70, 80);
            this.btnVerArchivados.Cursor    = Cursors.Hand;
            this.btnVerArchivados.FlatAppearance.BorderSize = 0;
            this.btnVerArchivados.FlatStyle = FlatStyle.Flat;
            this.btnVerArchivados.Font      = new Font("Segoe UI", 8.5F);
            this.btnVerArchivados.ForeColor = Color.White;
            this.btnVerArchivados.Location  = new Point(12, 332);
            this.btnVerArchivados.Name      = "btnVerArchivados";
            this.btnVerArchivados.Size      = new Size(102, 28);
            this.btnVerArchivados.TabIndex  = 10;
            this.btnVerArchivados.Tag       = "btn.usr.verarchivados";
            this.btnVerArchivados.Text      = "Ver archivados";
            this.btnVerArchivados.UseVisualStyleBackColor = false;
            this.btnVerArchivados.Click    += new System.EventHandler(this.BtnVerArchivados_Click);

            this.btnPurgar.BackColor = Color.FromArgb(60, 60, 70);
            this.btnPurgar.Cursor    = Cursors.Hand;
            this.btnPurgar.FlatAppearance.BorderSize = 0;
            this.btnPurgar.FlatStyle = FlatStyle.Flat;
            this.btnPurgar.Font      = new Font("Segoe UI", 8.5F);
            this.btnPurgar.ForeColor = Color.White;
            this.btnPurgar.Location  = new Point(120, 332);
            this.btnPurgar.Name      = "btnPurgar";
            this.btnPurgar.Size      = new Size(102, 28);
            this.btnPurgar.TabIndex  = 11;
            this.btnPurgar.Tag       = "btn.usr.purgar";
            this.btnPurgar.Text      = "Purgar (>1 año)";
            this.btnPurgar.UseVisualStyleBackColor = false;
            this.btnPurgar.Click    += new System.EventHandler(this.BtnPurgar_Click);

            this.dgvUsuarios.AllowUserToAddRows    = false;
            this.dgvUsuarios.AllowUserToDeleteRows = false;
            this.dgvUsuarios.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvUsuarios.BackgroundColor       = Color.White;
            this.dgvUsuarios.BorderStyle           = BorderStyle.None;
            this.dgvUsuarios.Dock                  = DockStyle.Fill;
            this.dgvUsuarios.Location              = new Point(0, 28);
            this.dgvUsuarios.MultiSelect           = false;
            this.dgvUsuarios.Name                  = "dgvUsuarios";
            this.dgvUsuarios.ReadOnly              = true;
            this.dgvUsuarios.RowHeadersVisible     = false;
            this.dgvUsuarios.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            this.dgvUsuarios.Size                  = new Size(672, 526);
            this.dgvUsuarios.TabIndex              = 1;
            this.dgvUsuarios.SelectionChanged     += new System.EventHandler(this.DgvUsuarios_SelectionChanged);

            this.lblListaTitulo.BackColor = Color.FromArgb(230, 230, 240);
            this.lblListaTitulo.Dock      = DockStyle.Top;
            this.lblListaTitulo.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblListaTitulo.Location  = new Point(0, 0);
            this.lblListaTitulo.Name      = "lblListaTitulo";
            this.lblListaTitulo.Padding   = new Padding(6, 6, 0, 0);
            this.lblListaTitulo.Size      = new Size(672, 28);
            this.lblListaTitulo.TabIndex  = 2;
            this.lblListaTitulo.Tag       = "lbl.listatitulo";
            this.lblListaTitulo.Text      = "Usuarios registrados en el sistema";

            // ══ Pestaña "Datos administrativos": alta, datos no sensibles, rol e historial ══
            this.tabDatos.BackColor = Color.White;
            this.tabDatos.Controls.Add(this.dgvDatos);
            this.tabDatos.Controls.Add(this.panelEdicion);
            this.tabDatos.Controls.Add(this.panelBusqueda);
            this.tabDatos.Location = new Point(4, 24);
            this.tabDatos.Name     = "tabDatos";
            this.tabDatos.Padding  = new Padding(10);
            this.tabDatos.Size     = new Size(912, 554);
            this.tabDatos.TabIndex = 1;
            this.tabDatos.Text     = "Datos administrativos";

            this.panelBusqueda.Controls.Add(this.lblBuscar);
            this.panelBusqueda.Controls.Add(this.txtBuscar);
            this.panelBusqueda.Controls.Add(this.btnBuscar);
            this.panelBusqueda.Controls.Add(this.btnVerTodos);
            this.panelBusqueda.Controls.Add(this.btnNuevo);
            this.panelBusqueda.Dock     = DockStyle.Top;
            this.panelBusqueda.Location = new Point(10, 10);
            this.panelBusqueda.Name     = "panelBusqueda";
            this.panelBusqueda.Size     = new Size(892, 60);
            this.panelBusqueda.TabIndex = 0;

            this.lblBuscar.AutoSize  = true;
            this.lblBuscar.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblBuscar.ForeColor = Tema.RosaOscuro;
            this.lblBuscar.Location  = new Point(0, 2);
            this.lblBuscar.Name      = "lblBuscar";
            this.lblBuscar.TabIndex  = 0;
            this.lblBuscar.Text      = "Buscar (nombre, apellido o email):";

            this.txtBuscar.Location = new Point(0, 24);
            this.txtBuscar.Name     = "txtBuscar";
            this.txtBuscar.Size     = new Size(260, 24);
            this.txtBuscar.TabIndex = 1;
            this.txtBuscar.KeyDown += new KeyEventHandler(this.TxtBuscar_KeyDown);

            this.btnBuscar.BackColor = Tema.RosaPrimario;
            this.btnBuscar.Cursor    = Cursors.Hand;
            this.btnBuscar.FlatAppearance.BorderSize = 0;
            this.btnBuscar.FlatStyle = FlatStyle.Flat;
            this.btnBuscar.Font      = new Font("Segoe UI", 8.5F);
            this.btnBuscar.ForeColor = Color.White;
            this.btnBuscar.Location  = new Point(268, 23);
            this.btnBuscar.Name      = "btnBuscar";
            this.btnBuscar.Size      = new Size(90, 30);
            this.btnBuscar.TabIndex  = 2;
            this.btnBuscar.Text      = "Buscar";
            this.btnBuscar.UseVisualStyleBackColor = false;
            this.btnBuscar.Click    += new System.EventHandler(this.BtnBuscar_Click);

            this.btnVerTodos.BackColor = Color.White;
            this.btnVerTodos.Cursor    = Cursors.Hand;
            this.btnVerTodos.FlatAppearance.BorderColor = Tema.RosaOscuro;
            this.btnVerTodos.FlatAppearance.BorderSize  = 1;
            this.btnVerTodos.FlatStyle = FlatStyle.Flat;
            this.btnVerTodos.Font      = new Font("Segoe UI", 8.5F);
            this.btnVerTodos.ForeColor = Tema.RosaOscuro;
            this.btnVerTodos.Location  = new Point(364, 23);
            this.btnVerTodos.Name      = "btnVerTodos";
            this.btnVerTodos.Size      = new Size(96, 30);
            this.btnVerTodos.TabIndex  = 3;
            this.btnVerTodos.Text      = "Ver todos";
            this.btnVerTodos.UseVisualStyleBackColor = false;
            this.btnVerTodos.Click    += new System.EventHandler(this.BtnVerTodos_Click);

            // btnNuevo — ÚNICO punto de alta de usuarios del sistema.
            this.btnNuevo.BackColor = Tema.RosaPrimario;
            this.btnNuevo.Cursor    = Cursors.Hand;
            this.btnNuevo.FlatAppearance.BorderSize = 0;
            this.btnNuevo.FlatStyle = FlatStyle.Flat;
            this.btnNuevo.Font      = new Font("Segoe UI", 8.5F);
            this.btnNuevo.ForeColor = Color.White;
            this.btnNuevo.Location  = new Point(466, 23);
            this.btnNuevo.Name      = "btnNuevo";
            this.btnNuevo.Size      = new Size(110, 30);
            this.btnNuevo.TabIndex  = 4;
            this.btnNuevo.Text      = "Nuevo usuario";
            this.btnNuevo.UseVisualStyleBackColor = false;
            this.btnNuevo.Click    += new System.EventHandler(this.BtnNuevo_Click);

            this.dgvDatos.AllowUserToAddRows    = false;
            this.dgvDatos.AllowUserToDeleteRows = false;
            this.dgvDatos.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDatos.BackgroundColor       = Color.White;
            this.dgvDatos.BorderStyle           = BorderStyle.FixedSingle;
            this.dgvDatos.Dock                  = DockStyle.Fill;
            this.dgvDatos.Location              = new Point(10, 70);
            this.dgvDatos.MultiSelect           = false;
            this.dgvDatos.Name                  = "dgvDatos";
            this.dgvDatos.ReadOnly              = true;
            this.dgvDatos.RowHeadersVisible     = false;
            this.dgvDatos.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            this.dgvDatos.Size                  = new Size(586, 474);
            this.dgvDatos.TabIndex              = 1;
            this.dgvDatos.SelectionChanged     += new System.EventHandler(this.DgvDatos_SelectionChanged);

            this.panelEdicion.AutoScroll  = true;
            this.panelEdicion.BackColor   = Tema.PanelClaro;
            this.panelEdicion.BorderStyle = BorderStyle.FixedSingle;
            this.panelEdicion.Controls.Add(this.lblDatos);
            this.panelEdicion.Controls.Add(this.lblNombre);
            this.panelEdicion.Controls.Add(this.txtNombre);
            this.panelEdicion.Controls.Add(this.lblApellido);
            this.panelEdicion.Controls.Add(this.txtApellido);
            this.panelEdicion.Controls.Add(this.lblUsername);
            this.panelEdicion.Controls.Add(this.txtUsername);
            this.panelEdicion.Controls.Add(this.lblEmail);
            this.panelEdicion.Controls.Add(this.txtEmail);
            this.panelEdicion.Controls.Add(this.lblNacimiento);
            this.panelEdicion.Controls.Add(this.dtpNacimiento);
            this.panelEdicion.Controls.Add(this.lblRol);
            this.panelEdicion.Controls.Add(this.cmbRol);
            this.panelEdicion.Controls.Add(this.btnGuardar);
            this.panelEdicion.Controls.Add(this.btnCambiarRol);
            this.panelEdicion.Controls.Add(this.btnHistorial);
            this.panelEdicion.Dock     = DockStyle.Right;
            this.panelEdicion.Location = new Point(596, 70);
            this.panelEdicion.Name     = "panelEdicion";
            this.panelEdicion.Padding  = new Padding(10);
            this.panelEdicion.Size     = new Size(306, 474);
            this.panelEdicion.TabIndex = 2;

            this.lblDatos.AutoSize  = true;
            this.lblDatos.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblDatos.ForeColor = Tema.RosaOscuro;
            this.lblDatos.Location  = new Point(16, 8);
            this.lblDatos.Name      = "lblDatos";
            this.lblDatos.TabIndex  = 0;
            this.lblDatos.Text      = "Datos del usuario seleccionado";

            this.lblNombre.AutoSize  = true;
            this.lblNombre.Font      = new Font("Segoe UI", 8.5F);
            this.lblNombre.ForeColor = Color.DimGray;
            this.lblNombre.Location  = new Point(16, 38);
            this.lblNombre.Name      = "lblNombre";
            this.lblNombre.TabIndex  = 1;
            this.lblNombre.Text      = "Nombre:";

            this.txtNombre.Location  = new Point(16, 56);
            this.txtNombre.MaxLength = 200;
            this.txtNombre.Name      = "txtNombre";
            this.txtNombre.Size      = new Size(260, 24);
            this.txtNombre.TabIndex  = 2;

            this.lblApellido.AutoSize  = true;
            this.lblApellido.Font      = new Font("Segoe UI", 8.5F);
            this.lblApellido.ForeColor = Color.DimGray;
            this.lblApellido.Location  = new Point(16, 86);
            this.lblApellido.Name      = "lblApellido";
            this.lblApellido.TabIndex  = 3;
            this.lblApellido.Text      = "Apellido:";

            this.txtApellido.Location  = new Point(16, 104);
            this.txtApellido.MaxLength = 200;
            this.txtApellido.Name      = "txtApellido";
            this.txtApellido.Size      = new Size(260, 24);
            this.txtApellido.TabIndex  = 4;

            this.lblUsername.AutoSize  = true;
            this.lblUsername.Font      = new Font("Segoe UI", 8.5F);
            this.lblUsername.ForeColor = Color.DimGray;
            this.lblUsername.Location  = new Point(16, 134);
            this.lblUsername.Name      = "lblUsername";
            this.lblUsername.TabIndex  = 5;
            this.lblUsername.Text      = "Nombre de usuario:";

            this.txtUsername.Location  = new Point(16, 152);
            this.txtUsername.MaxLength = 200;
            this.txtUsername.Name      = "txtUsername";
            this.txtUsername.Size      = new Size(260, 24);
            this.txtUsername.TabIndex  = 6;

            this.lblEmail.AutoSize  = true;
            this.lblEmail.Font      = new Font("Segoe UI", 8.5F);
            this.lblEmail.ForeColor = Color.DimGray;
            this.lblEmail.Location  = new Point(16, 182);
            this.lblEmail.Name      = "lblEmail";
            this.lblEmail.TabIndex  = 7;
            this.lblEmail.Text      = "Email:";

            this.txtEmail.Location  = new Point(16, 200);
            this.txtEmail.MaxLength = 200;
            this.txtEmail.Name      = "txtEmail";
            this.txtEmail.Size      = new Size(260, 24);
            this.txtEmail.TabIndex  = 8;

            this.lblNacimiento.AutoSize  = true;
            this.lblNacimiento.Font      = new Font("Segoe UI", 8.5F);
            this.lblNacimiento.ForeColor = Color.DimGray;
            this.lblNacimiento.Location  = new Point(16, 230);
            this.lblNacimiento.Name      = "lblNacimiento";
            this.lblNacimiento.TabIndex  = 9;
            this.lblNacimiento.Text      = "Fecha de nacimiento:";

            this.dtpNacimiento.Checked      = false;
            this.dtpNacimiento.Format       = DateTimePickerFormat.Short;
            this.dtpNacimiento.Location     = new Point(16, 248);
            this.dtpNacimiento.Name         = "dtpNacimiento";
            this.dtpNacimiento.ShowCheckBox = true;
            this.dtpNacimiento.Size         = new Size(260, 24);
            this.dtpNacimiento.TabIndex     = 10;

            this.lblRol.AutoSize  = true;
            this.lblRol.Font      = new Font("Segoe UI", 8.5F);
            this.lblRol.ForeColor = Color.DimGray;
            this.lblRol.Location  = new Point(16, 280);
            this.lblRol.Name      = "lblRol";
            this.lblRol.TabIndex  = 11;
            this.lblRol.Text      = "Rol:";

            this.cmbRol.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRol.Location      = new Point(16, 298);
            this.cmbRol.Name          = "cmbRol";
            this.cmbRol.Size          = new Size(260, 24);
            this.cmbRol.TabIndex      = 12;

            this.btnGuardar.BackColor = Tema.RosaPrimario;
            this.btnGuardar.Cursor    = Cursors.Hand;
            this.btnGuardar.FlatAppearance.BorderSize = 0;
            this.btnGuardar.FlatStyle = FlatStyle.Flat;
            this.btnGuardar.Font      = new Font("Segoe UI", 8.5F);
            this.btnGuardar.ForeColor = Color.White;
            this.btnGuardar.Location  = new Point(16, 336);
            this.btnGuardar.Name      = "btnGuardar";
            this.btnGuardar.Size      = new Size(260, 30);
            this.btnGuardar.TabIndex  = 13;
            this.btnGuardar.Text      = "Guardar cambios";
            this.btnGuardar.UseVisualStyleBackColor = false;
            this.btnGuardar.Click    += new System.EventHandler(this.BtnGuardar_Click);

            this.btnCambiarRol.BackColor = Color.White;
            this.btnCambiarRol.Cursor    = Cursors.Hand;
            this.btnCambiarRol.FlatAppearance.BorderColor = Tema.RosaOscuro;
            this.btnCambiarRol.FlatAppearance.BorderSize  = 1;
            this.btnCambiarRol.FlatStyle = FlatStyle.Flat;
            this.btnCambiarRol.Font      = new Font("Segoe UI", 8.5F);
            this.btnCambiarRol.ForeColor = Tema.RosaOscuro;
            this.btnCambiarRol.Location  = new Point(16, 374);
            this.btnCambiarRol.Name      = "btnCambiarRol";
            this.btnCambiarRol.Size      = new Size(260, 30);
            this.btnCambiarRol.TabIndex  = 14;
            this.btnCambiarRol.Text      = "Cambiar rol";
            this.btnCambiarRol.UseVisualStyleBackColor = false;
            this.btnCambiarRol.Click    += new System.EventHandler(this.BtnCambiarRol_Click);

            // btnHistorial — Historial de Cambios (Memento) del usuario seleccionado.
            this.btnHistorial.BackColor = Color.White;
            this.btnHistorial.Cursor    = Cursors.Hand;
            this.btnHistorial.FlatAppearance.BorderColor = Tema.RosaOscuro;
            this.btnHistorial.FlatAppearance.BorderSize  = 1;
            this.btnHistorial.FlatStyle = FlatStyle.Flat;
            this.btnHistorial.Font      = new Font("Segoe UI", 8.5F);
            this.btnHistorial.ForeColor = Tema.RosaOscuro;
            this.btnHistorial.Location  = new Point(16, 412);
            this.btnHistorial.Name      = "btnHistorial";
            this.btnHistorial.Size      = new Size(260, 30);
            this.btnHistorial.TabIndex  = 15;
            this.btnHistorial.Text      = "Ver historial de cambios";
            this.btnHistorial.UseVisualStyleBackColor = false;
            this.btnHistorial.Click    += new System.EventHandler(this.BtnHistorial_Click);

            // ── Usuarios ─────────────────────────────────────────────────────────
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.BackColor           = Color.White;
            this.ClientSize          = new Size(920, 660);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.panelHeader);
            this.Font                = new Font("Segoe UI", 8.25F);   // escala tipográfica de Tema
            this.MinimumSize         = new Size(840, 600);
            this.Name                = "Usuarios";
            this.Tag                 = "frm.adminusuarios";
            this.Text                = "Administración de Usuarios";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelCuentas.ResumeLayout(false);
            this.panelBusqueda.ResumeLayout(false);
            this.panelBusqueda.PerformLayout();
            this.panelEdicion.ResumeLayout(false);
            this.panelEdicion.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsuarios)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDatos)).EndInit();
            this.tabCuentas.ResumeLayout(false);
            this.tabDatos.ResumeLayout(false);
            this.tabs.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private Panel        panelHeader;
        private Label        lblTitulo;
        private Label        lblMensaje;
        private TabControl   tabs;
        private TabPage      tabCuentas;
        private TabPage      tabDatos;
        // Pestaña "Cuentas"
        private Panel        panelCuentas;
        private Button       btnRefrescar;
        private Label        separador1;
        private Label        lblResetTitulo;
        private Label        lblResetInfo;
        private Button       btnResetearClave;
        private Label        separador2;
        private Label        lblDesbloquearTitulo;
        private Label        lblDesbloquearInfo;
        private Button       btnDesbloquear;
        private Button       btnArchivar;
        private Button       btnVerArchivados;
        private Button       btnPurgar;
        private DataGridView dgvUsuarios;
        private Label        lblListaTitulo;
        // Pestaña "Datos administrativos"
        private Panel        panelBusqueda;
        private Label        lblBuscar;
        private TextBox      txtBuscar;
        private Button       btnBuscar;
        private Button       btnVerTodos;
        private Button       btnNuevo;
        private DataGridView dgvDatos;
        private Panel        panelEdicion;
        private Label        lblDatos;
        private Label        lblNombre;
        private TextBox      txtNombre;
        private Label        lblApellido;
        private TextBox      txtApellido;
        private Label        lblUsername;
        private TextBox      txtUsername;
        private Label        lblEmail;
        private TextBox      txtEmail;
        private Label        lblNacimiento;
        private DateTimePicker dtpNacimiento;
        private Label        lblRol;
        private ComboBox     cmbRol;
        private Button       btnGuardar;
        private Button       btnCambiarRol;
        private Button       btnHistorial;
    }
}
