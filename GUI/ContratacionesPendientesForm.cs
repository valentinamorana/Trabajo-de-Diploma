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
                tabla.Columns.Add("Modalidad", typeof(string));
                tabla.Columns.Add("Intentos", typeof(string));
                tabla.Columns.Add("Fecha", typeof(string));

                foreach (var c in _contrataciones)
                    tabla.Rows.Add(
                        c.IdContratacion, c.NombreCliente, c.NombrePlan, c.Modalidad.ToString(),
                        $"{c.IntentosPago}/3", c.FechaAlta.ToString("dd/MM/yyyy HH:mm"));

                dgvContrataciones.DataSource = tabla;
                if (dgvContrataciones.Columns.Contains("ID"))
                    dgvContrataciones.Columns["ID"].Width = 44;
                TraducirHeadersGrilla(Traductor.ObtenerTraducciones(_idioma));

                lblConteo.Text = $"{_contrataciones.Count} contratación(es) pendiente(s) de pago.";
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

            if (cmbMedioPago.SelectedItem == null)
            {
                MostrarError("Seleccioná el medio de pago antes de cobrar.");
                return;
            }
            string medioPago = cmbMedioPago.SelectedItem.ToString();

            var confirmar = MessageBox.Show(
                $"¿Confirmar el cobro de la Contratación #{contratacion.IdContratacion}?\n\n" +
                $"Cliente: {contratacion.NombreCliente}\nPlan: {contratacion.NombrePlan}\nMedio de pago: {medioPago}\n\n" +
                "Se emitirá el comprobante y la suscripción quedará formalizada.",
                "Confirmar Cobro",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                contratacionBLL.ConfirmarPago(this.Text, contratacion, medioPago);
                MostrarOk($"Contratación #{contratacion.IdContratacion} cobrada. Suscripción formalizada.");
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnIntentoFallido_Click(object sender, EventArgs e)
        {
            var contratacion = ObtenerSeleccionada();
            if (contratacion == null) return;

            var confirmar = MessageBox.Show(
                $"¿Registrar un intento de pago fallido para la Contratación #{contratacion.IdContratacion}?\n\n" +
                $"Intentos hasta ahora: {contratacion.IntentosPago}/3. Al llegar a 3 se cancela automáticamente.",
                "Registrar Intento Fallido",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                contratacionBLL.RegistrarIntentoFallido(this.Text, contratacion);
                MostrarOk($"Intento fallido registrado para la Contratación #{contratacion.IdContratacion}.");
                CargarContrataciones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
