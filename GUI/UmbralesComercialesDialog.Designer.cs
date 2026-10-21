using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class UmbralesComercialesDialog
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
            this.lblInfo = new Label();
            this.dgvUmbrales = new DataGridView();
            this.colNombre = new DataGridViewTextBoxColumn();
            this.colValor = new DataGridViewTextBoxColumn();
            this.colRango = new DataGridViewTextBoxColumn();
            this.colDefecto = new DataGridViewTextBoxColumn();
            this.colExplicacion = new DataGridViewTextBoxColumn();
            this.pnlBotones = new Panel();
            this.btnGuardar = new Button();
            this.btnRestablecer = new Button();
            this.btnCancelar = new Button();
            this.lblMensaje = new Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUmbrales)).BeginInit();
            this.pnlBotones.SuspendLayout();
            this.SuspendLayout();

            // ── lblTitulo / lblInfo ────────────────────────────────────────────
            this.lblTitulo.Dock = DockStyle.Top;
            this.lblTitulo.Font = Tema.FuenteTitulo;
            this.lblTitulo.ForeColor = Tema.RosaOscuro;
            this.lblTitulo.Height = 32;
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Padding = new Padding(8, 6, 0, 0);
            this.lblTitulo.Text = "Umbrales del motor de recomendaciones";

            this.lblInfo.Dock = DockStyle.Top;
            this.lblInfo.ForeColor = Tema.TextoMuted;
            this.lblInfo.Height = 36;
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Padding = new Padding(8, 0, 8, 0);

            // ── dgvUmbrales: solo la columna Valor es editable ────────────────
            this.dgvUmbrales.AllowUserToAddRows = false;
            this.dgvUmbrales.AllowUserToDeleteRows = false;
            this.dgvUmbrales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvUmbrales.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvUmbrales.Columns.AddRange(new DataGridViewColumn[] { this.colNombre, this.colValor, this.colRango, this.colDefecto, this.colExplicacion });
            this.dgvUmbrales.Dock = DockStyle.Fill;
            this.dgvUmbrales.Name = "dgvUmbrales";
            this.dgvUmbrales.CellValidating += new DataGridViewCellValidatingEventHandler(this.DgvUmbrales_CellValidating);

            this.colNombre.FillWeight = 30F;
            this.colNombre.Name = "colNombre";
            this.colNombre.ReadOnly = true;
            this.colValor.FillWeight = 12F;
            this.colValor.Name = "colValor";
            this.colRango.FillWeight = 14F;
            this.colRango.Name = "colRango";
            this.colRango.ReadOnly = true;
            this.colDefecto.FillWeight = 10F;
            this.colDefecto.Name = "colDefecto";
            this.colDefecto.ReadOnly = true;
            this.colExplicacion.FillWeight = 60F;
            this.colExplicacion.Name = "colExplicacion";
            this.colExplicacion.ReadOnly = true;
            this.colExplicacion.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // ── Botones ────────────────────────────────────────────────────────
            this.pnlBotones.Dock = DockStyle.Bottom;
            this.pnlBotones.Height = 44;
            this.pnlBotones.Name = "pnlBotones";
            this.pnlBotones.Controls.Add(this.btnGuardar);
            this.pnlBotones.Controls.Add(this.btnRestablecer);
            this.pnlBotones.Controls.Add(this.btnCancelar);

            this.btnGuardar.Location = new Point(8, 7);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.Size = new Size(140, 30);
            this.btnGuardar.Text = "Guardar";
            this.btnGuardar.Click += new System.EventHandler(this.BtnGuardar_Click);

            this.btnRestablecer.Location = new Point(156, 7);
            this.btnRestablecer.Name = "btnRestablecer";
            this.btnRestablecer.Size = new Size(220, 30);
            this.btnRestablecer.Text = "Restablecer valores de fábrica";
            this.btnRestablecer.Click += new System.EventHandler(this.BtnRestablecer_Click);

            this.btnCancelar.DialogResult = DialogResult.Cancel;
            this.btnCancelar.Location = new Point(384, 7);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new Size(110, 30);
            this.btnCancelar.Text = "Cancelar";

            this.lblMensaje.Dock = DockStyle.Bottom;
            this.lblMensaje.Height = 22;
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Padding = new Padding(8, 3, 8, 0);

            // ── UmbralesComercialesDialog ──────────────────────────────────────
            this.AcceptButton = this.btnGuardar;
            this.BackColor = Tema.Papel;
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new Size(980, 600);
            this.Controls.Add(this.dgvUmbrales);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.pnlBotones);
            this.Font = Tema.FuenteNormal;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(760, 420);
            this.Name = "UmbralesComercialesDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Umbrales";
            ((System.ComponentModel.ISupportInitialize)(this.dgvUmbrales)).EndInit();
            this.pnlBotones.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private Label lblTitulo;
        private Label lblInfo;
        private DataGridView dgvUmbrales;
        private DataGridViewTextBoxColumn colNombre;
        private DataGridViewTextBoxColumn colValor;
        private DataGridViewTextBoxColumn colRango;
        private DataGridViewTextBoxColumn colDefecto;
        private DataGridViewTextBoxColumn colExplicacion;
        private Panel pnlBotones;
        private Button btnGuardar;
        private Button btnRestablecer;
        private Button btnCancelar;
        private Label lblMensaje;
    }
}
