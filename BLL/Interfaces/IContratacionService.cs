using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PN02 — Comercialización de la suscripción.
    ///
    /// Casos de uso definidos:
    ///   CrearContratacion()          — Venta capta al cliente y su plan elegido (CU01-VTA)
    ///   ObtenerPendientesDePago()    — Caja consulta la cola de contrataciones a cobrar
    ///   ConfirmarPago()              — Caja cobra y emite el comprobante (CU01-CAJ + CU02-CAJ),
    ///                                   dispara BLL.Cliente.ActivarSuscripcion
    ///   RegistrarIntentoFallido()    — Caja registra un intento de pago que no se concretó;
    ///                                   al tercer intento cancela automáticamente (CU03-CAJ)
    /// </summary>
    public interface IContratacionService
    {
        List<BE.Contratacion> ObtenerPendientesDePago();

        BE.Contratacion ObtenerPorId(int idContratacion);

        // Crea una contratación pendiente de pago para un cliente y un plan. Devuelve el ID generado.
        int CrearContratacion(string modulo, int idCliente, int idPlan, BE.Builders.ModalidadCobro modalidad);

        // Confirma el pago: marca la contratación como Pagada, emite el comprobante y
        // formaliza la suscripción del cliente (BLL.Cliente.ActivarSuscripcion).
        void ConfirmarPago(string modulo, BE.Contratacion contratacion, string medioPago);

        // Registra un intento de pago fallido. Si se alcanzan los 3 intentos, cancela
        // automáticamente la contratación.
        void RegistrarIntentoFallido(string modulo, BE.Contratacion contratacion);
    }
}
