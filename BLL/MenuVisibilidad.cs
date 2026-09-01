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
            // PN03 — Métricas, promociones y toma de decisiones.
            ("sugerirPromocionToolStripMenuItem",              "mnuSugerenciaPromocion"),
            ("gestionPromocionesToolStripMenuItem",             "mnuPromocionesAdmin"),
            ("revisionContablePromocionesToolStripMenuItem",    "mnuPromocionesContable"),
            ("promocionesVigentesToolStripMenuItem",            "mnuPromocionesVigentes"),
            // PN04 — Inspección de Devolución (Depósito = OperadorDeInventario).
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
            ("adminUsuariosItem",                      "mnuUsuarios"),
            // Bitácora.
            ("bitSistemaToolStripMenuItem",            "mnuAuditoria"),
            ("bitNegocioToolStripMenuItem",             "mnuAuditoria"),
            ("reporteJornadaToolStripMenuItem",        "mnuAuditoria"),
            // PdN10 — patente propia: no todo el que audita necesita decidir sobre retención.
            ("analisisAbandonoToolStripMenuItem",      "mnuAnalisisAbandono"),
            // Bloque 3 (Idea de Negocio) — PdN8, 9, 11, 12, 13: cada uno con patente propia,
            // mismo criterio que PdN10 (decisión de negocio, no auditoría genérica).
            ("ventasVendedorToolStripMenuItem",        "mnuVentasVendedor"),
            ("analisisRotacionToolStripMenuItem",      "mnuAnalisisRotacion"),
            ("analisisMantenimientoToolStripMenuItem", "mnuAnalisisMantenimiento"),
            ("analisisEscasezToolStripMenuItem",       "mnuAnalisisEscasez"),
            ("recomendacionPrendasToolStripMenuItem",  "mnuRecomendacionPrendas"),
        };

        /// <summary>Grupo (Name del menú contenedor) → hojas hijas que determinan su visibilidad (OR).</summary>
        public static readonly (string Grupo, string[] Hijos)[] Grupos =
        {
            ("suscriptoresToolStripMenuItem", new[]
            {
                "clientesToolStripMenuItem", "planesToolStripMenuItem",
                "renovacionSuscripcionToolStripMenuItem", "cobroSuscripcionToolStripMenuItem",
                "nuevaContratacionToolStripMenuItem"
            }),
            ("ventasToolStripMenuItem", new[] { "pedidosVentaToolStripMenuItem", "pedidosRealizadosToolStripMenuItem" }),
            // PN02 — rol Caja, separado de Vendedor.
            ("cajaToolStripMenuItem", new[] { "contratacionesPendientesToolStripMenuItem" }),
            // PN03 — Gerencia (GerenteComercial), Administración y Contabilidad (roles nuevos)
            // y Vendedor conviven en un mismo grupo de menú; cada hoja sigue gobernada por su
            // propia patente, el grupo solo se muestra si al menos una de las 4 es visible.
            ("promocionesToolStripMenuItem", new[]
            {
                "sugerirPromocionToolStripMenuItem", "gestionPromocionesToolStripMenuItem",
                "revisionContablePromocionesToolStripMenuItem", "promocionesVigentesToolStripMenuItem"
            }),
            // "Analítica" se partió en dos menúes de primer nivel (antes eran 9 ítems en un
            // solo dropdown plano): Auditoría (bitácoras + reporte de jornada, gobernados por
            // mnuAuditoria) y Analítica de Negocio (los 6 reportes de decisión comercial del
            // Bloque 3 + PdN10, cada uno con patente propia).
            ("auditoriaToolStripMenuItem", new[]
            {
                "bitSistemaToolStripMenuItem", "bitNegocioToolStripMenuItem", "reporteJornadaToolStripMenuItem"
            }),
            ("analiticaNegocioToolStripMenuItem", new[]
            {
                "analisisAbandonoToolStripMenuItem", "ventasVendedorToolStripMenuItem",
                "analisisRotacionToolStripMenuItem", "analisisMantenimientoToolStripMenuItem",
                "analisisEscasezToolStripMenuItem", "recomendacionPrendasToolStripMenuItem"
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
        ///   • Grupo genérico (Suscriptores/Ventas/Auditoría/Analítica de Negocio): visible si algún hijo lo es.
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
                visible["inspeccionDevolucionToolStripMenuItem"] || Permite("mnuStock");

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
