using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI.Exportacion
{
    /// <summary>
    /// PN02 — arma los objetos de información del diagrama de actividad de Comercialización de
    /// la suscripción como documentos PDF generados directamente (ExportadorPdf, sin impresora virtual):
    ///   • Planes disponibles                        (Vendedor → Cliente)
    ///   • Aviso de desistimiento                    (Vendedor)
    ///   • Orden de cobro (contratación pendiente)   (Vendedor → Caja)
    ///   • Liquidación (importe a cobrar)            (Caja)
    ///   • Comprobante                               (Caja → Cliente)
    ///   • Constancia de suscripción                 (Caja → Cliente)
    ///   • Intentos de cobro y Constancia de cancelación (Caja)
    /// Solo da formato a datos que ya resolvió la BLL; la exportación la hace el Exportador que
    /// fabrica <see cref="GeneradorDocumentoContratacion"/>.
    /// </summary>
    public static class DocumentosContratacion
    {
        private static string Tr(string clave, string fallback)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(clave) ? t[clave].Texto : fallback;
        }

        private static string Fecha(DateTime? f, bool conHora = true) =>
            f.HasValue ? f.Value.ToString(conHora ? "g" : "d") : "—";

        public static string Modalidad(BE.Builders.ModalidadCobro m)
        {
            switch (m)
            {
                case BE.Builders.ModalidadCobro.Trimestral: return Tr("modalidad.trimestral", "Trimestral");
                case BE.Builders.ModalidadCobro.Anual:      return Tr("modalidad.anual", "Anual");
                default:                                    return Tr("modalidad.mensual", "Mensual");
            }
        }

        private static void Cabecera(StringBuilder sb, BE.Contratacion c)
        {
            sb.AppendLine($"{Tr("doc.contr.numero", "Contratación")}: #{c.IdContratacion}");
            sb.AppendLine($"{Tr("doc.ped.cliente", "Cliente")}: {c.NombreCliente}");
            sb.AppendLine($"{Tr("doc.contr.plan", "Plan")}: {c.NombrePlan}  —  {Tr("doc.contr.modalidad", "Modalidad")}: {Modalidad(c.Modalidad)}");
            sb.AppendLine($"{Tr("doc.ped.vendedor", "Vendedor")}: {c.NombreVendedor ?? "—"}  —  {Tr("doc.contr.alta", "Registrada el")}: {Fecha(c.FechaAlta)}");
            sb.AppendLine();
        }

        private static void Importes(StringBuilder sb, BE.LiquidacionContratacion l)
        {
            if (l == null) return;
            sb.AppendLine($"{Tr("doc.contr.bruto", "Importe del plan")}: {l.Bruto:C2}");
            if (l.Descuento > 0)
                sb.AppendLine($"{Tr("doc.contr.descuento", "Descuento")}: -{l.Descuento:C2}  ({l.NombrePromocion ?? Tr("lbl.contratacion.creditoreferido", "crédito por referido")})");
            if (l.CreditoCambioPlan > 0)
                sb.AppendLine(string.Format(Tr("doc.contr.creditoupgrade",
                    "Crédito por cambio a un plan superior (días no usados del plan anterior): -{0:C2}. El plan nuevo rige desde hoy."),
                    l.CreditoCambioPlan));
            if (l.CambioProgramadoDesde.HasValue)
                sb.AppendLine(string.Format(Tr("doc.contr.programado",
                    "Cambio a un plan igual o más barato: el plan actual sigue hasta el {0:d}; desde ese día rige el nuevo."),
                    l.CambioProgramadoDesde.Value));
            if (l.Cargos > 0)
                sb.AppendLine(string.Format(Tr("doc.contr.cargos",
                    "Cargos por daño o pérdida de prendas: {0} cargo(s), +{1:C2}"), l.CantidadCargos, l.Cargos));
            sb.AppendLine($"{Tr("doc.contr.total", "Total a cobrar")}: {l.Total:C2}");
            if (l.CantidadCuotas > 1 || l.RecargoCuotas > 0)
                sb.AppendLine(string.Format(Tr("doc.contr.cuotas.detalle",
                    "Tarjeta de crédito en {0} cuota(s) de {1:C2} — recargo {2:C2} — total a abonar {3:C2}"),
                    l.CantidadCuotas, l.ValorCuota, l.RecargoCuotas, l.TotalConRecargo));
            sb.AppendLine(Tr("doc.contr.undescuento", "Se aplica un solo descuento por cobro: el mayor entre la promoción vigente del plan y el crédito por referido."));
        }

        private static ReporteExportable Doc(string titulo, string archivo, StringBuilder sb) =>
            new ReporteExportable { Titulo = titulo, NombreArchivo = archivo, TextoPlano = sb.ToString() };

        // «Planes disponibles»: nombre, precio mensual y límite de prendas.
        public static ReporteExportable PlanesDisponibles(List<BE.PlanSuscripcion> planes)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Tr("doc.aviso.fecha", "Fecha del aviso")}: {Fecha(DateTime.Now)}");
            sb.AppendLine();
            foreach (var p in planes)
                sb.AppendLine(string.Format(Tr("doc.contr.planlinea", "• {0}: {1:C2} por mes — hasta {2} prenda(s) en simultáneo"),
                                            p.Nombre, p.Precio, p.LimitePrendas));
            sb.AppendLine();
            sb.AppendLine(Tr("doc.contr.modalidades", "Modalidades de cobro: mensual, trimestral (3 meses) o anual (12 meses). Sin permanencia."));
            return Doc(Tr("doc.contr.planes.titulo", "Planes disponibles"), "PlanesDisponibles", sb);
        }

        // «Aviso de desistimiento» (el cliente no eligió plan y modalidad).
        public static ReporteExportable AvisoDesistimiento(BE.DesistimientoContratacion d)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Tr("doc.ped.cliente", "Cliente")}: {d.NombreCliente}");
            sb.AppendLine($"{Tr("doc.ped.vendedor", "Vendedor")}: {d.NombreVendedor}  —  {Tr("doc.aviso.fecha", "Fecha del aviso")}: {Fecha(d.Fecha)}");
            sb.AppendLine($"{Tr("doc.contr.planconsiderado", "Plan considerado")}: " +
                          (d.NombrePlan != null ? $"{d.NombrePlan} ({(d.Modalidad.HasValue ? Modalidad(d.Modalidad.Value) : "—")})" : "—"));
            sb.AppendLine($"{Tr("doc.desist.motivo", "Motivo comunicado por el cliente")}: {d.Motivo}");
            sb.AppendLine();
            sb.AppendLine(Tr("doc.contr.desist.nota", "No se registró ninguna contratación ni se generó un cobro."));
            return Doc($"{Tr("doc.desist.titulo", "Aviso de desistimiento")} — {d.NombreCliente}", $"DesistimientoContratacion_{d.IdDesistimiento}", sb);
        }

        // «Orden de cobro»: la contratación pendiente que el cliente lleva a Caja.
        public static ReporteExportable OrdenDeCobro(BE.Contratacion c, BE.LiquidacionContratacion l)
        {
            var sb = new StringBuilder();
            Cabecera(sb, c);
            Importes(sb, l);
            sb.AppendLine();
            sb.AppendLine(Tr("doc.contr.orden.nota", "La suscripción queda vigente recién cuando Caja confirma el cobro."));
            return Doc($"{Tr("doc.contr.orden.titulo", "Orden de cobro")} #{c.IdContratacion}", $"OrdenCobro_{c.IdContratacion}", sb);
        }

        // «Liquidación»: lo que Caja calcula antes de cobrar.
        public static ReporteExportable Liquidacion(BE.Contratacion c, BE.LiquidacionContratacion l, List<BE.IntentoPago> intentos = null,
                                                    List<BE.PlanCuotas> planesCuotas = null)
        {
            var sb = new StringBuilder();
            Cabecera(sb, c);
            Importes(sb, l);
            // PN02 — «Planes de cuotas disponibles» (tarjeta de crédito) para esta modalidad.
            if (l != null && planesCuotas != null && planesCuotas.Count > 0)
            {
                sb.AppendLine(Tr("doc.contr.cuotas.opciones", "Con tarjeta de crédito se puede pagar en:"));
                foreach (var p in planesCuotas)
                {
                    var f = BLL.Politicas.PoliticaCuotas.Financiar(l.Total, p);
                    sb.AppendLine(string.Format(Tr("doc.contr.cuotas.opcion", "   {0} cuota(s) de {1:C2}  —  recargo {2:0.##} %  —  total {3:C2}"),
                        f.CantidadCuotas, f.ValorCuota, f.RecargoPorcentaje, f.TotalFinanciado));
                }
            }
            sb.AppendLine();
            if (intentos != null && intentos.Count > 0) IntentosTexto(sb, intentos);
            else sb.AppendLine($"{Tr("doc.contr.intentos", "Intentos de cobro fallidos")}: {c.IntentosPago}/{BE.Contratacion.MaxIntentosPago}");
            return Doc($"{Tr("doc.contr.liq.titulo", "Liquidación")} — {Tr("doc.contr.numero", "Contratación")} #{c.IdContratacion}", $"Liquidacion_{c.IdContratacion}", sb);
        }

        // «Comprobante»: emitido al confirmar el cobro.
        // N01 — «Comprobante» del cobro recurrente de la suscripción (mismo formato que el de PN02).
        public static ReporteExportable ComprobanteCobro(BE.Cobro c, BE.Cliente cliente)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var sb = new StringBuilder();
            sb.AppendLine($"{Tr("doc.contr.comprobante", "Comprobante")}: {c.NumeroComprobante ?? "—"}  —  {Fecha(c.FechaResolucion)}");
            sb.AppendLine();
            sb.AppendLine($"{Tr("doc.ped.cliente", "Cliente")}: {c.NombreCliente ?? cliente?.NombreCompleto}");
            sb.AppendLine($"{Tr("doc.contr.plan", "Plan")}: {cliente?.NombrePlan ?? "—"}" +
                          (c.Modalidad.HasValue ? $"  —  {Tr("doc.contr.modalidad", "Modalidad")}: {Modalidad(c.Modalidad.Value)}" : ""));
            if (cliente?.FechaVencimiento != null)
                sb.AppendLine(string.Format(Tr("doc.cobro.vigencia", "Suscripción vigente hasta el {0}."), Fecha(cliente.FechaVencimiento, false)));
            sb.AppendLine($"{Tr("doc.contr.medio", "Medio de pago")}: {c.NombreMedioPago ?? "—"}");
            sb.AppendLine($"{Tr("doc.contr.cobro", "Cobró")}: {c.Actor ?? "—"}");
            if (c.DescuentoAplicado.HasValue)
                sb.AppendLine($"{Tr("doc.contr.descuento", "Descuento")}: -{c.DescuentoAplicado.Value:C2}");
            sb.AppendLine($"{Tr("doc.contr.importe", "Importe cobrado")}: {c.Importe:C2}");
            return Doc($"{Tr("doc.contr.comprobante", "Comprobante")} {c.NumeroComprobante}", $"Comprobante_{c.NumeroComprobante ?? c.IdCobro.ToString()}", sb);
        }

        public static ReporteExportable Comprobante(BE.Contratacion c)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Tr("doc.contr.comprobante", "Comprobante")}: {c.NumeroComprobante}  —  {Fecha(c.FechaComprobante)}");
            sb.AppendLine();
            Cabecera(sb, c);
            sb.AppendLine($"{Tr("doc.contr.medio", "Medio de pago")}: {c.NombreMedioPago ?? "—"}");
            sb.AppendLine($"{Tr("doc.contr.cobro", "Cobró")}: {c.NombreCaja ?? "—"}");
            if (c.DescuentoAplicado.HasValue && c.DescuentoAplicado.Value > 0)
                sb.AppendLine($"{Tr("doc.contr.descuento", "Descuento")}: -{c.DescuentoAplicado.Value:C2}");
            if (c.CreditoCambioPlan.HasValue)
                sb.AppendLine(string.Format(Tr("doc.contr.creditoupgrade.cobrado",
                    "Crédito por cambio a un plan superior: -{0:C2}"), c.CreditoCambioPlan.Value));
            sb.AppendLine($"{Tr("doc.contr.importe", "Importe cobrado")}: {(c.Importe.HasValue ? c.Importe.Value.ToString("C2") : "—")}");
            if (c.IdPlanCuotas.HasValue)
            {
                sb.AppendLine(string.Format(Tr("doc.contr.cuotas.plan", "Cuotas: {0}  —  recargo por financiación {1:0.##} %: {2:C2}"),
                    c.CantidadCuotas ?? 1, c.RecargoPorcentaje ?? 0m, c.RecargoCuotas ?? 0m));
                if (c.ValorCuota.HasValue)
                    sb.AppendLine(string.Format(Tr("doc.contr.cuotas.valor", "Valor de cada cuota: {0:C2}"), c.ValorCuota.Value));
                sb.AppendLine($"{Tr("doc.contr.totalabonado", "Total abonado")}: {(c.TotalAbonado.HasValue ? c.TotalAbonado.Value.ToString("C2") : "—")}");
            }
            return Doc($"{Tr("doc.contr.comprobante", "Comprobante")} {c.NumeroComprobante}", $"Comprobante_{c.NumeroComprobante}", sb);
        }

        // «Constancia de suscripción»: período activado por el cobro.
        public static ReporteExportable ConstanciaSuscripcion(BE.Contratacion c)
        {
            string referenteAcreditado = c.NombreReferenteAcreditado;
            var sb = new StringBuilder();
            Cabecera(sb, c);
            sb.AppendLine(string.Format(Tr("doc.contr.vigencia", "Suscripción vigente desde el {0} hasta el {1}."),
                                        Fecha(c.VigenciaDesde, false), Fecha(c.VigenciaHasta, false)));
            sb.AppendLine($"{Tr("doc.contr.comprobante", "Comprobante")}: {c.NumeroComprobante}");
            if (!string.IsNullOrEmpty(referenteAcreditado))
                sb.AppendLine(string.Format(Tr("doc.contr.referido", "Se acreditó el beneficio por referido a {0}."), referenteAcreditado));
            return Doc($"{Tr("doc.contr.constancia.titulo", "Constancia de suscripción")} — {c.NombreCliente}", $"ConstanciaSuscripcion_{c.IdContratacion}", sb);
        }

        // «Constancia de cancelación»: máximo de intentos de cobro alcanzado.
        public static ReporteExportable ConstanciaCancelacion(BE.Contratacion c, List<BE.IntentoPago> intentos)
        {
            var sb = new StringBuilder();
            Cabecera(sb, c);
            if (!string.IsNullOrEmpty(c.MotivoAnulacion))
            {
                // Anulada por Caja antes de cobrarla (no por agotar los intentos).
                sb.AppendLine(string.Format(Tr("doc.contr.anulada", "La contratación fue anulada por Caja el {0}. Motivo: {1}"),
                                            Fecha(c.FechaResolucion), c.MotivoAnulacion));
                sb.AppendLine($"{Tr("doc.contr.anulo", "Anuló")}: {c.NombreCaja ?? "—"}");
                if (intentos != null && intentos.Count > 0) { sb.AppendLine(); IntentosTexto(sb, intentos); }
            }
            else
            {
                sb.AppendLine(string.Format(Tr("doc.contr.cancelada", "La contratación se canceló el {0} al no concretarse el pago en {1} intentos."),
                                            Fecha(c.FechaResolucion), BE.Contratacion.MaxIntentosPago));
                sb.AppendLine();
                IntentosTexto(sb, intentos);
            }
            return Doc($"{Tr("doc.contr.cancelacion.titulo", "Constancia de cancelación")} — {Tr("doc.contr.numero", "Contratación")} #{c.IdContratacion}", $"ConstanciaCancelacion_{c.IdContratacion}", sb);
        }

        private static void IntentosTexto(StringBuilder sb, List<BE.IntentoPago> intentos)
        {
            sb.AppendLine(Tr("doc.contr.intentos", "Intentos de cobro fallidos") + ":");
            foreach (var i in intentos)
                sb.AppendLine($"   {i.NroIntento}. {Fecha(i.Fecha)}  —  {i.NombreMedioPago ?? "—"}  —  {i.Motivo}  ({i.NombreCaja ?? "—"})");
        }

        public static void Imprimir(ReporteExportable reporte, IWin32Window propietario)
        {
            // Creator → Factory Method → Product (Exportador concreto a PDF)
            GeneradorReporte generador = new GeneradorDocumentoContratacion(reporte.Titulo);
            Exportador exportador = generador.CrearExportador("pdf");
            exportador.Exportar(reporte, propietario);
        }
    }
}
