using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Reglas que la auditoría de GUI sacó de los formularios y llevó a la BLL:
    ///   • BLL.PanelAlertas: cada rol ve solo las alertas que puede atender (DV/backup → admin).
    ///   • BLL.EvaluacionControlStock: "¿Selección disponible?" y acciones de Depósito (PN01).
    ///   • BLL.InspeccionDevolucion: cargo + baja en UNA operación del DAL (PN04).
    ///   • BLL.PanelTareas: tablero logístico y suscripciones a gestionar.
    ///   • BLL.ListaEspera: el Vendedor puede anotar con su patente propia.
    /// </summary>
    [TestClass]
    public class AuditoriaGuiTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static BE.Usuario Usuario(string perfil, params string[] patentes)
        {
            var u = new BE.Usuario { Id = 7, Username = "u_" + perfil, Perfil = perfil, Contraseña = Encriptador.Hash("Clave1!") };
            foreach (var p in patentes) u.Permisos.Add(new BE.Permiso { NombreMenu = p });
            return u;
        }

        private static void LoginComoAdministrador() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        // Todas las alertas posibles activas a la vez.
        private static List<BE.Alerta> TodasLasAlertas() =>
            BLL.PanelAlertas.EvaluarAlertas(vencidas: 1, porVencer: 1, diasSinBackup: -1, enLimpieza: 1, dvRotas: 2,
                reservadasEspera: 1, enControl: 1, conFaltantes: 1, separados: 1, contratacionesPendientes: 1);

        private static string[] Claves(IEnumerable<BE.Alerta> a) => a.Select(x => x.ClaveI18n).ToArray();

        // ── PanelAlertas: filtro por patentes ─────────────────────────────────

        [TestMethod]
        public void Alertas_Vendedor_NoVeIntegridadNiBackup()
        {
            var visibles = BLL.PanelAlertas.FiltrarPorPatentes(TodasLasAlertas(),
                new[] { BE.Patentes.Clientes, BE.Patentes.PedidosVenta, BE.Patentes.ListaEspera }, esAdmin: false);
            var claves = Claves(visibles);

            CollectionAssert.DoesNotContain(claves, "alert.dv.corruptos");
            CollectionAssert.DoesNotContain(claves, "alert.backup.nunca");
            CollectionAssert.DoesNotContain(claves, "alert.contr.pendientes", "La cola de cobro es de Caja.");
            CollectionAssert.Contains(claves, "alert.pedidos.faltantes");
            CollectionAssert.Contains(claves, "alert.pedidos.formalizar");
            CollectionAssert.Contains(claves, "alert.subs.vencidas");
        }

        [TestMethod]
        public void Alertas_Caja_SoloVeLaColaDeCobro()
        {
            var claves = Claves(BLL.PanelAlertas.FiltrarPorPatentes(TodasLasAlertas(), new[] { BE.Patentes.Caja }, false));
            CollectionAssert.AreEquivalent(new[] { "alert.contr.pendientes" }, claves);
        }

        [TestMethod]
        public void Alertas_Deposito_VeControlDeStockYLimpieza_NoFaltantes()
        {
            var claves = Claves(BLL.PanelAlertas.FiltrarPorPatentes(TodasLasAlertas(),
                new[] { BE.Patentes.ControlStock, BE.Patentes.Prendas, BE.Patentes.InspeccionDevolucion }, false));
            CollectionAssert.Contains(claves, "alert.pedidos.control");
            CollectionAssert.Contains(claves, "alert.prendas.limpieza");
            CollectionAssert.DoesNotContain(claves, "alert.pedidos.faltantes");
            CollectionAssert.DoesNotContain(claves, "alert.dv.corruptos");
        }

        [TestMethod]
        public void Alertas_Administrador_VeTodo()
        {
            var todas = TodasLasAlertas();
            Assert.AreEqual(todas.Count, BLL.PanelAlertas.FiltrarPorPatentes(todas, new string[0], esAdmin: true).Count);
        }

        [TestMethod]
        public void Alertas_PatenteDeAdministracion_VeIntegridad_SinDistinguirMayusculas()
        {
            Assert.IsTrue(BLL.PanelAlertas.PuedeVer("alert.dv.corruptos", new[] { "MNUUSUARIOS" }, false));
            Assert.IsFalse(BLL.PanelAlertas.PuedeVer("alert.dv.corruptos", new[] { BE.Patentes.Auditoria }, false));
        }

        [TestMethod]
        public void Alertas_ClaveSinDuenio_SeOcultaANoAdmins()
        {
            Assert.IsFalse(BLL.PanelAlertas.PuedeVer("alert.inexistente", new[] { BE.Patentes.Usuarios }, false));
            Assert.IsTrue(BLL.PanelAlertas.PuedeVer("alert.inexistente", new string[0], true));
        }

        [TestMethod]
        public void Alertas_TodaClaveQueEmiteEvaluarTieneDuenio()
        {
            foreach (var a in TodasLasAlertas())
                Assert.IsTrue(BLL.PanelAlertas.PatentesPorAlerta.ContainsKey(a.ClaveI18n), a.ClaveI18n);
        }

        [TestMethod]
        public void PuedeVerIntegridad_SoloAdminOPatenteDeUsuarios()
        {
            Assert.IsFalse(BLL.PanelAlertas.PuedeVerIntegridad(null));
            Assert.IsFalse(BLL.PanelAlertas.PuedeVerIntegridad(Usuario("Vendedor", BE.Patentes.Clientes)));
            Assert.IsTrue(BLL.PanelAlertas.PuedeVerIntegridad(Usuario("Administrador")));
            Assert.IsTrue(BLL.PanelAlertas.PuedeVerIntegridad(Usuario("Soporte", BE.Patentes.Usuarios)));
        }

        [TestMethod]
        public void ObtenerAlertas_SinUsuario_ListaVacia()
        {
            Assert.AreEqual(0, new BLL.PanelAlertas().ObtenerAlertas(null).Count);
        }

        // ── EvaluacionControlStock (PN01) ─────────────────────────────────────

        private static BE.LineaControlStock Linea(bool disponible) => new BE.LineaControlStock
        {
            Prenda = new BE.Prenda { IdPrenda = 1 },
            EstadoActual = disponible ? BE.EstadoPrenda.Disponible : BE.EstadoPrenda.EnUso
        };

        private static BE.Pedido PedidoEnControl(bool todasConfirmadas)
        {
            var p = new BE.Pedido { IdPedido = 1, Estado = BE.EstadoPedido.EnControlStock };
            p.Prendas = new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1 } };
            p.PrendasConfirmadas = todasConfirmadas ? new List<int> { 1 } : new List<int>();
            return p;
        }

        [TestMethod]
        public void ControlStock_TodasDisponiblesSinConfirmar_PermiteConfirmar()
        {
            var r = BLL.EvaluacionControlStock.Evaluar(new List<BE.LineaControlStock> { Linea(true), Linea(true) }, PedidoEnControl(false));
            Assert.IsTrue(r.SeleccionDisponible);
            Assert.IsTrue(r.PuedeConfirmar);
            Assert.IsFalse(r.PuedeSeparar);
            Assert.IsFalse(r.PuedeInformarFaltantes);
        }

        [TestMethod]
        public void ControlStock_TodasDisponiblesYConfirmadas_PermiteSeparar()
        {
            var r = BLL.EvaluacionControlStock.Evaluar(new List<BE.LineaControlStock> { Linea(true) }, PedidoEnControl(true));
            Assert.IsTrue(r.PuedeSeparar);
            Assert.IsFalse(r.PuedeConfirmar);
        }

        [TestMethod]
        public void ControlStock_AlgunaNoDisponible_SoloInformarFaltantes()
        {
            var r = BLL.EvaluacionControlStock.Evaluar(new List<BE.LineaControlStock> { Linea(true), Linea(false) }, PedidoEnControl(false));
            Assert.IsFalse(r.SeleccionDisponible);
            Assert.IsTrue(r.PuedeInformarFaltantes);
            Assert.IsFalse(r.PuedeConfirmar);
            Assert.IsFalse(r.PuedeSeparar);
        }

        [TestMethod]
        public void ControlStock_PlanillaVacia_NoEsSeleccionDisponible()
        {
            var r = BLL.EvaluacionControlStock.Evaluar(new List<BE.LineaControlStock>(), PedidoEnControl(false));
            Assert.IsFalse(r.SeleccionDisponible);
            Assert.IsTrue(r.PuedeInformarFaltantes);
        }

        [TestMethod]
        public void ControlStock_PedidoFueraDeControl_NadaHabilitado()
        {
            var pedido = PedidoEnControl(true);
            pedido.Estado = BE.EstadoPedido.Separado;
            var r = BLL.EvaluacionControlStock.Evaluar(new List<BE.LineaControlStock> { Linea(true) }, pedido);
            Assert.IsFalse(r.PuedeInformarFaltantes || r.PuedeConfirmar || r.PuedeSeparar);
            Assert.IsFalse(BLL.EvaluacionControlStock.Evaluar(null, null).PuedeInformarFaltantes);
        }

        // ── InspeccionDevolucion (PN04) ───────────────────────────────────────

        private sealed class FakeInspeccionDAL : DAL.Interfaces.IInspeccionDevolucionDAL
        {
            public readonly List<(BE.CargoPrenda Cargo, BE.EstadoPrenda Esperado)> Llamadas = new List<(BE.CargoPrenda, BE.EstadoPrenda)>();
            public Exception Falla;
            public int DarDeBajaConCargo(BE.CargoPrenda cargo, BE.EstadoPrenda estadoEsperado)
            {
                if (Falla != null) throw Falla;
                Llamadas.Add((cargo, estadoEsperado));
                return 99;
            }

            // Por defecto, la prenda es de un pedido entregado hace 45 días (compra tácita vencida).
            public BE.Pedido PedidoEnCurso = new BE.Pedido
            {
                IdPedido = 10, IdCliente = 3, Estado = BE.EstadoPedido.Entregado, FechaEntrega = Hoy.AddDays(-45)
            };
            public BE.Pedido ObtenerPedidoEnCurso(int idPrenda) => PedidoEnCurso;
        }

        private static readonly DateTime Hoy = new DateTime(2026, 10, 9);

        private static BLL.InspeccionDevolucion Inspeccion(FakeInspeccionDAL dal) =>
            new BLL.InspeccionDevolucion(dal, new PrendaServiceEspia(), () => Hoy);

        // Espía de CambiarEstado (re-implementa la interfaz sobre el fake existente).
        private sealed class PrendaServiceEspia : FakePrendaService, BLL.Interfaces.IPrendaService
        {
            public readonly List<BE.EstadoPrenda> Cambios = new List<BE.EstadoPrenda>();
            public new void CambiarEstado(string modulo, BE.Prenda prenda, BE.EstadoPrenda nuevoEstado, string actor = null)
                => Cambios.Add(nuevoEstado);
        }

        private static BE.Prenda Prenda(BE.EstadoPrenda estado, int? ultimoCliente = 3) => new BE.Prenda
        {
            IdPrenda = 5, Nombre = "Campera", Estado = estado, IdUltimoCliente = ultimoCliente,
            IdClienteActual = estado == BE.EstadoPrenda.EnUso ? ultimoCliente : null
        };

        [TestMethod]
        public void DarDeBajaConCargo_EnLimpieza_UnaSolaOperacionAtomica()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL();
            var bll = new BLL.InspeccionDevolucion(dal, new PrendaServiceEspia());
            var prenda = Prenda(BE.EstadoPrenda.EnLimpieza);

            int id = bll.DarDeBajaConCargo("Test", prenda, "Mancha irrecuperable", 1500m);

            Assert.AreEqual(99, id);
            Assert.AreEqual(1, dal.Llamadas.Count);
            Assert.AreEqual(BE.EstadoPrenda.EnLimpieza, dal.Llamadas[0].Esperado);
            Assert.AreEqual(3, dal.Llamadas[0].Cargo.IdCliente);
            Assert.AreEqual(1500m, dal.Llamadas[0].Cargo.Monto);
            Assert.AreEqual(BE.EstadoCargo.Pendiente, dal.Llamadas[0].Cargo.Estado);
            Assert.AreEqual(BE.EstadoPrenda.Baja, prenda.Estado);
        }

        [TestMethod]
        public void ReportarPerdida_EnUso_DaDeBajaYLimpiaClienteActual()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL();
            var prenda = Prenda(BE.EstadoPrenda.EnUso);

            Inspeccion(dal).ReportarPerdida("Test", prenda, "Perdida", 2000m);

            Assert.AreEqual(BE.EstadoPrenda.EnUso, dal.Llamadas.Single().Esperado);
            Assert.AreEqual(BE.EstadoPrenda.Baja, prenda.Estado);
            Assert.IsNull(prenda.IdClienteActual);
        }

        // PN04 — compra tácita: el cliente nunca recibió la prenda (pedido Separado, Pendiente o
        // Despachado). Antes se podía cobrar la reposición completa y despachar el pedido igual.
        [TestMethod]
        public void ReportarPerdida_PedidoNoEntregado_RechazaSinTocarElDAL()
        {
            LoginComoAdministrador();
            foreach (var estado in new[] { BE.EstadoPedido.Separado, BE.EstadoPedido.Pendiente, BE.EstadoPedido.Despachado })
            {
                var dal = new FakeInspeccionDAL();
                dal.PedidoEnCurso = new BE.Pedido { IdPedido = 10, IdCliente = 3, Estado = estado };
                var prenda = Prenda(BE.EstadoPrenda.EnUso);
                try
                {
                    Inspeccion(dal).ReportarPerdida("Test", prenda, "Perdida", 2000m);
                    Assert.Fail("Debía rechazar una prenda que el cliente todavía no recibió (pedido " + estado + ").");
                }
                catch (BE.AppException ex) { Assert.AreEqual("err.bll.insp.perdida_no_entregado", ex.Clave); }
                Assert.AreEqual(0, dal.Llamadas.Count);
                Assert.AreEqual(BE.EstadoPrenda.EnUso, prenda.Estado);
            }
        }

        [TestMethod]
        public void ReportarPerdida_AntesDeLos30Dias_RechazaConLosDiasQueFaltan()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL();
            dal.PedidoEnCurso.FechaEntrega = Hoy.AddDays(-29);
            try
            {
                Inspeccion(dal).ReportarPerdida("Test", Prenda(BE.EstadoPrenda.EnUso), "Perdida", 2000m);
                Assert.Fail("Debía rechazar: el plazo de compra tácita no venció.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.insp.perdida_plazo", ex.Clave);
                StringAssert.Contains(ex.Message, "Faltan 1 día");
            }
            Assert.AreEqual(0, dal.Llamadas.Count);
        }

        [TestMethod]
        public void ReportarPerdida_ElDia30_Permite()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL();
            dal.PedidoEnCurso.FechaEntrega = Hoy.AddDays(-30).AddHours(15);   // la hora no cuenta
            Inspeccion(dal).ReportarPerdida("Test", Prenda(BE.EstadoPrenda.EnUso), "Perdida", 2000m);
            Assert.AreEqual(1, dal.Llamadas.Count);
        }

        [TestMethod]
        public void PuedeReportarPerdida_SoloConPedidoEntregadoHace30Dias()
        {
            var bll = Inspeccion(new FakeInspeccionDAL());
            var prenda = Prenda(BE.EstadoPrenda.EnUso);
            var entregado = new BE.Pedido { Estado = BE.EstadoPedido.Entregado, FechaEntrega = Hoy.AddDays(-30) };
            Assert.IsTrue(bll.PuedeReportarPerdida(prenda, entregado));
            entregado.FechaEntrega = Hoy.AddDays(-10);
            Assert.IsFalse(bll.PuedeReportarPerdida(prenda, entregado));
            Assert.IsFalse(bll.PuedeReportarPerdida(prenda, new BE.Pedido { Estado = BE.EstadoPedido.Despachado }));
            Assert.IsFalse(bll.PuedeReportarPerdida(Prenda(BE.EstadoPrenda.EnLimpieza), entregado));
            Assert.IsFalse(bll.PuedeReportarPerdida(prenda, null));
        }

        [TestMethod]
        public void DarDeBajaConCargo_PrendaQueNoEstaEnLimpieza_RechazaSinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL();
            try
            {
                new BLL.InspeccionDevolucion(dal, new PrendaServiceEspia())
                    .DarDeBajaConCargo("Test", Prenda(BE.EstadoPrenda.Disponible), "x", 10m);
                Assert.Fail("Debía rechazar una prenda que no está En Limpieza.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.insp.no_en_limpieza", ex.Clave); }
            Assert.AreEqual(0, dal.Llamadas.Count);
        }

        [TestMethod]
        public void ReportarPerdida_SinUltimoCliente_RechazaSinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL();
            try
            {
                Inspeccion(dal)
                    .ReportarPerdida("Test", Prenda(BE.EstadoPrenda.EnUso, ultimoCliente: null), "x", 10m);
                Assert.Fail("Debía rechazar una prenda sin último cliente.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.cargoprenda.sin_cliente", ex.Clave); }
            Assert.AreEqual(0, dal.Llamadas.Count);
        }

        [TestMethod]
        public void DarDeBajaConCargo_MontoInvalido_RechazaSinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL();
            try
            {
                new BLL.InspeccionDevolucion(dal, new PrendaServiceEspia())
                    .DarDeBajaConCargo("Test", Prenda(BE.EstadoPrenda.EnLimpieza), "Rotura", 0m);
                Assert.Fail("Debía rechazar un monto no positivo.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.cargoprenda.monto_invalido", ex.Clave); }
            Assert.AreEqual(0, dal.Llamadas.Count);
        }

        [TestMethod]
        public void DarDeBajaConCargo_FallaLaTransaccion_LaPrendaNoCambiaEnMemoria()
        {
            LoginComoAdministrador();
            var dal = new FakeInspeccionDAL { Falla = new BE.AppException("err.dal.prenda.estado_cambio", "cambió") };
            var prenda = Prenda(BE.EstadoPrenda.EnLimpieza);
            try
            {
                new BLL.InspeccionDevolucion(dal, new PrendaServiceEspia()).DarDeBajaConCargo("Test", prenda, "Rotura", 10m);
                Assert.Fail("Debía propagar la falla del DAL.");
            }
            catch (BE.AppException) { }
            Assert.AreEqual(BE.EstadoPrenda.EnLimpieza, prenda.Estado);
        }

        [TestMethod]
        public void DarDeBajaConCargo_SinPermiso_Rechaza()
        {
            SessionManager.Login(Usuario("Vendedor", BE.Patentes.Clientes));
            var dal = new FakeInspeccionDAL();
            try
            {
                new BLL.InspeccionDevolucion(dal, new PrendaServiceEspia())
                    .DarDeBajaConCargo("Test", Prenda(BE.EstadoPrenda.EnLimpieza), "Rotura", 10m);
                Assert.Fail("Debía exigir el permiso de Stock.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
            Assert.AreEqual(0, dal.Llamadas.Count);
        }

        [TestMethod]
        public void AprobarReingreso_UsaCambiarEstadoADisponible()
        {
            LoginComoAdministrador();
            var espia = new PrendaServiceEspia();
            new BLL.InspeccionDevolucion(new FakeInspeccionDAL(), espia).AprobarReingreso("Test", Prenda(BE.EstadoPrenda.EnLimpieza));
            CollectionAssert.AreEqual(new[] { BE.EstadoPrenda.Disponible }, espia.Cambios);
        }

        // ── PanelTareas: tablero logístico y suscripciones ────────────────────

        private static BE.Pedido P(int id, BE.EstadoPedido e) =>
            new BE.Pedido { IdPedido = id, Estado = e, FechaPedido = DateTime.Now.AddDays(-id) };

        [TestMethod]
        public void TableroLogistica_SoloPedidosFormalizados_EnOrdenDeAntiguedad()
        {
            var t = BLL.PanelTareas.ClasificarLogistica(new[]
            {
                P(1, BE.EstadoPedido.Pendiente), P(2, BE.EstadoPedido.Pendiente),
                P(3, BE.EstadoPedido.Despachado), P(4, BE.EstadoPedido.Entregado),
                P(5, BE.EstadoPedido.EnControlStock), P(6, BE.EstadoPedido.Separado),
                P(7, BE.EstadoPedido.Cancelado), null
            });

            Assert.AreEqual(2, t.CantidadPendientes);
            Assert.AreEqual(1, t.CantidadDespachados);
            Assert.AreEqual(1, t.CantidadEntregados);
            Assert.AreEqual(2, t.Pendientes[0].IdPedido, "El más antiguo primero.");
        }

        [TestMethod]
        public void TableroLogistica_DesdeElServicio()
        {
            var s = new FakePedidoService();
            s.Pedidos.AddRange(new[] { P(1, BE.EstadoPedido.Despachado), P(2, BE.EstadoPedido.ConFaltantes) });
            var t = new BLL.PanelTareas(s, () => 0).ObtenerTableroLogistica();
            Assert.AreEqual(1, t.CantidadDespachados);
            Assert.AreEqual(0, t.CantidadPendientes);
        }

        [TestMethod]
        public void SuscripcionesAGestionar_VencidasYProximasA7Dias()
        {
            var clientes = new[]
            {
                new BE.Cliente { IdPlan = 1, FechaVencimiento = DateTime.Today.AddDays(-2) },  // vencida
                new BE.Cliente { IdPlan = 1, FechaVencimiento = DateTime.Today.AddDays(3) },   // por vencer
                new BE.Cliente { IdPlan = 1, FechaVencimiento = DateTime.Today.AddDays(40) },  // al día
                null
            };
            int esperado = clientes.Count(c => c != null && c.RequiereGestionDeVencimiento());
            Assert.AreEqual(esperado, BLL.PanelTareas.ContarSuscripcionesAGestionar(clientes));
            Assert.AreEqual(0, BLL.PanelTareas.ContarSuscripcionesAGestionar(null));
        }

        // ── ListaEspera: permiso propio de escritura ──────────────────────────

        [TestMethod]
        public void ListaEspera_VendedorConSusPatentes_PuedeAnotar()
        {
            SessionManager.Login(Usuario("Vendedor", BE.Patentes.ListaEspera, BE.Patentes.ListaEsperaEditar));
            var dalLe = new FakeListaEsperaDAL();
            var dalPrenda = new FakePrendaDAL { Todas = new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1, Nombre = "Vestido", Estado = BE.EstadoPrenda.EnUso } } };
            var dalCliente = new FakeClienteDAL { ClientePorId = new BE.Cliente { IdCliente = 10, Nombre = "Ana", Apellido = "G", IdPlan = 1, FechaVencimiento = DateTime.Today.AddDays(30) } };

            new BLL.ListaEspera(dalLe, dalPrenda, dalCliente).Anotar("Test", 1, 10, "vendedor");

            Assert.AreEqual(1, dalLe.AltaVeces, "Antes exigía mnuStockEditar y al Vendedor le fallaba.");
        }

        [TestMethod]
        public void ListaEspera_SinPatentesDeListaEspera_Rechaza()
        {
            SessionManager.Login(Usuario("Deposito", BE.Patentes.Stock, BE.Patentes.StockEditar));
            var dalLe = new FakeListaEsperaDAL();
            try
            {
                new BLL.ListaEspera(dalLe, new FakePrendaDAL(), new FakeClienteDAL()).Anotar("Test", 1, 10, "x");
                Assert.Fail("Debía exigir la patente de Lista de Espera.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
            Assert.AreEqual(0, dalLe.AltaVeces);
        }
    }
}
