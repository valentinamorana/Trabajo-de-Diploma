using System.Drawing;
using System.Windows.Forms;

namespace GUI.Inteligencia
{
    partial class AvisoComercialForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            this.lblTitulo = new Label();
            this.lblResumen = new Label();
            this.lblDestacados = new Label();
            this.pnlBotones = new FlowLayoutPanel();
            this.btnIr = new Button();
            this.btnNoMostrarHoy = new Button();
            this.btnCerrar = new Button();
            this.pnlBotones.SuspendLayout();
            this.SuspendLayout();

            // ── lblTitulo ─────────────────────────────────────────────────────
            this.lblTitulo.Dock = DockStyle.Top;
            this.lblTitulo.Font = Tema.FuenteSubtitulo;
            this.lblTitulo.ForeColor = Tema.RosaOscuro;
            this.lblTitulo.Height = 30;
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Padding = new Padding(10, 8, 10, 0);
            this.lblTitulo.Text = "Centro de decisiones: hay temas para hoy";

            // ── lblResumen: los tres contadores ───────────────────────────────
            this.lblResumen.Dock = DockStyle.Top;
            this.lblResumen.Font = Tema.FuenteNormal;
            this.lblResumen.ForeColor = Tema.Tinta;
            this.lblResumen.Height = 62;
            this.lblResumen.Name = "lblResumen";
            this.lblResumen.Padding = new Padding(12, 4, 10, 0);

            // ── lblDestacados: lo más urgente ─────────────────────────────────
            this.lblDestacados.Dock = DockStyle.Fill;
            this.lblDestacados.Font = Tema.FuentePequena;
            this.lblDestacados.ForeColor = Tema.TextoMuted;
            this.lblDestacados.Name = "lblDestacados";
            this.lblDestacados.Padding = new Padding(12, 2, 10, 0);

            // ── pnlBotones ────────────────────────────────────────────────────
            this.pnlBotones.Controls.Add(this.btnIr);
            this.pnlBotones.Controls.Add(this.btnNoMostrarHoy);
            this.pnlBotones.Controls.Add(this.btnCerrar);
            this.pnlBotones.Dock = DockStyle.Bottom;
            this.pnlBotones.Height = 44;
            this.pnlBotones.Name = "pnlBotones";
            this.pnlBotones.Padding = new Padding(8, 6, 8, 6);

            this.btnIr.AutoSize = true;
            this.btnIr.Name = "btnIr";
            this.btnIr.Height = 30;
            this.btnIr.Text = "Ir al Centro de decisiones";
            this.btnIr.Click += new System.EventHandler(this.BtnIr_Click);

            this.btnNoMostrarHoy.AutoSize = true;
            this.btnNoMostrarHoy.Name = "btnNoMostrarHoy";
            this.btnNoMostrarHoy.Height = 30;
            this.btnNoMostrarHoy.Text = "No mostrar hoy";
            this.btnNoMostrarHoy.Click += new System.EventHandler(this.BtnNoMostrarHoy_Click);

            this.btnCerrar.AutoSize = true;
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Height = 30;
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.Click += new System.EventHandler(this.BtnCerrar_Click);

            // ── AvisoComercialForm ────────────────────────────────────────────
            this.BackColor = Tema.Papel;
            this.ClientSize = new Size(440, 220);
            this.Controls.Add(this.lblDestacados);
            this.Controls.Add(this.lblResumen);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.pnlBotones);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AvisoComercialForm";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "Avisos";

            this.pnlBotones.ResumeLayout(false);
            this.pnlBotones.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private Label lblTitulo;
        private Label lblResumen;
        private Label lblDestacados;
        private FlowLayoutPanel pnlBotones;
        private Button btnIr;
        private Button btnNoMostrarHoy;
        private Button btnCerrar;
    }
}
