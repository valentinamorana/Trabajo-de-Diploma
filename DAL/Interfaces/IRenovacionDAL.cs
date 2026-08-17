using System.Collections.Generic;
using System.Data.SqlClient;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Renovación de suscripción (PdN5).</summary>
    public interface IRenovacionDAL
    {
        /// <summary>Inserta un nuevo intento de renovación. Devuelve el ID generado.</summary>
        int Alta(BE.Renovacion renovacion);

        /// <summary>Igual que <see cref="Alta"/>, pero sobre una transacción ya abierta por el
        /// caller (ver <see cref="IClienteDAL.EjecutarTransaccion"/>) — para que el INSERT del
        /// historial y el UPDATE de Cliente sean atómicos.</summary>
        int AltaEnTx(SqlConnection conexion, SqlTransaction tx, BE.Renovacion renovacion);

        /// <summary>Marca una renovación como resuelta (Resultado + FechaResolucion).</summary>
        void Resolver(int idRenovacion, BE.EstadoRenovacion resultado, int? idPlanNuevo);

        List<BE.Renovacion> ObtenerPorCliente(int idCliente);
        List<BE.Renovacion> ObtenerTodos();
    }
}
