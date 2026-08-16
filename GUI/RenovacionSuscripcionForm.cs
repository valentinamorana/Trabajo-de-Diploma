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
    public class RenovacionSuscripcionForm : Form, IIdiomaObserver
    {
        private readonly BLL.Interfaces.IClienteService    _bllCliente    = new BLL.Cliente();
        private readonly BLL.Interfaces.IPlanSuscripcionService _bllPlan  = new BLL.PlanSuscripcion();
        private readonly BLL.Interfaces.IRenovacionService _bllRenovacion = new BLL.Renovacion();

        private Label      _lblTitulo;
        private Label      _lblCliente;
        private ComboBox   _cmbCliente;
        private Label      _lblEstadoActual;
        private GroupBox   _grpDecision;
        private RadioButton _rbRenovar;
        private RadioButton _rbCambiarPlan;
        private RadioButton _rbBaja;
        private Label      _lblPlanNuevo;
        private ComboBox   _cmbPlanNuevo;
        private Label      _lblModalidad;
        private ComboBox   _cmbModalidad;
        private Button     _btnProcesar;
        private Label      _lblResultado;

        public RenovacionSuscripcionForm()
        {
            this.Text            = "Renovación de Suscripción";
            this.Size            = new Size(480, 430);
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
            CargarPlanes();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private string T(string k, string fb)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(k) ? t[k].Texto : fb;
        }

        // Resuelve una clave de traducción con argumentos (mismo criterio que
        // FormBase.MostrarError(Exception) para AppException.Clave/Args): si la clave no
        // está en el corpus del idioma activo, cae al mensaje ya formateado en español.
        private string T(string clave, string fallback, object[] args)
        {
            if (string.IsNullOrEmpty(clave)) return fallback;
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            if (!t.ContainsKey(clave)) return fallback;
            string texto = t[clave].Texto;
            return (args != null && args.Length > 0) ? string.Format(texto, args) : texto;
        }

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string Tr(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text             = Tr("renov.titulo", "Renovación de Suscripción");
            _lblTitulo.Text        = Tr("renov.titulo", "Renovación de Suscripción");
            _lblCliente.Text       = Tr("renov.cliente", "Cliente:");
            _grpDecision.Text      = Tr("renov.decision", "Decisión (tras contactar al cliente)");
            _rbRenovar.Text        = Tr("renov.renovar", "Renovar mismo plan");
            _rbCambiarPlan.Text    = Tr("renov.cambiarplan", "Cambiar de plan");
            _rbBaja.Text           = Tr("renov.baja", "Dar de baja");
            _lblPlanNuevo.Text     = Tr("renov.plannuevo", "Plan nuevo:");
            _lblModalidad.Text     = Tr("renov.modalidad", "Modalidad de cobro:");
            _btnProcesar.Text      = Tr("renov.procesar", "Procesar");
        }

        private void ConstruirUI()
        {
            _lblTitulo = new Label { Location = new Point(15, 12), AutoSize = true, Font = new Font(Font, FontStyle.Bold) };

            _lblCliente = new Label { Location = new Point(15, 50), AutoSize = true };
            _cmbCliente = new ComboBox { Location = new Point(120, 47), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbCliente.SelectedIndexChanged += (s, e) => MostrarEstadoActual();

            _lblEstadoActual = new Label { Location = new Point(15, 80), AutoSize = true, ForeColor = Color.DimGray };

            _grpDecision = new GroupBox { Location = new Point(15, 110), Size = new Size(430, 110) };
            _rbRenovar     = new RadioButton { Location = new Point(15, 25), AutoSize = true, Checked = true };
            _rbCambiarPlan = new RadioButton { Location = new Point(15, 50), AutoSize = true };
            _rbBaja        = new RadioButton { Location = new Point(15, 75), AutoSize = true };
            _rbCambiarPlan.CheckedChanged += (s, e) => _cmbPlanNuevo.Enabled = _rbCambiarPlan.Checked;
            _grpDecision.Controls.AddRange(new Control[] { _rbRenovar, _rbCambiarPlan, _rbBaja });

            _lblPlanNuevo = new Label { Location = new Point(15, 230), AutoSize = true };
            _cmbPlanNuevo = new ComboBox { Location = new Point(120, 227), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };

            _lblModalidad = new Label { Location = new Point(15, 265), AutoSize = true };
            _cmbModalidad = new ComboBox { Location = new Point(120, 262), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbModalidad.Items.AddRange(new object[]
            {
                BE.Builders.ModalidadCobro.Mensual,
                BE.Builders.ModalidadCobro.Trimestral,
                BE.Builders.ModalidadCobro.Anual
            });
            _cmbModalidad.SelectedIndex = 0;

            _btnProcesar = new Button { Location = new Point(15, 305), Width = 150, Height = 32 };
            _btnProcesar.Click += BtnProcesar_Click;

            _lblResultado = new Label { Location = new Point(15, 345), AutoSize = false, Size = new Size(430, 60), ForeColor = Color.DarkGreen };

            this.Controls.AddRange(new Control[]
            {
                _lblTitulo, _lblCliente, _cmbCliente, _lblEstadoActual, _grpDecision,
                _lblPlanNuevo, _cmbPlanNuevo, _lblModalidad, _cmbModalidad, _btnProcesar, _lblResultado
            });
        }

        private void CargarClientes()
        {
            _cmbCliente.Items.Clear();
            foreach (var c in _bllCliente.ObtenerTodos())
                _cmbCliente.Items.Add(new ClienteItem(c));
            if (_cmbCliente.Items.Count > 0) _cmbCliente.SelectedIndex = 0;
        }

        private void CargarPlanes()
        {
            _cmbPlanNuevo.Items.Clear();
            foreach (var p in _bllPlan.ObtenerActivos())
                _cmbPlanNuevo.Items.Add(new PlanItem(p));
        }

        private void MostrarEstadoActual()
        {
            if (!(_cmbCliente.SelectedItem is ClienteItem item)) { _lblEstadoActual.Text = string.Empty; return; }
            var c = item.Cliente;
            string vencimiento = c.FechaVencimiento.HasValue ? c.FechaVencimiento.Value.ToString("dd/MM/yyyy") : T("susc.sinfecha", "sin fecha");
            string estado = c.VencimientoExpirado ? T("renov.estado.vencida", "VENCIDA")
                           : c.SuscripcionProximaAVencer() ? T("renov.estado.porvencer", "próxima a vencer")
                                                            : T("renov.estado.vigente", "vigente");
            _lblEstadoActual.Text = T("renov.estado.resumen", "Plan actual: {0} — Vencimiento: {1} ({2})",
                new object[] { c.NombrePlan ?? T("susc.sinplan", "sin plan"), vencimiento, estado });
        }

        private void BtnProcesar_Click(object sender, EventArgs e)
        {
            _lblResultado.ForeColor = Color.DarkGreen;
            _lblResultado.Text = string.Empty;

            if (!(_cmbCliente.SelectedItem is ClienteItem item))
                return;

            var decision = _rbRenovar.Checked      ? BLL.Manejadores.DecisionRenovacion.Renovar
                          : _rbCambiarPlan.Checked  ? BLL.Manejadores.DecisionRenovacion.CambiarPlan
                                                     : BLL.Manejadores.DecisionRenovacion.Baja;

            int? idPlanNuevo = (decision == BLL.Manejadores.DecisionRenovacion.CambiarPlan
                                 && _cmbPlanNuevo.SelectedItem is PlanItem planItem)
                ? planItem.Plan.IdPlan
                : (int?)null;

            var modalidad = (BE.Builders.ModalidadCobro)_cmbModalidad.SelectedItem;

            try
            {
                var cliente = _bllCliente.ObtenerPorId(item.Cliente.IdCliente);
                var actor = Seguridad.SessionManager.IsLoggedIn
                    ? Seguridad.SessionManager.GetInstance().Usuario.Username
                    : null;

                var resultado = _bllRenovacion.Procesar(this.Text, cliente, decision, idPlanNuevo, modalidad, actor);

                _lblResultado.ForeColor = resultado.Estado == BE.EstadoRenovacion.Pendiente ? Color.DarkOrange : Color.DarkGreen;
                _lblResultado.Text = T(resultado.Clave, resultado.Mensaje, resultado.Args);

                MostrarEstadoActual();
            }
            catch (BE.AppException ex)
            {
                _lblResultado.ForeColor = Color.DarkRed;
                _lblResultado.Text = T(ex.Clave, ex.Message, ex.Args);
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

        private sealed class PlanItem
        {
            public BE.PlanSuscripcion Plan { get; }
            public PlanItem(BE.PlanSuscripcion p) => Plan = p;
            public override string ToString() => Plan.Nombre;
        }
    }
}
