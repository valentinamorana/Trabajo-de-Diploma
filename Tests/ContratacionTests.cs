using System;
using System.Collections.Generic;
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
        private const int IdTarjeta  = 2;

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

            public BLL.Contratacion Crear()
                => new BLL.Contratacion(DalContratacion, DalCliente, DalEmpleado, DalPlan, ClienteBLL);

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
        // contratación, aunque tenga ambos permisos (un Administrador que hace las dos cosas).
        [TestMethod]
        public void ConfirmarCobro_ElMismoEmpleadoQueVendio_LanzaCobraElVendedor_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var c = ctx.Pendiente();
            c.IdVendedor = 5;   // el empleado vinculado al usuario en sesión (Caja) es el 5
            EsperarError(() => ctx.Crear().ConfirmarCobro("Test", c, 1), "err.bll.contratacion.cobra_el_vendedor");
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarCobroVeces);
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
    }
}
