using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Regresión para la visibilidad del menú principal por rol (GUI.Menu.AplicarPermisos,
    /// vía BLL.MenuVisibilidad.Resolver — la lógica pura extraída para poder testearla).
    /// Los 7 roles y sus patentes reproducen EXACTAMENTE lo que siembra
    /// BD/00_Instalacion_Completa.sql, no una versión inventada — si este archivo y ese
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
            // Analítica de Negocio es una sola entrada; el Administrador ve los 6 reportes en el selector.
            Assert.IsTrue(V(v, "analiticaNegocioToolStripMenuItem"));
            CollectionAssert.AreEqual(Todos, BLL.MenuVisibilidad.ReportesVisibles(new string[0], esAdmin: true));
            Assert.IsTrue(V(v, "gestionToolStripMenuItem"));
            Assert.IsTrue(V(v, "grpUsuarios"));
            Assert.IsTrue(V(v, "grpSistema"));
            Assert.IsTrue(V(v, "usuariosToolStripMenuItem"));
            Assert.IsTrue(V(v, "perfilesToolStripMenuItem"));
            Assert.IsTrue(V(v, "idiomasToolStripMenuItem"));
            Assert.IsTrue(V(v, "backupToolStripMenuItem"));
            Assert.IsTrue(V(v, "integridadToolStripMenuItem"));
        }

        /// <summary>
        /// "Cuentas de Usuario" y "Administración de Usuarios" se fusionaron en un solo formulario
        /// (GUI.Usuarios, con pestañas): queda un único ítem de menú, gobernado por la misma patente
        /// que tenían los dos (mnuUsuarios). Sin esa patente no se ve; el ítem viejo ya no existe.
        /// </summary>
        [TestMethod]
        public void AdministracionDeUsuarios_UnSoloItem_GobernadoPorMnuUsuarios()
        {
            var con = BLL.MenuVisibilidad.Resolver(new[] { "mnuUsuarios" }, esAdmin: false);
            Assert.IsTrue(V(con, "usuariosToolStripMenuItem"));
            Assert.IsTrue(V(con, "grpUsuarios"));
            Assert.IsFalse(con.ContainsKey("adminUsuariosItem"), "El ítem 'Administración de Usuarios' separado se eliminó.");

            var sin = BLL.MenuVisibilidad.Resolver(new[] { "mnuAuditoria", "mnuClientes", "mnuCaja" }, esAdmin: false);
            Assert.IsFalse(V(sin, "usuariosToolStripMenuItem"));
            Assert.IsFalse(V(sin, "grpUsuarios"));
        }

        /// <summary>
        /// La "Vista completa" del Composite dejó de ser un formulario aparte (se abría con un botón
        /// de Perfiles y Permisos) y es una pestaña de GestorPermisos: la ve exactamente quien ve
        /// "Perfiles y Permisos", es decir quien tiene mnuUsuarios (o el Administrador).
        /// </summary>
        [TestMethod]
        public void VistaCompletaDelComposite_SoloConMnuUsuarios()
        {
            Assert.IsTrue(V(BLL.MenuVisibilidad.Resolver(new[] { "mnuUsuarios" }, esAdmin: false), "perfilesToolStripMenuItem"));
            Assert.IsTrue(V(BLL.MenuVisibilidad.Resolver(new string[0], esAdmin: true), "perfilesToolStripMenuItem"));

            // Ningún rol operativo la ve: ni Auditor ni Gerente Comercial ni Caja ni Depósito.
            foreach (var patentes in new[]
            {
                new[] { "mnuAuditoria" },
                new[] { "mnuClientes", "mnuPedidosVenta", "mnuSugerenciaPromocion", "mnuAnalisisAbandono" },
                new[] { "mnuCaja", "mnuCajaEditar", "mnuCobroSuscripcion" },
                new[] { "mnuControlStock", "mnuInspeccionDevolucion", "mnuStock" },
            })
                Assert.IsFalse(V(BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false), "perfilesToolStripMenuItem"),
                    "Perfiles y Permisos (y su pestaña Vista completa) es solo para mnuUsuarios: " + string.Join(", ", patentes));
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
            Assert.AreEqual(0, BLL.MenuVisibilidad.ReportesVisibles(new[] { "mnuAuditoria" }, esAdmin: false).Count);

            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "inventarioToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasToolStripMenuItem"));
            Assert.IsFalse(V(v, "gestionToolStripMenuItem"));
        }

        [TestMethod]
        public void Vendedor_VeSuscriptoresInventarioPrendasYSoloPedidosDeVenta()
        {
            var patentes = new[] { "mnuPrendas", "mnuClientes", "mnuPlanSuscripciones",
                                    "mnuRenovacionSuscripcion", "mnuPedidosVenta",
                                    "mnuRecomendacionPrendas" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            // PdN13 — patente propia de Vendedor: la usa en el momento de armar el pedido. Es el
            // ÚNICO reporte de su selector: Abandono y Ventas por Vendedor son de GerenteComercial,
            // Rotación/Mantenimiento/Escasez de GerenteInventario.
            CollectionAssert.AreEqual(new[] { BE.ReporteAnalitica.RecomendacionPrendas },
                BLL.MenuVisibilidad.ReportesVisibles(patentes, esAdmin: false));

            Assert.IsTrue(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsTrue(V(v, "clientesToolStripMenuItem"));
            Assert.IsTrue(V(v, "planesToolStripMenuItem"));
            Assert.IsTrue(V(v, "renovacionSuscripcionToolStripMenuItem"));
            // El cobro recurrente (N01) es de Caja: quien vende no cobra.
            Assert.IsFalse(V(v, "cobroSuscripcionToolStripMenuItem"));
            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosVentaToolStripMenuItem"));

            // Vendedor NO despacha: Pedidos Realizados no aparece.
            Assert.IsFalse(V(v, "pedidosRealizadosToolStripMenuItem"));
            // PdN13 (Recomendación de Prendas) vive dentro de "Analítica de Negocio": al tener
            // esa patente propia, la entrada se vuelve visible aunque no tenga las otras 5.
            Assert.IsTrue(V(v, "analiticaNegocioToolStripMenuItem"));
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
                                    "mnuRenovacionSuscripcion",
                                    "mnuPedidosVenta", "mnuPedidosRealizados", "mnuAnalisisAbandono",
                                    "mnuVentasVendedor", "mnuRecomendacionPrendas" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            // PdN10 y PdN8 — patentes propias de GerenteComercial; PdN13 heredada de Vendedor
            // (Composite). Rotación/Mantenimiento/Escasez son decisiones de GerenteInventario.
            CollectionAssert.AreEqual(new[]
                {
                    BE.ReporteAnalitica.AnalisisAbandono, BE.ReporteAnalitica.VentasVendedor,
                    BE.ReporteAnalitica.RecomendacionPrendas
                },
                BLL.MenuVisibilidad.ReportesVisibles(patentes, esAdmin: false));

            Assert.IsTrue(V(v, "pedidosVentaToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));

            // Con sus patentes de reporte, la entrada "Analítica de Negocio" queda visible.
            // Auditoría es un menú aparte (gobernado por mnuAuditoria) y sigue oculto para este rol.
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
        public void Deposito_VeSoloInventarioPrendas()
        {
            var v = BLL.MenuVisibilidad.Resolver(new[] { "mnuPrendas", "mnuStock" }, esAdmin: false);

            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasToolStripMenuItem"));
        }

        [TestMethod]
        public void Deposito_ConPatenteNueva_VeInspeccionDevolucion()
        {
            // PN04 — Inspección de Devolución, mismo rol (Depósito = rol Deposito).
            var v = BLL.MenuVisibilidad.Resolver(new[] { "mnuStock", "mnuInspeccionDevolucion" }, esAdmin: false);

            Assert.IsTrue(V(v, "inspeccionDevolucionToolStripMenuItem"));
            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
        }

        [TestMethod]
        public void GerenteInventario_HeredaAmbosOperadores()
        {
            // Composite: GerenteInventario → OperadorLogistico + Deposito.
            var patentes = new[] { "mnuPedidosRealizados", "mnuPrendas", "mnuStock",
                                    "mnuAnalisisRotacion", "mnuAnalisisMantenimiento", "mnuAnalisisEscasez" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            Assert.IsTrue(V(v, "inventarioToolStripMenuItem"));
            Assert.IsTrue(V(v, "prendasToolStripMenuItem"));
            Assert.IsTrue(V(v, "ventasToolStripMenuItem"));
            Assert.IsTrue(V(v, "pedidosRealizadosToolStripMenuItem"));

            // PdN9/PdN11/PdN12 — patentes propias de GerenteInventario.
            // Ventas por Vendedor y Abandono son decisiones de GerenteComercial: no están en su selector.
            CollectionAssert.AreEqual(new[]
                {
                    BE.ReporteAnalitica.RotacionPrendas, BE.ReporteAnalitica.TiemposMantenimiento,
                    BE.ReporteAnalitica.EscasezStock
                },
                BLL.MenuVisibilidad.ReportesVisibles(patentes, esAdmin: false));

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
                "bitSistemaToolStripMenuItem", "bitNegocioToolStripMenuItem", "reporteJornadaToolStripMenuItem",
                "suscriptoresToolStripMenuItem", "ventasToolStripMenuItem",
                "auditoriaToolStripMenuItem", "analiticaNegocioToolStripMenuItem",
                "grpUsuarios", "grpSistema", "gestionToolStripMenuItem"
            };

            foreach (var nombre in esperados)
                Assert.IsTrue(v.ContainsKey(nombre), $"Falta '{nombre}' en el resultado de Resolver — revisar Menu.cs vs BLL.MenuVisibilidad.");
        }

        [TestMethod]
        public void Caja_VeContratacionesPendientesYCobroDeSuscripcion()
        {
            // Caja confirma el cobro de la contratación (PN02) y el cobro recurrente (N01).
            var v = BLL.MenuVisibilidad.Resolver(new[] { "mnuCaja", "mnuCajaEditar", "mnuCobroSuscripcion" }, esAdmin: false);

            Assert.IsTrue(V(v, "cajaToolStripMenuItem"));
            Assert.IsTrue(V(v, "contratacionesPendientesToolStripMenuItem"));
            Assert.IsTrue(V(v, "cobroSuscripcionToolStripMenuItem"));
            Assert.IsFalse(V(v, "suscriptoresToolStripMenuItem"));
            Assert.IsFalse(V(v, "ventasToolStripMenuItem"));
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

        // ── Analítica de Negocio: una sola entrada + selector filtrado por patente ──────────

        private static readonly BE.ReporteAnalitica[] Todos =
        {
            BE.ReporteAnalitica.AnalisisAbandono, BE.ReporteAnalitica.VentasVendedor,
            BE.ReporteAnalitica.RotacionPrendas, BE.ReporteAnalitica.TiemposMantenimiento,
            BE.ReporteAnalitica.EscasezStock, BE.ReporteAnalitica.RecomendacionPrendas
        };

        [TestMethod]
        public void AnaliticaNegocio_CadaPatenteDeReporte_HabilitaLaEntradaYSoloSuReporte()
        {
            // La entrada del menú se ve con CUALQUIERA de las 6 patentes, y el selector ofrece
            // únicamente el reporte de esa patente (las patentes por reporte no cambian).
            var casos = new (string Patente, BE.ReporteAnalitica Reporte)[]
            {
                ("mnuAnalisisAbandono",      BE.ReporteAnalitica.AnalisisAbandono),
                ("mnuVentasVendedor",        BE.ReporteAnalitica.VentasVendedor),
                ("mnuAnalisisRotacion",      BE.ReporteAnalitica.RotacionPrendas),
                ("mnuAnalisisMantenimiento", BE.ReporteAnalitica.TiemposMantenimiento),
                ("mnuAnalisisEscasez",       BE.ReporteAnalitica.EscasezStock),
                ("mnuRecomendacionPrendas",  BE.ReporteAnalitica.RecomendacionPrendas),
            };

            foreach (var c in casos)
            {
                var v = BLL.MenuVisibilidad.Resolver(new[] { c.Patente }, esAdmin: false);
                Assert.IsTrue(V(v, "analiticaNegocioToolStripMenuItem"), c.Patente);
                CollectionAssert.AreEqual(new[] { c.Reporte },
                    BLL.MenuVisibilidad.ReportesVisibles(new[] { c.Patente }, esAdmin: false), c.Patente);
            }
        }

        [TestMethod]
        public void AnaliticaNegocio_SinPatentesDeReporte_EntradaOcultaYSelectorVacio()
        {
            var patentes = new[] { "mnuPrendas", "mnuAuditoria", "mnuUsuarios" };
            var v = BLL.MenuVisibilidad.Resolver(patentes, esAdmin: false);

            Assert.IsFalse(V(v, "analiticaNegocioToolStripMenuItem"));
            Assert.AreEqual(0, BLL.MenuVisibilidad.ReportesVisibles(patentes, esAdmin: false).Count);
            Assert.AreEqual(0, BLL.MenuVisibilidad.ReportesVisibles(null, esAdmin: false).Count);
        }

        [TestMethod]
        public void AnaliticaNegocio_SelectorRespetaElOrdenFijoYEsCaseInsensitive()
        {
            // Las patentes llegan en cualquier orden y mayúsculas: el selector sale en el orden
            // de BE.ReporteAnalitica, sin duplicados.
            var patentes = new[] { "MNURECOMENDACIONPRENDAS", "mnuanalisisabandono", "mnuAnalisisEscasez", "mnuAnalisisEscasez" };

            CollectionAssert.AreEqual(new[]
                {
                    BE.ReporteAnalitica.AnalisisAbandono, BE.ReporteAnalitica.EscasezStock,
                    BE.ReporteAnalitica.RecomendacionPrendas
                },
                BLL.MenuVisibilidad.ReportesVisibles(patentes, esAdmin: false));
        }

        [TestMethod]
        public void AnaliticaNegocio_CadaReporteTieneExactamenteUnaPatente()
        {
            // Guarda contra un reporte nuevo en BE.ReporteAnalitica sin patente asignada (no
            // aparecería nunca en el selector, ni siquiera para el Administrador).
            CollectionAssert.AreEquivalent(Todos,
                BLL.MenuVisibilidad.PatentesReportes.Select(p => p.Reporte).ToArray());
            CollectionAssert.AllItemsAreUnique(BLL.MenuVisibilidad.PatentesReportes.Select(p => p.Permiso).ToArray());
            CollectionAssert.AreEquivalent(Todos, (BE.ReporteAnalitica[])System.Enum.GetValues(typeof(BE.ReporteAnalitica)));
        }
    }
}
