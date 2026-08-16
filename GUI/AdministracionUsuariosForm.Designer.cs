using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class AdministracionUsuariosForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            this.panelHeader    = new Panel();
            this.lblTitulo      = new Label();
            this.lblBuscar      = new Label();
            this.txtBuscar      = new TextBox();
            this.btnBuscar      = new Button();
            this.btnRefrescar   = new Button();
            this.btnNuevo       = new Button();
            this.dgv            = new DataGridView();
            this.panelEdicion   = new Panel();
            this.lblDatos       = new Label();
            this.lblNombre      = new Label();
            this.txtNombre      = new TextBox();
            this.lblApellido    = new Label();
            this.txtApellido    = new TextBox();
            this.lblUsername    = new Label();
            this.txtUsername    = new TextBox();
            this.lblEmail       = new Label();
            this.txtEmail       = new TextBox();
            this.lblNacimiento  = new Label();
            this.dtpNacimiento  = new DateTimePicker();
            this.lblRol         = new Label();
            this.cmbRol         = new ComboBox();
            this.btnGuardar     = new Button();
            this.btnCambiarRol  = new Button();
            this.btnHistorial   = new Button();
            this.lblMensaje     = new Label();
            this.btnCerrar      = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.panelHeader.SuspendLayout();
            this.panelEdicion.SuspendLayout();
            this.SuspendLayout();

            // ── panelHeader / lblTitulo ────────────────────────────────────────
            this.panelHeader.BackColor = RosaOscuro;
            this.panelHeader.Controls.Add(this.lblTitulo);
            this.panelHeader.Dock     = DockStyle.Top;
            this.panelHeader.Location = new Point(0, 0);
            this.panelHeader.Name     = "panelHeader";
            this.panelHeader.Size     = new Size(900, 48);
            this.panelHeader.TabIndex = 0;

            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.BackColor = Color.Transparent;
            this.lblTitulo.Font      = new Font("Segoe UI", 13F, FontStyle.Bold);
            this.lblTitulo.ForeColor = Color.White;
            this.lblTitulo.Location  = new Point(16, 10);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Text      = "Administración de Usuarios";

            // ── búsqueda ───────────────────────────────────────────────────────
            this.lblBuscar.AutoSize  = true;
            this.lblBuscar.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblBuscar.ForeColor = RosaOscuro;
            this.lblBuscar.Location  = new Point(16, 60);
            this.lblBuscar.Name      = "lblBuscar";
            this.lblBuscar.TabIndex  = 1;
            this.lblBuscar.Text      = "Buscar (nombre, apellido o email):";

            this.txtBuscar.Location = new Point(16, 82);
            this.txtBuscar.Name     = "txtBuscar";
            this.txtBuscar.Size     = new Size(260, 24);
            this.txtBuscar.TabIndex = 2;
            this.txtBuscar.KeyDown += new KeyEventHandler(this.TxtBuscar_KeyDown);

            this.btnBuscar.BackColor = RosaPrimario;
            this.btnBuscar.Cursor    = Cursors.Hand;
            this.btnBuscar.FlatAppearance.BorderSize = 0;
            this.btnBuscar.FlatStyle = FlatStyle.Flat;
            this.btnBuscar.Font      = new Font("Segoe UI", 8.5F);
            this.btnBuscar.ForeColor = Color.White;
            this.btnBuscar.Location  = new Point(284, 81);
            this.btnBuscar.Name      = "btnBuscar";
            this.btnBuscar.Size      = new Size(90, 30);
            this.btnBuscar.TabIndex  = 3;
            this.btnBuscar.Text      = "🔍 Buscar";
            this.btnBuscar.UseVisualStyleBackColor = false;
            this.btnBuscar.Click += new System.EventHandler(this.BtnBuscar_Click);

            this.btnRefrescar.BackColor = Color.White;
            this.btnRefrescar.Cursor    = Cursors.Hand;
            this.btnRefrescar.FlatAppearance.BorderColor = RosaOscuro;
            this.btnRefrescar.FlatAppearance.BorderSize  = 1;
            this.btnRefrescar.FlatStyle = FlatStyle.Flat;
            this.btnRefrescar.Font      = new Font("Segoe UI", 8.5F);
            this.btnRefrescar.ForeColor = RosaOscuro;
            this.btnRefrescar.Location  = new Point(380, 81);
            this.btnRefrescar.Name      = "btnRefrescar";
            this.btnRefrescar.Size      = new Size(96, 30);
            this.btnRefrescar.TabIndex  = 4;
            this.btnRefrescar.Text      = "↻ Ver todos";
            this.btnRefrescar.UseVisualStyleBackColor = false;
            this.btnRefrescar.Click += new System.EventHandler(this.BtnRefrescar_Click);

            this.btnNuevo.BackColor = RosaPrimario;
            this.btnNuevo.Cursor    = Cursors.Hand;
            this.btnNuevo.FlatAppearance.BorderSize = 0;
            this.btnNuevo.FlatStyle = FlatStyle.Flat;
            this.btnNuevo.Font      = new Font("Segoe UI", 8.5F);
            this.btnNuevo.ForeColor = Color.White;
            this.btnNuevo.Location  = new Point(482, 81);
            this.btnNuevo.Name      = "btnNuevo";
            this.btnNuevo.Size      = new Size(100, 30);
            this.btnNuevo.TabIndex  = 5;
            this.btnNuevo.Text      = "➕ Nuevo usuario";
            this.btnNuevo.UseVisualStyleBackColor = false;
            this.btnNuevo.Click += new System.EventHandler(this.BtnNuevo_Click);

            // ── grilla ─────────────────────────────────────────────────────────
            this.dgv.AllowUserToAddRows    = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv.BackgroundColor = Color.White;
            this.dgv.BorderStyle     = BorderStyle.FixedSingle;
            this.dgv.Location        = new Point(16, 116);
            this.dgv.MultiSelect     = false;
            this.dgv.Name            = "dgvUsuarios";
            this.dgv.ReadOnly        = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.SelectionMode   = DataGridViewSelectionMode.FullRowSelect;
            this.dgv.Size            = new Size(560, 410);
            this.dgv.TabIndex        = 6;
            this.dgv.SelectionChanged += new System.EventHandler(this.DgvSelectionChanged);

            // ── panel de edición ──────────────────────────────────────────────
            this.panelEdicion.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            this.panelEdicion.BackColor = PanelClaro;
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
            this.panelEdicion.Controls.Add(this.lblMensaje);
            this.panelEdicion.Location = new Point(588, 116);
            this.panelEdicion.Name     = "panelEdicion";
            this.panelEdicion.Padding  = new Padding(10);
            this.panelEdicion.Size     = new Size(286, 410);
            this.panelEdicion.TabIndex = 7;

            this.lblDatos.AutoSize  = true;
            this.lblDatos.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblDatos.ForeColor = RosaOscuro;
            this.lblDatos.Location  = new Point(10, 8);
            this.lblDatos.Name      = "lblDatos";
            this.lblDatos.TabIndex  = 0;
            this.lblDatos.Text      = "Datos del usuario seleccionado";

            this.lblNombre.AutoSize  = true;
            this.lblNombre.Font      = new Font("Segoe UI", 8.5F);
            this.lblNombre.ForeColor = Color.DimGray;
            this.lblNombre.Location  = new Point(10, 38);
            this.lblNombre.Name      = "lblNombre";
            this.lblNombre.TabIndex  = 1;
            this.lblNombre.Text      = "Nombre:";

            this.txtNombre.Location  = new Point(10, 56);
            this.txtNombre.MaxLength = 200;
            this.txtNombre.Name      = "txtNombre";
            this.txtNombre.Size      = new Size(260, 24);
            this.txtNombre.TabIndex  = 2;

            this.lblApellido.AutoSize  = true;
            this.lblApellido.Font      = new Font("Segoe UI", 8.5F);
            this.lblApellido.ForeColor = Color.DimGray;
            this.lblApellido.Location  = new Point(10, 86);
            this.lblApellido.Name      = "lblApellido";
            this.lblApellido.TabIndex  = 3;
            this.lblApellido.Text      = "Apellido:";

            this.txtApellido.Location  = new Point(10, 104);
            this.txtApellido.MaxLength = 200;
            this.txtApellido.Name      = "txtApellido";
            this.txtApellido.Size      = new Size(260, 24);
            this.txtApellido.TabIndex  = 4;

            this.lblUsername.AutoSize  = true;
            this.lblUsername.Font      = new Font("Segoe UI", 8.5F);
            this.lblUsername.ForeColor = Color.DimGray;
            this.lblUsername.Location  = new Point(10, 134);
            this.lblUsername.Name      = "lblUsername";
            this.lblUsername.TabIndex  = 5;
            this.lblUsername.Text      = "Nombre de usuario:";

            this.txtUsername.Location  = new Point(10, 152);
            this.txtUsername.MaxLength = 200;
            this.txtUsername.Name      = "txtUsername";
            this.txtUsername.Size      = new Size(260, 24);
            this.txtUsername.TabIndex  = 6;

            this.lblEmail.AutoSize  = true;
            this.lblEmail.Font      = new Font("Segoe UI", 8.5F);
            this.lblEmail.ForeColor = Color.DimGray;
            this.lblEmail.Location  = new Point(10, 182);
            this.lblEmail.Name      = "lblEmail";
            this.lblEmail.TabIndex  = 7;
            this.lblEmail.Text      = "Email:";

            this.txtEmail.Location  = new Point(10, 200);
            this.txtEmail.MaxLength = 200;
            this.txtEmail.Name      = "txtEmail";
            this.txtEmail.Size      = new Size(260, 24);
            this.txtEmail.TabIndex  = 8;

            this.lblNacimiento.AutoSize  = true;
            this.lblNacimiento.Font      = new Font("Segoe UI", 8.5F);
            this.lblNacimiento.ForeColor = Color.DimGray;
            this.lblNacimiento.Location  = new Point(10, 230);
            this.lblNacimiento.Name      = "lblNacimiento";
            this.lblNacimiento.TabIndex  = 9;
            this.lblNacimiento.Text      = "Fecha de nacimiento:";

            this.dtpNacimiento.Checked  = false;
            this.dtpNacimiento.Format   = DateTimePickerFormat.Short;
            this.dtpNacimiento.Location = new Point(10, 248);
            this.dtpNacimiento.Name     = "dtpNacimiento";
            this.dtpNacimiento.ShowCheckBox = true;
            this.dtpNacimiento.Size     = new Size(260, 24);
            this.dtpNacimiento.TabIndex = 10;

            this.lblRol.AutoSize  = true;
            this.lblRol.Font      = new Font("Segoe UI", 8.5F);
            this.lblRol.ForeColor = Color.DimGray;
            this.lblRol.Location  = new Point(10, 280);
            this.lblRol.Name      = "lblRol";
            this.lblRol.TabIndex  = 11;
            this.lblRol.Text      = "Rol:";

            this.cmbRol.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRol.Location      = new Point(10, 298);
            this.cmbRol.Name          = "cmbRol";
            this.cmbRol.Size          = new Size(260, 24);
            this.cmbRol.TabIndex      = 12;

            this.btnGuardar.BackColor = RosaPrimario;
            this.btnGuardar.Cursor    = Cursors.Hand;
            this.btnGuardar.FlatAppearance.BorderSize = 0;
            this.btnGuardar.FlatStyle = FlatStyle.Flat;
            this.btnGuardar.Font      = new Font("Segoe UI", 8.5F);
            this.btnGuardar.ForeColor = Color.White;
            this.btnGuardar.Location  = new Point(10, 336);
            this.btnGuardar.Name      = "btnGuardar";
            this.btnGuardar.Size      = new Size(260, 30);
            this.btnGuardar.TabIndex  = 13;
            this.btnGuardar.Text      = "💾 Guardar cambios";
            this.btnGuardar.UseVisualStyleBackColor = false;
            this.btnGuardar.Click += new System.EventHandler(this.BtnGuardar_Click);

            this.btnCambiarRol.BackColor = Color.White;
            this.btnCambiarRol.Cursor    = Cursors.Hand;
            this.btnCambiarRol.FlatAppearance.BorderColor = RosaOscuro;
            this.btnCambiarRol.FlatAppearance.BorderSize  = 1;
            this.btnCambiarRol.FlatStyle = FlatStyle.Flat;
            this.btnCambiarRol.Font      = new Font("Segoe UI", 8.5F);
            this.btnCambiarRol.ForeColor = RosaOscuro;
            this.btnCambiarRol.Location  = new Point(10, 374);
            this.btnCambiarRol.Name      = "btnCambiarRol";
            this.btnCambiarRol.Size      = new Size(260, 30);
            this.btnCambiarRol.TabIndex  = 14;
            this.btnCambiarRol.Text      = "🔁 Cambiar rol";
            this.btnCambiarRol.UseVisualStyleBackColor = false;
            this.btnCambiarRol.Click += new System.EventHandler(this.BtnCambiarRol_Click);

            this.btnHistorial.BackColor = Color.White;
            this.btnHistorial.Cursor    = Cursors.Hand;
            this.btnHistorial.FlatAppearance.BorderColor = RosaOscuro;
            this.btnHistorial.FlatAppearance.BorderSize  = 1;
            this.btnHistorial.FlatStyle = FlatStyle.Flat;
            this.btnHistorial.Font      = new Font("Segoe UI", 8.5F);
            this.btnHistorial.ForeColor = RosaOscuro;
            this.btnHistorial.Location  = new Point(10, 412);
            this.btnHistorial.Name      = "btnHistorial";
            this.btnHistorial.Size      = new Size(260, 30);
            this.btnHistorial.TabIndex  = 15;
            this.btnHistorial.Text      = "📜 Ver historial de cambios";
            this.btnHistorial.UseVisualStyleBackColor = false;
            this.btnHistorial.Click += new System.EventHandler(this.BtnHistorial_Click);

            this.lblMensaje.Font      = new Font("Segoe UI", 8.5F);
            this.lblMensaje.ForeColor = Color.DimGray;
            this.lblMensaje.Location  = new Point(10, 452);
            this.lblMensaje.Name      = "lblMensaje";
            this.lblMensaje.Size      = new Size(262, 60);
            this.lblMensaje.TabIndex  = 16;

            // ── btnCerrar ──────────────────────────────────────────────────────
            this.btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnCerrar.BackColor = Color.FromArgb(236, 236, 242);
            this.btnCerrar.Cursor    = Cursors.Hand;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = FlatStyle.Flat;
            this.btnCerrar.Font      = new Font("Segoe UI", 8.5F);
            this.btnCerrar.ForeColor = Color.FromArgb(70, 70, 80);
            this.btnCerrar.Location  = new Point(786, 532);
            this.btnCerrar.Name      = "btnCerrar";
            this.btnCerrar.Size      = new Size(90, 30);
            this.btnCerrar.TabIndex  = 8;
            this.btnCerrar.Text      = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);

            // ── AdministracionUsuariosForm ────────────────────────────────────
            this.BackColor = Color.White;
            this.Controls.Add(this.lblBuscar);
            this.Controls.Add(this.txtBuscar);
            this.Controls.Add(this.btnBuscar);
            this.Controls.Add(this.btnRefrescar);
            this.Controls.Add(this.btnNuevo);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.panelEdicion);
            this.Controls.Add(this.btnCerrar);
            this.Controls.Add(this.panelHeader);
            this.Font          = new Font("Segoe UI", 9F);
            this.MinimumSize   = new Size(820, 540);
            this.Name          = "AdministracionUsuariosForm";
            this.Size          = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text          = "Administración de Usuarios";

            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelEdicion.ResumeLayout(false);
            this.panelEdicion.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private Panel  panelHeader;
        private Label  lblTitulo;
        private Label  lblBuscar;
        private TextBox txtBuscar;
        private Button btnBuscar;
        private Button btnRefrescar;
        private Button btnNuevo;
        private DataGridView dgv;
        private Panel  panelEdicion;
        private Label  lblDatos;
        private Label  lblNombre;
        private TextBox txtNombre;
        private Label  lblApellido;
        private TextBox txtApellido;
        private Label  lblUsername;
        private TextBox txtUsername;
        private Label  lblEmail;
        private TextBox txtEmail;
        private Label  lblNacimiento;
        private DateTimePicker dtpNacimiento;
        private Label  lblRol;
        private ComboBox cmbRol;
        private Button btnGuardar;
        private Button btnCambiarRol;
        private Button btnHistorial;
        private Label  lblMensaje;
        private Button btnCerrar;
    }
}
