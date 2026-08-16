namespace BLL.Manejadores
{
    /// <summary>Petición que viaja por la cadena de manejadores de renovación (PdN5).</summary>
    public class ContextoRenovacion
    {
        public BE.Cliente Cliente { get; set; }
        public DecisionRenovacion Decision { get; set; }

        /// <summary>Solo se usa si Decision == CambiarPlan.</summary>
        public int? IdPlanNuevo { get; set; }

        public BE.Builders.ModalidadCobro Modalidad { get; set; }
        public string Actor { get; set; }
        public string Modulo { get; set; }
    }
}
