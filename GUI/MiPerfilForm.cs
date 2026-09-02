using System;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// "Mi Perfil" — preferencias del usuario en sesión (RF-22/23 + personalización de UI).
    /// El usuario ve sus datos y configura: idioma, tipografía, tamaño de letra, tema
    /// (claro/oscuro), formato de fecha y notificaciones. Todo se guarda en la BD
    /// (Usuario.IdIdioma + tabla Preferencia) y se aplica al instante (idioma por Observer;
    /// fuente/tema re-aplicados a los formularios abiertos).
    /// </summary>
    public partial class MiPerfilForm : Form, IIdiomaObserver
    {
        private readonly BE.Usuario  _usuario;
        private readonly BLL.Usuario _usuarioBLL = new BLL.Usuario();
        private BE.Preferencia       _pref;

        public MiPerfilForm(BE.Usuario usuario)
        {
            _usuario = usuario ?? new BLL.Usuario().ObtenerUsuarioActivo();
            try { _pref = new BLL.Preferencia().Obtener(_usuario?.Id ?? 0); }
            catch { _pref = new BE.Preferencia(); }

            InitializeComponent();

            lblUsuarioVal.Text = _usuario?.Username ?? "—";
            lblPerfilVal.Text  = TraductorPerfil.Nombre(_usuario?.Perfil);
        }

        private string T(string k, string fb)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(k) ? t[k].Texto : fb;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try { string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (System.IO.File.Exists(ico)) this.Icon = new Icon(ico); } catch { }
            GestorIdioma.SuscribirObservador(this);
            CargarIdiomas();
            CargarPreferencias();
            try { PreferenciasUI.Aplicar(this); } catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        // Combo de idioma: cargado EN VIVO desde la tabla Idioma (un idioma nuevo aparece solo).
        private void CargarIdiomas()
        {
            System.Collections.Generic.IList<Idioma> idiomas;
            try { idiomas = new BLL.IdiomaService().ObtenerIdiomasActivosComoIdioma(); }
            catch { idiomas = Traductor.ObtenerIdiomas(); }

            cmbIdioma.DisplayMember = "Nombre";
            cmbIdioma.ValueMember   = "Id";
            cmbIdioma.DataSource     = idiomas;

            string actual = _usuario?.IdIdioma ?? GestorIdioma.IdiomaActual?.Id ?? "ES";
            for (int i = 0; i < idiomas.Count; i++)
                if (string.Equals(idiomas[i].Id, actual, StringComparison.OrdinalIgnoreCase)) { cmbIdioma.SelectedIndex = i; break; }
        }

        // Preselecciona los combos de preferencias con lo guardado en BD.
        private void CargarPreferencias()
        {
            SeleccionarOAgregar(cmbFuente, _pref.FuenteFamilia, "Segoe UI");
            SeleccionarOAgregar(cmbTamano, _pref.FuenteTamano,  "Normal");
            SeleccionarOAgregar(cmbTema,   _pref.Tema,          "Claro");
            SeleccionarOAgregar(cmbFecha,  _pref.FormatoFecha,  "dd/MM/yyyy");
            chkNotif.Checked = _pref.Notificaciones;
        }

        private static void SeleccionarOAgregar(ComboBox cmb, string valor, string fallback)
        {
            string v = string.IsNullOrEmpty(valor) ? fallback : valor;
            int idx = cmb.Items.IndexOf(v);
            if (idx < 0) idx = cmb.Items.Add(v);
            cmb.SelectedIndex = idx;
        }

        private void BtnGuardar_Click(object sender, EventArgs e) => Guardar();

        private void Guardar()
        {
            try
            {
                // 1) Preferencias de UI → BD (tabla Preferencia).
                var pref = new BE.Preferencia
                {
                    IdUsuario      = _usuario.Id,
                    FuenteFamilia  = cmbFuente.SelectedItem?.ToString() ?? "Segoe UI",
                    FuenteTamano   = cmbTamano.SelectedItem?.ToString() ?? "Normal",
                    Tema           = cmbTema.SelectedItem?.ToString()   ?? "Claro",
                    FormatoFecha   = cmbFecha.SelectedItem?.ToString()  ?? "dd/MM/yyyy",
                    Notificaciones = chkNotif.Checked
                };
                new BLL.Preferencia().Guardar(pref);
                _pref = pref;
                PreferenciasUI.Set(pref);

                // 2) Idioma → Usuario.IdIdioma + aplicar por Observer.
                var idioma = cmbIdioma.SelectedItem as Idioma;
                if (idioma != null)
                {
                    _usuarioBLL.GuardarPreferenciaIdioma(_usuario.Id, idioma.Id);
                    if (_usuario != null) _usuario.IdIdioma = idioma.Id;
                    try { GestorIdioma.CambiarIdioma(idioma, new BLL.IdiomaService().CargarTraducciones(idioma.Id)); }
                    catch { GestorIdioma.CambiarIdioma(idioma); }
                }

                // 3) Aplicar fuente/tema en vivo a todos los formularios abiertos.
                PreferenciasUI.ReaplicarTodo();

                lblEstado.ForeColor = Color.FromArgb(40, 140, 60);
                lblEstado.Text = "✓ " + T("perfil.guardado", "Preferencias guardadas.");
            }
            catch (Exception ex)
            {
                lblEstado.ForeColor = Color.FromArgb(180, 50, 50);
                string msg = ex is BE.AppException appEx
                    ? Traductor.Resolver(appEx.Clave, ex.Message, appEx.Args, GestorIdioma.IdiomaActual)
                    : ex.Message;
                lblEstado.Text = "✗ " + msg;
            }
        }

        public void UpdateLanguage(Idioma idioma)
        {
            this.Text          = T("perfil.frm.titulo", "Mi Perfil");
            lblTitulo.Text     = T("perfil.frm.titulo", "Mi Perfil");
            lblUsuarioCap.Text = T("perfil.usuario", "Usuario:");
            lblPerfilCap.Text  = T("perfil.perfil", "Perfil / Rol:");
            lblPerfilVal.Text  = TraductorPerfil.Nombre(_usuario?.Perfil);
            lblSeccion.Text    = T("perfil.seccion", "Preferencias");
            lblIdiomaCap.Text  = T("perfil.idioma", "Idioma preferido:");
            lblFuenteCap.Text  = T("perfil.fuente", "Tipografía:");
            lblTamanoCap.Text  = T("perfil.tamano", "Tamaño de letra:");
            lblTemaCap.Text    = T("perfil.tema", "Tema:");
            lblFechaCap.Text   = T("perfil.fecha", "Formato de fecha:");
            chkNotif.Text      = T("perfil.notif", "Recibir notificaciones");
            btnGuardar.Text    = T("perfil.btn.guardar", "Guardar preferencias");
            btnDefault.Text    = T("perfil.btn.default", "Restaurar valores de fábrica");
        }

        private void BtnDefault_Click(object sender, EventArgs e) => RestaurarDefault();

        // Restaura las preferencias de UI a los valores por defecto y las guarda/aplica.
        // (El idioma no se toca: es una preferencia aparte.)
        private void RestaurarDefault()
        {
            SeleccionarOAgregar(cmbFuente, "Segoe UI",   "Segoe UI");
            SeleccionarOAgregar(cmbTamano, "Normal",     "Normal");
            SeleccionarOAgregar(cmbTema,   "Claro",      "Claro");
            SeleccionarOAgregar(cmbFecha,  "dd/MM/yyyy", "dd/MM/yyyy");
            chkNotif.Checked = true;
            Guardar();
        }
    }
}
