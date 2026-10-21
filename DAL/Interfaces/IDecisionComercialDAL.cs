using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>
    /// PN03 — Contrato del acceso a datos de DecisionComercial (permite dobles de prueba). Cada cambio
    /// de estado es un claim atómico (UPDATE ... WHERE Estado = @esperado): si otra sesión ya lo cambió,
    /// no escribe y devuelve false.
    /// </summary>
    public interface IDecisionComercialDAL
    {
        /// <summary>Inserta la decisión y devuelve el ID generado.</summary>
        int Alta(BE.DecisionComercial decision);

        /// <summary>estadoEsperado → decision.Estado, con Motivo, PospuestaHasta, AccionElegida, impacto y referencia.</summary>
        bool CambiarEstado(BE.DecisionComercial decision, BE.EstadoDecision estadoEsperado);

        /// <summary>Guarda el resultado real medido (o cargado) y la fecha de evaluación.</summary>
        bool RegistrarResultado(int idDecision, decimal resultadoReal, DateTime fechaEvaluacion);

        BE.DecisionComercial ObtenerPorId(int idDecision);

        /// <summary>Decisiones del rango [desde, hastaExcl) (null = sin límite), más recientes primero.</summary>
        List<BE.DecisionComercial> Obtener(DateTime? desde, DateTime? hastaExcl);

        /// <summary>
        /// Aceptada → Ejecutada (claim atómico sobre <paramref name="estadoEsperado"/>) y, en la MISMA transacción,
        /// qué tocó la decisión (tabla DecisionComercialDetalle: tipo Cliente o Prenda + IDs). false si otra sesión
        /// ya la cambió (no se escribe nada).
        /// </summary>
        bool EjecutarConDetalle(BE.DecisionComercial decision, BE.EstadoDecision estadoEsperado, string tipo, IEnumerable<int> ids);

        /// <summary>IDs del detalle de cada decisión pedida (idDecision → IDs del tipo indicado).</summary>
        Dictionary<int, List<int>> ObtenerDetalle(IEnumerable<int> idsDecision, string tipo);
    }
}
