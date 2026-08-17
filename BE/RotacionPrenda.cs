namespace BE
{
    /// <summary>PdN9 — Fila del reporte de Rotación de Prendas: una prenda de alta o baja
    /// demanda según la cantidad de pedidos que la incluyeron.</summary>
    public class RotacionPrenda
    {
        public int IdPrenda { get; set; }
        public string NombrePrenda { get; set; }
        public string Categoria { get; set; }
        public int CantidadPedidos { get; set; }

        /// <summary>Motivo ya formateado en español — fallback si Clave no está en el corpus.</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción (Servicios.Multiidioma) para mostrar el motivo en el idioma activo.</summary>
        public string Clave { get; set; }

        /// <summary>Argumentos para string.Format sobre el texto traducido de Clave.</summary>
        public object[] Args { get; set; }
    }
}
