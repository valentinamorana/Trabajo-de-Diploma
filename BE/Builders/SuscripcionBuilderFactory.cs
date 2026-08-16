using System;

namespace BE.Builders
{
    /// <summary>Resuelve el SuscripcionBuilder concreto para una modalidad de cobro.</summary>
    public static class SuscripcionBuilderFactory
    {
        public static SuscripcionBuilder Crear(ModalidadCobro modalidad)
        {
            switch (modalidad)
            {
                case ModalidadCobro.Mensual:    return new SuscripcionMensualBuilder();
                case ModalidadCobro.Trimestral: return new SuscripcionTrimestralBuilder();
                case ModalidadCobro.Anual:      return new SuscripcionAnualBuilder();
                default:
                    throw new ArgumentOutOfRangeException(nameof(modalidad), modalidad, "Modalidad de cobro desconocida.");
            }
        }
    }
}
