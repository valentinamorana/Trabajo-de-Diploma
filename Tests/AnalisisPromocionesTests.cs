using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PN03 — BLL.AnalisisPromociones: cruza los reportes de rotación y abandono con la sugerencia
    /// de promociones (métricas → decisión).
    /// </summary>
    [TestClass]
    public class AnalisisPromocionesTests
    {
        private class RotacionFake : BLL.Interfaces.IAnalisisRotacionService
        {
            public List<BE.RotacionPrenda> Resultado { get; set; } = new List<BE.RotacionPrenda>();
            public List<BE.RotacionPrenda> Detectar() => Resultado;
        }

        private class AbandonoFake : BLL.Interfaces.IAnalisisAbandonoService
        {
            public List<BE.ClienteEnRiesgo> Resultado { get; set; } = new List<BE.ClienteEnRiesgo>();
            public void CambiarEstrategia(BLL.Estrategias.EstrategiaRiesgo estrategia) { }
            public List<BE.ClienteEnRiesgo> Detectar() => Resultado;
        }

        private static BE.RotacionPrenda BajaDemanda(string nombre, string categoria) => new BE.RotacionPrenda
        {
            NombrePrenda = nombre, Categoria = categoria, CantidadPedidos = 0, Clave = "rotacion.motivo.bajademanda"
        };

        private static BLL.AnalisisPromociones Crear(RotacionFake rot, AbandonoFake ab, params BE.PlanSuscripcion[] planes)
            => new BLL.AnalisisPromociones(rot, ab, new FakePlanSuscripcionDAL { Planes = new List<BE.PlanSuscripcion>(planes) });

        [TestMethod]
        public void Detectar_SinDatos_NoProponeNada()
            => Assert.AreEqual(0, Crear(new RotacionFake(), new AbandonoFake()).Detectar().Count);

        [TestMethod]
        public void Detectar_VariasPrendasSinPedidosEnUnaCategoria_ProponePromocionPorCategoria()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(BajaDemanda("Abrigo Camel", "Abrigo"));
            rot.Resultado.Add(BajaDemanda("Tapado Rojo", "abrigo"));
            rot.Resultado.Add(BajaDemanda("Camisa Blanca", "Camisa"));   // una sola: no alcanza el mínimo

            var r = Crear(rot, new AbandonoFake()).Detectar();

            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("Rotación", r[0].Origen);
            Assert.AreEqual("Abrigo", r[0].CategoriaPrenda);
            Assert.IsNull(r[0].IdPlan);
            Assert.AreEqual(BE.TipoDescuento.MontoFijo, r[0].TipoSugerido);
            Assert.AreEqual(2 * BLL.AnalisisPromociones.ValorReferenciaPorPrenda, r[0].BeneficioEstimado);
            StringAssert.Contains(r[0].Motivo, "2 prenda(s)");
        }

        [TestMethod]
        public void Detectar_IgnoraLasPrendasDeAltaDemanda()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "A", Categoria = "Vestido", Clave = "rotacion.motivo.altademanda" });
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "B", Categoria = "Vestido", Clave = "rotacion.motivo.altademanda" });

            Assert.AreEqual(0, Crear(rot, new AbandonoFake()).Detectar().Count);
        }

        [TestMethod]
        public void Detectar_ClientesEnRiesgoDeUnPlan_ProponePromocionDeRetencionConElIngresoEnRiesgo()
        {
            var ab = new AbandonoFake();
            ab.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 1, NombrePlan = "Básico" });
            ab.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 2, NombrePlan = "Básico" });
            var plan = new BE.PlanSuscripcion { IdPlan = 7, Nombre = "Básico", Precio = 8000m, Estado = true };

            var r = Crear(new RotacionFake(), ab, plan).Detectar();

            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("Abandono", r[0].Origen);
            Assert.AreEqual(7, r[0].IdPlan);
            Assert.AreEqual(BE.TipoDescuento.Porcentaje, r[0].TipoSugerido);
            Assert.AreEqual(16000m, r[0].BeneficioEstimado, "2 clientes × $8000 de ingreso mensual en riesgo.");
        }

        [TestMethod]
        public void Detectar_PlanInactivoODesconocido_NoProponePromocion()
        {
            var ab = new AbandonoFake();
            ab.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 1, NombrePlan = "Premium" });
            var inactivo = new BE.PlanSuscripcion { IdPlan = 9, Nombre = "Premium", Precio = 1000m, Estado = false };

            Assert.AreEqual(0, Crear(new RotacionFake(), ab, inactivo).Detectar().Count);
        }

        [TestMethod]
        public void Detectar_OrdenaPorBeneficioEstimadoDescendente()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(BajaDemanda("A", "Abrigo"));
            rot.Resultado.Add(BajaDemanda("B", "Abrigo"));
            var ab = new AbandonoFake();
            ab.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 1, NombrePlan = "Básico" });
            var plan = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Precio = 8000m, Estado = true };

            var r = Crear(rot, ab, plan).Detectar();

            Assert.AreEqual("Abandono", r[0].Origen);   // 8000 > 2000
            Assert.AreEqual("Rotación", r[1].Origen);
        }
    }
}
