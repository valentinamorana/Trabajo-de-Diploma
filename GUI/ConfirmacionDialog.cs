using System;
using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// Confirmación Sí/No con los colores de la aplicación (reemplaza a MessageBox.YesNo). Hecha con
    /// el Diseñador; <see cref="FormBase.MostrarConfirmacionSiNo"/> le pasa el texto, los botones
    /// traducidos y ajusta el alto al texto.
    /// </summary>
    public partial class ConfirmacionDialog : Form
    {
        public ConfirmacionDialog()
        {
            InitializeComponent();
        }

        /// <param name="porDefectoNo">Para acciones destructivas: Enter elige «No».</param>
        public void Configurar(string texto, string titulo, string textoSi, string textoNo, bool porDefectoNo)
        {
            Text          = titulo;
            lblTexto.Text = texto;
            btnSi.Text    = textoSi;
            btnNo.Text    = textoNo;

            // El alto se adapta al texto: varias confirmaciones (cobro, cancelación) traen un detalle
            // de 5-6 líneas que no entraba en un cuadro fijo. El ancho del texto es el del Diseñador.
            int anchoTexto = lblTexto.Width;
            Size medida = TextRenderer.MeasureText(texto ?? string.Empty, lblTexto.Font,
                new Size(anchoTexto, int.MaxValue), TextFormatFlags.WordBreak);
            int altoTexto = Math.Max(44, Math.Min(medida.Height + 8, 420));
            lblTexto.Height = altoTexto;
            int topBotones = lblTexto.Top + altoTexto + 14;
            btnSi.Top = btnNo.Top = topBotones;
            ClientSize = new Size(ClientSize.Width, topBotones + btnSi.Height + 18);

            AcceptButton  = porDefectoNo ? btnNo : btnSi;
            ActiveControl = porDefectoNo ? btnNo : btnSi;
        }
    }
}
