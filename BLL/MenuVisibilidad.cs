using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica PURA de qué ítems del menú principal quedan visibles según las patentes
    /// efectivas del usuario — extraída de GUI.Menu.AplicarPermisos para poder testearla
    /// sin instanciar la UI (WinForms exige STA y no tiene sentido levantar un Form solo
    /// para verificar una decisión de datos). Mismo criterio de separación que
    /// BLL.PanelAlertas.EvaluarAlertas: la DECISIÓN vive acá (testeable, sin BD ni UI),
    /// GUI.Menu.AplicarPermisos solo APLICA el resultado a los ToolStripMenuItem reales
    /// buscándolos por su .Name — esta clase es la ÚNICA fuente de verdad del mapeo
    /// patente → ítem de menú; si Menu.cs y esta clase difieren, es un bug.
    /// </summary>
    public static class MenuVisibilidad
    {
        /// <summary>Ítem hoja (Name del ToolStripMenuItem) → patente que lo habilita.</summary>
        public static readonly (string Item, string Permiso)[] Hojas =
        {
            ("prendasToolStripMenuItem",              "mnuPrendas"),
            ("clientesToolStripMenuItem",              "mnuClientes"),
            ("planesToolStripMenuItem",                "mnuPlanSuscripciones"),
            ("renovacionSuscripcionToolStripMenuItem", "mnuRenovacionSuscripcion"),
            ("cobroSuscripcionToolStripMenuItem",      "mnuCobroSuscripcion"),
            ("pedidosVentaToolStripMenuItem",          "mnuPedidosVenta"),
            ("pedidosRealizadosToolStripMenuItem",     "mnuPedidosRealizados"),
            // PN02 — Comercialización de la suscripción. Nueva Contratación la ve quien ya
            // gestiona Clientes (Venta); Contrataciones Pendientes es exclusivo del rol Caja.
            ("nuevaContratacionToolStripMenuItem",      "mnuClientes"),
            ("contratacionesPendientesToolStripMenuItem", "mnuCaja"),
            // PN03 — Inteligencia comercial y promociones. El Centro de decisiones comerciales es de
            // Gerencia: lo gobierna la misma patente que Sugerir Promoción (no hace falta una nueva).
            ("centroDecisionesToolStripMenuItem",              "mnuSugerenciaPromocion"),
            ("sugerirPromocionToolStripMenuItem",              "mnuSugerenciaPromocion"),
            ("gestionPromocionesToolStripMenuItem",             "mnuPromocionesAdmin"),
            ("revisionContablePromocionesToolStripMenuItem",    "mnuPromocionesContable"),
            ("promocionesVigentesToolStripMenuItem",            "mnuPromocionesVigentes"),
            // PN01 — Control de Stock (Depósito = carril "Controlador de Stock" del diagrama).
            ("controlStockToolStripMenuItem",          "mnuControlStock"),
            // PN04 — Inspección de Devolución (Depósito = rol Deposito).
            ("inspeccionDevolucionToolStripMenuItem",  "mnuInspeccionDevolucion"),
            // Mejora opcional (no requerida por la cátedra) — ver README.
            ("listaEsperaToolStripMenuItem",           "mnuListaEspera"),
            // Bloque "Administrar" + "Sistema": todo gobernado por la patente de gestión.
            ("usuariosToolStripMenuItem",              "mnuUsuarios"),
            ("perfilesToolStripMenuItem",              "mnuUsuarios"),
            ("idiomasToolStripMenuItem",               "mnuUsuarios"),
            ("historialUsuariosToolStripMenuItem",     "mnuUsuarios"),
            ("backupToolStripMenuItem",                "mnuUsuarios"),
            ("integridadToolStripMenuItem",            "mnuUsuarios"),
            // Bitácora.
            ("bitSistemaToolStripMenuItem",            "mnuAuditoria"),
            ("bitNegocioToolStripMenuItem",             "mnuAuditoria"),
            ("reporteJornadaToolStripMenuItem",        "mnuAuditoria"),
            // Reportes de decisión (Bloque 3 + PdN10), cada uno en el menú de su rol: Ventas
            // (Recomendación de Prendas, Vendedor; Abandono y Ventas por Vendedor, Gerencia Comercial)
            // e Inventario (Rotación, Mantenimiento y Escasez, Gerencia de Inventario). Promociones
            // queda solo con PN03.
            ("analisisAbandonoToolStripMenuItem", "mnuAnalisisAbandono"),
            ("ventasVendedorToolStripMenuItem", "mnuVentasVendedor"),
            ("analisisRotacionToolStripMenuItem", "mnuAnalisisRotacion"),
            ("analisisMantenimientoToolStripMenuItem", "mnuAnalisisMantenimiento"),
            ("analisisEscasezToolStripMenuItem", "mnuAnalisisEscasez"),
            ("recomendacionPrendasToolStripMenuItem", "mnuRecomendacionPrendas"),
        };

        /// <summary>
        /// Reporte de decisión → ítem del menú que lo abre y patente que lo habilita. Los 6 abren el
        /// mismo formulario (GUI.ReporteNegocioForm) con su reporte; cada ítem también figura en
        /// <see cref="Hojas"/> (lo verifica MenuVisibilidadTests).
        /// </summary>
        public static readonly (BE.ReporteAnalitica Reporte, string Item, string Permiso)[] PatentesReportes =
        {
            (BE.ReporteAnalitica.AnalisisAbandono, "analisisAbandonoToolStripMenuItem", "mnuAnalisisAbandono"),
            (BE.ReporteAnalitica.VentasVendedor, "ventasVendedorToolStripMenuItem", "mnuVentasVendedor"),
            (BE.ReporteAnalitica.RotacionPrendas, "analisisRotacionToolStripMenuItem", "mnuAnalisisRotacion"),
            (BE.ReporteAnalitica.TiemposMantenimiento, "analisisMantenimientoToolStripMenuItem", "mnuAnalisisMantenimiento"),
            (BE.ReporteAnalitica.EscasezStock, "analisisEscasezToolStripMenuItem", "mnuAnalisisEscasez"),
            (BE.ReporteAnalitica.RecomendacionPrendas, "recomendacionPrendasToolStripMenuItem", "mnuRecomendacionPrendas"),
        };

        /// <summary>Reporte que abre el ítem del menú con ese .Name (null si no es un ítem de reporte).</summary>
        public static BE.ReporteAnalitica? ReporteDelItem(string item)
        {
            foreach (var r in PatentesReportes)
                if (string.Equals(r.Item, item, StringComparison.Ordinal)) return r.Reporte;
            return null;
        }

        /// <summary>.Name del ítem del menú que abre el reporte.</summary>
        public static string ItemDelReporte(BE.ReporteAnalitica reporte) =>
            PatentesReportes.First(r => r.Reporte == reporte).Item;

        /// <summary>Grupo (Name del menú contenedor) → hojas hijas que determinan su visibilidad (OR).</summary>
        public static readonly (string Grupo, string[] Hijos)[] Grupos =
        {
            ("suscriptoresToolStripMenuItem", new[]
            {
                "clientesToolStripMenuItem", "planesToolStripMenuItem",
                "renovacionSuscripcionToolStripMenuItem",
                "nuevaContratacionToolStripMenuItem"
            }),
            ("ventasToolStripMenuItem", new[]
            {
                "pedidosVentaToolStripMenuItem", "pedidosRealizadosToolStripMenuItem", "recomendacionPrendasToolStripMenuItem",
                "analisisAbandonoToolStripMenuItem", "ventasVendedorToolStripMenuItem"
            }),
            // PN02 — rol Caja, separado de Vendedor.
            // El cobro recurrente (N01) también es de Caja: quien vende no cobra.
            ("cajaToolStripMenuItem", new[] { "contratacionesPendientesToolStripMenuItem", "cobroSuscripcionToolStripMenuItem" }),
            // PN03 — Gerencia (GerenteComercial), Administración y Contabilidad (roles nuevos)
            // y Vendedor conviven en un mismo grupo de menú; cada hoja sigue gobernada por su
            // propia patente, el grupo solo se muestra si alguna es visible.
            ("promocionesToolStripMenuItem", new[]
            {
                "centroDecisionesToolStripMenuItem", "sugerirPromocionToolStripMenuItem", "gestionPromocionesToolStripMenuItem",
                "revisionContablePromocionesToolStripMenuItem", "promocionesVigentesToolStripMenuItem"
            }),
            // "Analítica" se partió en dos menúes de primer nivel (antes eran 9 ítems en un
            // solo dropdown plano): Auditoría (bitácoras + reporte de jornada, gobernados por
            // mnuAuditoria) y los reportes de decisión, que ahora están en el menú de cada rol.
            ("auditoriaToolStripMenuItem", new[]
            {
                "bitSistemaToolStripMenuItem", "bitNegocioToolStripMenuItem", "reporteJornadaToolStripMenuItem"
            }),
        };

        /// <summary>
        /// Dado el conjunto de patentes efectivas del usuario (NombreMenu, case-insensitive)
        /// y si es Administrador (bypass total en las 3 capas), devuelve qué ítems quedan
        /// visibles, indexados por su .Name. Replica exactamente las reglas de
        /// GUI.Menu.AplicarPermisos:
        ///   • Panel de Control: siempre visible para cualquier logueado.
        ///   • Hoja: visible si tiene la patente exacta (o es Admin).
        ///   • Inventario: visible si Prendas, Lista de Espera o Inspección de Devolución son
        ///     visibles, o tiene mnuStock (Stock no tiene ToolStripMenuItem propio en el Designer).
        ///   • Grupo genérico (Suscriptores/Ventas/Caja/Promociones/Auditoría): visible si algún hijo lo es.
        ///   • Reportes de decisión: hojas comunes (Hojas), cada una en el menú de su rol.
        ///   • Administrar/Usuarios▸/Sistema▸: gobernados por mnuUsuarios.
        /// </summary>
        public static Dictionary<string, bool> Resolver(IEnumerable<string> patentes, bool esAdmin)
        {
            var nombresMenu = new HashSet<string>(patentes ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            bool Permite(string nm) => esAdmin || nombresMenu.Contains(nm);

            var visible = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["panelControlToolStripMenuItem"] = true
            };

            foreach (var h in Hojas)
                visible[h.Item] = Permite(h.Permiso);

            visible["inventarioToolStripMenuItem"] =
                visible["prendasToolStripMenuItem"] || visible["listaEsperaToolStripMenuItem"] ||
                visible["inspeccionDevolucionToolStripMenuItem"] || visible["controlStockToolStripMenuItem"] ||
                visible["analisisRotacionToolStripMenuItem"] || visible["analisisMantenimientoToolStripMenuItem"] ||
                visible["analisisEscasezToolStripMenuItem"] ||
                Permite("mnuStock");

            foreach (var g in Grupos)
                visible[g.Grupo] = g.Hijos.Any(h => visible.TryGetValue(h, out var v) && v);

            bool tieneUsuarios = Permite("mnuUsuarios");
            visible["grpUsuarios"] = tieneUsuarios;
            visible["grpSistema"] = tieneUsuarios;
            visible["gestionToolStripMenuItem"] = tieneUsuarios;

            return visible;
        }
    }
}
