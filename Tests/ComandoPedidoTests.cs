using Microsoft.VisualStudio.TestTools.UnitTesting;
using BLL.Comandos;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PdN3 — Pruebas del patrón Command para Devolución/Cancelación de pedidos.
    /// El Invoker es una cola batch (TomarOrden/ProcesarOrdenes), igual que
    /// EmpresaInvoker del ejemplo de cátedra — no ejecuta inmediato ni deshace.
    /// </summary>
    [TestClass]
    public class ComandoPedidoTests
    {
        private static BE.Pedido PedidoDePrueba() => new BE.Pedido { IdPedido = 1, IdCliente = 1 };

        [TestMethod]
        public void CancelacionCommand_Ejecutar_LlamaACancelar()
        {
            var receptor = new FakePedidoService();
            var comando = new CancelacionCommand(receptor, PedidoDePrueba(), "Test", "no le gustó");

            comando.Ejecutar();

            Assert.AreEqual(1, receptor.CancelarVeces);
            Assert.AreEqual("no le gustó", receptor.UltimoMotivoCancelacion);
        }

        [TestMethod]
        public void DevolucionCommand_Ejecutar_LlamaARegistrarDevolucion()
        {
            var receptor = new FakePedidoService();
            var comando = new DevolucionCommand(receptor, PedidoDePrueba(), "Test");

            comando.Ejecutar();

            Assert.AreEqual(1, receptor.RegistrarDevolucionVeces);
        }

        [TestMethod]
        public void InvocadorPedido_TomarOrden_NoEjecutaTodavia()
        {
            var receptor = new FakePedidoService();
            var invocador = new InvocadorPedido();

            invocador.TomarOrden(new CancelacionCommand(receptor, PedidoDePrueba(), "Test", "motivo"));

            Assert.AreEqual(0, receptor.CancelarVeces);
        }

        [TestMethod]
        public void InvocadorPedido_ProcesarOrdenes_EjecutaTodasEnElOrdenTomado()
        {
            var receptor = new FakePedidoService();
            var invocador = new InvocadorPedido();

            invocador.TomarOrden(new CancelacionCommand(receptor, PedidoDePrueba(), "Test", "motivo1"));
            invocador.TomarOrden(new DevolucionCommand(receptor, PedidoDePrueba(), "Test"));
            invocador.TomarOrden(new CancelacionCommand(receptor, PedidoDePrueba(), "Test", "motivo2"));

            invocador.ProcesarOrdenes();

            Assert.AreEqual(2, receptor.CancelarVeces);
            Assert.AreEqual(1, receptor.RegistrarDevolucionVeces);
            Assert.AreEqual("motivo2", receptor.UltimoMotivoCancelacion);
        }

        [TestMethod]
        public void InvocadorPedido_ProcesarOrdenes_VaciaLaColaDespues()
        {
            var receptor = new FakePedidoService();
            var invocador = new InvocadorPedido();
            invocador.TomarOrden(new DevolucionCommand(receptor, PedidoDePrueba(), "Test"));

            invocador.ProcesarOrdenes();
            invocador.ProcesarOrdenes(); // segunda vez: la cola ya está vacía

            Assert.AreEqual(1, receptor.RegistrarDevolucionVeces);
        }
    }
}
