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
        }

        private void BtnActualizar_Click(object sender, EventArgs e) => CargarAlertas();

        // Pide las alertas a la BLL, las traduce y las pinta. Sin lógica de negocio acá.
        private void CargarAlertas()
        {
            flow.SuspendLayout();
            flow.Controls.Clear();

            System.Collections.Generic.List<BE.Alerta> alertas;
            // Solo las alertas que el rol del usuario puede atender (filtro por patentes en BLL).
            try { alertas = new BLL.PanelAlertas().ObtenerAlertas(new BLL.Usuario().ObtenerUsuarioActivo()); }
            catch (Exception ex)
            {
                // Excepción inesperada: se registra en bitácora (detalle técnico) y se muestra
                // solo un mensaje genérico traducido — antes mostraba ex.Message crudo, sin
                // traducir, directo en la tarjeta de alerta (único lugar de la GUI donde eso pasaba).
                try
                {
                    new BLL.Bitacora().RegistrarSinSesion(nameof(AlertasForm),
                        "Excepción: " + ex.GetType().Name, BE.Criticidad.Alta,
                        detalle: $"{nameof(AlertasForm)}.CargarAlertas: {ex.Message}");
                }
                catch { /* el fallo al registrar no debe romper el manejo del error */ }

                string generico = Tr("msg.error.inesperado",
                    "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.");
                flow.Controls.Add(CrearFila(BE.NivelAlerta.Critica, generico));
                flow.ResumeLayout();
                return;
            }

            if (alertas.Count == 0)
            {
                var ok = new Label
                {
                    Text      = Tr("alert.sinalertas", "No hay alertas activas. Todo en orden."),
                    Font      = new Font("Segoe UI", 10F),
                    ForeColor = Tema.Exito,
                    AutoSize  = true,
                    Margin    = new Padding(6, 10, 6, 6)
                };
                flow.Controls.Add(ok);
                flow.ResumeLayout();
                return;
            }

            foreach (var a in alertas)
            {
                string texto = Tr(a.ClaveI18n, a.MensajeFallback);
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
                Text      = texto,
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
