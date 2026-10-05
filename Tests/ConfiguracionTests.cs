using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Configuración común a TODO el proyecto de tests. Las clases de BLL registran en la
    /// bitácora y revalidan al usuario de la sesión contra la base; acá se reemplazan esos
    /// puntos por dobles en memoria para que ninguna prueba lea ni escriba una base real
    /// (además, Tests/App.config apunta a WardrobeFlowDB_Tests, nunca a la base de la app).
    /// </summary>
    [TestClass]
    public static class ConfiguracionTests
    {
        // Bitácoras compartidas por todas las pruebas (se pueden inspeccionar desde un test).
        public static readonly FakeRegistroBitacora        Bitacora        = new FakeRegistroBitacora();
        public static readonly FakeRegistroBitacoraNegocio BitacoraNegocio = new FakeRegistroBitacoraNegocio();

        [AssemblyInitialize]
        public static void Inicializar(TestContext _)
        {
            Servicios.FabricaBitacora.Sistema = () => Bitacora;
            Servicios.FabricaBitacora.Negocio = () => BitacoraNegocio;
            InstalarUsuarioVigentePorDefecto();
        }

        // Revalidación del usuario de la sesión (BLL.PermisosAccion): por defecto, el usuario
        // logueado en la prueba sigue activo y con el mismo rol (no hay base que consultar).
        public static void InstalarUsuarioVigentePorDefecto()
        {
            BLL.PermisosAccion.LectorUsuarioVigente = id =>
            {
                var s = Seguridad.SessionManager.IsLoggedIn ? Seguridad.SessionManager.GetInstance().Usuario : null;
                return s != null && s.Id == id ? new BLL.UsuarioVigente(true, s.Perfil, s.Rol) : null;
            };
            BLL.PermisosAccion.LimpiarCacheVigencia();
        }
    }
}
