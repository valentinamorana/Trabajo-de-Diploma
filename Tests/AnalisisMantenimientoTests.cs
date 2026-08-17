using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>BLL.AnalisisMantenimiento (PdN11) — prendas con tiempos o cantidad de mantenimientos excesivos.</summary>
    [TestClass]
    public class AnalisisMantenimientoTests
    {
        private static BE.MantenimientoPrenda Cerrado(int idPrenda, string nombre, int diasDuracion) => new BE.MantenimientoPrenda
        {
            IdPrenda = idPrenda,
            NombrePrenda = nombre,
            FechaEntrada = DateTime.Today.AddDays(-diasDuracion),
            FechaSalida = DateTime.Today
        };

        [TestMethod]
        public void Detectar_CantidadExcesiva_Flaggeado()
        {
            var fake = new FakeMantenimientoPrendaDAL();
            fake.Todos.Add(Cerrado(1, "Campera", 1));
            fake.Todos.Add(Cerrado(1, "Campera", 1));
            fake.Todos.Add(Cerrado(1, "Campera", 1)); // 3 mantenimientos = umbral (CantidadMantenimientosExcesiva)
            var bll = new BLL.AnalisisMantenimiento(fake);

            var resultado = bll.Detectar();

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual(3, resultado[0].CantidadMantenimientos);
            Assert.AreEqual("mant.analisis.motivo.cantidad", resultado[0].Clave);
        }

        [TestMethod]
        public void Detectar_DuracionPromedioExcesiva_Flaggeado()
        {
            var fake = new FakeMantenimientoPrendaDAL();
            fake.Todos.Add(Cerrado(1, "Campera", 7)); // > 5 días promedio (DuracionPromedioExcesivaDias)
            var bll = new BLL.AnalisisMantenimiento(fake);

            var resultado = bll.Detectar();

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("mant.analisis.motivo.duracion", resultado[0].Clave);
        }

        [TestMethod]
        public void Detectar_AmbosExcesivos_MotivoCombinado()
        {
            var fake = new FakeMantenimientoPrendaDAL();
            fake.Todos.Add(Cerrado(1, "Campera", 7));
            fake.Todos.Add(Cerrado(1, "Campera", 7));
            fake.Todos.Add(Cerrado(1, "Campera", 7));
            var bll = new BLL.AnalisisMantenimiento(fake);

            var resultado = bll.Detectar();

            Assert.AreEqual("mant.analisis.motivo.ambos", resultado[0].Clave);
        }

        [TestMethod]
        public void Detectar_NiCantidadNiDuracionExcesiva_NoFlaggeado()
        {
            var fake = new FakeMantenimientoPrendaDAL();
            fake.Todos.Add(Cerrado(1, "Campera", 1));
            var bll = new BLL.AnalisisMantenimiento(fake);

            var resultado = bll.Detectar();

            Assert.AreEqual(0, resultado.Count);
        }

        [TestMethod]
        public void Detectar_MantenimientoAbierto_CuentaParaCantidadPeroNoParaPromedio()
        {
            var fake = new FakeMantenimientoPrendaDAL();
            fake.Todos.Add(new BE.MantenimientoPrenda { IdPrenda = 1, NombrePrenda = "Campera", FechaEntrada = DateTime.Today, FechaSalida = null });
            fake.Todos.Add(Cerrado(1, "Campera", 1));
            fake.Todos.Add(Cerrado(1, "Campera", 1)); // total 3 = cantidad excesiva, aunque 1 sigue abierto
            var bll = new BLL.AnalisisMantenimiento(fake);

            var resultado = bll.Detectar();

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual(3, resultado[0].CantidadMantenimientos);
            Assert.IsNotNull(resultado[0].DuracionPromedioDias, "El promedio se calcula solo sobre los 2 cerrados.");
        }
    }
}
