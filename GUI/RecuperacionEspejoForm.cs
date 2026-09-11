using System;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// T07 — Consola de Recuperación de Integridad asistida por el ESPEJO (Usuario_Seguridad).
    ///
    /// Muestra el diagnóstico a nivel de CAMPO (qué cambió respecto del último estado legítimo) y
    /// ofrece tres opciones, habilitadas según el tipo de daño (inspirado en el proyecto Agus):
    ///   • Reparar desde Espejo  — restaura los valores legítimos guardados en el espejo.
    ///   • Asumir Pérdida        — acepta los datos actuales y recalcula todos los DV.
    ///   • Restaurar Backup      — cuando el espejo no alcanza (vaciado/eliminación física).
    ///
    /// Es la ÚNICA vía de recuperación del sistema. Se abre desde el Diagnóstico de Integridad
    /// (admin logueado) y desde el arranque (Program), cuando un Administrador autenticado entra
    /// con la base comprometida. Cada acción se confirma con ConfirmarAdminForm.
    /// </summary>
    public partial class RecuperacionEspejoForm : FormBase
    {
        // True si se ejecutó una recuperación con éxito (el llamador puede reiniciar/refrescar).
        public bool RecuperadoExitosamente { get; private set; }

        public RecuperacionEspejoForm()
        {
            InitializeComponent();

            this.Text            = Tr("rec.frm.titulo",    this.Text);
            btnCerrar.Text       = Tr("rec.btn.cerrar",     btnCerrar.Text);
            btnBackup.Text       = Tr("rec.btn.backup",     btnBackup.Text);
            btnAsumir.Text       = Tr("rec.btn.asumir",     btnAsumir.Text);
            btnReparar.Text      = Tr("rec.btn.reparar",    btnReparar.Text);
            colId.HeaderText       = Tr("rec.col.id",       colId.HeaderText);
            colUsuario.HeaderText  = Tr("rec.col.usuario",  colUsuario.HeaderText);
            colTipo.HeaderText     = Tr("rec.col.tipo",     colTipo.HeaderText);
            colCampo.HeaderText    = Tr("rec.col.campo",    colCampo.HeaderText);
            colActual.HeaderText   = Tr("rec.col.actual",   colActual.HeaderText);
            colEsperado.HeaderText = Tr("rec.col.esperado", colEsperado.HeaderText);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CargarDiagnostico();
        }

        private void CargarDiagnostico()
        {
            grid.Rows.Clear();
            try
            {
                var d = BLL.RecuperacionIntegridad.Diagnosticar();

                lblEstado.Text = d.Integro
                    ? Tr("rec.estado.integro", "Estado: ÍNTEGRO")
                    : Tr("rec.estado.comprometido", "Estado: COMPROMETIDO");

                txtResumen.Text = string.Join("\r\n", d.Resumen.ToArray());
                if (d.DvvAlmacenado != null || d.DvvCalculado != 0)
                    txtResumen.Text += "\r\n" + string.Format(
                        Tr("rec.dvv.detalle", "DVV almacenado: {0}  |  DVV calculado: {1}"),
                        d.DvvAlmacenado?.ToString() ?? "—", d.DvvCalculado);

                string tMod = Tr("rec.tipo.modificada",   "Modificada");
                string tDvh = Tr("rec.tipo.dvh",          "DVH corrupto");
                string tIns = Tr("rec.tipo.insertada",    "Inserción externa");
                string tDel = Tr("rec.tipo.eliminada",    "Eliminada (falta)");

                foreach (var a in d.Alteradas)
                {
                    string tipo = a.Tipo == BLL.TipoAlteracion.Modificada ? tMod
                                : a.Tipo == BLL.TipoAlteracion.DVHCorrupto ? tDvh : tIns;
                    if (a.Campos != null && a.Campos.Count > 0)
                        foreach (var c in a.Campos)
                            grid.Rows.Add(a.Id, a.Username, tipo, c.Campo, c.ValorActual, c.ValorEsperado);
                    else
                        grid.Rows.Add(a.Id, a.Username, tipo, "—", a.Descripcion ?? "", "—");
                }
                foreach (var f in d.Faltantes)
                    grid.Rows.Add(f.Id, f.Username, tDel, "—",
                        Tr("rec.fila.ausente", "(ausente en la tabla actual)"), Tr("rec.fila.enespejo", "(presente en el espejo)"));

                btnReparar.Enabled = d.PuedeReparar;
                btnAsumir.Enabled  = d.PuedeAsumirPerdida;
                btnBackup.Enabled  = d.PuedeRestaurarBackup;

                // Pista visual: si todo está íntegro, deshabilitar las acciones correctivas.
                if (d.Integro)
                {
                    btnReparar.Enabled = false;
                    btnAsumir.Enabled  = false;
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private bool ConfirmarAdmin()
        {
            using (var admin = new ConfirmarAdminForm())
                return admin.ShowDialog(this) == DialogResult.OK && admin.Autorizado;
        }

        private void BtnCerrar_Click(object sender, EventArgs e) => this.Close();

        private void BtnReparar_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    Tr("rec.conf.reparar", "¿Restaurar la tabla Usuario a los valores legítimos del espejo?\n\nSe revertirán las filas modificadas y se eliminarán las inserciones externas."),
                    Tr("rec.conf.reparar.titulo", "Confirmar Reparación"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            if (!ConfirmarAdmin()) return;

            try
            {
                BLL.RecuperacionIntegridad.RepararDesdeEspejo();
                RecuperadoExitosamente = true;
                MessageBox.Show(Tr("rec.msg.reparado", "Saneamiento completado: se restauraron los datos legítimos del espejo."),
                    Tr("rec.msg.exito.titulo", "Éxito"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDiagnostico();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnAsumir_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    Tr("rec.conf.asumir", "ATENCIÓN: se aceptarán los datos ACTUALES como legítimos y se recalcularán todos los dígitos verificadores.\n\nSi hubo manipulación, quedará consolidada. ¿Continuar?"),
                    Tr("rec.conf.asumir.titulo", "Confirmar Asumir Pérdida"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes) return;
            if (!ConfirmarAdmin()) return;

            try
            {
                BLL.RecuperacionIntegridad.AsumirPerdida();
                RecuperadoExitosamente = true;
                MessageBox.Show(Tr("rec.msg.asumido", "Dígitos verificadores recalculados sobre los datos actuales."),
                    Tr("rec.msg.exito.titulo", "Éxito"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDiagnostico();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            if (!ConfirmarAdmin()) return;

            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Copias de Seguridad (*.wfbak;*.bak)|*.wfbak;*.bak";
                ofd.Title  = Tr("rec.backup.seleccionar", "Seleccionar Backup para Restaurar");
                if (ofd.ShowDialog() != DialogResult.OK) return;

                // RF-08 — Informar el ALCANCE de la pérdida (fecha del backup + registros actuales
                // posteriores que se perderían al sobrescribir) antes de confirmar la restauración.
                string alcance = ConstruirAlcancePerdida(ofd.FileName);

                if (MessageBox.Show(
                        Tr("conf.rest.sobreescribir", "¿Está seguro? Esta operación sobrescribirá todos los datos actuales y reiniciará la aplicación.") + alcance,
                        Tr("msg.backup.titulorestaura", "Confirmar Restauración"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                string clave = null;
                if (BLL.Backup.EsCifrado(ofd.FileName))
                {
                    using (var dlg = new InputDialog(
                        Tr("dlg.backup.clave.titulo", "Contraseña del backup"),
                        Tr("dlg.backup.clave.ingresar", "Ingresá la contraseña con la que se cifró este backup:"),
                        esPassword: true))
                    {
                        if (dlg.ShowDialog(this) != DialogResult.OK) return;
                        clave = dlg.InputText;
                    }
                }

                try
                {
                    new BLL.Backup().RestaurarBackup("RecuperacionEspejo", ofd.FileName, clave);
                    MessageBox.Show(Tr("msg.backup.restauradaexito", "Base de datos restaurada con éxito.\nLa aplicación se reiniciará."),
                        Tr("rpt.dlg.exito.titulo", "Éxito"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RecuperadoExitosamente = true;
                    Application.Restart();
                }
                catch (Exception ex) { MostrarError(ex); }
            }
        }

        // RF-08 — Arma el aviso de alcance de la pérdida: fecha del backup + desglose de los
        // registros de la base actual posteriores a esa fecha (los que se perderían). Tolerante a
        // fallos: si no puede leerse la fecha o contar, devuelve el aviso genérico o vacío.
        private string ConstruirAlcancePerdida(string ruta)
        {
            try
            {
                var bll = new BLL.Backup();
                DateTime? fecha = bll.ObtenerFechaBackup(ruta);
                if (!fecha.HasValue)
                    return Tr("msg.backup.alcance.desconocido",
                        "\n\nNo se pudo determinar la fecha del backup. Se perderán todos los cambios posteriores a su creación.");

                var sb = new System.Text.StringBuilder();
                sb.Append(string.Format(
                    Tr("msg.backup.alcance",
                      "\n\nEl backup es del {0} (hace {1} día(s)).\nSe PERDERÁN todos los cambios posteriores a esa fecha."),
                    fecha.Value.ToString("dd/MM/yyyy HH:mm"),
                    Math.Max(0, (int)(DateTime.Now - fecha.Value).TotalDays)));

                var cambios = bll.ObtenerCambiosDesde(fecha);
                if (cambios == null || cambios.Count == 0)
                    sb.Append(Tr("msg.backup.sinperdida",
                        "\n\nNo hay registros nuevos posteriores a esa fecha: no se perdería información reciente."));
                else
                {
                    sb.Append(Tr("msg.backup.perdida.titulo",
                        "\n\nSe perderán estos registros creados después del backup:"));
                    foreach (var c in cambios)
                        sb.Append($"\n  • {c.Entidad}: {c.Cantidad}");
                }
                return sb.ToString();
            }
            catch { return string.Empty; }
        }
    }
}
