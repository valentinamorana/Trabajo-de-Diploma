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
            public System.DateTime? UltimoDesde { get; private set; }
            public List<BE.RotacionPrenda> Detectar(System.DateTime? desde = null) { UltimoDesde = desde; return Resultado; }
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
            Assert.AreEqual(BE.OrigenMetrica.Rotacion, r[0].Origen);
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
            Assert.AreEqual(BE.OrigenMetrica.Abandono, r[0].Origen);
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

            Assert.AreEqual(BE.OrigenMetrica.Abandono, r[0].Origen);   // 8000 > 2000
            Assert.AreEqual(BE.OrigenMetrica.Rotacion, r[1].Origen);
        }

        // ── Analizar métricas («Reporte de métricas») → ¿Hay oportunidad? ────

        private static void Login() => Seguridad.SessionManager.Login(new BE.Usuario
        {
            Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Seguridad.Encriptador.Hash("Admin1!")
        });

        [TestCleanup] public void Cleanup() => Seguridad.SessionManager.Logout();

        [TestMethod]
        public void AnalizarMetricas_SinCasos_NoHayOportunidad_FinSinPromocion()
        {
            Login();
            var bll = Crear(new RotacionFake(), new AbandonoFake());

            var reporte = bll.AnalizarMetricas("Test");

            Assert.IsFalse(bll.HayOportunidad(reporte));
            Assert.AreEqual(0, reporte.Oportunidades.Count);
        }

        [TestMethod]
        public void AnalizarMetricas_ConCasos_ArmaElReporteDeAbandonoPorPlanYRotacionPorCategoria()
        {
            Login();
            var rot = new RotacionFake();
            rot.Resultado.Add(BajaDemanda("A", "Abrigo"));
            rot.Resultado.Add(BajaDemanda("B", "Abrigo"));
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "C", Categoria = "Vestido", Clave = "rotacion.motivo.altademanda" });
            var ab = new AbandonoFake();
            ab.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 1, NombrePlan = "Básico" });
            var plan = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Precio = 8000m, Estado = true };
            var bll = Crear(rot, ab, plan);

            var reporte = bll.AnalizarMetricas("Test");

            Assert.IsTrue(bll.HayOportunidad(reporte));
            Assert.AreEqual(1, reporte.AbandonoPorPlan.Count);
            Assert.AreEqual(1, reporte.AbandonoPorPlan[0].ClientesEnRiesgo);
            Assert.AreEqual(8000m, reporte.AbandonoPorPlan[0].IngresoMensualEnRiesgo);
            var abrigo = reporte.RotacionPorCategoria.Find(m => m.Categoria == "Abrigo");
            Assert.AreEqual(2, abrigo.PrendasBajaDemanda);
            Assert.AreEqual(1, reporte.RotacionPorCategoria.Find(m => m.Categoria == "Vestido").PrendasAltaDemanda);
            Assert.AreEqual(2, reporte.Oportunidades.Count);
        }

        [TestMethod]
        public void AnalizarMetricas_SinSesion_LanzaSesionExpirada()
        {
            Seguridad.SessionManager.Logout();
            try
            {
                Crear(new RotacionFake(), new AbandonoFake()).AnalizarMetricas("Test");
                Assert.Fail("Debía exigir sesión.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sesion_expirada", ex.Clave); }
        }

        [TestMethod]
        public void AnalizarMetricas_SinPeriodo_AnalizaLosUltimos90DiasEIncluyeElImpactoDeLasPromociones()
        {
            Login();
            var rot = new RotacionFake();
            var promos = new FakePromocionDAL();
            promos.Impacto.Add(new BE.MetricaImpactoPromocion
            {
                IdPromocion = 3, Nombre = "Verano", Estado = BE.EstadoPromocion.Vigente, Cobros = 4, TotalDescontado = 2000m, TotalCobrado = 18000m
            });
            var bll = new BLL.AnalisisPromociones(rot, new AbandonoFake(), new FakePlanSuscripcionDAL(), promos);

            var reporte = bll.AnalizarMetricas("Test");

            var desde = System.DateTime.Today.AddDays(-BLL.AnalisisPromociones.DiasPeriodoPorDefecto);
            Assert.AreEqual(desde, reporte.Desde);
            Assert.AreEqual(System.DateTime.Today, reporte.Hasta);
            Assert.AreEqual(desde, rot.UltimoDesde, "La rotación cuenta solo los pedidos del período.");
            Assert.AreEqual(desde, promos.UltimoDesdeImpacto);
            Assert.AreEqual(1, reporte.ImpactoPromociones.Count);
            Assert.AreEqual(2000m, reporte.ImpactoPromociones[0].TotalDescontado);
        }

        [TestMethod]
        public void AnalizarMetricas_PeriodoInvertido_LanzaRangoInvalido()
        {
            Login();
            try
            {
                Crear(new RotacionFake(), new AbandonoFake()).AnalizarMetricas("Test", System.DateTime.Today, System.DateTime.Today.AddDays(-1));
                Assert.Fail("Debía rechazar un período invertido.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.promocion.rango_fechas_invalido", ex.Clave); }
        }

        [TestMethod]
        public void Detectar_PrendaSinPedidosEnElPeriodo_CuentaComoBajaDemanda()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "A", Categoria = "Saco", Clave = "rotacion.motivo.bajademanda.periodo" });
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "B", Categoria = "Saco", Clave = "rotacion.motivo.bajademanda.periodo" });
            Assert.AreEqual(1, Crear(rot, new AbandonoFake()).Detectar().Count);
        }
    }
}
