using System.Collections.Generic;
using System.Data.SqlClient;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Renovación de suscripción (PdN5).</summary>
    public interface IRenovacionDAL
    {
        /// <summary>Inserta el registro de la renovación sobre una transacción ya abierta por el
        /// caller (ver <see cref="IClienteDAL.EjecutarTransaccion"/>) — para que el INSERT del
        /// historial y el UPDATE de Cliente sean atómicos.</summary>
        int AltaEnTx(SqlConnection conexion, SqlTransaction tx, BE.Renovacion renovacion);

        List<BE.Renovacion> ObtenerPorCliente(int idCliente);
        List<BE.Renovacion> ObtenerTodos();
    }
}
