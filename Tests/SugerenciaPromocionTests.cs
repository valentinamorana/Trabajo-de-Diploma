using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.SugerenciaPromocion — PN03 (Métricas, promociones y toma de decisiones),
    /// CU-GE-01-Sugerir Promoción a la Administración. Actor: GerenteComercial (Gerencia).
    /// </summary>
    [TestClass]
    public class SugerenciaPromocionTests
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
            public FakeSugerenciaPromocionDAL DalSugerencia = new FakeSugerenciaPromocionDAL();
            public FakePlanSuscripcionDAL DalPlan = new FakePlanSuscripcionDAL { PlanPorId = PlanActivo() };

            public BLL.SugerenciaPromocion Crear()
                => new BLL.SugerenciaPromocion(DalSugerencia, DalPlan);
        }

        // ── RegistrarSugerencia ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void RegistrarSugerencia_DatosValidosConPlan_PersisteYDevuelveElIdGenerado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.AltaIdGenerado = 55;
            var bll = ctx.Crear();

            int id = bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Abandono, 1, null, "Motivo válido", BE.TipoDescuento.Porcentaje, 100m);

            Assert.AreEqual(55, id);
            Assert.AreEqual(1, ctx.DalSugerencia.AltaVeces);
            Assert.AreEqual(1, ctx.DalSugerencia.UltimoAlta.IdPlan);
            Assert.IsNull(ctx.DalSugerencia.UltimoAlta.CategoriaPrenda);
            Assert.AreEqual("Motivo válido", ctx.DalSugerencia.UltimoAlta.Motivo);
            Assert.AreEqual(BE.TipoDescuento.Porcentaje, ctx.DalSugerencia.UltimoAlta.TipoDescuentoSugerido);
            Assert.AreEqual(100m, ctx.DalSugerencia.UltimoAlta.BeneficioEstimado);
            Assert.AreEqual(BE.EstadoSugerencia.Pendiente, ctx.DalSugerencia.UltimoAlta.Estado);
            Assert.AreEqual(BE.OrigenMetrica.Abandono, ctx.DalSugerencia.UltimoAlta.OrigenMetrica, "Guarda el origen de la métrica.");
            Assert.AreEqual(1, ctx.DalSugerencia.UltimoAlta.IdUsuarioAlta, "Guarda quién la registró.");
        }

        [TestMethod]
        public void RegistrarSugerencia_DatosValidosConCategoria_Persiste()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.AltaIdGenerado = 56;
            var bll = ctx.Crear();

            int id = bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Manual, null, "Camisas", "Motivo válido", BE.TipoDescuento.MontoFijo, 50m);

            Assert.AreEqual(56, id);
            Assert.AreEqual(1, ctx.DalSugerencia.AltaVeces);
            Assert.IsNull(ctx.DalSugerencia.UltimoAlta.IdPlan);
            Assert.AreEqual("Camisas", ctx.DalSugerencia.UltimoAlta.CategoriaPrenda);
        }

        [TestMethod]
        public void RegistrarSugerencia_AmbosIdPlanYCategoria_LanzaDestinoInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Manual, 1, "Camisas", "Motivo", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía rechazar una sugerencia con plan y categoría a la vez.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.destino_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void RegistrarSugerencia_NingunoIdPlanNiCategoria_LanzaDestinoInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Manual, null, null, "Motivo", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía rechazar una sugerencia sin plan ni categoría.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.destino_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void RegistrarSugerencia_PlanInexistente_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId = null;
            var bll = ctx.Crear();

            try
            {
                bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Manual, 1, null, "Motivo", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía rechazar un plan inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.plan_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void RegistrarSugerencia_MotivoVacio_LanzaMotivoRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Manual, 1, null, "   ", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía exigir el motivo de la sugerencia.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.motivo_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void RegistrarSugerencia_MotivoNull_LanzaMotivoRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Manual, 1, null, null, BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía exigir el motivo de la sugerencia.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.motivo_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void RegistrarSugerencia_BeneficioInvalido_LanzaBeneficioInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.RegistrarSugerencia("Test", BE.OrigenMetrica.Manual, 1, null, "Motivo", BE.TipoDescuento.Porcentaje, 0m);
                Assert.Fail("Debía exigir un beneficio estimado mayor a cero.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.beneficio_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        // ── ¿Acepta la sugerencia? No → DescartarSugerencia ──────────────────

        private static BE.SugerenciaPromocion Sugerencia(BE.EstadoSugerencia estado) => new BE.SugerenciaPromocion
        {
            IdSugerencia = 5, IdPlan = 1, Motivo = "m", TipoDescuentoSugerido = BE.TipoDescuento.Porcentaje,
            BeneficioEstimado = 100m, Estado = estado
        };

        private static void EsperarError(Action accion, string claveEsperada)
        {
            try { accion(); Assert.Fail("Debía lanzar " + claveEsperada); }
            catch (BE.AppException ex) { Assert.AreEqual(claveEsperada, ex.Clave); }
        }

        [TestMethod]
        public void DescartarSugerencia_Pendiente_LaDescartaConSuMotivo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Pendiente);

            ctx.Crear().DescartarSugerencia("Test", 5, "  No hay margen este trimestre  ");

            Assert.AreEqual(1, ctx.DalSugerencia.DescartarVeces);
            Assert.AreEqual("No hay margen este trimestre", ctx.DalSugerencia.UltimoMotivoDescarte);
        }

        [TestMethod]
        public void DescartarSugerencia_YaEvaluada_LanzaSugerenciaEvaluada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Evaluada);

            EsperarError(() => ctx.Crear().DescartarSugerencia("Test", 5, "motivo"), "err.bll.promocion.sugerencia_evaluada");
            Assert.AreEqual(0, ctx.DalSugerencia.DescartarVeces);
        }

        [TestMethod]
        public void DescartarSugerencia_YaDescartada_LanzaSugerenciaEvaluada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Descartada);

            EsperarError(() => ctx.Crear().DescartarSugerencia("Test", 5, "motivo"), "err.bll.promocion.sugerencia_evaluada");
        }

        [TestMethod]
        public void DescartarSugerencia_SinMotivo_LanzaMotivoDescarteRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Pendiente);

            EsperarError(() => ctx.Crear().DescartarSugerencia("Test", 5, "   "), "err.bll.sugerenciapromocion.motivodescarte_requerido");
            Assert.AreEqual(0, ctx.DalSugerencia.DescartarVeces);
        }

        [TestMethod]
        public void DescartarSugerencia_OtraSesionLaEvaluoAntes_LanzaSugerenciaEvaluada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Pendiente);
            ctx.DalSugerencia.DescartarResultado = false;

            EsperarError(() => ctx.Crear().DescartarSugerencia("Test", 5, "motivo"), "err.bll.promocion.sugerencia_evaluada");
        }

        [TestMethod]
        public void DescartarSugerencia_Inexistente_LanzaSugerenciaInexistente()
        {
            LoginComoAdministrador();
            EsperarError(() => new Contexto().Crear().DescartarSugerencia("Test", 5, "motivo"),
                "err.bll.promocion.sugerencia_inexistente");
        }

        [TestMethod]
        public void TransicionValida_PendientePuedeEvaluarseODescartarse_LaDescartadaEsFinal()
        {
            Assert.IsTrue(Sugerencia(BE.EstadoSugerencia.Pendiente).TransicionValida(BE.EstadoSugerencia.Evaluada));
            Assert.IsTrue(Sugerencia(BE.EstadoSugerencia.Pendiente).TransicionValida(BE.EstadoSugerencia.Descartada));
            Assert.IsFalse(Sugerencia(BE.EstadoSugerencia.Descartada).TransicionValida(BE.EstadoSugerencia.Pendiente));
            Assert.IsFalse(Sugerencia(BE.EstadoSugerencia.Evaluada).TransicionValida(BE.EstadoSugerencia.Descartada));
        }

        [TestMethod]
        public void ValorInicialPromocion_PorcentajeConBeneficioEnPesos_ProponeDiezPorCiento()
        {
            var s = Sugerencia(BE.EstadoSugerencia.Pendiente);
            s.BeneficioEstimado = 16000m;
            Assert.AreEqual(10m, s.ValorInicialPromocion());
            s.TipoDescuentoSugerido = BE.TipoDescuento.MontoFijo;
            Assert.AreEqual(16000m, s.ValorInicialPromocion());
        }
    }
}
