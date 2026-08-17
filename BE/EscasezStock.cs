namespace BE
{
    /// <summary>PdN12 — Fila del reporte de Escasez: una combinación Talle+Categoría cuyo
    /// stock Disponible cayó por debajo del umbral mínimo configurado.</summary>
    public class EscasezStock
    {
        public string Talle { get; set; }
        public string Categoria { get; set; }
        public int CantidadDisponible { get; set; }
        public int Umbral { get; set; }

        /// <summary>Motivo ya formateado en español — fallback si Clave no está en el corpus.</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción (Servicios.Multiidioma) para mostrar el motivo en el idioma activo.</summary>
        public string Clave { get; set; }

        /// <summary>Argumentos para string.Format sobre el texto traducido de Clave.</summary>
        public object[] Args { get; set; }
    }
}
