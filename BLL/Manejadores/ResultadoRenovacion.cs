namespace BLL.Manejadores
{
    /// <summary>Resultado devuelto por la cadena de manejadores de renovación (PdN5).</summary>
    public class ResultadoRenovacion
    {
        /// <summary>False si ningún eslabón resolvió el caso (no debería ocurrir: Baja es fallback final).</summary>
        public bool Resuelto { get; set; }

        public BE.EstadoRenovacion Estado { get; set; }
        public string Mensaje { get; set; }

        /// <summary>0 si no se llegó a persistir ningún registro de historial (caso Pendiente).</summary>
        public int IdRenovacion { get; set; }
    }
}
