using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.VersionUsuario.RestaurarVersion (Memento: rollback de datos administrativos de un
    /// usuario). Usuario inyectado con FakeUsuarioDAL y Caretaker en memoria (sin BD).
    /// </summary>
    [TestClass]
    public class VersionUsuarioTests
    {
        private FakeUsuarioDAL _dal;
        private Dictionary<int, BE.VersionUsuario> _versiones;
        private List<(int IdUsuario, BE.VersionUsuario Version)> _guardadas;

        [TestInitialize]
        public void Setup()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
            _dal       = new FakeUsuarioDAL();
            _versiones = new Dictionary<int, BE.VersionUsuario>();
            _guardadas = new List<(int, BE.VersionUsuario)>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
        }

        private BLL.VersionUsuario Crear() => new BLL.VersionUsuario(_dal,
            id => _versiones.TryGetValue(id, out var v) ? v : null,
            (id, m) => _guardadas.Add((id, (BE.VersionUsuario)m)));

        private static void LoginComo(string perfil)
            => SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = perfil, Rol = perfil });

        // Usuario 5 hoy se llama "ana.nueva"; la versión 10 lo devuelve a "ana".
        private void SembrarUsuarioYVersion(string usernameVersion = "ana")
        {
            _dal.Usuarios.Add(new BE.Usuario { Id = 5, Username = "ana.nueva", Nombre = "Ana", Apellido = "Pérez", Email = "nueva@x.com" });
            _versiones[10] = new BE.VersionUsuario
            {
                Id = 10, IdUsuario = 5, UsernameSnapshot = usernameVersion,
                NombreSnapshot = "Ana", ApellidoSnapshot = "Pérez", EmailSnapshot = "vieja@x.com"
            };
        }

        private static BE.AppException Capturar(Action accion)
        {
            try { accion(); return null; }
            catch (BE.AppException ex) { return ex; }
        }

        [TestMethod]
        public void RestaurarVersion_SinSesion_LanzaSesionExpirada()
        {
            SembrarUsuarioYVersion();
            var ex = Capturar(() => Crear().RestaurarVersion("Test", 10));
            Assert.AreEqual("err.bll.sesion_expirada", ex?.Clave);
            Assert.AreEqual(0, _dal.RestaurarVersionVeces);
        }

        [TestMethod]
        public void RestaurarVersion_NoAdministrador_LanzaSinPermiso()
        {
            LoginComo("Vendedor");
            SembrarUsuarioYVersion();
            var ex = Capturar(() => Crear().RestaurarVersion("Test", 10));
            Assert.AreEqual("err.bll.version.sin_permiso", ex?.Clave);
            Assert.AreEqual(0, _dal.RestaurarVersionVeces);
        }

        [TestMethod]
        public void RestaurarVersion_VersionInexistente_LanzaNoExiste()
        {
            LoginComo(BE.Roles.Administrador);
            SembrarUsuarioYVersion();
            var ex = Capturar(() => Crear().RestaurarVersion("Test", 999));
            Assert.AreEqual("err.bll.version.no_existe", ex?.Clave);
        }

        [TestMethod]
        public void RestaurarVersion_UsuarioYaNoExiste_LanzaUsuarioNoExiste()
        {
            LoginComo(BE.Roles.Administrador);
            SembrarUsuarioYVersion();
            _dal.Usuarios.Clear();
            var ex = Capturar(() => Crear().RestaurarVersion("Test", 10));
            Assert.AreEqual("err.bll.usuario.no_existe", ex?.Clave);
            Assert.AreEqual(0, _dal.RestaurarVersionVeces);
        }

        [TestMethod]
        public void RestaurarVersion_UsernameTomadoPorOtroActivo_LanzaUsernameEnUsoSinEscribir()
        {
            LoginComo(BE.Roles.Administrador);
            SembrarUsuarioYVersion();
            _dal.Usuarios.Add(new BE.Usuario { Id = 6, Username = "ANA" });   // sin distinguir mayúsculas

            var ex = Capturar(() => Crear().RestaurarVersion("Test", 10));

            Assert.AreEqual("err.bll.version.username_en_uso", ex?.Clave);
            CollectionAssert.AreEqual(new object[] { "ana" }, ex.Args);
            StringAssert.Contains(ex.Message, "'ana'");
            Assert.AreEqual(0, _dal.RestaurarVersionVeces, "No se llega al UPDATE (que chocaría con UQ_Usuario_Username).");
            Assert.AreEqual(0, _guardadas.Count, "Tampoco se graba un snapshot.");
            Assert.AreEqual("ana.nueva", _dal.ObtenerPorId(5).Username);
        }

        [TestMethod]
        public void RestaurarVersion_UsernameTomadoPorUnArchivado_TambienLoRechaza()
        {
            LoginComo(BE.Roles.Administrador);
            SembrarUsuarioYVersion();
            _dal.Archivados.Add(new BE.Usuario { Id = 7, Username = "ana", Activo = false });

            var ex = Capturar(() => Crear().RestaurarVersion("Test", 10));

            Assert.AreEqual("err.bll.version.username_en_uso", ex?.Clave);
            Assert.AreEqual(0, _dal.RestaurarVersionVeces);
        }

        [TestMethod]
        public void RestaurarVersion_UsernameLibre_RestauraYGrabaSnapshotDelNuevoEstado()
        {
            LoginComo(BE.Roles.Administrador);
            SembrarUsuarioYVersion();

            Crear().RestaurarVersion("Test", 10);

            Assert.AreEqual(1, _dal.RestaurarVersionVeces);
            var u = _dal.ObtenerPorId(5);
            Assert.AreEqual("ana", u.Username);
            Assert.AreEqual("vieja@x.com", u.Email);
            Assert.AreEqual(1, _guardadas.Count);
            Assert.AreEqual(5, _guardadas[0].IdUsuario);
            Assert.AreEqual("ana", _guardadas[0].Version.UsernameSnapshot, "El snapshot refleja el estado restaurado.");
            StringAssert.Contains(_guardadas[0].Version.Detalle, "'ana.nueva' → 'ana'");
        }

        [TestMethod]
        public void RestaurarVersion_MismoUsernameQueElPropio_NoCuentaComoEnUso()
        {
            LoginComo(BE.Roles.Administrador);
            SembrarUsuarioYVersion(usernameVersion: "ana.nueva");   // la versión solo cambia el email

            Crear().RestaurarVersion("Test", 10);

            Assert.AreEqual(1, _dal.RestaurarVersionVeces);
            Assert.AreEqual("vieja@x.com", _dal.ObtenerPorId(5).Email);
        }
    }
}
