using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>BLL.AnalisisRotacion (PdN9) — prendas de baja o alta demanda.</summary>
    [TestClass]
    public class AnalisisRotacionTests
    {
        private static BE.Prenda Prenda(int id, string nombre, int diasEnCatalogo, BE.EstadoPrenda estado = BE.EstadoPrenda.Disponible) => new BE.Prenda
        {
            IdPrenda = id,
            Nombre = nombre,
            Categoria = "Remeras",
            Estado = estado,
            FechaAlta = DateTime.Today.AddDays(-diasEnCatalogo)
        };

        [TestMethod]
        public void Detectar_SinPedidosYAntigua_CandidataABaja()
        {
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Todas.Add(Prenda(1, "Remera vieja", 40)); // > 30 días (DiasAntiguedadMinimaParaBajaDemanda)
            var dalPedido = new FakePedidoDAL(); // sin pedidos registrados
            var bll = new BLL.AnalisisRotacion(dalPrenda, dalPedido);

            var resultado = bll.Detectar();

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("rotacion.motivo.bajademanda", resultado[0].Clave);
        }

        [TestMethod]
        public void Detectar_SinPedidosPeroReciente_NoFlaggeada()
        {
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Todas.Add(Prenda(1, "Remera nueva", 5)); // recién ingresada
            var dalPedido = new FakePedidoDAL();
            var bll = new BLL.AnalisisRotacion(dalPrenda, dalPedido);

            var resultado = bll.Detectar();

            Assert.AreEqual(0, resultado.Count, "Muy nueva como para considerar la falta de pedidos una señal real.");
        }

        [TestMethod]
        public void Detectar_MuchosPedidos_CandidataAReposicion()
        {
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Todas.Add(Prenda(1, "Remera popular", 60));
            var dalPedido = new FakePedidoDAL();
            dalPedido.CantidadPedidosPorPrenda[1] = 5; // = umbral (CantidadPedidosParaAltaDemanda)
            var bll = new BLL.AnalisisRotacion(dalPrenda, dalPedido);

            var resultado = bll.Detectar();

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("rotacion.motivo.altademanda", resultado[0].Clave);
        }

        [TestMethod]
        public void Detectar_PrendaDeBaja_Excluida()
        {
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Todas.Add(Prenda(1, "Remera dada de baja", 60, BE.EstadoPrenda.Baja));
            var dalPedido = new FakePedidoDAL();
            var bll = new BLL.AnalisisRotacion(dalPrenda, dalPedido);

            var resultado = bll.Detectar();

            Assert.AreEqual(0, resultado.Count, "Una prenda ya dada de baja no debe re-sugerirse.");
        }

        [TestMethod]
        public void Detectar_RotacionNormal_NoFlaggeada()
        {
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Todas.Add(Prenda(1, "Remera normal", 60));
            var dalPedido = new FakePedidoDAL();
            dalPedido.CantidadPedidosPorPrenda[1] = 2; // ni 0 ni >= 5
            var bll = new BLL.AnalisisRotacion(dalPrenda, dalPedido);

            var resultado = bll.Detectar();

            Assert.AreEqual(0, resultado.Count);
        }
    }
}
