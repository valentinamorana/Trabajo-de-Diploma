using System.Drawing;
using System.Windows.Forms;

namespace GUI.Inteligencia
{
    /// <summary>
    /// Tarjeta de un KPI de la pestaña Analizar: nombre, valor, variación contra el período anterior
    /// (▲/▼ en verde si es favorable, rojo si no) y la interpretación de una línea. Se crea en tiempo de
    /// ejecución porque la cantidad de KPI la decide la BLL; el contenedor (flpKpis) está en el Designer.
    /// </summary>
    internal sealed class TarjetaKpi : Panel
    {
        private readonly Label lblNombre = new Label();
        private readonly Label lblValor = new Label();
        private readonly Label lblVariacion = new Label();
        private readonly Label lblInterpretacion = new Label();

        public TarjetaKpi(BE.KpiComercial kpi, ToolTip tip)
        {
            Size = new Size(280, 88);
            Margin = new Padding(6);
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;

            lblNombre.SetBounds(8, 6, 264, 16);
            lblNombre.Font = Tema.FuentePequena;
            lblNombre.ForeColor = Tema.TextoMuted;
            lblNombre.AutoEllipsis = true;

            lblValor.SetBounds(8, 22, 150, 26);
            lblValor.Font = Tema.FuenteTitulo;
            lblValor.ForeColor = Tema.Tinta;

            lblVariacion.SetBounds(160, 26, 112, 20);
            lblVariacion.Font = Tema.FuenteSubtitulo;
            lblVariacion.TextAlign = ContentAlignment.MiddleRight;

            lblInterpretacion.SetBounds(8, 50, 264, 34);
            lblInterpretacion.Font = Tema.FuentePequena;
            lblInterpretacion.ForeColor = Tema.TextoSecundario;
            lblInterpretacion.AutoEllipsis = true;

            Controls.AddRange(new Control[] { lblNombre, lblValor, lblVariacion, lblInterpretacion });
            Mostrar(kpi, tip);
        }

        public void Mostrar(BE.KpiComercial kpi, ToolTip tip)
        {
            lblNombre.Text = TextosComerciales.R(kpi.Nombre);
            lblValor.Text = TextosComerciales.ValorKpi(kpi.Unidad, kpi.Valor);
            lblVariacion.Text = TextosComerciales.Variacion(kpi);
            lblVariacion.ForeColor = kpi.EsFavorable == true ? Tema.Exito : kpi.EsFavorable == false ? Tema.Error : Tema.TextoMuted;
            lblInterpretacion.Text = TextosComerciales.R(kpi.Interpretacion);
            string anterior = kpi.ValorAnterior.HasValue
                ? TextosComerciales.Tr("cdc.kpi.anterior", "Período anterior: {0}", TextosComerciales.ValorKpi(kpi.Unidad, kpi.ValorAnterior.Value))
                : TextosComerciales.Tr("cdc.kpi.sinanterior", "Indicador sin historia: es una foto de hoy.");
            string texto = lblInterpretacion.Text + "\n" + anterior;
            foreach (Control c in Controls) tip?.SetToolTip(c, texto);
            tip?.SetToolTip(this, texto);
        }
    }
}
