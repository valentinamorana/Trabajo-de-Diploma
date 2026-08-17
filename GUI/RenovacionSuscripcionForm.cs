using System;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN5 — Renovación de suscripción. El Vendedor/Supervisor contacta al cliente por
    /// fuera del sistema (teléfono/WhatsApp/mail) y carga acá la decisión tomada; el
    /// patrón Chain of Responsibility (BLL.Manejadores) resuelve el resto.
    /// </summary>
    public partial class RenovacionSuscripcionForm : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService    _bllCliente    = new BLL.Cliente();
        private readonly BLL.Interfaces.IPlanSuscripcionService _bllPlan  = new BLL.PlanSuscripcion();
        private readonly BLL.Interfaces.IRenovacionService _bllRenovacion = new BLL.Renovacion();

        public RenovacionSuscripcionForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
            CargarClientes();
            CargarPlanes();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private string T(string k, string fb) => T(k, fb, null);

        private string T(string clave, string fallback, object[] args)
            => Traductor.Resolver(clave, fallback, args, GestorIdioma.IdiomaActual);

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string Tr(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text             = Tr("renov.titulo", "Renovación de Suscripción");
            lblTitulo.Text         = Tr("renov.titulo", "Renovación de Suscripción");
            lblCliente.Text        = Tr("renov.cliente", "Cliente:");
            grpDecision.Text       = Tr("renov.decision", "Decisión (tras contactar al cliente)");
            rbRenovar.Text         = Tr("renov.renovar", "Renovar mismo plan");
            rbCambiarPlan.Text     = Tr("renov.cambiarplan", "Cambiar de plan");
            rbBaja.Text            = Tr("renov.baja", "Dar de baja");
            lblPlanNuevo.Text      = Tr("renov.plannuevo", "Plan nuevo:");
            lblModalidad.Text      = Tr("renov.modalidad", "Modalidad de cobro:");
            btnProcesar.Text       = Tr("renov.procesar", "Procesar");
        }

        private void CmbCliente_SelectedIndexChanged(object sender, EventArgs e) => MostrarEstadoActual();

        private void RbCambiarPlan_CheckedChanged(object sender, EventArgs e) => cmbPlanNuevo.Enabled = rbCambiarPlan.Checked;

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
                lblResultado.ForeColor = Color.DarkRed;
                lblResultado.Text = ex.Message;
            }
        }

        private void CargarPlanes()
        {
            try
            {
                cmbPlanNuevo.Items.Clear();
                foreach (var p in _bllPlan.ObtenerActivos())
                    cmbPlanNuevo.Items.Add(new PlanItem(p));
            }
            catch (Exception ex)
            {
                lblResultado.ForeColor = Color.DarkRed;
                lblResultado.Text = ex.Message;
            }
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
            string estado = c.VencimientoExpirado ? Tr("renov.estado.vencida", "VENCIDA")
                           : c.SuscripcionProximaAVencer() ? Tr("renov.estado.porvencer", "próxima a vencer")
                                                            : Tr("renov.estado.vigente", "vigente");
            lblEstadoActual.Text = Tr("renov.estado.resumen", "Plan actual: {0} — Vencimiento: {1} ({2})",
                new object[] { c.NombrePlan ?? Tr("susc.sinplan", "sin plan"), vencimiento, estado });
        }

        private void BtnProcesar_Click(object sender, EventArgs e)
        {
            lblResultado.ForeColor = Color.DarkGreen;
            lblResultado.Text = string.Empty;

            if (!(cmbCliente.SelectedItem is ClienteItem item))
                return;

            var decision = rbRenovar.Checked      ? BLL.Manejadores.DecisionRenovacion.Renovar
                          : rbCambiarPlan.Checked  ? BLL.Manejadores.DecisionRenovacion.CambiarPlan
                                                     : BLL.Manejadores.DecisionRenovacion.Baja;

            int? idPlanNuevo = (decision == BLL.Manejadores.DecisionRenovacion.CambiarPlan
                                 && cmbPlanNuevo.SelectedItem is PlanItem planItem)
                ? planItem.Plan.IdPlan
                : (int?)null;

            var modalidad = (BE.Builders.ModalidadCobro)cmbModalidad.SelectedItem;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                var actor = Seguridad.SessionManager.IsLoggedIn
                    ? Seguridad.SessionManager.GetInstance().Usuario.Username
                    : null;

                var resultado = _bllRenovacion.Procesar(this.Text, cliente, decision, idPlanNuevo, modalidad, actor);

                lblResultado.ForeColor = resultado.Estado == BE.EstadoRenovacion.Pendiente ? Color.DarkOrange : Color.DarkGreen;
                lblResultado.Text = T(resultado.Clave, resultado.Mensaje, resultado.Args);

                MostrarEstadoActual();
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

        private sealed class PlanItem
        {
            public BE.PlanSuscripcion Plan { get; }
            public PlanItem(BE.PlanSuscripcion p) => Plan = p;
            public override string ToString() => Plan.Nombre;
        }
    }
}
