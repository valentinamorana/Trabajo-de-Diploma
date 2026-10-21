using System;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Analizar» — Cálculo puro de los KPI y de su variación contra el período anterior de igual
    /// duración. Reglas de la variación:
    ///   · Porcentajes (% de bajas, ocupación): la variación se expresa en puntos porcentuales (pp) y se
    ///     considera estable si es menor a 0,5 pp.
    ///   · Montos y cantidades: variación relativa (%) = (actual − anterior) / |anterior| × 100; estable si
    ///     es menor al 1 %. Si el anterior es 0 no hay variación relativa (se informa "desde cero").
    ///   · Favorable: sube y "subir es bueno" (ingresos), o baja y "subir es malo" (bajas, descuentos).
    /// </summary>
    public static class CalculoKpi
    {
        public const decimal ToleranciaPp = 0.5m;
        public const decimal ToleranciaRelativaPct = 1m;

        /// <summary>% de bajas = bajas / clientes al inicio × 100 (2 decimales); null si no había clientes.</summary>
        public static decimal? TasaBajas(int bajas, int clientesInicio) =>
            clientesInicio > 0 ? Math.Round(bajas * 100m / clientesInicio, 2) : (decimal?)null;

        /// <summary>Clientes al inicio del período reconstruidos: activos al final − altas + bajas.</summary>
        public static int ClientesAlInicio(int activosAlFinal, int altas, int bajas) =>
            Math.Max(0, activosAlFinal - altas + bajas);

        /// <summary>Clientes al FIN del período reconstruidos desde hoy: activos hoy − altas posteriores al
        /// período + bajas posteriores al período (si el período termina hoy, no hay posteriores).</summary>
        public static int ClientesAlFinal(int activosHoy, int altasPosteriores, int bajasPosteriores) =>
            Math.Max(0, activosHoy - altasPosteriores + bajasPosteriores);

        /// <summary>MRR al FIN del período: MRR de hoy − MRR de las altas posteriores + MRR de las bajas posteriores.</summary>
        public static decimal MrrAlFinal(decimal mrrHoy, decimal mrrAltasPosteriores, decimal mrrBajasPosteriores) =>
            Math.Max(0m, mrrHoy - mrrAltasPosteriores + mrrBajasPosteriores);

        /// <summary>MRR al inicio: MRR al final − MRR de las altas + MRR de las bajas (no negativo).</summary>
        public static decimal MrrAlInicio(decimal mrrFinal, decimal mrrAltas, decimal mrrBajas) =>
            Math.Max(0m, mrrFinal - mrrAltas + mrrBajas);

        /// <summary>Porcentaje parte / total × 100 (2 decimales); 0 si el total es 0.</summary>
        public static decimal Porcentaje(decimal parte, decimal total) =>
            total > 0 ? Math.Round(parte * 100m / total, 2) : 0m;

        public static BE.KpiComercial Crear(string clave, BE.TextoTraducible nombre, BE.UnidadKpi unidad,
                                            decimal valor, decimal? anterior, bool subirEsBueno)
        {
            var kpi = new BE.KpiComercial
            {
                Clave = clave, Nombre = nombre, Unidad = unidad, Valor = valor,
                ValorAnterior = anterior, SubirEsBueno = subirEsBueno
            };
            if (!anterior.HasValue)
            {
                kpi.Tendencia = BE.TendenciaKpi.SinDato;
                kpi.Interpretacion = new BE.TextoTraducible("kpi.interp.sindato",
                    "{0}: sin datos del período anterior para comparar.", nombre);
                return kpi;
            }

            decimal diferencia = valor - anterior.Value;
            kpi.Variacion = Math.Round(diferencia, 2);
            kpi.VariacionPorcentual = anterior.Value != 0
                ? Math.Round(diferencia * 100m / Math.Abs(anterior.Value), 2)
                : (decimal?)null;

            bool estable = unidad == BE.UnidadKpi.Porcentaje
                ? Math.Abs(diferencia) < ToleranciaPp
                : (anterior.Value == 0 ? diferencia == 0 : Math.Abs(kpi.VariacionPorcentual.Value) < ToleranciaRelativaPct);
            if (estable)
            {
                kpi.Tendencia = BE.TendenciaKpi.Estable;
                kpi.Interpretacion = new BE.TextoTraducible("kpi.interp.estable",
                    "{0} se mantuvo estable respecto del período anterior.", nombre);
                return kpi;
            }

            bool sube = diferencia > 0;
            kpi.Tendencia = sube ? BE.TendenciaKpi.Sube : BE.TendenciaKpi.Baja;
            kpi.EsFavorable = sube == subirEsBueno;
            var calificacion = kpi.EsFavorable.Value
                ? new BE.TextoTraducible("kpi.favorable", "favorable")
                : new BE.TextoTraducible("kpi.desfavorable", "desfavorable");

            if (unidad == BE.UnidadKpi.Porcentaje)
                kpi.Interpretacion = sube
                    ? new BE.TextoTraducible("kpi.interp.sube.pp", "{0} subió {1:0.#} pp respecto del período anterior ({2}).", nombre, Math.Abs(diferencia), calificacion)
                    : new BE.TextoTraducible("kpi.interp.baja.pp", "{0} bajó {1:0.#} pp respecto del período anterior ({2}).", nombre, Math.Abs(diferencia), calificacion);
            else if (anterior.Value == 0)
                kpi.Interpretacion = new BE.TextoTraducible("kpi.interp.desdecero",
                    "{0} no registraba valores en el período anterior y ahora sí ({1}).", nombre, calificacion);
            else
                kpi.Interpretacion = sube
                    ? new BE.TextoTraducible("kpi.interp.sube.pct", "{0} subió un {1:0.#} % respecto del período anterior ({2}).", nombre, Math.Abs(kpi.VariacionPorcentual.Value), calificacion)
                    : new BE.TextoTraducible("kpi.interp.baja.pct", "{0} bajó un {1:0.#} % respecto del período anterior ({2}).", nombre, Math.Abs(kpi.VariacionPorcentual.Value), calificacion);
            return kpi;
        }
    }
}
