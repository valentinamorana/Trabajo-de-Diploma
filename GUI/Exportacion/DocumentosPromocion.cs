using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI.Exportacion
{
    /// <summary>
    /// PN03 — arma los objetos de información del diagrama de actividad de Métricas, promociones
    /// y toma de decisiones como documentos PDF generados directamente
    /// (ExportadorPdf, sin impresora virtual):
    ///   • Reporte de métricas                               (Gerencia)
    ///   • Sugerencia de promoción                           (Gerencia → Administración)
    ///   • Constancia de descarte (de sugerencia o promoción) (Administración)
    ///   • Ficha de promoción (con su historial de estados)  (Administración → Contabilidad)
    ///   • Dictamen contable                                 (Contabilidad → Administración)
    ///   • Solicitud de baja                                 (Vendedor → Administración)
    ///   • Resolución de baja (informe a Gerencia o a Ventas) (Administración)
    /// Solo da formato a datos que ya resolvió la BLL; la exportación la hace el Exportador que
    /// fabrica <see cref="GeneradorDocumentoPromocion"/>.
    /// </summary>
    public static class DocumentosPromocion
    {
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
                case BE.EstadoPromocion.BajaSolicitada:        return Tr("est.promo.bajasolicitada", "Baja solicitada");
                case BE.EstadoPromocion.Desactivada:           return Tr("est.promo.desactivada", "Desactivada");
                case BE.EstadoPromocion.Descartada:            return Tr("est.promo.descartada", "Descartada");
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

        public static string AplicaA(int? idPlan, string nombrePlan, string categoria) =>
            idPlan.HasValue
                ? $"{Tr("doc.promo.plan", "Plan")}: {nombrePlan ?? "#" + idPlan}"
                : $"{Tr("doc.promo.categoria", "Categoría")}: {categoria}";

        private static string Valor(BE.Promocion p) =>
            p.TipoDescuento == BE.TipoDescuento.Porcentaje ? $"{p.Valor:0.##} %" : p.Valor.ToString("C2");

        private static ReporteExportable Doc(string titulo, string archivo, StringBuilder sb) =>
            new ReporteExportable { Titulo = titulo, NombreArchivo = archivo, TextoPlano = sb.ToString() };

        private static void Cabecera(StringBuilder sb, BE.Promocion p)
        {
            sb.AppendLine($"{Tr("doc.promo.promocion", "Promoción")}: #{p.IdPromocion} — {p.Nombre}");
            sb.AppendLine(AplicaA(p.IdPlan, p.NombrePlan, p.CategoriaPrenda));
            sb.AppendLine($"{Tr("doc.promo.beneficio", "Beneficio")}: {Tipo(p.TipoDescuento)} — {Valor(p)}");
            sb.AppendLine($"{Tr("doc.promo.vigencia", "Vigencia")}: {Fecha(p.FechaInicio, false)} — {Fecha(p.FechaFin, false)}");
            sb.AppendLine($"{Tr("doc.promo.estado", "Estado")}: {Estado(p.Estado)}");
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
            sb.AppendLine(Tr("doc.promo.impacto", "Impacto de las promociones en el período (cobros que las usaron):"));
            if (r.ImpactoPromociones.Count == 0) sb.AppendLine("   " + Tr("doc.promo.impacto.sindatos", "Ningún cobro del período usó una promoción."));
            foreach (var m in r.ImpactoPromociones)
                sb.AppendLine(string.Format(Tr("doc.promo.impactolinea", "   • {0} ({1}): {2} cobro(s) — {3:C2} descontados — {4:C2} cobrados"),
                                            m.Nombre, Estado(m.Estado), m.Cobros, m.TotalDescontado, m.TotalCobrado));
            sb.AppendLine();
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
            if (r.HayOportunidad())
            {
                sb.AppendLine(Tr("doc.promo.oportunidades", "Oportunidades de promoción detectadas:"));
                foreach (var c in r.Oportunidades)
                    sb.AppendLine($"   • [{Origen(c.Origen)}] {c.Motivo} ({Tr("doc.promo.beneficioest", "Beneficio estimado")}: {c.BeneficioEstimado:C2})");
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
            sb.AppendLine(AplicaA(s.IdPlan, s.NombrePlan, s.CategoriaPrenda));
            sb.AppendLine($"{Tr("doc.promo.tiposugerido", "Tipo de descuento sugerido")}: {Tipo(s.TipoDescuentoSugerido)}");
            sb.AppendLine($"{Tr("doc.promo.beneficioest", "Beneficio estimado")}: {s.BeneficioEstimado:C2}");
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
            if (p.AplicaACategoria())
                sb.AppendLine(Tr("doc.promo.notacategoria", "Promoción por categoría: es informativa, no descuenta en el cobro de la suscripción."));
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
                : Tr("doc.promo.rechazada", "RECHAZADA — vuelve a Administración para reformularla o descartarla.")));
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
            sb.AppendLine(Tr("doc.promo.solicitud.nota", "Mientras Administración resuelve la solicitud, la promoción no se aplica en los cobros."));
            return Doc($"{Tr("doc.promo.solicitud.titulo", "Solicitud de baja")} #{s.IdSolicitud} — {p.Nombre}", $"SolicitudBaja_{s.IdSolicitud}", sb);
        }

        // «Resolución de baja»: informe a Gerencia si se aprueba, a Ventas si se rechaza.
        public static ReporteExportable ResolucionBaja(BE.Promocion p, BE.SolicitudBajaPromocion s)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Tr("doc.promo.destinatario", "Destinatario")}: " + (s.FueAprobada()
                ? Tr("doc.promo.agerencia", "Gerencia")
                : Tr("doc.promo.aventas", "Ventas")));
            sb.AppendLine();
            Cabecera(sb, p);
            sb.AppendLine($"{Tr("doc.promo.solicito", "Solicitó (Ventas)")}: {s.NombreUsuarioSolicita ?? "—"}  —  {Fecha(s.FechaSolicitud)}");
            sb.AppendLine($"{Tr("doc.promo.motivosolicitud", "Motivo de la solicitud")}: {s.Motivo}");
            sb.AppendLine($"{Tr("doc.promo.resultado", "Resultado")}: " + (s.FueAprobada()
                ? Tr("doc.promo.bajaaprobada", "Baja APROBADA — la promoción quedó Desactivada.")
                : Tr("doc.promo.bajarechazada", "Baja RECHAZADA — la promoción sigue Vigente.")));
            sb.AppendLine($"{Tr("doc.promo.motivoresolucion", "Motivo de la resolución")}: {s.MotivoResolucion ?? "—"}");
            sb.AppendLine($"{Tr("doc.promo.resolvio", "Resolvió (Administración)")}: {s.NombreUsuarioResuelve ?? "—"}  —  {Fecha(s.FechaResolucion)}");
            string titulo = s.FueAprobada()
                ? Tr("doc.promo.resolucion.gerencia", "Resolución de baja — Informe a Gerencia")
                : Tr("doc.promo.resolucion.ventas", "Resolución de baja — Informe a Ventas");
            return Doc($"{titulo} #{s.IdSolicitud}", $"ResolucionBaja_{s.IdSolicitud}", sb);
        }

        // «Constancia de descarte» de una promoción (¿Reformular? No).
        public static ReporteExportable ConstanciaDescartePromocion(BE.Promocion p, BE.PromocionHistorial descarte)
        {
            var sb = new StringBuilder();
            Cabecera(sb, p);
            sb.AppendLine(string.Format(Tr("doc.promo.descarteprom", "Administración descartó la promoción el {0} tras el rechazo contable."),
                                        Fecha(descarte?.Fecha)));
            sb.AppendLine($"{Tr("doc.promo.motivodescarte", "Motivo del descarte")}: {descarte?.Observacion ?? "—"}");
            sb.AppendLine($"{Tr("doc.promo.descartadapor", "Descartó")}: {descarte?.NombreUsuario ?? "—"}");
            return Doc($"{Tr("doc.promo.constanciadescarte.titulo", "Constancia de descarte")} — {Tr("doc.promo.promocion", "Promoción")} #{p.IdPromocion}",
                       $"ConstanciaDescartePromocion_{p.IdPromocion}", sb);
        }

        public static void Imprimir(ReporteExportable reporte, IWin32Window propietario)
        {
            // Creator → Factory Method → Product (Exportador concreto a PDF)
            GeneradorReporte generador = new GeneradorDocumentoPromocion(reporte.Titulo);
            Exportador exportador = generador.CrearExportador("pdf");
            exportador.Exportar(reporte, propietario);
        }
    }
}
