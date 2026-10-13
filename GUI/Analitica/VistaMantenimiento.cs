using System.Drawing;

namespace GUI.Analitica
{
    /// <summary>
    /// PdN11 — Tiempos de Mantenimiento. El Gerente de Inventario genera el listado de prendas
    /// cuyo historial de MantenimientoPrenda supera el umbral aceptable de cantidad o duración
    /// promedio — señal de una prenda problemática a revisar. Sin filtros.
    /// </summary>
    internal sealed class VistaMantenimiento : VistaReporteAnalitica
    {
        private readonly BLL.Interfaces.IAnalisisMantenimientoService _bll = new BLL.AnalisisMantenimiento();

        public override BE.ReporteAnalitica Tipo => BE.ReporteAnalitica.TiemposMantenimiento;
        public override string Titulo => Tr("mant.analisis.titulo", "Tiempos de Mantenimiento");
        public override string NombreArchivo => "TiemposMantenimiento";

        public override string[] Encabezados => new[]
        {
            Tr("mant.analisis.col.prenda", "Prenda"),
            Tr("mant.analisis.col.cantidad", "Cant. mantenimientos"),
            Tr("mant.analisis.col.promedio", "Prom. días"),
            Tr("mant.analisis.col.maximo", "Máx. días"),
            Tr("mant.analisis.col.motivo", "Motivo")
        };

        public override Exportacion.GeneradorReporte CrearGenerador() => new Exportacion.GeneradorMantenimiento();

        public override ResultadoVistaReporte Generar()
        {
            var resultados = _bll.Detectar();

            var tabla = NuevaTabla();
            foreach (var r in resultados)
                tabla.Rows.Add(
                    r.NombrePrenda,
                    r.CantidadMantenimientos,
                    r.DuracionPromedioDias.HasValue ? r.DuracionPromedioDias.Value.ToString("F1") : "—",
                    r.DuracionMaximaDias.HasValue ? r.DuracionMaximaDias.Value.ToString() : "—",
                    Tr(r.Clave, r.Motivo, r.Args));

            return new ResultadoVistaReporte
            {
                Tabla = tabla,
                Cantidad = resultados.Count,
                ColorMensaje = resultados.Count > 0 ? Color.DarkOrange : Color.DarkGreen,
                Mensaje = string.Format(
                    Tr("mant.analisis.resultado", "{0} prenda(s) con mantenimiento excesivo."),
                    new object[] { resultados.Count })
            };
        }
    }
}
