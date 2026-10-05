using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Tareas de los paneles de inicio (PN01/PN02): qué pedidos son tarea del Vendedor y cuáles
    /// esperan a Depósito. La clasificación vive en BLL.PanelTareas; los paneles solo la muestran.
    /// </summary>
    [TestClass]
    public class PanelTareasTests
    {
        private static BE.Pedido P(int id, BE.EstadoPedido e) =>
            new BE.Pedido { IdPedido = id, IdCliente = 1, Estado = e, FechaPedido = System.DateTime.Now.AddDays(-id) };

        private static FakePedidoService Servicio(params BE.Pedido[] pedidos)
        {
            var s = new FakePedidoService();
            s.Pedidos.AddRange(pedidos);
            return s;
        }

        [TestMethod]
        public void TareasVendedor_ClasificaPorEstadoDelArmado()
        {
            var bll = new BLL.PanelTareas(Servicio(
                P(1, BE.EstadoPedido.EnControlStock), P(2, BE.EstadoPedido.ConFaltantes),
                P(3, BE.EstadoPedido.Separado), P(4, BE.EstadoPedido.Separado),
                P(5, BE.EstadoPedido.Pendiente), P(6, BE.EstadoPedido.Desistido)), () => 2);

            var t = bll.ObtenerTareasVendedor();

            Assert.AreEqual(1, t.EnControlStock.Count);
            Assert.AreEqual(1, t.ConFaltantes.Count);
            Assert.AreEqual(2, t.Separados.Count);
            Assert.AreEqual(3, t.PedidosParaAtender, "Comunicar faltantes + formalizar");
            Assert.AreEqual(2, t.ContratacionesEnCaja);
        }

        [TestMethod]
        public void TareasVendedor_SiFallaElConteoDeContrataciones_SigueConLosPedidos()
        {
            var bll = new BLL.PanelTareas(Servicio(P(1, BE.EstadoPedido.ConFaltantes)),
                                          () => throw new System.Exception("sin base"));
            var t = bll.ObtenerTareasVendedor();
            Assert.AreEqual(1, t.PedidosParaAtender);
            Assert.AreEqual(0, t.ContratacionesEnCaja);
        }

        [TestMethod]
        public void TareasDeposito_CuentaLaColaDeControlDeStock()
        {
            var s = Servicio();
            s.ColaControlStock = new List<BE.Pedido> { P(1, BE.EstadoPedido.EnControlStock), P(2, BE.EstadoPedido.EnControlStock) };
            Assert.AreEqual(2, new BLL.PanelTareas(s, () => 0).ContarPedidosAControlar());
        }
    }
}
