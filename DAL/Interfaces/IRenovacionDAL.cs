using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Renovación de suscripción (PdN5).</summary>
    public interface IRenovacionDAL
    {
        /// <summary>Inserta un nuevo intento de renovación. Devuelve el ID generado.</summary>
        int Alta(BE.Renovacion renovacion);

        /// <summary>Marca una renovación como resuelta (Resultado + FechaResolucion).</summary>
        void Resolver(int idRenovacion, BE.EstadoRenovacion resultado, int? idPlanNuevo);

        List<BE.Renovacion> ObtenerPorCliente(int idCliente);
        List<BE.Renovacion> ObtenerTodos();
    }
}
