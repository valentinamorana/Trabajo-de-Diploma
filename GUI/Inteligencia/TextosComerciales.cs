using System;
using System.Linq;
using System.Text;
using Servicios.Multiidioma;

namespace GUI.Inteligencia
{
    /// <summary>
    /// Presentación del Centro de decisiones comerciales (PN03): resuelve en el idioma activo los textos
    /// que arma la BLL (BE.TextoTraducible, con argumentos anidados) y las etiquetas de los enums. Pura
    /// presentación: ningún cálculo de negocio vive acá.
    /// </summary>
    internal static class TextosComerciales
    {
        public static string Tr(string clave, string fallback, params object[] args)
        {
            try { return Traductor.Resolver(clave, fallback, args != null && args.Length > 0 ? args : null, GestorIdioma.IdiomaActual); }
            catch (FormatException) { return BE.TextoTraducible.Formatear(fallback, args); }
        }

        /// <summary>Texto en el idioma activo (los argumentos que son TextoTraducible se resuelven primero).</summary>
        public static string R(BE.TextoTraducible t)
        {
            if (t == null) return string.Empty;
            var args = t.Args?.Select(a => a is BE.TextoTraducible x ? (object)R(x) : a).ToArray();
            try { return Traductor.Resolver(t.Clave, t.Fallback, args != null && args.Length > 0 ? args : null, GestorIdioma.IdiomaActual); }
            catch (FormatException) { return t.Espanol; }   // traducción con un formato roto: se muestra en español
        }

        public static string Prioridad(BE.PrioridadRecomendacion p)
        {
            switch (p)
            {
                case BE.PrioridadRecomendacion.Alta: return Tr("cdc.prioridad.alta", "Alta");
                case BE.PrioridadRecomendacion.Media: return Tr("cdc.prioridad.media", "Media");
                default: return Tr("cdc.prioridad.baja", "Baja");
            }
        }

        public static string Confianza(BE.NivelConfianza c)
        {
            switch (c)
            {
                case BE.NivelConfianza.Alta: return Tr("cdc.confianza.alta", "Alta");
                case BE.NivelConfianza.Media: return Tr("cdc.confianza.media", "Media");
                default: return Tr("cdc.confianza.baja", "Baja");
            }
        }

        public static string Estado(BE.EstadoDecision e)
        {
            switch (e)
            {
                case BE.EstadoDecision.Aceptada: return Tr("cdc.estado.aceptada", "Aceptada");
                case BE.EstadoDecision.Descartada: return Tr("cdc.estado.descartada", "Descartada");
                case BE.EstadoDecision.Pospuesta: return Tr("cdc.estado.pospuesta", "Pospuesta");
                case BE.EstadoDecision.Ejecutada: return Tr("cdc.estado.ejecutada", "Ejecutada");
                default: return Tr("cdc.estado.pendiente", "Pendiente");
            }
        }

        /// <summary>Estado de una recomendación según su última decisión ("Nueva" si no tiene).</summary>
        /// <param name="decisionVigente">false si la última decisión ya no rige (BLL DecisionesComerciales.DecisionVigente):
        /// la recomendación se muestra para decidir, con la decisión anterior como dato.</param>
        public static string EstadoRecomendacion(BE.Recomendacion r, bool decisionVigente = true)
        {
            var d = r?.UltimaDecision;
            if (d == null) return Tr("cdc.estado.nueva", "Nueva");
            if (!decisionVigente)
                return Tr("cdc.estado.reabierta", "Para decidir (antes: {0}, {1:d})", Estado(d.Estado), d.Fecha);
            if (d.Estado == BE.EstadoDecision.Pospuesta && d.PospuestaHasta.HasValue)
                return Tr("cdc.estado.pospuestahasta", "Pospuesta hasta {0:d}", d.PospuestaHasta.Value);
            return Estado(d.Estado);
        }

        public static string Veredicto(BE.VeredictoPromocion v)
        {
            switch (v)
            {
                case BE.VeredictoPromocion.Mantener: return Tr("cdc.veredicto.mantener", "Mantener");
                case BE.VeredictoPromocion.Extender: return Tr("cdc.veredicto.extender", "Extender");
                case BE.VeredictoPromocion.DarDeBaja: return Tr("cdc.veredicto.darbaja", "Dar de baja");
                default: return Tr("cdc.veredicto.sindatos", "Sin datos suficientes");
            }
        }

        public static string VeredictoSimulacion(BE.VeredictoSimulacion v)
        {
            switch (v)
            {
                case BE.VeredictoSimulacion.Conviene: return Tr("cdc.sim.conviene", "Conviene");
                case BE.VeredictoSimulacion.Riesgoso: return Tr("cdc.sim.riesgoso", "Riesgoso");
                case BE.VeredictoSimulacion.NoConviene: return Tr("cdc.sim.noconviene", "No conviene");
                default: return Tr("cdc.sim.sindatos", "Sin datos");
            }
        }

        public static string Accion(BE.AccionRecomendacion a)
        {
            switch (a)
            {
                case BE.AccionRecomendacion.SugerirPromocion: return Tr("cdc.accion.sugerir", "Abrir la sugerencia de promoción");
                case BE.AccionRecomendacion.ExportarContactos: return Tr("cdc.accion.contactos", "Exportar contactos (CSV)");
                case BE.AccionRecomendacion.OrdenCompra: return Tr("cdc.accion.orden", "Generar la orden de compra (PDF)");
                case BE.AccionRecomendacion.SolicitarBajaPromocion: return Tr("cdc.accion.baja", "Solicitar la baja de la promoción");
                case BE.AccionRecomendacion.ExtenderPromocion: return Tr("cdc.accion.extender", "Sugerir la extensión");
                case BE.AccionRecomendacion.DarDeBajaPrendas: return Tr("cdc.accion.darbajaprendas", "Dar de baja las prendas");
                case BE.AccionRecomendacion.EnviarAMantenimiento: return Tr("cdc.accion.revision", "Mandar las prendas a revisión");
                default: return Tr("cdc.accion.nodisponible", "Acción no disponible");
            }
        }

        /// <summary>
        /// Mes en formato neutro ("2026-05"): la cultura de la app es fija (es-AR) y los idiomas no traen la
        /// suya, así que un nombre de mes saldría en español también en EN, PT o RU. Lo usan la tendencia y
        /// las cohortes (grilla, gráfico, mensajes e informe).
        /// </summary>
        public static string Mes(DateTime mes) => mes.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);

        public static string Regla(string tipo) => Tr("cdc.regla." + (tipo ?? "").ToLowerInvariant(), tipo ?? "");

        /// <summary>Valor de un KPI con su unidad.</summary>
        public static string ValorKpi(BE.UnidadKpi unidad, decimal valor)
        {
            switch (unidad)
            {
                case BE.UnidadKpi.Moneda: return valor.ToString("C0");
                case BE.UnidadKpi.Porcentaje: return valor.ToString("0.#") + " %";
                default: return valor.ToString("0.##");
            }
        }

        /// <summary>Variación con flecha: "▲ 4 pp", "▼ 3,2 %", "= estable" o "sin comparación".</summary>
        public static string Variacion(BE.KpiComercial k)
        {
            switch (k.Tendencia)
            {
                case BE.TendenciaKpi.SinDato: return Tr("cdc.kpi.sincomparacion", "sin comparación");
                case BE.TendenciaKpi.Estable: return "= " + Tr("cdc.kpi.estable", "estable");
            }
            string flecha = k.Tendencia == BE.TendenciaKpi.Sube ? "▲ " : "▼ ";
            if (k.Unidad == BE.UnidadKpi.Porcentaje) return flecha + Math.Abs(k.Variacion ?? 0m).ToString("0.#") + " pp";
            if (k.VariacionPorcentual.HasValue) return flecha + Math.Abs(k.VariacionPorcentual.Value).ToString("0.#") + " %";
            return flecha + ValorKpi(k.Unidad, Math.Abs(k.Variacion ?? 0m));
        }

        // ── Glosario de métricas (F1 del Centro de decisiones, botón Glosario e Informe PDF) ──

        /// <summary>(clave del nombre, nombre por defecto, clave base del glosario).</summary>
        public static readonly (string ClaveNombre, string Nombre, string Clave)[] Metricas =
        {
            ("kpi.mrr", "Ingreso recurrente mensual (MRR)", "glos.mrr"),
            ("kpi.churn", "% de bajas", "glos.churn"),
            ("glos.cohorte.nombre", "Retención por cohorte", "glos.cohorte"),
            ("kpi.riesgo", "Clientes en riesgo", "glos.riesgo"),
            ("kpi.ingresoriesgo", "Ingreso en riesgo", "glos.ingresoriesgo"),
            ("kpi.descontado", "Descontado por promociones", "glos.descontado"),
            ("kpi.ocupacion", "Ocupación del stock", "glos.ocupacion"),
            ("kpi.ticket", "Ticket promedio", "glos.ticket"),
            ("kpi.ingresos", "Ingresos cobrados", "glos.ingresos"),
            ("glos.proyeccion.nombre", "Proyección", "glos.proyeccion"),
            ("glos.equilibrio.nombre", "Retención de equilibrio", "glos.equilibrio"),
            ("glos.roi.nombre", "ROI", "glos.roi"),
            ("glos.mejora.nombre", "Mejora de retención", "glos.mejora"),
            ("glos.beneficioreal.nombre", "Beneficio real de una promoción", "glos.beneficioreal"),
            ("glos.campania.nombre", "Resultado de una campaña de contacto", "glos.campania"),
            ("glos.metaavance.nombre", "Avance de una meta", "glos.metaavance"),
            ("glos.metaproyeccion.nombre", "Proyección al cierre del mes (metas)", "glos.metaproyeccion"),
            ("glos.metasemaforo.nombre", "Semáforo de una meta", "glos.metasemaforo")
        };

        // Fallbacks en español (los mismos textos que traducciones.tsv).
        private static readonly string[][] GlosarioEs =
        {
            new[] { "Σ precio mensual del plan de los clientes que pagan hoy (con plan, sin pausa vigente, sin la suscripción vencida y sin suspensión por falta de pago). Al fin del período: MRR de hoy − altas + bajas posteriores al período; al inicio: MRR al fin − altas + bajas del período. Fuente: Cliente, PlanSuscripcion, HistorialRenovacion.", "Si cae, revisar las bajas y las renovaciones en juego." },
            new[] { "Bajas del período / clientes al inicio × 100. Clientes al fin del período = activos hoy − altas + bajas posteriores al período; al inicio = clientes al fin − altas + bajas del período. Fuente: HistorialRenovacion.", "Umbral: % de bajas alto (5 %) o suba de 2 pp → promoción de retención en el plan." },
            new[] { "Clientes de la cohorte (mismo mes de alta) sin baja registrada al cierre del mes k desde el alta / clientes de la cohorte × 100 (Mes 0 = el mes del alta). Pausas, vencimientos y suspensiones no son bajas, igual que en el % de bajas; el mes en curso se marca con * y no entra en el promedio. Fuente: Contratacion (primer pago), HistorialRenovacion (Resultado = Baja).", "Peor cohorte: la de menor retención al mes 3 (o al último mes que completaron al menos 2 cohortes) entre las de 4 o más clientes; el plan que más explica sus bajas lleva a su recomendación de bajas o al Simulador." },
            new[] { "Clientes que el Análisis de Abandono (PdN10) marca en riesgo hoy.", "Son el objetivo de la promoción de retención." },
            new[] { "Σ precio mensual del plan de los clientes en riesgo.", "Base del beneficio de una sugerencia de promoción." },
            new[] { "Σ descuentos aplicados en los cobros del período (Contratacion + HistorialCobro).", "Costo real de las promociones: se compara con lo que retienen." },
            new[] { "Prendas en uso / prendas activas × 100. Fuente: Prenda.", "Se le puede fijar una meta mensual: si está en riesgo, suben de prioridad dar de baja las prendas sin rotación o con mantenimiento excesivo (menos prendas activas) y el upgrade de plan (más prendas en uso). Mandarlas a revisión no la cambia." },
            new[] { "Ingresos cobrados / cantidad de cobros del período.", "Valor de un cliente: se usa en las estimaciones de las reglas." },
            new[] { "Σ importes cobrados en el período (PN02 + N01).", "Tendencia del negocio; se proyecta 3 meses." },
            new[] { "Regresión lineal por mínimos cuadrados sobre los últimos 6 meses (y = a + b·x); R² indica qué tan confiable es. Un mes parcial (el período termina antes de fin de mes, p. ej. el mes en curso) se muestra con marcador hueco y asterisco, pero no entra en la regresión.", "Con R² bajo, la proyección es orientativa." },
            new[] { "Costo del descuento / (clientes objetivo × precio × horizonte) × 100.", "La promoción conviene si retiene al menos ese %." },
            new[] { "Beneficio neto / costo del descuento × 100.", "Debajo del ROI mínimo (20 %), la promoción es riesgosa." },
            new[] { "Retención durante la vigencia − retención del período anterior (pp). Fuente: AnalisisPromociones.", "Debajo del 50 % de lo esperado → dar de baja; cumple y vence pronto → extender." },
            new[] { "Clientes en juego × mejora × precio × horizonte − total descontado.", "Se compara con el impacto estimado de la decisión (cierre del ciclo)." },
            new[] { "(respondieron − tasa base × contactados) × valor por respuesta, en los 30 días desde la ejecución (parcial hasta que cierran). Responde: renovó o cambió de plan (upgrade: pasó a un plan más caro). Tasa base: la de los planes contactados en los 30 días anteriores. Valor: precio actual del plan (bajas: × horizonte; upgrade: diferencia × horizonte). Cada cliente cuenta para la primera campaña que lo contactó.", "Es lo que la campaña logró por encima de las renovaciones de rutina; se compara con el impacto estimado." },
            new[] { "Mayor es mejor (MRR, ocupación): real / objetivo × 100. Menor es mejor (% de bajas): real / máximo permitido × 100. El real se mide con las fórmulas de los KPI, al cierre del mes (o a hoy en el mes en curso).", "Mayor es mejor: 100 % o más se cumple. Menor es mejor: hasta 100 % se cumple." },
            new[] { "Valor de hoy + pendiente × días que faltan del mes, con la pendiente de la regresión lineal por mínimos cuadrados (la misma de la tendencia) sobre el valor del indicador al cierre de cada día transcurrido. El último día del mes es el valor real. Desde el día 7 del mes; la ocupación es una foto del día y no se proyecta.", "Si la proyección no llega al objetivo, la meta está en riesgo." },
            new[] { "Mes futuro: pendiente. Mes cerrado: cumplida o no cumplida. Mes en curso: con la proyección (o el valor de hoy si no hay proyección): cumple el real y la proyección → cumplida; cumple la proyección → en camino; si no → en riesgo; un máximo acumulativo (% de bajas) ya superado → no cumplida.", "Una meta en riesgo sube un nivel la prioridad de las recomendaciones que la mueven y se avisa al iniciar sesión." }
        };

        public static string GlosarioFormula(int i) => Tr(Metricas[i].Clave + ".formula", GlosarioEs[i][0]);
        public static string GlosarioDecision(int i) => Tr(Metricas[i].Clave + ".decision", GlosarioEs[i][1]);
        public static string GlosarioNombre(int i) => Tr(Metricas[i].ClaveNombre, Metricas[i].Nombre);

        /// <summary>Glosario completo en texto (para la ayuda F1 y el botón Glosario).</summary>
        public static string GlosarioTexto()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Tr("cdc.glosario.titulo", "Glosario de métricas"));
            for (int i = 0; i < Metricas.Length; i++)
            {
                sb.AppendLine();
                sb.AppendLine("• " + GlosarioNombre(i));
                sb.AppendLine("   " + GlosarioFormula(i));
                sb.AppendLine("   → " + GlosarioDecision(i));
            }
            return sb.ToString();
        }
    }
}
