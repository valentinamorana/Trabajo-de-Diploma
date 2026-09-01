using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Pedido — la clase más crítica del sistema (cupo de suscripción, alta de pedido,
    /// transiciones de estado). Hasta esta sesión no tenía tests directos porque hardcodeaba
    /// sus 5 dependencias DAL sin inyección — se refactorizó para recibirlas por constructor
    /// (mismo patrón que BLL.Cliente/Renovacion/Cobro) específicamente para poder escribir
    /// estos tests.
    /// </summary>
    [TestClass]
    public class PedidoTests
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

        private static BE.Cliente ClienteConPlanVigente() => new BE.Cliente
        {
            IdCliente = 10,
            Nombre = "Ana",
            Apellido = "Gómez",
            IdPlan = 1,
            NombrePlan = "Básico",
            LimitePrendas = 3,
            StockUtilizado = 0,
            FechaVencimiento = DateTime.Today.AddDays(30)
        };

        private static BE.PlanSuscripcion PlanBasico() => new BE.PlanSuscripcion
        {
            IdPlan = 1,
            Nombre = "Básico",
            LimitePrendas = 3,
            Precio = 1000,
            Estado = true
        };

        private static BE.Prenda PrendaDisponible(int id = 1) => new BE.Prenda
        {
            IdPrenda = id,
            Nombre = "Remera",
            Estado = BE.EstadoPrenda.Disponible
        };

        private class Contexto
        {
            public FakePedidoDAL DalPedido = new FakePedidoDAL();
            public FakeClienteDAL DalCliente = new FakeClienteDAL();
            public FakeEmpleadoDAL DalEmpleado = new FakeEmpleadoDAL { EmpleadoPorUsuario = new BE.Empleado { IdEmpleado = 5 } };
            public FakePlanSuscripcionDAL DalPlan = new FakePlanSuscripcionDAL { PlanPorId = PlanBasico() };
            public FakePedidoHistorialDAL DalHistorial = new FakePedidoHistorialDAL();

            // PN01 (split lógico Depósito): BLL.Pedido ahora consume BLL.Prenda.VerificarDisponibilidad,
            // que relee el estado desde la base — sembrado por defecto con la misma prenda que
            // devuelve PrendaDisponible() para que los tests existentes seleccionen "sí, disponible"
            // sin tener que tocar cada uno.
            public FakePrendaDAL DalPrenda = new FakePrendaDAL { Todas = new List<BE.Prenda> { PrendaDisponible() } };
            public BLL.Prenda PrendaBLL => new BLL.Prenda(DalPrenda, new FakeMantenimientoPrendaDAL());

            public BLL.Pedido Crear() => new BLL.Pedido(DalPedido, DalCliente, DalEmpleado, DalPlan, DalHistorial, PrendaBLL);
        }

        // ── CrearPedido ───────────────────────────────────────────────────────

        [TestMethod]
        public void CrearPedido_DatosValidos_PersisteYRegistraHistorial()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalPedido.AltaIdGenerado = 99;
            var bll = ctx.Crear();

            int id = bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });

            Assert.AreEqual(99, id);
            Assert.AreEqual(1, ctx.DalPedido.AltaVeces);
            Assert.AreEqual(5, ctx.DalPedido.UltimoAlta.IdEmpleado);
            Assert.AreEqual(1, ctx.DalHistorial.RegistrarCambiosVeces);
        }

        [TestMethod]
        public void CrearPedido_SinPrendas_LanzaSinPrendas()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda>());
                Assert.Fail("Debía exigir al menos una prenda.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.sin_prendas", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPedido.AltaVeces);
        }

        // Lista de Espera (mejora opcional, no requerida por la cátedra — ver README):
        // una prenda Disponible pero reservada por Lista de Espera para OTRO cliente se
        // rechaza igual, aunque BE.Prenda.EstaDisponible() diga que sí se puede.
        [TestMethod]
        public void CrearPedido_PrendaReservadaPorListaDeEsperaParaOtroCliente_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente(); // IdCliente = 10

            var dalListaEspera = new FakeListaEsperaDAL();
            dalListaEspera.Registros.Add(new BE.ListaEspera
            {
                IdListaEspera = 1, IdPrenda = 1, IdCliente = 999, // otro cliente, no el 10
                Estado = BE.EstadoListaEspera.Reservada, FechaLimiteReserva = DateTime.Now.AddHours(10)
            });
            var listaEsperaBLL = new BLL.ListaEspera(dalListaEspera, new FakePrendaDAL(), new FakeClienteDAL());

            var bll = new BLL.Pedido(ctx.DalPedido, ctx.DalCliente, ctx.DalEmpleado, ctx.DalPlan, ctx.DalHistorial, listaEsperaBLL, ctx.PrendaBLL);

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía rechazar una prenda reservada para otro cliente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.prenda_reservada", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPedido.AltaVeces);
        }

        // La misma reserva, pero para el cliente que SÍ está pidiendo la prenda: se permite,
        // y al persistir el pedido la reserva se cierra (Convertida).
        [TestMethod]
        public void CrearPedido_PrendaReservadaParaElMismoCliente_PermiteYCierraLaReserva()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente(); // IdCliente = 10
            ctx.DalPedido.AltaIdGenerado = 99;

            var dalListaEspera = new FakeListaEsperaDAL();
            var reserva = new BE.ListaEspera
            {
                IdListaEspera = 1, IdPrenda = 1, IdCliente = 10, // el mismo cliente del pedido
                Estado = BE.EstadoListaEspera.Reservada, FechaLimiteReserva = DateTime.Now.AddHours(10)
            };
            dalListaEspera.Registros.Add(reserva);
            var listaEsperaBLL = new BLL.ListaEspera(dalListaEspera, new FakePrendaDAL(), new FakeClienteDAL());

            var bll = new BLL.Pedido(ctx.DalPedido, ctx.DalCliente, ctx.DalEmpleado, ctx.DalPlan, ctx.DalHistorial, listaEsperaBLL, ctx.PrendaBLL);

            int id = bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });

            Assert.AreEqual(99, id);
            Assert.AreEqual(BE.EstadoListaEspera.Convertida, reserva.Estado);
        }

        [TestMethod]
        public void CrearPedido_ClienteInexistente_LanzaClienteInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto(); // ClientePorId queda null
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía rechazar un cliente inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.cliente_inexistente", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_ClienteSinPlan_LanzaSinPlan()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.IdPlan = null;
            ctx.DalCliente.ClientePorId = cliente;
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía exigir un plan asignado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.sin_plan", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_SuspendidoPorPago_LanzaPagoSuspendido_AntesQueVencimiento()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.FechaVencimiento = DateTime.Today.AddDays(-10); // también vencida
            cliente.FechaLimiteGracia = DateTime.Today.AddDays(-1); // gracia ya vencida → suspendido
            ctx.DalCliente.ClientePorId = cliente;
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía bloquear por suspensión de pago.");
            }
            catch (BE.AppException ex)
            {
                // El chequeo de pago va ANTES que el de vencimiento genérico (a propósito,
                // ver comentario en BLL.Pedido.ObtenerClienteValidado) — verifica que siga así.
                Assert.AreEqual("err.bll.pedido.pago_suspendido", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_SuscripcionVencida_LanzaSuscripcionVencida()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.FechaVencimiento = DateTime.Today.AddDays(-5);
            ctx.DalCliente.ClientePorId = cliente;
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía rechazar una suscripción vencida.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.suscripcion_vencida", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_ConDespachoActivo_LanzaYaDespachado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalPedido.PedidosDevueltos.Add(new BE.Pedido { IdPedido = 7, IdCliente = 10, Estado = BE.EstadoPedido.Despachado });
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía bloquear un segundo pedido con uno despachado pendiente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.ya_despachado", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_SuperaLimiteDelPlan_LanzaLimitePlan()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            cliente.StockUtilizado = 1;
            ctx.DalCliente.ClientePorId = cliente;
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía rechazar superar el límite del plan.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.limite_plan", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_PrendaNoDisponible_LanzaPrendaNoDisponible()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            // PN01 (split lógico Depósito): la disponibilidad ahora se relee desde la base
            // (BLL.Prenda.VerificarDisponibilidad), no del objeto en memoria que se pasa acá —
            // por eso lo que importa es el estado sembrado en el Fake DAL, no en `prendaOcupada`.
            ctx.DalPrenda.Todas = new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.EnUso } };
            var bll = ctx.Crear();
            var prendaOcupada = PrendaDisponible();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { prendaOcupada });
                Assert.Fail("Debía rechazar una prenda no disponible.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.prenda_no_disponible", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_SinEmpleadoVinculado_LanzaEmpleadoSinVinculo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalEmpleado.EmpleadoPorUsuario = null; // usuario sin Empleado vinculado
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía exigir un Empleado vinculado al usuario.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.empleado_sin_vinculo", ex.Clave);
            }
        }

        [TestMethod]
        public void CrearPedido_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.CrearPedido("Test", 10, new List<BE.Prenda> { PrendaDisponible() });
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }

        // ── ValidarCupoDisponible (PN01) ─────────────────────────────────────

        [TestMethod]
        public void ValidarCupoDisponible_DentroDelLimite_DevuelvePlan()
        {
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var cliente = ClienteConPlanVigente(); // LimitePrendas=3, StockUtilizado=0

            var plan = bll.ValidarCupoDisponible(cliente, 2);

            Assert.AreEqual("Básico", plan.Nombre);
        }

        [TestMethod]
        public void ValidarCupoDisponible_ExcedeLimite_LanzaLimitePlan()
        {
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            cliente.StockUtilizado = 1;

            try
            {
                bll.ValidarCupoDisponible(cliente, 1);
                Assert.Fail("Debía rechazar superar el límite del plan.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.limite_plan", ex.Clave);
            }
        }

        // ── ReservarPrendas (PN01) ───────────────────────────────────────────

        [TestMethod]
        public void ReservarPrendas_DatosValidos_DelegaEnAltaDelDAL()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPedido.AltaIdGenerado = 42;
            var bll = ctx.Crear();

            int id = bll.ReservarPrendas(new List<BE.Prenda> { PrendaDisponible() }, 10);

            Assert.AreEqual(42, id);
            Assert.AreEqual(1, ctx.DalPedido.AltaVeces);
            Assert.AreEqual(10, ctx.DalPedido.UltimoAlta.IdCliente);
            Assert.AreEqual(5, ctx.DalPedido.UltimoAlta.IdEmpleado);
        }

        // ── Transiciones de estado ───────────────────────────────────────────

        [TestMethod]
        public void Despachar_PedidoPendiente_Despacha()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Pendiente };

            bll.Despachar("Test", pedido);

            Assert.AreEqual(1, ctx.DalPedido.DespacharVeces);
        }

        [TestMethod]
        public void Despachar_PedidoNoPendiente_LanzaDespacharEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Entregado };

            try
            {
                bll.Despachar("Test", pedido);
                Assert.Fail("Debía rechazar despachar un pedido no Pendiente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.despachar_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPedido.DespacharVeces);
        }

        [TestMethod]
        public void RegistrarDevolucion_PedidoNoEntregado_LanzaDevolucionEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Despachado };

            try
            {
                bll.RegistrarDevolucion("Test", pedido);
                Assert.Fail("Debía exigir que el pedido esté Entregado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.devolucion_estado", ex.Clave);
            }
        }

        [TestMethod]
        public void RegistrarDevolucion_SinPrendasParaDevolver_LanzaDevolucionYaHecha()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPedido.RegistrarDevolucionRespuesta = 0; // ninguna prenda EnUso ya
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Entregado };

            try
            {
                bll.RegistrarDevolucion("Test", pedido);
                Assert.Fail("Debía avisar que la devolución ya se había hecho.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.devolucion_ya_hecha", ex.Clave);
            }
        }

        [TestMethod]
        public void Cancelar_SinMotivo_LanzaCancelarSinMotivo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Pendiente };

            try
            {
                bll.Cancelar("Test", pedido, "   ");
                Assert.Fail("Debía exigir un motivo.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.cancelar_sin_motivo", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPedido.CancelarVeces);
        }

        [TestMethod]
        public void Cancelar_PedidoPendienteConMotivo_Cancela()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Pendiente };

            bll.Cancelar("Test", pedido, "  Cliente se arrepintió  ");

            Assert.AreEqual(1, ctx.DalPedido.CancelarVeces);
            Assert.AreEqual("Cliente se arrepintió", ctx.DalPedido.UltimoMotivoCancelar, "Debe recortar espacios.");
        }

        [TestMethod]
        public void DesCancelar_PrendasYaNoDisponibles_LanzaDescancelarPrendas()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPedido.DesCancelarRespuesta = false;
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Cancelado };

            try
            {
                bll.DesCancelar("Test", pedido);
                Assert.Fail("Debía rechazar si las prendas ya no están disponibles.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.descancelar_prendas", ex.Clave);
            }
        }

        // ── CalcularNivelUrgencia — lógica pura, sin DAL ─────────────────────

        [TestMethod]
        public void NivelUrgencia_Entregado_NoAplica()
        {
            var bll = new Contexto().Crear();
            var pedido = new BE.Pedido { Estado = BE.EstadoPedido.Entregado, FechaPedido = DateTime.Now.AddDays(-10) };

            Assert.AreEqual(BE.NivelUrgencia.NoAplica, bll.CalcularNivelUrgencia(pedido));
        }

        [TestMethod]
        public void NivelUrgencia_PendienteReciente_Reciente()
        {
            var bll = new Contexto().Crear();
            var pedido = new BE.Pedido { Estado = BE.EstadoPedido.Pendiente, FechaPedido = DateTime.Now };

            Assert.AreEqual(BE.NivelUrgencia.Reciente, bll.CalcularNivelUrgencia(pedido));
        }

        [TestMethod]
        public void NivelUrgencia_PendienteMasDeTresDias_Urgente()
        {
            var bll = new Contexto().Crear();
            var pedido = new BE.Pedido { Estado = BE.EstadoPedido.Pendiente, FechaPedido = DateTime.Now.AddDays(-4) };

            Assert.AreEqual(BE.NivelUrgencia.Urgente, bll.CalcularNivelUrgencia(pedido));
        }

        [TestMethod]
        public void NivelUrgencia_DespachadoMasDeCincoDias_Urgente()
        {
            var bll = new Contexto().Crear();
            var pedido = new BE.Pedido
            {
                Estado = BE.EstadoPedido.Despachado,
                FechaPedido = DateTime.Now.AddDays(-10),
                FechaDespacho = DateTime.Now.AddDays(-6)
            };

            Assert.AreEqual(BE.NivelUrgencia.Urgente, bll.CalcularNivelUrgencia(pedido));
        }

        // ── RestaurarOperacion ────────────────────────────────────────────────

        [TestMethod]
        public void RestaurarOperacion_SinCambiosRegistrados_LanzaHistorialVacio()
        {
            LoginComoAdministrador();
            var ctx = new Contexto(); // CambiosParaOperacion queda vacío
            var bll = ctx.Crear();

            try
            {
                bll.RestaurarOperacion("Test", 1, 99);
                Assert.Fail("Debía avisar que no hay cambios para esa operación.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.historial_vacio", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPedido.RestaurarOperacionAtomicaVeces);
        }
    }
}
