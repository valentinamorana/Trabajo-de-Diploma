using System;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN6 — Cobro de suscripción. El Vendedor intenta cobrar (fuera del sistema:
    /// efectivo/transferencia, sin pasarela) y carga acá el resultado; el patrón Chain
    /// of Responsibility (BLL.Manejadores) resuelve el resto: confirma la renovación,
    /// aplica un período de gracia, o suspende nuevos pedidos.
    /// </summary>
    public class CobroSuscripcionForm : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService _bllCliente = new BLL.Cliente();
        private readonly BLL.Interfaces.ICobroService    _bllCobro  = new BLL.Cobro();

        private Label      _lblTitulo;
        private Label      _lblCliente;
        private ComboBox   _cmbCliente;
        private Label      _lblEstadoActual;
        private GroupBox   _grpDecision;
        private RadioButton _rbCobrado;
        private RadioButton _rbPagoFallido;
        private Label      _lblModalidad;
        private ComboBox   _cmbModalidad;
        private Button     _btnProcesar;
        private Label      _lblResultado;

        public CobroSuscripcionForm()
        {
            this.Text            = "Cobro de Suscripción";
            this.Size            = new Size(480, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.StartPosition   = FormStartPosition.CenterParent;
            ConstruirUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
            CargarClientes();
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
            string Tr(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text           = Tr("cobro.titulo", "Cobro de Suscripción");
            _lblTitulo.Text     = Tr("cobro.titulo", "Cobro de Suscripción");
            _lblCliente.Text    = Tr("cobro.cliente", "Cliente:");
            _grpDecision.Text   = Tr("cobro.decision", "Resultado del cobro");
            _rbCobrado.Text     = Tr("cobro.cobrado", "Cobrado");
            _rbPagoFallido.Text = Tr("cobro.pagofallido", "Pago fallido");
            _lblModalidad.Text  = Tr("renov.modalidad", "Modalidad de cobro:");
            _btnProcesar.Text   = Tr("cobro.procesar", "Procesar");
        }

        private void ConstruirUI()
        {
            _lblTitulo = new Label { Location = new Point(15, 12), AutoSize = true, Font = new Font(Font, FontStyle.Bold) };

            _lblCliente = new Label { Location = new Point(15, 50), AutoSize = true };
            _cmbCliente = new ComboBox { Location = new Point(120, 47), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbCliente.SelectedIndexChanged += (s, e) => MostrarEstadoActual();

            _lblEstadoActual = new Label { Location = new Point(15, 80), AutoSize = false, Size = new Size(430, 34), ForeColor = Color.DimGray };

            _grpDecision = new GroupBox { Location = new Point(15, 120), Size = new Size(430, 80) };
            _rbCobrado     = new RadioButton { Location = new Point(15, 25), AutoSize = true, Checked = true };
            _rbPagoFallido = new RadioButton { Location = new Point(15, 50), AutoSize = true };
            _grpDecision.Controls.AddRange(new Control[] { _rbCobrado, _rbPagoFallido });

            _lblModalidad = new Label { Location = new Point(15, 215), AutoSize = true };
            _cmbModalidad = new ComboBox { Location = new Point(120, 212), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbModalidad.Items.AddRange(new object[]
            {
                BE.Builders.ModalidadCobro.Mensual,
                BE.Builders.ModalidadCobro.Trimestral,
                BE.Builders.ModalidadCobro.Anual
            });
            _cmbModalidad.SelectedIndex = 0;
            // El cobro solo usa la modalidad cuando el resultado extiende la vigencia.
            _rbCobrado.CheckedChanged += (s, e) => _cmbModalidad.Enabled = _rbCobrado.Checked;

            _btnProcesar = new Button { Location = new Point(15, 250), Width = 150, Height = 32 };
            _btnProcesar.Click += BtnProcesar_Click;

            _lblResultado = new Label { Location = new Point(15, 290), AutoSize = false, Size = new Size(430, 55), ForeColor = Color.DarkGreen };

            this.Controls.AddRange(new Control[]
            {
                _lblTitulo, _lblCliente, _cmbCliente, _lblEstadoActual, _grpDecision,
                _lblModalidad, _cmbModalidad, _btnProcesar, _lblResultado
            });
        }

        private void CargarClientes()
        {
            _cmbCliente.Items.Clear();
            foreach (var c in _bllCliente.ObtenerTodos())
                _cmbCliente.Items.Add(new ClienteItem(c));
            if (_cmbCliente.Items.Count > 0) _cmbCliente.SelectedIndex = 0;
        }

        private void MostrarEstadoActual()
        {
            if (!(_cmbCliente.SelectedItem is ClienteItem item)) { _lblEstadoActual.Text = string.Empty; return; }
            var c = item.Cliente;
            string vencimiento = c.FechaVencimiento.HasValue ? c.FechaVencimiento.Value.ToString("dd/MM/yyyy") : "sin fecha";
            string estadoPago = c.EstaSuspendidoPorPago ? "SUSPENDIDO por falta de pago"
                               : c.EstaEnGracia          ? $"en gracia hasta {c.FechaLimiteGracia:dd/MM/yyyy}"
                                                          : "al día";
            _lblEstadoActual.Text = $"Plan: {c.NombrePlan ?? "sin plan"} — Vencimiento: {vencimiento}\nEstado de pago: {estadoPago}";
        }

        private void BtnProcesar_Click(object sender, EventArgs e)
        {
            _lblResultado.ForeColor = Color.DarkGreen;
            _lblResultado.Text = string.Empty;

            if (!(_cmbCliente.SelectedItem is ClienteItem item))
                return;

            var decision = _rbCobrado.Checked
                ? BLL.Manejadores.DecisionCobro.Cobrado
                : BLL.Manejadores.DecisionCobro.PagoFallido;

            var modalidad = (BE.Builders.ModalidadCobro)_cmbModalidad.SelectedItem;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                var actor = Seguridad.SessionManager.IsLoggedIn
                    ? Seguridad.SessionManager.GetInstance().Usuario.Username
                    : null;

                var resultado = _bllCobro.Procesar(this.Text, cliente, decision, modalidad, actor);

                _lblResultado.ForeColor = resultado.Estado == BE.EstadoCobro.Pendiente ? Color.DarkOrange
                                         : resultado.Estado == BE.EstadoCobro.Suspendido ? Color.DarkRed
                                         : resultado.Estado == BE.EstadoCobro.Gracia ? Color.DarkOrange
                                         : Color.DarkGreen;
                _lblResultado.Text = resultado.Mensaje;

                CargarClientes();
            }
            catch (BE.AppException ex)
            {
                var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                _lblResultado.ForeColor = Color.DarkRed;
                _lblResultado.Text = t.ContainsKey(ex.Clave) ? t[ex.Clave].Texto : ex.Message;
            }
            catch (Exception ex)
            {
                _lblResultado.ForeColor = Color.DarkRed;
                _lblResultado.Text = ex.Message;
            }
        }

        private sealed class ClienteItem
        {
            public BE.Cliente Cliente { get; }
            public ClienteItem(BE.Cliente c) => Cliente = c;
            public override string ToString() => $"{Cliente.NombreCompleto} (DNI {Cliente.DNI})";
        }
    }
}
