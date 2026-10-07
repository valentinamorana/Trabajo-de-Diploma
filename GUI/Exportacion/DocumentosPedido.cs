using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI.Exportacion
{
    /// <summary>
    /// PN01 — arma los objetos de información del diagrama de actividad de Armar pedido como
    /// documentos PDF generados directamente (ExportadorPdf, sin impresora virtual):
    ///   • Planilla de control de existencias + detalle de selección   (Vendedor → Depósito)
    ///   • Informe de disponibilidad (prendas faltantes y alternativas) (Depósito → Vendedor)
    ///   • Constancia de prendas separadas                             (Depósito)
    ///   • Confirmación y constancia del pedido                        (Vendedor → Cliente)
    ///   • Aviso de desistimiento                                       (Vendedor)
    /// Solo arma el contenido; la exportación la hace el Exportador que fabrica
    /// <see cref="GeneradorDocumentoPedido"/>.
    /// </summary>
    public static class DocumentosPedido
    {
        private static string Tr(string clave, string fallback)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(clave) ? t[clave].Texto : fallback;
        }

        private static string Fecha(DateTime? f) => f.HasValue ? f.Value.ToString("g") : "—";

        private static string Prenda(BE.Prenda p) =>
            $"#{p.IdPrenda}  {p.Nombre}  —  {p.Categoria ?? "—"} / {Tr("col.prenda.talle", "Talle")} {p.Talle ?? "—"} / {p.Color ?? "—"}";

        private static void Cabecera(StringBuilder sb, BE.Pedido pedido)
        {
            sb.AppendLine($"{Tr("doc.ped.pedido", "Pedido")}: #{pedido.IdPedido}");
            sb.AppendLine($"{Tr("doc.ped.cliente", "Cliente")}: {pedido.NombreCliente}");
            sb.AppendLine($"{Tr("doc.ped.vendedor", "Vendedor")}: {pedido.NombreEmpleado}");
            sb.AppendLine($"{Tr("doc.ped.fecha", "Fecha del pedido")}: {Fecha(pedido.FechaPedido)}");
            sb.AppendLine();
        }

        // Detalle de la selección agrupado como lo pide el diagrama (modelo, color, talle, cantidad).
        private static void DetalleAgrupado(StringBuilder sb, IEnumerable<BE.Prenda> prendas)
        {
            sb.AppendLine(Tr("doc.ped.detalle", "Detalle de la selección (prenda, talle, color, cantidad):"));
            foreach (var g in prendas.GroupBy(p => new { p.Nombre, p.Talle, p.Color }))
                sb.AppendLine($"   {g.Key.Nombre}  —  {g.Key.Talle ?? "—"} / {g.Key.Color ?? "—"}  ×{g.Count()}");
            sb.AppendLine();
        }

        // "Detalle de selección dentro del cupo": plan, límite, prendas en uso y seleccionadas.
        private static void LineaCupo(StringBuilder sb, BE.Cliente cliente, int seleccionadas)
        {
            if (cliente == null) return;
            sb.AppendLine(string.Format(
                Tr("doc.ped.cupo", "Plan {0}: {1} prenda(s) seleccionadas + {2} en uso, de {3} permitidas."),
                cliente.NombrePlan ?? "—", seleccionadas, cliente.StockUtilizado, cliente.LimitePrendas));
            sb.AppendLine();
        }

        private static void CabeceraCliente(StringBuilder sb, BE.Cliente c)
        {
            sb.AppendLine($"{Tr("doc.ped.cliente", "Cliente")}: {c.NombreCompleto}  —  DNI {c.DNI}");
            sb.AppendLine($"{Tr("doc.aviso.plan", "Plan")}: {c.NombrePlan ?? "—"}  —  {Tr("doc.aviso.vence", "Vence")}: {(c.FechaVencimiento.HasValue ? c.FechaVencimiento.Value.ToString("d") : "—")}");
            sb.AppendLine($"{Tr("doc.aviso.fecha", "Fecha del aviso")}: {Fecha(DateTime.Now)}");
            sb.AppendLine();
        }

        public static ReporteExportable PlanillaControl(BE.Pedido pedido, List<BE.LineaControlStock> lineas = null,
                                                        BE.Cliente cliente = null)
        {
            var sb = new StringBuilder();
            Cabecera(sb, pedido);
            sb.AppendLine($"{Tr("doc.ped.envio", "Enviado a control de stock")}: {Fecha(pedido.FechaEnvioControl)}");
            sb.AppendLine();
            LineaCupo(sb, cliente, pedido.CantidadPrendas);
            DetalleAgrupado(sb, pedido.Prendas);
            sb.AppendLine(Tr("doc.ped.unidades", "Unidades a controlar:"));
            foreach (var p in pedido.Prendas)
            {
                var l = lineas?.Find(x => x.Prenda.IdPrenda == p.IdPrenda);
                string estado = l == null ? "[  ]"
                    : (l.Disponible ? Tr("doc.ped.disp", "[Disponible]") : Tr("doc.ped.nodisp", "[NO disponible]"));
                sb.AppendLine($"   {estado}  {Prenda(p)}");
            }
            sb.AppendLine();
            sb.AppendLine(string.Format(Tr("doc.ped.total", "Total: {0} prenda(s)"), pedido.CantidadPrendas));

            return new ReporteExportable
            {
                Titulo        = $"{Tr("doc.planilla.titulo", "Planilla de control de existencias")} — {Tr("doc.ped.pedido", "Pedido")} #{pedido.IdPedido}",
                NombreArchivo = $"PlanillaControl_Pedido{pedido.IdPedido}",
                TextoPlano    = sb.ToString()
            };
        }

        public static ReporteExportable InformeFaltantes(BE.Pedido pedido, List<BE.PedidoFaltante> faltantes)
        {
            var sb = new StringBuilder();
            Cabecera(sb, pedido);
            sb.AppendLine($"{Tr("doc.ped.controlo", "Controló")}: {pedido.NombreEmpleadoControl ?? "—"}  —  {Fecha(pedido.FechaControl)}");
            sb.AppendLine();
            sb.AppendLine(Tr("doc.faltantes.lista", "Prendas no disponibles y alternativas sugeridas:"));
            foreach (var f in faltantes)
            {
                string motivo = f.ReservadaParaOtro
                    ? Tr("doc.faltantes.reservada", "reservada por Lista de Espera para otro cliente")
                    : string.Format(Tr("doc.faltantes.estado", "estado: {0}"), f.EstadoAlRevisar);
                sb.AppendLine($" • {Prenda(f.Prenda)}  ({motivo})");
                if (f.Alternativas.Count == 0)
                    sb.AppendLine("      " + Tr("doc.faltantes.sinalt", "Sin alternativas disponibles de la misma categoría y talle."));
                else
                    foreach (var a in f.Alternativas)
                        sb.AppendLine($"      → {Prenda(a)}");
            }

            return new ReporteExportable
            {
                Titulo        = $"{Tr("doc.faltantes.titulo", "Informe de disponibilidad (faltantes y alternativas)")} — {Tr("doc.ped.pedido", "Pedido")} #{pedido.IdPedido}",
                NombreArchivo = $"InformeFaltantes_Pedido{pedido.IdPedido}",
                TextoPlano    = sb.ToString()
            };
        }

        public static ReporteExportable ConstanciaSeparacion(BE.Pedido pedido)
        {
            var sb = new StringBuilder();
            Cabecera(sb, pedido);
            sb.AppendLine($"{Tr("doc.ped.controlo", "Controló")}: {pedido.NombreEmpleadoControl ?? "—"}  —  {Fecha(pedido.FechaControl)}");
            sb.AppendLine($"{Tr("doc.separacion.fecha", "Prendas separadas el")}: {Fecha(pedido.FechaSeparacion)}");
            sb.AppendLine();
            sb.AppendLine(Tr("doc.separacion.lista", "Prendas separadas para el pedido (quedan en uso a nombre del cliente):"));
            foreach (var p in pedido.Prendas)
                sb.AppendLine($"   {Prenda(p)}");
            sb.AppendLine();
            sb.AppendLine(string.Format(Tr("doc.ped.total", "Total: {0} prenda(s)"), pedido.CantidadPrendas));

            return new ReporteExportable
            {
                Titulo        = $"{Tr("doc.separacion.titulo", "Constancia de prendas separadas")} — {Tr("doc.ped.pedido", "Pedido")} #{pedido.IdPedido}",
                NombreArchivo = $"ConstanciaSeparacion_Pedido{pedido.IdPedido}",
                TextoPlano    = sb.ToString()
            };
        }

        public static ReporteExportable ConfirmacionPedido(BE.Pedido pedido)
        {
            var sb = new StringBuilder();
            Cabecera(sb, pedido);
            sb.AppendLine($"{Tr("doc.confirmacion.formalizado", "Pedido formalizado el")}: {Fecha(pedido.FechaFormalizacion)}");
            sb.AppendLine(Tr("doc.confirmacion.cerrada", "La selección quedó cerrada y no admite modificaciones."));
            sb.AppendLine();
            DetalleAgrupado(sb, pedido.Prendas);
            foreach (var p in pedido.Prendas)
                sb.AppendLine($"   {Prenda(p)}");
            sb.AppendLine();
            sb.AppendLine(string.Format(Tr("doc.ped.total", "Total: {0} prenda(s)"), pedido.CantidadPrendas));

            return new ReporteExportable
            {
                Titulo        = $"{Tr("doc.confirmacion.titulo", "Confirmación y constancia del pedido")} #{pedido.IdPedido}",
                NombreArchivo = $"Confirmacion_Pedido{pedido.IdPedido}",
                TextoPlano    = sb.ToString()
            };
        }

        public static ReporteExportable AvisoDesistimiento(BE.Pedido pedido)
        {
            var sb = new StringBuilder();
            Cabecera(sb, pedido);
            string etapa = pedido.EtapaDesistimiento == BE.EtapaDesistimiento.Disponibilidad
                ? Tr("doc.desist.etapa.disp", "falta de disponibilidad de prendas")
                : Tr("doc.desist.etapa.cupo", "la selección excede el cupo del plan");
            sb.AppendLine($"{Tr("doc.desist.etapa", "Desistió por")}: {etapa}");
            sb.AppendLine($"{Tr("doc.desist.motivo", "Motivo comunicado por el cliente")}: {pedido.MotivoDesistimiento}");
            sb.AppendLine();
            DetalleAgrupado(sb, pedido.Prendas);

            return new ReporteExportable
            {
                Titulo        = $"{Tr("doc.desist.titulo", "Aviso de desistimiento")} — {Tr("doc.ped.pedido", "Pedido")} #{pedido.IdPedido}",
                NombreArchivo = $"Desistimiento_Pedido{pedido.IdPedido}",
                TextoPlano    = sb.ToString()
            };
        }

        // "Aviso de suscripción no vigente" / "Aviso de pedido activo": el cliente no puede seguir.
        public static ReporteExportable AvisoImposibilidad(BE.Cliente cliente, bool porPedidoActivo, string motivo)
        {
            var sb = new StringBuilder();
            CabeceraCliente(sb, cliente);
            sb.AppendLine(motivo);
            sb.AppendLine();
            sb.AppendLine(Tr("doc.aviso.nocontinua", "No se puede armar un pedido nuevo en este momento."));

            string titulo = porPedidoActivo
                ? Tr("doc.aviso.activo.titulo", "Aviso de pedido activo")
                : Tr("doc.aviso.novigente.titulo", "Aviso de suscripción no vigente");
            return new ReporteExportable
            {
                Titulo        = $"{titulo} — {cliente.NombreCompleto}",
                NombreArchivo = (porPedidoActivo ? "AvisoPedidoActivo_" : "AvisoSuscripcionNoVigente_") + cliente.IdCliente,
                TextoPlano    = sb.ToString()
            };
        }

        // "Detalle de restricciones de cupo": límite del plan, en uso, seleccionadas y exceso.
        public static ReporteExportable RestriccionesCupo(BE.Cliente cliente, List<BE.Prenda> seleccion)
        {
            var sb = new StringBuilder();
            CabeceraCliente(sb, cliente);
            LineaCupo(sb, cliente, seleccion.Count);
            int exceso = cliente.StockUtilizado + seleccion.Count - cliente.LimitePrendas;
            sb.AppendLine(string.Format(Tr("doc.cupo.exceso", "La selección excede el cupo del plan en {0} prenda(s)."), exceso));
            sb.AppendLine(Tr("doc.cupo.opciones", "El cliente puede ajustar la selección o desistir."));
            sb.AppendLine();
            DetalleAgrupado(sb, seleccion);

            return new ReporteExportable
            {
                Titulo        = $"{Tr("doc.cupo.titulo", "Detalle de restricciones de cupo")} — {cliente.NombreCompleto}",
                NombreArchivo = $"RestriccionesCupo_{cliente.IdCliente}",
                TextoPlano    = sb.ToString()
            };
        }

        // "Detalle de prendas confirmadas" (Depósito, antes de separarlas).
        public static ReporteExportable DetallePrendasConfirmadas(BE.Pedido pedido)
        {
            var sb = new StringBuilder();
            Cabecera(sb, pedido);
            sb.AppendLine($"{Tr("doc.ped.controlo", "Controló")}: {pedido.NombreEmpleadoControl ?? "—"}  —  {Fecha(pedido.FechaControl)}");
            sb.AppendLine();
            sb.AppendLine(Tr("doc.confirmadas.lista", "Prendas confirmadas como disponibles (pendientes de separar):"));
            foreach (var p in pedido.Prendas.Where(x => pedido.PrendasConfirmadas.Contains(x.IdPrenda)))
                sb.AppendLine($"   {Prenda(p)}");
            sb.AppendLine();
            sb.AppendLine(string.Format(Tr("doc.ped.total", "Total: {0} prenda(s)"), pedido.PrendasConfirmadas.Count));

            return new ReporteExportable
            {
                Titulo        = $"{Tr("doc.confirmadas.titulo", "Detalle de prendas confirmadas")} — {Tr("doc.ped.pedido", "Pedido")} #{pedido.IdPedido}",
                NombreArchivo = $"PrendasConfirmadas_Pedido{pedido.IdPedido}",
                TextoPlano    = sb.ToString()
            };
        }

        // Muestra la vista previa del documento (desde ahí se imprime o se guarda como PDF).
        public static void Imprimir(ReporteExportable reporte, IWin32Window propietario)
        {
            // Creator → Factory Method → Product (Exportador concreto a PDF)
            GeneradorReporte generador = new GeneradorDocumentoPedido(reporte.Titulo);
            Exportador exportador = generador.CrearExportador("pdf");
            exportador.Exportar(reporte, propietario);
        }
    }
}
