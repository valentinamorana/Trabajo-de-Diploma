using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GUI.Inteligencia;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PN03 — Umbrales del motor de recomendaciones (Centro de decisiones comerciales): cada valor con su
    /// rango, el de fábrica y la explicación de qué decisión dispara. La validación (rango, enteros,
    /// coherencia entre umbrales) y el guardado en ParametroSistema son de BLL.Inteligencia.UmbralesComerciales.
    /// </summary>
    public partial class UmbralesComercialesDialog : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly List<BE.UmbralComercial> _umbrales;
        private readonly BLL.Inteligencia.UmbralesComerciales _bll;
        private readonly string _modulo;

        public UmbralesComercialesDialog(List<BE.UmbralComercial> umbrales, BLL.Inteligencia.UmbralesComerciales bll, string modulo)
        {
            InitializeComponent();
            _umbrales = umbrales ?? new List<BE.UmbralComercial>();
            _bll = bll ?? throw new ArgumentNullException(nameof(bll));
            _modulo = modulo;
            Estilos.EstiloFormulario.Grilla(dgvUmbrales);
            Estilos.EstiloFormulario.BotonPrimario(btnGuardar);
            Estilos.EstiloFormulario.BotonSecundario(btnRestablecer);
            colValor.DefaultCellStyle.BackColor = Tema.FondoInfo;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
            Cargar(false);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            Cargar(false, conservarValores: true);
        }

        private void Traducir(Idioma idioma)
        {
            Text = Tr("cdc.umbrales.titulo", "Umbrales del motor de recomendaciones");
            lblTitulo.Text = Text;
            lblInfo.Text = Tr("cdc.umbrales.info", "Edite la columna Valor. Cada umbral decide cuándo el motor propone una acción; los cambios rigen desde el próximo análisis.");
            colNombre.HeaderText = Tr("cdc.col.umbral", "Umbral");
            colValor.HeaderText = Tr("cdc.col.valor", "Valor");
            colRango.HeaderText = Tr("cdc.col.rango", "Rango");
            colDefecto.HeaderText = Tr("cdc.col.defecto", "De fábrica");
            colExplicacion.HeaderText = Tr("cdc.col.explicacion", "Qué decide");
            btnGuardar.Text = Tr("cdc.umbrales.guardar", "Guardar");
            btnRestablecer.Text = Tr("cdc.umbrales.restablecer", "Restablecer valores de fábrica");
            btnCancelar.Text = Tr("btn.cancelar", "Cancelar");
        }

        private void Cargar(bool deFabrica, bool conservarValores = false)
        {
            // Los valores editados se conservan por la clave del umbral (Tag de cada fila), no por la
            // posición: el usuario pudo haber ordenado la grilla por un encabezado.
            var actuales = new Dictionary<string, string>();
            if (conservarValores)
                foreach (DataGridViewRow r in dgvUmbrales.Rows)
                    if (r.Tag is BE.UmbralComercial previo) actuales[previo.Clave] = Convert.ToString(r.Cells[colValor.Index].Value);
            dgvUmbrales.Rows.Clear();
            for (int i = 0; i < _umbrales.Count; i++)
            {
                var u = _umbrales[i];
                string formato = u.Decimales > 0 ? "0.0" : "0";
                string valor = conservarValores && actuales.TryGetValue(u.Clave, out var editado)
                    ? editado : (deFabrica ? u.PorDefecto : u.Valor).ToString(formato);
                int fila = dgvUmbrales.Rows.Add(TextosComerciales.R(u.Nombre), valor,
                    $"{u.Minimo.ToString(formato)} – {u.Maximo.ToString(formato)}", u.PorDefecto.ToString(formato), TextosComerciales.R(u.Explicacion));
                dgvUmbrales.Rows[fila].Tag = u;
            }
        }

        private void DgvUmbrales_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != colValor.Index) return;
            if (!decimal.TryParse(Convert.ToString(e.FormattedValue), out _))
            {
                MostrarError(Tr("cdc.umbrales.numero", "Ingrese un número."));
                e.Cancel = true;
            }
        }

        private void BtnRestablecer_Click(object sender, EventArgs e) => Cargar(true);

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            var valores = new Dictionary<string, decimal>();
            foreach (DataGridViewRow r in dgvUmbrales.Rows)
            {
                var u = (BE.UmbralComercial)r.Tag;
                if (!decimal.TryParse(Convert.ToString(r.Cells[colValor.Index].Value), out var v))
                {
                    MostrarError(Tr("cdc.umbrales.numero", "Ingrese un número."));
                    return;
                }
                valores[u.Clave] = v;
            }
            try
            {
                _bll.Guardar(_modulo, valores);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
