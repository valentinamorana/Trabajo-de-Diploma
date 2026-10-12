using System;

namespace BE
{
    /// <summary>
    /// PN03 — «Solicitud de baja» que el Vendedor envía sobre una promoción Vigente. No cambia el
    /// estado de la promoción: queda Pendiente hasta que Administración la atiende con una
    /// «Respuesta a la solicitud». Si Administración está de acuerdo, da de baja la promoción
    /// (DioDeBaja); si no, la promoción sigue Vigente. Dar de baja una promoción también cierra
    /// sus solicitudes pendientes. La observación contable vive en DictamenContable.
    /// </summary>
    public class SolicitudBajaPromocion
    {
        public int IdSolicitud { get; set; }
        public int IdPromocion { get; set; }
        public int IdUsuarioSolicita { get; set; }
        public string Motivo { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public EstadoSolicitudBaja Estado { get; set; } = EstadoSolicitudBaja.Pendiente;

        /// <summary>Usuario de Administración que la atendió y cuándo (null mientras está Pendiente).</summary>
        public int? IdUsuarioResuelve { get; set; }
        public DateTime? FechaResolucion { get; set; }

        /// <summary>«Respuesta a la solicitud»: obligatoria al atenderla.</summary>
        public string Respuesta { get; set; }

        /// <summary>true si al atenderla Administración dio de baja la promoción.</summary>
        public bool DioDeBaja { get; set; }

        /// <summary>Cargados por JOIN con Usuario y Promocion, no persisten.</summary>
        public string NombreUsuarioSolicita { get; set; }
        public string NombreUsuarioResuelve { get; set; }
        public string NombrePromocion { get; set; }

        public bool EstaPendiente() => Estado == EstadoSolicitudBaja.Pendiente;
        public bool FueAtendida()   => Estado == EstadoSolicitudBaja.Atendida;
    }
}
