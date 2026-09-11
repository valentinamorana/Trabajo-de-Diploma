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
            TraducirHeadersGrilla(t);
            CargarMediosPago(t);
        }

        // Antes hardcodeado en el Designer ("Efectivo"/"Tarjeta"/"Transferencia" fijos, sin pasar
        // por el traductor) — única lista de opciones de las 10 pantallas de este alcance que no
        // pasaba por el sistema de multiidioma. El VALOR persistido (BE.Contratacion.MedioPago)
        // se mantiene en español canónico independientemente del idioma de la UI — solo se
        // traduce el texto visible (DisplayMember), vía MedioPagoItem (mismo patrón que
        // Usuarios.PerfilItem).
        private void CargarMediosPago(IDictionary<string, Traduccion> t)
        {
            string TT(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;
            object seleccionActual = cmbMedioPago.SelectedItem is MedioPagoItem mpi ? mpi.Value : null;

            cmbMedioPago.DataSource = null;
            cmbMedioPago.DisplayMember = "Label";
            cmbMedioPago.ValueMember   = "Value";
            cmbMedioPago.DataSource = new[]
            {
                new MedioPagoItem("Efectivo",      TT("medio.efectivo",      "Efectivo")),
                new MedioPagoItem("Tarjeta",        TT("medio.tarjeta",       "Tarjeta")),
                new MedioPagoItem("Transferencia",  TT("medio.transferencia", "Transferencia")),
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
                        c.IdContratacion, c.NombreCliente, c.NombrePlan, c.MontoPlan.ToString("C2"),
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

            var t = Traductor.ObtenerTraducciones(_idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            if (cmbMedioPago.SelectedItem == null)
            {
                MostrarError(T("err.contratacion.mediopago_requerido", "Seleccioná el medio de pago antes de cobrar."));
                return;
            }
            string medioPago = ((MedioPagoItem)cmbMedioPago.SelectedItem).Value;

            var confirmar = MessageBox.Show(
                string.Format(
                    T("conf.contratacion.cobro.msg",
                      "¿Confirmar el cobro de la Contratación #{0}?\n\n" +
                      "Cliente: {1}\nPlan: {2}\nMonto: {3:C2}\nMedio de pago: {4}\n\n" +
                      "Se emitirá el comprobante y la suscripción quedará formalizada."),
                    contratacion.IdContratacion, contratacion.NombreCliente, contratacion.NombrePlan,
                    contratacion.MontoPlan, medioPago),
                T("conf.contratacion.cobro.titulo", "Confirmar Cobro"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                contratacionBLL.ConfirmarPago(this.Text, contratacion, medioPago);
                MostrarOk(string.Format(T("msg.contratacion.cobrada", "Contratación #{0} cobrada. Suscripción formalizada."),
                    contratacion.IdContratacion));
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnIntentoFallido_Click(object sender, EventArgs e)
        {
            var contratacion = ObtenerSeleccionada();
            if (contratacion == null) return;

            var t = Traductor.ObtenerTraducciones(_idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            var confirmar = MessageBox.Show(
                string.Format(
                    T("conf.contratacion.intentofallido.msg",
                      "¿Registrar un intento de pago fallido para la Contratación #{0}?\n\n" +
                      "Intentos hasta ahora: {1}/3. Al llegar a 3 se cancela automáticamente."),
                    contratacion.IdContratacion, contratacion.IntentosPago),
                T("conf.contratacion.intentofallido.titulo", "Registrar Intento Fallido"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                contratacionBLL.RegistrarIntentoFallido(this.Text, contratacion);
                MostrarOk(string.Format(T("msg.contratacion.intentofallido_registrado", "Intento fallido registrado para la Contratación #{0}."),
                    contratacion.IdContratacion));
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
