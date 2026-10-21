using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// PN03 «Simular» — Escenario del simulador "¿Qué pasa si…?". Todo en la base mensual que usa
    /// Contabilidad (BE.AnalisisImpactoPromocion): el descuento lo reciben TODOS los clientes activos
    /// del plan; el beneficio viene de los clientes en riesgo que la promoción retiene.
    /// </summary>
    public class EscenarioPromocion
    {
        public string Nombre { get; set; }
        public int IdPlan { get; set; }
        public string NombrePlan { get; set; }
        /// <summary>Precio mensual del plan.</summary>
        public decimal PrecioPlan { get; set; }
        /// <summary>Clientes activos del plan: reciben el descuento.</summary>
        public int ClientesPlan { get; set; }
        /// <summary>Clientes en riesgo a los que apunta la promoción.</summary>
        public int ClientesObjetivo { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public int DuracionDias { get; set; }
        /// <summary>% de los clientes objetivo que la promoción retiene.</summary>
        public decimal TasaRetencion { get; set; }
        /// <summary>Meses que se estima que sigue pagando un cliente retenido.</summary>
        public decimal HorizonteMeses { get; set; }
    }

    public enum VeredictoSimulacion
    {
        SinDatos = 0,
        Conviene = 1,
        Riesgoso = 2,
        NoConviene = 3
    }

    /// <summary>Un punto de la curva de sensibilidad: beneficio neto según la tasa de retención.</summary>
    public class PuntoSensibilidad
    {
        public decimal TasaRetencion { get; set; }
        public decimal BeneficioNeto { get; set; }
    }

    /// <summary>Resultado del simulador (BLL.Inteligencia.SimuladorPromocion).</summary>
    public class ResultadoSimulacion
    {
        public EscenarioPromocion Escenario { get; set; }
        public decimal Meses { get; set; }
        public decimal CostoDescuento { get; set; }
        public decimal ClientesRetenidos { get; set; }
        public decimal IngresoRetenido { get; set; }
        public decimal BeneficioNeto { get; set; }
        /// <summary>ROI (%) = beneficio neto / costo × 100; null si la promoción no cuesta nada.</summary>
        public decimal? Roi { get; set; }
        /// <summary>Retención mínima (%) para no perder plata; null si no hay ingreso a retener.
        /// Puede ser mayor a 100: la promoción no conviene con ninguna retención.</summary>
        public decimal? TasaEquilibrio { get; set; }
        /// <summary>Margen de seguridad en pp: retención supuesta − retención de equilibrio.</summary>
        public decimal? MargenSeguridad { get; set; }
        public VeredictoSimulacion Veredicto { get; set; }
        public TextoTraducible Explicacion { get; set; }
        public List<PuntoSensibilidad> Curva { get; set; } = new List<PuntoSensibilidad>();
    }
}
