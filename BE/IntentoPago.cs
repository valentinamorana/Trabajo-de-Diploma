using System;

namespace BE
{
    /// <summary>
    /// PN02 — "Registrar intento" (objeto «Intento» del diagrama): un cobro que no se concretó.
    /// Cada intento queda registrado con su número (1..3), el medio con el que se intentó, el
    /// motivo y quién de Caja lo registró. La cantidad de intentos de una contratación se cuenta
    /// sobre esta tabla (no se guarda un contador aparte).
    /// </summary>
    public class IntentoPago
    {
        public int      IdIntento       { get; set; }
        public int      IdContratacion  { get; set; }
        public int      NroIntento      { get; set; }
        public DateTime Fecha           { get; set; }
        public int?     IdMedioPago     { get; set; }
        public string   Motivo          { get; set; }
        public int      IdCaja          { get; set; }

        /// <summary>Cargados por JOIN, no persisten.</summary>
        public string NombreMedioPago { get; set; }
        public string NombreCaja      { get; set; }
    }

    /// <summary>Resultado de "Registrar intento" → ¿Alcanzó el máximo de 3 intentos?</summary>
    public class ResultadoIntentoPago
    {
        public int  NroIntento { get; set; }
        public int  Maximo     { get; set; }
        /// <summary>True si con este intento se llegó al máximo y la contratación quedó Cancelada.</summary>
        public bool Cancelada  { get; set; }
    }
}
