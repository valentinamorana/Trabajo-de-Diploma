namespace BLL.Manejadores
{
    /// <summary>Resultado devuelto por la cadena de manejadores de cobro (PdN6).</summary>
    public class ResultadoCobro
    {
        /// <summary>False si ningún eslabón resolvió el caso (no debería ocurrir: Suspender es fallback final).</summary>
        public bool Resuelto { get; set; }

        public BE.EstadoCobro Estado { get; set; }
        public string Mensaje { get; set; }

        /// <summary>0 si no se llegó a persistir ningún registro de historial (caso Pendiente).</summary>
        public int IdCobro { get; set; }
    }
}
