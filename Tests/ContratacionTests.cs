using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Contratacion — PN02 (Comercialización de la suscripción), una prueba por rama del
    /// diagrama de actividad aprobado. Carril Vendedor: Identificar cliente, Presentar planes,
    /// Asentar desistimiento, ¿Contratación válida? y Registrar contratación. Carril Caja:
    /// Confirmar cobro → Emitir comprobante → Activar suscripción (vía el doble
    /// FakeClienteService) → ¿Referido?; o Registrar intento → ¿Alcanzó el máximo de 3? → Cancelar.
    /// </summary>
    [TestClass]
    public class ContratacionTests
    {
        private const int IdEfectivo = 1;
        private const int IdTarjeta  = 2;   // Tarjeta de débito: no financia
        private const int IdTarjetaCredito = 4;
        // PlanCuotas del seed: 1 → 1 cuota 0 %; 2 → 3 cuotas 5 %; 3 → 6 cuotas 10 %; 4 → 12 cuotas 20 %.
        private const int Plan1Cuota = 1, Plan3Cuotas = 2, Plan6Cuotas = 3, Plan12Cuotas = 4;

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

        // Mismo helper que PedidoTests: usuario no administrador con patentes explícitas.
        private static void LoginComo(string perfil, params string[] patentes)
        {
            var u = new BE.Usuario
            {
                Id = 7,
                Username = perfil.ToLowerInvariant(),
                Perfil = perfil,
                Contraseña = Encriptador.Hash("Clave1!")
            };
            foreach (var nm in patentes)
                u.Permisos.Add(new BE.Permiso { NombreMenu = nm });
            SessionManager.Login(u);
        }

        private static void LoginComoVendedor() => LoginComo("Vendedor", BE.Patentes.Clientes, BE.Patentes.ClientesEditar);
        private static void LoginComoCaja()     => LoginComo("Caja", BE.Patentes.Caja, BE.Patentes.CajaEditar);

        private static void EsperarError(Action accion, string claveEsperada)
        {
            try { accion(); Assert.Fail("Debía lanzar " + claveEsperada); }
            catch (BE.AppException ex) { Assert.AreEqual(claveEsperada, ex.Clave); }
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
            IdVendedor = 6,   // distinto del empleado de Caja (5): quien vende no cobra
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
            public FakeCargoPrendaDAL DalCargos = new FakeCargoPrendaDAL();

            public BLL.Contratacion Crear()
                => new BLL.Contratacion(DalContratacion, DalCliente, DalEmpleado, DalPlan, ClienteBLL) { DalCargos = DalCargos };

            // Deja la contratación pendiente como "estado fresco de la BD" y la devuelve.
            public BE.Contratacion Pendiente()
            {
                var c = ContratacionPendiente();
                DalContratacion.ContratacionPorId = c;
                return c;
            }
        }

        // ══ Vendedor: Identificar cliente → ¿Registrado? ═══════════════════════

        [TestMethod]
        public void IdentificarCliente_Registrado_DevuelveLosClientesDelServicioDeClientes()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.ClienteBLL.ClientesEncontrados = new List<BE.Cliente> { ClienteExistente() };

            var r = ctx.Crear().IdentificarCliente("30111222");

            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(10, r[0].IdCliente);
            Assert.AreEqual(1, ctx.ClienteBLL.BuscarPorIdentificacionVeces, "Delega la búsqueda en BLL.Cliente.");
            Assert.AreEqual("30111222", ctx.ClienteBLL.UltimaIdentificacion);
        }

        [TestMethod]
        public void IdentificarCliente_NoRegistrado_DevuelveListaVacia()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();

            var r = ctx.Crear().IdentificarCliente("Inexistente");

            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.Count, "Lista vacía = No registrado: el Vendedor lo da de alta en el ABM.");
            Assert.AreEqual(1, ctx.ClienteBLL.BuscarPorIdentificacionVeces);
        }

        // ══ Vendedor: Presentar planes («Planes disponibles») ═══════════════════

        [TestMethod]
        public void PresentarPlanes_DevuelveSoloLosPlanesActivos()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.Planes = new List<BE.PlanSuscripcion>
            {
                new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico",  Precio = 1000, LimitePrendas = 3, Estado = true },
                new BE.PlanSuscripcion { IdPlan = 2, Nombre = "Viejo",   Precio = 500,  LimitePrendas = 1, Estado = false },
                new BE.PlanSuscripcion { IdPlan = 3, Nombre = "Premium", Precio = 3000, LimitePrendas = 8, Estado = true }
            };

            var planes = ctx.Crear().PresentarPlanes();

            Assert.AreEqual(2, planes.Count);
            Assert.IsTrue(planes.TrueForAll(p => p.Estado));
            Assert.IsFalse(planes.Exists(p => p.IdPlan == 2));
        }

        // ══ Vendedor: ¿Elige plan y modalidad? No → Asentar desistimiento ═══════

        [TestMethod]
        public void AsentarDesistimiento_ConPlanConsiderado_GuardaPlanModalidadMotivoYVendedor()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.AltaDesistimientoIdGenerado = 33;

            int id = ctx.Crear().AsentarDesistimiento("Test", 10, 1, BE.Builders.ModalidadCobro.Trimestral, "  Le pareció caro  ");

            Assert.AreEqual(33, id);
            Assert.AreEqual(1, ctx.DalContratacion.AltaDesistimientoVeces);
            var d = ctx.DalContratacion.UltimoDesistimiento;
            Assert.AreEqual(10, d.IdCliente);
            Assert.AreEqual(1, d.IdPlan);
            Assert.AreEqual(BE.Builders.ModalidadCobro.Trimestral, d.Modalidad);
            Assert.AreEqual("Le pareció caro", d.Motivo);
            Assert.AreEqual(5, d.IdVendedor);
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces, "Un desistimiento no genera contratación.");
        }

        [TestMethod]
        public void AsentarDesistimiento_SinPlan_IgnoraLaModalidad()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();

            ctx.Crear().AsentarDesistimiento("Test", 10, null, BE.Builders.ModalidadCobro.Anual, "Solo vino a consultar");

            var d = ctx.DalContratacion.UltimoDesistimiento;
            Assert.IsNull(d.IdPlan);
            Assert.IsNull(d.Modalidad, "Sin plan elegido, la modalidad no tiene sentido y no se guarda.");
            Assert.AreEqual(1, ctx.DalContratacion.AltaDesistimientoVeces);
        }

        [TestMethod]
        public void AsentarDesistimiento_SinMotivo_LanzaDesistirSinMotivo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();

            EsperarError(() => ctx.Crear().AsentarDesistimiento("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual, "   "),
                "err.bll.contratacion.desistir_sin_motivo");
            Assert.AreEqual(0, ctx.DalContratacion.AltaDesistimientoVeces);
        }

        [TestMethod]
        public void AsentarDesistimiento_ClienteInexistente_LanzaClienteInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = null;

            EsperarError(() => ctx.Crear().AsentarDesistimiento("Test", 10, null, null, "No le interesa"),
                "err.bll.contratacion.cliente_inexistente");
            Assert.AreEqual(0, ctx.DalContratacion.AltaDesistimientoVeces);
        }

        // ══ Vendedor: ¿Contratación válida? (No → Informar motivo) ══════════════

        [TestMethod]
        public void ValidarContratacion_ClienteInexistente_LanzaClienteInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = null;

            EsperarError(() => ctx.Crear().ValidarContratacion(10, 1), "err.bll.contratacion.cliente_inexistente");
        }

        [TestMethod]
        public void ValidarContratacion_PlanInactivo_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId.Estado = false;

            EsperarError(() => ctx.Crear().ValidarContratacion(10, 1), "err.bll.contratacion.plan_inexistente");
        }

        [TestMethod]
        public void ValidarContratacion_PlanConMenosCupoQueLasPrendasEnUso_LanzaPlanInsuficiente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId.StockUtilizado = 5; // el plan Básico permite 3

            EsperarError(() => ctx.Crear().ValidarContratacion(10, 1), "err.bll.cliente.plan_insuficiente");
        }

        [TestMethod]
        public void ValidarContratacion_ClienteYaTienePendiente_LanzaPendienteExistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.PendientesDePago = new List<BE.Contratacion>
            {
                new BE.Contratacion { IdContratacion = 5, IdCliente = 10, Estado = BE.EstadoContratacion.PendientePago }
            };

            EsperarError(() => ctx.Crear().ValidarContratacion(10, 1), "err.bll.contratacion.pendiente_existente");
        }

        [TestMethod]
        public void ValidarContratacion_Valida_DevuelveElPlan_SinPersistirNada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();

            var plan = ctx.Crear().ValidarContratacion(10, 1);

            Assert.IsNotNull(plan);
            Assert.AreEqual(1, plan.IdPlan);
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces, "Validar no registra la contratación.");
        }

        // ══ Vendedor: Registrar contratación («Orden de cobro») ═════════════════

        [TestMethod]
        public void RegistrarContratacion_DatosValidos_PersistePendienteDePagoConElVendedor()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.AltaIdGenerado = 99;

            int id = ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Trimestral);

            Assert.AreEqual(99, id);
            Assert.AreEqual(1, ctx.DalContratacion.AltaVeces);
            var alta = ctx.DalContratacion.UltimoAlta;
            Assert.AreEqual(10, alta.IdCliente);
            Assert.AreEqual(1, alta.IdPlan);
            Assert.AreEqual(5, alta.IdVendedor, "El vendedor es el empleado vinculado al usuario en sesión.");
            Assert.AreEqual(BE.Builders.ModalidadCobro.Trimestral, alta.Modalidad);
            Assert.AreEqual(BE.EstadoContratacion.PendientePago, alta.Estado);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces, "La suscripción no queda vigente hasta el cobro.");
        }

        [TestMethod]
        public void RegistrarContratacion_PlanConMenosCupoQueLasPrendasEnUso_LanzaPlanInsuficiente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId.StockUtilizado = 5; // el plan Básico permite 3

            EsperarError(() => ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.cliente.plan_insuficiente");
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void RegistrarContratacion_ClienteInexistente_LanzaClienteInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = null;

            EsperarError(() => ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.contratacion.cliente_inexistente");
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void RegistrarContratacion_PlanInexistente_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId = null;

            EsperarError(() => ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.contratacion.plan_inexistente");
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void RegistrarContratacion_PlanInactivo_LanzaPlanInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId.Estado = false;

            EsperarError(() => ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.contratacion.plan_inexistente");
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void RegistrarContratacion_ClienteYaTienePendiente_LanzaPendienteExistente_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.PendientesDePago = new List<BE.Contratacion>
            {
                new BE.Contratacion { IdContratacion = 5, IdCliente = 10, Estado = BE.EstadoContratacion.PendientePago }
            };

            EsperarError(() => ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.contratacion.pendiente_existente");
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void RegistrarContratacion_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var ctx = new Contexto();

            EsperarError(() => ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.sesion_expirada");
        }

        // ══ Caja: ¿Se concreta el pago? Sí → Confirmar cobro → Comprobante → Activar ══

        [TestMethod]
        public void ConfirmarCobro_DatosValidos_EmiteComprobanteActivaSuscripcionYRegistraVigencia()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Trimestral;

            var liq = ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjeta);

            // Confirmar cobro
            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(contratacion.IdContratacion, ctx.DalContratacion.UltimoIdContratacionConfirmado);
            Assert.AreEqual(5, ctx.DalContratacion.UltimoIdCaja);
            Assert.AreEqual(IdTarjeta, ctx.DalContratacion.UltimoIdMedioPago);
            // Emitir comprobante: CMP-NNNNNN-AAAAMMDD
            string comprobante = ctx.DalContratacion.UltimoNumeroComprobante;
            Assert.IsTrue(Regex.IsMatch(comprobante, @"^CMP-\d{6}-\d{8}$"), "Formato inválido: " + comprobante);
            Assert.AreEqual($"CMP-000007-{DateTime.Now:yyyyMMdd}", comprobante);
            Assert.AreEqual(comprobante, liq.NumeroComprobante);
            // Activar suscripción
            Assert.AreEqual(1, ctx.ClienteBLL.ActivarSuscripcionVeces);
            Assert.AreEqual(contratacion.IdPlan, ctx.ClienteBLL.UltimoIdPlan);
            Assert.AreEqual(BE.Builders.ModalidadCobro.Trimestral, ctx.ClienteBLL.UltimaModalidad);
            // Constancia de suscripción: el período que calculó el Builder (inicio → vencimiento)
            Assert.AreEqual(1, ctx.DalContratacion.RegistrarVigenciaVeces);
            var hasta = ctx.DalContratacion.UltimaVigenciaHasta.Value;
            var desde = ctx.DalContratacion.UltimaVigenciaDesde.Value;
            Assert.IsTrue(desde < hasta);
            Assert.AreEqual(hasta, liq.VigenciaHasta);
            Assert.AreEqual(desde, liq.VigenciaDesde);
            // ¿Referido? No
            Assert.IsNull(liq.ReferenteAcreditado);
            Assert.AreEqual(0, ctx.DalContratacion.ReabrirPagoVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_ClienteReferidoSinAcreditar_InformaElReferenteAcreditado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId.IdClienteReferente = 5;
            ctx.DalCliente.ClientePorId.BeneficioReferidoOtorgado = false;
            ctx.DalCliente.OtrosClientesPorId[5] = new BE.Cliente { IdCliente = 5, Nombre = "Ref", Apellido = "Erente" };
            var contratacion = ctx.Pendiente();

            var liq = ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo);

            Assert.AreEqual("Ref Erente", liq.ReferenteAcreditado);
            Assert.AreEqual(1, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_BeneficioDeReferidoYaOtorgado_NoInformaReferente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId.IdClienteReferente = 5;
            ctx.DalCliente.ClientePorId.BeneficioReferidoOtorgado = true;
            ctx.DalCliente.OtrosClientesPorId[5] = new BE.Cliente { IdCliente = 5, Nombre = "Ref", Apellido = "Erente" };
            var contratacion = ctx.Pendiente();

            var liq = ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo);

            Assert.IsNull(liq.ReferenteAcreditado, "El crédito al referente se acredita una sola vez.");
        }

        [TestMethod]
        public void ConfirmarCobro_MedioDePagoInexistente_LanzaMedioInvalido_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, 99), "err.bll.contratacion.medio_invalido");
            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, 0),  "err.bll.contratacion.medio_invalido");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_ContratacionInexistente_LanzaInexistente_SinTocarElDAL()
        {
            // Revalida contra el estado FRESCO de la BD (dalContratacion.ObtenerPorId), no el
            // objeto que trae el caller — cubre el caso de que otra sesión de Caja ya la haya
            // cobrado/eliminado en el ínterin. ContratacionPorId queda null (default del Fake).
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ContratacionPendiente();

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo), "err.bll.contratacion.inexistente");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_ContratacionNoPendientePago_LanzaCobrarEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            contratacion.Estado = BE.EstadoContratacion.Pagada;

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo), "err.bll.contratacion.cobrar_estado");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var ctx = new Contexto();
            var contratacion = ContratacionPendiente();

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo), "err.bll.sesion_expirada");
        }

        [TestMethod]
        public void ConfirmarCobro_PlanDadoDeBajaAntesDelCobro_LanzaPlanBaja_SinTocarNada()
        {
            // El plan se dio de baja entre el registro y el cobro: la contratación sigue Pendiente de pago.
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Estado = false };
            var contratacion = ctx.Pendiente();

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo), "err.bll.contratacion.plan_baja");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
            Assert.AreEqual(BE.EstadoContratacion.PendientePago, contratacion.Estado);
        }

        [TestMethod]
        public void ConfirmarCobro_OtraSesionGanoElClaim_LanzaCobrarConcurrente_SinActivarLaSuscripcion()
        {
            // Dos sesiones de Caja superan la revalidación; el UPDATE condicional deja pasar a una sola.
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.ConfirmarCobroResultado = false;
            var contratacion = ctx.Pendiente();

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo), "err.bll.contratacion.cobrar_concurrente");
            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces, "No debe activarse dos veces la suscripción.");
            Assert.AreEqual(0, ctx.DalContratacion.ReabrirPagoVeces);
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarVigenciaVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_FallaLaActivacion_CompensaReabriendoElPagoYRelanza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.ClienteBLL.ActivarSuscripcionLanza = new InvalidOperationException("boom");
            var contratacion = ctx.Pendiente();

            try
            {
                ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo);
                Assert.Fail("Debía propagar el fallo de la activación.");
            }
            catch (InvalidOperationException)
            {
            }
            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(1, ctx.DalContratacion.ReabrirPagoVeces, "Nunca debe quedar Pagada sin suscripción activa.");
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarVigenciaVeces);
        }

        // ══ Caja: ¿Se concreta el pago? No → Registrar intento → ¿Máximo de 3? ══

        [TestMethod]
        public void RegistrarIntentoFallido_PrimerYSegundoIntento_NoCancelan_SigueEnLaCola()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            var bll = ctx.Crear();

            var r1 = bll.RegistrarIntentoFallido("Test", contratacion, IdTarjeta, "  Tarjeta rechazada  ");
            var r2 = bll.RegistrarIntentoFallido("Test", contratacion, null, "No trajo efectivo");

            Assert.AreEqual(1, r1.NroIntento);
            Assert.IsFalse(r1.Cancelada);
            Assert.AreEqual(2, r2.NroIntento);
            Assert.IsFalse(r2.Cancelada);
            Assert.AreEqual(BE.Contratacion.MaxIntentosPago, r2.Maximo);
            Assert.AreEqual(BE.EstadoContratacion.PendientePago, contratacion.Estado);
            Assert.AreEqual(2, ctx.DalContratacion.RegistrarIntentoVeces);
            Assert.AreEqual("No trajo efectivo", ctx.DalContratacion.UltimoMotivoIntento);
            Assert.IsNull(ctx.DalContratacion.UltimoIdMedioPago, "El medio del intento es opcional.");
            Assert.AreEqual("Tarjeta rechazada", ctx.DalContratacion.ObtenerIntentos(7)[0].Motivo, "El motivo se guarda sin espacios.");
            Assert.AreEqual(IdTarjeta, ctx.DalContratacion.ObtenerIntentos(7)[0].IdMedioPago);
            Assert.AreEqual(5, ctx.DalContratacion.UltimoIdCaja);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_TercerIntento_CancelaLaContratacion()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            var bll = ctx.Crear();

            bll.RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "No abonó");
            bll.RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "No abonó");
            var r3 = bll.RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "No abonó");

            Assert.AreEqual(3, r3.NroIntento);
            Assert.IsTrue(r3.Cancelada);
            Assert.AreEqual(BE.Contratacion.MaxIntentosPago, ctx.DalContratacion.UltimoMaximo, "El BLL pasa el máximo de 3 al DAL.");
            Assert.AreEqual(BE.EstadoContratacion.Cancelada, contratacion.Estado);
            Assert.AreEqual(3, ctx.DalContratacion.ObtenerIntentos(7).Count);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_SinMotivo_LanzaIntentoSinMotivo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();

            EsperarError(() => ctx.Crear().RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "  "),
                "err.bll.contratacion.intento_sin_motivo");
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarIntentoVeces);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_MedioDePagoInexistente_LanzaMedioInvalido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();

            EsperarError(() => ctx.Crear().RegistrarIntentoFallido("Test", contratacion, 99, "Rechazada"),
                "err.bll.contratacion.medio_invalido");
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarIntentoVeces);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_ContratacionInexistente_LanzaInexistente_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ContratacionPendiente();

            EsperarError(() => ctx.Crear().RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "Rechazada"),
                "err.bll.contratacion.inexistente");
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarIntentoVeces);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_ContratacionNoPendientePago_LanzaCobrarEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            contratacion.Estado = BE.EstadoContratacion.Cancelada;

            EsperarError(() => ctx.Crear().RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "Rechazada"),
                "err.bll.contratacion.cobrar_estado");
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarIntentoVeces);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_OtraSesionYaLaResolvio_LanzaCobrarConcurrente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.RegistrarIntentoDevuelveNull = true;
            var contratacion = ctx.Pendiente();

            EsperarError(() => ctx.Crear().RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "Rechazada"),
                "err.bll.contratacion.cobrar_concurrente");
            Assert.AreEqual(1, ctx.DalContratacion.RegistrarIntentoVeces);
        }

        [TestMethod]
        public void RegistrarIntentoFallido_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var ctx = new Contexto();
            var contratacion = ContratacionPendiente();

            EsperarError(() => ctx.Crear().RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "Rechazada"),
                "err.bll.sesion_expirada");
        }

        // ══ Máquina de estados de BE.Contratacion ══════════════════════════════

        private static BE.Contratacion En(BE.EstadoContratacion estado) => new BE.Contratacion { Estado = estado };

        [TestMethod]
        public void TransicionValida_PendientePago_SoloAPagadaOCancelada()
        {
            var c = En(BE.EstadoContratacion.PendientePago);
            Assert.IsTrue(c.TransicionValida(BE.EstadoContratacion.Pagada));
            Assert.IsTrue(c.TransicionValida(BE.EstadoContratacion.Cancelada));
            Assert.IsFalse(c.TransicionValida(BE.EstadoContratacion.PendientePago));
        }

        [TestMethod]
        public void TransicionValida_Pagada_SoloVuelveAPendientePorCompensacion()
        {
            var c = En(BE.EstadoContratacion.Pagada);
            Assert.IsTrue(c.TransicionValida(BE.EstadoContratacion.PendientePago));
            Assert.IsFalse(c.TransicionValida(BE.EstadoContratacion.Cancelada));
            Assert.IsFalse(c.TransicionValida(BE.EstadoContratacion.Pagada));
        }

        [TestMethod]
        public void TransicionValida_Cancelada_EsFinal()
        {
            var c = En(BE.EstadoContratacion.Cancelada);
            Assert.IsFalse(c.TransicionValida(BE.EstadoContratacion.PendientePago));
            Assert.IsFalse(c.TransicionValida(BE.EstadoContratacion.Pagada));
            Assert.IsFalse(c.TransicionValida(BE.EstadoContratacion.Cancelada));
        }

        // ══ Separación de funciones: Vendedor registra, Caja cobra ═════════════

        [TestMethod]
        public void RegistrarContratacion_UsuarioDeCaja_LanzaSinPermiso()
        {
            LoginComoCaja();
            var ctx = new Contexto();

            EsperarError(() => ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.sin_permiso");
            Assert.AreEqual(0, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void ConfirmarCobroYRegistrarIntento_UsuarioVendedor_LanzanSinPermiso()
        {
            LoginComoVendedor();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo), "err.bll.sin_permiso");
            EsperarError(() => ctx.Crear().RegistrarIntentoFallido("Test", contratacion, IdEfectivo, "Rechazada"),
                "err.bll.sin_permiso");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarIntentoVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void RegistrarContratacion_UsuarioVendedor_Puede()
        {
            LoginComoVendedor();
            var ctx = new Contexto();

            ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(1, ctx.DalContratacion.AltaVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_UsuarioDeCaja_Puede()
        {
            LoginComoCaja();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();

            ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo);

            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarCobroVeces);
        }
        // ══ Precio pactado en la «Orden de cobro» ═══════════════════════════════

        [TestMethod]
        public void RegistrarContratacion_GuardaElPrecioMensualPactado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.Crear().RegistrarContratacion("Test", 10, 1, BE.Builders.ModalidadCobro.Mensual);
            Assert.AreEqual(1000m, ctx.DalContratacion.UltimoAlta.PrecioMensual);
        }

        [TestMethod]
        public void ConfirmarCobro_ElPlanCambioDePrecio_CobraElPrecioPactado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            c.PrecioMensual = 1000m;
            ctx.DalPlan.PlanPorId.Precio = 1500m;   // subió mientras esperaba en la cola

            var liq = ctx.Crear().ConfirmarCobro("Test", c, 1);

            Assert.AreEqual(1000m, liq.Bruto);
            Assert.AreEqual(1000m, ctx.DalContratacion.UltimoImporte);
        }

        [TestMethod]
        public void CalcularImporte_SinPrecioPactado_UsaElPrecioDelPlan()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            c.PrecioMensual = null;
            Assert.AreEqual(1000m, ctx.Crear().CalcularImporte(c).Bruto);
        }

        // Caja confirma el importe que vio en la «Liquidación»: si cambió, no se cobra otro monto.
        [TestMethod]
        public void ConfirmarCobro_ImporteDistintoDelConfirmado_LanzaImporteCambiado_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", c, 1, 900m), "err.bll.contratacion.importe_cambiado");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_ImporteIgualAlConfirmado_Cobra()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            ctx.Crear().ConfirmarCobro("Test", c, 1, 1000m);
            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        // Separación de funciones por persona: el empleado que vendió no puede cobrar su propia
        // contratación (un usuario de Caja que también hubiera vendido).
        [TestMethod]
        public void ConfirmarCobro_ElMismoEmpleadoQueVendio_LanzaCobraElVendedor_SinCobrar()
        {
            LoginComoCaja();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            c.IdVendedor = 5;   // el empleado vinculado al usuario en sesión (Caja) es el 5
            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", c, 1), "err.bll.contratacion.cobra_el_vendedor");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        // Excepción: el Administrador puede registrar y cobrar la misma contratación.
        [TestMethod]
        public void ConfirmarCobro_AdministradorQueVendio_PuedeCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            c.IdVendedor = 5;   // el mismo empleado que cobra
            ctx.Crear().ConfirmarCobro("Test", c, 1);
            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        [TestMethod]
        public void AsentarDesistimiento_MotivoMasLargoQueLaColumna_SeRechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            EsperarError(() => ctx.Crear().AsentarDesistimiento("Test", 10, null, null,
                new string('x', BLL.Contratacion.LargoMaximoMotivo + 1)), "err.bll.contratacion.motivo_largo");
        }

        // El cliente sumó prendas en uso después de registrar: al cobrar se revalida el cupo.
        [TestMethod]
        public void ConfirmarCobro_CupoInsuficienteAlCobrar_LanzaPlanInsuficiente_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            ctx.DalCliente.ClientePorId.StockUtilizado = 5;   // el plan permite 3
            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", c, 1), "err.bll.cliente.plan_insuficiente");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        // Con 3 intentos ya registrados (el contador quedó en el tope), se cancela sin agregar un cuarto.
        [TestMethod]
        public void RegistrarIntentoFallido_YaEnElTope_CancelaSinAgregarOtroIntento()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            ctx.DalContratacion.IntentosPrevios[c.IdContratacion] = 3;

            var r = ctx.Crear().RegistrarIntentoFallido("Test", c, null, "x");

            Assert.AreEqual(3, r.NroIntento);
            Assert.IsTrue(r.Cancelada);
            Assert.AreEqual(0, ctx.DalContratacion.Intentos.Count);
        }

        // ══ Reglas que conectan PN02 con N01 y el ABM de clientes ═══════════════

        [TestMethod]
        public void Cliente_Baja_ConContratacionPendiente_Rechaza()
        {
            LoginComoAdministrador();
            var dal = new FakeClienteDAL { TieneContratacionPendienteRespuesta = true };
            var cliente = ClienteExistente();
            EsperarError(() => new BLL.Cliente(dal, new FakePlanSuscripcionDAL()).Baja("Test", cliente), "err.bll.cliente.baja_contratacion");
        }

        [TestMethod]
        public void Cobro_ClienteConContratacionPendiente_Rechaza()
        {
            LoginComoAdministrador();
            var dal = new FakeClienteDAL { TieneContratacionPendienteRespuesta = true };
            var cliente = ClienteExistente();
            cliente.IdPlan = 1;
            EsperarError(() => new BLL.Cobro(dal, new FakeCobroDAL(), new FakeCargoPrendaDAL())
                    .Procesar("Test", cliente, BLL.Manejadores.DecisionCobro.Cobrado, BE.Builders.ModalidadCobro.Mensual, "caja"),
                "err.bll.cobro.contratacion_pendiente");
        }

        // N1 (re-auditoría): los listados de Cobro y Renovación consultan las contrataciones
        // pendientes UNA sola vez, no una vez por cliente, y excluyen a esos clientes.
        [TestMethod]
        public void ObtenerElegibles_ConsultaPendientesUnaSolaVez()
        {
            LoginComoAdministrador();
            var dal = new FakeClienteDAL();
            for (int i = 1; i <= 20; i++)
                dal.ClientesDevueltos.Add(new BE.Cliente { IdCliente = i, Nombre = "C" + i, Apellido = "X", IdPlan = 1,
                                                           FechaVencimiento = DateTime.Today.AddDays(-1) });
            dal.IdsConContratacionPendiente.Add(3);

            var cobro = new BLL.Cobro(dal, new FakeCobroDAL(), new FakeCargoPrendaDAL()).ObtenerElegibles();
            Assert.AreEqual(1, dal.ConsultasIdsConContratacionPendiente);
            Assert.AreEqual(19, cobro.Count);
            Assert.IsFalse(cobro.Any(c => c.IdCliente == 3));

            var renov = new BLL.Renovacion(dal, new FakeRenovacionDAL(), new FakePlanSuscripcionDAL(), new FakePrendaDAL())
                .ObtenerElegibles(BLL.Manejadores.DecisionRenovacion.Renovar);
            Assert.AreEqual(2, dal.ConsultasIdsConContratacionPendiente);
            Assert.IsFalse(renov.Any(c => c.IdCliente == 3));
        }

        [TestMethod]
        public void Renovacion_ClienteConContratacionPendiente_Rechaza()
        {
            LoginComoAdministrador();
            var dal = new FakeClienteDAL { TieneContratacionPendienteRespuesta = true };
            var cliente = ClienteExistente();
            cliente.IdPlan = 1;
            EsperarError(() => new BLL.Renovacion(dal, new FakeRenovacionDAL(), new FakePlanSuscripcionDAL(), new FakePrendaDAL())
                    .Procesar("Test", cliente, BLL.Manejadores.DecisionRenovacion.Renovar, null, BE.Builders.ModalidadCobro.Mensual, "vendedor"),
                "err.bll.renovacion.contratacion_pendiente");
        }

        // Activar una suscripción sin Contratación + Caja es una corrección del Administrador.
        [TestMethod]
        public void Cliente_ActivarSuscripcion_NoAdministrador_Rechaza()
        {
            LoginComoVendedor();
            var cliente = ClienteExistente();
            EsperarError(() => new BLL.Cliente(new FakeClienteDAL(), new FakePlanSuscripcionDAL { PlanPorId = PlanActivo() })
                    .ActivarSuscripcion("Test", cliente, 1, BE.Builders.ModalidadCobro.Mensual),
                "err.bll.cliente.plan_solo_admin");
        }

        [TestMethod]
        public void Cliente_PuedeCorregirPlanDirectamente_SoloAdministrador()
        {
            LoginComoVendedor();
            Assert.IsFalse(new BLL.Cliente(new FakeClienteDAL()).PuedeCorregirPlanDirectamente());
            SessionManager.Logout();
            LoginComoAdministrador();
            Assert.IsTrue(new BLL.Cliente(new FakeClienteDAL()).PuedeCorregirPlanDirectamente());
        }
        // ══ Caja: ¿Tarjeta de crédito? → planes de cuotas → recargo y valor de cuota ══

        [TestMethod]
        public void PoliticaCuotas_Financiar_CalculaRecargoTotalYValorDeCuota()
        {
            var f = BLL.Politicas.PoliticaCuotas.Financiar(1000m, new BE.PlanCuotas { IdPlanCuotas = 4, CantidadCuotas = 12, RecargoPorcentaje = 20m });
            Assert.AreEqual(12, f.CantidadCuotas);
            Assert.AreEqual(200m, f.Recargo);
            Assert.AreEqual(1200m, f.TotalFinanciado);
            Assert.AreEqual(100m, f.ValorCuota);

            var unPago = BLL.Politicas.PoliticaCuotas.Financiar(1000m, null);
            Assert.AreEqual(1, unPago.CantidadCuotas);
            Assert.AreEqual(0m, unPago.Recargo);
            Assert.AreEqual(1000m, unPago.ValorCuota);

            // Redondeo a 2 decimales: 3 cuotas con 5 % sobre 999,99
            var r = BLL.Politicas.PoliticaCuotas.Financiar(999.99m, new BE.PlanCuotas { CantidadCuotas = 3, RecargoPorcentaje = 5m });
            Assert.AreEqual(50.00m, r.Recargo);
            Assert.AreEqual(350.00m, r.ValorCuota);
        }

        [TestMethod]
        public void ObtenerPlanesCuotas_NoOfreceMasCuotasQueMesesDeLaModalidad()
        {
            var bll = new Contexto().Crear();
            CollectionAssert.AreEqual(new[] { 1 },
                bll.ObtenerPlanesCuotas(BE.Builders.ModalidadCobro.Mensual).Select(p => p.CantidadCuotas).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3 },
                bll.ObtenerPlanesCuotas(BE.Builders.ModalidadCobro.Trimestral).Select(p => p.CantidadCuotas).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 6, 12 },
                bll.ObtenerPlanesCuotas(BE.Builders.ModalidadCobro.Anual).Select(p => p.CantidadCuotas).ToArray());
        }

        [TestMethod]
        public void ObtenerPlanesCuotas_PlanInactivo_NoSeOfrece()
        {
            var ctx = new Contexto();
            ctx.DalContratacion.PlanesCuotas.Find(p => p.CantidadCuotas == 6).Activo = false;
            CollectionAssert.AreEqual(new[] { 1, 3, 12 },
                ctx.Crear().ObtenerPlanesCuotas(BE.Builders.ModalidadCobro.Anual).Select(p => p.CantidadCuotas).ToArray());
        }

        [TestMethod]
        public void ConfirmarCobro_TarjetaCreditoEn3CuotasTrimestral_RegistraPlanYRecargoDel5PorCiento()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Trimestral;

            var liq = ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjetaCredito, null, Plan3Cuotas);

            Assert.IsTrue(liq.Total > 0);
            Assert.AreEqual(1, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(IdTarjetaCredito, ctx.DalContratacion.UltimoIdMedioPago);
            Assert.AreEqual(Plan3Cuotas, ctx.DalContratacion.UltimoIdPlanCuotas);
            decimal recargo = Math.Round(liq.Total * 0.05m, 2, MidpointRounding.AwayFromZero);
            Assert.AreEqual(recargo, ctx.DalContratacion.UltimoRecargoCuotas);
            // El importe del cobro (sin recargo) es el mismo: el recargo se guarda aparte.
            Assert.AreEqual(liq.Total, ctx.DalContratacion.UltimoImporte);
            Assert.AreEqual(3, liq.CantidadCuotas);
            Assert.AreEqual(recargo, liq.RecargoCuotas);
            Assert.AreEqual(liq.Total + recargo, liq.TotalConRecargo);
            Assert.AreEqual(Math.Round((liq.Total + recargo) / 3, 2, MidpointRounding.AwayFromZero), liq.ValorCuota);
            Assert.AreEqual(1, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_TarjetaCreditoSinElegirCuotas_UnSoloPagoSinRecargo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();

            var liq = ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjetaCredito);

            Assert.AreEqual(Plan1Cuota, ctx.DalContratacion.UltimoIdPlanCuotas);
            Assert.AreEqual(0m, ctx.DalContratacion.UltimoRecargoCuotas);
            Assert.AreEqual(1, liq.CantidadCuotas);
            Assert.AreEqual(liq.Total, liq.TotalConRecargo);
        }

        [TestMethod]
        public void ConfirmarCobro_MedioQueNoFinancia_NoRegistraPlanNiRecargo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();

            var liq = ctx.Crear().ConfirmarCobro("Test", contratacion, IdEfectivo);

            Assert.IsNull(ctx.DalContratacion.UltimoIdPlanCuotas);
            Assert.IsNull(ctx.DalContratacion.UltimoRecargoCuotas);
            Assert.AreEqual(1, liq.CantidadCuotas);
            Assert.AreEqual(0m, liq.RecargoCuotas);
        }

        [TestMethod]
        public void ConfirmarCobro_CuotasConTarjetaDeDebito_LanzaCuotasMedio_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Anual;

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjeta, null, Plan6Cuotas),
                "err.bll.contratacion.cuotas_medio");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
            Assert.AreEqual(0, ctx.ClienteBLL.ActivarSuscripcionVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_MasCuotasQueMesesDeLaModalidad_LanzaCuotasModalidad_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();   // Mensual: 1 cuota como máximo

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjetaCredito, null, Plan3Cuotas),
                "err.bll.contratacion.cuotas_modalidad");
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Trimestral;   // hasta 3
            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjetaCredito, null, Plan12Cuotas),
                "err.bll.contratacion.cuotas_modalidad");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        [TestMethod]
        public void ConfirmarCobro_PlanDeCuotasInexistenteOInactivo_LanzaCuotasInvalidas_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var contratacion = ctx.Pendiente();
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Anual;

            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjetaCredito, null, 99),
                "err.bll.contratacion.cuotas_invalidas");
            ctx.DalContratacion.PlanesCuotas.Find(p => p.IdPlanCuotas == Plan6Cuotas).Activo = false;
            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", contratacion, IdTarjetaCredito, null, Plan6Cuotas),
                "err.bll.contratacion.cuotas_invalidas");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        [TestMethod]
        public void CalcularImporte_TarjetaCreditoEn12CuotasAnual_DevuelveDetalleDeFinanciacion()
        {
            var ctx = new Contexto();
            var contratacion = ContratacionPendiente();
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Anual;

            var liq = ctx.Crear().CalcularImporte(contratacion, IdTarjetaCredito, Plan12Cuotas);

            Assert.AreEqual(12, liq.CantidadCuotas);
            Assert.AreEqual(20m, liq.RecargoPorcentaje);
            Assert.AreEqual(Math.Round(liq.Total * 0.20m, 2, MidpointRounding.AwayFromZero), liq.RecargoCuotas);
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces, "Calcular no cobra.");
        }
        [TestMethod]
        public void MedioDePagoHistorico_NoSeOfreceNiSeAceptaEnUnCobroNuevo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalContratacion.MediosPago.Add(new BE.MedioPago { IdMedioPago = 9, Nombre = "Tarjeta (anterior a débito/crédito)", ClaveTraduccion = "medio.tarjeta_historica", Activo = false });
            var contratacion = ctx.Pendiente();
            var bll = ctx.Crear();

            Assert.IsFalse(bll.ObtenerMediosPago().Any(m => m.IdMedioPago == 9), "Un medio histórico no se ofrece en Caja.");
            EsperarError(() => bll.ConfirmarCobro("Test", contratacion, 9), "err.bll.contratacion.medio_invalido");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
        }

        // ══ Upgrade: el plan nuevo rige desde hoy y se cobra la diferencia ═════

        private static BE.PlanSuscripcion PlanBasico()  => new BE.PlanSuscripcion { IdPlan = 2, Nombre = "Básico",  LimitePrendas = 3, Precio = 1000m, Estado = true };
        private static BE.PlanSuscripcion PlanPremium() => new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Premium", LimitePrendas = 6, Precio = 3000m, Estado = true };

        // Cliente con el Básico vigente por 15 días más que contrata el Premium (IdPlan 1).
        private static Contexto ContextoUpgrade(out BE.Contratacion c)
        {
            var ctx = new Contexto();
            ctx.DalPlan.PlanPorId = PlanPremium();
            ctx.DalPlan.Planes = new List<BE.PlanSuscripcion> { PlanPremium(), PlanBasico() };
            ctx.DalCliente.ClientePorId.IdPlan = 2;
            ctx.DalCliente.ClientePorId.FechaVencimiento = DateTime.Today.AddDays(15);
            c = ctx.Pendiente();
            c.PrecioMensual = 3000m;
            return ctx;
        }

        [TestMethod]
        public void PoliticaCambioPlan_PlanMasCaroConPeriodoVigente_CreditoPorDiasNoUsados()
        {
            var cliente = new BE.Cliente { IdPlan = 2, FechaVencimiento = DateTime.Today.AddDays(15) };
            // 1000 por mes × 15 días / 30 = 500
            Assert.AreEqual(500m, BLL.Politicas.PoliticaCambioPlan.Credito(cliente, PlanBasico(), 1, 3000m, DateTime.Today, 3000m));
        }

        [TestMethod]
        public void PoliticaCambioPlan_PlanIgualOMasBarato_NoEsUpgrade()
        {
            var cliente = new BE.Cliente { IdPlan = 1, FechaVencimiento = DateTime.Today.AddDays(15) };
            Assert.IsFalse(BLL.Politicas.PoliticaCambioPlan.EsUpgrade(cliente, PlanPremium(), 2, 1000m, DateTime.Today), "Más barato.");
            Assert.IsFalse(BLL.Politicas.PoliticaCambioPlan.EsUpgrade(cliente, PlanPremium(), 1, 3000m, DateTime.Today), "Mismo plan (renovación).");
        }

        [TestMethod]
        public void PoliticaCambioPlan_PlanVencido_NoHayCredito()
        {
            var cliente = new BE.Cliente { IdPlan = 2, FechaVencimiento = DateTime.Today.AddDays(-3) };
            Assert.AreEqual(0m, BLL.Politicas.PoliticaCambioPlan.Credito(cliente, PlanBasico(), 1, 3000m, DateTime.Today, 3000m));
        }

        [TestMethod]
        public void PoliticaCambioPlan_SuscripcionPausada_NoAcreditaLosDiasDePausa()
        {
            // Vence en 25 días, pero 10 de esos son pausa que todavía no pasó: pagados quedan 15.
            var cliente = new BE.Cliente
            {
                IdPlan = 2, FechaVencimiento = DateTime.Today.AddDays(25), FechaPausaHasta = DateTime.Today.AddDays(10)
            };
            Assert.AreEqual(15, BLL.Politicas.PoliticaCambioPlan.DiasRestantes(cliente, DateTime.Today));
            Assert.AreEqual(500m, BLL.Politicas.PoliticaCambioPlan.Credito(cliente, PlanBasico(), 1, 3000m, DateTime.Today, 3000m));
        }

        [TestMethod]
        public void PoliticaCambioPlan_CreditoMayorQueLoQueQuedaPorCobrar_SeLimita()
        {
            var cliente = new BE.Cliente { IdPlan = 2, FechaVencimiento = DateTime.Today.AddDays(300) };
            Assert.AreEqual(3000m, BLL.Politicas.PoliticaCambioPlan.Credito(cliente, PlanBasico(), 1, 3000m, DateTime.Today, 3000m));
        }

        [TestMethod]
        public void ConfirmarCobro_Upgrade_CobraLaDiferenciaYElPlanNuevoRigeDesdeHoy()
        {
            LoginComoAdministrador();
            var ctx = ContextoUpgrade(out var c);

            var liq = ctx.Crear().ConfirmarCobro("Test", c, IdEfectivo);

            Assert.AreEqual(3000m, liq.Bruto);
            Assert.AreEqual(500m, liq.CreditoCambioPlan);
            Assert.AreEqual(2500m, liq.Total);
            Assert.AreEqual(2500m, ctx.DalContratacion.UltimoImporte);
            Assert.AreEqual(500m, ctx.DalContratacion.UltimoCreditoCambioPlan);
            Assert.IsTrue(ctx.ClienteBLL.UltimoIniciarHoy, "El período del plan nuevo arranca hoy.");
        }

        [TestMethod]
        public void CalcularImporte_Upgrade_LaLiquidacionYaMuestraElCredito()
        {
            LoginComoAdministrador();
            var ctx = ContextoUpgrade(out var c);
            var liq = ctx.Crear().CalcularImporte(c);
            Assert.AreEqual(500m, liq.CreditoCambioPlan);
            Assert.AreEqual(2500m, liq.Total);
        }

        [TestMethod]
        public void ConfirmarCobro_RenovacionDelMismoPlan_SinCreditoYAContinuacion()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId.IdPlan = 1;
            ctx.DalCliente.ClientePorId.FechaVencimiento = DateTime.Today.AddDays(15);
            var c = ctx.Pendiente();

            var liq = ctx.Crear().ConfirmarCobro("Test", c, IdEfectivo);

            Assert.AreEqual(0m, liq.CreditoCambioPlan);
            Assert.IsNull(ctx.DalContratacion.UltimoCreditoCambioPlan);
            Assert.IsFalse(ctx.ClienteBLL.UltimoIniciarHoy);
        }

        // ══ Cargos de PN04 en el cobro de PN02 ════════════════════════════════
        // Antes solo el cobro de N01 los sumaba: renovar o cambiar de plan por PN02 los dejaba sin cobrar.

        [TestMethod]
        public void CalcularImporte_ConCargosPendientes_LosSumaAlTotal()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCargos.Alta(new BE.CargoPrenda { IdCliente = 10, IdPrenda = 1, Motivo = "Rotura", Monto = 400m });
            ctx.DalCargos.Alta(new BE.CargoPrenda { IdCliente = 99, IdPrenda = 2, Motivo = "Otro cliente", Monto = 999m });

            var liq = ctx.Crear().CalcularImporte(ctx.Pendiente());

            Assert.AreEqual(1, liq.CantidadCargos);
            Assert.AreEqual(400m, liq.Cargos);
            Assert.AreEqual(1000m + 400m, liq.Total);
        }

        [TestMethod]
        public void ConfirmarCobro_ConCargosPendientes_CobraYLiquidaLosCargos()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            int idCargo = ctx.DalCargos.Alta(new BE.CargoPrenda { IdCliente = 10, IdPrenda = 1, Motivo = "Rotura", Monto = 400m });

            var liq = ctx.Crear().ConfirmarCobro("Test", ctx.Pendiente(), IdEfectivo, importeConfirmado: 1400m);

            Assert.AreEqual(1400m, liq.Total);
            Assert.AreEqual(1400m, ctx.DalContratacion.UltimoImporte);
            CollectionAssert.AreEqual(new[] { idCargo }, ctx.DalContratacion.UltimosCargos.ToArray());
        }

        [TestMethod]
        public void ConfirmarCobro_FallaLaActivacion_LosCargosVuelvenAPendientes()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            int idCargo = ctx.DalCargos.Alta(new BE.CargoPrenda { IdCliente = 10, IdPrenda = 1, Motivo = "Rotura", Monto = 400m });
            ctx.ClienteBLL.ActivarSuscripcionLanza = new InvalidOperationException("falla");

            try { ctx.Crear().ConfirmarCobro("Test", ctx.Pendiente(), IdEfectivo); Assert.Fail("Debía propagar la falla."); }
            catch (InvalidOperationException) { }

            CollectionAssert.AreEqual(new[] { idCargo }, ctx.DalContratacion.CargosReabiertos.ToArray());
        }

        // Downgrade o cambio lateral con el período vigente: rige al vencer, así que no se registra
        // (antes el plan y su límite cambiaban el día del cobro, contra el nodo a11 del diagrama).
        [TestMethod]
        public void ValidarContratacion_PlanMasBaratoConPeriodoVigente_LoRechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.Planes = new List<BE.PlanSuscripcion> { PlanPremium(), PlanBasico() };
            ctx.DalCliente.ClientePorId.IdPlan = 1;   // Premium vigente 15 días más
            ctx.DalCliente.ClientePorId.FechaVencimiento = DateTime.Today.AddDays(15);
            EsperarError(() => ctx.Crear().ValidarContratacion(10, 2), "err.bll.contratacion.cambio_plan_vigente");
        }

        [TestMethod]
        public void ValidarContratacion_PlanMasBaratoConPeriodoVencido_LoPermite()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPlan.Planes = new List<BE.PlanSuscripcion> { PlanPremium(), PlanBasico() };
            ctx.DalCliente.ClientePorId.IdPlan = 1;
            ctx.DalCliente.ClientePorId.FechaVencimiento = DateTime.Today.AddDays(-2);
            Assert.AreEqual(2, ctx.Crear().ValidarContratacion(10, 2).IdPlan);
        }

        [TestMethod]
        public void ConfirmarCobro_UpgradeConTotalCero_IgualArrancaHoy()
        {
            LoginComoAdministrador();
            var ctx = ContextoUpgrade(out var c);
            ctx.DalCliente.ClientePorId.DescuentoProximoCobro = 100000m;   // crédito por referidos que cubre todo

            var liq = ctx.Crear().ConfirmarCobro("Test", c, IdEfectivo);

            Assert.AreEqual(0m, liq.Total);
            Assert.IsTrue(ctx.ClienteBLL.UltimoIniciarHoy, "Es upgrade aunque no haya crédito por cambio de plan.");
        }

        [TestMethod]
        public void Builder_IniciarHoy_ElPeriodoArrancaHoyAunqueQuedeTiempoPagado()
        {
            var cliente = new BE.Cliente { IdPlan = 2, FechaVencimiento = DateTime.Today.AddDays(15) };

            var upgrade = BE.Builders.DirectorSuscripcion.Construir(
                BE.Builders.SuscripcionBuilderFactory.Crear(BE.Builders.ModalidadCobro.Mensual), cliente, PlanPremium(), iniciarHoy: true);
            var renovacion = BE.Builders.DirectorSuscripcion.Construir(
                BE.Builders.SuscripcionBuilderFactory.Crear(BE.Builders.ModalidadCobro.Mensual), cliente, PlanPremium());

            Assert.AreEqual(DateTime.Today, upgrade.InicioPeriodo);
            Assert.AreEqual(DateTime.Today.AddDays(15), renovacion.InicioPeriodo, "Sin upgrade sigue a continuación.");
        }

        // ══ Anular contratación (Caja, con motivo) ═══════════════════════════

        [TestMethod]
        public void Anular_Pendiente_LaCancelaConMotivo()
        {
            LoginComoCaja();
            var ctx = new Contexto();
            var c = ctx.Pendiente();

            ctx.Crear().Anular("Test", c, "  El cliente se arrepintió  ");

            Assert.AreEqual(1, ctx.DalContratacion.AnularVeces);
            Assert.AreEqual("El cliente se arrepintió", ctx.DalContratacion.UltimoMotivoAnulacion);
            Assert.AreEqual(5, ctx.DalContratacion.UltimoIdCaja);
            Assert.AreEqual(0, ctx.DalContratacion.RegistrarIntentoVeces, "No inventa intentos de pago.");
        }

        [TestMethod]
        public void Anular_SinMotivo_LanzaSinMotivo_SinTocarElDAL()
        {
            LoginComoCaja();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            EsperarError(() => ctx.Crear().Anular("Test", c, "   "), "err.bll.contratacion.anular_sin_motivo");
            Assert.AreEqual(0, ctx.DalContratacion.AnularVeces);
        }

        [TestMethod]
        public void Anular_ContratacionYaPagada_LanzaAnularEstado()
        {
            LoginComoCaja();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            c.Estado = BE.EstadoContratacion.Pagada;
            EsperarError(() => ctx.Crear().Anular("Test", c, "Error de carga"), "err.bll.contratacion.anular_estado");
            Assert.AreEqual(0, ctx.DalContratacion.AnularVeces);
        }

        [TestMethod]
        public void Anular_OtraSesionLaResolvio_LanzaCobrarConcurrente()
        {
            LoginComoCaja();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            ctx.DalContratacion.AnularResultado = false;
            EsperarError(() => ctx.Crear().Anular("Test", c, "Error de carga"), "err.bll.contratacion.cobrar_concurrente");
        }

        [TestMethod]
        public void Anular_Vendedor_NoTienePermiso()
        {
            LoginComoVendedor();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            EsperarError(() => ctx.Crear().Anular("Test", c, "Error de carga"), "err.bll.sin_permiso");
            Assert.AreEqual(0, ctx.DalContratacion.AnularVeces);
        }
    }
}
