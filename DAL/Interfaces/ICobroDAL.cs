using System.Collections.Generic;
using System.Data.SqlClient;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Cobro de suscripción (PdN6).</summary>
    public interface ICobroDAL
    {
        /// <summary>Inserta un nuevo intento de cobro. Devuelve el ID generado.</summary>
        int Alta(BE.Cobro cobro);

        /// <summary>Igual que <see cref="Alta"/>, pero sobre una transacción ya abierta por el
        /// caller (ver <see cref="IClienteDAL.EjecutarTransaccion"/>) — para que el INSERT del
        /// historial y el UPDATE de Cliente sean atómicos.</summary>
        int AltaEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cobro cobro);

        List<BE.Cobro> ObtenerPorCliente(int idCliente);
        List<BE.Cobro> ObtenerTodos();
    }
}
