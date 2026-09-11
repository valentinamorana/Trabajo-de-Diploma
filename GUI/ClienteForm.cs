using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;
using System.Linq;

namespace GUI
{
    /// <summary>
    /// Diálogo modal para dar de alta o modificar un cliente.
    /// Se abre desde el formulario Clientes con ShowDialog().
    /// Devuelve DialogResult.OK con ClienteEditado cargado si el usuario confirma.
    /// </summary>
    public partial class ClienteForm : FormBase
    {
        // ── Resultado del diálogo ─────────────────────────────────────────────
        public BE.Cliente ClienteEditado { get; private set; }

        // ── Modo del formulario ───────────────────────────────────────────────
        private readonly bool _esEdicion;
        private readonly BE.Cliente _clienteOriginal;

        private List<BE.PlanSuscripcion> _planes;
        private List<BE.Cliente> _clientesParaReferente;

        protected override Label MensajeLabel => lblMensaje;

        /// <summary>Constructor para ALTA (cliente nuevo).</summary>
        public ClienteForm() : this(null) { }

        /// <summary>
        /// Constructor para ALTA o EDICIÓN.
        /// Si cliente es null, modo alta; si tiene datos, modo edición.
        /// </summary>
        public ClienteForm(BE.Cliente cliente)
        {
            InitializeComponent();

            _esEdicion       = cliente != null;
            _clienteOriginal = cliente;

            CargarPlanes();
            CargarReferentes();
            AplicarIdioma(GestorIdioma.IdiomaActual);

            // PN02 — el plan (y su activación) ya no se elige en este diálogo: un cliente
            // nuevo se registra sin plan y lo adquiere después a través de una Contratación
            // (Vendedor) confirmada por Caja. En edición sí se puede corregir el plan
            // directamente (ajuste administrativo puntual, no pasa por Caja) junto con el
            // vencimiento, o mediante el proceso de Renovación (PdN5 — Chain of Responsibility).
            lblPlan.Visible = _esEdicion;
            cmbPlan.Visible = _esEdicion;
            chkVencimiento.Visible = _esEdicion;
            dtpVencimiento.Visible = _esEdicion;

            // Bloque 1 — Programa de referidos: el referente se fija una única vez, al alta
            // (ver DAL.Cliente.Alta/Modificar — IdClienteReferente no se puede editar después).
            lblReferente.Visible = !_esEdicion;
            cmbReferente.Visible = !_esEdicion;

            if (_esEdicion) CargarDatosExistentes();
        }

        // ── Traducción ────────────────────────────────────────────────────────

        /// <summary>
        /// Aplica el idioma activo al abrir el diálogo.
        /// ClienteForm es modal → no necesita Observer completo, basta con leer en construcción.
        /// </summary>
        private void AplicarIdioma(Idioma idioma)
        {
            this.Text       = _esEdicion ? Tr("frm.editarcliente", "Editar Cliente")
                                         : Tr("frm.nuevocliente",  "Nuevo Cliente");
            btnGuardar.Text = _esEdicion ? Tr("btn.guardar.cambios",  "Guardar Cambios")
                                         : Tr("btn.registrar.cliente","Registrar Cliente");
            btnCancelar.Text  = Tr("btn.cancelar",        "Cancelar");
            lblNombre.Text          = Tr("lbl.cli.nombre",      "Nombre *");
            lblApellido.Text        = Tr("lbl.cli.apellido",    "Apellido *");
            lblDNI.Text             = Tr("lbl.cli.dni",         "DNI * (7-8 dígitos)");
            lblEmail.Text           = Tr("lbl.cli.email",       "Email");
            lblFechaNacimiento.Text = Tr("lbl.cli.fechanac",    "Fecha de Nacimiento *");
            lblMetodoPago.Text      = Tr("lbl.cli.metodopago",  "Método de Pago *");
            lblPlan.Text            = Tr("lbl.cli.plan",        "Plan de Suscripción");
            lblReferente.Text       = Tr("lbl.cli.referente",   "Referido por");
            chkVencimiento.Text     = Tr("lbl.cli.vencimiento", "Fecha de Vencimiento");

            // Actualizar ítem "— Sin plan —" del combo de planes (índice 0)
            if (cmbPlan.Items.Count > 0)
                cmbPlan.Items[0] = Tr("combo.cli.sinplan", "— Sin plan —");

            // Actualizar ítem "— Ninguno —" del combo de referentes (índice 0)
            if (cmbReferente.Items.Count > 0)
                cmbReferente.Items[0] = Tr("combo.cli.sinreferente", "— Ninguno —");

            // Recargar cmbMetodoPago con etiquetas traducidas (valor interno = clave de BD en español)
            RellenarComboMetodoPago();
        }

        // Clave fija en BD ← muestra etiqueta traducida.
        // El SelectedValue siempre es la cadena en español almacenada en la BD ("Efectivo", etc.).
        private void RellenarComboMetodoPago()
        {
            string prevValue = (cmbMetodoPago.SelectedItem as MetodoItem)?.Value
                            ?? cmbMetodoPago.SelectedItem?.ToString();

            var items = new[]
            {
                new MetodoItem("Efectivo",      Tr("metodo.efectivo",      "Efectivo")),
                new MetodoItem("Débito",        Tr("metodo.debito",        "Débito")),
                new MetodoItem("Crédito",       Tr("metodo.credito",       "Crédito")),
                new MetodoItem("Transferencia", Tr("metodo.transferencia", "Transferencia")),
            };

            cmbMetodoPago.DataSource    = null;
            cmbMetodoPago.DisplayMember = "Label";
            cmbMetodoPago.ValueMember   = "Value";
            cmbMetodoPago.DataSource    = items;

            // Restaurar selección previa por valor interno
            int idx = Array.FindIndex(items, m => m.Value == prevValue);
            cmbMetodoPago.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private class MetodoItem
        {
            public string Value { get; }
            public string Label { get; }
            public MetodoItem(string value, string label) { Value = value; Label = label; }
            public override string ToString() => Label;
        }

        // ── Eventos del Designer ──────────────────────────────────────────────

        private void ChkVencimiento_CheckedChanged(object sender, EventArgs e)
        {
            dtpVencimiento.Enabled = chkVencimiento.Checked;
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ── Carga de datos ────────────────────────────────────────────────────

        private void CargarPlanes()
        {
            try
            {
                var bllPlan = new BLL.PlanSuscripcion();
                _planes = bllPlan.ObtenerActivos();

                string sinPlan = Tr("combo.cli.sinplan", "— Sin plan —");
                cmbPlan.Items.Clear();
                cmbPlan.Items.Add(sinPlan);
                foreach (var p in _planes)
                    cmbPlan.Items.Add($"{p.Nombre}  ({p.LimitePrendas} prendas — ${p.Precio:N2})");

                cmbPlan.SelectedIndex = 0;
            }
            catch
            {
                string errPlanes = Tr("err.cli.errorplanes", "— Error al cargar planes —");
                cmbPlan.Items.Add(errPlanes);
                cmbPlan.SelectedIndex = 0;
            }
        }

        private void CargarReferentes()
        {
            try
            {
                var bllCliente = new BLL.Cliente();
                _clientesParaReferente = bllCliente.ObtenerTodos();

                string ninguno = Tr("combo.cli.sinreferente", "— Ninguno —");
                cmbReferente.Items.Clear();
                cmbReferente.Items.Add(ninguno);
                foreach (var c in _clientesParaReferente)
                    cmbReferente.Items.Add($"{c.Nombre} {c.Apellido} (DNI {c.DNI})");

                cmbReferente.SelectedIndex = 0;
            }
            catch
            {
                cmbReferente.Items.Clear();
                cmbReferente.Items.Add("— Ninguno —");
                cmbReferente.SelectedIndex = 0;
            }
        }

        private void CargarDatosExistentes()
        {
            txtNombre.Text   = _clienteOriginal.Nombre;
            txtApellido.Text = _clienteOriginal.Apellido;
            txtDNI.Text      = _clienteOriginal.DNI;
            txtEmail.Text    = _clienteOriginal.Email ?? "";

            // Buscar por Value interno (independiente del idioma mostrado)
            int idxPago = -1;
            for (int pi = 0; pi < cmbMetodoPago.Items.Count; pi++)
                if ((cmbMetodoPago.Items[pi] as MetodoItem)?.Value == _clienteOriginal.MetodoPago) { idxPago = pi; break; }
            cmbMetodoPago.SelectedIndex = idxPago >= 0 ? idxPago : 0;

            // Seleccionar plan actual
            if (_clienteOriginal.IdPlan.HasValue && _planes != null)
            {
                int planIdx = _planes.FindIndex(p => p.IdPlan == _clienteOriginal.IdPlan.Value);
                cmbPlan.SelectedIndex = planIdx >= 0 ? planIdx + 1 : 0;  // +1 por "Sin plan"
            }

            // Fecha de nacimiento
            dtpFechaNacimiento.Value = _clienteOriginal.FechaNacimiento.HasValue
                ? _clienteOriginal.FechaNacimiento.Value
                : DateTime.Today.AddYears(-18);

            // Fecha de vencimiento
            if (_clienteOriginal.FechaVencimiento.HasValue)
            {
                chkVencimiento.Checked = true;
                dtpVencimiento.Enabled = true;
                dtpVencimiento.Value   = _clienteOriginal.FechaVencimiento.Value;
            }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            lblMensaje.Text = string.Empty;

            try
            {
                int? idPlan = null;
                if (cmbPlan.SelectedIndex > 0 && _planes != null && _planes.Count >= cmbPlan.SelectedIndex)
                    idPlan = _planes[cmbPlan.SelectedIndex - 1].IdPlan;

                int? idReferente = (!_esEdicion && cmbReferente.SelectedIndex > 0
                                     && _clientesParaReferente != null && _clientesParaReferente.Count >= cmbReferente.SelectedIndex)
                    ? _clientesParaReferente[cmbReferente.SelectedIndex - 1].IdCliente
                    : (int?)null;

                ClienteEditado = new BE.Cliente
                {
                    IdCliente        = _esEdicion ? _clienteOriginal.IdCliente : 0,
                    Nombre           = txtNombre.Text.Trim(),
                    Apellido         = txtApellido.Text.Trim(),
                    DNI              = txtDNI.Text.Trim(),
                    Email            = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    MetodoPago       = (cmbMetodoPago.SelectedItem as MetodoItem)?.Value ?? "Efectivo",
                    IdPlan           = idPlan,
                    FechaNacimiento  = dtpFechaNacimiento.Value.Date,
                    FechaAlta        = _esEdicion ? _clienteOriginal.FechaAlta : DateTime.Now,
                    FechaVencimiento = chkVencimiento.Checked ? dtpVencimiento.Value.Date : (DateTime?)null,
                    IdClienteReferente = idReferente
                };

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }
    }
}
