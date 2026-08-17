using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.CargoPrenda — Bloque 1: cargo por daño/pérdida de prenda, registrado sobre el
    /// último cliente que la tuvo (BE.Prenda.IdUltimoCliente) y liquidado junto con su
    /// próxima renovación (ver ProcesarPago_ConCargosPendientes... en CobroTests).
    /// </summary>
    [TestClass]
    public class CargoPrendaTests
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

        private static BE.Prenda PrendaConUltimoCliente() => new BE.Prenda
        {
            IdPrenda = 5,
            Nombre = "Campera de cuero",
            IdUltimoCliente = 2,
            NombreUltimoCliente = "Luis Pérez"
        };

        [TestMethod]
        public void RegistrarCargo_DatosValidos_InsertaPendienteContraElUltimoCliente()
        {
            LoginComoAdministrador();
            var fake = new FakeCargoPrendaDAL();
            var bll = new BLL.CargoPrenda(fake);
            var prenda = PrendaConUltimoCliente();

            bll.RegistrarCargo("Test", prenda, "Rotura en el cierre", 500m, "operador1");

            Assert.AreEqual(1, fake.Registros.Count);
            var cargo = fake.Registros[0];
            Assert.AreEqual(2, cargo.IdCliente);
            Assert.AreEqual(5, cargo.IdPrenda);
            Assert.AreEqual(500m, cargo.Monto);
            Assert.AreEqual(BE.EstadoCargo.Pendiente, cargo.Estado);
        }

        [TestMethod]
        public void RegistrarCargo_PrendaSinUltimoCliente_LanzaSinCliente_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var fake = new FakeCargoPrendaDAL();
            var bll = new BLL.CargoPrenda(fake);
            var prenda = PrendaConUltimoCliente();
            prenda.IdUltimoCliente = null;

            try
            {
                bll.RegistrarCargo("Test", prenda, "Rotura", 500m);
                Assert.Fail("Debía rechazar una prenda sin último cliente conocido.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cargoprenda.sin_cliente", ex.Clave);
            }
            Assert.AreEqual(0, fake.Registros.Count);
        }

        [TestMethod]
        public void RegistrarCargo_MotivoVacio_LanzaMotivoRequerido()
        {
            LoginComoAdministrador();
            var bll = new BLL.CargoPrenda(new FakeCargoPrendaDAL());
            var prenda = PrendaConUltimoCliente();

            try
            {
                bll.RegistrarCargo("Test", prenda, "   ", 500m);
                Assert.Fail("Debía exigir un motivo.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cargoprenda.motivo_requerido", ex.Clave);
            }
        }

        [TestMethod]
        public void RegistrarCargo_MontoCero_LanzaMontoInvalido()
        {
            LoginComoAdministrador();
            var bll = new BLL.CargoPrenda(new FakeCargoPrendaDAL());
            var prenda = PrendaConUltimoCliente();

            try
            {
                bll.RegistrarCargo("Test", prenda, "Rotura", 0m);
                Assert.Fail("Debía rechazar un monto de cero.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cargoprenda.monto_invalido", ex.Clave);
            }
        }

        [TestMethod]
        public void RegistrarCargo_SinSesion_LanzaSesionExpirada()
        {
            var bll = new BLL.CargoPrenda(new FakeCargoPrendaDAL());
            var prenda = PrendaConUltimoCliente();

            try
            {
                bll.RegistrarCargo("Test", prenda, "Rotura", 500m);
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }

        [TestMethod]
        public void ObtenerPendientesPorCliente_DelegaEnElDAL()
        {
            var fake = new FakeCargoPrendaDAL();
            fake.Alta(new BE.CargoPrenda { IdPrenda = 1, IdCliente = 2, Motivo = "Rotura", Monto = 500m });
            var bll = new BLL.CargoPrenda(fake);

            var resultado = bll.ObtenerPendientesPorCliente(2);

            Assert.AreEqual(1, resultado.Count);
        }
    }
}
