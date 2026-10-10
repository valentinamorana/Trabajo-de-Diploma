using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// • Refresco de permisos EN VIVO resuelto por la BLL (BLL.Sesion.RefrescarPermisos): la GUI
    ///   ya no escribe el estado de autorización de la sesión.
    /// • Lectura de la bitácora del SISTEMA: lista blanca (Administrador o patente de Auditoría)
    ///   exigida en la BLL.
    /// </summary>
    [TestClass]
    public class SeguridadSesionBitacoraTests
    {
        [TestInitialize]
        public void Preparar()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
        }

        [TestCleanup]
        public void Limpiar()
        {
            SessionManager.Logout();
            BLL.PermisosAccion.LimpiarCacheVigencia();
        }

        // MSTest v1 no trae Assert.ThrowsException: acepta T o un derivado y lo devuelve.
        private static T Lanza<T>(System.Action accion) where T : System.Exception
        {
            try { accion(); }
            catch (T ex) { return ex; }
            Assert.Fail("Se esperaba una excepción " + typeof(T).Name + ".");
            return null;
        }

        private static void Login(string perfil, params string[] patentes)
        {
            SessionManager.Login(new BE.Usuario
            {
                Id = 30, Username = "u30", Perfil = perfil, Rol = perfil,
                Permisos = patentes.Select(p => new BE.Permiso { NombreMenu = p }).ToList()
            });
        }

        // ── Refresco de permisos ────────────────────────────────────────────────────────

        [TestMethod]
        public void RefrescarPermisos_RecargaDesdeElOrigenYActualizaLaSesion()
        {
            // El usuario entró con permisos viejos (vacíos); el rol "Gerente" del árbol del doble
            // resuelve a mnuClientes + mnuStock.
            Login("Gerente");

            var permisos = BLL.Sesion.RefrescarPermisos(new BLL.Familia(new FakePermisoDAL()));

            var nombres = permisos.Select(p => p.NombreMenu).OrderBy(n => n).ToList();
            CollectionAssert.AreEqual(new List<string> { "mnuClientes", "mnuStock" }, nombres);
            Assert.AreSame(permisos, SessionManager.GetInstance().Usuario.Permisos,
                "La sesión debe quedar con la lista recargada.");
            Assert.IsTrue(SessionManager.GetInstance().TienePermiso("mnuStock"));
        }

        [TestMethod]
        public void RefrescarPermisos_SinSesion_DevuelveNull()
        {
            Assert.IsNull(BLL.Sesion.RefrescarPermisos(new BLL.Familia(new FakePermisoDAL())));
        }

        [TestMethod]
        public void SessionManager_UsuarioNoTieneSetterPublico()
        {
            var prop = typeof(SessionManager).GetProperty("Usuario");
            Assert.IsNotNull(prop);
            Assert.IsNull(prop.GetSetMethod(false),
                "El usuario de la sesión solo se fija en Login: no debe poder reemplazarse desde afuera.");
        }

        // ── Bitácora del sistema: lista blanca ──────────────────────────────────────────

        [TestMethod]
        public void PuedeVerSistema_SinSesion_False()
        {
            Assert.IsFalse(new BLL.Bitacora().UsuarioPuedeVerSistema());
        }

        [TestMethod]
        public void PuedeVerSistema_Administrador_True()
        {
            Login("Administrador");
            Assert.IsTrue(new BLL.Bitacora().UsuarioPuedeVerSistema());
        }

        [TestMethod]
        public void PuedeVerSistema_RolConPatenteAuditoria_True()
        {
            Login("Auditor", BE.Patentes.Auditoria);
            Assert.IsTrue(new BLL.Bitacora().UsuarioPuedeVerSistema());
        }

        [TestMethod]
        public void PuedeVerSistema_RolesOperativosSinAuditoria_False()
        {
            // Antes (lista negra) todos estos pasaban salvo el Gerente Comercial.
            foreach (var perfil in new[] { "Vendedor", "Operador", "GerenteComercial", "RolNuevoCualquiera" })
            {
                SessionManager.Logout();
                Login(perfil, BE.Patentes.Clientes, BE.Patentes.Prendas);
                Assert.IsFalse(new BLL.Bitacora().UsuarioPuedeVerSistema(), perfil);
            }
        }

        [TestMethod]
        public void LecturaSistema_SinPermiso_SeRechazaEnLaBLL()
        {
            Login("Vendedor", BE.Patentes.Clientes);
            var bll = new BLL.Bitacora();

            var e1 = Lanza<BE.AppException>(() => bll.ObtenerUltimosNDiasSistema(7));
            var e2 = Lanza<BE.AppException>(() => bll.ObtenerTodosSistema());
            var e3 = Lanza<BE.AppException>(() => bll.BuscarPorFiltrosSistema(null, null, 0, null, -1));

            foreach (var e in new[] { e1, e2, e3 })
                Assert.AreEqual("err.bll.bitacora.sin_permiso_sistema", e.Clave);
        }

        [TestMethod]
        public void LecturaSistema_SinSesion_SeRechaza()
        {
            var e = Lanza<BE.AppException>(() => new BLL.Bitacora().ObtenerUltimosNDiasSistema(7));
            Assert.AreEqual("err.bll.sesion_expirada", e.Clave);
        }
    }
}
