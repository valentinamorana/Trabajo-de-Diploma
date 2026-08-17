using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — Lista de Espera de prendas (mejora opcional, no requerida
    /// por la cátedra — ver README, sección "Módulos"). Permite ver y cancelar las
    /// anotaciones; el alta se hace desde <see cref="Prendas"/> (botón "Anotar en Lista
    /// de Espera" sobre una prenda EnUso) y la reserva se dispara sola al liberarse la
    /// prenda (BLL.Prenda.CambiarEstado).
    ///
    /// Accesible desde Menú → Inventario → Lista de Espera (permiso mnuListaEspera).
    /// </summary>
    public partial class ListaEsperaForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IListaEsperaService listaEsperaBLL = new BLL.ListaEspera();

        private List<BE.ListaEspera> _filas = new List<BE.ListaEspera>();
        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public ListaEsperaForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
            CargarFilas();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            AplicarFiltro();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(lblEstado, t);
            Aplicar(btnCancelar, t);
            Aplicar(btnRefrescar, t);
            RellenarComboEstado(idioma);
            TraducirHeadersGrilla();
        }

        private void RellenarComboEstado(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string T(string key, string fallback) => t.ContainsKey(key) ? t[key].Texto : fallback;

            int prevIdx = cmbFiltroEstado.SelectedIndex < 0 ? 0 : cmbFiltroEstado.SelectedIndex;
            cmbFiltroEstado.SelectedIndexChanged -= CmbFiltroEstado_SelectedIndexChanged;
            cmbFiltroEstado.Items.Clear();
            cmbFiltroEstado.Items.Add(T("combo.prenda.todos",      "Todos"));
            cmbFiltroEstado.Items.Add(T("listaespera.pendiente",   "Pendiente"));
            cmbFiltroEstado.Items.Add(T("listaespera.reservada",   "Reservada"));
            cmbFiltroEstado.Items.Add(T("listaespera.convertida",  "Convertida"));
            cmbFiltroEstado.Items.Add(T("listaespera.cancelada",   "Cancelada"));
            cmbFiltroEstado.SelectedIndex = prevIdx < cmbFiltroEstado.Items.Count ? prevIdx : 0;
            cmbFiltroEstado.SelectedIndexChanged += CmbFiltroEstado_SelectedIndexChanged;
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        // ── Eventos del Designer ──────────────────────────────────────────────

        private void ListaEsperaForm_Load(object sender, EventArgs e) { }

        private void CmbFiltroEstado_SelectedIndexChanged(object sender, EventArgs e) => AplicarFiltro();

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarFilas();

        // ── Carga y filtrado ──────────────────────────────────────────────────

        private void CargarFilas()
        {
            try
            {
                _filas = listaEsperaBLL.ObtenerActivas();
                AplicarFiltro();
                var t = Traductor.ObtenerTraducciones(_idioma);
                string fmt = t.ContainsKey("msg.listaespera.cargadas") ? t["msg.listaespera.cargadas"].Texto : "{0} anotación(es) activa(s).";
                MostrarOk(string.Format(fmt, _filas.Count));
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void AplicarFiltro()
        {
            int idx = cmbFiltroEstado.SelectedIndex;  // 0=Todos 1=Pendiente 2=Reservada 3=Convertida 4=Cancelada

            var lista = _filas.FindAll(f =>
                idx == 0
                || (idx == 1 && f.Estado == BE.EstadoListaEspera.Pendiente)
                || (idx == 2 && f.Estado == BE.EstadoListaEspera.Reservada)
                || (idx == 3 && f.Estado == BE.EstadoListaEspera.Convertida)
                || (idx == 4 && f.Estado == BE.EstadoListaEspera.Cancelada));

            var tabla = new DataTable();
            tabla.Columns.Add("ID",       typeof(int));
            tabla.Columns.Add("Prenda",   typeof(string));
            tabla.Columns.Add("Cliente",  typeof(string));
            tabla.Columns.Add("Alta",     typeof(string));
            tabla.Columns.Add("Estado",   typeof(string));
            tabla.Columns.Add("Vence",    typeof(string));
            tabla.Columns.Add("_EstadoKey", typeof(int));

            foreach (var f in lista)
                tabla.Rows.Add(
                    f.IdListaEspera, f.NombrePrenda, f.NombreCliente,
                    f.FechaAlta.ToString("dd/MM/yyyy HH:mm"),
                    EstadoLabel(f.Estado),
                    f.ReservaVigente ? f.FechaLimiteReserva.Value.ToString("dd/MM HH:mm") : "—",
                    (int)f.Estado);

            dgvListaEspera.DataSource = tabla;

            if (dgvListaEspera.Columns.Contains("_EstadoKey"))
                dgvListaEspera.Columns["_EstadoKey"].Visible = false;
            if (dgvListaEspera.Columns.Contains("ID"))
                dgvListaEspera.Columns["ID"].Width = 44;

            ColorearFilas();
            TraducirHeadersGrilla();

            var t = Traductor.ObtenerTraducciones(_idioma);
            string fmtM = t.ContainsKey("msg.prenda.conteo") ? t["msg.prenda.conteo"].Texto : "Mostrando {0} de {1}";
            lblConteo.Text = string.Format(fmtM, lista.Count, _filas.Count);

            btnCancelar.Enabled = false;
        }

        private void TraducirHeadersGrilla()
        {
            var t = Traductor.ObtenerTraducciones(_idioma);
            void RH(string col, string key, string fallback)
            {
                if (dgvListaEspera.Columns.Contains(col) && t.ContainsKey(key))
                    dgvListaEspera.Columns[col].HeaderText = t[key].Texto;
                else if (dgvListaEspera.Columns.Contains(col))
                    dgvListaEspera.Columns[col].HeaderText = fallback;
            }
            RH("Prenda",  "col.prenda.nombre",      "Prenda");
            RH("Cliente", "col.prenda.cliente",     "Cliente");
            RH("Alta",    "col.listaespera.alta",   "Anotado el");
            RH("Estado",  "col.prenda.estado",      "Estado");
            RH("Vence",   "col.listaespera.vence",  "Reserva vence");
        }

        private void ColorearFilas()
        {
            if (dgvListaEspera.DataSource == null) return;
            if (!dgvListaEspera.Columns.Contains("_EstadoKey")) return;
            foreach (DataGridViewRow row in dgvListaEspera.Rows)
            {
                if (!int.TryParse(row.Cells["_EstadoKey"].Value?.ToString(), out int key)) continue;
                row.DefaultCellStyle.ForeColor = key switch
                {
                    (int)BE.EstadoListaEspera.Pendiente  => Color.FromArgb(160, 100, 0),
                    (int)BE.EstadoListaEspera.Reservada  => Color.FromArgb(30, 130, 30),
                    (int)BE.EstadoListaEspera.Cancelada  => Color.FromArgb(150, 50, 50),
                    _                                     => Color.Black
                };
            }
        }

        private void DgvListaEspera_SelectionChanged(object sender, EventArgs e)
        {
            var fila = ObtenerFilaSeleccionada();
            btnCancelar.Enabled = fila != null &&
                (fila.Estado == BE.EstadoListaEspera.Pendiente || fila.Estado == BE.EstadoListaEspera.Reservada);
        }

        private BE.ListaEspera ObtenerFilaSeleccionada()
        {
            if (dgvListaEspera.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvListaEspera.SelectedRows[0].Cells["ID"].Value);
            return _filas.Find(f => f.IdListaEspera == id);
        }

        private string EstadoLabel(BE.EstadoListaEspera estado)
        {
            var t = Traductor.ObtenerTraducciones(_idioma);
            switch (estado)
            {
                case BE.EstadoListaEspera.Pendiente:  return t.ContainsKey("listaespera.pendiente")  ? t["listaespera.pendiente"].Texto  : "Pendiente";
                case BE.EstadoListaEspera.Reservada:  return t.ContainsKey("listaespera.reservada")  ? t["listaespera.reservada"].Texto  : "Reservada";
                case BE.EstadoListaEspera.Convertida: return t.ContainsKey("listaespera.convertida") ? t["listaespera.convertida"].Texto : "Convertida";
                case BE.EstadoListaEspera.Cancelada:  return t.ContainsKey("listaespera.cancelada")  ? t["listaespera.cancelada"].Texto  : "Cancelada";
                default: return estado.ToString();
            }
        }

        // ── Acciones ──────────────────────────────────────────────────────────

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            var fila = ObtenerFilaSeleccionada();
            if (fila == null) return;

            var t = Traductor.ObtenerTraducciones(_idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;
            string body = string.Format(
                T("conf.listaespera.cancelar.body", "¿Cancelar la anotación de {0} por '{1}'?"),
                fila.NombreCliente, fila.NombrePrenda);

            var confirmar = MessageBox.Show(body,
                T("conf.listaespera.cancelar.titulo", "Confirmar Cancelación"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                string actor = Seguridad.SessionManager.IsLoggedIn
                    ? Seguridad.SessionManager.GetInstance().Usuario.Username : null;
                listaEsperaBLL.Cancelar(this.Text, fila.IdListaEspera, actor);
                MostrarOk(string.Format(T("msg.listaespera.cancelada", "Anotación de {0} cancelada."), fila.NombreCliente));
                CargarFilas();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
