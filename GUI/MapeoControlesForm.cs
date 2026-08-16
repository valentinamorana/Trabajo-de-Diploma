using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Etapa 4.3 — Pantalla de MAPEO de controles a patentes. El admin elige una patente y un
    /// formulario, y asocia/desasocia los controles (botones, ítems de menú) que esa patente
    /// gobierna. Los formularios y controles disponibles provienen del registro en runtime (C1):
    /// aparecen los que se abrieron al menos una vez en esta ejecución.
    /// </summary>
    public partial class MapeoControlesForm : Form
    {
        private readonly BLL.ControlMapeado _bll     = new BLL.ControlMapeado();
        private readonly BLL.Familia        _famBLL  = new BLL.Familia();

        // Conjunto COMPLETO de mapeos de la patente seleccionada (todos los forms): se edita el
        // subconjunto del form actual y al guardar se persiste el conjunto completo (preserva otros forms).
        private List<BE.ControlMapeado> _mapeosPatente = new List<BE.ControlMapeado>();
        // Todos los mapeos del sistema, para no ofrecer un control ya tomado por OTRA patente
        // (la tabla tiene UNIQUE(Formulario, NombreControl): un control pertenece a una sola patente).
        private List<BE.ControlMapeado> _todosLosMapeos = new List<BE.ControlMapeado>();

        private int    PatenteId   => (cmbPatente.SelectedItem as ItemPatente)?.Id ?? 0;
        private string FormSel     => cmbFormulario.SelectedItem as string;

        private class ItemPatente { public int Id; public string Nombre; public override string ToString() => Nombre; }

        public MapeoControlesForm()
        {
            InitializeComponent();
        }

        private string T(string k, string fb)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(k) ? t[k].Texto : fb;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new Icon(ico); } catch { }
            try
            {
                _todosLosMapeos = _bll.ObtenerTodos();

                cmbPatente.Items.Clear();
                foreach (var p in _famBLL.ObtenerPatentesDisponibles().OrderBy(p => p.Nombre))
                    cmbPatente.Items.Add(new ItemPatente { Id = p.Id, Nombre = p.Nombre });

                cmbFormulario.Items.Clear();
                foreach (string f in RegistroControles.Formularios())
                    if (!string.Equals(f, "Menu", StringComparison.OrdinalIgnoreCase))
                        cmbFormulario.Items.Add(f);   // el Menú se gobierna por NombreMenu, no por mapeo de control

                if (cmbFormulario.Items.Count == 0)
                    lblEstado.Text = T("map.sinforms", "Abrí los formularios que quieras mapear y volvé a entrar acá.");

                if (cmbPatente.Items.Count    > 0) cmbPatente.SelectedIndex    = 0;
                if (cmbFormulario.Items.Count > 0) cmbFormulario.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, T("msg.error.titulo", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CmbPatente_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarMapeosPatente();
            RefrescarListas();
        }

        private void CmbFormulario_SelectedIndexChanged(object sender, EventArgs e) => RefrescarListas();

        private void BtnAgregar_Click(object sender, EventArgs e) => Asociar();

        private void BtnQuitar_Click(object sender, EventArgs e) => Quitar();

        private void BtnGuardar_Click(object sender, EventArgs e) => Guardar();

        private void BtnCerrar_Click(object sender, EventArgs e) => this.Close();

        private void CargarMapeosPatente()
        {
            _mapeosPatente = PatenteId == 0 ? new List<BE.ControlMapeado>() : _bll.ObtenerPorPermiso(PatenteId);
        }

        private void RefrescarListas()
        {
            lstDisponibles.Items.Clear();
            lstAsociados.Items.Clear();
            if (PatenteId == 0 || FormSel == null) return;

            var controles = RegistroControles.Controles(FormSel);

            // Asociados a ESTA patente en ESTE form.
            var asociadosNombres = _mapeosPatente
                .Where(m => string.Equals(m.Formulario, FormSel, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.NombreControl).ToList();

            // Controles tomados por OTRA patente (no se pueden re-asignar: UNIQUE por control).
            var tomadosPorOtra = new HashSet<string>(
                _todosLosMapeos
                    .Where(m => string.Equals(m.Formulario, FormSel, StringComparison.OrdinalIgnoreCase)
                                && m.IdPermiso != PatenteId)
                    .Select(m => m.NombreControl),
                StringComparer.OrdinalIgnoreCase);

            foreach (var c in controles)
            {
                if (asociadosNombres.Contains(c.Nombre, StringComparer.OrdinalIgnoreCase))
                    lstAsociados.Items.Add(c);
                else if (!tomadosPorOtra.Contains(c.Nombre))
                    lstDisponibles.Items.Add(c);
            }
        }

        private void Asociar()
        {
            if (PatenteId == 0 || FormSel == null) return;
            if (!(lstDisponibles.SelectedItem is RegistroControles.ControlInfo ci)) return;
            _mapeosPatente.Add(new BE.ControlMapeado { IdPermiso = PatenteId, Formulario = FormSel, NombreControl = ci.Nombre });
            RefrescarListas();
        }

        private void Quitar()
        {
            if (!(lstAsociados.SelectedItem is RegistroControles.ControlInfo ci)) return;
            _mapeosPatente.RemoveAll(m => string.Equals(m.Formulario, FormSel, StringComparison.OrdinalIgnoreCase)
                                       && string.Equals(m.NombreControl, ci.Nombre, StringComparison.OrdinalIgnoreCase));
            RefrescarListas();
        }

        private void Guardar()
        {
            if (PatenteId == 0) return;
            try
            {
                _bll.GuardarAsociados(PatenteId, _mapeosPatente);
                _todosLosMapeos = _bll.ObtenerTodos();
                ManejadorSeguridad.InvalidarCache();   // cambiaron los mapeos → refrescar la caché
                // Re-aplica en vivo a los forms abiertos.
                if (Seguridad.SessionManager.IsLoggedIn)
                    ManejadorSeguridad.ActualizarSeguridadFormulariosAbiertos(Seguridad.SessionManager.GetInstance().Usuario);
                lblEstado.ForeColor = Color.FromArgb(40, 140, 60);
                lblEstado.Text = "✓ " + T("map.guardado", "Mapeo guardado.");
            }
            catch (Exception ex)
            {
                lblEstado.ForeColor = Color.FromArgb(180, 50, 50);
                lblEstado.Text = "✗ " + ex.Message;
            }
        }
    }
}
