using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación: Centro de Alertas.
    ///
    /// Lista las alertas operativas que calcula <see cref="BLL.PanelAlertas"/>
    /// (vencimientos, backup, stock, integridad). El formulario NO tiene lógica de
    /// negocio: solo pide las alertas a la BLL, las traduce y las dibuja. Se traduce
    /// en vivo (patrón Observer).
    ///
    /// 1.6.1: Cada alerta se puede marcar como leída (o todas a la vez): deja de contar en «Alertas (N)» del
    /// menú y de listarse mientras su situación no cambie (BLL.AlertasLeidas guarda por usuario qué vio). Si
    /// cambia la cantidad o el valor, vuelve a aparecer como nueva. «Mostrar también las leídas» las lista
    /// atenuadas, con la opción de volver a marcarlas como no leídas.
    /// </summary>
    public partial class AlertasForm : FormBase, IIdiomaObserver
    {
        public AlertasForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
            CargarAlertas();
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarAlertas();
        }

        private void Traducir(Idioma idioma)
        {
            Text           = Tr("frm.alertas", "Centro de Alertas");
            lblTitulo.Text = Tr("frm.alertas", "Centro de Alertas");
            btnActualizar.Text = Tr("btn.actualizar", "Actualizar");
            chkMostrarLeidas.Text = Tr("alert.mostrarleidas", "Mostrar también las leídas");
            btnMarcarTodas.Text = Tr("alert.marcartodas", "Marcar todas como leídas");
        }

        private void BtnActualizar_Click(object sender, EventArgs e) => CargarAlertas();

        private void ChkMostrarLeidas_CheckedChanged(object sender, EventArgs e) => CargarAlertas();

        private System.Collections.Generic.List<BE.Alerta> _alertas = new System.Collections.Generic.List<BE.Alerta>();
        private readonly BLL.AlertasLeidas _leidas = new BLL.AlertasLeidas();
        private BE.Usuario _usuario;

        private void BtnMarcarTodas_Click(object sender, EventArgs e) =>
            Marcar(() => _leidas.MarcarLeidas(_usuario.Id, _alertas.Where(a => !a.Leida).ToList()));

        // Marca (o desmarca) y redibuja; el aviso del menú se recalcula con las no leídas.
        private void Marcar(Action accion)
        {
            if (_usuario == null) return;
            try { accion(); }
            catch (Exception ex) { MostrarErrorEnTarjeta(ex); return; }
            CargarAlertas();
            (MdiParent as Menu)?.ActualizarBadgeAlertas();
        }

        // Pide las alertas a la BLL (con la marca de leída), las traduce y las pinta. Sin lógica de negocio acá.
        private void CargarAlertas()
        {
            flow.SuspendLayout();
            flow.Controls.Clear();

            // Solo las alertas que el rol del usuario puede atender (filtro por patentes en BLL).
            try
            {
                _usuario = new BLL.Usuario().ObtenerUsuarioActivo();
                _alertas = new BLL.PanelAlertas().ObtenerAlertasConLectura(_usuario, _leidas);
            }
            catch (Exception ex)
            {
                _alertas = new System.Collections.Generic.List<BE.Alerta>();
                MostrarErrorEnTarjeta(ex);
                btnMarcarTodas.Enabled = false;
                flow.ResumeLayout();
                return;
            }

            int noLeidas = _alertas.Count(a => !a.Leida), leidas = _alertas.Count - noLeidas;
            btnMarcarTodas.Enabled = noLeidas > 0;
            var visibles = _alertas.Where(a => chkMostrarLeidas.Checked || !a.Leida).ToList();

            if (visibles.Count == 0)
            {
                string texto = _alertas.Count == 0
                    ? Tr("alert.sinalertas", "No hay alertas activas. Todo en orden.")
                    : Tr("alert.todasleidas", "No hay alertas nuevas. {0} alerta(s) leída(s): marque «Mostrar también las leídas» para verlas.", new object[] { leidas });
                var ok = new Label
                {
                    Text        = texto,
                    Font        = new Font("Segoe UI", 10F),
                    ForeColor   = Tema.Exito,
                    AutoSize    = true,
                    MaximumSize = new Size(Math.Max(300, flow.ClientSize.Width - 40), 0),
                    Margin      = new Padding(6, 10, 6, 6)
                };
                flow.Controls.Add(ok);
                flow.ResumeLayout();
                return;
            }

            foreach (var a in visibles)
            {
                string texto = Tr(a.ClaveI18n, a.MensajeFallback);
                if (a.Parametros != null && a.Parametros.Length > 0)
                {
                    try { texto = string.Format(texto, a.Parametros); } catch { }
                }
                flow.Controls.Add(CrearFila(a.Nivel, texto, a));
            }

            flow.ResumeLayout();
        }

        // Excepción inesperada: se registra en bitácora (detalle técnico) y se muestra solo un mensaje
        // genérico traducido en una tarjeta (sin ex.Message crudo).
        private void MostrarErrorEnTarjeta(Exception ex)
        {
            try
            {
                new BLL.Bitacora().RegistrarSinSesion(nameof(AlertasForm),
                    "Excepción: " + ex.GetType().Name, BE.Criticidad.Alta,
                    detalle: $"{nameof(AlertasForm)}: {ex.Message}");
            }
            catch { /* el fallo al registrar no debe romper el manejo del error */ }

            string generico = Tr("msg.error.inesperado",
                "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.");
            flow.Controls.Add(CrearFila(BE.NivelAlerta.Critica, generico, null));
        }

        // Tarjeta de una alerta: barra de color por severidad + texto + «Marcar como leída» (o «no leída»).
        // Una alerta leída se dibuja atenuada.
        private Panel CrearFila(BE.NivelAlerta nivel, string texto, BE.Alerta alerta)
        {
            Color barra, fondo, tinta;
            switch (nivel)
            {
                case BE.NivelAlerta.Critica:
                    barra = Tema.RosaOscuro; fondo = Tema.RosaPalido;
                    tinta = Tema.RosaTinta;
                    break;
                case BE.NivelAlerta.Advertencia:
                    barra = Tema.Alerta; fondo = Tema.FondoAlerta;
                    tinta = Tema.Alerta;
                    break;
                default:
                    barra = Tema.Info; fondo = Tema.FondoInfo;
                    tinta = Tema.Info;
                    break;
            }
            bool leida = alerta != null && alerta.Leida;
            if (leida) { barra = Tema.Borde; fondo = Tema.Papel; tinta = Tema.TextoMuted; }

            int ancho = flow.ClientSize.Width - flow.Padding.Horizontal - 24;
            if (ancho < 200) ancho = 480;

            var card = new Panel
            {
                Size      = new Size(ancho, 56),
                BackColor = fondo,
                Margin    = new Padding(2, 4, 2, 4)
            };
            var franja = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = barra };
            var lbl = new Label
            {
                Text      = leida ? texto + "  (" + Tr("alert.leida", "leída") + ")" : texto,
                Dock      = DockStyle.Fill,
                Padding   = new Padding(10, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 9.5F),
                ForeColor = tinta
            };
            card.Controls.Add(lbl);
            if (alerta != null)
            {
                var marcar = new Button
                {
                    Text      = leida ? Tr("alert.marcarnoleida", "Marcar como no leída") : Tr("alert.marcarleida", "Marcar como leída"),
                    Dock      = DockStyle.Right,
                    Width     = 150,
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI", 9F),
                    ForeColor = tinta,
                    BackColor = fondo,
                    Cursor    = Cursors.Hand,
                    TabStop   = true
                };
                marcar.FlatAppearance.BorderColor = barra;
                marcar.Click += (s, e) => Marcar(() =>
                {
                    if (alerta.Leida) _leidas.MarcarNoLeida(_usuario.Id, alerta);
                    else _leidas.MarcarLeidas(_usuario.Id, new[] { alerta });
                });
                card.Controls.Add(marcar);
            }
            card.Controls.Add(franja);
            return card;
        }
    }
}
