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
        Manejadores.ResultadoCobro Procesar(
            string modulo, BE.Cliente cliente, Manejadores.DecisionCobro decision,
            BE.Builders.ModalidadCobro modalidad, string actor);

        // Devuelve el historial de intentos de cobro de un cliente.
        List<BE.Cobro> ObtenerHistorial(int idCliente);
    }
}
