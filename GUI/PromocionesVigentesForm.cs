using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;
using Docs = GUI.Exportacion.DocumentosPromocion;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, carril Vendedor de la región de vigencia: consulta las
    /// promociones Vigentes (y las que tienen la baja pedida) y puede solicitar la baja de una
    /// Vigente con motivo («Solicitud de baja»). Imprime la solicitud y la «Resolución de baja»
    /// (informe a Ventas cuando Administración la rechaza).
    /// </summary>
    public partial class PromocionesVigentesForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly MenuDocumentosPromocion menuDocumentos;

        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PromocionesVigentesForm()
        {
            InitializeComponent();
            // Paleta centralizada (GUI/Tema.cs).
            btnSugerirBaja.BackColor = Tema.RosaPrimario;
            btnImprimir.BackColor = Tema.RosaOscuro;
            menuDocumentos = new MenuDocumentosPromocion(promocionBLL, ObtenerSeleccionada, this,
                ex => MostrarError(ex), (k, f) => Tr(k, f));
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
            CargarPromociones();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            foreach (Control c in panelTop.Controls)
                if (c.Tag != null && t.ContainsKey(c.Tag.ToString()))
                    c.Text = t[c.Tag.ToString()].Texto;
            tip.SetToolTip(btnRefrescar, Tr("tip.actualizar", "Actualizar"));
            btnRefrescar.Text = Tr("tip.actualizar", "Actualizar");
            menuDocumentos.Traducir();
            TraducirHeadersGrilla();
        }

        private void TraducirHeadersGrilla()
        {
            void RH(string col, string clave, string fb)
            {
                if (dgvPromociones.Columns.Contains(col)) dgvPromociones.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("ID", "col.promo.id", "ID");
            RH("Nombre", "col.promo.nombre", "Nombre");
            RH("Aplica a", "col.promo.aplicaa", "Aplica a");
            RH("Tipo", "col.promo.tipo", "Tipo");
            RH("Valor", "col.promo.valor", "Valor");
            RH("Vigencia", "col.promo.vigencia", "Vigencia");
            RH("Estado", "col.promo.estado", "Estado");
        }

        private void PromocionesVigentesForm_Load(object sender, EventArgs e) => CargarPromociones();

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarPromociones();

        private void CargarPromociones()
        {
            try
            {
                _promociones = promocionBLL.ObtenerParaVentas();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Nombre", typeof(string));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Tipo", typeof(string));
                tabla.Columns.Add("Valor", typeof(decimal));
                tabla.Columns.Add("Vigencia", typeof(string));
                tabla.Columns.Add("Estado", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(p.IdPromocion, p.Nombre, Docs.AplicaA(p.IdPlan, p.NombrePlan, p.CategoriaPrenda),
                        Docs.Tipo(p.TipoDescuento), p.Valor, $"{p.FechaInicio:d} - {p.FechaFin:d}",
                        Docs.Estado(p.Estado));

                dgvPromociones.DataSource = tabla;
                if (dgvPromociones.Columns.Contains("ID")) dgvPromociones.Columns["ID"].Width = 44;
                TraducirHeadersGrilla();

                lblConteo.Text = Tr("promo.conteo.vigentes", "{0} promoción(es) vigente(s).", new object[] { _promociones.Count });
                dgvPromociones.ClearSelection();
                Habilitar(null);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.Promocion ObtenerSeleccionada()
        {
            if (dgvPromociones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPromociones.SelectedRows[0].Cells["ID"].Value);
            return _promociones.Find(p => p.IdPromocion == id);
        }

        private void DgvPromociones_SelectionChanged(object sender, EventArgs e) => Habilitar(ObtenerSeleccionada());

        private void Habilitar(BE.Promocion p)
        {
            btnSugerirBaja.Enabled = p != null && p.PuedeSolicitarseBaja();
            btnImprimir.Enabled = p != null;
        }

        // (a) Vendedor solicita la baja (motivo obligatorio) → «Solicitud de baja».
        private void BtnSugerirBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerSeleccionada();
            if (promocion == null) return;

            string motivo;
            using (var dlg = new InputDialog(
                Tr("dlg.promo.solicitarbaja.titulo", "Solicitar baja de promoción"),
                Tr("inputdlg.sugerirbaja.prompt", "Motivo para sugerir la baja de '{0}':", new object[] { promocion.Nombre }),
                esPassword: false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText;
            }

            try
            {
                promocionBLL.SolicitarBaja(this.Text, promocion, motivo);
                MostrarOk(Tr("msg.promo.sugerenciabaja_enviada", "Se envió a Administración la sugerencia de baja de '{0}'.", new object[] { promocion.Nombre }));
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
            // La solicitud de baja está en el menú "Imprimir".
        }

        private void BtnImprimir_Click(object sender, EventArgs e) => menuDocumentos.Mostrar(btnImprimir);
    }
}
