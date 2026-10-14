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

        /// <summary>Prendas que volvieron efectivamente (sin las reportadas como perdidas), tras Ejecutar.</summary>
        public int PrendasDevueltas { get; private set; }

        public override void Ejecutar() => PrendasDevueltas = _receptor.RegistrarDevolucion(_modulo, _pedido);
    }
}
