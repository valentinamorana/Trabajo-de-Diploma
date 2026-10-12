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
    ///   Crear promoción manual. ¿Reformular? Sí: Reformular; No: Dar de baja.
    ///   Solicitudes de baja pendientes (Ventas): Atender («Respuesta a la solicitud»); si
    ///   Administración está de acuerdo, la promoción se da de baja. Dar de baja directamente.
    /// Imprime la sugerencia, la ficha, el dictamen, la solicitud, la respuesta y la constancia de
    /// baja. Solo muestra datos y llama a la BLL: las guardas viven en BLL.Promocion /
    /// BLL.SugerenciaPromocion y los predicados en BE.
    /// </summary>
    public partial class PromocionesAdministracionForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly BLL.Interfaces.ISugerenciaPromocionService sugerenciaBLL = new BLL.SugerenciaPromocion();
        private readonly MenuDocumentosPromocion menuDocumentos;

        private List<BE.SugerenciaPromocion> _sugerencias = new List<BE.SugerenciaPromocion>();
        private List<BE.SolicitudBajaPromocion> _solicitudes = new List<BE.SolicitudBajaPromocion>();
        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PromocionesAdministracionForm()
        {
            InitializeComponent();
            // Mismo estilo de grilla que el resto de las pantallas.
            GUI.Estilos.EstiloFormulario.Grilla(dgvSugerencias);
            GUI.Estilos.EstiloFormulario.Grilla(dgvSolicitudes);
            GUI.Estilos.EstiloFormulario.Grilla(dgvPromociones);
            // Paleta centralizada (GUI/Tema.cs).
            btnUsarSugerencia.BackColor = btnNuevaManual.BackColor = btnReformular.BackColor = Tema.RosaPrimario;
            btnAtenderSolicitud.BackColor = Tema.Alerta;
            btnDescartarSugerencia.BackColor = btnDarDeBaja.BackColor = Tema.Error;
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
            foreach (Control c in new Control[] { lblSugerenciasTitulo, lblSolicitudesTitulo, lblPromocionesTitulo })
                Aplicar(c, t);
            foreach (Control c in flowSugerencias.Controls) Aplicar(c, t);
            foreach (Control c in flowSolicitudes.Controls) Aplicar(c, t);
            foreach (Control c in flowPromociones.Controls) Aplicar(c, t);
            tip.SetToolTip(btnRefrescar, Tr("tip.actualizar", "Actualizar"));
            btnRefrescar.Text = Tr("tip.actualizar", "Actualizar");
            menuDocumentos.Traducir();
            TraducirHeadersSugerencias();
            TraducirHeadersSolicitudes();
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

        private void TraducirHeadersSolicitudes()
        {
            void RH(string col, string clave, string fb)
            {
                if (dgvSolicitudes.Columns.Contains(col)) dgvSolicitudes.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("ID", "col.promo.id", "ID");
            RH("Promoción", "col.promo.promocion", "Promoción");
            RH("Motivo", "col.promo.motivo", "Motivo");
            RH("Solicitada por", "col.promo.solicitadapor", "Solicitada por");
            RH("Fecha", "col.promo.fecha", "Fecha");
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
            RH("Baja solicitada", "col.promo.bajasolicitada", "Baja solicitada");
        }

        private void PromocionesAdministracionForm_Load(object sender, EventArgs e) => CargarTodo();

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarTodo();

        private void CargarTodo()
        {
            CargarSugerencias();
            CargarSolicitudes();
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
                    tabla.Rows.Add(s.IdSugerencia, Docs.AplicaA(s.IdPlan, s.NombrePlan),
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

        // ── Solicitudes de baja pendientes (Ventas): Atender ─────────────────

        private void CargarSolicitudes()
        {
            try
            {
                _solicitudes = promocionBLL.ObtenerSolicitudesBajaPendientes();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Promoción", typeof(string));
                tabla.Columns.Add("Motivo", typeof(string));
                tabla.Columns.Add("Solicitada por", typeof(string));
                tabla.Columns.Add("Fecha", typeof(string));

                foreach (var s in _solicitudes)
                    tabla.Rows.Add(s.IdSolicitud, $"#{s.IdPromocion} — {s.NombrePromocion}", s.Motivo,
                        s.NombreUsuarioSolicita ?? "—", s.FechaSolicitud.ToString("g"));

                dgvSolicitudes.DataSource = tabla;
                if (dgvSolicitudes.Columns.Contains("ID")) dgvSolicitudes.Columns["ID"].Width = 44;
                TraducirHeadersSolicitudes();
                dgvSolicitudes.ClearSelection();
                btnAtenderSolicitud.Enabled = false;
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.SolicitudBajaPromocion ObtenerSolicitudSeleccionada()
        {
            if (dgvSolicitudes.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvSolicitudes.SelectedRows[0].Cells["ID"].Value);
            return _solicitudes.Find(s => s.IdSolicitud == id);
        }

        private void DgvSolicitudes_SelectionChanged(object sender, EventArgs e)
        {
            var s = ObtenerSolicitudSeleccionada();
            btnAtenderSolicitud.Enabled = s != null && s.EstaPendiente();
        }

        // Atender la solicitud: ¿de acuerdo con dar de baja? → respuesta obligatoria («Respuesta a la solicitud»).
        private void BtnAtenderSolicitud_Click(object sender, EventArgs e)
        {
            var solicitud = ObtenerSolicitudSeleccionada();
            if (solicitud == null) return;

            var decision = MessageBox.Show(this,
                Tr("dlg.promo.atender.pregunta",
                   "Ventas pide dar de baja '{0}'.\nMotivo: {1}\n\n¿Estás de acuerdo con darla de baja?\n\nSí: se da de baja.  No: sigue vigente.",
                   new object[] { solicitud.NombrePromocion, solicitud.Motivo }),
                Tr("dlg.promo.atender.titulo", "Atender solicitud de baja"),
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (decision == DialogResult.Cancel) return;
            bool darDeBaja = decision == DialogResult.Yes;

            string respuesta = PedirTexto(Tr("dlg.promo.atender.titulo", "Atender solicitud de baja"),
                darDeBaja
                    ? Tr("dlg.promo.atender.prompt.baja", "Respuesta para Ventas (la promoción '{0}' se da de baja):", new object[] { solicitud.NombrePromocion })
                    : Tr("dlg.promo.atender.prompt.sigue", "Respuesta para Ventas (por qué '{0}' sigue vigente):", new object[] { solicitud.NombrePromocion }));
            if (respuesta == null) return;

            try
            {
                promocionBLL.AtenderSolicitud(this.Text, solicitud, respuesta, darDeBaja);
                MostrarOk(darDeBaja
                    ? Tr("msg.promo.solicitud.dadabaja", "Solicitud #{0} atendida: la promoción '{1}' quedó dada de baja.", new object[] { solicitud.IdSolicitud, solicitud.NombrePromocion })
                    : Tr("msg.promo.solicitud.sigue", "Solicitud #{0} atendida: la promoción '{1}' sigue vigente.", new object[] { solicitud.IdSolicitud, solicitud.NombrePromocion }));
                CargarSolicitudes();
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
            // La respuesta a la solicitud está en el menú "Imprimir" de la promoción.
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
                tabla.Columns.Add("Baja solicitada", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(p.IdPromocion, p.Nombre, Docs.AplicaA(p.IdPlan, p.NombrePlan),
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
            btnDarDeBaja.Enabled = p != null && p.PuedeDarseDeBaja();
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

        // Dar de baja (motivo obligatorio) una Vigente o una Rechazada (¿Reformular? No) → «Constancia de baja».
        // Si tenía una solicitud de baja pendiente, queda atendida con este motivo.
        private void BtnDarDeBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;
            string motivo = PedirTexto(Tr("dlg.promo.dardebaja.titulo", "Dar de baja la promoción"),
                Tr("dlg.promo.dardebaja.prompt", "Motivo para dar de baja la promoción '{0}':", new object[] { promocion.Nombre }));
            if (motivo == null) return;

            try
            {
                promocionBLL.DarDeBaja(this.Text, promocion, motivo);
                MostrarOk(Tr("msg.promo.dadabaja", "Promoción '{0}' dada de baja.", new object[] { promocion.Nombre }));
                CargarSolicitudes();
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
            // La constancia de baja está en el menú "Imprimir".
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

    }
}
