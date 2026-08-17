using System;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Bloque 1 — Diálogo para registrar un cargo por daño/pérdida sobre el último cliente
    /// que tuvo la prenda (BE.Prenda.IdUltimoCliente). Se ofrece desde Prendas.cs justo
    /// después de confirmar el paso a Baja.
    /// </summary>
    public partial class CargoPrendaDialog : Form
    {
        public string Motivo { get; private set; }
        public decimal Monto { get; private set; }

        public CargoPrendaDialog(BE.Prenda prenda)
        {
            InitializeComponent();
            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new System.Drawing.Icon(ico); } catch { }

            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            this.Text           = T("frm.cargoprenda",       "Cargo por Daño/Pérdida");
            lblMotivo.Text      = T("lbl.cargoprenda.motivo", "Motivo (daño o pérdida) *");
            lblMonto.Text       = T("lbl.cargoprenda.monto",  "Monto a cobrar *");
            btnConfirmar.Text   = T("btn.registrar.cargo",    "Registrar Cargo");
            btnCancelar.Text    = T("btn.cancelar",           "Cancelar");

            string fmtInfo = T("lbl.cargoprenda.info", "Prenda: {0}\nÚltimo cliente: {1}");
            lblPrendaInfo.Text = string.Format(fmtInfo, prenda.Nombre, prenda.NombreUltimoCliente ?? "—");
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            if (string.IsNullOrWhiteSpace(txtMotivo.Text))
            {
                lblMensaje.Text = T("msg.cargoprenda.motivorequerido", "Indicá el motivo del cargo.");
                return;
            }
            if (numMonto.Value <= 0)
            {
                lblMensaje.Text = T("msg.cargoprenda.montoinvalido", "El monto debe ser mayor a cero.");
                return;
            }

            Motivo = txtMotivo.Text.Trim();
            Monto = numMonto.Value;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
