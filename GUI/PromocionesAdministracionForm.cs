using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, CU-ADM-Gestionar Promociones. Actor: Administración (rol
    /// AdministracionComercial). Consume las sugerencias de Gerencia, da de alta promociones
    /// (desde sugerencia o manual), las desactiva directamente si están Vigentes, y resuelve
    /// las solicitudes de baja que envía Ventas.
    /// </summary>
    public partial class PromocionesAdministracionForm : FormBase
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly BLL.Interfaces.ISugerenciaPromocionService sugerenciaBLL = new BLL.SugerenciaPromocion();

        private List<BE.SugerenciaPromocion> _sugerencias = new List<BE.SugerenciaPromocion>();
        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        public PromocionesAdministracionForm()
        {
            InitializeComponent();
        }

        private void PromocionesAdministracionForm_Load(object sender, EventArgs e)
        {
            CargarTodo();
        }

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarTodo();

        private void CargarTodo()
        {
            CargarSugerencias();
            CargarPromociones();
        }

        private void CargarSugerencias()
        {
            try
            {
                _sugerencias = sugerenciaBLL.ObtenerPendientes();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Motivo", typeof(string));
                tabla.Columns.Add("Tipo Sugerido", typeof(string));
                tabla.Columns.Add("Beneficio Est.", typeof(decimal));

                foreach (var s in _sugerencias)
                    tabla.Rows.Add(
                        s.IdSugerencia,
                        s.AplicaAPlan() ? $"Plan: {s.NombrePlan}" : $"Categoría: {s.CategoriaPrenda}",
                        s.Motivo, s.TipoDescuentoSugerido.ToString(), s.BeneficioEstimado);

                dgvSugerencias.DataSource = tabla;
                if (dgvSugerencias.Columns.Contains("ID")) dgvSugerencias.Columns["ID"].Width = 44;

                btnUsarSugerencia.Enabled = false;
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void CargarPromociones()
        {
            try
            {
                _promociones = promocionBLL.ObtenerTodas();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Nombre", typeof(string));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Estado", typeof(string));
                tabla.Columns.Add("Motivo Baja", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(
                        p.IdPromocion, p.Nombre,
                        p.AplicaAPlan() ? $"Plan: {p.NombrePlan}" : $"Categoría: {p.CategoriaPrenda}",
                        p.Estado.ToString(), p.MotivoBaja ?? "—");

                dgvPromociones.DataSource = tabla;
                if (dgvPromociones.Columns.Contains("ID")) dgvPromociones.Columns["ID"].Width = 44;

                lblConteo.Text = $"{_promociones.Count} promoción(es) en total.";
                DeshabilitarBotonesPromocion();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void DgvSugerencias_SelectionChanged(object sender, EventArgs e)
        {
            btnUsarSugerencia.Enabled = dgvSugerencias.SelectedRows.Count > 0;
        }

        private BE.SugerenciaPromocion ObtenerSugerenciaSeleccionada()
        {
            if (dgvSugerencias.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvSugerencias.SelectedRows[0].Cells["ID"].Value);
            return _sugerencias.Find(s => s.IdSugerencia == id);
        }

        private void BtnUsarSugerencia_Click(object sender, EventArgs e)
        {
            var sugerencia = ObtenerSugerenciaSeleccionada();
            if (sugerencia == null) return;
            AbrirAltaPromocion(sugerencia);
        }

        private void BtnNuevaManual_Click(object sender, EventArgs e) => AbrirAltaPromocion(null);

        private void AbrirAltaPromocion(BE.SugerenciaPromocion sugerencia)
        {
            using (var form = new AltaPromocionForm(sugerencia))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    MostrarOk($"Promoción #{form.IdPromocionCreada} registrada.");
                    CargarTodo();
                }
            }
        }

        private void DgvPromociones_SelectionChanged(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) { DeshabilitarBotonesPromocion(); return; }

            btnDesactivar.Enabled = promocion.PuedeDesactivarseDirecto();
            btnAprobarBaja.Enabled = promocion.PuedeResolverseBaja();
            btnRechazarBaja.Enabled = promocion.PuedeResolverseBaja();
        }

        private void DeshabilitarBotonesPromocion()
        {
            btnDesactivar.Enabled = false;
            btnAprobarBaja.Enabled = false;
            btnRechazarBaja.Enabled = false;
        }

        private BE.Promocion ObtenerPromocionSeleccionada()
        {
            if (dgvPromociones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPromociones.SelectedRows[0].Cells["ID"].Value);
            return _promociones.Find(p => p.IdPromocion == id);
        }

        private void BtnDesactivar_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;

            var confirmar = MessageBox.Show($"¿Desactivar la promoción '{promocion.Nombre}'?",
                "Confirmar Desactivación", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                promocionBLL.Desactivar(this.Text, promocion);
                MostrarOk($"Promoción '{promocion.Nombre}' desactivada.");
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnAprobarBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;

            var confirmar = MessageBox.Show(
                $"¿Aprobar la baja de '{promocion.Nombre}' sugerida por Ventas?\nMotivo: {promocion.MotivoBaja}",
                "Confirmar Baja", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                promocionBLL.AprobarBaja(this.Text, promocion);
                MostrarOk($"Promoción '{promocion.Nombre}' dada de baja.");
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnRechazarBaja_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerPromocionSeleccionada();
            if (promocion == null) return;

            string motivo;
            using (var dlg = new InputDialog("Rechazar Baja de Promoción",
                $"Motivo por el cual '{promocion.Nombre}' sigue vigente:", esPassword: false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText;
            }
            if (string.IsNullOrWhiteSpace(motivo)) return;

            try
            {
                promocionBLL.RechazarBaja(this.Text, promocion, motivo);
                MostrarOk($"Se rechazó la baja de '{promocion.Nombre}': sigue vigente.");
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
