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
    /// Solo presenta: los pasos y la persistencia viven en la BLL.
    /// </summary>
    internal sealed class GuiaPrimerUsoForm : FormBase, IIdiomaObserver
    {
        private readonly BE.Usuario _usuario;
        private readonly BLL.GuiaPrimerUso _preferencia;
        private readonly IList<BLL.GuiaPrimerUso.Paso> _pasos;

        private readonly Label lblTitulo;
        private readonly Label lblIntro;
        private readonly TableLayoutPanel tblPasos;
        private readonly List<Label> _lblPasos = new List<Label>();
        private readonly CheckBox chkNoMostrar;
        private readonly Button btnComenzar;

        private const int AnchoTexto = 500;

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

            Text            = "Bienvenido a WardrobeFlow";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            BackColor       = Tema.Papel;
            AutoScaleMode   = AutoScaleMode.Font;
            Font            = Tema.FuenteNormal;
            AutoSize        = true;
            AutoSizeMode    = AutoSizeMode.GrowAndShrink;
            Padding         = new Padding(0, 0, 0, 12);

            lblTitulo = new Label
            {
                Dock = DockStyle.Top, Height = 46, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0), BackColor = Tema.RosaOscuro, ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold), Text = "Bienvenido a WardrobeFlow"
            };

            lblIntro = new Label
            {
                AutoSize = true, MaximumSize = new Size(AnchoTexto + 40, 0), ForeColor = Tema.Tinta,
                Font = Tema.FuenteSubtitulo, Margin = new Padding(0, 0, 0, 10)
            };

            tblPasos = new TableLayoutPanel
            {
                AutoSize = true, ColumnCount = 2, GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Margin = new Padding(0)
            };
            tblPasos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            tblPasos.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < _pasos.Count; i++)
            {
                var numero = new Label
                {
                    Text = (i + 1).ToString(), Width = 26, Height = 26, TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Tema.RosaPrimario, ForeColor = Color.White, Font = Tema.FuenteSubtitulo,
                    Margin = new Padding(0, 2, 8, 10)
                };
                var texto = new Label
                {
                    AutoSize = true, MaximumSize = new Size(AnchoTexto, 0), ForeColor = Tema.Tinta,
                    Margin = new Padding(0, 4, 0, 10)
                };
                _lblPasos.Add(texto);
                tblPasos.Controls.Add(numero, 0, i);
                tblPasos.Controls.Add(texto, 1, i);
            }

            chkNoMostrar = new CheckBox
            {
                AutoSize = true, ForeColor = Tema.TextoSecundario, Margin = new Padding(0, 12, 0, 0),
                Checked = !_preferencia.DebeMostrarse(usuario?.Username)
            };

            btnComenzar = new Button
            {
                AutoSize = true, MinimumSize = new Size(120, 32), DialogResult = DialogResult.OK,
                BackColor = Tema.RosaPrimario, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 8, 0, 0), Anchor = AnchorStyles.Right
            };
            btnComenzar.FlatAppearance.BorderSize = 0;

            var cuerpo = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
                Padding = new Padding(18, 14, 18, 0), Dock = DockStyle.Top
            };
            cuerpo.Controls.Add(lblIntro);
            cuerpo.Controls.Add(tblPasos);
            cuerpo.Controls.Add(chkNoMostrar);
            cuerpo.Controls.Add(btnComenzar);

            Controls.Add(cuerpo);
            Controls.Add(lblTitulo);
            AcceptButton = btnComenzar;
            CancelButton = btnComenzar;
            MinimumSize  = new Size(AnchoTexto + 110, 0);
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
