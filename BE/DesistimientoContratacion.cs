using System;

namespace BE
{
    /// <summary>
    /// PN02 — "¿Elige plan y modalidad? No → Asentar desistimiento". El cliente ya fue
    /// identificado y vio los planes, pero no contrata. No genera una Contratacion (no hubo
    /// nada que cobrar): se registra aparte, con el plan y la modalidad que estaba considerando
    /// si los había elegido, y el motivo que comunicó.
    /// </summary>
    public class DesistimientoContratacion
    {
        public int      IdDesistimiento { get; set; }
        public int      IdCliente       { get; set; }
        public int?     IdPlan          { get; set; }
        public Builders.ModalidadCobro? Modalidad { get; set; }
        public string   Motivo          { get; set; }
        public DateTime Fecha           { get; set; }
        public int      IdVendedor      { get; set; }

        /// <summary>Cargados por JOIN, no persisten.</summary>
        public string NombreCliente  { get; set; }
        public string NombrePlan     { get; set; }
        public string NombreVendedor { get; set; }
    }
}
