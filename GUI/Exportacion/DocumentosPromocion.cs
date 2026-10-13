using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI.Exportacion
{
    /// <summary>
    /// PN03 — arma los objetos de información del diagrama de actividad de Evaluar y aplicar
    /// promociones comerciales como documentos PDF generados directamente
    /// (ExportadorPdf, sin impresora virtual):
    ///   • Reporte de métricas                               (Gerencia)
    ///   • Sugerencia de promoción                           (Gerencia → Administración)
    ///   • Constancia de descarte de una sugerencia          (Administración)
    ///   • Ficha de promoción (con su historial de estados)  (Administración → Contabilidad)
    ///   • Dictamen contable                                 (Contabilidad → Administración)
    ///   • Solicitud de baja                                 (Vendedor → Administración)
    ///   • Respuesta a la solicitud de baja                  (Administración → Ventas)
    ///   • Constancia de baja                                (Administración)
    /// Solo da formato a datos que ya resolvió la BLL; la exportación la hace el Exportador que
    /// fabrica <see cref="GeneradorDocumentoPromocion"/>.
    /// </summary>
    public static class DocumentosPromocion
    {
        // Motivo de la candidata en el idioma activo (la BLL deja la clave y sus argumentos).
        private static string MotivoCandidata(BE.CandidataSugerencia c) =>
            c.ClaveMotivo == null ? c.Motivo : string.Format(Tr(c.ClaveMotivo, c.Motivo), c.ArgsMotivo ?? new object[0]);

        private static string Tr(string clave, string fallback)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(clave) ? t[clave].Texto : fallback;
        }

        private static string Fecha(DateTime? f, bool conHora = true) =>
            f.HasValue ? f.Value.ToString(conHora ? "g" : "d") : "—";

        // ── Textos de los enums (los usan también las pantallas de PN03) ───────

        public static string Estado(BE.EstadoPromocion e)
        {
            switch (e)
            {
                case BE.EstadoPromocion.EnRevisionContable:    return Tr("est.promo.enrevision", "En revisión contable");
                case BE.EstadoPromocion.Vigente:               return Tr("est.promo.vigente", "Vigente");
                case BE.EstadoPromocion.RechazadaContabilidad: return Tr("est.promo.rechazada", "Rechazada por Contabilidad");
                case BE.EstadoPromocion.DadaDeBaja:            return Tr("est.promo.dadadebaja", "Dada de baja");
                default:                                       return Tr("est.promo.vencida", "Vencida");
            }
        }

        public static string EstadoSugerencia(BE.EstadoSugerencia e)
        {
            switch (e)
            {
                case BE.EstadoSugerencia.Evaluada:   return Tr("est.sugpromo.evaluada", "Aceptada (promoción creada)");
                case BE.EstadoSugerencia.Descartada: return Tr("est.sugpromo.descartada", "Descartada");
                default:                             return Tr("est.sugpromo.pendiente", "Pendiente");
            }
        }

        public static string Origen(BE.OrigenMetrica o)
        {
            switch (o)
            {
                case BE.OrigenMetrica.Abandono: return Tr("origen.metrica.abandono", "Abandono por plan");
                case BE.OrigenMetrica.Rotacion: return Tr("origen.metrica.rotacion", "Rotación por categoría");
                default:                        return Tr("origen.metrica.manual", "Manual");
            }
        }

        public static string Tipo(BE.TipoDescuento t)
        {
            switch (t)
            {
                case BE.TipoDescuento.Porcentaje: return Tr("tipodesc.porcentaje", "Porcentaje");
                case BE.TipoDescuento.MontoFijo:  return Tr("tipodesc.montofijo", "Monto fijo");
                default:                          return Tr("tipodesc.preciopromocional", "Precio promocional");
            }
        }

        // Las sugerencias por categoría de la versión anterior no tienen plan: se muestran con "—".
        public static string AplicaA(int? idPlan, string nombrePlan) =>
            $"{Tr("doc.promo.plan", "Plan")}: {nombrePlan ?? (idPlan.HasValue ? "#" + idPlan : "—")}";

        internal static string Valor(BE.Promocion p) =>
            p.TipoDescuento == BE.TipoDescuento.Porcentaje ? $"{p.Valor:0.##} %" : p.Valor.ToString("C2");

        private static ReporteExportable Doc(string titulo, string archivo, StringBuilder sb) =>
            new ReporteExportable { Titulo = titulo, NombreArchivo = archivo, TextoPlano = sb.ToString() };

        private static void Cabecera(StringBuilder sb, BE.Promocion p)
        {
            sb.AppendLine($"{Tr("doc.promo.promocion", "Promoción")}: #{p.IdPromocion} — {p.Nombre}");
            sb.AppendLine(AplicaA(p.IdPlan, p.NombrePlan));
            sb.AppendLine($"{Tr("doc.promo.beneficio", "Beneficio")}: {Tipo(p.TipoDescuento)} — {Valor(p)}");
            sb.AppendLine($"{Tr("doc.promo.vigencia", "Vigencia")}: {Fecha(p.FechaInicio, false)} — {Fecha(p.FechaFin, false)}");
            sb.AppendLine($"{Tr("doc.promo.estado", "Estado")}: {Estado(p.Estado)}");
            sb.AppendLine();
        }

        // "Evaluar resultado de la promoción": movimientos del plan durante la vigencia contra el
        // período anterior de igual duración, y retención real contra la esperada.
        private static void ResultadoPromociones(StringBuilder sb, List<BE.ResultadoPromocion> resultados, bool noDisponible)
        {
            sb.AppendLine(Tr("doc.promo.resultado.titulo",
                "Resultado de las promociones (vigencia contra el período anterior de igual duración):"));
            if (noDisponible)
                sb.AppendLine("   " + Tr("doc.promo.resultado.nodisponible", "No se pudieron leer las promociones: vuelva a analizar las métricas."));
            else if (resultados == null || resultados.Count == 0)
                sb.AppendLine("   " + Tr("doc.promo.resultado.sindatos", "No hay promociones vigentes, dadas de baja o vencidas en el período."));
            foreach (var r in resultados ?? new List<BE.ResultadoPromocion>())
            {
                if (r.NoSePudoCalcular)
                {
                    sb.AppendLine(string.Format(Tr("doc.promo.resultado.error",
                        "   • {0}: no se pudo calcular su resultado. Vuelva a analizar las métricas."), r.Nombre));
                    continue;
                }
                sb.AppendLine(string.Format(Tr("doc.promo.resultado.promo", "   • {0} ({1}) — plan {2} — vigencia {3:d} al {4:d} ({5} día(s)); antes: {6:d} al {7:d}"),
                    r.Nombre, Estado(r.Estado), r.NombrePlan ?? "—", r.Desde, r.Hasta.AddDays(-1), r.Dias, r.AnteriorDesde, r.Desde.AddDays(-1)));
                sb.AppendLine(string.Format(Tr("doc.promo.resultado.movimientos",
                    "      Bajas {0} (antes {1}, {2:+0;-0;0}) · Cambios a otro plan {3} (antes {4}, {5:+0;-0;0}) · Pausas {6} (antes {7}, {8:+0;-0;0}) · Renovaciones {9} (antes {10}, {11:+0;-0;0})"),
                    r.Durante.Bajas, r.Antes.Bajas, r.VariacionBajas, r.Durante.CambiosDePlan, r.Antes.CambiosDePlan, r.VariacionCambiosDePlan,
                    r.Durante.Pausas, r.Antes.Pausas, r.VariacionPausas,
                    r.Durante.Renovaciones, r.Antes.Renovaciones, r.VariacionRenovaciones));
                string esperada = r.TasaRetencionEsperada.HasValue
                    ? string.Format(Tr("doc.promo.resultado.esperada", "esperada {0:0.##} %"), r.TasaRetencionEsperada.Value)
                    : Tr("doc.promo.resultado.sinesperada", "sin tasa esperada (alta manual)");
                // La mejora existe solo si hay retención en los DOS tramos (MejoraRetencion es null si no).
                string antes = r.MejoraRetencion.HasValue
                    ? string.Format(Tr("doc.promo.resultado.antes", "antes {0:0.##} %, mejora {1:+0.##;-0.##;0} pts"),
                        r.TasaRetencionAntes.Value, r.MejoraRetencion.Value)
                    : Tr("doc.promo.resultado.sinantes", "sin base del período anterior");
                sb.AppendLine(r.TasaRetencionReal.HasValue
                    ? string.Format(Tr("doc.promo.resultado.retencion",
                        "      Clientes con la renovación en juego: {0}; retenidos: {1} ({2:0.##} %; {3}; {4})"),
                        r.ClientesEnJuego, r.ClientesRetenidos, r.TasaRetencionReal.Value, antes, esperada)
                    : string.Format(Tr("doc.promo.resultado.sinjuego",
                        "      Ningún cliente del plan tuvo la renovación en juego durante la vigencia ({0})."), esperada));
            }
            sb.AppendLine();
        }

        // «Reporte de métricas»: abandono por plan, rotación por categoría y oportunidades.
        public static ReporteExportable ReporteMetricas(BE.ReporteMetricas r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Tr("doc.promo.fechareporte", "Fecha del reporte")}: {Fecha(r.Fecha)}");
            sb.AppendLine(string.Format(Tr("doc.promo.periodo", "Período analizado: del {0:d} al {1:d}"), r.Desde, r.Hasta));
            sb.AppendLine();
            // Impacto de las promociones en los cobros del período (PN02 + N01).
            sb.AppendLine(Tr("doc.promo.impactoperiodo", "Impacto de las promociones en el período (cobros que las usaron):"));
            if (r.ImpactoPromociones.Count == 0) sb.AppendLine("   " + Tr("doc.promo.impacto.sindatos", "Ningún cobro del período usó una promoción."));
            foreach (var m in r.ImpactoPromociones)
                sb.AppendLine(string.Format(Tr("doc.promo.impactolinea", "   • {0} ({1}): {2} cobro(s) — {3:C2} descontados — {4:C2} cobrados"),
                                            m.Nombre, Estado(m.Estado), m.Cobros, m.TotalDescontado, m.TotalCobrado));
            sb.AppendLine();
            ResultadoPromociones(sb, r.ResultadoPromociones, r.ResultadoNoDisponible);
            sb.AppendLine(Tr("doc.promo.abandonoplan", "Abandono por plan (clientes en riesgo):"));
            if (r.AbandonoPorPlan.Count == 0) sb.AppendLine("   " + Tr("doc.promo.sindatos", "Sin casos detectados."));
            foreach (var m in r.AbandonoPorPlan)
                sb.AppendLine(string.Format(Tr("doc.promo.abandonolinea", "   • {0}: {1} cliente(s) en riesgo — {2:C2} de ingreso mensual en riesgo"),
                                            m.NombrePlan, m.ClientesEnRiesgo, m.IngresoMensualEnRiesgo));
            sb.AppendLine();
            sb.AppendLine(Tr("doc.promo.rotacioncategoria", "Rotación por categoría (prendas):"));
            if (r.RotacionPorCategoria.Count == 0) sb.AppendLine("   " + Tr("doc.promo.sindatos", "Sin casos detectados."));
            foreach (var m in r.RotacionPorCategoria)
                sb.AppendLine(string.Format(Tr("doc.promo.rotacionlinea", "   • {0}: {1} de baja demanda, {2} de alta demanda"),
                                            m.Categoria, m.PrendasBajaDemanda, m.PrendasAltaDemanda));
            sb.AppendLine();
            // Oportunidades por categoría: quedan como dato, pero no son base de una sugerencia.
            var porCategoria = r.RotacionPorCategoria.FindAll(m => m.EsOportunidad);
            if (porCategoria.Count > 0)
            {
                sb.AppendLine(Tr("doc.promo.oportunidadescategoria",
                    "Oportunidades por categoría (solo informativas: las promociones son por plan):"));
                foreach (var m in porCategoria)
                    sb.AppendLine("   • " + string.Format(Tr("promo.cand.rotacion",
                        "Análisis de rotación: {0} prenda(s) de la categoría {1} sin pedidos ({2})."),
                        m.PrendasBajaDemanda, m.Categoria, m.EjemplosBajaDemanda));
                sb.AppendLine();
            }
            if (r.HayOportunidad())
            {
                sb.AppendLine(Tr("doc.promo.oportunidades", "Oportunidades de promoción detectadas:"));
                foreach (var c in r.Oportunidades)
                    sb.AppendLine($"   • [{Origen(c.Origen)}] {MotivoCandidata(c)} ({Tr("doc.promo.beneficioest", "Beneficio estimado")}: {c.BeneficioEstimado:C2} = {c.IngresoMensualEnRiesgo:C2} × {c.TasaRetencion:0.##} % {Tr("doc.promo.retencionesperada", "de retención esperada")})");
            }
            else
                sb.AppendLine(Tr("doc.promo.sinoportunidad", "¿Hay oportunidad? No: no se detectaron casos para promocionar. Fin sin promoción."));
            return Doc(Tr("doc.promo.reporte.titulo", "Reporte de métricas"), $"ReporteMetricas_{r.Fecha:yyyyMMdd_HHmm}", sb);
        }

        // «Sugerencia de promoción» (Gerencia → Administración).
        public static ReporteExportable Sugerencia(BE.SugerenciaPromocion s)
        {
            var sb = new StringBuilder();
            CuerpoSugerencia(sb, s);
            return Doc($"{Tr("doc.promo.sugerencia.titulo", "Sugerencia de promoción")} #{s.IdSugerencia}", $"SugerenciaPromocion_{s.IdSugerencia}", sb);
        }

        private static void CuerpoSugerencia(StringBuilder sb, BE.SugerenciaPromocion s)
        {
            sb.AppendLine($"{Tr("doc.promo.sugerencia", "Sugerencia")}: #{s.IdSugerencia}  —  {Fecha(s.FechaAlta)}");
            sb.AppendLine($"{Tr("doc.promo.gerencia", "Registrada por (Gerencia)")}: {s.NombreUsuarioAlta ?? "—"}");
            sb.AppendLine($"{Tr("doc.promo.origen", "Origen de la métrica")}: {Origen(s.OrigenMetrica)}");
            sb.AppendLine(AplicaA(s.IdPlan, s.NombrePlan));
            sb.AppendLine($"{Tr("doc.promo.tiposugerido", "Tipo de descuento sugerido")}: {Tipo(s.TipoDescuentoSugerido)}");
            sb.AppendLine($"{Tr("doc.promo.beneficioest", "Beneficio estimado")}: {s.BeneficioEstimado:C2} = {s.IngresoEnRiesgo():C2} × {s.TasaRetencion:0.##} % {Tr("doc.promo.retencionesperada", "de retención esperada")}");
            sb.AppendLine($"{Tr("doc.promo.motivo", "Motivo")}: {s.Motivo}");
            sb.AppendLine($"{Tr("doc.promo.estado", "Estado")}: {EstadoSugerencia(s.Estado)}");
        }

        // «Constancia de descarte» de una sugerencia (¿Acepta la sugerencia? No).
        public static ReporteExportable ConstanciaDescarteSugerencia(BE.SugerenciaPromocion s)
        {
            var sb = new StringBuilder();
            CuerpoSugerencia(sb, s);
            sb.AppendLine();
            sb.AppendLine(string.Format(Tr("doc.promo.descartesug", "Administración descartó la sugerencia el {0}."), Fecha(s.FechaEvaluacion)));
            sb.AppendLine($"{Tr("doc.promo.motivodescarte", "Motivo del descarte")}: {s.MotivoDescarte}");
            return Doc($"{Tr("doc.promo.constanciadescarte.titulo", "Constancia de descarte")} — {Tr("doc.promo.sugerencia", "Sugerencia")} #{s.IdSugerencia}",
                       $"ConstanciaDescarteSugerencia_{s.IdSugerencia}", sb);
        }

        // «Ficha de promoción»: condiciones validadas e historial de estados.
        public static ReporteExportable FichaPromocion(BE.Promocion p, List<BE.PromocionHistorial> historial)
        {
            var sb = new StringBuilder();
            Cabecera(sb, p);
            if (!string.IsNullOrWhiteSpace(p.Descripcion))
                sb.AppendLine($"{Tr("doc.promo.descripcion", "Descripción")}: {p.Descripcion}");
            sb.AppendLine($"{Tr("doc.promo.margen", "Margen estimado")}: {p.MargenEstimado:C2}");
            sb.AppendLine($"{Tr("doc.promo.impacto", "Impacto económico")}: {p.ImpactoEconomico ?? "—"}");
            sb.AppendLine($"{Tr("doc.promo.creadapor", "Creada por (Administración)")}: {p.NombreUsuarioAlta ?? "—"}  —  {Fecha(p.FechaAlta)}");
            sb.AppendLine($"{Tr("doc.promo.origensug", "Origen")}: " + (p.IdSugerenciaOrigen.HasValue
                ? string.Format(Tr("doc.promo.desdesugerencia", "sugerencia de Gerencia #{0}"), p.IdSugerenciaOrigen.Value)
                : Tr("doc.promo.manual", "alta manual")));
            sb.AppendLine();
            if (historial != null && historial.Count > 0)
            {
                sb.AppendLine(Tr("doc.promo.historial", "Historial de estados:"));
                foreach (var h in historial)
                    sb.AppendLine($"   {Fecha(h.Fecha)}  {(h.EstadoAnterior.HasValue ? Estado(h.EstadoAnterior.Value) : "—")} → {Estado(h.EstadoNuevo)}" +
                                  $"  ({h.NombreUsuario ?? "—"}){(string.IsNullOrWhiteSpace(h.Observacion) ? "" : ": " + h.Observacion)}");
            }
            return Doc($"{Tr("doc.promo.ficha.titulo", "Ficha de promoción")} #{p.IdPromocion}", $"FichaPromocion_{p.IdPromocion}", sb);
        }

        // «Dictamen contable» (resultado, observación, usuario, fecha).
        public static ReporteExportable DictamenContable(BE.Promocion p, BE.DictamenContable d)
        {
            var sb = new StringBuilder();
            Cabecera(sb, p);
            sb.AppendLine($"{Tr("doc.promo.resultado", "Resultado")}: " + (d.Aprobada
                ? Tr("doc.promo.aprobada", "APROBADA — la promoción queda Vigente.")
                : Tr("doc.promo.rechazada", "RECHAZADA — vuelve a Administración para reformularla o darla de baja.")));
            sb.AppendLine($"{Tr("doc.promo.observacion", "Observación")}: {d.Observacion}");
            sb.AppendLine($"{Tr("doc.promo.contable", "Dictaminó (Contabilidad)")}: {d.NombreUsuario ?? "—"}  —  {Fecha(d.Fecha)}");
            return Doc($"{Tr("doc.promo.dictamen.titulo", "Dictamen contable")} — {Tr("doc.promo.promocion", "Promoción")} #{p.IdPromocion}",
                       $"DictamenContable_{d.IdDictamen}", sb);
        }

        // «Solicitud de baja» (Vendedor → Administración).
        public static ReporteExportable SolicitudBaja(BE.Promocion p, BE.SolicitudBajaPromocion s)
        {
            var sb = new StringBuilder();
            Cabecera(sb, p);
            sb.AppendLine($"{Tr("doc.promo.solicito", "Solicitó (Ventas)")}: {s.NombreUsuarioSolicita ?? "—"}  —  {Fecha(s.FechaSolicitud)}");
            sb.AppendLine($"{Tr("doc.promo.motivo", "Motivo")}: {s.Motivo}");
            sb.AppendLine();
            sb.AppendLine(Tr("doc.promo.solicitud.nota2", "La promoción sigue Vigente y se aplica en los cobros hasta que Administración atienda la solicitud."));
            return Doc($"{Tr("doc.promo.solicitud.titulo", "Solicitud de baja")} #{s.IdSolicitud} — {p.Nombre}", $"SolicitudBaja_{s.IdSolicitud}", sb);
        }

        // «Respuesta a la solicitud» de baja (Administración → Ventas): si estuvo de acuerdo, la
        // promoción quedó dada de baja; si no, sigue Vigente.
        public static ReporteExportable RespuestaSolicitud(BE.Promocion p, BE.SolicitudBajaPromocion s)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Tr("doc.promo.destinatario", "Destinatario")}: {Tr("doc.promo.aventas", "Ventas")}");
            sb.AppendLine();
            Cabecera(sb, p);
            sb.AppendLine($"{Tr("doc.promo.solicito", "Solicitó (Ventas)")}: {s.NombreUsuarioSolicita ?? "—"}  —  {Fecha(s.FechaSolicitud)}");
            sb.AppendLine($"{Tr("doc.promo.motivosolicitud", "Motivo de la solicitud")}: {s.Motivo}");
            sb.AppendLine($"{Tr("doc.promo.resultado", "Resultado")}: " + (s.DioDeBaja
                ? Tr("doc.promo.respuesta.dadabaja", "Administración estuvo de acuerdo: la promoción quedó dada de baja.")
                : Tr("doc.promo.respuesta.sigue", "Administración no estuvo de acuerdo: la promoción sigue Vigente.")));
            sb.AppendLine($"{Tr("doc.promo.respuesta", "Respuesta")}: {s.Respuesta ?? "—"}");
            sb.AppendLine($"{Tr("doc.promo.atendio", "Atendió (Administración)")}: {s.NombreUsuarioResuelve ?? "—"}  —  {Fecha(s.FechaResolucion)}");
            return Doc($"{Tr("doc.promo.respuesta.titulo", "Respuesta a la solicitud de baja")} #{s.IdSolicitud}",
                       $"RespuestaSolicitudBaja_{s.IdSolicitud}", sb);
        }

        // «Constancia de baja» de una promoción (Vigente o Rechazada → Dada de baja).
        public static ReporteExportable ConstanciaBaja(BE.Promocion p, BE.PromocionHistorial baja)
        {
            var sb = new StringBuilder();
            Cabecera(sb, p);
            sb.AppendLine(string.Format(Tr("doc.promo.bajaprom", "Administración dio de baja la promoción el {0} (estaba {1})."),
                                        Fecha(baja?.Fecha),
                                        baja?.EstadoAnterior != null ? Estado(baja.EstadoAnterior.Value) : "—"));
            sb.AppendLine($"{Tr("doc.promo.motivobajaadmin", "Motivo de la baja")}: {baja?.Observacion ?? "—"}");
            sb.AppendLine($"{Tr("doc.promo.dadabajapor", "Dio de baja")}: {baja?.NombreUsuario ?? "—"}");
            return Doc($"{Tr("doc.promo.constanciabaja.titulo", "Constancia de baja")} — {Tr("doc.promo.promocion", "Promoción")} #{p.IdPromocion}",
                       $"ConstanciaBajaPromocion_{p.IdPromocion}", sb);
        }

        public static void Imprimir(ReporteExportable reporte, IWin32Window propietario)
        {
            // Creator → Factory Method → Product (Exportador concreto a PDF)
            GeneradorReporte generador = new GeneradorDocumentoPromocion(reporte.Titulo);
            generador.Exportar("pdf", reporte, propietario);
        }
    }
}
