namespace BLL.Comandos
{
    /// <summary>Comando concreto: despacha un pedido formalizado (Pendiente → Despachado). No es reversible.</summary>
    public sealed class DespachoCommand : PedidoCommand
    {
        public DespachoCommand(Interfaces.IPedidoService receptor, BE.Pedido pedido, string modulo)
            : base(receptor, pedido, modulo)
        {
        }

        public override void Ejecutar() => _receptor.Despachar(_modulo, _pedido);
    }
}
