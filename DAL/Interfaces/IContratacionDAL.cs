using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Contratacion (PN02, permite inyección y dobles de prueba).</summary>
    public interface IContratacionDAL
    {
        // Contrataciones en estado PendientePago (la cola que consulta Caja).
        List<BE.Contratacion> ObtenerPendientesDePago();

        // Contrataciones ya resueltas (Pagadas o Canceladas), para volver a imprimir sus constancias.
        List<BE.Contratacion> ObtenerResueltas();

        BE.Contratacion ObtenerPorId(int idContratacion);

        // "Registrar contratación": inserta en PendientePago («Orden de cobro»). Devuelve el ID.
        int Alta(BE.Contratacion contratacion);

        // "Confirmar cobro" + "Emitir comprobante": marca la contratación como Pagada con el medio
        // de pago, el comprobante, quién cobró y, si pagó con tarjeta de crédito, el plan de cuotas y
        // el recargo. "Claim" atómico: el UPDATE exige que siga PendientePago; devuelve false si otra
        // sesión de Caja ya la resolvió.
        bool ConfirmarCobro(int idContratacion, int idCaja, int idMedioPago, string numeroComprobante,
                            decimal importe, decimal descuento, int? idPromocion,
                            int? idPlanCuotas = null, decimal? recargoCuotas = null,
                            decimal? creditoCambioPlan = null);

        // "Activar suscripción" («Constancia de suscripción»): guarda el período activado y, si hubo
        // "¿Referido? Sí → Acreditar crédito", a qué referente se le acreditó el beneficio.
        void RegistrarVigencia(int idContratacion, DateTime desde, DateTime hasta, int? idReferenteAcreditado = null);

        // Compensación técnica: revierte un cobro recién confirmado a PendientePago (solo si está
        // Pagada) cuando la activación de la suscripción falló después del claim.
        void ReabrirPago(int idContratacion);

        // "Anular contratación" (Caja, con motivo): claim atómico sobre Pendiente de pago.
        bool Anular(int idContratacion, string motivo, int idCaja);

        // "Registrar intento" → ¿Alcanzó el máximo? En una transacción: exige que siga
        // PendientePago, guarda el intento con el número siguiente y, si llega al máximo, la
        // cancela. Devuelve null si ya no estaba pendiente (otra sesión la resolvió).
        BE.ResultadoIntentoPago RegistrarIntentoFallido(int idContratacion, int? idMedioPago, string motivo, int idCaja, int maximo);

        List<BE.IntentoPago> ObtenerIntentos(int idContratacion);

        List<BE.MedioPago> ObtenerMediosPago();

        // Catálogo de planes de cuotas (pago con tarjeta de crédito).
        List<BE.PlanCuotas> ObtenerPlanesCuotas();

        // "Asentar desistimiento": el cliente identificado no elige plan y modalidad.
        int AltaDesistimiento(BE.DesistimientoContratacion desistimiento);

        BE.DesistimientoContratacion ObtenerDesistimiento(int idDesistimiento);
    }
}
