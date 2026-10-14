namespace BE
{
    /// <summary>
    /// Los 6 reportes de la pantalla "Analítica de Negocio" (Bloque 3 + PdN10). Antes cada uno
    /// era un formulario propio con su ítem de menú; ahora conviven en un solo formulario con
    /// un selector, y cada reporte sigue gobernado por SU patente (BLL.MenuVisibilidad.
    /// PatentesReportes): el selector solo ofrece los que el usuario tiene permitidos.
    /// El orden de la enumeración es el orden en que aparecen en el selector.
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
