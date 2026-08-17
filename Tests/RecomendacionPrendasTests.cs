using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>BLL.RecomendacionPrendas (PdN13) — sugerencias basadas en el historial del cliente.</summary>
    [TestClass]
    public class RecomendacionPrendasTests
    {
        [TestMethod]
        public void Recomendar_SinHistorial_ListaVacia()
        {
            var dalPedido = new FakePedidoDAL(); // PrendasHistoricasPorCliente vacío
            var dalPrenda = new FakePrendaDAL();
            var bll = new BLL.RecomendacionPrendas(dalPedido, dalPrenda);

            var resultado = bll.Recomendar(10);

            Assert.AreEqual(0, resultado.Count);
        }

        [TestMethod]
        public void Recomendar_CategoriaYColorCoinciden_UnaSolaRecomendacionConAmbasCoincidencias()
        {
            var dalPedido = new FakePedidoDAL();
            dalPedido.PrendasHistoricasPorCliente.Add(new BE.Prenda { IdPrenda = 1, Categoria = "Remeras", Color = "Rojo" });
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Disponibles.Add(new BE.Prenda { IdPrenda = 2, Nombre = "Remera roja", Categoria = "Remeras", Color = "Rojo" });
            var bll = new BLL.RecomendacionPrendas(dalPedido, dalPrenda);

            var resultado = bll.Recomendar(10);

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual(2, resultado[0].Coincidencias);
            Assert.AreEqual("recom.motivo.ambos", resultado[0].Clave);
        }

        [TestMethod]
        public void Recomendar_ExcluyePrendasYaPedidasAntes()
        {
            var dalPedido = new FakePedidoDAL();
            dalPedido.PrendasHistoricasPorCliente.Add(new BE.Prenda { IdPrenda = 2, Categoria = "Remeras" });
            var dalPrenda = new FakePrendaDAL();
            // La misma prenda (ID 2) sigue figurando como "disponible" en el catálogo (podría
            // haber vuelto tras una devolución) — no debe re-sugerirse al mismo cliente.
            dalPrenda.Disponibles.Add(new BE.Prenda { IdPrenda = 2, Nombre = "Remera ya usada", Categoria = "Remeras" });
            var bll = new BLL.RecomendacionPrendas(dalPedido, dalPrenda);

            var resultado = bll.Recomendar(10);

            Assert.AreEqual(0, resultado.Count);
        }

        [TestMethod]
        public void Recomendar_SoloCategoriaCoincide_UnaCoincidencia()
        {
            var dalPedido = new FakePedidoDAL();
            dalPedido.PrendasHistoricasPorCliente.Add(new BE.Prenda { IdPrenda = 1, Categoria = "Remeras", Color = "Rojo" });
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Disponibles.Add(new BE.Prenda { IdPrenda = 2, Nombre = "Remera azul", Categoria = "Remeras", Color = "Azul" });
            var bll = new BLL.RecomendacionPrendas(dalPedido, dalPrenda);

            var resultado = bll.Recomendar(10);

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual(1, resultado[0].Coincidencias);
            Assert.AreEqual("recom.motivo.categoria", resultado[0].Clave);
        }

        [TestMethod]
        public void Recomendar_SinNingunaCoincidencia_NoSeSugiere()
        {
            var dalPedido = new FakePedidoDAL();
            dalPedido.PrendasHistoricasPorCliente.Add(new BE.Prenda { IdPrenda = 1, Categoria = "Remeras", Color = "Rojo" });
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Disponibles.Add(new BE.Prenda { IdPrenda = 2, Nombre = "Pantalón verde", Categoria = "Pantalones", Color = "Verde" });
            var bll = new BLL.RecomendacionPrendas(dalPedido, dalPrenda);

            var resultado = bll.Recomendar(10);

            Assert.AreEqual(0, resultado.Count);
        }

        [TestMethod]
        public void Recomendar_OrdenaPorCantidadDeCoincidencias()
        {
            var dalPedido = new FakePedidoDAL();
            dalPedido.PrendasHistoricasPorCliente.Add(new BE.Prenda { IdPrenda = 1, Categoria = "Remeras", Color = "Rojo" });
            var dalPrenda = new FakePrendaDAL();
            dalPrenda.Disponibles.Add(new BE.Prenda { IdPrenda = 2, Nombre = "Solo categoría", Categoria = "Remeras", Color = "Azul" });
            dalPrenda.Disponibles.Add(new BE.Prenda { IdPrenda = 3, Nombre = "Categoría y color", Categoria = "Remeras", Color = "Rojo" });
            var bll = new BLL.RecomendacionPrendas(dalPedido, dalPrenda);

            var resultado = bll.Recomendar(10);

            Assert.AreEqual(2, resultado.Count);
            Assert.AreEqual("Categoría y color", resultado[0].Prenda.Nombre, "2 coincidencias va primero.");
        }
    }
}
