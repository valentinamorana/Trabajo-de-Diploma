namespace BE
{
    /// <summary>PdN10 — Resultado de evaluar una BLL.Estrategias.EstrategiaRiesgo sobre un cliente.</summary>
    public class ResultadoRiesgo
    {
        public bool EnRiesgo { get; set; }

        /// <summary>Motivo ya formateado en español — fallback si Clave no está en el corpus.</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción (Servicios.Multiidioma) para mostrar el motivo en el idioma activo.</summary>
        public string Clave { get; set; }

        /// <summary>Argumentos para string.Format sobre el texto traducido de Clave.</summary>
        public object[] Args { get; set; }
    }
}
