using System;

namespace BE.Builders
{
    /// <summary>Builder concreto: vigencia de 1 mes desde la activación.</summary>
    public sealed class SuscripcionMensualBuilder : SuscripcionBuilder
    {
        public override ModalidadCobro Modalidad => ModalidadCobro.Mensual;

        protected override DateTime CalcularVencimiento(DateTime fechaActivacion) => fechaActivacion.AddMonths(1);
    }
}
