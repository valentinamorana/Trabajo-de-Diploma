using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Promocion — PN03 (Métricas, promociones y toma de decisiones). Una prueba por rama del
    /// diagrama de actividad aprobado: crear (desde sugerencia o manual) → Validar → ¿Aprueba?
    /// (quien crea no dictamina) → ¿Reformular? / Descartar; región de vigencia: solicitud de baja
    /// → ¿Aprueba la baja?, desactivación directa y vencimiento. Cada transición escribe historial.
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

            public BLL.Promocion Crear()
                => new BLL.Promocion(DalPromocion, DalSugerencia, DalPlan);
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
                DateTime.Today, DateTime.Today.AddDays(30), 1, null, 500m, "impacto");

            Assert.AreEqual(77, id);
            Assert.AreEqual(1, ctx.DalPromocion.AltaVeces);
            Assert.AreEqual(BE.EstadoPromocion.EnRevisionContable, ctx.DalPromocion.UltimoAlta.Estado);
            Assert.IsNull(ctx.DalPromocion.UltimoAlta.IdSugerenciaOrigen);
            Assert.AreEqual(1, ctx.DalPromocion.UltimoAlta.IdPlan);
        }

        [TestMethod]
        public void CrearManual_DatosValidosConCategoria_Persiste()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPromocion.AltaIdGenerado = 78;
            var bll = ctx.Crear();

            int id = bll.CrearManual("Test", "Promo Camisas", "desc", BE.TipoDescuento.MontoFijo, 20m,
                DateTime.Today, DateTime.Today.AddDays(10), null, "Camisas", 100m, "impacto");

            Assert.AreEqual(78, id);
            Assert.AreEqual(1, ctx.DalPromocion.AltaVeces);
            Assert.IsNull(ctx.DalPromocion.UltimoAlta.IdPlan);
            Assert.AreEqual("Camisas", ctx.DalPromocion.UltimoAlta.CategoriaPrenda);
        }

        [TestMethod]
        public void CrearManual_AmbosDestinos_LanzaDestinoInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 10m,
                    DateTime.Today, DateTime.Today.AddDays(30), 1, "Camisas", 500m, "impacto");
                Assert.Fail("Debía rechazar una promoción con plan y categoría a la vez.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.destino_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearManual_NingunDestino_LanzaDestinoInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearManual("Test", "Promo Test", "desc", BE.TipoDescuento.Porcentaje, 10m,
                    DateTime.Today, DateTime.Today.AddDays(30), null, null, 500m, "impacto");
                Assert.Fail("Debía rechazar una promoción sin plan ni categoría.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.destino_invalido", ex.Clave);
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
                    DateTime.Today, DateTime.Today.AddDays(30), 1, null, 500m, "impacto");
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
                    DateTime.Today, DateTime.Today.AddDays(30), 1, null, 500m, "impacto");
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
                    DateTime.Today, DateTime.Today.AddDays(30), 1, null, 500m, "impacto");
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
                    DateTime.Today, DateTime.Today.AddDays(30), 1, null, 500m, "impacto");
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
                    DateTime.Today, DateTime.Today.AddDays(-1), 1, null, 500m, "impacto");
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
                CategoriaPrenda = null,
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
            Assert.IsNull(ctx.DalPromocion.UltimoAlta.CategoriaPrenda);
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
                    DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1), 1, null, 500m, "impacto"),
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
                DateTime.Today, DateTime.Today.AddDays(10), 1, null, 0m, "i");

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

        // ¿Reformular? No → Descartar promoción

        [TestMethod]
        public void DescartarPromocion_TrasElRechazo_PasaADescartadaConElMotivoEnElHistorial()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.RechazadaContabilidad);
            ctx.DalPromocion.Todas.Add(promo);

            ctx.Crear().DescartarPromocion("Test", promo, "No conviene al negocio");

            Assert.AreEqual(BE.EstadoPromocion.Descartada, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.RechazadaContabilidad, BE.EstadoPromocion.Descartada, 1);
            Assert.AreEqual("No conviene al negocio", ctx.Crear().ObtenerDescarte(42).Observacion);
        }

        [TestMethod]
        public void DescartarPromocion_SinMotivo_LanzaMotivoDescarteRequerido()
        {
            LoginComo(1);
            var ctx = new Contexto();
            EsperarError(() => ctx.Crear().DescartarPromocion("Test", Promo(BE.EstadoPromocion.RechazadaContabilidad), " "),
                "err.bll.promocion.motivodescarte_requerido");
            Assert.AreEqual(0, ctx.DalPromocion.Transiciones);
        }

        [TestMethod]
        public void DescartarPromocion_NoRechazada_LanzaDescartarEstado()
        {
            LoginComo(1);
            EsperarError(() => new Contexto().Crear().DescartarPromocion("Test", Promo(BE.EstadoPromocion.EnRevisionContable), "m"),
                "err.bll.promocion.descartar_estado");
        }

        // (a) Vendedor solicita la baja → ¿Aprueba la baja?

        [TestMethod]
        public void SolicitarBaja_Vigente_GuardaLaSolicitudYPasaABajaSolicitada()
        {
            LoginComo(3);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.Vigente);

            int idSolicitud = ctx.Crear().SolicitarBaja("Test", promo, " Se superpone con otra ");

            Assert.AreEqual(1, idSolicitud);
            var s = ctx.DalPromocion.Solicitudes[0];
            Assert.AreEqual("Se superpone con otra", s.Motivo);
            Assert.AreEqual(3, s.IdUsuarioSolicita);
            Assert.AreEqual(BE.EstadoSolicitudBaja.Pendiente, s.Estado);
            Assert.AreEqual(BE.EstadoPromocion.BajaSolicitada, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.Vigente, BE.EstadoPromocion.BajaSolicitada, 3);
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

        // Contexto con una promoción Vigente aprobada por Contabilidad y una solicitud de baja pendiente.
        private static Contexto ConBajaSolicitada(out BE.Promocion promo)
        {
            var ctx = new Contexto();
            promo = Promo(BE.EstadoPromocion.BajaSolicitada);
            promo.Observacion = "Aprobada: margen suficiente";
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

        [TestMethod]
        public void AprobarBaja_GuardaLaResolucionAprobadaYDesactiva()
        {
            LoginComo(1);
            var ctx = ConBajaSolicitada(out var promo);

            int idSolicitud = ctx.Crear().AprobarBaja("Test", promo, "");

            Assert.AreEqual(1, idSolicitud);
            var r = ctx.DalPromocion.UltimaResolucion;
            Assert.AreEqual(BE.EstadoSolicitudBaja.Aprobada, r.Estado);
            Assert.AreEqual(1, r.IdUsuarioResuelve);
            Assert.IsNotNull(r.FechaResolucion);
            Assert.AreEqual("Erosiona el margen", r.Motivo, "La solicitud conserva el motivo del Vendedor.");
            Assert.AreEqual(BE.EstadoPromocion.Desactivada, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.BajaSolicitada, BE.EstadoPromocion.Desactivada, 1);
        }

        [TestMethod]
        public void RechazarBaja_GuardaLaResolucionConMotivoYVuelveAVigente_SinPerderLaObservacionContable()
        {
            LoginComo(1);
            var ctx = ConBajaSolicitada(out var promo);

            ctx.Crear().RechazarBaja("Test", promo, "La promoción sigue rindiendo");

            var r = ctx.DalPromocion.UltimaResolucion;
            Assert.AreEqual(BE.EstadoSolicitudBaja.Rechazada, r.Estado);
            Assert.AreEqual("La promoción sigue rindiendo", r.MotivoResolucion);
            Assert.AreEqual(BE.EstadoPromocion.Vigente, promo.Estado);
            Assert.AreEqual(1, ctx.DalPromocion.Dictamenes.Count, "El dictamen contable no se toca.");
            Assert.AreEqual("Aprobada: margen suficiente", ctx.DalPromocion.Dictamenes[0].Observacion);
            Assert.AreEqual("Aprobada: margen suficiente", promo.Observacion);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.BajaSolicitada, BE.EstadoPromocion.Vigente, 1);
        }

        [TestMethod]
        public void RechazarBaja_SinMotivo_LanzaMotivoRechazoBajaRequerido()
        {
            LoginComo(1);
            var ctx = ConBajaSolicitada(out var promo);
            EsperarError(() => ctx.Crear().RechazarBaja("Test", promo, "  "), "err.bll.promocion.motivorechazobaja_requerido");
            Assert.AreEqual(BE.EstadoPromocion.BajaSolicitada, promo.Estado);
        }

        [TestMethod]
        public void AprobarBaja_SinSolicitudPendiente_LanzaSolicitudInexistente()
        {
            LoginComo(1);
            EsperarError(() => new Contexto().Crear().AprobarBaja("Test", Promo(BE.EstadoPromocion.BajaSolicitada), null),
                "err.bll.promocion.solicitud_inexistente");
        }

        [TestMethod]
        public void AprobarBaja_NoBajaSolicitada_LanzaResolverBajaEstado()
        {
            LoginComo(1);
            EsperarError(() => new Contexto().Crear().AprobarBaja("Test", Promo(BE.EstadoPromocion.Vigente), null),
                "err.bll.promocion.resolverbaja_estado");
        }

        // (b) Administración desactiva directamente

        [TestMethod]
        public void Desactivar_VigenteConMotivo_DesactivaYGuardaElMotivoEnElHistorial()
        {
            LoginComo(1);
            var ctx = new Contexto();
            var promo = Promo(BE.EstadoPromocion.Vigente);

            ctx.Crear().Desactivar("Test", promo, "Cambio de estrategia comercial");

            Assert.AreEqual(BE.EstadoPromocion.Desactivada, promo.Estado);
            AssertHistorial(ctx.DalPromocion.Historial[0], BE.EstadoPromocion.Vigente, BE.EstadoPromocion.Desactivada, 1);
            Assert.AreEqual("Cambio de estrategia comercial", ctx.DalPromocion.Historial[0].Observacion);
        }

        [TestMethod]
        public void Desactivar_SinMotivo_LanzaMotivoDesactivarRequerido()
        {
            LoginComo(1);
            var ctx = new Contexto();
            EsperarError(() => ctx.Crear().Desactivar("Test", Promo(BE.EstadoPromocion.Vigente), null),
                "err.bll.promocion.motivodesactivar_requerido");
            Assert.AreEqual(0, ctx.DalPromocion.Transiciones);
        }

        [TestMethod]
        public void Desactivar_NoVigente_LanzaDesactivarEstado()
        {
            LoginComo(1);
            EsperarError(() => new Contexto().Crear().Desactivar("Test", Promo(BE.EstadoPromocion.BajaSolicitada), "m"),
                "err.bll.promocion.desactivar_estado");
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
            bll.CrearManual("Test", "Promo", "d", BE.TipoDescuento.Porcentaje, 10m, DateTime.Today, DateTime.Today.AddDays(30), 1, null, 0m, "i");
            var promo = ctx.DalPromocion.UltimoAlta;
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
            bll.RechazarBaja("Test", promo, "Sigue rindiendo");
            bll.Desactivar("Test", promo, "Fin de campaña");

            var esperado = new[]
            {
                "-→EnRevisionContable", "EnRevisionContable→RechazadaContabilidad", "RechazadaContabilidad→EnRevisionContable",
                "EnRevisionContable→Vigente", "Vigente→BajaSolicitada", "BajaSolicitada→Vigente", "Vigente→Desactivada"
            };
            CollectionAssert.AreEqual(esperado, ctx.DalPromocion.ObtenerHistorial(42)
                .ConvertAll(h => $"{(h.EstadoAnterior.HasValue ? h.EstadoAnterior.ToString() : "-")}→{h.EstadoNuevo}"));
            Assert.AreEqual(2, ctx.DalPromocion.Dictamenes.Count);
            Assert.IsTrue(promo.EsFinal());
        }

        // Máquina de estados (BE)

        [TestMethod]
        public void TransicionValida_LosEstadosFinalesNoTienenSalida()
        {
            foreach (var final in new[] { BE.EstadoPromocion.Desactivada, BE.EstadoPromocion.Descartada, BE.EstadoPromocion.Vencida })
                foreach (BE.EstadoPromocion destino in Enum.GetValues(typeof(BE.EstadoPromocion)))
                    Assert.IsFalse(Promo(final).TransicionValida(destino), $"{final} → {destino}");
        }

        [TestMethod]
        public void TransicionValida_SigueElDiagrama()
        {
            Assert.IsTrue(Promo(BE.EstadoPromocion.EnRevisionContable).TransicionValida(BE.EstadoPromocion.Vigente));
            Assert.IsTrue(Promo(BE.EstadoPromocion.RechazadaContabilidad).TransicionValida(BE.EstadoPromocion.Descartada));
            Assert.IsTrue(Promo(BE.EstadoPromocion.Vigente).TransicionValida(BE.EstadoPromocion.Vencida));
            Assert.IsTrue(Promo(BE.EstadoPromocion.BajaSolicitada).TransicionValida(BE.EstadoPromocion.Vigente));
            Assert.IsFalse(Promo(BE.EstadoPromocion.EnRevisionContable).TransicionValida(BE.EstadoPromocion.Desactivada));
            Assert.IsFalse(Promo(BE.EstadoPromocion.BajaSolicitada).TransicionValida(BE.EstadoPromocion.Vencida));
        }
    }
}
