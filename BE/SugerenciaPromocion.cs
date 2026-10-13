using System;

namespace BE
{
    /// <summary>
    /// PN03 — «Sugerencia de promoción» que Gerencia (rol GerenteComercial) envía a Administración
    /// tras "Analizar métricas". Administración la acepta (crea la promoción a partir de ella:
    /// Evaluada) o la descarta con motivo (Descartada). Aplica siempre a un plan (la BLL lo exige);
    /// IdPlan es nullable solo por las sugerencias por categoría de la versión anterior, que la
    /// base cerró al discontinuarlas.
    /// </summary>
    public class SugerenciaPromocion
    {
        public int IdSugerencia { get; set; }
        public int? IdPlan { get; set; }
        public string Motivo { get; set; }
        public TipoDescuento TipoDescuentoSugerido { get; set; }
        public decimal BeneficioEstimado { get; set; }

        /// <summary>
        /// Porcentaje (0-100] de los clientes en riesgo que Gerencia espera que la promoción retenga.
        /// El beneficio estimado es el ingreso mensual en riesgo × TasaRetencion / 100: la promoción
        /// ya no supone que retiene a todos, y el supuesto queda explícito para Contabilidad.
        /// Las sugerencias anteriores a la tasa quedaron con 100 (era lo que suponían).
        /// </summary>
        public decimal TasaRetencion { get; set; } = TasaRetencionPorDefecto;

        public const decimal TasaRetencionPorDefecto = 30m;

        /// <summary>Beneficio mensual estimado: ingreso en riesgo × tasa de retención / 100.</summary>
        public static decimal CalcularBeneficio(decimal ingresoMensualEnRiesgo, decimal tasaRetencion) =>
            Math.Round(ingresoMensualEnRiesgo * tasaRetencion / 100m, 2);

        /// <summary>Ingreso mensual en riesgo del que partió el beneficio (lo inverso de CalcularBeneficio).</summary>
        public decimal IngresoEnRiesgo() => TasaRetencion > 0 ? Math.Round(BeneficioEstimado * 100m / TasaRetencion, 2) : 0m;

        public EstadoSugerencia Estado { get; set; } = EstadoSugerencia.Pendiente;
        public DateTime FechaAlta { get; set; }

        /// <summary>Métrica de la que surgió (abandono, rotación) o Manual.</summary>
        public OrigenMetrica OrigenMetrica { get; set; } = OrigenMetrica.Manual;

        /// <summary>Usuario de Gerencia que la registró. Null en datos previos.</summary>
        public int? IdUsuarioAlta { get; set; }

        /// <summary>«Constancia de descarte»: motivo obligatorio al descartarla.</summary>
        public string MotivoDescarte { get; set; }

        /// <summary>Cuándo Administración la aceptó (Evaluada) o la descartó.</summary>
        public DateTime? FechaEvaluacion { get; set; }

        /// <summary>Cargados por JOIN, no persisten.</summary>
        public string NombrePlan { get; set; }
        public string NombreUsuarioAlta { get; set; }

        public bool AplicaAPlan() => IdPlan.HasValue;

        /// <summary>Guarda de "¿Acepta la sugerencia?": solo se decide sobre una sugerencia Pendiente.</summary>
        public bool PuedeEvaluarse() => Estado == EstadoSugerencia.Pendiente;
        public bool EstaDescartada() => Estado == EstadoSugerencia.Descartada;

        /// <summary>
        /// Valor con el que Administración arranca la promoción creada desde esta sugerencia. El
        /// beneficio estimado es un importe en $: si el tipo sugerido es un PORCENTAJE y ese importe
        /// supera 100, no sirve como valor (16000 % es inválido) y se propone 10 %.
        /// </summary>
        public decimal ValorInicialPromocion() =>
            TipoDescuentoSugerido == TipoDescuento.Porcentaje && BeneficioEstimado > 100 ? 10m : BeneficioEstimado;

        /// <summary>
        /// Pendiente → Evaluada (se creó la promoción) | Descartada (fin).
        /// Evaluada → Pendiente solo como compensación técnica si el alta de la promoción falla.
        /// </summary>
        public bool TransicionValida(EstadoSugerencia destino)
        {
            switch (Estado)
            {
                case EstadoSugerencia.Pendiente:
                    return destino == EstadoSugerencia.Evaluada || destino == EstadoSugerencia.Descartada;
                case EstadoSugerencia.Evaluada:
                    return destino == EstadoSugerencia.Pendiente;
                default:
                    return false;
            }
        }
    }
}
