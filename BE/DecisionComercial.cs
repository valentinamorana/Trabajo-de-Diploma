using System;

namespace BE
{
    /// <summary>
    /// Estado de una decisión comercial (tabla DecisionComercial, CHECK Estado IN (0..4)):
    ///   Pendiente  → estado conceptual inicial de una recomendación sin decidir.
    ///   Aceptada   → el usuario eligió una opción (la principal o una alternativa).
    ///   Descartada → fin: no se hará (motivo obligatorio).
    ///   Pospuesta  → se vuelve a evaluar el día PospuestaHasta.
    ///   Ejecutada  → fin: la acción se concretó (p. ej. se registró la sugerencia; Referencia la identifica).
    /// </summary>
    public enum EstadoDecision
    {
        Pendiente = 0,
        Aceptada = 1,
        Descartada = 2,
        Pospuesta = 3,
        Ejecutada = 4
    }

    /// <summary>
    /// PN03 — «Decisión comercial»: lo que Gerencia decidió sobre una recomendación del motor. Cierra el
    /// ciclo analizar → recomendar/decidir → aplicar → medir: guarda el impacto estimado y, cuando se
    /// puede medir (o lo carga el usuario), el resultado real.
    /// </summary>
    public class DecisionComercial
    {
        public int IdDecision { get; set; }
        public DateTime Fecha { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string TipoRegla { get; set; }
        public string ClaveRecomendacion { get; set; }
        public PrioridadRecomendacion Prioridad { get; set; }
        public string Titulo { get; set; }
        public string Diagnostico { get; set; }
        /// <summary>Descripción de la opción elegida (en español, como la bitácora).</summary>
        public string AccionElegida { get; set; }
        /// <summary>true si se eligió una alternativa y no la acción principal.</summary>
        public bool EsAlternativa { get; set; }
        public decimal ImpactoEstimado { get; set; }
        public EstadoDecision Estado { get; set; } = EstadoDecision.Pendiente;
        /// <summary>Motivo del descarte o de la postergación (obligatorio al descartar).</summary>
        public string Motivo { get; set; }
        /// <summary>Qué identifica Referencia: Sugerencia, Promocion, SolicitudBaja…</summary>
        public string TipoReferencia { get; set; }
        public int? Referencia { get; set; }
        public DateTime? PospuestaHasta { get; set; }
        /// <summary>Cuándo se ejecutó (pasó a Ejecutada); null si todavía no. Es la fecha que cuenta para
        /// saber si la decisión sigue rigiendo y desde cuándo se mide su resultado (no la de creación).</summary>
        public DateTime? FechaEjecucion { get; set; }
        public DateTime? FechaEvaluacion { get; set; }
        public decimal? ResultadoReal { get; set; }

        /// <summary>No se persiste: true si el resultado medido es parcial (la ventana de medición todavía
        /// no cerró). Lo completa BLL.Inteligencia.InteligenciaComercial.MedirDecisiones.</summary>
        public bool ResultadoParcial { get; set; }

        /// <summary>Fecha desde la que rige la decisión ejecutada: la de ejecución (o la de creación si es anterior a ese dato).</summary>
        public DateTime FechaVigencia => FechaEjecucion ?? Fecha;

        public const string RefSugerencia = "Sugerencia";
        public const string RefPromocion = "Promocion";
        public const string RefSolicitudBaja = "SolicitudBaja";

        // Qué tocó una decisión ejecutada (tabla DecisionComercialDetalle): los clientes contactados en
        // una campaña (para medir cuántos renovaron) o las prendas dadas de baja / mandadas a revisión.
        public const string DetalleCliente = "Cliente";
        public const string DetallePrenda = "Prenda";

        /// <summary>Máquina de estados de la decisión.</summary>
        public bool TransicionValida(EstadoDecision destino)
        {
            switch (Estado)
            {
                case EstadoDecision.Pendiente:
                    return destino == EstadoDecision.Aceptada || destino == EstadoDecision.Descartada
                        || destino == EstadoDecision.Pospuesta;
                case EstadoDecision.Pospuesta:
                    // Al volver a evaluarla se puede aceptar, descartar o posponer otra vez.
                    return destino == EstadoDecision.Aceptada || destino == EstadoDecision.Descartada
                        || destino == EstadoDecision.Pospuesta;
                case EstadoDecision.Aceptada:
                    // Se concreta (Ejecutada) o se revierte antes de ejecutarla (Descartada, con motivo).
                    return destino == EstadoDecision.Ejecutada || destino == EstadoDecision.Descartada;
                default:
                    return false;   // Descartada y Ejecutada son finales
            }
        }

        public bool EsFinal() => Estado == EstadoDecision.Descartada || Estado == EstadoDecision.Ejecutada;

        /// <summary>Pospuesta y todavía no llegó la fecha de volver a evaluarla (el día PospuestaHasta vuelve a
        /// mostrarse: es el día en que los avisos al iniciar sesión la recuerdan).</summary>
        public bool PospuestaVigente(DateTime hoy) =>
            Estado == EstadoDecision.Pospuesta && PospuestaHasta.HasValue && PospuestaHasta.Value.Date > hoy.Date;

        /// <summary>Solo una decisión aceptada o ejecutada tiene un resultado que medir.</summary>
        public bool AdmiteResultado() => Estado == EstadoDecision.Aceptada || Estado == EstadoDecision.Ejecutada;

        /// <summary>Desvío real − estimado ($); null sin resultado real.</summary>
        public decimal? Desvio => ResultadoReal.HasValue ? ResultadoReal.Value - ImpactoEstimado : (decimal?)null;
    }
}
