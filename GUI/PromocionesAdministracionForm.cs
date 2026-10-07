using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;
using Docs = GUI.Exportacion.DocumentosPromocion;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, carril Administración del diagrama de actividad (rol
    /// AdministracionComercial):
    ///   ¿Acepta la sugerencia? Sí: Crear promoción desde la sugerencia; No: Descartar sugerencia.
    ///   Crear promoción manual. ¿Reformular? Sí: Reformular; No: Descartar promoción.
    ///   ¿Aprueba la baja? Sí/No («Resolución de baja»). Desactivar directamente.
    /// Imprime la sugerencia, la ficha, el dictamen, la solicitud y la resolución de baja y las
    /// constancias de descarte. Solo muestra datos y llama a la BLL: las guardas viven en
    /// BLL.Promocion / BLL.SugerenciaPromocion y los predicados en BE.
    /// </summary>
    public partial class PromocionesAdministracionForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly BLL.Interfaces.ISugerenciaPromocionService sugerenciaBLL = new BLL.SugerenciaPromocion();
        private readonly MenuDocumentosPromocion menuDocumentos;

        private List<BE.SugerenciaPromocion> _sugerencias = new List<BE.SugerenciaPromocion>();
        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PromocionesAdministracionForm()
        {
            InitializeComponent();
            // Paleta centralizada (GUI/Tema.cs).
            btnUsarSugerencia.BackColor = btnNuevaManual.BackColor = btnReformular.BackColor = Tema.RosaPrimario;
            btnAprobarBaja.BackColor = Tema.Exito;
            btnDescartarSugerencia.BackColor = btnDescartarPromocion.BackColor = btnDesactivar.BackColor = Tema.Error;
            btnRechazarBaja.BackColor = Tema.Alerta;
            btnImprimirSugerencia.BackColor = btnImprimir.BackColor = Tema.RosaOscuro;
            btnHistorial.BackColor = Tema.TextoMuted;
            menuDocumentos = new MenuDocumentosPromocion(promocionBLL, ObtenerPromocionSeleccionada, this,
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
            CargarTodo();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            foreach (Control c in new Control[] { lblSugerenciasTitulo, lblPromocionesTitulo })
                Aplicar(c, t);
            foreach (Control c in flowSugerencias.Controls) Aplicar(c, t);
            foreach (Control c in flowPromociones.Controls) Aplicar(c, t);
            tip.SetToolTip(btnRefrescar, Tr("tip.actualizar", "Actualizar"));
            btnRefrescar.Text = Tr("tip.actualizar", "Actualizar");
            menuDocumentos.Traducir();
            TraducirHeadersSugerencias();
            TraducirHeadersPromociones();
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        private void TraducirHeadersSugerencias()
        {
            void RH(string col, string clave, string fb)
            {
                if (dgvSugerencias.Columns.Contains(col)) dgvSugerencias.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("ID", "col.promo.id", "ID");
            RH("Aplica a", "col.promo.aplicaa", "Aplica a");
            RH("Origen", "col.promo.origen", "Origen");
            RH("Motivo", "col.promo.motivo", "Motivo");
            RH("Tipo Sugerido", "col.promo.tiposugerido", "Tipo Sugerido");
            RH("Beneficio Est.", "col.promo.beneficioest", "Beneficio Est.");
            RH("Sugerida por", "col.promo.sugeridapor", "Sugerida por");
        }

        private void TraducirHeadersPromociones()
        {
            void RH(string col, string clave, string fb)
            {
                if (dgvPromociones.Columns.Contains(col)) dgvPromociones.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("ID", "col.promo.id", "ID");
            RH("Nombre", "col.promo.nombre", "Nombre");
            RH("Aplica a", "col.promo.aplicaa", "Aplica a");
            RH("Estado", "col.promo.estado", "Estado");
            RH("Vigencia", "col.promo.vigencia", "Vigencia");
            RH("Creada por", "col.promo.creadapor", "Creada por");
            RH("Motivo Baja", "col.promo.motivobaja", "Motivo Baja");
        }

        private void PromocionesAdministracionForm_Load(object sender, EventArgs e) => CargarTodo();

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarTodo();

        private void CargarTodo()
        {
            CargarSugerencias();
            CargarPromociones();
        }

        // ── Sugerencias de Gerencia: ¿Acepta la sugerencia? ──────────────────

        private void CargarSugerencias()
        {
            try
            {
                _sugerencias = sugerenciaBLL.ObtenerPendientes();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Origen", typeof(string));
                tabla.Columns.Add("Motivo", typeof(string));
                tabla.Columns.Add("Tipo Sugerido", typeof(string));
                tabla.Columns.Add("Beneficio Est.", typeof(decimal));
                tabla.Columns.Add("Sugerida por", typeof(string));

                foreach (var s in _sugerencias)
                    tabla.Rows.Add(s.IdSugerencia, Docs.AplicaA(s.IdPlan, s.NombrePlan, s.CategoriaPrenda),
                        Docs.Origen(s.OrigenMetrica), s.Motivo, Docs.Tipo(s.TipoDescuentoSugerido),
                        s.BeneficioEstimado, s.NombreUsuarioAlta ?? "—");

                dgvSugerencias.DataSource = tabla;
                if (dgvSugerencias.Columns.Contains("ID")) dgvSugerencias.Columns["ID"].Width = 44;
                TraducirHeadersSugerencias();
                dgvSugerencias.ClearSelection();
                HabilitarSugerencia(null);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.SugerenciaPromocion ObtenerSugerenciaSeleccionada()
        {
            if (dgvSugerencias.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvSugerencias.SelectedRows[0].Cells["ID"].Value);
            return _sugerencias.Find(s => s.IdSugerencia == id);
        }

        private void DgvSugerencias_SelectionChanged(object sender, EventArgs e) => HabilitarSugerencia(ObtenerSugerenciaSeleccionada());

        private void HabilitarSugerencia(BE.SugerenciaPromocion s)
        {
            btnUsarSugerencia.Enabled = btnDescartarSugerencia.Enabled = s != null && s.PuedeEvaluarse();
            btnImprimirSugerencia.Enabled = s != null;
        }

        // ¿Acepta la sugerencia? Sí → Crear promoción desde la sugerencia.
        private void BtnUsarSugerencia_Click(object sender, EventArgs e)
        {
            var sugerencia = ObtenerSugerenciaSeleccionada();
            if (sugerencia != null) AbrirAltaPromocion(sugerencia, null);
        }

        // ¿Acepta la sugerencia? No → Descartar sugerencia (motivo obligatorio) → «Constancia de descarte».
        private void BtnDescartarSugerencia_Click(object sender, EventArgs e)
        {
            var sugerencia = ObtenerSugerenciaSeleccionada();
            if (sugerencia == null) return;
            string motivo = PedirTexto(Tr("dlg.promo.descartarsug.titulo", "Descartar sugerencia"),
                Tr("dlg.promo.descartarsug.prompt", "Motivo por el que se descarta la sugerencia #{0}:", new object[] { sugerencia.IdSugerencia }));
            if (motivo == null) return;

            try
            {
                sugerenciaBLL.DescartarSugerencia(this.Text, sugerencia.IdSugerencia, motivo);
                MostrarOk(Tr("msg.promo.sugdescartada", "Sugerencia #{0} descartada.", new object[] { sugerencia.IdSugerencia }));
                CargarSugerencias();
            }
            catch (Exception ex) { MostrarError(ex); }
            // La constancia de descarte se imprime con "Imprimir sugerencia" (sale la de descarte).
        }

        private void BtnImprimirSugerencia_Click(object sender, EventArgs e)
        {
            var sugerencia = ObtenerSugerenciaSeleccionada();
            if (sugerencia == null) return;
            // Descartada: sale la «Constancia de descarte» (ya no se pregunta al descartar).
            Imprimir(() =>
            {
                var s = sugerenciaBLL.ObtenerPorId(sugerencia.IdSugerencia) ?? sugerencia;
                return s.EstaDescartada() ? Docs.ConstanciaDescarteSugerencia(s) : Docs.Sugerencia(s);
            });
        }

        // ── Promociones ──────────────────────────────────────────────────────

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
                tabla.Columns.Add("Vigencia", typeof(string));
                tabla.Columns.Add("Creada por", typeof(string));
                tabla.Columns.Add("Motivo Baja", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(p.IdPromocion, p.Nombre, Docs.AplicaA(p.IdPlan, p.NombrePlan, p.CategoriaPrenda),
                        Docs.Estado(p.Estado), $"{p.FechaInicio:d} - {p.FechaFin:d}",
                        p.NombreUsuarioAlta ?? "—", p.MotivoBaja ?? "—");

                dgvPromociones.DataSource = tabla;
                if (dgvPromociones.Columns.Contains("ID")) dgvPromociones.Columns["ID"].Width = 44;
                TraducirHeadersPromociones();

                lblConteo.Text = Tr("promo.conteo", "{0} promoción(es) en total.", new object[] { _promociones.Count });
                dgvPromociones.ClearSelection();
                HabilitarPromocion(null);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.Promocion ObtenerPromocionSeleccionada()
        {
            if (dgvPromociones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPromociones.SelectedRows[0].Cells["ID"].Value);
            return _promociones.Find(p => p.IdPromocion == id);
        }

        private void DgvPromociones_SelectionChanged(object sender, EventArgs e) => HabilitarPromocion(ObtenerPromocionSeleccionada());

        // Acciones habilitadas según el estado de la promoción (predicados de BE).
        private void HabilitarPromocion(BE.Promocion p)
        {
            btnReformular.Enabled = p != null && p.PuedeReformularse();
            btnDescartarPromocion.Enabled = p != null && p.PuedeDescartarse();
            btnDesactivar.Enabled = p != null && p.PuedeDesactivarseDirecto();
            btnAprobarBaja.Enabled = btnRechazarBaja.Enabled = p != null && p.PuedeResolverseBaja();
            btnHistorial.Enabled = btnImprimir.Enabled = p != null;
        }

        private void BtnNuevaManual_Click(object sender, EventArgs e) => AbrirAltaPromocion(null, null);

        // ¿Reformular? Sí → Reformular (vuelve a Validar y a revisión contable).
        private void BtnReformular_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion != null) AbrirAltaPromocion(null, promocion);
        }

        // Alta (desde sugerencia o manual) o reformulación → «Ficha de promoción».
        private void AbrirAltaPromocion(BE.SugerenciaPromocion sugerencia, BE.Promocion reformular)
        {
            int idPromocion;
            using (var form = new AltaPromocionForm(sugerencia, reformular))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                idPromocion = form.IdPromocionCreada;
            }
            MostrarOk(reformular != null
                ? Tr("msg.promo.reformulada", "Promoción #{0} reformulada: vuelve a revisión contable.", new object[] { idPromocion })
                : Tr("msg.promo.creada", "Promoción #{0} registrada, pendiente de revisión contable.", new object[] { idPromocion }));
            CargarTodo();   // la ficha para Contabilidad está en el menú "Imprimir"
        }

        // ¿Reformular? No → Descartar promoción (motivo obligatorio) → «Constancia de descarte».
        private void BtnDescartarPromocion_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;
            string motivo = PedirTexto(Tr("dlg.promo.descartar.titulo", "Descartar promoción"),
                Tr("dlg.promo.descartar.prompt", "Motivo por el que no se reformula la promoción '{0}':", new object[] { promocion.Nombre }));
            if (motivo == null) return;

            try
            {
                promocionBLL.DescartarPromocion(this.Text, promocion, motivo);
                MostrarOk(Tr("msg.promo.descartada", "Promoción '{0}' descartada.", new object[] { promocion.Nombre }));
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
            // La constancia de descarte está en el menú "Imprimir".
        }

        // (b) Administración desactiva directamente (motivo obligatorio).
        private void BtnDesactivar_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;
            string motivo = PedirTexto(Tr("dlg.promo.desactivar.titulo", "Desactivar promoción"),
                Tr("dlg.promo.desactivar.prompt", "Motivo para desactivar la promoción '{0}':", new object[] { promocion.Nombre }));
            if (motivo == null) return;

            try
            {
                promocionBLL.Desactivar(this.Text, promocion, motivo);
                MostrarOk(Tr("msg.promo.desactivada", "Promoción '{0}' desactivada.", new object[] { promocion.Nombre }));
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ¿Aprueba la baja? Sí → Desactivada + «Resolución de baja» (informe a Gerencia).
        private void BtnAprobarBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;
            string observacion = PedirTexto(Tr("dlg.promo.aprobarbaja.titulo", "Aprobar baja de promoción"),
                Tr("dlg.promo.aprobarbaja.prompt", "Ventas pidió la baja de '{0}'.\nMotivo: {1}\n\nObservación de la resolución (opcional):",
                   new object[] { promocion.Nombre, promocion.MotivoBaja }), obligatorio: false);
            if (observacion == null) return;

            ResolverBaja(promocion, () => promocionBLL.AprobarBaja(this.Text, promocion, observacion),
                Tr("msg.promo.dadabaja", "Promoción '{0}' dada de baja.", new object[] { promocion.Nombre }));
        }

        // ¿Aprueba la baja? No (motivo obligatorio) → vuelve a Vigente + «Resolución de baja» (informe a Ventas).
        private void BtnRechazarBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;
            string motivo = PedirTexto(Tr("inputdlg.rechazarbaja.titulo", "Rechazar Baja de Promoción"),
                Tr("inputdlg.rechazarbaja.prompt", "Motivo por el cual '{0}' sigue vigente:", new object[] { promocion.Nombre }));
            if (motivo == null) return;

            ResolverBaja(promocion, () => promocionBLL.RechazarBaja(this.Text, promocion, motivo),
                Tr("msg.promo.bajarechazada", "Se rechazó la baja de '{0}': sigue vigente.", new object[] { promocion.Nombre }));
        }

        // La resolución de baja (informe a Gerencia o a Ventas) está en el menú "Imprimir".
        private void ResolverBaja(BE.Promocion promocion, Func<int> resolver, string mensajeOk)
        {
            try
            {
                resolver();
                MostrarOk(mensajeOk);
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnHistorial_Click(object sender, EventArgs e)
        {
            var p = ObtenerPromocionSeleccionada();
            if (p == null) return;
            try
            {
                var lineas = promocionBLL.ObtenerHistorial(p.IdPromocion).ConvertAll(h =>
                    $"{h.Fecha:g} — {(h.EstadoAnterior.HasValue ? Docs.Estado(h.EstadoAnterior.Value) : "—")} → " +
                    $"{Docs.Estado(h.EstadoNuevo)} ({h.NombreUsuario ?? "—"}){(string.IsNullOrWhiteSpace(h.Observacion) ? "" : ": " + h.Observacion)}");
                MessageBox.Show(lineas.Count == 0 ? "—" : string.Join("\n", lineas),
                    Tr("lbl.promo.historial.titulo", "Historial de estados — Promoción #{0}", new object[] { p.IdPromocion }),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnImprimir_Click(object sender, EventArgs e) => menuDocumentos.Mostrar(btnImprimir);

        // ── Auxiliares de presentación ──────────────────────────────────────

        // Devuelve null si se canceló. Si es obligatorio y quedó vacío, la BLL informa el error.
        private string PedirTexto(string titulo, string prompt, bool obligatorio = true)
        {
            using (var dlg = new InputDialog(titulo, prompt, esPassword: false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return null;
                return obligatorio ? dlg.InputText : (dlg.InputText ?? "");
            }
        }

        private void Imprimir(Func<Exportacion.ReporteExportable> armar)
        {
            try { Docs.Imprimir(armar(), this); }
            catch (Exception ex) { MostrarError(ex); }
        }

        private bool Preguntar(string texto) =>
            (FormBase.MostrarConfirmacionSiNo(this, texto, this.Text, porDefectoNo: true) ? DialogResult.Yes : DialogResult.No) == DialogResult.Yes;
    }
}
