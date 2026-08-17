using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Cliente (permite inyección y dobles de prueba).</summary>
    public interface IClienteDAL
    {
        List<BE.Cliente> ObtenerTodos();
        BE.Cliente       ObtenerPorId(int idCliente);
        int              Alta(BE.Cliente cliente);
        void             Modificar(BE.Cliente cliente);
        void             Baja(int idCliente);
        bool             ExisteDNI(string dni);
        bool             ExisteDNIParaOtro(string dni, int idExcluir);

        /// <summary>Ejecuta una acción dentro de una única transacción de BD (commit si no lanza, rollback si lanza).
        /// Permite que los manejadores de Renovación/Cobro actualicen Cliente y su historial de forma atómica —
        /// antes eran dos round-trips independientes sin garantía de consistencia entre sí.</summary>
        void EjecutarTransaccion(Action<SqlConnection, SqlTransaction> accion);

        /// <summary>Igual que <see cref="Modificar"/>, pero sobre una transacción YA abierta (ver <see cref="EjecutarTransaccion"/>).
        /// No recalcula el DV — el caller debe llamar a <see cref="RecalcularDV"/> después de confirmar la transacción.</summary>
        void ModificarEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cliente cliente);

        /// <summary>Recalcula el Dígito Verificador (T07) de la tabla Cliente. Público para poder
        /// invocarse tras confirmar una transacción externa armada con <see cref="EjecutarTransaccion"/>.</summary>
        void RecalcularDV();
    }
}
