using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.RecuperacionAdmin — ruta de recuperación de cuentas de Administrador bloqueadas
    /// mediante claves de emergencia de un solo uso. Estaba diseñada explícitamente para
    /// testearse por interfaces (DIP, ver su propio doc-comment) pero no tenía ningún test ni
    /// el doble de prueba necesario (Tests/Fakes/FakeClaveRecuperacionDAL, agregado en esta
    /// sesión). No requiere sesión iniciada — es el flujo de emergencia para cuando nadie puede
    /// loguearse.
    /// </summary>
    [TestClass]
    public class RecuperacionAdminTests
    {
        private const string ClaveEnClaro = "ABC123XY";

        private static BE.Usuario AdminBloqueado() => new BE.Usuario
        {
            Id = 1,
            Username = "admin2",
            Perfil = "Administrador",
            Bloqueado = true
        };

        private static FakeClaveRecuperacionDAL ClaveDalConUnaDisponible()
            => new FakeClaveRecuperacionDAL
            {
                Disponibles = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, string>>
                {
                    new System.Collections.Generic.KeyValuePair<int, string>(1, Encriptador.Hash(ClaveEnClaro))
                }
            };

        // ── DesbloquearConClave ──────────────────────────────────────────────────

        [TestMethod]
        public void DesbloquearConClave_UsernameVacio_LanzaCampos()
        {
            var bll = new BLL.RecuperacionAdmin(new FakeUsuarioDAL(), new FakeClaveRecuperacionDAL());

            try
            {
                bll.DesbloquearConClave("Test", "  ", ClaveEnClaro);
                Assert.Fail("Debía exigir un username.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.emergencia.campos", ex.Clave);
            }
        }

        [TestMethod]
        public void DesbloquearConClave_ClaveVacia_LanzaCampos()
        {
            var bll = new BLL.RecuperacionAdmin(new FakeUsuarioDAL(), new FakeClaveRecuperacionDAL());

            try
            {
                bll.DesbloquearConClave("Test", "admin2", "   ");
                Assert.Fail("Debía exigir una clave de emergencia.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.emergencia.campos", ex.Clave);
            }
        }

        [TestMethod]
        public void DesbloquearConClave_UsuarioInexistente_LanzaInvalida()
        {
            var dalUsuario = new FakeUsuarioDAL(); // sin sembrar: ObtenerPorUsername devuelve null
            var bll = new BLL.RecuperacionAdmin(dalUsuario, ClaveDalConUnaDisponible());

            try
            {
                bll.DesbloquearConClave("Test", "noexiste", ClaveEnClaro);
                Assert.Fail("Debía rechazar un usuario inexistente.");
            }
            catch (BE.AppException ex)
            {
                // Mismo mensaje que "clave inválida" a propósito: no revela si el usuario existe.
                Assert.AreEqual("err.bll.emergencia.invalida", ex.Clave);
            }
        }

        [TestMethod]
        public void DesbloquearConClave_UsuarioNoAdministrador_LanzaSoloAdmin()
        {
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(new BE.Usuario { Id = 2, Username = "vendedor1", Perfil = "Vendedor", Bloqueado = true });
            var bll = new BLL.RecuperacionAdmin(dalUsuario, ClaveDalConUnaDisponible());

            try
            {
                bll.DesbloquearConClave("Test", "vendedor1", ClaveEnClaro);
                Assert.Fail("Debía rechazar un usuario no-Administrador.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.emergencia.solo_admin", ex.Clave);
            }
        }

        [TestMethod]
        public void DesbloquearConClave_AdminNoBloqueado_LanzaNoBloqueada()
        {
            var dalUsuario = new FakeUsuarioDAL();
            var admin = AdminBloqueado();
            admin.Bloqueado = false;
            dalUsuario.Usuarios.Add(admin);
            var bll = new BLL.RecuperacionAdmin(dalUsuario, ClaveDalConUnaDisponible());

            try
            {
                bll.DesbloquearConClave("Test", "admin2", ClaveEnClaro);
                Assert.Fail("Debía rechazar un admin que no está bloqueado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.emergencia.no_bloqueada", ex.Clave);
            }
        }

        [TestMethod]
        public void DesbloquearConClave_SinClavesDisponibles_LanzaSinClaves()
        {
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(AdminBloqueado());
            var bll = new BLL.RecuperacionAdmin(dalUsuario, new FakeClaveRecuperacionDAL()); // sin sembrar

            try
            {
                bll.DesbloquearConClave("Test", "admin2", ClaveEnClaro);
                Assert.Fail("Debía avisar que no quedan claves.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.emergencia.sin_claves", ex.Clave);
            }
        }

        [TestMethod]
        public void DesbloquearConClave_ClaveIncorrecta_LanzaInvalida()
        {
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(AdminBloqueado());
            var dalClave = ClaveDalConUnaDisponible();
            var bll = new BLL.RecuperacionAdmin(dalUsuario, dalClave);

            try
            {
                bll.DesbloquearConClave("Test", "admin2", "CLAVE-QUE-NO-ES");
                Assert.Fail("Debía rechazar una clave que no coincide con ningún hash disponible.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.emergencia.invalida", ex.Clave);
            }
            Assert.AreEqual(0, dalUsuario.DesbloquearVeces);
            Assert.AreEqual(1, dalClave.ContarDisponibles(), "Una clave incorrecta no debe consumir ninguna clave real.");
        }

        [TestMethod]
        public void DesbloquearConClave_ClaveValida_DesbloqueaYConsumeLaClave()
        {
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(AdminBloqueado());
            var dalClave = ClaveDalConUnaDisponible();
            var bll = new BLL.RecuperacionAdmin(dalUsuario, dalClave);

            bool ok = bll.DesbloquearConClave("Test", "admin2", ClaveEnClaro);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, dalUsuario.DesbloquearVeces);
            Assert.AreEqual(1, dalUsuario.UltimoIdDesbloqueado);
            Assert.AreEqual(1, dalClave.MarcarUsadaVeces, "La clave usada debe consumirse (uso único).");
            Assert.AreEqual(0, dalClave.ContarDisponibles());
        }

        [TestMethod]
        public void DesbloquearConClave_ClaveValidaEnMinusculas_FuncionaIgual()
        {
            // Las claves de emergencia se generan/comparan en mayúsculas — verificar que el
            // método las normaliza (ToUpperInvariant) antes de compararlas contra el hash.
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(AdminBloqueado());
            var dalClave = ClaveDalConUnaDisponible();
            var bll = new BLL.RecuperacionAdmin(dalUsuario, dalClave);

            bool ok = bll.DesbloquearConClave("Test", "admin2", ClaveEnClaro.ToLowerInvariant());

            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void DesbloquearConClave_ClaveYaConsumidaPorOtraEjecucion_LanzaInvalida()
        {
            // MarcarUsada devuelve false si otra ejecución ya la consumió en paralelo (carrera) —
            // el método debe tratarlo como clave inválida en vez de reportar éxito.
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(AdminBloqueado());
            var dalClave = ClaveDalConUnaDisponible();
            dalClave.SimularConsumidaPorOtro = true;
            var bll = new BLL.RecuperacionAdmin(dalUsuario, dalClave);

            try
            {
                bll.DesbloquearConClave("Test", "admin2", ClaveEnClaro);
                Assert.Fail("Debía rechazar una clave ya consumida por otra ejecución.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.emergencia.invalida", ex.Clave);
            }
            Assert.AreEqual(0, dalUsuario.DesbloquearVeces);
        }

        // ── ValidarClaveMaestra ──────────────────────────────────────────────────
        // Tests/App.config no define MasterRecoveryKeyHash (a diferencia de GUI/App.config) —
        // por eso solo se puede ejercitar acá el camino "clave maestra desactivada/sin
        // configurar", no el de coincidencia exitosa contra un hash real.

        [TestMethod]
        public void ValidarClaveMaestra_ClaveVacia_DevuelveFalse()
        {
            var bll = new BLL.RecuperacionAdmin(new FakeUsuarioDAL(), new FakeClaveRecuperacionDAL());
            Assert.IsFalse(bll.ValidarClaveMaestra(""));
        }

        [TestMethod]
        public void ValidarClaveMaestra_ClaveNull_DevuelveFalse()
        {
            var bll = new BLL.RecuperacionAdmin(new FakeUsuarioDAL(), new FakeClaveRecuperacionDAL());
            Assert.IsFalse(bll.ValidarClaveMaestra(null));
        }

        [TestMethod]
        public void ValidarClaveMaestra_SinHashConfigurado_DevuelveFalse()
        {
            var bll = new BLL.RecuperacionAdmin(new FakeUsuarioDAL(), new FakeClaveRecuperacionDAL());
            Assert.IsFalse(bll.ValidarClaveMaestra("cualquier-clave"));
        }

        // ── ContarClavesDisponibles ──────────────────────────────────────────────

        [TestMethod]
        public void ContarClavesDisponibles_DelegaEnElDAL()
        {
            var dalClave = ClaveDalConUnaDisponible();
            var bll = new BLL.RecuperacionAdmin(new FakeUsuarioDAL(), dalClave);

            Assert.AreEqual(1, bll.ContarClavesDisponibles());
        }
    }
}
