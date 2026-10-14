using System.Drawing;
using System.Windows.Forms;

namespace GUI.Analitica
{
    /// <summary>
    /// PdN12 — Escasez de Stock por Talle/Categoría. El Gerente de Inventario configura un umbral
    /// mínimo de stock Disponible y genera las combinaciones Talle+Categoría que caen por debajo,
    /// para planificar compras antes de un faltante.
    /// </summary>
    internal sealed class VistaEscasez : VistaReporteAnalitica
    {
        private readonly BLL.Interfaces.IAnalisisEscasezService _bll = new BLL.AnalisisEscasez();

        private Label _lblUmbral;
        private NumericUpDown _numUmbral;

        public override BE.ReporteAnalitica Tipo => BE.ReporteAnalitica.EscasezStock;
        public override string Titulo => Tr("escasez.titulo", "Escasez de Stock");
        public override string NombreArchivo => "EscasezStock";

        public override string[] Encabezados => new[]
        {
            Tr("escasez.col.talle", "Talle"),
            Tr("escasez.col.categoria", "Categoría"),
            Tr("escasez.col.disponible", "Disponible"),
            Tr("escasez.col.motivo", "Motivo")
        };

        public override Exportacion.GeneradorReporte CrearGenerador() => new Exportacion.GeneradorEscasez();

        protected override Control CrearFiltros()
        {
            _lblUmbral = new Label { Name = "lblUmbral" };
            _numUmbral = new NumericUpDown { Name = "numUmbral", Width = 80, Minimum = 0, Maximum = 1000 };
            _numUmbral.Value = BLL.AnalisisEscasez.UmbralPorDefecto;
            return FilaFiltro(_lblUmbral, _numUmbral);
        }

        public override void Traducir()
        {
            if (_lblUmbral != null)
                _lblUmbral.Text = Tr("escasez.umbral", "Umbral mínimo:");
        }

        public override ResultadoVistaReporte Generar()
        {
            int umbral = _numUmbral != null ? (int)_numUmbral.Value : BLL.AnalisisEscasez.UmbralPorDefecto;
            var resultados = _bll.Detectar(umbral);

            var tabla = NuevaTabla();
            foreach (var r in resultados)
                tabla.Rows.Add(r.Talle, r.Categoria, r.CantidadDisponible, Tr(r.Clave, r.Motivo, r.Args));

            return new ResultadoVistaReporte
            {
                Tabla = tabla,
                Cantidad = resultados.Count,
                ColorMensaje = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen,
                Mensaje = string.Format(
                    Tr("escasez.resultado", "{0} combinación(es) por debajo del umbral."),
                    new object[] { resultados.Count })
            };
        }
    }
}
