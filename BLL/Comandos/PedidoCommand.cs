namespace BLL.Comandos
{
    /// <summary>
    /// Patrón Command (PdN3 — Devolución / Cancelación). Equivalente a OrdenCommand del
    /// ejemplo de cátedra: clase abstracta (no interfaz) cuyo constructor recibe y guarda
    /// el Receiver y los datos de la operación, con un único método abstracto Ejecutar().
    /// El ejemplo de cátedra no contempla deshacer — esa capacidad no se replica acá.
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

        public abstract void Ejecutar();
    }
}
