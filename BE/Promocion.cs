using System;

namespace BE
{
    /// <summary>
    /// PN03 — Métricas, promociones y toma de decisiones. Una Promocion aplica a UNO de estos
    /// dos, nunca ambos: un plan de suscripción (IdPlan) o una categoría de prenda (CategoriaPrenda,
    /// string libre — no hay tabla Categoria propia, ver BE.Prenda.Categoria). Las promociones por
    /// categoría son informativas: no descuentan en el cobro de la suscripción.
    ///
    /// Los objetos del flujo que antes eran columnas de esta tabla viven en tablas propias (3FN):
    /// «Dictamen contable» (DictamenContable), «Solicitud/Resolución de baja»
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

        public int? IdPlan { get; set; }
        public string CategoriaPrenda { get; set; }

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

        public bool AplicaAPlan() => IdPlan.HasValue;
        public bool AplicaACategoria() => !string.IsNullOrWhiteSpace(CategoriaPrenda);

        /// <summary>Se aplica en el cobro de PN02: Vigente y dentro de sus fechas. Una Vencida nunca aplica.</summary>
        public bool EstaVigente() => Estado == EstadoPromocion.Vigente
            && DateTime.Today >= FechaInicio.Date && DateTime.Today <= FechaFin.Date;

        public bool EstaVencida() => Estado == EstadoPromocion.Vencida;

        /// <summary>Evento (c) de la región interrumpible: llegó la FechaFin estando Vigente.</summary>
        public bool DebeVencer(DateTime hoy) => Estado == EstadoPromocion.Vigente && FechaFin.Date < hoy.Date;

        public bool EsFinal() => Estado == EstadoPromocion.Desactivada || Estado == EstadoPromocion.Descartada
                              || Estado == EstadoPromocion.Vencida;

        public bool PuedeAprobarseORechazarseContable() => Estado == EstadoPromocion.EnRevisionContable;

        /// <summary>Guarda de separación de funciones: quien creó la promoción no la dictamina.</summary>
        public bool PuedeDictaminarla(int idUsuario) => !IdUsuarioAlta.HasValue || IdUsuarioAlta.Value != idUsuario;

        public bool PuedeSolicitarseBaja() => Estado == EstadoPromocion.Vigente;
        public bool PuedeResolverseBaja() => Estado == EstadoPromocion.BajaSolicitada;
        public bool PuedeDesactivarseDirecto() => Estado == EstadoPromocion.Vigente;
        public bool PuedeReformularse() => Estado == EstadoPromocion.RechazadaContabilidad;
        public bool PuedeDescartarse() => Estado == EstadoPromocion.RechazadaContabilidad;

        /// <summary>
        /// Advertencia de "Analizar margen e impacto": otra promoción Vigente (o con baja
        /// solicitada, que puede volver a Vigente si se rechaza la baja) del MISMO plan cuyas fechas se cruzan.
        /// </summary>
        public bool SeSuperponeCon(Promocion otra) =>
            otra != null && otra.IdPromocion != IdPromocion && IdPlan.HasValue && otra.IdPlan == IdPlan
            && (otra.Estado == EstadoPromocion.Vigente || otra.Estado == EstadoPromocion.BajaSolicitada)
            && otra.FechaInicio.Date <= FechaFin.Date && FechaInicio.Date <= otra.FechaFin.Date;

        /// <summary>
        /// Máquina de estados del diagrama de actividad de PN03:
        ///   EnRevisionContable    → Vigente | RechazadaContabilidad  (¿Aprueba?)
        ///   RechazadaContabilidad → EnRevisionContable (Reformular) | Descartada
        ///   Vigente               → BajaSolicitada | Desactivada (directa) | Vencida (FechaFin)
        ///   BajaSolicitada        → Desactivada (baja aprobada) | Vigente (baja rechazada)
        /// Desactivada, Descartada y Vencida son finales.
        /// </summary>
        public bool TransicionValida(EstadoPromocion destino)
        {
            switch (Estado)
            {
                case EstadoPromocion.EnRevisionContable:
                    return destino == EstadoPromocion.Vigente || destino == EstadoPromocion.RechazadaContabilidad;
                case EstadoPromocion.RechazadaContabilidad:
                    return destino == EstadoPromocion.EnRevisionContable || destino == EstadoPromocion.Descartada;
                case EstadoPromocion.Vigente:
                    return destino == EstadoPromocion.BajaSolicitada || destino == EstadoPromocion.Desactivada
                        || destino == EstadoPromocion.Vencida;
                case EstadoPromocion.BajaSolicitada:
                    return destino == EstadoPromocion.Desactivada || destino == EstadoPromocion.Vigente;
                default:
                    return false;
            }
        }
    }
}
