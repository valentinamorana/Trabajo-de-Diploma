using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN02, CU01-CAJ-Gestionar Cobro + CU02-CAJ-Emitir Comprobante +
    /// CU03-CAJ-Cancelar Contratación. Caja es un rol propio, separado de Vendedor: acá se
    /// cobran las contrataciones que Venta dejó pendientes de pago (ver NuevaContratacionForm).
    ///
    /// Mismo esqueleto que <see cref="PedidosRealizados"/>: grilla de la cola + acciones por
    /// fila + refresco completo después de cada acción.
    ///
    /// Accesible desde Menú → Caja → Contrataciones Pendientes (permiso mnuCaja).
    /// </summary>
    public partial class ContratacionesPendientesForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IContratacionService contratacionBLL = new BLL.Contratacion();

        private List<BE.Contratacion> _contrataciones = new List<BE.Contratacion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public ContratacionesPendientesForm()
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
            CargarContrataciones();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(lblMedioPago,      t);
            Aplicar(btnCobrar,         t);
            Aplicar(btnIntentoFallido, t);
            tip.SetToolTip(btnRefrescar, Tr("tip.actualizar", "Actualizar"));
            TraducirHeadersGrilla(t);
            CargarMediosPago();
        }

        // Antes hardcodeado en el Designer ("Efectivo"/"Tarjeta"/"Transferencia" fijos, sin pasar
        // por el traductor) — única lista de opciones de las 10 pantallas de este alcance que no
        // pasaba por el sistema de multiidioma. El VALOR persistido (BE.Contratacion.MedioPago)
        // se mantiene en español canónico independientemente del idioma de la UI — solo se
        // traduce el texto visible (DisplayMember), vía MedioPagoItem (mismo patrón que
        // Usuarios.PerfilItem).
        private void CargarMediosPago()
        {
            object seleccionActual = cmbMedioPago.SelectedItem is MedioPagoItem mpi ? mpi.Value : null;

            cmbMedioPago.DataSource = null;
            cmbMedioPago.DisplayMember = "Label";
            cmbMedioPago.ValueMember   = "Value";
            cmbMedioPago.DataSource = new[]
            {
                new MedioPagoItem("Efectivo",      Tr("medio.efectivo",      "Efectivo")),
                new MedioPagoItem("Tarjeta",        Tr("medio.tarjeta",       "Tarjeta")),
                new MedioPagoItem("Transferencia",  Tr("medio.transferencia", "Transferencia")),
            };
            cmbMedioPago.SelectedIndex = -1;
            if (seleccionActual != null)
            {
                for (int i = 0; i < cmbMedioPago.Items.Count; i++)
                    if (((MedioPagoItem)cmbMedioPago.Items[i]).Value.Equals(seleccionActual))
                    { cmbMedioPago.SelectedIndex = i; break; }
            }
        }

        private sealed class MedioPagoItem
        {
            public string Value { get; }
            public string Label { get; }
            public MedioPagoItem(string value, string label) { Value = value; Label = label; }
            public override string ToString() => Label;
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        /// <summary>Renombra el HeaderText de las columnas de dgvContrataciones según el idioma activo.</summary>
        private void TraducirHeadersGrilla(IDictionary<string, Traduccion> t)
        {
            void RH(string col, string clave)
            {
                if (dgvContrataciones.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvContrataciones.Columns[col].HeaderText = t[clave].Texto;
            }

            RH("ID",        "col.contr.id");
            RH("Cliente",   "col.contr.cliente");
            RH("Plan",      "col.contr.plan");
            RH("Monto",     "col.contr.monto");
            RH("Modalidad", "col.contr.modalidad");
            RH("Intentos",  "col.contr.intentos");
            RH("Fecha",     "col.contr.fecha");
        }

        private void ContratacionesPendientesForm_Load(object sender, EventArgs e)
        {
            CargarContrataciones();
        }

        private void BtnRefrescar_Click(object sender, EventArgs e)
        {
            CargarContrataciones();
        }

        private void CargarContrataciones()
        {
            try
            {
                _contrataciones = contratacionBLL.ObtenerPendientesDePago();
                // Un solo cálculo en lote para toda la cola (las promociones se leen una vez).
                _importes = new System.Collections.Generic.Dictionary<int, BE.LiquidacionContratacion>();
                try { _importes = contratacionBLL.CalcularImportes(_contrataciones); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[ContratacionesPendientesForm] No se pudieron calcular los importes: {ex.Message}"); }

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Cliente", typeof(string));
                tabla.Columns.Add("Plan", typeof(string));
                tabla.Columns.Add("Monto", typeof(string));
                tabla.Columns.Add("Modalidad", typeof(string));
                tabla.Columns.Add("Intentos", typeof(string));
                tabla.Columns.Add("Fecha", typeof(string));

                foreach (var c in _contrataciones)
                    tabla.Rows.Add(
                        c.IdContratacion, c.NombreCliente, c.NombrePlan, FormatearMonto(c),
                        c.Modalidad.ToString(), $"{c.IntentosPago}/3", c.FechaAlta.ToString("dd/MM/yyyy HH:mm"));

                dgvContrataciones.DataSource = tabla;
                if (dgvContrataciones.Columns.Contains("ID"))
                    dgvContrataciones.Columns["ID"].Width = 44;
                TraducirHeadersGrilla(Traductor.ObtenerTraducciones(_idioma));

                var tCnt = Traductor.ObtenerTraducciones(_idioma);
                lblConteo.Text = string.Format(
                    tCnt.ContainsKey("contratacion.conteo") ? tCnt["contratacion.conteo"].Texto : "{0} contratación(es) pendiente(s) de pago.",
                    _contrataciones.Count);
                DeshabilitarBotones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void DgvContrataciones_SelectionChanged(object sender, EventArgs e)
        {
            bool haySeleccion = dgvContrataciones.SelectedRows.Count > 0;
            btnCobrar.Enabled = haySeleccion;
            btnIntentoFallido.Enabled = haySeleccion;
        }

        // Importes calculados en lote por CargarContrataciones (clave = IdContratacion).
        private System.Collections.Generic.Dictionary<int, BE.LiquidacionContratacion> _importes =
            new System.Collections.Generic.Dictionary<int, BE.LiquidacionContratacion>();

        // PN03: importe a cobrar con el descuento aplicable (promoción vigente del plan o crédito por referido).
        // Si no se pudo calcular se muestra "—": nunca un monto sin descuento que después no coincida con el cobro.
        private string FormatearMonto(BE.Contratacion c)
        {
            if (!_importes.TryGetValue(c.IdContratacion, out var liq)) return "—";
            return liq.Descuento > 0
                ? $"{liq.Total:C2} (-{liq.Descuento:C2})"
                : liq.Total.ToString("C2");
        }

        private BE.Contratacion ObtenerSeleccionada()
        {
            if (dgvContrataciones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvContrataciones.SelectedRows[0].Cells["ID"].Value);
            return _contrataciones.Find(c => c.IdContratacion == id);
        }

        private void DeshabilitarBotones()
        {
            btnCobrar.Enabled = false;
            btnIntentoFallido.Enabled = false;
        }

        private void BtnCobrar_Click(object sender, EventArgs e)
        {
            var contratacion = ObtenerSeleccionada();
            if (contratacion == null) return;

            if (cmbMedioPago.SelectedItem == null)
            {
                MostrarError(Tr("err.contratacion.mediopago_requerido", "Seleccioná el medio de pago antes de cobrar."));
                return;
            }
            string medioPago = ((MedioPagoItem)cmbMedioPago.SelectedItem).Value;

            BE.LiquidacionContratacion liquidacion;
            try { liquidacion = contratacionBLL.CalcularImporte(contratacion); }
            catch (Exception ex) { MostrarError(ex); return; }
            string detalleDescuento = liquidacion.Descuento > 0
                ? "\n" + Tr("conf.contratacion.cobro.desc", "Descuento aplicado: {0:C2} ({1}).",
                    new object[] { liquidacion.Descuento,
                                   liquidacion.NombrePromocion ?? Tr("lbl.contratacion.creditoreferido", "crédito por referido") })
                : "";

            var confirmar = MessageBox.Show(
                Tr("conf.contratacion.cobro.msg",
                    "¿Confirmar el cobro de la Contratación #{0}?\n\n" +
                    "Cliente: {1}\nPlan: {2}\nMonto: {3:C2}\nMedio de pago: {4}\n\n" +
                    "Se emitirá el comprobante y la suscripción quedará formalizada.",
                    new object[] { contratacion.IdContratacion, contratacion.NombreCliente, contratacion.NombrePlan,
                                    liquidacion.Total, medioPago }) + detalleDescuento,
                Tr("conf.contratacion.cobro.titulo", "Confirmar Cobro"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                var cobro = contratacionBLL.ConfirmarPago(this.Text, contratacion, medioPago);
                MostrarOk(Tr("msg.contratacion.cobrada.comprobante",
                    "Contratación #{0} cobrada por {1:C2}. Comprobante {2}. Suscripción formalizada.",
                    new object[] { contratacion.IdContratacion, cobro.Total, cobro.NumeroComprobante }));
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnIntentoFallido_Click(object sender, EventArgs e)
        {
            var contratacion = ObtenerSeleccionada();
            if (contratacion == null) return;

            var confirmar = MessageBox.Show(
                Tr("conf.contratacion.intentofallido.msg",
                    "¿Registrar un intento de pago fallido para la Contratación #{0}?\n\n" +
                    "Intentos hasta ahora: {1}/3. Al llegar a 3 se cancela automáticamente.",
                    new object[] { contratacion.IdContratacion, contratacion.IntentosPago }),
                Tr("conf.contratacion.intentofallido.titulo", "Registrar Intento Fallido"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                contratacionBLL.RegistrarIntentoFallido(this.Text, contratacion);
                MostrarOk(Tr("msg.contratacion.intentofallido_registrado", "Intento fallido registrado para la Contratación #{0}.",
                    new object[] { contratacion.IdContratacion }));
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
