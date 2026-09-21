using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Diálogo simple para seleccionar el nuevo estado de una prenda.
    /// Recibe las opciones válidas desde Prendas.cs y devuelve el estado elegido.
    /// </summary>
    public partial class CambioEstadoDialog : FormBase
    {
        public BE.EstadoPrenda EstadoSeleccionado { get; private set; }

        private readonly List<(string texto, BE.EstadoPrenda estado)> _opciones;

        public CambioEstadoDialog(BE.Prenda prenda,
            List<(string texto, BE.EstadoPrenda estado)> opciones,
            string estadoTraducido = null)
        {
            InitializeComponent();
            _opciones = opciones;

            // Traducir controles del Designer
            this.Text              = Tr("frm.cambioestado",     "Cambiar Estado de Prenda");
            lblNuevoEstado.Text    = Tr("lbl.nuevoestado",      "Nuevo estado:");
            btnConfirmar.Text      = Tr("btn.confirmar.cambio", "Confirmar Cambio");
            btnCancelar.Text       = Tr("btn.cancelar",         "Cancelar");

            string fmtInfo = Tr("lbl.cambioest.info", "Prenda: {0}  —  Estado actual: {1}");
            string estadoLabel = estadoTraducido ?? prenda.Estado.ToString();
            lblPrendaInfo.Text = string.Format(fmtInfo, prenda.Nombre, estadoLabel);

            foreach (var op in _opciones)
                cmbOpciones.Items.Add(op.texto);
            if (cmbOpciones.Items.Count > 0)
                cmbOpciones.SelectedIndex = 0;
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            int idx = cmbOpciones.SelectedIndex;
            if (idx < 0) { lblMensaje.Text = Tr("msg.cambioest.selecciona", "Seleccioná una opción."); return; }

            EstadoSeleccionado = _opciones[idx].estado;

            // Confirmación extra para Baja (irreversible)
            if (EstadoSeleccionado == BE.EstadoPrenda.Baja)
            {
                var conf = MessageBox.Show(
                    Tr("msg.cambioest.bajairrev", "La baja es irreversible. ¿Confirmar?"),
                    Tr("conf.baja.titulo", "Dar de Baja"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (conf != DialogResult.Yes) return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
