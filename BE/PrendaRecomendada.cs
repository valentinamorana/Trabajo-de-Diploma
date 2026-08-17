namespace BE
{
    /// <summary>PdN13 — Una prenda Disponible sugerida para un cliente, junto con el motivo
    /// de la recomendación (coincide con su categoría y/o color preferidos).</summary>
    public class PrendaRecomendada
    {
        public Prenda Prenda { get; set; }

        /// <summary>Cuántos de los 2 criterios (categoría, color) coinciden con la preferencia del cliente.</summary>
        public int Coincidencias { get; set; }

        /// <summary>Motivo ya formateado en español — fallback si Clave no está en el corpus.</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción (Servicios.Multiidioma) para mostrar el motivo en el idioma activo.</summary>
        public string Clave { get; set; }

        /// <summary>Argumentos para string.Format sobre el texto traducido de Clave.</summary>
        public object[] Args { get; set; }
    }
}
