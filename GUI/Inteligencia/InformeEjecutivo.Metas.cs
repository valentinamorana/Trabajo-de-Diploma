using System.Collections.Generic;
using System.Data;
using System.Linq;
using GUI.Exportacion;
using static GUI.Inteligencia.TextosComerciales;
using static GUI.Inteligencia.TextosMetas;

namespace GUI.Inteligencia
{
    /// <summary>
    /// Sección "Metas del mes" del Informe ejecutivo: cada meta con su objetivo, el valor real, el avance, la
    /// proyección al cierre y el semáforo, más la explicación, y justo después el gráfico real contra
    /// objetivo. Pura presentación de lo que calculó la BLL (MetasComerciales).
    /// </summary>
    internal static partial class InformeEjecutivo
    {
        public sealed partial class Datos
        {
            public List<BE.SeguimientoMeta> Metas = new List<BE.SeguimientoMeta>();
            /// <summary>Imagen del gráfico "Metas: real contra objetivo"; va a continuación de la tabla de metas.</summary>
            public byte[] GraficoMetas;
        }

        private static void AgregarSeccionMetas(List<SeccionReporte> secciones, List<BE.SeguimientoMeta> metas, byte[] grafico)
        {
            if (metas == null || metas.Count == 0) return;
            secciones.Add(new SeccionReporte
            {
                Titulo = Tr("cdc.meta.informe.titulo", "Metas del mes ({0:MM/yyyy})", metas[0].Meta.Periodo),
                Tabla = TablaMetas(metas, out var encabezados), Encabezados = encabezados,
                Parrafos = metas.Select(m => $"• {R(m.NombreIndicador)}: {R(m.Explicacion)}").ToList()
            });
            if (grafico != null)
                secciones.Add(new SeccionReporte { Titulo = Tr("cdc.meta.grafico", "Metas: real contra objetivo"), Imagen = grafico });
        }

        public static DataTable TablaMetas(List<BE.SeguimientoMeta> metas, out string[] encabezados)
        {
            var t = Tabla("Indicador", "Objetivo", "Real", "Avance", "Proyección", "Estado");
            foreach (var m in metas ?? new List<BE.SeguimientoMeta>())
                t.Rows.Add(R(m.NombreIndicador), ValorMeta(m.Unidad, m.Meta.ValorObjetivo), ValorMeta(m.Unidad, m.ValorActual),
                    m.AvancePorcentaje.HasValue ? m.AvancePorcentaje.Value.ToString("0.#") + " %" : "—",
                    ValorMeta(m.Unidad, m.Proyeccion), EstadoMeta(m.Estado));
            encabezados = new[] { Tr("cdc.col.indicador", "Indicador"), Tr("cdc.meta.col.objetivo", "Objetivo"), Tr("cdc.meta.col.real", "Real"),
                                  Tr("cdc.meta.col.avance", "Avance"), Tr("cdc.meta.col.proyeccion", "Proyección al cierre"), Tr("cdc.meta.col.estado", "Estado") };
            return t;
        }
    }
}
