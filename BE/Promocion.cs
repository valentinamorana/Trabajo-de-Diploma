using System;

namespace BE
{
    /// <summary>
    /// PN03 — Métricas, promociones y toma de decisiones. Una Promocion aplica a UNO de estos
    /// dos, nunca ambos: un plan de suscripción (IdPlan) o una categoría de prenda para compra
    /// definitiva (CategoriaPrenda, string libre — no hay tabla Categoria propia, ver
    /// BE.Prenda.Categoria).
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
        public string Observacion { get; set; }
        public string MotivoBaja { get; set; }
        public int? IdSugerenciaOrigen { get; set; }

        public DateTime FechaAlta { get; set; }

        /// <summary>Cargado por JOIN, no persiste.</summary>
        public string NombrePlan { get; set; }

        public bool AplicaAPlan() => IdPlan.HasValue;
        public bool AplicaACategoria() => !string.IsNullOrWhiteSpace(CategoriaPrenda);
        public bool EstaVigente() => Estado == EstadoPromocion.Vigente
            && DateTime.Today >= FechaInicio.Date && DateTime.Today <= FechaFin.Date;

        public bool PuedeAprobarseORechazarseContable() => Estado == EstadoPromocion.EnRevisionContable;
        public bool PuedeSugerirseBaja() => Estado == EstadoPromocion.Vigente;
        public bool PuedeResolverseBaja() => Estado == EstadoPromocion.BajaSolicitada;
        public bool PuedeDesactivarseDirecto() => Estado == EstadoPromocion.Vigente;
    }
}
