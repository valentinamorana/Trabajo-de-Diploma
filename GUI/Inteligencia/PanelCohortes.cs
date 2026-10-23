using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static GUI.Inteligencia.TextosComerciales;
using Bll = BLL.Inteligencia.AnalisisCohortes;

namespace GUI.Inteligencia
{
    /// <summary>
    /// Pestaña «Cohortes» del Centro de decisiones comerciales (PN03): mapa de calor de la retención por
    /// mes de alta, la curva de retención promedio y la peor cohorte. Solo presenta la
    /// <see cref="BE.MatrizCohortes"/> que calcula BLL.Inteligencia.AnalisisCohortes.
    ///
    /// Movimiento: doble clic en la peor cohorte o el botón «Actuar sobre la peor cohorte» levantan
    /// <see cref="ActuarSobrePeorCohorte"/>; el formulario decide adónde llevar al usuario (la
    /// recomendación de bajas del plan o el Simulador). Así el control no conoce al formulario.
    ///
    /// La grilla se puede ordenar por cualquier encabezado: cada fila se resuelve por su dato (el mes de
    /// la cohorte), nunca por su posición en pantalla.
    /// </summary>
    internal sealed partial class PanelCohortes : UserControl
    {
        // Tramos del mapa de calor: BLL AnalisisCohortes.Nivel, sobre el mismo % entero que muestra la celda.
        private static readonly Color ColorAlto = Color.FromArgb(160, 215, 170);
        private static readonly Color ColorMedio = Tema.FondoExito;
        private static readonly Color ColorBajo = Tema.FondoAlerta;
        private static readonly Color ColorCritico = Tema.FondoError;

        private const string ColCohorte = "Cohorte", ColClientes = "Clientes", PrefijoMes = "M";

        private Font fuentePeor;
        private BE.MatrizCohortes _matriz;

        /// <summary>El usuario pidió actuar sobre la peor cohorte (botón o doble clic en su fila).</summary>
        public event EventHandler<BE.PeorCohorte> ActuarSobrePeorCohorte;

        public BE.MatrizCohortes Matriz => _matriz;

        public PanelCohortes()
        {
            InitializeComponent();
            // Estilo de la aplicación; la disposición está en el Diseñador (PanelCohortes.Designer.cs).
            Estilos.EstiloFormulario.Grilla(dgv);
            Estilos.EstiloFormulario.BotonPrimario(btnActuar);
        }

        private void BtnActuar_Click(object sender, EventArgs e) => Actuar();
        private void Dgv_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e) => dgv.ClearSelection();

        // La primera vez que la pestaña toma su tamaño real, la grilla ocupa un poco más de la mitad.
        private bool _splitAjustado;
        private void Split_SizeChanged(object sender, EventArgs e)
        {
            if (_splitAjustado || split.Height < 240) return;
            _splitAjustado = true;
            try { split.SplitterDistance = (int)(split.Height * 0.55); } catch (InvalidOperationException) { }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            lblExplicacion.MaximumSize = new Size(Math.Max(100, ClientSize.Width - Padding.Horizontal), 0);
        }

        // ══ Idioma ═══════════════════════════════════════════════════════════════

        public void Traducir()
        {
            lblExplicacion.Text = Tr("cdc.cohorte.explicacion",
                "Cómo leerlo: cada fila agrupa a los clientes por el mes en que se suscribieron y cada columna muestra qué % de ellos sigue activo (sin baja) k meses después del alta; Mes 0 es el mes del alta y * marca un mes todavía en curso.");
            btnActuar.Text = Tr("cdc.cohorte.btn.actuar", "Actuar sobre la peor cohorte");
            tip.SetToolTip(btnActuar, Tr("cdc.cohorte.tip.actuar",
                "Lleva a la recomendación de bajas del plan que más explica la peor cohorte (o, si no la hay, al Simulador). También con doble clic en su fila."));
            ArmarLeyenda();
            Mostrar(_matriz);
        }

        private void ArmarLeyenda()
        {
            flpLeyenda.SuspendLayout();
            foreach (Control c in flpLeyenda.Controls.Cast<Control>().ToList()) c.Dispose();
            flpLeyenda.Controls.Clear();
            void Item(Color fondo, string texto, Color? tinta = null) =>
                flpLeyenda.Controls.Add(new MuestraLeyenda(texto, fondo, tinta ?? Tema.Tinta) { Margin = new Padding(0, 2, 6, 2) });
            Item(Tema.Papel, Tr("cdc.cohorte.leyenda.titulo", "Retención:"), Tema.TextoMuted);
            Item(ColorAlto, Tr("cdc.cohorte.leyenda.alta", "{0} % o más", Bll.TramoAlto));
            Item(ColorMedio, Tr("cdc.cohorte.leyenda.media", "{0} a {1} %", Bll.TramoMedio, Bll.TramoAlto - 1));
            Item(ColorBajo, Tr("cdc.cohorte.leyenda.baja", "{0} a {1} %", Bll.TramoBajo, Bll.TramoMedio - 1));
            Item(ColorCritico, Tr("cdc.cohorte.leyenda.critica", "menos de {0} %", Bll.TramoBajo));
            Item(Color.White, Tr("cdc.cohorte.leyenda.parcial", "* mes en curso (no entra en el promedio)"), Tema.TextoMuted);
            Item(Color.White, Tr("cdc.cohorte.leyenda.peor", "▶ peor cohorte (doble clic para actuar)"), Tema.Error);
            flpLeyenda.ResumeLayout();
        }

        // ══ Datos ═══════════════════════════════════════════════════════════════

        /// <summary>Dibuja la matriz (null = todavía no se analizó).</summary>
        public void Mostrar(BE.MatrizCohortes matriz)
        {
            _matriz = matriz;
            var t = new DataTable();
            t.Columns.Add(ColCohorte, typeof(DateTime));
            t.Columns.Add(ColClientes, typeof(int));
            int columnas = matriz == null || matriz.SinDatos ? 0 : matriz.MaximoDesplazamiento + 1;
            for (int k = 0; k < columnas; k++) t.Columns.Add(PrefijoMes + k, typeof(decimal));
            if (matriz != null)
                foreach (var f in matriz.Cohortes.AsEnumerable().Reverse())   // la más reciente arriba
                {
                    var valores = new object[2 + columnas];
                    valores[0] = f.Mes;
                    valores[1] = f.Tamanio;
                    for (int k = 0; k < columnas; k++)
                    {
                        var c = f.Celda(k);
                        valores[2 + k] = c != null ? (object)c.Retencion : DBNull.Value;
                    }
                    t.Rows.Add(valores);
                }
            dgv.DataSource = t;
            if (dgv.Columns.Count >= 2)
            {
                dgv.Columns[ColCohorte].HeaderText = Tr("cdc.cohorte.col.cohorte", "Cohorte (mes de alta)");
                dgv.Columns[ColCohorte].FillWeight = 150;
                dgv.Columns[ColClientes].HeaderText = Tr("cdc.cohorte.col.clientes", "Clientes");
                dgv.Columns[ColClientes].FillWeight = 70;
                dgv.Columns[ColClientes].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                for (int k = 0; k < columnas; k++)
                {
                    var col = dgv.Columns[PrefijoMes + k];
                    col.HeaderText = Tr("cdc.cohorte.col.mes", "Mes {0}", k);
                    col.FillWeight = 60;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            lblResumen.Text = matriz == null ? Tr("cdc.grafico.analice", "Elija el período y pulse Analizar.") : R(matriz.Resumen);
            lblResumen.ForeColor = matriz?.Peor != null ? Tema.Error : Tema.TextoMuted;
            btnActuar.Enabled = matriz?.Peor != null;
            GraficosComerciales.CurvaRetencion(chart, matriz);
        }

        /// <summary>Imagen del gráfico de la curva (para el Informe ejecutivo).</summary>
        public byte[] GraficoPng() => GraficosComerciales.Png(chart);

        // La fila se resuelve por su dato (mes de la cohorte): ordenar por encabezado no la confunde.
        private BE.FilaCohorte Fila(int indiceFila)
        {
            if (_matriz == null || indiceFila < 0 || indiceFila >= dgv.Rows.Count) return null;
            if (!(dgv.Rows[indiceFila].DataBoundItem is DataRowView drv) || drv.Row[ColCohorte] == DBNull.Value) return null;
            var mes = (DateTime)drv.Row[ColCohorte];
            return _matriz.Cohortes.FirstOrDefault(c => c.Mes == mes);
        }

        private static int? Desplazamiento(DataGridViewColumn col) =>
            col != null && col.Name.StartsWith(PrefijoMes) && int.TryParse(col.Name.Substring(PrefijoMes.Length), out var k) ? k : (int?)null;

        private static Color ColorDe(decimal retencion)
        {
            switch (Bll.Nivel(retencion))
            {
                case BE.NivelRetencion.Alta: return ColorAlto;
                case BE.NivelRetencion.Media: return ColorMedio;
                case BE.NivelRetencion.Baja: return ColorBajo;
                default: return ColorCritico;
            }
        }

        private static Color Oscurecer(Color c) => Color.FromArgb((int)(c.R * 0.82), (int)(c.G * 0.82), (int)(c.B * 0.82));

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var fila = Fila(e.RowIndex);
            if (fila == null) return;
            var col = dgv.Columns[e.ColumnIndex];
            if (col.Name == ColCohorte)
            {
                e.Value = (fila.EsPeor ? "▶ " : "") + Mes(fila.Mes);
                e.FormattingApplied = true;
                if (fila.EsPeor)
                {
                    e.CellStyle.ForeColor = Tema.Error;
                    e.CellStyle.SelectionForeColor = Color.White;
                    e.CellStyle.Font = fuentePeor ?? (fuentePeor = new Font(dgv.Font, FontStyle.Bold));
                }
                return;
            }
            if (col.Name == ColClientes)
            {
                if (!fila.Comparable) e.CellStyle.ForeColor = Tema.TextoMuted;   // debajo del mínimo: no compite
                return;
            }
            int? k = Desplazamiento(col);
            var celda = k.HasValue ? fila.Celda(k.Value) : null;
            if (celda == null)
            {
                e.Value = string.Empty;
                e.FormattingApplied = true;
                return;
            }
            e.Value = Bll.RetencionMostrada(celda.Retencion).ToString("0") + " %" + (celda.EsParcial ? " *" : "");
            e.FormattingApplied = true;
            var fondo = ColorDe(celda.Retencion);
            e.CellStyle.BackColor = fondo;
            e.CellStyle.ForeColor = Tema.Tinta;
            e.CellStyle.SelectionBackColor = Oscurecer(fondo);
            e.CellStyle.SelectionForeColor = Tema.Tinta;
            if (celda.EsParcial) e.CellStyle.ForeColor = Tema.TextoSecundario;
            if (fila.EsPeor && _matriz?.Peor != null && celda.Desplazamiento == _matriz.Peor.Desplazamiento)
                e.CellStyle.Font = fuentePeor ?? (fuentePeor = new Font(dgv.Font, FontStyle.Bold));
        }

        private void Dgv_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var fila = Fila(e.RowIndex);
            if (fila == null) return;
            var col = dgv.Columns[e.ColumnIndex];
            int? k = Desplazamiento(col);
            var celda = k.HasValue ? fila.Celda(k.Value) : null;
            if (celda != null)
                e.ToolTipText = Tr("cdc.cohorte.tip", "Cohorte {0:yyyy-MM}, mes {1}: {2} de {3} cliente(s) activos ({4:0.#} %)",
                                   fila.Mes, celda.Desplazamiento, celda.Activos, fila.Tamanio, celda.Retencion)
                                + (celda.EsParcial ? " — " + Tr("cdc.cohorte.parcial", "mes en curso: no entra en el promedio ni en la comparación") : "");
            else if (col.Name == ColClientes && !fila.Comparable)
                e.ToolTipText = Tr("cdc.cohorte.tip.chica", "Cohorte de menos de {0} clientes: se muestra, pero no compite por la peor cohorte (sería ruido).",
                                   _matriz.MinimoClientes);
            else if (fila.EsPeor)
                e.ToolTipText = Tr("cdc.cohorte.tip.actuar",
                    "Lleva a la recomendación de bajas del plan que más explica la peor cohorte (o, si no la hay, al Simulador). También con doble clic en su fila.");
        }

        private void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (Fila(e.RowIndex)?.EsPeor == true) Actuar();
        }

        private void Actuar()
        {
            if (_matriz?.Peor != null) ActuarSobrePeorCohorte?.Invoke(this, _matriz.Peor);
        }

        /// <summary>
        /// Muestra de color de la leyenda. Se dibuja sola (no es un Label) para que el modo oscuro de
        /// PreferenciasUI no le cambie la tinta: el color de la muestra es el mismo que el de la celda.
        /// </summary>
        private sealed class MuestraLeyenda : Control
        {
            private readonly Color fondo, tinta;

            public MuestraLeyenda(string texto, Color fondo, Color tinta)
            {
                this.fondo = fondo;
                this.tinta = tinta;
                Text = texto;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                Font = Tema.FuentePequena;
                Medir();
            }

            // Las preferencias de tamaño de letra cambian la fuente: la muestra se vuelve a medir.
            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                Medir();
            }

            private void Medir()
            {
                var medida = TextRenderer.MeasureText(Text ?? string.Empty, Font);
                Size = new Size(medida.Width + 12, medida.Height + 6);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using (var b = new SolidBrush(fondo)) e.Graphics.FillRectangle(b, ClientRectangle);
                using (var p = new Pen(Tema.Borde)) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, tinta,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}
