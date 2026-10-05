using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// PN01 — Decisión PURA del carril "Controlador de Stock": dada la planilla revisada
    /// (líneas con el estado real de cada prenda) y el pedido, responde "¿Selección
    /// disponible?" y qué acción corresponde (informar faltantes / confirmar / separar).
    /// Antes la tomaba GUI/ControlStockForm (TrueForAll(Disponible) + reglas de habilitación).
    /// Sin acceso a datos → testeable.
    /// </summary>
    public static class EvaluacionControlStock
    {
        public static BE.EvaluacionControlStock Evaluar(List<BE.LineaControlStock> lineas, BE.Pedido pedido)
        {
            var resultado = new BE.EvaluacionControlStock();
            if (pedido == null || pedido.Estado != BE.EstadoPedido.EnControlStock)
                return resultado;   // nada habilitado: el pedido ya no está en control de stock

            bool disponible = lineas != null && lineas.Count > 0 && lineas.All(l => l != null && l.Disponible);

            resultado.SeleccionDisponible    = disponible;
            resultado.PuedeInformarFaltantes = !disponible;
            resultado.PuedeConfirmar         = disponible && !pedido.TodasConfirmadas;
            resultado.PuedeSeparar           = disponible && pedido.TodasConfirmadas;
            return resultado;
        }
    }
}
