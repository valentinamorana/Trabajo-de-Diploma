namespace GUI
{
    partial class Menu
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Menu));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsIdioma = new System.Windows.Forms.ToolStrip();
            this.lblIdioma = new System.Windows.Forms.ToolStripLabel();
            this.tsIdiomaSep = new System.Windows.Forms.ToolStripSeparator();
            this.cmbIdiomaMenu = new System.Windows.Forms.ToolStripComboBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.usuarioToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miPerfilItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cerrarSesionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelControlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.inventarioToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.prendasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.outfitsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.categoriasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.suscriptoresToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ventasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clientesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.planesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renovacionSuscripcionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cobroSuscripcionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pedidosVentaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pedidosRealizadosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gestionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.grpUsuarios = new System.Windows.Forms.ToolStripMenuItem();
            this.adminUsuariosItem = new System.Windows.Forms.ToolStripMenuItem();
            this.usuariosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.perfilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sepAdministrarToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.grpSistema = new System.Windows.Forms.ToolStripMenuItem();
            this.idiomasToolStripMenuItem  = new System.Windows.Forms.ToolStripMenuItem();
            this.backupToolStripMenuItem         = new System.Windows.Forms.ToolStripMenuItem();
            this.historialUsuariosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.integridadToolStripMenuItem     = new System.Windows.Forms.ToolStripMenuItem();
            this.bitacoraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bitSistemaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bitNegocioToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sepBitacoraToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.reporteJornadaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.analisisAbandonoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ventanaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alertasItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsIdioma.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // contextMenuStrip1
            //
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            //
            // tsIdioma — barra de selección de idioma (debajo del MenuStrip principal).
            //
            this.tsIdioma.Dock      = System.Windows.Forms.DockStyle.Top;
            this.tsIdioma.BackColor = System.Drawing.Color.FromArgb(40, 40, 55);
            this.tsIdioma.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsIdioma.Padding   = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.tsIdioma.Height    = 28;
            this.tsIdioma.Name      = "tsIdioma";
            this.tsIdioma.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblIdioma,
            this.tsIdiomaSep,
            this.cmbIdiomaMenu});
            //
            // lblIdioma
            //
            this.lblIdioma.Name      = "lblIdioma";
            this.lblIdioma.Text      = "Idioma:";
            this.lblIdioma.ForeColor = System.Drawing.Color.FromArgb(200, 200, 210);
            this.lblIdioma.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            //
            // tsIdiomaSep
            //
            this.tsIdiomaSep.Name = "tsIdiomaSep";
            //
            // cmbIdiomaMenu
            //
            this.cmbIdiomaMenu.Name          = "cmbIdiomaMenu";
            this.cmbIdiomaMenu.AutoSize      = false;
            this.cmbIdiomaMenu.Width         = 140;
            this.cmbIdiomaMenu.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbIdiomaMenu.Font          = new System.Drawing.Font("Segoe UI", 8.5F);
            this.cmbIdiomaMenu.SelectedIndexChanged += new System.EventHandler(this.CmbIdiomaMenu_Changed);
            //
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.usuarioToolStripMenuItem,
            this.panelControlToolStripMenuItem,
            this.suscriptoresToolStripMenuItem,
            this.inventarioToolStripMenuItem,
            this.ventasToolStripMenuItem,
            this.bitacoraToolStripMenuItem,
            this.gestionToolStripMenuItem,
            this.ventanaToolStripMenuItem,
            this.alertasItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1100, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            //
            // usuarioToolStripMenuItem — se renombra "Sesión" (evita confundirlo con "Perfiles y Permisos").
            //
            this.usuarioToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miPerfilItem,
            this.cerrarSesionToolStripMenuItem});
//            this.usuarioToolStripMenuItem.Image = global::GUI.Properties.Resources._3106921_1_;
            this.usuarioToolStripMenuItem.Name = "usuarioToolStripMenuItem";
            this.usuarioToolStripMenuItem.Tag = "mnu.sesion";
            this.usuarioToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.usuarioToolStripMenuItem.Text = "Sesión";
            // Cuenta (Sesión/Mi Perfil) pasa al costado derecho, junto a Alertas — deja de competir
            // visualmente con los módulos de negocio (hallazgo #10 del rediseño UX/UI).
            this.usuarioToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            //
            // miPerfilItem — "Mi Perfil" (preferencias del usuario), disponible para todos.
            //
            this.miPerfilItem.Name = "miPerfilItem";
            this.miPerfilItem.Tag  = "perfil.menu";
            this.miPerfilItem.Text = "Mi Perfil";
            this.miPerfilItem.Click += new System.EventHandler(this.MiPerfil_Click);
            //
            // panelControlToolStripMenuItem
            //
            this.panelControlToolStripMenuItem.Name = "panelControlToolStripMenuItem";
            this.panelControlToolStripMenuItem.Tag = "mnu.dashboard";
            this.panelControlToolStripMenuItem.Size = new System.Drawing.Size(120, 20);
            this.panelControlToolStripMenuItem.Text = "Panel de Control";
            this.panelControlToolStripMenuItem.Click += new System.EventHandler(this.panelControlToolStripMenuItem_Click);
            // 
            // cerrarSesionToolStripMenuItem
            // 
            this.cerrarSesionToolStripMenuItem.Name = "cerrarSesionToolStripMenuItem";
            this.cerrarSesionToolStripMenuItem.Tag = "mnu.cerrarsesion";
            this.cerrarSesionToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.cerrarSesionToolStripMenuItem.Text = "Cerrar Sesion";
            this.cerrarSesionToolStripMenuItem.Click += new System.EventHandler(this.cerrarSesionToolStripMenuItem_Click);
            // 
            // inventarioToolStripMenuItem
            // 
            this.inventarioToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.prendasToolStripMenuItem,
            this.outfitsToolStripMenuItem,
            this.categoriasToolStripMenuItem});
            this.inventarioToolStripMenuItem.Name = "inventarioToolStripMenuItem";
            this.inventarioToolStripMenuItem.Tag = "mnu.inventario";
            this.inventarioToolStripMenuItem.Size = new System.Drawing.Size(72, 20);
            this.inventarioToolStripMenuItem.Text = "Inventario";
            // 
            // prendasToolStripMenuItem
            // 
            this.prendasToolStripMenuItem.Name = "prendasToolStripMenuItem";
            this.prendasToolStripMenuItem.Tag = "mnu.prendas";
            this.prendasToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.prendasToolStripMenuItem.Text = "Prendas";
            this.prendasToolStripMenuItem.Click += new System.EventHandler(this.prendasToolStripMenuItem_Click);
            // 
            // outfitsToolStripMenuItem
            // 
            this.outfitsToolStripMenuItem.Name = "outfitsToolStripMenuItem";
            this.outfitsToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.outfitsToolStripMenuItem.Text = "Outfits";
            this.outfitsToolStripMenuItem.Click += new System.EventHandler(this.outfitsToolStripMenuItem_Click);
            // 
            // categoriasToolStripMenuItem
            // 
            this.categoriasToolStripMenuItem.Name = "categoriasToolStripMenuItem";
            this.categoriasToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.categoriasToolStripMenuItem.Text = "Categorias";
            this.categoriasToolStripMenuItem.Click += new System.EventHandler(this.categoriasToolStripMenuItem_Click);
            //
            // suscriptoresToolStripMenuItem
            //
            // Se separa de "Ventas": Clientes/Planes/Renovación tienen ritmo semanal, no diario
            // (hallazgo #6 del rediseño UX/UI) — antes vivían mezclados con Pedidos de Venta.
            this.suscriptoresToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clientesToolStripMenuItem,
            this.planesToolStripMenuItem,
            this.renovacionSuscripcionToolStripMenuItem,
            this.cobroSuscripcionToolStripMenuItem});
            this.suscriptoresToolStripMenuItem.Name = "suscriptoresToolStripMenuItem";
            this.suscriptoresToolStripMenuItem.Tag = "mnu.suscriptores";
            this.suscriptoresToolStripMenuItem.Size = new System.Drawing.Size(90, 20);
            this.suscriptoresToolStripMenuItem.Text = "Suscriptores";
            //
            // ventasToolStripMenuItem
            //
            this.ventasToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pedidosVentaToolStripMenuItem,
            this.pedidosRealizadosToolStripMenuItem});
            this.ventasToolStripMenuItem.Name = "ventasToolStripMenuItem";
            this.ventasToolStripMenuItem.Tag = "mnu.ventas";
            this.ventasToolStripMenuItem.Size = new System.Drawing.Size(53, 20);
            this.ventasToolStripMenuItem.Text = "Ventas";
            // 
            // clientesToolStripMenuItem
            // 
            this.clientesToolStripMenuItem.Name = "clientesToolStripMenuItem";
            this.clientesToolStripMenuItem.Tag = "mnu.clientes";
            this.clientesToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.clientesToolStripMenuItem.Text = "Clientes";
            this.clientesToolStripMenuItem.Click += new System.EventHandler(this.clientesToolStripMenuItem_Click);
            // 
            // planesToolStripMenuItem
            // 
            this.planesToolStripMenuItem.Name = "planesToolStripMenuItem";
            this.planesToolStripMenuItem.Tag = "mnu.planes";
            this.planesToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.planesToolStripMenuItem.Text = "Planes de Suscripcion";
            this.planesToolStripMenuItem.Click += new System.EventHandler(this.planesToolStripMenuItem_Click);
            //
            // renovacionSuscripcionToolStripMenuItem
            //
            this.renovacionSuscripcionToolStripMenuItem.Name = "renovacionSuscripcionToolStripMenuItem";
            this.renovacionSuscripcionToolStripMenuItem.Tag = "mnu.renovacion";
            this.renovacionSuscripcionToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.renovacionSuscripcionToolStripMenuItem.Text = "Renovación de Suscripción";
            this.renovacionSuscripcionToolStripMenuItem.Click += new System.EventHandler(this.renovacionSuscripcionToolStripMenuItem_Click);
            //
            // cobroSuscripcionToolStripMenuItem
            //
            this.cobroSuscripcionToolStripMenuItem.Name = "cobroSuscripcionToolStripMenuItem";
            this.cobroSuscripcionToolStripMenuItem.Tag = "mnu.cobro";
            this.cobroSuscripcionToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.cobroSuscripcionToolStripMenuItem.Text = "Cobro de Suscripción";
            this.cobroSuscripcionToolStripMenuItem.Click += new System.EventHandler(this.cobroSuscripcionToolStripMenuItem_Click);
            //
            // pedidosVentaToolStripMenuItem
            // 
            this.pedidosVentaToolStripMenuItem.Name = "pedidosVentaToolStripMenuItem";
            this.pedidosVentaToolStripMenuItem.Tag = "mnu.pedidosventa";
            this.pedidosVentaToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.pedidosVentaToolStripMenuItem.Text = "Pedidos de Venta";
            this.pedidosVentaToolStripMenuItem.Click += new System.EventHandler(this.pedidosVentaToolStripMenuItem_Click);
            // 
            // pedidosRealizadosToolStripMenuItem
            // 
            this.pedidosRealizadosToolStripMenuItem.Name = "pedidosRealizadosToolStripMenuItem";
            this.pedidosRealizadosToolStripMenuItem.Tag = "mnu.pedidosreal";
            this.pedidosRealizadosToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.pedidosRealizadosToolStripMenuItem.Text = "Pedidos Realizados";
            this.pedidosRealizadosToolStripMenuItem.Click += new System.EventHandler(this.pedidosRealizadosToolStripMenuItem_Click);
            //
            // gestionToolStripMenuItem — reorganizado en submenús: Usuarios ▸, Perfiles, ──, Sistema ▸.
            //
            this.gestionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.grpUsuarios,
            this.perfilesToolStripMenuItem,
            this.sepAdministrarToolStripSeparator,
            this.grpSistema});
            this.gestionToolStripMenuItem.Name = "gestionToolStripMenuItem";
            this.gestionToolStripMenuItem.Tag = "mnu.administrar";
            this.gestionToolStripMenuItem.Size = new System.Drawing.Size(81, 20);
            this.gestionToolStripMenuItem.Text = "Administrar";
            //
            // grpUsuarios — submenú "Usuarios": ABM de datos + cuentas + historial de cambios.
            //
            this.grpUsuarios.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.adminUsuariosItem,
            this.usuariosToolStripMenuItem,
            this.historialUsuariosToolStripMenuItem});
            this.grpUsuarios.Name = "grpUsuarios";
            this.grpUsuarios.Tag  = "mnu.grp.usuarios";
            this.grpUsuarios.Text = "Usuarios";
            //
            // adminUsuariosItem — ABM de datos no sensibles + cambiar rol + historial.
            //
            this.adminUsuariosItem.Name = "adminUsuariosItem";
            this.adminUsuariosItem.Tag  = "mnu.adminusuarios";
            this.adminUsuariosItem.Text = "Administración de Usuarios";
            this.adminUsuariosItem.Click += new System.EventHandler(this.AdminUsuarios_Click);
            //
            // usuariosToolStripMenuItem — pasa a llamarse "Cuentas de Usuario" (no confundir con
            // "Administración de Usuarios").
            //
            this.usuariosToolStripMenuItem.Name = "usuariosToolStripMenuItem";
            this.usuariosToolStripMenuItem.Tag = "mnu.cuentas";
            this.usuariosToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.usuariosToolStripMenuItem.Text = "Cuentas de Usuario";
            this.usuariosToolStripMenuItem.Click += new System.EventHandler(this.usuariosToolStripMenuItem_Click);
            //
            // sepAdministrarToolStripSeparator
            //
            this.sepAdministrarToolStripSeparator.Name = "sepAdministrarToolStripSeparator";
            //
            // grpSistema — submenú "Sistema": herramientas transversales.
            //
            this.grpSistema.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.idiomasToolStripMenuItem,
            this.backupToolStripMenuItem,
            this.integridadToolStripMenuItem});
            this.grpSistema.Name = "grpSistema";
            this.grpSistema.Tag  = "mnu.grp.sistema";
            this.grpSistema.Text = "Sistema";
            //
            // perfilesToolStripMenuItem
            //
            this.perfilesToolStripMenuItem.Name = "perfilesToolStripMenuItem";
            this.perfilesToolStripMenuItem.Tag = "mnu.perfiles";
            this.perfilesToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.perfilesToolStripMenuItem.Text = "Perfiles y Permisos";
            this.perfilesToolStripMenuItem.Click += new System.EventHandler(this.perfilesToolStripMenuItem_Click);
            //
            // idiomasToolStripMenuItem
            //
            this.idiomasToolStripMenuItem.Name = "idiomasToolStripMenuItem";
            this.idiomasToolStripMenuItem.Tag = "mnu.idiomas";
            this.idiomasToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.idiomasToolStripMenuItem.Text = "Gestión de Idiomas";
            this.idiomasToolStripMenuItem.Click += new System.EventHandler(this.idiomasToolStripMenuItem_Click);
            //
            // historialUsuariosToolStripMenuItem
            //
            this.historialUsuariosToolStripMenuItem.Name = "historialUsuariosToolStripMenuItem";
            this.historialUsuariosToolStripMenuItem.Tag  = "mnu.historialusr";
            this.historialUsuariosToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.historialUsuariosToolStripMenuItem.Text = "Historial de Cambios";
            this.historialUsuariosToolStripMenuItem.Click += new System.EventHandler(this.historialUsuariosToolStripMenuItem_Click);
            //
            // backupToolStripMenuItem
            //
            this.backupToolStripMenuItem.Name = "backupToolStripMenuItem";
            this.backupToolStripMenuItem.Tag  = "mnu.backup";
            this.backupToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.backupToolStripMenuItem.Text = "Backup y Restauración";
            this.backupToolStripMenuItem.Click += new System.EventHandler(this.backupToolStripMenuItem_Click);
            //
            // integridadToolStripMenuItem
            //
            this.integridadToolStripMenuItem.Name = "integridadToolStripMenuItem";
            this.integridadToolStripMenuItem.Tag  = "mnu.integridad";
            this.integridadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.integridadToolStripMenuItem.Text = "Diagnóstico de Integridad";
            this.integridadToolStripMenuItem.Click += new System.EventHandler(this.integridadToolStripMenuItem_Click);
            //
            // bitacoraToolStripMenuItem
            //
            this.bitacoraToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bitSistemaToolStripMenuItem,
            this.bitNegocioToolStripMenuItem,
            this.sepBitacoraToolStripSeparator,
            this.reporteJornadaToolStripMenuItem,
            this.analisisAbandonoToolStripMenuItem});
            this.bitacoraToolStripMenuItem.Name = "bitacoraToolStripMenuItem";
            this.bitacoraToolStripMenuItem.Tag = "mnu.bitacora";
            this.bitacoraToolStripMenuItem.Size = new System.Drawing.Size(70, 20);
            // Se renombra a "Analítica": agrupa Bitácora con los reportes del Bloque 3 (Idea de
            // Negocio) a medida que se implementen — misma clave de traducción "mnu.bitacora",
            // solo cambia el texto que representa (ver rediseño UX/UI, sección 10).
            this.bitacoraToolStripMenuItem.Text = "Analítica";
            //
            // bitSistemaToolStripMenuItem
            //
            this.bitSistemaToolStripMenuItem.Name = "bitSistemaToolStripMenuItem";
            this.bitSistemaToolStripMenuItem.Tag = "mnu.bitacora.sistema";
            this.bitSistemaToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.bitSistemaToolStripMenuItem.Text = "🔐  Bitácora del Sistema";
            this.bitSistemaToolStripMenuItem.Click += new System.EventHandler(this.bitSistemaToolStripMenuItem_Click);
            //
            // bitNegocioToolStripMenuItem
            //
            this.bitNegocioToolStripMenuItem.Name = "bitNegocioToolStripMenuItem";
            this.bitNegocioToolStripMenuItem.Tag = "mnu.bitacora.negocio";
            this.bitNegocioToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.bitNegocioToolStripMenuItem.Text = "📦  Bitácora de Negocio";
            this.bitNegocioToolStripMenuItem.Click += new System.EventHandler(this.bitNegocioToolStripMenuItem_Click);
            //
            // sepBitacoraToolStripSeparator
            //
            this.sepBitacoraToolStripSeparator.Name = "sepBitacoraToolStripSeparator";
            this.sepBitacoraToolStripSeparator.Size = new System.Drawing.Size(197, 6);
            //
            // reporteJornadaToolStripMenuItem
            //
            this.reporteJornadaToolStripMenuItem.Name = "reporteJornadaToolStripMenuItem";
            this.reporteJornadaToolStripMenuItem.Tag = "mnu.reportejornada";
            this.reporteJornadaToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.reporteJornadaToolStripMenuItem.Text = "📊  Reporte de Jornada";
            this.reporteJornadaToolStripMenuItem.Click += new System.EventHandler(this.reporteJornadaToolStripMenuItem_Click);
            //
            // analisisAbandonoToolStripMenuItem
            //
            this.analisisAbandonoToolStripMenuItem.Name = "analisisAbandonoToolStripMenuItem";
            this.analisisAbandonoToolStripMenuItem.Tag = "mnu.abandono";
            this.analisisAbandonoToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.analisisAbandonoToolStripMenuItem.Text = "📉  Análisis de Abandono";
            this.analisisAbandonoToolStripMenuItem.Click += new System.EventHandler(this.analisisAbandonoToolStripMenuItem_Click);
            //
            // ventanaToolStripMenuItem
            //
            // Lista nativa de ventanas MDI abiertas (hallazgo #2 del rediseño UX/UI: hoy no hay
            // forma de ver/ordenar las pantallas abiertas más que buscarlas a mano).
            this.ventanaToolStripMenuItem.Name = "ventanaToolStripMenuItem";
            this.ventanaToolStripMenuItem.Tag = "mnu.ventana";
            this.ventanaToolStripMenuItem.Size = new System.Drawing.Size(66, 20);
            this.ventanaToolStripMenuItem.Text = "Ventanas Abiertas";
            this.menuStrip1.MdiWindowListItem = this.ventanaToolStripMenuItem;
            //
            // alertasItem — Centro de Alertas, alineado a la derecha (badge se compone en código).
            //
            this.alertasItem.Name      = "alertasItem";
            this.alertasItem.Tag       = "mnu.alertas";
            this.alertasItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.alertasItem.Click    += new System.EventHandler(this.AlertasItem_Click);
            //
            // Menu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(228)))), ((int)(((byte)(235)))));
            this.ClientSize = new System.Drawing.Size(1100, 650);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.tsIdioma);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Menu";
            this.Text = "WardrobeFlow";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.tsIdioma.ResumeLayout(false);
            this.tsIdioma.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem usuarioToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cerrarSesionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem panelControlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem inventarioToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem prendasToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem backupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem historialUsuariosToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem outfitsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem categoriasToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bitacoraToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bitSistemaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bitNegocioToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator sepBitacoraToolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem reporteJornadaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem analisisAbandonoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gestionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem usuariosToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem perfilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem suscriptoresToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ventasToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clientesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem planesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renovacionSuscripcionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cobroSuscripcionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pedidosVentaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pedidosRealizadosToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem idiomasToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem integridadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ventanaToolStripMenuItem;
        private System.Windows.Forms.ToolStrip tsIdioma;
        private System.Windows.Forms.ToolStripLabel lblIdioma;
        private System.Windows.Forms.ToolStripSeparator tsIdiomaSep;
        private System.Windows.Forms.ToolStripComboBox cmbIdiomaMenu;
        private System.Windows.Forms.ToolStripMenuItem miPerfilItem;
        private System.Windows.Forms.ToolStripMenuItem adminUsuariosItem;
        private System.Windows.Forms.ToolStripMenuItem grpUsuarios;
        private System.Windows.Forms.ToolStripMenuItem grpSistema;
        private System.Windows.Forms.ToolStripSeparator sepAdministrarToolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem alertasItem;
    }
}