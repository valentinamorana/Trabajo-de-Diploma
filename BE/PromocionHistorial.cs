using System;

namespace BE
{
    /// <summary>
    /// PN03 — Una fila por cada transición de estado de una Promocion (tabla PromocionHistorial).
    /// EstadoAnterior es null en el alta. IdUsuario es el usuario en sesión que provocó la
    /// transición (en el vencimiento automático, el que consultó la lista).
    /// </summary>
    public class PromocionHistorial
    {
        public int IdHistorial { get; set; }
        public int IdPromocion { get; set; }
        public EstadoPromocion? EstadoAnterior { get; set; }
        public EstadoPromocion EstadoNuevo { get; set; }
        public int? IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string Observacion { get; set; }

        /// <summary>Cargado por JOIN con Usuario, no persiste.</summary>
        public string NombreUsuario { get; set; }
    }
}
