using System;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — Centro de Alertas.
    ///
    /// Lista las alertas operativas que calcula <see cref="BLL.PanelAlertas"/>
    /// (vencimientos, backup, stock, integridad). El formulario NO tiene lógica de
    /// negocio: solo pide las alertas a la BLL, las traduce y las dibuja. Se traduce
    /// en vivo (patrón Observer).
    /// </summary>
    public partial class AlertasForm : Form, IIdiomaObserver
    {
        public AlertasForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
            CargarAlertas();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarAlertas();
        }

        private void Traducir(Idioma idioma)
        {
            var t = Traductor.ObtenerTraducciones(idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;
            Text           = T("frm.alertas", "Centro de Alertas");
            lblTitulo.Text = "🔔  " + T("frm.alertas", "Centro de Alertas");
        }

        private void BtnActualizar_Click(object sender, EventArgs e) => CargarAlertas();

        // Pide las alertas a la BLL, las traduce y las pinta. Sin lógica de negocio acá.
        private void CargarAlertas()
        {
            flow.SuspendLayout();
            flow.Controls.Clear();

            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            System.Collections.Generic.List<BE.Alerta> alertas;
            try { alertas = new BLL.PanelAlertas().ObtenerAlertas(); }
            catch (Exception ex)
            {
                flow.Controls.Add(CrearFila(BE.NivelAlerta.Critica, ex.Message));
                flow.ResumeLayout();
                return;
            }

            if (alertas.Count == 0)
            {
                var ok = new Label
                {
                    Text      = "✓  " + T("alert.sinalertas", "No hay alertas activas. Todo en orden."),
                    Font      = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(60, 110, 70),
                    AutoSize  = true,
                    Margin    = new Padding(6, 10, 6, 6)
                };
                flow.Controls.Add(ok);
                flow.ResumeLayout();
                return;
            }

            foreach (var a in alertas)
            {
                string texto = t.ContainsKey(a.ClaveI18n) ? t[a.ClaveI18n].Texto : a.MensajeFallback;
                if (a.Parametros != null && a.Parametros.Length > 0)
                {
                    try { texto = string.Format(texto, a.Parametros); } catch { }
                }
                flow.Controls.Add(CrearFila(a.Nivel, texto));
            }

            flow.ResumeLayout();
        }

        // Tarjeta de una alerta: barra de color por severidad + texto.
        private Panel CrearFila(BE.NivelAlerta nivel, string texto)
        {
            Color barra, fondo, tinta;
            string icono;
            switch (nivel)
            {
                case BE.NivelAlerta.Critica:
                    barra = Color.FromArgb(176, 62, 96); fondo = Color.FromArgb(252, 228, 235);
                    tinta = Color.FromArgb(120, 30, 55); icono = "⛔";
                    break;
                case BE.NivelAlerta.Advertencia:
                    barra = Color.FromArgb(214, 158, 46); fondo = Color.FromArgb(252, 245, 224);
                    tinta = Color.FromArgb(120, 86, 10);  icono = "⚠";
                    break;
                default:
                    barra = Color.FromArgb(90, 120, 170); fondo = Color.FromArgb(232, 238, 248);
                    tinta = Color.FromArgb(45, 65, 105);  icono = "ℹ";
                    break;
            }

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
                Text      = icono + "   " + texto,
                Dock      = DockStyle.Fill,
                Padding   = new Padding(10, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 9.5F),
                ForeColor = tinta
            };
            card.Controls.Add(lbl);
            card.Controls.Add(franja);
            return card;
        }
    }
}
