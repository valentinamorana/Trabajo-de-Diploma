using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Login y recuperación de acceso — hallazgos de la auditoría de seguridad:
    ///   • el estado "bloqueada" no se revela sin la contraseña correcta (anti-enumeración);
    ///   • el contador de intentos usa el valor devuelto por el UPDATE (no una lectura vieja);
    ///   • la bitácora no guarda lo tipeado como usuario inexistente;
    ///   • con la integridad comprometida se autentica contra el espejo (no contra Usuario);
    ///   • la confirmación de Administrador y las claves de emergencia cuentan intentos.
    /// </summary>
    [TestClass]
    public class LoginSeguridadTests
    {
        private const string Clave = "Clave1!";
        private static readonly string Hash = Encriptador.Hash(Clave);

        [TestInitialize]
        public void Setup()
        {
            SessionManager.Logout();
            ContadorSesion.GetInstance().Resetear();
            BLL.Configuracion.IntegridadComprometida = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            SessionManager.Logout();
            ContadorSesion.GetInstance().Resetear();
            BLL.Configuracion.IntegridadComprometida = false;
            BLL.Usuario.LectorEspejo = u => new DAL.EspejoUsuario().ObtenerPorUsername(u);
        }

        private static BLL.Usuario Crear(FakeUsuarioDAL dal) =>
            new BLL.Usuario(dal, new BLL.Familia(new FakePermisoDAL()));

        private static BE.LoginException CapturarLogin(Action a)
        {
            try { a(); return null; }
            catch (BE.LoginException ex) { return ex; }
        }

        [TestMethod]
        public void CuentaBloqueada_ConClaveIncorrecta_RespondeComoCualquierFallo_NoRevelaElBloqueo()
        {
            var dal = new FakeUsuarioDAL();
            dal.Usuarios.Add(new BE.Usuario { Id = 1, Username = "ana", Contraseña = Hash, Perfil = "Vendedor", Bloqueado = true });

            var ex = CapturarLogin(() => Crear(dal).Login("Test", "ana", "otra"));

            Assert.AreEqual(BE.LoginException.TipoError.CredencialesInvalidas, ex.Tipo);
        }

        [TestMethod]
        public void CuentaBloqueada_ConClaveCorrecta_InformaElBloqueo()
        {
            var dal = new FakeUsuarioDAL();
            dal.Usuarios.Add(new BE.Usuario { Id = 1, Username = "ana", Contraseña = Hash, Perfil = "Vendedor", Bloqueado = true });

            var ex = CapturarLogin(() => Crear(dal).Login("Test", "ana", Clave));

            Assert.AreEqual(BE.LoginException.TipoError.CuentaBloqueada, ex.Tipo);
        }

        [TestMethod]
        public void TercerFallo_BloqueaSegunElContadorDevueltoPorLaBase()
        {
            // La base ya registra 2 intentos (por ejemplo, desde otra terminal): el tercero bloquea
            // según el valor que devuelve el UPDATE ... OUTPUT inserted.IntentosFallidos.
            var dal = new FakeUsuarioDAL();
            dal.Usuarios.Add(new BE.Usuario { Id = 1, Username = "ana", Contraseña = Hash, Perfil = "Vendedor", IntentosFallidos = 2 });

            var ex = CapturarLogin(() => Crear(dal).Login("Test", "ana", "mala"));

            Assert.AreEqual(BE.LoginException.TipoError.CredencialesInvalidas, ex.Tipo, "El bloqueo no se anuncia sin la clave correcta.");
            Assert.AreEqual(1, dal.BloquearConTiempoVeces);
        }

        [TestMethod]
        public void UsuarioInexistente_LaBitacoraNoGuardaLoTipeado()
        {
            var dal = new FakeUsuarioDAL();
            string tipeado = "MiClaveSecreta#2026";   // por ejemplo, una contraseña en el campo usuario
            CapturarLogin(() => Crear(dal).Login("Test", tipeado, "x"));

            lock (ConfiguracionTests.Bitacora.Registros)
                Assert.IsFalse(ConfiguracionTests.Bitacora.Registros.Exists(r => (r.Actividad ?? "").Contains(tipeado)));
            Assert.AreNotEqual(tipeado, BLL.Usuario.HuellaUsuario(tipeado));
            Assert.AreEqual(8, BLL.Usuario.HuellaUsuario(tipeado).Length);
        }

        [TestMethod]
        public void IntegridadComprometida_UsaElRolDelEspejo_NoElDeLaTablaAlterada()
        {
            // En la tabla Usuario alguien se puso rol Administrador; el espejo conserva "Vendedor".
            var dal = new FakeUsuarioDAL();
            dal.Usuarios.Add(new BE.Usuario { Id = 2, Username = "vend", Contraseña = Hash, Perfil = "Administrador", Rol = "Administrador" });
            var espejo = FilaEspejo(2, "vend", Hash, "Vendedor");
            BLL.Usuario.LectorEspejo = u => u == "vend" ? espejo : null;
            BLL.Configuracion.IntegridadComprometida = true;

            Assert.IsTrue(Crear(dal).Login("Test", "vend", Clave));

            var s = SessionManager.GetInstance().Usuario;
            Assert.AreEqual("Vendedor", s.Perfil);
            Assert.IsFalse(s.EsAdministrador, "No llega a la consola de recuperación.");
            Assert.AreEqual(0, dal.IncrementarIntentosVeces, "No escribe en la tabla comprometida.");
        }

        [TestMethod]
        public void IntegridadComprometida_FilaDelEspejoConDVHInvalido_Rechaza()
        {
            var dal = new FakeUsuarioDAL();
            var espejo = FilaEspejo(1, "admin", Hash, "Administrador");
            espejo.DVHAlmacenado = espejo.DVHAlmacenado + 1;   // el espejo también fue alterado
            BLL.Usuario.LectorEspejo = u => espejo;
            BLL.Configuracion.IntegridadComprometida = true;

            var ex = CapturarLogin(() => Crear(dal).Login("Test", "admin", Clave));
            Assert.AreEqual(BE.LoginException.TipoError.CredencialesInvalidas, ex.Tipo);
            Assert.IsFalse(SessionManager.IsLoggedIn);
        }

        [TestMethod]
        public void IntegridadComprometida_UsuarioSinEspejo_Rechaza()
        {
            var dal = new FakeUsuarioDAL();
            dal.Usuarios.Add(new BE.Usuario { Id = 9, Username = "intruso", Contraseña = Hash, Perfil = "Administrador" });
            BLL.Usuario.LectorEspejo = u => null;   // insertado por SQL: no está en el espejo
            BLL.Configuracion.IntegridadComprometida = true;

            var ex = CapturarLogin(() => Crear(dal).Login("Test", "intruso", Clave));
            Assert.AreEqual(BE.LoginException.TipoError.CredencialesInvalidas, ex.Tipo);
        }

        [TestMethod]
        public void ValidarCredencialesAdmin_CuentaIntentos_YTrasElLimiteDejaDeValidar()
        {
            var dal = new FakeUsuarioDAL();
            dal.Usuarios.Add(new BE.Usuario { Id = 1, Username = "admin", Contraseña = Hash, Perfil = "Administrador" });
            var bll = Crear(dal);

            for (int i = 0; i < 3; i++) Assert.IsFalse(bll.ValidarCredencialesAdmin("admin", "mala"));
            Assert.IsTrue(ContadorSesion.GetInstance().LimiteAlcanzado);
            Assert.IsFalse(bll.ValidarCredencialesAdmin("admin", Clave), "Con el límite alcanzado no valida ni la clave correcta.");
        }

        [TestMethod]
        public void DesbloquearConClave_TrasElLimiteDeIntentos_Rechaza()
        {
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(new BE.Usuario { Id = 1, Username = "admin2", Perfil = "Administrador", Bloqueado = true });
            var dalClave = new FakeClaveRecuperacionDAL
            {
                Disponibles = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(1, Encriptador.Hash("ABC123XY")) }
            };
            var bll = new BLL.RecuperacionAdmin(dalUsuario, dalClave);
            for (int i = 0; i < 3; i++)
                try { bll.DesbloquearConClave("Test", "admin2", "NO-ES"); } catch (BE.AppException) { }

            try { bll.DesbloquearConClave("Test", "admin2", "ABC123XY"); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.emergencia.limite", ex.Clave); }
            Assert.AreEqual(0, dalUsuario.DesbloquearVeces);
        }

        [TestMethod]
        public void DesbloquearConClave_SinClaveValida_NoRevelaElEstadoDeLaCuenta()
        {
            // Un usuario NO administrador y NO bloqueado, con una clave inválida: antes respondía
            // "solo Administradores" / "no está bloqueada"; ahora la misma respuesta que siempre.
            var dalUsuario = new FakeUsuarioDAL();
            dalUsuario.Usuarios.Add(new BE.Usuario { Id = 3, Username = "vend", Perfil = "Vendedor" });
            var dalClave = new FakeClaveRecuperacionDAL
            {
                Disponibles = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(1, Encriptador.Hash("ABC123XY")) }
            };
            try { new BLL.RecuperacionAdmin(dalUsuario, dalClave).DesbloquearConClave("Test", "vend", "NO-ES"); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.emergencia.invalida", ex.Clave); }
        }

        private static BE.FilaUsuarioDV FilaEspejo(int id, string user, string hash, string rol)
        {
            var f = new BE.FilaUsuarioDV
            {
                Id = id, Username = user, Clave = hash, Rol = rol, Perfil = rol, Estado = "1", IntentosFallidos = "0",
                Activo = "1", RequiereCambioClave = "0", CantidadBloqueos = "0", FechaBloqueo = ""
            };
            f.DVHAlmacenado = Seguridad.CalculadorDV.Crear().CalcularDVH(f.CamposParaDVH());
            return f;
        }
    }
}
