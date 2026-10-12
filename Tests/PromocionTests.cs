using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Promocion — PN03 (Métricas, promociones y toma de decisiones). Una prueba por rama del
    /// diagrama de actividad aprobado: crear (siempre para un plan, desde sugerencia o manual) →
    /// Validar → ¿Aprueba? (quien crea no dictamina) → ¿Reformular? / Dar de baja; región de
    /// vigencia: solicitud de baja (no cambia el estado) → Atender (de acuerdo: Dar de baja), baja
    /// directa y vencimiento. Cada transición escribe historial.
    /// </summary>
    [TestClass]
    public class PromocionTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static void LoginComoAdministrador()
        {
            SessionManager.Login(new BE.Usuario
            {
                Id = 1,
                Username = "admin",
                Perfil = "Administrador",
                Contraseña = Encriptador.Hash("Admin1!")
            });
        }

        private static BE.PlanSuscripcion PlanActivo() => new BE.PlanSuscripcion
        {
            IdPlan = 1,
            Nombre = "Básico",
            LimitePrendas = 3,
            Precio = 1000,
            Estado = true
        };

        private class Contexto
        {
            public FakePromocionDAL DalPromocion = new FakePromocionDAL();
            public FakeSugerenciaPromocionDAL DalSugerencia = new FakeSugerenciaPromocionDAL();
            public FakePlanSuscripcionDAL DalPlan = new FakePlanSuscripcionDAL { PlanPorId = PlanActivo() };

            // "Analizar margen e impacto": clientes activos del plan (sin base de datos).
            public int ClientesActivos;
            public int ConteosDeClientes;

            public BLL.Promocion Crear()
                => new BLL.Promocion(DalPromocion, DalSugerencia, DalPlan, idPlan => { ConteosDeClientes++; return ClientesActivos; });
        }

        // ── CrearManual ───────────────────────────────────────────────────────

        [TestMethod]
        public void CrearManual_DatosValidosConPlan_PersisteConEstadoInicialEnRevisionContable()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPromocion.AltaIdGenerado = 77;
            var bll = ctx.Crear();

            int id = bll.CrearManual("Test", "Promo Verano", "desc", BE.TipoDescuento.Porcentaje, 10m,
                DateTime.Today, DateTime.Today.AddDays(30), 1, 500m, "impacto");

            Assert.AreEqual(77, id);
            Assert.AreEqual(1, ctx.DalPromocion.AltaVeces);
            Assert.AreEqual(BE.EstadoPromocion.EnRevisionContable, ctx.DalPromocion.UltimoAlta.Estado);
            Assert.IsNull(ctx.DalPromocion.UltimoAlta.IdSugerenciaOrigen);
            Assert.AreEqual(1, ctx.DalPromocion.UltimoAlta.IdPlan);
        }

        [TestMethod]
        public void CrearManual_SinPlan_LanzaPlanRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 10m,
                    DateTime.Today, DateTime.Today.AddDays(30), 0, 500m, "impacto");
                Assert.Fail("Debía rechazar una promoción sin plan: el plan es obligatorio.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.plan_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearManual_NombreVacio_LanzaNombreRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "   ", "desc", BE.TipoDescuento.Porcentaje, 10m,
                    DateTime.Today, DateTime.Today.AddDays(30), 1, 500m, "impacto");
                Assert.Fail("Debía exigir el nombre de la promoción.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.nombre_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearManual_PlanInexistente_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId = null;
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 10m,
                    DateTime.Today, DateTime.Today.AddDays(30), 1, 500m, "impacto");
                Assert.Fail("Debía rechazar un plan inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.plan_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearManual_ValorInvalido_LanzaValorInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 0m,
                    DateTime.Today, DateTime.Today.AddDays(30), 1, 500m, "impacto");
                Assert.Fail("Debía exigir un valor mayor a cero.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.valor_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearManual_PorcentajeMayorA100_LanzaPorcentajeInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 150m,
                    DateTime.Today, DateTime.Today.AddDays(30), 1, 500m, "impacto");
                Assert.Fail("Debía rechazar un porcentaje mayor a 100.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.porcentaje_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearManual_RangoFechasInvalido_LanzaRangoFechasInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 10m,
                    DateTime.Today, DateTime.Today.AddDays(-1), 1, 500m, "impacto");
                Assert.Fail("Debía rechazar una fecha de fin anterior a la de inicio.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.rango_fechas_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        // ── CrearDesdeSugerencia ──────────────────────────────────────────────

        [TestMethod]
        public void CrearDesdeSugerencia_SugerenciaExistente_PersisteTomandoDatosDeLaSugerenciaYMarcaEvaluada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = new BE.SugerenciaPromocion
            {
                IdSugerencia = 5,
                IdPlan = 1,
                Motivo = "Motivo de la sugerencia",
                TipoDescuentoSugerido = BE.TipoDescuento.Porcentaje,
                BeneficioEstimado = 100m,
                Estado = BE.EstadoSugerencia.Pendiente
            };
            ctx.DalPromocion.AltaIdGenerado = 88;
            var bll = ctx.Crear();

            int id = bll.CrearDesdeSugerencia("Test", 5, "Promo desde sugerencia", "desc",
                BE.TipoDescuento.Porcentaje, 15m, DateTime.Today, DateTime.Today.AddDays(20), 200m, "impacto");

            Assert.AreEqual(88, id);
            Assert.AreEqual(1, ctx.DalPromocion.AltaVeces);
            Assert.AreEqual(1, ctx.DalPromocion.UltimoAlta.IdPlan);
            Assert.AreEqual(5, ctx.DalPromocion.UltimoAlta.IdSugerenciaOrigen);
            Assert.AreEqual(1, ctx.DalSugerencia.MarcarEvaluadaVeces);
            Assert.AreEqual(5, ctx.DalSugerencia.UltimoIdEvaluado);
        }

        [TestMethod]
        public void CrearDesdeSugerencia_SugerenciaInexistente_LanzaSugerenciaInexistenteYNoLlamaAlta()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = null;
            var bll = ctx.Crear();

            try
            {
                bll.CrearDesdeSugerencia("Test", 5, "Promo desde sugerencia", "desc",
                    BE.TipoDescuento.Porcentaje, 15m, DateTime.Today, DateTime.Today.AddDays(20), 200m, "impacto");
                Assert.Fail("Debía rechazar una sugerencia inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.sugerencia_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
            Assert.AreEqual(0, ctx.DalSugerencia.MarcarEvaluadaVeces);
        }

        // ── Ramas del diagrama aprobado de PN03 ───────────────────────────────
        // Usuarios: 1 = Administración (crea), 2 = Contabilidad (dictamina), 3 = Ventas (pide la baja).
        // Todos entran como Administrador para no depender del catálogo de patentes: la guarda
        // "quien crea no dictamina" se aplica igual al Administrador.

        // Usuario de Contabilidad (no administrador) con las patentes de revisión contable.
        private static void LoginComoContable(int idUsuario)
        {
            SessionManager.Logout();
            var u = new BE.Usuario
            {
                Id = idUsuario,
                Username = "contable" + idUsuario,
                Perfil = "Contabilidad",
                Contraseña = Encriptador.Hash("Clave1!")
            };
            u.Permisos.Add(new BE.Permiso { NombreMenu = BE.Patentes.PromocionesContable });
            u.Permisos.Add(new BE.Permiso { NombreMenu = BE.Patentes.PromocionesContableEditar });
            SessionManager.Login(u);
        }

        // Excepción: el Administrador puede dictaminar una promoción que creó él.
        [TestMethod]
        public void AprobarContable_AdministradorQueLaCreo_PuedeDictaminar()
        {
            LoginComo(1);
            var ctx = new Contexto();
            ctx.Crear().AprobarContable("Test", Promo(BE.EstadoPromocion.EnRevisionContable, 1), "ok");
            Assert.AreEqual(1, ctx.DalPromocion.Dictamenes.Count);
        }

        [TestMethod]
        public void CrearManual_FechaFinYaPasada_LanzaFechaFinPasada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            EsperarError(() => ctx.Crear().CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 10m,
                    DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1), 1, 500m, "impacto"),
                "err.bll.promocion.fecha_fin_pasada");
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void AprobarContable_FechaFinYaPasada_LanzaAprobarVencida_SinDictamen()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable, 1);
            promo.FechaInicio = DateTime.Today.AddDays(-10);
            promo.FechaFin = DateTime.Today.AddDays(-1);   // terminó mientras esperaba la revisión

            EsperarError(() => ctx.Crear().AprobarContable("Test", promo, "ok"), "err.bll.promocion.aprobar_vencida");
            Assert.AreEqual(0, ctx.DalPromocion.Dictamenes.Count);
        }

        private static void LoginComo(int idUsuario)
        {
            SessionManager.Logout();
            SessionManager.Login(new BE.Usuario
            {
                Id = idUsuario,
                Username = "usuario" + idUsuario,
                Perfil = "Administrador",
                Contraseña = Encriptador.Hash("Admin1!")
            });
        }

        private static BE.Promocion Promo(BE.EstadoPromocion estado, int? idUsuarioAlta = 1) => new BE.Promocion
        {
            IdPromocion = 42,
            Nombre = "Promo Test",
            TipoDescuento = BE.TipoDescuento.Porcentaje,
            Valor = 10,
            FechaInicio = DateTime.Today.AddDays(-5),
            FechaFin = DateTime.Today.AddDays(30),
            Estado = estado,
            IdPlan = 1,
            IdUsuarioAlta = idUsuarioAlta
        };

        private static void EsperarError(Action accion, string claveEsperada)
        {
            try { accion(); Assert.Fail("Debía lanzar " + claveEsperada); }
            catch (BE.AppException ex) { Assert.AreEqual(claveEsperada, ex.Clave); }
        }

        private static void AssertHistorial(BE.PromocionHistorial h, BE.EstadoPromocion? anterior, BE.EstadoPromocion nuevo, int? idUsuario)
        {
            Assert.AreEqual(anterior, h.EstadoAnterior);
            Assert.AreEqual(nuevo, h.EstadoNuevo);
            Assert.AreEqual(idUsuario, h.IdUsuario);
        }

        // Crear → Validar → EnRevisionContable

        [TestMethod]
        public void CrearManual_GuardaElCreadorYEscribeHistorialDeAlta()
        {
            LoginComo(1);
            var ctx = new Contexto();
            ctx.DalPromocion.AltaIdGenerado = 7;

            ctx.Crear().CrearManual("Test", "Promo", "d", BE.TipoDescuento.MontoFijo, 100m,
                DateTime.Today, DateTime.Today.AddDays(10), 1, 0m, "i");

            Assert.AreEqual(1, ctx.DalPromocion.UltimoAlta.IdUsuarioAlta);
            Assert.AreEqual(1, ctx.DalPromocion.Historial.Count);
            AssertHistorial(ctx.DalPromocion.Historial[0], null, BE.EstadoPromocion.EnRevisionContable, 1);
            Assert.AreEqual(7, ctx.DalPromocion.Historial[0].IdPromocion);
        }

        [TestMethod]
        public void CrearDesdeSugerencia_EscribeHistorialConLaSugerenciaDeOrigen()
        {
            LoginComo(1);
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = new BE.SugerenciaPromocion
            {
                IdSugerencia = 9, IdPlan = 1, Motivo = "m", TipoDescuentoSugerido = BE.TipoDescuento.Porcentaje,
                BeneficioEstimado = 5000m, Estado = BE.EstadoSugerencia.Pendiente
            };

            ctx.Crear().CrearDesdeSugerencia("Test", 9, "Promo", "d", BE.TipoDescuento.Porcentaje, 10m,
                DateTime.Today, DateTime.Today.AddDays(10), 0m, "i");

            AssertHistorial(ctx.DalPromocion.Historial[0], null, BE.EstadoPromocion.EnRevisionContable, 1);
            StringAssert.Contains(ctx.DalPromocion.Historial[0].Observacion, "#9");
        }

        [TestMethod]
        public void CrearDesdeSugerencia_SugerenciaPorCategoriaDeLaVersionAnterior_LanzaSugerenciaSinPlan()
        {
            LoginComo(1);
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = new BE.SugerenciaPromocion
            {
                IdSugerencia = 9, IdPlan = null, Motivo = "m", BeneficioEstimado = 10m, Estado = BE.EstadoSugerencia.Pendiente
            };

            EsperarError(() => ctx.Crear().CrearDesdeSugerencia("Test", 9, "Promo", "d",
                    BE.TipoDescuento.MontoFijo, 10m, DateTime.Today, DateTime.Today.AddDays(10), 0m, "i"),
                "err.bll.promocion.sugerencia_sin_plan");
            Assert.AreEqual(0, ctx.DalSugerencia.MarcarEvaluadaVeces);
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearDesdeSugerencia_FallaElAlta_DevuelveLaSugerenciaAPendiente()
        {
            LoginComo(1);
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = new BE.SugerenciaPromocion
            {
                IdSugerencia = 9, IdPlan = 1, Motivo = "m", BeneficioEstimado = 10m, Estado = BE.EstadoSugerencia.Pendiente
            };
            ctx.DalPromocion.AltaExcepcion = new InvalidOperationException("BD caída");

            try
            {
                ctx.Crear().CrearDesdeSugerencia("Test", 9, "Promo", "d",
                    BE.TipoDescuento.MontoFijo, 10m, DateTime.Today, DateTime.Today.AddDays(10), 0m, "i");
                Assert.Fail("Debía propagar el error del alta.");
            }
            catch (InvalidOperationException) { }

            Assert.AreEqual(1, ctx.DalSugerencia.MarcarEvaluadaVeces);
            Assert.AreEqual(1, ctx.DalSugerencia.ReabrirEvaluacionVeces);
        }

        // Contabilidad: Analizar margen e impacto

        [TestMethod]
        public void AnalizarMargenEImpacto_MuestraElBeneficioDeLaSugerenciaYLasPromocionesSuperpuestasDelPlan()
        {
            LoginComo(2);
            var ctx = new Contexto();
            var enRevision = Promo(BE.EstadoPromocion.EnRevisionContable);
            enRevision.IdSugerenciaOrigen = 9;
            var superpuesta = Promo(BE.EstadoPromocion.Vigente);
            superpuesta.IdPromocion = 50;
            var otroPlan = Promo(BE.EstadoPromocion.Vigente);
            otroPlan.IdPromocion = 51; otroPlan.IdPlan = 2;
            var sinCruce = Promo(BE.EstadoPromocion.Vigente);
            sinCruce.IdPromocion = 52; sinCruce.FechaInicio = DateTime.Today.AddDays(60); sinCruce.FechaFin = DateTime.Today.AddDays(90);
            ctx.DalPromocion.Todas.AddRange(new[] { enRevision, superpuesta, otroPlan, sinCruce });
            ctx.DalSugerencia.SugerenciaPorId = new BE.SugerenciaPromocion
            {
                IdSugerencia = 9, BeneficioEstimado = 12000m, OrigenMetrica = BE.OrigenMetrica.Abandono
            };

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.AreEqual(12000m, a.BeneficioEstimadoSugerencia);
            Assert.AreEqual(BE.OrigenMetrica.Abandono, a.OrigenSugerencia);
            Assert.IsTrue(a.TieneSuperposicion());
            CollectionAssert.AreEqual(new[] { 50 }, a.Superpuestas.ConvertAll(p => p.IdPromocion));
            Assert.IsTrue(a.UsuarioPuedeDictaminar);
        }

        [TestMethod]
        public void AnalizarMargenEImpacto_AltaManual_SinBeneficioDeGerencia()
        {
            LoginComo(2);
            var ctx = new Contexto();
            ctx.DalPromocion.Todas.Add(Promo(BE.EstadoPromocion.EnRevisionContable));

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.IsNull(a.BeneficioEstimadoSugerencia);
            Assert.IsFalse(a.TieneSuperposicion());
        }

        [TestMethod]
        public void AnalizarMargenEImpacto_ElCreadorNoPuedeDictaminar()
        {
            LoginComoContable(1);   // un Administrador sí podría (ver AprobarContable_AdministradorQueLaCreo_PuedeDictaminar)
            var ctx = new Contexto();
            ctx.DalPromocion.Todas.Add(Promo(BE.EstadoPromocion.EnRevisionContable, idUsuarioAlta: 1));

            Assert.IsFalse(ctx.Crear().AnalizarMargenEImpacto(42).UsuarioPuedeDictaminar);
        }

        // ── Margen proyectado = beneficio estimado − costo del descuento (mensual) ──

        [TestMethod]
        public void AnalizarMargenEImpacto_Porcentaje_CalculaCostoSobreLosClientesActivosYConviene()
        {
            LoginComo(2);
            var ctx = new Contexto { ClientesActivos = 8 };
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable);   // 10 % sobre el plan 1 (precio 1000)
            promo.IdSugerenciaOrigen = 9;
            ctx.DalPromocion.Todas.Add(promo);
            ctx.DalSugerencia.SugerenciaPorId = new BE.SugerenciaPromocion
            {
                IdSugerencia = 9, BeneficioEstimado = 12000m, OrigenMetrica = BE.OrigenMetrica.Abandono
            };

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.IsTrue(a.MargenCalculable);
            Assert.AreEqual(1000m, a.PrecioPlan);
            Assert.AreEqual(100m, a.DescuentoPorCliente, "10 % de 1000.");
            Assert.AreEqual(8, a.ClientesActivosPlan);
            Assert.AreEqual(800m, a.CostoDescuentoProyectado, "100 × 8 clientes.");
            Assert.IsTrue(a.BeneficioDeSugerencia);
            Assert.AreEqual(12000m, a.BeneficioEstimado);
            Assert.AreEqual(11200m, a.MargenProyectado, "12000 − 800.");
            Assert.AreEqual(true, a.Conviene);
        }

        [TestMethod]
        public void AnalizarMargenEImpacto_MontoFijoAltaManual_UsaElMargenDeAdministracionYNoConviene()
        {
            LoginComo(2);
            var ctx = new Contexto { ClientesActivos = 30 };
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable);
            promo.TipoDescuento = BE.TipoDescuento.MontoFijo;
            promo.Valor = 500m;
            promo.MargenEstimado = 10000m;
            ctx.DalPromocion.Todas.Add(promo);

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.IsFalse(a.BeneficioDeSugerencia, "Alta manual: no hay sugerencia de Gerencia.");
            Assert.AreEqual(10000m, a.BeneficioEstimado, "Se usa el margen estimado que cargó Administración.");
            Assert.AreEqual(500m, a.DescuentoPorCliente);
            Assert.AreEqual(15000m, a.CostoDescuentoProyectado, "500 × 30 clientes.");
            Assert.AreEqual(-5000m, a.MargenProyectado);
            Assert.AreEqual(false, a.Conviene);
        }

        [TestMethod]
        public void AnalizarMargenEImpacto_MontoFijoMayorAlPrecio_SeTopeaAlPrecioDelPlan()
        {
            LoginComo(2);
            var ctx = new Contexto { ClientesActivos = 2 };
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable);
            promo.TipoDescuento = BE.TipoDescuento.MontoFijo;
            promo.Valor = 5000m;
            promo.MargenEstimado = 3000m;
            ctx.DalPromocion.Todas.Add(promo);

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.AreEqual(1000m, a.DescuentoPorCliente, "Igual que en el cobro: el descuento no supera el precio del plan.");
            Assert.AreEqual(2000m, a.CostoDescuentoProyectado);
            Assert.AreEqual(1000m, a.MargenProyectado);
        }

        [TestMethod]
        public void AnalizarMargenEImpacto_MargenCero_NoConviene()
        {
            LoginComo(2);
            var ctx = new Contexto { ClientesActivos = 5 };
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable);   // 100 por cliente → costo 500
            promo.MargenEstimado = 500m;
            ctx.DalPromocion.Todas.Add(promo);

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.AreEqual(0m, a.MargenProyectado);
            Assert.AreEqual(false, a.Conviene, "Si el beneficio apenas cubre el descuento, no conviene.");
        }

        [TestMethod]
        public void AnalizarMargenEImpacto_SinClientesActivos_ElCostoEsCeroYConvieneSiHayBeneficio()
        {
            LoginComo(2);
            var ctx = new Contexto { ClientesActivos = 0 };
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable);
            promo.MargenEstimado = 200m;
            ctx.DalPromocion.Todas.Add(promo);

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.AreEqual(0m, a.CostoDescuentoProyectado);
            Assert.AreEqual(200m, a.MargenProyectado);
            Assert.AreEqual(true, a.Conviene);
        }

        [TestMethod]
        public void AnalizarMargenEImpacto_PlanInexistente_NoCalculaElMargen()
        {
            LoginComo(2);
            var ctx = new Contexto { ClientesActivos = 10 };
            ctx.DalPlan.PlanPorId = null;
            ctx.DalPromocion.Todas.Add(Promo(BE.EstadoPromocion.EnRevisionContable));

            var a = ctx.Crear().AnalizarMargenEImpacto(42);

            Assert.IsFalse(a.MargenCalculable);
            Assert.IsNull(a.Conviene);
            Assert.AreEqual(0, ctx.ConteosDeClientes, "Sin plan no consulta los clientes.");
            Assert.AreEqual(0m, a.CostoDescuentoProyectado);
        }

        // ¿Aprueba?

        [TestMethod]
        public void AprobarContable_GuardaElDictamenAprobadoYPasaAVigente()
        {
            LoginComo(2);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable);

            int idDictamen = ctx.Crear().AprobarContable("Test", promo, "  Margen suficiente  ");

            Assert.AreEqual(1, idDictamen);
            var d = ctx.DalPromocion.Dictamenes[0];
            Assert.IsTrue(d.Aprobada);
            Assert.AreEqual("Margen suficiente", d.Observacion);
            Assert.AreEqual(2, d.IdUsuario);
            Assert.AreEqual(BE.EstadoPromocion.Vigente, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.EnRevisionContable, BE.EstadoPromocion.Vigente, 2);
        }

        [TestMethod]
        public void RechazarContable_GuardaElDictamenRechazadoYPasaARechazada()
        {
            LoginComo(2);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.EnRevisionContable);

            ctx.Crear().RechazarContable("Test", promo, "Erosiona el margen");

            var d = ctx.DalPromocion.Dictamenes[0];
            Assert.IsFalse(d.Aprobada);
            Assert.AreEqual("Erosiona el margen", d.Observacion);
            Assert.AreEqual(BE.EstadoPromocion.RechazadaContabilidad, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.EnRevisionContable,
                BE.EstadoPromocion.RechazadaContabilidad, 2);
        }

        [TestMethod]
        public void AprobarContable_QuienCreoLaPromocionNoPuedeDictaminarla()
        {
            LoginComoContable(1);
            var ctx = new Contexto();

            EsperarError(() => ctx.Crear().AprobarContable("Test", Promo(BE.EstadoPromocion.EnRevisionContable, 1), "ok"),
                "err.bll.promocion.creador_no_dictamina");
            Assert.AreEqual(0, ctx.DalPromocion.Dictamenes.Count);
        }

        [TestMethod]
        public void RechazarContable_QuienCreoLaPromocionNoPuedeDictaminarla()
        {
            LoginComoContable(1);
            var ctx = new Contexto();

            EsperarError(() => ctx.Crear().RechazarContable("Test", Promo(BE.EstadoPromocion.EnRevisionContable, 1), "no"),
                "err.bll.promocion.creador_no_dictamina");
            Assert.AreEqual(0, ctx.DalPromocion.Transiciones);
        }

        [TestMethod]
        public void AprobarContable_NoEnRevisionContable_LanzaRevisionContableEstado()
        {
            LoginComo(2);
            EsperarError(() => new Contexto().Crear().AprobarContable("Test", Promo(BE.EstadoPromocion.Vigente), "ok"),
                "err.bll.promocion.revisioncontable_estado");
        }

        [TestMethod]
        public void AprobarContable_ObservacionVacia_LanzaObservacionRequerida()
        {
            LoginComo(2);
            var ctx = new Contexto();
            EsperarError(() => ctx.Crear().AprobarContable("Test", Promo(BE.EstadoPromocion.EnRevisionContable), "  "),
                "err.bll.promocion.observacion_requerida");
            Assert.AreEqual(0, ctx.DalPromocion.Dictamenes.Count);
        }

        [TestMethod]
        public void RechazarContable_ObservacionVacia_LanzaObservacionRequerida()
        {
            LoginComo(2);
            EsperarError(() => new Contexto().Crear().RechazarContable("Test", Promo(BE.EstadoPromocion.EnRevisionContable), ""),
                "err.bll.promocion.observacion_requerida");
        }

        // ¿Reformular? No → Dar de baja (también una Vigente, directamente o al atender una solicitud)

        [TestMethod]
        public void DarDeBaja_Rechazada_PasaADadaDeBajaConElMotivoEnElHistorial()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.RechazadaContabilidad);
            ctx.DalPromocion.Todas.Add(promo);

            ctx.Crear().DarDeBaja("Test", promo, " No conviene al negocio ");

            Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, promo.Estado);
            Assert.AreEqual(BE.EstadoPromocion.RechazadaContabilidad, ctx.DalPromocion.UltimoEstadoEsperado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.RechazadaContabilidad, BE.EstadoPromocion.DadaDeBaja, 1);
            Assert.AreEqual("No conviene al negocio", ctx.Crear().ObtenerBaja(42).Observacion, "Base de la «Constancia de baja».");
            Assert.IsTrue(promo.EsFinal());
        }

        [TestMethod]
        public void DarDeBaja_Vigente_PasaADadaDeBajaConElMotivoEnElHistorial()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.Vigente);

            ctx.Crear().DarDeBaja("Test", promo, "Cambio de estrategia comercial");

            Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.Vigente, BE.EstadoPromocion.DadaDeBaja, 1);
            Assert.AreEqual("Cambio de estrategia comercial", ctx.DalPromocion.Historial[0].Observacion);
        }

        [TestMethod]
        public void DarDeBaja_VigenteConSolicitudPendiente_LaCierraComoAtendidaConLaBaja()
        {
            LoginComo(1);
            var ctx = ConSolicitudPendiente(out var promo);

            ctx.Crear().DarDeBaja("Test", promo, "Fin de campaña");

            var s = ctx.DalPromocion.Solicitudes[0];
            Assert.AreEqual(BE.EstadoSolicitudBaja.Atendida, s.Estado);
            Assert.IsTrue(s.DioDeBaja);
            Assert.AreEqual("Fin de campaña", s.Respuesta);
            Assert.AreEqual(1, s.IdUsuarioResuelve);
            Assert.IsFalse(promo.TieneSolicitudBajaPendiente());
            Assert.AreEqual(0, ctx.Crear().ObtenerSolicitudesBajaPendientes().Count);
        }

        [TestMethod]
        public void DarDeBaja_SinMotivo_LanzaMotivoBajaRequerido()
        {
            LoginComo(1);
            var ctx = new Contexto();
            EsperarError(() => ctx.Crear().DarDeBaja("Test", Promo(BE.EstadoPromocion.Vigente), " "),
                "err.bll.promocion.motivobaja_admin_requerido");
            Assert.AreEqual(0, ctx.DalPromocion.Transiciones);
        }

        [TestMethod]
        public void DarDeBaja_EnRevisionOFinal_LanzaDarBajaEstado()
        {
            LoginComo(1);
            foreach (var estado in new[] { BE.EstadoPromocion.EnRevisionContable, BE.EstadoPromocion.DadaDeBaja, BE.EstadoPromocion.Vencida })
            {
                var ctx = new Contexto();
                EsperarError(() => ctx.Crear().DarDeBaja("Test", Promo(estado), "m"), "err.bll.promocion.darbaja_estado");
                Assert.AreEqual(0, ctx.DalPromocion.Transiciones, estado.ToString());
            }
        }

        [TestMethod]
        public void DarDeBaja_OtraSesionCambioElEstado_LanzaEstadoConcurrente()
        {
            LoginComo(1);
            var ctx = new Contexto();
            ctx.DalPromocion.ClaimResultado = false;
            var promo = Promo(BE.EstadoPromocion.Vigente);

            EsperarError(() => ctx.Crear().DarDeBaja("Test", promo, "m"), "err.bll.promocion.estado_concurrente");
            Assert.AreEqual(BE.EstadoPromocion.Vigente, promo.Estado);
        }

        // (a) Vendedor solicita la baja: solo registra la solicitud, la promoción sigue Vigente

        [TestMethod]
        public void SolicitarBaja_Vigente_GuardaLaSolicitudPendienteSinCambiarElEstado()
        {
            LoginComo(3);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.Vigente);
            ctx.DalPromocion.Todas.Add(promo);

            int idSolicitud = ctx.Crear().SolicitarBaja("Test", promo, " Se superpone con otra ");

            Assert.AreEqual(1, idSolicitud);
            var s = ctx.DalPromocion.Solicitudes[0];
            Assert.AreEqual("Se superpone con otra", s.Motivo);
            Assert.AreEqual(3, s.IdUsuarioSolicita);
            Assert.AreEqual(BE.EstadoSolicitudBaja.Pendiente, s.Estado);
            Assert.AreEqual(BE.EstadoPromocion.Vigente, promo.Estado, "Pedir la baja no cambia el estado.");
            Assert.AreEqual(0, ctx.DalPromocion.Transiciones, "No es una transición: no escribe historial.");
            Assert.IsTrue(promo.TieneSolicitudBajaPendiente());
            Assert.IsFalse(promo.PuedeSolicitarseBaja(), "Una sola solicitud pendiente por promoción.");
            Assert.IsTrue(promo.EstaVigente(), "Sigue aplicándose en el cobro.");
        }

        [TestMethod]
        public void SolicitarBaja_NoVigente_LanzaSugerirBajaEstado()
        {
            LoginComo(3);
            EsperarError(() => new Contexto().Crear().SolicitarBaja("Test", Promo(BE.EstadoPromocion.EnRevisionContable), "m"),
                "err.bll.promocion.sugerirbaja_estado");
        }

        [TestMethod]
        public void SolicitarBaja_SinMotivo_LanzaMotivoBajaRequerido()
        {
            LoginComo(3);
            var ctx = new Contexto();
            EsperarError(() => ctx.Crear().SolicitarBaja("Test", Promo(BE.EstadoPromocion.Vigente), ""),
                "err.bll.promocion.motivobaja_requerido");
            Assert.AreEqual(0, ctx.DalPromocion.Solicitudes.Count);
        }

        [TestMethod]
        public void SolicitarBaja_YaTieneUnaPendiente_LanzaSolicitudYaPendiente()
        {
            LoginComo(3);
            var ctx = ConSolicitudPendiente(out var promo);
            EsperarError(() => ctx.Crear().SolicitarBaja("Test", promo, "otra vez"), "err.bll.promocion.solicitud_ya_pendiente");
            Assert.AreEqual(1, ctx.DalPromocion.Solicitudes.Count);
        }

        [TestMethod]
        public void SolicitarBaja_OtraSesionYaLaPidio_LanzaSolicitudConcurrente()
        {
            LoginComo(3);
            var ctx = ConSolicitudPendiente(out var promo);
            promo.MotivoBaja = null;   // la lista de esta sesión se cargó antes de la otra solicitud
            EsperarError(() => ctx.Crear().SolicitarBaja("Test", promo, "m"), "err.bll.promocion.solicitud_concurrente");
            Assert.AreEqual(1, ctx.DalPromocion.Solicitudes.Count);
        }

        // Contexto con una promoción Vigente aprobada por Contabilidad y una solicitud de baja pendiente.
        private static Contexto ConSolicitudPendiente(out BE.Promocion promo)
        {
            var ctx = new Contexto();
            promo = Promo(BE.EstadoPromocion.Vigente);
            promo.Observacion = "Aprobada: margen suficiente";
            promo.MotivoBaja = "Erosiona el margen";
            ctx.DalPromocion.Todas.Add(promo);
            ctx.DalPromocion.Dictamenes.Add(new BE.DictamenContable
            {
                IdDictamen = 1, IdPromocion = 42, IdUsuario = 2, Aprobada = true, Observacion = "Aprobada: margen suficiente"
            });
            ctx.DalPromocion.Solicitudes.Add(new BE.SolicitudBajaPromocion
            {
                IdSolicitud = 1, IdPromocion = 42, IdUsuarioSolicita = 3, Motivo = "Erosiona el margen",
                Estado = BE.EstadoSolicitudBaja.Pendiente
            });
            return ctx;
        }

        // Administración atiende la solicitud («Respuesta a la solicitud»)

        [TestMethod]
        public void AtenderSolicitud_DeAcuerdo_DaDeBajaLaPromocionYQuedaAtendidaConLaRespuesta()
        {
            LoginComo(1);
            var ctx = ConSolicitudPendiente(out var promo);
            var solicitud = ctx.Crear().ObtenerSolicitudesBajaPendientes()[0];

            ctx.Crear().AtenderSolicitud("Test", solicitud, " Tiene razón, la damos de baja ", darDeBaja: true);

            Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.Vigente, BE.EstadoPromocion.DadaDeBaja, 1);
            StringAssert.Contains(ctx.DalPromocion.Historial[0].Observacion, "#1");
            var s = ctx.DalPromocion.Solicitudes[0];
            Assert.AreEqual(BE.EstadoSolicitudBaja.Atendida, s.Estado);
            Assert.IsTrue(s.DioDeBaja);
            Assert.AreEqual("Tiene razón, la damos de baja", s.Respuesta);
            Assert.AreEqual("Erosiona el margen", s.Motivo, "La solicitud conserva el motivo del Vendedor.");
            Assert.AreEqual(1, s.IdUsuarioResuelve);
            Assert.IsNotNull(s.FechaResolucion);
        }

        [TestMethod]
        public void AtenderSolicitud_NoDeAcuerdo_QuedaAtendidaYLaPromocionSigueVigente_SinTocarElDictamen()
        {
            LoginComo(1);
            var ctx = ConSolicitudPendiente(out var promo);
            var solicitud = ctx.DalPromocion.Solicitudes[0];

            ctx.Crear().AtenderSolicitud("Test", solicitud, "La promoción sigue rindiendo", darDeBaja: false);

            Assert.AreEqual(BE.EstadoPromocion.Vigente, promo.Estado);
            Assert.AreEqual(0, ctx.DalPromocion.Transiciones, "La promoción no cambió de estado.");
            var r = ctx.DalPromocion.UltimaResolucion;
            Assert.AreEqual(BE.EstadoSolicitudBaja.Atendida, r.Estado);
            Assert.IsFalse(r.DioDeBaja);
            Assert.AreEqual("La promoción sigue rindiendo", r.Respuesta);
            Assert.AreEqual(1, r.IdUsuarioResuelve);
            Assert.IsTrue(solicitud.FueAtendida());
            Assert.AreEqual(0, ctx.Crear().ObtenerSolicitudesBajaPendientes().Count);
            Assert.AreEqual(1, ctx.DalPromocion.Dictamenes.Count, "El dictamen contable no se toca.");
            Assert.AreEqual("Aprobada: margen suficiente", ctx.DalPromocion.Dictamenes[0].Observacion);
        }

        [TestMethod]
        public void AtenderSolicitud_SinRespuesta_LanzaRespuestaRequerida()
        {
            LoginComo(1);
            var ctx = ConSolicitudPendiente(out var promo);
            EsperarError(() => ctx.Crear().AtenderSolicitud("Test", ctx.DalPromocion.Solicitudes[0], "  ", darDeBaja: true),
                "err.bll.promocion.respuesta_requerida");
            Assert.AreEqual(BE.EstadoPromocion.Vigente, promo.Estado);
            Assert.IsTrue(ctx.DalPromocion.Solicitudes[0].EstaPendiente());
        }

        [TestMethod]
        public void AtenderSolicitud_YaAtendida_LanzaSolicitudAtendida()
        {
            LoginComo(1);
            var ctx = ConSolicitudPendiente(out _);
            var solicitud = ctx.DalPromocion.Solicitudes[0];
            ctx.Crear().AtenderSolicitud("Test", solicitud, "Sigue", darDeBaja: false);

            EsperarError(() => ctx.Crear().AtenderSolicitud("Test", solicitud, "Otra respuesta", darDeBaja: true),
                "err.bll.promocion.solicitud_atendida");
            Assert.AreEqual("Sigue", ctx.DalPromocion.Solicitudes[0].Respuesta);
        }

        [TestMethod]
        public void AtenderSolicitud_OtraSesionLaAtendio_LanzaSolicitudAtendida()
        {
            LoginComo(1);
            var ctx = ConSolicitudPendiente(out var promo);
            // Copia en pantalla, cargada antes de que otra sesión la atendiera.
            var enPantalla = new BE.SolicitudBajaPromocion { IdSolicitud = 1, IdPromocion = 42, Motivo = "Erosiona el margen" };
            ctx.DalPromocion.Solicitudes[0].Estado = BE.EstadoSolicitudBaja.Atendida;
            ctx.DalPromocion.Solicitudes[0].Respuesta = "Ya respondida";

            EsperarError(() => ctx.Crear().AtenderSolicitud("Test", enPantalla, "Sigue", darDeBaja: false),
                "err.bll.promocion.solicitud_atendida");
            Assert.IsTrue(enPantalla.EstaPendiente(), "La copia en pantalla no se marca como atendida si la base no la cambió.");
        }

        // (c) Llega la FechaFin → Vencida

        [TestMethod]
        public void CerrarVencidas_VigenteConFechaFinPasada_PasaAVencidaConHistorial_YNoTocaLasDemas()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var vencida = Promo(BE.EstadoPromocion.Vigente);
            vencida.FechaInicio = DateTime.Today.AddDays(-30); vencida.FechaFin = DateTime.Today.AddDays(-1);
            var enCurso = Promo(BE.EstadoPromocion.Vigente);
            enCurso.IdPromocion = 43;
            var enRevision = Promo(BE.EstadoPromocion.EnRevisionContable);
            enRevision.IdPromocion = 44; enRevision.FechaFin = DateTime.Today.AddDays(-1); enRevision.FechaInicio = DateTime.Today.AddDays(-9);
            ctx.DalPromocion.Todas.AddRange(new[] { vencida, enCurso, enRevision });

            int cerradas = ctx.Crear().CerrarVencidas();

            Assert.AreEqual(1, cerradas);
            Assert.AreEqual(BE.EstadoPromocion.Vencida, vencida.Estado);
            Assert.AreEqual(BE.EstadoPromocion.Vigente, enCurso.Estado);
            Assert.AreEqual(BE.EstadoPromocion.EnRevisionContable, enRevision.Estado);
            // El vencimiento lo firma el sistema, no quien abrió la pantalla.
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.Vigente, BE.EstadoPromocion.Vencida, null);
        }

        [TestMethod]
        public void CerrarVencidas_YaCerrada_NoDuplicaElHistorial()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var vencida = Promo(BE.EstadoPromocion.Vigente);
            vencida.FechaInicio = DateTime.Today.AddDays(-30); vencida.FechaFin = DateTime.Today.AddDays(-1);
            ctx.DalPromocion.Todas.Add(vencida);
            var bll = ctx.Crear();

            bll.CerrarVencidas();
            Assert.AreEqual(0, bll.CerrarVencidas());
            Assert.AreEqual(1, ctx.DalPromocion.Historial.Count);
        }

        [TestMethod]
        public void ObtenerTodas_CierraLasVencidasAntesDeListar()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var vencida = Promo(BE.EstadoPromocion.Vigente);
            vencida.FechaInicio = DateTime.Today.AddDays(-30); vencida.FechaFin = DateTime.Today.AddDays(-1);
            ctx.DalPromocion.Todas.Add(vencida);

            var lista = ctx.Crear().ObtenerTodas();

            Assert.AreEqual(BE.EstadoPromocion.Vencida, lista[0].Estado);
            Assert.AreEqual(0, ctx.Crear().ObtenerParaVentas().Count, "Una vencida ya no se ofrece a Ventas.");
        }

        // Cada transición escribe historial: recorrido completo del diagrama.

        [TestMethod]
        public void RecorridoCompleto_CadaTransicionEscribeSuFilaDeHistorial()
        {
            var ctx = new Contexto();
            ctx.DalPromocion.AltaIdGenerado = 42;
            var bll = ctx.Crear();

            LoginComo(1);
            bll.CrearManual("Test", "Promo", "d", BE.TipoDescuento.Porcentaje, 10m, DateTime.Today, DateTime.Today.AddDays(30), 1, 0m, "i");
            var promo = ctx.DalPromocion.UltimoAlta;
            promo.IdPromocion = 42;
            ctx.DalPromocion.Todas.Add(promo);

            LoginComo(2);
            bll.RechazarContable("Test", promo, "Revisar el valor");
            LoginComo(1);
            promo.Valor = 8;
            bll.Reformular("Test", promo);
            LoginComo(2);
            bll.AprobarContable("Test", promo, "Ahora sí");
            LoginComo(3);
            bll.SolicitarBaja("Test", promo, "No rinde");
            LoginComo(1);
            bll.AtenderSolicitud("Test", ctx.DalPromocion.Solicitudes[0], "Sigue rindiendo", darDeBaja: false);
            promo.MotivoBaja = null;   // la pantalla vuelve a cargar la lista: ya no hay solicitud pendiente
            LoginComo(3);
            bll.SolicitarBaja("Test", promo, "Sigue sin rendir");
            LoginComo(1);
            bll.AtenderSolicitud("Test", ctx.DalPromocion.Solicitudes[1], "De acuerdo", darDeBaja: true);

            var esperado = new[]
            {
                "-→EnRevisionContable", "EnRevisionContable→RechazadaContabilidad", "RechazadaContabilidad→EnRevisionContable",
                "EnRevisionContable→Vigente", "Vigente→DadaDeBaja"
            };
            CollectionAssert.AreEqual(esperado, ctx.DalPromocion.ObtenerHistorial(42)
                .ConvertAll(h => $"{(h.EstadoAnterior.HasValue ? h.EstadoAnterior.ToString() : "-")}→{h.EstadoNuevo}"));
            Assert.AreEqual(2, ctx.DalPromocion.Dictamenes.Count);
            Assert.AreEqual(2, ctx.DalPromocion.Solicitudes.Count);
            Assert.IsFalse(ctx.DalPromocion.Solicitudes[0].DioDeBaja);
            Assert.IsTrue(ctx.DalPromocion.Solicitudes[1].DioDeBaja);
            Assert.IsTrue(promo.EsFinal());
        }

        [TestMethod]
        public void RecorridoRechazada_NoSeReformulaYSeDaDeBaja()
        {
            var ctx = new Contexto();
            ctx.DalPromocion.AltaIdGenerado = 42;
            var bll = ctx.Crear();

            LoginComo(1);
            bll.CrearManual("Test", "Promo", "d", BE.TipoDescuento.Porcentaje, 10m, DateTime.Today, DateTime.Today.AddDays(30), 1, 0m, "i");
            var promo = ctx.DalPromocion.UltimoAlta;
            promo.IdPromocion = 42;
            ctx.DalPromocion.Todas.Add(promo);
            LoginComo(2);
            bll.RechazarContable("Test", promo, "No cubre el costo");
            LoginComo(1);
            bll.DarDeBaja("Test", promo, "No se reformula");

            CollectionAssert.AreEqual(
                new[] { "-→EnRevisionContable", "EnRevisionContable→RechazadaContabilidad", "RechazadaContabilidad→DadaDeBaja" },
                ctx.DalPromocion.ObtenerHistorial(42)
                    .ConvertAll(h => $"{(h.EstadoAnterior.HasValue ? h.EstadoAnterior.ToString() : "-")}→{h.EstadoNuevo}"));
        }

        // Máquina de estados (BE)

        [TestMethod]
        public void EstadoPromocion_SoloTieneLosEstadosDelFlujoSimplificado()
            => CollectionAssert.AreEqual(new[] { 0, 1, 2, 4, 6 },
                   ((BE.EstadoPromocion[])Enum.GetValues(typeof(BE.EstadoPromocion))).Select(e => (int)e).ToArray());

        [TestMethod]
        public void TransicionValida_LosEstadosFinalesNoTienenSalida()
        {
            foreach (var final in new[] { BE.EstadoPromocion.DadaDeBaja, BE.EstadoPromocion.Vencida })
                foreach (BE.EstadoPromocion destino in Enum.GetValues(typeof(BE.EstadoPromocion)))
                    Assert.IsFalse(Promo(final).TransicionValida(destino), $"{final} → {destino}");
        }

        [TestMethod]
        public void TransicionValida_EnRevisionContable_SoloVaAVigenteORechazada()
        {
            var p = Promo(BE.EstadoPromocion.EnRevisionContable);
            Assert.IsTrue(p.TransicionValida(BE.EstadoPromocion.Vigente));
            Assert.IsTrue(p.TransicionValida(BE.EstadoPromocion.RechazadaContabilidad));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPromocion.DadaDeBaja));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPromocion.Vencida));
        }

        [TestMethod]
        public void TransicionValida_Rechazada_SeReformulaOSeDaDeBaja()
        {
            var p = Promo(BE.EstadoPromocion.RechazadaContabilidad);
            Assert.IsTrue(p.TransicionValida(BE.EstadoPromocion.EnRevisionContable));
            Assert.IsTrue(p.TransicionValida(BE.EstadoPromocion.DadaDeBaja));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPromocion.Vigente));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPromocion.Vencida));
        }

        [TestMethod]
        public void TransicionValida_Vigente_SeDaDeBajaOVence()
        {
            var p = Promo(BE.EstadoPromocion.Vigente);
            Assert.IsTrue(p.TransicionValida(BE.EstadoPromocion.DadaDeBaja));
            Assert.IsTrue(p.TransicionValida(BE.EstadoPromocion.Vencida));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPromocion.EnRevisionContable));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPromocion.RechazadaContabilidad));
        }

        [TestMethod]
        public void PuedeDarseDeBaja_SoloVigenteORechazada()
        {
            foreach (BE.EstadoPromocion e in Enum.GetValues(typeof(BE.EstadoPromocion)))
                Assert.AreEqual(e == BE.EstadoPromocion.Vigente || e == BE.EstadoPromocion.RechazadaContabilidad,
                    Promo(e).PuedeDarseDeBaja(), e.ToString());
        }

        [TestMethod]
        public void PuedeSolicitarseBaja_VigenteSinSolicitudPendiente()
        {
            Assert.IsTrue(Promo(BE.EstadoPromocion.Vigente).PuedeSolicitarseBaja());
            var conPendiente = Promo(BE.EstadoPromocion.Vigente);
            conPendiente.MotivoBaja = "Ya pedida";
            Assert.IsFalse(conPendiente.PuedeSolicitarseBaja());
            Assert.IsFalse(Promo(BE.EstadoPromocion.RechazadaContabilidad).PuedeSolicitarseBaja());
        }

        [TestMethod]
        public void SeSuperponeCon_SoloOtraVigenteDelMismoPlanConFechasCruzadas()
        {
            var p = Promo(BE.EstadoPromocion.EnRevisionContable);
            var vigente = Promo(BE.EstadoPromocion.Vigente); vigente.IdPromocion = 50;
            var dadaDeBaja = Promo(BE.EstadoPromocion.DadaDeBaja); dadaDeBaja.IdPromocion = 51;
            var otroPlan = Promo(BE.EstadoPromocion.Vigente); otroPlan.IdPromocion = 52; otroPlan.IdPlan = 2;
            Assert.IsTrue(p.SeSuperponeCon(vigente));
            Assert.IsFalse(p.SeSuperponeCon(dadaDeBaja));
            Assert.IsFalse(p.SeSuperponeCon(otroPlan));
        }
    }
}
