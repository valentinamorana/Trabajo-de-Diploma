using System;

namespace BE
{
    /// <summary>
    /// PN03 — «Solicitud de baja» que el Vendedor envía sobre una promoción Vigente y su
    /// «Resolución de baja» (Administración la aprueba o la rechaza). La observación contable
    /// de la promoción no se toca: vive en DictamenContable.
    /// </summary>
    public class SolicitudBajaPromocion
    {
        public int IdSolicitud { get; set; }
        public int IdPromocion { get; set; }
        public int IdUsuarioSolicita { get; set; }
        public string Motivo { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public EstadoSolicitudBaja Estado { get; set; } = EstadoSolicitudBaja.Pendiente;
        public int? IdUsuarioResuelve { get; set; }
        public string MotivoResolucion { get; set; }
        public DateTime? FechaResolucion { get; set; }

        /// <summary>Cargados por JOIN con Usuario, no persisten.</summary>
        public string NombreUsuarioSolicita { get; set; }
        public string NombreUsuarioResuelve { get; set; }

        public bool EstaPendiente() => Estado == EstadoSolicitudBaja.Pendiente;
        public bool FueAprobada()   => Estado == EstadoSolicitudBaja.Aprobada;
        public bool FueResuelta()   => Estado != EstadoSolicitudBaja.Pendiente;
    }
}
