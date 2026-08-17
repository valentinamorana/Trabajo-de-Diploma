namespace BLL.Manejadores
{
    /// <summary>Resultado devuelto por la cadena de manejadores de cobro (PdN6).</summary>
    public class ResultadoCobro
    {
        /// <summary>False si ningún eslabón resolvió el caso (no debería ocurrir: Suspender es fallback final).</summary>
        public bool Resuelto { get; set; }

        public BE.EstadoCobro Estado { get; set; }

        /// <summary>Mensaje ya formateado en español — fallback si Clave no está en el corpus.</summary>
        public string Mensaje { get; set; }

        /// <summary>Clave de traducción (Servicios.Multiidioma) para mostrar el mensaje en el idioma activo.</summary>
        public string Clave { get; set; }

        /// <summary>Argumentos para string.Format sobre el texto traducido de Clave.</summary>
        public object[] Args { get; set; }

        /// <summary>0 si no se llegó a persistir ningún registro de historial (caso Pendiente).</summary>
        public int IdCobro { get; set; }
    }
}
