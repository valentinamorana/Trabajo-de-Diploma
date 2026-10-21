using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Inteligencia comercial» — Catálogo de los umbrales configurables del motor de
    /// recomendaciones y del simulador: clave, valor por defecto, rango válido y explicación.
    /// Los valores se guardan en ParametroSistema ("Comercial.&lt;Clave&gt;", sembrados por la sección
    /// 20o del script); si falta uno o no se puede leer, rige el valor por defecto de este catálogo.
    /// Gerencia los edita desde el diálogo "Umbrales" del Centro de decisiones.
    /// </summary>
    public static class CatalogoUmbrales
    {
        public const string Prefijo = "Comercial.";

        // ── Bajas y promociones de retención (ReglaChurnPlan, simulador) ──────────
        public const string ChurnAltoPct = "ChurnAltoPct";
        public const string SubidaChurnPp = "SubidaChurnPp";
        public const string DescuentoBasePct = "DescuentoBasePct";
        public const string DescuentoMaximoPct = "DescuentoMaximoPct";
        public const string TasaRetencionDefecto = "TasaRetencionDefecto";
        public const string HorizonteMeses = "HorizonteMeses";
        public const string RoiMinimoPct = "RoiMinimoPct";
        public const string MargenSeguridadPp = "MargenSeguridadPp";
        // ── Renovaciones (ReglaRenovaciones) ──────────────────────────────────────
        public const string DiasPorVencer = "DiasPorVencer";
        public const string TasaRecuperoCobroPct = "TasaRecuperoCobroPct";
        // ── Stock (ReglaEscasez, ReglaBajaRotacion) ───────────────────────────────
        public const string EscasezMinimo = "EscasezMinimo";
        public const string DemandaEscasez = "DemandaEscasez";
        public const string DiasSinRotacion = "DiasSinRotacion";
        public const string MinPrendasLiquidar = "MinPrendasLiquidar";
        public const string RecuperoLiquidacionPct = "RecuperoLiquidacionPct";
        // ── Promociones vigentes (ReglaRendimientoPromocion, Medir resultado) ─────
        public const string RendimientoMinimoPct = "RendimientoMinimoPct";
        public const string DiasMinimosEvaluacion = "DiasMinimosEvaluacion";
        public const string DiasAvisoVencimientoPromo = "DiasAvisoVencimientoPromo";
        // ── Upgrade de plan (ReglaUpgradePlan) ────────────────────────────────────
        public const string ConversionUpgradePct = "ConversionUpgradePct";
        // ── Decisiones ────────────────────────────────────────────────────────────
        public const string DiasSilencioDescarte = "DiasSilencioDescarte";

        private sealed class Definicion
        {
            public string Clave; public decimal PorDefecto; public decimal Minimo; public decimal Maximo; public int Decimales;
            public string Nombre; public string Explicacion;
        }

        private static Definicion D(string clave, decimal defecto, decimal min, decimal max, int dec, string nombre, string explicacion)
            => new Definicion { Clave = clave, PorDefecto = defecto, Minimo = min, Maximo = max, Decimales = dec, Nombre = nombre, Explicacion = explicacion };

        // Orden = orden del diálogo. Textos en español (fallback); la GUI los traduce con umbral.<clave> y umbral.<clave>.ayuda.
        private static readonly List<Definicion> Definiciones = new List<Definicion>
        {
            D(ChurnAltoPct, 5m, 0.5m, 50m, 1, "% de bajas alto",
              "Si el % de bajas de un plan en el período llega a este valor, se recomienda una promoción de retención."),
            D(SubidaChurnPp, 2m, 0.5m, 30m, 1, "Suba de bajas (pp)",
              "También se recomienda la promoción si el % de bajas subió estos puntos porcentuales contra el período anterior."),
            D(DescuentoBasePct, 10m, 5m, 50m, 0, "Descuento base (%)",
              "Descuento de partida de la promoción sugerida; crece con la gravedad de las bajas, de a 5 %."),
            D(DescuentoMaximoPct, 30m, 5m, 90m, 0, "Descuento máximo (%)",
              "Tope del descuento que puede proponer el motor."),
            D(TasaRetencionDefecto, 30m, 1m, 100m, 0, "Retención esperada por defecto (%)",
              "Retención que se supone cuando el plan todavía no tiene promociones medidas."),
            D(HorizonteMeses, 6m, 1m, 24m, 0, "Meses que paga un cliente retenido",
              "Horizonte del beneficio: cuántos meses se estima que sigue pagando un cliente retenido."),
            D(RoiMinimoPct, 20m, 0m, 500m, 0, "ROI mínimo (%)",
              "Con un ROI menor, el simulador califica la promoción como riesgosa aunque gane plata."),
            D(MargenSeguridadPp, 10m, 0m, 100m, 0, "Margen de seguridad (pp)",
              "Distancia mínima entre la retención supuesta y la de equilibrio para considerar que la promoción conviene."),
            D(DiasPorVencer, 7m, 1m, 30m, 0, "Días para vencer",
              "Se gestionan las renovaciones vencidas y las que vencen dentro de estos días."),
            D(TasaRecuperoCobroPct, 60m, 1m, 100m, 0, "Recupero por gestión de cobro (%)",
              "Parte del dinero en juego que se estima recuperar gestionando la renovación a tiempo."),
            D(EscasezMinimo, 3m, 1m, 50m, 0, "Stock mínimo por categoría y talle",
              "Con menos prendas disponibles que este mínimo y demanda, se recomienda reponer."),
            D(DemandaEscasez, 2m, 1m, 100m, 0, "Pedidos que indican demanda",
              "Pedidos del período que cuentan como demanda de una categoría y talle (además de la lista de espera)."),
            D(DiasSinRotacion, 60m, 15m, 365m, 0, "Días sin pedidos",
              "Una prenda disponible sin pedidos en el período y con al menos esta antigüedad es de baja rotación."),
            D(MinPrendasLiquidar, 3m, 1m, 100m, 0, "Mínimo de prendas para liquidar",
              "Se recomienda liquidar recién cuando hay al menos esta cantidad de prendas de baja rotación."),
            D(RecuperoLiquidacionPct, 40m, 0m, 100m, 0, "Recupero de una liquidación (%)",
              "Parte del valor de reposición que se recupera al liquidar una prenda."),
            D(RendimientoMinimoPct, 50m, 0m, 100m, 0, "Rendimiento mínimo de una promoción (%)",
              "Si una promoción vigente logra menos que este % de la retención esperada, se recomienda darla de baja."),
            D(DiasMinimosEvaluacion, 14m, 1m, 90m, 0, "Días mínimos para evaluar una promoción",
              "Antes de estos días de vigencia no se recomienda dar de baja ni extender una promoción."),
            D(DiasAvisoVencimientoPromo, 15m, 1m, 60m, 0, "Días de aviso antes del vencimiento",
              "Una promoción que rinde y vence dentro de estos días se recomienda extender."),
            D(ConversionUpgradePct, 20m, 1m, 100m, 0, "Conversión de upgrade (%)",
              "Parte de los clientes que usan todo su cupo que se estima que aceptan pasar al plan superior."),
            D(DiasSilencioDescarte, 30m, 0m, 365m, 0, "Días que se oculta una recomendación descartada",
              "Una recomendación descartada no vuelve a mostrarse durante estos días (salvo que se pidan las descartadas).")
        };

        public static IEnumerable<string> Claves => Definiciones.Select(d => d.Clave);

        private static Definicion Buscar(string clave) =>
            Definiciones.FirstOrDefault(d => string.Equals(d.Clave, clave, StringComparison.OrdinalIgnoreCase));

        public static decimal PorDefecto(string clave) => Buscar(clave)?.PorDefecto ?? 0m;

        /// <summary>Valores por defecto de todos los umbrales.</summary>
        public static Dictionary<string, decimal> ValoresPorDefecto() =>
            Definiciones.ToDictionary(d => d.Clave, d => d.PorDefecto, StringComparer.OrdinalIgnoreCase);

        /// <summary>Valor de un umbral en el contexto (o su valor por defecto).</summary>
        public static decimal Valor(BE.ContextoComercial ctx, string clave) =>
            ctx != null ? ctx.Umbral(clave, PorDefecto(clave)) : PorDefecto(clave);

        /// <summary>
        /// Combina lo guardado con los valores por defecto: un valor ausente, ilegible o fuera de rango
        /// se reemplaza por el de fábrica (la base no puede dejar al motor con un umbral absurdo).
        /// </summary>
        public static Dictionary<string, decimal> Combinar(IDictionary<string, string> guardados)
        {
            var valores = ValoresPorDefecto();
            if (guardados == null) return valores;
            foreach (var d in Definiciones)
            {
                if (!guardados.TryGetValue(d.Clave, out var texto) || string.IsNullOrWhiteSpace(texto)) continue;
                if (decimal.TryParse(texto.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
                    && v >= d.Minimo && v <= d.Maximo)
                    valores[d.Clave] = v;
            }
            return valores;
        }

        /// <summary>Umbrales completos (con rango y explicación) para el diálogo de edición.</summary>
        public static List<BE.UmbralComercial> Describir(IDictionary<string, decimal> valores) =>
            Definiciones.Select(d => new BE.UmbralComercial
            {
                Clave = d.Clave,
                Valor = valores != null && valores.TryGetValue(d.Clave, out var v) ? v : d.PorDefecto,
                PorDefecto = d.PorDefecto,
                Minimo = d.Minimo,
                Maximo = d.Maximo,
                Decimales = d.Decimales,
                Nombre = new BE.TextoTraducible("umbral." + d.Clave, d.Nombre),
                Explicacion = new BE.TextoTraducible("umbral." + d.Clave + ".ayuda", d.Explicacion)
            }).ToList();

        /// <summary>Valida un valor: clave conocida y dentro del rango. Lanza AppException traducible.</summary>
        public static void Validar(string clave, decimal valor)
        {
            var d = Buscar(clave) ?? throw new BE.AppException("err.bll.umbral.desconocido",
                "El umbral '{0}' no existe.", clave);
            if (valor < d.Minimo || valor > d.Maximo)
                throw new BE.AppException("err.bll.umbral.rango",
                    "El umbral '{0}' debe estar entre {1} y {2}.", d.Nombre, d.Minimo, d.Maximo);
            if (d.Decimales == 0 && valor != Math.Truncate(valor))
                throw new BE.AppException("err.bll.umbral.entero", "El umbral '{0}' debe ser un número entero.", d.Nombre);
        }
    }

    /// <summary>
    /// Lectura y guardado de los umbrales (ParametroSistema). Guardar exige la patente de Gerencia
    /// (la misma de "Sugerir promoción") y deja rastro en la bitácora con los valores cambiados.
    /// </summary>
    public class UmbralesComerciales
    {
        private readonly DAL.Interfaces.IInteligenciaComercialDAL dal;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();

        public UmbralesComerciales() : this(new DAL.InteligenciaComercial()) { }

        public UmbralesComerciales(DAL.Interfaces.IInteligenciaComercialDAL dal)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
        }

        /// <summary>Valores vigentes; si la tabla no se puede leer, los de fábrica (el análisis no se corta).</summary>
        public Dictionary<string, decimal> ObtenerValores()
        {
            Exigir();
            try { return CatalogoUmbrales.Combinar(dal.ObtenerParametros(CatalogoUmbrales.Prefijo)); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.UmbralesComerciales] " + ex.Message);
                return CatalogoUmbrales.ValoresPorDefecto();
            }
        }

        public List<BE.UmbralComercial> Obtener() => CatalogoUmbrales.Describir(ObtenerValores());

        // Leer o cambiar los umbrales es de Gerencia (patente de "Sugerir promoción"), como el resto de PN03.
        private static void Exigir() =>
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

        public void Guardar(string modulo, IDictionary<string, decimal> valores)
        {
            Exigir();
            if (valores == null || valores.Count == 0) return;
            foreach (var kv in valores) CatalogoUmbrales.Validar(kv.Key, kv.Value);

            // Coherencia entre umbrales: el tope del descuento no puede quedar por debajo del base.
            var combinados = ObtenerValores();
            foreach (var kv in valores) combinados[kv.Key] = kv.Value;
            if (combinados[CatalogoUmbrales.DescuentoMaximoPct] < combinados[CatalogoUmbrales.DescuentoBasePct])
                throw new BE.AppException("err.bll.umbral.descuentos",
                    "El descuento máximo no puede ser menor que el descuento base.");

            var anteriores = ObtenerValores();
            var cambios = valores.Where(kv => !anteriores.TryGetValue(kv.Key, out var a) || a != kv.Value).ToList();
            if (cambios.Count == 0) return;

            dal.GuardarParametros(CatalogoUmbrales.Prefijo,
                cambios.ToDictionary(kv => kv.Key, kv => kv.Value.ToString(CultureInfo.InvariantCulture)));
            try
            {
                bitacora.Registrar(modulo, "Umbrales comerciales modificados: " +
                    string.Join(", ", cambios.Select(kv => $"{kv.Key} = {kv.Value.ToString(CultureInfo.InvariantCulture)}")),
                    BE.Criticidad.Media);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.UmbralesComerciales] " + ex.Message); }
        }
    }
}
