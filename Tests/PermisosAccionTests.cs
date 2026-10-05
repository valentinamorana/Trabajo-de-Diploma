using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;

namespace Tests
{
    /// <summary>
    /// Permisos granulares de acción ("Ver" vs "Configurar") — BLL.PermisosAccion.
    /// Fail-closed: toda escritura exige la patente de EDICIÓN (ya no hay fallback a la de ver
    /// cuando el catálogo no se puede leer) y el usuario de la sesión debe seguir activo y con
    /// el mismo rol en la base.
    /// </summary>
    [TestClass]
    public class PermisosAccionTests
    {
        [TestInitialize]
        public void Setup()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
        }

        [TestCleanup]
        public void Cleanup()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
        }

        private static void Login(string perfil, params string[] patentes)
        {
            var permisos = new System.Collections.Generic.List<BE.Permiso>();
            foreach (var p in patentes) permisos.Add(new BE.Permiso { NombreMenu = p });
            SessionManager.Login(new BE.Usuario { Id = 7, Username = "u7", Perfil = perfil, Rol = perfil, Permisos = permisos });
        }

        [TestMethod]
        public void Admin_SiemprePuede_AunqueNoTengaNada()
        {
            Assert.IsTrue(BLL.PermisosAccion.PermiteAccion(esAdmin: true, tieneEditar: false));
        }

        [TestMethod]
        public void ConPatenteDeEdicion_Puede_SinElla_No()
        {
            Assert.IsTrue(BLL.PermisosAccion.PermiteAccion(esAdmin: false, tieneEditar: true));
            Assert.IsFalse(BLL.PermisosAccion.PermiteAccion(esAdmin: false, tieneEditar: false));
        }

        [TestMethod]
        public void Exigir_SoloConPatenteDeVer_Rechaza_FallaCerrado()
        {
            // Antes, si la patente de edición no figuraba en el catálogo (o el catálogo no se podía
            // leer), alcanzaba con la de VER. Ahora la de edición es obligatoria.
            Login("Vendedor", BE.Patentes.Clientes);
            var ex = Capturar(() => BLL.PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes));
            Assert.AreEqual("err.bll.sin_permiso", ex?.Clave);
        }

        [TestMethod]
        public void Exigir_ConPatenteDeEdicion_YUsuarioVigente_Permite()
        {
            Login("Vendedor", BE.Patentes.ClientesEditar);
            BLL.PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
        }

        [TestMethod]
        public void Exigir_UsuarioArchivadoEnLaBase_Rechaza()
        {
            Login("Vendedor", BE.Patentes.ClientesEditar);
            BLL.PermisosAccion.LectorUsuarioVigente = id => new BLL.UsuarioVigente(false, "Vendedor", "Vendedor");
            var ex = Capturar(() => BLL.PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes));
            Assert.AreEqual("err.bll.permisos.usuario_no_vigente", ex?.Clave);
        }

        [TestMethod]
        public void Exigir_RolCambiadoEnLaBase_Rechaza_AunSiendoAdminEnLaSesion()
        {
            // La sesión dice Administrador, pero en la base ya no lo es: no puede seguir operando.
            Login("Administrador");
            BLL.PermisosAccion.LectorUsuarioVigente = id => new BLL.UsuarioVigente(true, "Vendedor", "Vendedor");
            var ex = Capturar(() => BLL.PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes));
            Assert.AreEqual("err.bll.permisos.usuario_no_vigente", ex?.Clave);
        }

        [TestMethod]
        public void Exigir_UsuarioEliminado_Rechaza()
        {
            Login("Administrador");
            BLL.PermisosAccion.LectorUsuarioVigente = id => null;
            var ex = Capturar(() => BLL.PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes));
            Assert.AreEqual("err.bll.permisos.usuario_no_vigente", ex?.Clave);
        }

        [TestMethod]
        public void Exigir_SiNoSePuedeLeerLaBase_FallaCerrado()
        {
            Login("Administrador");
            BLL.PermisosAccion.LectorUsuarioVigente = id => throw new System.Exception("sin conexión");
            var ex = Capturar(() => BLL.PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes));
            Assert.AreEqual("err.bll.permisos.no_verificable", ex?.Clave);
        }

        [TestMethod]
        public void Exigir_CacheaLaVigencia_NoReleeEnCadaLlamada()
        {
            Login("Administrador");
            int lecturas = 0;
            BLL.PermisosAccion.LectorUsuarioVigente = id => { lecturas++; return new BLL.UsuarioVigente(true, "Administrador", "Administrador"); };
            for (int i = 0; i < 5; i++)
                BLL.PermisosAccion.Exigir(BE.Patentes.ClientesEditar, BE.Patentes.Clientes);
            Assert.AreEqual(1, lecturas, "Dentro de los 60 s la vigencia se toma de la caché.");
        }

        private static BE.AppException Capturar(System.Action a)
        {
            try { a(); return null; }
            catch (BE.AppException ex) { return ex; }
        }
    }
}
