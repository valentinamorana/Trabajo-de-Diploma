namespace BE
{
    /// <summary>PdN11 — Fila del reporte de Tiempos de Mantenimiento: una prenda con
    /// cantidad o duración promedio de mantenimientos por encima del umbral aceptable.</summary>
    public class TiempoMantenimientoPrenda
    {
        public int IdPrenda { get; set; }
        public string NombrePrenda { get; set; }
        public int CantidadMantenimientos { get; set; }
        public double? DuracionPromedioDias { get; set; }
        public int? DuracionMaximaDias { get; set; }

        /// <summary>Motivo ya formateado en español — fallback si Clave no está en el corpus.</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción (Servicios.Multiidioma) para mostrar el motivo en el idioma activo.</summary>
        public string Clave { get; set; }

        /// <summary>Argumentos para string.Format sobre el texto traducido de Clave.</summary>
        public object[] Args { get; set; }
    }
}
