using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Politicas
{
    /// <summary>
    /// PN04 — Prenda no devuelta = compra tácita (decisión de la alumna, 05/10, como NUULY): si a los
    /// 30 días de la entrega el cliente no devolvió la prenda, se le cobra la reposición y la prenda se
    /// da de baja (CU06-DEP Reportar Prenda Perdida). Antes de ese plazo la prenda sigue siendo un
    /// alquiler en curso y no se puede reportar perdida; tampoco se puede si el cliente nunca la
    /// recibió (el pedido todavía no está Entregado).
    /// </summary>
    public static class PoliticaCompraTacita
    {
        public const int DiasCompraTacita = 30;

        /// <summary>Fecha desde la que rige la compra tácita, o null si el pedido no se entregó.</summary>
        public static DateTime? FechaCompraTacita(BE.Pedido pedido)
        {
            if (pedido == null || pedido.Estado != BE.EstadoPedido.Entregado || !pedido.FechaEntrega.HasValue)
                return null;
            return pedido.FechaEntrega.Value.Date.AddDays(DiasCompraTacita);
        }

        /// <summary>True si la prenda del pedido ya se puede reportar perdida (pedido Entregado hace 30 días o más).</summary>
        public static bool PlazoVencido(BE.Pedido pedido, DateTime hoy)
        {
            var desde = FechaCompraTacita(pedido);
            return desde.HasValue && hoy.Date >= desde.Value;
        }

        /// <summary>
        /// PN04 — pedido atrasado: Entregado, SIN devolución registrada y con el plazo de la compra
        /// tácita cumplido (30 días o más desde la entrega). Es el mismo umbral que habilita Reportar
        /// Prenda Perdida: la lista de atrasados es la cola de "reclamar o reportar perdida".
        /// </summary>
        public static bool EstaAtrasado(BE.Pedido pedido, DateTime hoy) =>
            pedido != null && !pedido.FechaDevolucion.HasValue && PlazoVencido(pedido, hoy);

        /// <summary>Filtra los pedidos atrasados (ver <see cref="EstaAtrasado"/>).</summary>
        public static List<BE.Pedido> Atrasados(IEnumerable<BE.Pedido> pedidos, DateTime hoy) =>
            (pedidos ?? Enumerable.Empty<BE.Pedido>()).Where(p => EstaAtrasado(p, hoy)).ToList();

        /// <summary>
        /// Valida la regla y lanza AppException con el motivo: pedido no entregado (el cliente nunca
        /// recibió la prenda) o plazo de 30 días todavía corriendo.
        /// </summary>
        public static void Exigir(BE.Pedido pedido, DateTime hoy)
        {
            var desde = FechaCompraTacita(pedido);
            if (!desde.HasValue)
                throw new BE.AppException("err.bll.insp.perdida_no_entregado",
                    "Solo se puede reportar como perdida una prenda de un pedido Entregado: el cliente todavía no la recibió.");
            if (hoy.Date < desde.Value)
                throw new BE.AppException("err.bll.insp.perdida_plazo",
                    "La compra tácita rige a los {0} días de la entrega: desde el {1:d}. Faltan {2} día(s).",
                    DiasCompraTacita, desde.Value, (desde.Value - hoy.Date).Days);
        }
    }
}
