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

        /// <summary>Ítems retirados de la interfaz (módulos no implementados): siempre ocultos.</summary>
        public static readonly string[] SiempreOcultos = { "outfitsToolStripMenuItem", "categoriasToolStripMenuItem" };

        /// <summary>Grupo (Name del menú contenedor) → hojas hijas que determinan su visibilidad (OR).</summary>
        public static readonly (string Grupo, string[] Hijos)[] Grupos =
        {
            ("suscriptoresToolStripMenuItem", new[]
            {
                "clientesToolStripMenuItem", "planesToolStripMenuItem",
                "renovacionSuscripcionToolStripMenuItem", "cobroSuscripcionToolStripMenuItem"
            }),
            ("ventasToolStripMenuItem", new[] { "pedidosVentaToolStripMenuItem", "pedidosRealizadosToolStripMenuItem" }),
            ("bitacoraToolStripMenuItem", new[]
            {
                "bitSistemaToolStripMenuItem", "bitNegocioToolStripMenuItem", "reporteJornadaToolStripMenuItem",
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
        ///   • Outfits/Categorías: siempre ocultos (módulo no implementado), sin excepción.
        ///   • Inventario: visible si Prendas es visible o tiene mnuStock (Stock no tiene
        ///     ToolStripMenuItem propio en el Designer).
        ///   • Grupo genérico (Suscriptores/Ventas/Analítica): visible si algún hijo lo es.
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

            foreach (var oculto in SiempreOcultos)
                visible[oculto] = false;

            visible["inventarioToolStripMenuItem"] = visible["prendasToolStripMenuItem"] || Permite("mnuStock");

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
