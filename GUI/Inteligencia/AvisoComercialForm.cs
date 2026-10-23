using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static GUI.Inteligencia.TextosComerciales;

namespace GUI.Inteligencia
{
    /// <summary>
    /// PN03 — Aviso no modal al iniciar sesión (Gerencia): cuántas recomendaciones de prioridad alta hay,
    /// cuántas decisiones pospuestas llegaron a su fecha y cuántas metas del mes están en riesgo, con un botón
    /// que abre el Centro de decisiones. "No mostrar hoy" lo recuerda por usuario (BLL.AvisosComerciales).
    /// Se muestra con Show (no ShowDialog): no frena el menú ni ningún otro flujo.
    /// </summary>
    public partial class AvisoComercialForm : Form
    {
        private readonly BE.ResumenAvisosComerciales _resumen;
        private readonly int _idUsuario;
        private readonly string _modulo;
        private readonly Action _abrirCentro;

        public AvisoComercialForm(BE.ResumenAvisosComerciales resumen, int idUsuario, string modulo, Action abrirCentro)
        {
            InitializeComponent();
            _resumen = resumen ?? new BE.ResumenAvisosComerciales();
            _idUsuario = idUsuario;
            _modulo = modulo;
            _abrirCentro = abrirCentro;
            Estilos.EstiloFormulario.BotonPrimario(btnIr);
            Estilos.EstiloFormulario.BotonSecundario(btnNoMostrarHoy);
            Estilos.EstiloFormulario.BotonSecundario(btnCerrar);
            Traducir();
        }

        private void Traducir()
        {
            Text = Tr("aviso.inicio.ventana", "Avisos comerciales");
            lblTitulo.Text = Tr("aviso.inicio.titulo", "Centro de decisiones: hay temas para hoy");
            var lineas = new System.Collections.Generic.List<string>();
            if (_resumen.RecomendacionesAlta > 0)
                lineas.Add("• " + Tr("aviso.inicio.recomendaciones", "{0} recomendación(es) de prioridad alta para decidir", _resumen.RecomendacionesAlta));
            if (_resumen.PospuestasVencidas > 0)
                lineas.Add("• " + Tr("aviso.inicio.pospuestas", "{0} decisión(es) pospuesta(s) llegaron a su fecha de revisión", _resumen.PospuestasVencidas));
            if (_resumen.MetasEnRiesgo > 0)
                lineas.Add("• " + Tr("aviso.inicio.metas", "{0} meta(s) del mes en riesgo", _resumen.MetasEnRiesgo));
            lblResumen.Text = string.Join(Environment.NewLine, lineas);
            lblDestacados.Text = string.Join(Environment.NewLine, _resumen.Destacados.Select(d => "– " + R(d)));
            btnIr.Text = Tr("aviso.inicio.ir", "Ir al Centro de decisiones");
            btnNoMostrarHoy.Text = Tr("aviso.inicio.nomostrar", "No mostrar hoy");
            btnCerrar.Text = Tr("btn.cerrar", "Cerrar");
        }

        /// <summary>Muestra el aviso, sin modal, en la esquina inferior derecha del menú.</summary>
        public void MostrarEnEsquina(Form duenio)
        {
            var area = duenio.RectangleToScreen(duenio.ClientRectangle);
            Location = new Point(Math.Max(area.Left, area.Right - Width - 16), Math.Max(area.Top, area.Bottom - Height - 16));
            Show(duenio);
        }

        private void BtnIr_Click(object sender, EventArgs e)
        {
            try { _abrirCentro?.Invoke(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[AvisoComercialForm] " + ex.Message); }
            Close();
        }

        private void BtnNoMostrarHoy_Click(object sender, EventArgs e)
        {
            try { new BLL.Inteligencia.AvisosComerciales().OcultarHoy(_modulo, _idUsuario); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[AvisoComercialForm] " + ex.Message); }
            Close();
        }

        private void BtnCerrar_Click(object sender, EventArgs e) => Close();

        // D02 — F1 abre la ayuda del aviso (no hereda de FormBase: es una ventana liviana y no modal).
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                Ayuda.AyudaEnLinea.Mostrar(this, nameof(AvisoComercialForm));
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
