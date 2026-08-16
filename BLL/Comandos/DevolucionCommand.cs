namespace BLL.Comandos
{
    /// <summary>
    /// Comando concreto: registra la devolución de las prendas de un pedido Entregado
    /// (equivalente a AltaStockCommand).
    /// </summary>
    public sealed class DevolucionCommand : PedidoCommand
    {
        public DevolucionCommand(Interfaces.IPedidoService receptor, BE.Pedido pedido, string modulo)
            : base(receptor, pedido, modulo)
        {
        }

        public override void Ejecutar() => _receptor.RegistrarDevolucion(_modulo, _pedido);
    }
}
