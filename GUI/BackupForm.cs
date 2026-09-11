using Servicios.Multiidioma;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class BackupForm : FormBase, IIdiomaObserver
    {
        private readonly BLL.Backup _bll = new BLL.Backup();

        private static readonly string DirBackups =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

        public BackupForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);   // FormBase: ícono + tema/fuente del usuario + seguridad de controles
            GestorIdioma.SuscribirObservador(this);

            Traducir(GestorIdioma.IdiomaActual);
            lblRuta.Text = DirBackups;
            CargarLista();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarLista();
        }

        private void Traducir(Idioma idioma)
        {
            this.Text         = Tr("frm.backup",           "Backup y Restauración");
            lblTitulo.Text    = Tr("frm.backup",           "Backup y Restauración");
            lblRutaLabel.Text = Tr("lbl.backup.ubicacion", "Ubicación de copias:");
            btnCrear.Text     = Tr("btn.backup.crear",     "Generar Copia de Seguridad");
            btnRestaurar.Text = Tr("btn.backup.restaurar", "Restaurar seleccionado");
            btnEliminar.Text  = Tr("btn.backup.eliminar",  "Eliminar");
            btnExterno.Text   = Tr("btn.backup.externo",   "Desde archivo...");
            lblInfo.Text      = Tr("lbl.backup.info",      "Nota: la restauración cierra las conexiones activas y reinicia la aplicación.");
            colArchivo.Text   = Tr("col.backup.archivo",   "Archivo");
            colFecha.Text     = Tr("col.backup.fecha",     "Fecha");
            colAutor.Text     = Tr("col.backup.autor",     "Autor");
            colTamanio.Text   = Tr("col.backup.tamanio",   "Tamaño");
            btnInicial.Text   = Tr("btn.backup.inicial",   "Backup de instalación limpia");
        }

        // Carga los .bak de la carpeta Backups/ ordenados por fecha descendente (más reciente primero).
        private void CargarLista()
        {
            lstBackups.Items.Clear();
            btnRestaurar.Enabled = false;
            btnEliminar.Enabled  = false;

            if (!Directory.Exists(DirBackups))
            {
                lblConteo.Text = Tr("lbl.backup.sincopias", "Sin copias de seguridad generadas aún.");
                return;
            }

            // Incluye los backups cifrados (.wfbak) y los .bak planos legacy.
            var archivos = new DirectoryInfo(DirBackups).GetFiles("*.bak")
                .Concat(new DirectoryInfo(DirBackups).GetFiles("*" + BLL.Backup.ExtensionCifrada))
                .ToArray();
            Array.Sort(archivos, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));

            foreach (var fi in archivos)
            {
                string tamanio = fi.Length >= 1_048_576
                    ? $"{fi.Length / 1_048_576.0:F1} MB"
                    : $"{fi.Length / 1024.0:F0} KB";

                var item = new ListViewItem(fi.Name) { Tag = fi.FullName };
                item.SubItems.Add(fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm"));
                item.SubItems.Add(_bll.ExtraerAutorDeNombre(fi.Name));
                item.SubItems.Add(tamanio);
                lstBackups.Items.Add(item);
            }

            lblConteo.Text = archivos.Length == 0
                ? Tr("lbl.backup.sincopias", "Sin copias de seguridad generadas aún.")
                : string.Format(Tr("lbl.backup.conteo", "{0} copia(s) disponible(s). La más reciente: {1}"),
                    archivos.Length, archivos[0].LastWriteTime.ToString("dd/MM/yyyy HH:mm"));
        }

        private void lstBackups_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool seleccionado    = lstBackups.SelectedItems.Count > 0;
            btnRestaurar.Enabled = seleccionado;
            btnEliminar.Enabled  = seleccionado;
        }

        private async void btnCrear_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(DirBackups))
                    Directory.CreateDirectory(DirBackups);

                string clave = PedirClaveNueva();
                if (clave == null) return;   // cancelado o inválido

                string filename = await EjecutarConEsperaAsync(() => _bll.RealizarBackup(this.Text, DirBackups, clave));
                MessageBox.Show(
                    string.Format(Tr("msg.backup.creadoexito", "Copia de seguridad generada con éxito:\n{0}"), filename),
                    Tr("rpt.dlg.exito.titulo", "Éxito"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarLista();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Tr("msg.backup.errorgenerar", "Error al generar copia de seguridad:\n{0}"), ex.Message),
                    Tr("msg.error.titulo", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnInicial_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(DirBackups))
                    Directory.CreateDirectory(DirBackups);

                string clave = PedirClaveNueva();
                if (clave == null) return;

                string filename = await EjecutarConEsperaAsync(() => _bll.RealizarBackupInicial(this.Text, DirBackups, clave));
                MessageBox.Show(
                    string.Format(Tr("msg.backup.inicialexito", "Backup de instalación limpia generado:\n{0}"), filename),
                    Tr("rpt.dlg.exito.titulo", "Éxito"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarLista();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Tr("msg.backup.errorgenerar", "Error al generar copia de seguridad:\n{0}"), ex.Message),
                    Tr("msg.error.titulo", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnRestaurar_Click(object sender, EventArgs e)
        {
            if (lstBackups.SelectedItems.Count == 0) return;
            await Restaurar(lstBackups.SelectedItems[0].Tag as string);
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (lstBackups.SelectedItems.Count == 0) return;

            string ruta     = lstBackups.SelectedItems[0].Tag as string;
            string filename = Path.GetFileName(ruta);

            if (MessageBox.Show(
                    string.Format(Tr("msg.backup.confirmeliminar",
                        "¿Eliminar la copia de seguridad?\n\"{0}\"\n\nEsta acción no se puede deshacer."), filename),
                    Tr("msg.backup.tituloeliminar", "Confirmar Eliminación"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                _bll.EliminarBackup(this.Text, ruta);
                CargarLista();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Tr("msg.backup.erroreliminar", "Error al eliminar:\n{0}"), ex.Message),
                    Tr("msg.error.titulo", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Restaura un archivo elegido manualmente (útil para backups en USB u otra ubicación).
        private async void btnExterno_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Copias de Seguridad (*.wfbak;*.bak)|*.wfbak;*.bak";
                ofd.Title  = Tr("dlg.backup.seleccionarexterno", "Seleccionar Copia de Seguridad para Restaurar");
                if (Directory.Exists(DirBackups))
                    ofd.InitialDirectory = DirBackups;

                if (ofd.ShowDialog() != DialogResult.OK) return;
                await Restaurar(ofd.FileName);
            }
        }

        private async Task Restaurar(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return;

            // RF-08 — Informar el ALCANCE de la pérdida: todo lo creado/modificado después de la
            // fecha del backup se perderá. Se lee la fecha real del header del .bak.
            string alcance;
            DateTime? fechaBackup = null;
            try { fechaBackup = _bll.ObtenerFechaBackup(ruta); } catch { /* header ilegible */ }
            if (fechaBackup.HasValue)
            {
                var antiguedad = DateTime.Now - fechaBackup.Value;
                alcance = string.Format(
                    Tr("msg.backup.alcance",
                      "\n\nEl backup es del {0} (hace {1} día(s)).\nSe PERDERÁN todos los cambios posteriores a esa fecha."),
                    fechaBackup.Value.ToString("dd/MM/yyyy HH:mm"),
                    Math.Max(0, (int)antiguedad.TotalDays));

                // RF-08 — Detalle a nivel REGISTRO: qué se perderá concretamente, no solo "todo lo
                // posterior a la fecha". Se cuentan las filas de la base actual posteriores al backup.
                alcance += ConstruirDetallePerdida(fechaBackup);
            }
            else
            {
                alcance = Tr("msg.backup.alcance.desconocido",
                    "\n\nNo se pudo determinar la fecha del backup. Se perderán todos los cambios posteriores a su creación.");
            }

            string msg = string.Format(
                Tr("msg.backup.confirmrestaura",
                    "¿Restaurar la base de datos desde:\n\"{0}\"?\n\nEsta operación sobrescribirá todos los datos actuales\ny reiniciará la aplicación."),
                Path.GetFileName(ruta)) + alcance;

            if (MessageBox.Show(msg, Tr("msg.backup.titulorestaura", "Confirmar Restauración"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // Si el backup está cifrado (.wfbak), pedir la contraseña para descifrarlo.
            string clave = null;
            if (BLL.Backup.EsCifrado(ruta))
            {
                clave = PedirClaveExistente();
                if (clave == null) return;   // cancelado
            }

            try
            {
                await EjecutarConEsperaAsync(() => _bll.RestaurarBackup(this.Text, ruta, clave));
                MessageBox.Show(
                    Tr("msg.backup.restauradaexito", "Base de datos restaurada con éxito.\nLa aplicación se reiniciará."),
                    Tr("msg.backup.restauradatitulo", "Restauración Exitosa"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Tr("msg.backup.errorrestaurar", "Error al restaurar:\n{0}"), ex.Message),
                    Tr("msg.error.titulo", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // RF-08 — Construye el desglose de lo que se perderá al restaurar: cuántos registros de
        // cada entidad existen en la base actual con fecha posterior a la del backup. Si no hay
        // ninguno, lo informa explícitamente (restaurar es seguro respecto a datos posteriores).
        private string ConstruirDetallePerdida(DateTime? fechaBackup)
        {
            try
            {
                var cambios = _bll.ObtenerCambiosDesde(fechaBackup);
                if (cambios == null || cambios.Count == 0)
                    return Tr("msg.backup.sinperdida",
                        "\n\nNo hay registros nuevos posteriores a esa fecha: no se perdería información reciente.");

                var sb = new System.Text.StringBuilder();
                sb.Append(Tr("msg.backup.perdida.titulo",
                    "\n\nSe perderán estos registros creados después del backup:"));
                foreach (var c in cambios)
                    sb.Append($"\n  • {c.Entidad}: {c.Cantidad}");
                return sb.ToString();
            }
            catch
            {
                // El preview es informativo; si el conteo falla no debe impedir la restauración.
                return string.Empty;
            }
        }

        // Pide una contraseña NUEVA (con confirmación) para cifrar un backup. null = cancelar/invalida.
        private string PedirClaveNueva()
        {
            using (var d1 = new InputDialog(
                Tr("dlg.backup.clave.titulo", "Contraseña del backup"),
                Tr("dlg.backup.clave.nueva", "Ingresá una contraseña para CIFRAR el backup.\nLa vas a necesitar para restaurarlo (no se puede recuperar)."),
                esPassword: true))
            {
                if (d1.ShowDialog(this) != DialogResult.OK) return null;
                string p1 = d1.InputText;
                if (string.IsNullOrEmpty(p1))
                {
                    MessageBox.Show(Tr("dlg.backup.clave.vacia", "La contraseña no puede estar vacía."),
                        Tr("msg.error.titulo", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                using (var d2 = new InputDialog(
                    Tr("dlg.backup.clave.titulo", "Contraseña del backup"),
                    Tr("dlg.backup.clave.repetir", "Repetí la contraseña para confirmar:"),
                    esPassword: true))
                {
                    if (d2.ShowDialog(this) != DialogResult.OK) return null;
                    if (d2.InputText != p1)
                    {
                        MessageBox.Show(Tr("dlg.backup.clave.nocoincide", "Las contraseñas no coinciden."),
                            Tr("msg.error.titulo", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                }
                return p1;
            }
        }

        // Pide la contraseña de un backup EXISTENTE para descifrarlo. null = cancelar.
        private string PedirClaveExistente()
        {
            using (var d = new InputDialog(
                Tr("dlg.backup.clave.titulo", "Contraseña del backup"),
                Tr("dlg.backup.clave.ingresar", "Ingresá la contraseña con la que se cifró este backup:"),
                esPassword: true))
            {
                return d.ShowDialog(this) == DialogResult.OK ? d.InputText : null;
            }
        }

        // Ejecuta una operación de backup/restauración (I/O de archivos potencialmente grande)
        // en un hilo de background, deshabilitando los controles y mostrando el cursor de espera
        // mientras corre — antes eran síncronas en el hilo de UI y con una base grande la ventana
        // podía marcarse "No responde".
        private async Task<T> EjecutarConEsperaAsync<T>(Func<T> operacion)
        {
            this.Cursor = Cursors.WaitCursor;
            SetControlesHabilitados(false);
            try
            {
                return await Task.Run(operacion);
            }
            finally
            {
                SetControlesHabilitados(true);
                this.Cursor = Cursors.Default;
            }
        }

        private Task EjecutarConEsperaAsync(Action operacion)
            => EjecutarConEsperaAsync<object>(() => { operacion(); return null; });

        private void SetControlesHabilitados(bool habilitado)
        {
            btnCrear.Enabled     = habilitado;
            btnInicial.Enabled   = habilitado;
            btnExterno.Enabled   = habilitado;
            lstBackups.Enabled   = habilitado;
            bool haySeleccion    = habilitado && lstBackups.SelectedItems.Count > 0;
            btnEliminar.Enabled  = haySeleccion;
            btnRestaurar.Enabled = haySeleccion;
        }
    }
}
