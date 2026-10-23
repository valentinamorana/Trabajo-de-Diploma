using System;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// PN02 — Resultado de la venta (NuevaContratacionForm) con el botón para imprimir el documento que
    /// el cliente se lleva: «Orden de cobro» o «Aviso de desistimiento». Hecho con el Diseñador.
    /// </summary>
    public partial class ResultadoContratacionDialog : Form
    {
        /// <summary>Se levanta al presionar el botón de imprimir; el formulario que lo abre arma el documento.</summary>
        public event EventHandler Imprimir;

        public ResultadoContratacionDialog()
        {
            InitializeComponent();
        }

        public void Configurar(string titulo, string mensaje, string textoImprimir, string textoCerrar)
        {
            Text             = titulo;
            lblMensaje.Text  = mensaje;
            btnImprimir.Text = textoImprimir;
            btnCerrar.Text   = textoCerrar;
        }

        private void BtnImprimir_Click(object sender, EventArgs e) => Imprimir?.Invoke(this, EventArgs.Empty);
    }
}
