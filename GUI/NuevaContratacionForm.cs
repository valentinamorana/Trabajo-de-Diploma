using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN02, carril Vendedor del diagrama de actividad:
    ///   Identificar cliente → ¿Registrado? (No: Registrar cliente) → Presentar planes →
    ///   ¿Elige plan y modalidad? (No: Asentar desistimiento) → Registrar contratación →
    ///   ¿Contratación válida? (No: Informar motivo; Sí: «Orden de cobro» para Caja).
    /// Solo muestra datos y llama a BLL.Contratacion: las reglas (validez, cupo, importe) viven ahí.
    ///
    /// Accesible desde Menú → Suscriptores → Nueva contratación (permiso mnuClientes).
    /// </summary>
    public partial class NuevaContratacionForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IContratacionService contratacionBLL = new BLL.Contratacion();
        private readonly BLL.Interfaces.IClienteService clienteBLL = new BLL.Cliente();

        private List<BE.Cliente> _coincidencias = new List<BE.Cliente>();
        private List<BE.PlanSuscripcion> _planes = new List<BE.PlanSuscripcion>();
        private BE.Cliente _cliente;
        private bool _contratacionValida;

        public int IdContratacionCreada { get; private set; }
        public bool FueDesistimiento { get; private set; }

        public NuevaContratacionForm()
        {
            InitializeComponent();
        }

        // Abre el asistente con el cliente ya identificado (por ejemplo, recién registrado en Clientes).
        private readonly string _identificacionInicial;
        public NuevaContratacionForm(string identificacion) : this()
        {
            _identificacionInicial = identificacion;
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

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            foreach (Control c in Controls)
                if (c.Tag != null && t.ContainsKey(c.Tag.ToString()))
                    c.Text = t[c.Tag.ToString()].Texto;
            CargarModalidades();
            MostrarPlanes();
            if (_cliente != null) lblFicha.Text = Ficha(_cliente);
            ActualizarImporte();
        }

        private void NuevaContratacionForm_Load(object sender, EventArgs e)
        {
            try { _planes = contratacionBLL.PresentarPlanes(); }
            catch (Exception ex) { MostrarError(ex); }
            MostrarPlanes();
            txtIdentificacion.Focus();
            if (!string.IsNullOrWhiteSpace(_identificacionInicial))
            {
                txtIdentificacion.Text = _identificacionInicial;
                BuscarCliente();
            }
        }

        // ── "Identificar cliente" → ¿Registrado? ─────────────────────────────

        private void TxtIdentificacion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BuscarCliente(); }
        }

        private void BtnBuscar_Click(object sender, EventArgs e) => BuscarCliente();

        private void BuscarCliente()
        {
            SeleccionarCliente(null);
            lstCoincidencias.Visible = false;
            lstCoincidencias.Items.Clear();
            btnRegistrarCliente.Visible = false;

            try { _coincidencias = contratacionBLL.IdentificarCliente(txtIdentificacion.Text); }
            catch (Exception ex) { MostrarError(ex); return; }

            if (_coincidencias.Count == 0)
            {
                // ¿Registrado? No → Registrar cliente.
                MostrarError(Tr("msg.contr.noregistrado",
                    "El cliente no está registrado. Registralo para continuar con la contratación."));
                btnRegistrarCliente.Visible = true;
                return;
            }
            if (_coincidencias.Count == 1) { SeleccionarCliente(_coincidencias[0]); return; }

            foreach (var c in _coincidencias)
                lstCoincidencias.Items.Add($"{c.NombreCompleto}  (DNI {c.DNI})");
            lstCoincidencias.Visible = true;
            MostrarOk(Tr("lbl.ped.variascoinc", "Hay varios clientes que coinciden: elegí el correcto en la lista."));
        }

        private void LstCoincidencias_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = lstCoincidencias.SelectedIndex;
            if (i >= 0 && i < _coincidencias.Count) SeleccionarCliente(_coincidencias[i]);
        }

        // "Registrar cliente" (ABM de Clientes): el alta solo registra los datos personales.
        private void BtnRegistrarCliente_Click(object sender, EventArgs e)
        {
            using (var form = new ClienteForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    clienteBLL.Alta(this.Text, form.ClienteEditado);
                    txtIdentificacion.Text = form.ClienteEditado.DNI;
                    BuscarCliente();
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        private void SeleccionarCliente(BE.Cliente cliente)
        {
            _cliente = cliente;
            lblFicha.Visible = cliente != null;
            lblFicha.Text = cliente != null ? Ficha(cliente) : string.Empty;
            dgvPlanes.Enabled = cmbModalidad.Enabled = btnDesistir.Enabled = cliente != null;
            if (cliente != null) lstCoincidencias.Visible = lstCoincidencias.Visible && _coincidencias.Count > 1;
            Validar();
        }

        private string Ficha(BE.Cliente c) => string.Format(
            Tr("lbl.contr.ficha", "Cliente: {0}  —  DNI {1}\nPlan actual: {2}  —  Vence: {3}  —  Prendas en uso: {4}"),
            c.NombreCompleto, c.DNI, c.NombrePlan ?? "—",
            c.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "—", c.StockUtilizado);

        // ── "Presentar planes" («Planes disponibles») ────────────────────────

        private void MostrarPlanes()
        {
            int? sel = PlanSeleccionado()?.IdPlan;
            var tabla = new DataTable();
            tabla.Columns.Add("ID", typeof(int));
            tabla.Columns.Add(Tr("col.contr.plan", "Plan"), typeof(string));
            tabla.Columns.Add(Tr("col.contr.preciomensual", "Precio mensual"), typeof(string));
            tabla.Columns.Add(Tr("col.contr.limite", "Límite de prendas"), typeof(int));
            foreach (var p in _planes)
                tabla.Rows.Add(p.IdPlan, p.Nombre, p.Precio.ToString("C2"), p.LimitePrendas);
            dgvPlanes.DataSource = tabla;
            if (dgvPlanes.Columns.Contains("ID")) dgvPlanes.Columns["ID"].Visible = false;
            dgvPlanes.ClearSelection();
            if (sel.HasValue)
                foreach (DataGridViewRow r in dgvPlanes.Rows)
                    if ((int)r.Cells["ID"].Value == sel.Value) r.Selected = true;
        }

        private void BtnImprimirPlanes_Click(object sender, EventArgs e)
        {
            try { Exportacion.DocumentosContratacion.Imprimir(Exportacion.DocumentosContratacion.PlanesDisponibles(_planes), this); }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.PlanSuscripcion PlanSeleccionado()
        {
            if (dgvPlanes.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPlanes.SelectedRows[0].Cells["ID"].Value);
            return _planes.Find(p => p.IdPlan == id);
        }

        private BE.Builders.ModalidadCobro? ModalidadSeleccionada() =>
            cmbModalidad.SelectedItem is ModalidadItem m ? (BE.Builders.ModalidadCobro?)m.Valor : null;

        private void CargarModalidades()
        {
            var actual = ModalidadSeleccionada();
            cmbModalidad.Items.Clear();
            foreach (BE.Builders.ModalidadCobro m in Enum.GetValues(typeof(BE.Builders.ModalidadCobro)))
                cmbModalidad.Items.Add(new ModalidadItem(m, Exportacion.DocumentosContratacion.Modalidad(m)));
            cmbModalidad.SelectedIndex = 0;
            for (int i = 0; i < cmbModalidad.Items.Count; i++)
                if (actual.HasValue && ((ModalidadItem)cmbModalidad.Items[i]).Valor == actual.Value) cmbModalidad.SelectedIndex = i;
        }

        private sealed class ModalidadItem
        {
            public BE.Builders.ModalidadCobro Valor { get; }
            public string Texto { get; }
            public ModalidadItem(BE.Builders.ModalidadCobro v, string t) { Valor = v; Texto = t; }
            public override string ToString() => Texto;
        }

        private void DgvPlanes_SelectionChanged(object sender, EventArgs e) => Validar();

        private void CmbModalidad_SelectedIndexChanged(object sender, EventArgs e) => ActualizarImporte();

        // ¿Contratación válida? Lo decide la BLL; acá solo se informa el motivo ("Informar motivo").
        private void Validar()
        {
            _contratacionValida = false;
            var plan = PlanSeleccionado();
            if (_cliente != null && plan != null)
            {
                try
                {
                    contratacionBLL.ValidarContratacion(_cliente.IdCliente, plan.IdPlan);
                    _contratacionValida = true;
                    lblMensaje.Text = string.Empty;
                }
                catch (Exception ex) { MostrarError(ex); }
            }
            btnConfirmar.Enabled = _contratacionValida;
            ActualizarImporte();
        }

        // Importe que el Vendedor le informa al cliente (lo calcula la BLL con la misma regla del cobro).
        private void ActualizarImporte()
        {
            lblImporte.Text = string.Empty;
            var plan = PlanSeleccionado();
            var modalidad = ModalidadSeleccionada();
            if (_cliente == null || plan == null || !modalidad.HasValue) return;
            try
            {
                var liq = contratacionBLL.EstimarImporte(_cliente.IdCliente, plan.IdPlan, modalidad.Value);
                if (liq == null) return;
                lblImporte.Text = liq.Descuento > 0
                    ? string.Format(Tr("lbl.contr.importedesc", "Importe a abonar en Caja: {0:C2}\n(incluye un descuento de {1:C2}: {2})"),
                                    liq.Total, liq.Descuento, liq.NombrePromocion ?? Tr("lbl.contratacion.creditoreferido", "crédito por referido"))
                    : string.Format(Tr("lbl.contr.importe", "Importe a abonar en Caja: {0:C2}"), liq.Total);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[NuevaContratacionForm] Importe: " + ex.Message); }
        }

        // ── "Registrar contratación" → «Orden de cobro» ──────────────────────

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            var plan = PlanSeleccionado();
            var modalidad = ModalidadSeleccionada();
            if (_cliente == null || plan == null || !modalidad.HasValue)
            {
                MostrarError(Tr("err.contratacion.faltandatos", "Seleccioná un cliente y un plan para continuar."));
                return;
            }

            if (!ConfirmarSiNo(
                    Tr("conf.contratacion.crear.msg",
                       "¿Registrar la contratación del plan '{0}' ({1}) para {2}?\n\nQuedará pendiente de pago hasta que Caja confirme el cobro.",
                       new object[] { plan.Nombre, Exportacion.DocumentosContratacion.Modalidad(modalidad.Value), _cliente.NombreCompleto }),
                    Tr("conf.contratacion.crear.titulo", "Confirmar Contratación")))
                return;

            try
            {
                IdContratacionCreada = contratacionBLL.RegistrarContratacion(this.Text, _cliente.IdCliente, plan.IdPlan, modalidad.Value);
                OfrecerImprimir(Tr("conf.contr.imprimirorden", "¿Imprimir la orden de cobro para que el cliente abone en Caja?"), () =>
                {
                    var c = contratacionBLL.ObtenerPorId(IdContratacionCreada);
                    return Exportacion.DocumentosContratacion.OrdenDeCobro(c, contratacionBLL.CalcularImporte(c));
                });
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MostrarError(ex); Validar(); }
        }

        // ── ¿Elige plan y modalidad? No → "Asentar desistimiento" ─────────────

        private void BtnDesistir_Click(object sender, EventArgs e)
        {
            if (_cliente == null) return;
            string motivo;
            using (var dlg = new InputDialog(
                Tr("dlg.desistir.titulo", "Asentar desistimiento"),
                Tr("dlg.desistir.prompt", "Motivo del desistimiento que comunicó el cliente:"), false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText;
            }

            try
            {
                int id = contratacionBLL.AsentarDesistimiento(this.Text, _cliente.IdCliente,
                    PlanSeleccionado()?.IdPlan, ModalidadSeleccionada(), motivo);
                FueDesistimiento = true;
                OfrecerImprimir(Tr("conf.ped.aviso", "¿Imprimir el aviso de desistimiento?"),
                    () => Exportacion.DocumentosContratacion.AvisoDesistimiento(contratacionBLL.ObtenerDesistimiento(id)));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void OfrecerImprimir(string pregunta, Func<Exportacion.ReporteExportable> armar)
        {
            if (!ConfirmarSiNo(pregunta, this.Text, porDefectoNo: true))
                return;
            try { Exportacion.DocumentosContratacion.Imprimir(armar(), this); }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
