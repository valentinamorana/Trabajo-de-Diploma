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
    /// PN03 «Aplicar» — cada recomendación aceptada hace algo: qué patente exige cada acción, qué prendas
    /// admiten la baja o la revisión (y se cambian con la operación existente), cómo la decisión guarda a
    /// quiénes contactó y cómo se mide después la campaña. También los avisos al iniciar sesión.
    /// </summary>
    [TestClass]
    public class AccionesRecomendacionTests
    {
        private static readonly DateTime Hoy = new DateTime(2026, 10, 20);

        [TestInitialize] public void Setup() { SessionManager.Logout(); ConfiguracionTests.BitacoraNegocio.Eventos.Clear(); }
        [TestCleanup] public void Cleanup() => SessionManager.Logout();

        private static void LoginAdmin() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        // ══ Permisos y destinos ══════════════════════════════════════════════════

        [TestMethod]
        public void PatenteRequerida_CadaAccionPideLaDeSuOperacion()
        {
            Assert.AreEqual(BE.Patentes.SugerenciaPromocion, AccionesRecomendacion.PatenteRequerida(BE.AccionRecomendacion.SugerirPromocion));
            Assert.AreEqual(BE.Patentes.SugerenciaPromocion, AccionesRecomendacion.PatenteRequerida(BE.AccionRecomendacion.ExtenderPromocion));
            Assert.AreEqual(BE.Patentes.PromocionesVigentesEditar, AccionesRecomendacion.PatenteRequerida(BE.AccionRecomendacion.SolicitarBajaPromocion));
            Assert.AreEqual(BE.Patentes.StockEditar, AccionesRecomendacion.PatenteRequerida(BE.AccionRecomendacion.DarDeBajaPrendas));
            Assert.AreEqual(BE.Patentes.StockEditar, AccionesRecomendacion.PatenteRequerida(BE.AccionRecomendacion.EnviarAMantenimiento));
            Assert.IsNull(AccionesRecomendacion.PatenteRequerida(BE.AccionRecomendacion.ExportarContactos));
            Assert.IsNull(AccionesRecomendacion.PatenteRequerida(BE.AccionRecomendacion.OrdenCompra));
            foreach (BE.AccionRecomendacion a in Enum.GetValues(typeof(BE.AccionRecomendacion)))
                Assert.AreEqual(a == BE.AccionRecomendacion.DarDeBajaPrendas || a == BE.AccionRecomendacion.EnviarAMantenimiento,
                    AccionesRecomendacion.TocaPrendas(a), a.ToString());
        }

        [TestMethod]
        public void PuedeEjecutar_SinLaPatenteNoSeAcepta()
        {
            var baja = new BE.OpcionRecomendacion { Accion = BE.AccionRecomendacion.DarDeBajaPrendas };
            Assert.IsFalse(AccionesRecomendacion.PuedeEjecutar(baja, p => false));
            Assert.IsTrue(AccionesRecomendacion.PuedeEjecutar(baja, p => p == BE.Patentes.StockEditar));
            Assert.IsTrue(AccionesRecomendacion.PuedeEjecutar(new BE.OpcionRecomendacion { Accion = BE.AccionRecomendacion.ExportarContactos }, p => false));
            Assert.IsFalse(AccionesRecomendacion.PuedeEjecutar(null, p => true));
        }

        // ══ Prendas: baja y revisión con la operación existente ═════════════════

        // Doble completo de IPrendaService: transiciones manuales como BLL.Prenda (Disponible → EnLimpieza/Baja).
        private sealed class CatalogoPrendas : BLL.Interfaces.IPrendaService
        {
            public List<BE.Prenda> Catalogo = new List<BE.Prenda>();
            public List<(int Id, BE.EstadoPrenda Destino)> Cambios = new List<(int, BE.EstadoPrenda)>();
            public HashSet<int> Fallan = new HashSet<int>();
            public List<BE.Prenda> ObtenerTodos() => Catalogo;
            public List<BE.EstadoPrenda> ObtenerTransicionesManuales(BE.Prenda p) =>
                p.Estado == BE.EstadoPrenda.Disponible ? new List<BE.EstadoPrenda> { BE.EstadoPrenda.EnLimpieza, BE.EstadoPrenda.Baja }
                : p.Estado == BE.EstadoPrenda.EnLimpieza ? new List<BE.EstadoPrenda> { BE.EstadoPrenda.Disponible, BE.EstadoPrenda.Baja }
                : new List<BE.EstadoPrenda>();
            public void CambiarEstado(string modulo, BE.Prenda prenda, BE.EstadoPrenda nuevoEstado)
            {
                if (Fallan.Contains(prenda.IdPrenda))
                    throw new BE.AppException("err.bll.prenda.transicion_generica", "La transición no está permitida.");
                Cambios.Add((prenda.IdPrenda, nuevoEstado));
                prenda.Estado = nuevoEstado;
            }
            public List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null) => new List<BE.Prenda>();
            public List<BE.Prenda> ObtenerPorCliente(int idCliente) => new List<BE.Prenda>();
            public void Alta(string modulo, BE.Prenda prenda) { }
            public void Modificar(string modulo, BE.Prenda prenda) { }
            public void ReingresarInspeccionada(string modulo, BE.Prenda prenda) { }
            public bool EstaPendienteDeInspeccion(BE.Prenda prenda) => false;
            public (bool Disponible, List<BE.Prenda> NoDisponibles) VerificarDisponibilidad(List<BE.Prenda> s) => (true, new List<BE.Prenda>());
            public List<BE.MantenimientoPrenda> ObtenerHistorialMantenimiento(int idPrenda) => new List<BE.MantenimientoPrenda>();
            public List<BE.MantenimientoPrenda> ObtenerEnMantenimiento() => new List<BE.MantenimientoPrenda>();
            public BE.OcupacionStock ObtenerOcupacion() => new BE.OcupacionStock();
            public List<BE.Prenda> ObtenerEnLimpieza() => new List<BE.Prenda>();
        }

        private static CatalogoPrendas Catalogo() => new CatalogoPrendas
        {
            Catalogo =
            {
                new BE.Prenda { IdPrenda = 1, Nombre = "Vestido", Estado = BE.EstadoPrenda.Disponible },
                new BE.Prenda { IdPrenda = 2, Nombre = "Saco", Estado = BE.EstadoPrenda.Disponible },
                new BE.Prenda { IdPrenda = 3, Nombre = "Blazer", Estado = BE.EstadoPrenda.EnUso },
                new BE.Prenda { IdPrenda = 4, Nombre = "Viejo", Estado = BE.EstadoPrenda.Baja },
                new BE.Prenda { IdPrenda = 5, Nombre = "Falda", Estado = BE.EstadoPrenda.EnLimpieza }
            }
        };

        [TestMethod]
        public void Planificar_SeparaLasQueAdmitenElCambioDeLasQueNo()
        {
            var plan = new AccionesPrendas(Catalogo()).Planificar(BE.AccionRecomendacion.DarDeBajaPrendas, new[] { 1, 2, 3, 4, 5, 99, 1 });
            Assert.AreEqual(BE.EstadoPrenda.Baja, plan.Destino);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 5 }, plan.Aplicables.Select(p => p.IdPrenda).ToList());
            CollectionAssert.AreEquivalent(new[] { 3, 4 }, plan.NoAplicables.Select(p => p.IdPrenda).ToList(), "En uso y ya de baja.");
            CollectionAssert.AreEqual(new[] { 99 }, plan.NoEncontradas);

            var revision = new AccionesPrendas(Catalogo()).Planificar(BE.AccionRecomendacion.EnviarAMantenimiento, new[] { 1, 5 });
            Assert.AreEqual(BE.EstadoPrenda.EnLimpieza, revision.Destino);
            CollectionAssert.AreEqual(new[] { 1 }, revision.Aplicables.Select(p => p.IdPrenda).ToList(), "La que ya está en limpieza no se vuelve a mandar.");
        }

        [TestMethod]
        public void Planificar_AccionQueNoTocaPrendas_Rechaza()
        {
            try { new AccionesPrendas(Catalogo()).Planificar(BE.AccionRecomendacion.ExportarContactos, new[] { 1 }); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.accion.noprendas", ex.Clave); }
        }

        [TestMethod]
        public void Aplicar_CambiaCadaPrendaConLaOperacionExistente_YUnaQueFallaNoCortaLasDemas()
        {
            LoginAdmin();
            var catalogo = Catalogo();
            catalogo.Fallan.Add(2);
            var acciones = new AccionesPrendas(catalogo);
            var r = acciones.Aplicar("Test", acciones.Planificar(BE.AccionRecomendacion.DarDeBajaPrendas, new[] { 1, 2, 3 }));
            CollectionAssert.AreEqual(new[] { 1 }, r.Aplicadas);
            Assert.AreEqual(2, r.Fallidas.Single().Prenda.IdPrenda);
            CollectionAssert.AreEqual(new[] { (1, BE.EstadoPrenda.Baja) }, catalogo.Cambios);
        }

        [TestMethod]
        public void Aplicar_SinLaPatenteDeStock_NoTocaNinguna()
        {
            SessionManager.Login(new BE.Usuario { Id = 3, Username = "gcomercial", Perfil = "GerenteComercial",
                Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = BE.Patentes.SugerenciaPromocion } } });
            var catalogo = Catalogo();
            var acciones = new AccionesPrendas(catalogo);
            var plan = acciones.Planificar(BE.AccionRecomendacion.DarDeBajaPrendas, new[] { 1, 2 });
            try { acciones.Aplicar("Test", plan); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
            Assert.AreEqual(0, catalogo.Cambios.Count);
        }

        // ══ Decisión ejecutada con detalle y medición de la campaña ══════════════

        private static BE.Recomendacion RecRenovaciones() => new BE.Recomendacion
        {
            TipoRegla = ReglaRenovaciones.Codigo, Clave = ReglaRenovaciones.Codigo, Prioridad = BE.PrioridadRecomendacion.Alta,
            Titulo = new BE.TextoTraducible("t", "Gestionar la renovación"),
            Principal = new BE.OpcionRecomendacion { Descripcion = new BE.TextoTraducible("o", "Campaña"), ImpactoEstimado = 1800m,
                                                     Accion = BE.AccionRecomendacion.ExportarContactos }
        };

        [TestMethod]
        public void MarcarEjecutadaConDetalle_GuardaLosContactadosYQuedaEjecutada()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = new DecisionesComerciales(dal, () => Hoy);
            var d = svc.Aceptar("Test", RecRenovaciones(), 0);
            svc.MarcarEjecutadaConDetalle("Test", d.IdDecision, BE.DecisionComercial.DetalleCliente, new[] { 7, 8, 7 });
            Assert.AreEqual(BE.EstadoDecision.Ejecutada, dal.Filas.Single().Estado);
            CollectionAssert.AreEqual(new[] { 7, 8 }, svc.ObtenerDetalle(new[] { d.IdDecision }, BE.DecisionComercial.DetalleCliente)[d.IdDecision]);
            Assert.AreEqual(0, svc.ObtenerDetalle(new[] { d.IdDecision }, BE.DecisionComercial.DetallePrenda).Count);
        }

        private static BE.DecisionComercial Ejecutada(string regla, DateTime fecha, int id = 1) => new BE.DecisionComercial
        {
            IdDecision = id, TipoRegla = regla, Fecha = fecha.AddDays(-90), FechaEjecucion = fecha, Estado = BE.EstadoDecision.Ejecutada
        };

        private static BE.MovimientoSuscripcion Mov(int cliente, DateTime fecha, BE.EstadoRenovacion r, int plan = 1, int? nuevo = null) =>
            new BE.MovimientoSuscripcion { IdCliente = cliente, Fecha = fecha, Resultado = r, IdPlanAnterior = plan, IdPlanNuevo = nuevo };

        private static readonly Dictionary<int, decimal> Precios = new Dictionary<int, decimal> { [1] = 1000m, [2] = 1500m };
        private static readonly Dictionary<int, int?> Plan1 = Enumerable.Range(1, 20).ToDictionary(i => i, i => (int?)1);

        [TestMethod]
        public void MedirCampania_Renovaciones_IncrementalSobreLaTasaBaseEnLaVentanaDesdeLaEjecucion()
        {
            var d = Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-40));          // creada hace 130 días, ejecutada hace 40
            var movimientos = new List<BE.MovimientoSuscripcion>
            {
                Mov(1, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada),
                Mov(1, Hoy.AddDays(-32), BE.EstadoRenovacion.Renovada),          // solo cuenta el primero
                Mov(2, Hoy.AddDays(-33), BE.EstadoRenovacion.CambioPlan, 1, 2),
                Mov(3, Hoy.AddDays(-34), BE.EstadoRenovacion.Baja),              // no renovó
                Mov(4, Hoy.AddDays(-5), BE.EstadoRenovacion.Renovada),           // fuera de la ventana de 30 días
                Mov(9, Hoy.AddDays(-36), BE.EstadoRenovacion.Renovada),          // no fue contactado
                // Ventana anterior (base): en el plan 1, 1 de 4 renovaciones resueltas fue renovación → 25 %.
                Mov(50, Hoy.AddDays(-45), BE.EstadoRenovacion.Renovada),
                Mov(51, Hoy.AddDays(-46), BE.EstadoRenovacion.Baja),
                Mov(52, Hoy.AddDays(-47), BE.EstadoRenovacion.Baja),
                Mov(53, Hoy.AddDays(-48), BE.EstadoRenovacion.Baja)
            };
            var r = MedicionCampanias.Medir(d, new[] { 1, 2, 3, 4 }, movimientos, Precios, Plan1, 6m, Hoy);
            Assert.IsFalse(r.EnMedicion, "La ventana de 30 días ya cerró.");
            Assert.AreEqual(2, r.Respondieron);
            Assert.AreEqual(3, r.Comparables, "1, 2 y 3 resolvieron en la ventana; el 4 no tuvo movimiento en ella.");
            Assert.AreEqual(0.25m, r.TasaBase);
            Assert.AreEqual(1250m, r.ValorPorRespuesta, "(1000 + 1500) / 2, con el precio actual del plan.");
            // (2 − 25 % × 3 comparables) × 1250 = 1562,50: solo lo que superó a las renovaciones de rutina.
            Assert.AreEqual(1562.5m, r.Valor);
        }

        [TestMethod]
        public void MedirCampania_VentanaAbierta_EstaEnMedicion_YSinContactadosNoHayMedicion()
        {
            var reciente = Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-5));
            var r = MedicionCampanias.Medir(reciente, new[] { 1 }, new BE.MovimientoSuscripcion[0], Precios, Plan1, 6m, Hoy);
            Assert.IsTrue(r.EnMedicion);
            Assert.IsNull(r.Valor, "Con la ventana abierta no hay importe.");
            Assert.IsNull(MedicionCampanias.Medir(reciente, new int[0], new BE.MovimientoSuscripcion[0], Precios, Plan1, 6m, Hoy), "Sin contactados no hay campaña.");
            var descartada = Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-40)); descartada.Estado = BE.EstadoDecision.Descartada;
            Assert.IsNull(MedicionCampanias.Medir(descartada, new[] { 1 }, new BE.MovimientoSuscripcion[0], Precios, Plan1, 6m, Hoy));
        }

        [TestMethod]
        public void MedirCampania_Churn_PorHorizonte_YUpgrade_CambioAUnPlanMasCaroAunqueAntesRenovo()
        {
            var movimientos = new[]
            {
                Mov(1, Hoy.AddDays(-5), BE.EstadoRenovacion.Renovada),
                Mov(2, Hoy.AddDays(-3), BE.EstadoRenovacion.CambioPlan, 1, 2),
                Mov(3, Hoy.AddDays(-8), BE.EstadoRenovacion.Renovada),
                Mov(3, Hoy.AddDays(-2), BE.EstadoRenovacion.CambioPlan, 1, 2)    // renovó y después subió de plan
            };
            // Se miden el día en que cierra la ventana (ejecutadas hace 10 días: dentro de 20).
            var cierre = Hoy.AddDays(20);
            // Bajas: (3 que se quedaron − 0) × precio promedio × horizonte = 3 × (1000 + 1500 + 1000) / 3 × 6.
            Assert.AreEqual(21000m, MedicionCampanias.Medir(Ejecutada(ReglaChurnPlan.Codigo, Hoy.AddDays(-10)), new[] { 1, 2, 3 }, movimientos, Precios, Plan1, 6m, cierre).Valor);
            // Upgrade: los clientes 2 y 3 pasaron a un plan más caro (el 3 aunque antes renovó): 2 × 500 × 6.
            var up = MedicionCampanias.Medir(Ejecutada(ReglaUpgradePlan.Codigo, Hoy.AddDays(-10)), new[] { 1, 2, 3 }, movimientos, Precios, Plan1, 6m, cierre);
            Assert.AreEqual(2, up.Respondieron);
            Assert.AreEqual(6000m, up.Valor);
        }

        [TestMethod]
        public void Atribuir_CadaClienteALaPrimeraCampaniaQueLoContacto()
        {
            var primera = Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-20), id: 1);
            var segunda = Ejecutada(ReglaUpgradePlan.Codigo, Hoy.AddDays(-10), id: 2);
            var a = MedicionCampanias.Atribuir(new[] { segunda, primera },
                new Dictionary<int, List<int>> { [1] = new List<int> { 1, 2 }, [2] = new List<int> { 2, 3 } });
            CollectionAssert.AreEqual(new[] { 1, 2 }, a[1]);
            CollectionAssert.AreEqual(new[] { 3 }, a[2], "El cliente 2 ya lo cuenta la primera campaña.");
        }

        // ══ Ventana de medición: sin importe hasta que cierra ════════════════════

        private static readonly int[] Seis = { 1, 2, 3, 4, 5, 6 };

        // Seis contactados del plan 1; en los 30 días anteriores a la ejecución, 1 de las 2 renovaciones
        // resueltas del plan fue renovación: tasa base 50 %.
        private static List<BE.MovimientoSuscripcion> ConBase(DateTime ejecucion, params BE.MovimientoSuscripcion[] respuestas)
        {
            var l = new List<BE.MovimientoSuscripcion>
            {
                Mov(50, ejecucion.AddDays(-10), BE.EstadoRenovacion.Renovada),
                Mov(51, ejecucion.AddDays(-12), BE.EstadoRenovacion.Baja)
            };
            l.AddRange(respuestas);
            return l;
        }

        [TestMethod]
        public void MedirCampania_VentanaAbierta_SinImporte_ConElAvanceYLaFechaDeCierre()
        {
            // Ejecutada hoy: la fórmula daría (0 − 50 % × 6) × 1000 = −3000 antes de que nadie pudiera responder.
            var r = MedicionCampanias.Medir(Ejecutada(ReglaRenovaciones.Codigo, Hoy), Seis, ConBase(Hoy), Precios, Plan1, 6m, Hoy);
            Assert.IsTrue(r.EnMedicion);
            Assert.IsNull(r.Valor, "Con la ventana abierta no hay importe (ni negativo ni cero).");
            Assert.AreEqual(0, r.Respondieron);
            Assert.AreEqual(6, r.Contactados);
            Assert.AreEqual(Hoy.AddDays(30), r.Hasta);
            var m = r.ComoMedicion();
            Assert.AreEqual(BE.EstadoMedicion.EnMedicion, m.Estado);
            Assert.IsNull(m.Valor);
            Assert.AreEqual(Hoy.AddDays(30), m.FechaCierre);

            // A los 10 días respondieron 2 de 6: sigue sin importe, con el avance y el día en que cierra.
            var movs = ConBase(Hoy.AddDays(-10), Mov(1, Hoy.AddDays(-8), BE.EstadoRenovacion.Renovada),
                                                 Mov(2, Hoy.AddDays(-1), BE.EstadoRenovacion.CambioPlan, 1, 2));
            var avance = MedicionCampanias.Medir(Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-10)), Seis, movs, Precios, Plan1, 6m, Hoy).ComoMedicion();
            Assert.AreEqual(BE.EstadoMedicion.EnMedicion, avance.Estado);
            Assert.IsNull(avance.Valor);
            Assert.AreEqual(2, avance.Respondieron);
            Assert.AreEqual(6, avance.Contactados);
            Assert.AreEqual(Hoy.AddDays(20), avance.FechaCierre);
            Assert.IsFalse(avance.EsFinal);
        }

        [TestMethod]
        public void MedirCampania_VentanaCerrada_MuestraElImporte_PositivoONegativo()
        {
            var ejecutada = Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-40));
            // 5 renovaron y el 6 no tuvo movimiento: (5 − 50 % × 5 comparables) × 1000 = 2500.
            var bien = ConBase(Hoy.AddDays(-40), Enumerable.Range(1, 5).Select(c => Mov(c, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada)).ToArray());
            var r = MedicionCampanias.Medir(ejecutada, Seis, bien, Precios, Plan1, 6m, Hoy);
            Assert.IsFalse(r.EnMedicion);
            Assert.AreEqual(5, r.Comparables);
            Assert.AreEqual(2500m, r.Valor);
            Assert.AreEqual(BE.EstadoMedicion.Medido, r.ComoMedicion().Estado);
            Assert.AreEqual(2500m, r.ComoMedicion().Valor);
            Assert.IsTrue(r.ComoMedicion().EsFinal);

            // 1 renovó y 5 se dieron de baja: (1 − 50 % × 6) × 1000 = −2000. Quedó debajo de la rutina y se muestra igual.
            var mal = ConBase(Hoy.AddDays(-40), new[] { Mov(1, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada) }
                .Concat(Enumerable.Range(2, 5).Select(c => Mov(c, Hoy.AddDays(-34), BE.EstadoRenovacion.Baja))).ToArray());
            var n = MedicionCampanias.Medir(ejecutada, Seis, mal, Precios, Plan1, 6m, Hoy).ComoMedicion();
            Assert.AreEqual(BE.EstadoMedicion.Medido, n.Estado);
            Assert.AreEqual(-2000m, n.Valor);
        }

        [TestMethod]
        public void MedirCampania_ElDia30CierraLaVentana_YLoQuePasaEseDiaNoCuenta()
        {
            var movs = ConBase(Hoy.AddDays(-30), Mov(1, Hoy.AddDays(-1).AddHours(23), BE.EstadoRenovacion.Renovada),   // último día de la ventana
                                                 Mov(2, Hoy.AddHours(9), BE.EstadoRenovacion.Renovada));                 // el día del cierre: afuera
            // Ejecutada hace exactamente 30 días: hoy cierra y ya hay importe: (1 − 50 % × 1 comparable) × 1000
            // (la renovación del cliente 2 es del día del cierre: no cuenta, ni como respuesta ni como comparable).
            var r = MedicionCampanias.Medir(Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-30)), Seis, movs, Precios, Plan1, 6m, Hoy);
            Assert.IsFalse(r.EnMedicion);
            Assert.AreEqual(Hoy, r.Hasta);
            Assert.AreEqual(1, r.Respondieron);
            Assert.AreEqual(1, r.Comparables);
            Assert.AreEqual(500m, r.Valor);

            // Con la hora de la ejecución (15:00) también cierra al empezar el día 30, a cualquier hora de hoy.
            var conHora = MedicionCampanias.Medir(Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-30).AddHours(15)), Seis, movs, Precios, Plan1, 6m, Hoy.AddHours(8));
            Assert.IsFalse(conHora.EnMedicion);
            Assert.AreEqual(Hoy, conHora.Hasta);
            Assert.AreEqual(1, conHora.Respondieron);

            // Ejecutada hace 29 días: todavía en medición, cierra mañana (y la renovación de hoy sí cuenta).
            var dia29 = MedicionCampanias.Medir(Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-29)), Seis, movs, Precios, Plan1, 6m, Hoy);
            Assert.IsTrue(dia29.EnMedicion);
            Assert.IsNull(dia29.Valor);
            Assert.AreEqual(2, dia29.Respondieron);
            Assert.AreEqual(Hoy.AddDays(1), dia29.ComoMedicion().FechaCierre);
        }

        [TestMethod]
        public void MedirCampania_SinFechaEjecucion_MideDesdeLaCreacion()
        {
            BE.DecisionComercial Anterior(DateTime fecha) => new BE.DecisionComercial
            {
                IdDecision = 1, TipoRegla = ReglaRenovaciones.Codigo, Fecha = fecha, FechaEjecucion = null, Estado = BE.EstadoDecision.Ejecutada
            };
            var vieja = MedicionCampanias.Medir(Anterior(Hoy.AddDays(-40)), Seis,
                ConBase(Hoy.AddDays(-40), Mov(1, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada)), Precios, Plan1, 6m, Hoy);
            Assert.AreEqual(Hoy.AddDays(-40), vieja.Desde, "Sin FechaEjecucion, la ventana empieza en la fecha de creación.");
            Assert.IsFalse(vieja.EnMedicion);
            Assert.AreEqual(500m, vieja.Valor, "(1 − 50 % × 1 comparable) × 1000.");

            var reciente = MedicionCampanias.Medir(Anterior(Hoy.AddDays(-10)), Seis, ConBase(Hoy.AddDays(-10)), Precios, Plan1, 6m, Hoy).ComoMedicion();
            Assert.AreEqual(BE.EstadoMedicion.EnMedicion, reciente.Estado);
            Assert.IsNull(reciente.Valor);
            Assert.AreEqual(Hoy.AddDays(20), reciente.FechaCierre);
        }

        // ══ Población comparable: el mismo denominador en la base y en la campaña ══

        [TestMethod]
        public void MedirCampania_EjemploAuditoria_LosContactadosSinMovimientoNoLaVuelvenNegativa()
        {
            var ejecutada = Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-40));
            var movs = new List<BE.MovimientoSuscripcion>();
            // Ventana anterior: 9 de 10 renovaciones resueltas del plan fueron renovación → tasa base 90 %.
            movs.AddRange(Enumerable.Range(50, 9).Select(c => Mov(c, Hoy.AddDays(-50), BE.EstadoRenovacion.Renovada)));
            movs.Add(Mov(59, Hoy.AddDays(-55), BE.EstadoRenovacion.Baja));
            // Campaña: 10 contactados; 6 renovaron y 4 no tuvieron ningún movimiento en la ventana.
            movs.AddRange(Enumerable.Range(1, 6).Select(c => Mov(c, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada)));

            var r = MedicionCampanias.Medir(ejecutada, Enumerable.Range(1, 10), movs, Precios, Plan1, 6m, Hoy);
            Assert.IsFalse(r.EnMedicion);
            Assert.AreEqual(10, r.Contactados);
            Assert.AreEqual(6, r.Comparables, "Los 4 sin movimiento no tienen resultado que comparar.");
            Assert.AreEqual(6, r.Respondieron);
            Assert.AreEqual(0.9m, r.TasaBase);
            // (6 − 90 % × 6) × 1000 = 600: renovó el 100 % de los comparables contra el 90 % de rutina.
            // Con el denominador anterior (todos los contactados) daba (6 − 90 % × 10) × 1000 = −3000.
            Assert.AreEqual(600m, r.Valor);
        }

        [TestMethod]
        public void MedirCampania_IgualALaRutinaDaCero_YConBajasRealesDaNegativo()
        {
            var ejecutada = Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-40));
            // Tasa base 50 % (ConBase). 2 renovaron, 2 se dieron de baja y 2 sin movimiento: 50 % de los
            // comparables, igual que la rutina → (2 − 50 % × 4) × 1000 = 0.
            var igual = ConBase(Hoy.AddDays(-40),
                Mov(1, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada), Mov(2, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada),
                Mov(3, Hoy.AddDays(-34), BE.EstadoRenovacion.Baja), Mov(4, Hoy.AddDays(-34), BE.EstadoRenovacion.Baja));
            var r = MedicionCampanias.Medir(ejecutada, Seis, igual, Precios, Plan1, 6m, Hoy);
            Assert.AreEqual(4, r.Comparables);
            Assert.AreEqual(0m, r.Valor);

            // Si los otros 2 también se dan de baja, la campaña quedó debajo de la rutina: (2 − 50 % × 6) × 1000.
            var peor = ConBase(Hoy.AddDays(-40), igual.Skip(2).Concat(new[]
                { Mov(5, Hoy.AddDays(-33), BE.EstadoRenovacion.Baja), Mov(6, Hoy.AddDays(-33), BE.EstadoRenovacion.Baja) }).ToArray());
            var n = MedicionCampanias.Medir(ejecutada, Seis, peor, Precios, Plan1, 6m, Hoy);
            Assert.AreEqual(6, n.Comparables);
            Assert.AreEqual(-1000m, n.Valor);
        }

        [TestMethod]
        public void MedirCampania_LaTasaBaseEsPorCliente_ConElPrimerMovimientoResuelto()
        {
            // En la ventana anterior, el cliente 50 renovó dos veces y el 51 se dio de baja: 1 de 2 clientes
            // (no 2 de 3 movimientos). La pausa del 52 no es una renovación resuelta: no es comparable.
            var movs = new List<BE.MovimientoSuscripcion>
            {
                Mov(50, Hoy.AddDays(-60), BE.EstadoRenovacion.Renovada),
                Mov(50, Hoy.AddDays(-45), BE.EstadoRenovacion.Renovada),
                Mov(51, Hoy.AddDays(-50), BE.EstadoRenovacion.Baja),
                Mov(52, Hoy.AddDays(-50), BE.EstadoRenovacion.Pausada),
                Mov(60, Hoy.AddDays(-50), BE.EstadoRenovacion.Renovada, plan: 2),   // otro plan: no cuenta
                Mov(1, Hoy.AddDays(-35), BE.EstadoRenovacion.Renovada)
            };
            var r = MedicionCampanias.Medir(Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-40)), new[] { 1 }, movs, Precios, Plan1, 6m, Hoy);
            Assert.AreEqual(0.5m, r.TasaBase);
            Assert.AreEqual(500m, r.Valor, "(1 − 50 % × 1) × 1000.");
        }

        [TestMethod]
        public void MedirDecisiones_LaAtribucionNoDependeDelFiltroDelHistorial()
        {
            LoginAdmin();
            var dalDec = new FakeDecisionComercialDAL();
            dalDec.Filas.Add(Ejecutada(ReglaRenovaciones.Codigo, Hoy.AddDays(-20), id: 1));
            dalDec.Filas.Add(Ejecutada(ReglaUpgradePlan.Codigo, Hoy.AddDays(-10), id: 2));
            dalDec.Detalle[(1, BE.DecisionComercial.DetalleCliente)] = new List<int> { 7, 8 };
            dalDec.Detalle[(2, BE.DecisionComercial.DetalleCliente)] = new List<int> { 8, 9 };
            var planes = new FakePlanSuscripcionDAL { Planes = new List<BE.PlanSuscripcion> { new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Precio = 1000m, Estado = true } } };
            var svc = new InteligenciaComercial(new FakeInteligenciaComercialDAL(), planes, null, null, null, null, dalDec, () => Hoy);

            var todas = svc.MedirDecisiones(dalDec.Obtener(null, null));
            Assert.AreEqual(2, todas[1].Contactados);
            Assert.AreEqual(1, todas[2].Contactados, "El cliente 8 cuenta para la primera campaña que lo contactó.");

            // El filtro del Historial (fechas o estado) dejó solo la segunda: la atribución no cambia.
            var filtrada = svc.MedirDecisiones(new[] { dalDec.ObtenerPorId(2) });
            Assert.AreEqual(1, filtrada.Count);
            Assert.AreEqual(1, filtrada[2].Contactados, "Sin la primera en la lista, el cliente 8 sigue siendo de la primera.");
        }

        [TestMethod]
        public void ConsultasDeDecisiones_SinSesionOSinLaPatenteDeGerencia_Rechazan()
        {
            var dal = new FakeDecisionComercialDAL();
            dal.Filas.Add(Ejecutada(ReglaRenovaciones.Codigo, Hoy, id: 1));
            dal.Detalle[(1, BE.DecisionComercial.DetalleCliente)] = new List<int> { 7 };
            var svc = new DecisionesComerciales(dal, () => Hoy);
            void Rechaza(Action a, string clave)
            {
                try { a(); Assert.Fail("Se esperaba " + clave); }
                catch (BE.AppException ex) { Assert.AreEqual(clave, ex.Clave); }
            }
            foreach (var clave in new[] { "err.bll.sesion_expirada", "err.bll.sin_permiso" })
            {
                if (clave == "err.bll.sin_permiso")
                    SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor",
                        Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = BE.Patentes.Clientes } } });
                Rechaza(() => svc.Obtener(), clave);
                Rechaza(() => svc.AsociarDecisiones(new[] { RecRenovaciones() }), clave);
                Rechaza(() => svc.ObtenerDetalle(new[] { 1 }, BE.DecisionComercial.DetalleCliente), clave);
            }
            SessionManager.Logout();
            LoginAdmin();
            Assert.AreEqual(1, svc.Obtener().Count);
            Assert.IsTrue(svc.AsociarDecisiones(new[] { RecRenovaciones() }));
            Assert.AreEqual(1, svc.ObtenerDetalle(new[] { 1 }, BE.DecisionComercial.DetalleCliente).Count);
        }

        [TestMethod]
        public void MedirDecisiones_CampaniaAceptadaSinEjecutar_NoSeMide()
        {
            LoginAdmin();
            var dalDec = new FakeDecisionComercialDAL();
            var planes = new FakePlanSuscripcionDAL { Planes = new List<BE.PlanSuscripcion> { new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Precio = 1000m, Estado = true } } };
            var svc = new InteligenciaComercial(new FakeInteligenciaComercialDAL(), planes, null, null, null, null, dalDec, () => Hoy);
            var d = new DecisionesComerciales(dalDec, () => Hoy).Aceptar("Test", RecRenovaciones(), 0);
            dalDec.Detalle[(d.IdDecision, BE.DecisionComercial.DetalleCliente)] = new List<int> { 7 };   // aunque tuviera contactos guardados
            var lista = dalDec.Obtener(null, null);
            Assert.IsNull(lista.Single().FechaEjecucion);
            Assert.IsFalse(svc.MedirDecisiones(lista).ContainsKey(d.IdDecision), "Sin ejecutar no hay campaña que medir.");
        }

        [TestMethod]
        public void Resumir_LasCampaniasEnMedicionNoSeSuman_HastaQueCierran()
        {
            BE.DecisionComercial D(int id, BE.EstadoDecision e, decimal estimado, decimal? real = null) =>
                new BE.DecisionComercial { IdDecision = id, Estado = e, ImpactoEstimado = estimado, ResultadoReal = real };
            var lista = new List<BE.DecisionComercial>
            {
                D(1, BE.EstadoDecision.Ejecutada, 500m), D(2, BE.EstadoDecision.Ejecutada, 800m), D(3, BE.EstadoDecision.Ejecutada, 1800m),
                D(4, BE.EstadoDecision.Aceptada, 300m, 250m), D(5, BE.EstadoDecision.Descartada, 999m), D(6, BE.EstadoDecision.Ejecutada, 100m)
            };
            var medidos = new Dictionary<int, BE.MedicionDecision>
            {
                [1] = BE.MedicionDecision.Medida(1000m),
                [2] = BE.MedicionDecision.Medida(-2000m),
                [3] = BE.MedicionDecision.EnCurso(2, 6, Hoy.AddDays(20)),
                [5] = BE.MedicionDecision.Medida(7777m),     // descartada: no admite resultado
                [6] = BE.MedicionDecision.SinDatos()
            };
            var r = MedicionDecisiones.Resumir(lista, medidos);
            Assert.AreEqual(6, r.Decisiones);
            Assert.AreEqual(5, r.Aceptadas);
            Assert.AreEqual(3500m, r.EstimadoAceptadas);
            Assert.AreEqual(1, r.ConReal);
            Assert.AreEqual(300m, r.EstimadoConReal);
            Assert.AreEqual(250m, r.Real);
            Assert.AreEqual(2, r.Medidas, "Solo las de resultado final.");
            Assert.AreEqual(1300m, r.EstimadoMedidas, "El estimado de la campaña en medición tampoco se suma.");
            Assert.AreEqual(-1000m, r.Medido, "1000 − 2000: la negativa cerrada suma; la abierta no.");
            Assert.AreEqual(1, r.EnMedicion);

            // Al cerrar la campaña 3 entra en las sumas.
            medidos[3] = BE.MedicionDecision.Medida(600m);
            var cerrada = MedicionDecisiones.Resumir(lista, medidos);
            Assert.AreEqual(3, cerrada.Medidas);
            Assert.AreEqual(3100m, cerrada.EstimadoMedidas);
            Assert.AreEqual(-400m, cerrada.Medido);
            Assert.AreEqual(0, cerrada.EnMedicion);

            var sinMediciones = MedicionDecisiones.Resumir(lista, null);
            Assert.AreEqual(0, sinMediciones.Medidas);
            Assert.AreEqual(0, sinMediciones.EnMedicion);
            Assert.AreEqual(0, MedicionDecisiones.Resumir(null, medidos).Decisiones);
        }

        [TestMethod]
        public void MedirDecisiones_IncluyeLasCampaniasConContactosGuardados_EnMedicionHastaQueCierra()
        {
            LoginAdmin();
            var dalDec = new FakeDecisionComercialDAL();
            var dal = new FakeInteligenciaComercialDAL();
            dal.Movimientos.Add(Mov(7, Hoy.AddDays(-2), BE.EstadoRenovacion.Renovada));
            var planes = new FakePlanSuscripcionDAL { Planes = new List<BE.PlanSuscripcion> { new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Precio = 1000m, Estado = true } } };
            var svc = new InteligenciaComercial(dal, planes, null, null, null, null, dalDec, () => Hoy);
            var decisiones = new DecisionesComerciales(dalDec, () => Hoy.AddDays(-5));
            var d = decisiones.Aceptar("Test", RecRenovaciones(), 0);
            decisiones.MarcarEjecutadaConDetalle("Test", d.IdDecision, BE.DecisionComercial.DetalleCliente, new[] { 7, 8 });

            var lista = dalDec.Obtener(null, null);
            var medida = svc.MedirDecisiones(lista)[d.IdDecision];
            Assert.AreEqual(BE.EstadoMedicion.EnMedicion, medida.Estado, "La ventana de 30 días todavía no cerró.");
            Assert.IsNull(medida.Valor, "En medición no hay importe.");
            Assert.AreEqual(1, medida.Respondieron, "El cliente 7 ya renovó.");
            Assert.AreEqual(2, medida.Contactados);
            Assert.AreEqual(Hoy.AddDays(25), medida.FechaCierre, "Ejecutada hace 5 días: cierra dentro de 25.");
            var resumen = MedicionDecisiones.Resumir(lista, svc.MedirDecisiones(lista));
            Assert.AreEqual(1, resumen.EnMedicion);
            Assert.AreEqual(0, resumen.Medidas);
            Assert.AreEqual(0m, resumen.Medido, "La campaña en medición no suma.");

            // El día del cierre, el importe: el cliente 7 renovó (sin historia: tasa base 0).
            var alCierre = new InteligenciaComercial(dal, planes, null, null, null, null, dalDec, () => Hoy.AddDays(25));
            var final = alCierre.MedirDecisiones(dalDec.Obtener(null, null))[d.IdDecision];
            Assert.AreEqual(BE.EstadoMedicion.Medido, final.Estado);
            Assert.AreEqual(1000m, final.Valor);
        }

        // ══ Fecha de ejecución ═══════════════════════════════════════════════════

        [TestMethod]
        public void PospuestaNoventaDiasYEjecutadaHoy_RigeYSeMideDesdeLaEjecucion()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var hace90 = new DecisionesComerciales(dal, () => Hoy.AddDays(-90));
            var rec = RecRenovaciones();
            hace90.Posponer("Test", rec, 90, null);
            var hoy = new DecisionesComerciales(dal, () => Hoy);
            hoy.AsociarDecisiones(new[] { rec });
            var d = hoy.Aceptar("Test", rec, 0);
            hoy.MarcarEjecutadaConDetalle("Test", d.IdDecision, BE.DecisionComercial.DetalleCliente, new[] { 1 });
            var ejecutada = dal.Filas.Single();
            Assert.AreEqual(Hoy.AddDays(-90), ejecutada.Fecha, "La creación no cambia.");
            Assert.AreEqual(Hoy, ejecutada.FechaEjecucion);
            Assert.IsTrue(DecisionesComerciales.DecisionVigente(ejecutada, Hoy, 30, Hoy.AddDays(-89)),
                "Se ejecutó dentro del período analizado: sigue rigiendo aunque se creó antes.");
            var r = MedicionCampanias.Medir(ejecutada, new[] { 1 }, new[] { Mov(1, Hoy.AddDays(-30), BE.EstadoRenovacion.Renovada) }, Precios, Plan1, 6m, Hoy);
            Assert.AreEqual(0, r.Respondieron, "Una renovación anterior a la ejecución no es respuesta a la campaña.");
            Assert.AreEqual(Hoy, r.Desde);
        }

        // ══ Una decisión abierta por situación y opciones que ya no existen ═════

        [TestMethod]
        public void Aceptar_ConOtraDecisionAbiertaParaLaMismaClave_Rechaza()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = new DecisionesComerciales(dal, () => Hoy);
            svc.Posponer("Test", RecRenovaciones(), 5, null);
            try { svc.Aceptar("Test", RecRenovaciones(), 0); Assert.Fail(); }   // recomendación sin la decisión asociada
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.decision.abierta", ex.Clave); }
            Assert.AreEqual(1, dal.Filas.Count);
        }

        [TestMethod]
        public void Aceptar_ConUnaAceptadaDeUnaOpcionQueYaNoExiste_LaCierraYAceptaLaNueva()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = new DecisionesComerciales(dal, () => Hoy);
            var vieja = svc.Aceptar("Test", RecRenovaciones(), 0);
            var fila = dal.Filas.Single(); fila.AccionElegida = "Gestionar el cobro de las 2 renovaciones desde Cobro de renovaciones";
            var rec = RecRenovaciones();
            svc.AsociarDecisiones(new[] { rec });
            var nueva = svc.Aceptar("Test", rec, 0);
            Assert.AreNotEqual(vieja.IdDecision, nueva.IdDecision);
            Assert.AreEqual(BE.EstadoDecision.Descartada, dal.Filas.Single(x => x.IdDecision == vieja.IdDecision).Estado);
            Assert.AreEqual(BE.EstadoDecision.Aceptada, dal.Filas.Single(x => x.IdDecision == nueva.IdDecision).Estado);
        }

        // ══ Ejecutar prendas: lo aplicado siempre queda registrado ═══════════════

        private static (DecisionesComerciales Svc, FakeDecisionComercialDAL Dal, int Id) DecisionAceptada()
        {
            var dal = new FakeDecisionComercialDAL();
            var svc = new DecisionesComerciales(dal, () => Hoy);
            var rec = new BE.Recomendacion
            {
                TipoRegla = ReglaBajaRotacion.Codigo, Clave = ReglaBajaRotacion.Codigo, Titulo = new BE.TextoTraducible("t", "Liquidar"),
                Principal = new BE.OpcionRecomendacion { Descripcion = new BE.TextoTraducible("o", "Dar de baja"), ImpactoEstimado = 100m,
                                                         Accion = BE.AccionRecomendacion.DarDeBajaPrendas }
            };
            return (svc, dal, svc.Aceptar("Test", rec, 0).IdDecision);
        }

        // Falla con una excepción que NO es de negocio en la prenda indicada.
        private sealed class CatalogoQueFalla : BLL.Interfaces.IPrendaService
        {
            private readonly CatalogoPrendas interno = Catalogo();
            public int FallaEn;
            public List<BE.Prenda> ObtenerTodos() => interno.ObtenerTodos();
            public List<BE.EstadoPrenda> ObtenerTransicionesManuales(BE.Prenda p) => interno.ObtenerTransicionesManuales(p);
            public void CambiarEstado(string modulo, BE.Prenda prenda, BE.EstadoPrenda nuevoEstado)
            {
                if (prenda.IdPrenda == FallaEn) throw new InvalidOperationException("se cortó la conexión");
                interno.CambiarEstado(modulo, prenda, nuevoEstado);
            }
            public List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null) => new List<BE.Prenda>();
            public List<BE.Prenda> ObtenerPorCliente(int idCliente) => new List<BE.Prenda>();
            public void Alta(string modulo, BE.Prenda prenda) { }
            public void Modificar(string modulo, BE.Prenda prenda) { }
            public void ReingresarInspeccionada(string modulo, BE.Prenda prenda) { }
            public bool EstaPendienteDeInspeccion(BE.Prenda prenda) => false;
            public (bool Disponible, List<BE.Prenda> NoDisponibles) VerificarDisponibilidad(List<BE.Prenda> s) => (true, new List<BE.Prenda>());
            public List<BE.MantenimientoPrenda> ObtenerHistorialMantenimiento(int idPrenda) => new List<BE.MantenimientoPrenda>();
            public List<BE.MantenimientoPrenda> ObtenerEnMantenimiento() => new List<BE.MantenimientoPrenda>();
            public BE.OcupacionStock ObtenerOcupacion() => new BE.OcupacionStock();
            public List<BE.Prenda> ObtenerEnLimpieza() => new List<BE.Prenda>();
        }

        [TestMethod]
        public void EjecutarDecision_ErrorNoDeNegocioEnUnaPrenda_LasDemasSeAplicanYLaDecisionQuedaEjecutadaConLoAplicado()
        {
            LoginAdmin();
            var (svc, dal, id) = DecisionAceptada();
            var catalogo = new CatalogoQueFalla { FallaEn = 2 };
            var acciones = new AccionesPrendas(catalogo);
            var r = acciones.EjecutarDecision("Test", acciones.Planificar(BE.AccionRecomendacion.DarDeBajaPrendas, new[] { 1, 2, 5 }), id, svc);
            CollectionAssert.AreEqual(new[] { 1, 5 }, r.Aplicadas);
            Assert.IsInstanceOfType(r.Fallidas.Single().Error, typeof(InvalidOperationException));
            Assert.IsTrue(r.Registrada);
            var d = dal.Filas.Single();
            Assert.AreEqual(BE.EstadoDecision.Ejecutada, d.Estado);
            StringAssert.Contains(d.Motivo, "#2", "Las que no se cambiaron quedan anotadas.");
            CollectionAssert.AreEqual(new[] { 1, 5 }, dal.Detalle[(id, BE.DecisionComercial.DetallePrenda)]);
        }

        [TestMethod]
        public void EjecutarDecision_FallaAlMarcarEjecutada_LoAplicadoQuedaInformadoYSinDetalleSuelto()
        {
            LoginAdmin();
            var (svc, dal, id) = DecisionAceptada();
            dal.FallarEjecucion = true;     // otra sesión cambió la decisión
            var catalogo = Catalogo();
            var acciones = new AccionesPrendas(catalogo);
            var r = acciones.EjecutarDecision("Test", acciones.Planificar(BE.AccionRecomendacion.DarDeBajaPrendas, new[] { 1, 2 }), id, svc);
            CollectionAssert.AreEqual(new[] { 1, 2 }, r.Aplicadas, "Las prendas ya cambiaron (cada cambio se confirma solo).");
            Assert.IsFalse(r.Registrada);
            Assert.AreEqual("err.bll.decision.concurrente", ((BE.AppException)r.ErrorRegistro).Clave);
            Assert.AreEqual(BE.EstadoDecision.Aceptada, dal.Filas.Single().Estado);
            Assert.AreEqual(0, dal.Detalle.Count, "Detalle y estado van en la misma transacción: no queda detalle suelto.");
        }

        // ══ Lo que ve cada usuario: solo opciones que puede ejecutar ═════════════

        private static BE.OpcionRecomendacion Op(BE.AccionRecomendacion a, decimal impacto) =>
            new BE.OpcionRecomendacion { Accion = a, ImpactoEstimado = impacto, Descripcion = new BE.TextoTraducible("o", a.ToString()) };

        [TestMethod]
        public void FiltrarPorPermiso_QuitaOpcionesYRecomendacionesQueElUsuarioNoPuedeEjecutar()
        {
            var rotacion = new BE.Recomendacion { Clave = "R", Principal = Op(BE.AccionRecomendacion.DarDeBajaPrendas, 100m) };
            var mant = new BE.Recomendacion
            {
                Clave = "M", Principal = Op(BE.AccionRecomendacion.EnviarAMantenimiento, 0m),
                Alternativas = { Op(BE.AccionRecomendacion.DarDeBajaPrendas, -50m) }
            };
            var churn = new BE.Recomendacion
            {
                Clave = "C", Principal = Op(BE.AccionRecomendacion.SugerirPromocion, 900m),
                Alternativas = { Op(BE.AccionRecomendacion.ExportarContactos, 300m) }
            };
            var promo = new BE.Recomendacion
            {
                Clave = "P", Principal = Op(BE.AccionRecomendacion.SolicitarBajaPromocion, 500m),
                Alternativas = { Op(BE.AccionRecomendacion.ExportarContactos, 10m) }
            };
            Func<string, bool> gerente = p => p == BE.Patentes.SugerenciaPromocion;   // sin stock ni promociones vigentes
            var vistas = AccionesRecomendacion.FiltrarPorPermiso(new[] { rotacion, mant, churn, promo }, gerente);
            CollectionAssert.AreEqual(new[] { "C", "P" }, vistas.Select(r => r.Clave).ToList(), "Sin opciones ejecutables, no se muestran.");
            Assert.AreEqual(2, churn.Opciones().Count, "Las que puede ejecutar quedan intactas.");
            Assert.AreEqual(BE.AccionRecomendacion.ExportarContactos, promo.Principal.Accion, "La que queda pasa a principal.");
            Assert.AreEqual(0, promo.Alternativas.Count);
            Assert.AreEqual(4, AccionesRecomendacion.FiltrarPorPermiso(new[] { rotacion, mant, churn, promo }, p => true).Count);
        }

        [TestMethod]
        public void Recomendar_SinLaPatenteDeStock_NoMuestraLasDePrendasNiLesSubeLaPrioridad()
        {
            LoginAdmin();
            var dal = new FakeInteligenciaComercialDAL();
            for (int i = 1; i <= 6; i++)
                dal.Prendas.Add(new BE.PrendaComercial { IdPrenda = i, Nombre = "P" + i, Categoria = "Vestido", Talle = "M",
                                                         Estado = BE.EstadoPrenda.Disponible, FechaAlta = Hoy.AddDays(-200), PrecioReposicion = 1000m });
            var planes = new FakePlanSuscripcionDAL { Planes = new List<BE.PlanSuscripcion> { new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", LimitePrendas = 3, Precio = 1000m, Estado = true } } };
            var metas = new FakeMetaComercialDAL();
            metas.Filas.Add(new BE.MetaComercial { IdMeta = 1, Periodo = new DateTime(2026, 10, 1), Indicador = BE.IndicadorMeta.OcupacionStock, ValorObjetivo = 60m, Sentido = BE.SentidoMeta.MayorEsMejor });
            var svc = new InteligenciaComercial(dal, planes, null, null, null, null, new FakeDecisionComercialDAL(), () => Hoy, null, metas);

            svc.TienePermiso = p => p == BE.Patentes.SugerenciaPromocion;
            var gerente = svc.Recomendar(svc.Analizar("Test", Hoy.AddDays(-89), Hoy));
            Assert.IsFalse(gerente.Recomendaciones.Any(r => r.TipoRegla == ReglaBajaRotacion.Codigo));
            Assert.IsTrue(gerente.Metas.Single().RequiereAccion, "La meta de ocupación sigue en riesgo (se ve igual).");

            svc.TienePermiso = p => true;
            var admin = svc.Recomendar(svc.Analizar("Test", Hoy.AddDays(-89), Hoy));
            var rotacion = admin.Recomendaciones.Single(r => r.TipoRegla == ReglaBajaRotacion.Codigo);
            Assert.IsNotNull(rotacion.PrioridadElevada, "Con permiso, la liquidación sube por la meta de ocupación.");
        }
        // ══ Avisos al iniciar sesión ═════════════════════════════════════════════

        private static BE.Recomendacion Rec(string clave, BE.PrioridadRecomendacion prioridad, BE.DecisionComercial ultima = null) => new BE.Recomendacion
        {
            TipoRegla = "X", Clave = clave, Prioridad = prioridad, UltimaDecision = ultima,
            Titulo = new BE.TextoTraducible("t", "Recomendación " + clave),
            Principal = new BE.OpcionRecomendacion { ImpactoEstimado = 1m, Accion = BE.AccionRecomendacion.OrdenCompra }
        };

        private static BE.DecisionComercial Pospuesta(string clave, DateTime hasta, DateTime fecha) => new BE.DecisionComercial
        {
            IdDecision = Math.Abs(clave.GetHashCode() % 1000) + 1, ClaveRecomendacion = clave, Fecha = fecha,
            Estado = BE.EstadoDecision.Pospuesta, PospuestaHasta = hasta, Titulo = "T " + clave
        };

        [TestMethod]
        public void Resumir_CuentaAltasDecidiblesPospuestasVencidasYMetasEnRiesgo()
        {
            var descartadaHoy = new BE.DecisionComercial { Estado = BE.EstadoDecision.Descartada, Fecha = Hoy.AddDays(-2), ClaveRecomendacion = "C" };
            var recs = new[]
            {
                Rec("A", BE.PrioridadRecomendacion.Alta),
                Rec("B", BE.PrioridadRecomendacion.Media),
                Rec("C", BE.PrioridadRecomendacion.Alta, descartadaHoy),                                  // en silencio
                Rec("D", BE.PrioridadRecomendacion.Alta, Pospuesta("D", Hoy.AddDays(3), Hoy.AddDays(-4))), // oculta
                Rec("P1", BE.PrioridadRecomendacion.Media), Rec("P2", BE.PrioridadRecomendacion.Media),
                Rec("P3", BE.PrioridadRecomendacion.Media), Rec("P4", BE.PrioridadRecomendacion.Media)
            };
            var historial = new[]
            {
                Pospuesta("P1", Hoy, Hoy.AddDays(-7)),                  // hoy: cuenta
                Pospuesta("P2", Hoy.AddDays(-1), Hoy.AddDays(-8)),      // ya pasó: cuenta
                Pospuesta("P3", Hoy.AddDays(1), Hoy.AddDays(-6)),       // mañana: no
                Pospuesta("P4", Hoy.AddDays(-3), Hoy.AddDays(-9)),
                new BE.DecisionComercial { IdDecision = 900, ClaveRecomendacion = "P4", Fecha = Hoy.AddDays(-1), Estado = BE.EstadoDecision.Aceptada } // después se aceptó
            };
            var metas = new[]
            {
                new BE.SeguimientoMeta { Meta = new BE.MetaComercial(), EsMesEnCurso = true, Estado = BE.EstadoMeta.EnRiesgo,
                                         NombreIndicador = new BE.TextoTraducible("x", "% de bajas") },
                new BE.SeguimientoMeta { Meta = new BE.MetaComercial(), EsMesEnCurso = true, Estado = BE.EstadoMeta.EnCamino },
                new BE.SeguimientoMeta { Meta = new BE.MetaComercial(), EsMesEnCurso = false, Estado = BE.EstadoMeta.NoCumplida }
            };
            var r = AvisosComerciales.Resumir(recs, historial, metas, Hoy, 30, Hoy.AddDays(-89));
            Assert.AreEqual(1, r.RecomendacionesAlta);
            Assert.AreEqual(2, r.PospuestasVencidas);
            Assert.AreEqual(1, r.MetasEnRiesgo);
            Assert.IsTrue(r.HayAlgo);
            Assert.AreEqual("aviso.inicio.destacado.meta", r.Destacados[0].Clave, "Lo primero que se destaca es la meta en riesgo.");
            Assert.IsTrue(r.Destacados.Count <= AvisosComerciales.MaximoDestacados);
        }

        [TestMethod]
        public void Resumir_NoCuentaDosVeces_NiPospuestasDeRecomendacionesQueYaNoExisten()
        {
            var vencida = Pospuesta("A", Hoy, Hoy.AddDays(-7));
            var recs = new[] { Rec("A", BE.PrioridadRecomendacion.Alta, vencida) };   // alta y pospuesta que vence hoy
            var historial = new[]
            {
                vencida,
                Pospuesta("Vendedores:1:Concentracion", Hoy.AddDays(-2), Hoy.AddDays(-10))   // regla retirada
            };
            var r = AvisosComerciales.Resumir(recs, historial, null, Hoy, 30, Hoy.AddDays(-89));
            Assert.AreEqual(1, r.PospuestasVencidas);
            Assert.AreEqual(0, r.RecomendacionesAlta, "La misma situación se cuenta una vez (como pospuesta).");
            Assert.AreEqual(1, r.Destacados.Count);
        }

        [TestMethod]
        public void Resumir_SinNadaQueAvisar_NoHayAviso()
        {
            var r = AvisosComerciales.Resumir(new[] { Rec("A", BE.PrioridadRecomendacion.Media) }, null, null, Hoy, 30, Hoy.AddDays(-89));
            Assert.IsFalse(r.HayAlgo);
            Assert.AreEqual(0, r.Destacados.Count);
        }

        [TestMethod]
        public void Pospuesta_VuelveAMostrarseElDiaDeSuFecha()
        {
            var rec = Rec("A", BE.PrioridadRecomendacion.Alta, Pospuesta("A", Hoy, Hoy.AddDays(-7)));
            Assert.IsTrue(DecisionesComerciales.DebeMostrarse(rec, Hoy, 30), "El día en que el aviso la recuerda, ya se ve en el Centro.");
            Assert.IsFalse(DecisionesComerciales.DebeMostrarse(rec, Hoy.AddDays(-1), 30));
        }

        [TestMethod]
        public void NoMostrarHoy_SeRecuerdaPorUsuarioYSoloPorEseDia()
        {
            LoginAdmin();
            var dal = new FakeInteligenciaComercialDAL();
            var hoy = Hoy;
            var avisos = new AvisosComerciales(dal, () => hoy);
            Assert.IsFalse(avisos.OcultoHoy(1));
            avisos.OcultarHoy("Test", 1);
            Assert.IsTrue(avisos.OcultoHoy(1));
            Assert.IsFalse(avisos.OcultoHoy(2), "Es por usuario.");
            hoy = Hoy.AddDays(1);
            Assert.IsFalse(avisos.OcultoHoy(1), "Al día siguiente vuelve a avisar.");
            Assert.IsTrue(AvisosComerciales.Corresponde(p => p == BE.Patentes.SugerenciaPromocion));
            Assert.IsFalse(AvisosComerciales.Corresponde(p => false));
        }
    }
}
