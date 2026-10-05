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

        [TestMethod]
        public void RestaurarOperacion_SinPermisoDeEdicion_LanzaSinPermisoYNoTocaElHistorial()
        {
            SessionManager.Login(new BE.Usuario
            {
                Id = 2,
                Username = "auditor",
                Perfil = "Auditor",
                Contraseña = Encriptador.Hash("Auditor1!")
            });
            var ctx = new Contexto();
            var bll = ctx.Crear();

            try
            {
                bll.RestaurarOperacion("Test", 1, 1);
                Assert.Fail("Debía exigir permiso de edición de pedidos.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sin_permiso", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPedido.RestaurarOperacionAtomicaVeces, "No debe llegar a modificar el pedido.");
        }

        [TestMethod]
        public void DespacharYEntregar_ExigenPermisoDePedidosRealizados_NoElDeVentas()
        {
            SessionManager.Login(new BE.Usuario
            {
                Id = 3,
                Username = "sinpermisos",
                Perfil = "Auditor",
                Contraseña = Encriptador.Hash("Auditor1!")
            });
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, IdCliente = 10, Estado = BE.EstadoPedido.Pendiente };

            try { bll.Despachar("Test", pedido); Assert.Fail("Despachar exige PedidosRealizadosEditar."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }

            pedido.Estado = BE.EstadoPedido.Despachado;
            try { bll.MarcarEntregado("Test", pedido); Assert.Fail("Entregar exige PedidosRealizadosEditar."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
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
            public BLL.Prenda PrendaBLL => new BLL.Prenda(DalPrenda, new FakeMantenimientoPrendaDAL(), DalListaEspera);

            // Lista de Espera (mejora opcional): si no se inyecta un doble acá, BLL.Pedido cae en
            // su lazy `new ListaEspera()` real (BLL.Pedido.cs), que abre DAL.ListaEspera/DAL.Prenda/
            // DAL.Cliente REALES contra la connection string de Tests/App.config. Si esa base
            // (.\SQLEXPRESS WardrobeFlowDB) existe y tiene datos — por ej. de correr la GUI a mano —
            // estos tests dejan de ser unitarios y su resultado depende del estado de esa base.
            // Se vio en vivo: una fila real de ListaEspera con IdPrenda=1 reservada para otro cliente
            // hizo fallar CrearPedido_DatosValidos_PersisteYRegistraHistorial y
            // CrearPedido_SinEmpleadoVinculado_LanzaEmpleadoSinVinculo con "err.bll.pedido.prenda_reservada"
            // en vez de sus resultados esperados. Inyectar el Fake (responde "no reservada" por
            // defecto) mantiene estos tests herméticos sin importar el estado de la base real.
            public FakeListaEsperaService DalListaEspera = new FakeListaEsperaService();

            public BLL.Pedido Crear() => new BLL.Pedido(DalPedido, DalCliente, DalEmpleado, DalPlan, DalHistorial, DalListaEspera, PrendaBLL);
        }

        // ══ PN01 — Armar pedido (diagrama de actividad): una prueba por rama ══════

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

        private static BE.Prenda Prenda(int id, string categoria = "Remeras", string talle = "M",
                                        BE.EstadoPrenda estado = BE.EstadoPrenda.Disponible) => new BE.Prenda
        {
            IdPrenda = id, Nombre = "Prenda " + id, Categoria = categoria, Talle = talle, Color = "Negro", Estado = estado
        };

        private static BE.Pedido PedidoEn(BE.EstadoPedido estado, params BE.Prenda[] prendas) => new BE.Pedido
        {
            IdPedido = 50, IdCliente = 10, IdEmpleado = 5, Estado = estado, NombreCliente = "Ana Gómez",
            FechaPedido = DateTime.Now, Prendas = new List<BE.Prenda>(prendas)
        };

        // ── Verificar la vigencia → ¿Suscripción vigente? (No: informar imposibilidad) ──

        [TestMethod]
        public void VerificarVigencia_ClienteInexistente_LanzaClienteInexistente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto(); // ClientePorId queda null
            try { ctx.Crear().VerificarVigencia(10); Assert.Fail("Debía rechazar un cliente inexistente."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.cliente_inexistente", ex.Clave); }
        }

        [TestMethod]
        public void VerificarVigencia_SinPlan_LanzaSinPlan()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.IdPlan = null;
            ctx.DalCliente.ClientePorId = cliente;
            try { ctx.Crear().VerificarVigencia(10); Assert.Fail("Debía exigir un plan asignado."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.sin_plan", ex.Clave); }
        }

        [TestMethod]
        public void VerificarVigencia_SuspendidoPorPago_LanzaPagoSuspendido_AntesQueVencimiento()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.FechaVencimiento = DateTime.Today.AddDays(-10); // también vencida
            cliente.FechaLimiteGracia = DateTime.Today.AddDays(-1); // gracia ya vencida → suspendido
            ctx.DalCliente.ClientePorId = cliente;
            try { ctx.Crear().VerificarVigencia(10); Assert.Fail("Debía bloquear por suspensión de pago."); }
            catch (BE.AppException ex)
            {
                // El chequeo de pago va ANTES que el de vencimiento genérico (a propósito,
                // ver comentario en BLL.Pedido.ObtenerClienteValidado) — verifica que siga así.
                Assert.AreEqual("err.bll.pedido.pago_suspendido", ex.Clave);
            }
        }

        [TestMethod]
        public void VerificarVigencia_SuscripcionVencida_LanzaSuscripcionVencida()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.FechaVencimiento = DateTime.Today.AddDays(-5);
            ctx.DalCliente.ClientePorId = cliente;
            try { ctx.Crear().VerificarVigencia(10); Assert.Fail("Debía rechazar una suscripción vencida."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.suscripcion_vencida", ex.Clave); }
        }

        [TestMethod]
        public void VerificarVigencia_SuscripcionPausada_LanzaSuscripcionPausada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.FechaPausaHasta = DateTime.Today.AddDays(5);
            ctx.DalCliente.ClientePorId = cliente;
            try { ctx.Crear().VerificarVigencia(10); Assert.Fail("Debía rechazar una suscripción pausada."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.suscripcion_pausada", ex.Clave); }
        }

        [TestMethod]
        public void VerificarVigencia_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            try { new Contexto().Crear().VerificarVigencia(10); Assert.Fail("Debía exigir sesión iniciada."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sesion_expirada", ex.Clave); }
        }

        // ── Revisar existencia de un pedido activo → ¿Posee pedido activo? ──

        [TestMethod]
        public void RevisarPedidoActivo_ConDespachoActivo_LanzaYaDespachado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPedido.PedidosDevueltos.Add(new BE.Pedido { IdPedido = 7, IdCliente = 10, Estado = BE.EstadoPedido.Despachado });
            try { ctx.Crear().RevisarPedidoActivo(ClienteConPlanVigente()); Assert.Fail("Debía bloquear con un pedido despachado."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.ya_despachado", ex.Clave); }
        }

        [TestMethod]
        public void RevisarPedidoActivo_PedidoEnCadaEstadoDelArmado_LanzaPedidoActivo()
        {
            LoginComoAdministrador();
            foreach (var estado in new[] { BE.EstadoPedido.EnControlStock, BE.EstadoPedido.ConFaltantes,
                                           BE.EstadoPedido.Separado, BE.EstadoPedido.Pendiente })
            {
                var ctx = new Contexto();
                ctx.DalPedido.PedidosDevueltos.Add(new BE.Pedido { IdPedido = 7, IdCliente = 10, Estado = estado });
                try
                {
                    ctx.Crear().RevisarPedidoActivo(ClienteConPlanVigente());
                    Assert.Fail($"Un pedido {estado} debía contar como activo.");
                }
                catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.pedido_activo", ex.Clave, estado.ToString()); }
            }
        }

        [TestMethod]
        public void RevisarPedidoActivo_PedidosTerminados_NoBloquean()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            foreach (var estado in new[] { BE.EstadoPedido.Cancelado, BE.EstadoPedido.Desistido, BE.EstadoPedido.Entregado })
                ctx.DalPedido.PedidosDevueltos.Add(new BE.Pedido { IdPedido = 7 + (int)estado, IdCliente = 10, Estado = estado });

            ctx.Crear().RevisarPedidoActivo(ClienteConPlanVigente()); // no lanza
        }

        [TestMethod]
        public void RevisarPedidoActivo_ConPrendasPendientesDeDevolucion_LanzaCuentaBloqueada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.PorCliente = new List<BE.Prenda>
            {
                new BE.Prenda { IdPrenda = 99, Nombre = "Blazer", Estado = BE.EstadoPrenda.EnUso, IdClienteActual = 10 }
            };
            try { ctx.Crear().RevisarPedidoActivo(ClienteConPlanVigente()); Assert.Fail("Debía estar bloqueada."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.cuenta_bloqueada", ex.Clave); }
        }

        // ── Comprobar el cupo del plan → ¿Excede el cupo disponible? ──

        [TestMethod]
        public void ComprobarCupo_DentroDelLimite_DevuelvePlan()
        {
            LoginComoAdministrador();
            var plan = new Contexto().Crear().ComprobarCupo(ClienteConPlanVigente(), 2); // límite 3
            Assert.AreEqual("Básico", plan.Nombre);
        }

        [TestMethod]
        public void ComprobarCupo_ExcedeLimite_LanzaLimitePlan()
        {
            LoginComoAdministrador();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            cliente.StockUtilizado = 1;
            try { new Contexto().Crear().ComprobarCupo(cliente, 1); Assert.Fail("Debía rechazar superar el límite."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.limite_plan", ex.Clave); }
        }

        // ── Enviar selección para control stock (sin reservar prendas) ──

        [TestMethod]
        public void EnviarAControlStock_DatosValidos_CreaPedidoEnControlSinReservar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalPedido.AltaIdGenerado = 99;

            int id = ctx.Crear().EnviarAControlStock("Test", 10, new List<BE.Prenda> { PrendaDisponible() });

            Assert.AreEqual(99, id);
            Assert.AreEqual(1, ctx.DalPedido.AltaSinReservaVeces);
            Assert.AreEqual(BE.EstadoPedido.EnControlStock, ctx.DalPedido.UltimoAltaSinReserva.Estado);
            Assert.IsNotNull(ctx.DalPedido.UltimoAltaSinReserva.FechaEnvioControl);
            Assert.AreEqual(5, ctx.DalPedido.UltimoAltaSinReserva.IdEmpleado);
            Assert.AreEqual(0, ctx.DalPedido.SepararPrendasVeces, "Enviar a control no reserva prendas.");
            Assert.AreEqual("ENVIAR_CONTROL", ctx.DalHistorial.UltimoCambiosRegistrados[0].Accion);
        }

        [TestMethod]
        public void EnviarAControlStock_SinPrendas_LanzaSinPrendas()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            try { ctx.Crear().EnviarAControlStock("Test", 10, new List<BE.Prenda>()); Assert.Fail("Debía exigir prendas."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.sin_prendas", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.AltaSinReservaVeces);
        }

        [TestMethod]
        public void EnviarAControlStock_ConPedidoActivo_NoCreaNada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalPedido.PedidosDevueltos.Add(new BE.Pedido { IdPedido = 7, IdCliente = 10, Estado = BE.EstadoPedido.EnControlStock });
            try { ctx.Crear().EnviarAControlStock("Test", 10, new List<BE.Prenda> { PrendaDisponible() }); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.pedido_activo", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.AltaSinReservaVeces);
        }

        [TestMethod]
        public void EnviarAControlStock_SuperaLimiteDelPlan_LanzaLimitePlan()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            ctx.DalCliente.ClientePorId = cliente;
            try
            {
                ctx.Crear().EnviarAControlStock("Test", 10, new List<BE.Prenda> { Prenda(1), Prenda(2) });
                Assert.Fail("Una selección que excede el cupo no se envía a control.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.limite_plan", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.AltaSinReservaVeces);
        }

        [TestMethod]
        public void EnviarAControlStock_SinEmpleadoVinculado_LanzaEmpleadoSinVinculo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalEmpleado.EmpleadoPorUsuario = null; // usuario sin Empleado vinculado
            try { ctx.Crear().EnviarAControlStock("Test", 10, new List<BE.Prenda> { PrendaDisponible() }); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.empleado_sin_vinculo", ex.Clave); }
        }

        // ── Asentar desistimiento ──

        [TestMethod]
        public void AsentarDesistimiento_SeleccionQueExcedeElCupo_RegistraPedidoDesistidoSinReservar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            ctx.DalCliente.ClientePorId = cliente;
            ctx.DalPedido.AltaIdGenerado = 77;

            int id = ctx.Crear().AsentarDesistimiento("Test", 10, new List<BE.Prenda> { Prenda(1), Prenda(2) }, "  No quiere menos prendas ");

            Assert.AreEqual(77, id);
            var p = ctx.DalPedido.UltimoAltaSinReserva;
            Assert.AreEqual(BE.EstadoPedido.Desistido, p.Estado);
            Assert.AreEqual(BE.EtapaDesistimiento.Cupo, p.EtapaDesistimiento);
            Assert.AreEqual("No quiere menos prendas", p.MotivoDesistimiento);
            Assert.AreEqual(0, ctx.DalPedido.SepararPrendasVeces);
        }

        [TestMethod]
        public void AsentarDesistimiento_SeleccionDentroDelCupo_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente(); // límite 3
            try { ctx.Crear().AsentarDesistimiento("Test", 10, new List<BE.Prenda> { Prenda(1) }, "x"); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.desistimiento_sin_exceso", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.AltaSinReservaVeces);
        }

        [TestMethod]
        public void AsentarDesistimiento_SinMotivo_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            try { ctx.Crear().AsentarDesistimiento("Test", PedidoEn(BE.EstadoPedido.ConFaltantes, Prenda(1)), "  ", BE.EtapaDesistimiento.Disponibilidad); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.desistir_sin_motivo", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.RegistrarDesistimientoVeces);
        }

        [TestMethod]
        public void AsentarDesistimiento_PedidoConFaltantes_PasaADesistido()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.Crear().AsentarDesistimiento("Test", PedidoEn(BE.EstadoPedido.ConFaltantes, Prenda(1)),
                                             "No le sirven las alternativas", BE.EtapaDesistimiento.Disponibilidad);

            Assert.AreEqual(1, ctx.DalPedido.RegistrarDesistimientoVeces);
            Assert.AreEqual(BE.EtapaDesistimiento.Disponibilidad, ctx.DalPedido.UltimaEtapaDesistimiento);
            Assert.AreEqual("DESISTIR", ctx.DalHistorial.UltimoCambiosRegistrados[0].Accion);
        }

        [TestMethod]
        public void AsentarDesistimiento_PedidoQueNoTieneFaltantes_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            try { ctx.Crear().AsentarDesistimiento("Test", PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1)), "x", BE.EtapaDesistimiento.Disponibilidad); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.desistir_estado", ex.Clave); }
        }

        // ── Recibir selección ajustada por disponibilidad (vuelve al control del cupo) ──

        [TestMethod]
        public void AjustarSeleccion_PedidoConFaltantes_ReemplazaYVuelveAControl()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            var pedido = PedidoEn(BE.EstadoPedido.ConFaltantes, Prenda(1), Prenda(2));
            ctx.DalPedido.PedidosDevueltos.Add(pedido); // el propio pedido no cuenta como "otro activo"

            ctx.Crear().AjustarSeleccion("Test", pedido, new List<BE.Prenda> { Prenda(1), Prenda(3) });

            Assert.AreEqual(1, ctx.DalPedido.ReemplazarSeleccionVeces);
            Assert.AreEqual(2, ctx.DalPedido.UltimaSeleccionReemplazada.Count);
            var estado = ctx.DalHistorial.UltimoCambiosRegistrados.Find(c => c.Campo == "Estado");
            Assert.AreEqual("EnControlStock", estado.ValorNuevo);
        }

        [TestMethod]
        public void AjustarSeleccion_ExcedeElCupo_NoReemplaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            ctx.DalCliente.ClientePorId = cliente;
            try { ctx.Crear().AjustarSeleccion("Test", PedidoEn(BE.EstadoPedido.ConFaltantes, Prenda(1)), new List<BE.Prenda> { Prenda(1), Prenda(2) }); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.limite_plan", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.ReemplazarSeleccionVeces);
        }

        [TestMethod]
        public void AjustarSeleccion_PedidoSinFaltantes_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            try { ctx.Crear().AjustarSeleccion("Test", PedidoEn(BE.EstadoPedido.Separado, Prenda(1)), new List<BE.Prenda> { Prenda(1) }); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.ajustar_estado", ex.Clave); }
        }

        // Nota del diagrama: "desde aquí la selección queda formalizada y no admite modificaciones".
        [TestMethod]
        public void PedidoFormalizado_NoAdmiteModificaciones()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            var formalizado = PedidoEn(BE.EstadoPedido.Pendiente, Prenda(1));
            formalizado.FechaFormalizacion = DateTime.Now;
            var bll = ctx.Crear();

            try { bll.AjustarSeleccion("Test", formalizado, new List<BE.Prenda> { Prenda(2) }); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.ajustar_estado", ex.Clave); }
            try { bll.AsentarDesistimiento("Test", formalizado, "x", BE.EtapaDesistimiento.Disponibilidad); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.desistir_estado", ex.Clave); }
            try { bll.FormalizarPedido("Test", formalizado); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.formalizar_estado", ex.Clave); }

            Assert.AreEqual(0, ctx.DalPedido.ReemplazarSeleccionVeces);
            Assert.AreEqual(0, ctx.DalPedido.RegistrarDesistimientoVeces);
            Assert.AreEqual(0, ctx.DalPedido.FormalizarVeces);
        }

        // La selección ajustada vuelve al merge del cupo, no a la verificación de vigencia.
        [TestMethod]
        public void AjustarSeleccion_SoloVuelveAComprobarElCupo()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.FechaVencimiento = DateTime.Today.AddDays(-1); // venció mientras tenía faltantes
            ctx.DalCliente.ClientePorId = cliente;
            var pedido = PedidoEn(BE.EstadoPedido.ConFaltantes, Prenda(1));

            ctx.Crear().AjustarSeleccion("Test", pedido, new List<BE.Prenda> { Prenda(3) });

            Assert.AreEqual(1, ctx.DalPedido.ReemplazarSeleccionVeces);
        }

        // ¿Desea ajustar la selección? No (al ajustar excede el cupo) → Asentar desistimiento.
        [TestMethod]
        public void AsentarDesistimiento_PorCupoAlAjustar_GuardaLaSeleccionAjustada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            ctx.DalCliente.ClientePorId = cliente;
            var ajustada = new List<BE.Prenda> { Prenda(3), Prenda(4) };

            ctx.Crear().AsentarDesistimiento("Test", PedidoEn(BE.EstadoPedido.ConFaltantes, Prenda(1)),
                                             "Quiere las dos", BE.EtapaDesistimiento.Cupo, ajustada);

            Assert.AreEqual(BE.EtapaDesistimiento.Cupo, ctx.DalPedido.UltimaEtapaDesistimiento);
            CollectionAssert.AreEqual(ajustada, ctx.DalPedido.UltimaSeleccionDesistida);
        }

        [TestMethod]
        public void AsentarDesistimiento_PorCupoAlAjustar_SinExceso_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente(); // límite 3
            try
            {
                ctx.Crear().AsentarDesistimiento("Test", PedidoEn(BE.EstadoPedido.ConFaltantes, Prenda(1)),
                                                 "x", BE.EtapaDesistimiento.Cupo, new List<BE.Prenda> { Prenda(3) });
                Assert.Fail();
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.desistimiento_sin_exceso", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.RegistrarDesistimientoVeces);
        }

        // El desistimiento por cupo ocurre después de "¿Posee pedido activo? No".
        [TestMethod]
        public void AsentarDesistimiento_PorCupo_ClienteConPedidoActivo_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.LimitePrendas = 1;
            ctx.DalCliente.ClientePorId = cliente;
            ctx.DalPedido.PedidosDevueltos.Add(new BE.Pedido { IdPedido = 2, IdCliente = 10, Estado = BE.EstadoPedido.EnControlStock });

            try { ctx.Crear().AsentarDesistimiento("Test", 10, new List<BE.Prenda> { Prenda(1), Prenda(2) }, "x"); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.pedido_activo", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.AltaSinReservaVeces);
        }

        // ── Depósito: Revisar stock → ¿Selección disponible? ──

        [TestMethod]
        public void RevisarStock_ReleeElEstadoRealDeCadaPrenda()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1), Prenda(2, estado: BE.EstadoPrenda.EnUso) };

            var lineas = ctx.Crear().RevisarStock(PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1), Prenda(2)));

            Assert.IsTrue(lineas.Find(l => l.Prenda.IdPrenda == 1).Disponible);
            var falta = lineas.Find(l => l.Prenda.IdPrenda == 2);
            Assert.IsFalse(falta.Disponible);
            Assert.AreEqual(BE.EstadoPrenda.EnUso, falta.EstadoActual);
        }

        [TestMethod]
        public void RevisarStock_PrendaReservadaParaOtroCliente_NoEstaDisponible()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1) };
            ctx.DalListaEspera.EstaReservadaParaOtroRespuesta = true;

            var lineas = ctx.Crear().RevisarStock(PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1)));

            Assert.IsTrue(lineas[0].ReservadaParaOtro);
            Assert.IsFalse(lineas[0].Disponible);
        }

        [TestMethod]
        public void RevisarStock_PedidoQueNoEstaEnControl_Rechaza()
        {
            LoginComoAdministrador();
            try { new Contexto().Crear().RevisarStock(PedidoEn(BE.EstadoPedido.Separado, Prenda(1))); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.control_estado", ex.Clave); }
        }

        [TestMethod]
        public void AccionesDeDeposito_SinPermisoDeControlDeStock_Rechazan()
        {
            LoginComo("Vendedor", BE.Patentes.PedidosVenta, BE.Patentes.PedidosVentaEditar);
            var ctx = new Contexto();
            var pedido = PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1));
            var bll = ctx.Crear();

            try { bll.RevisarStock(pedido); Assert.Fail("El Vendedor no revisa el stock."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
            try { bll.SepararPrendas("Test", pedido); Assert.Fail("El Vendedor no separa prendas."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.SepararPrendasVeces);
        }

        // ── Depósito: ¿Selección disponible? No → Informe de prendas faltantes (y alternativas) ──

        [TestMethod]
        public void InformarFaltantes_SugiereAlternativasDeLaMismaCategoriaYTalle()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1), Prenda(2, estado: BE.EstadoPrenda.EnLimpieza) };
            ctx.DalPrenda.Disponibles = new List<BE.Prenda>
            {
                Prenda(1),                         // ya está en el pedido: no es alternativa
                Prenda(3),                         // misma categoría y talle: sí
                Prenda(4, talle: "S"),             // otro talle: no
                Prenda(5, categoria: "Vestidos")   // otra categoría: no
            };
            var pedido = PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1), Prenda(2));

            var faltantes = ctx.Crear().InformarFaltantes("Test", pedido);

            Assert.AreEqual(1, faltantes.Count);
            Assert.AreEqual(2, faltantes[0].Prenda.IdPrenda);
            Assert.AreEqual(BE.EstadoPrenda.EnLimpieza, faltantes[0].EstadoAlRevisar);
            CollectionAssert.AreEqual(new[] { 3 }, faltantes[0].Alternativas.ConvertAll(a => a.IdPrenda));
            Assert.AreEqual(1, ctx.DalPedido.RegistrarFaltantesVeces);
            Assert.AreEqual(5, ctx.DalPedido.UltimoIdEmpleadoControl);
            Assert.AreEqual("INFORMAR_FALTANTES", ctx.DalHistorial.UltimoCambiosRegistrados[0].Accion);
        }

        [TestMethod]
        public void InformarFaltantes_TodoDisponible_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1) };
            try { ctx.Crear().InformarFaltantes("Test", PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1))); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.sin_faltantes", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.RegistrarFaltantesVeces);
        }

        // ── Depósito: ¿Selección disponible? Sí → Confirmar prendas disponibles → Separar ──

        [TestMethod]
        public void ConfirmarPrendasDisponibles_TodoDisponible_LasConfirma()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1) };

            ctx.Crear().ConfirmarPrendasDisponibles("Test", PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1)));

            Assert.AreEqual(1, ctx.DalPedido.ConfirmarPrendasVeces);
            Assert.AreEqual("CONFIRMAR_PRENDAS", ctx.DalHistorial.UltimoCambiosRegistrados[0].Accion);
        }

        [TestMethod]
        public void ConfirmarPrendasDisponibles_ConUnaFaltante_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1, estado: BE.EstadoPrenda.EnUso) };
            try { ctx.Crear().ConfirmarPrendasDisponibles("Test", PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1))); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.hay_faltantes", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.ConfirmarPrendasVeces);
        }

        [TestMethod]
        public void SepararPrendas_SinConfirmarAntes_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            try { ctx.Crear().SepararPrendas("Test", PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1))); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.sin_confirmar", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.SepararPrendasVeces);
        }

        [TestMethod]
        public void SepararPrendas_Confirmadas_ReservaYCierraLaListaDeEspera()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1) };
            var pedido = PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1));
            pedido.PrendasConfirmadas = new List<int> { 1 };

            ctx.Crear().SepararPrendas("Test", pedido);

            Assert.AreEqual(1, ctx.DalPedido.SepararPrendasVeces);
            Assert.AreEqual(1, ctx.DalListaEspera.CerrarSiReservadaVeces);
            var estado = ctx.DalHistorial.UltimoCambiosRegistrados.Find(c => c.Campo == "Estado");
            Assert.AreEqual("Separado", estado.ValorNuevo);
        }

        [TestMethod]
        public void SepararPrendas_OtraOperacionTomoLaPrenda_EmiteInformeDeFaltantesYNoSepara()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1) };   // disponible al revisar
            var pedido = PedidoEn(BE.EstadoPedido.EnControlStock, Prenda(1));
            pedido.PrendasConfirmadas = new List<int> { 1 };
            ctx.DalPedido.PedidosDevueltos.Add(pedido);
            // Entre la revisión y el UPDATE otra operación toma la prenda: la DAL rechaza la reserva
            // (rollback) y, al releerla, la prenda ya figura en uso.
            ctx.DalPedido.AntesDeSeparar = () =>
                ctx.DalPrenda.Todas = new List<BE.Prenda> { Prenda(1, estado: BE.EstadoPrenda.EnUso) };
            ctx.DalPedido.SepararPrendasLanza = new BE.AppException("err.dal.pedido.prenda_tomada", "tomada");

            try
            {
                ctx.Crear().SepararPrendas("Test", pedido);
                Assert.Fail("Debía volver al circuito de faltantes.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.separar_faltantes", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.SepararPrendasVeces, "No queda nada separado.");
            Assert.AreEqual(1, ctx.DalPedido.RegistrarFaltantesVeces, "¿Selección disponible? No → informe de faltantes.");
        }

        // ── Formalizar el pedido / Preparar la confirmación ──

        [TestMethod]
        public void FormalizarPedido_Separado_PasaAPendiente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();

            ctx.Crear().FormalizarPedido("Test", PedidoEn(BE.EstadoPedido.Separado, Prenda(1)));

            Assert.AreEqual(1, ctx.DalPedido.FormalizarVeces);
            var estado = ctx.DalHistorial.UltimoCambiosRegistrados.Find(c => c.Campo == "Estado");
            Assert.AreEqual("Separado", estado.ValorAnterior);
            Assert.AreEqual("Pendiente", estado.ValorNuevo);
        }

        [TestMethod]
        public void FormalizarPedido_SinSeparar_Rechaza()
        {
            LoginComoAdministrador();
            foreach (var estado in new[] { BE.EstadoPedido.EnControlStock, BE.EstadoPedido.ConFaltantes, BE.EstadoPedido.Pendiente })
            {
                var ctx = new Contexto();
                try { ctx.Crear().FormalizarPedido("Test", PedidoEn(estado, Prenda(1))); Assert.Fail(estado.ToString()); }
                catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.formalizar_estado", ex.Clave); }
                Assert.AreEqual(0, ctx.DalPedido.FormalizarVeces);
            }
        }

        [TestMethod]
        public void PrepararConfirmacion_PedidoNoFormalizado_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPedido.PedidosDevueltos.Add(PedidoEn(BE.EstadoPedido.Separado, Prenda(1)));
            try { ctx.Crear().PrepararConfirmacion("Test", 50); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.confirmacion_estado", ex.Clave); }
        }

        [TestMethod]
        public void PrepararConfirmacion_PedidoFormalizado_LoDevuelve()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var pedido = PedidoEn(BE.EstadoPedido.Pendiente, Prenda(1));
            pedido.FechaFormalizacion = DateTime.Now;
            ctx.DalPedido.PedidosDevueltos.Add(pedido);

            Assert.AreEqual(50, ctx.Crear().PrepararConfirmacion("Test", 50).IdPedido);
        }

        [TestMethod]
        public void PrepararConfirmacion_PedidoFormalizadoYCancelado_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var pedido = PedidoEn(BE.EstadoPedido.Cancelado, Prenda(1));
            pedido.FechaFormalizacion = DateTime.Now;
            ctx.DalPedido.PedidosDevueltos.Add(pedido);
            try { ctx.Crear().PrepararConfirmacion("Test", 50); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.confirmacion_estado", ex.Clave); }
        }

        // ── Máquina de estados del pedido (BE) ──

        [TestMethod]
        public void TransicionesDelArmado_SiguenElDiagrama()
        {
            var p = new BE.Pedido { Estado = BE.EstadoPedido.EnControlStock };
            Assert.IsTrue(p.TransicionValida(BE.EstadoPedido.ConFaltantes));
            Assert.IsTrue(p.TransicionValida(BE.EstadoPedido.Separado));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPedido.Pendiente), "No se formaliza sin separar.");

            p.Estado = BE.EstadoPedido.ConFaltantes;
            Assert.IsTrue(p.TransicionValida(BE.EstadoPedido.EnControlStock));
            Assert.IsTrue(p.TransicionValida(BE.EstadoPedido.Desistido));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPedido.Separado));

            p.Estado = BE.EstadoPedido.Separado;
            Assert.IsTrue(p.TransicionValida(BE.EstadoPedido.Pendiente));
            Assert.IsFalse(p.TransicionValida(BE.EstadoPedido.Cancelado));

            p.Estado = BE.EstadoPedido.Desistido;
            Assert.IsFalse(p.TransicionValida(BE.EstadoPedido.EnControlStock), "Desistido es final.");
        }

        [TestMethod]
        public void RestaurarOperacion_PasoDelControlDeStock_NoSePuedeRevertir()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalHistorial.CambiosParaOperacion = new List<BE.PedidoHistorial>
            {
                new BE.PedidoHistorial { Campo = "Estado", Accion = "SEPARAR", ValorAnterior = "EnControlStock", ValorNuevo = "Separado" }
            };
            try { ctx.Crear().RestaurarOperacion("Test", 1, 3); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.restaurar_no_permitido", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.RestaurarOperacionAtomicaVeces);
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
        public void MarcarEntregado_PedidoDespachado_Entrega()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Despachado };

            bll.MarcarEntregado("Test", pedido);

            Assert.AreEqual(1, ctx.DalPedido.MarcarEntregadoVeces);
        }

        [TestMethod]
        public void MarcarEntregado_PedidoNoDespachado_LanzaEntregarEstado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.Pendiente };

            try
            {
                bll.MarcarEntregado("Test", pedido);
                Assert.Fail("Debía rechazar marcar como entregado un pedido no Despachado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.entregar_estado", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPedido.MarcarEntregadoVeces);
        }

        [TestMethod]
        public void RegistrarDevolucion_PedidoEntregado_DevuelveLasPrendasYRegistraElHistorial()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPedido.RegistrarDevolucionRespuesta = 2; // dos prendas pasan de En uso a En limpieza
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, IdCliente = 10, Estado = BE.EstadoPedido.Entregado };

            bll.RegistrarDevolucion("Test", pedido);

            Assert.AreEqual(1, ctx.DalPedido.RegistrarDevolucionVeces, "La devolución (En uso a En limpieza, con mantenimiento) se hace una sola vez.");
            Assert.AreEqual(1, ctx.DalHistorial.RegistrarCambiosVeces, "Queda la operación DEVOLUCION en el historial.");
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
        public void DesCancelar_OtraSesionLoCambio_LanzaEstadoCambiado()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalPedido.DesCancelarRespuesta = false;
            var bll = ctx.Crear();
            var pedido = new BE.Pedido { IdPedido = 1, IdCliente = 10, Estado = BE.EstadoPedido.Cancelado };

            try
            {
                bll.DesCancelar("Test", pedido);
                Assert.Fail("Debía rechazar si el pedido ya no está Cancelado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.pedido.estado_cambiado", ex.Clave);
            }
        }

        [TestMethod]
        public void DesCancelar_Reactiva_VuelveAControlDeStockSinReservar()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            var bll = ctx.Crear();
            var pedido = new BE.Pedido
            {
                IdPedido = 1,
                IdCliente = 10,
                Estado = BE.EstadoPedido.Cancelado,
                MotivoCancelacion = "Cliente se arrepintió"
            };

            bll.DesCancelar("Test", pedido);

            Assert.AreEqual(1, ctx.DalPedido.DesCancelarVeces);
            Assert.AreEqual(1, ctx.DalHistorial.RegistrarCambiosVeces);

            var estado = ctx.DalHistorial.UltimoCambiosRegistrados.Find(c => c.Campo == "Estado");
            Assert.AreEqual("Cancelado", estado.ValorAnterior);
            Assert.AreEqual("EnControlStock", estado.ValorNuevo);
            Assert.AreEqual(0, ctx.DalPedido.SepararPrendasVeces, "Reactivar no reserva: separa Depósito.");

            var motivo = ctx.DalHistorial.UltimoCambiosRegistrados.Find(c => c.Campo == "MotivoCancelacion");
            Assert.AreEqual("Cliente se arrepintió", motivo.ValorAnterior);
            Assert.IsNull(motivo.ValorNuevo);
        }

        // Reactivar vuelve a "Enviar selección para control stock": no puede saltarse las reglas de PN01.
        [TestMethod]
        public void DesCancelar_ClienteConOtroPedidoActivo_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalPedido.PedidosDevueltos.Add(new BE.Pedido { IdPedido = 2, IdCliente = 10, Estado = BE.EstadoPedido.EnControlStock });
            var pedido = new BE.Pedido { IdPedido = 1, IdCliente = 10, Estado = BE.EstadoPedido.Cancelado };

            try { ctx.Crear().DesCancelar("Test", pedido); Assert.Fail("Debía rechazar: el cliente ya tiene otro pedido en curso."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.pedido_activo", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.DesCancelarVeces);
        }

        [TestMethod]
        public void DesCancelar_SuscripcionVencida_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var cliente = ClienteConPlanVigente();
            cliente.FechaVencimiento = DateTime.Today.AddDays(-3);
            ctx.DalCliente.ClientePorId = cliente;
            var pedido = new BE.Pedido { IdPedido = 1, IdCliente = 10, Estado = BE.EstadoPedido.Cancelado };

            try { ctx.Crear().DesCancelar("Test", pedido); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.suscripcion_vencida", ex.Clave); }
            Assert.AreEqual(0, ctx.DalPedido.DesCancelarVeces);
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

        [TestMethod]
        public void RestaurarOperacion_ConCambiosRegistrados_RevierteYReRegistraElHistorialConLosValoresInvertidos()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalHistorial.CambiosParaOperacion = new List<BE.PedidoHistorial>
            {
                new BE.PedidoHistorial { Campo = "Estado", Accion = "CANCELAR", ValorAnterior = "Pendiente", ValorNuevo = "Cancelado" }
            };
            var bll = ctx.Crear();

            bll.RestaurarOperacion("Test", 1, 5);

            // El DAL debe recibir el ValorAnterior original (a donde hay que volver).
            Assert.AreEqual(1, ctx.DalPedido.RestaurarOperacionAtomicaVeces);
            Assert.AreEqual("Estado", ctx.DalPedido.UltimoRestaurarOperacionCampos[0].Campo);
            Assert.AreEqual("Pendiente", ctx.DalPedido.UltimoRestaurarOperacionCampos[0].ValorAnterior);

            // El nuevo evento RESTAURAR debe quedar con los valores INVERTIDOS respecto al cambio
            // original: lo que estaba "Nuevo" pasa a ser el "Anterior" de este evento (el estado
            // del que se viene al restaurar) y el "Anterior" original pasa a ser el "Nuevo" (a
            // donde se vuelve) — swap fácil de invertir por error, de ahí el test explícito.
            Assert.AreEqual(1, ctx.DalHistorial.RegistrarCambiosVeces);
            var restaurado = ctx.DalHistorial.UltimoCambiosRegistrados.Find(c => c.Campo == "Estado");
            Assert.AreEqual("RESTAURAR", restaurado.Accion);
            Assert.AreEqual("Cancelado", restaurado.ValorAnterior);
            Assert.AreEqual("Pendiente", restaurado.ValorNuevo);
        }

        // ── PN01/PN04: cuenta bloqueada hasta registrar la devolución (regla de NUULY) ────

        [TestMethod]
        public void ValidarPuedeArmarPedido_SinPrendasPendientes_DevuelveElCliente()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();

            var cliente = ctx.Crear().ValidarPuedeArmarPedido(10);

            Assert.AreEqual(10, cliente.IdCliente);
        }

        [TestMethod]
        public void ValidarPuedeArmarPedido_ConPrendasPendientes_LanzaCuentaBloqueada_YSeDesbloqueaAlDevolver()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalCliente.ClientePorId = ClienteConPlanVigente();
            ctx.DalPrenda.PorCliente = new List<BE.Prenda> { new BE.Prenda { IdPrenda = 99, Estado = BE.EstadoPrenda.EnUso } };
            var bll = ctx.Crear();

            try { bll.ValidarPuedeArmarPedido(10); Assert.Fail("Debía estar bloqueada."); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.pedido.cuenta_bloqueada", ex.Clave); }

            // PN04: al registrarse la devolución las prendas dejan de estar EnUso y la cuenta se desbloquea.
            ctx.DalPrenda.PorCliente = new List<BE.Prenda>();
            Assert.AreEqual(10, bll.ValidarPuedeArmarPedido(10).IdCliente);
        }
    }
}
