using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PN03 «Análisis de cohortes» (Centro de decisiones, pestaña Cohortes): la matriz de retención por
    /// mes de alta, el mes parcial, la curva promedio, la regla de la peor cohorte (tamaño mínimo y mes
    /// de comparación), el plan que la explica, el paso a la recomendación y los datos de la demo.
    /// Todo con datos armados a mano (BLL pura, sin SQL Server).
    /// </summary>
    [TestClass]
    public class AnalisisCohortesTests
    {
        private const int Basico = 1, Estandar = 2, Premium = 3;
        private static readonly Dictionary<int, string> Nombres = new Dictionary<int, string>
        {
            [Basico] = "Básico", [Estandar] = "Estándar", [Premium] = "Premium"
        };

        [TestInitialize] public void Setup() => SessionManager.Logout();
        [TestCleanup] public void Cleanup() => SessionManager.Logout();

        private static void LoginAdmin() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        /// <summary>Datos de prueba: altas y bajas por cliente.</summary>
        private sealed class Datos
        {
            public List<BE.AltaSuscripcion> Altas = new List<BE.AltaSuscripcion>();
            public List<BE.MovimientoSuscripcion> Movimientos = new List<BE.MovimientoSuscripcion>();
            private int siguiente = 1;

            /// <summary>Agrega <paramref name="cantidad"/> clientes dados de alta en <paramref name="alta"/>; devuelve sus IDs.</summary>
            public List<int> Alta(DateTime alta, int plan, int cantidad = 1)
            {
                var ids = new List<int>();
                for (int i = 0; i < cantidad; i++)
                {
                    int id = siguiente++;
                    Altas.Add(new BE.AltaSuscripcion { IdCliente = id, IdPlan = plan, Fecha = alta.AddHours(10) });
                    ids.Add(id);
                }
                return ids;
            }

            public Datos Baja(int idCliente, DateTime fecha, int? plan = null)
            {
                Movimientos.Add(new BE.MovimientoSuscripcion
                {
                    IdCliente = idCliente, IdPlanAnterior = plan ?? Altas.First(a => a.IdCliente == idCliente).IdPlan,
                    Fecha = fecha.AddHours(12), Resultado = BE.EstadoRenovacion.Baja
                });
                return this;
            }

            public BE.MatrizCohortes Calcular(DateTime corte, int? idPlan = null, int minimo = AnalisisCohortes.MinimoClientesCohorte) =>
                AnalisisCohortes.Calcular(Altas, Movimientos, corte, idPlan, Nombres, minimo);
        }

        // ── Matriz ────────────────────────────────────────────────────────────────

        [TestMethod]
        public void Matriz_RetencionPorMesDesdeElAlta_ConTamanioYMesParcial()
        {
            // Corte: 20/10/2026 (octubre es el mes en curso, parcial).
            var d = new Datos();
            var junio = d.Alta(new DateTime(2026, 6, 10), Basico, 4);
            d.Baja(junio[0], new DateTime(2026, 6, 25));   // baja en el mismo mes del alta → Mes 0 = 75 %
            d.Baja(junio[1], new DateTime(2026, 8, 3));    // baja en el mes 2
            d.Baja(junio[2], new DateTime(2026, 10, 5));   // baja en el mes 4, que es el mes en curso
            var m = d.Calcular(new DateTime(2026, 10, 20));

            Assert.AreEqual(1, m.Cohortes.Count);
            var f = m.Cohortes[0];
            Assert.AreEqual(new DateTime(2026, 6, 1), f.Mes);
            Assert.AreEqual(4, f.Tamanio);
            Assert.AreEqual(4, m.MaximoDesplazamiento, "Junio → octubre: meses 0 a 4.");
            CollectionAssert.AreEqual(new[] { 75m, 75m, 50m, 50m, 25m }, f.Celdas.Select(c => c.Retencion).ToArray());
            CollectionAssert.AreEqual(new[] { 3, 3, 2, 2, 1 }, f.Celdas.Select(c => c.Activos).ToArray());
            Assert.IsTrue(f.Celda(4).EsParcial, "Octubre todavía no terminó al corte.");
            Assert.IsFalse(f.Celda(3).EsParcial);
            Assert.IsNull(f.Celda(5), "Un mes futuro no tiene celda.");
            Assert.IsTrue(m.HayParciales);
        }

        [TestMethod]
        public void MesParcial_NoEntraEnLaCurvaPromedio_YAlCierreDelMesYaNoEsParcial()
        {
            var d = new Datos();
            var sep = d.Alta(new DateTime(2026, 9, 5), Basico, 4);
            d.Alta(new DateTime(2026, 10, 2), Basico, 2);
            d.Baja(sep[0], new DateTime(2026, 10, 10));

            var aMitad = d.Calcular(new DateTime(2026, 10, 20));
            var puntoMes1 = aMitad.Curva.Single(p => p.Desplazamiento == 1);
            Assert.IsNull(puntoMes1.Retencion, "El mes 1 de septiembre (octubre) es parcial: no hay promedio todavía.");
            var mes0 = aMitad.Curva.Single(p => p.Desplazamiento == 0);
            Assert.AreEqual(1, mes0.Cohortes, "Solo septiembre completó el mes 0; la cohorte de octubre está en curso.");
            Assert.AreEqual(100m, mes0.Retencion);
            Assert.IsTrue(aMitad.Cohortes.Single(c => c.Mes.Month == 10).Celda(0).EsParcial);

            // El 31/10 octubre ya está completo: nada es parcial.
            var alCierre = d.Calcular(new DateTime(2026, 10, 31));
            Assert.IsFalse(alCierre.HayParciales);
            Assert.AreEqual(75m, alCierre.Curva.Single(p => p.Desplazamiento == 1).Retencion);
        }

        [TestMethod]
        public void CurvaPromedio_PonderaPorTamanio()
        {
            var d = new Datos();
            var julio = d.Alta(new DateTime(2026, 7, 1), Basico, 8);   // 8 clientes, 2 bajas en el mes 1 → 75 %
            d.Baja(julio[0], new DateTime(2026, 8, 5)).Baja(julio[1], new DateTime(2026, 8, 6));
            var agosto = d.Alta(new DateTime(2026, 8, 1), Basico, 2);  // 2 clientes, 1 baja en el mes 1 → 50 %
            d.Baja(agosto[0], new DateTime(2026, 9, 9));
            var m = d.Calcular(new DateTime(2026, 9, 30));
            // (6 + 1) / (8 + 2) = 70 % (el promedio simple daría 62,5 %).
            Assert.AreEqual(70m, m.Curva.Single(p => p.Desplazamiento == 1).Retencion);
            Assert.AreEqual(10, m.Curva.Single(p => p.Desplazamiento == 1).Clientes);
        }

        [TestMethod]
        public void ActivoEnElMes_PausasYCambiosDePlanNoSonBajas_YUnaBajaAnteriorAlAltaNoCuenta()
        {
            var d = new Datos();
            var ids = d.Alta(new DateTime(2026, 8, 10), Basico, 4);
            d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = ids[0], Fecha = new DateTime(2026, 9, 1), Resultado = BE.EstadoRenovacion.Pausada, IdPlanAnterior = Basico });
            d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = ids[1], Fecha = new DateTime(2026, 9, 1), Resultado = BE.EstadoRenovacion.CambioPlan, IdPlanAnterior = Basico, IdPlanNuevo = Premium });
            d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = ids[2], Fecha = new DateTime(2026, 7, 1), Resultado = BE.EstadoRenovacion.Baja, IdPlanAnterior = Basico });
            var m = d.Calcular(new DateTime(2026, 9, 30));
            Assert.IsTrue(m.Cohortes[0].Celdas.All(c => c.Retencion == 100m),
                "Pausa y cambio de plan no son bajas (mismo criterio que el % de bajas); la baja previa al alta es de otra suscripción.");
        }

        [TestMethod]
        public void FiltroDePlan_LaCohorteSonLosQueSeSuscribieronAEsePlan()
        {
            var d = new Datos();
            d.Alta(new DateTime(2026, 8, 3), Basico, 3);
            d.Alta(new DateTime(2026, 8, 4), Premium, 5);
            var m = d.Calcular(new DateTime(2026, 9, 30), Premium);
            Assert.AreEqual(1, m.Cohortes.Count);
            Assert.AreEqual(5, m.Cohortes[0].Tamanio);
            Assert.AreEqual("Premium", m.NombrePlan);
        }

        [TestMethod]
        public void Ventana_SoloLosUltimos12MesesDeAlta()
        {
            var d = new Datos();
            d.Alta(new DateTime(2025, 10, 31), Basico);   // 12 meses antes del mes del corte: queda afuera
            d.Alta(new DateTime(2025, 11, 1), Basico);
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(1, m.Cohortes.Count);
            Assert.AreEqual(new DateTime(2025, 11, 1), m.Cohortes[0].Mes);
            Assert.AreEqual(11, m.MaximoDesplazamiento);
        }

        [TestMethod]
        public void SinDatos_MatrizVacia_SinPeorYConMotivo()
        {
            var m = AnalisisCohortes.Calcular(null, null, new DateTime(2026, 10, 20));
            Assert.IsTrue(m.SinDatos);
            Assert.AreEqual(0, m.Curva.Count);
            Assert.IsNull(m.Peor);
            Assert.AreEqual("cdc.cohorte.sinpeor.sindatos", m.MotivoSinPeor.Clave);
            Assert.AreSame(m.MotivoSinPeor, m.Resumen);
        }

        [TestMethod]
        public void UnaSolaCohorte_NoHayConQueCompararla()
        {
            var d = new Datos();
            var ids = d.Alta(new DateTime(2026, 5, 4), Basico, 10);
            d.Baja(ids[0], new DateTime(2026, 6, 1));
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(1, m.Cohortes.Count);
            Assert.IsNull(m.Peor);
            Assert.AreEqual("cdc.cohorte.sinpeor.pocas", m.MotivoSinPeor.Clave);
            Assert.IsFalse(m.Cohortes[0].EsPeor);
        }

        // ── Peor cohorte ──────────────────────────────────────────────────────────

        /// <summary>Mayo (6, retiene 50 % al mes 3, bajas del Básico), junio (5, 80 %) y julio (5, 100 %).</summary>
        private static Datos TresCohortes()
        {
            var d = new Datos();
            var mayoB = d.Alta(new DateTime(2026, 5, 5), Basico, 4);
            var mayoP = d.Alta(new DateTime(2026, 5, 6), Premium, 2);
            d.Baja(mayoB[0], new DateTime(2026, 6, 10)).Baja(mayoB[1], new DateTime(2026, 7, 10)).Baja(mayoP[0], new DateTime(2026, 8, 20));
            d.Baja(mayoB[2], new DateTime(2026, 10, 2));   // después del mes 3 de mayo (agosto): no explica la comparación
            var junio = d.Alta(new DateTime(2026, 6, 8), Estandar, 5);
            d.Baja(junio[0], new DateTime(2026, 7, 15));
            d.Alta(new DateTime(2026, 7, 9), Estandar, 5);
            return d;
        }

        [TestMethod]
        public void PeorCohorte_MenorRetencionAlMes3_YElPlanQueMasExplicaSusBajas()
        {
            var m = TresCohortes().Calcular(new DateTime(2026, 10, 20));
            Assert.IsNotNull(m.Peor);
            Assert.AreEqual(new DateTime(2026, 5, 1), m.Peor.Mes);
            Assert.AreEqual(3, m.Peor.Desplazamiento, "Mayo y junio completaron el mes 3; julio no (octubre está en curso).");
            Assert.AreEqual(50m, m.Peor.Retencion);
            Assert.AreEqual(3, m.Peor.Bajas, "Solo las bajas hasta el cierre del mes 3.");
            Assert.AreEqual(Basico, m.Peor.IdPlan);
            Assert.AreEqual("Básico", m.Peor.NombrePlan);
            Assert.AreEqual(2, m.Peor.BajasDelPlan);
            Assert.AreEqual("ChurnPlan:" + Basico, m.Peor.ClaveRecomendacion);
            Assert.AreEqual(Math.Round((3m + 4m) * 100m / 11m, 1), m.Peor.RetencionPromedio, "Promedio de mayo y junio al mes 3.");
            Assert.IsTrue(m.Cohortes.Single(c => c.Mes.Month == 5).EsPeor);
            Assert.AreEqual(1, m.Cohortes.Count(c => c.EsPeor));
            Assert.AreEqual("cdc.cohorte.peor", m.Resumen.Clave);
        }

        [TestMethod]
        public void PeorCohorte_MesDeComparacion_EsElUltimoQueCompletanAlMenosDosCohortes()
        {
            // Corte 15/08 (agosto en curso): mayo completó hasta el mes 2, junio hasta el 1 y julio hasta el 0.
            // Ningún mes 3 está completo y el mes 2 lo completó solo mayo: se compara en el mes 1 (mayo y junio).
            var m = TresCohortes().Calcular(new DateTime(2026, 8, 15));
            Assert.IsNotNull(m.Peor);
            Assert.AreEqual(1, m.Peor.Desplazamiento);
            // Mes 1: mayo 5 de 6 (83,3 %), junio 4 de 5 (80 %) → la peor es junio, con su baja del Estándar.
            Assert.AreEqual(new DateTime(2026, 6, 1), m.Peor.Mes);
            Assert.AreEqual(80m, m.Peor.Retencion);
            Assert.AreEqual(Estandar, m.Peor.IdPlan);
        }

        [TestMethod]
        public void PeorCohorte_LasCohortesChicasNoCompiten()
        {
            var d = TresCohortes();
            var chica = d.Alta(new DateTime(2026, 4, 2), Premium, 2);   // 2 clientes: 0 % al mes 3, pero es ruido
            d.Baja(chica[0], new DateTime(2026, 5, 5)).Baja(chica[1], new DateTime(2026, 5, 6));
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.IsFalse(m.Cohortes.Single(c => c.Mes.Month == 4).Comparable);
            Assert.AreEqual(new DateTime(2026, 5, 1), m.Peor.Mes, "La de abril tiene menos de 4 clientes: no puede ser la peor.");

            // Con mínimo 2, abril sí compite y es la peor.
            var conMinimo2 = d.Calcular(new DateTime(2026, 10, 20), minimo: 2);
            Assert.AreEqual(new DateTime(2026, 4, 1), conMinimo2.Peor.Mes);
            Assert.AreEqual(0m, conMinimo2.Peor.Retencion);
            Assert.AreEqual(Premium, conMinimo2.Peor.IdPlan);
        }

        [TestMethod]
        public void PeorCohorte_EmpateDeRetencion_GanaLaDeMasClientes()
        {
            var d = new Datos();
            var a = d.Alta(new DateTime(2026, 5, 3), Basico, 4);
            d.Baja(a[0], new DateTime(2026, 6, 3));                    // 75 %
            var b = d.Alta(new DateTime(2026, 6, 3), Estandar, 8);
            d.Baja(b[0], new DateTime(2026, 7, 3)).Baja(b[1], new DateTime(2026, 7, 4));   // 75 %
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(new DateTime(2026, 6, 1), m.Peor.Mes);
            Assert.AreEqual(Estandar, m.Peor.IdPlan);
        }

        [TestMethod]
        public void PlanDeLaPeor_EmpateDeBajas_GanaElDeMayorProporcionSobreSusAltas()
        {
            var d = new Datos();
            var b = d.Alta(new DateTime(2026, 5, 3), Basico, 2);
            var e = d.Alta(new DateTime(2026, 5, 4), Estandar, 4);
            d.Baja(b[0], new DateTime(2026, 6, 10)).Baja(e[0], new DateTime(2026, 6, 11));
            d.Alta(new DateTime(2026, 6, 2), Premium, 4);
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(Basico, m.Peor.IdPlan, "1 de 2 del Básico (50 %) pesa más que 1 de 4 del Estándar.");
            Assert.AreEqual(1, m.Peor.BajasDelPlan);
        }

        [TestMethod]
        public void PlanDeLaPeor_EsElPlanDelQueSeFue_NoElDelAlta()
        {
            var d = new Datos();
            var ids = d.Alta(new DateTime(2026, 5, 3), Basico, 4);
            d.Baja(ids[0], new DateTime(2026, 6, 10), Premium).Baja(ids[1], new DateTime(2026, 6, 11), Premium);   // cambiaron a Premium y se fueron
            d.Alta(new DateTime(2026, 6, 2), Basico, 4);
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(Premium, m.Peor.IdPlan, "Mismo criterio que ReglaChurnPlan: la baja cuenta en el plan que deja.");
        }

        [TestMethod]
        public void TodasRetienenElCien_NoHayPeorCohorte()
        {
            var d = new Datos();
            d.Alta(new DateTime(2026, 5, 3), Basico, 5);
            d.Alta(new DateTime(2026, 6, 3), Estandar, 5);
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.IsNull(m.Peor);
            Assert.AreEqual("cdc.cohorte.sinpeor.todas", m.MotivoSinPeor.Clave);
            Assert.IsFalse(m.Cohortes.Any(c => c.EsPeor));
        }

        // ── Movimiento: a la recomendación ────────────────────────────────────────

        [TestMethod]
        public void RecomendacionDeLaPeor_LaDeBajasDelPlanPorSuClave()
        {
            var m = TresCohortes().Calcular(new DateTime(2026, 10, 20));
            var recs = new List<BE.Recomendacion>
            {
                new BE.Recomendacion { Clave = "ChurnPlan:" + Estandar },
                new BE.Recomendacion { Clave = "Renovaciones" },
                new BE.Recomendacion { Clave = "ChurnPlan:" + Basico }
            };
            Assert.AreSame(recs[2], AnalisisCohortes.RecomendacionDeLaPeor(m.Peor, recs));
            Assert.IsNull(AnalisisCohortes.RecomendacionDeLaPeor(m.Peor, recs.Take(2)));
            Assert.IsNull(AnalisisCohortes.RecomendacionDeLaPeor(null, recs));
        }

        [TestMethod]
        public void ExplicarSinRecomendacion_DiceElMotivo()
        {
            var peor = new BE.PeorCohorte { IdPlan = Estandar, NombrePlan = "Estándar" };
            var ctx = new BE.ContextoComercial();
            Assert.AreEqual("cdc.cohorte.sinrec.fueraanalisis", AnalisisCohortes.ExplicarSinRecomendacion(peor, ctx).Clave);

            ctx.Planes.Add(new BE.MetricaPlanComercial { IdPlan = Estandar, Nombre = "Estándar", Precio = 1000m, TienePromocionVigente = true, Churn = 20m });
            Assert.AreEqual("cdc.cohorte.sinrec.promovigente", AnalisisCohortes.ExplicarSinRecomendacion(peor, ctx).Clave);

            ctx.Planes[0].TienePromocionVigente = false;
            ctx.Planes[0].Churn = 2m;
            var umbral = AnalisisCohortes.ExplicarSinRecomendacion(peor, ctx);
            Assert.AreEqual("cdc.cohorte.sinrec.umbral", umbral.Clave);
            StringAssert.Contains(umbral.Espanol, "2 %");

            Assert.AreEqual("cdc.cohorte.sinrec.sinplan", AnalisisCohortes.ExplicarSinRecomendacion(new BE.PeorCohorte(), ctx).Clave);
        }

        // ── Servicio ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void Analizar_SinSesion_Rechaza()
        {
            var bll = new AnalisisCohortes(new FakeInteligenciaComercialDAL(), new FakePlanSuscripcionDAL(), () => new DateTime(2026, 10, 20));
            try
            {
                bll.Analizar(new DateTime(2026, 10, 20));
                Assert.Fail("Sin sesión no se puede consultar.");
            }
            catch (BE.AppException) { }
        }

        [TestMethod]
        public void Analizar_LeeDelDal_ConElCorteComoMaximoHoy()
        {
            LoginAdmin();
            var dal = new FakeInteligenciaComercialDAL();
            var d = TresCohortes();
            dal.Altas.AddRange(d.Altas);
            dal.Movimientos.AddRange(d.Movimientos);
            dal.Altas.Add(new BE.AltaSuscripcion { IdCliente = 999, IdPlan = Basico, Fecha = new DateTime(2026, 10, 25) });   // futura al corte
            var planes = new FakePlanSuscripcionDAL
            {
                Planes = new List<BE.PlanSuscripcion> { new BE.PlanSuscripcion { IdPlan = Basico, Nombre = "Básico", Precio = 1000m, Estado = true } }
            };
            var bll = new AnalisisCohortes(dal, planes, () => new DateTime(2026, 10, 20));
            var m = bll.Analizar(new DateTime(2026, 12, 31));   // el período no puede pasar de hoy
            Assert.AreEqual(new DateTime(2026, 10, 20), m.Corte);
            Assert.AreEqual(16, m.TotalClientes, "El alta del 25/10 es posterior al corte.");
            Assert.AreEqual("Básico", m.Peor.NombrePlan);
        }

        // ── Revisión: casos borde ─────────────────────────────────────────────────

        [TestMethod]
        public void Nivel_ElColorSaleDelMismoPorcentajeEnteroQueSeMuestra()
        {
            Assert.AreEqual(90m, AnalisisCohortes.RetencionMostrada(89.5m));
            Assert.AreEqual(BE.NivelRetencion.Alta, AnalisisCohortes.Nivel(89.5m), "Se muestra «90 %»: tramo de 90 % o más.");
            Assert.AreEqual(BE.NivelRetencion.Media, AnalisisCohortes.Nivel(89.4m));
            Assert.AreEqual(BE.NivelRetencion.Media, AnalisisCohortes.Nivel(74.5m), "Se muestra «75 %».");
            Assert.AreEqual(BE.NivelRetencion.Baja, AnalisisCohortes.Nivel(74.4m));
            Assert.AreEqual(BE.NivelRetencion.Baja, AnalisisCohortes.Nivel(49.5m), "Se muestra «50 %».");
            Assert.AreEqual(BE.NivelRetencion.Critica, AnalisisCohortes.Nivel(49.4m));
            Assert.AreEqual(BE.NivelRetencion.Alta, AnalisisCohortes.Nivel(100m));
            Assert.AreEqual(BE.NivelRetencion.Critica, AnalisisCohortes.Nivel(0m));
        }

        [TestMethod]
        public void PeorCohorte_SeComparaLaProporcionExacta_NoElPorcentajeRedondeado()
        {
            var d = new Datos();
            var mayo = d.Alta(new DateTime(2026, 5, 4), Basico, 6);          // 4 de 6 = 66,67 % → se muestra 66,7
            d.Baja(mayo[0], new DateTime(2026, 6, 2)).Baja(mayo[1], new DateTime(2026, 6, 3));
            var junio = d.Alta(new DateTime(2026, 6, 4), Estandar, 1000);    // 667 de 1000 = 66,70 % → también 66,7
            for (int i = 0; i < 333; i++) d.Baja(junio[i], new DateTime(2026, 7, 5));
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(m.Cohortes[0].Celda(3).Retencion, m.Cohortes[1].Celda(3).Retencion, "Redondeadas son iguales.");
            Assert.AreEqual(new DateTime(2026, 5, 1), m.Peor.Mes, "Mayo retiene menos de verdad: no gana el desempate por tamaño.");
        }

        [TestMethod]
        public void PeorCohorte_EmpateDeRetencionYTamanio_GanaLaMasAntigua()
        {
            var d = new Datos();
            var mayo = d.Alta(new DateTime(2026, 5, 4), Basico, 4);
            d.Baja(mayo[0], new DateTime(2026, 6, 2));
            var junio = d.Alta(new DateTime(2026, 6, 4), Estandar, 4);
            d.Baja(junio[0], new DateTime(2026, 7, 2));
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(new DateTime(2026, 5, 1), m.Peor.Mes);
            Assert.AreEqual(Basico, m.Peor.IdPlan);
        }

        [TestMethod]
        public void BajaSinPlanAnterior_BajaLaRetencion_PeroNoSeAtribuyeANingunPlan()
        {
            var d = new Datos();
            var mayo = d.Alta(new DateTime(2026, 5, 4), Basico, 4);
            d.Alta(new DateTime(2026, 6, 4), Basico, 4);
            foreach (var id in mayo.Take(2))
                d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = id, IdPlanAnterior = null, Fecha = new DateTime(2026, 6, 10), Resultado = BE.EstadoRenovacion.Baja });
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(50m, m.Peor.Retencion);
            Assert.AreEqual(2, m.Peor.Bajas);
            Assert.IsNull(m.Peor.IdPlan, "Igual que el % de bajas por plan: sin IdPlanAnterior no cuenta en ningún plan (ni en el del alta).");
            Assert.IsNull(m.Peor.ClaveRecomendacion);
            Assert.AreEqual("cdc.cohorte.peor.sinplan", m.Resumen.Clave);
            Assert.AreEqual(0, m.Cohortes[0].BajasPorPlan.Count);

            // Con una baja más que sí tiene plan, esa define el plan.
            d.Baja(mayo[2], new DateTime(2026, 7, 1));
            var m2 = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(Basico, m2.Peor.IdPlan);
            Assert.AreEqual(1, m2.Peor.BajasDelPlan);
            Assert.AreEqual(3, m2.Peor.Bajas);
        }

        [TestMethod]
        public void Reactivacion_VariasAltasDeUnCliente_CuentaLaPrimeraYLaBajaCortaLaPermanencia()
        {
            var d = new Datos();
            var ids = d.Alta(new DateTime(2026, 5, 4), Basico, 4);
            d.Baja(ids[0], new DateTime(2026, 6, 10));
            d.Altas.Add(new BE.AltaSuscripcion { IdCliente = ids[0], IdPlan = Premium, Fecha = new DateTime(2026, 8, 3) });   // vuelve en agosto
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.AreEqual(1, m.Cohortes.Count, "La segunda alta no arma otra cohorte.");
            Assert.AreEqual(4, m.Cohortes[0].Tamanio);
            Assert.AreEqual(75m, m.Cohortes[0].Celda(4).Retencion, "Después de reactivarse sigue contando como baja.");
        }

        [TestMethod]
        public void BajaEnElLimiteDelMes_A_Las00DelDia1EsDelMesSiguiente_YALas2359DelUltimoDiaEsDeEseMes()
        {
            var d = new Datos();
            var ids = d.Alta(new DateTime(2026, 5, 4), Basico, 4);
            d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = ids[0], IdPlanAnterior = Basico, Fecha = new DateTime(2026, 7, 1, 0, 0, 0), Resultado = BE.EstadoRenovacion.Baja });
            d.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = ids[1], IdPlanAnterior = Basico, Fecha = new DateTime(2026, 6, 30, 23, 59, 0), Resultado = BE.EstadoRenovacion.Baja });
            var f = d.Calcular(new DateTime(2026, 10, 20)).Cohortes[0];
            Assert.AreEqual(4, f.Celda(0).Activos, "Mayo: nadie se fue.");
            Assert.AreEqual(3, f.Celda(1).Activos, "Junio: solo la baja del 30/06 a las 23:59.");
            Assert.AreEqual(2, f.Celda(2).Activos, "Julio: también la del 01/07 a las 00:00.");
        }

        [TestMethod]
        public void Analizar_ConHastaAnteriorAHoy_CortaEnEseDia()
        {
            LoginAdmin();
            var dal = new FakeInteligenciaComercialDAL();
            var d = new Datos();
            var mayo = d.Alta(new DateTime(2026, 5, 4), Basico, 4);
            d.Baja(mayo[0], new DateTime(2026, 9, 10));   // posterior al corte
            d.Alta(new DateTime(2026, 9, 2), Basico, 3);  // posterior al corte
            dal.Altas.AddRange(d.Altas);
            dal.Movimientos.AddRange(d.Movimientos);
            var bll = new AnalisisCohortes(dal, new FakePlanSuscripcionDAL(), () => new DateTime(2026, 10, 20));
            var m = bll.Analizar(new DateTime(2026, 8, 31));
            Assert.AreEqual(new DateTime(2026, 8, 31), m.Corte);
            Assert.AreEqual(1, m.Cohortes.Count);
            Assert.AreEqual(3, m.MaximoDesplazamiento, "Mayo a agosto.");
            Assert.IsFalse(m.HayParciales, "El 31/08 cierra agosto.");
            Assert.IsTrue(m.Cohortes[0].Celdas.All(c => c.Retencion == 100m), "La baja de septiembre es posterior al corte.");
        }

        [TestMethod]
        public void FiltroDePlan_ConBajaDesdeOtroPlan_CuentaEnLaCohorte_YElPlanEsElQueDejo()
        {
            var d = new Datos();
            var mayo = d.Alta(new DateTime(2026, 5, 4), Basico, 4);
            d.Baja(mayo[0], new DateTime(2026, 6, 10), Premium).Baja(mayo[1], new DateTime(2026, 6, 11), Premium);   // pasaron a Premium y se fueron
            d.Alta(new DateTime(2026, 6, 4), Basico, 4);
            d.Alta(new DateTime(2026, 6, 5), Premium, 9);   // otro plan: fuera del filtro
            var m = d.Calcular(new DateTime(2026, 10, 20), Basico);
            Assert.AreEqual(8, m.TotalClientes);
            Assert.AreEqual(50m, m.Peor.Retencion);
            Assert.AreEqual(Premium, m.Peor.IdPlan);

            // Con el Centro filtrado al Básico, Premium no está en el análisis: la pantalla lo explica.
            var ctx = new BE.ContextoComercial();
            ctx.Planes.Add(new BE.MetricaPlanComercial { IdPlan = Basico, Nombre = "Básico", Precio = 1000m, Churn = 30m });
            Assert.AreEqual("cdc.cohorte.sinrec.fueraanalisis", AnalisisCohortes.ExplicarSinRecomendacion(m.Peor, ctx).Clave);
        }

        [TestMethod]
        public void MesComparacionMayorQueLaHistoria_UsaElUltimoMesComun()
        {
            var d = new Datos();
            var mayo = d.Alta(new DateTime(2026, 5, 4), Basico, 4);
            d.Baja(mayo[0], new DateTime(2026, 6, 10));
            d.Alta(new DateTime(2026, 6, 4), Estandar, 4);
            var m = AnalisisCohortes.Calcular(d.Altas, d.Movimientos, new DateTime(2026, 8, 31), null, Nombres, mesComparacion: 10);
            Assert.AreEqual(3, m.MaximoDesplazamiento);
            Assert.AreEqual(2, m.Peor.Desplazamiento, "Mayo completó el mes 3 pero junio no: el último mes común es el 2.");
            Assert.AreEqual(new DateTime(2026, 5, 1), m.Peor.Mes);
        }

        [TestMethod]
        public void SoloCohortesEnCurso_NoHayMesesCompletos()
        {
            var d = new Datos();
            d.Alta(new DateTime(2026, 10, 2), Basico, 5);
            var m = d.Calcular(new DateTime(2026, 10, 20));
            Assert.IsFalse(m.SinDatos);
            Assert.IsTrue(m.SinMesesCompletos, "El gráfico y el informe dicen que todavía no hay meses completos (no «no hay altas»).");
            Assert.IsNull(m.Peor);

            var conMesCompleto = d.Calcular(new DateTime(2026, 10, 31));
            Assert.IsFalse(conMesCompleto.SinMesesCompletos);
        }

        [TestMethod]
        public void ExplicarSinRecomendacion_ReglaConErrorOPlanSinClientesAlInicio_TienenSuPropioMotivo()
        {
            var peor = new BE.PeorCohorte { IdPlan = Basico, NombrePlan = "Básico" };
            var ctx = new BE.ContextoComercial();
            ctx.Planes.Add(new BE.MetricaPlanComercial { IdPlan = Basico, Nombre = "Básico", Precio = 1000m, Churn = 2m });

            Assert.AreEqual("cdc.cohorte.sinrec.errorregla",
                AnalisisCohortes.ExplicarSinRecomendacion(peor, ctx, new[] { "Escasez", BLL.Inteligencia.Reglas.ReglaChurnPlan.Codigo }).Clave,
                "Si la regla falló, no se puede afirmar que el % de bajas no llega al umbral.");
            Assert.AreEqual("cdc.cohorte.sinrec.umbral", AnalisisCohortes.ExplicarSinRecomendacion(peor, ctx, new[] { "Escasez" }).Clave);

            ctx.Planes[0].Churn = null;   // sin clientes al inicio del período
            Assert.AreEqual("cdc.cohorte.sinrec.sinclientes", AnalisisCohortes.ExplicarSinRecomendacion(peor, ctx).Clave);
        }

        // ── Datos de la demo (dbo.RestablecerDatosDemo) ───────────────────────────

        // Los clientes con plan de la demo: (plan del alta, días desde el alta, días desde la baja).
        // Copia de la tabla #Cli de BD/00_Instalacion_Completa.sql (sección 21z3b): si la demo cambia, actualizar acá.
        private static readonly (int Plan, int Alta, int? Baja)[] ClientesDemo =
        {
            (Premium, 120, null), (Estandar, 90, null), (Basico, 60, null), (Premium, 75, null), (Estandar, 150, null),
            (Estandar, 200, null), (Basico, 100, null), (Estandar, 115, null), (Estandar, 85, null), (Basico, 60, null),
            (Basico, 100, null), (Premium, 80, null), (Basico, 150, 40), (Estandar, 110, null), (Premium, 130, null),
            (Estandar, 95, null), (Basico, 70, null), (Premium, 45, null), (Premium, 185, null), (Estandar, 182, null),
            (Estandar, 170, null), (Basico, 165, 100), (Premium, 150, null), (Estandar, 140, null), (Basico, 132, null),
            (Estandar, 125, 70), (Premium, 118, null), (Basico, 105, 25), (Estandar, 95, null), (Basico, 90, 12),
            (Premium, 75, 15), (Basico, 65, null), (Basico, 55, 8), (Estandar, 50, null), (Premium, 40, null),
            (Basico, 30, null), (Basico, 170, 120), (Estandar, 100, null), (Basico, 60, 20), (Basico, 20, null),
            (Premium, 12, null), (Estandar, 8, null)
        };

        private static BE.MatrizCohortes Demo(DateTime hoy)
        {
            var d = new Datos();
            foreach (var c in ClientesDemo)
            {
                int id = d.Alta(hoy.AddDays(-c.Alta), c.Plan)[0];
                if (c.Baja.HasValue) d.Baja(id, hoy.AddDays(-c.Baja.Value));
            }
            return d.Calcular(hoy);
        }

        [TestMethod]
        public void Demo_MatrizConVariasCohortes_YLaPeorLaExplicaElBasico()
        {
            var m = Demo(new DateTime(2026, 10, 20));
            Assert.IsTrue(m.Cohortes.Count >= 6, "La demo trae 6 meses de historia: hay varias cohortes.");
            Assert.AreEqual(42, m.TotalClientes);
            Assert.IsTrue(m.Cohortes.Where(c => c.Celda(1) != null && !c.Celda(1).EsParcial)
                                    .Select(c => c.Celda(1).Retencion).Distinct().Count() >= 3,
                "Las cohortes retienen distinto.");
            Assert.IsNotNull(m.Peor);
            Assert.AreEqual(new DateTime(2026, 5, 1), m.Peor.Mes);
            Assert.AreEqual(Basico, m.Peor.IdPlan, "El Básico concentra las bajas de la demo y es el plan con la recomendación de bajas.");
            Assert.IsTrue(m.Peor.Retencion < m.Peor.RetencionPromedio, "Es claramente peor que el promedio.");
        }

        [TestMethod]
        public void Demo_CualquierDiaDelAnio_HayPeorCohorte_YCasiSiempreLaExplicaElBasico()
        {
            // La demo se restablece con fechas relativas al día en que se carga: la matriz cambia según el día.
            int dias = 0, basico = 0;
            for (var hoy = new DateTime(2026, 1, 1); hoy < new DateTime(2027, 1, 1); hoy = hoy.AddDays(1), dias++)
            {
                var m = Demo(hoy);
                Assert.IsNotNull(m.Peor, $"Sin peor cohorte el {hoy:d}: {m.MotivoSinPeor?.Espanol}");
                if (m.Peor.IdPlan == Basico) basico++;
            }
            Assert.IsTrue(basico >= dias * 9 / 10, $"El Básico explica la peor cohorte {basico} de {dias} días (se esperaba al menos el 90 %).");
        }
    }
}
