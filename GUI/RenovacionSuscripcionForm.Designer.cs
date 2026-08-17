using System;
using System.Drawing;
using System.Windows.Forms;

namespace GUI
{
    partial class RenovacionSuscripcionForm
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
            this.lblTitulo       = new Label();
            this.lblCliente      = new Label();
            this.cmbCliente      = new ComboBox();
            this.lblEstadoActual = new Label();
            this.grpDecision     = new GroupBox();
            this.rbRenovar       = new RadioButton();
            this.rbCambiarPlan   = new RadioButton();
            this.rbBaja          = new RadioButton();
            this.rbPausar        = new RadioButton();
            this.dtpPausaHasta   = new DateTimePicker();
            this.lblPlanNuevo    = new Label();
            this.cmbPlanNuevo    = new ComboBox();
            this.lblModalidad    = new Label();
            this.cmbModalidad    = new ComboBox();
            this.btnProcesar     = new Button();
            this.btnReanudar     = new Button();
            this.lblResultado    = new Label();
            this.grpDecision.SuspendLayout();
            this.SuspendLayout();

            // ── lblTitulo ──────────────────────────────────────────────────────
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font     = new Font(this.Font, FontStyle.Bold);
            this.lblTitulo.Location = new Point(15, 12);
            this.lblTitulo.Name     = "lblTitulo";
            this.lblTitulo.TabIndex = 0;

            // ── lblCliente / cmbCliente ──────────────────────────────────────
            this.lblCliente.AutoSize = true;
            this.lblCliente.Location = new Point(15, 50);
            this.lblCliente.Name     = "lblCliente";
            this.lblCliente.TabIndex = 1;

            this.cmbCliente.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbCliente.Location      = new Point(120, 47);
            this.cmbCliente.Name          = "cmbCliente";
            this.cmbCliente.TabIndex      = 2;
            this.cmbCliente.Width         = 320;
            this.cmbCliente.SelectedIndexChanged += new System.EventHandler(this.CmbCliente_SelectedIndexChanged);

            // ── lblEstadoActual ───────────────────────────────────────────────
            this.lblEstadoActual.AutoSize  = true;
            this.lblEstadoActual.ForeColor = Color.DimGray;
            this.lblEstadoActual.Location  = new Point(15, 80);
            this.lblEstadoActual.Name      = "lblEstadoActual";
            this.lblEstadoActual.TabIndex  = 3;

            // ── grpDecision ───────────────────────────────────────────────────
            this.grpDecision.Controls.Add(this.rbRenovar);
            this.grpDecision.Controls.Add(this.rbCambiarPlan);
            this.grpDecision.Controls.Add(this.rbBaja);
            this.grpDecision.Controls.Add(this.rbPausar);
            this.grpDecision.Controls.Add(this.dtpPausaHasta);
            this.grpDecision.Location = new Point(15, 110);
            this.grpDecision.Name     = "grpDecision";
            this.grpDecision.Size     = new Size(430, 135);
            this.grpDecision.TabIndex = 4;
            this.grpDecision.TabStop  = false;

            this.rbRenovar.AutoSize = true;
            this.rbRenovar.Checked  = true;
            this.rbRenovar.Location = new Point(15, 25);
            this.rbRenovar.Name     = "rbRenovar";
            this.rbRenovar.TabIndex = 0;
            this.rbRenovar.TabStop  = true;
            this.rbRenovar.UseVisualStyleBackColor = true;

            this.rbCambiarPlan.AutoSize = true;
            this.rbCambiarPlan.Location = new Point(15, 50);
            this.rbCambiarPlan.Name     = "rbCambiarPlan";
            this.rbCambiarPlan.TabIndex = 1;
            this.rbCambiarPlan.UseVisualStyleBackColor = true;
            this.rbCambiarPlan.CheckedChanged += new System.EventHandler(this.RbCambiarPlan_CheckedChanged);

            this.rbBaja.AutoSize = true;
            this.rbBaja.Location = new Point(15, 75);
            this.rbBaja.Name     = "rbBaja";
            this.rbBaja.TabIndex = 2;
            this.rbBaja.UseVisualStyleBackColor = true;

            this.rbPausar.AutoSize = true;
            this.rbPausar.Location = new Point(15, 100);
            this.rbPausar.Name     = "rbPausar";
            this.rbPausar.TabIndex = 3;
            this.rbPausar.UseVisualStyleBackColor = true;
            this.rbPausar.CheckedChanged += new System.EventHandler(this.RbPausar_CheckedChanged);

            this.dtpPausaHasta.Enabled  = false;
            this.dtpPausaHasta.Format   = DateTimePickerFormat.Short;
            this.dtpPausaHasta.Location = new Point(250, 98);
            this.dtpPausaHasta.MinDate  = DateTime.Today;
            this.dtpPausaHasta.Name     = "dtpPausaHasta";
            this.dtpPausaHasta.TabIndex = 4;
            this.dtpPausaHasta.Width    = 150;

            // ── lblPlanNuevo / cmbPlanNuevo ───────────────────────────────────
            this.lblPlanNuevo.AutoSize = true;
            this.lblPlanNuevo.Location = new Point(15, 255);
            this.lblPlanNuevo.Name     = "lblPlanNuevo";
            this.lblPlanNuevo.TabIndex = 5;

            this.cmbPlanNuevo.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbPlanNuevo.Enabled       = false;
            this.cmbPlanNuevo.Location      = new Point(120, 252);
            this.cmbPlanNuevo.Name          = "cmbPlanNuevo";
            this.cmbPlanNuevo.TabIndex      = 6;
            this.cmbPlanNuevo.Width         = 320;

            // ── lblModalidad / cmbModalidad ───────────────────────────────────
            this.lblModalidad.AutoSize = true;
            this.lblModalidad.Location = new Point(15, 290);
            this.lblModalidad.Name     = "lblModalidad";
            this.lblModalidad.TabIndex = 7;

            this.cmbModalidad.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbModalidad.Items.AddRange(new object[] {
                BE.Builders.ModalidadCobro.Mensual,
                BE.Builders.ModalidadCobro.Trimestral,
                BE.Builders.ModalidadCobro.Anual });
            this.cmbModalidad.Location     = new Point(120, 287);
            this.cmbModalidad.Name         = "cmbModalidad";
            this.cmbModalidad.SelectedIndex = 0;
            this.cmbModalidad.TabIndex     = 8;
            this.cmbModalidad.Width        = 200;

            // ── btnProcesar / btnReanudar ────────────────────────────────────
            this.btnProcesar.Height   = 32;
            this.btnProcesar.Location = new Point(15, 330);
            this.btnProcesar.Name     = "btnProcesar";
            this.btnProcesar.TabIndex = 9;
            this.btnProcesar.Width    = 150;
            this.btnProcesar.UseVisualStyleBackColor = true;
            this.btnProcesar.Click += new System.EventHandler(this.BtnProcesar_Click);

            this.btnReanudar.Height   = 32;
            this.btnReanudar.Location = new Point(180, 330);
            this.btnReanudar.Name     = "btnReanudar";
            this.btnReanudar.TabIndex = 10;
            this.btnReanudar.Width    = 150;
            this.btnReanudar.UseVisualStyleBackColor = true;
            this.btnReanudar.Click += new System.EventHandler(this.BtnReanudar_Click);

            // ── lblResultado ───────────────────────────────────────────────────
            this.lblResultado.AutoSize  = false;
            this.lblResultado.ForeColor = Color.DarkGreen;
            this.lblResultado.Location  = new Point(15, 370);
            this.lblResultado.Name      = "lblResultado";
            this.lblResultado.Size      = new Size(430, 60);
            this.lblResultado.TabIndex  = 11;

            // ── RenovacionSuscripcionForm ──────────────────────────────────────
            this.ClientSize      = new Size(480, 455);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblCliente);
            this.Controls.Add(this.cmbCliente);
            this.Controls.Add(this.lblEstadoActual);
            this.Controls.Add(this.grpDecision);
            this.Controls.Add(this.lblPlanNuevo);
            this.Controls.Add(this.cmbPlanNuevo);
            this.Controls.Add(this.lblModalidad);
            this.Controls.Add(this.cmbModalidad);
            this.Controls.Add(this.btnProcesar);
            this.Controls.Add(this.btnReanudar);
            this.Controls.Add(this.lblResultado);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.Name            = "RenovacionSuscripcionForm";
            this.StartPosition   = FormStartPosition.CenterParent;
            this.Text            = "Renovación de Suscripción";

            this.grpDecision.ResumeLayout(false);
            this.grpDecision.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label      lblTitulo;
        private Label      lblCliente;
        private ComboBox   cmbCliente;
        private Label      lblEstadoActual;
        private GroupBox   grpDecision;
        private RadioButton rbRenovar;
        private RadioButton rbCambiarPlan;
        private RadioButton rbBaja;
        private RadioButton rbPausar;
        private DateTimePicker dtpPausaHasta;
        private Label      lblPlanNuevo;
        private ComboBox   cmbPlanNuevo;
        private Label      lblModalidad;
        private ComboBox   cmbModalidad;
        private Button     btnProcesar;
        private Button     btnReanudar;
        private Label      lblResultado;
    }
}
