using System.Drawing;
using System.Windows.Forms;

namespace GUI.Analitica
{
    /// <summary>
    /// PdN10 — Análisis de Abandono. El Gerente Comercial elige el criterio de riesgo
    /// (patrón Strategy, BLL.Estrategias) y genera la lista de clientes que ese criterio
    /// marca en riesgo, cruzando su último pedido contra el vencimiento de la suscripción.
    /// </summary>
    internal sealed class VistaAbandono : VistaReporteAnalitica
    {
        private readonly BLL.Interfaces.IAnalisisAbandonoService _bll = new BLL.AnalisisAbandono();

        private Label _lblEstrategia;
        private ComboBox _cmbEstrategia;

        public override BE.ReporteAnalitica Tipo => BE.ReporteAnalitica.AnalisisAbandono;
        public override string Titulo => Tr("abandono.titulo", "Análisis de Abandono");
        public override string NombreArchivo => "AnalisisAbandono";

        public override string[] Encabezados => new[]
        {
            Tr("abandono.col.cliente", "Cliente"),
            Tr("abandono.col.plan", "Plan"),
            Tr("abandono.col.vencimiento", "Vencimiento"),
            Tr("abandono.col.ultimopedido", "Último pedido"),
            Tr("abandono.col.motivo", "Motivo")
        };

        public override Exportacion.GeneradorReporte CrearGenerador() => new Exportacion.GeneradorAnalisisAbandono();

        protected override Control CrearFiltros()
        {
            _lblEstrategia = new Label { Name = "lblEstrategia" };
            _cmbEstrategia = new ComboBox { Name = "cmbEstrategia", DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            return FilaFiltro(_lblEstrategia, _cmbEstrategia);
        }

        public override void Traducir()
        {
            if (_lblEstrategia == null) return;
            _lblEstrategia.Text = Tr("abandono.estrategia", "Criterio de riesgo:");
            CargarEstrategias();
        }

        /// <summary>
        /// Rellena el combo de estrategias con los 3 criterios traducidos, respetando el
        /// índice previamente seleccionado — mismo criterio que
        /// Bitacora.RellenarComboCriticidad para que un cambio de idioma no pierda la
        /// selección del usuario. El orden de las 3 estrategias es fijo, así que
        /// preservar por índice (no por instancia) es seguro.
        /// </summary>
        private void CargarEstrategias()
        {
            int idx = _cmbEstrategia.SelectedIndex;
            if (idx < 0) idx = 0;

            _cmbEstrategia.Items.Clear();
            _cmbEstrategia.Items.Add(new EstrategiaItem(new BLL.Estrategias.EstrategiaVencimientoInactividad(),
                Tr("abandono.estr.vencinactividad", "Vence pronto y sin actividad reciente")));
            _cmbEstrategia.Items.Add(new EstrategiaItem(new BLL.Estrategias.EstrategiaInactividadPura(),
                Tr("abandono.estr.inactividad", "Sin actividad hace tiempo (aunque esté vigente)")));
            _cmbEstrategia.Items.Add(new EstrategiaItem(new BLL.Estrategias.EstrategiaClienteNuevoInactivo(),
                Tr("abandono.estr.nuncaactivo", "Se dio de alta y nunca hizo un pedido")));

            _cmbEstrategia.SelectedIndex = (idx >= 0 && idx < _cmbEstrategia.Items.Count) ? idx : 0;
        }

        public override ResultadoVistaReporte Generar()
        {
            if (!(_cmbEstrategia?.SelectedItem is EstrategiaItem item)) return null;

            _bll.CambiarEstrategia(item.Estrategia);
            var resultados = _bll.Detectar();

            var tabla = NuevaTabla();
            foreach (var r in resultados)
            {
                tabla.Rows.Add(
                    r.NombreCliente,
                    r.NombrePlan ?? "",
                    r.FechaVencimiento.HasValue ? r.FechaVencimiento.Value.ToString("d") : Tr("susc.sinfecha", "sin fecha"),
                    r.FechaUltimoPedido.HasValue ? r.FechaUltimoPedido.Value.ToString("d") : Tr("abandono.nuncapidio", "nunca"),
                    Tr(r.Clave, r.Motivo, r.Args));
            }

            return new ResultadoVistaReporte
            {
                Tabla = tabla,
                Cantidad = resultados.Count,
                ColorMensaje = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen,
                Mensaje = string.Format(
                    Tr("abandono.resultado", "{0} cliente(s) en riesgo según '{1}'."),
                    new object[] { resultados.Count, item.ToString() })
            };
        }

        // Envuelve una estrategia con un nombre amigable para el combo (igual criterio
        // que ClienteItem/PlanItem en RenovacionSuscripcionForm).
        private sealed class EstrategiaItem
        {
            public BLL.Estrategias.EstrategiaRiesgo Estrategia { get; }
            private readonly string _nombre;
            public EstrategiaItem(BLL.Estrategias.EstrategiaRiesgo estrategia, string nombre)
            {
                Estrategia = estrategia;
                _nombre = nombre;
            }
            public override string ToString() => _nombre;
        }
    }
}
