using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Historial de cambios de un Pedido — T06b.
    ///
    /// Muestra todos los eventos registrados sobre el pedido (quién, cuándo, qué campo,
    /// valor anterior y valor nuevo). Permite filtrar por fecha y por tipo de acción,
    /// y restaurar el pedido al estado previo a cualquier operación registrada.
    ///
    /// Accesible desde PedidosVenta y PedidosRealizados mediante un botón "Historial".
    /// </summary>
    public partial class PedidoHistorialForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPedidoService _pedidoBLL = new BLL.Pedido();
        private readonly int _idPedido;

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PedidoHistorialForm(int idPedido)
        {
            InitializeComponent();

            _idPedido = idPedido;
            lblPedidoInfo.Text = $"Pedido #{_idPedido}";
            dtpDesde.Value = DateTime.Today.AddMonths(-1);
            dtpHasta.Value = DateTime.Today;
        }

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(_idioma);
            Buscar();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        // ── IIdiomaObserver ───────────────────────────────────────────────────

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
        }

        // ── Traducción ────────────────────────────────────────────────────────

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;   // sincronizar antes de llamar a TraducirHeadersGrilla()
            this.Text           = Tr("frm.historial",      "Historial de Cambios — Pedido");
            grpFiltros.Text     = Tr("lbl.hist.filtros",   "Filtros");
            lblDesde.Text       = Tr("lbl.hist.desde",     "Desde:");
            lblHasta.Text       = Tr("lbl.hist.hasta",     "Hasta:");
            lblAccion.Text      = Tr("lbl.hist.accion",    "Acción:");
            btnBuscar.Text      = Tr("btn.hist.buscar",    "Buscar");
            btnRestaurar.Text   = Tr("btn.hist.restaurar", "⟲ Restaurar");
            btnCerrar.Text      = Tr("btn.hist.cerrar",    "Cerrar");
            lblPedidoInfo.Text  = Tr("lbl.hist.pedido",    "Pedido #") + _idPedido;

            RellenarComboAcciones();
            TraducirHeadersGrilla();
        }

        private void RellenarComboAcciones()
        {
            // Opciones fijas de acción — incluye "Todas" como opción vacía
            var items = new List<ComboItem>
            {
                new ComboItem("",           Tr("combo.hist.todas",      "— Todas —")),
                new ComboItem("CREAR",      Tr("accion.crear",          "Crear")),
                new ComboItem("DESPACHAR",  Tr("accion.despachar",      "Despachar")),
                new ComboItem("ENTREGAR",   Tr("accion.entregar",       "Entregar")),
                new ComboItem("CANCELAR",   Tr("accion.cancelar",       "Cancelar")),
                new ComboItem("DESCANCELAR",Tr("accion.descancelar",    "Des-cancelar")),
                new ComboItem("DEVOLUCION", Tr("accion.devolucion",     "Devolución")),
                new ComboItem("RESTAURAR",  Tr("accion.restaurar",      "Restaurar")),
            };

            string prevValue = (cmbAccion.SelectedItem as ComboItem)?.Value ?? "";
            cmbAccion.DataSource    = null;
            cmbAccion.DisplayMember = "Label";
            cmbAccion.ValueMember   = "Value";
            cmbAccion.DataSource    = items;

            // Restaurar selección previa
            foreach (ComboItem it in items)
                if (it.Value == prevValue) { cmbAccion.SelectedItem = it; break; }
        }

        private void TraducirHeadersGrilla()
        {
            var t = Traductor.ObtenerTraducciones(_idioma);
            void RH(string col, string clave, string fallback)
            {
                if (dgv.Columns.Contains(col) && t.ContainsKey(clave))
                    dgv.Columns[col].HeaderText = t[clave].Texto;
                else if (dgv.Columns.Contains(col))
                    dgv.Columns[col].HeaderText = fallback;
            }

            RH("IdOperacion",   "col.hist.op",       "Op.");
            RH("Fecha",         "col.hist.fecha",     "Fecha");
            RH("NombreUsuario", "col.hist.usuario",   "Usuario");
            RH("Accion",        "col.hist.accion",    "Acción");
            RH("Campo",         "col.hist.campo",     "Campo");
            RH("ValorAnterior", "col.hist.anterior",  "Valor Anterior");
            RH("ValorNuevo",    "col.hist.nuevo",     "Valor Nuevo");
        }

        // ── Búsqueda ──────────────────────────────────────────────────────────

        private void Buscar()
        {
            try
            {
                string accion = (cmbAccion.SelectedItem as ComboItem)?.Value;
                if (string.IsNullOrEmpty(accion)) accion = null;

                DateTime? desde = chkDesde.Checked ? (DateTime?)dtpDesde.Value.Date : null;
                DateTime? hasta = chkHasta.Checked ? (DateTime?)dtpHasta.Value.Date : null;

                DataTable dt = _pedidoBLL.ObtenerHistorial(_idPedido, accion, desde, hasta);

                dgv.DataSource = dt;
                ConfigurarColumnas();
                TraducirHeadersGrilla();

                var tb = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                MostrarOk(string.Format(tb.ContainsKey("pedidos.hist.encontrados") ? tb["pedidos.hist.encontrados"].Texto : "{0} registro(s) encontrado(s).", dt.Rows.Count));
                btnRestaurar.Enabled = false;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void ConfigurarColumnas()
        {
            if (!dgv.Columns.Contains("IdHistorial")) return;

            // Ocultar columna PK interna
            dgv.Columns["IdHistorial"].Visible = false;

            // Anchos razonables
            void W(string col, int w)
            {
                if (dgv.Columns.Contains(col)) dgv.Columns[col].Width = w;
            }
            W("IdOperacion",   44);
            W("Fecha",        145);
            W("NombreUsuario", 90);
            W("Accion",        95);
            W("Campo",         95);
            W("ValorAnterior",160);
            W("ValorNuevo",   160);
        }

        // ── Restaurar ─────────────────────────────────────────────────────────

        private void Restaurar()
        {
            if (dgv.SelectedRows.Count == 0)
            {
                var tHR = Traductor.ObtenerTraducciones(_idioma);
                MostrarError(tHR.ContainsKey("err.hist.restaurar") ? tHR["err.hist.restaurar"].Texto : "Seleccioná una fila del historial para restaurar.");
                return;
            }

            DataRow row = (dgv.SelectedRows[0].DataBoundItem as DataRowView)?.Row;
            if (row == null) return;

            int idOperacion = Convert.ToInt32(row["IdOperacion"]);
            string accion   = row["Accion"].ToString();

            string advertencia = Tr("conf.hist.restaurar.msg",
                "¿Restaurar el pedido #{0} al estado anterior a '{1}' (op. #{2})?\n\nNota: esta operación modifica el estado del Pedido en la base de datos.\nEl estado de las Prendas asociadas NO se revierte automáticamente.\n\n¿Confirmar?",
                new object[] { _idPedido, accion, idOperacion });

            if (MessageBox.Show(advertencia, Tr("msg.backup.titulorestaura", "Confirmar Restauración"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                _pedidoBLL.RestaurarOperacion(this.Text, _idPedido, idOperacion);
                MostrarOk(Tr("msg.hist.restaurado", "Pedido #{0} restaurado correctamente.", new object[] { _idPedido }));
                Buscar();   // Recargar historial para ver el evento RESTAURAR
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        private void BtnBuscar_Click(object sender, EventArgs e)     => Buscar();
        private void BtnRestaurar_Click(object sender, EventArgs e)  => Restaurar();
        private void BtnCerrar_Click(object sender, EventArgs e)     => this.Close();

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            btnRestaurar.Enabled = dgv.SelectedRows.Count > 0;
        }

        private void ChkDesde_CheckedChanged(object sender, EventArgs e)
            => dtpDesde.Enabled = chkDesde.Checked;

        private void ChkHasta_CheckedChanged(object sender, EventArgs e)
            => dtpHasta.Enabled = chkHasta.Checked;

        // ── Helper interno ────────────────────────────────────────────────────

        /// <summary>Par clave/label para el ComboBox de acciones.</summary>
        private class ComboItem
        {
            public string Value { get; }
            public string Label { get; }
            public ComboItem(string value, string label) { Value = value; Label = label; }
            public override string ToString() => Label;
        }
    }
}
