using System;
using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PdN5 — Gestión de renovación de suscripciones. Orquesta la cadena de manejadores
    /// (BLL.Manejadores) que resuelve la decisión tomada tras contactar al cliente.
    /// </summary>
    public interface IRenovacionService
    {
        // Procesa una renovación para el cliente indicado según la decisión tomada.
        // fechaPausaHasta solo se usa cuando decision == Pausar.
        Manejadores.ResultadoRenovacion Procesar(
            string modulo, BE.Cliente cliente, Manejadores.DecisionRenovacion decision,
            int? idPlanNuevo, BE.Builders.ModalidadCobro modalidad, string actor,
            DateTime? fechaPausaHasta = null);

        // Devuelve el historial de intentos de renovación de un cliente.
        List<BE.Renovacion> ObtenerHistorial(int idCliente);
    }
}
