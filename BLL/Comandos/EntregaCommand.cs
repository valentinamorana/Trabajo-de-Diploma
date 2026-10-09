namespace BLL.Comandos
{
    /// <summary>Comando concreto: registra la entrega al cliente (Despachado → Entregado). No es reversible.</summary>
    public sealed class EntregaCommand : PedidoCommand
    {
        public EntregaCommand(Interfaces.IPedidoService receptor, BE.Pedido pedido, string modulo)
            : base(receptor, pedido, modulo)
        {
        }

        public override void Ejecutar() => _receptor.MarcarEntregado(_modulo, _pedido);
    }
}
