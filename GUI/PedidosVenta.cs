using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — Módulo de Pedidos de Venta.
    ///
    /// Permite al Vendedor:
    ///   Ver todos los pedidos realizados con su estado actual
    ///   Crear un nuevo pedido (abre NuevoPedidoForm)
    ///   Cancelar un pedido pendiente (libera prendas)
    ///   Ver detalle de prendas de cada pedido al seleccionarlo
    ///
    /// Hereda de <see cref="FormBase"/>:
    ///   - MostrarOk() y MostrarError() → heredados, no se redeclaran
    ///   - MensajeLabel → sobreescrito para devolver el lblMensaje de este formulario
    ///
    /// Accesible desde Menú → Ventas → Pedidos de Venta (permiso mnuPedidosVenta).
    /// </summary>
    public partial class PedidosVenta : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPedidoService pedidoBLL = new BLL.Pedido();

        private List<BE.Pedido> _pedidos = new List<BE.Pedido>();

        // Idioma activo — se actualiza en Traducir() para usarlo en EstadoLabel() y ColorearFilasPedidos()
        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PedidosVenta()
        {
            InitializeComponent();
        }

        // ── Observer de idioma ────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            // Recargar la grilla para que EstadoLabel() y los headers usen el nuevo idioma
            CargarPedidos();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;  // mantener sincronizado para EstadoLabel y ColorearFilasPedidos
            var t = Traductor.ObtenerTraducciones(idioma);
            btnRefrescar.Text = Tr("tip.actualizar", "Actualizar");
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(btnNuevoPedido,   t);
            Aplicar(btnCancelar,      t);
            Aplicar(btnDesCancelar,   t);
            Aplicar(lblDetalleTitulo, t);
            Aplicar(btnHistorial,     t);
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        // ── Eventos del Designer ──────────────────────────────────────────────

        private void PedidosVenta_Load(object sender, EventArgs e)
        {
            CargarPedidos();
        }

        private void BtnRefrescar_Click(object sender, EventArgs e)
        {
            CargarPedidos();
        }

        // ── Carga ─────────────────────────────────────────────────────────────

        private void CargarPedidos()
        {
            try
            {
                _pedidos = pedidoBLL.ObtenerTodos();
                var tabla = new DataTable();
                tabla.Columns.Add("ID",         typeof(int));
                tabla.Columns.Add("Fecha",      typeof(string));
                tabla.Columns.Add("Cliente",    typeof(string));
                tabla.Columns.Add("Vendedor",   typeof(string));
                tabla.Columns.Add("Prendas",    typeof(int));
                tabla.Columns.Add("Estado",     typeof(string));
                tabla.Columns.Add("Despacho",   typeof(string));
                tabla.Columns.Add("Entrega",    typeof(string));
                tabla.Columns.Add("Motivo",     typeof(string));
                // Columna interna: int del enum para colorear sin depender del idioma activo
                tabla.Columns.Add("_EstadoKey", typeof(int));

                foreach (var p in _pedidos)
                {
                    tabla.Rows.Add(
                        p.IdPedido,
                        p.FechaPedido.ToString("dd/MM/yyyy HH:mm"),
                        p.NombreCliente,
                        p.NombreEmpleado,
                        p.CantidadPrendas,
                        EstadoLabel(p.Estado),
                        p.FechaDespacho.HasValue ? p.FechaDespacho.Value.ToString("dd/MM/yyyy") : "—",
                        p.FechaEntrega.HasValue  ? p.FechaEntrega.Value.ToString("dd/MM/yyyy")  : "—",
                        p.MotivoCancelacion ?? "",
                        (int)p.Estado);
                }

                dgvPedidos.DataSource = tabla;
                ColorearFilasPedidos();

                if (dgvPedidos.Columns.Contains("ID"))
                    dgvPedidos.Columns["ID"].Width = 44;

                // Ocultar la columna interna y traducir headers
                if (dgvPedidos.Columns.Contains("_EstadoKey"))
                    dgvPedidos.Columns["_EstadoKey"].Visible = false;
                TraducirHeadersGrilla();

                lblConteo.Text = Tr("msg.ped.conteo", "{0} pedido(s)", new object[] { _pedidos.Count });
                dgvDetallePrendas.DataSource = null;
                // lblDetalleTitulo se traduce via Tag en Traducir(); no hardcodear aquí
                Aplicar(lblDetalleTitulo, Traductor.ObtenerTraducciones(_idioma));

                MostrarOk(Tr("msg.ped.cargados", "{0} pedido(s) cargado(s).", new object[] { _pedidos.Count }));
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void ColorearFilasPedidos()
        {
            foreach (DataGridViewRow row in dgvPedidos.Rows)
            {
                // Usar _EstadoKey (int del enum) en lugar del texto visible,
                // así el coloreado funciona independientemente del idioma activo.
                if (!dgvPedidos.Columns.Contains("_EstadoKey")) continue;
                if (!int.TryParse(row.Cells["_EstadoKey"].Value?.ToString(), out int estadoKey)) continue;
                row.DefaultCellStyle.ForeColor = estadoKey switch
                {
                    (int)BE.EstadoPedido.Pendiente  => Color.FromArgb(160, 100, 0),
                    (int)BE.EstadoPedido.Despachado => Color.FromArgb(30, 100, 170),
                    (int)BE.EstadoPedido.Entregado  => Color.FromArgb(30, 130, 30),
                    (int)BE.EstadoPedido.Cancelado  => Color.FromArgb(160, 50, 50),
                    _                               => Color.Black
                };
            }
        }

        /// <summary>
        /// Renombra los HeaderText de las columnas de dgvPedidos según el idioma activo.
        /// Los nombres internos del DataTable no cambian (se usan en ObtenerPedidoSeleccionado).
        /// </summary>
        private void TraducirHeadersGrilla()
        {
            var t = Traductor.ObtenerTraducciones(_idioma);

            void RH(string col, string clave)
            {
                if (dgvPedidos.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvPedidos.Columns[col].HeaderText = t[clave].Texto;
            }

            RH("Fecha",    "col.ped.fecha");
            RH("Cliente",  "col.ped.cliente");
            RH("Vendedor", "col.ped.vendedor");
            RH("Prendas",  "col.ped.prendas");
            RH("Estado",   "col.ped.estado");
            RH("Despacho", "col.ped.despacho");
            RH("Entrega",  "col.ped.entrega");
            RH("Motivo",   "col.ped.motivo");
        }

        private void DgvPedidos_SelectionChanged(object sender, EventArgs e)
        {
            bool hay = dgvPedidos.SelectedRows.Count > 0;
            dgvDetallePrendas.DataSource = null;

            if (!hay)
            {
                btnCancelar.Enabled    = false;
                btnDesCancelar.Enabled = false;
                btnHistorial.Enabled   = false;
                return;
            }

            var pedido = ObtenerPedidoSeleccionado();
            if (pedido == null) return;

            btnCancelar.Enabled    = pedido.Estado == BE.EstadoPedido.Pendiente;
            btnDesCancelar.Enabled = pedido.Estado == BE.EstadoPedido.Cancelado;
            btnHistorial.Enabled   = true;

            // Cargar detalle de prendas del pedido seleccionado
            CargarDetallePrendas(pedido.IdPedido);

            string motivoLabel = Tr("lbl.motivo", "Motivo:");
            lblDetalleTitulo.Text = Tr("lbl.ped.seleccionado", "Pedido #{0} — {1} — {2}",
                new object[] { pedido.IdPedido, pedido.NombreCliente, EstadoLabel(pedido.Estado) }) +
                (!string.IsNullOrEmpty(pedido.MotivoCancelacion)
                    ? $"  |  {motivoLabel} {pedido.MotivoCancelacion}" : "");
        }

        private void CargarDetallePrendas(int idPedido)
        {
            try
            {
                var pedidoCompleto = pedidoBLL.ObtenerPorId(idPedido);
                if (pedidoCompleto == null) return;

                var tabla = new DataTable();
                tabla.Columns.Add(Tr("col.prenda.nombre",    "Prenda"),    typeof(string));
                tabla.Columns.Add(Tr("col.prenda.categoria", "Categoría"), typeof(string));
                tabla.Columns.Add(Tr("col.prenda.talle",     "Talle"),     typeof(string));
                tabla.Columns.Add(Tr("col.prenda.color",     "Color"),     typeof(string));
                tabla.Columns.Add(Tr("col.prenda.estado",    "Estado"),    typeof(string));

                foreach (var p in pedidoCompleto.Prendas)
                    tabla.Rows.Add(p.Nombre, p.Categoria ?? "—",
                        p.Talle ?? "—", p.Color ?? "—", EstadoPrendaLabel(p.Estado));

                dgvDetallePrendas.DataSource = tabla;
            }
            catch (Exception ex)
            {
                // Antes solo trazaba (Trace.TraceError): el usuario veía la grilla de detalle
                // vacía sin ningún mensaje, sin poder distinguir "no hay prendas" de "falló la
                // consulta", y tampoco quedaba auditado en bitácora. MostrarError(ex), heredado
                // de FormBase, cubre ambas cosas.
                MostrarError(ex);
            }
        }

        private string EstadoPrendaLabel(BE.EstadoPrenda estado)
        {
            switch (estado)
            {
                case BE.EstadoPrenda.Disponible:  return Tr("prenda.disponible",  "Disponible");
                case BE.EstadoPrenda.EnUso:       return Tr("prenda.enuso",       "En Uso");
                case BE.EstadoPrenda.EnLimpieza:  return Tr("prenda.enlimpieza",  "En Limpieza");
                case BE.EstadoPrenda.Baja:        return Tr("prenda.baja",        "Baja");
                default: return estado.ToString();
            }
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        private void BtnNuevoPedido_Click(object sender, EventArgs e)
        {
            using (var form = new NuevoPedidoForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var tCreado = Traductor.ObtenerTraducciones(_idioma);
                string fmtCreado = tCreado.ContainsKey("msg.ped.creado") ? tCreado["msg.ped.creado"].Texto : "Pedido #{0} creado. Estado: Pendiente.";
                MostrarOk(string.Format(fmtCreado, form.IdPedidoCreado));
                CargarPedidos();
            }
        }

        private void BtnCancelarPedido_Click(object sender, EventArgs e)
        {
            var seleccionado = ObtenerPedidoSeleccionado();
            if (seleccionado == null) return;

            // Releer el estado ACTUAL desde BD antes de actuar: `seleccionado` viene de la grilla
            // cacheada en memoria (última vez que se llamó CargarPedidos()), que puede estar
            // desactualizada si otro operador ya cambió este pedido. Mismo criterio que ya usa
            // BtnDevolucion_Click en PedidosRealizados.
            var pedido = pedidoBLL.ObtenerPorId(seleccionado.IdPedido);
            if (pedido == null)
            {
                MostrarError(Tr("msg.ped.yanoexiste", "Este pedido ya no existe. Actualizá la grilla."));
                CargarPedidos();
                return;
            }

            // Pedir motivo de cancelación con un dialog inline
            string motivo = PedirTexto(
                $"{Tr("dlg.cancelped.titulo", "Motivo de Cancelación")} — Pedido #{pedido.IdPedido} ({pedido.NombreCliente}):",
                Tr("dlg.cancelped.titulo", "Motivo de Cancelación"));

            if (string.IsNullOrWhiteSpace(motivo))
            {
                MostrarError(Tr("msg.cancelped.req", "La cancelación requiere un motivo."));
                return;
            }

            string bodyCanc = string.Format(
                Tr("conf.cancelped.body", "¿Cancelar el Pedido #{0} de {1}?\n\nMotivo: {2}\n\nLas prendas volverán a estado Disponible."),
                pedido.IdPedido, pedido.NombreCliente, motivo);

            var confirmar = MessageBox.Show(
                bodyCanc,
                Tr("conf.cancelped.titulo", "Confirmar Cancelación"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                // Patrón Command (PdN3): la GUI arma el pedido de cancelación y se lo entrega
                // al invocador — no decide nada, solo empaqueta la petición. La decisión y la
                // ejecución real siguen 100% en BLL.Pedido.Cancelar, adentro del Command.
                var invocador = new BLL.Comandos.InvocadorPedido();
                invocador.TomarOrden(new BLL.Comandos.CancelacionCommand(pedidoBLL, pedido, this.Text, motivo));
                invocador.ProcesarOrdenes();

                MostrarOk(Tr("msg.ped.cancelado", "Pedido #{0} cancelado. Prendas liberadas.", new object[] { pedido.IdPedido }));
                CargarPedidos();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void BtnDesCancelarPedido_Click(object sender, EventArgs e)
        {
            var seleccionado = ObtenerPedidoSeleccionado();
            if (seleccionado == null) return;

            // Releer el estado ACTUAL desde BD antes de actuar (ver comentario en
            // BtnCancelarPedido_Click).
            var pedido = pedidoBLL.ObtenerPorId(seleccionado.IdPedido);
            if (pedido == null)
            {
                MostrarError(Tr("msg.ped.yanoexiste", "Este pedido ya no existe. Actualizá la grilla."));
                CargarPedidos();
                return;
            }

            string bodyDesc = string.Format(
                Tr("conf.descancelar.body", "¿Des-cancelar el Pedido #{0} de {1}?\n\nSe verificará que las prendas originales estén disponibles\ny el pedido volverá a estado Pendiente."),
                pedido.IdPedido, pedido.NombreCliente);

            var confirmar = MessageBox.Show(
                bodyDesc,
                Tr("conf.descancelar.titulo", "Confirmar Des-cancelación"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                pedidoBLL.DesCancelar(this.Text, pedido);
                string fmtReact = Tr("msg.ped.reactivado", "Pedido #{0} reactivado — volvió a Pendiente.");
                MostrarOk(string.Format(fmtReact, pedido.IdPedido));
                CargarPedidos();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private BE.Pedido ObtenerPedidoSeleccionado()
        {
            if (dgvPedidos.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPedidos.SelectedRows[0].Cells["ID"].Value);
            return _pedidos.Find(p => p.IdPedido == id);
        }

        private string EstadoLabel(BE.EstadoPedido estado)
        {
            var t = Traductor.ObtenerTraducciones(_idioma);
            switch (estado)
            {
                case BE.EstadoPedido.Pendiente:  return t.ContainsKey("est.pendiente")  ? t["est.pendiente"].Texto  : "Pendiente";
                case BE.EstadoPedido.Despachado: return t.ContainsKey("est.despachado") ? t["est.despachado"].Texto : "Despachado";
                case BE.EstadoPedido.Entregado:  return t.ContainsKey("est.entregado")  ? t["est.entregado"].Texto  : "Entregado";
                case BE.EstadoPedido.Cancelado:  return t.ContainsKey("est.cancelado")  ? t["est.cancelado"].Texto  : "Cancelado";
                default: return estado.ToString();
            }
        }

        /// <summary>
        /// Muestra un dialog simple para pedir texto al usuario.
        /// Devuelve null si cancela o deja vacío.
        /// </summary>
        private string PedirTexto(string prompt, string titulo)
        {
            string resultado = null;
            using (var dlg = new Form())
            {
                dlg.Text            = titulo;
                dlg.ClientSize      = new Size(420, 130);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.StartPosition   = FormStartPosition.CenterParent;

                dlg.Controls.Add(new Label
                {
                    Text = prompt, Left = 12, Top = 12,
                    Width = 396, Height = 36,
                    Font = new Font("Segoe UI", 9f)
                });

                var txt = new TextBox { Left = 12, Top = 52, Width = 396 };
                dlg.Controls.Add(txt);

                var tPedir = Traductor.ObtenerTraducciones(_idioma);
                string aceptarTxt  = tPedir.ContainsKey("btn.aceptar")  ? tPedir["btn.aceptar"].Texto  : "Aceptar";
                string cancelarTxt = tPedir.ContainsKey("btn.cancelar") ? tPedir["btn.cancelar"].Texto : "Cancelar";

                var btnOk = new Button
                {
                    Text         = aceptarTxt, Left = 220, Top = 84,
                    Width = 90, Height = 30,
                    DialogResult = DialogResult.OK,
                    BackColor    = Color.SteelBlue, ForeColor = Color.White,
                    FlatStyle    = FlatStyle.Flat
                };
                btnOk.FlatAppearance.BorderSize = 0;

                var btnCancel = new Button
                {
                    Text         = cancelarTxt, Left = 318, Top = 84,
                    Width = 90, Height = 30,
                    DialogResult = DialogResult.Cancel,
                    FlatStyle    = FlatStyle.Flat
                };

                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    resultado = txt.Text.Trim();
            }
            return resultado;
        }

        // ── Historial de cambios ──────────────────────────────────────────────

        private void BtnHistorial_Click(object sender, EventArgs e)
        {
            var pedido = ObtenerPedidoSeleccionado();
            if (pedido == null) return;

            using (var frm = new PedidoHistorialForm(pedido.IdPedido))
                frm.ShowDialog(this);
        }
    }
}
