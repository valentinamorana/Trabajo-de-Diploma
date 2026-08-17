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
    public partial class CobroSuscripcionForm : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService _bllCliente = new BLL.Cliente();
        private readonly BLL.Interfaces.ICobroService    _bllCobro  = new BLL.Cobro();

        public CobroSuscripcionForm()
        {
            InitializeComponent();
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

        private string T(string clave, string fallback, object[] args = null)
            => Traductor.Resolver(clave, fallback, args, GestorIdioma.IdiomaActual);

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string Tr(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text          = Tr("cobro.titulo", "Cobro de Suscripción");
            lblTitulo.Text     = Tr("cobro.titulo", "Cobro de Suscripción");
            lblCliente.Text    = Tr("cobro.cliente", "Cliente:");
            grpDecision.Text   = Tr("cobro.decision", "Resultado del cobro");
            rbCobrado.Text     = Tr("cobro.cobrado", "Cobrado");
            rbPagoFallido.Text = Tr("cobro.pagofallido", "Pago fallido");
            lblModalidad.Text  = Tr("renov.modalidad", "Modalidad de cobro:");
            btnProcesar.Text   = Tr("cobro.procesar", "Procesar");
        }

        private void CmbCliente_SelectedIndexChanged(object sender, EventArgs e) => MostrarEstadoActual();

        // El cobro solo usa la modalidad cuando el resultado extiende la vigencia.
        private void RbCobrado_CheckedChanged(object sender, EventArgs e) => cmbModalidad.Enabled = rbCobrado.Checked;

        private void CargarClientes()
        {
            cmbCliente.Items.Clear();
            foreach (var c in _bllCliente.ObtenerTodos())
                cmbCliente.Items.Add(new ClienteItem(c));
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
        }

        private void MostrarEstadoActual()
        {
            if (!(cmbCliente.SelectedItem is ClienteItem item)) { lblEstadoActual.Text = string.Empty; return; }
            var c = item.Cliente;

            // Un solo fetch del diccionario mergeado (BD + corpus) para las 5 sub-resoluciones,
            // en vez de que cada T(...) lo reconstruya desde cero (Traductor.ObtenerTraducciones).
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string Tr(string clave, string fallback, object[] args = null) => Traductor.Resolver(clave, fallback, args, t);

            string vencimiento = c.FechaVencimiento.HasValue ? c.FechaVencimiento.Value.ToString("dd/MM/yyyy") : Tr("susc.sinfecha", "sin fecha");
            string estadoPago = c.EstaSuspendidoPorPago ? Tr("cobro.estado.suspendido", "SUSPENDIDO por falta de pago")
                               : c.EstaEnGracia          ? Tr("cobro.estado.engracia", "en gracia hasta {0:dd/MM/yyyy}", new object[] { c.FechaLimiteGracia })
                                                          : Tr("cobro.estado.aldia", "al día");
            lblEstadoActual.Text = Tr("cobro.estado.resumen", "Plan: {0} — Vencimiento: {1}\nEstado de pago: {2}",
                new object[] { c.NombrePlan ?? Tr("susc.sinplan", "sin plan"), vencimiento, estadoPago });
        }

        private void BtnProcesar_Click(object sender, EventArgs e)
        {
            lblResultado.ForeColor = Color.DarkGreen;
            lblResultado.Text = string.Empty;

            if (!(cmbCliente.SelectedItem is ClienteItem item))
                return;

            var decision = rbCobrado.Checked
                ? BLL.Manejadores.DecisionCobro.Cobrado
                : BLL.Manejadores.DecisionCobro.PagoFallido;

            var modalidad = (BE.Builders.ModalidadCobro)cmbModalidad.SelectedItem;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                var actor = Seguridad.SessionManager.IsLoggedIn
                    ? Seguridad.SessionManager.GetInstance().Usuario.Username
                    : null;

                var resultado = _bllCobro.Procesar(this.Text, cliente, decision, modalidad, actor);

                lblResultado.ForeColor = resultado.Estado == BE.EstadoCobro.Pendiente ? Color.DarkOrange
                                         : resultado.Estado == BE.EstadoCobro.Suspendido ? Color.DarkRed
                                         : resultado.Estado == BE.EstadoCobro.Gracia ? Color.DarkOrange
                                         : Color.DarkGreen;
                lblResultado.Text = T(resultado.Clave, resultado.Mensaje, resultado.Args);

                CargarClientes();
            }
            catch (BE.AppException ex)
            {
                lblResultado.ForeColor = Color.DarkRed;
                lblResultado.Text = T(ex.Clave, ex.Message, ex.Args);
            }
            catch (Exception ex)
            {
                lblResultado.ForeColor = Color.DarkRed;
                lblResultado.Text = ex.Message;
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
