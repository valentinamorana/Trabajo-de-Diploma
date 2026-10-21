using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GUI.Exportacion;
using static GUI.Inteligencia.TextosComerciales;

namespace GUI.Inteligencia
{
    /// <summary>Informe ejecutivo — sección «Retención por cohorte» (tabla de los primeros meses, la peor cohorte y la curva).</summary>
    internal static partial class InformeEjecutivo
    {
        /// <summary>Meses desde el alta que muestra la tabla del informe (Mes 0 a Mes 5).</summary>
        public const int MesesCohorteEnInforme = 6;

        /// <summary>Secciones de cohortes del informe (vacío si no se analizó o no hay altas).</summary>
        public static List<SeccionReporte> SeccionesCohortes(BE.MatrizCohortes m, byte[] grafico)
        {
            var secciones = new List<SeccionReporte>();
            if (m == null) return secciones;
            var seccion = new SeccionReporte { Titulo = Tr("cdc.cohorte.informe.titulo", "Retención por cohorte"), PaginaNueva = true };
            seccion.Parrafos.Add(Tr("cdc.cohorte.explicacion",
                "Cómo leerlo: cada fila agrupa a los clientes por el mes en que se suscribieron y cada columna muestra qué % de ellos sigue activo (sin baja) k meses después del alta; Mes 0 es el mes del alta y * marca un mes todavía en curso."));
            seccion.Parrafos.Add(R(m.Resumen));
            if (!m.SinDatos)
            {
                int columnas = Math.Min(MesesCohorteEnInforme, m.MaximoDesplazamiento + 1);
                var t = new DataTable();
                t.Columns.Add("Cohorte", typeof(string));
                t.Columns.Add("Clientes", typeof(string));
                for (int k = 0; k < columnas; k++) t.Columns.Add("M" + k, typeof(string));
                foreach (var f in m.Cohortes.AsEnumerable().Reverse())
                {
                    var fila = new List<object> { (f.EsPeor ? "▶ " : "") + Mes(f.Mes), f.Tamanio.ToString() };
                    for (int k = 0; k < columnas; k++)
                    {
                        var c = f.Celda(k);
                        fila.Add(c == null ? "" : BLL.Inteligencia.AnalisisCohortes.RetencionMostrada(c.Retencion).ToString("0") + " %" + (c.EsParcial ? " *" : ""));
                    }
                    t.Rows.Add(fila.ToArray());
                }
                var promedio = new List<object> { Tr("cdc.cohorte.promedio", "Promedio"), m.TotalClientes.ToString() };
                for (int k = 0; k < columnas; k++)
                {
                    var p = m.Curva.FirstOrDefault(x => x.Desplazamiento == k);
                    promedio.Add(p?.Retencion != null ? BLL.Inteligencia.AnalisisCohortes.RetencionMostrada(p.Retencion.Value).ToString("0") + " %" : "—");
                }
                t.Rows.Add(promedio.ToArray());
                seccion.Tabla = t;
                var enc = new List<string> { Tr("cdc.cohorte.col.cohorte", "Cohorte (mes de alta)"), Tr("cdc.cohorte.col.clientes", "Clientes") };
                for (int k = 0; k < columnas; k++) enc.Add(Tr("cdc.cohorte.col.mes", "Mes {0}", k));
                seccion.Encabezados = enc.ToArray();
                seccion.Parrafos.Add(Tr("cdc.cohorte.informe.regla",
                    "Peor cohorte: la de menor retención en el mes de comparación (el mes {1} o, si todavía no hay datos, el último que completaron al menos 2 cohortes) entre las de {0} o más clientes; los meses en curso (*) no entran.",
                    m.MinimoClientes, m.MesComparacion));
            }
            secciones.Add(seccion);
            if (grafico != null && !m.SinDatos && !m.SinMesesCompletos)
                secciones.Add(new SeccionReporte { Titulo = Tr("cdc.cohorte.grafico.titulo", "Curva de retención por mes desde el alta"), Imagen = grafico });
            return secciones;
        }
    }
}
