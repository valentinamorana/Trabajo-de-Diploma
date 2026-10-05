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
    public partial class MiPerfilForm : FormBase, IIdiomaObserver
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
            PoblarOpciones();
        }

        // Opción de combo con VALOR guardado fijo (el que entiende BE.Preferencia: "Chico",
        // "Normal", "Grande", "Claro", "Oscuro") y TEXTO traducido. Antes el combo mostraba y
        // guardaba el texto en español, así que no se podía traducir sin romper lo guardado.
        private sealed class Opcion
        {
            public string Valor { get; }
            public string Texto { get; }
            public Opcion(string valor, string texto) { Valor = valor; Texto = texto; }
            public override string ToString() => Texto;
        }

        // (Re)carga los combos de tamaño y tema con el texto del idioma activo, conservando
        // la opción elegida.
        private void PoblarOpciones()
        {
            string tamSel  = ValorSeleccionado(cmbTamano);
            string temaSel = ValorSeleccionado(cmbTema);

            cmbTamano.Items.Clear();
            cmbTamano.Items.Add(new Opcion("Chico",  Tr("perfil.tamano.chico",  "Chico")));
            cmbTamano.Items.Add(new Opcion("Normal", Tr("perfil.tamano.normal", "Normal")));
            cmbTamano.Items.Add(new Opcion("Grande", Tr("perfil.tamano.grande", "Grande")));

            cmbTema.Items.Clear();
            cmbTema.Items.Add(new Opcion("Claro",  Tr("perfil.tema.claro",  "Claro")));
            cmbTema.Items.Add(new Opcion("Oscuro", Tr("perfil.tema.oscuro", "Oscuro")));

            if (tamSel  != null) SeleccionarValor(cmbTamano, tamSel,  "Normal");
            if (temaSel != null) SeleccionarValor(cmbTema,   temaSel, "Claro");
        }

        private static string ValorSeleccionado(ComboBox cmb) =>
            (cmb.SelectedItem as Opcion)?.Valor ?? cmb.SelectedItem?.ToString();

        // Selecciona por valor guardado (sin distinguir mayúsculas). Un valor desconocido
        // (guardado por una versión anterior) se agrega tal cual para no perderlo.
        private static void SeleccionarValor(ComboBox cmb, string valor, string fallback)
        {
            string v = string.IsNullOrEmpty(valor) ? fallback : valor;
            for (int i = 0; i < cmb.Items.Count; i++)
                if (cmb.Items[i] is Opcion o && string.Equals(o.Valor, v, StringComparison.OrdinalIgnoreCase))
                { cmb.SelectedIndex = i; return; }
            cmb.SelectedIndex = cmb.Items.Add(new Opcion(v, v));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CargarIdiomas();
            CargarPreferencias();
            try { PreferenciasUI.Aplicar(this); } catch { }
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
            SeleccionarValor(cmbTamano, _pref.FuenteTamano, "Normal");
            SeleccionarValor(cmbTema,   _pref.Tema,         "Claro");
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
                    FuenteTamano   = ValorSeleccionado(cmbTamano) ?? "Normal",
                    Tema           = ValorSeleccionado(cmbTema)   ?? "Claro",
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

                lblEstado.ForeColor = Tema.Exito;
                lblEstado.Text = Tr("perfil.guardado", "Preferencias guardadas.");
            }
            catch (Exception ex)
            {
                lblEstado.ForeColor = Tema.Error;
                lblEstado.Text = MensajeDeError(ex);
            }
        }

        public void UpdateLanguage(Idioma idioma)
        {
            this.Text          = Tr("perfil.frm.titulo", "Mi Perfil");
            lblTitulo.Text     = Tr("perfil.frm.titulo", "Mi Perfil");
            lblUsuarioCap.Text = Tr("perfil.usuario", "Usuario:");
            lblPerfilCap.Text  = Tr("perfil.perfil", "Perfil / Rol:");
            lblPerfilVal.Text  = TraductorPerfil.Nombre(_usuario?.Perfil);
            lblSeccion.Text    = Tr("perfil.seccion", "Preferencias");
            lblIdiomaCap.Text  = Tr("perfil.idioma", "Idioma preferido:");
            lblFuenteCap.Text  = Tr("perfil.fuente", "Tipografía:");
            lblTamanoCap.Text  = Tr("perfil.tamano", "Tamaño de letra:");
            lblTemaCap.Text    = Tr("perfil.tema", "Tema:");
            lblFechaCap.Text   = Tr("perfil.fecha", "Formato de fecha:");
            chkNotif.Text      = Tr("perfil.notif", "Recibir notificaciones");
            btnGuardar.Text    = Tr("perfil.btn.guardar", "Guardar preferencias");
            btnDefault.Text    = Tr("perfil.btn.default", "Restaurar valores de fábrica");
            PoblarOpciones();
        }

        private void BtnDefault_Click(object sender, EventArgs e) => RestaurarDefault();

        // Restaura las preferencias de UI a los valores por defecto y las guarda/aplica.
        // (El idioma no se toca: es una preferencia aparte.)
        private void RestaurarDefault()
        {
            SeleccionarOAgregar(cmbFuente, "Segoe UI",   "Segoe UI");
            SeleccionarValor(cmbTamano, "Normal", "Normal");
            SeleccionarValor(cmbTema,   "Claro",  "Claro");
            SeleccionarOAgregar(cmbFecha,  "dd/MM/yyyy", "dd/MM/yyyy");
            chkNotif.Checked = true;
            Guardar();
        }
    }
}
