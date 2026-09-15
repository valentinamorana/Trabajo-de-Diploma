using System;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// RF-10 — Diálogo de autodesbloqueo de un Administrador mediante una clave de emergencia
    /// de un solo uso (tipo códigos de respaldo de Steam / 2FA). Se abre desde el Login cuando
    /// la cuenta quedó bloqueada por intentos fallidos, sin depender de otro Administrador.
    /// </summary>
    public partial class DesbloqueoEmergenciaForm : FormBase
    {
        private readonly BLL.RecuperacionAdmin _recBLL = new BLL.RecuperacionAdmin();

        public DesbloqueoEmergenciaForm(string usuarioSugerido = null)
        {
            InitializeComponent();

            this.Text          = Tr("emg.titulo",           this.Text);
            lblTitulo.Text     = Tr("emg.encabezado",        lblTitulo.Text);
            lblInfo.Text       = Tr("emg.info",              lblInfo.Text);
            lblUsuario.Text    = Tr("emg.usuario",           lblUsuario.Text);
            lblClave.Text      = Tr("emg.clave",             lblClave.Text);
            btnDesbloquear.Text = Tr("emg.btn.desbloquear",  btnDesbloquear.Text);
            btnCancelar.Text   = Tr("btn.cancelar",          btnCancelar.Text);

            txtClave.SetPlaceholder("XXXX-XXXX-XXXX");

            if (!string.IsNullOrWhiteSpace(usuarioSugerido))
            {
                txtUsuario.Text = usuarioSugerido;
                txtClave.Select();
            }
        }

        private void BtnDesbloquear_Click(object sender, EventArgs e) => Desbloquear();

        // Mismo mecanismo que Login.btnMostrarClave_Click.
        private void BtnMostrarClave_Click(object sender, EventArgs e)
        {
            if (txtClave.PasswordChar == '\0')
            {
                txtClave.PasswordChar = '●';
                btnMostrarClave.Font = new Font("Segoe UI Emoji", 9f, FontStyle.Strikeout);
            }
            else
            {
                txtClave.PasswordChar = '\0';
                btnMostrarClave.Font = new Font("Segoe UI Emoji", 9f);
            }
        }

        private void Desbloquear()
        {
            lblError.Text = string.Empty;
            btnDesbloquear.Enabled = false;
            try
            {
                bool ok = _recBLL.DesbloquearConClave(this.Text, txtUsuario.Text, txtClave.Text);
                if (ok)
                {
                    MessageBox.Show(
                        Tr("emg.exito", "Cuenta desbloqueada con éxito.\nYa podés iniciar sesión normalmente."),
                        Tr("emg.exito.titulo", "Cuenta desbloqueada"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (BE.AppException ax)
            {
                lblError.Text = Traductor.Resolver(ax.Clave, ax.Message, ax.Args, GestorIdioma.IdiomaActual);
            }
            catch (Exception)
            {
                // Excepción inesperada (no BE.AppException, sin clave de traducción): mismo criterio
                // que FormBase.MostrarError(Exception) — mensaje genérico, no el texto técnico crudo.
                lblError.Text = Tr("msg.error.inesperado",
                    "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.");
            }
            finally
            {
                btnDesbloquear.Enabled = true;
            }
        }
    }

    // Pequeño helper para mostrar un placeholder (texto guía) en un TextBox.
    internal static class TextBoxPlaceholderExtensions
    {
        public static void SetPlaceholder(this TextBox txt, string placeholder)
        {
            void Apply()
            {
                if (string.IsNullOrEmpty(txt.Text))
                {
                    txt.ForeColor = Color.Gray;
                    txt.Text = placeholder;
                }
            }
            txt.GotFocus += (s, e) =>
            {
                if (txt.Text == placeholder) { txt.Text = string.Empty; txt.ForeColor = Color.Black; }
            };
            txt.LostFocus += (s, e) => Apply();
            Apply();
        }
    }
}
