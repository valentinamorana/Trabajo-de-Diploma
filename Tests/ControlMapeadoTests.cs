using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Etapa 4 — permisos a nivel de control (BLL.ControlMapeado). Cubre el acceso a los mapeos y las
    /// reglas que aplica GUI.ManejadorSeguridad: qué mapeos corresponden a cada formulario (el Menú
    /// queda afuera), el bypass del Administrador y el acceso por patente exacta.
    /// </summary>
    [TestClass]
    public class ControlMapeadoTests
    {
        private static BE.ControlMapeado M(int idPermiso, string form, string control)
            => new BE.ControlMapeado { IdPermiso = idPermiso, Formulario = form, NombreControl = control };

        private static BE.Usuario Usuario(string perfil, params int[] patentes) => new BE.Usuario
        {
            Id = 50, Username = "u", Perfil = perfil,
            Permisos = patentes.Select(id => new BE.Permiso { Id = id }).ToList()
        };

        // ── ObtenerTodos ────────────────────────────────────────────────────

        [TestMethod]
        public void ObtenerTodos_DevuelveLoQueTraeElDAL()
        {
            var dal = new FakeControlMapeadoDAL();
            dal.Mapeos.Add(M(1, "Prendas", "btnAgregar"));
            dal.Mapeos.Add(M(2, "Clientes", "btnEliminar"));

            var lista = new BLL.ControlMapeado(dal).ObtenerTodos();

            Assert.AreEqual(1, dal.LlamadasObtenerTodos);
            CollectionAssert.AreEquivalent(new[] { "btnAgregar", "btnEliminar" },
                                           lista.Select(m => m.NombreControl).ToList());
        }

        [TestMethod]
        public void ObtenerTodos_SinMapeos_ListaVaciaNoNull()
        {
            var lista = new BLL.ControlMapeado(new FakeControlMapeadoDAL()).ObtenerTodos();
            Assert.IsNotNull(lista);
            Assert.AreEqual(0, lista.Count);
        }

        // ── AplicablesA ─────────────────────────────────────────────────────

        [TestMethod]
        public void AplicablesA_FiltraPorFormularioSinDistinguirMayusculas()
        {
            var todos = new List<BE.ControlMapeado>
            {
                M(1, "Prendas",  "btnAgregar"),
                M(2, "PRENDAS",  "btnEliminar"),
                M(3, "Clientes", "btnAgregar"),
            };

            var r = BLL.ControlMapeado.AplicablesA(todos, "prendas");

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, r.Select(m => m.IdPermiso).ToList());
        }

        [TestMethod]
        public void AplicablesA_FormularioMenu_NoRecibeMapeosDeControl()
        {
            // El Menú tiene su propio gateo por NombreMenu (BLL.MenuVisibilidad); aplicar también los
            // mapeos de control sería un segundo mecanismo sobre los mismos ítems.
            var todos = new List<BE.ControlMapeado>
            {
                M(1, "Menu", "prendasToolStripMenuItem"),
                M(2, "Menu", "clientesToolStripMenuItem"),
            };

            Assert.AreEqual(0, BLL.ControlMapeado.AplicablesA(todos, "Menu").Count);
            Assert.AreEqual(0, BLL.ControlMapeado.AplicablesA(todos, "MENU").Count);
        }

        [TestMethod]
        public void AplicablesA_FormularioSinMapeos_ListaVacia()
        {
            var todos = new List<BE.ControlMapeado> { M(1, "Prendas", "btnAgregar") };
            Assert.AreEqual(0, BLL.ControlMapeado.AplicablesA(todos, "Clientes").Count);
        }

        [TestMethod]
        public void AplicablesA_EntradasNulasOVacias_NoRompen()
        {
            var todos = new List<BE.ControlMapeado> { M(1, "Prendas", "btnAgregar") };

            Assert.AreEqual(0, BLL.ControlMapeado.AplicablesA(null, "Prendas").Count);
            Assert.AreEqual(0, BLL.ControlMapeado.AplicablesA(todos, null).Count);
            Assert.AreEqual(0, BLL.ControlMapeado.AplicablesA(todos, "").Count);
            Assert.AreEqual(1, BLL.ControlMapeado.AplicablesA(
                new List<BE.ControlMapeado> { null, M(1, "Prendas", "btnAgregar") }, "Prendas").Count);
        }

        // ── PatentesDe / TieneAcceso ────────────────────────────────────────

        [TestMethod]
        public void PatentesDe_DevuelveLosIdsDeLosPermisosCargados()
        {
            var p = BLL.ControlMapeado.PatentesDe(Usuario("Vendedor", 4, 7, 7));
            CollectionAssert.AreEquivalent(new[] { 4, 7 }, p.ToList());
        }

        [TestMethod]
        public void PatentesDe_UsuarioSinPermisosONull_ConjuntoVacio()
        {
            Assert.AreEqual(0, BLL.ControlMapeado.PatentesDe(new BE.Usuario { Permisos = null }).Count);
            Assert.AreEqual(0, BLL.ControlMapeado.PatentesDe(null).Count);
        }

        [TestMethod]
        public void TieneAcceso_ConLaPatenteDelMapeo_Permite()
        {
            var u = Usuario("Vendedor", 4, 7);
            Assert.IsTrue(BLL.ControlMapeado.TieneAcceso(M(7, "Prendas", "btnAgregar"),
                          u.EsAdministrador, BLL.ControlMapeado.PatentesDe(u)));
        }

        [TestMethod]
        public void TieneAcceso_SinLaPatenteDelMapeo_Niega()
        {
            var u = Usuario("Vendedor", 4, 7);
            Assert.IsFalse(BLL.ControlMapeado.TieneAcceso(M(9, "Prendas", "btnEliminar"),
                           u.EsAdministrador, BLL.ControlMapeado.PatentesDe(u)));
        }

        [TestMethod]
        public void TieneAcceso_AdministradorSinPatentes_VeTodo()
        {
            // B1 — bypass del Administrador: no depende de tener las patentes bien asignadas en la BD.
            var admin = Usuario("Administrador");
            Assert.IsTrue(admin.EsAdministrador);
            Assert.IsTrue(BLL.ControlMapeado.TieneAcceso(M(9, "Prendas", "btnEliminar"),
                          admin.EsAdministrador, BLL.ControlMapeado.PatentesDe(admin)));
        }

        [TestMethod]
        public void TieneAcceso_UsuarioSinPermisosCargados_Niega()
        {
            var u = new BE.Usuario { Id = 3, Perfil = "Vendedor", Permisos = null };
            Assert.IsFalse(BLL.ControlMapeado.TieneAcceso(M(1, "Prendas", "btnAgregar"),
                           u.EsAdministrador, BLL.ControlMapeado.PatentesDe(u)));
            Assert.IsFalse(BLL.ControlMapeado.TieneAcceso(M(1, "Prendas", "btnAgregar"), false, null));
        }

        [TestMethod]
        public void Escenario_VendedorEnPrendas_SoloVeLoQueSuPatenteHabilita()
        {
            var dal = new FakeControlMapeadoDAL();
            dal.Mapeos.Add(M(10, "Prendas",  "btnAgregar"));
            dal.Mapeos.Add(M(11, "Prendas",  "btnEliminar"));
            dal.Mapeos.Add(M(11, "Clientes", "btnEliminar"));
            dal.Mapeos.Add(M(10, "Menu",     "prendasToolStripMenuItem"));
            var u = Usuario("Vendedor", 10);

            var visibles = BLL.ControlMapeado.AplicablesA(new BLL.ControlMapeado(dal).ObtenerTodos(), "Prendas")
                .Where(m => BLL.ControlMapeado.TieneAcceso(m, u.EsAdministrador, BLL.ControlMapeado.PatentesDe(u)))
                .Select(m => m.NombreControl).ToList();

            CollectionAssert.AreEqual(new[] { "btnAgregar" }, visibles);
        }
    }
}
