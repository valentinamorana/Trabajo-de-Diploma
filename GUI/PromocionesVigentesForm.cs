using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, CU-VEND-04-Sugerir Baja de Promoción. Actor: Vendedor
    /// (consulta las promociones Vigentes y puede sugerir que Administración dé de baja una).
    /// </summary>
    public partial class PromocionesVigentesForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();

        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PromocionesVigentesForm()
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
            Aplicar(btnSugerirBaja, t);
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

            RH("ID",       "col.promo.id");
            RH("Nombre",   "col.promo.nombre");
            RH("Aplica a", "col.promo.aplicaa");
            RH("Tipo",     "col.promo.tipo");
            RH("Valor",    "col.promo.valor");
            RH("Vigencia", "col.promo.vigencia");
        }

        private void PromocionesVigentesForm_Load(object sender, EventArgs e)
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
                _promociones = promocionBLL.ObtenerVigentes();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Nombre", typeof(string));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Tipo", typeof(string));
                tabla.Columns.Add("Valor", typeof(decimal));
                tabla.Columns.Add("Vigencia", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(
                        p.IdPromocion, p.Nombre,
                        p.AplicaAPlan() ? $"Plan: {p.NombrePlan}" : $"Categoría: {p.CategoriaPrenda}",
                        p.TipoDescuento.ToString(), p.Valor,
                        $"{p.FechaInicio:dd/MM/yyyy} - {p.FechaFin:dd/MM/yyyy}");

                dgvPromociones.DataSource = tabla;
                if (dgvPromociones.Columns.Contains("ID"))
                    dgvPromociones.Columns["ID"].Width = 44;
                TraducirHeadersGrilla(Traductor.ObtenerTraducciones(_idioma));

                lblConteo.Text = $"{_promociones.Count} promoción(es) vigente(s).";
                btnSugerirBaja.Enabled = false;
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void DgvPromociones_SelectionChanged(object sender, EventArgs e)
        {
            btnSugerirBaja.Enabled = dgvPromociones.SelectedRows.Count > 0;
        }

        private BE.Promocion ObtenerSeleccionada()
        {
            if (dgvPromociones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPromociones.SelectedRows[0].Cells["ID"].Value);
            return _promociones.Find(p => p.IdPromocion == id);
        }

        private void BtnSugerirBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerSeleccionada();
            if (promocion == null) return;

            string motivo;
            using (var dlg = new InputDialog("Sugerir Baja de Promoción",
                $"Motivo para sugerir la baja de '{promocion.Nombre}':", esPassword: false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText;
            }
            if (string.IsNullOrWhiteSpace(motivo)) return;

            try
            {
                promocionBLL.SugerirBaja(this.Text, promocion, motivo);
                MostrarOk($"Se envió a Administración la sugerencia de baja de '{promocion.Nombre}'.");
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
