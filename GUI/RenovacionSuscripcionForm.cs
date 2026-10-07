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
            // Rango de la pausa (antes en el Designer, donde rompía el diseñador de VS).
            dtpPausaHasta.MinDate = DateTime.Today;
            dtpPausaHasta.MaxDate = DateTime.Today.AddMonths(BLL.Manejadores.PausarSuscripcionHandler.MaxMesesPausa);
            cmbModalidad.Items.AddRange(Enum.GetValues(typeof(BE.Builders.ModalidadCobro)).Cast<object>().ToArray());
            // Se muestra traducida ("Trimestral"); el ítem sigue siendo el enum.
            cmbModalidad.FormattingEnabled = true;
            cmbModalidad.Format += (s, e) =>
            {
                if (e.ListItem is BE.Builders.ModalidadCobro m) e.Value = Exportacion.DocumentosContratacion.Modalidad(m);
            };
            cmbModalidad.SelectedIndex = 0;
            Estilos.EstiloFormulario.BotonPrimario(btnProcesar);
            Estilos.EstiloFormulario.BotonSecundario(btnReanudar);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
            // Renovar o cambiar de plan sin cobro es solo del Administrador (lo exige BLL.Renovacion);
            // el resto renueva con una Nueva Contratación que cobra Caja.
            bool esAdmin = false;
            try { esAdmin = new BLL.Usuario().ObtenerUsuarioActivo()?.EsAdministrador == true; } catch { }
            if (!esAdmin)
            {
                rbRenovar.Enabled = false;
                rbCambiarPlan.Enabled = false;
                if (rbRenovar.Checked || rbCambiarPlan.Checked) rbPausar.Checked = true;
            }
            CargarClientes();
            CargarPlanes();
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

        // CheckedChanged se dispara dos veces por cambio (el radio que se desmarca y el que se
        // marca): solo se recarga con el que queda marcado.
        private void RbRenovar_CheckedChanged(object sender, EventArgs e)
        {
            if (rbRenovar.Checked) CargarClientes();
        }

        private void RbCambiarPlan_CheckedChanged(object sender, EventArgs e)
        {
            cmbPlanNuevo.Enabled = rbCambiarPlan.Checked;
            if (rbCambiarPlan.Checked) CargarClientes();
        }

        private void RbBaja_CheckedChanged(object sender, EventArgs e)
        {
            if (rbBaja.Checked) CargarClientes();
        }

        private void RbPausar_CheckedChanged(object sender, EventArgs e)
        {
            dtpPausaHasta.Enabled = rbPausar.Checked;
            if (rbPausar.Checked) CargarClientes();
        }

        // Decisión marcada en los radio buttons.
        private BLL.Manejadores.DecisionRenovacion DecisionMarcada() =>
              rbRenovar.Checked     ? BLL.Manejadores.DecisionRenovacion.Renovar
            : rbCambiarPlan.Checked ? BLL.Manejadores.DecisionRenovacion.CambiarPlan
            : rbPausar.Checked      ? BLL.Manejadores.DecisionRenovacion.Pausar
                                    : BLL.Manejadores.DecisionRenovacion.Baja;

        // Carga los clientes elegibles para la decisión marcada. Qué clientes son elegibles lo
        // decide la BLL (BLL.Renovacion.ObtenerElegibles). Conserva la selección si el cliente
        // sigue en la lista.
        private void CargarClientes()
        {
            try
            {
                MostrarElegibles(_bllRenovacion.ObtenerElegibles(DecisionMarcada()));
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void MostrarElegibles(System.Collections.Generic.List<BE.Cliente> elegibles)
        {
            int? idPrevio = (cmbCliente.SelectedItem as ClienteItem)?.Cliente.IdCliente;

            cmbCliente.Items.Clear();
            foreach (var c in elegibles)
                cmbCliente.Items.Add(new ClienteItem(c));

            var items = cmbCliente.Items.Cast<ClienteItem>().ToList();
            int idx = idPrevio.HasValue ? items.FindIndex(i => i.Cliente.IdCliente == idPrevio.Value) : -1;
            cmbCliente.SelectedIndex = idx >= 0 ? idx : (items.Count > 0 ? 0 : -1);
            btnProcesar.Enabled = items.Count > 0;
            MostrarEstadoActual();
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
            if (!(cmbCliente.SelectedItem is ClienteItem item))
            {
                lblEstadoActual.Text = cmbCliente.Items.Count == 0
                    ? Tr("renov.sinelegibles", "No hay clientes en condiciones de procesar esta decisión.")
                    : string.Empty;
                btnReanudar.Enabled = false;
                return;
            }
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

            string vencimiento = c.FechaVencimiento.HasValue ? c.FechaVencimiento.Value.ToString("d") : Tr("susc.sinfecha", "sin fecha");
            // El estado lo deriva BE.Cliente; acá solo se elige el texto.
            string estado;
            switch (c.ObtenerEstadoSuscripcion())
            {
                case BE.EstadoVencimiento.Pausada:        estado = Tr("renov.estado.pausada",   "PAUSADA");          break;
                case BE.EstadoVencimiento.Vencida:        estado = Tr("renov.estado.vencida",   "VENCIDA");          break;
                case BE.EstadoVencimiento.ProximaAVencer: estado = Tr("renov.estado.porvencer", "próxima a vencer"); break;
                default:                                  estado = Tr("renov.estado.vigente",   "vigente");          break;
            }
            lblEstadoActual.Text = Tr("renov.estado.resumen", "Plan actual: {0} — Vencimiento: {1} ({2})",
                new object[] { c.NombrePlan ?? Tr("susc.sinplan", "sin plan"), vencimiento, estado });
            if (c.EstaPausada)
                lblEstadoActual.Text += " — " + Tr("renov.estado.pausadahasta", "pausada hasta {0}",
                    new object[] { c.FechaPausaHasta.Value.ToString("d") });

            btnReanudar.Enabled = c.EstaPausada;
        }

        private void BtnProcesar_Click(object sender, EventArgs e)
        {
            lblResultado.ForeColor = Color.DarkGreen;
            lblResultado.Text = string.Empty;

            if (!(cmbCliente.SelectedItem is ClienteItem item))
                return;

            var decision = DecisionMarcada();

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
            var confirmar = (FormBase.MostrarConfirmacionSiNo(this,
                bodyConf,
                Tr("conf.renov.procesar.tit", "Confirmar Renovación"), porDefectoNo: esBaja) ? DialogResult.Yes : DialogResult.No);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                var actor = BLL.Sesion.Actor;

                var resultado = _bllRenovacion.Procesar(this.Text, cliente, decision, idPlanNuevo, modalidad, actor, fechaPausaHasta);

                lblResultado.ForeColor = resultado.Estado == BE.EstadoRenovacion.Pendiente ? Color.DarkOrange : Color.DarkGreen;
                lblResultado.Text = Tr(resultado.Clave, resultado.Mensaje, resultado.Args);

                // Recarga la lista completa (no solo el estado del cliente actual): la acción
                // recién procesada puede haber sacado a este cliente — o metido a otros — de la
                // lista de elegibles para la decisión marcada (ej. ya no está próximo a vencer
                // tras renovar). Sin esto, un segundo clic en "Procesar" sobre el mismo cliente
                // reproducía el mismo rechazo confuso que este filtro buscaba evitar.
                CargarClientes();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                // Si la falla ocurrió después de una escritura parcial, recargar igual: mejor
                // mostrar el estado real (aunque haya cambiado) que dejar la pantalla congelada
                // con datos de antes del intento. CargarClientes() ya contiene su propio
                // try/catch, así que un fallo del refresco no tapa el error ya mostrado arriba.
                CargarClientes();
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
                CargarClientes();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                CargarClientes();
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
