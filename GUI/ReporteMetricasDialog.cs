using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// PN03 — «Reporte de métricas» de Sugerir promoción: el texto del reporte, la lista de
    /// oportunidades para elegir una y los botones Usar esta idea, Imprimir reporte y Cerrar.
    /// Hecho con el Diseñador; SugerirPromocionForm carga el contenido y arma la impresión.
    /// </summary>
    public partial class ReporteMetricasDialog : Form
    {
        /// <summary>Se levanta con «Imprimir reporte».</summary>
        public event EventHandler Imprimir;

        public ReporteMetricasDialog()
        {
            InitializeComponent();
        }

        /// <param name="formato">Texto de cada oportunidad en la lista.</param>
        public void Configurar(string titulo, string texto, IEnumerable<BE.CandidataSugerencia> oportunidades, bool hayOportunidad,
                               Func<BE.CandidataSugerencia, string> formato, string usar, string imprimir, string cerrar)
        {
            Text                     = titulo;
            txtReporte.Text          = texto;
            btnUsar.Text             = usar;
            btnImprimir.Text         = imprimir;
            btnCerrar.Text           = cerrar;
            lstOportunidades.Visible = hayOportunidad;
            btnUsar.Visible          = hayOportunidad;
            AcceptButton             = hayOportunidad ? btnUsar : null;
            lstOportunidades.Format += (s, ev) =>
            {
                if (ev.ListItem is BE.CandidataSugerencia c && formato != null) ev.Value = formato(c);
            };
            lstOportunidades.Items.Clear();
            if (oportunidades != null)
                foreach (var c in oportunidades) lstOportunidades.Items.Add(c);
            if (lstOportunidades.Items.Count > 0) lstOportunidades.SelectedIndex = 0;
        }

        public BE.CandidataSugerencia OportunidadElegida => lstOportunidades.SelectedItem as BE.CandidataSugerencia;

        private void BtnImprimir_Click(object sender, EventArgs e) => Imprimir?.Invoke(this, EventArgs.Empty);
    }
}
