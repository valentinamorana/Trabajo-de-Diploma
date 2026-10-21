using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Decidir» — Registro de las decisiones de Gerencia sobre las recomendaciones del motor
    /// (tabla DecisionComercial). Cada recomendación se puede:
    ///   · Aceptar (una opción: la principal o una alternativa) → Aceptada; al concretarse la acción
    ///     (p. ej. se registró la sugerencia) → Ejecutada, con la referencia (IdSugerencia, IdPromocion…).
    ///   · Descartar (motivo obligatorio) → Descartada; no se vuelve a mostrar por DiasSilencioDescarte días.
    ///   · Posponer (1 a 90 días) → Pospuesta; se oculta hasta esa fecha y después se vuelve a evaluar.
    /// Las transiciones las valida BE.DecisionComercial.TransicionValida y el DAL las aplica con un claim
    /// atómico. Toda decisión queda en la bitácora del sistema y en la de negocio (DecisionComercial).
    /// Escribir exige la patente de Gerencia (mnuSugerenciaPromocion), la misma de "Sugerir promoción".
    /// </summary>
    public class DecisionesComerciales
    {
        public const int DiasMaximoPosponer = 90;

        private readonly DAL.Interfaces.IDecisionComercialDAL dal;
        private readonly Func<DateTime> ahora;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();
        private readonly Servicios.IRegistroBitacoraNegocio bitacoraNeg = Servicios.FabricaBitacora.CrearNegocio();

        public DecisionesComerciales() : this(new DAL.DecisionComercial()) { }

        public DecisionesComerciales(DAL.Interfaces.IDecisionComercialDAL dal, Func<DateTime> ahora = null)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
            this.ahora = ahora ?? (() => DateTime.Now);
        }

        // ── Consultas ─────────────────────────────────────────────────────────────

        // Las consultas también exigen la patente de Gerencia: el historial de decisiones y a quiénes se
        // contactó son datos del Centro de decisiones, no solo de quien escribe en él.

        /// <summary>Decisiones del rango [desde, hasta] (fechas inclusivas; null = sin límite).</summary>
        public List<BE.DecisionComercial> Obtener(DateTime? desde = null, DateTime? hasta = null)
        {
            Exigir();
            return dal.Obtener(desde?.Date, hasta?.Date.AddDays(1));
        }

        /// <summary>
        /// Asocia a cada recomendación su última decisión (por Clave). Best-effort: si la tabla todavía no
        /// existe (base sin migrar) las recomendaciones se muestran igual, sin historial.
        /// </summary>
        public bool AsociarDecisiones(IEnumerable<BE.Recomendacion> recomendaciones)
        {
            Exigir();   // fuera del best-effort: sin la patente se rechaza, no se muestra «sin historial»
            List<BE.DecisionComercial> todas;
            try { todas = dal.Obtener(null, null) ?? new List<BE.DecisionComercial>(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.DecisionesComerciales] " + ex.Message);
                return false;
            }
            var ultima = todas.GroupBy(d => d.ClaveRecomendacion ?? "")
                              .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Fecha).ThenByDescending(d => d.IdDecision).First());
            foreach (var r in recomendaciones ?? Enumerable.Empty<BE.Recomendacion>())
                r.UltimaDecision = r.Clave != null && ultima.TryGetValue(r.Clave, out var d) ? d : null;
            return true;
        }

        /// <summary>
        /// ¿Se muestra la recomendación en la lista por defecto? No, si está pospuesta y no llegó la fecha,
        /// o si se descartó hace menos de <paramref name="diasSilencioDescarte"/> días.
        /// </summary>
        public static bool DebeMostrarse(BE.Recomendacion rec, DateTime hoy, int diasSilencioDescarte)
        {
            var d = rec?.UltimaDecision;
            if (d == null) return true;
            if (d.PospuestaVigente(hoy)) return false;
            if (d.Estado == BE.EstadoDecision.Descartada && (hoy.Date - d.Fecha.Date).TotalDays < diasSilencioDescarte) return false;
            return true;
        }

        /// <summary>
        /// ¿La última decisión sigue rigiendo sobre la recomendación? Una decisión abierta (aceptada o
        /// pospuesta) siempre rige. Una FINAL deja de regir cuando ya no corresponde a la situación actual:
        ///   · Descartada → pasado el silencio de <paramref name="diasSilencioDescarte"/> días (el mismo
        ///     criterio con el que DebeMostrarse la vuelve a mostrar).
        ///   · Ejecutada  → si se ejecutó antes del inicio del período analizado: la recomendación se
        ///     calculó con datos posteriores a esa acción, así que es una situación nueva.
        /// Si no rige, la recomendación se puede volver a decidir (la decisión anterior queda como dato).
        /// </summary>
        public static bool DecisionVigente(BE.DecisionComercial d, DateTime hoy, int diasSilencioDescarte, DateTime inicioAnalisis)
        {
            if (d == null) return false;
            switch (d.Estado)
            {
                case BE.EstadoDecision.Descartada:
                    return (hoy.Date - d.Fecha.Date).TotalDays < diasSilencioDescarte;
                case BE.EstadoDecision.Ejecutada:
                    // Cuenta cuándo se EJECUTÓ (una pospuesta 90 días y ejecutada hoy rige desde hoy).
                    return d.FechaVigencia.Date >= inicioAnalisis.Date;
                default:
                    return true;
            }
        }

        /// <summary>
        /// ¿Se puede Aceptar, Descartar o Posponer la recomendación? Sí, salvo que su última decisión sea
        /// final (descartada o ejecutada) y siga rigiendo (<see cref="DecisionVigente"/>).
        /// </summary>
        public static bool PuedeDecidirse(BE.Recomendacion rec, DateTime hoy, int diasSilencioDescarte, DateTime inicioAnalisis)
        {
            if (rec == null) return false;
            var d = rec.UltimaDecision;
            return d == null || !d.EsFinal() || !DecisionVigente(d, hoy, diasSilencioDescarte, inicioAnalisis);
        }

        // ── Decisiones ────────────────────────────────────────────────────────────

        /// <summary>Acepta la opción <paramref name="indiceOpcion"/> (0 = principal). Si la recomendación ya
        /// estaba aceptada y sin ejecutar, devuelve esa decisión (se puede reintentar la acción).</summary>
        public BE.DecisionComercial Aceptar(string modulo, BE.Recomendacion rec, int indiceOpcion)
        {
            Exigir();
            var opcion = OpcionElegida(rec, indiceOpcion);
            var abierta = Abierta(rec);
            if (abierta != null && abierta.Estado == BE.EstadoDecision.Aceptada
                && !rec.Opciones().Any(o => o.Descripcion?.Espanol == abierta.AccionElegida))
            {
                // Aceptada con una opción que ya no existe (las acciones cambiaron de versión): no se puede
                // ejecutar, así que se cierra con el motivo y se sigue con la decisión nueva.
                abierta.Motivo = "La opción aceptada ya no existe en esta versión; se reemplaza por una nueva decisión.";
                Persistir(abierta, BE.EstadoDecision.Descartada);
                Registrar(modulo, abierta, "descartada — " + abierta.Motivo);
                rec.UltimaDecision = abierta;
                abierta = null;
            }
            if (abierta != null && abierta.Estado == BE.EstadoDecision.Aceptada)
            {
                if (abierta.AccionElegida == opcion.Descripcion?.Espanol) return abierta;
                throw new BE.AppException("err.bll.decision.yaaceptada",
                    "La recomendación ya tiene una decisión aceptada (#{0}) con otra opción. Descártela en el historial para elegir otra.",
                    abierta.IdDecision);
            }

            var decision = abierta != null && abierta.TransicionValida(BE.EstadoDecision.Aceptada) ? abierta : Nueva(rec);
            decision.AccionElegida = opcion.Descripcion?.Espanol;
            decision.EsAlternativa = indiceOpcion > 0;
            decision.ImpactoEstimado = opcion.ImpactoEstimado;
            decision.PospuestaHasta = null;
            decision.Motivo = null;
            if (opcion.Parametros?.IdPromocion != null)
            {
                decision.TipoReferencia = BE.DecisionComercial.RefPromocion;
                decision.Referencia = opcion.Parametros.IdPromocion;
            }
            Persistir(decision, BE.EstadoDecision.Aceptada);
            Registrar(modulo, decision, $"aceptada — opción: {decision.AccionElegida} (impacto estimado {decision.ImpactoEstimado:C2})");
            return decision;
        }

        public BE.DecisionComercial Descartar(string modulo, BE.Recomendacion rec, string motivo)
        {
            Exigir();
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.decision.motivo_requerido", "Debe indicar el motivo por el que descarta la recomendación.");
            var abierta = Abierta(rec);
            var decision = abierta != null && abierta.TransicionValida(BE.EstadoDecision.Descartada) ? abierta : Nueva(rec);
            decision.Motivo = motivo.Trim();
            decision.PospuestaHasta = null;
            if (decision.IdDecision == 0)
            {
                decision.AccionElegida = rec.Principal?.Descripcion?.Espanol;
                decision.ImpactoEstimado = rec.ImpactoEstimado;
            }
            Persistir(decision, BE.EstadoDecision.Descartada);
            Registrar(modulo, decision, $"descartada — motivo: {decision.Motivo}");
            return decision;
        }

        public BE.DecisionComercial Posponer(string modulo, BE.Recomendacion rec, int dias, string motivo)
        {
            Exigir();
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            if (dias < 1 || dias > DiasMaximoPosponer)
                throw new BE.AppException("err.bll.decision.dias_posponer",
                    "Se puede posponer entre 1 y {0} días.", DiasMaximoPosponer);
            var abierta = Abierta(rec);
            if (abierta != null && abierta.Estado == BE.EstadoDecision.Aceptada)
                throw new BE.AppException("err.bll.decision.transicion",
                    "La decisión #{0} está '{1}': no se puede pasar a '{2}'.", abierta.IdDecision, abierta.Estado, BE.EstadoDecision.Pospuesta);
            var decision = abierta != null && abierta.TransicionValida(BE.EstadoDecision.Pospuesta) ? abierta : Nueva(rec);
            decision.PospuestaHasta = ahora().Date.AddDays(dias);
            decision.Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
            if (decision.IdDecision == 0)
            {
                decision.AccionElegida = rec.Principal?.Descripcion?.Espanol;
                decision.ImpactoEstimado = rec.ImpactoEstimado;
            }
            Persistir(decision, BE.EstadoDecision.Pospuesta);
            Registrar(modulo, decision, $"pospuesta hasta el {decision.PospuestaHasta:dd/MM/yyyy}" +
                (decision.Motivo != null ? $" — motivo: {decision.Motivo}" : ""));
            return decision;
        }

        /// <summary>Descarta desde el historial una decisión aceptada (sin ejecutar) o pospuesta.</summary>
        public void DescartarDecision(string modulo, int idDecision, string motivo)
        {
            Exigir();
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.decision.motivo_requerido", "Debe indicar el motivo por el que descarta la recomendación.");
            var decision = dal.ObtenerPorId(idDecision)
                ?? throw new BE.AppException("err.bll.decision.inexistente", "La decisión ya no existe.");
            decision.Motivo = motivo.Trim();
            decision.PospuestaHasta = null;
            Persistir(decision, BE.EstadoDecision.Descartada);
            Registrar(modulo, decision, $"descartada — motivo: {decision.Motivo}");
        }

        /// <summary>Aceptada → Ejecutada: la acción se concretó (con la referencia de lo que generó).</summary>
        public void MarcarEjecutada(string modulo, int idDecision, string tipoReferencia, int? referencia)
        {
            Exigir();
            var decision = dal.ObtenerPorId(idDecision)
                ?? throw new BE.AppException("err.bll.decision.inexistente", "La decisión ya no existe.");
            if (tipoReferencia != null)
            {
                decision.TipoReferencia = tipoReferencia;
                decision.Referencia = referencia;
            }
            decision.FechaEjecucion = ahora();
            Persistir(decision, BE.EstadoDecision.Ejecutada);
            Registrar(modulo, decision, "ejecutada" + (referencia.HasValue ? $" — {tipoReferencia} #{referencia}" : ""));
        }

        /// <summary>
        /// Aceptada → Ejecutada guardando además qué tocó la decisión (los clientes contactados o las prendas
        /// cambiadas, tabla DecisionComercialDetalle) en UNA transacción con el cambio de estado: así el
        /// historial puede medir después el resultado real (MedicionCampanias) y nunca queda una decisión
        /// ejecutada sin su detalle (ni al revés). <paramref name="nota"/> (opcional) queda en el motivo, p. ej.
        /// qué prendas no se pudieron cambiar y por qué. Si otra sesión la cambió, err.bll.decision.concurrente.
        /// </summary>
        public void MarcarEjecutadaConDetalle(string modulo, int idDecision, string tipoDetalle, IEnumerable<int> ids, string nota = null)
        {
            Exigir();
            var lista = (ids ?? Enumerable.Empty<int>()).Distinct().ToList();
            var decision = dal.ObtenerPorId(idDecision)
                ?? throw new BE.AppException("err.bll.decision.inexistente", "La decisión ya no existe.");
            var origen = decision.Estado;
            if (!decision.TransicionValida(BE.EstadoDecision.Ejecutada))
                throw new BE.AppException("err.bll.decision.transicion",
                    "La decisión #{0} está '{1}': no se puede pasar a '{2}'.", decision.IdDecision, origen, BE.EstadoDecision.Ejecutada);
            decision.Estado = BE.EstadoDecision.Ejecutada;
            decision.FechaEjecucion = ahora();
            if (!string.IsNullOrWhiteSpace(nota)) decision.Motivo = nota.Trim();
            if (!dal.EjecutarConDetalle(decision, origen, tipoDetalle ?? BE.DecisionComercial.DetalleCliente, lista))
            {
                decision.Estado = origen;
                throw new BE.AppException("err.bll.decision.concurrente",
                    "La decisión cambió desde otra sesión. Actualice el historial.");
            }
            Registrar(modulo, decision, (lista.Count > 0 ? $"ejecutada — {lista.Count} {tipoDetalle}(s): {string.Join(", ", lista.Take(50))}" : "ejecutada")
                                        + (string.IsNullOrWhiteSpace(nota) ? "" : " — " + nota.Trim()));
        }
        /// <summary>IDs de los clientes o prendas que tocó cada decisión (best-effort: sin tabla, vacío).</summary>
        public Dictionary<int, List<int>> ObtenerDetalle(IEnumerable<int> idsDecision, string tipoDetalle)
        {
            Exigir();
            try { return dal.ObtenerDetalle(idsDecision, tipoDetalle) ?? new Dictionary<int, List<int>>(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.DecisionesComerciales] Detalle: " + ex.Message);
                return new Dictionary<int, List<int>>();
            }
        }

        /// <summary>Guarda el resultado real (medido por el sistema o cargado por Gerencia).</summary>
        public void RegistrarResultado(string modulo, int idDecision, decimal resultadoReal)
        {
            Exigir();
            var decision = dal.ObtenerPorId(idDecision)
                ?? throw new BE.AppException("err.bll.decision.inexistente", "La decisión ya no existe.");
            if (!decision.AdmiteResultado())
                throw new BE.AppException("err.bll.decision.sinresultado",
                    "Solo una decisión aceptada o ejecutada tiene un resultado que medir.");
            if (!dal.RegistrarResultado(idDecision, Math.Round(resultadoReal, 2), ahora()))
                throw new BE.AppException("err.bll.decision.concurrente",
                    "La decisión cambió desde otra sesión. Actualice el historial.");
            decision.ResultadoReal = Math.Round(resultadoReal, 2);
            Registrar(modulo, decision, $"resultado real {decision.ResultadoReal:C2} (estimado {decision.ImpactoEstimado:C2})");
        }

        // ── Auxiliares ────────────────────────────────────────────────────────────

        private static void Exigir() =>
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

        private static BE.OpcionRecomendacion OpcionElegida(BE.Recomendacion rec, int indice)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            var opciones = rec.Opciones();
            if (indice < 0 || indice >= opciones.Count)
                throw new BE.AppException("err.bll.decision.opcion", "Elija una de las opciones de la recomendación.");
            return opciones[indice];
        }

        // Decisión abierta (no final) sobre la misma situación: se continúa en vez de duplicarla.
        private static BE.DecisionComercial Abierta(BE.Recomendacion rec) =>
            rec.UltimaDecision != null && !rec.UltimaDecision.EsFinal() ? rec.UltimaDecision : null;

        private BE.DecisionComercial Nueva(BE.Recomendacion rec) => new BE.DecisionComercial
        {
            Fecha = ahora(),
            IdUsuario = BLLHelper.ResolverUsuarioActivo(),
            TipoRegla = rec.TipoRegla,
            ClaveRecomendacion = rec.Clave,
            Prioridad = rec.Prioridad,
            Titulo = rec.Titulo?.Espanol,
            Diagnostico = rec.Diagnostico?.Espanol,
            Estado = BE.EstadoDecision.Pendiente
        };

        // Valida la transición y la escribe: alta si es nueva, claim atómico si ya existía.
        private void Persistir(BE.DecisionComercial decision, BE.EstadoDecision destino)
        {
            var origen = decision.Estado;
            if (!decision.TransicionValida(destino))
                throw new BE.AppException("err.bll.decision.transicion",
                    "La decisión #{0} está '{1}': no se puede pasar a '{2}'.", decision.IdDecision, origen, destino);
            if (decision.IdDecision == 0 && HayOtraAbierta(decision.ClaveRecomendacion))
                throw new BE.AppException("err.bll.decision.abierta",
                    "La recomendación ya tiene una decisión abierta (aceptada o pospuesta). Actualice el análisis o resuélvala en el historial.");
            decision.Estado = destino;
            if (decision.IdDecision == 0)
            {
                decision.IdDecision = dal.Alta(decision);
                return;
            }
            if (!dal.CambiarEstado(decision, origen))
            {
                decision.Estado = origen;
                throw new BE.AppException("err.bll.decision.concurrente",
                    "La decisión cambió desde otra sesión. Actualice el historial.");
            }
        }

        // Una sola decisión abierta (Aceptada o Pospuesta) por situación: la respalda el índice filtrado
        // UX_DecisionComercial_Abierta (sección 20p). Si la tabla no se puede leer, decide el índice.
        private bool HayOtraAbierta(string clave)
        {
            try
            {
                return (dal.Obtener(null, null) ?? new List<BE.DecisionComercial>())
                    .Any(d => d.ClaveRecomendacion == clave && !d.EsFinal() && d.Estado != BE.EstadoDecision.Pendiente);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.DecisionesComerciales] " + ex.Message);
                return false;
            }
        }

        private void Registrar(string modulo, BE.DecisionComercial d, string detalle)
        {
            string texto = $"Decisión comercial #{d.IdDecision} ({d.TipoRegla}) {detalle}: {d.Titulo}";
            try { bitacora.Registrar(modulo, texto, BE.Criticidad.Baja); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.DecisionesComerciales] " + ex.Message); }
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.DecisionComercial, texto);
        }
    }
}
