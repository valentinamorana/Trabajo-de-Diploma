using System.Drawing;

namespace GUI.Analitica
{
    /// <summary>
    /// PdN9 — Rotación de Prendas. El Gerente de Inventario genera el listado de prendas de baja
    /// demanda (candidatas a baja) o alta demanda (candidatas a reposición), cruzando el catálogo
    /// activo con la cantidad de pedidos por prenda. Sin filtros.
    /// </summary>
    internal sealed class VistaRotacion : VistaReporteAnalitica
    {
        private readonly BLL.Interfaces.IAnalisisRotacionService _bll = new BLL.AnalisisRotacion();

        public override BE.ReporteAnalitica Tipo => BE.ReporteAnalitica.RotacionPrendas;
        public override string Titulo => Tr("rotacion.titulo", "Rotación de Prendas");
        public override string NombreArchivo => "RotacionPrendas";

        public override string[] Encabezados => new[]
        {
            Tr("rotacion.col.prenda", "Prenda"),
            Tr("rotacion.col.categoria", "Categoría"),
            Tr("rotacion.col.cantidad", "Cant. pedidos"),
            Tr("rotacion.col.motivo", "Motivo")
        };

        public override Exportacion.GeneradorReporte CrearGenerador() => new Exportacion.GeneradorRotacion();

        public override ResultadoVistaReporte Generar()
        {
            var resultados = _bll.Detectar();

            var tabla = NuevaTabla();
            foreach (var r in resultados)
                tabla.Rows.Add(r.NombrePrenda, r.Categoria ?? "", r.CantidadPedidos,
                    Tr(r.Clave, r.Motivo, r.Args));

            return new ResultadoVistaReporte
            {
                Tabla = tabla,
                Cantidad = resultados.Count,
                ColorMensaje = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen,
                Mensaje = string.Format(
                    Tr("rotacion.resultado", "{0} prenda(s) marcada(s) por rotación."),
                    new object[] { resultados.Count })
            };
        }
    }
}
