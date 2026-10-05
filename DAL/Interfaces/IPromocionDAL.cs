using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>
    /// Contrato del acceso a datos de Promocion (PN03, permite dobles de prueba).
    ///
    /// Cada transición de estado es un claim atómico (UPDATE ... WHERE Estado = @esperado) y, en
    /// la MISMA transacción, inserta la fila de PromocionHistorial que arma la BLL (y el objeto
    /// del flujo que corresponda: «Dictamen contable», «Solicitud/Resolución de baja»). Si otra
    /// sesión ya cambió el estado, no escribe nada y devuelve false / 0.
    /// </summary>
    public interface IPromocionDAL
    {
        List<BE.Promocion> ObtenerTodas();
        // Vigentes que aplican HOY (Estado Vigente y dentro de sus fechas): las usa el cobro de PN02.
        List<BE.Promocion> ObtenerVigentes();
        List<BE.Promocion> ObtenerPendientesRevisionContable();
        BE.Promocion ObtenerPorId(int idPromocion);

        // Alta en EnRevisionContable + historial (EstadoAnterior null). Devuelve el ID nuevo.
        int Alta(BE.Promocion promocion, BE.PromocionHistorial historial);

        // RechazadaContabilidad → EnRevisionContable con las condiciones nuevas + historial.
        bool Reformular(BE.Promocion promocion, BE.PromocionHistorial historial);

        // estadoEsperado → historial.EstadoNuevo + historial (desactivar, descartar, vencer).
        bool CambiarEstado(int idPromocion, BE.EstadoPromocion estadoEsperado, BE.PromocionHistorial historial);

        // EnRevisionContable → historial.EstadoNuevo + «Dictamen contable» + historial. Devuelve el ID del dictamen (0 si perdió el claim).
        int Dictaminar(BE.DictamenContable dictamen, BE.PromocionHistorial historial);

        // Vigente → BajaSolicitada + «Solicitud de baja» + historial. Devuelve el ID de la solicitud (0 si perdió el claim).
        int SolicitarBaja(BE.SolicitudBajaPromocion solicitud, BE.PromocionHistorial historial);

        // BajaSolicitada → historial.EstadoNuevo + «Resolución de baja» (la solicitud pendiente se cierra) + historial.
        bool ResolverBaja(BE.SolicitudBajaPromocion resolucion, BE.PromocionHistorial historial);

        List<BE.PromocionHistorial> ObtenerHistorial(int idPromocion);
        List<BE.DictamenContable> ObtenerDictamenes(int idPromocion);
        List<BE.SolicitudBajaPromocion> ObtenerSolicitudesBaja(int idPromocion);
    }
}
