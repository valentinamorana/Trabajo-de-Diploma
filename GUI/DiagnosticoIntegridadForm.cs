using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    public partial class DiagnosticoIntegridadForm : Form, IIdiomaObserver
    {
        public DiagnosticoIntegridadForm()
        {
            InitializeComponent();
        }

        // Helper de traducción
        private string T(string key, string fallback)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(key) ? t[key].Texto : fallback;
        }

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (System.IO.File.Exists(ico)) this.Icon = new System.Drawing.Icon(ico);
            }
            catch { }
            GestorIdioma.SuscribirObservador(this);
            UpdateLanguage(GestorIdioma.IdiomaActual);
            CargarDiagnostico();
            CargarHistorial();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        // ── IIdiomaObserver ───────────────────────────────────────────────────

        public void UpdateLanguage(Idioma idioma)
        {
            this.Text                   = T("diag.frm.titulo",         "Diagnóstico de Integridad");
            lblFilasRotas.Text          = T("diag.lbl.filasrotas",     "Filas con DVH inválido:");
            btnActualizar.Text          = T("diag.btn.actualizar",     "Actualizar");
            btnRecalcularTodo.Text      = T("diag.btn.recalcular",     "Recalcular Todo");
            btnEspejo.Text              = T("diag.btn.espejo",         "Recuperación (Espejo)...");
            // (botón "Reparar Seleccionadas" eliminado: lo reemplaza la consola del Espejo)
            btnActualizarHist.Text      = T("diag.btn.actualizar",     "Actualizar");

            if (tabs.TabPages.Count >= 1)
                tabs.TabPages[0].Text = T("diag.tab.diagnostico",    "Diagnóstico");
            if (tabs.TabPages.Count >= 2)
                tabs.TabPages[1].Text = T("diag.tab.historial",      "Historial de Verificaciones");

            ActualizarHeadersGrilla();
            CargarDiagnostico();
            CargarHistorial();
        }

        private void ActualizarHeadersGrilla()
        {
            void SetH(DataGridView g, string col, string key, string fb)
            {
                if (g.Columns.Contains(col)) g.Columns[col].HeaderText = T(key, fb);
            }
            SetH(gridRotas,    "colId",       "diag.col.id",       "ID");
            SetH(gridRotas,    "colUsuario",   "diag.col.usuario",  "Usuario");
            SetH(gridRotas,    "colDVHAlm",    "diag.col.dvh.alm",  "DVH Almacenado");
            SetH(gridRotas,    "colDVHCalc",   "diag.col.dvh.calc", "DVH Calculado");
            SetH(gridRotas,    "colEstado",    "diag.col.estado",   "Estado");
            SetH(gridHistorial,"hFecha",       "diag.col.fecha",    "Fecha");
            SetH(gridHistorial,"hTabla",       "diag.col.tabla",    "Tabla");
            SetH(gridHistorial,"hDVVAlm",      "diag.col.dvv.alm",  "DVV Almacenado");
            SetH(gridHistorial,"hDVVCalc",     "diag.col.dvv.calc", "DVV Calculado");
            SetH(gridHistorial,"hRotas",       "diag.col.filas.corr","Filas Corruptas");
            SetH(gridHistorial,"hResultado",   "diag.col.resultado","Resultado");
            SetH(gridHistorial,"hOrigen",      "diag.col.origen",   "Disparado por");
        }

        // ── Carga de datos ────────────────────────────────────────────────────

        private void CargarDiagnostico()
        {
            btnActualizar.Enabled = false;
            try
            {
                var diag = BLL.Configuracion.ObtenerDiagnostico();

                lblEstadoDVV.Text = diag.Integro
                    ? T("diag.estado.integro",      "Estado: INTEGRO")
                    : T("diag.estado.comprometido", "Estado: COMPROMETIDO");
                lblEstadoDVV.ForeColor = diag.Integro
                    ? Color.FromArgb(40, 140, 60)
                    : Color.FromArgb(180, 50, 50);

                lblDVVDetalle.Text = string.Format(
                    T("diag.dvv.detalle", "DVV almacenado: {0}   |   DVV calculado: {1}   |   Filas con DVH inválido: {2}"),
                    diag.DVVAlmacenado?.ToString() ?? "—",
                    diag.DVVCalculado,
                    diag.FilasRotas.Count);

                if (diag.TablasAdicionalesCorruptas.Count > 0)
                    lblDVVDetalle.Text += "   |   " + string.Format(
                        T("diag.tablas.corruptas", "Tablas con DV inválido: {0}"),
                        string.Join(", ", diag.TablasAdicionalesCorruptas));

                gridRotas.Rows.Clear();
                string sinDVH      = T("diag.fila.sinDVH",     "Sin DVH");
                string noCoincide  = T("diag.fila.nocoincide", "DVH no coincide");
                string dvhRuntime  = T("diag.fila.recalculado", "Recalculado en vivo");
                foreach (var fila in diag.FilasRotas)
                {
                    string estadoFila = fila.DVHAlmacenado == null ? sinDVH : noCoincide;
                    gridRotas.Rows.Add(fila.Id, fila.Username,
                        fila.DVHAlmacenado?.ToString() ?? "—",
                        dvhRuntime,
                        estadoFila);
                }

                // Tablas adicionales (Cliente / Empleado / Pedido) con DV inválido: se listan
                // como filas informativas para que el problema sea visible y se sepa que hay que
                // recalcular (antes una corrupción en Cliente no aparecía por ningún lado).
                foreach (var tablaCorrupta in diag.TablasAdicionalesCorruptas)
                {
                    int tIdx = gridRotas.Rows.Add("—",
                        string.Format(T("diag.tabla.corrupta", "Tabla '{0}'"), tablaCorrupta),
                        "—", "—",
                        T("diag.tabla.dvinvalido", "DV inválido — usá «Recalcular Todo»"));
                    gridRotas.Rows[tIdx].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(180, 50, 50);
                    gridRotas.Rows[tIdx].ReadOnly = true;
                }

                // Si no hay NINGÚN problema (ni en Usuario ni en las tablas adicionales),
                // mostrar el cartel "Todo íntegro" SOBRE la grilla (no como fila en Usuario).
                bool gridVacio = diag.FilasRotas.Count == 0 && diag.TablasAdicionalesCorruptas.Count == 0;
                lblGridVacio.Text    = T("diag.sinfilas", "✓ Todo íntegro — no hay filas con problemas de integridad.");
                lblGridVacio.Visible = gridVacio;
                if (gridVacio) lblGridVacio.BringToFront(); else lblGridVacio.SendToBack();

                // Habilitar el recálculo total si CUALQUIER tabla protegida está comprometida
                // (incluye Cliente/Empleado/Pedido, no solo Usuario).
                btnRecalcularTodo.Enabled = !diag.Integro;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(T("diag.err.cargar", "Error al cargar diagnóstico: {0}"), ex.Message),
                    T("diag.err.titulo", "Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnActualizar.Enabled = true;
            }
        }

        private void CargarHistorial()
        {
            gridHistorial.Rows.Clear();
            try
            {
                string ok    = T("diag.hist.ok",    "OK");
                string fallo = T("diag.hist.fallo", "FALLO");
                var lista = BLL.Configuracion.ObtenerHistorialIntegridad(150);
                foreach (var h in lista)
                {
                    int idx = gridHistorial.Rows.Add(
                        h.FechaVerificacion.ToString("dd/MM/yyyy HH:mm:ss"),
                        h.NombreTabla,
                        h.DVVAlmacenado?.ToString() ?? "—",
                        h.DVVCalculado.ToString(),
                        h.FilasCorruptas.ToString(),
                        h.Resultado ? ok : fallo,
                        h.DisparadoPor);

                    // Corrección pedida: al pasar el mouse sobre una verificación, explicar
                    // POR QUÉ falló (o por qué está OK). El tooltip se fija en toda la fila.
                    string tip = ConstruirTooltipHistorial(h);
                    foreach (DataGridViewCell celda in gridHistorial.Rows[idx].Cells)
                        celda.ToolTipText = tip;
                }
            }
            catch
            {
                // Si la tabla aún no existe, mostrar vacío silenciosamente
            }
        }

        // Arma el texto del tooltip de una verificación del historial: si FALLÓ, detalla la causa
        // (DVV que no coincide y/o filas con DVH inválido); si está OK, lo confirma.
        private string ConstruirTooltipHistorial(BE.HistorialIntegridad h)
        {
            if (h.Resultado)
                return T("diag.tip.ok", "Verificación correcta: los dígitos verificadores coinciden con los datos.");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Format(
                T("diag.tip.fallo.titulo", "Verificación FALLIDA — tabla {0}"), h.NombreTabla));

            // DVV no coincide → se agregaron/quitaron/reordenaron filas.
            if (h.DVVAlmacenado == null || h.DVVAlmacenado != h.DVVCalculado)
                sb.AppendLine(string.Format(
                    T("diag.tip.dvv.mismatch",
                      "• El DVV no coincide: almacenado {0} ≠ calculado {1} (se insertaron, eliminaron o reordenaron filas)."),
                    h.DVVAlmacenado?.ToString() ?? "—", h.DVVCalculado));

            // Filas con DVH inválido → se modificó el contenido de esas filas.
            if (h.FilasCorruptas > 0)
                sb.AppendLine(string.Format(
                    T("diag.tip.filas.corruptas",
                      "• {0} fila(s) con DVH inválido: el contenido de esas filas fue modificado directamente en la base."),
                    h.FilasCorruptas));

            sb.Append(T("diag.tip.causa",
                "Posible manipulación directa de la base de datos. Reparar desde la pestaña Diagnóstico o restaurar un backup."));
            return sb.ToString();
        }

        private void GridHistorial_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = gridHistorial.Columns[e.ColumnIndex];
            if (col.Name != "hResultado") return;

            string ok  = T("diag.hist.ok", "OK");
            string val = e.Value?.ToString() ?? "";
            e.CellStyle.ForeColor = val == ok
                ? Color.FromArgb(30, 130, 50)
                : Color.FromArgb(180, 50, 50);
            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        // ── Acciones ──────────────────────────────────────────────────────────

        private void BtnActualizar_Click(object sender, EventArgs e) => CargarDiagnostico();

        private void BtnActualizarHist_Click(object sender, EventArgs e) => CargarHistorial();

        private void BtnEspejo_Click(object sender, EventArgs e)
        {
            using (var rec = new RecuperacionEspejoForm())
            {
                rec.ShowDialog(this);
            }
            CargarDiagnostico();
            CargarHistorial();
        }

        private void BtnRecalcularTodo_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    T("diag.conf.recalcular",
                      "¿Recalcular todos los DVH y el DVV de la tabla Usuario?\n\nEsta operación sobreescribirá todos los dígitos verificadores almacenados."),
                    T("diag.conf.recalcular.titulo", "Confirmar Recálculo Total"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            using (var admin = new ConfirmarAdminForm())
            {
                if (admin.ShowDialog(this) != DialogResult.OK || !admin.Autorizado) return;

                try
                {
                    BLL.Configuracion.RecalcularIntegridadDV();
                    MessageBox.Show(
                        T("diag.msg.recalc.exito", "Dígitos verificadores recalculados con éxito."),
                        T("diag.msg.exito.titulo", "Éxito"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(T("diag.err.recalcular", "Error al recalcular: {0}"), ex.Message),
                        T("diag.err.titulo", "Error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            CargarDiagnostico();
            CargarHistorial();
        }
    }
}
