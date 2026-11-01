using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class ConstanciaDecisionForm
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
            this.components = new System.ComponentModel.Container();
            this.tip = new ToolTip(this.components);
            this.lblEncabezado = new Label();
            this.lblVerificacion = new Label();
            this.txtContenido = new RichTextBox();
            this.pnlBotones = new Panel();
            this.btnExportar = new Button();
            this.btnCerrar = new Button();
            this.lblMensaje = new Label();
            this.pnlBotones.SuspendLayout();
            this.SuspendLayout();

            // ── Encabezado: «ACEPTADA · Decisión #12 · Decidió … el … a las …» ─────────
            this.lblEncabezado.Dock = DockStyle.Top;
            this.lblEncabezado.Font = Tema.FuenteSubtitulo;
            this.lblEncabezado.ForeColor = Tema.RosaOscuro;
            this.lblEncabezado.Height = 34;
            this.lblEncabezado.Name = "lblEncabezado";
            this.lblEncabezado.Padding = new Padding(8, 8, 8, 0);
            this.lblEncabezado.AutoEllipsis = true;

            // ── Verificación contra la base (solo al abrir un archivo) ───────────────────
            this.lblVerificacion.Dock = DockStyle.Top;
            this.lblVerificacion.Height = 30;
            this.lblVerificacion.Name = "lblVerificacion";
            this.lblVerificacion.Padding = new Padding(8, 6, 8, 0);
            this.lblVerificacion.Visible = false;

            // ── Secciones legibles (solo lectura) ────────────────────────────────────────
            this.txtContenido.BackColor = Color.White;
            this.txtContenido.BorderStyle = BorderStyle.None;
            this.txtContenido.Dock = DockStyle.Fill;
            this.txtContenido.Name = "txtContenido";
            this.txtContenido.ReadOnly = true;
            this.txtContenido.TabIndex = 0;

            // ── Botones ──────────────────────────────────────────────────────────────────
            this.pnlBotones.Dock = DockStyle.Bottom;
            this.pnlBotones.Height = 44;
            this.pnlBotones.Name = "pnlBotones";
            this.pnlBotones.Controls.Add(this.btnExportar);
            this.pnlBotones.Controls.Add(this.btnCerrar);

            this.btnExportar.Location = new Point(8, 7);
            this.btnExportar.Name = "btnExportar";
            this.btnExportar.Size = new Size(200, 30);
            this.btnExportar.TabIndex = 1;
            this.btnExportar.Text = "Exportar XML/JSON…";
            this.btnExportar.UseVisualStyleBackColor = true;
            this.btnExportar.Click += new System.EventHandler(this.BtnExportar_Click);

            this.btnCerrar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnCerrar.DialogResult = DialogResult.Cancel;
            this.btnCerrar.Location = new Point(762, 7);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new Size(110, 30);
            this.btnCerrar.TabIndex = 2;
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = true;

            this.lblMensaje.Dock = DockStyle.Bottom;
            this.lblMensaje.Height = 22;
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Padding = new Padding(8, 3, 8, 0);

            // ── ConstanciaDecisionForm ───────────────────────────────────────────────────
            this.BackColor = Tema.Papel;
            this.CancelButton = this.btnCerrar;
            this.ClientSize = new Size(880, 620);
            this.Controls.Add(this.txtContenido);
            this.Controls.Add(this.lblVerificacion);
            this.Controls.Add(this.lblEncabezado);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.pnlBotones);
            this.Font = Tema.FuenteNormal;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(600, 400);
            this.Name = "ConstanciaDecisionForm";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Constancia de la decisión";
            this.pnlBotones.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ToolTip tip;
        private Label lblEncabezado;
        private Label lblVerificacion;
        private RichTextBox txtContenido;
        private Panel pnlBotones;
        private Button btnExportar;
        private Button btnCerrar;
        private Label lblMensaje;
    }
}
