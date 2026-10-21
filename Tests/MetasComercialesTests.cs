using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia;
using BLL.Inteligencia.Reglas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PN03 «Metas comerciales» — validación (rango, mes editable, una por indicador y mes), avance,
    /// semáforo, proyección al cierre, medición con las fórmulas de los KPI y el "movimiento": una meta en
    /// riesgo sube la prioridad de las recomendaciones que la mueven. Todo con dobles, sin SQL Server.
    /// </summary>
    [TestClass]
    public class MetasComercialesTests
    {
        private static readonly DateTime Hoy = new DateTime(2026, 10, 20);
        private static readonly DateTime Octubre = new DateTime(2026, 10, 1);

        [TestInitialize] public void Setup() { SessionManager.Logout(); ConfiguracionTests.BitacoraNegocio.Eventos.Clear(); }
        [TestCleanup] public void Cleanup() => SessionManager.Logout();

        private static void LoginAdmin() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        private static MetasComerciales Servicio(FakeMetaComercialDAL dal) => new MetasComerciales(dal, () => Hoy.AddHours(10));

        private static BE.MetaComercial Meta(BE.IndicadorMeta indicador, DateTime mes, decimal objetivo) => new BE.MetaComercial
        {
            IdMeta = 1, Periodo = mes, Indicador = indicador, ValorObjetivo = objetivo,
            Sentido = CatalogoIndicadores.De(indicador).Sentido
        };

        // 10 clientes del plan 1 ($1000) que pagan hoy; en octubre: 1 alta (el 10) y 2 bajas (el 5 y el 15).
        private static DatosMetas Datos(decimal? ocupacion = 40m)
        {
            var d = new DatosMetas { Hoy = Hoy, OcupacionHoy = ocupacion, Precios = new Dictionary<int, decimal> { [1] = 1000m } };
            for (int i = 1; i <= 10; i++)
                d.Clientes.Add(new BE.ClienteComercial { IdCliente = i, IdPlan = 1, FechaVencimiento = Hoy.AddDays(15) });
            d.Altas.Add(new BE.AltaSuscripcion { IdCliente = 1, IdPlan = 1, Fecha = new DateTime(2026, 10, 10) });
            d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = 20, IdPlanAnterior = 1, Fecha = new DateTime(2026, 10, 5), Resultado = BE.EstadoRenovacion.Baja });
            d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = 21, IdPlanAnterior = 1, Fecha = new DateTime(2026, 10, 15), Resultado = BE.EstadoRenovacion.Baja });
            return d;
        }

        private static void AssertError(string clave, Action accion)
        {
            try { accion(); Assert.Fail("Se esperaba " + clave); }
            catch (BE.AppException ex) { Assert.AreEqual(clave, ex.Clave); }
        }

        // ══ Validación y escritura ═══════════════════════════════════════════════

        [TestMethod]
        public void Validar_RangoMesCerradoMesLejanoYDecimales()
        {
            MetasComerciales.Validar(BE.IndicadorMeta.Mrr, Octubre, 50000m, Hoy);
            MetasComerciales.Validar(BE.IndicadorMeta.PorcentajeBajas, Octubre.AddMonths(12), 0m, Hoy);
            AssertError("err.bll.meta.mescerrado", () => MetasComerciales.Validar(BE.IndicadorMeta.Mrr, Octubre.AddMonths(-1), 50000m, Hoy));
            AssertError("err.bll.meta.mesfuturo", () => MetasComerciales.Validar(BE.IndicadorMeta.Mrr, Octubre.AddMonths(13), 50000m, Hoy));
            AssertError("err.bll.meta.rango", () => MetasComerciales.Validar(BE.IndicadorMeta.PorcentajeBajas, Octubre, 101m, Hoy));
            AssertError("err.bll.meta.rango", () => MetasComerciales.Validar(BE.IndicadorMeta.Mrr, Octubre, 0m, Hoy));
            AssertError("err.bll.meta.rango", () => MetasComerciales.Validar(BE.IndicadorMeta.OcupacionStock, Octubre, -5m, Hoy));
            AssertError("err.bll.meta.decimales", () => MetasComerciales.Validar(BE.IndicadorMeta.OcupacionStock, Octubre, 50.123m, Hoy));
            AssertError("err.bll.meta.indicador", () => MetasComerciales.Validar((BE.IndicadorMeta)99, Octubre, 1m, Hoy));
        }

        [TestMethod]
        public void Guardar_DosVecesElMismoIndicadorYMes_ModificaLaMetaNoLaDuplica()
        {
            LoginAdmin();
            var dal = new FakeMetaComercialDAL();
            var svc = Servicio(dal);
            var primera = svc.Guardar("Test", BE.IndicadorMeta.PorcentajeBajas, Hoy, 5m);
            var segunda = svc.Guardar("Test", BE.IndicadorMeta.PorcentajeBajas, Octubre.AddDays(9), 4m);
            Assert.AreEqual(1, dal.Filas.Count, "Una sola meta por indicador y mes.");
            Assert.AreEqual(primera.IdMeta, segunda.IdMeta);
            Assert.AreEqual(4m, dal.Filas[0].ValorObjetivo);
            Assert.AreEqual(Octubre, dal.Filas[0].Periodo, "El período se guarda como el primer día del mes.");
            Assert.AreEqual(BE.SentidoMeta.MenorEsMejor, dal.Filas[0].Sentido, "El sentido lo pone el catálogo.");
            Assert.AreEqual(1, dal.Filas[0].IdUsuario);
            Assert.AreEqual(2, ConfiguracionTests.BitacoraNegocio.Eventos.Count(e => e.Tipo == BE.TipoEventoNegocio.DecisionComercial));

            svc.Guardar("Test", BE.IndicadorMeta.Mrr, Hoy, 20000m);
            Assert.AreEqual(2, dal.Filas.Count, "Otro indicador del mismo mes es otra meta.");
        }

        [TestMethod]
        public void Guardar_SinLaPatenteDeGerencia_Rechaza()
        {
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor", Permisos = new List<BE.Permiso>() });
            var dal = new FakeMetaComercialDAL();
            AssertError("err.bll.sin_permiso", () => Servicio(dal).Guardar("Test", BE.IndicadorMeta.Mrr, Hoy, 1000m));
            Assert.AreEqual(0, dal.Filas.Count);
        }

        [TestMethod]
        public void Eliminar_SoloMetasDelMesEnCursoOFuturas()
        {
            LoginAdmin();
            var dal = new FakeMetaComercialDAL();
            var svc = Servicio(dal);
            var meta = svc.Guardar("Test", BE.IndicadorMeta.Mrr, Hoy, 1000m);
            var vieja = new BE.MetaComercial { IdMeta = 99, Periodo = Octubre.AddMonths(-1), Indicador = BE.IndicadorMeta.Mrr, ValorObjetivo = 1m };
            dal.Filas.Add(vieja);
            AssertError("err.bll.meta.mescerrado", () => svc.Eliminar("Test", vieja));
            svc.Eliminar("Test", meta);
            CollectionAssert.AreEqual(new[] { 99 }, dal.Filas.Select(f => f.IdMeta).ToList());
            Assert.IsTrue(MetasComerciales.EsMesEditable(Octubre, Hoy));
            Assert.IsFalse(MetasComerciales.EsMesEditable(Octubre.AddMonths(-1), Hoy));
        }

        // ══ Avance, semáforo y proyección (puros) ════════════════════════════════

        [TestMethod]
        public void Avance_MayorEsMejorRealSobreObjetivo_MenorEsMejorRealSobreMaximo()
        {
            Assert.AreEqual(90m, MetasComerciales.Avance(BE.SentidoMeta.MayorEsMejor, 9000m, 10000m));
            Assert.AreEqual(60m, MetasComerciales.Avance(BE.SentidoMeta.MenorEsMejor, 3m, 5m));
            Assert.AreEqual(0m, MetasComerciales.Avance(BE.SentidoMeta.MenorEsMejor, 0m, 0m));
            Assert.IsNull(MetasComerciales.Avance(BE.SentidoMeta.MenorEsMejor, 2m, 0m));
            Assert.IsNull(MetasComerciales.Avance(BE.SentidoMeta.MayorEsMejor, null, 10m));
        }

        [TestMethod]
        public void Semaforo_MesesCerradosFuturosYSinDato()
        {
            var sep = Octubre.AddMonths(-1);
            Assert.AreEqual(BE.EstadoMeta.Pendiente, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, Octubre.AddMonths(1), Hoy, 10m, null, null));
            Assert.AreEqual(BE.EstadoMeta.SinDatos, MetasComerciales.Semaforo(BE.IndicadorMeta.OcupacionStock, sep, Hoy, 50m, null, null));
            Assert.AreEqual(BE.EstadoMeta.Cumplida, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, sep, Hoy, 10000m, 10000m, null));
            Assert.AreEqual(BE.EstadoMeta.NoCumplida, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, sep, Hoy, 10000m, 9999m, null));
            Assert.AreEqual(BE.EstadoMeta.Cumplida, MetasComerciales.Semaforo(BE.IndicadorMeta.PorcentajeBajas, sep, Hoy, 5m, 5m, null));
            Assert.AreEqual(BE.EstadoMeta.NoCumplida, MetasComerciales.Semaforo(BE.IndicadorMeta.PorcentajeBajas, sep, Hoy, 5m, 5.1m, null));
        }

        [TestMethod]
        public void Semaforo_MesEnCurso_UmbralesConLaProyeccion()
        {
            // MRR (mayor es mejor, no acumulativo)
            Assert.AreEqual(BE.EstadoMeta.Cumplida, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, Octubre, Hoy, 10000m, 10500m, 10200m));
            Assert.AreEqual(BE.EstadoMeta.EnRiesgo, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, Octubre, Hoy, 10000m, 10500m, 9900m),
                "Ya llegó, pero al ritmo actual cierra por debajo.");
            Assert.AreEqual(BE.EstadoMeta.EnCamino, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, Octubre, Hoy, 10000m, 9500m, 10100m));
            Assert.AreEqual(BE.EstadoMeta.EnRiesgo, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, Octubre, Hoy, 10000m, 9500m, 9800m));
            Assert.AreEqual(BE.EstadoMeta.Cumplida, MetasComerciales.Semaforo(BE.IndicadorMeta.Mrr, Octubre, Hoy, 10000m, 10000m, null),
                "Sin proyección (pocos días) se usa el valor de hoy.");
            // % de bajas (menor es mejor, acumulativo): nunca "cumplida" antes del cierre; pasarse es definitivo.
            Assert.AreEqual(BE.EstadoMeta.EnCamino, MetasComerciales.Semaforo(BE.IndicadorMeta.PorcentajeBajas, Octubre, Hoy, 5m, 2m, 4m));
            Assert.AreEqual(BE.EstadoMeta.EnRiesgo, MetasComerciales.Semaforo(BE.IndicadorMeta.PorcentajeBajas, Octubre, Hoy, 5m, 4m, 6m));
            Assert.AreEqual(BE.EstadoMeta.NoCumplida, MetasComerciales.Semaforo(BE.IndicadorMeta.PorcentajeBajas, Octubre, Hoy, 5m, 5.5m, 8m));
            // Ocupación (foto del día, sin proyección)
            Assert.AreEqual(BE.EstadoMeta.EnRiesgo, MetasComerciales.Semaforo(BE.IndicadorMeta.OcupacionStock, Octubre, Hoy, 60m, 40m, null));
        }

        [TestMethod]
        public void Proyectar_RegresionLinealHastaElUltimoDiaDelMes()
        {
            var serie = Enumerable.Range(1, 10).Select(i => (decimal)i).ToList();      // y = 1 + x
            Assert.AreEqual(31m, MetasComerciales.Proyectar(serie, 31, false), "x = 30 el día 31.");
            Assert.IsNull(MetasComerciales.Proyectar(serie.Take(MetasComerciales.DiasMinimosProyeccion - 1).ToList(), 31, false),
                "Con menos de 7 días todavía no se proyecta.");
            var cayendo = Enumerable.Range(0, 10).Select(i => 100m - 20m * i).ToList();
            Assert.AreEqual(0m, MetasComerciales.Proyectar(cayendo, 30, false), "Nunca negativa.");
            Assert.AreEqual(100m, MetasComerciales.Proyectar(Enumerable.Range(0, 10).Select(i => 10m * i).ToList(), 30, true), "Un porcentaje no pasa de 100.");
            Assert.AreEqual(5m, MetasComerciales.Proyectar(Enumerable.Repeat(5m, 8).ToList(), 30, true));
        }

        [TestMethod]
        public void Proyectar_AnclaEnElValorDeHoy_ElUltimoDiaEsElReal()
        {
            var mes = Enumerable.Range(0, 31).Select(i => 100m + 3m * i).ToList();
            Assert.AreEqual(mes.Last(), MetasComerciales.Proyectar(mes, 31, false), "El último día del mes el estimado es el valor real.");
            var conRuido = new List<decimal> { 10, 30, 10, 30, 10, 30, 10, 30 };
            Assert.AreEqual(Math.Round(30m + ProyeccionSerie.RegresionLineal(conRuido).Pendiente * 22m, 2),
                MetasComerciales.Proyectar(conRuido, 30, false), "Hoy + pendiente × días que faltan.");
        }

        [TestMethod]
        public void Proyectar_SaltoReciente_NoSeDiluyeEnLaRecta()
        {
            // Diez días planos en 1000 y un salto a 5000 el día 11: la recta sola cerraría cerca de 3500;
            // anclada en el valor de hoy, el estimado parte de 5000.
            var serie = Enumerable.Repeat(1000m, 10).Concat(new[] { 5000m }).ToList();
            var ajuste = ProyeccionSerie.RegresionLineal(serie);
            decimal? estimado = MetasComerciales.Proyectar(serie, 30, false);
            Assert.AreEqual(Math.Round(5000m + ajuste.Pendiente * 19m, 2), estimado);
            Assert.IsTrue(estimado > ajuste.Estimar(29), "Más alto que la recta sin anclar.");
            Assert.IsTrue(estimado >= 5000m);
        }

        // ══ Medición con las fórmulas de los KPI ═════════════════════════════════

        [TestMethod]
        public void ValorAl_MrrYPorcentajeDeBajasComoLosKpi()
        {
            var d = Datos();
            Assert.AreEqual(10000m, MetasComerciales.ValorAl(BE.IndicadorMeta.Mrr, Octubre, Hoy, d), "MRR de hoy.");
            // Al cierre del 4: MRR hoy − 1000 de la alta del 10 + 2000 de las bajas del 5 y del 15.
            Assert.AreEqual(11000m, MetasComerciales.ValorAl(BE.IndicadorMeta.Mrr, Octubre, new DateTime(2026, 10, 4), d));
            // % de bajas del mes a hoy: 2 bajas / (10 − 1 alta + 2 bajas = 11) = 18,18 %.
            Assert.AreEqual(18.18m, MetasComerciales.ValorAl(BE.IndicadorMeta.PorcentajeBajas, Octubre, Hoy, d));
            // Al cierre del 10: 1 baja / (10 + 1 baja posterior − 1 alta + 1 baja = 11).
            Assert.AreEqual(9.09m, MetasComerciales.ValorAl(BE.IndicadorMeta.PorcentajeBajas, Octubre, new DateTime(2026, 10, 10), d));
            Assert.AreEqual(40m, MetasComerciales.ValorAl(BE.IndicadorMeta.OcupacionStock, Octubre, Hoy, d));
            Assert.IsNull(MetasComerciales.ValorAl(BE.IndicadorMeta.OcupacionStock, Octubre, Hoy.AddDays(-1), d), "La ocupación es una foto del día.");
            Assert.IsNull(MetasComerciales.ValorAl(BE.IndicadorMeta.Mrr, Octubre, Hoy.AddDays(1), d), "No hay datos del futuro.");
            Assert.AreEqual(20, MetasComerciales.SerieDiaria(BE.IndicadorMeta.Mrr, Octubre, d).Count, "Del 1 al 20 de octubre.");
        }

        [TestMethod]
        public void Seguir_MesEnCursoConProyeccionAvanceYSerieDeSeisMeses()
        {
            var meta = Meta(BE.IndicadorMeta.PorcentajeBajas, Octubre, 10m);
            var anterior = Meta(BE.IndicadorMeta.PorcentajeBajas, Octubre.AddMonths(-1), 8m);
            var s = MetasComerciales.Seguir(meta, new[] { meta, anterior }, Datos());
            Assert.IsTrue(s.EsMesEnCurso);
            Assert.AreEqual(18.18m, s.ValorActual);
            Assert.AreEqual(BE.EstadoMeta.NoCumplida, s.Estado, "Un máximo acumulativo ya superado no se recupera en el mes.");
            Assert.IsTrue(s.RequiereAccion);
            Assert.AreEqual(181.8m, s.AvancePorcentaje);
            Assert.IsNotNull(s.Proyeccion, "Con 20 días del mes hay proyección.");
            Assert.AreEqual(20, s.DiasTranscurridos);
            Assert.AreEqual(31, s.DiasDelMes);
            Assert.AreEqual(MetasComerciales.MesesHistoria, s.Serie.Count);
            Assert.AreEqual(Octubre, s.Serie.Last().Mes);
            Assert.AreEqual(8m, s.Serie[4].Objetivo, "La meta de septiembre aparece en la serie.");
            Assert.AreEqual("cdc.meta.exp.superada", s.Explicacion.Clave);
        }

        [TestMethod]
        public void Seguir_OcupacionSinHistoria_SinProyeccionYSinDatoEnMesesCerrados()
        {
            var actual = MetasComerciales.Seguir(Meta(BE.IndicadorMeta.OcupacionStock, Octubre, 60m), null, Datos(ocupacion: 40m));
            Assert.IsNull(actual.Proyeccion);
            Assert.AreEqual(BE.EstadoMeta.EnRiesgo, actual.Estado);
            Assert.AreEqual("cdc.meta.exp.enriesgo.sinproy", actual.Explicacion.Clave);
            var cerrada = MetasComerciales.Seguir(Meta(BE.IndicadorMeta.OcupacionStock, Octubre.AddMonths(-1), 60m), null, Datos());
            Assert.AreEqual(BE.EstadoMeta.SinDatos, cerrada.Estado);
            Assert.AreEqual("cdc.meta.exp.foto", cerrada.Explicacion.Clave);
            var futura = MetasComerciales.Seguir(Meta(BE.IndicadorMeta.Mrr, Octubre.AddMonths(1), 60m), null, Datos());
            Assert.AreEqual(BE.EstadoMeta.Pendiente, futura.Estado);
            Assert.IsFalse(futura.RequiereAccion);
        }

        // ══ Movimiento: una meta en riesgo sube la prioridad de lo que la mueve ═

        private static BE.Recomendacion Rec(string tipo, BE.PrioridadRecomendacion prioridad, decimal impacto) => new BE.Recomendacion
        {
            TipoRegla = tipo, Clave = tipo, Prioridad = prioridad,
            Principal = new BE.OpcionRecomendacion { ImpactoEstimado = impacto, Accion = BE.AccionRecomendacion.ExportarContactos }
        };

        private static BE.SeguimientoMeta EnRiesgo(BE.IndicadorMeta indicador) => new BE.SeguimientoMeta
        {
            Meta = Meta(indicador, Octubre, 5m), NombreIndicador = CatalogoIndicadores.De(indicador).NombreTraducible,
            Unidad = CatalogoIndicadores.De(indicador).Unidad, ValorActual = 4m, Proyeccion = 7m,
            Estado = BE.EstadoMeta.EnRiesgo, EsMesEnCurso = true
        };

        [TestMethod]
        public void ElevarPrioridad_MetaDeBajasEnRiesgo_SubenLasDeBajasYRenovacionesYLoExplican()
        {
            var churn = Rec(ReglaChurnPlan.Codigo, BE.PrioridadRecomendacion.Media, 5000m);
            var renov = Rec(ReglaRenovaciones.Codigo, BE.PrioridadRecomendacion.Baja, 100m);
            var escasez = Rec(ReglaEscasez.Codigo, BE.PrioridadRecomendacion.Media, 9000m);
            var perdedora = Rec(ReglaUpgradePlan.Codigo, BE.PrioridadRecomendacion.Baja, -10m);
            var yaAlta = Rec(ReglaRendimientoPromocion.Codigo, BE.PrioridadRecomendacion.Alta, 10m);

            var resultado = MetasComerciales.ElevarPrioridad(new[] { escasez, renov, churn, perdedora, yaAlta },
                new[] { EnRiesgo(BE.IndicadorMeta.PorcentajeBajas) });

            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, churn.Prioridad);
            Assert.AreEqual(BE.PrioridadRecomendacion.Media, renov.Prioridad);
            Assert.AreEqual("cdc.meta.prioridad", churn.PrioridadElevada.Clave);
            Assert.AreEqual(BE.PrioridadRecomendacion.Media, escasez.Prioridad, "La escasez no mueve el % de bajas.");
            Assert.IsNull(escasez.PrioridadElevada);
            Assert.AreEqual(BE.PrioridadRecomendacion.Baja, perdedora.Prioridad, "Una recomendación que pierde plata no sube.");
            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, yaAlta.Prioridad);
            Assert.AreEqual("cdc.meta.prioridad.yaalta", yaAlta.PrioridadElevada.Clave);
            CollectionAssert.AreEqual(new[] { ReglaChurnPlan.Codigo, ReglaRendimientoPromocion.Codigo },
                resultado.Take(2).Select(r => r.TipoRegla).ToList(), "Reordenada como el motor: prioridad y después impacto.");

            MetasComerciales.ElevarPrioridad(resultado, new[] { EnRiesgo(BE.IndicadorMeta.PorcentajeBajas) });
            Assert.AreEqual(BE.PrioridadRecomendacion.Media, renov.Prioridad, "Sube una sola vez.");
        }

        [TestMethod]
        public void ElevarPrioridad_SinMetasEnRiesgoOMetasDeOtroMes_NoCambiaNada()
        {
            var churn = Rec(ReglaChurnPlan.Codigo, BE.PrioridadRecomendacion.Baja, 5000m);
            var encamino = EnRiesgo(BE.IndicadorMeta.PorcentajeBajas); encamino.Estado = BE.EstadoMeta.EnCamino;
            var cerrada = EnRiesgo(BE.IndicadorMeta.PorcentajeBajas); cerrada.Estado = BE.EstadoMeta.NoCumplida; cerrada.EsMesEnCurso = false;
            MetasComerciales.ElevarPrioridad(new[] { churn }, new[] { encamino, cerrada });
            Assert.AreEqual(BE.PrioridadRecomendacion.Baja, churn.Prioridad);
            Assert.IsNull(churn.PrioridadElevada);
        }

        [TestMethod]
        public void ElevarPrioridad_OcupacionEnRiesgo_SubeLaLiquidacionDePrendas()
        {
            var rotacion = Rec(ReglaBajaRotacion.Codigo, BE.PrioridadRecomendacion.Media, 1200m);
            MetasComerciales.ElevarPrioridad(new[] { rotacion }, new[] { EnRiesgo(BE.IndicadorMeta.OcupacionStock) });
            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, rotacion.Prioridad);
            CollectionAssert.Contains(CatalogoIndicadores.MovidosPor(ReglaBajaRotacion.Codigo).ToList(), BE.IndicadorMeta.OcupacionStock);
        }

        // ══ Integración con el servicio del Centro de decisiones ════════════════

        [TestMethod]
        public void Recomendar_ConUnaMetaDeBajasEnRiesgo_LaRecomendacionDeBajasSubeDePrioridad()
        {
            LoginAdmin();
            var dalMetas = new FakeMetaComercialDAL();
            dalMetas.Filas.Add(new BE.MetaComercial { IdMeta = 1, Periodo = Octubre, Indicador = BE.IndicadorMeta.PorcentajeBajas,
                                                      ValorObjetivo = 1m, Sentido = BE.SentidoMeta.MenorEsMejor });
            var dal = new FakeInteligenciaComercialDAL();
            for (int i = 1; i <= 20; i++)
                dal.Clientes.Add(new BE.ClienteComercial { IdCliente = i, IdPlan = 1, FechaVencimiento = Hoy.AddDays(30) });
            foreach (var dia in new[] { 2, 4, 8 })
                dal.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = 100 + dia, IdPlanAnterior = 1, Fecha = new DateTime(2026, 10, dia), Resultado = BE.EstadoRenovacion.Baja });
            var planes = new FakePlanSuscripcionDAL { Planes = new List<BE.PlanSuscripcion> { new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", LimitePrendas = 3, Precio = 1000m, Estado = true } } };
            var svc = new InteligenciaComercial(dal, planes, null, null, null, null, new FakeDecisionComercialDAL(), () => Hoy,
                                                null, dalMetas);

            var resultado = svc.Recomendar(svc.Analizar("Test", Hoy.AddDays(-29), Hoy));
            var metas = resultado.Metas;
            Assert.AreEqual(1, metas.Count);
            Assert.IsTrue(metas[0].RequiereAccion, metas[0].Estado.ToString());
            var churn = resultado.Recomendaciones.FirstOrDefault(r => r.TipoRegla == ReglaChurnPlan.Codigo && r.ImpactoEstimado > 0);
            Assert.IsNotNull(churn, "La regla de bajas recomienda en el plan Básico.");
            Assert.IsNotNull(churn.PrioridadElevada, "La recomendación que mueve la meta explica por qué subió.");
            Assert.AreEqual(0, svc.SeguirMetas(Octubre.AddMonths(1)).Count, "Sin metas en el mes no hay seguimiento.");
        }
    }
}
