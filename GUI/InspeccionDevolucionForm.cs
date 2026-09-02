using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN04, CU-DEP-01 Inspeccionar Devolución. Actor: Depósito
    /// (OperadorDeInventario). Lógica alineada a Nuuly: binaria, sin aprobador — la prenda
    /// devuelta (EnLimpieza) reingresa sin cargo o se da de baja y se cobra el precio de
    /// reposición completo (BLL.CargoPrenda.RegistrarCargo, ya existente desde Bloque 1).
    /// </summary>
    public partial class InspeccionDevolucionForm : FormBase, IIdiomaObserver
    {
        protected override System.Windows.Forms.Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPrendaService prendaBLL = new BLL.Prenda();
        private readonly BLL.Interfaces.ICargoPrendaService cargoBLL = new BLL.CargoPrenda();

        private List<BE.Prenda> _prendas = new List<BE.Prenda>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public InspeccionDevolucionForm()
        {
            InitializeComponent();
        }

        // ── Observer de idioma ────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir(GestorIdioma.IdiomaActual);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarPrendas();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            Aplicar(btnAprobarReingreso, t);
            Aplicar(btnDarDeBajaConCargo, t);
            TraducirHeadersGrilla(t);
        }

        private static void Aplicar(Control c, IDictionary<string, Traduccion> t)
        {
            if (c?.Tag != null && t.ContainsKey(c.Tag.ToString()))
                c.Text = t[c.Tag.ToString()].Texto;
        }

        /// <summary>Renombra el HeaderText de las columnas de dgvPrendas según el idioma activo.</summary>
        private void TraducirHeadersGrilla(IDictionary<string, Traduccion> t)
        {
            void RH(string col, string clave)
            {
                if (dgvPrendas.Columns.Contains(col) && t.ContainsKey(clave))
                    dgvPrendas.Columns[col].HeaderText = t[clave].Texto;
            }

            RH("ID",        "col.insp.id");
            RH("Nombre",    "col.insp.nombre");
            RH("Talle",     "col.insp.talle");
            RH("Color",     "col.insp.color");
            RH("Categoría", "col.insp.categoria");
            RH("Último Cliente", "col.insp.ultimocliente");
        }

        private void InspeccionDevolucionForm_Load(object sender, EventArgs e)
        {
            CargarPrendas();
        }

        private void BtnRefrescar_Click(object sender, EventArgs e)
        {
            CargarPrendas();
        }

        private void CargarPrendas()
        {
            try
            {
                _prendas = prendaBLL.ObtenerEnLimpieza();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Nombre", typeof(string));
                tabla.Columns.Add("Talle", typeof(string));
                tabla.Columns.Add("Color", typeof(string));
                tabla.Columns.Add("Categoría", typeof(string));
                tabla.Columns.Add("Último Cliente", typeof(string));

                foreach (var p in _prendas)
                    tabla.Rows.Add(
                        p.IdPrenda, p.Nombre, p.Talle ?? "—", p.Color ?? "—",
                        p.Categoria ?? "—", p.NombreUltimoCliente ?? "—");

                dgvPrendas.DataSource = tabla;
                if (dgvPrendas.Columns.Contains("ID"))
                    dgvPrendas.Columns["ID"].Width = 44;
                TraducirHeadersGrilla(Traductor.ObtenerTraducciones(_idioma));

                var tCnt = Traductor.ObtenerTraducciones(_idioma);
                lblConteo.Text = string.Format(
                    tCnt.ContainsKey("insp.conteo") ? tCnt["insp.conteo"].Texto : "{0} prenda(s) pendiente(s) de inspección.",
                    _prendas.Count);
                DeshabilitarBotones();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void DgvPrendas_SelectionChanged(object sender, EventArgs e)
        {
            bool haySeleccion = dgvPrendas.SelectedRows.Count > 0;
            btnAprobarReingreso.Enabled = haySeleccion;
            btnDarDeBajaConCargo.Enabled = haySeleccion;
        }

        private void DeshabilitarBotones()
        {
            btnAprobarReingreso.Enabled = false;
            btnDarDeBajaConCargo.Enabled = false;
        }

        private BE.Prenda ObtenerSeleccionada()
        {
            if (dgvPrendas.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPrendas.SelectedRows[0].Cells["ID"].Value);
            return _prendas.Find(p => p.IdPrenda == id);
        }

        // Camino A — reingresa sin cargo. Mismo BLL.Prenda.CambiarEstado que ya usa GUI.Prendas,
        // sin aprobación de nadie (Nuuly: desgaste normal cubierto por la cuota).
        private void BtnAprobarReingreso_Click(object sender, EventArgs e)
        {
            var prenda = ObtenerSeleccionada();
            if (prenda == null) return;

            var t = Traductor.ObtenerTraducciones(_idioma);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            var confirmar = MessageBox.Show(
                string.Format(T("conf.insp.reingreso.msg", "¿Aprobar el reingreso de '{0}' a Disponible?"), prenda.Nombre),
                T("conf.insp.reingreso.titulo", "Confirmar Reingreso"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                string actor = Seguridad.SessionManager.IsLoggedIn
                    ? Seguridad.SessionManager.GetInstance().Usuario.Username : null;
                prendaBLL.CambiarEstado(this.Text, prenda, BE.EstadoPrenda.Disponible, actor);
                MostrarOk(string.Format(T("msg.insp.reingreso_ok", "'{0}' reingresó a Disponible."), prenda.Nombre));
                CargarPrendas();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // Camino B — se da de baja y se cobra el precio de reposición completo. Mismo
        // BLL.CargoPrenda.RegistrarCargo que ya usa GUI.Prendas, sin aprobación de nadie
        // (Nuuly: dañada sin reparación posible = cobro directo).
        private void BtnDarDeBajaConCargo_Click(object sender, EventArgs e)
        {
            var prenda = ObtenerSeleccionada();
            if (prenda == null) return;

            using (var dlg = new CargoPrendaDialog(prenda, prenda.PrecioReposicion))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    string actor = Seguridad.SessionManager.IsLoggedIn
                        ? Seguridad.SessionManager.GetInstance().Usuario.Username : null;

                    // Cobrar ANTES de dar de baja: si RegistrarCargo fallara después de la
                    // baja, la prenda quedaría destruida sin cobro (Baja es estado final, sin
                    // forma de reintentar el descarte). En este orden, si CambiarEstado fallara
                    // después del cargo, la prenda queda EnLimpieza con un cargo ya registrado
                    // — recuperable manualmente, y sin riesgo de "pérdida total silenciosa".
                    // Riesgo residual aceptado: ambos pasos no corren en una única transacción
                    // (BLL.Prenda y BLL.CargoPrenda son servicios distintos); no está más
                    // desarrollado por quedar fuera de alcance de este TP.
                    cargoBLL.RegistrarCargo(this.Text, prenda, dlg.Motivo, dlg.Monto, actor);
                    prendaBLL.CambiarEstado(this.Text, prenda, BE.EstadoPrenda.Baja, actor);
                    var tBaja = Traductor.ObtenerTraducciones(_idioma);
                    MostrarOk(string.Format(
                        tBaja.ContainsKey("msg.insp.baja_ok") ? tBaja["msg.insp.baja_ok"].Texto : "'{0}' dada de baja — cargo de ${1} registrado.",
                        prenda.Nombre, dlg.Monto));
                    CargarPrendas();
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }
    }
}
