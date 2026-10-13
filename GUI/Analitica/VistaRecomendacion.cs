using System.Drawing;
using System.Windows.Forms;

namespace GUI.Analitica
{
    /// <summary>
    /// PdN13 — Recomendación de Prendas para un Cliente. El Vendedor elige un cliente y genera
    /// prendas Disponibles afines a su historial de pedidos (categoría/color más pedidos), para
    /// ofrecerlas en el próximo pedido.
    /// </summary>
    internal sealed class VistaRecomendacion : VistaReporteAnalitica
    {
        private readonly BLL.Interfaces.IClienteService _bllCliente = new BLL.Cliente();
        private readonly BLL.Interfaces.IRecomendacionPrendasService _bll = new BLL.RecomendacionPrendas();

        private Label _lblCliente;
        private ComboBox _cmbCliente;

        public override BE.ReporteAnalitica Tipo => BE.ReporteAnalitica.RecomendacionPrendas;
        public override string Titulo => Tr("recom.titulo", "Recomendación de Prendas");
        public override string NombreArchivo => "RecomendacionPrendas";

        public override string[] Encabezados => new[]
        {
            Tr("recom.col.prenda", "Prenda"),
            Tr("recom.col.categoria", "Categoría"),
            Tr("recom.col.color", "Color"),
            Tr("recom.col.motivo", "Motivo")
        };

        public override Exportacion.GeneradorReporte CrearGenerador() => new Exportacion.GeneradorRecomendacion();

        protected override Control CrearFiltros()
        {
            _lblCliente = new Label { Name = "lblCliente" };
            _cmbCliente = new ComboBox { Name = "cmbCliente", DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            return FilaFiltro(_lblCliente, _cmbCliente);
        }

        public override void Traducir()
        {
            if (_lblCliente != null)
                _lblCliente.Text = Tr("renov.cliente", "Cliente:");
        }

        /// <summary>Carga los clientes del selector (puede lanzar: lo muestra el formulario).</summary>
        public override void Cargar()
        {
            if (_cmbCliente == null) return;
            _cmbCliente.Items.Clear();
            foreach (var c in _bllCliente.ObtenerTodos())
                _cmbCliente.Items.Add(new ClienteItem(c));
            if (_cmbCliente.Items.Count > 0) _cmbCliente.SelectedIndex = 0;
        }

        public override ResultadoVistaReporte Generar()
        {
            if (!(_cmbCliente?.SelectedItem is ClienteItem item)) return null;

            var resultados = _bll.Recomendar(item.Cliente.IdCliente);

            var tabla = NuevaTabla();
            foreach (var r in resultados)
                tabla.Rows.Add(r.Prenda.Nombre, r.Prenda.Categoria ?? "", r.Prenda.Color ?? "",
                    Tr(r.Clave, r.Motivo, r.Args));

            return new ResultadoVistaReporte
            {
                Tabla = tabla,
                Cantidad = resultados.Count,
                ColorMensaje = resultados.Count > 0 ? Color.DarkGreen : Color.DarkOrange,
                Mensaje = resultados.Count > 0
                    ? string.Format(Tr("recom.resultado", "{0} prenda(s) recomendada(s)."), new object[] { resultados.Count })
                    : Tr("recom.sinhistorial", "El cliente no tiene historial de pedidos suficiente para recomendar.")
            };
        }

        private sealed class ClienteItem
        {
            public BE.Cliente Cliente { get; }
            public ClienteItem(BE.Cliente c) => Cliente = c;
            public override string ToString() => $"{Cliente.NombreCompleto} (DNI {Cliente.DNI})";
        }
    }
}
