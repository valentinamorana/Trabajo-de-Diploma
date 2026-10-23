using System;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// Panel de Control — cada cuántos días se recuerda hacer un backup (BLL.Configuracion).
    /// Hecho con el Diseñador; los textos los traduce <see cref="DashboardForm"/> antes de mostrarlo.
    /// </summary>
    public partial class RecordatorioBackupDialog : Form
    {
        public RecordatorioBackupDialog()
        {
            InitializeComponent();
        }

        /// <summary>Días elegidos (entre 1 y 365).</summary>
        public int Dias
        {
            get => (int)numDias.Value;
            set => numDias.Value = Math.Max(numDias.Minimum, Math.Min(numDias.Maximum, value));
        }

        public void Traducir(string titulo, string cada, string dias, string guardar, string cancelar)
        {
            Text             = titulo;
            lblCada.Text     = cada;
            lblDias.Text     = dias;
            btnGuardar.Text  = guardar;
            btnCancelar.Text = cancelar;
        }
    }
}
