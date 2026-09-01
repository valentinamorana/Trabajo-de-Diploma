using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, CU-GE-01-Sugerir Promoción a la Administración.
    /// Actor: GerenteComercial (Gerencia reusa este rol ya existente).
    /// </summary>
    public partial class SugerirPromocionForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.ISugerenciaPromocionService sugerenciaBLL = new BLL.SugerenciaPromocion();
        private readonly BLL.Interfaces.IPlanSuscripcionService planBLL = new BLL.PlanSuscripcion();

        private List<BE.PlanSuscripcion> _planes = new List<BE.PlanSuscripcion>();

        public SugerirPromocionForm()
        {
            InitializeComponent();
        }

        // ── Observer de idioma ────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
        }

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(rbPlan,              t);
            Aplicar(rbCategoria,         t);
            Aplicar(lblTipoDescuento,    t);
            Aplicar(lblBeneficioEstimado, t);
            Aplicar(lblMotivo,           t);
            Aplicar(btnEnviar,           t);
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        private void SugerirPromocionForm_Load(object sender, EventArgs e)
        {
            try
            {
                _planes = planBLL.ObtenerActivos();
                cmbPlan.DataSource = _planes;
                cmbPlan.DisplayMember = nameof(BE.PlanSuscripcion.Nombre);
                cmbPlan.ValueMember = nameof(BE.PlanSuscripcion.IdPlan);
                cmbTipoDescuento.DataSource = Enum.GetValues(typeof(BE.TipoDescuento));
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void RbPlan_CheckedChanged(object sender, EventArgs e)
        {
            cmbPlan.Enabled = rbPlan.Checked;
            txtCategoria.Enabled = !rbPlan.Checked;
        }

        private void BtnEnviar_Click(object sender, EventArgs e)
        {
            try
            {
                int? idPlan = rbPlan.Checked ? (int?)cmbPlan.SelectedValue : null;
                string categoria = rbPlan.Checked ? null : txtCategoria.Text;
                var tipo = (BE.TipoDescuento)cmbTipoDescuento.SelectedItem;
                decimal beneficio = numBeneficioEstimado.Value;

                int id = sugerenciaBLL.Crear(this.Text, idPlan, categoria, txtMotivo.Text, tipo, beneficio);

                MostrarOk($"Sugerencia #{id} enviada a Administración.");
                txtMotivo.Clear();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
