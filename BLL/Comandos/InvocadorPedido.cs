using System.Collections.Generic;

namespace BLL.Comandos
{
    /// <summary>
    /// Invoker del patrón Command — equivalente a EmpresaInvoker del ejemplo de cátedra:
    /// acumula comandos con TomarOrden y los ejecuta todos en lote con ProcesarOrdenes,
    /// vaciando la cola al terminar. No ejecuta inmediato ni mantiene historial de deshacer.
    /// </summary>
    public sealed class InvocadorPedido
    {
        private readonly List<PedidoCommand> _ordenes = new List<PedidoCommand>();

        public void TomarOrden(PedidoCommand cmd) => _ordenes.Add(cmd);

        public void ProcesarOrdenes()
        {
            foreach (var orden in _ordenes)
                orden.Ejecutar();
            _ordenes.Clear();
        }
    }
}
