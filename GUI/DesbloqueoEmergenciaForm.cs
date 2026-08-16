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
    public partial class DesbloqueoEmergenciaForm : Form
    {
        private readonly BLL.RecuperacionAdmin _recBLL = new BLL.RecuperacionAdmin();

        public DesbloqueoEmergenciaForm(string usuarioSugerido = null)
        {
            InitializeComponent();

            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text          = T("emg.titulo",           this.Text);
            lblTitulo.Text     = T("emg.encabezado",        lblTitulo.Text);
            lblInfo.Text       = T("emg.info",              lblInfo.Text);
            lblUsuario.Text    = T("emg.usuario",           lblUsuario.Text);
            lblClave.Text      = T("emg.clave",             lblClave.Text);
            btnDesbloquear.Text = T("emg.btn.desbloquear",  btnDesbloquear.Text);
            btnCancelar.Text   = T("btn.cancelar",          btnCancelar.Text);

            txtClave.SetPlaceholder("XXXX-XXXX-XXXX");

            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new Icon(ico); } catch { }

            if (!string.IsNullOrWhiteSpace(usuarioSugerido))
            {
                txtUsuario.Text = usuarioSugerido;
                txtClave.Select();
            }
        }

        private void BtnDesbloquear_Click(object sender, EventArgs e) => Desbloquear();

        private void Desbloquear()
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            lblError.Text = string.Empty;
            btnDesbloquear.Enabled = false;
            try
            {
                bool ok = _recBLL.DesbloquearConClave(this.Text, txtUsuario.Text, txtClave.Text);
                if (ok)
                {
                    MessageBox.Show(
                        T("emg.exito", "Cuenta desbloqueada con éxito.\nYa podés iniciar sesión normalmente."),
                        T("emg.exito.titulo", "Cuenta desbloqueada"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (BE.AppException ax)
            {
                lblError.Text = t.ContainsKey(ax.Clave) ? t[ax.Clave].Texto : ax.Message;
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
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
