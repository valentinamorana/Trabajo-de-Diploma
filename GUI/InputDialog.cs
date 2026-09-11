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
    public partial class InputDialog : FormBase
    {
        public string InputText => txtInput.Text;

        public InputDialog(string titulo, string prompt, bool esPassword)
        {
            InitializeComponent();

            this.Text      = titulo;
            lblPrompt.Text = prompt;
            if (esPassword) txtInput.UseSystemPasswordChar = true;

            // Ícono, tema/fuente del usuario y seguridad de controles ahora los aplica
            // FormBase.OnLoad — antes se duplicaba a mano solo el ícono acá, y el diálogo
            // quedaba con tema claro/fuente por defecto aunque el usuario hubiera elegido
            // tema oscuro en "Mi Perfil". Seguro de usar incluso antes del login:
            // PreferenciasUI.Aplicar usa un default razonable si todavía no se cargó ninguna
            // preferencia, y ManejadorSeguridad.AplicarSeguridad se guarda con IsLoggedIn.
        }
    }
}
