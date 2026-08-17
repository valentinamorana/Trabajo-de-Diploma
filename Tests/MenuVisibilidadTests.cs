using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Regresión para la visibilidad del menú principal por rol (GUI.Menu.AplicarPermisos,
    /// vía BLL.MenuVisibilidad.Resolver — la lógica pura extraída para poder testearla).
    /// Los 7 roles y sus patentes reproducen EXACTAMENTE lo que siembra
    /// BD/07_Reset_Perfiles_Permisos.sql, no una versión inventada — si este archivo y ese
    /// script se desincronizan, alguno de los dos está mal.
    /// </summary>
    [TestClass]
    public class MenuVisibilidadTests
    {
        private static bool V(Dictionary<string, bool> d, string item) => d.TryGetValue(item, out var v) && v;

        [TestMethod]
        public void Administrador_VeTodoMenosOutfitsYCategorias()
        {
            // Bypass total: no depende de tener las patentes bien asignadas en la BD.
            var v = BLL.MenuVisibilidad.Resolver(new string[0], esAdmin: true);

            Assert.IsTrue(V(v, "panelControlToolStripMenuItem"));
            Assert.IsTrue(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsTrue(V(v, "clientesToolStripMenuItem"));
            Assert.IsTrue(V(v, "planesToolStripMenuItem"));
            Assert.IsTrue(V(v, "renovacionSuscripcionToolStripMenuItem"));
            Assert.IsTrue(V(v, "cobroSuscripcionToolStripMenuItem"));
            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosVentaToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitacoraToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitSistemaToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitNegocioToolStripMenuItem"));
            Assert.IsTrue(V(v, "reporteJornadaToolStripMenuItem"));
            Assert.IsTrue(V(v, "gestionToolStripMenuItem"));
            Assert.IsTrue(V(v, "grpUsuarios"));
            Assert.IsTrue(V(v, "grpSistema"));
            Assert.IsTrue(V(v, "usuariosToolStripMenuItem"));
            Assert.IsTrue(V(v, "perfilesToolStripMenuItem"));
            Assert.IsTrue(V(v, "idiomasToolStripMenuItem"));
            Assert.IsTrue(V(v, "backupToolStripMenuItem"));
            Assert.IsTrue(V(v, "integridadToolStripMenuItem"));

            // Caso especial: Outfits/Categorías NUNCA se ven, ni siquiera para Admin
            // (módulo no implementado, Visible=false fijo en el código).
            Assert.IsFalse(V(v, "outfitsToolStripMenuItem"));
            Assert.IsFalse(V(v, "categoriasToolStripMenuItem"));
        }

        [TestMethod]
        public void Auditor_SoloVePanelYAnalitica()
        {
            var v = BLL.MenuVisibilidad.Resolver(new[] { "mnuAuditoria" }, esAdmin: false);

            Assert.IsTrue(V(v, "panelControlToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitacoraToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitSistemaToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitNegocioToolStripMenuItem"));
            Assert.IsTrue(V(v, "reporteJornadaToolStripMenuItem"));

            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "inventarioToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasToolStripMenuItem"));
            Assert.IsFalse(V(v, "gestionToolStripMenuItem"));
        }

        [TestMethod]
        public void Vendedor_VeSuscriptoresInventarioPrendasYSoloPedidosDeVenta()
        {
            var patentes = new[] { "mnuPrendas", "mnuClientes", "mnuPlanSuscripciones",
                                    "mnuRenovacionSuscripcion", "mnuCobroSuscripcion", "mnuPedidosVenta" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            Assert.IsTrue(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsTrue(V(v, "clientesToolStripMenuItem"));
            Assert.IsTrue(V(v, "planesToolStripMenuItem"));
            Assert.IsTrue(V(v, "renovacionSuscripcionToolStripMenuItem"));
            Assert.IsTrue(V(v, "cobroSuscripcionToolStripMenuItem"));
            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosVentaToolStripMenuItem"));

            // Vendedor NO despacha: Pedidos Realizados no aparece.
            Assert.IsFalse(V(v, "pedidosRealizadosToolStripMenuItem"));
            Assert.IsFalse(V(v, "bitacoraToolStripMenuItem"));
            Assert.IsFalse(V(v, "gestionToolStripMenuItem"));
        }

        [TestMethod]
        public void GerenteComercial_HeredaVendedorMasPedidosRealizados()
        {
            // Composite: GerenteComercial → Vendedor. Las patentes efectivas ya vienen
            // resueltas (recursivamente) antes de llegar acá — se simula el resultado final.
            var patentes = new[] { "mnuPrendas", "mnuClientes", "mnuPlanSuscripciones",
                                    "mnuRenovacionSuscripcion", "mnuCobroSuscripcion",
                                    "mnuPedidosVenta", "mnuPedidosRealizados" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            Assert.IsTrue(V(v, "pedidosVentaToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
        }

        [TestMethod]
        public void OperadorLogistico_SoloVePedidosRealizados()
        {
            var v = BLL.MenuVisibilidad.Resolver(new[] { "mnuPedidosRealizados" }, esAdmin: false);

            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));
            Assert.IsFalse(V(v, "pedidosVentaToolStripMenuItem"));
            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "inventarioToolStripMenuItem"));
        }

        [TestMethod]
        public void OperadorDeInventario_VeSoloInventarioPrendas()
        {
            var v = BLL.MenuVisibilidad.Resolver(new[] { "mnuPrendas", "mnuStock" }, esAdmin: false);

            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasToolStripMenuItem"));
        }

        [TestMethod]
        public void GerenteInventario_HeredaAmbosOperadores_PeroOutfitsYCategoriasSiguenOcultos()
        {
            // Composite: GerenteInventario → OperadorLogistico + OperadorDeInventario,
            // más mnuCategorias/mnuOutfits propias (asignadas en BD, sin efecto visual).
            var patentes = new[] { "mnuCategorias", "mnuOutfits", "mnuPedidosRealizados", "mnuPrendas", "mnuStock" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));

            // Tiene las patentes pero el módulo no está implementado: sigue oculto.
            Assert.IsFalse(V(v, "outfitsToolStripMenuItem"));
            Assert.IsFalse(V(v, "categoriasToolStripMenuItem"));

            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "bitacoraToolStripMenuItem"));
        }

        /// <summary>
        /// Guarda contra pérdida silenciosa de entradas en Hojas/Grupos: GUI.Menu.AplicarPermisos
        /// aplica el resultado por .Name y, desde el fix de fail-closed, cualquier ToolStripMenuItem
        /// cuyo .Name no aparezca acá queda OCULTO sin importar sus permisos reales. Si esta lista se
        /// desincroniza con el array 'items' de Menu.cs, este test es la única señal antes de que un
        /// usuario reporte "no veo tal pantalla y sí debería".
        /// </summary>
        [TestMethod]
        public void Resolver_DevuelveTodosLosItemsRealesDelMenu()
        {
            var v = BLL.MenuVisibilidad.Resolver(new string[0], esAdmin: true);

            string[] esperados =
            {
                "panelControlToolStripMenuItem", "inventarioToolStripMenuItem",
                "prendasToolStripMenuItem", "outfitsToolStripMenuItem", "categoriasToolStripMenuItem",
                "clientesToolStripMenuItem", "planesToolStripMenuItem",
                "renovacionSuscripcionToolStripMenuItem", "cobroSuscripcionToolStripMenuItem",
                "pedidosVentaToolStripMenuItem", "pedidosRealizadosToolStripMenuItem",
                "usuariosToolStripMenuItem", "perfilesToolStripMenuItem", "idiomasToolStripMenuItem",
                "historialUsuariosToolStripMenuItem", "backupToolStripMenuItem", "integridadToolStripMenuItem",
                "adminUsuariosItem",
                "bitSistemaToolStripMenuItem", "bitNegocioToolStripMenuItem", "reporteJornadaToolStripMenuItem",
                "suscriptoresToolStripMenuItem", "ventasToolStripMenuItem", "bitacoraToolStripMenuItem",
                "grpUsuarios", "grpSistema", "gestionToolStripMenuItem"
            };

            foreach (var nombre in esperados)
                Assert.IsTrue(v.ContainsKey(nombre), $"Falta '{nombre}' en el resultado de Resolver — revisar Menu.cs vs BLL.MenuVisibilidad.");
        }

        [TestMethod]
        public void SinPatentesNiAdmin_SoloPanelDeControl()
        {
            var v = BLL.MenuVisibilidad.Resolver(new string[0], esAdmin: false);

            Assert.IsTrue(V(v, "panelControlToolStripMenuItem"));
            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "inventarioToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasToolStripMenuItem"));
            Assert.IsFalse(V(v, "bitacoraToolStripMenuItem"));
            Assert.IsFalse(V(v, "gestionToolStripMenuItem"));
        }

        [TestMethod]
        public void PatentesCaseInsensitive()
        {
            var v = BLL.MenuVisibilidad.Resolver(new[] { "MNUPRENDAS" }, esAdmin: false);
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
        }
    }
}
