using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Formulario de creación de Pedido de Venta.
    ///
    /// Flujo en 2 pasos visuales dentro del mismo form:
    ///   PASO 1 — Seleccionar cliente
    ///   PASO 2 — Seleccionar prendas disponibles (respeta límite del plan)
    ///
    /// Devuelve DialogResult.OK cuando el pedido fue creado exitosamente.
    /// El ID del pedido creado queda en IdPedidoCreado.
    /// </summary>
    /// <summary>
    /// Hereda de <see cref="FormBase"/>:
    ///   - MostrarError() → heredado, no se redeclara
    ///   - MensajeLabel → sobreescrito para devolver el lblMensaje de este formulario
    /// </summary>
    public partial class NuevoPedidoForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        public int IdPedidoCreado { get; private set; }

        // ── BLL ───────────────────────────────────────────────────────────────
        private readonly BLL.Interfaces.IClienteService clienteBLL = new BLL.Cliente();
        private readonly BLL.Interfaces.IPrendaService  prendaBLL  = new BLL.Prenda();
        private readonly BLL.Interfaces.IPedidoService  pedidoBLL  = new BLL.Pedido();

        // ── Estado interno ────────────────────────────────────────────────────
        private List<BE.Cliente> _clientes    = new List<BE.Cliente>();
        private List<BE.Prenda>  _disponibles = new List<BE.Prenda>();
        private BE.Cliente       _clienteSel  = null;
        private Idioma           _idioma      = GestorIdioma.IdiomaActual;

        public NuevoPedidoForm()
        {
            InitializeComponent();
            AplicarIdioma(GestorIdioma.IdiomaActual);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        // ── IIdiomaObserver ───────────────────────────────────────────────────

        public void UpdateLanguage(Idioma idioma)
        {
            AplicarIdioma(idioma);
            if (dgvPrendas.Columns.Count > 0)
                TraducirHeadersGrilla();
        }

        // ── Traducción ────────────────────────────────────────────────────────

        private void AplicarIdioma(Idioma idioma)
        {
            _idioma = idioma;
            this.Text                 = Tr("frm.nuevopedido",     "Nuevo Pedido de Venta");
            lblPaso.Text              = Tr("paso1.texto",          "Paso 1 de 2 — Seleccionar Cliente");
            lblSeleccionaCliente.Text = Tr("lbl.ped.selcliente",   "Seleccioná el cliente para este pedido:");
            lblInstruccion.Text       = Tr("lbl.ped.selprendas",   "Seleccioná las prendas para incluir en el pedido (checkbox):");
            btnSiguiente.Text         = Tr("btn.siguiente",         "Siguiente →");
            btnVolver.Text            = Tr("btn.volver",            "← Volver");
            btnConfirmar.Text         = Tr("btn.confirmar.pedido",  "✓ Confirmar Pedido");
        }

        private void TraducirHeadersGrilla()
        {
            var t = Traductor.ObtenerTraducciones(_idioma);
            void RH(string col, string clave, string fallback)
            {
                if (dgvPrendas.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvPrendas.Columns[col].HeaderText = t[clave].Texto;
                else if (dgvPrendas.Columns.Contains(col))
                    dgvPrendas.Columns[col].HeaderText = fallback;
            }
            RH("Nombre",   "col.prenda.nombre",    "Nombre");
            RH("Categoria","col.prenda.categoria",  "Categoría");
            RH("Talle",    "col.prenda.talle",      "Talle");
            RH("Color",    "col.prenda.color",      "Color");
        }

        private void NuevoPedidoForm_Load(object sender, EventArgs e)
        {
            CargarDatosIniciales();
        }

        private void BtnVolver_Click(object sender, EventArgs e)
        {
            MostrarPaso(1);
        }

        private void DgvPrendas_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvPrendas.IsCurrentCellDirty)
                dgvPrendas.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // ── Carga de datos ────────────────────────────────────────────────────

        private void CargarDatosIniciales()
        {
            try
            {
                string placeholder = Tr("combo.ped.placeholder", "— Seleccioná un cliente —");

                _clientes = clienteBLL.ObtenerTodos();
                cmbCliente.Items.Clear();
                cmbCliente.Items.Add(placeholder);
                foreach (var c in _clientes)
                {
                    string etiqueta = $"{c.NombreCompleto}  (DNI {c.DNI})";
                    if (c.VencimientoExpirado)
                        etiqueta += "  ⚠ venc.";
                    else if (c.SuscripcionProximaAVencer())
                        etiqueta += $"  ⏰ {c.DiasHastaVencimiento().Value}d";
                    cmbCliente.Items.Add(etiqueta);
                }
                cmbCliente.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void CargarPrendasDisponibles()
        {
            try
            {
                _disponibles = prendaBLL.ObtenerDisponibles(_clienteSel?.IdCliente);
                dgvPrendas.Rows.Clear();

                foreach (var p in _disponibles)
                {
                    dgvPrendas.Rows.Add(false, p.IdPrenda, p.Nombre,
                        p.Categoria ?? "—", p.Talle ?? "—", p.Color ?? "—");
                }

                TraducirHeadersGrilla();
                ActualizarResumen();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ── Navegación entre pasos ────────────────────────────────────────────

        private void MostrarPaso(int paso)
        {
            panelPaso1.Visible = paso == 1;
            panelPaso2.Visible = paso == 2;
            string paso1txt = Tr("paso1.texto", "Paso 1 de 2 — Seleccionar Cliente");
            string paso2txt = Tr("paso2.texto", "Paso 2 de 2 — Seleccionar Prendas");
            lblPaso.Text = paso == 1
                ? paso1txt
                : $"{paso2txt}  ({_clienteSel?.NombreCompleto})";
            lblMensaje.Text = string.Empty;

            if (paso == 2) CargarPrendasDisponibles();
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        private void CmbCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbCliente.SelectedIndex - 1;
            if (idx < 0)
            {
                lblInfoPlan.Visible  = false;
                btnSiguiente.Enabled = false;
                _clienteSel          = null;
                return;
            }

            _clienteSel = _clientes[idx];

            // Consultar la BLL — sin interpretar reglas de negocio en la GUI
            var estado = clienteBLL.ObtenerEstadoComercial(_clienteSel, 0);

            if (!estado.PuedeProceder)
            {
                btnSiguiente.Enabled  = false;
                lblInfoPlan.ForeColor = Color.DarkRed;
                lblInfoPlan.Visible   = true;

                if (estado.MotivoBloqueo == "SIN_PLAN")
                {
                    lblInfoPlan.Text = string.Format(
                        Tr("err.ped.sinplan",
                            "⚠ {0} no tiene plan asignado.\nAsigná un plan en el módulo de Clientes antes de crear un pedido."),
                        _clienteSel.NombreCompleto);
                }
                else if (estado.MotivoBloqueo == "SUSCRIPCION_VENCIDA")
                {
                    lblInfoPlan.Text = string.Format(
                        Tr("err.ped.suscvencida",
                            "⚠ La suscripción de {0} venció el {1}.\nRenovar en el módulo de Clientes."),
                        _clienteSel.NombreCompleto,
                        estado.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "—");
                }
                return;
            }

            btnSiguiente.Enabled  = true;
            lblInfoPlan.Visible   = true;

            string infoBase = string.Format(
                Tr("lbl.ped.infoplan",
                    "Cliente: {0}\nPlan: {1}\nPrendas en uso actualmente: {2}\nMétodo de pago: {3}\nAlta: {4}"),
                _clienteSel.NombreCompleto,
                estado.NombrePlan ?? "—",
                estado.StockUtilizado,
                estado.MetodoPago,
                estado.FechaAlta.ToString("dd/MM/yyyy"));

            if (estado.SuscripcionProximaAVencer)
            {
                lblInfoPlan.ForeColor = Color.FromArgb(160, 100, 0);
                lblInfoPlan.Text = infoBase + "\n" + string.Format(
                    Tr("lbl.ped.proxvencer",
                        "⏰ La suscripción vence en {0} día(s). Avisale al cliente para renovarla."),
                    estado.DiasHastaVencimiento);
            }
            else
            {
                lblInfoPlan.ForeColor = Color.FromArgb(176, 62, 96);
                lblInfoPlan.Text = infoBase;
            }
        }

        private void BtnSiguiente_Click(object sender, EventArgs e)
        {
            if (_clienteSel == null) return;
            MostrarPaso(2);
        }

        private void DgvPrendas_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvPrendas.Columns["Sel"].Index)
                ActualizarResumen();
        }

        private void ActualizarResumen()
        {
            int seleccionadas = ContarSeleccionadas();

            // La BLL evalúa todas las reglas de negocio y devuelve un DTO listo para mostrar
            var estado = clienteBLL.ObtenerEstadoComercial(_clienteSel, seleccionadas);

            int enUso  = estado.StockUtilizado;
            int limite = estado.LimitePrendas;
            int total  = enUso + seleccionadas;

            string linea1 = string.Format(
                Tr("lbl.ped.res.linea1", "Seleccionadas: {0}  |  Ya en uso: {1}  |  Total: {2}"),
                seleccionadas, enUso, total);
            string linea2 = "";

            if (limite > 0)
            {
                if (estado.SuperaLimite)
                {
                    linea2 = string.Format(
                        Tr("lbl.ped.res.limite",
                           "✗ El plan '{0}' permite {1} prenda(s). Estás superando el límite por {2}."),
                        estado.NombrePlan, limite, estado.Exceso);
                    lblResumen.ForeColor = Color.DarkRed;
                    btnConfirmar.Enabled = false;
                }
                else if (seleccionadas == 0)
                {
                    linea2 = string.Format(
                        Tr("lbl.ped.res.vacio", "Podés agregar hasta {0} prenda(s) (plan {1})."),
                        estado.PrendasDisponibles, estado.NombrePlan);
                    lblResumen.ForeColor = Color.DimGray;
                    btnConfirmar.Enabled = false;
                }
                else if (seleccionadas < estado.PrendasDisponibles)
                {
                    linea2 = string.Format(
                        Tr("lbl.ped.res.parcial",
                           "ℹ El plan '{0}' permite {1}. Estás eligiendo {2} de {3} posibles — podés agregar más."),
                        estado.NombrePlan, limite, seleccionadas, estado.PrendasDisponibles);
                    lblResumen.ForeColor = Color.FromArgb(140, 100, 0);
                    btnConfirmar.Enabled = true;
                }
                else
                {
                    linea2 = string.Format(
                        Tr("lbl.ped.res.lleno",
                           "✓ Alcanzás el máximo del plan '{0}' ({1} prendas)."),
                        estado.NombrePlan, limite);
                    lblResumen.ForeColor = Color.DarkGreen;
                    btnConfirmar.Enabled = true;
                }
            }
            else
            {
                btnConfirmar.Enabled = seleccionadas > 0;
                lblResumen.ForeColor = Color.FromArgb(176, 62, 96);
            }

            lblResumen.Text = string.IsNullOrEmpty(linea2)
                ? linea1
                : linea1 + "\r\n" + linea2;
        }

        private int ContarSeleccionadas()
        {
            int count = 0;
            foreach (DataGridViewRow row in dgvPrendas.Rows)
            {
                if (row.Cells["Sel"].Value is bool sel && sel) count++;
            }
            return count;
        }

        private List<BE.Prenda> ObtenerPrendasSeleccionadas()
        {
            var lista = new List<BE.Prenda>();
            foreach (DataGridViewRow row in dgvPrendas.Rows)
            {
                if (row.Cells["Sel"].Value is bool sel && sel)
                {
                    int id = Convert.ToInt32(row.Cells["ID"].Value);
                    var p = _disponibles.Find(pr => pr.IdPrenda == id);
                    if (p != null) lista.Add(p);
                }
            }
            return lista;
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            var prendas = ObtenerPrendasSeleccionadas();
            if (prendas.Count == 0)
            {
                var tSP = Traductor.ObtenerTraducciones(_idioma);
                MostrarError(tSP.ContainsKey("err.ped.sinprendas") ? tSP["err.ped.sinprendas"].Texto : "Seleccioná al menos una prenda.");
                return;
            }

            // Confirmación final
            string detallesPrendas = string.Join("\n  • ",
                prendas.ConvertAll(p => $"{p.Nombre} ({p.Talle} — {p.Color})"));

            var confirmar = MessageBox.Show(
                string.Format(
                    Tr("conf.ped.msg",
                       "Confirmar pedido para {0}:\n\n  • {1}\n\nTotal: {2} prenda(s)\nMétodo de pago: {3}"),
                    _clienteSel.NombreCompleto,
                    detallesPrendas,
                    prendas.Count,
                    _clienteSel.MetodoPago),
                Tr("conf.ped.titulo", "Confirmar Pedido"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                btnConfirmar.Enabled = false;
                var tp = Traductor.ObtenerTraducciones(_idioma);
                btnConfirmar.Text = tp.ContainsKey("btn.procesando") ? tp["btn.procesando"].Texto : "Procesando...";
                // Sin este Refresh(), WinForms no repinta el cambio de Enabled/Text hasta que el
                // hilo de UI vuelve al loop de mensajes — es decir, hasta que CrearPedido (llamada
                // sincrónica a BD) ya terminó. El usuario nunca llegaba a ver realmente "Procesando…".
                this.Refresh();

                IdPedidoCreado = pedidoBLL.CrearPedido(this.Text, _clienteSel.IdCliente, prendas);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                btnConfirmar.Enabled = true;
                var te = Traductor.ObtenerTraducciones(_idioma);
                btnConfirmar.Text = te.ContainsKey("btn.confirmar.pedido") ? te["btn.confirmar.pedido"].Texto : "✓ Confirmar Pedido";
                MostrarError(ex);
            }
        }

    }
}
