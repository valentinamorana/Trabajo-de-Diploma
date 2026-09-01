using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, CU-CONT-03-Analizar Promoción. Actor: Contabilidad (rol
    /// nuevo, separado de Administración y de Gerencia).
    /// </summary>
    public partial class PromocionesContabilidadForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();

        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PromocionesContabilidadForm()
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
            CargarPromociones();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(lblObservacion, t);
            Aplicar(btnAprobar,     t);
            Aplicar(btnRechazar,    t);
            TraducirHeadersGrilla(t);
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        /// <summary>Renombra el HeaderText de las columnas de dgvPromociones según el idioma activo.</summary>
        private void TraducirHeadersGrilla(IDictionary<string, Traduccion> t)
        {
            void RH(string col, string clave)
            {
                if (dgvPromociones.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvPromociones.Columns[col].HeaderText = t[clave].Texto;
            }

            RH("ID",                 "col.promo.id");
            RH("Nombre",             "col.promo.nombre");
            RH("Aplica a",           "col.promo.aplicaa");
            RH("Tipo",               "col.promo.tipo");
            RH("Valor",              "col.promo.valor");
            RH("Margen Est.",        "col.promo.margenest");
            RH("Impacto Económico",  "col.promo.impactoeconomico");
        }

        private void PromocionesContabilidadForm_Load(object sender, EventArgs e)
        {
            CargarPromociones();
        }

        private void BtnRefrescar_Click(object sender, EventArgs e)
        {
            CargarPromociones();
        }

        private void CargarPromociones()
        {
            try
            {
                _promociones = promocionBLL.ObtenerPendientesRevisionContable();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Nombre", typeof(string));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Tipo", typeof(string));
                tabla.Columns.Add("Valor", typeof(decimal));
                tabla.Columns.Add("Margen Est.", typeof(decimal));
                tabla.Columns.Add("Impacto Económico", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(
                        p.IdPromocion, p.Nombre,
                        p.AplicaAPlan() ? $"Plan: {p.NombrePlan}" : $"Categoría: {p.CategoriaPrenda}",
                        p.TipoDescuento.ToString(), p.Valor, p.MargenEstimado, p.ImpactoEconomico ?? "—");

                dgvPromociones.DataSource = tabla;
                if (dgvPromociones.Columns.Contains("ID"))
                    dgvPromociones.Columns["ID"].Width = 44;
                TraducirHeadersGrilla(Traductor.ObtenerTraducciones(_idioma));

                lblConteo.Text = $"{_promociones.Count} promoción(es) pendiente(s) de revisión contable.";
                DeshabilitarBotones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void DgvPromociones_SelectionChanged(object sender, EventArgs e)
        {
            bool haySeleccion = dgvPromociones.SelectedRows.Count > 0;
            btnAprobar.Enabled = haySeleccion;
            btnRechazar.Enabled = haySeleccion;
        }

        private void DeshabilitarBotones()
        {
            btnAprobar.Enabled = false;
            btnRechazar.Enabled = false;
        }

        private BE.Promocion ObtenerSeleccionada()
        {
            if (dgvPromociones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPromociones.SelectedRows[0].Cells["ID"].Value);
            return _promociones.Find(p => p.IdPromocion == id);
        }

        private void BtnAprobar_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerSeleccionada();
            if (promocion == null) return;

            if (string.IsNullOrWhiteSpace(txtObservacion.Text))
            {
                MostrarError("Ingresá una observación antes de aprobar.");
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Aprobar y activar la promoción '{promocion.Nombre}'?",
                "Confirmar Aprobación", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                promocionBLL.AprobarContable(this.Text, promocion, txtObservacion.Text);
                MostrarOk($"Promoción '{promocion.Nombre}' aprobada y activada.");
                txtObservacion.Clear();
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnRechazar_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerSeleccionada();
            if (promocion == null) return;

            if (string.IsNullOrWhiteSpace(txtObservacion.Text))
            {
                MostrarError("Ingresá una observación antes de rechazar.");
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Rechazar la promoción '{promocion.Nombre}'? Vuelve a Administración para reformularla.",
                "Confirmar Rechazo", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                promocionBLL.RechazarContable(this.Text, promocion, txtObservacion.Text);
                MostrarOk($"Promoción '{promocion.Nombre}' rechazada.");
                txtObservacion.Clear();
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
