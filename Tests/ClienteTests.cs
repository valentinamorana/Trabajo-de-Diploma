using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Cliente — gestión de clientes (PdN1). Cubre validación de alta/modificación,
    /// detección de DNI duplicado, bloqueo de baja con prendas en uso y el DTO de estado
    /// comercial que usa NuevoPedidoForm para decidir si un cliente puede pedir.
    ///
    /// NO cubre ActivarSuscripcion() ni la rama de "cambio de plan" de Modificar(): ambas
    /// llaman a DAL.PlanSuscripcion.ObtenerPorId directo (campo concreto, no inyectado —
    /// mismo patrón de acoplamiento que BLL.Pedido, ver nota en el README de Tests) y no
    /// se pueden ejercitar sin una conexión real a la base.
    /// </summary>
    [TestClass]
    public class ClienteTests
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

        private static BE.Cliente ClienteValido() => new BE.Cliente
        {
            IdCliente = 10,
            Nombre = "Ana",
            Apellido = "Gómez",
            DNI = "30111222",
            IdPlan = 1,
            NombrePlan = "Básico",
            LimitePrendas = 3,
            FechaNacimiento = DateTime.Today.AddYears(-25)
        };

        // ── ObtenerTodos / ObtenerPorId ──────────────────────────────────────────

        [TestMethod]
        public void ObtenerTodos_DelegaEnElDAL()
        {
            var fake = new FakeClienteDAL { ClientesDevueltos = { ClienteValido() } };
            var bll = new BLL.Cliente(fake);

            var resultado = bll.ObtenerTodos();

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("Ana", resultado[0].Nombre);
        }

        [TestMethod]
        public void ObtenerPorId_DelegaEnElDAL()
        {
            var fake = new FakeClienteDAL { ClientePorId = ClienteValido() };
            var bll = new BLL.Cliente(fake);

            var resultado = bll.ObtenerPorId(10);

            Assert.IsNotNull(resultado);
            Assert.AreEqual(10, resultado.IdCliente);
        }

        // ── Alta — validaciones ───────────────────────────────────────────────

        [TestMethod]
        public void Alta_NombreVacio_LanzaNombreRequerido()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.Nombre = "  ";

            try
            {
                bll.Alta("Test", cliente);
                Assert.Fail("Debía rechazar un nombre vacío.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.nombre_requerido", ex.Clave);
            }
            Assert.AreEqual(0, fake.AltaVeces, "No debe tocar el DAL si la validación falla.");
        }

        [TestMethod]
        public void Alta_DniConLetras_LanzaDniSoloNumeros()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.DNI = "3011122A";

            try
            {
                bll.Alta("Test", cliente);
                Assert.Fail("Debía rechazar un DNI con letras.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.dni_numeros", ex.Clave);
            }
        }

        [TestMethod]
        public void Alta_DniCorto_LanzaDniFormato()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.DNI = "123456"; // 6 dígitos, mínimo es 7

            try
            {
                bll.Alta("Test", cliente);
                Assert.Fail("Debía rechazar un DNI de menos de 7 dígitos.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.dni_formato", ex.Clave);
            }
        }

        [TestMethod]
        public void Alta_SinPlan_LanzaPlanRequerido()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.IdPlan = null;

            try
            {
                bll.Alta("Test", cliente);
                Assert.Fail("Debía exigir un plan.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.plan_requerido", ex.Clave);
            }
        }

        [TestMethod]
        public void Alta_MenorDeEdad_LanzaMenorDeEdad()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.FechaNacimiento = DateTime.Today.AddYears(-17);

            try
            {
                bll.Alta("Test", cliente);
                Assert.Fail("Debía rechazar a un menor de 18.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.menor_edad", ex.Clave);
            }
        }

        [TestMethod]
        public void Alta_DniYaExiste_LanzaDniDuplicado_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL { ExisteDNIRespuesta = true };
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();

            try
            {
                bll.Alta("Test", cliente);
                Assert.Fail("Debía rechazar un DNI duplicado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.dni_duplicado", ex.Clave);
            }
            Assert.AreEqual(0, fake.AltaVeces, "No debe insertar si el DNI ya existe.");
        }

        [TestMethod]
        public void Alta_DatosValidos_InsertaYAsignaId()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL { AltaIdGenerado = 42 };
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.IdCliente = 0;

            bll.Alta("Test", cliente);

            Assert.AreEqual(1, fake.AltaVeces);
            Assert.AreEqual(42, cliente.IdCliente, "Debe reflejar el ID generado por el DAL.");
            Assert.AreEqual(DateTime.Today, cliente.FechaAlta.Date);
        }

        [TestMethod]
        public void Alta_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout — no hay sesión activa.
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);

            try
            {
                bll.Alta("Test", ClienteValido());
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }

        // ── Baja ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void Baja_ConPrendasEnUso_LanzaBajaPrendas_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.StockUtilizado = 2;

            try
            {
                bll.Baja("Test", cliente);
                Assert.Fail("Debía bloquear la baja con prendas en uso.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.baja_prendas", ex.Clave);
            }
            Assert.AreEqual(0, fake.BajaVeces);
        }

        [TestMethod]
        public void Baja_SinPrendasEnUso_DaDeBaja()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.StockUtilizado = 0;

            bll.Baja("Test", cliente);

            Assert.AreEqual(1, fake.BajaVeces);
            Assert.AreEqual(cliente.IdCliente, fake.UltimoIdBaja);
        }

        // ── Modificar (sin cambio de plan — no toca DAL.PlanSuscripcion) ────────

        [TestMethod]
        public void Modificar_DniDuplicadoParaOtroCliente_LanzaDniDuplicadoOtro()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL { ExisteDNIParaOtroRespuesta = true };
            var bll = new BLL.Cliente(fake);

            try
            {
                bll.Modificar("Test", ClienteValido());
                Assert.Fail("Debía rechazar un DNI que ya usa otro cliente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.dni_duplicado_otro", ex.Clave);
            }
            Assert.AreEqual(0, fake.ModificarVeces);
        }

        [TestMethod]
        public void Modificar_MismoPlan_ActualizaSinConsultarPlanes()
        {
            LoginComoAdministrador();
            var cliente = ClienteValido();
            var fake = new FakeClienteDAL { ClientePorId = cliente }; // "actual" == mismo IdPlan
            var bll = new BLL.Cliente(fake);

            // No debe lanzar (si intentara consultar DAL.PlanSuscripcion sin BD, explotaría).
            bll.Modificar("Test", cliente);

            Assert.AreEqual(1, fake.ModificarVeces);
            Assert.AreSame(cliente, fake.UltimoModificado);
        }

        // ── ReanudarPausa (Bloque 1) ─────────────────────────────────────────────

        [TestMethod]
        public void ReanudarPausa_ClientePausado_LimpiaFechaPausaHastaSinTocarVencimiento()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.FechaPausaHasta = DateTime.Today.AddDays(5);
            cliente.FechaVencimiento = DateTime.Today.AddDays(20);

            bll.ReanudarPausa("Test", cliente);

            Assert.IsNull(cliente.FechaPausaHasta);
            Assert.AreEqual(DateTime.Today.AddDays(20), cliente.FechaVencimiento, "No debe tocar el vencimiento.");
            Assert.AreEqual(1, fake.ModificarVeces);
        }

        [TestMethod]
        public void ReanudarPausa_ClienteNoPausado_LanzaNoPausada_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var fake = new FakeClienteDAL();
            var bll = new BLL.Cliente(fake);
            var cliente = ClienteValido();
            cliente.FechaPausaHasta = null;

            try
            {
                bll.ReanudarPausa("Test", cliente);
                Assert.Fail("Debía rechazar reanudar un cliente que no está pausado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cliente.no_pausada", ex.Clave);
            }
            Assert.AreEqual(0, fake.ModificarVeces);
        }

        // ── ObtenerEstadoComercial — DTO puro, sin DAL ──────────────────────────

        [TestMethod]
        public void EstadoComercial_SinPlan_NoPuedeProceder()
        {
            var bll = new BLL.Cliente(new FakeClienteDAL());
            var cliente = ClienteValido();
            cliente.IdPlan = null;

            var estado = bll.ObtenerEstadoComercial(cliente, 1);

            Assert.IsFalse(estado.PuedeProceder);
            Assert.AreEqual("SIN_PLAN", estado.MotivoBloqueo);
        }

        [TestMethod]
        public void EstadoComercial_SuscripcionVencida_NoPuedeProceder()
        {
            var bll = new BLL.Cliente(new FakeClienteDAL());
            var cliente = ClienteValido();
            cliente.FechaVencimiento = DateTime.Today.AddDays(-1);

            var estado = bll.ObtenerEstadoComercial(cliente, 1);

            Assert.IsFalse(estado.PuedeProceder);
            Assert.AreEqual("SUSCRIPCION_VENCIDA", estado.MotivoBloqueo);
        }

        [TestMethod]
        public void EstadoComercial_SuperaLimiteDelPlan_InformaExceso()
        {
            var bll = new BLL.Cliente(new FakeClienteDAL());
            var cliente = ClienteValido();
            cliente.LimitePrendas = 3;
            cliente.StockUtilizado = 2;

            var estado = bll.ObtenerEstadoComercial(cliente, 2); // 2 + 2 = 4 > 3

            Assert.IsTrue(estado.PuedeProceder, "Puede proceder informativamente, pero marcado con exceso.");
            Assert.IsTrue(estado.SuperaLimite);
            Assert.AreEqual(1, estado.Exceso);
        }

        [TestMethod]
        public void EstadoComercial_DentroDelLimite_SinExceso()
        {
            var bll = new BLL.Cliente(new FakeClienteDAL());
            var cliente = ClienteValido();
            cliente.LimitePrendas = 3;
            cliente.StockUtilizado = 1;

            var estado = bll.ObtenerEstadoComercial(cliente, 1); // 1 + 1 = 2 <= 3

            Assert.IsTrue(estado.PuedeProceder);
            Assert.IsFalse(estado.SuperaLimite);
            Assert.AreEqual(0, estado.Exceso);
            Assert.AreEqual(2, estado.PrendasDisponibles); // 3 - 1
        }
    }
}
