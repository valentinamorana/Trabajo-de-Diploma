using System;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Cambio de contraseña OBLIGATORIO tras el login cuando la cuenta tiene una clave
    /// temporal/generada (RequiereCambioClave = 1): alta de usuario o reset por un admin.
    /// Modal. Solo cierra con OK si el cambio fue exitoso; ante un error muestra el detalle
    /// y permanece abierto. La cancelación la maneja el Login (cierra la sesión y vuelve al ingreso).
    /// </summary>
    public partial class CambioClaveObligatorioForm : FormBase
    {
        private readonly BLL.Usuario _usuarioBLL = new BLL.Usuario();

        public CambioClaveObligatorioForm()
        {
            InitializeComponent();

            this.Text        = Tr("frm.cambioclave.titulo",   this.Text);
            lblInfo.Text      = Tr("lbl.cambioclave.info",     lblInfo.Text);
            lblNueva.Text     = Tr("lbl.cambioclave.nueva",    lblNueva.Text);
            lblRepetir.Text   = Tr("lbl.cambioclave.repetir",  lblRepetir.Text);
            lblReglas.Text    = Tr("lbl.cambioclave.reglas",   lblReglas.Text);
            btnCambiar.Text   = Tr("btn.cambioclave.cambiar",  btnCambiar.Text);
            btnCancelar.Text  = Tr("btn.cambioclave.cancelar", btnCancelar.Text);
        }

        private void BtnCambiar_Click(object sender, EventArgs e) => Confirmar();

        private void Confirmar()
        {
            lblError.Text = string.Empty;

            // Feedback temprano en la propia GUI (antes dependía 100% de que la BLL rechazara la
            // clave débil tras el roundtrip, sin decirle al usuario cuál regla incumplió hasta
            // recibir la excepción) — BLL.Usuario.ValidarContrasena ya existía justo para esto
            // ("para que la GUI pueda dar feedback temprano sin acceder directamente a Seguridad")
            // pero ninguna pantalla la llamaba todavía. Misma regla que ya se muestra en lblReglas.
            if (string.IsNullOrEmpty(txtNueva.Text))
            {
                lblError.Text = Tr("err.cambioclave.vacia", "Ingresá una contraseña nueva.");
                txtNueva.Focus();
                return;
            }

            var (valida, claveErr, mensajeErr) = _usuarioBLL.ValidarContrasena(txtNueva.Text);
            if (!valida)
            {
                lblError.Text = Traductor.Resolver(claveErr, mensajeErr, null, GestorIdioma.IdiomaActual);
                txtNueva.Focus();
                return;
            }

            if (txtNueva.Text != txtRepetir.Text)
            {
                lblError.Text = Tr("err.cambioclave.nocoincide", "Las contraseñas no coinciden.");
                txtRepetir.Clear();
                txtRepetir.Focus();
                return;
            }

            try
            {
                // La BLL valida requisitos, que difiera de la actual, persiste y baja el flag.
                _usuarioBLL.CambiarClavePropia(this.Text, txtNueva.Text);
                MessageBox.Show(
                    Tr("msg.cambioclave.exito", "Contraseña actualizada. Ya podés usar el sistema."),
                    Tr("rpt.dlg.exito.titulo", "Éxito"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (BE.AppException ex)
            {
                // Error de negocio esperado (requisitos / clave igual): mostrar traducido inline.
                lblError.Text = Traductor.Resolver(ex.Clave, ex.Message, ex.Args, GestorIdioma.IdiomaActual);
            }
            catch (Exception)
            {
                // Excepción inesperada (sin clave de traducción): mensaje genérico, no el texto
                // técnico crudo, mismo criterio que FormBase.MostrarError(Exception).
                lblError.Text = Tr("msg.error.inesperado",
                    "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.");
            }
        }
    }
}
