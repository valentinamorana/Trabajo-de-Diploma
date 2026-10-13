using System.Collections.Generic;
using System.Linq;
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
        public void Detectar_VariasPrendasSinPedidosEnUnaCategoria_NoEsBaseDeUnaSugerencia()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(BajaDemanda("Abrigo Camel", "Abrigo"));
            rot.Resultado.Add(BajaDemanda("Tapado Rojo", "abrigo"));

            // Las promociones son siempre por plan: una categoría no genera una oportunidad para sugerir.
            Assert.AreEqual(0, Crear(rot, new AbandonoFake()).Detectar().Count);
        }

        [TestMethod]
        public void RotacionPorCategoria_VariasPrendasSinPedidos_QuedaEnElReporteComoOportunidadInformativa()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(BajaDemanda("Abrigo Camel", "Abrigo"));
            rot.Resultado.Add(BajaDemanda("Tapado Rojo", "abrigo"));
            rot.Resultado.Add(BajaDemanda("Camisa Blanca", "Camisa"));   // una sola: no alcanza el mínimo

            var r = Crear(rot, new AbandonoFake()).RotacionPorCategoria();

            var abrigo = r.Find(m => m.Categoria == "Abrigo");
            Assert.IsTrue(abrigo.EsOportunidad);
            Assert.AreEqual(2, abrigo.PrendasBajaDemanda);
            StringAssert.Contains(abrigo.EjemplosBajaDemanda, "Abrigo Camel");
            Assert.IsFalse(r.Find(m => m.Categoria == "Camisa").EsOportunidad);
        }

        [TestMethod]
        public void Detectar_IgnoraLasPrendasDeAltaDemanda()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "A", Categoria = "Vestido", Clave = "rotacion.motivo.altademanda" });
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "B", Categoria = "Vestido", Clave = "rotacion.motivo.altademanda" });

            Assert.AreEqual(0, Crear(rot, new AbandonoFake()).Detectar().Count);
            Assert.IsFalse(Crear(rot, new AbandonoFake()).RotacionPorCategoria()[0].EsOportunidad);
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
            Assert.AreEqual(2, r[0].ClientesEnRiesgo);
            Assert.AreEqual(16000m, r[0].IngresoMensualEnRiesgo, "2 clientes × $8000 de ingreso mensual en riesgo.");
            Assert.AreEqual(30m, r[0].TasaRetencion, "Tasa de retención esperada por defecto.");
            Assert.AreEqual(4800m, r[0].BeneficioEstimado, "El beneficio ya no supone retener al 100 %: $16000 × 30 %.");
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
            var ab = new AbandonoFake();
            ab.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 1, NombrePlan = "Básico" });
            ab.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 2, NombrePlan = "Premium" });
            var basico = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Precio = 8000m, Estado = true };
            var premium = new BE.PlanSuscripcion { IdPlan = 3, Nombre = "Premium", Precio = 25000m, Estado = true };

            var r = Crear(new RotacionFake(), ab, basico, premium).Detectar();

            Assert.AreEqual(3, r[0].IdPlan);   // 25000 > 8000
            Assert.AreEqual(1, r[1].IdPlan);
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
            Assert.IsTrue(abrigo.EsOportunidad, "La categoría queda en el reporte como oportunidad informativa.");
            Assert.AreEqual(1, reporte.RotacionPorCategoria.Find(m => m.Categoria == "Vestido").PrendasAltaDemanda);
            Assert.AreEqual(1, reporte.Oportunidades.Count, "Solo la oportunidad por plan sirve de base para una sugerencia.");
            Assert.AreEqual(1, reporte.Oportunidades[0].IdPlan);
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
        public void RotacionPorCategoria_PrendaSinPedidosEnElPeriodo_CuentaComoBajaDemanda()
        {
            var rot = new RotacionFake();
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "A", Categoria = "Saco", Clave = "rotacion.motivo.bajademanda.periodo" });
            rot.Resultado.Add(new BE.RotacionPrenda { NombrePrenda = "B", Categoria = "Saco", Clave = "rotacion.motivo.bajademanda.periodo" });
            var saco = Crear(rot, new AbandonoFake()).RotacionPorCategoria()[0];
            Assert.AreEqual(2, saco.PrendasBajaDemanda);
            Assert.IsTrue(saco.EsOportunidad);
        }

        // ── PN03: Evaluar resultado de la promoción ──────────────────────────

        private static readonly System.DateTime Hoy = new System.DateTime(2026, 10, 13);

        private static BE.DecisionRenovacion Dec(int cliente, int diasDesdeHoy, BE.EstadoRenovacion r) =>
            new BE.DecisionRenovacion { IdCliente = cliente, Fecha = Hoy.AddDays(diasDesdeHoy), Resultado = r };

        private static BLL.AnalisisPromociones CrearConPromos(FakePromocionDAL dal, FakeSugerenciaPromocionDAL sug = null) =>
            new BLL.AnalisisPromociones(new RotacionFake(), new AbandonoFake(), new FakePlanSuscripcionDAL(),
                                        dal, sug ?? new FakeSugerenciaPromocionDAL(), () => Hoy);

        [TestMethod]
        public void ResultadoPromociones_ComparaLaVigenciaContraElPeriodoAnteriorYMideLaRetencion()
        {
            var dal = new FakePromocionDAL();
            // Vigente desde hace 10 días (aprobada ese día), plan 3, nacida de una sugerencia con 30 % esperado.
            dal.Todas.Add(new BE.Promocion
            {
                IdPromocion = 1, Nombre = "Retención Básico", Estado = BE.EstadoPromocion.Vigente, IdPlan = 3, NombrePlan = "Básico",
                FechaInicio = Hoy.AddDays(-10), FechaFin = Hoy.AddDays(20), IdSugerenciaOrigen = 9
            });
            dal.Historial.Add(new BE.PromocionHistorial { IdPromocion = 1, EstadoNuevo = BE.EstadoPromocion.Vigente, Fecha = Hoy.AddDays(-10) });
            dal.Decisiones.AddRange(new[]
            {
                // Durante [hoy-10, hoy]: el cliente 1 renovó, el 2 pausó, el 3 se dio de baja.
                (3, Dec(1, -5, BE.EstadoRenovacion.Renovada)), (3, Dec(2, -3, BE.EstadoRenovacion.Pausada)), (3, Dec(3, -1, BE.EstadoRenovacion.Baja)),
                // Antes [hoy-21, hoy-10): tres bajas y una renovación.
                (3, Dec(4, -15, BE.EstadoRenovacion.Baja)), (3, Dec(5, -14, BE.EstadoRenovacion.Baja)), (3, Dec(6, -12, BE.EstadoRenovacion.Baja)),
                (3, Dec(7, -11, BE.EstadoRenovacion.Renovada)),
                // Otro plan: no cuenta.
                (8, Dec(8, -2, BE.EstadoRenovacion.Baja))
            });
            var sug = new FakeSugerenciaPromocionDAL { SugerenciaPorId = new BE.SugerenciaPromocion { IdSugerencia = 9, TasaRetencion = 30m } };

            var r = CrearConPromos(dal, sug).ResultadoPromociones(Hoy.AddDays(-90), Hoy);

            Assert.AreEqual(1, r.Count);
            var res = r[0];
            Assert.AreEqual(Hoy.AddDays(-10), res.Desde);
            Assert.AreEqual(Hoy.AddDays(1), res.Hasta, "Sigue vigente: se evalúa hasta hoy inclusive.");
            Assert.AreEqual(11, res.Dias);
            Assert.AreEqual(1, res.Durante.Bajas);
            Assert.AreEqual(3, res.Antes.Bajas);
            Assert.AreEqual(-2, res.VariacionBajas, "Bajaron las bajas del plan durante la promoción.");
            Assert.AreEqual(1, res.Durante.Pausas);
            Assert.AreEqual(1, res.Durante.Renovaciones);
            Assert.AreEqual(3, res.ClientesEnJuego);
            Assert.AreEqual(2, res.ClientesRetenidos, "Renovar o pausar retiene; la baja no.");
            Assert.AreEqual(66.67m, res.TasaRetencionReal);
            Assert.AreEqual(4, res.ClientesEnJuegoAntes);
            Assert.AreEqual(25m, res.TasaRetencionAntes, "Antes: 1 de 4 se quedó.");
            Assert.AreEqual(41.67m, res.MejoraRetencion);
            Assert.AreEqual(30m, res.TasaRetencionEsperada);
            Assert.IsTrue(res.RetuvoClientes());
            Assert.AreEqual(true, res.CumplioRetencionEsperada(), "La mejora (41,67 pts) supera el 30 % esperado.");
        }

        [TestMethod]
        public void ResultadoPromociones_DadaDeBaja_TerminaEnLaFechaDeBaja_YSoloEvaluaVigenteBajaOVencida()
        {
            var dal = new FakePromocionDAL();
            dal.Todas.Add(new BE.Promocion { IdPromocion = 1, Estado = BE.EstadoPromocion.DadaDeBaja, IdPlan = 3,
                                             FechaInicio = Hoy.AddDays(-30), FechaFin = Hoy.AddDays(30) });
            dal.Historial.Add(new BE.PromocionHistorial { IdPromocion = 1, EstadoNuevo = BE.EstadoPromocion.DadaDeBaja, Fecha = Hoy.AddDays(-20) });
            dal.Todas.Add(new BE.Promocion { IdPromocion = 2, Estado = BE.EstadoPromocion.EnRevisionContable, IdPlan = 3,
                                             FechaInicio = Hoy.AddDays(-30), FechaFin = Hoy.AddDays(30) });
            dal.Todas.Add(new BE.Promocion { IdPromocion = 3, Estado = BE.EstadoPromocion.RechazadaContabilidad, IdPlan = 3,
                                             FechaInicio = Hoy.AddDays(-30), FechaFin = Hoy.AddDays(30) });
            dal.Decisiones.Add((3, Dec(1, -25, BE.EstadoRenovacion.Baja)));
            dal.Decisiones.Add((3, Dec(2, -5, BE.EstadoRenovacion.Baja)));   // después de la baja: no cuenta

            var r = CrearConPromos(dal).ResultadoPromociones(Hoy.AddDays(-90), Hoy);

            Assert.AreEqual(1, r.Count, "En revisión y rechazadas no se evalúan: nunca se aplicaron.");
            Assert.AreEqual(Hoy.AddDays(-19), r[0].Hasta, "La vigencia termina el día de la baja.");
            Assert.AreEqual(1, r[0].Durante.Bajas);
            Assert.IsNull(r[0].TasaRetencionEsperada, "Alta manual: sin tasa esperada.");
            Assert.IsNull(r[0].CumplioRetencionEsperada());
            Assert.AreEqual(0, r[0].ClientesRetenidos);
        }

        [TestMethod]
        public void ResultadoPromociones_FueraDelPeriodo_NoSeEvalua_YLaUltimaDecisionDelClienteEsLaQueCuenta()
        {
            var dal = new FakePromocionDAL();
            dal.Todas.Add(new BE.Promocion { IdPromocion = 1, Estado = BE.EstadoPromocion.Vencida, IdPlan = 3,
                                             FechaInicio = Hoy.AddDays(-200), FechaFin = Hoy.AddDays(-150) });
            dal.Todas.Add(new BE.Promocion { IdPromocion = 2, Estado = BE.EstadoPromocion.Vencida, IdPlan = 4,
                                             FechaInicio = Hoy.AddDays(-40), FechaFin = Hoy.AddDays(-20) });
            // El cliente 1 pausó y después se dio de baja: no se retuvo.
            dal.Decisiones.Add((4, Dec(1, -35, BE.EstadoRenovacion.Pausada)));
            dal.Decisiones.Add((4, Dec(1, -25, BE.EstadoRenovacion.Baja)));

            var r = CrearConPromos(dal).ResultadoPromociones(Hoy.AddDays(-90), Hoy);

            Assert.AreEqual(1, r.Count, "La vencida hace 150 días no toca el período analizado.");
            Assert.AreEqual(2, r[0].IdPromocion);
            Assert.AreEqual(Hoy.AddDays(-19), r[0].Hasta, "Vencida: hasta su fecha de fin inclusive.");
            Assert.AreEqual(1, r[0].ClientesEnJuego);
            Assert.AreEqual(0, r[0].ClientesRetenidos);
            Assert.AreEqual(0m, r[0].TasaRetencionReal);
        }

        // Revisión 13/10: un plan que siempre renueva mucho no debe parecer un éxito de la promoción.
        [TestMethod]
        public void ResultadoPromociones_RetencionAltaDeSiempre_NoCumpleLaEsperada()
        {
            var dal = new FakePromocionDAL();
            dal.Todas.Add(new BE.Promocion { IdPromocion = 1, Estado = BE.EstadoPromocion.Vencida, IdPlan = 3, IdSugerenciaOrigen = 9,
                                             FechaInicio = Hoy.AddDays(-20), FechaFin = Hoy.AddDays(-11) });
            // Antes y durante: 9 de 10 renuevan (90 %).
            for (int i = 0; i < 10; i++)
            {
                var r = i == 0 ? BE.EstadoRenovacion.Baja : BE.EstadoRenovacion.Renovada;
                dal.Decisiones.Add((3, Dec(100 + i, -25, r)));   // antes [hoy-30, hoy-20)
                dal.Decisiones.Add((3, Dec(200 + i, -15, r)));   // durante [hoy-20, hoy-10)
            }
            var sug = new FakeSugerenciaPromocionDAL { SugerenciaPorId = new BE.SugerenciaPromocion { IdSugerencia = 9, TasaRetencion = 30m } };

            var res = CrearConPromos(dal, sug).ResultadoPromociones(Hoy.AddDays(-90), Hoy)[0];

            Assert.AreEqual(90m, res.TasaRetencionReal);
            Assert.AreEqual(90m, res.TasaRetencionAntes);
            Assert.AreEqual(0m, res.MejoraRetencion);
            Assert.AreEqual(false, res.CumplioRetencionEsperada(), "Sin mejora sobre la base, la promoción no retuvo a nadie más.");
        }

        // Pasarse a otro plan es una pérdida para el plan de la promoción.
        [TestMethod]
        public void ResultadoPromociones_CambioAOtroPlan_CuentaComoPerdida()
        {
            var dal = new FakePromocionDAL();
            dal.Todas.Add(new BE.Promocion { IdPromocion = 1, Estado = BE.EstadoPromocion.Vigente, IdPlan = 3,
                                             FechaInicio = Hoy.AddDays(-10), FechaFin = Hoy.AddDays(10) });
            dal.Decisiones.Add((3, Dec(1, -5, BE.EstadoRenovacion.CambioPlan)));
            dal.Decisiones.Add((3, Dec(2, -4, BE.EstadoRenovacion.Renovada)));

            var res = CrearConPromos(dal).ResultadoPromociones(Hoy.AddDays(-90), Hoy)[0];

            Assert.AreEqual(1, res.Durante.CambiosDePlan);
            Assert.AreEqual(2, res.ClientesEnJuego);
            Assert.AreEqual(1, res.ClientesRetenidos);
        }

        // Revisión 13/10: sin decisiones durante la vigencia pero sí antes, no hay mejora (antes se
        // pedía MejoraRetencion.Value y el reporte de métricas se caía).
        [TestMethod]
        public void ResultadoPromociones_SinDecisionesDuranteLaVigencia_NoHayMejora()
        {
            var dal = new FakePromocionDAL();
            dal.Todas.Add(new BE.Promocion { IdPromocion = 1, Estado = BE.EstadoPromocion.Vigente, IdPlan = 3,
                                             FechaInicio = Hoy.AddDays(-1), FechaFin = Hoy.AddDays(30) });
            dal.Decisiones.Add((3, Dec(1, -2, BE.EstadoRenovacion.Renovada)));   // solo en el período anterior

            var res = CrearConPromos(dal).ResultadoPromociones(Hoy.AddDays(-90), Hoy)[0];

            Assert.IsNull(res.TasaRetencionReal);
            Assert.AreEqual(100m, res.TasaRetencionAntes);
            Assert.IsNull(res.MejoraRetencion);
            Assert.IsNull(res.CumplioRetencionEsperada());
        }

        // Revisión 13/10: si falla una promoción, las demás se evalúan y la fallida queda marcada.
        [TestMethod]
        public void ResultadoPromociones_FallaUnaPromocion_LasDemasSeEvaluanYQuedaMarcada()
        {
            var dal = new FakePromocionDAL { FallarDecisionesDelPlan = 3 };
            dal.Todas.Add(new BE.Promocion { IdPromocion = 1, Nombre = "Falla", Estado = BE.EstadoPromocion.Vigente, IdPlan = 3,
                                             FechaInicio = Hoy.AddDays(-10), FechaFin = Hoy.AddDays(10) });
            dal.Todas.Add(new BE.Promocion { IdPromocion = 2, Nombre = "Anda", Estado = BE.EstadoPromocion.Vigente, IdPlan = 4,
                                             FechaInicio = Hoy.AddDays(-10), FechaFin = Hoy.AddDays(10) });

            var r = CrearConPromos(dal).ResultadoPromociones(Hoy.AddDays(-90), Hoy);

            Assert.AreEqual(2, r.Count);
            Assert.IsTrue(r.Single(x => x.IdPromocion == 1).NoSePudoCalcular);
            Assert.IsFalse(r.Single(x => x.IdPromocion == 2).NoSePudoCalcular);
        }

        // Revisión 13/10: si no se pueden leer las promociones, el reporte lo dice (no "no hay promociones").
        [TestMethod]
        public void AnalizarMetricas_NoSePuedenLeerLasPromociones_ResultadoNoDisponible()
        {
            Login();
            var dal = new FakePromocionDAL { FallarObtenerTodas = true };
            var bll = new BLL.AnalisisPromociones(new RotacionFake(), new AbandonoFake(), new FakePlanSuscripcionDAL(), dal,
                                                  new FakeSugerenciaPromocionDAL(), () => Hoy);
            Assert.IsNull(bll.ResultadoPromociones(Hoy.AddDays(-90), Hoy));
            var reporte = bll.AnalizarMetricas("Test", Hoy.AddDays(-90), Hoy);
            Assert.IsTrue(reporte.ResultadoNoDisponible);
            Assert.AreEqual(0, reporte.ResultadoPromociones.Count);
        }

        [TestMethod]
        public void ResultadoPromociones_SinDalDePromociones_DevuelveVacio()
            => Assert.AreEqual(0, Crear(new RotacionFake(), new AbandonoFake()).ResultadoPromociones(Hoy.AddDays(-90), Hoy).Count);
    }
}
