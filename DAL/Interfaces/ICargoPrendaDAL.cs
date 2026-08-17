using System.Collections.Generic;
using System.Data.SqlClient;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Cargo por daño/pérdida de prenda.</summary>
    public interface ICargoPrendaDAL
    {
        /// <summary>Inserta un nuevo cargo (Estado=Pendiente). Devuelve el ID generado.</summary>
        int Alta(BE.CargoPrenda cargo);

        /// <summary>Cargos Pendientes de un cliente — los que ProcesarPagoHandler suma al próximo cobro.</summary>
        List<BE.CargoPrenda> ObtenerPendientesPorCliente(int idCliente);

        /// <summary>Marca como Cobrados los cargos indicados dentro de una transacción YA abierta
        /// (ver <see cref="IClienteDAL.EjecutarTransaccion"/>) — para que el INSERT del cobro y este
        /// UPDATE sean atómicos junto con el descuento aplicado al Cliente.</summary>
        void MarcarCobradosEnTx(SqlConnection conexion, SqlTransaction tx, List<int> idsCargo, System.DateTime fechaCobro);

        List<BE.CargoPrenda> ObtenerTodos();
    }
}
