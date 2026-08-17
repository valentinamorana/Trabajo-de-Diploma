using System;
using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// Diálogo simple de entrada de texto (con opción de máscara para contraseñas), reutilizable
    /// sin dependencias externas. Devuelve el texto en <see cref="InputText"/> y DialogResult.OK
    /// si el usuario confirma. (Equivalente al InputDialog de Stach.)
    /// </summary>
    public partial class InputDialog : Form
    {
        public string InputText => txtInput.Text;

        public InputDialog(string titulo, string prompt, bool esPassword)
        {
            InitializeComponent();

            this.Text      = titulo;
            lblPrompt.Text = prompt;
            if (esPassword) txtInput.UseSystemPasswordChar = true;

            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new Icon(ico); } catch { }
        }
    }
}
