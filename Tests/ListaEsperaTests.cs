using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Lista de Espera de prendas (mejora opcional, no requerida por la cátedra — ver
    /// README). Mismo criterio de DI/Fakes que CobroTests/PedidoTests.
    /// </summary>
    [TestClass]
    public class ListaEsperaTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        // Assert.ThrowsException no está disponible en esta versión de MSTest.
        private static void AssertThrows(Action accion)
        {
            try { accion(); }
            catch (BE.AppException) { return; }
            Assert.Fail("Se esperaba una BE.AppException.");
        }

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

        private static BE.Prenda PrendaEnUso(int id = 1) => new BE.Prenda
        {
            IdPrenda = id, Nombre = "Vestido Azul", Estado = BE.EstadoPrenda.EnUso
        };

        private static BE.Cliente ClienteVigente(int id = 10) => new BE.Cliente
        {
            IdCliente = id, Nombre = "Ana", Apellido = "Gómez",
            IdPlan = 1, FechaVencimiento = DateTime.Today.AddDays(30)
        };

        private class Contexto
        {
            public FakeListaEsperaDAL DalListaEspera = new FakeListaEsperaDAL();
            public FakePrendaDAL DalPrenda = new FakePrendaDAL();
            public FakeClienteDAL DalCliente = new FakeClienteDAL();

            public BLL.ListaEspera Crear() => new BLL.ListaEspera(DalListaEspera, DalPrenda, DalCliente);
        }

        // ── Anotar ────────────────────────────────────────────────────────────

        [TestMethod]
        public void Anotar_PrendaEnUsoYClienteVigente_Persiste()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { PrendaEnUso() };
            ctx.DalCliente.ClientePorId = ClienteVigente();
            var bll = ctx.Crear();

            bll.Anotar("Test", idPrenda: 1, idCliente: 10, actor: "admin");

            Assert.AreEqual(1, ctx.DalListaEspera.AltaVeces);
            Assert.AreEqual(BE.EstadoListaEspera.Pendiente, ctx.DalListaEspera.Registros[0].Estado);
        }

        [TestMethod]
        public void Anotar_PrendaDisponible_RechazaConAppException()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1, Nombre = "X", Estado = BE.EstadoPrenda.Disponible } };
            ctx.DalCliente.ClientePorId = ClienteVigente();
            var bll = ctx.Crear();

            AssertThrows(() => bll.Anotar("Test", 1, 10, "admin"));
            Assert.AreEqual(0, ctx.DalListaEspera.AltaVeces);
        }

        [TestMethod]
        public void Anotar_ClienteSinSuscripcionVigente_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { PrendaEnUso() };
            ctx.DalCliente.ClientePorId = new BE.Cliente { IdCliente = 10, IdPlan = 1, FechaVencimiento = DateTime.Today.AddDays(-5) };
            var bll = ctx.Crear();

            AssertThrows(() => bll.Anotar("Test", 1, 10, "admin"));
        }

        [TestMethod]
        public void Anotar_MismoClienteDosVeces_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            ctx.DalPrenda.Todas = new List<BE.Prenda> { PrendaEnUso() };
            ctx.DalCliente.ClientePorId = ClienteVigente();
            var bll = ctx.Crear();

            bll.Anotar("Test", 1, 10, "admin");
            AssertThrows(() => bll.Anotar("Test", 1, 10, "admin"));
            Assert.AreEqual(1, ctx.DalListaEspera.AltaVeces);
        }

        // ── NotificarSiCorresponde (liberación de la prenda) ────────────────────

        [TestMethod]
        public void NotificarSiCorresponde_HayPendiente_ReservaElMasAntiguoFIFO()
        {
            var ctx = new Contexto();
            var masViejo = new BE.ListaEspera { IdListaEspera = 1, IdPrenda = 1, IdCliente = 10, Estado = BE.EstadoListaEspera.Pendiente, FechaAlta = DateTime.Now.AddHours(-2) };
            var masNuevo = new BE.ListaEspera { IdListaEspera = 2, IdPrenda = 1, IdCliente = 20, Estado = BE.EstadoListaEspera.Pendiente, FechaAlta = DateTime.Now.AddHours(-1) };
            ctx.DalListaEspera.Registros.AddRange(new[] { masNuevo, masViejo });
            var bll = ctx.Crear();

            bll.NotificarSiCorresponde(1, "sistema");

            Assert.AreEqual(BE.EstadoListaEspera.Reservada, masViejo.Estado);
            Assert.AreEqual(BE.EstadoListaEspera.Pendiente, masNuevo.Estado);
            Assert.IsTrue(masViejo.FechaLimiteReserva.Value > DateTime.Now);
        }

        [TestMethod]
        public void NotificarSiCorresponde_NadieEspera_NoHaceNada()
        {
            var ctx = new Contexto();
            var bll = ctx.Crear();

            bll.NotificarSiCorresponde(1, "sistema");

            Assert.AreEqual(0, ctx.DalListaEspera.CambiarEstadoVeces);
        }

        // ── EstaReservadaParaOtro / CerrarSiReservada (integración con BLL.Pedido) ──

        [TestMethod]
        public void EstaReservadaParaOtro_ReservaVigenteDeOtroCliente_DevuelveTrue()
        {
            var ctx = new Contexto();
            ctx.DalListaEspera.Registros.Add(new BE.ListaEspera
            {
                IdListaEspera = 1, IdPrenda = 1, IdCliente = 20,
                Estado = BE.EstadoListaEspera.Reservada, FechaLimiteReserva = DateTime.Now.AddHours(10)
            });
            var bll = ctx.Crear();

            Assert.IsTrue(bll.EstaReservadaParaOtro(1, idClienteSolicitante: 99));
            Assert.IsFalse(bll.EstaReservadaParaOtro(1, idClienteSolicitante: 20)); // el propio cliente sí puede
        }

        [TestMethod]
        public void CerrarSiReservada_ReservaDelCliente_PasaAConvertida()
        {
            var ctx = new Contexto();
            var fila = new BE.ListaEspera
            {
                IdListaEspera = 1, IdPrenda = 1, IdCliente = 10,
                Estado = BE.EstadoListaEspera.Reservada, FechaLimiteReserva = DateTime.Now.AddHours(10)
            };
            ctx.DalListaEspera.Registros.Add(fila);
            var bll = ctx.Crear();

            bll.CerrarSiReservada("Test", 1, 10, "admin");

            Assert.AreEqual(BE.EstadoListaEspera.Convertida, fila.Estado);
        }

        [TestMethod]
        public void CerrarSiReservada_ReservaExpirada_NoLaCierra()
        {
            var ctx = new Contexto();
            var fila = new BE.ListaEspera
            {
                IdListaEspera = 1, IdPrenda = 1, IdCliente = 10,
                Estado = BE.EstadoListaEspera.Reservada, FechaLimiteReserva = DateTime.Now.AddHours(-1)
            };
            ctx.DalListaEspera.Registros.Add(fila);
            var bll = ctx.Crear();

            bll.CerrarSiReservada("Test", 1, 10, "admin");

            Assert.AreEqual(BE.EstadoListaEspera.Reservada, fila.Estado); // sigue igual, no se tocó
        }

        // ── Cancelar ──────────────────────────────────────────────────────────

        [TestMethod]
        public void Cancelar_FilaPendiente_PasaACancelada()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var fila = new BE.ListaEspera { IdListaEspera = 1, IdPrenda = 1, IdCliente = 10, Estado = BE.EstadoListaEspera.Pendiente };
            ctx.DalListaEspera.Registros.Add(fila);
            var bll = ctx.Crear();

            bll.Cancelar("Test", 1, "admin");

            Assert.AreEqual(BE.EstadoListaEspera.Cancelada, fila.Estado);
        }

        [TestMethod]
        public void Cancelar_FilaYaConvertida_Rechaza()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var fila = new BE.ListaEspera { IdListaEspera = 1, IdPrenda = 1, IdCliente = 10, Estado = BE.EstadoListaEspera.Convertida };
            ctx.DalListaEspera.Registros.Add(fila);
            var bll = ctx.Crear();

            AssertThrows(() => bll.Cancelar("Test", 1, "admin"));
        }
    }
}
