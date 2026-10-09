using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN02, carril Caja del diagrama de actividad:
    ///   Consultar cola → Calcular importe («Liquidación») → el cliente abona (medio de pago) →
    ///   ¿Tarjeta de crédito? Sí: ofrecer planes de cuotas → elegir cuotas → recargo y valor de cuota →
    ///   ¿Se concreta el pago? Sí: Confirmar cobro → «Comprobante» → «Constancia de suscripción»;
    ///   No: Registrar intento («Intento») → ¿3 intentos? Sí: «Constancia de cancelación».
    /// Vista "Resueltas" para volver a imprimir comprobantes y constancias.
    /// Solo muestra datos y llama a BLL.Contratacion: importes, validaciones y cancelación viven ahí.
    ///
    /// Accesible desde Menú → Caja → Contrataciones Pendientes (permiso mnuCaja).
    /// </summary>
    public partial class ContratacionesPendientesForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IContratacionService contratacionBLL = new BLL.Contratacion();

        private List<BE.Contratacion> _contrataciones = new List<BE.Contratacion>();
        private List<BE.MedioPago> _medios = new List<BE.MedioPago>();
        private List<BE.PlanCuotas> _planesCuotas = new List<BE.PlanCuotas>();
        private Dictionary<int, BE.LiquidacionContratacion> _importes = new Dictionary<int, BE.LiquidacionContratacion>();
        private Idioma _idioma = GestorIdioma.IdiomaActual;

        private bool VistaResueltas => cmbVista.SelectedIndex == 1;

        public ContratacionesPendientesForm()
        {
            InitializeComponent();
            // Estilo de grilla compartido (encabezado rosa, filas alternadas) — EstiloFormulario.
            Estilos.EstiloFormulario.Grilla(dgvContrataciones);
            // Paleta centralizada (GUI/Tema.cs).
            btnCobrar.BackColor = Tema.Exito;
            btnIntentoFallido.BackColor = Tema.RosaPrimario;
            btnImprimirConstancia.BackColor = Tema.RosaOscuro;
            btnVerIntentos.BackColor = btnImprimirLiquidacion.BackColor = btnImprimirComprobante.BackColor = Tema.TextoMuted;
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
            CargarContrataciones();
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

            // Reconstruir el combo SIN disparar SelectedIndexChanged: antes eso recargaba la
            // grilla y UpdateLanguage la volvía a cargar (doble consulta al cambiar de idioma).
            int vista = Math.Max(0, cmbVista.SelectedIndex);
            cmbVista.SelectedIndexChanged -= CmbVista_SelectedIndexChanged;
            cmbVista.Items.Clear();
            cmbVista.Items.Add(Tr("lbl.contr.vista.pendientes", "Pendientes de pago"));
            cmbVista.Items.Add(Tr("lbl.contr.vista.resueltas", "Resueltas"));
            cmbVista.SelectedIndex = vista;
            cmbVista.SelectedIndexChanged += CmbVista_SelectedIndexChanged;
            CargarMediosPago();
        }

        // Catálogo de medios de pago (BD). El valor es el Id; el texto visible se traduce.
        private void CargarMediosPago()
        {
            int? actual = (cmbMedioPago.SelectedItem as MedioItem)?.Id;
            try { _medios = contratacionBLL.ObtenerMediosPago(); }
            catch (Exception ex) { MostrarError(ex); return; }
            cmbMedioPago.Items.Clear();
            foreach (var m in _medios)
                cmbMedioPago.Items.Add(new MedioItem(m.IdMedioPago, Tr(m.ClaveTraduccion, m.Nombre)));
            cmbMedioPago.SelectedIndex = -1;
            if (actual.HasValue)
                for (int i = 0; i < cmbMedioPago.Items.Count; i++)
                    if (((MedioItem)cmbMedioPago.Items[i]).Id == actual.Value) cmbMedioPago.SelectedIndex = i;
            CargarCuotas();
        }

        // ── ¿Tarjeta de crédito? → Ofrecer planes de cuotas según la modalidad ──

        private BE.MedioPago MedioActual()
        {
            var id = MedioSeleccionado();
            return id.HasValue ? _medios.Find(m => m.IdMedioPago == id.Value) : null;
        }

        private void CmbMedioPago_SelectedIndexChanged(object sender, EventArgs e) => CargarCuotas();

        private void CmbCuotas_SelectedIndexChanged(object sender, EventArgs e) => ActualizarDetalleCuotas();

        // Los planes dependen del medio (solo tarjeta de crédito) y de la modalidad de la contratación
        // seleccionada (no más cuotas que meses que cubre el cobro). BLL aplica las mismas reglas al cobrar.
        private void CargarCuotas()
        {
            cmbCuotas.Items.Clear();
            _planesCuotas = new List<BE.PlanCuotas>();
            var medio = MedioActual();
            var c = ObtenerSeleccionada();
            if (medio != null && medio.PermiteCuotas && c != null && c.PuedeCobrarse() && !VistaResueltas)
            {
                try { _planesCuotas = contratacionBLL.ObtenerPlanesCuotas(c.Modalidad); }
                catch (Exception ex) { MostrarError(ex); }
                foreach (var p in _planesCuotas) cmbCuotas.Items.Add(new MedioItem(p.IdPlanCuotas, TextoPlan(p)));
                if (cmbCuotas.Items.Count > 0) cmbCuotas.SelectedIndex = 0;
            }
            cmbCuotas.Enabled = cmbCuotas.Items.Count > 1;
            ActualizarDetalleCuotas();
        }

        private string TextoPlan(BE.PlanCuotas p) => p.SinInteres
            ? string.Format(Tr("lbl.contr.cuotas.sininteres", "{0} cuota(s) sin interés"), p.CantidadCuotas)
            : string.Format(Tr("lbl.contr.cuotas.recargo", "{0} cuotas (recargo {1:0.##} %)"), p.CantidadCuotas, p.RecargoPorcentaje);

        private int? PlanCuotasSeleccionado() => cmbCuotas.Items.Count > 0 ? (cmbCuotas.SelectedItem as MedioItem)?.Id : null;

        // «Detalle de financiación»: valor de cuota, recargo y total que abona el cliente.
        private void ActualizarDetalleCuotas()
        {
            lblDetalleCuotas.Text = "";
            var c = ObtenerSeleccionada();
            var idPlan = PlanCuotasSeleccionado();
            if (c == null || !idPlan.HasValue || !_importes.TryGetValue(c.IdContratacion, out var liq)) return;
            var plan = _planesCuotas.Find(p => p.IdPlanCuotas == idPlan.Value);
            var f = BLL.Politicas.PoliticaCuotas.Financiar(liq.Total, plan);
            lblDetalleCuotas.Text = string.Format(
                Tr("lbl.contr.cuotas.detalle", "{0} cuota(s) de {1:C2} — recargo {2:C2} — total a abonar {3:C2}"),
                f.CantidadCuotas, f.ValorCuota, f.Recargo, f.TotalFinanciado);
        }

        private sealed class MedioItem
        {
            public int Id { get; }
            public string Texto { get; }
            public MedioItem(int id, string texto) { Id = id; Texto = texto; }
            public override string ToString() => Texto;
        }

        private int? MedioSeleccionado() => (cmbMedioPago.SelectedItem as MedioItem)?.Id;

        private void ContratacionesPendientesForm_Load(object sender, EventArgs e) => CargarContrataciones();

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarContrataciones();

        private void CmbVista_SelectedIndexChanged(object sender, EventArgs e) => CargarContrataciones();

        // ── "Consultar cola" / resueltas ─────────────────────────────────────

        private void CargarContrataciones()
        {
            if (cmbVista.SelectedIndex < 0) return;
            try
            {
                _contrataciones = VistaResueltas ? contratacionBLL.ObtenerResueltas() : contratacionBLL.ObtenerPendientesDePago();
                _importes = new Dictionary<int, BE.LiquidacionContratacion>();
                if (!VistaResueltas)
                {
                    try { _importes = contratacionBLL.CalcularImportes(_contrataciones); }
                    catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[ContratacionesPendientesForm] Importes: {ex.Message}"); }
                }

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Cliente", typeof(string));
                tabla.Columns.Add("Plan", typeof(string));
                tabla.Columns.Add("Monto", typeof(string));
                tabla.Columns.Add("Modalidad", typeof(string));
                tabla.Columns.Add("Intentos", typeof(string));
                tabla.Columns.Add("Estado", typeof(string));
                tabla.Columns.Add("Comprobante", typeof(string));
                tabla.Columns.Add("Vigencia", typeof(string));
                tabla.Columns.Add("Fecha", typeof(string));

                foreach (var c in _contrataciones)
                    tabla.Rows.Add(
                        c.IdContratacion, c.NombreCliente, c.NombrePlan, FormatearMonto(c),
                        Exportacion.DocumentosContratacion.Modalidad(c.Modalidad),
                        $"{c.IntentosPago}/{BE.Contratacion.MaxIntentosPago}",
                        EstadoTexto(c.Estado), c.NumeroComprobante ?? "—",
                        c.VigenciaHasta.HasValue ? $"{c.VigenciaDesde:d} → {c.VigenciaHasta:d}" : "—",
                        (c.FechaResolucion ?? c.FechaAlta).ToString("g"));

                dgvContrataciones.DataSource = tabla;
                if (dgvContrataciones.Columns.Contains("ID")) dgvContrataciones.Columns["ID"].Width = 44;
                dgvContrataciones.Columns["Estado"].Visible = dgvContrataciones.Columns["Comprobante"].Visible =
                    dgvContrataciones.Columns["Vigencia"].Visible = VistaResueltas;
                TraducirHeadersGrilla();

                lblConteo.Text = string.Format(VistaResueltas
                        ? Tr("lbl.contr.conteo.resueltas", "{0} contratación(es) resuelta(s).")
                        : Tr("contratacion.conteo", "{0} contratación(es) pendiente(s) de pago."),
                    _contrataciones.Count);
                dgvContrataciones.ClearSelection();
                HabilitarAcciones(null);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void TraducirHeadersGrilla()
        {
            void RH(string col, string clave, string fb)
            {
                if (dgvContrataciones.Columns.Contains(col)) dgvContrataciones.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("ID", "col.contr.id", "ID");
            RH("Cliente", "col.contr.cliente", "Cliente");
            RH("Plan", "col.contr.plan", "Plan");
            RH("Monto", "col.contr.monto", "Monto");
            RH("Modalidad", "col.contr.modalidad", "Modalidad");
            RH("Intentos", "col.contr.intentos", "Intentos");
            RH("Estado", "col.contr.estado", "Estado");
            RH("Comprobante", "col.contr.comprobante", "Comprobante");
            RH("Vigencia", "col.contr.vigencia", "Vigencia");
            RH("Fecha", "col.contr.fecha", "Fecha");
        }

        private string EstadoTexto(BE.EstadoContratacion e)
        {
            switch (e)
            {
                case BE.EstadoContratacion.Pagada:    return Tr("est.contr.pagada", "Pagada");
                case BE.EstadoContratacion.Cancelada: return Tr("est.contr.cancelada", "Cancelada");
                default:                              return Tr("est.contr.pendiente", "Pendiente de pago");
            }
        }

        // Importe a cobrar con el descuento aplicable (pendientes) o el importe cobrado (resueltas).
        private string FormatearMonto(BE.Contratacion c)
        {
            if (VistaResueltas)
            {
                if (!c.Importe.HasValue) return "—";
                return (c.CantidadCuotas ?? 1) > 1
                    ? string.Format(Tr("lbl.contr.monto.cuotas", "{0:C2} en {1} cuotas"), c.TotalAbonado, c.CantidadCuotas)
                    : c.Importe.Value.ToString("C2");
            }
            if (!_importes.TryGetValue(c.IdContratacion, out var liq)) return "—";
            decimal rebaja = liq.Descuento + liq.CreditoCambioPlan;   // promoción/referido + crédito por upgrade
            return rebaja > 0 ? $"{liq.Total:C2} (-{rebaja:C2})" : liq.Total.ToString("C2");
        }

        private BE.Contratacion ObtenerSeleccionada()
        {
            if (dgvContrataciones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvContrataciones.SelectedRows[0].Cells["ID"].Value);
            return _contrataciones.Find(c => c.IdContratacion == id);
        }

        private void DgvContrataciones_SelectionChanged(object sender, EventArgs e) => HabilitarAcciones(ObtenerSeleccionada());

        // Acciones habilitadas según el estado de la contratación (predicados de BE).
        private void HabilitarAcciones(BE.Contratacion c)
        {
            bool pendiente = c != null && c.PuedeCobrarse();
            btnCobrar.Enabled = btnIntentoFallido.Enabled = btnImprimirLiquidacion.Enabled = btnAnular.Enabled = pendiente;
            cmbMedioPago.Enabled = !VistaResueltas;
            btnVerIntentos.Enabled = c != null && c.TieneIntentos();
            btnImprimirComprobante.Enabled = c != null && c.EstaPagada();
            btnImprimirConstancia.Enabled = c != null && !c.PuedeCobrarse();
            CargarCuotas();
        }

        // ── ¿Se concreta el pago? Sí → Confirmar cobro → Comprobante → Constancia ─

        private void BtnCobrar_Click(object sender, EventArgs e)
        {
            var contratacion = ObtenerSeleccionada();
            if (contratacion == null) return;
            var idMedio = MedioSeleccionado();
            if (!idMedio.HasValue)
            {
                MostrarError(Tr("err.contratacion.mediopago_requerido", "Seleccioná el medio de pago antes de cobrar."));
                return;
            }

            var idPlanCuotas = PlanCuotasSeleccionado();
            BE.LiquidacionContratacion liq;
            try { liq = contratacionBLL.CalcularImporte(contratacion, idMedio.Value, idPlanCuotas); }
            catch (Exception ex) { MostrarError(ex); return; }
            string detalleDescuento = liq.Descuento > 0
                ? "\n" + Tr("conf.contratacion.cobro.desc", "Descuento aplicado: {0:C2} ({1}).",
                    new object[] { liq.Descuento, liq.NombrePromocion ?? Tr("lbl.contratacion.creditoreferido", "crédito por referido") })
                : "";
            if (liq.CreditoCambioPlan > 0)
                detalleDescuento += "\n" + Tr("conf.contr.cobro.upgrade",
                    "Cambio a un plan superior: rige desde hoy. Crédito por los días no usados del plan anterior: {0:C2}.",
                    new object[] { liq.CreditoCambioPlan });
            if (liq.Cargos > 0)
                detalleDescuento += "\n" + Tr("conf.contr.cobro.cargos",
                    "Incluye {0} cargo(s) por daño o pérdida de prendas: {1:C2}.",
                    new object[] { liq.CantidadCargos, liq.Cargos });
            if (liq.CantidadCuotas > 1 || liq.RecargoCuotas > 0)
                detalleDescuento += "\n" + Tr("conf.contr.cobro.cuotas",
                    "Tarjeta de crédito en {0} cuota(s) de {1:C2} (recargo {2:C2}). Total a abonar: {3:C2}.",
                    new object[] { liq.CantidadCuotas, liq.ValorCuota, liq.RecargoCuotas, liq.TotalConRecargo });

            if (!ConfirmarSiNo(
                    Tr("conf.contr.cobro.confirmar",
                       "¿Confirmar el cobro de la Contratación #{0}?\n\nCliente: {1}\nPlan: {2}\nMonto: {3:C2}\nMedio de pago: {4}\n\n" +
                       "Se emitirá el comprobante y la suscripción quedará formalizada.",
                       new object[] { contratacion.IdContratacion, contratacion.NombreCliente, contratacion.NombrePlan,
                                      liq.TotalConRecargo, cmbMedioPago.SelectedItem.ToString() }) + detalleDescuento,
                    Tr("conf.contratacion.cobro.titulo", "Confirmar Cobro")))
                return;

            try
            {
                var cobro = contratacionBLL.ConfirmarCobro(this.Text, contratacion, idMedio.Value, liq.Total, idPlanCuotas);
                MostrarOk(Tr("msg.contratacion.cobrada.comprobante",
                    "Contratación #{0} cobrada por {1:C2}. Comprobante {2}. Suscripción formalizada.",
                    new object[] { contratacion.IdContratacion, cobro.TotalConRecargo, cobro.NumeroComprobante }) +
                    (cobro.VigenciaHasta.HasValue
                        ? " " + Tr("msg.contr.vigencia", "Vigente del {0:d} al {1:d}.", new object[] { cobro.VigenciaDesde, cobro.VigenciaHasta })
                        : "") +
                    (cobro.ReferenteAcreditado != null
                        ? " " + Tr("msg.contr.referido", "Se acreditó el beneficio por referido a {0}.", new object[] { cobro.ReferenteAcreditado })
                        : ""));

                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); return; }

            // La única impresión que se ofrece tras una acción: el comprobante que se lleva el cliente.
            // La constancia de suscripción queda en "Imprimir constancia". Va aparte: un error al
            // imprimir no es un error del cobro, que ya quedó hecho.
            if (Preguntar(Tr("conf.contr.imprimircomprobante", "¿Imprimir el comprobante para el cliente?")))
            {
                try
                {
                    var pagada = contratacionBLL.ObtenerPorId(contratacion.IdContratacion);
                    Exportacion.DocumentosContratacion.Imprimir(Exportacion.DocumentosContratacion.Comprobante(pagada), this);
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        // ── ¿Se concreta el pago? No → Registrar intento → ¿3 intentos? ───────

        private void BtnIntentoFallido_Click(object sender, EventArgs e)
        {
            var contratacion = ObtenerSeleccionada();
            if (contratacion == null) return;

            string motivo;
            using (var dlg = new InputDialog(
                Tr("conf.contratacion.intentofallido.titulo", "Registrar Intento Fallido"),
                string.Format(Tr("dlg.contr.intento.prompt",
                    "Motivo por el que no se concretó el pago (Contratación #{0}, intentos: {1}/{2}).\nAl tercer intento se cancela automáticamente:"),
                    contratacion.IdContratacion, contratacion.IntentosPago, BE.Contratacion.MaxIntentosPago),
                false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText;
            }

            try
            {
                var r = contratacionBLL.RegistrarIntentoFallido(this.Text, contratacion, MedioSeleccionado(), motivo);
                if (r.Cancelada)
                {
                    MostrarError(Tr("msg.contr.cancelada",
                        "Contratación #{0} cancelada: no se concretó el pago en {1} intentos.",
                        new object[] { contratacion.IdContratacion, r.Maximo }));
                    // La constancia de cancelación se imprime desde "Imprimir constancia".
                }
                else
                {
                    MostrarOk(Tr("msg.contr.intento",
                        "Intento {0} de {1} registrado para la Contratación #{2}. Sigue en la cola para volver a cobrarla.",
                        new object[] { r.NroIntento, r.Maximo, contratacion.IdContratacion }));
                }
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // "Anular contratación": el cliente se arrepintió o hubo un error de carga (sin intentos de pago).
        private void BtnAnular_Click(object sender, EventArgs e)
        {
            var contratacion = ObtenerSeleccionada();
            if (contratacion == null) return;

            string motivo;
            using (var dlg = new InputDialog(
                Tr("dlg.contr.anular.titulo", "Anular contratación"),
                string.Format(Tr("dlg.contr.anular.prompt",
                    "Motivo por el que se anula la Contratación #{0} de {1} (no se cobra y sale de la cola):"),
                    contratacion.IdContratacion, contratacion.NombreCliente),
                false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText;
            }

            try
            {
                contratacionBLL.Anular(this.Text, contratacion, motivo);
                MostrarOk(Tr("msg.contr.anulada",
                    "Contratación #{0} anulada. La constancia se imprime desde \"Imprimir constancia\" en las resueltas.",
                    new object[] { contratacion.IdContratacion }));
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ── Consultas e impresiones ──────────────────────────────────────────

        private void BtnVerIntentos_Click(object sender, EventArgs e)
        {
            var c = ObtenerSeleccionada();
            if (c == null) return;
            try
            {
                var lineas = contratacionBLL.ObtenerIntentos(c.IdContratacion).ConvertAll(i =>
                    $"{i.NroIntento}. {i.Fecha:g} — {i.NombreMedioPago ?? "—"} — {i.Motivo} ({i.NombreCaja ?? "—"})");
                MessageBox.Show(string.Join("\n", lineas),
                    string.Format(Tr("lbl.contr.intentos.titulo", "Intentos de cobro — Contratación #{0}"), c.IdContratacion),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnImprimirLiquidacion_Click(object sender, EventArgs e)
        {
            var c = ObtenerSeleccionada();
            if (c == null) return;
            try
            {
                Exportacion.DocumentosContratacion.Imprimir(Exportacion.DocumentosContratacion.Liquidacion(
                    c, contratacionBLL.CalcularImporte(c), contratacionBLL.ObtenerIntentos(c.IdContratacion),
                    contratacionBLL.ObtenerPlanesCuotas(c.Modalidad)), this);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnImprimirComprobante_Click(object sender, EventArgs e)
        {
            var c = ObtenerSeleccionada();
            if (c == null) return;
            try { Exportacion.DocumentosContratacion.Imprimir(Exportacion.DocumentosContratacion.Comprobante(c), this); }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnImprimirConstancia_Click(object sender, EventArgs e)
        {
            var c = ObtenerSeleccionada();
            if (c != null) ImprimirConstancia(c);
        }

        // Constancia de suscripción (Pagada) o de cancelación (Cancelada).
        private void ImprimirConstancia(BE.Contratacion c)
        {
            try
            {
                var doc = c.EstaCancelada()
                    ? Exportacion.DocumentosContratacion.ConstanciaCancelacion(c, contratacionBLL.ObtenerIntentos(c.IdContratacion))
                    : Exportacion.DocumentosContratacion.ConstanciaSuscripcion(c);
                Exportacion.DocumentosContratacion.Imprimir(doc, this);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private bool Preguntar(string texto) =>
            ConfirmarSiNo(texto, this.Text, porDefectoNo: true);
    }
}
