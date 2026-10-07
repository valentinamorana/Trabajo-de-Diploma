using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, carril Administración: "Crear promoción" (desde una
    /// sugerencia de Gerencia o manual) y "Reformular" una promoción rechazada por Contabilidad.
    /// La actividad "Validar" y el paso a EnRevisionContable son de BLL.Promocion.
    /// </summary>
    public partial class AltaPromocionForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly BLL.Interfaces.IPlanSuscripcionService planBLL = new BLL.PlanSuscripcion();

        private readonly BE.SugerenciaPromocion _sugerenciaOrigen;
        // PN03: si viene una promoción Rechazada por Contabilidad, el formulario la reformula.
        private readonly BE.Promocion _reformular;
        private List<BE.PlanSuscripcion> _planes = new List<BE.PlanSuscripcion>();

        public int IdPromocionCreada { get; private set; }

        /// <param name="sugerenciaOrigen">Si viene de una sugerencia de Gerencia, precarga plan/categoría
        /// y los bloquea (no se puede cambiar el destino de la sugerencia). Null para alta manual.</param>
        public AltaPromocionForm(BE.SugerenciaPromocion sugerenciaOrigen = null, BE.Promocion promocionAReformular = null)
        {
            InitializeComponent();
            _sugerenciaOrigen = sugerenciaOrigen;
            _reformular = promocionAReformular;
        }

        // ── Observer de idioma ────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
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
                // Categorías del catálogo como sugerencias (se puede escribir otra).
                cmbCategoria.Items.Clear();
                foreach (string categoria in new BLL.Prenda().ObtenerCategorias()) cmbCategoria.Items.Add(categoria);
                // Se muestra traducido ("Monto fijo"); el ítem sigue siendo el enum.
                cmbTipoDescuento.FormattingEnabled = true;
                cmbTipoDescuento.Format -= FormatearTipo;
                cmbTipoDescuento.Format += FormatearTipo;
                dtpInicio.Value = DateTime.Today;
                dtpFin.Value = DateTime.Today.AddMonths(1);

                if (_reformular != null)
                {
                    lblSugerencia.Text = Tr("promo.reformular", "Reformulando la promoción #{0}. Observación de Contabilidad: {1}",
                        new object[] { _reformular.IdPromocion, _reformular.Observacion ?? "—" });
                    rbPlan.Checked = _reformular.AplicaAPlan();
                    rbCategoria.Checked = _reformular.AplicaACategoria();
                    if (_reformular.AplicaAPlan()) cmbPlan.SelectedValue = _reformular.IdPlan.Value;
                    else cmbCategoria.Text = _reformular.CategoriaPrenda;
                    rbPlan.Enabled = false;
                    rbCategoria.Enabled = false;
                    cmbPlan.Enabled = _reformular.AplicaAPlan();
                    cmbCategoria.Enabled = _reformular.AplicaACategoria();
                    txtNombre.Text = _reformular.Nombre;
                    txtDescripcion.Text = _reformular.Descripcion;
                    cmbTipoDescuento.SelectedItem = _reformular.TipoDescuento;
                    numValor.Value = Math.Min(numValor.Maximum, _reformular.Valor);
                    dtpInicio.Value = _reformular.FechaInicio < dtpInicio.MinDate ? dtpInicio.MinDate : _reformular.FechaInicio;
                    dtpFin.Value = _reformular.FechaFin < dtpFin.MinDate ? dtpFin.MinDate : _reformular.FechaFin;
                    numMargenEstimado.Value = Math.Min(numMargenEstimado.Maximum, Math.Max(numMargenEstimado.Minimum, _reformular.MargenEstimado));
                    txtImpactoEconomico.Text = _reformular.ImpactoEconomico;
                }
                else if (_sugerenciaOrigen != null)
                {
                    lblSugerencia.Text = Tr("promo.sugerenciaorigen", "A partir de la sugerencia #{0}: {1}",
                        new object[] { _sugerenciaOrigen.IdSugerencia, _sugerenciaOrigen.Motivo });
                    rbPlan.Checked = _sugerenciaOrigen.AplicaAPlan();
                    rbCategoria.Checked = _sugerenciaOrigen.AplicaACategoria();
                    if (_sugerenciaOrigen.AplicaAPlan()) cmbPlan.SelectedValue = _sugerenciaOrigen.IdPlan.Value;
                    else cmbCategoria.Text = _sugerenciaOrigen.CategoriaPrenda;
                    rbPlan.Enabled = false;
                    rbCategoria.Enabled = false;
                    cmbPlan.Enabled = _sugerenciaOrigen.AplicaAPlan();
                    cmbCategoria.Enabled = _sugerenciaOrigen.AplicaACategoria();
                    cmbTipoDescuento.SelectedItem = _sugerenciaOrigen.TipoDescuentoSugerido;
                    numValor.Value = Math.Min(numValor.Maximum, _sugerenciaOrigen.ValorInicialPromocion());
                }
                else
                {
                    lblSugerencia.Text = Tr("promo.altamanual", "Alta manual (sin sugerencia de Gerencia).");
                    RbPlan_CheckedChanged(this, EventArgs.Empty);
                }
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void RbPlan_CheckedChanged(object sender, EventArgs e)
        {
            cmbPlan.Enabled = rbPlan.Checked;
            cmbCategoria.Enabled = !rbPlan.Checked;
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            var tipo = (BE.TipoDescuento)cmbTipoDescuento.SelectedItem;

            // Sin validaciones acá: la actividad "Validar" es BLL.Promocion.ValidarPromocion
            // (destino único, valor y fechas); si falla, se muestra su mensaje traducido.
            try
            {
                if (_reformular != null)
                {
                    _reformular.Nombre = txtNombre.Text;
                    _reformular.Descripcion = txtDescripcion.Text;
                    _reformular.TipoDescuento = tipo;
                    _reformular.Valor = numValor.Value;
                    _reformular.FechaInicio = dtpInicio.Value;
                    _reformular.FechaFin = dtpFin.Value;
                    _reformular.MargenEstimado = numMargenEstimado.Value;
                    _reformular.ImpactoEconomico = txtImpactoEconomico.Text;
                    promocionBLL.Reformular(this.Text, _reformular);
                    IdPromocionCreada = _reformular.IdPromocion;
                }
                else if (_sugerenciaOrigen != null)
                {
                    IdPromocionCreada = promocionBLL.CrearDesdeSugerencia(this.Text, _sugerenciaOrigen.IdSugerencia,
                        txtNombre.Text, txtDescripcion.Text, tipo, numValor.Value,
                        dtpInicio.Value, dtpFin.Value, numMargenEstimado.Value, txtImpactoEconomico.Text);
                }
                else
                {
                    int? idPlan = rbPlan.Checked ? (int?)cmbPlan.SelectedValue : null;
                    string categoria = rbPlan.Checked ? null : cmbCategoria.Text;
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

        private static void FormatearTipo(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is BE.TipoDescuento t) e.Value = Exportacion.DocumentosPromocion.Tipo(t);
        }
    }
}
