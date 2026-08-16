using System;

namespace BE.Builders
{
    /// <summary>Builder concreto: vigencia de 12 meses desde la activación.</summary>
    public sealed class SuscripcionAnualBuilder : SuscripcionBuilder
    {
        public override ModalidadCobro Modalidad => ModalidadCobro.Anual;

        protected override DateTime CalcularVencimiento(DateTime fechaActivacion) => fechaActivacion.AddMonths(12);
    }
}
