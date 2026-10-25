namespace BE
{
    /// <summary>
    /// Los 6 reportes de decisión (Bloque 3 + PdN10). Todos se muestran con el mismo formulario
    /// (GUI.ReporteNegocioForm), pero cada uno tiene su ítem en el menú de su rol y su patente
    /// (BLL.MenuVisibilidad.PatentesReportes).
    /// </summary>
    public enum ReporteAnalitica
    {
        /// <summary>PdN10 — clientes en riesgo de abandono (mnuAnalisisAbandono).</summary>
        AnalisisAbandono = 0,

        /// <summary>PdN8 — desempeño de pedidos por vendedor (mnuVentasVendedor).</summary>
        VentasVendedor = 1,

        /// <summary>PdN9 — prendas de baja o alta demanda (mnuAnalisisRotacion).</summary>
        RotacionPrendas = 2,

        /// <summary>PdN11 — prendas con mantenimiento excesivo (mnuAnalisisMantenimiento).</summary>
        TiemposMantenimiento = 3,

        /// <summary>PdN12 — Talle+Categoría por debajo del umbral de stock (mnuAnalisisEscasez).</summary>
        EscasezStock = 4,

        /// <summary>PdN13 — prendas afines al historial de un cliente (mnuRecomendacionPrendas).</summary>
        RecomendacionPrendas = 5
    }
}
