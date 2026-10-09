using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PN01 — Armar pedido de prendas: actividades del carril Vendedor del diagrama de
    /// actividad hasta "Enviar selección para control stock".
    ///
    ///   PASO 1 — Recibir identificación (DNI, nombre o apellido) → Ficha del cliente →
    ///            Verificar la vigencia de la suscripción → Revisar existencia de un pedido activo
    ///   PASO 2 — Presentar catálogo → Anotar la selección + Comprobar el cupo del plan →
    ///            ¿Excede el cupo? Sí: Informar exceso → el cliente ajusta la selección o
    ///            desiste (Asentar desistimiento). No: Enviar selección para control stock.
    ///
    /// Modo AJUSTE: para un pedido con faltantes informados por Depósito ("Recibir selección
    /// ajustada por disponibilidad"): abre directamente el paso 2 con la selección sin los
    /// faltantes y las alternativas resaltadas, y la reenvía a control de stock.
    ///
    /// Devuelve DialogResult.OK cuando se envió/reenvió la selección o se asentó el
    /// desistimiento. El ID queda en IdPedidoCreado.
    /// </summary>
    /// <summary>
    /// Hereda de <see cref="FormBase"/>:
    ///   - MostrarError() → heredado, no se redeclara
    ///   - MensajeLabel → sobreescrito para devolver el lblMensaje de este formulario
    /// </summary>
    public partial class NuevoPedidoForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        public int IdPedidoCreado { get; private set; }

        // True si el resultado fue un desistimiento (no un envío a control).
        public bool FueDesistimiento { get; private set; }

        // ── BLL ───────────────────────────────────────────────────────────────
        private readonly BLL.Interfaces.IClienteService clienteBLL = new BLL.Cliente();
        private readonly BLL.Interfaces.IPrendaService  prendaBLL  = new BLL.Prenda();
        private readonly BLL.Interfaces.IPedidoService  pedidoBLL  = new BLL.Pedido();

        // ── Estado interno ────────────────────────────────────────────────────
        private List<BE.Cliente> _coincidencias = new List<BE.Cliente>();
        private List<BE.Prenda>  _disponibles   = new List<BE.Prenda>();
        private BE.Cliente       _clienteSel    = null;
        private Idioma           _idioma        = GestorIdioma.IdiomaActual;

        // Modo ajuste (pedido con faltantes).
        private readonly BE.Pedido               _pedidoAjuste;
        private readonly List<BE.PedidoFaltante> _faltantes;
        private bool EsAjuste => _pedidoAjuste != null;

        public NuevoPedidoForm()
        {
            InitializeComponent();
            AplicarIdioma(GestorIdioma.IdiomaActual);
        }

        // Modo ajuste: "Recibir selección ajustada por disponibilidad".
        public NuevoPedidoForm(BE.Pedido pedidoConFaltantes, List<BE.PedidoFaltante> faltantes) : this()
        {
            _pedidoAjuste = pedidoConFaltantes ?? throw new ArgumentNullException(nameof(pedidoConFaltantes));
            _faltantes    = faltantes ?? new List<BE.PedidoFaltante>();
            AplicarIdioma(GestorIdioma.IdiomaActual);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        // ── IIdiomaObserver ───────────────────────────────────────────────────

        public void UpdateLanguage(Idioma idioma)
        {
            AplicarIdioma(idioma);
            if (dgvPrendas.Columns.Count > 0)
                TraducirHeadersGrilla();
        }

        // ── Traducción ────────────────────────────────────────────────────────

        private void AplicarIdioma(Idioma idioma)
        {
            _idioma = idioma;
            this.Text                 = EsAjuste
                ? Tr("frm.ajustarpedido", "Ajustar selección del pedido")
                : Tr("frm.nuevopedido",   "Nuevo Pedido de Venta");
            lblPaso.Text              = Tr("paso1.identificar",     "Paso 1 de 2 — Identificar al cliente");
            lblSeleccionaCliente.Text = Tr("lbl.ped.identificacion","Identificación del cliente (DNI, nombre o apellido):");
            lblInstruccion.Text       = EsAjuste
                ? Tr("lbl.ped.ajuste", "Ajustá la selección: las prendas faltantes ya no figuran y las alternativas sugeridas están resaltadas.")
                : Tr("lbl.ped.selprendas", "Seleccioná las prendas para incluir en el pedido (checkbox):");
            btnBuscar.Text            = Tr("btn.ped.buscar",        "Buscar");
            btnSiguiente.Text         = Tr("btn.siguiente",         "Siguiente →");
            btnVolver.Text            = Tr("btn.volver",            "← Volver");
            btnConfirmar.Text         = EsAjuste
                ? Tr("btn.ped.reenviarcontrol", "Reenviar a control de stock")
                : Tr("btn.ped.enviarcontrol",   "Enviar a control de stock");
            btnDesistir.Text          = Tr("btn.ped.desistir",      "Registrar desistimiento");
            btnImprimirAviso.Text     = Tr("btn.ped.imprimiraviso", "Imprimir aviso");
            btnImprimirCupo.Text      = Tr("btn.ped.imprimircupo",  "Imprimir detalle");
        }

        private void TraducirHeadersGrilla()
        {
            var t = Traductor.ObtenerTraducciones(_idioma);
            void RH(string col, string clave, string fallback)
            {
                if (dgvPrendas.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvPrendas.Columns[col].HeaderText = t[clave].Texto;
                else if (dgvPrendas.Columns.Contains(col))
                    dgvPrendas.Columns[col].HeaderText = fallback;
            }
            RH("Nombre",   "col.prenda.nombre",    "Nombre");
            RH("Categoria","col.prenda.categoria",  "Categoría");
            RH("Talle",    "col.prenda.talle",      "Talle");
            RH("Color",    "col.prenda.color",      "Color");
        }

        private void NuevoPedidoForm_Load(object sender, EventArgs e)
        {
            if (!EsAjuste)
            {
                txtIdentificacion.Focus();
                return;
            }

            // Modo ajuste: el cliente ya está identificado; se abre directamente el catálogo.
            try
            {
                _clienteSel = clienteBLL.ObtenerPorId(_pedidoAjuste.IdCliente);
                btnVolver.Visible = false;
                MostrarPaso(2);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void BtnVolver_Click(object sender, EventArgs e)
        {
            MostrarPaso(1);
        }

        private void DgvPrendas_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvPrendas.IsCurrentCellDirty)
                dgvPrendas.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // ── Paso 1: Recibir identificación ────────────────────────────────────

        private void TxtIdentificacion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            BuscarCliente();
        }

        private void BtnBuscar_Click(object sender, EventArgs e) => BuscarCliente();

        private void BuscarCliente()
        {
            _clienteSel = null;
            btnSiguiente.Enabled     = false;
            OcultarAviso();
            lstCoincidencias.Visible = false;
            lstCoincidencias.Items.Clear();

            try
            {
                _coincidencias = clienteBLL.BuscarPorIdentificacion(txtIdentificacion.Text);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                return;
            }

            if (_coincidencias.Count == 0)
            {
                MostrarAviso(Tr("err.ped.noencontrado",
                    "No se encontró ningún cliente con esa identificación. Verificá el DNI, el nombre o el apellido."));
                return;
            }

            if (_coincidencias.Count == 1)
            {
                SeleccionarCliente(_coincidencias[0]);
                return;
            }

            // Varias coincidencias (por nombre o apellido): el Vendedor elige la correcta.
            foreach (var c in _coincidencias)
                lstCoincidencias.Items.Add($"{c.NombreCompleto}  (DNI {c.DNI})");
            lstCoincidencias.Visible = true;
            MostrarAviso(Tr("lbl.ped.variascoinc", "Hay varios clientes que coinciden: elegí el correcto en la lista."),
                         Tema.Info);
        }

        private void LstCoincidencias_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstCoincidencias.SelectedIndex;
            if (idx >= 0 && idx < _coincidencias.Count)
                SeleccionarCliente(_coincidencias[idx]);
        }

        // Ficha del cliente → ¿Suscripción vigente? → ¿Posee pedido activo?
        private void SeleccionarCliente(BE.Cliente cliente)
        {
            _clienteSel = cliente;
            btnSiguiente.Enabled = false;
            OcultarAviso();

            string ficha = FichaCliente(cliente);

            // "Verificar la vigencia de la suscripción" → No: "Informar imposibilidad de continuar".
            try
            {
                _clienteSel = pedidoBLL.VerificarVigencia(cliente.IdCliente);
            }
            catch (BE.AppException ex)
            {
                string motivo = Resolver(ex);
                MostrarAviso(ficha + "\n\n" + Tr("lbl.ped.imposible", "No se puede continuar:") + " " + motivo);
                // Aviso de suscripción no vigente (imprimible para el cliente).
                OfrecerAviso(() => Exportacion.DocumentosPedido.AvisoImposibilidad(cliente, false, motivo));
                return;
            }
            catch (Exception ex) { MostrarError(ex); return; }

            // "Revisar existencia de un pedido activo" → Sí: "Informar existencia de pedido activo".
            try
            {
                pedidoBLL.RevisarPedidoActivo(_clienteSel);
            }
            catch (BE.AppException ex)
            {
                string motivo = Resolver(ex);
                MostrarAviso(ficha + "\n\n" + Tr("lbl.ped.pedidoactivo", "Pedido activo:") + " " + motivo);
                // Aviso de pedido activo (imprimible para el cliente).
                OfrecerAviso(() => Exportacion.DocumentosPedido.AvisoImposibilidad(_clienteSel, true, motivo));
                return;
            }
            catch (Exception ex) { MostrarError(ex); return; }

            // Habilitado: se presenta el catálogo en el paso 2.
            var estado = clienteBLL.ObtenerEstadoComercial(_clienteSel, 0);
            lblInfoPlan.Visible = true;
            if (estado.SuscripcionProximaAVencer)
            {
                lblInfoPlan.ForeColor = Tema.Alerta;
                lblInfoPlan.Text = ficha + "\n" + string.Format(
                    Tr("lbl.ped.proxvencer",
                        "La suscripción vence en {0} día(s). Avisale al cliente para renovarla."),
                    estado.DiasHastaVencimiento);
            }
            else
            {
                lblInfoPlan.ForeColor = Tema.RosaOscuro;
                lblInfoPlan.Text = ficha;
            }
            btnSiguiente.Enabled = true;
        }

        // Ficha del Cliente (suscripción, plan, pedidos, datos del cliente).
        private string FichaCliente(BE.Cliente c)
        {
            string ultimoPedido = "—";
            try
            {
                var ult = pedidoBLL.ObtenerTodos()
                    .Where(p => p.IdCliente == c.IdCliente)
                    .OrderByDescending(p => p.FechaPedido)
                    .FirstOrDefault();
                if (ult != null)
                    ultimoPedido = $"#{ult.IdPedido} ({EstadoLabel(ult.Estado)}, {ult.FechaPedido:d})";
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[NuevoPedidoForm] Ficha: " + ex.Message); }

            return string.Format(
                Tr("lbl.ped.ficha",
                    "Cliente: {0}  —  DNI {1}\nPlan: {2} (hasta {3} prendas)  —  Vence: {4}\n" +
                    "Prendas en uso: {5}  —  Último pedido: {6}\nMétodo de pago: {7}  —  Alta: {8}"),
                c.NombreCompleto, c.DNI,
                c.NombrePlan ?? "—", c.LimitePrendas,
                c.FechaVencimiento?.ToString("d") ?? "—",
                c.StockUtilizado, ultimoPedido,
                c.MetodoPago ?? "—", c.FechaAlta.ToString("d"));
        }

        private Func<Exportacion.ReporteExportable> _avisoActual;

        private void OfrecerAviso(Func<Exportacion.ReporteExportable> armar)
        {
            _avisoActual = armar;
            btnImprimirAviso.Visible = true;
        }

        private void OcultarAviso()
        {
            _avisoActual = null;
            btnImprimirAviso.Visible = false;
        }

        private void BtnImprimirAviso_Click(object sender, EventArgs e)
        {
            if (_avisoActual == null) return;
            try { Exportacion.DocumentosPedido.Imprimir(_avisoActual(), this); }
            catch (Exception ex) { MostrarError(ex); }
        }

        // "Detalle de restricciones de cupo" (imprimible cuando la selección excede el cupo).
        private void BtnImprimirCupo_Click(object sender, EventArgs e)
        {
            try
            {
                var cliente = clienteBLL.ObtenerPorId(_clienteSel.IdCliente) ?? _clienteSel;
                Exportacion.DocumentosPedido.Imprimir(
                    Exportacion.DocumentosPedido.RestriccionesCupo(cliente, ObtenerPrendasSeleccionadas()), this);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void MostrarAviso(string texto, Color? color = null)
        {
            lblInfoPlan.ForeColor = color ?? Color.DarkRed;
            lblInfoPlan.Visible   = true;
            lblInfoPlan.Text      = texto;
        }

        private static string Resolver(BE.AppException ex) =>
            Traductor.Resolver(ex.Clave, ex.Message, ex.Args, GestorIdioma.IdiomaActual);

        private void BtnSiguiente_Click(object sender, EventArgs e)
        {
            if (_clienteSel == null) return;
            MostrarPaso(2);
        }

        // ── Paso 2: Presentar catálogo / selección / cupo ─────────────────────

        private void MostrarPaso(int paso)
        {
            panelPaso1.Visible = paso == 1;
            panelPaso2.Visible = paso == 2;
            string paso1txt = Tr("paso1.identificar", "Paso 1 de 2 — Identificar al cliente");
            string paso2txt = EsAjuste
                ? string.Format(Tr("paso.ajuste.texto", "Ajustar selección del Pedido #{0}"), _pedidoAjuste.IdPedido)
                : Tr("paso2.texto", "Paso 2 de 2 — Seleccionar Prendas");
            lblPaso.Text = paso == 1
                ? paso1txt
                : $"{paso2txt}  ({_clienteSel?.NombreCompleto})";
            lblMensaje.Text = string.Empty;

            if (paso == 2) CargarPrendasDisponibles();
        }

        private void CargarPrendasDisponibles()
        {
            try
            {
                _disponibles = prendaBLL.ObtenerDisponibles(_clienteSel?.IdCliente);

                var preseleccion  = new HashSet<int>();
                var alternativas  = new HashSet<int>();
                if (EsAjuste)
                {
                    var faltantes = new HashSet<int>(_faltantes.Select(f => f.Prenda.IdPrenda));
                    foreach (var p in _pedidoAjuste.Prendas)
                        if (!faltantes.Contains(p.IdPrenda)) preseleccion.Add(p.IdPrenda);
                    foreach (var f in _faltantes)
                        foreach (var a in f.Alternativas) alternativas.Add(a.IdPrenda);

                    // Las alternativas sugeridas primero, para que el Vendedor las vea.
                    _disponibles = _disponibles
                        .OrderByDescending(p => alternativas.Contains(p.IdPrenda))
                        .ThenByDescending(p => preseleccion.Contains(p.IdPrenda))
                        .ToList();
                }

                dgvPrendas.Rows.Clear();
                foreach (var p in _disponibles)
                {
                    int i = dgvPrendas.Rows.Add(preseleccion.Contains(p.IdPrenda), p.IdPrenda, p.Nombre,
                        p.Categoria ?? "—", p.Talle ?? "—", p.Color ?? "—");
                    if (alternativas.Contains(p.IdPrenda))
                        dgvPrendas.Rows[i].DefaultCellStyle.BackColor = Tema.FondoAlerta;
                }

                TraducirHeadersGrilla();
                ActualizarResumen();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void DgvPrendas_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != dgvPrendas.Columns["Sel"].Index) return;
            // ActualizarResumen consulta la BLL (estado comercial del cliente): si falla, se
            // informa acá en vez de dejar que la excepción llegue al manejador global.
            try { ActualizarResumen(); }
            catch (Exception ex) { MostrarError(ex); }
        }

        // "Anotar la selección" ∥ "Comprobar el cupo del plan" → ¿Excede el cupo disponible?
        private void ActualizarResumen()
        {
            var seleccion    = ObtenerPrendasSeleccionadas();
            int seleccionadas = seleccion.Count;

            // La BLL evalúa todas las reglas de negocio y devuelve un DTO listo para mostrar
            var estado = clienteBLL.ObtenerEstadoComercial(_clienteSel, seleccionadas);

            int enUso  = estado.StockUtilizado;
            int limite = estado.LimitePrendas;
            int total  = enUso + seleccionadas;

            string linea1 = string.Format(
                Tr("lbl.ped.res.linea1", "Seleccionadas: {0}  |  Ya en uso: {1}  |  Total: {2}"),
                seleccionadas, enUso, total);
            string linea2 = "";
            bool excede = false;

            if (limite > 0)
            {
                if (estado.SuperaLimite)
                {
                    // "Informar exceso de cupo" (Detalle de restricciones de cupo).
                    excede = true;
                    linea2 = string.Format(
                        Tr("lbl.ped.res.limite",
                           "El plan '{0}' permite {1} prenda(s). Estás superando el límite por {2}."),
                        estado.NombrePlan, limite, estado.Exceso) + " " +
                        Tr("lbl.ped.res.ajustar", "El cliente ajusta la selección o desiste.");
                    lblResumen.ForeColor = Color.DarkRed;
                }
                else if (seleccionadas == 0)
                {
                    linea2 = string.Format(
                        Tr("lbl.ped.res.vacio", "Podés agregar hasta {0} prenda(s) (plan {1})."),
                        estado.PrendasDisponibles, estado.NombrePlan);
                    lblResumen.ForeColor = Color.DimGray;
                }
                else if (seleccionadas < estado.PrendasDisponibles)
                {
                    linea2 = string.Format(
                        Tr("lbl.ped.res.parcial",
                           "El plan '{0}' permite {1}. Estás eligiendo {2} de {3} posibles — podés agregar más."),
                        estado.NombrePlan, limite, seleccionadas, estado.PrendasDisponibles);
                    lblResumen.ForeColor = Tema.Alerta;
                }
                else
                {
                    linea2 = string.Format(
                        Tr("lbl.ped.res.lleno",
                           "Alcanzás el máximo del plan '{0}' ({1} prendas)."),
                        estado.NombrePlan, limite);
                    lblResumen.ForeColor = Color.DarkGreen;
                }
            }
            else
            {
                excede = seleccionadas > 0;   // plan sin cupo: cualquier selección lo excede
                lblResumen.ForeColor = excede ? Color.DarkRed : Tema.RosaOscuro;
            }

            btnConfirmar.Enabled = seleccionadas > 0 && !excede;
            btnDesistir.Visible  = excede;
            btnDesistir.Enabled  = excede;
            btnImprimirCupo.Visible = excede;

            lblResumen.Text = string.IsNullOrEmpty(linea2)
                ? linea1
                : linea1 + "\r\n" + linea2;

            lblDetalle.Text = DetalleSeleccion(seleccion);
        }

        // Detalle de selección (prenda, color, talle, cantidad).
        private string DetalleSeleccion(List<BE.Prenda> seleccion)
        {
            if (seleccion.Count == 0) return string.Empty;
            var grupos = seleccion
                .GroupBy(p => new { p.Nombre, p.Talle, p.Color })
                .Select(g => $"{g.Key.Nombre} — {g.Key.Talle ?? "—"} / {g.Key.Color ?? "—"} ×{g.Count()}");
            return Tr("lbl.ped.detalle", "Detalle de la selección:") + "  " + string.Join("   •   ", grupos);
        }

        private List<BE.Prenda> ObtenerPrendasSeleccionadas()
        {
            var lista = new List<BE.Prenda>();
            foreach (DataGridViewRow row in dgvPrendas.Rows)
            {
                if (row.Cells["Sel"].Value is bool sel && sel)
                {
                    int id = Convert.ToInt32(row.Cells["ID"].Value);
                    var p = _disponibles.Find(pr => pr.IdPrenda == id);
                    if (p != null) lista.Add(p);
                }
            }
            return lista;
        }

        // ── "Enviar selección para control stock" ────────────────────────────

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            var prendas = ObtenerPrendasSeleccionadas();
            if (prendas.Count == 0)
            {
                MostrarError(Tr("err.ped.sinprendas", "Seleccioná al menos una prenda."));
                return;
            }

            string detallesPrendas = string.Join("\n  • ",
                prendas.ConvertAll(p => $"{p.Nombre} ({p.Talle} — {p.Color})"));

            var confirmar = (FormBase.MostrarConfirmacionSiNo(this,
                string.Format(
                    Tr("conf.ped.enviar.msg",
                       "Enviar a control de stock la selección de {0}:\n\n  • {1}\n\nTotal: {2} prenda(s)\n\n" +
                       "Depósito revisará la disponibilidad. Las prendas no quedan reservadas hasta que las separe."),
                    _clienteSel.NombreCompleto,
                    detallesPrendas,
                    prendas.Count),
                Tr("conf.ped.enviar.titulo", "Enviar a control de stock"), porDefectoNo: false) ? DialogResult.Yes : DialogResult.No);

            if (confirmar != DialogResult.Yes) return;

            try
            {
                btnConfirmar.Enabled = false;
                btnConfirmar.Text = Tr("btn.procesando", "Procesando...");
                this.Refresh();

                if (EsAjuste)
                {
                    pedidoBLL.AjustarSeleccion(this.Text, _pedidoAjuste, prendas);
                    IdPedidoCreado = _pedidoAjuste.IdPedido;
                }
                else
                {
                    IdPedidoCreado = pedidoBLL.EnviarAControlStock(this.Text, _clienteSel.IdCliente, prendas);
                }

                // La planilla de control se imprime desde "Documentos ▾" (Control de stock / Pedidos de venta).

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                btnConfirmar.Enabled = true;
                btnConfirmar.Text = EsAjuste
                    ? Tr("btn.ped.reenviarcontrol", "Reenviar a control de stock")
                    : Tr("btn.ped.enviarcontrol",   "Enviar a control de stock");
                MostrarError(ex);
            }
        }

        // ── "Comunicar desistimiento" → "Asentar desistimiento" (exceso de cupo) ─

        private void BtnDesistir_Click(object sender, EventArgs e)
        {
            var prendas = ObtenerPrendasSeleccionadas();
            string motivo;
            using (var dlg = new InputDialog(
                Tr("dlg.desistir.titulo", "Asentar desistimiento"),
                Tr("dlg.desistir.prompt", "Motivo del desistimiento que comunicó el cliente:"),
                false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                motivo = dlg.InputText?.Trim();
            }
            if (string.IsNullOrWhiteSpace(motivo))
            {
                MostrarError(Tr("msg.desistir.req", "El desistimiento requiere un motivo."));
                return;
            }

            try
            {
                if (EsAjuste)
                {
                    // Excede el cupo al ajustar: queda asentada la selección ajustada que el cliente no corrigió.
                    pedidoBLL.AsentarDesistimiento(this.Text, _pedidoAjuste, motivo, BE.EtapaDesistimiento.Cupo, prendas);
                    IdPedidoCreado = _pedidoAjuste.IdPedido;
                }
                else
                {
                    IdPedidoCreado = pedidoBLL.AsentarDesistimiento(this.Text, _clienteSel.IdCliente, prendas, motivo);
                }
                FueDesistimiento = true;
                // El aviso de desistimiento se imprime desde "Documentos ▾" en Pedidos de venta.

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private string EstadoLabel(BE.EstadoPedido estado) => EstadosPedido.Etiqueta(estado);
    }
}
