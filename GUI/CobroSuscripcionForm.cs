using System;
using System.Linq;
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
    public partial class CobroSuscripcionForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService _bllCliente = new BLL.Cliente();
        private readonly BLL.Interfaces.ICobroService    _bllCobro  = new BLL.Cobro();
        private readonly BLL.Interfaces.ICargoPrendaService _bllCargoPrenda = new BLL.CargoPrenda();

        protected override Label MensajeLabel => lblResultado;

        public CobroSuscripcionForm()
        {
            InitializeComponent();
            cmbModalidad.Items.AddRange(Enum.GetValues(typeof(BE.Builders.ModalidadCobro)).Cast<object>().ToArray());
            cmbModalidad.SelectedIndex = 0;
            Estilos.EstiloFormulario.BotonPrimario(btnProcesar);
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
            this.Text          = Tr("cobro.titulo", "Cobro de Suscripción");
            lblTitulo.Text     = Tr("cobro.titulo", "Cobro de Suscripción");
            lblCliente.Text    = Tr("cobro.cliente", "Cliente:");
            grpDecision.Text   = Tr("cobro.decision", "Resultado del cobro");
            rbCobrado.Text     = Tr("cobro.cobrado", "Cobrado");
            rbPagoFallido.Text = Tr("cobro.pagofallido", "Pago fallido");
            lblModalidad.Text  = Tr("cobro.modalidad", "Modalidad de cobro:");
            btnProcesar.Text   = Tr("cobro.procesar", "Procesar");
        }

        private void CmbCliente_SelectedIndexChanged(object sender, EventArgs e) => MostrarEstadoActual();

        // El cobro solo usa la modalidad cuando el resultado extiende la vigencia.
        private void RbCobrado_CheckedChanged(object sender, EventArgs e) => cmbModalidad.Enabled = rbCobrado.Checked;

        private void CargarClientes()
        {
            try
            {
                cmbCliente.Items.Clear();
                foreach (var c in _bllCliente.ObtenerTodos())
                    cmbCliente.Items.Add(new ClienteItem(c));
                if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void MostrarEstadoActual()
        {
            if (!(cmbCliente.SelectedItem is ClienteItem item)) { lblEstadoActual.Text = string.Empty; return; }
            var c = item.Cliente;

            // Un solo fetch del diccionario mergeado (BD + corpus) para las 5 sub-resoluciones,
            // en vez de que cada Tr(...) lo reconstruya desde cero (Traductor.ObtenerTraducciones).
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string Tr(string clave, string fallback, object[] args = null) => Traductor.Resolver(clave, fallback, args, t);

            string vencimiento = c.FechaVencimiento.HasValue ? c.FechaVencimiento.Value.ToString("dd/MM/yyyy") : Tr("susc.sinfecha", "sin fecha");
            string estadoPago = c.EstaSuspendidoPorPago ? Tr("cobro.estado.suspendido", "SUSPENDIDO por falta de pago")
                               : c.EstaEnGracia          ? Tr("cobro.estado.engracia", "en gracia hasta {0:dd/MM/yyyy}", new object[] { c.FechaLimiteGracia })
                                                          : Tr("cobro.estado.aldia", "al día");
            lblEstadoActual.Text = Tr("cobro.estado.resumen", "Plan: {0} — Vencimiento: {1}\nEstado de pago: {2}",
                new object[] { c.NombrePlan ?? Tr("susc.sinplan", "sin plan"), vencimiento, estadoPago });

            // Bloque 1 — anticipar en pantalla lo que este cobro va a sumar/restar, antes de
            // procesarlo: descuento por referido pendiente y cargos por daño/pérdida pendientes.
            if (c.TieneDescuentoPendiente)
                lblEstadoActual.Text += "\n" + Tr("cobro.estado.descuentopendiente",
                    "Tiene un descuento de ${0} pendiente para este cobro (programa de referidos).",
                    new object[] { c.DescuentoProximoCobro });

            try
            {
                var cargosPendientes = _bllCargoPrenda.ObtenerPendientesPorCliente(c.IdCliente);
                if (cargosPendientes.Count > 0)
                {
                    decimal total = 0;
                    foreach (var cargo in cargosPendientes) total += cargo.Monto;
                    lblEstadoActual.Text += "\n" + Tr("cobro.estado.cargospendientes",
                        "Tiene {0} cargo(s) por daño/pérdida pendiente(s) por ${1} que se sumarán a este cobro.",
                        new object[] { cargosPendientes.Count, total });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"[CobroSuscripcionForm] No se pudieron cargar los cargos pendientes: {ex.Message}");
            }
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

            // Confirmación antes de procesar — antes se ejecutaba sin ninguna, en la pantalla que
            // efectivamente mueve dinero (mismo criterio que ya aplican Planes/Promociones/
            // Contrataciones para acciones sensibles).
            var confirmar = MessageBox.Show(
                Tr("conf.cobro.procesar.msg", "¿Procesar este cobro para {0}?", new object[] { item.Cliente.NombreCompleto }),
                Tr("conf.cobro.procesar.tit", "Confirmar Cobro"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

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
                lblResultado.Text = Tr(resultado.Clave, resultado.Mensaje, resultado.Args);

                CargarClientes();
            }
            catch (Exception ex)
            {
                // Si la falla ocurrió después de una escritura parcial, refrescar igual: mejor
                // mostrar el estado real (aunque haya cambiado) que dejar la pantalla congelada
                // con datos de antes del intento.
                MostrarError(ex);
                CargarClientes();
            }
        }
    }
}
