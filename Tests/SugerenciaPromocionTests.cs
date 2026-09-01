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

        // ── Crear ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Crear_DatosValidosConPlan_PersisteYDevuelveElIdGenerado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.AltaIdGenerado = 55;
            var bll = ctx.Crear();

            int id = bll.Crear("Test", 1, null, "Motivo válido", BE.TipoDescuento.Porcentaje, 100m);

            Assert.AreEqual(55, id);
            Assert.AreEqual(1, ctx.DalSugerencia.AltaVeces);
            Assert.AreEqual(1, ctx.DalSugerencia.UltimoAlta.IdPlan);
            Assert.IsNull(ctx.DalSugerencia.UltimoAlta.CategoriaPrenda);
            Assert.AreEqual("Motivo válido", ctx.DalSugerencia.UltimoAlta.Motivo);
            Assert.AreEqual(BE.TipoDescuento.Porcentaje, ctx.DalSugerencia.UltimoAlta.TipoDescuentoSugerido);
            Assert.AreEqual(100m, ctx.DalSugerencia.UltimoAlta.BeneficioEstimado);
            Assert.AreEqual(BE.EstadoSugerencia.Pendiente, ctx.DalSugerencia.UltimoAlta.Estado);
        }

        [TestMethod]
        public void Crear_DatosValidosConCategoria_Persiste()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalSugerencia.AltaIdGenerado = 56;
            var bll = ctx.Crear();

            int id = bll.Crear("Test", null, "Camisas", "Motivo válido", BE.TipoDescuento.MontoFijo, 50m);

            Assert.AreEqual(56, id);
            Assert.AreEqual(1, ctx.DalSugerencia.AltaVeces);
            Assert.IsNull(ctx.DalSugerencia.UltimoAlta.IdPlan);
            Assert.AreEqual("Camisas", ctx.DalSugerencia.UltimoAlta.CategoriaPrenda);
        }

        [TestMethod]
        public void Crear_AmbosIdPlanYCategoria_LanzaDestinoInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.Crear("Test", 1, "Camisas", "Motivo", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía rechazar una sugerencia con plan y categoría a la vez.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.destino_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void Crear_NingunoIdPlanNiCategoria_LanzaDestinoInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.Crear("Test", null, null, "Motivo", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía rechazar una sugerencia sin plan ni categoría.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.destino_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void Crear_PlanInexistente_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId = null;
            var bll = ctx.Crear();

            try
            {
                bll.Crear("Test", 1, null, "Motivo", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía rechazar un plan inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.plan_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void Crear_MotivoVacio_LanzaMotivoRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.Crear("Test", 1, null, "   ", BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía exigir el motivo de la sugerencia.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.motivo_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void Crear_MotivoNull_LanzaMotivoRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.Crear("Test", 1, null, null, BE.TipoDescuento.Porcentaje, 100m);
                Assert.Fail("Debía exigir el motivo de la sugerencia.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.motivo_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }

        [TestMethod]
        public void Crear_BeneficioInvalido_LanzaBeneficioInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.Crear("Test", 1, null, "Motivo", BE.TipoDescuento.Porcentaje, 0m);
                Assert.Fail("Debía exigir un beneficio estimado mayor a cero.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sugerenciapromocion.beneficio_invalido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalSugerencia.AltaVeces);
        }
    }
}
