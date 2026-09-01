using System;

namespace BE
{
    /// <summary>
    /// PN03 — Sugerencia de promoción que Gerencia (rol GerenteComercial) envía a Administración.
    /// Administración la consume para precargar el alta de una Promocion, o la descarta y carga
    /// una manual. Aplica a UNO de estos dos, nunca ambos: IdPlan o CategoriaPrenda.
    /// </summary>
    public class SugerenciaPromocion
    {
        public int IdSugerencia { get; set; }
        public int? IdPlan { get; set; }
        public string CategoriaPrenda { get; set; }
        public string Motivo { get; set; }
        public TipoDescuento TipoDescuentoSugerido { get; set; }
        public decimal BeneficioEstimado { get; set; }
        public EstadoSugerencia Estado { get; set; } = EstadoSugerencia.Pendiente;
        public DateTime FechaAlta { get; set; }

        /// <summary>Cargado por JOIN, no persiste.</summary>
        public string NombrePlan { get; set; }

        public bool AplicaAPlan() => IdPlan.HasValue;
        public bool AplicaACategoria() => !string.IsNullOrWhiteSpace(CategoriaPrenda);
    }
}
