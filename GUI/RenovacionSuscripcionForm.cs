using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PdN5 — Renovación de suscripción. El Vendedor/Gerencia contacta al cliente por
    /// fuera del sistema (teléfono/WhatsApp/mail) y carga acá la decisión tomada; el
    /// patrón Chain of Responsibility (BLL.Manejadores) resuelve el resto.
    /// </summary>
    public partial class RenovacionSuscripcionForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService    _bllCliente    = new BLL.Cliente();
        private readonly BLL.Interfaces.IPlanSuscripcionService _bllPlan  = new BLL.PlanSuscripcion();
        private readonly BLL.Interfaces.IRenovacionService _bllRenovacion = new BLL.Renovacion();

        protected override Label MensajeLabel => lblResultado;

        public RenovacionSuscripcionForm()
        {
            InitializeComponent();
            cmbModalidad.Items.AddRange(Enum.GetValues(typeof(BE.Builders.ModalidadCobro)).Cast<object>().ToArray());
            cmbModalidad.SelectedIndex = 0;
            Estilos.EstiloFormulario.BotonPrimario(btnProcesar);
            Estilos.EstiloFormulario.BotonSecundario(btnReanudar);
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

        private void Traducir(Idioma idioma)
        {
            this.Text             = Tr("renov.titulo", "Renovación de Suscripción");
            lblTitulo.Text         = Tr("renov.titulo", "Renovación de Suscripción");
            lblCliente.Text        = Tr("renov.cliente", "Cliente:");
            grpDecision.Text       = Tr("renov.decision", "Decisión (tras contactar al cliente)");
            rbRenovar.Text         = Tr("renov.renovar", "Renovar mismo plan");
            rbCambiarPlan.Text     = Tr("renov.cambiarplan", "Cambiar de plan");
            rbBaja.Text            = Tr("renov.baja", "Dar de baja");
            rbPausar.Text          = Tr("renov.pausar", "Pausar hasta:");
            lblPlanNuevo.Text      = Tr("renov.plannuevo", "Plan nuevo:");
            lblModalidad.Text      = Tr("renov.modalidad", "Modalidad de cobro:");
            btnProcesar.Text       = Tr("renov.procesar", "Procesar");
            btnReanudar.Text       = Tr("renov.reanudar", "Reanudar ahora");
        }

        private void CmbCliente_SelectedIndexChanged(object sender, EventArgs e) => MostrarEstadoActual();

        private void RbCambiarPlan_CheckedChanged(object sender, EventArgs e) => cmbPlanNuevo.Enabled = rbCambiarPlan.Checked;

        private void RbPausar_CheckedChanged(object sender, EventArgs e) => dtpPausaHasta.Enabled = rbPausar.Checked;

        private void CargarClientes()
        {
            try
            {
                cmbCliente.Items.Clear();
                // Solo clientes con un plan asignado: sin plan no hay suscripción que renovar,
                // cambiar, pausar o dar de baja (mismo criterio que BLL.Renovacion.Procesar,
                // que rechaza a un cliente sin plan con err.bll.renovacion.sin_plan).
                foreach (var c in _bllCliente.ObtenerTodos().Where(c => c.TienePlan()))
                    cmbCliente.Items.Add(new ClienteItem(c));
                if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
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
                MostrarError(ex);
            }
        }

        // Lee el cliente seleccionado en el combo (posiblemente desactualizado: es el objeto
        // cacheado la última vez que se llamó CargarClientes()).
        private void MostrarEstadoActual()
        {
            if (!(cmbCliente.SelectedItem is ClienteItem item)) { lblEstadoActual.Text = string.Empty; return; }
            MostrarEstadoActual(item.Cliente);
        }

        // Muestra el estado de un cliente puntual. BtnProcesar_Click/BtnReanudar_Click le pasan el
        // objeto RECIÉN releído (y mutado en el lugar por la cadena de Manejadores) tras la acción,
        // en vez de que esta vuelva a leer cmbCliente.SelectedItem — que seguiría apuntando al
        // ClienteItem cacheado ANTES de la acción, mostrando el estado/vencimiento anterior aunque
        // la operación ya se haya aplicado y persistido correctamente.
        private void MostrarEstadoActual(BE.Cliente c)
        {
            // Un solo fetch del diccionario mergeado (BD + corpus) para las 5 sub-resoluciones,
            // en vez de que cada Tr(...) lo reconstruya desde cero (Traductor.ObtenerTraducciones).
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string Tr(string clave, string fallback, object[] args = null) => Traductor.Resolver(clave, fallback, args, t);

            string vencimiento = c.FechaVencimiento.HasValue ? c.FechaVencimiento.Value.ToString("dd/MM/yyyy") : Tr("susc.sinfecha", "sin fecha");
            string estado = c.EstaPausada ? Tr("renov.estado.pausada", "PAUSADA")
                           : c.VencimientoExpirado ? Tr("renov.estado.vencida", "VENCIDA")
                           : c.SuscripcionProximaAVencer() ? Tr("renov.estado.porvencer", "próxima a vencer")
                                                            : Tr("renov.estado.vigente", "vigente");
            lblEstadoActual.Text = Tr("renov.estado.resumen", "Plan actual: {0} — Vencimiento: {1} ({2})",
                new object[] { c.NombrePlan ?? Tr("susc.sinplan", "sin plan"), vencimiento, estado });
            if (c.EstaPausada)
                lblEstadoActual.Text += " — " + Tr("renov.estado.pausadahasta", "pausada hasta {0}",
                    new object[] { c.FechaPausaHasta.Value.ToString("dd/MM/yyyy") });

            btnReanudar.Enabled = c.EstaPausada;
        }

        private void BtnProcesar_Click(object sender, EventArgs e)
        {
            lblResultado.ForeColor = Color.DarkGreen;
            lblResultado.Text = string.Empty;

            if (!(cmbCliente.SelectedItem is ClienteItem item))
                return;

            var decision = rbRenovar.Checked      ? BLL.Manejadores.DecisionRenovacion.Renovar
                          : rbCambiarPlan.Checked  ? BLL.Manejadores.DecisionRenovacion.CambiarPlan
                          : rbPausar.Checked       ? BLL.Manejadores.DecisionRenovacion.Pausar
                                                     : BLL.Manejadores.DecisionRenovacion.Baja;

            int? idPlanNuevo = (decision == BLL.Manejadores.DecisionRenovacion.CambiarPlan
                                 && cmbPlanNuevo.SelectedItem is PlanItem planItem)
                ? planItem.Plan.IdPlan
                : (int?)null;

            DateTime? fechaPausaHasta = decision == BLL.Manejadores.DecisionRenovacion.Pausar
                ? dtpPausaHasta.Value.Date
                : (DateTime?)null;

            var modalidad = (BE.Builders.ModalidadCobro)cmbModalidad.SelectedItem;

            // Confirmación antes de procesar — antes se ejecutaba sin ninguna, incluida la opción
            // "Dar de baja" (irreversible desde esta pantalla), a diferencia del resto de las
            // pantallas de Suscripciones/Promociones que sí confirman acciones sensibles.
            bool esBaja = decision == BLL.Manejadores.DecisionRenovacion.Baja;
            string bodyConf = esBaja
                ? Tr("conf.renov.baja.msg", "¿Dar de baja la suscripción de {0}?\n\nEsta acción es irreversible.", new object[] { item.Cliente.NombreCompleto })
                : Tr("conf.renov.procesar.msg", "¿Procesar esta decisión para {0}?", new object[] { item.Cliente.NombreCompleto });
            var confirmar = MessageBox.Show(
                bodyConf,
                Tr("conf.renov.procesar.tit", "Confirmar Renovación"),
                MessageBoxButtons.YesNo,
                esBaja ? MessageBoxIcon.Warning : MessageBoxIcon.Question,
                esBaja ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                var actor = Seguridad.SessionManager.IsLoggedIn
                    ? Seguridad.SessionManager.GetInstance().Usuario.Username
                    : null;

                var resultado = _bllRenovacion.Procesar(this.Text, cliente, decision, idPlanNuevo, modalidad, actor, fechaPausaHasta);

                lblResultado.ForeColor = resultado.Estado == BE.EstadoRenovacion.Pendiente ? Color.DarkOrange : Color.DarkGreen;
                lblResultado.Text = Tr(resultado.Clave, resultado.Mensaje, resultado.Args);

                // `cliente` es el objeto recién releído y mutado por la cadena de Manejadores —
                // refleja el estado post-acción sin depender de cmbCliente.SelectedItem (stale).
                MostrarEstadoActual(cliente);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                // Si la falla ocurrió después de una escritura parcial, releer y refrescar el
                // estado igual: mejor mostrar el dato real (aunque haya cambiado) que dejar la
                // pantalla congelada con el estado de antes del intento. Best-effort: si el
                // refresco en sí falla, no debe tapar el error ya mostrado arriba.
                try { MostrarEstadoActual(_bllCliente.ObtenerPorId(item.Cliente.IdCliente)); } catch { }
            }
        }

        private void BtnReanudar_Click(object sender, EventArgs e)
        {
            lblResultado.ForeColor = Color.DarkGreen;
            lblResultado.Text = string.Empty;

            if (!(cmbCliente.SelectedItem is ClienteItem item))
                return;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                _bllCliente.ReanudarPausa(this.Text, cliente);
                lblResultado.Text = Tr("renov.msg.reanudada", "Suscripción reanudada.");
                MostrarEstadoActual(cliente);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                try { MostrarEstadoActual(_bllCliente.ObtenerPorId(item.Cliente.IdCliente)); } catch { }
            }
        }

        private sealed class PlanItem
        {
            public BE.PlanSuscripcion Plan { get; }
            public PlanItem(BE.PlanSuscripcion p) => Plan = p;
            public override string ToString() => Plan.Nombre;
        }
    }
}
