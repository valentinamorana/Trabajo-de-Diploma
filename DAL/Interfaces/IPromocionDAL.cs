using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>
    /// Contrato del acceso a datos de Promocion (PN03, permite dobles de prueba).
    ///
    /// Cada transición de estado es un claim atómico (UPDATE ... WHERE Estado = @esperado) y, en
    /// la MISMA transacción, inserta la fila de PromocionHistorial que arma la BLL (y el objeto
    /// del flujo que corresponda: «Dictamen contable», cierre de las «Solicitudes de baja»). Si otra
    /// sesión ya cambió el estado, no escribe nada y devuelve false / 0.
    /// </summary>
    public interface IPromocionDAL
    {
        List<BE.Promocion> ObtenerTodas();
        // Vigentes que aplican HOY (Estado Vigente y dentro de sus fechas): las usa el cobro de PN02.
        List<BE.Promocion> ObtenerVigentes();
        // PN03 — impacto de cada promoción en los cobros del período [desde, hasta] (PN02 + N01).
        List<BE.MetricaImpactoPromocion> ObtenerImpacto(System.DateTime desde, System.DateTime hasta);
        List<BE.Promocion> ObtenerPendientesRevisionContable();
        BE.Promocion ObtenerPorId(int idPromocion);

        // Alta en EnRevisionContable + historial (EstadoAnterior null). Devuelve el ID nuevo.
        int Alta(BE.Promocion promocion, BE.PromocionHistorial historial);

        // RechazadaContabilidad → EnRevisionContable con las condiciones nuevas + historial.
        bool Reformular(BE.Promocion promocion, BE.PromocionHistorial historial);

        // estadoEsperado → historial.EstadoNuevo + historial (vencer).
        bool CambiarEstado(int idPromocion, BE.EstadoPromocion estadoEsperado, BE.PromocionHistorial historial);

        // EnRevisionContable → historial.EstadoNuevo + «Dictamen contable» + historial. Devuelve el ID del dictamen (0 si perdió el claim).
        int Dictaminar(BE.DictamenContable dictamen, BE.PromocionHistorial historial);

        // estadoEsperado (Vigente o RechazadaContabilidad) → DadaDeBaja + historial, y en la misma
        // transacción atiende las solicitudes de baja pendientes (DioDeBaja = 1, con esa respuesta).
        bool DarDeBaja(int idPromocion, BE.EstadoPromocion estadoEsperado, BE.PromocionHistorial historial, string respuestaSolicitudes);

        // «Solicitud de baja» Pendiente, solo si la promoción sigue Vigente y no tiene otra pendiente.
        // No cambia el estado de la promoción. Devuelve el ID de la solicitud (0 si no se pudo registrar).
        int SolicitarBaja(BE.SolicitudBajaPromocion solicitud);

        // Pendiente → Atendida con su respuesta (sin dar de baja). false si otra sesión ya la atendió.
        bool AtenderSolicitud(BE.SolicitudBajaPromocion atendida);

        // Solicitudes de baja Pendientes de todas las promociones (bandeja de Administración).
        List<BE.SolicitudBajaPromocion> ObtenerSolicitudesBajaPendientes();

        List<BE.PromocionHistorial> ObtenerHistorial(int idPromocion);
        List<BE.DictamenContable> ObtenerDictamenes(int idPromocion);
        List<BE.SolicitudBajaPromocion> ObtenerSolicitudesBaja(int idPromocion);
    }
}
