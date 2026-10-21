using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GUI.Exportacion;
using static GUI.Inteligencia.TextosComerciales;

namespace GUI.Inteligencia
{
    /// <summary>
    /// Arma el «Informe ejecutivo» del Centro de decisiones comerciales como un reporte compuesto
    /// (ReporteExportable.Secciones), que ExportadorPdf escribe con iTextSharp: portada con los filtros,
    /// resumen de KPI, gráficos (PNG de MSChart), recomendaciones con su impacto, decisiones tomadas,
    /// escenarios simulados, medición de promociones y glosario de métricas. También arma las tablas de
    /// recomendaciones, decisiones, contactos y orden de compra para exportar a CSV o PDF.
    /// </summary>
    internal static partial class InformeEjecutivo
    {
        public sealed partial class Datos
        {
            public BE.PanelComercial Panel;
            public List<BE.Recomendacion> Recomendaciones = new List<BE.Recomendacion>();
            public List<BE.DecisionComercial> Decisiones = new List<BE.DecisionComercial>();
            public Dictionary<int, decimal?> Medidos = new Dictionary<int, decimal?>();
            public List<BE.ResultadoSimulacion> Escenarios = new List<BE.ResultadoSimulacion>();
            public List<BE.MedicionPromocion> Mediciones = new List<BE.MedicionPromocion>();
            public List<(string Titulo, byte[] Png)> Graficos = new List<(string, byte[])>();
            public string Usuario;
            /// <summary>Retención por cohorte (InformeEjecutivo.Cohortes.cs) y la imagen de su curva.</summary>
            public BE.MatrizCohortes Cohortes;
            public byte[] GraficoCohortes;
        }

        public static ReporteExportable Armar(Datos d)
        {
            var p = d.Panel;
            string titulo = Tr("cdc.informe.titulo", "Informe ejecutivo — Inteligencia comercial");
            var secciones = new List<SeccionReporte>
            {
                new SeccionReporte
                {
                    EsPortada = true, Titulo = titulo,
                    Parrafos =
                    {
                        Tr("cdc.informe.periodo", "Período analizado: {0:d} al {1:d}", p.Desde, p.Hasta),
                        Tr("cdc.informe.anterior", "Comparado con: {0:d} al {1:d}", p.AnteriorDesde, p.AnteriorHasta),
                        Tr("cdc.informe.plan", "Plan: {0}", p.NombrePlan ?? Tr("cdc.todoslosplanes", "Todos los planes")),
                        Tr("cdc.informe.usuario", "Emitido por: {0}", d.Usuario ?? "—"),
                        Tr("cdc.informe.ciclo", "Analizar → recomendar/decidir → simular → aplicar promoción → medir resultado")
                    }
                }
            };

            // ── KPI ──
            var kpis = Tabla("Indicador", "Valor", "Anterior", "Variación", "Interpretación");
            foreach (var k in p.Kpis)
                kpis.Rows.Add(R(k.Nombre), ValorKpi(k.Unidad, k.Valor),
                    k.ValorAnterior.HasValue ? ValorKpi(k.Unidad, k.ValorAnterior.Value) : "—", Variacion(k), R(k.Interpretacion));
            secciones.Add(new SeccionReporte
            {
                Titulo = Tr("cdc.informe.kpi", "Resumen de indicadores"), PaginaNueva = true, Tabla = kpis,
                Encabezados = new[] { Tr("cdc.col.indicador", "Indicador"), Tr("cdc.col.valor", "Valor"), Tr("cdc.col.anterior", "Anterior"),
                                      Tr("cdc.col.variacion", "Variación"), Tr("cdc.col.interpretacion", "Interpretación") },
                Parrafos = p.Avisos.Select(a => "⚠ " + R(a)).ToList()
            });

            // ── Gráficos ──
            foreach (var g in d.Graficos.Where(g => g.Png != null))
                secciones.Add(new SeccionReporte { Titulo = g.Titulo, Imagen = g.Png });

            // ── Cohortes ──
            secciones.AddRange(SeccionesCohortes(d.Cohortes, d.GraficoCohortes));

            // ── Recomendaciones ──
            var recs = TablaRecomendaciones(d.Recomendaciones, out var encRecs);
            var seccionRecs = new SeccionReporte
            {
                Titulo = Tr("cdc.informe.recomendaciones", "Recomendaciones del motor"), PaginaNueva = true, Tabla = recs, Encabezados = encRecs
            };
            if (d.Recomendaciones.Count == 0)
                seccionRecs.Parrafos.Add(Tr("cdc.recs.ninguna", "El motor no encontró situaciones que requieran una decisión con los umbrales actuales."));
            foreach (var r in d.Recomendaciones)
                seccionRecs.Parrafos.Add($"• {R(r.Titulo)}: {R(r.Diagnostico)} {Tr("cdc.informe.formula", "Cálculo")}: {R(r.Principal?.Formula)}");
            secciones.Add(seccionRecs);

            // ── Decisiones ──
            if (d.Decisiones.Count > 0)
                secciones.Add(new SeccionReporte
                {
                    Titulo = Tr("cdc.informe.decisiones", "Decisiones tomadas"),
                    Tabla = TablaDecisiones(d.Decisiones, d.Medidos, out var encDec), Encabezados = encDec
                });

            // ── Escenarios ──
            if (d.Escenarios.Count > 0)
            {
                var esc = Tabla("Escenario", "Plan", "Descuento", "Días", "Retención", "Costo", "Beneficio neto", "ROI", "Equilibrio", "Veredicto");
                for (int i = 0; i < d.Escenarios.Count; i++)
                {
                    var s = d.Escenarios[i];
                    esc.Rows.Add(((char)('A' + i)).ToString(), s.Escenario.NombrePlan, s.Escenario.DescuentoPorcentaje.ToString("0.#") + " %",
                        s.Escenario.DuracionDias, s.Escenario.TasaRetencion.ToString("0.#") + " %", s.CostoDescuento.ToString("C0"),
                        s.BeneficioNeto.ToString("C0"), s.Roi.HasValue ? s.Roi.Value.ToString("0.#") + " %" : "—",
                        s.TasaEquilibrio.HasValue ? s.TasaEquilibrio.Value.ToString("0.#") + " %" : "—", VeredictoSimulacion(s.Veredicto));
                }
                secciones.Add(new SeccionReporte
                {
                    Titulo = Tr("cdc.informe.escenarios", "Escenarios simulados"), Tabla = esc,
                    Encabezados = new[] { Tr("cdc.col.escenario", "Escenario"), Tr("cdc.col.plan", "Plan"), Tr("cdc.col.descuento", "Descuento"),
                        Tr("cdc.col.dias", "Días"), Tr("cdc.col.retencion", "Retención"), Tr("cdc.col.costo", "Costo"), Tr("cdc.col.neto", "Beneficio neto"),
                        Tr("cdc.col.roi", "ROI"), Tr("cdc.col.equilibrio", "Equilibrio"), Tr("cdc.col.veredicto", "Veredicto") },
                    Parrafos = d.Escenarios.Select((s, i) => $"{(char)('A' + i)}: {R(s.Explicacion)}").ToList()
                });
            }

            // ── Medición ──
            if (d.Mediciones.Count > 0)
                secciones.Add(new SeccionReporte
                {
                    Titulo = Tr("cdc.informe.medicion", "Resultado de las promociones"),
                    Tabla = TablaMediciones(d.Mediciones, out var encMed), Encabezados = encMed,
                    Parrafos = d.Mediciones.Select(m => $"• {m.Nombre}: {Veredicto(m.Veredicto)} — {R(m.Explicacion)}").ToList()
                });

            // ── Metas del mes (InformeEjecutivo.Metas.cs) ──
            AgregarSeccionMetas(secciones, d.Metas);

            // ── Glosario ──
            var glos = Tabla("Métrica", "Fórmula y fuente", "Umbral y decisión");
            for (int i = 0; i < Metricas.Length; i++)
                glos.Rows.Add(GlosarioNombre(i), GlosarioFormula(i), GlosarioDecision(i));
            secciones.Add(new SeccionReporte
            {
                Titulo = Tr("cdc.glosario.titulo", "Glosario de métricas"), PaginaNueva = true, Tabla = glos,
                Encabezados = new[] { Tr("cdc.col.metrica", "Métrica"), Tr("cdc.col.formula", "Fórmula y fuente"), Tr("cdc.col.decision", "Umbral y decisión") }
            });

            return new ReporteExportable
            {
                Titulo = titulo,
                NombreArchivo = $"InformeEjecutivo_{p.Desde:yyyyMMdd}_{p.Hasta:yyyyMMdd}",
                Secciones = secciones
            };
        }

        private static DataTable Tabla(params string[] columnas)
        {
            var t = new DataTable();
            foreach (var c in columnas) t.Columns.Add(c, typeof(string));
            return t;
        }

        public static DataTable TablaRecomendaciones(List<BE.Recomendacion> recs, out string[] encabezados)
        {
            var t = Tabla("Prioridad", "Regla", "Recomendacion", "Propuesta", "Impacto", "Formula", "Alternativas", "Confianza", "Estado");
            foreach (var r in recs)
                t.Rows.Add(Prioridad(r.Prioridad), Regla(r.TipoRegla), R(r.Titulo), R(r.Principal?.Descripcion), r.ImpactoEstimado.ToString("C0"),
                    R(r.Principal?.Formula), string.Join(" | ", r.Alternativas.Select(a => $"{R(a.Descripcion)} ({a.ImpactoEstimado:C0})")),
                    Confianza(r.Confianza), EstadoRecomendacion(r));
            encabezados = new[] { Tr("cdc.col.prioridad", "Prioridad"), Tr("cdc.col.regla", "Regla"), Tr("cdc.col.recomendacion", "Recomendación"),
                Tr("cdc.col.propuesta", "Acción propuesta"), Tr("cdc.col.impacto", "Impacto estimado"), Tr("cdc.col.formula", "Fórmula y fuente"),
                Tr("cdc.col.alternativas", "Alternativas"), Tr("cdc.col.confianza", "Confianza"), Tr("cdc.col.estado", "Estado") };
            return t;
        }

        public static DataTable TablaDecisiones(List<BE.DecisionComercial> lista, Dictionary<int, decimal?> medidos, out string[] encabezados)
        {
            var t = Tabla("ID", "Fecha", "Usuario", "Regla", "Titulo", "Opcion", "Estado", "Estimado", "Real", "Medido", "Desvio", "Referencia", "Motivo");
            foreach (var d in lista)
            {
                decimal? medido = medidos != null && medidos.TryGetValue(d.IdDecision, out var m) ? m : null;
                t.Rows.Add(d.IdDecision.ToString(), d.Fecha.ToString("g"), d.NombreUsuario ?? "", Regla(d.TipoRegla), d.Titulo,
                    (d.EsAlternativa ? "↳ " : "") + d.AccionElegida, Estado(d.Estado) + (d.PospuestaHasta.HasValue ? $" ({d.PospuestaHasta:d})" : ""),
                    d.ImpactoEstimado.ToString("C0"), d.ResultadoReal?.ToString("C0") ?? "—", medido.HasValue ? medido.Value.ToString("C0") + (d.ResultadoParcial ? " " + Tr("cdc.medido.parcial", "(parcial)") : "") : "—",
                    d.Desvio?.ToString("+$#,0;-$#,0;0") ?? "—", d.Referencia.HasValue ? $"{d.TipoReferencia} #{d.Referencia}" : "", d.Motivo ?? "");
            }
            encabezados = new[] { "ID", Tr("cdc.col.fecha", "Fecha"), Tr("cdc.col.usuario", "Usuario"), Tr("cdc.col.regla", "Regla"),
                Tr("cdc.col.recomendacion", "Recomendación"), Tr("cdc.col.opcion", "Opción elegida"), Tr("cdc.col.estado", "Estado"),
                Tr("cdc.col.impacto", "Impacto estimado"), Tr("cdc.col.real", "Resultado real"), Tr("cdc.col.medido", "Medido por el sistema"),
                Tr("cdc.col.desvio", "Desvío"), Tr("cdc.col.referencia", "Referencia"), Tr("cdc.col.motivo", "Motivo") };
            return t;
        }

        public static DataTable TablaMediciones(List<BE.MedicionPromocion> lista, out string[] encabezados)
        {
            var t = Tabla("ID", "Promocion", "Plan", "Estado", "Dias", "Bajas", "Pausas", "Cambios", "Renovaciones",
                          "Esperada", "Real", "Mejora", "Descontado", "BeneficioReal", "RoiReal", "RoiEsperado", "Veredicto");
            foreach (var m in lista)
            {
                var r = m.Resultado;
                string Par(int a, int b) => $"{a} → {b}";
                string Pct(decimal? v) => v.HasValue ? v.Value.ToString("0.#") + " %" : "—";
                t.Rows.Add(m.IdPromocion.ToString(), m.Nombre, r.NombrePlan, Exportacion.DocumentosPromocion.Estado(r.Estado),
                    r.NoSePudoCalcular ? "—" : r.Dias.ToString(), Par(r.Antes.Bajas, r.Durante.Bajas), Par(r.Antes.Pausas, r.Durante.Pausas),
                    Par(r.Antes.CambiosDePlan, r.Durante.CambiosDePlan), Par(r.Antes.Renovaciones, r.Durante.Renovaciones),
                    Pct(r.TasaRetencionEsperada), Pct(r.TasaRetencionReal), r.MejoraRetencion.HasValue ? r.MejoraRetencion.Value.ToString("+0.#;-0.#;0") + " pp" : "—",
                    m.TotalDescontado.ToString("C0"), m.BeneficioReal?.ToString("C0") ?? "—", Pct(m.RoiReal), Pct(m.RoiEstimado), Veredicto(m.Veredicto));
            }
            encabezados = new[] { "ID", Tr("cdc.col.promocion", "Promoción"), Tr("cdc.col.plan", "Plan"), Tr("cdc.col.estado", "Estado"),
                Tr("cdc.col.dias", "Días"), Tr("cdc.col.bajas", "Bajas (antes → durante)"), Tr("cdc.col.pausas", "Pausas"),
                Tr("cdc.col.cambios", "Cambios de plan"), Tr("cdc.col.renovaciones", "Renovaciones"), Tr("cdc.col.esperada", "Retención esperada"),
                Tr("cdc.col.retreal", "Retención real"), Tr("cdc.col.mejora", "Mejora"), Tr("cdc.col.descontado", "Descontado"),
                Tr("cdc.col.beneficioreal", "Beneficio real"), Tr("cdc.col.roireal", "ROI real"), Tr("cdc.col.roiesperado", "ROI esperado"),
                Tr("cdc.col.veredicto", "Veredicto") };
            return t;
        }

        public static ReporteExportable Contactos(List<BE.ClienteVencimiento> contactos, string titulo)
        {
            var t = Tabla("ID", "Cliente", "Email", "Plan", "Vencimiento", "Dias", "Precio");
            foreach (var c in contactos)
                t.Rows.Add(c.IdCliente.ToString(), c.Nombre, c.Email ?? "", c.NombrePlan ?? "", c.FechaVencimiento.ToString("d"),
                           c.DiasRestantes.ToString(), c.Precio.ToString("0.00"));
            return new ReporteExportable
            {
                Titulo = titulo, NombreArchivo = $"Contactos_{DateTime.Today:yyyyMMdd}", Datos = t,
                Encabezados = new[] { "ID", Tr("cdc.col.cliente", "Cliente"), "Email", Tr("cdc.col.plan", "Plan"),
                    Tr("cdc.col.vencimiento", "Vencimiento"), Tr("cdc.col.dias", "Días"), Tr("cdc.col.precio", "Precio") }
            };
        }

        public static ReporteExportable OrdenCompra(List<BE.LineaOrdenCompra> lineas, BE.Recomendacion rec)
        {
            var secciones = new List<SeccionReporte>();
            var t = Tabla("Categoria", "Talle", "Cantidad", "Costo", "Subtotal");
            foreach (var l in lineas)
                t.Rows.Add(l.Categoria, l.Talle, l.Cantidad.ToString(), l.CostoUnitario.ToString("C2"), l.Subtotal.ToString("C2"));
            t.Rows.Add(Tr("cdc.orden.total", "Total"), "", lineas.Sum(l => l.Cantidad).ToString(), "", lineas.Sum(l => l.Subtotal).ToString("C2"));
            secciones.Add(new SeccionReporte
            {
                Titulo = Tr("cdc.orden.titulo", "Orden de compra sugerida"),
                Parrafos =
                {
                    Tr("cdc.orden.fecha", "Fecha: {0:d}", DateTime.Today),
                    R(rec?.Diagnostico),
                    Tr("cdc.orden.nota", "Documento sugerido por el Centro de decisiones comerciales: Compras confirma proveedor y precios antes de emitirla.")
                },
                Tabla = t,
                Encabezados = new[] { Tr("cdc.col.categoria", "Categoría"), Tr("cdc.col.talle", "Talle"), Tr("cdc.col.cantidad", "Cantidad"),
                                      Tr("cdc.col.costo", "Costo unitario"), Tr("cdc.col.subtotal", "Subtotal") }
            });
            return new ReporteExportable
            {
                Titulo = Tr("cdc.orden.titulo", "Orden de compra sugerida"),
                NombreArchivo = $"OrdenCompraSugerida_{DateTime.Today:yyyyMMdd}",
                Secciones = secciones
            };
        }
    }
}
