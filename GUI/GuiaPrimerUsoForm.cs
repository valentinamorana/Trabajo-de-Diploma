using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// USABILIDAD — Guía de primer uso ("Bienvenido a WardrobeFlow"): 5 pasos adaptados al rol
    /// (BLL.GuiaPrimerUso.PasosPara). Se abre sola en el primer ingreso del usuario, después de
    /// que el Menú muestra su Panel de Control, y en cualquier momento desde Sesión → Ver guía de
    /// inicio. "No volver a mostrar" se guarda por usuario con BLL.GuiaPrimerUso (archivo local).
    /// Solo presenta: los pasos y la persistencia viven en la BLL. Hecho con el Diseñador.
    /// </summary>
    internal sealed partial class GuiaPrimerUsoForm : FormBase, IIdiomaObserver
    {
        private readonly BE.Usuario _usuario;
        private readonly BLL.GuiaPrimerUso _preferencia;
        private readonly IList<BLL.GuiaPrimerUso.Paso> _pasos;

        private readonly List<Label> _lblPasos = new List<Label>();

        /// <remarks>
        /// "No volver a mostrar" refleja la preferencia guardada: al abrirse sola (el usuario todavía
        /// no la descartó) viene DESMARCADA, así la guía reaparece en cada ingreso hasta que el usuario
        /// la marque; abierta desde el menú por alguien que ya la descartó, viene marcada.
        /// </remarks>
        public GuiaPrimerUsoForm(BE.Usuario usuario, BLL.GuiaPrimerUso preferencia)
        {
            _usuario     = usuario;
            _preferencia = preferencia ?? new BLL.GuiaPrimerUso();
            _pasos       = BLL.GuiaPrimerUso.PasosPara(usuario?.Perfil, usuario?.EsAdministrador == true);

            InitializeComponent();
            chkNoMostrar.Checked = !_preferencia.DebeMostrarse(usuario?.Username);

            // Los pasos están en el Diseñador (hasta 5: los 3 del rol y los 2 comunes); los que el rol
            // no usa se ocultan.
            _lblPasos.AddRange(new[] { lblPaso1, lblPaso2, lblPaso3, lblPaso4, lblPaso5 });
            var numeros = new[] { lblNumero1, lblNumero2, lblNumero3, lblNumero4, lblNumero5 };
            for (int i = 0; i < _lblPasos.Count; i++)
                numeros[i].Visible = _lblPasos[i].Visible = i < _pasos.Count;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir();
            ActiveControl = btnComenzar;
        }

        public void UpdateLanguage(Idioma idioma) => Traducir();

        private void Traducir()
        {
            string titulo = Tr("ux.guia.titulo", "Bienvenido a WardrobeFlow");
            Text           = titulo;
            lblTitulo.Text = titulo;
            const string IntroDeFabrica = "Hola, {0}. Estos son los primeros pasos para su rol, {1}:";
            lblIntro.Text  = BLL.MapaNavegacion.FormatoSeguro(Tr("ux.guia.intro", IntroDeFabrica), IntroDeFabrica,
                _usuario?.Username ?? "", GUI.Menu.RolTraducido(_usuario));
            for (int i = 0; i < _pasos.Count && i < _lblPasos.Count; i++)
                _lblPasos[i].Text = Tr(_pasos[i].Clave, _pasos[i].TextoEs);
            chkNoMostrar.Text = Tr("ux.guia.nomostrar", "No volver a mostrar al iniciar sesión");
            btnComenzar.Text  = Tr("ux.guia.comenzar", "Comenzar");
        }

        // La preferencia se guarda al cerrar (con el botón, Esc o la X).
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                string username = _usuario?.Username;
                if (chkNoMostrar.Checked) _preferencia.NoVolverAMostrar(username);
                else                      _preferencia.VolverAMostrar(username);
            }
            finally { base.OnFormClosed(e); }
        }
    }
}
