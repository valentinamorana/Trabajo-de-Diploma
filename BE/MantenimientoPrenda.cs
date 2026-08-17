using System;

namespace BE
{
    public class MantenimientoPrenda
    {
        public int       IdMantenimiento { get; set; }
        public int       IdPrenda        { get; set; }
        public string    NombrePrenda    { get; set; }
        public DateTime  FechaEntrada    { get; set; }
        public DateTime? FechaSalida     { get; set; }
        public string    Actor           { get; set; }

        public bool EstaAbierto => !FechaSalida.HasValue;

        public int? DuracionDias => FechaSalida.HasValue
            ? (int?)(FechaSalida.Value.Date - FechaEntrada.Date).TotalDays
            : null;

        // Días transcurridos desde que la prenda entró a mantenimiento, tenga o no
        // FechaSalida aún — a diferencia de DuracionDias (solo mantenimientos cerrados),
        // esto sirve para las tarjetas Kanban de los dashboards que muestran mantenimientos
        // EN CURSO. Reemplaza el cálculo "DateTime.Today - FechaEntrada" que antes vivía
        // duplicado en GUI.DashboardSupervisor y GUI.DashboardControlStock.
        public int DiasTranscurridos => (int)(DateTime.Today - FechaEntrada.Date).TotalDays;

        // Umbrales de antigüedad que los dashboards usan para resaltar mantenimientos
        // demorados. Antes duplicados (con el mismo número "mágico" pero orden de
        // comparación distinto) en GUI.DashboardSupervisor y GUI.DashboardControlStock —
        // centralizado acá para que ambos dashboards coincidan siempre en el mismo criterio.
        public NivelUrgencia NivelUrgencia =>
            DiasTranscurridos > 7 ? NivelUrgencia.Urgente
            : DiasTranscurridos >= 2 ? NivelUrgencia.Normal
            : NivelUrgencia.Reciente;
    }
}
