using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// T04 — BLL.Familia, métodos de ESCRITURA (CrearRol, EliminarRol, AgregarComponente,
    /// GuardarAsignacionRol). Antes solo estaba testeada la función pura SistemaConservaGestion,
    /// no la orquestación real que la dispara en producción.
    ///
    /// Deliberadamente NO cubre el camino feliz de GuardarAsignacionRol/EliminarRol/
    /// QuitarComponente: los tres pasan por ExigirSistemaConservaGestion, que internamente
    /// instancia "new Usuario()" (BLL concreto con su DAL real) para poder enumerar TODOS los
    /// usuarios del sistema — no hay forma de inyectar un doble ahí sin tocar ese código de
    /// producción, así que ejercitarlos de punta a punta pegaría contra la base real y el
    /// resultado dependería de qué usuarios/administradores existan en ella (no determinístico).
    /// Sí se cubren todas las validaciones que ocurren ANTES de llegar a ese guard, que es
    /// donde vive la lógica de negocio real de estos métodos.
    /// </summary>
    [TestClass]
    public class FamiliaEscrituraTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static void LoginComoAdministrador()
        {
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador" });
        }

        private static void LoginComoVendedorSinGestion()
        {
            SessionManager.Login(new BE.Usuario
            {
                Id = 2, Username = "vend", Perfil = "Vendedor",
                Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = "mnuClientes" } }
            });
        }

        // ── VerificarPuedeGestionar (guard común a todos los métodos de escritura) ──────────

        [TestMethod]
        public void CrearRol_SinSesion_LanzaSesionExpirada_SinTocarElDAL()
        {
            var dal = new FakePermisoDAL();
            var bll = new BLL.Familia(dal);

            try
            {
                bll.CrearRol("NuevoRol");
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
            Assert.AreEqual(0, dal.AltaComponenteVeces);
        }

        [TestMethod]
        public void CrearRol_UsuarioSinPermisoDeGestion_LanzaSinPermiso_SinTocarElDAL()
        {
            LoginComoVendedorSinGestion();
            var dal = new FakePermisoDAL();
            var bll = new BLL.Familia(dal);

            try
            {
                bll.CrearRol("NuevoRol");
                Assert.Fail("Debía exigir el permiso de gestión de usuarios.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.familia.sin_permiso", ex.Clave);
            }
            Assert.AreEqual(0, dal.AltaComponenteVeces);
        }

        // ── CrearRol ─────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void CrearRol_NombreVacio_LanzaNombreVacio_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakePermisoDAL();
            var bll = new BLL.Familia(dal);

            try
            {
                bll.CrearRol("   ");
                Assert.Fail("Debía rechazar un nombre vacío.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.nombre_vacio", ex.Clave);
            }
            Assert.AreEqual(0, dal.AltaComponenteVeces);
        }

        [TestMethod]
        public void CrearRol_NombreDuplicado_LanzaRolDuplicado_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakePermisoDAL { IdsPorRol = new Dictionary<string, int> { ["Supervisor"] = 5 } };
            var bll = new BLL.Familia(dal);

            try
            {
                bll.CrearRol("Supervisor");
                Assert.Fail("Debía rechazar un rol ya existente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.rol_duplicado", ex.Clave);
            }
            Assert.AreEqual(0, dal.AltaComponenteVeces);
        }

        [TestMethod]
        public void CrearRol_DatosValidos_PersisteYDevuelveElId()
        {
            LoginComoAdministrador();
            var dal = new FakePermisoDAL { AltaComponenteIdGenerado = 42 };
            var bll = new BLL.Familia(dal);

            int id = bll.CrearRol("Auditor");

            Assert.AreEqual(42, id);
            Assert.AreEqual(1, dal.AltaComponenteVeces);
        }

        // ── EliminarRol — validaciones previas al guard sistémico ──────────────────────────

        [TestMethod]
        public void EliminarRol_RolInexistente_LanzaRolInexistente_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakePermisoDAL(); // IdsPorRol vacío → ObtenerIdRol devuelve 0
            var bll = new BLL.Familia(dal);

            try
            {
                bll.EliminarRol("NoExiste");
                Assert.Fail("Debía rechazar un rol inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.rol_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, dal.BajaComponenteVeces);
        }

        [TestMethod]
        public void EliminarRol_ConUsuariosAsignados_LanzaRolEnUso_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakePermisoDAL
            {
                IdsPorRol = new Dictionary<string, int> { ["Vendedor"] = 7 },
                ContarUsuariosPorRolRespuesta = 3,
                ObtenerUsuariosPorRolRespuesta = new List<string> { "ana", "beto", "cami" }
            };
            var bll = new BLL.Familia(dal);

            try
            {
                bll.EliminarRol("Vendedor");
                Assert.Fail("Debía rechazar eliminar un rol con usuarios asignados.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.rol_en_uso", ex.Clave);
            }
            Assert.AreEqual(0, dal.BajaComponenteVeces);
        }

        // ── AgregarComponente / ValidarSinCiclo — completamente aislado (no pasa por el guard) ──

        [TestMethod]
        public void AgregarComponente_CicloDirecto_LanzaCiclo_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakePermisoDAL();
            var bll = new BLL.Familia(dal);

            try
            {
                bll.AgregarComponente(idPadre: 5, idHijo: 5);
                Assert.Fail("Debía rechazar que un componente se contenga a sí mismo.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.ciclo", ex.Clave);
            }
            Assert.AreEqual(0, dal.AgregarRelacionVeces);
        }

        [TestMethod]
        public void AgregarComponente_CicloIndirecto_LanzaCiclo_SinTocarElDAL()
        {
            LoginComoAdministrador();
            // Gerente (200) → Ventas (1): Ventas ya es descendiente de Gerente.
            // Intentar agregar Gerente como hijo de Ventas cerraría el ciclo.
            var ventas = new BE.Rol { Id = 1, Nombre = "Ventas" };
            var gerente = new BE.Rol { Id = 200, Nombre = "Gerente" };
            gerente.AgregarHijo(ventas);

            var dal = new FakePermisoDAL { ArbolPersonalizado = new List<BE.Componente> { gerente } };
            var bll = new BLL.Familia(dal);

            try
            {
                bll.AgregarComponente(idPadre: 1, idHijo: 200);
                Assert.Fail("Debía detectar el ciclo indirecto.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.ciclo", ex.Clave);
            }
            Assert.AreEqual(0, dal.AgregarRelacionVeces);
        }

        [TestMethod]
        public void AgregarComponente_SinCiclo_AgregaRelacion()
        {
            LoginComoAdministrador();
            var ventas = new BE.Rol { Id = 1, Nombre = "Ventas" };
            var stock = new BE.Patente { Id = 3, Nombre = "Stock" };
            var dal = new FakePermisoDAL { ArbolPersonalizado = new List<BE.Componente> { ventas, stock } };
            var bll = new BLL.Familia(dal);

            bll.AgregarComponente(idPadre: 1, idHijo: 3);

            Assert.AreEqual(1, dal.AgregarRelacionVeces);
            Assert.AreEqual((1, 3), dal.UltimaRelacionAgregada);
        }

        // ── GuardarAsignacionRol — validaciones previas al guard sistémico ─────────────────

        [TestMethod]
        public void GuardarAsignacionRol_RolInexistente_LanzaRolInexistente_SinTocarElDAL()
        {
            LoginComoAdministrador();
            var dal = new FakePermisoDAL();
            var bll = new BLL.Familia(dal);

            try
            {
                bll.GuardarAsignacionRol("NoExiste", new List<int> { 1, 2 });
                Assert.Fail("Debía rechazar un rol inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.rol_inexistente", ex.Clave);
            }
            Assert.AreEqual(0, dal.AgregarRelacionVeces);
            Assert.AreEqual(0, dal.QuitarRelacionVeces);
        }

        [TestMethod]
        public void GuardarAsignacionRol_UsuarioNoAdminQuitaSuPropioAccesoDeGestion_LanzaAutobloqueo_SinTocarElDAL()
        {
            // Anti-autobloqueo: un no-Administrador no puede editar SU PROPIO rol de forma que
            // pierda el acceso de gestión (mnuUsuarios) — acá el rol "Vendedor" ni siquiera tiene
            // el permiso de gestión en el árbol simulado, así que ConservaGestion da false.
            SessionManager.Login(new BE.Usuario
            {
                Id = 3, Username = "vend", Perfil = "Vendedor",
                Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = "mnuUsuarios" } }
            });
            var ventas = new BE.Rol { Id = 1, Nombre = "Vendedor" }; // sin ninguna patente de gestión
            var dal = new FakePermisoDAL
            {
                ArbolPersonalizado = new List<BE.Componente> { ventas },
                IdsPorRol = new Dictionary<string, int> { ["Vendedor"] = 1 }
            };
            var bll = new BLL.Familia(dal);

            try
            {
                bll.GuardarAsignacionRol("Vendedor", new List<int>());
                Assert.Fail("Debía rechazar que el usuario se quite a sí mismo el acceso de gestión.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.familia.autobloqueo", ex.Clave);
            }
            Assert.AreEqual(0, dal.AgregarRelacionVeces);
            Assert.AreEqual(0, dal.QuitarRelacionVeces);
        }
    }
}
