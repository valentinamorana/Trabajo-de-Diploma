using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>BLL.AnalisisEscasez (PdN12) — detección de escasez de stock por Talle+Categoría.</summary>
    [TestClass]
    public class AnalisisEscasezTests
    {
        [TestMethod]
        public void Detectar_UmbralNegativo_LanzaArgumentOutOfRange()
        {
            var bll = new BLL.AnalisisEscasez(new FakePrendaDAL());

            try
            {
                bll.Detectar(-1);
                Assert.Fail("Debía rechazar un umbral negativo.");
            }
            catch (ArgumentOutOfRangeException) { /* esperado */ }
        }

        [TestMethod]
        public void Detectar_StockPorDebajoDelUmbral_Flaggeado()
        {
            var fake = new FakePrendaDAL();
            fake.ConteoDisponiblesPorTalleCategoria.Add(new BE.StockPorTalleCategoria { Talle = "M", Categoria = "Remeras", CantidadDisponible = 2 });
            var bll = new BLL.AnalisisEscasez(fake);

            var resultado = bll.Detectar(3);

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("M", resultado[0].Talle);
            Assert.AreEqual(3, resultado[0].Umbral);
        }

        [TestMethod]
        public void Detectar_StockIgualAlUmbral_NoFlaggeado()
        {
            var fake = new FakePrendaDAL();
            fake.ConteoDisponiblesPorTalleCategoria.Add(new BE.StockPorTalleCategoria { Talle = "M", Categoria = "Remeras", CantidadDisponible = 3 });
            var bll = new BLL.AnalisisEscasez(fake);

            var resultado = bll.Detectar(3);

            Assert.AreEqual(0, resultado.Count, "El umbral es el mínimo aceptable — igual al umbral no es escasez.");
        }

        [TestMethod]
        public void Detectar_SinCombinacionesPorDebajo_ListaVacia()
        {
            var fake = new FakePrendaDAL();
            fake.ConteoDisponiblesPorTalleCategoria.Add(new BE.StockPorTalleCategoria { Talle = "L", Categoria = "Pantalones", CantidadDisponible = 10 });
            var bll = new BLL.AnalisisEscasez(fake);

            var resultado = bll.Detectar(3);

            Assert.AreEqual(0, resultado.Count);
        }

        [TestMethod]
        public void Detectar_OrdenaPorCantidadAscendente()
        {
            var fake = new FakePrendaDAL();
            fake.ConteoDisponiblesPorTalleCategoria.Add(new BE.StockPorTalleCategoria { Talle = "S", Categoria = "Remeras", CantidadDisponible = 2 });
            fake.ConteoDisponiblesPorTalleCategoria.Add(new BE.StockPorTalleCategoria { Talle = "M", Categoria = "Remeras", CantidadDisponible = 0 });
            var bll = new BLL.AnalisisEscasez(fake);

            var resultado = bll.Detectar(5);

            Assert.AreEqual(2, resultado.Count);
            Assert.AreEqual("M", resultado[0].Talle, "El más escaso (0) va primero.");
        }
    }
}
