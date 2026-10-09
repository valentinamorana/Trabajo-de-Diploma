namespace BLL.Comandos
{
    /// <summary>
    /// Comando concreto: cancela un pedido en control de stock, separado o pendiente de despacho
    /// (equivalente a BajaStockCommand). Es reversible: Deshacer lo reactiva (vuelve a control de
    /// stock, con las mismas validaciones que BLL.Pedido.DesCancelar).
    /// </summary>
    public sealed class CancelacionCommand : PedidoCommand
    {
        private readonly string _motivo;

        public CancelacionCommand(Interfaces.IPedidoService receptor, BE.Pedido pedido, string modulo, string motivo)
            : base(receptor, pedido, modulo)
        {
            _motivo = motivo;
        }

        public override void Ejecutar() => _receptor.Cancelar(_modulo, _pedido, _motivo);

        public override bool EsReversible => true;

        // Se relee el pedido: el estado en memoria puede no reflejar la cancelación.
        public override void Deshacer() =>
            _receptor.DesCancelar(_modulo, _receptor.ObtenerPorId(_pedido.IdPedido) ?? _pedido);
    }
}
