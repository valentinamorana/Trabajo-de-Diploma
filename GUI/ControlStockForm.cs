using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PN01 — Control de Stock: carril "Controlador de Stock" del diagrama de actividad de
    /// Armar pedido, a cargo del rol Depósito (Inventario → Control de Stock, patente
    /// mnuControlStock).
    ///
    ///   Revisar stock de las prendas (planilla de control de existencias) → ¿Selección disponible?
    ///     No → Informe de prendas faltantes (faltantes y alternativas) → el Vendedor las comunica
    ///     Sí → Confirmar prendas disponibles → Separar prendas del pedido (constancia)
    ///
    /// Hereda de <see cref="FormBase"/> (MostrarOk/MostrarError, Tr, MensajeLabel).
    /// </summary>
    public partial class ControlStockForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPedidoService pedidoBLL = new BLL.Pedido();
        private readonly BLL.Interfaces.IClienteService clienteBLL = new BLL.Cliente();

        private List<BE.Pedido>             _cola    = new List<BE.Pedido>();
        private BE.Pedido                   _pedido  = null;   // pedido seleccionado (releído de la base)
        private List<BE.LineaControlStock>  _lineas  = new List<BE.LineaControlStock>();

        // true mientras se recarga la cola: evita la recursión CargarCola → SelectionChanged →
        // RevisarStock → (falla o pedido movido) → CargarCola → ...
        private bool _cargando;

        // "Documentos ▾": los documentos del pedido se imprimen a pedido, no tras cada acción.
        private readonly MenuDocumentosPedido menuDocumentos;

        public ControlStockForm()
        {
            InitializeComponent();
            menuDocumentos = new MenuDocumentosPedido(pedidoBLL, clienteBLL, () => _pedido, () => this.Text, this,
                ex => MostrarError(ex), (k, f) => Tr(k, f), () => _lineas);
            // Estilo de grilla compartido (encabezado rosa, filas alternadas) — EstiloFormulario.
            Estilos.EstiloFormulario.Grilla(dgvCola);
            Estilos.EstiloFormulario.Grilla(dgvPlanilla);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarCola();
        }

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            this.Text = Tr("frm.controlstock", "Control de Stock");
            btnRefrescar.Text = Tr("tip.actualizar", "Actualizar");
            foreach (var c in new Control[] { btnInformarFaltantes, btnConfirmarPrendas, btnSepararPrendas, btnImprimirPlanilla })
                if (c.Tag != null && t.ContainsKey(c.Tag.ToString())) c.Text = t[c.Tag.ToString()].Texto;
            menuDocumentos.Traducir();
        }

        private void ControlStockForm_Load(object sender, EventArgs e) => CargarCola();

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarCola();

        // ── Cola de pedidos enviados a control de stock (FIFO) ────────────────

        private void CargarCola()
        {
            if (_cargando) return;
            _cargando = true;
            try
            {
                _cola = pedidoBLL.ObtenerColaControlStock();
                var tabla = new DataTable();
                tabla.Columns.Add("ID",          typeof(int));
                tabla.Columns.Add("Enviado",     typeof(string));
                tabla.Columns.Add("Cliente",     typeof(string));
                tabla.Columns.Add("Vendedor",    typeof(string));
                tabla.Columns.Add("Confirmado",  typeof(string));

                foreach (var p in _cola)
                    tabla.Rows.Add(p.IdPedido,
                        (p.FechaEnvioControl ?? p.FechaPedido).ToString("g"),
                        p.NombreCliente, p.NombreEmpleado,
                        p.FechaControl.HasValue ? p.FechaControl.Value.ToString("dd/MM HH:mm") : "—");

                dgvCola.DataSource = tabla;
                if (dgvCola.Columns.Contains("ID")) dgvCola.Columns["ID"].Width = 50;
                TraducirHeaders(dgvCola, new Dictionary<string, (string, string)>
                {
                    { "Enviado",    ("col.cs.enviado",    "Enviado a control") },
                    { "Cliente",    ("col.ped.cliente",   "Cliente") },
                    { "Vendedor",   ("col.ped.vendedor",  "Vendedor") },
                    { "Confirmado", ("col.cs.confirmado", "Prendas confirmadas") }
                });

                lblConteo.Text = Tr("msg.cs.conteo", "{0} pedido(s) para controlar", new object[] { _cola.Count });
                // Tras recargar no queda ninguna fila seleccionada: el usuario elige el pedido
                // (antes el DataBinding seleccionaba la primera y disparaba RevisarStock solo).
                dgvCola.ClearSelection();
                LimpiarPlanilla();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
            finally
            {
                _cargando = false;
            }
        }

        private void DgvCola_SelectionChanged(object sender, EventArgs e)
        {
            if (_cargando) return;
            if (dgvCola.SelectedRows.Count == 0) { LimpiarPlanilla(); return; }
            int id = Convert.ToInt32(dgvCola.SelectedRows[0].Cells["ID"].Value);
            RevisarStock(id);
        }

        // ── "Revisar stock de las prendas" → ¿Selección disponible? ──────────

        private void RevisarStock(int idPedido)
        {
            try
            {
                _pedido = pedidoBLL.ObtenerPorId(idPedido);
                if (_pedido == null || _pedido.Estado != BE.EstadoPedido.EnControlStock)
                {
                    MostrarError(Tr("msg.cs.yanoencontrol", "Este pedido ya no está en control de stock. Se actualiza la cola."));
                    CargarCola();
                    return;
                }

                _lineas = pedidoBLL.RevisarStock(_pedido);

                var tabla = new DataTable();
                tabla.Columns.Add("ID",          typeof(int));
                tabla.Columns.Add("Prenda",      typeof(string));
                tabla.Columns.Add("Categoria",   typeof(string));
                tabla.Columns.Add("Talle",       typeof(string));
                tabla.Columns.Add("Color",       typeof(string));
                tabla.Columns.Add("EstadoActual", typeof(string));
                tabla.Columns.Add("Disponible",  typeof(string));
                tabla.Columns.Add("Confirmada",  typeof(string));

                string si = Tr("lbl.si", "Sí"), no = Tr("lbl.no", "No");
                foreach (var l in _lineas)
                    tabla.Rows.Add(l.Prenda.IdPrenda, l.Prenda.Nombre, l.Prenda.Categoria ?? "—",
                        l.Prenda.Talle ?? "—", l.Prenda.Color ?? "—",
                        l.ReservadaParaOtro ? Tr("lbl.cs.reservada", "Reservada (Lista de Espera)") : EstadoPrendaLabel(l.EstadoActual),
                        l.Disponible ? si : no,
                        l.Confirmada ? si : no);

                dgvPlanilla.DataSource = tabla;
                if (dgvPlanilla.Columns.Contains("ID")) dgvPlanilla.Columns["ID"].Width = 50;
                TraducirHeaders(dgvPlanilla, new Dictionary<string, (string, string)>
                {
                    { "Prenda",       ("col.prenda.nombre",    "Prenda") },
                    { "Categoria",    ("col.prenda.categoria", "Categoría") },
                    { "Talle",        ("col.prenda.talle",     "Talle") },
                    { "Color",        ("col.prenda.color",     "Color") },
                    { "EstadoActual", ("col.cs.estadoactual",  "Estado actual") },
                    { "Disponible",   ("col.cs.disponible",    "¿Disponible?") },
                    { "Confirmada",   ("col.cs.confirmada",    "Confirmada") }
                });
                for (int i = 0; i < _lineas.Count; i++)
                    if (!_lineas[i].Disponible)
                        dgvPlanilla.Rows[i].DefaultCellStyle.ForeColor = Tema.Error;

                // "¿Selección disponible?" y las acciones habilitadas las decide la BLL.
                var evaluacion = BLL.EvaluacionControlStock.Evaluar(_lineas, _pedido);
                bool disponible = evaluacion.SeleccionDisponible;
                lblPlanillaTitulo.Text = string.Format(
                    Tr("lbl.cs.planilla.pedido",
                       "Planilla de control — Pedido #{0} — {1} — ¿Selección disponible? {2}"),
                    _pedido.IdPedido, _pedido.NombreCliente, disponible ? si : no);
                lblPlanillaTitulo.ForeColor = disponible ? Tema.Exito : Tema.Error;

                // Decisión del diagrama: No → Informe de faltantes; Sí → Confirmar → Separar.
                btnInformarFaltantes.Enabled = evaluacion.PuedeInformarFaltantes;
                btnConfirmarPrendas.Enabled  = evaluacion.PuedeConfirmar;
                btnSepararPrendas.Enabled    = evaluacion.PuedeSeparar;
                btnImprimirPlanilla.Enabled  = true;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void LimpiarPlanilla()
        {
            _pedido = null;
            _lineas = new List<BE.LineaControlStock>();
            dgvPlanilla.DataSource = null;
            lblPlanillaTitulo.Text = Tr("lbl.cs.planilla", "Planilla de control de existencias — seleccioná un pedido de la cola");
            lblPlanillaTitulo.ForeColor = Tema.RosaOscuro;
            btnInformarFaltantes.Enabled = btnConfirmarPrendas.Enabled =
                btnSepararPrendas.Enabled = btnImprimirPlanilla.Enabled = false;
        }

        // ── "Informe de prendas faltantes" ────────────────────────────────────

        private void BtnInformarFaltantes_Click(object sender, EventArgs e)
        {
            if (_pedido == null) return;
            try
            {
                var faltantes = pedidoBLL.InformarFaltantes(this.Text, _pedido);
                MostrarOk(Tr("msg.cs.faltantes",
                    "Informe de faltantes del Pedido #{0} emitido: {1} prenda(s). El Vendedor lo comunicará al cliente.",
                    new object[] { _pedido.IdPedido, faltantes.Count }));
                // El informe se imprime desde "Documentos ▾" (aquí o en Pedidos de venta).
                CargarCola();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ── "Confirmar prendas disponibles" ───────────────────────────────────

        private void BtnConfirmarPrendas_Click(object sender, EventArgs e)
        {
            if (_pedido == null) return;
            try
            {
                pedidoBLL.ConfirmarPrendasDisponibles(this.Text, _pedido);
                MostrarOk(Tr("msg.cs.confirmadas",
                    "Pedido #{0}: {1} prenda(s) confirmadas como disponibles. Separalas para reservarlas.",
                    new object[] { _pedido.IdPedido, _pedido.CantidadPrendas }));
                int id = _pedido.IdPedido;
                CargarCola();
                SeleccionarEnCola(id);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ── "Separar prendas del pedido" ──────────────────────────────────────

        private void BtnSepararPrendas_Click(object sender, EventArgs e)
        {
            if (_pedido == null) return;
            if (!Preguntar(string.Format(Tr("conf.cs.separar",
                    "¿Separar las {0} prenda(s) del Pedido #{1} para {2}?\n\nQuedan reservadas (en uso) a nombre del cliente."),
                    _pedido.CantidadPrendas, _pedido.IdPedido, _pedido.NombreCliente)))
                return;

            int id = _pedido.IdPedido;
            try
            {
                pedidoBLL.SepararPrendas(this.Text, _pedido);
                MostrarOk(Tr("msg.cs.separadas",
                    "Pedido #{0}: prendas separadas. El Vendedor ya puede formalizar el pedido.", new object[] { id }));
            }
            // Al separar, otra operación tomó una prenda: el pedido volvió a "¿Selección disponible? No"
            // y ya se emitió el informe de faltantes (se imprime desde "Documentos ▾").
            catch (Exception ex) { MostrarError(ex); }
            CargarCola();
        }

        // "Documentos ▾": planilla, informe de faltantes, prendas confirmadas, constancia de separación…
        private void BtnImprimirPlanilla_Click(object sender, EventArgs e) => menuDocumentos.Mostrar(btnImprimirPlanilla);

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool Preguntar(string texto) =>
            ConfirmarSiNo(texto, this.Text, porDefectoNo: true);

        private void SeleccionarEnCola(int idPedido)
        {
            foreach (DataGridViewRow row in dgvCola.Rows)
                if (Convert.ToInt32(row.Cells["ID"].Value) == idPedido)
                {
                    row.Selected = true;
                    dgvCola.CurrentCell = row.Cells["ID"];
                    return;
                }
        }

        private void TraducirHeaders(DataGridView grilla, Dictionary<string, (string Clave, string Fallback)> mapa)
        {
            foreach (var kv in mapa)
                if (grilla.Columns.Contains(kv.Key))
                    grilla.Columns[kv.Key].HeaderText = Tr(kv.Value.Clave, kv.Value.Fallback);
        }

        private string EstadoPrendaLabel(BE.EstadoPrenda estado)
        {
            switch (estado)
            {
                case BE.EstadoPrenda.Disponible: return Tr("prenda.disponible", "Disponible");
                case BE.EstadoPrenda.EnUso:      return Tr("prenda.enuso",      "En Uso");
                case BE.EstadoPrenda.EnLimpieza: return Tr("prenda.enlimpieza", "En Limpieza");
                case BE.EstadoPrenda.Baja:       return Tr("prenda.baja",       "Baja");
                default:                         return estado.ToString();
            }
        }
    }
}
