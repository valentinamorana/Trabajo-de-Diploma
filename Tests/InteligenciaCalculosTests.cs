using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// PN03 «Inteligencia comercial» — matemática pura del Centro de decisiones: proyección de series,
    /// KPI con variación, simulador "¿Qué pasa si…?", medición de promociones y catálogo de umbrales.
    /// Sin base de datos ni UI.
    /// </summary>
    [TestClass]
    public class InteligenciaCalculosTests
    {
        // ══ Proyección ══════════════════════════════════════════════════════════

        [TestMethod]
        public void Regresion_SerieLineal_PendienteOrdenadaYR2Exactos()
        {
            var a = ProyeccionSerie.RegresionLineal(new List<decimal> { 1, 2, 3, 4 });
            Assert.AreEqual(1m, a.Pendiente);
            Assert.AreEqual(1m, a.Ordenada);
            Assert.AreEqual(1m, a.R2);
            CollectionAssert.AreEqual(new List<decimal> { 5, 6, 7 }, ProyeccionSerie.Proyectar(new List<decimal> { 1, 2, 3, 4 }, 3));
        }

        [TestMethod]
        public void Regresion_SerieConstante_ProyectaElMismoValorConR2Uno()
        {
            var serie = new List<decimal> { 5, 5, 5 };
            Assert.AreEqual(0m, ProyeccionSerie.RegresionLineal(serie).Pendiente);
            Assert.AreEqual(1m, ProyeccionSerie.RegresionLineal(serie).R2);
            CollectionAssert.AreEqual(new List<decimal> { 5, 5 }, ProyeccionSerie.Proyectar(serie, 2));
        }

        [TestMethod]
        public void Regresion_SerieVacia_ProyectaCerosSinR2()
        {
            Assert.IsNull(ProyeccionSerie.RegresionLineal(new List<decimal>()).R2);
            CollectionAssert.AreEqual(new List<decimal> { 0, 0, 0 }, ProyeccionSerie.Proyectar(new List<decimal>(), 3));
            CollectionAssert.AreEqual(new List<decimal> { 0 }, ProyeccionSerie.Proyectar(null, 1));
        }

        [TestMethod]
        public void Regresion_UnSoloPunto_RepiteElValor()
        {
            var a = ProyeccionSerie.RegresionLineal(new List<decimal> { 7 });
            Assert.AreEqual(0m, a.Pendiente);
            Assert.IsNull(a.R2);
            CollectionAssert.AreEqual(new List<decimal> { 7, 7 }, ProyeccionSerie.Proyectar(new List<decimal> { 7 }, 2));
        }

        [TestMethod]
        public void Regresion_TendenciaDescendente_NoProyectaNegativos()
        {
            var p = ProyeccionSerie.Proyectar(new List<decimal> { 10, 5, 0 }, 3);
            Assert.IsTrue(p.All(v => v == 0m), "Una proyección negativa se lleva a 0.");
        }

        [TestMethod]
        public void Regresion_SerieConRuido_R2EntreCeroYUno()
        {
            var a = ProyeccionSerie.RegresionLineal(new List<decimal> { 10, 14, 9, 16, 12, 18 });
            Assert.IsTrue(a.R2 > 0m && a.R2 < 1m);
            Assert.IsTrue(a.Pendiente > 0m);
        }

        [TestMethod]
        public void PromedioMovil_UsaLosUltimosNValoresIncluidosLosProyectados()
        {
            CollectionAssert.AreEqual(new List<decimal> { 7.5m, 8.25m }, ProyeccionSerie.PromedioMovil(new List<decimal> { 3, 6, 9 }, 2, 2));
            CollectionAssert.AreEqual(new List<decimal> { 0 }, ProyeccionSerie.PromedioMovil(new List<decimal>(), 3, 1));
        }

        // ══ KPI ═════════════════════════════════════════════════════════════════

        [TestMethod]
        public void TasaBajas_YClientesAlInicio()
        {
            Assert.AreEqual(4m, CalculoKpi.TasaBajas(4, 100));
            Assert.IsNull(CalculoKpi.TasaBajas(0, 0), "Sin clientes al inicio no hay % de bajas (no se divide por cero).");
            Assert.AreEqual(100, CalculoKpi.ClientesAlInicio(90, 10, 20));
            Assert.AreEqual(0, CalculoKpi.ClientesAlInicio(0, 5, 0));
            Assert.AreEqual(1100m, CalculoKpi.MrrAlInicio(1000m, 200m, 300m));
        }

        [TestMethod]
        public void Kpi_PorcentajeQueSube_SeExpresaEnPuntosYEsDesfavorableSiSubirEsMalo()
        {
            var k = CalculoKpi.Crear("churn", new BE.TextoTraducible("kpi.churn", "% de bajas"), BE.UnidadKpi.Porcentaje, 9m, 5m, false);
            Assert.AreEqual(BE.TendenciaKpi.Sube, k.Tendencia);
            Assert.AreEqual(4m, k.Variacion);
            Assert.AreEqual(false, k.EsFavorable);
            Assert.AreEqual("kpi.interp.sube.pp", k.Interpretacion.Clave);
            StringAssert.Contains(k.Interpretacion.Espanol, "4 pp");
            StringAssert.Contains(k.Interpretacion.Espanol, "desfavorable");
        }

        [TestMethod]
        public void Kpi_MontoQueSube_VariacionRelativaFavorable()
        {
            var k = CalculoKpi.Crear("ingresos", new BE.TextoTraducible("kpi.ingresos", "Ingresos"), BE.UnidadKpi.Moneda, 110m, 100m, true);
            Assert.AreEqual(10m, k.VariacionPorcentual);
            Assert.AreEqual(true, k.EsFavorable);
            Assert.AreEqual("kpi.interp.sube.pct", k.Interpretacion.Clave);
        }

        [TestMethod]
        public void Kpi_VariacionMinima_EsEstable()
        {
            var k = CalculoKpi.Crear("ticket", new BE.TextoTraducible("kpi.ticket", "Ticket"), BE.UnidadKpi.Moneda, 100.5m, 100m, true);
            Assert.AreEqual(BE.TendenciaKpi.Estable, k.Tendencia);
            Assert.IsNull(k.EsFavorable);
            var p = CalculoKpi.Crear("churn", new BE.TextoTraducible("kpi.churn", "% de bajas"), BE.UnidadKpi.Porcentaje, 5.3m, 5m, false);
            Assert.AreEqual(BE.TendenciaKpi.Estable, p.Tendencia, "Menos de 0,5 pp es estable.");
        }

        [TestMethod]
        public void Kpi_SinAnterior_SinDato_YDesdeCero()
        {
            var sin = CalculoKpi.Crear("riesgo", new BE.TextoTraducible("kpi.riesgo", "Riesgo"), BE.UnidadKpi.Cantidad, 3m, null, false);
            Assert.AreEqual(BE.TendenciaKpi.SinDato, sin.Tendencia);
            Assert.IsNull(sin.Variacion);
            var cero = CalculoKpi.Crear("descontado", new BE.TextoTraducible("kpi.descontado", "Descontado"), BE.UnidadKpi.Moneda, 50m, 0m, false);
            Assert.AreEqual(BE.TendenciaKpi.Sube, cero.Tendencia);
            Assert.IsNull(cero.VariacionPorcentual, "Contra 0 no hay variación relativa.");
            Assert.AreEqual("kpi.interp.desdecero", cero.Interpretacion.Clave);
        }

        // ══ Simulador ═══════════════════════════════════════════════════════════

        private static BE.EscenarioPromocion Escenario(decimal tasa = 30m, int objetivo = 10, decimal descuento = 10m, int dias = 30, int clientesPlan = 100)
            => new BE.EscenarioPromocion
            {
                IdPlan = 1, NombrePlan = "Básico", PrecioPlan = 1000m, ClientesPlan = clientesPlan, ClientesObjetivo = objetivo,
                DescuentoPorcentaje = descuento, DuracionDias = dias, TasaRetencion = tasa, HorizonteMeses = 6m
            };

        [TestMethod]
        public void Simulador_EscenarioRentable_CostoBeneficioRoiYEquilibrio()
        {
            var r = SimuladorPromocion.Simular(Escenario(), 20m, 10m);
            Assert.AreEqual(10000m, r.CostoDescuento);          // 100 clientes × 1000 × 10 % × 1 mes
            Assert.AreEqual(18000m, r.IngresoRetenido);         // 3 retenidos × 1000 × 6 meses
            Assert.AreEqual(8000m, r.BeneficioNeto);
            Assert.AreEqual(80m, r.Roi);
            Assert.AreEqual(16.67m, r.TasaEquilibrio);          // 10000 / (10 × 1000 × 6)
            Assert.AreEqual(BE.VeredictoSimulacion.Conviene, r.Veredicto);
        }

        [TestMethod]
        public void Simulador_MargenChico_Riesgoso()
        {
            var r = SimuladorPromocion.Simular(Escenario(tasa: 20m), 20m, 10m);
            Assert.AreEqual(2000m, r.BeneficioNeto);
            Assert.AreEqual(BE.VeredictoSimulacion.Riesgoso, r.Veredicto, "Gana, pero 20 % está a 3,3 pp del equilibrio.");
            Assert.AreEqual("sim.ver.riesgoso", r.Explicacion.Clave, "ROI 20 % = mínimo: la causa es solo el margen.");
            StringAssert.Contains(r.Explicacion.Espanol, "el margen es chico");
        }

        [TestMethod]
        public void Simulador_RoiBajoElMinimoConMargenSuficiente_RiesgosoPorElRoi()
        {
            // Costo 100 × 1000 × 36 % = 36000; equilibrio 36000 / 60000 = 60 %; con 70 %: neto 6000, ROI 16,7 %, margen 10 pp.
            var r = SimuladorPromocion.Simular(Escenario(tasa: 70m, descuento: 36m), 20m, 10m);
            Assert.AreEqual(6000m, r.BeneficioNeto);
            Assert.AreEqual(60m, r.TasaEquilibrio);
            Assert.AreEqual(BE.VeredictoSimulacion.Riesgoso, r.Veredicto);
            Assert.AreEqual("sim.ver.riesgoso.roi", r.Explicacion.Clave);
            StringAssert.Contains(r.Explicacion.Espanol, "por debajo del mínimo de 20 %");
            Assert.IsFalse(r.Explicacion.Espanol.Contains("margen es chico"), "El margen no es la causa: no debe decirlo.");
        }

        [TestMethod]
        public void Simulador_RoiBajoYMargenChico_RiesgosoPorAmbasCausas()
        {
            // Equilibrio 60 %; con 65 %: neto 3000, ROI 8,3 %, margen 5 pp.
            var r = SimuladorPromocion.Simular(Escenario(tasa: 65m, descuento: 36m), 20m, 10m);
            Assert.AreEqual(BE.VeredictoSimulacion.Riesgoso, r.Veredicto);
            Assert.AreEqual("sim.ver.riesgoso.ambos", r.Explicacion.Clave);
            StringAssert.Contains(r.Explicacion.Espanol, "por debajo del mínimo de 20 %");
            StringAssert.Contains(r.Explicacion.Espanol, "el margen es chico");
            StringAssert.Contains(r.Explicacion.Espanol, 60m.ToString("0.#") + " %");
        }

        [TestMethod]
        public void Proyectar_SalteandoUnMesParcial_ArrancaUnMesDespues()
        {
            var serie = new List<decimal> { 10m, 20m, 30m, 40m, 50m };
            CollectionAssert.AreEqual(new List<decimal> { 60m, 70m }, ProyeccionSerie.Proyectar(serie, 2));
            CollectionAssert.AreEqual(new List<decimal> { 70m, 80m }, ProyeccionSerie.Proyectar(serie, 2, 1),
                "El mes parcial ocupa x = 5: el primer proyectado es x = 6.");
            CollectionAssert.AreEqual(new List<decimal> { 60m, 70m }, ProyeccionSerie.Proyectar(serie, 2, -3), "Un salto negativo se ignora.");
        }

        [TestMethod]
        public void ClientesYMrrAlFinal_ReconstruyenDesdeHoy()
        {
            Assert.AreEqual(9, CalculoKpi.ClientesAlFinal(10, 2, 1));
            Assert.AreEqual(10, CalculoKpi.ClientesAlFinal(10, 0, 0), "Si el período termina hoy, no hay posteriores.");
            Assert.AreEqual(0, CalculoKpi.ClientesAlFinal(1, 5, 0), "No negativo.");
            Assert.AreEqual(9000m, CalculoKpi.MrrAlFinal(10000m, 2000m, 1000m));
            Assert.AreEqual(0m, CalculoKpi.MrrAlFinal(1000m, 5000m, 0m));
        }

        [TestMethod]
        public void Simulador_RetencionBajoElEquilibrio_NoConvieneYExplicaElMinimo()
        {
            var r = SimuladorPromocion.Simular(Escenario(tasa: 10m), 20m, 10m);
            Assert.AreEqual(-4000m, r.BeneficioNeto);
            Assert.AreEqual(BE.VeredictoSimulacion.NoConviene, r.Veredicto);
            Assert.AreEqual("sim.ver.noconviene", r.Explicacion.Clave);
            StringAssert.Contains(r.Explicacion.Espanol, 16.7m.ToString("0.#"));
        }

        [TestMethod]
        public void Simulador_EquilibrioMayorA100_NuncaConviene()
        {
            var r = SimuladorPromocion.Simular(Escenario(tasa: 100m, descuento: 100m, dias: 60), 20m, 10m);
            Assert.IsTrue(r.TasaEquilibrio > 100m);
            Assert.AreEqual("sim.ver.nunca", r.Explicacion.Clave);
        }

        [TestMethod]
        public void Simulador_SinObjetivo_SoloCuesta_YSinNadie_SinDatos()
        {
            var sinObjetivo = SimuladorPromocion.Simular(Escenario(objetivo: 0), 20m, 10m);
            Assert.AreEqual(BE.VeredictoSimulacion.NoConviene, sinObjetivo.Veredicto);
            Assert.IsNull(sinObjetivo.TasaEquilibrio, "Sin clientes objetivo no hay retención de equilibrio (no se divide por cero).");
            var sinNadie = SimuladorPromocion.Simular(Escenario(objetivo: 0, clientesPlan: 0), 20m, 10m);
            Assert.AreEqual(BE.VeredictoSimulacion.SinDatos, sinNadie.Veredicto);
            Assert.IsNull(sinNadie.Roi, "Sin costo no hay ROI (no se divide por cero).");
        }

        [TestMethod]
        public void Simulador_CurvaDeSensibilidad_CruzaCeroEnElEquilibrio()
        {
            var r = SimuladorPromocion.Simular(Escenario(), 20m, 10m);
            Assert.AreEqual(21, r.Curva.Count);
            Assert.AreEqual(-10000m, r.Curva[0].BeneficioNeto);
            Assert.IsTrue(r.Curva.First(p => p.TasaRetencion == 15).BeneficioNeto < 0);
            Assert.IsTrue(r.Curva.First(p => p.TasaRetencion == 20).BeneficioNeto > 0);
        }

        [TestMethod]
        public void Simulador_ObjetivoMayorQueElPlan_ElDescuentoLoRecibenTodosLosObjetivo()
        {
            var r = SimuladorPromocion.Simular(Escenario(objetivo: 150, clientesPlan: 100), 20m, 10m);
            Assert.AreEqual(15000m, r.CostoDescuento);
        }

        [TestMethod]
        public void Simulador_DatosInvalidos_LanzaAppExceptionTraducible()
        {
            void Esperar(BE.EscenarioPromocion e, string clave)
            {
                try { SimuladorPromocion.Simular(e, 20m, 10m); Assert.Fail("Debía rechazar: " + clave); }
                catch (BE.AppException ex) { Assert.AreEqual(clave, ex.Clave); }
            }
            Esperar(Escenario(descuento: 0m), "err.bll.simulador.descuento");
            Esperar(Escenario(dias: 0), "err.bll.simulador.duracion");
            Esperar(Escenario(tasa: 120m), "err.bll.simulador.tasa");
            Esperar(Escenario(objetivo: -1), "err.bll.simulador.clientes");
            var sinPrecio = Escenario(); sinPrecio.PrecioPlan = 0m;
            Esperar(sinPrecio, "err.bll.simulador.precio");
        }

        // ══ Medición de promociones ═════════════════════════════════════════════

        private static readonly DateTime Hoy = new DateTime(2026, 10, 20);

        private static BE.ResultadoPromocion Resultado(int enJuego, int retenidos, int enJuegoAntes, int retenidosAntes, decimal? esperada, int dias = 31)
        {
            var r = new BE.ResultadoPromocion
            {
                IdPromocion = 7, Nombre = "Retención Básico", Estado = BE.EstadoPromocion.Vigente, IdPlan = 1, NombrePlan = "Básico",
                Desde = Hoy.AddDays(-dias + 1), Hasta = Hoy.AddDays(1), TasaRetencionEsperada = esperada,
                ClientesEnJuego = enJuego, ClientesRetenidos = retenidos, ClientesEnJuegoAntes = enJuegoAntes, ClientesRetenidosAntes = retenidosAntes
            };
            return r;
        }

        private static BE.Promocion Promo(int diasParaVencer) => new BE.Promocion
        {
            IdPromocion = 7, Nombre = "Retención Básico", Estado = BE.EstadoPromocion.Vigente, IdPlan = 1,
            TipoDescuento = BE.TipoDescuento.Porcentaje, Valor = 10m,
            FechaInicio = Hoy.AddDays(-30), FechaFin = Hoy.AddDays(diasParaVencer)
        };

        private static BE.MetricaImpactoPromocion Impacto(decimal descontado) =>
            new BE.MetricaImpactoPromocion { IdPromocion = 7, Cobros = 5, TotalDescontado = descontado };

        [TestMethod]
        public void Medicion_CumpleYVencePronto_ExtenderConBeneficioReal()
        {
            var m = MedicionPromociones.Evaluar(Resultado(10, 8, 10, 5, 30m), Promo(5), 1000m, Impacto(2000m), null, new CriterioMedicion(), Hoy);
            Assert.AreEqual(3m, m.RetenidosAtribuibles);        // 10 × 30 pp
            Assert.AreEqual(16000m, m.BeneficioReal);           // 3 × 1000 × 6 − 2000
            Assert.AreEqual(800m, m.RoiReal);
            Assert.AreEqual(BE.VeredictoPromocion.Extender, m.Veredicto);
        }

        [TestMethod]
        public void Medicion_CumpleYFaltaMucho_Mantener()
        {
            var m = MedicionPromociones.Evaluar(Resultado(10, 8, 10, 5, 30m), Promo(60), 1000m, Impacto(2000m), null, new CriterioMedicion(), Hoy);
            Assert.AreEqual(BE.VeredictoPromocion.Mantener, m.Veredicto);
        }

        [TestMethod]
        public void Medicion_RendimientoBajoLoEsperado_DarDeBaja()
        {
            var m = MedicionPromociones.Evaluar(Resultado(10, 6, 10, 5, 30m), Promo(60), 1000m, Impacto(2000m), null, new CriterioMedicion(), Hoy);
            Assert.AreEqual(BE.VeredictoPromocion.DarDeBaja, m.Veredicto);
            Assert.AreEqual("med.ver.bajorendimiento", m.Explicacion.Clave);
        }

        [TestMethod]
        public void Medicion_SinMejora_DarDeBaja_YConPerdida_DarDeBaja()
        {
            var sinMejora = MedicionPromociones.Evaluar(Resultado(10, 5, 10, 5, 30m), Promo(60), 1000m, Impacto(2000m), null, new CriterioMedicion(), Hoy);
            Assert.AreEqual("med.ver.sinmejora", sinMejora.Explicacion.Clave);
            var perdida = MedicionPromociones.Evaluar(Resultado(10, 6, 10, 5, null), Promo(60), 1000m, Impacto(10000m), null, new CriterioMedicion(), Hoy);
            Assert.AreEqual(BE.VeredictoPromocion.DarDeBaja, perdida.Veredicto);
            Assert.AreEqual("med.ver.perdida", perdida.Explicacion.Clave);
        }

        [TestMethod]
        public void Medicion_PocosDias_Mantener_YSinClientesEnJuego_SinDatos()
        {
            var pronto = MedicionPromociones.Evaluar(Resultado(10, 5, 10, 5, 30m, dias: 5), Promo(60), 1000m, Impacto(500m), null, new CriterioMedicion(), Hoy);
            Assert.AreEqual(BE.VeredictoPromocion.Mantener, pronto.Veredicto);
            Assert.AreEqual("med.ver.pocosdias", pronto.Explicacion.Clave);
            var vacia = MedicionPromociones.Evaluar(Resultado(0, 0, 0, 0, 30m), Promo(60), 1000m, null, null, new CriterioMedicion(), Hoy);
            Assert.AreEqual(BE.VeredictoPromocion.SinDatos, vacia.Veredicto);
            Assert.IsNull(vacia.BeneficioReal);
        }

        [TestMethod]
        public void Medicion_ConSugerencia_BeneficioYRoiEstimados()
        {
            var sugerencia = new BE.SugerenciaPromocion { IngresoMensualEnRiesgo = 10000m, TasaRetencion = 30m, BeneficioEstimado = 3000m };
            var m = MedicionPromociones.Evaluar(Resultado(10, 8, 10, 5, 30m), Promo(60), 1000m, Impacto(2000m), sugerencia, new CriterioMedicion(), Hoy);
            Assert.AreEqual(18000m, m.BeneficioEstimado);       // 10000 × 30 % × 6 meses
            Assert.AreEqual(800m, m.RoiEstimado);               // (18000 − 2000) / 2000
        }

        // ══ Umbrales ════════════════════════════════════════════════════════════

        [TestMethod]
        public void Umbrales_Combinar_IgnoraValoresIlegiblesOFueraDeRango()
        {
            var v = CatalogoUmbrales.Combinar(new Dictionary<string, string>
            {
                [CatalogoUmbrales.ChurnAltoPct] = "8.5",
                [CatalogoUmbrales.DiasPorVencer] = "abc",
                [CatalogoUmbrales.HorizonteMeses] = "999"
            });
            Assert.AreEqual(8.5m, v[CatalogoUmbrales.ChurnAltoPct]);
            Assert.AreEqual(7m, v[CatalogoUmbrales.DiasPorVencer]);
            Assert.AreEqual(6m, v[CatalogoUmbrales.HorizonteMeses]);
        }

        [TestMethod]
        public void Umbrales_Validar_RangoYEnteros()
        {
            CatalogoUmbrales.Validar(CatalogoUmbrales.ChurnAltoPct, 4.5m);
            try { CatalogoUmbrales.Validar(CatalogoUmbrales.DiasPorVencer, 3.5m); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.umbral.entero", ex.Clave); }
            try { CatalogoUmbrales.Validar(CatalogoUmbrales.ChurnAltoPct, 0m); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.umbral.rango", ex.Clave); }
            try { CatalogoUmbrales.Validar("NoExiste", 1m); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.umbral.desconocido", ex.Clave); }
        }

        [TestMethod]
        public void Umbrales_TodosLosValoresPorDefectoEstanDentroDeSuRango()
        {
            foreach (var u in CatalogoUmbrales.Describir(CatalogoUmbrales.ValoresPorDefecto()))
            {
                Assert.IsTrue(u.PorDefecto >= u.Minimo && u.PorDefecto <= u.Maximo, u.Clave);
                Assert.AreEqual("umbral." + u.Clave + ".ayuda", u.Explicacion.Clave);
            }
        }

        [TestMethod]
        public void TextoTraducible_ArgumentoAnidado_SeResuelveEnEspanol()
        {
            var t = new BE.TextoTraducible("x", "{0} — {1}", new BE.TextoTraducible("y", "Hola"), 3);
            Assert.AreEqual("Hola — 3", t.Espanol);
            Assert.AreEqual("sin {formato", new BE.TextoTraducible("z", "sin {formato", 1).Espanol, "Un formato roto no rompe.");
        }
    }
}
