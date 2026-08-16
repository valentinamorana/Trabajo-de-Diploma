namespace GUI
{
    partial class Login
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login));
            this.pnlHeader          = new System.Windows.Forms.Panel();
            this.picLogo            = new System.Windows.Forms.PictureBox();
            this.lblTitle           = new System.Windows.Forms.Label();
            this.lblSubtitulo       = new System.Windows.Forms.Label();
            this.cmbIdioma          = new System.Windows.Forms.ComboBox();
            this.pnlHeaderDivider   = new System.Windows.Forms.Panel();
            this.btnMinimizar       = new System.Windows.Forms.Button();
            this.btnCerrarVentana   = new System.Windows.Forms.Button();
            this.pnlLeft            = new System.Windows.Forms.Panel();
            this.lblTagline         = new System.Windows.Forms.Label();
            this.lblWordmarkDark    = new System.Windows.Forms.Label();
            this.lblWordmarkVino    = new System.Windows.Forms.Label();
            this.lblBrandDesc       = new System.Windows.Forms.Label();
            this.picClothingRack    = new System.Windows.Forms.PictureBox();
            this.picPriceTag        = new System.Windows.Forms.PictureBox();
            this.picPlantAndBag     = new System.Windows.Forms.PictureBox();
            this.pnlCardShadow      = new System.Windows.Forms.Panel();
            this.pnlCard            = new System.Windows.Forms.Panel();
            this.lblAccent          = new System.Windows.Forms.Label();
            this.pnlAccentRule      = new System.Windows.Forms.Panel();
            this.lblLoginSub        = new System.Windows.Forms.Label();
            this.lblUsuario         = new System.Windows.Forms.Label();
            this.pnlUsuarioBox      = new System.Windows.Forms.Panel();
            this.txtUsuario         = new System.Windows.Forms.TextBox();
            this.lblIconoUsuario    = new System.Windows.Forms.Label();
            this.lblContraseña      = new System.Windows.Forms.Label();
            this.pnlContraseñaBox   = new System.Windows.Forms.Panel();
            this.txtContraseña      = new System.Windows.Forms.TextBox();
            this.lblIconoContraseña = new System.Windows.Forms.Label();
            this.btnMostrarClave    = new System.Windows.Forms.Button();
            this.lnkOlvidaste       = new System.Windows.Forms.LinkLabel();
            this.lnkEmergencia      = new System.Windows.Forms.LinkLabel();
            this.lblError           = new System.Windows.Forms.Label();
            this.btnIngresar        = new System.Windows.Forms.Button();
            this.lblDivider         = new System.Windows.Forms.Label();
            this.btnSalir           = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picClothingRack)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPriceTag)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPlantAndBag)).BeginInit();
            this.pnlHeader.SuspendLayout();
            this.pnlLeft.SuspendLayout();
            this.pnlCard.SuspendLayout();
            this.pnlUsuarioBox.SuspendLayout();
            this.pnlContraseñaBox.SuspendLayout();
            this.SuspendLayout();

            // ── pnlHeader — franja superior de ancho completo (1000 × 64) ──
            // Marca + selector de idioma + controles de ventana (minimizar/cerrar) dibujados a
            // medida, porque el form usa FormBorderStyle.None (ver comentario más abajo).
            this.pnlHeader.BackColor = GUI.Tema.Papel;
            this.pnlHeader.Controls.Add(this.picLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitulo);
            this.pnlHeader.Controls.Add(this.cmbIdioma);
            this.pnlHeader.Controls.Add(this.pnlHeaderDivider);
            this.pnlHeader.Controls.Add(this.btnMinimizar);
            this.pnlHeader.Controls.Add(this.btnCerrarVentana);
            this.pnlHeader.Location  = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name      = "pnlHeader";
            this.pnlHeader.Size      = new System.Drawing.Size(1000, 64);
            this.pnlHeader.TabIndex  = 20;
            this.pnlHeader.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlHeader_MouseDown);

            // ── picLogo — isotipo + wordmark "WardrobeFlow" (asset 01, transparente) ──
            // Reemplaza visualmente el ícono de percha dibujado a mano + lblTitle (que se
            // oculta, no se elimina, para no perder el control ni su Tag/eventos futuros).
            this.picLogo.BackColor  = GUI.Tema.Papel;
            this.picLogo.Image      = global::GUI.Properties.Resources.login_logo;
            this.picLogo.Location   = new System.Drawing.Point(28, 12);
            this.picLogo.Name       = "picLogo";
            this.picLogo.Size       = new System.Drawing.Size(175, 40);
            this.picLogo.SizeMode   = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex   = 22;
            this.picLogo.TabStop    = false;
            this.picLogo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlHeader_MouseDown);

            // ── lblTitle — nombre de marca junto al ícono (oculto: lo muestra picLogo) ─
            this.lblTitle.AutoSize  = false;
            this.lblTitle.BackColor = GUI.Tema.Papel;
            this.lblTitle.Font      = new System.Drawing.Font("Segoe UI", 11.5F);
            this.lblTitle.ForeColor = GUI.Tema.Tinta;
            this.lblTitle.Location  = new System.Drawing.Point(70, 10);
            this.lblTitle.Name      = "lblTitle";
            this.lblTitle.Size      = new System.Drawing.Size(190, 20);
            this.lblTitle.TabIndex  = 11;
            this.lblTitle.Text      = "WardrobeFlow";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTitle.Visible   = false;

            // ── lblSubtitulo — "PORTAL DE EMPLEADOS" ─────────────────────
            this.lblSubtitulo.AutoSize  = false;
            this.lblSubtitulo.BackColor = GUI.Tema.Papel;
            this.lblSubtitulo.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblSubtitulo.ForeColor = GUI.Tema.TextoMuted;
            this.lblSubtitulo.Location  = new System.Drawing.Point(219, 24);
            this.lblSubtitulo.Name      = "lblSubtitulo";
            this.lblSubtitulo.Size      = new System.Drawing.Size(230, 18);
            this.lblSubtitulo.TabIndex  = 12;
            this.lblSubtitulo.Tag       = "lbl.subtitulo";
            this.lblSubtitulo.Text      = "PORTAL DE EMPLEADOS";
            this.lblSubtitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSubtitulo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlHeader_MouseDown);

            // ── cmbIdioma — selector de idioma (idiomas dinámicos de BD) ──────
            this.cmbIdioma.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbIdioma.FlatStyle     = System.Windows.Forms.FlatStyle.Flat;
            this.cmbIdioma.Font          = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbIdioma.ForeColor     = GUI.Tema.RosaOscuro;
            this.cmbIdioma.BackColor     = GUI.Tema.Papel;
            this.cmbIdioma.Location      = new System.Drawing.Point(729, 19);
            this.cmbIdioma.Name          = "cmbIdioma";
            this.cmbIdioma.Size          = new System.Drawing.Size(150, 26);
            this.cmbIdioma.TabIndex      = 8;
            this.cmbIdioma.TabStop       = false;
            this.cmbIdioma.SelectedIndexChanged += new System.EventHandler(this.CmbIdioma_SelectedIndexChanged);

            // ── pnlHeaderDivider — línea vertical entre el idioma y minimizar/cerrar ──
            this.pnlHeaderDivider.BackColor = GUI.Tema.Borde;
            this.pnlHeaderDivider.Location  = new System.Drawing.Point(895, 18);
            this.pnlHeaderDivider.Name      = "pnlHeaderDivider";
            this.pnlHeaderDivider.Size      = new System.Drawing.Size(1, 28);
            this.pnlHeaderDivider.TabIndex  = 26;

            // ── btnMinimizar — minimiza la ventana (form sin borde nativo) ──
            this.btnMinimizar.BackColor                 = GUI.Tema.Papel;
            this.btnMinimizar.Cursor                    = System.Windows.Forms.Cursors.Hand;
            this.btnMinimizar.FlatAppearance.BorderSize = 0;
            this.btnMinimizar.FlatAppearance.MouseOverBackColor = GUI.Tema.RosaPalido;
            this.btnMinimizar.FlatStyle                 = System.Windows.Forms.FlatStyle.Flat;
            this.btnMinimizar.Font                      = new System.Drawing.Font("Segoe UI", 12F);
            this.btnMinimizar.ForeColor                 = GUI.Tema.TextoMuted;
            this.btnMinimizar.Location                  = new System.Drawing.Point(912, 18);
            this.btnMinimizar.Name                      = "btnMinimizar";
            this.btnMinimizar.Size                      = new System.Drawing.Size(28, 28);
            this.btnMinimizar.TabIndex                  = 27;
            this.btnMinimizar.TabStop                   = false;
            this.btnMinimizar.Text                      = "–";
            this.btnMinimizar.UseVisualStyleBackColor    = false;
            this.btnMinimizar.Click += new System.EventHandler(this.btnMinimizar_Click);

            // ── btnCerrarVentana — cierra el Login (mismo efecto que la X nativa: ──
            // DialogResult.Cancel, ver Login.cs). No reemplaza a btnSalir (RF: Salir sigue
            // llamando Application.Exit()); esto solo repone el control de ventana nativo
            // que se pierde al usar FormBorderStyle.None.
            this.btnCerrarVentana.BackColor                 = GUI.Tema.Papel;
            this.btnCerrarVentana.Cursor                    = System.Windows.Forms.Cursors.Hand;
            this.btnCerrarVentana.FlatAppearance.BorderSize = 0;
            this.btnCerrarVentana.FlatAppearance.MouseOverBackColor = GUI.Tema.RosaPalido;
            this.btnCerrarVentana.FlatStyle                 = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrarVentana.Font                      = new System.Drawing.Font("Segoe UI", 10.5F);
            this.btnCerrarVentana.ForeColor                 = GUI.Tema.TextoMuted;
            this.btnCerrarVentana.Location                  = new System.Drawing.Point(944, 18);
            this.btnCerrarVentana.Name                      = "btnCerrarVentana";
            this.btnCerrarVentana.Size                      = new System.Drawing.Size(28, 28);
            this.btnCerrarVentana.TabIndex                  = 28;
            this.btnCerrarVentana.TabStop                   = false;
            this.btnCerrarVentana.Text                      = "✕";
            this.btnCerrarVentana.UseVisualStyleBackColor    = false;
            this.btnCerrarVentana.Click += new System.EventHandler(this.btnCerrarVentana_Click);

            // ── pnlLeft — panel de branding izquierdo (420 × 696) ──────────
            // Fondo rosa pálido (Tema.RosaPalido); ilustración vía PictureBox (assets 03/04/05,
            // transparentes), textura sutil de fondo vía Paint en Login.cs.
            this.pnlLeft.BackColor = GUI.Tema.RosaPalido;
            this.pnlLeft.Controls.Add(this.lblTagline);
            this.pnlLeft.Controls.Add(this.lblWordmarkDark);
            this.pnlLeft.Controls.Add(this.lblWordmarkVino);
            this.pnlLeft.Controls.Add(this.lblBrandDesc);
            this.pnlLeft.Controls.Add(this.picClothingRack);
            this.pnlLeft.Controls.Add(this.picPriceTag);
            this.pnlLeft.Controls.Add(this.picPlantAndBag);
            this.pnlLeft.Location  = new System.Drawing.Point(0, 64);
            this.pnlLeft.Name      = "pnlLeft";
            this.pnlLeft.Size      = new System.Drawing.Size(420, 696);
            this.pnlLeft.TabIndex  = 21;
            this.pnlLeft.Paint    += new System.Windows.Forms.PaintEventHandler(this.PnlLeft_Paint);

            // ── lblTagline — "ORGANIZÁ • GESTIONÁ • POTENCIÁ" (traducible vía Tag) ──
            this.lblTagline.BackColor = GUI.Tema.RosaPalido;
            this.lblTagline.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTagline.ForeColor = GUI.Tema.RosaOscuro;
            this.lblTagline.Location  = new System.Drawing.Point(32, 76);
            this.lblTagline.Name      = "lblTagline";
            this.lblTagline.Size      = new System.Drawing.Size(340, 20);
            this.lblTagline.TabIndex  = 30;
            this.lblTagline.Tag       = "lbl.tagline";
            this.lblTagline.Text      = "ORGANIZÁ • GESTIONÁ • POTENCIÁ";

            // ── lblWordmarkDark / lblWordmarkVino — "Wardrobe" / "Flow" en dos colores ──
            this.lblWordmarkDark.AutoSize  = false;
            this.lblWordmarkDark.BackColor = GUI.Tema.RosaPalido;
            this.lblWordmarkDark.Font      = new System.Drawing.Font("Segoe UI", 34F);
            this.lblWordmarkDark.ForeColor = GUI.Tema.Tinta;
            this.lblWordmarkDark.Location  = new System.Drawing.Point(28, 112);
            this.lblWordmarkDark.Name      = "lblWordmarkDark";
            this.lblWordmarkDark.Size      = new System.Drawing.Size(340, 58);
            this.lblWordmarkDark.TabIndex  = 31;
            this.lblWordmarkDark.Text      = "Wardrobe";

            this.lblWordmarkVino.AutoSize  = false;
            this.lblWordmarkVino.BackColor = GUI.Tema.RosaPalido;
            this.lblWordmarkVino.Font      = new System.Drawing.Font("Segoe UI", 34F);
            this.lblWordmarkVino.ForeColor = GUI.Tema.RosaOscuro;
            this.lblWordmarkVino.Location  = new System.Drawing.Point(28, 172);
            this.lblWordmarkVino.Name      = "lblWordmarkVino";
            this.lblWordmarkVino.Size      = new System.Drawing.Size(340, 58);
            this.lblWordmarkVino.TabIndex  = 32;
            this.lblWordmarkVino.Text      = "Flow";

            // ── lblBrandDesc — descripción de marca (traducible vía Tag) ──────
            this.lblBrandDesc.AutoSize  = false;
            this.lblBrandDesc.BackColor = GUI.Tema.RosaPalido;
            this.lblBrandDesc.Font      = new System.Drawing.Font("Segoe UI", 11F);
            this.lblBrandDesc.ForeColor = GUI.Tema.TextoMuted;
            this.lblBrandDesc.Location  = new System.Drawing.Point(32, 246);
            this.lblBrandDesc.Name      = "lblBrandDesc";
            this.lblBrandDesc.Size      = new System.Drawing.Size(340, 66);
            this.lblBrandDesc.TabIndex  = 33;
            this.lblBrandDesc.Tag       = "lbl.brand.desc";
            this.lblBrandDesc.Text      = "Gestioná tu flujo de\r\nprendas e información\r\nde manera simple y eficiente.";

            // ── picClothingRack — asset 03, percha con prendas ──────────────
            this.picClothingRack.BackColor = GUI.Tema.RosaPalido;
            this.picClothingRack.Image     = global::GUI.Properties.Resources.login_clothing_rack;
            this.picClothingRack.Location  = new System.Drawing.Point(14, 330);
            this.picClothingRack.Name      = "picClothingRack";
            this.picClothingRack.Size      = new System.Drawing.Size(230, 268);
            this.picClothingRack.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picClothingRack.TabIndex  = 23;
            this.picClothingRack.TabStop   = false;

            // ── picPriceTag — asset 04, cartel de precio ────────────────────
            this.picPriceTag.BackColor = GUI.Tema.RosaPalido;
            this.picPriceTag.Image     = global::GUI.Properties.Resources.login_price_tag;
            this.picPriceTag.Location  = new System.Drawing.Point(266, 330);
            this.picPriceTag.Name      = "picPriceTag";
            this.picPriceTag.Size      = new System.Drawing.Size(80, 124);
            this.picPriceTag.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPriceTag.TabIndex  = 24;
            this.picPriceTag.TabStop   = false;

            // ── picPlantAndBag — asset 05, planta + bolso ───────────────────
            this.picPlantAndBag.BackColor = GUI.Tema.RosaPalido;
            this.picPlantAndBag.Image     = global::GUI.Properties.Resources.login_plant_and_bag;
            this.picPlantAndBag.Location  = new System.Drawing.Point(258, 460);
            this.picPlantAndBag.Name      = "picPlantAndBag";
            this.picPlantAndBag.Size      = new System.Drawing.Size(104, 198);
            this.picPlantAndBag.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPlantAndBag.TabIndex  = 25;
            this.picPlantAndBag.TabStop   = false;

            // ── pnlCardShadow — sombra suave detrás de la card (capas dibujadas en Login.cs) ─
            this.pnlCardShadow.BackColor = GUI.Tema.RosaPalido;
            this.pnlCardShadow.Location  = new System.Drawing.Point(440, 92);
            this.pnlCardShadow.Name      = "pnlCardShadow";
            this.pnlCardShadow.Size      = new System.Drawing.Size(540, 648);
            this.pnlCardShadow.TabIndex  = 29;
            this.pnlCardShadow.Paint    += new System.Windows.Forms.PaintEventHandler(this.PnlCardShadow_Paint);

            // ── pnlCard — card blanca flotante con el formulario (500 × 612) ──
            // Esquinas redondeadas vía Region (Login.cs, radio Tema.RadioCard).
            this.pnlCard.BackColor = GUI.Tema.Papel;
            this.pnlCard.Controls.Add(this.lblAccent);
            this.pnlCard.Controls.Add(this.pnlAccentRule);
            this.pnlCard.Controls.Add(this.lblLoginSub);
            this.pnlCard.Controls.Add(this.lblUsuario);
            this.pnlCard.Controls.Add(this.pnlUsuarioBox);
            this.pnlCard.Controls.Add(this.lblContraseña);
            this.pnlCard.Controls.Add(this.pnlContraseñaBox);
            this.pnlCard.Controls.Add(this.lnkOlvidaste);
            this.pnlCard.Controls.Add(this.lnkEmergencia);
            this.pnlCard.Controls.Add(this.lblError);
            this.pnlCard.Controls.Add(this.btnIngresar);
            this.pnlCard.Controls.Add(this.lblDivider);
            this.pnlCard.Controls.Add(this.btnSalir);
            this.pnlCard.Location = new System.Drawing.Point(460, 104);
            this.pnlCard.Name     = "pnlCard";
            this.pnlCard.Size     = new System.Drawing.Size(500, 612);
            this.pnlCard.TabIndex = 13;

            // ── lblAccent — "Bienvenido / de nuevo" (dos líneas, como el mock) ──
            this.lblAccent.AutoSize  = false;
            this.lblAccent.BackColor = System.Drawing.Color.Transparent;
            this.lblAccent.Font      = new System.Drawing.Font("Segoe UI", 28F);
            this.lblAccent.ForeColor = GUI.Tema.Tinta;
            this.lblAccent.Location  = new System.Drawing.Point(48, 52);
            this.lblAccent.Name      = "lblAccent";
            this.lblAccent.Size      = new System.Drawing.Size(404, 84);
            this.lblAccent.TabIndex  = 10;
            this.lblAccent.Tag       = "lbl.bienvenido";
            this.lblAccent.Text      = "Bienvenido\r\nde nuevo";
            this.lblAccent.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // ── pnlAccentRule — rayita decorativa bajo "Bienvenido de nuevo" ───
            this.pnlAccentRule.BackColor = GUI.Tema.RosaOscuro;
            this.pnlAccentRule.Location  = new System.Drawing.Point(50, 140);
            this.pnlAccentRule.Name      = "pnlAccentRule";
            this.pnlAccentRule.Size      = new System.Drawing.Size(44, 3);
            this.pnlAccentRule.TabIndex  = 14;

            // ── lblLoginSub — subtítulo bajo el acento ─────────────────────
            this.lblLoginSub.AutoSize  = false;
            this.lblLoginSub.BackColor = System.Drawing.Color.Transparent;
            this.lblLoginSub.Font      = new System.Drawing.Font("Segoe UI", 10.5F);
            this.lblLoginSub.ForeColor = GUI.Tema.TextoMuted;
            this.lblLoginSub.Location  = new System.Drawing.Point(48, 156);
            this.lblLoginSub.Name      = "lblLoginSub";
            this.lblLoginSub.Size      = new System.Drawing.Size(404, 22);
            this.lblLoginSub.TabIndex  = 15;
            this.lblLoginSub.Tag       = "lbl.credenciales";
            this.lblLoginSub.Text      = "Ingresá para continuar en tu cuenta.";

            // ── lblUsuario — etiqueta "USUARIO" (uppercase small) ──────────
            this.lblUsuario.AutoSize  = false;
            this.lblUsuario.BackColor = System.Drawing.Color.Transparent;
            this.lblUsuario.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUsuario.ForeColor = GUI.Tema.TextoMuted;
            this.lblUsuario.Location  = new System.Drawing.Point(48, 206);
            this.lblUsuario.Name      = "lblUsuario";
            this.lblUsuario.Size      = new System.Drawing.Size(404, 16);
            this.lblUsuario.TabIndex  = 0;
            this.lblUsuario.Tag       = "lbl.usuario";
            this.lblUsuario.Text      = "USUARIO";
            this.lblUsuario.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // ── pnlUsuarioBox — caja con borde propio (Paint) que aloja ícono + textbox ─
            this.pnlUsuarioBox.BackColor = GUI.Tema.Papel;
            this.pnlUsuarioBox.Controls.Add(this.txtUsuario);
            this.pnlUsuarioBox.Controls.Add(this.lblIconoUsuario);
            this.pnlUsuarioBox.Location  = new System.Drawing.Point(48, 226);
            this.pnlUsuarioBox.Name      = "pnlUsuarioBox";
            this.pnlUsuarioBox.Size      = new System.Drawing.Size(404, 44);
            this.pnlUsuarioBox.TabIndex  = 1;
            this.pnlUsuarioBox.Paint    += new System.Windows.Forms.PaintEventHandler(this.DibujarBordeCampo);

            // ── txtUsuario ─────────────────────────────────────────────────
            this.txtUsuario.BackColor   = GUI.Tema.Papel;
            this.txtUsuario.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUsuario.Font        = new System.Drawing.Font("Segoe UI", 11.5F);
            this.txtUsuario.ForeColor   = GUI.Tema.Tinta;
            this.txtUsuario.Location    = new System.Drawing.Point(42, 13);
            this.txtUsuario.MaxLength   = 50;
            this.txtUsuario.Name        = "txtUsuario";
            this.txtUsuario.Size        = new System.Drawing.Size(352, 22);
            this.txtUsuario.TabIndex    = 1;

            // ── lblIconoUsuario — ícono de persona, no interactivo ──────────
            this.lblIconoUsuario.BackColor  = GUI.Tema.Papel;
            this.lblIconoUsuario.Font       = new System.Drawing.Font("Segoe UI Emoji", 11F);
            this.lblIconoUsuario.ForeColor  = GUI.Tema.TextoMuted;
            this.lblIconoUsuario.Location   = new System.Drawing.Point(10, 10);
            this.lblIconoUsuario.Name       = "lblIconoUsuario";
            this.lblIconoUsuario.Size       = new System.Drawing.Size(26, 26);
            this.lblIconoUsuario.TabIndex   = 34;
            this.lblIconoUsuario.Text       = "👤";
            this.lblIconoUsuario.TextAlign  = System.Drawing.ContentAlignment.MiddleCenter;

            // ── lblContraseña — etiqueta "CONTRASEÑA" ─────────────────────
            this.lblContraseña.AutoSize  = false;
            this.lblContraseña.BackColor = System.Drawing.Color.Transparent;
            this.lblContraseña.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblContraseña.ForeColor = GUI.Tema.TextoMuted;
            this.lblContraseña.Location  = new System.Drawing.Point(48, 282);
            this.lblContraseña.Name      = "lblContraseña";
            this.lblContraseña.Size      = new System.Drawing.Size(404, 16);
            this.lblContraseña.TabIndex  = 2;
            this.lblContraseña.Tag       = "lbl.contrasena";
            this.lblContraseña.Text      = "CONTRASEÑA";
            this.lblContraseña.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // ── pnlContraseñaBox ───────────────────────────────────────────
            this.pnlContraseñaBox.BackColor = GUI.Tema.Papel;
            this.pnlContraseñaBox.Controls.Add(this.txtContraseña);
            this.pnlContraseñaBox.Controls.Add(this.lblIconoContraseña);
            this.pnlContraseñaBox.Controls.Add(this.btnMostrarClave);
            this.pnlContraseñaBox.Location  = new System.Drawing.Point(48, 302);
            this.pnlContraseñaBox.Name      = "pnlContraseñaBox";
            this.pnlContraseñaBox.Size      = new System.Drawing.Size(404, 44);
            this.pnlContraseñaBox.TabIndex  = 3;
            this.pnlContraseñaBox.Paint    += new System.Windows.Forms.PaintEventHandler(this.DibujarBordeCampo);

            // ── txtContraseña ──────────────────────────────────────────────
            // Ancho recortado (352 − 24) para dejarle lugar al botón del ojo en el borde derecho.
            this.txtContraseña.BackColor    = GUI.Tema.Papel;
            this.txtContraseña.BorderStyle  = System.Windows.Forms.BorderStyle.None;
            this.txtContraseña.Font         = new System.Drawing.Font("Segoe UI", 11.5F);
            this.txtContraseña.ForeColor    = GUI.Tema.Tinta;
            this.txtContraseña.Location     = new System.Drawing.Point(42, 13);
            this.txtContraseña.MaxLength    = 100;
            this.txtContraseña.Name         = "txtContraseña";
            this.txtContraseña.PasswordChar = '●';
            this.txtContraseña.Size         = new System.Drawing.Size(328, 22);
            this.txtContraseña.TabIndex     = 3;

            // ── lblIconoContraseña — ícono de candado, no interactivo ───────
            this.lblIconoContraseña.BackColor  = GUI.Tema.Papel;
            this.lblIconoContraseña.Font       = new System.Drawing.Font("Segoe UI Emoji", 11F);
            this.lblIconoContraseña.ForeColor  = GUI.Tema.TextoMuted;
            this.lblIconoContraseña.Location   = new System.Drawing.Point(10, 10);
            this.lblIconoContraseña.Name       = "lblIconoContraseña";
            this.lblIconoContraseña.Size       = new System.Drawing.Size(26, 26);
            this.lblIconoContraseña.TabIndex   = 35;
            this.lblIconoContraseña.Text       = "🔒";
            this.lblIconoContraseña.TextAlign  = System.Drawing.ContentAlignment.MiddleCenter;

            // ── btnMostrarClave — ojito mostrar/ocultar contraseña ──────────
            this.btnMostrarClave.BackColor                 = GUI.Tema.Papel;
            this.btnMostrarClave.Cursor                    = System.Windows.Forms.Cursors.Hand;
            this.btnMostrarClave.FlatAppearance.BorderSize = 0;
            this.btnMostrarClave.FlatStyle                 = System.Windows.Forms.FlatStyle.Flat;
            this.btnMostrarClave.Font                      = new System.Drawing.Font("Segoe UI Emoji", 9F);
            this.btnMostrarClave.ForeColor                 = GUI.Tema.TextoMuted;
            this.btnMostrarClave.Location                  = new System.Drawing.Point(372, 11);
            this.btnMostrarClave.Name                      = "btnMostrarClave";
            this.btnMostrarClave.Size                      = new System.Drawing.Size(24, 26);
            this.btnMostrarClave.TabIndex                  = 36;
            this.btnMostrarClave.TabStop                   = false;
            this.btnMostrarClave.Text                      = "👁";
            this.btnMostrarClave.UseVisualStyleBackColor    = false;
            this.btnMostrarClave.Click += new System.EventHandler(this.btnMostrarClave_Click);

            // ── lnkOlvidaste — link alineado a la derecha bajo contraseña ─
            this.lnkOlvidaste.ActiveLinkColor = GUI.Tema.RosaOscuro;
            this.lnkOlvidaste.AutoSize        = false;
            this.lnkOlvidaste.BackColor       = System.Drawing.Color.Transparent;
            this.lnkOlvidaste.Font            = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lnkOlvidaste.LinkColor       = GUI.Tema.RosaOscuro;
            this.lnkOlvidaste.Location        = new System.Drawing.Point(48, 352);
            this.lnkOlvidaste.Name            = "lnkOlvidaste";
            this.lnkOlvidaste.Size            = new System.Drawing.Size(404, 18);
            this.lnkOlvidaste.TabIndex        = 6;
            this.lnkOlvidaste.TabStop         = true;
            this.lnkOlvidaste.Tag             = "lnk.olvide";
            this.lnkOlvidaste.Text            = "¿Olvidaste tu contraseña?";
            this.lnkOlvidaste.TextAlign       = System.Drawing.ContentAlignment.MiddleRight;
            this.lnkOlvidaste.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkOlvidaste_LinkClicked);

            // ── lnkEmergencia — RF-10, autodesbloqueo con clave de emergencia ─
            // Fila propia entre "¿Olvidaste tu contraseña?" y el cartel de error, para que
            // NO se superponga con el mensaje "cuenta bloqueada".
            this.lnkEmergencia.ActiveLinkColor = GUI.Tema.RosaOscuro;
            this.lnkEmergencia.AutoSize        = false;
            this.lnkEmergencia.BackColor       = System.Drawing.Color.Transparent;
            this.lnkEmergencia.Font            = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lnkEmergencia.LinkColor       = GUI.Tema.RosaOscuro;
            this.lnkEmergencia.Location        = new System.Drawing.Point(48, 374);
            this.lnkEmergencia.Name            = "lnkEmergencia";
            this.lnkEmergencia.Size            = new System.Drawing.Size(404, 18);
            this.lnkEmergencia.TabIndex        = 7;
            this.lnkEmergencia.TabStop         = true;
            this.lnkEmergencia.Tag             = "emg.link";
            this.lnkEmergencia.Text            = "¿Cuenta bloqueada? Usar clave de emergencia";
            this.lnkEmergencia.TextAlign       = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkEmergencia.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkEmergencia_LinkClicked);

            // ── lblError ───────────────────────────────────────────────────
            this.lblError.AutoSize  = false;
            this.lblError.BackColor = GUI.Tema.Papel;
            this.lblError.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblError.ForeColor = GUI.Tema.Error;
            this.lblError.Location  = new System.Drawing.Point(48, 400);
            this.lblError.Name      = "lblError";
            this.lblError.Size      = new System.Drawing.Size(404, 34);
            this.lblError.TabIndex  = 4;
            this.lblError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // ── btnIngresar — botón primario ancho completo (vino), esquinas redondeadas ─
            this.btnIngresar.BackColor                 = GUI.Tema.RosaOscuro;
            this.btnIngresar.Cursor                    = System.Windows.Forms.Cursors.Hand;
            this.btnIngresar.FlatAppearance.BorderSize = 0;
            this.btnIngresar.FlatAppearance.MouseDownBackColor = System.Windows.Forms.ControlPaint.Dark(GUI.Tema.RosaOscuro, 0.2f);
            this.btnIngresar.FlatAppearance.MouseOverBackColor = System.Windows.Forms.ControlPaint.Light(GUI.Tema.RosaOscuro, 0.15f);
            this.btnIngresar.FlatStyle                 = System.Windows.Forms.FlatStyle.Flat;
            this.btnIngresar.Font                      = new System.Drawing.Font("Segoe UI", 12.5F, System.Drawing.FontStyle.Bold);
            this.btnIngresar.ForeColor                 = GUI.Tema.Papel;
            this.btnIngresar.Location                  = new System.Drawing.Point(48, 444);
            this.btnIngresar.Name                      = "btnIngresar";
            this.btnIngresar.Size                       = new System.Drawing.Size(404, 54);
            this.btnIngresar.TabIndex                  = 5;
            this.btnIngresar.Tag                        = "btn.ingresar";
            this.btnIngresar.Text                       = "INGRESAR";
            this.btnIngresar.UseVisualStyleBackColor    = false;
            this.btnIngresar.Click += new System.EventHandler(this.btnIngresar_Click);

            // ── lblDivider — separador "o" con líneas a los lados ─────────
            // Las líneas horizontales se dibujan vía Paint event en Login.cs.
            this.lblDivider.AutoSize  = false;
            this.lblDivider.BackColor = System.Drawing.Color.Transparent;
            this.lblDivider.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblDivider.ForeColor = GUI.Tema.TextoMuted;
            this.lblDivider.Location  = new System.Drawing.Point(48, 512);
            this.lblDivider.Name      = "lblDivider";
            this.lblDivider.Size      = new System.Drawing.Size(404, 24);
            this.lblDivider.TabIndex  = 9;
            this.lblDivider.Tag       = "lbl.divider";
            this.lblDivider.Text      = "o";
            this.lblDivider.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblDivider.Paint    += new System.Windows.Forms.PaintEventHandler(this.LblDivider_Paint);

            // ── btnSalir — botón secundario ancho completo (blanco bordeado), esquinas redondeadas ─
            this.btnSalir.BackColor                         = GUI.Tema.Papel;
            this.btnSalir.Cursor                            = System.Windows.Forms.Cursors.Hand;
            this.btnSalir.FlatAppearance.BorderColor        = GUI.Tema.Borde;
            this.btnSalir.FlatAppearance.MouseOverBackColor = GUI.Tema.RosaPalido;
            this.btnSalir.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
            this.btnSalir.Font                              = new System.Drawing.Font("Segoe UI", 11F);
            this.btnSalir.ForeColor                         = GUI.Tema.TextoMuted;
            this.btnSalir.Location                          = new System.Drawing.Point(48, 544);
            this.btnSalir.Name                              = "btnSalir";
            this.btnSalir.Size                              = new System.Drawing.Size(404, 40);
            this.btnSalir.TabIndex                          = 10;
            this.btnSalir.Tag                               = "btn.salir";
            this.btnSalir.Text                               = "Salir";
            this.btnSalir.UseVisualStyleBackColor            = false;
            this.btnSalir.Click += new System.EventHandler(this.btnSalir_Click);

            // ── Login form ─────────────────────────────────────────────────
            // FormBorderStyle.None: la barra de título nativa de Windows se reemplaza por
            // pnlHeader (logo + idioma + minimizar/cerrar dibujados a medida, ver Login.cs para
            // el arrastre de ventana y los handlers de los botones). MaximizeBox/MinimizeBox no
            // aplican con None; el tamaño sigue fijo, igual que con FixedSingle antes.
            this.AcceptButton        = this.btnIngresar;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor           = GUI.Tema.RosaPalido;
            this.ClientSize          = new System.Drawing.Size(1000, 760);
            this.Controls.Add(this.pnlLeft);
            this.Controls.Add(this.pnlCardShadow);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.pnlHeader);
            this.ForeColor           = GUI.Tema.Tinta;
            this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.None;
            this.Icon                = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name                = "Login";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Tag                 = "frm.login";
            this.Text                = "WardrobeFlow";
            this.pnlHeader.ResumeLayout(false);
            this.pnlLeft.ResumeLayout(false);
            this.pnlCard.ResumeLayout(false);
            this.pnlUsuarioBox.ResumeLayout(false);
            this.pnlUsuarioBox.PerformLayout();
            this.pnlContraseñaBox.ResumeLayout(false);
            this.pnlContraseñaBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picClothingRack)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPriceTag)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPlantAndBag)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel      pnlHeader;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label      lblTitle;
        private System.Windows.Forms.Label      lblSubtitulo;
        private System.Windows.Forms.ComboBox   cmbIdioma;
        private System.Windows.Forms.Panel      pnlHeaderDivider;
        private System.Windows.Forms.Button     btnMinimizar;
        private System.Windows.Forms.Button     btnCerrarVentana;
        private System.Windows.Forms.Panel      pnlLeft;
        private System.Windows.Forms.Label      lblTagline;
        private System.Windows.Forms.Label      lblWordmarkDark;
        private System.Windows.Forms.Label      lblWordmarkVino;
        private System.Windows.Forms.Label      lblBrandDesc;
        private System.Windows.Forms.PictureBox picClothingRack;
        private System.Windows.Forms.PictureBox picPriceTag;
        private System.Windows.Forms.PictureBox picPlantAndBag;
        private System.Windows.Forms.Panel      pnlCardShadow;
        private System.Windows.Forms.Panel      pnlCard;
        private System.Windows.Forms.Label      lblAccent;
        private System.Windows.Forms.Panel      pnlAccentRule;
        private System.Windows.Forms.Label      lblLoginSub;
        private System.Windows.Forms.Label      lblDivider;
        private System.Windows.Forms.Panel      pnlUsuarioBox;
        private System.Windows.Forms.TextBox    txtUsuario;
        private System.Windows.Forms.Label      lblIconoUsuario;
        private System.Windows.Forms.Panel      pnlContraseñaBox;
        private System.Windows.Forms.TextBox    txtContraseña;
        private System.Windows.Forms.Label      lblIconoContraseña;
        private System.Windows.Forms.Button     btnMostrarClave;
        private System.Windows.Forms.Label      lblUsuario;
        private System.Windows.Forms.Label      lblContraseña;
        private System.Windows.Forms.Button     btnIngresar;
        private System.Windows.Forms.Button     btnSalir;
        private System.Windows.Forms.Label      lblError;
        private System.Windows.Forms.LinkLabel  lnkOlvidaste;
        private System.Windows.Forms.LinkLabel  lnkEmergencia;
    }
}
