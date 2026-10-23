using System;
using System.Windows.Forms;

namespace GUI.Ayuda
{
    /// <summary>
    /// D02 — Ventana de la ayuda en línea (F1): encabezado con el título del tema, el texto con
    /// desplazamiento y el botón Cerrar. Hecha con el Diseñador; los textos los pone
    /// <see cref="AyudaEnLinea.Mostrar"/> en el idioma del usuario.
    /// </summary>
    public partial class AyudaDialog : Form
    {
        public AyudaDialog()
        {
            InitializeComponent();
        }

        /// <summary>Carga los textos del tema (ya traducidos).</summary>
        public void Mostrar(string tituloVentana, string titulo, string texto, string textoCerrar)
        {
            Text               = tituloVentana;
            lblEncabezado.Text = titulo;
            txtCuerpo.Text     = (texto ?? string.Empty).Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            btnCerrar.Text     = textoCerrar;
        }
    }
}
