using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PdN6 — Gestión de cobro de suscripciones. Orquesta la cadena de manejadores
    /// (BLL.Manejadores) que resuelve el intento de cobro: confirma la renovación,
    /// aplica un período de gracia o suspende nuevos pedidos.
    /// </summary>
    public interface ICobroService
    {
        // Procesa un intento de cobro para el cliente indicado según la decisión tomada.
        // El actor (quién cobró) lo toma la BLL de la sesión activa.
        Manejadores.ResultadoCobro Procesar(
            string modulo, BE.Cliente cliente, Manejadores.DecisionCobro decision,
            BE.Builders.ModalidadCobro modalidad, int? idMedioPago = null);

        // Un cobro por ID (comprobante) y los medios de pago vigentes (N01).
        BE.Cobro ObtenerCobro(int idCobro);
        List<BE.MedioPago> ObtenerMediosPago();

        // Clientes a los que hoy corresponde procesarles un cobro (con plan, vencidos o
        // próximos a vencer y sin contratación PN02 pendiente de pago).
        List<BE.Cliente> ObtenerElegibles();

        // Total del próximo cobro del cliente (período con descuento + cargos por daño/pérdida pendientes).
        BE.PrevisualizacionCobro PrevisualizarCobro(int idCliente,
            BE.Builders.ModalidadCobro modalidad = BE.Builders.ModalidadCobro.Mensual);
    }
}
