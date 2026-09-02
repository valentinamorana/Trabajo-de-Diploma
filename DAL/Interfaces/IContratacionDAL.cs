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

        // Suma un intento de pago fallido. Devuelve la cantidad de intentos ya registrados.
        int IncrementarIntento(int idContratacion);

        // Marca la contratación como Pagada, registra el medio de pago, el comprobante y quién cobró.
        void ConfirmarPago(int idContratacion, int idCaja, string medioPago, string numeroComprobante);

        // Marca la contratación como Cancelada (máximo de intentos agotado).
        void Cancelar(int idContratacion);
    }
}
