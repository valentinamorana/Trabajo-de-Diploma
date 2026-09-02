using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, CU-ADM-Gestionar Promociones (alta, desde una sugerencia de
    /// Gerencia o manual). Actor: Administración (rol AdministracionComercial).
    /// </summary>
    public partial class AltaPromocionForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly BLL.Interfaces.IPlanSuscripcionService planBLL = new BLL.PlanSuscripcion();

        private readonly BE.SugerenciaPromocion _sugerenciaOrigen;
        private List<BE.PlanSuscripcion> _planes = new List<BE.PlanSuscripcion>();

        public int IdPromocionCreada { get; private set; }

        /// <param name="sugerenciaOrigen">Si viene de una sugerencia de Gerencia, precarga plan/categoría
        /// y los bloquea (no se puede cambiar el destino de la sugerencia). Null para alta manual.</param>
        public AltaPromocionForm(BE.SugerenciaPromocion sugerenciaOrigen = null)
        {
            InitializeComponent();
            _sugerenciaOrigen = sugerenciaOrigen;
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
            Aplicar(lblNombre,           t);
            Aplicar(lblDescripcion,      t);
            Aplicar(rbPlan,              t);
            Aplicar(rbCategoria,         t);
            Aplicar(lblTipoDescuento,    t);
            Aplicar(lblValor,            t);
            Aplicar(lblInicio,           t);
            Aplicar(lblFin,              t);
            Aplicar(lblMargenEstimado,   t);
            Aplicar(lblImpactoEconomico, t);
            Aplicar(btnConfirmar,        t);
            Aplicar(btnCancelar,         t);
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        private void AltaPromocionForm_Load(object sender, EventArgs e)
        {
            try
            {
                _planes = planBLL.ObtenerActivos();
                cmbPlan.DataSource = _planes;
                cmbPlan.DisplayMember = nameof(BE.PlanSuscripcion.Nombre);
                cmbPlan.ValueMember = nameof(BE.PlanSuscripcion.IdPlan);
                cmbTipoDescuento.DataSource = Enum.GetValues(typeof(BE.TipoDescuento));
                dtpInicio.Value = DateTime.Today;
                dtpFin.Value = DateTime.Today.AddMonths(1);

                var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

                if (_sugerenciaOrigen != null)
                {
                    lblSugerencia.Text = string.Format(
                        T("promo.sugerenciaorigen", "A partir de la sugerencia #{0}: {1}"),
                        _sugerenciaOrigen.IdSugerencia, _sugerenciaOrigen.Motivo);
                    rbPlan.Checked = _sugerenciaOrigen.AplicaAPlan();
                    rbCategoria.Checked = _sugerenciaOrigen.AplicaACategoria();
                    if (_sugerenciaOrigen.AplicaAPlan()) cmbPlan.SelectedValue = _sugerenciaOrigen.IdPlan.Value;
                    else txtCategoria.Text = _sugerenciaOrigen.CategoriaPrenda;
                    rbPlan.Enabled = false;
                    rbCategoria.Enabled = false;
                    cmbPlan.Enabled = _sugerenciaOrigen.AplicaAPlan();
                    txtCategoria.Enabled = _sugerenciaOrigen.AplicaACategoria();
                    cmbTipoDescuento.SelectedItem = _sugerenciaOrigen.TipoDescuentoSugerido;
                    numValor.Value = _sugerenciaOrigen.BeneficioEstimado;
                }
                else
                {
                    lblSugerencia.Text = T("promo.altamanual", "Alta manual (sin sugerencia de Gerencia).");
                    RbPlan_CheckedChanged(this, EventArgs.Empty);
                }
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void RbPlan_CheckedChanged(object sender, EventArgs e)
        {
            cmbPlan.Enabled = rbPlan.Checked;
            txtCategoria.Enabled = !rbPlan.Checked;
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            try
            {
                var tipo = (BE.TipoDescuento)cmbTipoDescuento.SelectedItem;

                if (_sugerenciaOrigen != null)
                {
                    IdPromocionCreada = promocionBLL.CrearDesdeSugerencia(this.Text, _sugerenciaOrigen.IdSugerencia,
                        txtNombre.Text, txtDescripcion.Text, tipo, numValor.Value,
                        dtpInicio.Value, dtpFin.Value, numMargenEstimado.Value, txtImpactoEconomico.Text);
                }
                else
                {
                    int? idPlan = rbPlan.Checked ? (int?)cmbPlan.SelectedValue : null;
                    string categoria = rbPlan.Checked ? null : txtCategoria.Text;
                    IdPromocionCreada = promocionBLL.CrearManual(this.Text, txtNombre.Text, txtDescripcion.Text,
                        tipo, numValor.Value, dtpInicio.Value, dtpFin.Value, idPlan, categoria,
                        numMargenEstimado.Value, txtImpactoEconomico.Text);
                }

                var tOk = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                MostrarOk(string.Format(
                    tOk.ContainsKey("msg.promo.creada") ? tOk["msg.promo.creada"].Texto : "Promoción #{0} registrada, pendiente de revisión contable.",
                    IdPromocionCreada));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
