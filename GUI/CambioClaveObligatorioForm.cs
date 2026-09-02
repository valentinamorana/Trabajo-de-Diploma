using System;
using System.Drawing;
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
    public partial class CambioClaveObligatorioForm : Form
    {
        private readonly BLL.Usuario _usuarioBLL = new BLL.Usuario();

        public CambioClaveObligatorioForm()
        {
            InitializeComponent();

            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text        = T("frm.cambioclave.titulo",   this.Text);
            lblInfo.Text      = T("lbl.cambioclave.info",     lblInfo.Text);
            lblNueva.Text     = T("lbl.cambioclave.nueva",    lblNueva.Text);
            lblRepetir.Text   = T("lbl.cambioclave.repetir",  lblRepetir.Text);
            lblReglas.Text    = T("lbl.cambioclave.reglas",   lblReglas.Text);
            btnCambiar.Text   = T("btn.cambioclave.cambiar",  btnCambiar.Text);
            btnCancelar.Text  = T("btn.cambioclave.cancelar", btnCancelar.Text);

            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new Icon(ico); } catch { }
        }

        private void BtnCambiar_Click(object sender, EventArgs e) => Confirmar();

        private void Confirmar()
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            lblError.Text = string.Empty;

            if (txtNueva.Text != txtRepetir.Text)
            {
                lblError.Text = T("err.cambioclave.nocoincide", "Las contraseñas no coinciden.");
                txtRepetir.Clear();
                txtRepetir.Focus();
                return;
            }

            try
            {
                // La BLL valida requisitos, que difiera de la actual, persiste y baja el flag.
                _usuarioBLL.CambiarClavePropia(this.Text, txtNueva.Text);
                MessageBox.Show(
                    T("msg.cambioclave.exito", "Contraseña actualizada. Ya podés usar el sistema."),
                    T("rpt.dlg.exito.titulo", "Éxito"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                lblError.Text = T("msg.error.inesperado",
                    "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.");
            }
        }
    }
}
