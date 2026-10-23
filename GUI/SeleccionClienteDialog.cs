using System.Collections.Generic;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// Selección de un cliente en un combo (por ejemplo, para anotarlo en la lista de espera de una
    /// prenda desde Prendas). Hecho con el Diseñador; los textos los pasa quien lo abre.
    /// </summary>
    public partial class SeleccionClienteDialog : Form
    {
        public SeleccionClienteDialog()
        {
            InitializeComponent();
        }

        public void Configurar(string titulo, string prompt, string aceptar, string cancelar, IEnumerable<BE.Cliente> clientes)
        {
            Text             = titulo;
            lblPrompt.Text   = prompt;
            btnAceptar.Text  = aceptar;
            btnCancelar.Text = cancelar;
            cmbCliente.Items.Clear();
            if (clientes != null)
                foreach (var c in clientes) cmbCliente.Items.Add(c);
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
        }

        public BE.Cliente ClienteSeleccionado => cmbCliente.SelectedItem as BE.Cliente;
    }
}
