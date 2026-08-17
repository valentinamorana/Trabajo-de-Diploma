using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class GestorPermisos
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
            this.components        = new System.ComponentModel.Container();
            this.panelHeader        = new Panel();
            this.lblTitulo          = new Label();
            this.lblSubtitulo       = new Label();
            this.tip                = new ToolTip(this.components);
            this.panelFooter        = new Panel();
            this.lblMensaje         = new Label();
            this.flAcciones         = new FlowLayoutPanel();
            this.btnCerrar          = new Button();
            this.btnExplorador      = new Button();
            this.btnActualizar      = new Button();
            this.lblEstructura      = new Label();
            this.tvEstructura       = new TreeView();
            this.lblDetalle         = new Label();
            this.lblModo            = new Label();
            this.rbCrear            = new RadioButton();
            this.rbEditar           = new RadioButton();
            this.lblNombreRol       = new Label();
            this.txtNombreRol       = new TextBox();
            this.grpCrear           = new GroupBox();
            this.btnCrearRaiz       = new Button();
            this.btnCrearSub        = new Button();
            this.grpEditar          = new GroupBox();
            this.btnEditarNombre    = new Button();
            this.btnEliminarRol     = new Button();
            this.grpAsignar         = new GroupBox();
            this.lblAsignar         = new Label();
            this.cmbAsignables      = new ComboBox();
            this.btnAsignar         = new Button();
            this.btnQuitar          = new Button();
            this.panelHeader.SuspendLayout();
            this.panelFooter.SuspendLayout();
            this.flAcciones.SuspendLayout();
            this.grpCrear.SuspendLayout();
            this.grpEditar.SuspendLayout();
            this.grpAsignar.SuspendLayout();
            this.SuspendLayout();

            // ── panelHeader ────────────────────────────────────────────────────
            this.panelHeader.Controls.Add(this.lblTitulo);
            this.panelHeader.Controls.Add(this.lblSubtitulo);
            this.panelHeader.Dock     = DockStyle.Top;
            this.panelHeader.Location = new Point(0, 0);
            this.panelHeader.Name     = "panelHeader";
            this.panelHeader.Size     = new Size(840, 58);
            this.panelHeader.TabIndex = 0;
            this.panelHeader.Paint += new PaintEventHandler(this.PanelHeader_Paint);

            this.lblTitulo.AutoSize  = true;
            this.lblTitulo.BackColor = Color.Transparent;
            this.lblTitulo.Font      = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitulo.ForeColor = Color.White;
            this.lblTitulo.Location  = new Point(18, 8);
            this.lblTitulo.Name      = "lblTitulo";
            this.lblTitulo.TabIndex  = 0;
            this.lblTitulo.Text      = "Perfiles y Permisos";

            this.lblSubtitulo.AutoSize  = true;
            this.lblSubtitulo.BackColor = Color.Transparent;
            this.lblSubtitulo.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            this.lblSubtitulo.ForeColor = Color.FromArgb(255, 224, 236);
            this.lblSubtitulo.Location  = new Point(20, 34);
            this.lblSubtitulo.Name      = "lblSubtitulo";
            this.lblSubtitulo.TabIndex  = 1;
            this.lblSubtitulo.Text      = "Gestión de roles (los permisos son un catálogo fijo)";

            // ── tip ────────────────────────────────────────────────────────────
            this.tip.AutoPopDelay = 30000;
            this.tip.InitialDelay = 250;
            this.tip.IsBalloon    = true;
            this.tip.ReshowDelay  = 100;
            this.tip.ShowAlways   = true;

            // ── panelFooter ───────────────────────────────────────────────────
            this.panelFooter.BackColor = PanelClaro;
            this.panelFooter.Controls.Add(this.lblMensaje);
            this.panelFooter.Controls.Add(this.flAcciones);
            this.panelFooter.Dock     = DockStyle.Bottom;
            this.panelFooter.Location = new Point(0, 616);
            this.panelFooter.Name     = "panelFooter";
            this.panelFooter.Size     = new Size(840, 52);
            this.panelFooter.TabIndex = 1;

            this.lblMensaje.Dock      = DockStyle.Fill;
            this.lblMensaje.Font      = new Font("Segoe UI", 8.5F);
            this.lblMensaje.ForeColor = Color.DimGray;
            this.lblMensaje.Location  = new Point(0, 0);
            this.lblMensaje.Name      = "lblMensaje";
            this.lblMensaje.Padding   = new Padding(16, 0, 0, 0);
            this.lblMensaje.Size      = new Size(340, 52);
            this.lblMensaje.TabIndex  = 0;
            this.lblMensaje.TextAlign = ContentAlignment.MiddleLeft;

            this.flAcciones.AutoSize      = false;
            this.flAcciones.BackColor     = PanelClaro;
            this.flAcciones.Controls.Add(this.btnCerrar);
            this.flAcciones.Controls.Add(this.btnExplorador);
            this.flAcciones.Controls.Add(this.btnActualizar);
            this.flAcciones.Dock          = DockStyle.Right;
            this.flAcciones.FlowDirection = FlowDirection.RightToLeft;
            this.flAcciones.Location      = new Point(340, 0);
            this.flAcciones.Name          = "flAcciones";
            this.flAcciones.Padding       = new Padding(0, 11, 10, 0);
            this.flAcciones.Size          = new Size(500, 52);
            this.flAcciones.TabIndex      = 1;
            this.flAcciones.WrapContents  = false;

            this.btnCerrar.BackColor = Neutro;
            this.btnCerrar.Cursor    = Cursors.Hand;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = FlatStyle.Flat;
            this.btnCerrar.Font      = new Font("Segoe UI", 8.5F);
            this.btnCerrar.ForeColor = Color.FromArgb(70, 70, 80);
            this.btnCerrar.Margin    = new Padding(8, 0, 0, 0);
            this.btnCerrar.Name      = "btnCerrar";
            this.btnCerrar.Size      = new Size(96, 30);
            this.btnCerrar.TabIndex  = 0;
            this.btnCerrar.Text      = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);

            this.btnExplorador.BackColor = Color.White;
            this.btnExplorador.Cursor    = Cursors.Hand;
            this.btnExplorador.FlatAppearance.BorderColor = RosaOscuro;
            this.btnExplorador.FlatAppearance.BorderSize  = 1;
            this.btnExplorador.FlatStyle = FlatStyle.Flat;
            this.btnExplorador.Font      = new Font("Segoe UI", 8.5F);
            this.btnExplorador.ForeColor = RosaOscuro;
            this.btnExplorador.Margin    = new Padding(8, 0, 0, 0);
            this.btnExplorador.Name      = "btnExplorador";
            this.btnExplorador.Size      = new Size(240, 30);
            this.btnExplorador.TabIndex  = 1;
            this.btnExplorador.Text      = "🌳 Ver vista completa del sistema";
            this.btnExplorador.UseVisualStyleBackColor = false;
            this.btnExplorador.Click += new System.EventHandler(this.BtnExplorador_Click);

            this.btnActualizar.BackColor = Color.White;
            this.btnActualizar.Cursor    = Cursors.Hand;
            this.btnActualizar.FlatAppearance.BorderColor = RosaOscuro;
            this.btnActualizar.FlatAppearance.BorderSize  = 1;
            this.btnActualizar.FlatStyle = FlatStyle.Flat;
            this.btnActualizar.Font      = new Font("Segoe UI", 8.5F);
            this.btnActualizar.ForeColor = RosaOscuro;
            this.btnActualizar.Margin    = new Padding(8, 0, 0, 0);
            this.btnActualizar.Name      = "btnActualizar";
            this.btnActualizar.Size      = new Size(120, 30);
            this.btnActualizar.TabIndex  = 2;
            this.btnActualizar.Text      = "↻ Actualizar";
            this.btnActualizar.UseVisualStyleBackColor = false;
            this.btnActualizar.Click += new System.EventHandler(this.BtnActualizar_Click);

            // ── columna izquierda: estructura ────────────────────────────────
            this.lblEstructura.AutoSize  = true;
            this.lblEstructura.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblEstructura.ForeColor = RosaOscuro;
            this.lblEstructura.Location  = new Point(16, 70);
            this.lblEstructura.Name      = "lblEstructura";
            this.lblEstructura.TabIndex  = 2;
            this.lblEstructura.Text      = "Estructura del sistema";

            this.tvEstructura.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            this.tvEstructura.BorderStyle  = BorderStyle.FixedSingle;
            this.tvEstructura.Font         = new Font("Segoe UI", 9F);
            this.tvEstructura.HideSelection = false;
            this.tvEstructura.Location     = new Point(16, 92);
            this.tvEstructura.Name         = "tvEstructura";
            this.tvEstructura.Size         = new Size(380, 452);
            this.tvEstructura.TabIndex     = 3;
            this.tvEstructura.AfterSelect += new TreeViewEventHandler(this.Tv_AfterSelect);

            // ── columna central: panel de acciones ───────────────────────────
            this.lblDetalle.AutoSize  = false;
            this.lblDetalle.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            this.lblDetalle.ForeColor = RosaOscuro;
            this.lblDetalle.Location  = new Point(420, 72);
            this.lblDetalle.Name      = "lblDetalle";
            this.lblDetalle.Size      = new Size(340, 20);
            this.lblDetalle.TabIndex  = 4;
            this.lblDetalle.Text      = "Detalle: (sin selección)";

            this.lblModo.AutoSize  = true;
            this.lblModo.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblModo.ForeColor = RosaOscuro;
            this.lblModo.Location  = new Point(420, 100);
            this.lblModo.Name      = "lblModo";
            this.lblModo.TabIndex  = 5;
            this.lblModo.Text      = "Modo:";

            this.rbCrear.AutoSize = true;
            this.rbCrear.Checked  = true;
            this.rbCrear.Location = new Point(472, 98);
            this.rbCrear.Name     = "rbCrear";
            this.rbCrear.TabIndex = 6;
            this.rbCrear.TabStop  = true;
            this.rbCrear.Text     = "Crear";
            this.rbCrear.UseVisualStyleBackColor = true;
            this.rbCrear.CheckedChanged += new System.EventHandler(this.RbCrear_CheckedChanged);

            this.rbEditar.AutoSize = true;
            this.rbEditar.Location = new Point(540, 98);
            this.rbEditar.Name     = "rbEditar";
            this.rbEditar.TabIndex = 7;
            this.rbEditar.Text     = "Editar / Eliminar";
            this.rbEditar.UseVisualStyleBackColor = true;
            this.rbEditar.CheckedChanged += new System.EventHandler(this.RbEditar_CheckedChanged);

            this.lblNombreRol.AutoSize  = true;
            this.lblNombreRol.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblNombreRol.ForeColor = RosaOscuro;
            this.lblNombreRol.Location  = new Point(420, 130);
            this.lblNombreRol.Name      = "lblNombreRol";
            this.lblNombreRol.TabIndex  = 8;
            this.lblNombreRol.Text      = "Nombre del rol:";

            this.txtNombreRol.Font     = new Font("Segoe UI", 9F);
            this.txtNombreRol.Location = new Point(420, 152);
            this.txtNombreRol.Name     = "txtNombreRol";
            this.txtNombreRol.Size     = new Size(340, 24);
            this.txtNombreRol.TabIndex = 9;

            // ── grpCrear ──────────────────────────────────────────────────────
            this.grpCrear.Controls.Add(this.btnCrearRaiz);
            this.grpCrear.Controls.Add(this.btnCrearSub);
            this.grpCrear.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.grpCrear.ForeColor = RosaOscuro;
            this.grpCrear.Location  = new Point(420, 186);
            this.grpCrear.Name      = "grpCrear";
            this.grpCrear.Size      = new Size(340, 110);
            this.grpCrear.TabIndex  = 10;
            this.grpCrear.TabStop   = false;
            this.grpCrear.Text      = "Crear rol";

            this.btnCrearRaiz.BackColor = RosaPrimario;
            this.btnCrearRaiz.Cursor    = Cursors.Hand;
            this.btnCrearRaiz.FlatAppearance.BorderSize = 0;
            this.btnCrearRaiz.FlatStyle = FlatStyle.Flat;
            this.btnCrearRaiz.Font      = new Font("Segoe UI", 8.5F);
            this.btnCrearRaiz.ForeColor = Color.White;
            this.btnCrearRaiz.Location  = new Point(14, 26);
            this.btnCrearRaiz.Name      = "btnCrearRaiz";
            this.btnCrearRaiz.Size      = new Size(312, 30);
            this.btnCrearRaiz.TabIndex  = 0;
            this.btnCrearRaiz.Text      = "➕ Crear rol raíz";
            this.btnCrearRaiz.UseVisualStyleBackColor = false;
            this.btnCrearRaiz.Click += new System.EventHandler(this.BtnCrearRaiz_Click);

            this.btnCrearSub.BackColor = Color.White;
            this.btnCrearSub.Cursor    = Cursors.Hand;
            this.btnCrearSub.FlatAppearance.BorderColor = RosaOscuro;
            this.btnCrearSub.FlatAppearance.BorderSize  = 1;
            this.btnCrearSub.FlatStyle = FlatStyle.Flat;
            this.btnCrearSub.Font      = new Font("Segoe UI", 8.5F);
            this.btnCrearSub.ForeColor = RosaOscuro;
            this.btnCrearSub.Location  = new Point(14, 64);
            this.btnCrearSub.Name      = "btnCrearSub";
            this.btnCrearSub.Size      = new Size(312, 30);
            this.btnCrearSub.TabIndex  = 1;
            this.btnCrearSub.Text      = "➕ Crear sub-rol";
            this.btnCrearSub.UseVisualStyleBackColor = false;
            this.btnCrearSub.Click += new System.EventHandler(this.BtnCrearSub_Click);

            // ── grpEditar ─────────────────────────────────────────────────────
            this.grpEditar.Controls.Add(this.btnEditarNombre);
            this.grpEditar.Controls.Add(this.btnEliminarRol);
            this.grpEditar.Controls.Add(this.grpAsignar);
            this.grpEditar.Controls.Add(this.btnQuitar);
            this.grpEditar.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.grpEditar.ForeColor = RosaOscuro;
            this.grpEditar.Location  = new Point(420, 186);
            this.grpEditar.Name      = "grpEditar";
            this.grpEditar.Size      = new Size(340, 300);
            this.grpEditar.TabIndex  = 11;
            this.grpEditar.TabStop   = false;
            this.grpEditar.Text      = "Editar rol";
            this.grpEditar.Visible   = false;

            this.btnEditarNombre.BackColor = Color.White;
            this.btnEditarNombre.Cursor    = Cursors.Hand;
            this.btnEditarNombre.FlatAppearance.BorderColor = RosaOscuro;
            this.btnEditarNombre.FlatAppearance.BorderSize  = 1;
            this.btnEditarNombre.FlatStyle = FlatStyle.Flat;
            this.btnEditarNombre.Font      = new Font("Segoe UI", 8.5F);
            this.btnEditarNombre.ForeColor = RosaOscuro;
            this.btnEditarNombre.Location  = new Point(14, 26);
            this.btnEditarNombre.Name      = "btnEditarNombre";
            this.btnEditarNombre.Size      = new Size(312, 30);
            this.btnEditarNombre.TabIndex  = 0;
            this.btnEditarNombre.Text      = "✏ Renombrar rol";
            this.btnEditarNombre.UseVisualStyleBackColor = false;
            this.btnEditarNombre.Click += new System.EventHandler(this.BtnEditarNombre_Click);

            this.btnEliminarRol.BackColor = Peligro;
            this.btnEliminarRol.Cursor    = Cursors.Hand;
            this.btnEliminarRol.FlatAppearance.BorderSize = 0;
            this.btnEliminarRol.FlatStyle = FlatStyle.Flat;
            this.btnEliminarRol.Font      = new Font("Segoe UI", 8.5F);
            this.btnEliminarRol.ForeColor = Color.White;
            this.btnEliminarRol.Location  = new Point(14, 62);
            this.btnEliminarRol.Name      = "btnEliminarRol";
            this.btnEliminarRol.Size      = new Size(312, 30);
            this.btnEliminarRol.TabIndex  = 1;
            this.btnEliminarRol.Text      = "🗑 Eliminar rol";
            this.btnEliminarRol.UseVisualStyleBackColor = false;
            this.btnEliminarRol.Click += new System.EventHandler(this.BtnEliminarRol_Click);

            // ── grpAsignar ────────────────────────────────────────────────────
            this.grpAsignar.Controls.Add(this.lblAsignar);
            this.grpAsignar.Controls.Add(this.cmbAsignables);
            this.grpAsignar.Controls.Add(this.btnAsignar);
            this.grpAsignar.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.grpAsignar.ForeColor = RosaOscuro;
            this.grpAsignar.Location  = new Point(14, 104);
            this.grpAsignar.Name      = "grpAsignar";
            this.grpAsignar.Size      = new Size(312, 116);
            this.grpAsignar.TabIndex  = 2;
            this.grpAsignar.TabStop   = false;
            this.grpAsignar.Text      = "Asignar permiso o rol";

            this.lblAsignar.AutoSize = true;
            this.lblAsignar.Font     = new Font("Segoe UI", 8.5F);
            this.lblAsignar.Location = new Point(12, 26);
            this.lblAsignar.Name     = "lblAsignar";
            this.lblAsignar.TabIndex = 0;
            this.lblAsignar.Text     = "Elegí un ítem:";

            this.cmbAsignables.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbAsignables.Font          = new Font("Segoe UI", 9F);
            this.cmbAsignables.Location      = new Point(12, 46);
            this.cmbAsignables.Name          = "cmbAsignables";
            this.cmbAsignables.Size          = new Size(288, 24);
            this.cmbAsignables.TabIndex      = 1;

            this.btnAsignar.BackColor = RosaPrimario;
            this.btnAsignar.Cursor    = Cursors.Hand;
            this.btnAsignar.FlatAppearance.BorderSize = 0;
            this.btnAsignar.FlatStyle = FlatStyle.Flat;
            this.btnAsignar.Font      = new Font("Segoe UI", 8.5F);
            this.btnAsignar.ForeColor = Color.White;
            this.btnAsignar.Location  = new Point(12, 76);
            this.btnAsignar.Name      = "btnAsignar";
            this.btnAsignar.Size      = new Size(288, 30);
            this.btnAsignar.TabIndex  = 2;
            this.btnAsignar.Text      = "Asignar ↓";
            this.btnAsignar.UseVisualStyleBackColor = false;
            this.btnAsignar.Click += new System.EventHandler(this.BtnAsignar_Click);

            this.btnQuitar.BackColor = Color.White;
            this.btnQuitar.Cursor    = Cursors.Hand;
            this.btnQuitar.FlatAppearance.BorderColor = RosaOscuro;
            this.btnQuitar.FlatAppearance.BorderSize  = 1;
            this.btnQuitar.FlatStyle = FlatStyle.Flat;
            this.btnQuitar.Font      = new Font("Segoe UI", 8.5F);
            this.btnQuitar.ForeColor = RosaOscuro;
            this.btnQuitar.Location  = new Point(14, 230);
            this.btnQuitar.Name      = "btnQuitar";
            this.btnQuitar.Size      = new Size(312, 30);
            this.btnQuitar.TabIndex  = 3;
            this.btnQuitar.Text      = "Quitar ítem seleccionado";
            this.btnQuitar.UseVisualStyleBackColor = false;
            this.btnQuitar.Click += new System.EventHandler(this.BtnQuitar_Click);

            // ── GestorPermisos ────────────────────────────────────────────────
            this.BackColor = Color.White;
            this.Controls.Add(this.lblEstructura);
            this.Controls.Add(this.tvEstructura);
            this.Controls.Add(this.lblDetalle);
            this.Controls.Add(this.lblModo);
            this.Controls.Add(this.rbCrear);
            this.Controls.Add(this.rbEditar);
            this.Controls.Add(this.lblNombreRol);
            this.Controls.Add(this.txtNombreRol);
            this.Controls.Add(this.grpCrear);
            this.Controls.Add(this.grpEditar);
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.panelHeader);
            this.Font          = new Font("Segoe UI", 9F);
            this.MinimumSize   = new Size(800, 640);
            this.Name          = "GestorPermisos";
            this.Size          = new Size(840, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text          = "Gestor de Perfiles — Roles y Permisos (Composite)";

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelFooter.ResumeLayout(false);
            this.flAcciones.ResumeLayout(false);
            this.grpCrear.ResumeLayout(false);
            this.grpEditar.ResumeLayout(false);
            this.grpAsignar.ResumeLayout(false);
            this.grpAsignar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

            this.panelFooter.BringToFront();
            this.panelHeader.BringToFront();
        }

        #endregion

        private Panel    panelHeader;
        private Label    lblTitulo;
        private Label    lblSubtitulo;
        private ToolTip  tip;
        private Panel    panelFooter;
        private Label    lblMensaje;
        private FlowLayoutPanel flAcciones;
        private Button   btnCerrar;
        private Button   btnExplorador;
        private Button   btnActualizar;
        private Label    lblEstructura;
        private TreeView tvEstructura;
        private Label    lblDetalle;
        private Label    lblModo;
        private RadioButton rbCrear;
        private RadioButton rbEditar;
        private Label    lblNombreRol;
        private TextBox  txtNombreRol;
        private GroupBox grpCrear;
        private Button   btnCrearRaiz;
        private Button   btnCrearSub;
        private GroupBox grpEditar;
        private Button   btnEditarNombre;
        private Button   btnEliminarRol;
        private GroupBox grpAsignar;
        private Label    lblAsignar;
        private ComboBox cmbAsignables;
        private Button   btnAsignar;
        private Button   btnQuitar;
    }
}
