using System;

namespace BE
{
    /// <summary>Por qué se abrió un mantenimiento (MantenimientoPrenda.Origen, TINYINT con CHECK 0/1).</summary>
    public enum OrigenMantenimiento
    {
        /// <summary>Un usuario mandó la prenda a limpieza desde Prendas/Stock.</summary>
        Manual = 0,
        /// <summary>La abrió la devolución de un pedido (PN04, DAL.Pedido.RegistrarDevolucion).</summary>
        Devolucion = 1
    }

    public class MantenimientoPrenda
    {
        public int       IdMantenimiento { get; set; }
        public int       IdPrenda        { get; set; }
        public string    NombrePrenda    { get; set; }
        public DateTime  FechaEntrada    { get; set; }
        public DateTime? FechaSalida     { get; set; }

        /// <summary>Quién abrió el mantenimiento (foto del username; null si lo abrió una devolución).</summary>
        public string    Actor           { get; set; }

        /// <summary>Origen del mantenimiento: es lo que decide si la prenda va a la Inspección de
        /// Devolución (PN04). Antes era una marca en <see cref="Actor"/> ("Devolución"), y un usuario
        /// con ese nombre podía hacer pasar una limpieza manual por devolución.</summary>
        public OrigenMantenimiento Origen { get; set; } = OrigenMantenimiento.Manual;

        public bool EstaAbierto => !FechaSalida.HasValue;

        /// <summary>Solo los mantenimientos abiertos por una devolución van a la Inspección de
        /// Devolución y se le pueden cobrar al último cliente.</summary>
        public bool VieneDeDevolucion => Origen == OrigenMantenimiento.Devolucion;

        public int? DuracionDias => FechaSalida.HasValue
            ? (int?)(FechaSalida.Value.Date - FechaEntrada.Date).TotalDays
            : null;

        // Días transcurridos desde que la prenda entró a mantenimiento, tenga o no
        // FechaSalida aún — a diferencia de DuracionDias (solo mantenimientos cerrados),
        // esto sirve para las tarjetas Kanban de los dashboards que muestran mantenimientos
        // EN CURSO. Reemplaza el cálculo "DateTime.Today - FechaEntrada" que antes vivía
        // duplicado en GUI.DashboardDeposito y el DashboardForm genérico.
        public int DiasTranscurridos => (int)(DateTime.Today - FechaEntrada.Date).TotalDays;

        // Umbrales de antigüedad que los dashboards usan para resaltar mantenimientos
        // demorados. Antes duplicados (con el mismo número "mágico" pero orden de
        // comparación distinto) en GUI.DashboardDeposito y el DashboardForm genérico —
        // centralizado acá para que ambos dashboards coincidan siempre en el mismo criterio.
        public NivelUrgencia NivelUrgencia =>
            DiasTranscurridos > 7 ? NivelUrgencia.Urgente
            : DiasTranscurridos >= 2 ? NivelUrgencia.Normal
            : NivelUrgencia.Reciente;
    }
}
