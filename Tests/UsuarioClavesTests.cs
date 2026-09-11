using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Usuario.Claves (partial) — ResetearClave, SolicitarRecuperacionClave y
    /// ValidarCredencialesAdmin. CambiarClavePropia ya está cubierto en detalle por
    /// UsuarioCambioClaveTests.cs.
    ///
    /// ResetearClave: solo se cubre el guard ValidarEsAdministrador() (antes de cualquier
    /// side-effect). El resto del método no es testeable de forma aislada sin tocar BD/disco:
    /// llama a Configuracion.AsegurarIntegridadUsuarios() (estática, contra la BD real) y a
    /// "new VersionUsuario()"/GeneradorCredenciales.ExportarCredenciales (BLL concreto y
    /// escritura de archivo real, ninguno inyectable desde acá) — mismo problema estructural
    /// que ExigirSistemaConservaGestion en BLL.Familia.
    ///
    /// SolicitarRecuperacionClave: se cubre el camino "no existe" (no toca bitácora). El
    /// camino "sí existe" escribe en Servicios.Bitacora (concreta, no inyectada) contra la
    /// BD real y queda fuera del mismo modo.
    ///
    /// ValidarCredencialesAdmin: sin ninguna dependencia de BD más allá del IUsuarioDAL
    /// inyectado — cobertura completa de las 6 ramas.
    /// </summary>
    [TestClass]
    public class UsuarioClavesTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static void LoginComoAdministrador()
            => SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador" });

        private static void LoginComoVendedor()
            => SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor" });

        // ── ResetearClave — guard previo a cualquier side-effect ────────────────────────

        [TestMethod]
        public void ResetearClave_SinSesion_LanzaSesionExpirada()
        {
            var bll = new BLL.Usuario(new FakeUsuarioDAL());

            try
            {
                bll.ResetearClave("Test", 5, "objetivo");
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }

        [TestMethod]
        public void ResetearClave_NoAdministrador_LanzaSinPermiso()
        {
            LoginComoVendedor();
            var bll = new BLL.Usuario(new FakeUsuarioDAL());

            try
            {
                bll.ResetearClave("Test", 5, "objetivo");
                Assert.Fail("Debía exigir ser Administrador.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.usuario.sin_permiso", ex.Clave);
            }
        }

        // ── SolicitarRecuperacionClave — camino "no existe" (no toca bitácora) ──────────

        [TestMethod]
        public void SolicitarRecuperacionClave_UsernameVacio_DevuelveFalse()
        {
            var bll = new BLL.Usuario(new FakeUsuarioDAL());
            Assert.IsFalse(bll.SolicitarRecuperacionClave("   "));
        }

        [TestMethod]
        public void SolicitarRecuperacionClave_UsernameNull_DevuelveFalse()
        {
            var bll = new BLL.Usuario(new FakeUsuarioDAL());
            Assert.IsFalse(bll.SolicitarRecuperacionClave(null));
        }

        [TestMethod]
        public void SolicitarRecuperacionClave_UsuarioInexistente_DevuelveFalse()
        {
            var fake = new FakeUsuarioDAL(); // sin usuarios sembrados
            var bll = new BLL.Usuario(fake);

            Assert.IsFalse(bll.SolicitarRecuperacionClave("no_existe"));
        }

        // ── ValidarCredencialesAdmin — sin dependencia de BD, cobertura completa ────────

        private static FakeUsuarioDAL ConUsuarioAdmin(string username, string passwordPlano, bool bloqueado = false)
        {
            var fake = new FakeUsuarioDAL();
            fake.Usuarios.Add(new BE.Usuario
            {
                Id = 1, Username = username, Perfil = "Administrador",
                Contraseña = Encriptador.Hash(passwordPlano), Bloqueado = bloqueado
            });
            return fake;
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_UsernameVacio_DevuelveFalse()
        {
            var bll = new BLL.Usuario(new FakeUsuarioDAL());
            Assert.IsFalse(bll.ValidarCredencialesAdmin("  ", "Clave1!"));
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_PasswordVacio_DevuelveFalse()
        {
            var bll = new BLL.Usuario(new FakeUsuarioDAL());
            Assert.IsFalse(bll.ValidarCredencialesAdmin("admin", "  "));
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_UsuarioInexistente_DevuelveFalse()
        {
            var bll = new BLL.Usuario(new FakeUsuarioDAL());
            Assert.IsFalse(bll.ValidarCredencialesAdmin("no_existe", "Clave1!"));
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_UsuarioBloqueado_DevuelveFalse()
        {
            var fake = ConUsuarioAdmin("admin", "Clave1!", bloqueado: true);
            var bll = new BLL.Usuario(fake);

            Assert.IsFalse(bll.ValidarCredencialesAdmin("admin", "Clave1!"));
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_PasswordIncorrecta_DevuelveFalse()
        {
            var fake = ConUsuarioAdmin("admin", "Clave1!");
            var bll = new BLL.Usuario(fake);

            Assert.IsFalse(bll.ValidarCredencialesAdmin("admin", "OtraClave1!"));
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_NoEsAdministrador_DevuelveFalse()
        {
            var fake = new FakeUsuarioDAL();
            fake.Usuarios.Add(new BE.Usuario
            {
                Id = 1, Username = "vend", Perfil = "Vendedor",
                Contraseña = Encriptador.Hash("Clave1!")
            });
            var bll = new BLL.Usuario(fake);

            Assert.IsFalse(bll.ValidarCredencialesAdmin("vend", "Clave1!"));
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_CredencialesValidasYEsAdministrador_DevuelveTrue()
        {
            var fake = ConUsuarioAdmin("admin", "Clave1!");
            var bll = new BLL.Usuario(fake);

            Assert.IsTrue(bll.ValidarCredencialesAdmin("admin", "Clave1!"));
        }
    }
}
