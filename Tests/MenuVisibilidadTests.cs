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
        public void Administrador_VeTodoElMenu()
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
            // "Analítica" (antes un único dropdown de 9 ítems) se partió en dos menúes de
            // primer nivel: Auditoría (bitácoras + reporte de jornada) y Analítica de Negocio
            // (los 6 reportes de decisión comercial, Bloque 3 + PdN10).
            Assert.IsTrue(V(v, "auditoriaToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitSistemaToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitNegocioToolStripMenuItem"));
            Assert.IsTrue(V(v, "reporteJornadaToolStripMenuItem"));
            Assert.IsTrue(V(v, "analiticaNegocioToolStripMenuItem"));
            Assert.IsTrue(V(v, "analisisAbandonoToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasVendedorToolStripMenuItem"));
            Assert.IsTrue(V(v, "analisisRotacionToolStripMenuItem"));
            Assert.IsTrue(V(v, "analisisMantenimientoToolStripMenuItem"));
            Assert.IsTrue(V(v, "analisisEscasezToolStripMenuItem"));
            Assert.IsTrue(V(v, "recomendacionPrendasToolStripMenuItem"));
            Assert.IsTrue(V(v, "gestionToolStripMenuItem"));
            Assert.IsTrue(V(v, "grpUsuarios"));
            Assert.IsTrue(V(v, "grpSistema"));
            Assert.IsTrue(V(v, "usuariosToolStripMenuItem"));
            Assert.IsTrue(V(v, "perfilesToolStripMenuItem"));
            Assert.IsTrue(V(v, "idiomasToolStripMenuItem"));
            Assert.IsTrue(V(v, "backupToolStripMenuItem"));
            Assert.IsTrue(V(v, "integridadToolStripMenuItem"));
        }

        [TestMethod]
        public void Auditor_SoloVePanelYAuditoria()
        {
            var v = BLL.MenuVisibilidad.Resolver(new[] { "mnuAuditoria" }, esAdmin: false);

            Assert.IsTrue(V(v, "panelControlToolStripMenuItem"));
            Assert.IsTrue(V(v, "auditoriaToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitSistemaToolStripMenuItem"));
            Assert.IsTrue(V(v, "bitNegocioToolStripMenuItem"));
            Assert.IsTrue(V(v, "reporteJornadaToolStripMenuItem"));

            // El Auditor no tiene ninguna patente de "Analítica de Negocio" (decisión
            // comercial, no auditoría): el menú entero queda oculto, ya no comparte
            // dropdown con Auditoría como antes de la separación.
            Assert.IsFalse(V(v, "analiticaNegocioToolStripMenuItem"));
            Assert.IsFalse(V(v, "analisisAbandonoToolStripMenuItem"));

            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "inventarioToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasToolStripMenuItem"));
            Assert.IsFalse(V(v, "gestionToolStripMenuItem"));
        }

        [TestMethod]
        public void Vendedor_VeSuscriptoresInventarioPrendasYSoloPedidosDeVenta()
        {
            var patentes = new[] { "mnuPrendas", "mnuClientes", "mnuPlanSuscripciones",
                                    "mnuRenovacionSuscripcion", "mnuCobroSuscripcion", "mnuPedidosVenta",
                                    "mnuRecomendacionPrendas" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            // PdN13 — patente propia de Vendedor: la usa en el momento de armar el pedido.
            Assert.IsTrue(V(v, "recomendacionPrendasToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasVendedorToolStripMenuItem"));      // decisión de GerenteComercial
            Assert.IsFalse(V(v, "analisisRotacionToolStripMenuItem"));    // decisión de GerenteInventario

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
            // PdN13 (Recomendación de Prendas) vive dentro de "Analítica de Negocio": al tener
            // esa patente propia, el menú se vuelve visible aunque no tenga los otros 5 hijos.
            Assert.IsTrue(V(v, "analiticaNegocioToolStripMenuItem"));
            Assert.IsFalse(V(v, "analisisAbandonoToolStripMenuItem")); // patente propia de GerenteComercial, no de Vendedor
            // Auditoría vive en un menú aparte ahora: sin mnuAuditoria, queda oculto entero.
            Assert.IsFalse(V(v, "auditoriaToolStripMenuItem"));
            Assert.IsFalse(V(v, "bitSistemaToolStripMenuItem"));
            Assert.IsFalse(V(v, "reporteJornadaToolStripMenuItem"));
            Assert.IsFalse(V(v, "gestionToolStripMenuItem"));
        }

        [TestMethod]
        public void GerenteComercial_HeredaVendedorMasPedidosRealizadosYAnalisisAbandono()
        {
            // Composite: GerenteComercial → Vendedor. Las patentes efectivas ya vienen
            // resueltas (recursivamente) antes de llegar acá — se simula el resultado final.
            var patentes = new[] { "mnuPrendas", "mnuClientes", "mnuPlanSuscripciones",
                                    "mnuRenovacionSuscripcion", "mnuCobroSuscripcion",
                                    "mnuPedidosVenta", "mnuPedidosRealizados", "mnuAnalisisAbandono",
                                    "mnuVentasVendedor", "mnuRecomendacionPrendas" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            // PdN8 — patente propia de GerenteComercial; PdN13 heredada de Vendedor (Composite).
            Assert.IsTrue(V(v, "ventasVendedorToolStripMenuItem"));
            Assert.IsTrue(V(v, "recomendacionPrendasToolStripMenuItem"));
            Assert.IsFalse(V(v, "analisisRotacionToolStripMenuItem")); // decisión de GerenteInventario

            Assert.IsTrue(V(v, "pedidosVentaToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));

            // PdN10 — patente propia de GerenteComercial: habilita el ítem Y, de paso,
            // el menú "Analítica de Negocio" completo (antes oculto para este rol). Auditoría
            // es un menú aparte ahora (gobernado por mnuAuditoria) y sigue oculto para este rol.
            Assert.IsTrue(V(v, "analisisAbandonoToolStripMenuItem"));
            Assert.IsTrue(V(v, "analiticaNegocioToolStripMenuItem"));
            Assert.IsFalse(V(v, "auditoriaToolStripMenuItem"));
            Assert.IsFalse(V(v, "bitSistemaToolStripMenuItem"));
            Assert.IsFalse(V(v, "bitNegocioToolStripMenuItem"));
            Assert.IsFalse(V(v, "reporteJornadaToolStripMenuItem"));
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
        public void GerenteInventario_HeredaAmbosOperadores()
        {
            // Composite: GerenteInventario → OperadorLogistico + OperadorDeInventario.
            var patentes = new[] { "mnuPedidosRealizados", "mnuPrendas", "mnuStock",
                                    "mnuAnalisisRotacion", "mnuAnalisisMantenimiento", "mnuAnalisisEscasez" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));

            // PdN9/PdN11/PdN12 — patentes propias de GerenteInventario.
            Assert.IsTrue(V(v, "analisisRotacionToolStripMenuItem"));
            Assert.IsTrue(V(v, "analisisMantenimientoToolStripMenuItem"));
            Assert.IsTrue(V(v, "analisisEscasezToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasVendedorToolStripMenuItem")); // decisión de GerenteComercial

            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            // PdN9/11/12 viven dentro de "Analítica de Negocio": al tener esas patentes propias,
            // el menú se vuelve visible. Auditoría queda oculto (gobernado por mnuAuditoria, que
            // este rol no tiene).
            Assert.IsTrue(V(v, "analiticaNegocioToolStripMenuItem"));
            Assert.IsFalse(V(v, "auditoriaToolStripMenuItem"));
            Assert.IsFalse(V(v, "bitSistemaToolStripMenuItem"));
            Assert.IsFalse(V(v, "reporteJornadaToolStripMenuItem"));
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
                "prendasToolStripMenuItem",
                "clientesToolStripMenuItem", "planesToolStripMenuItem",
                "renovacionSuscripcionToolStripMenuItem", "cobroSuscripcionToolStripMenuItem",
                "pedidosVentaToolStripMenuItem", "pedidosRealizadosToolStripMenuItem",
                "usuariosToolStripMenuItem", "perfilesToolStripMenuItem", "idiomasToolStripMenuItem",
                "historialUsuariosToolStripMenuItem", "backupToolStripMenuItem", "integridadToolStripMenuItem",
                "adminUsuariosItem",
                "bitSistemaToolStripMenuItem", "bitNegocioToolStripMenuItem", "reporteJornadaToolStripMenuItem",
                "analisisAbandonoToolStripMenuItem",
                "ventasVendedorToolStripMenuItem", "analisisRotacionToolStripMenuItem",
                "analisisMantenimientoToolStripMenuItem", "analisisEscasezToolStripMenuItem",
                "recomendacionPrendasToolStripMenuItem",
                "suscriptoresToolStripMenuItem", "ventasToolStripMenuItem",
                "auditoriaToolStripMenuItem", "analiticaNegocioToolStripMenuItem",
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
            Assert.IsFalse(V(v, "auditoriaToolStripMenuItem"));
            Assert.IsFalse(V(v, "analiticaNegocioToolStripMenuItem"));
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
