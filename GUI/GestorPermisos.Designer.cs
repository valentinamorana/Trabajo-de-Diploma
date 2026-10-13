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
            this.btnActualizar      = new Button();
            this.tabs               = new TabControl();
            this.tabGestion         = new TabPage();
            this.tabVistaCompleta   = new TabPage();
            this.panelVistaInfo     = new Panel();
            this.lblDescripcion     = new Label();
            this.lblLeyenda         = new Label();
            this.flVista            = new FlowLayoutPanel();
            this.btnExpandir        = new Button();
            this.btnColapsar        = new Button();
            this.tvVistaCompleta    = new TreeView();
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
            this.tabs.SuspendLayout();
            this.tabGestion.SuspendLayout();
            this.tabVistaCompleta.SuspendLayout();
            this.panelVistaInfo.SuspendLayout();
            this.flVista.SuspendLayout();
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
            this.btnActualizar.Text      = "Actualizar";
            this.btnActualizar.UseVisualStyleBackColor = false;
            this.btnActualizar.Click += new System.EventHandler(this.BtnActualizar_Click);

            // ── tabs: "Gestión de roles" y "Vista completa" (ex ExploradorCompositeForm) ──
            this.tabs.Controls.Add(this.tabGestion);
            this.tabs.Controls.Add(this.tabVistaCompleta);
            this.tabs.Dock          = DockStyle.Fill;
            this.tabs.Location      = new Point(0, 58);
            this.tabs.Name          = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size          = new Size(824, 551);
            this.tabs.TabIndex      = 2;

            this.tabGestion.BackColor = Color.White;
            this.tabGestion.Controls.Add(this.lblEstructura);
            this.tabGestion.Controls.Add(this.tvEstructura);
            this.tabGestion.Controls.Add(this.lblDetalle);
            this.tabGestion.Controls.Add(this.lblModo);
            this.tabGestion.Controls.Add(this.rbCrear);
            this.tabGestion.Controls.Add(this.rbEditar);
            this.tabGestion.Controls.Add(this.lblNombreRol);
            this.tabGestion.Controls.Add(this.txtNombreRol);
            this.tabGestion.Controls.Add(this.grpCrear);
            this.tabGestion.Controls.Add(this.grpEditar);
            this.tabGestion.Location = new Point(4, 24);
            this.tabGestion.Name     = "tabGestion";
            this.tabGestion.Size     = new Size(816, 523);
            this.tabGestion.TabIndex = 0;
            this.tabGestion.Text     = "Gestión de roles";

            // ── Pestaña "Vista completa": árbol Composite de solo lectura ─────
            this.tabVistaCompleta.BackColor = Color.White;
            this.tabVistaCompleta.Controls.Add(this.tvVistaCompleta);
            this.tabVistaCompleta.Controls.Add(this.flVista);
            this.tabVistaCompleta.Controls.Add(this.panelVistaInfo);
            this.tabVistaCompleta.Location = new Point(4, 24);
            this.tabVistaCompleta.Name     = "tabVistaCompleta";
            this.tabVistaCompleta.Size     = new Size(816, 523);
            this.tabVistaCompleta.TabIndex = 1;
            this.tabVistaCompleta.Text     = "Vista completa";

            this.panelVistaInfo.BackColor = Color.FromArgb(252, 240, 246);
            this.panelVistaInfo.Controls.Add(this.lblDescripcion);
            this.panelVistaInfo.Controls.Add(this.lblLeyenda);
            this.panelVistaInfo.Dock     = DockStyle.Top;
            this.panelVistaInfo.Name     = "panelVistaInfo";
            this.panelVistaInfo.Size     = new Size(816, 50);
            this.panelVistaInfo.TabIndex = 0;

            this.lblDescripcion.AutoSize  = true;
            this.lblDescripcion.Font      = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.lblDescripcion.ForeColor = RosaOscuro;
            this.lblDescripcion.Location  = new Point(14, 6);
            this.lblDescripcion.Name      = "lblDescripcion";
            this.lblDescripcion.TabIndex  = 0;
            this.lblDescripcion.Text      = "Estructura organizacional de WardrobeFlow — Solo lectura";

            this.lblLeyenda.AutoSize  = true;
            this.lblLeyenda.Font      = new Font("Segoe UI", 8.5F);
            this.lblLeyenda.ForeColor = Color.FromArgb(120, 30, 55);
            this.lblLeyenda.Location  = new Point(14, 27);
            this.lblLeyenda.Name      = "lblLeyenda";
            this.lblLeyenda.TabIndex  = 1;
            this.lblLeyenda.Text      = "Familia (nodo compuesto — Área o Rol)    Patente (hoja — permiso atómico)";

            this.flVista.BackColor     = Color.FromArgb(252, 240, 246);
            this.flVista.Controls.Add(this.btnExpandir);
            this.flVista.Controls.Add(this.btnColapsar);
            this.flVista.Dock          = DockStyle.Bottom;
            this.flVista.FlowDirection = FlowDirection.RightToLeft;
            this.flVista.Name          = "flVista";
            this.flVista.Padding       = new Padding(6);
            this.flVista.Size          = new Size(816, 46);
            this.flVista.TabIndex      = 2;

            this.btnExpandir.BackColor = RosaOscuro;
            this.btnExpandir.Cursor    = Cursors.Hand;
            this.btnExpandir.FlatAppearance.BorderSize = 0;
            this.btnExpandir.FlatStyle = FlatStyle.Flat;
            this.btnExpandir.Font      = new Font("Segoe UI", 9F);
            this.btnExpandir.ForeColor = Color.White;
            this.btnExpandir.Name      = "btnExpandir";
            this.btnExpandir.Size      = new Size(130, 32);
            this.btnExpandir.TabIndex  = 0;
            this.btnExpandir.Text      = "⊞ Expandir todo";
            this.btnExpandir.UseVisualStyleBackColor = false;
            this.btnExpandir.Click += new System.EventHandler(this.BtnExpandir_Click);

            this.btnColapsar.BackColor = Tema.RosaPrimario;
            this.btnColapsar.Cursor    = Cursors.Hand;
            this.btnColapsar.FlatAppearance.BorderSize = 0;
            this.btnColapsar.FlatStyle = FlatStyle.Flat;
            this.btnColapsar.Font      = new Font("Segoe UI", 9F);
            this.btnColapsar.ForeColor = Color.White;
            this.btnColapsar.Name      = "btnColapsar";
            this.btnColapsar.Size      = new Size(130, 32);
            this.btnColapsar.TabIndex  = 1;
            this.btnColapsar.Text      = "⊟ Colapsar todo";
            this.btnColapsar.UseVisualStyleBackColor = false;
            this.btnColapsar.Click += new System.EventHandler(this.BtnColapsar_Click);

            this.tvVistaCompleta.BackColor     = Color.FromArgb(252, 250, 252);
            this.tvVistaCompleta.BorderStyle   = BorderStyle.None;
            this.tvVistaCompleta.Dock          = DockStyle.Fill;
            this.tvVistaCompleta.Font          = new Font("Segoe UI", 9.5F);
            this.tvVistaCompleta.FullRowSelect = true;
            this.tvVistaCompleta.HideSelection = false;
            this.tvVistaCompleta.Indent        = 20;
            this.tvVistaCompleta.ItemHeight    = 24;
            this.tvVistaCompleta.Name          = "tvVistaCompleta";
            this.tvVistaCompleta.ShowLines     = true;
            this.tvVistaCompleta.ShowPlusMinus = true;
            this.tvVistaCompleta.TabIndex      = 1;

            // ── columna izquierda: estructura ────────────────────────────────
            this.lblEstructura.AutoSize  = true;
            this.lblEstructura.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblEstructura.ForeColor = RosaOscuro;
            this.lblEstructura.Location  = new Point(16, 12);
            this.lblEstructura.Name      = "lblEstructura";
            this.lblEstructura.TabIndex  = 2;
            this.lblEstructura.Text      = "Estructura del sistema";

            this.tvEstructura.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            this.tvEstructura.BorderStyle  = BorderStyle.FixedSingle;
            this.tvEstructura.Font         = new Font("Segoe UI", 9F);
            this.tvEstructura.HideSelection = false;
            this.tvEstructura.Location     = new Point(16, 34);
            this.tvEstructura.Name         = "tvEstructura";
            this.tvEstructura.Size         = new Size(380, 476);
            this.tvEstructura.TabIndex     = 3;
            this.tvEstructura.AfterSelect += new TreeViewEventHandler(this.Tv_AfterSelect);

            // ── columna central: panel de acciones ───────────────────────────
            this.lblDetalle.AutoSize  = false;
            this.lblDetalle.Font      = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            this.lblDetalle.ForeColor = RosaOscuro;
            this.lblDetalle.Location  = new Point(420, 14);
            this.lblDetalle.Name      = "lblDetalle";
            this.lblDetalle.Size      = new Size(340, 20);
            this.lblDetalle.TabIndex  = 4;
            this.lblDetalle.Text      = "Detalle: (sin selección)";

            this.lblModo.AutoSize  = true;
            this.lblModo.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblModo.ForeColor = RosaOscuro;
            this.lblModo.Location  = new Point(420, 42);
            this.lblModo.Name      = "lblModo";
            this.lblModo.TabIndex  = 5;
            this.lblModo.Text      = "Modo:";

            this.rbCrear.AutoSize = true;
            this.rbCrear.Checked  = true;
            this.rbCrear.Location = new Point(472, 40);
            this.rbCrear.Name     = "rbCrear";
            this.rbCrear.TabIndex = 6;
            this.rbCrear.TabStop  = true;
            this.rbCrear.Text     = "Crear";
            this.rbCrear.UseVisualStyleBackColor = true;
            this.rbCrear.CheckedChanged += new System.EventHandler(this.RbCrear_CheckedChanged);

            this.rbEditar.AutoSize = true;
            this.rbEditar.Location = new Point(540, 40);
            this.rbEditar.Name     = "rbEditar";
            this.rbEditar.TabIndex = 7;
            this.rbEditar.Text     = "Editar / Eliminar";
            this.rbEditar.UseVisualStyleBackColor = true;
            this.rbEditar.CheckedChanged += new System.EventHandler(this.RbEditar_CheckedChanged);

            this.lblNombreRol.AutoSize  = true;
            this.lblNombreRol.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblNombreRol.ForeColor = RosaOscuro;
            this.lblNombreRol.Location  = new Point(420, 72);
            this.lblNombreRol.Name      = "lblNombreRol";
            this.lblNombreRol.TabIndex  = 8;
            this.lblNombreRol.Text      = "Nombre del rol:";

            this.txtNombreRol.Font     = new Font("Segoe UI", 9F);
            this.txtNombreRol.Location = new Point(420, 94);
            this.txtNombreRol.Name     = "txtNombreRol";
            this.txtNombreRol.Size     = new Size(340, 24);
            this.txtNombreRol.TabIndex = 9;

            // ── grpCrear ──────────────────────────────────────────────────────
            this.grpCrear.Controls.Add(this.btnCrearRaiz);
            this.grpCrear.Controls.Add(this.btnCrearSub);
            this.grpCrear.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.grpCrear.ForeColor = RosaOscuro;
            this.grpCrear.Location  = new Point(420, 128);
            this.grpCrear.Name      = "grpCrear";
            this.grpCrear.Size      = new Size(340, 110);
            this.grpCrear.TabIndex  = 10;
            this.grpCrear.TabStop   = false;
            this.grpCrear.Text      = "Crear rol";

            this.btnCrearRaiz.BackColor = Tema.RosaPrimario;
            this.btnCrearRaiz.Cursor    = Cursors.Hand;
            this.btnCrearRaiz.FlatAppearance.BorderSize = 0;
            this.btnCrearRaiz.FlatStyle = FlatStyle.Flat;
            this.btnCrearRaiz.Font      = new Font("Segoe UI", 8.5F);
            this.btnCrearRaiz.ForeColor = Color.White;
            this.btnCrearRaiz.Location  = new Point(14, 26);
            this.btnCrearRaiz.Name      = "btnCrearRaiz";
            this.btnCrearRaiz.Size      = new Size(312, 30);
            this.btnCrearRaiz.TabIndex  = 0;
            this.btnCrearRaiz.Text      = "Crear rol raíz";
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
            this.btnCrearSub.Text      = "Crear sub-rol";
            this.btnCrearSub.UseVisualStyleBackColor = false;
            this.btnCrearSub.Click += new System.EventHandler(this.BtnCrearSub_Click);

            // ── grpEditar ─────────────────────────────────────────────────────
            this.grpEditar.Controls.Add(this.btnEditarNombre);
            this.grpEditar.Controls.Add(this.btnEliminarRol);
            this.grpEditar.Controls.Add(this.grpAsignar);
            this.grpEditar.Controls.Add(this.btnQuitar);
            this.grpEditar.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.grpEditar.ForeColor = RosaOscuro;
            this.grpEditar.Location  = new Point(420, 128);
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
            this.btnEditarNombre.Text      = "Renombrar rol";
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
            this.btnEliminarRol.Text      = "Eliminar rol";
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
            this.lblAsignar.Tag      = "lbl.permisos.elegiritem";
            this.lblAsignar.Text     = "Elegí un ítem:";

            this.cmbAsignables.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbAsignables.Font          = new Font("Segoe UI", 9F);
            this.cmbAsignables.Location      = new Point(12, 46);
            this.cmbAsignables.Name          = "cmbAsignables";
            this.cmbAsignables.Size          = new Size(288, 24);
            this.cmbAsignables.TabIndex      = 1;

            this.btnAsignar.BackColor = Tema.RosaPrimario;
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
            this.Controls.Add(this.tabs);
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
            this.panelVistaInfo.ResumeLayout(false);
            this.panelVistaInfo.PerformLayout();
            this.flVista.ResumeLayout(false);
            this.tabGestion.ResumeLayout(false);
            this.tabGestion.PerformLayout();
            this.tabVistaCompleta.ResumeLayout(false);
            this.tabs.ResumeLayout(false);
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
        private Button   btnActualizar;
        private TabControl tabs;
        private TabPage  tabGestion;
        private TabPage  tabVistaCompleta;
        private Panel    panelVistaInfo;
        private Label    lblDescripcion;
        private Label    lblLeyenda;
        private FlowLayoutPanel flVista;
        private Button   btnExpandir;
        private Button   btnColapsar;
        private TreeView tvVistaCompleta;
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
