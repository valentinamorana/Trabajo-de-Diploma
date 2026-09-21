using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Contratacion (PN02, permite inyección y dobles de prueba).</summary>
    public interface IContratacionDAL
    {
        // Contrataciones en estado PendientePago (la cola que ve Caja).
        List<BE.Contratacion> ObtenerPendientesDePago();

        BE.Contratacion ObtenerPorId(int idContratacion);

        // Inserta una nueva contratación en estado PendientePago. Devuelve el ID generado.
        int Alta(BE.Contratacion contratacion);

        // Suma un intento de pago fallido SOLO si la contratación sigue PendientePago. Devuelve la
        // cantidad de intentos ya registrados, o -1 si ya no estaba pendiente (otra sesión la resolvió).
        int IncrementarIntento(int idContratacion);

        // Marca la contratación como Pagada, registra el medio de pago, el comprobante y quién cobró.
        // Es un "claim" atómico: el UPDATE exige que siga PendientePago. Devuelve false si otra
        // sesión de Caja ya la resolvió (nadie más debe activar la suscripción en ese caso).
        bool ConfirmarPago(int idContratacion, int idCaja, string medioPago, string numeroComprobante,
                          decimal importe, decimal descuento, int? idPromocion);

        // Compensación: revierte un cobro recién confirmado a PendientePago (solo si está Pagada)
        // cuando la activación de la suscripción falló después del claim.
        void ReabrirPago(int idContratacion);

        // Marca la contratación como Cancelada (máximo de intentos agotado). Solo si sigue PendientePago.
        void Cancelar(int idContratacion);
    }
}
