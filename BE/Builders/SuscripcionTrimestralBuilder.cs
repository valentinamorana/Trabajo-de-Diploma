using System;

namespace BE.Builders
{
    /// <summary>Builder concreto: vigencia de 3 meses desde la activación.</summary>
    public sealed class SuscripcionTrimestralBuilder : SuscripcionBuilder
    {
        public override ModalidadCobro Modalidad => ModalidadCobro.Trimestral;

        protected override DateTime CalcularVencimiento(DateTime fechaActivacion) => fechaActivacion.AddMonths(3);
    }
}
