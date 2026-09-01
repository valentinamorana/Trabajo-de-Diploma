using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Prenda — cobertura puntual de VerificarDisponibilidad (PN01, split lógico del
    /// Depósito: antes era una relectura inline dentro de BLL.Pedido.ValidarDisponibilidadPrendas,
    /// ahora es un método público propio de BLL.Prenda). Se agregó DI de constructor (mismo
    /// criterio que BLL.Pedido/BLL.Cliente) específicamente para poder escribir estos tests.
    /// </summary>
    [TestClass]
    public class PrendaTests
    {
        private class Contexto
        {
            public FakePrendaDAL DalPrenda = new FakePrendaDAL();
            public FakeMantenimientoPrendaDAL DalMantenimiento = new FakeMantenimientoPrendaDAL();

            public BLL.Prenda Crear() => new BLL.Prenda(DalPrenda, DalMantenimiento);
        }

        [TestMethod]
        public void VerificarDisponibilidad_TodasDisponibles_DevuelveDisponibleTrue()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.Disponible });
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1 } });

            Assert.IsTrue(disponible);
            Assert.AreEqual(0, noDisponibles.Count);
        }

        [TestMethod]
        public void VerificarDisponibilidad_PrendaEnUso_DevuelveDisponibleFalseConDetalle()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.EnUso });
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1 } });

            Assert.IsFalse(disponible);
            Assert.AreEqual(1, noDisponibles.Count);
            Assert.AreEqual("Remera", noDisponibles[0].Nombre);
        }

        [TestMethod]
        public void VerificarDisponibilidad_PrendaInexistente_DevuelveDisponibleFalse()
        {
            var ctx = new Contexto(); // Todas queda vacío: ObtenerPorId no encuentra nada
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 99 } });

            Assert.IsFalse(disponible);
            Assert.AreEqual(1, noDisponibles.Count);
        }

        [TestMethod]
        public void VerificarDisponibilidad_SeleccionMixta_DevuelveSoloLasNoDisponibles()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.Disponible });
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 2, Nombre = "Pantalón", Estado = BE.EstadoPrenda.EnUso });
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1 }, new BE.Prenda { IdPrenda = 2 } });

            Assert.IsFalse(disponible);
            Assert.AreEqual(1, noDisponibles.Count);
            Assert.AreEqual(2, noDisponibles[0].IdPrenda);
        }
    }
}
