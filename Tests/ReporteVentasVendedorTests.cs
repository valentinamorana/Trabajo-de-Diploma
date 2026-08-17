using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>BLL.ReporteVentasVendedor (PdN8) — desempeño de pedidos por vendedor.</summary>
    [TestClass]
    public class ReporteVentasVendedorTests
    {
        [TestMethod]
        public void Obtener_DelegaEnElDAL()
        {
            var fake = new FakePedidoDAL();
            fake.EstadisticasPorEmpleado.Add(new BE.DesempenoVendedor
            {
                IdEmpleado = 1,
                NombreEmpleado = "Valentina Morana",
                TotalPedidos = 10,
                Entregados = 8,
                Cancelados = 2
            });
            var bll = new BLL.ReporteVentasVendedor(fake);

            var resultado = bll.Obtener();

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("Valentina Morana", resultado[0].NombreEmpleado);
            Assert.AreEqual(20.0, resultado[0].TasaCancelacion);
        }
    }
}
