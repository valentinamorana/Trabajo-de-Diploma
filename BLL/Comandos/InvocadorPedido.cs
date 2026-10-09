using System.Collections.Generic;

namespace BLL.Comandos
{
    /// <summary>
    /// Invoker del patrón Command — equivalente a EmpresaInvoker del ejemplo de cátedra: acumula
    /// comandos con TomarOrden y los ejecuta en lote con ProcesarOrdenes. Además guarda el historial
    /// de lo ejecutado para poder deshacer la última orden reversible de un pedido (cancelar →
    /// reactivar). Vive en la instancia de la pantalla: el historial dura lo que dura la pantalla.
    /// </summary>
    public sealed class InvocadorPedido
    {
        private readonly List<PedidoCommand> _ordenes = new List<PedidoCommand>();
        private readonly List<PedidoCommand> _historial = new List<PedidoCommand>();

        public void TomarOrden(PedidoCommand cmd) => _ordenes.Add(cmd);

        public void ProcesarOrdenes()
        {
            // Una orden que falla corta el lote; las ya ejecutadas quedan en el historial.
            try
            {
                foreach (var orden in _ordenes)
                {
                    orden.Ejecutar();
                    _historial.Add(orden);
                }
            }
            finally { _ordenes.Clear(); }
        }

        /// <summary>¿La última orden ejecutada sobre este pedido se puede deshacer?</summary>
        public bool PuedeDeshacer(int idPedido)
        {
            var ultima = _historial.FindLast(o => o.IdPedido == idPedido);
            return ultima != null && ultima.EsReversible;
        }

        /// <summary>Deshace la última orden ejecutada sobre el pedido (si es reversible) y la saca del historial.</summary>
        public void DeshacerUltima(int idPedido)
        {
            var ultima = _historial.FindLast(o => o.IdPedido == idPedido);
            if (ultima == null || !ultima.EsReversible)
                throw new BE.AppException("err.bll.comando.no_reversible", "Esta operación sobre el pedido no se puede deshacer.");
            ultima.Deshacer();
            _historial.Remove(ultima);
        }
    }
}
