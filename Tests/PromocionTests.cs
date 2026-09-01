using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Promocion — PN03 (Métricas, promociones y toma de decisiones). Administración
    /// crea/gestiona (manual o a partir de una sugerencia de Gerencia), Contabilidad aprueba o
    /// rechaza, Vendedor puede sugerir la baja de una promoción vigente y Administración
    /// resuelve esa solicitud (o desactiva directamente).
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

        private static BE.Promocion PromocionEnRevision() => new BE.Promocion
        {
            IdPromocion = 42,
            Nombre = "Promo Test",
            TipoDescuento = BE.TipoDescuento.Porcentaje,
            Valor = 10,
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(30),
            Estado = BE.EstadoPromocion.EnRevisionContable,
            IdPlan = 1
        };

        private static BE.Promocion PromocionVigente() => new BE.Promocion
        {
            IdPromocion = 42,
            Nombre = "Promo Test",
            TipoDescuento = BE.TipoDescuento.Porcentaje,
            Valor = 10,
            FechaInicio = DateTime.Today.AddDays(-5),
            FechaFin = DateTime.Today.AddDays(30),
            Estado = BE.EstadoPromocion.Vigente,
            IdPlan = 1
        };

        private static BE.Promocion PromocionBajaSolicitada() => new BE.Promocion
        {
            IdPromocion = 42,
            Nombre = "Promo Test",
            TipoDescuento = BE.TipoDescuento.Porcentaje,
            Valor = 10,
            FechaInicio = DateTime.Today.AddDays(-5),
            FechaFin = DateTime.Today.AddDays(30),
            Estado = BE.EstadoPromocion.BajaSolicitada,
            IdPlan = 1
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

        // ── AprobarContable ───────────────────────────────────────────────────

        [TestMethod]
        public void AprobarContable_EnRevisionContableConObservacion_CambiaEstadoAVigente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionEnRevision();

            bll.AprobarContable("Test", promocion, "Observación válida");

            Assert.AreEqual(1, ctx.DalPromocion.CambiarEstadoVeces);
            Assert.AreEqual(promocion.IdPromocion, ctx.DalPromocion.UltimoIdPromocionCambiarEstado);
            Assert.AreEqual(BE.EstadoPromocion.Vigente, ctx.DalPromocion.UltimoNuevoEstado);
            Assert.AreEqual("Observación válida", ctx.DalPromocion.UltimaObservacionOMotivo);
        }

        [TestMethod]
        public void AprobarContable_NoEnRevisionContable_LanzaRevisionContableEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionVigente();

            try
            {
                bll.AprobarContable("Test", promocion, "Observación válida");
                Assert.Fail("Debía exigir que la promoción esté En Revisión Contable.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.revisioncontable_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }

        [TestMethod]
        public void AprobarContable_ObservacionVacia_LanzaObservacionRequerida()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionEnRevision();

            try
            {
                bll.AprobarContable("Test", promocion, "   ");
                Assert.Fail("Debía exigir la observación.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.observacion_requerida", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }

        // ── RechazarContable ──────────────────────────────────────────────────

        [TestMethod]
        public void RechazarContable_EnRevisionContableConObservacion_CambiaEstadoARechazadaContabilidad()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionEnRevision();

            bll.RechazarContable("Test", promocion, "No es rentable");

            Assert.AreEqual(1, ctx.DalPromocion.CambiarEstadoVeces);
            Assert.AreEqual(promocion.IdPromocion, ctx.DalPromocion.UltimoIdPromocionCambiarEstado);
            Assert.AreEqual(BE.EstadoPromocion.RechazadaContabilidad, ctx.DalPromocion.UltimoNuevoEstado);
            Assert.AreEqual("No es rentable", ctx.DalPromocion.UltimaObservacionOMotivo);
        }

        [TestMethod]
        public void RechazarContable_NoEnRevisionContable_LanzaRevisionContableEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionVigente();

            try
            {
                bll.RechazarContable("Test", promocion, "No es rentable");
                Assert.Fail("Debía exigir que la promoción esté En Revisión Contable.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.revisioncontable_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }

        [TestMethod]
        public void RechazarContable_ObservacionVacia_LanzaObservacionRequerida()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionEnRevision();

            try
            {
                bll.RechazarContable("Test", promocion, "");
                Assert.Fail("Debía exigir la observación.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.observacion_requerida", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }

        // ── SugerirBaja ───────────────────────────────────────────────────────

        [TestMethod]
        public void SugerirBaja_PromocionVigenteConMotivo_LlamaSolicitarBaja()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionVigente();

            bll.SugerirBaja("Test", promocion, "Bajo rendimiento");

            Assert.AreEqual(1, ctx.DalPromocion.SolicitarBajaVeces);
            Assert.AreEqual(promocion.IdPromocion, ctx.DalPromocion.UltimoIdSolicitarBaja);
            Assert.AreEqual("Bajo rendimiento", ctx.DalPromocion.UltimoMotivoSolicitarBaja);
        }

        [TestMethod]
        public void SugerirBaja_NoVigente_LanzaSugerirBajaEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionEnRevision();

            try
            {
                bll.SugerirBaja("Test", promocion, "Bajo rendimiento");
                Assert.Fail("Debía exigir que la promoción esté Vigente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.sugerirbaja_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.SolicitarBajaVeces);
        }

        [TestMethod]
        public void SugerirBaja_MotivoVacio_LanzaMotivoBajaRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionVigente();

            try
            {
                bll.SugerirBaja("Test", promocion, "   ");
                Assert.Fail("Debía exigir el motivo de la baja.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.motivobaja_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.SolicitarBajaVeces);
        }

        // ── AprobarBaja ───────────────────────────────────────────────────────

        [TestMethod]
        public void AprobarBaja_BajaSolicitada_CambiaEstadoADesactivada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionBajaSolicitada();

            bll.AprobarBaja("Test", promocion);

            Assert.AreEqual(1, ctx.DalPromocion.CambiarEstadoVeces);
            Assert.AreEqual(promocion.IdPromocion, ctx.DalPromocion.UltimoIdPromocionCambiarEstado);
            Assert.AreEqual(BE.EstadoPromocion.Desactivada, ctx.DalPromocion.UltimoNuevoEstado);
        }

        [TestMethod]
        public void AprobarBaja_NoBajaSolicitada_LanzaResolverBajaEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionVigente();

            try
            {
                bll.AprobarBaja("Test", promocion);
                Assert.Fail("Debía exigir que la promoción tenga baja Solicitada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.resolverbaja_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }

        // ── RechazarBaja ──────────────────────────────────────────────────────

        [TestMethod]
        public void RechazarBaja_BajaSolicitadaConMotivo_CambiaEstadoAVigente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionBajaSolicitada();

            bll.RechazarBaja("Test", promocion, "Sigue siendo rentable");

            Assert.AreEqual(1, ctx.DalPromocion.CambiarEstadoVeces);
            Assert.AreEqual(promocion.IdPromocion, ctx.DalPromocion.UltimoIdPromocionCambiarEstado);
            Assert.AreEqual(BE.EstadoPromocion.Vigente, ctx.DalPromocion.UltimoNuevoEstado);
            Assert.AreEqual("Sigue siendo rentable", ctx.DalPromocion.UltimaObservacionOMotivo);
        }

        [TestMethod]
        public void RechazarBaja_MotivoVacio_LanzaMotivoRechazoBajaRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionBajaSolicitada();

            try
            {
                bll.RechazarBaja("Test", promocion, "");
                Assert.Fail("Debía exigir el motivo del rechazo de la baja.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.motivorechazobaja_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }

        // ── Desactivar ────────────────────────────────────────────────────────

        [TestMethod]
        public void Desactivar_PromocionVigente_CambiaEstadoADesactivada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionVigente();

            bll.Desactivar("Test", promocion);

            Assert.AreEqual(1, ctx.DalPromocion.CambiarEstadoVeces);
            Assert.AreEqual(promocion.IdPromocion, ctx.DalPromocion.UltimoIdPromocionCambiarEstado);
            Assert.AreEqual(BE.EstadoPromocion.Desactivada, ctx.DalPromocion.UltimoNuevoEstado);
        }

        [TestMethod]
        public void Desactivar_NoVigente_LanzaDesactivarEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var promocion = PromocionEnRevision();

            try
            {
                bll.Desactivar("Test", promocion);
                Assert.Fail("Debía exigir que la promoción esté Vigente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.promocion.desactivar_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }
    }
}
