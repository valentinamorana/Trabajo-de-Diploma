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

        private readonly BLL.AnalisisPromociones analisisBLL = new BLL.AnalisisPromociones();
        private Button btnDesdeAnalisis;

        public SugerirPromocionForm()
        {
            InitializeComponent();
            CrearBotonDesdeAnalisis();
        }

        // PN03: en vez de partir de una hoja en blanco, Gerencia puede cargar una idea detectada por los
        // reportes de rotación y abandono (un dato concreto) y ajustarla antes de enviarla.
        private void CrearBotonDesdeAnalisis()
        {
            btnDesdeAnalisis = new Button
            {
                Name = "btnDesdeAnalisis",
                Tag = "promocion.btn.desdeanalisis",
                Text = "Desde el análisis…",
                Location = new System.Drawing.Point(btnEnviar.Right + 8, btnEnviar.Top),
                Size = new System.Drawing.Size(160, btnEnviar.Height)
            };
            btnDesdeAnalisis.Click += BtnDesdeAnalisis_Click;
            Controls.Add(btnDesdeAnalisis);
        }

        private void BtnDesdeAnalisis_Click(object sender, EventArgs e)
        {
            try
            {
                var candidatas = analisisBLL.Detectar();
                if (candidatas.Count == 0)
                {
                    MostrarOk(Tr("msg.sugerencia.sinanalisis",
                        "Los reportes de rotación y abandono no detectan casos para sugerir por ahora."));
                    return;
                }

                var elegida = ElegirCandidata(candidatas);
                if (elegida == null) return;

                if (elegida.IdPlan.HasValue)
                {
                    rbPlan.Checked = true;
                    cmbPlan.SelectedValue = elegida.IdPlan.Value;
                }
                else
                {
                    rbCategoria.Checked = true;
                    txtCategoria.Text = elegida.CategoriaPrenda;
                }
                cmbTipoDescuento.SelectedItem = elegida.TipoSugerido;
                numBeneficioEstimado.Value = Math.Min(numBeneficioEstimado.Maximum, elegida.BeneficioEstimado);
                txtMotivo.Text = elegida.Motivo;
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.CandidataSugerencia ElegirCandidata(List<BE.CandidataSugerencia> candidatas)
        {
            using (var dlg = new Form
            {
                Text = Tr("promocion.desdeanalisis.titulo", "Ideas detectadas por los reportes"),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new System.Drawing.Size(560, 300)
            })
            {
                var lista = new ListBox { Dock = DockStyle.Top, Height = 170, DisplayMember = nameof(BE.CandidataSugerencia.Resumen) };
                foreach (var c in candidatas) lista.Items.Add(c);
                lista.SelectedIndex = 0;

                var detalle = new Label { Dock = DockStyle.Fill, Padding = new Padding(8) };
                lista.SelectedIndexChanged += (s, ev) =>
                {
                    var c = lista.SelectedItem as BE.CandidataSugerencia;
                    detalle.Text = c == null ? "" : c.Motivo + "\n\n" + Tr("promocion.desdeanalisis.beneficio",
                        "Beneficio estimado inicial: {0:C2} (editable antes de enviar).", new object[] { c.BeneficioEstimado });
                };
                detalle.Text = candidatas[0].Motivo + "\n\n" + Tr("promocion.desdeanalisis.beneficio",
                    "Beneficio estimado inicial: {0:C2} (editable antes de enviar).", new object[] { candidatas[0].BeneficioEstimado });

                var ok = new Button { Text = Tr("promocion.desdeanalisis.usar", "Usar esta idea"), DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 150 };
                var cancelar = new Button { Text = Tr("btn.cancelar", "Cancelar"), DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 100 };
                var panel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
                panel.Controls.Add(ok);
                panel.Controls.Add(cancelar);

                dlg.Controls.Add(detalle);
                dlg.Controls.Add(panel);
                dlg.Controls.Add(lista);
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancelar;

                return dlg.ShowDialog(this) == DialogResult.OK ? lista.SelectedItem as BE.CandidataSugerencia : null;
            }
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
            Aplicar(btnDesdeAnalisis, t);
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

                var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                MostrarOk(string.Format(
                    t.ContainsKey("msg.sugerencia.enviada") ? t["msg.sugerencia.enviada"].Texto : "Sugerencia #{0} enviada a Administración.",
                    id));
                txtMotivo.Clear();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
