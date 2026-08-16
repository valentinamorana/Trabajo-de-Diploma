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
            this.pnlHeader        = new System.Windows.Forms.Panel();
            this.picLogo          = new System.Windows.Forms.PictureBox();
            this.lblTitle         = new System.Windows.Forms.Label();
            this.lblSubtitulo     = new System.Windows.Forms.Label();
            this.pnlLeft          = new System.Windows.Forms.Panel();
            this.picClothingRack  = new System.Windows.Forms.PictureBox();
            this.picPriceTag      = new System.Windows.Forms.PictureBox();
            this.picPlantAndBag   = new System.Windows.Forms.PictureBox();
            this.pnlCard          = new System.Windows.Forms.Panel();
            this.lblAccent        = new System.Windows.Forms.Label();
            this.lblLoginSub      = new System.Windows.Forms.Label();
            this.lblUsuario       = new System.Windows.Forms.Label();
            this.pnlUsuarioBox    = new System.Windows.Forms.Panel();
            this.txtUsuario       = new System.Windows.Forms.TextBox();
            this.lblContraseña    = new System.Windows.Forms.Label();
            this.pnlContraseñaBox = new System.Windows.Forms.Panel();
            this.txtContraseña    = new System.Windows.Forms.TextBox();
            this.lnkOlvidaste     = new System.Windows.Forms.LinkLabel();
            this.lblError         = new System.Windows.Forms.Label();
            this.btnIngresar      = new System.Windows.Forms.Button();
            this.lblDivider       = new System.Windows.Forms.Label();
            this.btnSalir         = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picClothingRack)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPriceTag)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPlantAndBag)).BeginInit();
            this.pnlHeader.SuspendLayout();
            this.pnlCard.SuspendLayout();
            this.pnlUsuarioBox.SuspendLayout();
            this.pnlContraseñaBox.SuspendLayout();
            this.SuspendLayout();

            // ── pnlHeader — franja superior de ancho completo (760 × 56) ───
            // Marca + selector de idioma. Equivalente a la barra superior del mock (WardrobeFlow
            // | PORTAL DE EMPLEADOS ... selector). Fondo Tema.Papel (blanco de la paleta).
            this.pnlHeader.BackColor = GUI.Tema.Papel;
            this.pnlHeader.Controls.Add(this.picLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitulo);
            this.pnlHeader.Location  = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name      = "pnlHeader";
            this.pnlHeader.Size      = new System.Drawing.Size(760, 56);
            this.pnlHeader.TabIndex  = 20;

            // ── picLogo — isotipo + wordmark "WardrobeFlow" (asset 01, transparente) ──
            // Reemplaza visualmente el ícono de percha dibujado a mano + lblTitle (que se
            // oculta, no se elimina, para no perder el control ni su Tag/eventos futuros).
            this.picLogo.BackColor  = GUI.Tema.Papel;
            this.picLogo.Image      = global::GUI.Properties.Resources.login_logo;
            this.picLogo.Location   = new System.Drawing.Point(20, 10);
            this.picLogo.Name       = "picLogo";
            this.picLogo.Size       = new System.Drawing.Size(158, 36);
            this.picLogo.SizeMode   = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex   = 22;
            this.picLogo.TabStop    = false;

            // ── lblTitle — nombre de marca junto al ícono (oculto: lo muestra picLogo) ─
            this.lblTitle.AutoSize  = false;
            this.lblTitle.BackColor = GUI.Tema.Papel;
            this.lblTitle.Font      = new System.Drawing.Font("Segoe UI", 11.5F);
            this.lblTitle.ForeColor = GUI.Tema.Tinta;
            this.lblTitle.Location  = new System.Drawing.Point(66, 8);
            this.lblTitle.Name      = "lblTitle";
            this.lblTitle.Size      = new System.Drawing.Size(190, 20);
            this.lblTitle.TabIndex  = 11;
            this.lblTitle.Text      = "WardrobeFlow";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTitle.Visible   = false;

            // ── lblSubtitulo — "PORTAL DE EMPLEADOS" ─────────────────────
            this.lblSubtitulo.AutoSize  = false;
            this.lblSubtitulo.BackColor = GUI.Tema.Papel;
            this.lblSubtitulo.Font      = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this.lblSubtitulo.ForeColor = GUI.Tema.TextoMuted;
            this.lblSubtitulo.Location  = new System.Drawing.Point(192, 20);
            this.lblSubtitulo.Name      = "lblSubtitulo";
            this.lblSubtitulo.Size      = new System.Drawing.Size(240, 16);
            this.lblSubtitulo.TabIndex  = 12;
            this.lblSubtitulo.Tag       = "lbl.subtitulo";
            this.lblSubtitulo.Text      = "PORTAL DE EMPLEADOS";
            this.lblSubtitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ── pnlLeft — panel de branding izquierdo (300 × 564) ──────────
            // Fondo rosa pálido (Tema.RosaPalido); ilustración vía PictureBox (assets 03/04/05,
            // transparentes), textura sutil de fondo vía Paint en Login.cs.
            this.pnlLeft.BackColor = GUI.Tema.RosaPalido;
            this.pnlLeft.Controls.Add(this.picClothingRack);
            this.pnlLeft.Controls.Add(this.picPriceTag);
            this.pnlLeft.Controls.Add(this.picPlantAndBag);
            this.pnlLeft.Location  = new System.Drawing.Point(0, 56);
            this.pnlLeft.Name      = "pnlLeft";
            this.pnlLeft.Size      = new System.Drawing.Size(300, 564);
            this.pnlLeft.TabIndex  = 21;

            // ── picClothingRack — asset 03, percha con prendas ──────────────
            this.picClothingRack.BackColor = GUI.Tema.RosaPalido;
            this.picClothingRack.Image     = global::GUI.Properties.Resources.login_clothing_rack;
            this.picClothingRack.Location  = new System.Drawing.Point(10, 258);
            this.picClothingRack.Name      = "picClothingRack";
            this.picClothingRack.Size      = new System.Drawing.Size(165, 192);
            this.picClothingRack.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picClothingRack.TabIndex  = 23;
            this.picClothingRack.TabStop   = false;

            // ── picPriceTag — asset 04, cartel de precio ────────────────────
            this.picPriceTag.BackColor = GUI.Tema.RosaPalido;
            this.picPriceTag.Image     = global::GUI.Properties.Resources.login_price_tag;
            this.picPriceTag.Location  = new System.Drawing.Point(190, 258);
            this.picPriceTag.Name      = "picPriceTag";
            this.picPriceTag.Size      = new System.Drawing.Size(58, 90);
            this.picPriceTag.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPriceTag.TabIndex  = 24;
            this.picPriceTag.TabStop   = false;

            // ── picPlantAndBag — asset 05, planta + bolso ───────────────────
            this.picPlantAndBag.BackColor = GUI.Tema.RosaPalido;
            this.picPlantAndBag.Image     = global::GUI.Properties.Resources.login_plant_and_bag;
            this.picPlantAndBag.Location  = new System.Drawing.Point(185, 358);
            this.picPlantAndBag.Name      = "picPlantAndBag";
            this.picPlantAndBag.Size      = new System.Drawing.Size(75, 143);
            this.picPlantAndBag.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPlantAndBag.TabIndex  = 25;
            this.picPlantAndBag.TabStop   = false;

            // ── pnlCard — panel derecho, card blanca con el formulario (460 × 564) ─
            this.pnlCard.BackColor = GUI.Tema.Papel;
            this.pnlCard.Controls.Add(this.lblAccent);
            this.pnlCard.Controls.Add(this.lblLoginSub);
            this.pnlCard.Controls.Add(this.lblUsuario);
            this.pnlCard.Controls.Add(this.pnlUsuarioBox);
            this.pnlCard.Controls.Add(this.lblContraseña);
            this.pnlCard.Controls.Add(this.pnlContraseñaBox);
            this.pnlCard.Controls.Add(this.lnkOlvidaste);
            this.pnlCard.Controls.Add(this.lblError);
            this.pnlCard.Controls.Add(this.btnIngresar);
            this.pnlCard.Controls.Add(this.lblDivider);
            this.pnlCard.Controls.Add(this.btnSalir);
            this.pnlCard.Location = new System.Drawing.Point(300, 56);
            this.pnlCard.Name     = "pnlCard";
            this.pnlCard.Size     = new System.Drawing.Size(460, 564);
            this.pnlCard.TabIndex = 13;

            // ── lblAccent — "Bienvenido / de nuevo" (dos líneas, como el mock) ──
            this.lblAccent.AutoSize  = false;
            this.lblAccent.BackColor = System.Drawing.Color.Transparent;
            this.lblAccent.Font      = new System.Drawing.Font("Segoe UI", 20F);
            this.lblAccent.ForeColor = GUI.Tema.Tinta;
            this.lblAccent.Location  = new System.Drawing.Point(40, 56);
            this.lblAccent.Name      = "lblAccent";
            this.lblAccent.Size      = new System.Drawing.Size(380, 64);
            this.lblAccent.TabIndex  = 10;
            this.lblAccent.Tag       = "lbl.bienvenido";
            this.lblAccent.Text      = "Bienvenido\r\nde nuevo";
            this.lblAccent.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // ── lblLoginSub — subtítulo bajo el acento ─────────────────────
            this.lblLoginSub.AutoSize  = false;
            this.lblLoginSub.BackColor = System.Drawing.Color.Transparent;
            this.lblLoginSub.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblLoginSub.ForeColor = GUI.Tema.TextoMuted;
            this.lblLoginSub.Location  = new System.Drawing.Point(40, 138);
            this.lblLoginSub.Name      = "lblLoginSub";
            this.lblLoginSub.Size      = new System.Drawing.Size(380, 20);
            this.lblLoginSub.TabIndex  = 15;
            this.lblLoginSub.Tag       = "lbl.credenciales";
            this.lblLoginSub.Text      = "Ingresá para continuar en tu cuenta.";

            // ── lblUsuario — etiqueta "USUARIO" (uppercase small) ──────────
            this.lblUsuario.AutoSize  = false;
            this.lblUsuario.BackColor = System.Drawing.Color.Transparent;
            this.lblUsuario.Font      = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblUsuario.ForeColor = GUI.Tema.TextoMuted;
            this.lblUsuario.Location  = new System.Drawing.Point(40, 182);
            this.lblUsuario.Name      = "lblUsuario";
            this.lblUsuario.Size      = new System.Drawing.Size(380, 16);
            this.lblUsuario.TabIndex  = 0;
            this.lblUsuario.Tag       = "lbl.usuario";
            this.lblUsuario.Text      = "USUARIO";
            this.lblUsuario.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // ── pnlUsuarioBox — caja con borde propio (Paint) que aloja ícono + textbox ─
            this.pnlUsuarioBox.BackColor = GUI.Tema.Papel;
            this.pnlUsuarioBox.Controls.Add(this.txtUsuario);
            this.pnlUsuarioBox.Location  = new System.Drawing.Point(40, 200);
            this.pnlUsuarioBox.Name      = "pnlUsuarioBox";
            this.pnlUsuarioBox.Size      = new System.Drawing.Size(380, 34);
            this.pnlUsuarioBox.TabIndex  = 1;

            // ── txtUsuario ─────────────────────────────────────────────────
            this.txtUsuario.BackColor   = GUI.Tema.Papel;
            this.txtUsuario.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUsuario.Font        = new System.Drawing.Font("Segoe UI", 10F);
            this.txtUsuario.ForeColor   = GUI.Tema.Tinta;
            this.txtUsuario.Location    = new System.Drawing.Point(34, 8);
            this.txtUsuario.MaxLength   = 50;
            this.txtUsuario.Name        = "txtUsuario";
            this.txtUsuario.Size        = new System.Drawing.Size(338, 20);
            this.txtUsuario.TabIndex    = 1;

            // ── lblContraseña — etiqueta "CONTRASEÑA" ─────────────────────
            this.lblContraseña.AutoSize  = false;
            this.lblContraseña.BackColor = System.Drawing.Color.Transparent;
            this.lblContraseña.Font      = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblContraseña.ForeColor = GUI.Tema.TextoMuted;
            this.lblContraseña.Location  = new System.Drawing.Point(40, 248);
            this.lblContraseña.Name      = "lblContraseña";
            this.lblContraseña.Size      = new System.Drawing.Size(380, 16);
            this.lblContraseña.TabIndex  = 2;
            this.lblContraseña.Tag       = "lbl.contrasena";
            this.lblContraseña.Text      = "CONTRASEÑA";
            this.lblContraseña.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // ── pnlContraseñaBox ───────────────────────────────────────────
            this.pnlContraseñaBox.BackColor = GUI.Tema.Papel;
            this.pnlContraseñaBox.Controls.Add(this.txtContraseña);
            this.pnlContraseñaBox.Location  = new System.Drawing.Point(40, 266);
            this.pnlContraseñaBox.Name      = "pnlContraseñaBox";
            this.pnlContraseñaBox.Size      = new System.Drawing.Size(380, 34);
            this.pnlContraseñaBox.TabIndex  = 3;

            // ── txtContraseña ──────────────────────────────────────────────
            this.txtContraseña.BackColor    = GUI.Tema.Papel;
            this.txtContraseña.BorderStyle  = System.Windows.Forms.BorderStyle.None;
            this.txtContraseña.Font         = new System.Drawing.Font("Segoe UI", 10F);
            this.txtContraseña.ForeColor    = GUI.Tema.Tinta;
            this.txtContraseña.Location     = new System.Drawing.Point(34, 8);
            this.txtContraseña.MaxLength    = 100;
            this.txtContraseña.Name         = "txtContraseña";
            this.txtContraseña.PasswordChar = '●';
            this.txtContraseña.Size         = new System.Drawing.Size(338, 20);
            this.txtContraseña.TabIndex     = 3;

            // ── lnkOlvidaste — link alineado a la derecha bajo contraseña ─
            this.lnkOlvidaste.ActiveLinkColor = GUI.Tema.RosaOscuro;
            this.lnkOlvidaste.AutoSize        = false;
            this.lnkOlvidaste.BackColor       = System.Drawing.Color.Transparent;
            this.lnkOlvidaste.Font            = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lnkOlvidaste.LinkColor       = GUI.Tema.RosaOscuro;
            this.lnkOlvidaste.Location        = new System.Drawing.Point(40, 308);
            this.lnkOlvidaste.Name            = "lnkOlvidaste";
            this.lnkOlvidaste.Size            = new System.Drawing.Size(380, 16);
            this.lnkOlvidaste.TabIndex        = 6;
            this.lnkOlvidaste.TabStop         = true;
            this.lnkOlvidaste.Tag             = "lnk.olvide";
            this.lnkOlvidaste.Text            = "¿Olvidaste tu contraseña?";
            this.lnkOlvidaste.TextAlign       = System.Drawing.ContentAlignment.MiddleRight;
            this.lnkOlvidaste.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkOlvidaste_LinkClicked);

            // ── lblError ───────────────────────────────────────────────────
            this.lblError.AutoSize  = false;
            this.lblError.BackColor = GUI.Tema.Papel;
            this.lblError.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblError.ForeColor = GUI.Tema.Error;
            this.lblError.Location  = new System.Drawing.Point(40, 352);
            this.lblError.Name      = "lblError";
            this.lblError.Size      = new System.Drawing.Size(380, 34);
            this.lblError.TabIndex  = 4;
            this.lblError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // ── btnIngresar — botón primario ancho completo (vino) ─────────
            this.btnIngresar.BackColor                 = GUI.Tema.RosaOscuro;
            this.btnIngresar.Cursor                    = System.Windows.Forms.Cursors.Hand;
            this.btnIngresar.FlatAppearance.BorderSize = 0;
            this.btnIngresar.FlatAppearance.MouseDownBackColor = System.Windows.Forms.ControlPaint.Dark(GUI.Tema.RosaOscuro, 0.2f);
            this.btnIngresar.FlatAppearance.MouseOverBackColor = System.Windows.Forms.ControlPaint.Light(GUI.Tema.RosaOscuro, 0.15f);
            this.btnIngresar.FlatStyle                 = System.Windows.Forms.FlatStyle.Flat;
            this.btnIngresar.Font                      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnIngresar.ForeColor                 = GUI.Tema.Papel;
            this.btnIngresar.Location                  = new System.Drawing.Point(40, 394);
            this.btnIngresar.Name                      = "btnIngresar";
            this.btnIngresar.Size                       = new System.Drawing.Size(380, 46);
            this.btnIngresar.TabIndex                  = 5;
            this.btnIngresar.Tag                        = "btn.ingresar";
            this.btnIngresar.Text                       = "INGRESAR          →";
            this.btnIngresar.UseVisualStyleBackColor    = false;
            this.btnIngresar.Click += new System.EventHandler(this.btnIngresar_Click);

            // ── lblDivider — separador "o" con líneas a los lados ─────────
            // Las líneas horizontales se dibujan vía Paint event en Login.cs.
            this.lblDivider.AutoSize  = false;
            this.lblDivider.BackColor = System.Drawing.Color.Transparent;
            this.lblDivider.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblDivider.ForeColor = GUI.Tema.TextoMuted;
            this.lblDivider.Location  = new System.Drawing.Point(40, 450);
            this.lblDivider.Name      = "lblDivider";
            this.lblDivider.Size      = new System.Drawing.Size(380, 22);
            this.lblDivider.TabIndex  = 16;
            this.lblDivider.Tag       = "lbl.divider";
            this.lblDivider.Text      = "o";
            this.lblDivider.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // ── btnSalir — botón secundario ancho completo (blanco bordeado) ─
            this.btnSalir.BackColor                         = GUI.Tema.Papel;
            this.btnSalir.Cursor                            = System.Windows.Forms.Cursors.Hand;
            this.btnSalir.FlatAppearance.BorderColor        = GUI.Tema.Borde;
            this.btnSalir.FlatAppearance.MouseOverBackColor = GUI.Tema.RosaPalido;
            this.btnSalir.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
            this.btnSalir.Font                              = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnSalir.ForeColor                         = GUI.Tema.TextoMuted;
            this.btnSalir.Location                          = new System.Drawing.Point(40, 480);
            this.btnSalir.Name                              = "btnSalir";
            this.btnSalir.Size                              = new System.Drawing.Size(380, 40);
            this.btnSalir.TabIndex                          = 7;
            this.btnSalir.Tag                               = "btn.salir";
            this.btnSalir.Text                               = "Salir";
            this.btnSalir.UseVisualStyleBackColor            = false;
            this.btnSalir.Click += new System.EventHandler(this.btnSalir_Click);

            // ── Login form ─────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor           = GUI.Tema.RosaPalido;
            this.ClientSize          = new System.Drawing.Size(760, 620);
            this.Controls.Add(this.pnlLeft);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.pnlHeader);
            this.ForeColor           = GUI.Tema.Tinta;
            this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon                = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox         = false;
            this.MinimizeBox         = false;
            this.Name                = "Login";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Tag                 = "frm.login";
            this.Text                = "WardrobeFlow";
            this.pnlHeader.ResumeLayout(false);
            this.pnlCard.ResumeLayout(false);
            this.pnlCard.PerformLayout();
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
        private System.Windows.Forms.Panel      pnlLeft;
        private System.Windows.Forms.PictureBox picClothingRack;
        private System.Windows.Forms.PictureBox picPriceTag;
        private System.Windows.Forms.PictureBox picPlantAndBag;
        private System.Windows.Forms.Panel      pnlCard;
        private System.Windows.Forms.Label      lblAccent;
        private System.Windows.Forms.Label      lblLoginSub;
        private System.Windows.Forms.Label      lblDivider;
        private System.Windows.Forms.Panel      pnlUsuarioBox;
        private System.Windows.Forms.TextBox    txtUsuario;
        private System.Windows.Forms.Panel      pnlContraseñaBox;
        private System.Windows.Forms.TextBox    txtContraseña;
        private System.Windows.Forms.Label      lblUsuario;
        private System.Windows.Forms.Label      lblContraseña;
        private System.Windows.Forms.Button     btnIngresar;
        private System.Windows.Forms.Button     btnSalir;
        private System.Windows.Forms.Label      lblError;
        private System.Windows.Forms.LinkLabel  lnkOlvidaste;
    }
}
