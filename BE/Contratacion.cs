using System;

namespace BE
{
    /// <summary>
    /// PN02 — Comercialización de la suscripción. Estado intermedio entre "el cliente eligió
    /// un plan" (Vendedor registra la contratación: «Orden de cobro») y "la suscripción quedó
    /// vigente" (Caja confirma el cobro, emite el comprobante y se activa la suscripción). El
    /// Comprobante se guarda como columnas propias (número y fecha de emisión, relación 1:1).
    /// </summary>
    public class Contratacion
    {
        public const int MaxIntentosPago = 3;

        public int IdContratacion { get; set; }
        public int IdCliente { get; set; }
        public int IdPlan { get; set; }
        public int IdVendedor { get; set; }
        public int? IdCaja { get; set; }
        public Builders.ModalidadCobro Modalidad { get; set; }
        public EstadoContratacion Estado { get; set; } = EstadoContratacion.PendientePago;
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaResolucion { get; set; }

        /// <summary>Medio con el que se concretó el cobro (FK al catálogo MedioPago).</summary>
        public int? IdMedioPago { get; set; }

        public string NumeroComprobante { get; set; }
        public DateTime? FechaComprobante { get; set; }

        /// <summary>Importe efectivamente cobrado (plan menos el descuento aplicado). Null si aún no se cobró.</summary>
        public decimal? Importe { get; set; }

        /// <summary>Descuento aplicado en el cobro (promoción vigente o crédito por referido; nunca ambos).</summary>
        public decimal? DescuentoAplicado { get; set; }

        /// <summary>Promoción vigente aplicada al cobro (PN03), o null si no se aplicó ninguna.</summary>
        public int? IdPromocion { get; set; }

        /// <summary>Upgrade (BLL.Politicas.PoliticaCambioPlan): crédito por los días no usados del plan anterior,
        /// ya descontado del Importe. Null si el cobro no fue un cambio a un plan superior.</summary>
        public decimal? CreditoCambioPlan { get; set; }

        /// <summary>Motivo por el que Caja anuló la contratación antes de cobrarla (el cliente se
        /// arrepintió, error de carga). Null si se canceló por agotar los intentos de pago.</summary>
        public string MotivoAnulacion { get; set; }

        /// <summary>Pago en cuotas (solo Tarjeta de crédito): plan de cuotas elegido (FK a PlanCuotas),
        /// o null si se pagó con un medio que no financia.</summary>
        public int? IdPlanCuotas { get; set; }

        /// <summary>Recargo por financiación cobrado sobre el Importe (en pesos). Se guarda porque el
        /// porcentaje del plan puede cambiar después y el comprobante se puede reimprimir.</summary>
        public decimal? RecargoCuotas { get; set; }

        /// <summary>Cargados por JOIN con PlanCuotas, no persisten.</summary>
        public int? CantidadCuotas { get; set; }
        public decimal? RecargoPorcentaje { get; set; }

        /// <summary>Total que abonó el cliente: importe del cobro + recargo por cuotas.</summary>
        public decimal? TotalAbonado => Importe.HasValue ? Importe.Value + (RecargoCuotas ?? 0m) : (decimal?)null;

        /// <summary>Valor de cada cuota (null si no se pagó en cuotas).</summary>
        public decimal? ValorCuota => TotalAbonado.HasValue && (CantidadCuotas ?? 1) > 1
            ? Math.Round(TotalAbonado.Value / CantidadCuotas.Value, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        /// <summary>Período que activó el cobro («Constancia de suscripción»). Se guarda porque el
        /// vencimiento del cliente cambia con cada renovación y la constancia se puede reimprimir.</summary>
        public DateTime? VigenciaDesde { get; set; }
        public DateTime? VigenciaHasta { get; set; }

        /// <summary>«¿Referido? Sí»: cliente referente al que se le acreditó el beneficio con este cobro.</summary>
        public int? IdReferenteAcreditado { get; set; }

        /// <summary>Intentos de cobro fallidos registrados (se cuentan en ContratacionIntentoPago).</summary>
        public int IntentosPago { get; set; }

        /// <summary>Cargados por JOIN, no persisten.</summary>
        public string NombreCliente { get; set; }
        public string NombrePlan { get; set; }
        public string NombreMedioPago { get; set; }
        public string NombreVendedor { get; set; }
        public string NombreCaja { get; set; }
        public string NombreReferenteAcreditado { get; set; }

        /// <summary>Precio mensual pactado al registrar la contratación (se guarda). Caja cobra este
        /// precio aunque el plan cambie mientras la contratación espera en la cola.</summary>
        public decimal? PrecioMensual { get; set; }

        /// <summary>Precio mensual del plan al consultar (JOIN con PlanSuscripcion), no persiste.</summary>
        public decimal MontoPlan { get; set; }

        /// <summary>Alias del nombre del medio de pago (compatibilidad con pantallas y reportes).</summary>
        public string MedioPago => NombreMedioPago;

        public bool PuedeCobrarse() => Estado == EstadoContratacion.PendientePago;
        public bool EstaPagada()    => Estado == EstadoContratacion.Pagada;
        public bool EstaCancelada() => Estado == EstadoContratacion.Cancelada;
        public bool TieneIntentos() => IntentosPago > 0;

        /// <summary>
        /// Máquina de estados del diagrama de actividad de PN02:
        ///   PendientePago → Pagada    (¿Se concreta el pago? Sí → Confirmar cobro)
        ///   PendientePago → Cancelada (¿Alcanzó el máximo de 3 intentos? Sí → Cancelar contratación)
        ///   Pagada        → PendientePago, solo como compensación técnica si la activación falla.
        /// Cancelada es final.
        /// </summary>
        public bool TransicionValida(EstadoContratacion destino)
        {
            switch (Estado)
            {
                case EstadoContratacion.PendientePago:
                    return destino == EstadoContratacion.Pagada || destino == EstadoContratacion.Cancelada;
                case EstadoContratacion.Pagada:
                    return destino == EstadoContratacion.PendientePago;
                default:
                    return false;
            }
        }
    }
}
