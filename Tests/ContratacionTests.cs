using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Contratacion — PN02 (Comercialización de la suscripción). Venta capta al cliente
    /// y el plan elegido (CrearContratacion); Caja cobra y recién ahí se formaliza la
    /// suscripción (ConfirmarPago dispara BLL.Cliente.ActivarSuscripcion vía el doble
    /// FakeClienteService); un intento de pago que no se concreta puede cancelar
    /// automáticamente la contratación al llegar al máximo (RegistrarIntentoFallido).
    /// </summary>
    [TestClass]
    public class ContratacionTests
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

        private static BE.Cliente ClienteExistente() => new BE.Cliente
        {
            IdCliente = 10,
            Nombre = "Ana",
            Apellido = "Gómez"
        };

        private static BE.PlanSuscripcion PlanActivo() => new BE.PlanSuscripcion
        {
            IdPlan = 1,
            Nombre = "Básico",
            LimitePrendas = 3,
            Precio = 1000,
            Estado = true
        };

        private static BE.Contratacion ContratacionPendiente() => new BE.Contratacion
        {
            IdContratacion = 7,
            IdCliente = 10,
            IdPlan = 1,
            IdVendedor = 5,
            Modalidad = BE.Builders.ModalidadCobro.Mensual,
            Estado = BE.EstadoContratacion.PendientePago,
            NombreCliente = "Ana Gómez",
            NombrePlan = "Básico"
        };

        private class Contexto
        {
            public FakeContratacionDAL DalContratacion = new FakeContratacionDAL();
            public FakeClienteDAL DalCliente = new FakeClienteDAL { ClientePorId = ClienteExistente() };
            public FakeEmpleadoDAL DalEmpleado = new FakeEmpleadoDAL { EmpleadoPorUsuario = new BE.Empleado { IdEmpleado = 5 } };
            public FakePlanSuscripcionDAL DalPlan = new FakePlanSuscripcionDAL { PlanPorId = PlanActivo() };
            public FakeClienteService ClienteBLL = new FakeClienteService();

            public BLL.Contratacion Crear()
                => new BLL.Contratacion(DalContratacion, DalCliente, DalEmpleado, DalPlan, ClienteBLL);
        }

        // ── CrearContratacion ─────────────────────────────────────────────────

        [TestMethod]
        public void CrearContratacion_DatosValidos_PersisteYDevuelveElIdGenerado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.AltaIdGenerado = 99;
            var bll = ctx.Crear();

            int id = bll.CrearContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(99, id);
            Assert.AreEqual(1, ctx.DalContratacion.AltaVeces);
            Assert.AreEqual(10, ctx.DalContratacion.UltimoAlta.IdCliente);
            Assert.AreEqual(1, ctx.DalContratacion.UltimoAlta.IdPlan);
            Assert.AreEqual(5, ctx.DalContratacion.UltimoAlta.IdVendedor);
        }

        [TestMethod]
        public void CrearContratacion_ClienteInexistente_LanzaClienteInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = null;
            var bll = ctx.Crear();

            try
            {
                bll.CrearContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual);
                Assert.Fail("Debía rechazar un cliente inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.contratacion.cliente_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void CrearContratacion_PlanInexistente_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId = null;
            var bll = ctx.Crear();

            try
            {
                bll.CrearContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual);
                Assert.Fail("Debía rechazar un plan inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.contratacion.plan_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void CrearContratacion_PlanInactivo_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var plan = PlanActivo();
            plan.Estado = false;
            ctx.DalPlan.PlanPorId = plan;
            var bll = ctx.Crear();

            try
            {
                bll.CrearContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual);
                Assert.Fail("Debía rechazar un plan inactivo.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.contratacion.plan_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void CrearContratacion_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual);
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }

        // ── ConfirmarPago ─────────────────────────────────────────────────────

        [TestMethod]
        public void ConfirmarPago_DatosValidos_MarcaPagadaGeneraComprobanteYActivaSuscripcion()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();

            bll.ConfirmarPago("Test", contratacion, "Efectivo");

            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarPagoVeces);
            Assert.AreEqual(contratacion.IdContratacion, ctx.DalContratacion.UltimoIdContratacionConfirmado);
            Assert.AreEqual(5, ctx.DalContratacion.UltimoIdCaja);
            Assert.AreEqual("Efectivo", ctx.DalContratacion.UltimoMedioPago);
            Assert.IsFalse(string.IsNullOrWhiteSpace(ctx.DalContratacion.UltimoNumeroComprobante));
            Assert.AreEqual(1, ctx.ClienteBLL.ActivarSuscripcionVeces);
            Assert.AreEqual(contratacion.IdPlan, ctx.ClienteBLL.UltimoIdPlan);
            Assert.AreEqual(contratacion.Modalidad, ctx.ClienteBLL.UltimaModalidad);
        }

        [TestMethod]
        public void ConfirmarPago_ContratacionNoPendientePago_LanzaCobrarEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();
            contratacion.Estado = BE.EstadoContratacion.Pagada;

            try
            {
                bll.ConfirmarPago("Test", contratacion, "Efectivo");
                Assert.Fail("Debía exigir que la contratación esté Pendiente de pago.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.contratacion.cobrar_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarPagoVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarPago_MedioPagoVacio_LanzaMedioPagoRequerido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();

            try
            {
                bll.ConfirmarPago("Test", contratacion, "   ");
                Assert.Fail("Debía exigir el medio de pago.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.contratacion.medio_pago_requerido", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarPagoVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarPago_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();

            try
            {
                bll.ConfirmarPago("Test", contratacion, "Efectivo");
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }

        // ── RegistrarIntentoFallido ───────────────────────────────────────────

        [TestMethod]
        public void RegistrarIntentoFallido_PorDebajoDelMaximo_NoCancela()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.IntentosDespuesDeIncrementar = 2; // por debajo del máximo (3)
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();

            bll.RegistrarIntentoFallido("Test", contratacion);

            Assert.AreEqual(1, ctx.DalContratacion.IncrementarIntentoVeces);
            Assert.AreEqual(0, ctx.DalContratacion.CancelarVeces);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_TercerIntento_Cancela()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.IntentosDespuesDeIncrementar = 3; // llega al máximo
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();

            bll.RegistrarIntentoFallido("Test", contratacion);

            Assert.AreEqual(1, ctx.DalContratacion.IncrementarIntentoVeces);
            Assert.AreEqual(1, ctx.DalContratacion.CancelarVeces);
            Assert.AreEqual(contratacion.IdContratacion, ctx.DalContratacion.UltimoCancelarId);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_ContratacionNoPendientePago_LanzaCobrarEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();
            contratacion.Estado = BE.EstadoContratacion.Cancelada;

            try
            {
                bll.RegistrarIntentoFallido("Test", contratacion);
                Assert.Fail("Debía exigir que la contratación esté Pendiente de pago.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.contratacion.cobrar_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalContratacion.IncrementarIntentoVeces);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var contratacion = ContratacionPendiente();

            try
            {
                bll.RegistrarIntentoFallido("Test", contratacion);
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }
    }
}
