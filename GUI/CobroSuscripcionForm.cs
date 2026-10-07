using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN6 — Cobro de suscripción. Caja intenta cobrar (fuera del sistema:
    /// efectivo/transferencia, sin pasarela) y carga acá el resultado; el patrón Chain
    /// of Responsibility (BLL.Manejadores) resuelve el resto: confirma la renovación,
    /// aplica un período de gracia, o suspende nuevos pedidos.
    /// </summary>
    public partial class CobroSuscripcionForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService _bllCliente = new BLL.Cliente();
        private readonly BLL.Interfaces.ICobroService    _bllCobro  = new BLL.Cobro();

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
            Traducir(GestorIdioma.IdiomaActual);
            CargarMediosPago();
            CargarClientes();
        }

        // N01 — medios de pago vigentes (el mismo catálogo que Caja usa en PN02).
        private void CargarMediosPago()
        {
            try
            {
                cmbMedioPago.Items.Clear();
                foreach (var m in _bllCobro.ObtenerMediosPago()) cmbMedioPago.Items.Add(m);
                cmbMedioPago.SelectedIndex = -1;   // que Caja lo elija: no se asume efectivo
            }
            catch (Exception ex) { MostrarError(ex); }
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
            lblMedioPago.Text  = Tr("contratacion.mediopago", "Medio de pago:");
            btnProcesar.Text   = Tr("cobro.procesar", "Procesar");
        }

        private void CmbCliente_SelectedIndexChanged(object sender, EventArgs e) => MostrarEstadoActual();

        // El cobro solo usa la modalidad cuando el resultado extiende la vigencia.
        // El medio de pago también: un pago fallido no se cobró con ningún medio.
        private void RbCobrado_CheckedChanged(object sender, EventArgs e) =>
            cmbModalidad.Enabled = cmbMedioPago.Enabled = rbCobrado.Checked;

        private void CargarClientes()
        {
            try
            {
                cmbCliente.Items.Clear();
                // Qué clientes son elegibles lo decide la BLL (BLL.Cobro.ObtenerElegibles).
                foreach (var c in _bllCobro.ObtenerElegibles())
                    cmbCliente.Items.Add(new ClienteItem(c));
                btnProcesar.Enabled = cmbCliente.Items.Count > 0;
                if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
                else lblEstadoActual.Text = Tr("cobro.sinelegibles", "No hay clientes con un cobro pendiente de procesar.");
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

            MostrarTotal();
        }

        // La modalidad cambia los meses que se cobran: se recalcula el total.
        private void CmbModalidad_SelectedIndexChanged(object sender, EventArgs e) => MostrarTotal();

        // "Total a cobrar" ANTES de procesar, con la misma cuenta que el cobro (BLL.Cobro.PrevisualizarCobro):
        // período con el único descuento del ciclo + cargos por daño/pérdida pendientes.
        private void MostrarTotal()
        {
            lblTotal.Text = string.Empty;
            if (!(cmbCliente.SelectedItem is ClienteItem item) || !(cmbModalidad.SelectedItem is BE.Builders.ModalidadCobro modalidad))
                return;
            try
            {
                var previa = _bllCobro.PrevisualizarCobro(item.Cliente.IdCliente, modalidad);
                string detalle = string.Format(Tr("cobro.total.periodo", "período {0:C2}"), previa.Bruto);
                if (previa.Descuento > 0)
                    detalle += " − " + string.Format(Tr("cobro.total.descuento", "descuento {0:C2} ({1})"), previa.Descuento,
                        previa.NombrePromocion ?? Tr("lbl.contratacion.creditoreferido", "crédito por referido"));
                if (previa.TieneCargosPendientes)
                    detalle += " + " + string.Format(Tr("cobro.total.cargos", "{0} cargo(s) por daño/pérdida {1:C2}"),
                        previa.CantidadCargosPendientes, previa.TotalCargosPendientes);
                lblTotal.Text = string.Format(Tr("cobro.total", "Total a cobrar: {0:C2}\n({1})"), previa.Total, detalle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"[CobroSuscripcionForm] No se pudo calcular el total: {ex.Message}");
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
            int? idMedioPago = (cmbMedioPago.SelectedItem as BE.MedioPago)?.IdMedioPago;
            if (decision == BLL.Manejadores.DecisionCobro.Cobrado && !idMedioPago.HasValue)
            {
                MostrarError(Tr("err.bll.cobro.medio_requerido", "Indicá el medio de pago con el que abonó el cliente."));
                return;
            }

            // Confirmación antes de procesar — antes se ejecutaba sin ninguna, en la pantalla que
            // efectivamente mueve dinero (mismo criterio que ya aplican Planes/Promociones/
            // Contrataciones para acciones sensibles).
            string pregunta = Tr("conf.cobro.procesar.msg", "¿Procesar este cobro para {0}?", new object[] { item.Cliente.NombreCompleto });
            if (decision == BLL.Manejadores.DecisionCobro.Cobrado && !string.IsNullOrEmpty(lblTotal.Text))
                pregunta += "\n\n" + lblTotal.Text;   // el total que se va a cobrar
            var confirmar = MessageBox.Show(
                pregunta,
                Tr("conf.cobro.procesar.tit", "Confirmar Cobro"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                var actor = BLL.Sesion.Actor;

                var resultado = _bllCobro.Procesar(this.Text, cliente, decision, modalidad, actor, idMedioPago);

                lblResultado.ForeColor = resultado.Estado == BE.EstadoCobro.Pendiente ? Color.DarkOrange
                                         : resultado.Estado == BE.EstadoCobro.Suspendido ? Color.DarkRed
                                         : resultado.Estado == BE.EstadoCobro.Gracia ? Color.DarkOrange
                                         : Color.DarkGreen;
                lblResultado.Text = Tr(resultado.Clave, resultado.Mensaje, resultado.Args);
                if (resultado.NumeroComprobante != null)
                    lblResultado.Text += " " + Tr("cobro.comprobante", "Comprobante {0}.", new object[] { resultado.NumeroComprobante });

                CargarClientes();

                // Igual que en Caja (PN02): la única impresión que se ofrece es el comprobante para el cliente.
                if (resultado.Estado == BE.EstadoCobro.Cobrado && resultado.IdCobro > 0 &&
                    ConfirmarSiNo(Tr("conf.contr.imprimircomprobante", "¿Imprimir el comprobante para el cliente?"), this.Text, porDefectoNo: true))
                {
                    try
                    {
                        Exportacion.DocumentosContratacion.Imprimir(
                            Exportacion.DocumentosContratacion.ComprobanteCobro(_bllCobro.ObtenerCobro(resultado.IdCobro), cliente), this);
                    }
                    catch (Exception ex) { MostrarError(ex); }
                }
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
