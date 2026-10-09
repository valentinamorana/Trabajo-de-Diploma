namespace BLL.Comandos
{
    /// <summary>
    /// Patrón Command (PN01 y PN04 — operaciones sobre un pedido). Equivalente a OrdenCommand del
    /// ejemplo de cátedra: clase abstracta (no interfaz) cuyo constructor recibe y guarda el Receiver
    /// (BLL.Pedido) y los datos de la operación, con el método abstracto Ejecutar().
    /// A diferencia del ejemplo, un comando puede ser reversible (Deshacer): el Invocador guarda el
    /// historial de lo ejecutado y puede deshacer la última orden reversible (por ejemplo, cancelar →
    /// reactivar). Los que no se pueden revertir (despachar, entregar, devolver) lo informan con
    /// EsReversible = false.
    /// </summary>
    public abstract class PedidoCommand
    {
        protected readonly Interfaces.IPedidoService _receptor;
        protected readonly BE.Pedido _pedido;
        protected readonly string _modulo;

        public PedidoCommand(Interfaces.IPedidoService receptor, BE.Pedido pedido, string modulo)
        {
            _receptor = receptor;
            _pedido = pedido;
            _modulo = modulo;
        }

        public int IdPedido => _pedido.IdPedido;

        public abstract void Ejecutar();

        /// <summary>True si la operación se puede revertir con <see cref="Deshacer"/>.</summary>
        public virtual bool EsReversible => false;

        /// <summary>Revierte la operación. Solo los comandos reversibles lo implementan.</summary>
        public virtual void Deshacer() =>
            throw new BE.AppException("err.bll.comando.no_reversible", "Esta operación sobre el pedido no se puede deshacer.");
    }
}
