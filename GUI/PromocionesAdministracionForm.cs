using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, CU-ADM-Gestionar Promociones. Actor: Administración (rol
    /// AdministracionComercial). Consume las sugerencias de Gerencia, da de alta promociones
    /// (desde sugerencia o manual), las desactiva directamente si están Vigentes, y resuelve
    /// las solicitudes de baja que envía Ventas.
    /// </summary>
    public partial class PromocionesAdministracionForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly BLL.Interfaces.ISugerenciaPromocionService sugerenciaBLL = new BLL.SugerenciaPromocion();

        private List<BE.SugerenciaPromocion> _sugerencias = new List<BE.SugerenciaPromocion>();
        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PromocionesAdministracionForm()
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
            CargarTodo();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(btnUsarSugerencia,     t);
            Aplicar(btnNuevaManual,        t);
            Aplicar(btnDesactivar,         t);
            Aplicar(btnAprobarBaja,        t);
            Aplicar(btnRechazarBaja,       t);
            Aplicar(lblSugerenciasTitulo,  t);
            Aplicar(lblPromocionesTitulo,  t);
            TraducirHeadersSugerencias(t);
            TraducirHeadersPromociones(t);
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        /// <summary>Renombra el HeaderText de las columnas de dgvSugerencias según el idioma activo.</summary>
        private void TraducirHeadersSugerencias(IDictionary<string, Traduccion> t)
        {
            void RH(string col, string clave)
            {
                if (dgvSugerencias.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvSugerencias.Columns[col].HeaderText = t[clave].Texto;
            }

            RH("ID",            "col.promo.id");
            RH("Aplica a",      "col.promo.aplicaa");
            RH("Motivo",        "col.promo.motivo");
            RH("Tipo Sugerido", "col.promo.tiposugerido");
            RH("Beneficio Est.","col.promo.beneficioest");
        }

        /// <summary>Renombra el HeaderText de las columnas de dgvPromociones según el idioma activo.</summary>
        private void TraducirHeadersPromociones(IDictionary<string, Traduccion> t)
        {
            void RH(string col, string clave)
            {
                if (dgvPromociones.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvPromociones.Columns[col].HeaderText = t[clave].Texto;
            }

            RH("ID",          "col.promo.id");
            RH("Nombre",      "col.promo.nombre");
            RH("Aplica a",    "col.promo.aplicaa");
            RH("Estado",      "col.promo.estado");
            RH("Motivo Baja", "col.promo.motivobaja");
        }

        private void PromocionesAdministracionForm_Load(object sender, EventArgs e)
        {
            CargarTodo();
        }

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarTodo();

        private void CargarTodo()
        {
            CargarSugerencias();
            CargarPromociones();
        }

        private void CargarSugerencias()
        {
            try
            {
                _sugerencias = sugerenciaBLL.ObtenerPendientes();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Motivo", typeof(string));
                tabla.Columns.Add("Tipo Sugerido", typeof(string));
                tabla.Columns.Add("Beneficio Est.", typeof(decimal));

                foreach (var s in _sugerencias)
                    tabla.Rows.Add(
                        s.IdSugerencia,
                        s.AplicaAPlan() ? $"Plan: {s.NombrePlan}" : $"Categoría: {s.CategoriaPrenda}",
                        s.Motivo, s.TipoDescuentoSugerido.ToString(), s.BeneficioEstimado);

                dgvSugerencias.DataSource = tabla;
                if (dgvSugerencias.Columns.Contains("ID")) dgvSugerencias.Columns["ID"].Width = 44;
                TraducirHeadersSugerencias(Traductor.ObtenerTraducciones(_idioma));

                btnUsarSugerencia.Enabled = false;
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void CargarPromociones()
        {
            try
            {
                _promociones = promocionBLL.ObtenerTodas();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Nombre", typeof(string));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Estado", typeof(string));
                tabla.Columns.Add("Motivo Baja", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(
                        p.IdPromocion, p.Nombre,
                        p.AplicaAPlan() ? $"Plan: {p.NombrePlan}" : $"Categoría: {p.CategoriaPrenda}",
                        p.Estado.ToString(), p.MotivoBaja ?? "—");

                dgvPromociones.DataSource = tabla;
                if (dgvPromociones.Columns.Contains("ID")) dgvPromociones.Columns["ID"].Width = 44;
                TraducirHeadersPromociones(Traductor.ObtenerTraducciones(_idioma));

                var tCnt = Traductor.ObtenerTraducciones(_idioma);
                lblConteo.Text = string.Format(
                    tCnt.ContainsKey("promo.conteo") ? tCnt["promo.conteo"].Texto : "{0} promoción(es) en total.",
                    _promociones.Count);
                DeshabilitarBotonesPromocion();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void DgvSugerencias_SelectionChanged(object sender, EventArgs e)
        {
            btnUsarSugerencia.Enabled = dgvSugerencias.SelectedRows.Count > 0;
        }

        private BE.SugerenciaPromocion ObtenerSugerenciaSeleccionada()
        {
            if (dgvSugerencias.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvSugerencias.SelectedRows[0].Cells["ID"].Value);
            return _sugerencias.Find(s => s.IdSugerencia == id);
        }

        private void BtnUsarSugerencia_Click(object sender, EventArgs e)
        {
            var sugerencia = ObtenerSugerenciaSeleccionada();
            if (sugerencia == null) return;
            AbrirAltaPromocion(sugerencia);
        }

        private void BtnNuevaManual_Click(object sender, EventArgs e) => AbrirAltaPromocion(null);

        private void AbrirAltaPromocion(BE.SugerenciaPromocion sugerencia)
        {
            using (var form = new AltaPromocionForm(sugerencia))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var t = Traductor.ObtenerTraducciones(_idioma);
                    MostrarOk(string.Format(
                        t.ContainsKey("msg.promo.registrada") ? t["msg.promo.registrada"].Texto : "Promoción #{0} registrada.",
                        form.IdPromocionCreada));
                    CargarTodo();
                }
            }
        }

        private void DgvPromociones_SelectionChanged(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) { DeshabilitarBotonesPromocion(); return; }

            btnDesactivar.Enabled = promocion.PuedeDesactivarseDirecto();
            btnAprobarBaja.Enabled = promocion.PuedeResolverseBaja();
            btnRechazarBaja.Enabled = promocion.PuedeResolverseBaja();
        }

        private void DeshabilitarBotonesPromocion()
        {
            btnDesactivar.Enabled = false;
            btnAprobarBaja.Enabled = false;
            btnRechazarBaja.Enabled = false;
        }

        private BE.Promocion ObtenerPromocionSeleccionada()
        {
            if (dgvPromociones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPromociones.SelectedRows[0].Cells["ID"].Value);
            return _promociones.Find(p => p.IdPromocion == id);
        }

        private void BtnDesactivar_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;

            var t = Traductor.ObtenerTraducciones(_idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            var confirmar = MessageBox.Show(
                string.Format(T("conf.promo.desactivar.msg", "¿Desactivar la promoción '{0}'?"), promocion.Nombre),
                T("conf.promo.desactivar.titulo", "Confirmar Desactivación"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                promocionBLL.Desactivar(this.Text, promocion);
                MostrarOk(string.Format(T("msg.promo.desactivada", "Promoción '{0}' desactivada."), promocion.Nombre));
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnAprobarBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;

            var t = Traductor.ObtenerTraducciones(_idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            var confirmar = MessageBox.Show(
                string.Format(T("conf.promo.aprobarbaja.msg", "¿Aprobar la baja de '{0}' sugerida por Ventas?\nMotivo: {1}"),
                    promocion.Nombre, promocion.MotivoBaja),
                T("conf.promo.aprobarbaja.titulo", "Confirmar Baja"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                promocionBLL.AprobarBaja(this.Text, promocion);
                MostrarOk(string.Format(T("msg.promo.dadabaja", "Promoción '{0}' dada de baja."), promocion.Nombre));
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnRechazarBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;

            var t = Traductor.ObtenerTraducciones(_idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            string motivo;
            using (var dlg = new InputDialog(
                T("inputdlg.rechazarbaja.titulo", "Rechazar Baja de Promoción"),
                string.Format(T("inputdlg.rechazarbaja.prompt", "Motivo por el cual '{0}' sigue vigente:"), promocion.Nombre),
                esPassword: false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText;
            }
            if (string.IsNullOrWhiteSpace(motivo)) return;

            try
            {
                promocionBLL.RechazarBaja(this.Text, promocion, motivo);
                MostrarOk(string.Format(T("msg.promo.bajarechazada", "Se rechazó la baja de '{0}': sigue vigente."), promocion.Nombre));
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
