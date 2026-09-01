using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN02, CU01-VTA-Gestionar Suscripción (parte de Venta).
    /// El Vendedor elige cliente, plan y modalidad de cobro; el sistema deja la
    /// contratación pendiente de pago. La formalización de la suscripción ocurre
    /// recién cuando Caja confirma el pago (ver ContratacionesPendientesForm) — este
    /// formulario NO decide nada de eso, solo captura los datos e invoca a BLL.
    ///
    /// Accesible desde Menú → Ventas → Nueva Contratación (permiso mnuClientes).
    /// </summary>
    public partial class NuevaContratacionForm : FormBase
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IContratacionService contratacionBLL = new BLL.Contratacion();
        private readonly BLL.Interfaces.IClienteService clienteBLL = new BLL.Cliente();
        private readonly BLL.Interfaces.IPlanSuscripcionService planBLL = new BLL.PlanSuscripcion();

        private List<BE.Cliente> _clientes = new List<BE.Cliente>();
        private List<BE.PlanSuscripcion> _planes = new List<BE.PlanSuscripcion>();

        public int IdContratacionCreada { get; private set; }

        public NuevaContratacionForm()
        {
            InitializeComponent();
        }

        private void NuevaContratacionForm_Load(object sender, EventArgs e)
        {
            CargarDatosIniciales();
        }

        private void CargarDatosIniciales()
        {
            try
            {
                _clientes = clienteBLL.ObtenerTodos();
                cmbCliente.DataSource = _clientes;
                cmbCliente.DisplayMember = nameof(BE.Cliente.NombreCompleto);
                cmbCliente.ValueMember = nameof(BE.Cliente.IdCliente);

                _planes = planBLL.ObtenerActivos();
                cmbPlan.DataSource = _planes;
                cmbPlan.DisplayMember = nameof(BE.PlanSuscripcion.Nombre);
                cmbPlan.ValueMember = nameof(BE.PlanSuscripcion.IdPlan);

                cmbModalidad.DataSource = Enum.GetValues(typeof(BE.Builders.ModalidadCobro));
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            if (cmbCliente.SelectedItem == null || cmbPlan.SelectedItem == null)
            {
                MostrarError("Seleccioná un cliente y un plan para continuar.");
                return;
            }

            var cliente = (BE.Cliente)cmbCliente.SelectedItem;
            var plan = (BE.PlanSuscripcion)cmbPlan.SelectedItem;
            var modalidad = (BE.Builders.ModalidadCobro)cmbModalidad.SelectedItem;

            var confirmar = MessageBox.Show(
                $"¿Registrar la contratación del plan '{plan.Nombre}' ({modalidad}) para {cliente.NombreCompleto}?\n\n" +
                "Quedará pendiente de pago hasta que Caja confirme el cobro.",
                "Confirmar Contratación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                IdContratacionCreada = contratacionBLL.CrearContratacion(
                    this.Text, cliente.IdCliente, plan.IdPlan, modalidad);

                MostrarOk($"Contratación #{IdContratacionCreada} registrada, pendiente de pago.");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
