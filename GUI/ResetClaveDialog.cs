using System;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Diálogo modal para ingresar una nueva contraseña al resetear credenciales.
    ///
    /// Muestra el nombre de usuario objetivo, solicita la nueva contraseña dos veces
    /// (nueva + confirmación) y expone la clave validada en la propiedad NuevaClave.
    ///
    /// Uso:
    ///   using (var dialog = new ResetClaveDialog(username))
    ///   {
    ///       if (dialog.ShowDialog(this) == DialogResult.OK)
    ///           usuarioBLL.ResetearClave(this, idUsuario, dialog.NuevaClave);
    ///   }
    /// </summary>
    public partial class ResetClaveDialog : Form
    {
        /// <summary>
        /// Nueva contraseña validada (disponible solo cuando DialogResult == OK).
        /// </summary>
        public string NuevaClave { get; private set; }

        /// <summary>
        /// Construye el diálogo para el usuario especificado.
        /// </summary>
        /// <param name="username">Nombre del usuario cuya contraseña se va a resetear.</param>
        public ResetClaveDialog(string username)
        {
            InitializeComponent();
            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new System.Drawing.Icon(ico); } catch { }

            // ── Traducciones ──────────────────────────────────────────────────
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T_r(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text          = T_r("frm.resetclave", "Resetear Contraseña");
            lblTitulo.Text     = T_r("frm.resetclave", "Resetear Contraseña");
            lblUsuario.Text    = $"{T_r("lbl.usuario", "Usuario")}: {username}";
            lblNueva.Text      = T_r("lbl.nueva.clave", "Nueva contraseña (mín. 6 caracteres):");
            lblConfirmar.Text  = T_r("lbl.confirmar.clave", "Confirmar contraseña:");
            btnAceptar.Text    = T_r("btn.confirmar.reset", "Confirmar Reset");
            btnCancelar.Text   = T_r("btn.cancelar", "Cancelar");
        }

        /// <summary>
        /// Valida las contraseñas ingresadas y, si son válidas, cierra el diálogo con OK.
        /// </summary>
        private void BtnAceptar_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T_v(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            string nueva     = txtNuevaClave.Text;
            string confirmar = txtConfirmar.Text;

            // Solo verifica que ambas contraseñas coincidan — UX puro.
            // La validación de reglas (longitud, complejidad) la hace BLL vía Encriptador.
            if (nueva != confirmar)
            {
                lblError.Text = T_v("err.clave.nomatch", "Las contraseñas no coinciden.");
                txtConfirmar.Clear();
                txtConfirmar.Focus();
                return;
            }

            // Todo OK: exponer el valor y cerrar el diálogo
            NuevaClave         = nueva;
            this.DialogResult  = DialogResult.OK;
            this.Close();
        }
    }
}
