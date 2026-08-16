namespace BLL.Manejadores
{
    /// <summary>Petición que viaja por la cadena de manejadores de cobro (PdN6).</summary>
    public class ContextoCobro
    {
        public BE.Cliente Cliente { get; set; }
        public DecisionCobro Decision { get; set; }

        /// <summary>Solo se usa si el cobro se resuelve como Cobrado (confirma la renovación).</summary>
        public BE.Builders.ModalidadCobro Modalidad { get; set; }

        public string Actor { get; set; }
        public string Modulo { get; set; }
    }
}
