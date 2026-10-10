using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Panel de Control — se abre automáticamente al iniciar sesión como hijo MDI.
    ///
    /// Muestra solo las métricas a las que el usuario tiene permiso:
    ///   · Prendas disponibles  (requiere mnuPrendas)
    ///   · Clientes registrados (requiere mnuClientes)
    ///   · Pedidos pendientes   (requiere mnuPedidosVenta o mnuPedidosRealizados)
    ///   · Días sin backup      (requiere mnuUsuarios — solo Administrador)
    ///
    /// La tarjeta de Backup cambia de color según la antigüedad y muestra un aviso
    /// cuando se supera el umbral configurado (recordatorio.cfg en carpeta Backups).
    /// El botón permite configurar el intervalo de recordatorio.
    ///
    /// Implementa IIdiomaObserver: las etiquetas se traducen al cambiar el idioma.
    /// </summary>
    public partial class DashboardForm : FormBase, IIdiomaObserver
    {

        // ── Dependencias BLL ──────────────────────────────────────────────────
        private readonly BLL.Interfaces.IPrendaService  _bllPrenda   = new BLL.Prenda();
        private readonly BLL.Interfaces.IClienteService _bllCliente  = new BLL.Cliente();
        private readonly BLL.Interfaces.IPedidoService  _bllPedido   = new BLL.Pedido();
        private readonly BLL.Usuario  _bllUsuario  = new BLL.Usuario();
        private readonly BLL.Bitacora _bllBitacora = new BLL.Bitacora();

        // ── Visibilidad por rol ───────────────────────────────────────────────
        private readonly bool _verPrendas, _verClientes, _verPedidos, _verBackup, _verStock;
        // Granulares, para saber a CUÁL de las dos pantallas de pedidos navegar al hacer clic
        // en una fila de "Pedido" en Tareas Pendientes — _verPedidos por sí solo no alcanza,
        // porque se activa con cualquiera de las dos patentes, no necesariamente con ambas.
        private readonly bool _tienePedidosVenta, _tienePedidosRealizados;
        // Actividad reciente = bitácora del sistema (dato sensible de auditoría): solo se muestra
        // a quien tiene permiso de ver la auditoría (Administrador / Auditor), no a roles operativos.
        private readonly bool _verActividad;
        // Tareas de los roles sin panel propio (Caja, Administración Comercial, Contabilidad): antes
        // entraban a un panel vacío. El Administrador ya ve todo el resto del tablero.
        private readonly bool _verCaja, _verPromoAdmin, _verPromoContable;
        private Label _numContrCobrar, _txtContrCobrar, _numRenovCobrar, _txtRenovCobrar;
        private Label _numSugerencias, _txtSugerencias, _numBajasPromo, _txtBajasPromo;
        private Label _numRevContable, _txtRevContable;

        // ── Controles condicionados por PERMISOS (null si el rol no tiene acceso) ──────────
        // No son parte del Diseñador: su EXISTENCIA (no solo su visibilidad) depende de los
        // permisos del usuario logueado, así que se construyen en runtime desde
        // ConstruirElementosCondicionales() — ver ese método más abajo.
        private Label _numPrendas,  _txtPrendas;
        private Label _numClientes, _txtClientes;
        private Label _numPedidos,  _txtPedidos;
        private Label _numBackup,   _txtBackup;
        private Label _numOcupacion, _txtOcupacion;
        private Panel _cardBackupPanel;

        // Único botón de solo símbolo ("...") de las 16 pantallas de Dashboards/Análisis/Reportes/Historiales
        // sin tooltip — se crea en código (btnConfig no viene del Designer), así que el ToolTip
        // también se instancia acá.
        private readonly ToolTip _tipConfig = new ToolTip();

        private Panel        _panelActividad;
        private Label        _lblActTitulo;
        private DataGridView _dgvActividad;

        // ── Auto-refresh timer ────────────────────────────────────────────────
        private System.Windows.Forms.Timer _timer;

        // Se reasignaban con `new Font(...)` en cada refresco (timer de 2 min) sin liberar el
        // anterior — Font implementa IDisposable y envuelve un handle GDI; creadas una sola vez
        // acá y reutilizadas evita esa fuga en sesiones largas.
        private readonly Font _fontBackupChico  = new Font("Segoe UI", 20f, FontStyle.Bold);
        private readonly Font _fontBackupGrande = new Font("Segoe UI", 36f, FontStyle.Bold);

        public DashboardForm(List<BE.Permiso> permisos)
        {
            // Mismo criterio que BLL.MenuVisibilidad: NombreMenu sin distinguir mayúsculas y
            // bypass total para el Administrador (antes un admin cuyas patentes no estuvieran
            // todas asignadas veía un panel incompleto).
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (permisos != null)
                foreach (var p in permisos)
                    if (p?.NombreMenu != null) nombres.Add(p.NombreMenu);
            bool esAdmin = false;
            try { esAdmin = new BLL.Usuario().ObtenerUsuarioActivo()?.EsAdministrador == true; } catch { }
            bool Tiene(string patente) => esAdmin || nombres.Contains(patente);

            _verPrendas  = Tiene(BE.Patentes.Prendas);
            _verClientes = Tiene(BE.Patentes.Clientes);
            _verPedidos  = Tiene(BE.Patentes.PedidosVenta) || Tiene(BE.Patentes.PedidosRealizados);
            _verBackup   = Tiene(BE.Patentes.Usuarios);
            _verStock    = Tiene(BE.Patentes.Stock);
            _verActividad = Tiene(BE.Patentes.Auditoria);
            _tienePedidosVenta      = Tiene(BE.Patentes.PedidosVenta);
            _tienePedidosRealizados = Tiene(BE.Patentes.PedidosRealizados);
            _verCaja          = !esAdmin && nombres.Contains(BE.Patentes.Caja);
            _verPromoAdmin    = !esAdmin && nombres.Contains(BE.Patentes.PromocionesAdmin);
            _verPromoContable = !esAdmin && nombres.Contains(BE.Patentes.PromocionesContable);

            InitializeComponent();

            ConstruirElementosCondicionales();
        }

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);   // FormBase: ícono + tema/fuente del usuario + seguridad de controles
            Traducir(GestorIdioma.IdiomaActual);
            // Cargar datos en background — el form aparece inmediatamente
            ActualizarMetricas();
            CargarActividadReciente();
            CargarMiniStats();
            CargarTareasPendientes();
            _timer = new System.Windows.Forms.Timer { Interval = 2 * 60 * 1000 };
            _timer.Tick += (s, ev) => { ActualizarMetricas(); CargarActividadReciente(); CargarMiniStats(); CargarTareasPendientes(); };
            _timer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            base.OnFormClosing(e);
        }

        // ── IIdiomaObserver ───────────────────────────────────────────────────

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            ActualizarMetricas();
            CargarActividadReciente();
            CargarMiniStats();
            CargarTareasPendientes();
        }

        private void Traducir(Idioma idioma)
        {
            this.Text          = Tr("frm.dashboard",      "Panel de Control");
            lblTitulo.Text     = Tr("frm.dashboard",      "Panel de Control");
            lblSub.Text        = Tr("dash.general.subtitulo", "WardrobeFlow");
            btnRefrescar.Text  = Tr("dash.btn.refrescar", "Actualizar");

            if (_txtPrendas   != null) _txtPrendas.Text   = Tr("dash.prendas",    "Prendas\ndisponibles");
            if (_txtClientes  != null) _txtClientes.Text  = Tr("dash.clientes",   "Clientes\nregistrados");
            if (_txtPedidos   != null) _txtPedidos.Text   = Tr("dash.pedidos",    "Pedidos\npendientes");
            if (_txtBackup    != null) _txtBackup.Text    = Tr("dash.backup",     "días sin\nbackup");
            if (_txtOcupacion != null) _txtOcupacion.Text = Tr("dash.ocupacion",  "ocupación\ndel stock");
            if (_txtContrCobrar != null) _txtContrCobrar.Text = Tr("dash.caja.contrataciones", "contrataciones\npor cobrar");
            if (_txtRenovCobrar != null) _txtRenovCobrar.Text = Tr("dash.caja.renovaciones",   "renovaciones\npor cobrar");
            if (_txtSugerencias != null) _txtSugerencias.Text = Tr("dash.aco.sugerencias",     "sugerencias\npendientes");
            if (_txtBajasPromo  != null) _txtBajasPromo.Text  = Tr("dash.aco.bajas",           "bajas de promo\nsolicitadas");
            if (_txtRevContable != null) _txtRevContable.Text = Tr("dash.con.revision",        "promociones\nen revisión");

            if (_lblActTitulo != null) _lblActTitulo.Text = Tr("dash.actividad.titulo", "Actividad reciente");
            lblStTitulo.Text  = Tr("dash.stats.titulo",     "Resumen de eventos");
            if (_dgvActividad != null && _dgvActividad.Columns.Count >= 3)
            {
                _dgvActividad.Columns["colFecha"].HeaderText = Tr("dash.col.fecha",   "Fecha");
                _dgvActividad.Columns["colTipo"].HeaderText  = Tr("dash.col.evento",  "Evento");
                _dgvActividad.Columns["colUser"].HeaderText  = Tr("dash.col.usuario", "Usuario");
            }

            // Panel "Mis Tareas Pendientes": título y encabezados del grid (antes quedaban
            // siempre en español porque se fijaban una sola vez al construir la UI).
            lblTareasTitulo.Text = string.Format(Tr("dash.tareas.titulo", "Mis Tareas Pendientes ({0})"), 0);
            if (dgvTareas.Columns.Count >= 3)
            {
                dgvTareas.Columns["colTipo"].HeaderText  = Tr("dash.tareas.col.tipo",  "Tipo");
                dgvTareas.Columns["colDesc"].HeaderText  = Tr("dash.tareas.col.desc",  "Descripción");
                dgvTareas.Columns["colFecha"].HeaderText = Tr("dash.tareas.col.fecha", "Desde");
            }
        }

        // ── Métricas ──────────────────────────────────────────────────────────

        // Un widget que no pudo cargar deja su tarjeta en "—" (no rompe el dashboard), pero
        // se deja constancia acá para poder distinguir "dato en cero" de "falló la consulta".
        private static void LogWidget(string widget, Exception ex)
            => System.Diagnostics.Trace.TraceWarning($"[DashboardForm] No se pudo cargar '{widget}': {ex.Message}");

        private void ActualizarMetricas()
        {
            // Backup es I/O de archivo (rápido, sin BD) — se actualiza al instante
            if (_verBackup && _numBackup != null)
                ActualizarTarjetaBackup();

            Task.Run(() =>
            {
                int? nPrendas = null, nClientes = null, nPedidos = null;
                BE.OcupacionStock ocup = null;
                BE.Usuario usuario = null;
                DateTime? hora = null;

                if (_verPrendas)  try { nPrendas  = _bllPrenda.ObtenerDisponibles().Count; } catch (Exception ex) { LogWidget("prendas disponibles", ex); }
                if (_verClientes) try { nClientes = _bllCliente.ObtenerTodos().Count; } catch (Exception ex) { LogWidget("clientes", ex); }
                if (_verPedidos)  try { nPedidos  = _bllPedido.ObtenerPendientes().Count; } catch (Exception ex) { LogWidget("pedidos pendientes", ex); }
                if (_verPrendas)  try { ocup      = _bllPrenda.ObtenerOcupacion(); } catch (Exception ex) { LogWidget("ocupación de stock", ex); }
                try { usuario = _bllUsuario.ObtenerUsuarioActivo(); hora = _bllUsuario.ObtenerFechaInicioSesion(); } catch (Exception ex) { LogWidget("usuario activo", ex); }

                // Tareas de Caja, Administración Comercial y Contabilidad.
                int? nContr = null, nRenov = null, nSug = null, nBajas = null, nRev = null;
                if (_verCaja)
                {
                    try { nContr = new BLL.Contratacion().ContarPendientesDePago(); } catch (Exception ex) { LogWidget("contrataciones por cobrar", ex); }
                    try { nRenov = new BLL.Cobro().ObtenerElegibles().Count; } catch (Exception ex) { LogWidget("renovaciones por cobrar", ex); }
                }
                if (_verPromoAdmin)
                {
                    try { nSug = new BLL.SugerenciaPromocion().ObtenerPendientes().Count; } catch (Exception ex) { LogWidget("sugerencias pendientes", ex); }
                    try { nBajas = new BLL.Promocion().ObtenerTodas().Count(p => p.Estado == BE.EstadoPromocion.BajaSolicitada); } catch (Exception ex) { LogWidget("bajas de promociones", ex); }
                }
                if (_verPromoContable)
                    try { nRev = new BLL.Promocion().ObtenerPendientesRevisionContable().Count; } catch (Exception ex) { LogWidget("promociones en revisión", ex); }

                InvocarSeguro(() =>
                {
                    if (IsDisposed) return;
                    string N(int? n) => n.HasValue ? n.Value.ToString() : "—";
                    if (_numContrCobrar != null) _numContrCobrar.Text = N(nContr);
                    if (_numRenovCobrar != null) _numRenovCobrar.Text = N(nRenov);
                    if (_numSugerencias != null) _numSugerencias.Text = N(nSug);
                    if (_numBajasPromo  != null) _numBajasPromo.Text  = N(nBajas);
                    if (_numRevContable != null) _numRevContable.Text = N(nRev);
                    if (_numPrendas  != null) _numPrendas.Text  = nPrendas.HasValue  ? nPrendas.Value.ToString()  : "—";
                    if (_numClientes != null) _numClientes.Text = nClientes.HasValue ? nClientes.Value.ToString() : "—";
                    if (_numPedidos  != null) _numPedidos.Text  = nPedidos.HasValue  ? nPedidos.Value.ToString()  : "—";
                    if (ocup != null) ActualizarTarjetaOcupacion(ocup);
                    if (usuario != null)
                        lblSesion.Text =
                            $"{usuario.Username}  ·  {usuario.Perfil ?? "—"}" +
                            (hora.HasValue ? $"  ·  {Tr("dash.sesion.iniciada", "Sesión iniciada:")} {hora.Value:HH:mm}" : "");
                });
            });
        }

        private void ActualizarTarjetaBackup()
        {
            try
            {
                // Incluye los backups cifrados (.wfbak) y los .bak legacy (lo resuelve la BLL).
                FileInfo ultimo = BLL.Backup.ObtenerUltimoBackup();

                int umbral = BLL.Configuracion.ObtenerDiasRecordatorio();

                if (ultimo == null)
                {
                    _numBackup.Text              = "!";
                    _numBackup.Font              = _fontBackupGrande;
                    _cardBackupPanel.BackColor   = Tema.FondoError;
                    _numBackup.ForeColor         = Tema.Error;
                    _txtBackup.ForeColor         = Tema.Error;
                    _cardBackupPanel.Invalidate();
                    MostrarAviso(Tr("dash.aviso.sinbackup", "Sin backups. Generá uno desde Administrar → Backup."), Tema.Error);
                    return;
                }

                int dias = (int)(DateTime.Now - ultimo.LastWriteTime).TotalDays;

                // Número grande: días transcurridos (o "Hoy")
                if (dias == 0)
                {
                    _numBackup.Text = Tr("dash.backup.hoy", "Hoy");
                    _numBackup.Font = _fontBackupChico;
                }
                else
                {
                    _numBackup.Text = dias.ToString();
                    _numBackup.Font = _fontBackupGrande;
                }

                // Código de color: verde → amarillo → rojo según antigüedad vs umbral
                Color fondo, tinta;
                if (dias <= umbral / 2)
                {
                    fondo = Tema.FondoExito;   // verde
                    tinta = Tema.Exito;
                }
                else if (dias <= umbral)
                {
                    fondo = Tema.FondoAlerta;   // amarillo
                    tinta = Tema.Alerta;
                }
                else
                {
                    fondo = Tema.FondoError;   // rojo
                    tinta = Tema.Error;
                }

                _cardBackupPanel.BackColor = fondo;
                _numBackup.ForeColor       = tinta;
                _txtBackup.ForeColor       = tinta;
                _cardBackupPanel.Invalidate();

                if (dias > umbral)
                    MostrarAviso(
                        string.Format(Tr("dash.aviso.vencido", "Hace {0} día(s) sin backup — recordatorio cada {1} días."), dias, umbral),
                        Tema.Alerta);
                else
                    OcultarAviso();
            }
            catch { if (_numBackup != null) _numBackup.Text = "—"; }
        }

        private void MostrarAviso(string msg, Color color)
        {
            lblAviso.Text      = msg;
            lblAviso.ForeColor = color;
            lblAviso.Height    = 24;
            lblAviso.Visible   = true;
        }

        private void OcultarAviso()
        {
            lblAviso.Visible = false;
            lblAviso.Height  = 0;
        }

        // ── Ocupación del stock ───────────────────────────────────────────────

        private void ActualizarTarjetaOcupacion(BE.OcupacionStock oc)
        {
            if (_numOcupacion == null) return;
            _numOcupacion.Text = $"{oc.PorcentajeOcupacion}%";
            _numOcupacion.Font = new System.Drawing.Font("Segoe UI", 28f, System.Drawing.FontStyle.Bold);

            System.Drawing.Color fondo, tinta;
            if (oc.PorcentajeOcupacion < 70)
            { fondo = Tema.FondoExito; tinta = Tema.Exito; }
            else if (oc.PorcentajeOcupacion <= 90)
            { fondo = Tema.FondoAlerta; tinta = Tema.Alerta; }
            else
            { fondo = Tema.FondoError; tinta = Tema.Error; }

            if (_txtOcupacion != null)
            {
                _txtOcupacion.Text = string.Format(
                    Tr("dash.ocupacion.detalle", "{0} en uso · {1} libres"),
                    oc.EnUso, oc.Disponibles);
                _txtOcupacion.ForeColor = tinta;
            }
            _numOcupacion.ForeColor = tinta;
            var card = _numOcupacion.Parent;
            if (card != null) card.BackColor = fondo;
        }

        // ── Recordatorio: config en archivo ──────────────────────────────────

        private void ConfigurarRecordatorio()
        {
            int actual = BLL.Configuracion.ObtenerDiasRecordatorio();

            using (var dlg = new Form())
            {
                dlg.Text            = Tr("dash.cfg.titulo", "Recordatorio de Backup");
                dlg.ClientSize      = new Size(300, 150);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.BackColor       = Color.White;

                var lbl = new Label
                {
                    Text     = Tr("dash.cfg.recada", "Recordarme cada:"),
                    Left     = 16, Top = 22, Width = 268, Height = 20,
                    Font     = new Font("Segoe UI", 9f)
                };

                var spn = new NumericUpDown
                {
                    Left    = 16, Top = 48, Width = 80, Height = 28,
                    Minimum = 1, Maximum = 365, Value = actual,
                    Font    = new Font("Segoe UI", 10f)
                };

                var lblDias = new Label
                {
                    Text = Tr("dash.cfg.dias", "días"), Left = 104, Top = 52, Width = 60, Height = 20,
                    Font = new Font("Segoe UI", 9f)
                };

                var btnOk = new Button
                {
                    Text = Tr("dash.cfg.guardar", "Guardar"), Left = 80, Top = 104, Width = 90, Height = 30,
                    DialogResult = DialogResult.OK,
                    BackColor    = Tema.RosaOscuro,
                    ForeColor    = Color.White, FlatStyle = FlatStyle.Flat
                };
                btnOk.FlatAppearance.BorderSize = 0;

                var btnCancelar = new Button
                {
                    Text = Tr("btn.cancelar", "Cancelar"), Left = 184, Top = 104, Width = 100, Height = 30,
                    DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat
                };

                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancelar;
                dlg.Controls.AddRange(new Control[] { lbl, spn, lblDias, btnOk, btnCancelar });

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    BLL.Configuracion.GuardarDiasRecordatorio((int)spn.Value);
                    ActualizarMetricas();
                }
            }
        }

        // ── Handlers de eventos estáticos (wireados desde el Diseñador) ─────────

        private void PanelHeader_Paint(object sender, PaintEventArgs pe)
        {
            using (var br = new LinearGradientBrush(
                panelHeader.ClientRectangle,
                Tema.RosaPrimario,
                Tema.RosaOscuro,
                LinearGradientMode.Horizontal))
                pe.Graphics.FillRectangle(br, panelHeader.ClientRectangle);
        }

        private void PanelHeader_Resize(object sender, EventArgs e) => btnRefrescar.Left = panelHeader.Width - 112;

        private void BtnRefrescar_Click(object sender, EventArgs e)
        {
            ActualizarMetricas();
            CargarActividadReciente();
            CargarMiniStats();
            CargarTareasPendientes();
        }

        private void FlowCards_Resize(object sender, EventArgs e)
        {
            int count = flowCards.Controls.Count;
            if (count > 0)
            {
                int avail = flowCards.ClientSize.Width - flowCards.Padding.Horizontal - count * 8;
                int cardW = Math.Max(100, avail / count);
                foreach (Control card in flowCards.Controls)
                    card.Width = cardW;
            }
        }

        private void PanelCentro_Resize(object sender, EventArgs e) => AjustarAnchuras();

        // Ajusta el ancho del panel de Actividad Reciente al redimensionar (solo si existe:
        // depende del permiso de auditoría).
        private void AjustarAnchuras()
        {
            if (_panelActividad == null) return;
            int w = panelCentro.ClientSize.Width;
            _panelActividad.Width = (int)(w * 0.55);
        }

        // ── Construcción de elementos condicionados por PERMISOS ────────────────
        // Se arman después de InitializeComponent porque su EXISTENCIA (no solo su
        // visibilidad) depende de los permisos del usuario logueado: tarjetas KPI,
        // panel de Actividad Reciente, y la visibilidad del panel de Tareas Pendientes.
        private void ConstruirElementosCondicionales()
        {
            if (_verCaja)
            {
                flowCards.Controls.Add(CrearTarjeta(Tema.FondoInfo, Tema.Info, out _numContrCobrar, out _txtContrCobrar, out _));
                flowCards.Controls.Add(CrearTarjeta(Tema.FondoAlerta, Tema.Alerta, out _numRenovCobrar, out _txtRenovCobrar, out _));
            }
            if (_verPromoAdmin)
            {
                flowCards.Controls.Add(CrearTarjeta(Tema.FondoInfo, Tema.Info, out _numSugerencias, out _txtSugerencias, out _));
                flowCards.Controls.Add(CrearTarjeta(Tema.FondoAlerta, Tema.Alerta, out _numBajasPromo, out _txtBajasPromo, out _));
            }
            if (_verPromoContable)
                flowCards.Controls.Add(CrearTarjeta(Tema.FondoInfo, Tema.Info, out _numRevContable, out _txtRevContable, out _));

            if (_verPrendas)
                flowCards.Controls.Add(CrearTarjeta(
                    Tema.RosaPalido, Tema.RosaTinta,
                    out _numPrendas, out _txtPrendas, out _));

            if (_verClientes)
                flowCards.Controls.Add(CrearTarjeta(
                    Tema.RosaClara, Tema.RosaTinta,
                    out _numClientes, out _txtClientes, out _));

            if (_verPedidos)
                flowCards.Controls.Add(CrearTarjeta(
                    Tema.Borde, Tema.RosaOscuro,
                    out _numPedidos, out _txtPedidos, out _));

            if (_verBackup)
            {
                var tarjeta = CrearTarjeta(
                    Tema.FondoExito, Tema.Exito,
                    out _numBackup, out _txtBackup, out _cardBackupPanel);
                var btnConfig = new Button
                {
                    Text      = "...",
                    Font      = new Font("Segoe UI", 9f),
                    Size      = new Size(22, 22),
                    Location  = new Point(tarjeta.Width - 26, 4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Tema.Exito,
                    Cursor    = Cursors.Hand,
                    TabStop   = false,
                    Anchor    = AnchorStyles.Top | AnchorStyles.Right
                };
                btnConfig.FlatAppearance.BorderSize = 0;
                _tipConfig.SetToolTip(btnConfig, Tr("tip.config.backup", "Configurar recordatorio de backup"));
                btnConfig.Click += (s, e) => ConfigurarRecordatorio();
                tarjeta.Controls.Add(btnConfig);
                btnConfig.BringToFront();
                flowCards.Controls.Add(tarjeta);
            }

            // ── Tarjeta de ocupación del stock ────────────────────────────────
            if (_verPrendas)
                flowCards.Controls.Add(CrearTarjeta(
                    Tema.FondoExito, Tema.Exito,
                    out _numOcupacion, out _txtOcupacion, out _));

            // ── Panel Actividad Reciente ──────────────────────────────────────
            // Solo para quien puede ver la auditoría (Administrador / Auditor). Los roles
            // operativos (Operador, Vendedor, etc.) no ven la bitácora en su dashboard.
            if (_verActividad)
            {
                _panelActividad = new Panel
                {
                    Dock      = DockStyle.Left,
                    Width     = 0,      // se calcula en Resize
                    BackColor = Color.White,
                    Padding   = new Padding(0)
                };

                _lblActTitulo = new Label
                {
                    Text      = "Actividad reciente",
                    Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Tema.RosaOscuro,
                    Dock      = DockStyle.Top,
                    Height    = 28,
                    Padding   = new Padding(10, 6, 0, 0),
                    BackColor = Tema.RosaMuyClara
                };

                _dgvActividad = new DataGridView
                {
                    Name                        = "dgvActividad",
                    Dock                        = DockStyle.Fill,
                    BackgroundColor             = Color.White,
                    BorderStyle                 = BorderStyle.None,
                    RowHeadersVisible           = false,
                    AllowUserToAddRows          = false,
                    AllowUserToResizeRows       = false,
                    AllowUserToResizeColumns    = false,
                    ReadOnly                    = true,
                    SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                    EnableHeadersVisualStyles   = false,
                    Font                        = new Font("Segoe UI", 8f),
                    AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill,
                    CellBorderStyle             = DataGridViewCellBorderStyle.SingleHorizontal,
                    GridColor                   = Tema.Borde
                };
                _dgvActividad.ColumnHeadersDefaultCellStyle.BackColor = Tema.RosaOscuro;
                _dgvActividad.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                _dgvActividad.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8f, FontStyle.Bold);
                _dgvActividad.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha", HeaderText = "Fecha",   FillWeight = 28 });
                _dgvActividad.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTipo",  HeaderText = "Evento",  FillWeight = 36 });
                _dgvActividad.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser",  HeaderText = "Usuario", FillWeight = 36 });

                _panelActividad.Controls.Add(_dgvActividad);
                _panelActividad.Controls.Add(_lblActTitulo);
                _panelActividad.Tag = _dgvActividad;

                panelCentro.Controls.Add(_panelActividad);
            }

            // ── Panel Tareas Pendientes: visibilidad según permisos ────────────
            bool hayTareas = _verPedidos || _verStock;
            panelTareas.Height  = hayTareas ? 140 : 0;
            panelTareas.Visible = hayTareas;
        }

        private void CargarTareasPendientes()
        {
            if (!panelTareas.Visible) return;

            Task.Run(() =>
            {
                List<BE.MantenimientoPrenda> enMant  = null;
                List<BE.Pedido>              pedPend = null;

                if (_verStock)   try { enMant  = _bllPrenda.ObtenerEnMantenimiento(); } catch (Exception ex) { System.Diagnostics.Trace.TraceError("[DashboardForm.CargarTareasPendientes] " + ex.Message); }
                if (_verPedidos) try { pedPend = _bllPedido.ObtenerPendientes(); }       catch (Exception ex) { System.Diagnostics.Trace.TraceError("[DashboardForm.CargarTareasPendientes] " + ex.Message); }

                InvocarSeguro(() =>
                {
                    if (IsDisposed || !panelTareas.Visible) return;
                    dgvTareas.Rows.Clear();

                    string tipoMant   = Tr("dash.tarea.mantenimiento", "Mantenimiento");
                    string tipoPedido = Tr("dash.tarea.pedido",        "Pedido");

                    if (enMant != null)
                        foreach (var m in enMant)
                        {
                            int dias = m.DiasTranscurridos;
                            string desde = dias == 0 ? Tr("dash.hoy", "hoy") : string.Format(Tr("dash.hace_dias", "hace {0}d"), dias);
                            var fila = dgvTareas.Rows[dgvTareas.Rows.Add(tipoMant, m.NombrePrenda, desde)];
                            fila.DefaultCellStyle.ForeColor = m.NivelUrgencia == BE.NivelUrgencia.Reciente
                                ? Tema.Exito : Tema.Alerta;
                            fila.Tag = "mant";
                        }

                    if (pedPend != null)
                        foreach (var p in pedPend)
                        {
                            int dias = p.DiasDesdeAlta;
                            string desde = dias == 0 ? Tr("dash.hoy", "hoy") : string.Format(Tr("dash.hace_dias", "hace {0}d"), dias);
                            string desc  = $"#{p.IdPedido} — {p.NombreCliente ?? $"Cliente {p.IdCliente}"}";
                            var fila = dgvTareas.Rows[dgvTareas.Rows.Add(tipoPedido, desc, desde)];
                            fila.DefaultCellStyle.ForeColor = p.EsUrgentePorAntiguedad ? Tema.Error : Tema.Alerta;
                            fila.Tag = "pedido";
                        }

                    if (dgvTareas.Rows.Count == 0)
                    {
                        dgvTareas.Rows.Add("—", Tr("dash.tareas.sinpendientes", "Sin tareas pendientes"), "—");
                        dgvTareas.Rows[0].DefaultCellStyle.ForeColor = Color.Gray;
                    }

                    lblTareasTitulo.Text = string.Format(
                        Tr("dash.tareas.titulo", "Mis Tareas Pendientes ({0})"),
                        dgvTareas.Rows.Count == 1 && dgvTareas.Rows[0].Cells["colTipo"].Value?.ToString() == "—" ? 0 : dgvTareas.Rows.Count);
                });
            });
        }

        // Clic en una fila de "Mis Tareas Pendientes": navega según el TIPO real de la fila
        // (guardado en Tag, no en el texto traducido de la celda) y solo si el usuario tiene
        // el permiso de la pantalla destino — _verPedidos por sí solo no alcanza, porque se
        // activa con Pedidos de Venta O Pedidos Realizados, no necesariamente con ambas.
        private void DgvTareas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvTareas.Rows.Count) return;
            string tipo = dgvTareas.Rows[e.RowIndex].Tag as string;

            if (tipo == "mant" && _verPrendas)
                AbrirPantalla(() => new Prendas());
            else if (tipo == "pedido")
            {
                if (_tienePedidosVenta)               AbrirPantalla(() => new PedidosVenta());
                else if (_tienePedidosRealizados)      AbrirPantalla(() => new PedidosRealizados());
            }
        }

        // Abre (o enfoca si ya está abierta) la pantalla del tipo T. Genérico porque el mismo
        // patrón de dedup por MdiChildren se repite para cada pantalla destino posible.
        private void AbrirPantalla<T>(Func<T> crear) where T : Form
        {
            var menu = this.MdiParent;
            if (menu == null) return;
            foreach (Form hijo in menu.MdiChildren)
                if (hijo is T existente) { existente.BringToFront(); return; }
            var nueva = crear();
            nueva.MdiParent = menu;
            nueva.Show();
        }

        // El texto de cada actividad se guarda en español en la bitácora (BD). Esta tabla lo
        // traduce al idioma activo para la grilla de "Actividad reciente". Las actividades con
        // parte dinámica (nombre de archivo, usuario, cantidad) traducen solo la parte fija y
        // conservan el dato; las desconocidas se muestran tal cual.
        private string TraducirActividad(string actividad)
        {
            if (string.IsNullOrWhiteSpace(actividad)) return actividad ?? "";
            if (GestorIdioma.IdiomaActual?.Id == "ES") return actividad;   // ya está en español

            switch (actividad)
            {
                case BE.ActividadesBitacora.InicioSesion:                   return Tr("dash.act.login",           actividad);
                case BE.ActividadesBitacora.CierreSesion:                   return Tr("dash.act.logout",          actividad);
                case BE.ActividadesBitacora.CambioContrasenaPropia:         return Tr("dash.act.pwchange",        actividad);
                case BE.ActividadesBitacora.BloqueoDeCuenta:                return Tr("dash.act.accountlock",     actividad);
                case BE.ActividadesBitacora.IntentoFallidoLogin:            return Tr("dash.act.loginfail",       actividad);
                case BE.ActividadesBitacora.BajaLogicaUsuario:              return Tr("dash.act.userdeactivate",  actividad);
                case BE.ActividadesBitacora.CambioDeRolDeUsuario:           return Tr("dash.act.rolechange",      actividad);
                case BE.ActividadesBitacora.DesbloqueoConClaveDeEmergencia: return Tr("dash.act.emergencyunlock", actividad);
                case BE.ActividadesBitacora.ModificacionDeUsuario:          return Tr("dash.act.usermod",         actividad);
                case BE.ActividadesBitacora.PurgaUsuariosArchivados:        return Tr("dash.act.userpurge",       actividad);
                case BE.ActividadesBitacora.ResetContrasena:                return Tr("dash.act.pwreset",         actividad);
                case BE.ActividadesBitacora.SolicitudRecuperacionClave:     return Tr("dash.act.pwrecoveryreq",   actividad);
            }

            // Prefijos con parte dinámica (orden: el más específico primero). Misma fuente
            // (BE.ActividadesBitacora) que usa la BLL al escribir, así un cambio de texto no
            // puede desincronizar la traducción sin que el compilador lo note.
            var prefijos = new[]
            {
                new { Es = BE.ActividadesBitacora.BackupInstalacionLimpiaPrefijo, Key = "dash.act.backupinitial" },
                new { Es = BE.ActividadesBitacora.BackupCifradoGeneradoPrefijo,   Key = "dash.act.backupcreate"  },
                new { Es = BE.ActividadesBitacora.BackupEliminadoPrefijo,         Key = "dash.act.backupdelete"  },
                new { Es = BE.ActividadesBitacora.BaseDeDatosRestauradaPrefijo,   Key = "dash.act.dbrestore"     },
                new { Es = BE.ActividadesBitacora.BackupSinVerificacionPrefijo,   Key = "dash.act.backupnomac"   },
                new { Es = BE.ActividadesBitacora.DesbloqueoDeCuentaPrefijo,      Key = "dash.act.accountunlock" },
                new { Es = BE.ActividadesBitacora.AltaUsuarioPrefijo,             Key = "dash.act.useradd"       },
                new { Es = BE.ActividadesBitacora.RestauracionAVersionPrefijo,    Key = "dash.act.userrestore"   },
            };
            foreach (var p in prefijos)
                if (actividad.StartsWith(p.Es, StringComparison.Ordinal))
                    return string.Format(Tr(p.Key, "{0}"), actividad.Substring(p.Es.Length));

            return actividad;   // actividad desconocida → sin traducir
        }

        private void CargarActividadReciente()
        {
            if (!_verActividad) return;   // sin permiso de auditoría no se construye el panel
            Task.Run(() =>
            {
                System.Data.DataTable dt = null;
                try { dt = _bllBitacora.ObtenerUltimosNDiasSistema(7); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError("[DashboardForm.CargarActividadReciente] " + ex.Message); }

                InvocarSeguro(() =>
                {
                    if (IsDisposed || _dgvActividad == null) return;
                    _dgvActividad.Rows.Clear();
                    if (dt == null) return;
                    int n = 0;
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        if (n >= 8) break;
                        _dgvActividad.Rows.Add(row["fecha"]?.ToString() ?? "", TraducirActividad(row["actividad"]?.ToString() ?? ""), row["usuario"]?.ToString() ?? "");
                        n++;
                    }
                });
            });
        }

        private void CargarMiniStats()
        {
            Task.Run(() =>
            {
                System.Data.DataTable dtN = null, dtNeg = null;
                // La bitácora del SISTEMA solo se pide si el usuario puede verla (la BLL igual lo
                // exige); los roles operativos ven solo los conteos de la bitácora de negocio.
                if (_verActividad)
                    try { dtN = _bllBitacora.ObtenerUltimosNDiasSistema(30); } catch (Exception ex) { LogWidget("bitácora del sistema (30d)", ex); }
                try { dtNeg = _bllBitacora.ObtenerTodosNegocio(); } catch (Exception ex) { LogWidget("bitácora de negocio", ex); }

                InvocarSeguro(() =>
                {
                    if (IsDisposed) return;
                    flStats.Controls.Clear();

                    var tStats = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
                    string lblSistema30d = tStats.ContainsKey("dash.stats.sistema30d") ? tStats["dash.stats.sistema30d"].Texto : "Sistema (30d)";
                    if (_verActividad)
                        flStats.Controls.Add(CrearMiniStatRow(lblSistema30d, (dtN?.Rows.Count ?? 0).ToString(), Tema.RosaOscuro));

                    if (dtNeg != null)
                    {
                        var conteos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        foreach (System.Data.DataRow r in dtNeg.Rows)
                        {
                            string tipo = r["Tipo"]?.ToString() ?? "";
                            if (string.IsNullOrEmpty(tipo)) continue;
                            if (!conteos.ContainsKey(tipo)) conteos[tipo] = 0;
                            conteos[tipo]++;
                        }
                        foreach (var kv in conteos)
                            flStats.Controls.Add(CrearMiniStatRow(kv.Key, kv.Value.ToString(), Tema.RosaOscuro));
                    }
                });
            });
        }

        private static Panel CrearMiniStatRow(string label, string valor, Color color)
        {
            var row = new Panel { Height = 26, Dock = DockStyle.Top, BackColor = Color.Transparent, Width = 200 };
            var lv = new Label { Text = valor, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = color, AutoSize = true, Location = new Point(0, 4) };
            var ll = new Label { Text = label, Font = new Font("Segoe UI", 8f), ForeColor = Tema.TextoMuted, AutoSize = true, Location = new Point(34, 6) };
            row.Controls.Add(lv);
            row.Controls.Add(ll);
            return row;
        }

        private static Panel CrearTarjeta(Color fondo, Color tinta,
            out Label lblNum, out Label lblTxt, out Panel cardRef)
        {
            var card = new Panel
            {
                Width     = 148,
                Height    = 160,
                BackColor = fondo,
                Margin    = new Padding(0, 0, 8, 0)
            };

            card.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10))
                using (var br   = new SolidBrush(card.BackColor))
                    pe.Graphics.FillPath(br, path);
            };

            var num = new Label
            {
                Text      = "…",
                Font      = new Font("Segoe UI", 30f, FontStyle.Bold),
                ForeColor = tinta,
                AutoSize  = false,
                TextAlign = ContentAlignment.BottomCenter,
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location  = new Point(0, 20),
                Height    = 78,
                Width     = card.Width
            };

            var txt = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(
                    Math.Min(tinta.R + 50, 255),
                    Math.Min(tinta.G + 50, 255),
                    Math.Min(tinta.B + 50, 255)),
                AutoSize  = false,
                TextAlign = ContentAlignment.TopCenter,
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location  = new Point(0, 102),
                Height    = 44,
                Width     = card.Width
            };

            card.Resize += (s, e) => { num.Width = card.Width; txt.Width = card.Width; };
            card.Controls.Add(num);
            card.Controls.Add(txt);

            lblNum  = num;
            lblTxt  = txt;
            cardRef = card;
            return card;
        }

        private static GraphicsPath RoundedRect(Rectangle b, int r)
        {
            int d    = r * 2;
            var path = new GraphicsPath();
            path.AddArc(b.X,         b.Y,          d, d, 180, 90);
            path.AddArc(b.Right - d, b.Y,          d, d, 270, 90);
            path.AddArc(b.Right - d, b.Bottom - d, d, d,   0, 90);
            path.AddArc(b.X,         b.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
