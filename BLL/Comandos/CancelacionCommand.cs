namespace BLL.Comandos
{
    /// <summary>Comando concreto: cancela un pedido Pendiente (equivalente a BajaStockCommand).</summary>
    public sealed class CancelacionCommand : PedidoCommand
    {
        private readonly string _motivo;

        public CancelacionCommand(Interfaces.IPedidoService receptor, BE.Pedido pedido, string modulo, string motivo)
            : base(receptor, pedido, modulo)
        {
            _motivo = motivo;
        }

        public override void Ejecutar() => _receptor.Cancelar(_modulo, _pedido, _motivo);
    }
}
