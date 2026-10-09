using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;
using Docs = GUI.Exportacion.DocumentosPromocion;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, carril Gerencia del diagrama de actividad (rol GerenteComercial):
    ///   Analizar métricas («Reporte de métricas», imprimible) → ¿Hay oportunidad?
    ///   No: fin "Sin promoción". Sí: Registrar sugerencia («Sugerencia de promoción»), con el
    ///   origen de la métrica (abandono, rotación) o Manual si Gerencia la carga sin el reporte.
    /// Lista las sugerencias registradas para volver a imprimirlas (o su constancia de descarte).
    /// </summary>
    public partial class SugerirPromocionForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.ISugerenciaPromocionService sugerenciaBLL = new BLL.SugerenciaPromocion();
        private readonly BLL.Interfaces.IPlanSuscripcionService planBLL = new BLL.PlanSuscripcion();
        private readonly BLL.AnalisisPromociones analisisBLL = new BLL.AnalisisPromociones();

        private List<BE.PlanSuscripcion> _planes = new List<BE.PlanSuscripcion>();
        private List<BE.SugerenciaPromocion> _sugerencias = new List<BE.SugerenciaPromocion>();

        // Origen de la sugerencia que se está armando: Manual hasta que se use una oportunidad del reporte.
        private BE.OrigenMetrica _origen = BE.OrigenMetrica.Manual;

        public SugerirPromocionForm()
        {
            InitializeComponent();
            // Mismo estilo de grilla que el resto de las pantallas.
            GUI.Estilos.EstiloFormulario.Grilla(dgvSugerencias);
            // Paleta centralizada (GUI/Tema.cs).
            btnEnviar.BackColor = Tema.Exito;
            btnAnalizar.BackColor = Tema.RosaPrimario;
            btnImprimirSugerencia.BackColor = Tema.RosaOscuro;
            lblOrigen.ForeColor = Tema.TextoMuted;
            lblSugerenciasTitulo.ForeColor = Tema.Tinta;
        }

        // ── Observer de idioma ────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarSugerencias();
        }

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            foreach (Control c in Controls)
                if (c.Tag != null && t.ContainsKey(c.Tag.ToString()))
                    c.Text = t[c.Tag.ToString()].Texto;
            MostrarOrigen();
            TraducirHeaders();
            CargarPeriodos();
        }

        // PN03 — período de "Analizar métricas" (conserva la elección al cambiar de idioma).
        private sealed class PeriodoItem
        {
            public int Dias { get; set; }
            public string Texto { get; set; }
            public override string ToString() => Texto;
        }

        private void CargarPeriodos()
        {
            int elegido = (cmbPeriodo.SelectedItem as PeriodoItem)?.Dias ?? BLL.AnalisisPromociones.DiasPeriodoPorDefecto;
            cmbPeriodo.Items.Clear();
            foreach (int dias in new[] { 30, 90, 180, 365 })
            {
                var item = new PeriodoItem { Dias = dias, Texto = Tr("promocion.periodo." + dias, dias == 365 ? "Último año" : $"Últimos {dias} días") };
                cmbPeriodo.Items.Add(item);
                if (dias == elegido) cmbPeriodo.SelectedItem = item;
            }
        }

        private void MostrarOrigen()
        {
            lblOrigen.Text = Tr("promocion.origenactual", "Origen de la sugerencia: {0}", new object[] { Docs.Origen(_origen) });
        }

        private void TraducirHeaders()
        {
            void RH(string col, string clave, string fb)
            {
                if (dgvSugerencias.Columns.Contains(col)) dgvSugerencias.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("ID", "col.promo.id", "ID");
            RH("Fecha", "col.promo.fecha", "Fecha");
            RH("Aplica a", "col.promo.aplicaa", "Aplica a");
            RH("Origen", "col.promo.origen", "Origen");
            RH("Beneficio Est.", "col.promo.beneficioest", "Beneficio Est.");
            RH("Estado", "col.promo.estado", "Estado");
        }

// Selecciona la categoría en el combo (lista cerrada del catálogo Categoria). Si es una
        // categoría que ya no está activa, se agrega para no cambiarla por otra.
        private void SeleccionarCategoria(string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria)) { cmbCategoria.SelectedIndex = -1; return; }
            int idx = cmbCategoria.FindStringExact(categoria.Trim());
            if (idx < 0) idx = cmbCategoria.Items.Add(categoria.Trim());
            cmbCategoria.SelectedIndex = idx;
        }

        private void SugerirPromocionForm_Load(object sender, EventArgs e)
        {
            try
            {
                _planes = planBLL.ObtenerActivos();
                cmbPlan.DataSource = _planes;
                cmbPlan.DisplayMember = nameof(BE.PlanSuscripcion.Nombre);
                cmbPlan.ValueMember = nameof(BE.PlanSuscripcion.IdPlan);
                cmbTipoDescuento.DataSource = Enum.GetValues(typeof(BE.TipoDescuento));
                // Categorías del catálogo (tabla Categoria): lista cerrada, la FK de la base no acepta otra.
                cmbCategoria.Items.Clear();
                foreach (string categoria in new BLL.Prenda().ObtenerCategorias()) cmbCategoria.Items.Add(categoria);
                // Se muestra traducido ("Monto fijo"); el ítem sigue siendo el enum.
                cmbTipoDescuento.FormattingEnabled = true;
                cmbTipoDescuento.Format -= FormatearTipo;
                cmbTipoDescuento.Format += FormatearTipo;
            }
            catch (Exception ex) { MostrarError(ex); }
            CargarSugerencias();
        }

        private void RbPlan_CheckedChanged(object sender, EventArgs e)
        {
            cmbPlan.Enabled = rbPlan.Checked;
            cmbCategoria.Enabled = !rbPlan.Checked;
        }

        // ── Analizar métricas → ¿Hay oportunidad? ────────────────────────────

        private void BtnAnalizar_Click(object sender, EventArgs e)
        {
            BE.ReporteMetricas reporte;
            // Período elegido (días hacia atrás desde hoy); por defecto, los últimos 90.
            int dias = (cmbPeriodo.SelectedItem as PeriodoItem)?.Dias ?? BLL.AnalisisPromociones.DiasPeriodoPorDefecto;
            try { reporte = analisisBLL.AnalizarMetricas(this.Text, DateTime.Today.AddDays(-dias), DateTime.Today); }
            catch (Exception ex) { MostrarError(ex); return; }

            var elegida = MostrarReporte(reporte, analisisBLL.HayOportunidad(reporte));
            if (!analisisBLL.HayOportunidad(reporte))
            {
                // ¿Hay oportunidad? No → fin "Sin promoción".
                MostrarOk(Tr("msg.sugerencia.sinanalisis",
                    "Los reportes de rotación y abandono no detectan casos para sugerir por ahora."));
                return;
            }
            if (elegida == null) return;

            // ¿Hay oportunidad? Sí → se precarga la sugerencia con el dato del reporte (editable).
            if (elegida.IdPlan.HasValue)
            {
                rbPlan.Checked = true;
                cmbPlan.SelectedValue = elegida.IdPlan.Value;
            }
            else
            {
                rbCategoria.Checked = true;
                SeleccionarCategoria(elegida.CategoriaPrenda);
            }
            cmbTipoDescuento.SelectedItem = elegida.TipoSugerido;
            numBeneficioEstimado.Value = Math.Min(numBeneficioEstimado.Maximum, elegida.BeneficioEstimado);
            txtMotivo.Text = elegida.ClaveMotivo != null
                ? Tr(elegida.ClaveMotivo, elegida.Motivo, elegida.ArgsMotivo)
                : elegida.Motivo;
            _origen = elegida.Origen;
            MostrarOrigen();
        }

        // «Reporte de métricas» en pantalla, con la lista de oportunidades para elegir y la opción de imprimirlo.
        private BE.CandidataSugerencia MostrarReporte(BE.ReporteMetricas reporte, bool hayOportunidad)
        {
            using (var dlg = new Form
            {
                Text = Tr("doc.promo.reporte.titulo", "Reporte de métricas"),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Tema.Papel,
                ClientSize = new System.Drawing.Size(640, 460)
            })
            {
                var texto = new TextBox
                {
                    Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                    BackColor = Tema.Papel, ForeColor = Tema.Tinta, Text = Docs.ReporteMetricas(reporte).TextoPlano
                };
                var lista = new ListBox { Dock = DockStyle.Bottom, Height = 120, FormattingEnabled = true, Visible = hayOportunidad };
                lista.Format += (s, ev) =>
                {
                    if (ev.ListItem is BE.CandidataSugerencia c)
                        ev.Value = $"[{Docs.Origen(c.Origen)}] {Docs.AplicaA(c.IdPlan, c.NombrePlan, c.CategoriaPrenda)} — {c.BeneficioEstimado:C2}";
                };
                foreach (var c in reporte.Oportunidades) lista.Items.Add(c);
                if (lista.Items.Count > 0) lista.SelectedIndex = 0;

                var usar = new Button
                {
                    Text = Tr("promocion.desdeanalisis.usar", "Usar esta idea"), DialogResult = DialogResult.OK,
                    Dock = DockStyle.Right, Width = 150, Visible = hayOportunidad,
                    FlatStyle = FlatStyle.Flat, BackColor = Tema.Exito, ForeColor = System.Drawing.Color.White
                };
                var imprimir = new Button
                {
                    Text = Tr("promocion.btn.imprimirreporte", "Imprimir reporte"), Dock = DockStyle.Left, Width = 150,
                    FlatStyle = FlatStyle.Flat, BackColor = Tema.RosaOscuro, ForeColor = System.Drawing.Color.White
                };
                imprimir.Click += (s, ev) =>
                {
                    try { Docs.Imprimir(Docs.ReporteMetricas(reporte), dlg); }
                    catch (Exception ex) { MostrarError(ex); }
                };
                var cerrar = new Button { Text = Tr("btn.cerrar", "Cerrar"), DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 100 };
                var panel = new Panel { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(4) };
                panel.Controls.Add(usar);
                panel.Controls.Add(cerrar);
                panel.Controls.Add(imprimir);

                dlg.Controls.Add(texto);
                dlg.Controls.Add(lista);
                dlg.Controls.Add(panel);
                dlg.AcceptButton = hayOportunidad ? usar : null;
                dlg.CancelButton = cerrar;

                return dlg.ShowDialog(this) == DialogResult.OK ? lista.SelectedItem as BE.CandidataSugerencia : null;
            }
        }

        // ── Registrar sugerencia ─────────────────────────────────────────────

        private void BtnEnviar_Click(object sender, EventArgs e)
        {
            int id;
            try
            {
                int? idPlan = rbPlan.Checked ? (int?)cmbPlan.SelectedValue : null;
                string categoria = rbPlan.Checked ? null : cmbCategoria.Text;
                var tipo = (BE.TipoDescuento)cmbTipoDescuento.SelectedItem;

                id = sugerenciaBLL.RegistrarSugerencia(this.Text, _origen, idPlan, categoria, txtMotivo.Text,
                                                      tipo, numBeneficioEstimado.Value);
                MostrarOk(Tr("msg.sugerencia.enviada", "Sugerencia #{0} enviada a Administración.", new object[] { id }));
                txtMotivo.Clear();
                _origen = BE.OrigenMetrica.Manual;
                MostrarOrigen();
                CargarSugerencias();
            }
            catch (Exception ex) { MostrarError(ex); }
            // La sugerencia se imprime con "Imprimir sugerencia".
        }

        // ── Sugerencias registradas ──────────────────────────────────────────

        private void CargarSugerencias()
        {
            try
            {
                _sugerencias = sugerenciaBLL.ObtenerTodas();
                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Fecha", typeof(string));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Origen", typeof(string));
                tabla.Columns.Add("Beneficio Est.", typeof(decimal));
                tabla.Columns.Add("Estado", typeof(string));
                foreach (var s in _sugerencias)
                    tabla.Rows.Add(s.IdSugerencia, s.FechaAlta.ToString("d"),
                        Docs.AplicaA(s.IdPlan, s.NombrePlan, s.CategoriaPrenda), Docs.Origen(s.OrigenMetrica),
                        s.BeneficioEstimado, Docs.EstadoSugerencia(s.Estado));
                dgvSugerencias.DataSource = tabla;
                if (dgvSugerencias.Columns.Contains("ID")) dgvSugerencias.Columns["ID"].Width = 44;
                TraducirHeaders();
                dgvSugerencias.ClearSelection();
                btnImprimirSugerencia.Enabled = false;
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.SugerenciaPromocion ObtenerSugerenciaSeleccionada()
        {
            if (dgvSugerencias.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvSugerencias.SelectedRows[0].Cells["ID"].Value);
            return _sugerencias.Find(s => s.IdSugerencia == id);
        }

        private void DgvSugerencias_SelectionChanged(object sender, EventArgs e)
            => btnImprimirSugerencia.Enabled = ObtenerSugerenciaSeleccionada() != null;

        private void BtnImprimirSugerencia_Click(object sender, EventArgs e)
        {
            var s = ObtenerSugerenciaSeleccionada();
            if (s != null) Imprimir(s);
        }

        // La sugerencia descartada se imprime como «Constancia de descarte»; las demás, como «Sugerencia de promoción».
        private void Imprimir(BE.SugerenciaPromocion s)
        {
            try { Docs.Imprimir(s.EstaDescartada() ? Docs.ConstanciaDescarteSugerencia(s) : Docs.Sugerencia(s), this); }
            catch (Exception ex) { MostrarError(ex); }
        }

        private static void FormatearTipo(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is BE.TipoDescuento t) e.Value = Exportacion.DocumentosPromocion.Tipo(t);
        }
    }
}
