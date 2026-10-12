using System;

namespace BE
{
    /// <summary>
    /// PN03 — Métricas, promociones y toma de decisiones. Una Promocion aplica siempre a un plan
    /// de suscripción (IdPlan, obligatorio) y descuenta en el cobro de ese plan. Las promociones
    /// por categoría de prenda se discontinuaron: no descontaban en ningún cobro.
    ///
    /// Los objetos del flujo que antes eran columnas de esta tabla viven en tablas propias (3FN):
    /// «Dictamen contable» (DictamenContable), «Solicitud de baja» y su «Respuesta»
    /// (SolicitudBajaPromocion) y cada transición (PromocionHistorial).
    /// </summary>
    public class Promocion
    {
        public int IdPromocion { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public TipoDescuento TipoDescuento { get; set; }
        public decimal Valor { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public EstadoPromocion Estado { get; set; } = EstadoPromocion.EnRevisionContable;

        public int IdPlan { get; set; }

        public decimal MargenEstimado { get; set; }
        public string ImpactoEconomico { get; set; }
        public int? IdSugerenciaOrigen { get; set; }

        /// <summary>Usuario de Administración que la creó (no puede dictaminarla). Null en datos previos.</summary>
        public int? IdUsuarioAlta { get; set; }

        public DateTime FechaAlta { get; set; }

        /// <summary>Cargados por JOIN, no persisten.</summary>
        public string NombrePlan { get; set; }
        public string NombreUsuarioAlta { get; set; }

        /// <summary>Observación del último «Dictamen contable» (JOIN con DictamenContable), no persiste.</summary>
        public string Observacion { get; set; }

        /// <summary>Motivo de la «Solicitud de baja» pendiente (JOIN con SolicitudBajaPromocion), no persiste.</summary>
        public string MotivoBaja { get; set; }

        /// <summary>Se aplica en el cobro de PN02: Vigente y dentro de sus fechas. Una Vencida nunca aplica.</summary>
        public bool EstaVigente() => Estado == EstadoPromocion.Vigente
            && DateTime.Today >= FechaInicio.Date && DateTime.Today <= FechaFin.Date;

        public bool EstaVencida() => Estado == EstadoPromocion.Vencida;

        /// <summary>Evento (c) de la región interrumpible: llegó la FechaFin estando Vigente.</summary>
        public bool DebeVencer(DateTime hoy) => Estado == EstadoPromocion.Vigente && FechaFin.Date < hoy.Date;

        public bool EsFinal() => Estado == EstadoPromocion.DadaDeBaja || Estado == EstadoPromocion.Vencida;

        public bool PuedeAprobarseORechazarseContable() => Estado == EstadoPromocion.EnRevisionContable;

        /// <summary>Guarda de separación de funciones: quien creó la promoción no la dictamina.</summary>
        public bool PuedeDictaminarla(int idUsuario) => !IdUsuarioAlta.HasValue || IdUsuarioAlta.Value != idUsuario;

        /// <summary>Hay una «Solicitud de baja» pendiente (MotivoBaja solo se carga si la hay).</summary>
        public bool TieneSolicitudBajaPendiente() => !string.IsNullOrWhiteSpace(MotivoBaja);

        /// <summary>El Vendedor pide la baja de una Vigente que todavía no tiene una solicitud pendiente.</summary>
        public bool PuedeSolicitarseBaja() => Estado == EstadoPromocion.Vigente && !TieneSolicitudBajaPendiente();

        public bool PuedeReformularse() => Estado == EstadoPromocion.RechazadaContabilidad;

        /// <summary>"Dar de baja" (Administración): una promoción Vigente o una Rechazada por Contabilidad.</summary>
        public bool PuedeDarseDeBaja() => Estado == EstadoPromocion.Vigente || Estado == EstadoPromocion.RechazadaContabilidad;

        /// <summary>
        /// Advertencia de "Analizar margen e impacto": otra promoción Vigente del MISMO plan cuyas
        /// fechas se cruzan.
        /// </summary>
        public bool SeSuperponeCon(Promocion otra) =>
            otra != null && otra.IdPromocion != IdPromocion && otra.IdPlan == IdPlan
            && otra.Estado == EstadoPromocion.Vigente
            && otra.FechaInicio.Date <= FechaFin.Date && FechaInicio.Date <= otra.FechaFin.Date;

        /// <summary>
        /// Máquina de estados del diagrama de actividad de PN03:
        ///   EnRevisionContable    → Vigente | RechazadaContabilidad  (¿Aprueba?)
        ///   RechazadaContabilidad → EnRevisionContable (Reformular) | DadaDeBaja
        ///   Vigente               → DadaDeBaja | Vencida (FechaFin)
        /// DadaDeBaja y Vencida son finales. Pedir la baja no cambia el estado: la promoción sigue
        /// Vigente hasta que Administración atiende la solicitud.
        /// </summary>
        public bool TransicionValida(EstadoPromocion destino)
        {
            switch (Estado)
            {
                case EstadoPromocion.EnRevisionContable:
                    return destino == EstadoPromocion.Vigente || destino == EstadoPromocion.RechazadaContabilidad;
                case EstadoPromocion.RechazadaContabilidad:
                    return destino == EstadoPromocion.EnRevisionContable || destino == EstadoPromocion.DadaDeBaja;
                case EstadoPromocion.Vigente:
                    return destino == EstadoPromocion.DadaDeBaja || destino == EstadoPromocion.Vencida;
                default:
                    return false;
            }
        }
    }
}
