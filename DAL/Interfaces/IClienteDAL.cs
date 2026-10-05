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
        // PN02: el cliente tiene una contratación pendiente de pago (no se puede dar de baja).
        bool             TieneContratacionPendiente(int idCliente);
        // Los mismos clientes en una sola consulta (para filtrar listados sin una consulta por cliente).
        HashSet<int>     ObtenerIdsConContratacionPendiente();

        /// <summary>Ejecuta una acción dentro de una única transacción de BD (commit si no lanza, rollback si lanza).
        /// Permite que los manejadores de Renovación/Cobro actualicen Cliente y su historial de forma atómica —
        /// antes eran dos round-trips independientes sin garantía de consistencia entre sí.</summary>
        void EjecutarTransaccion(Action<SqlConnection, SqlTransaction> accion);

        /// <summary>Igual que <see cref="Modificar"/>, pero sobre una transacción YA abierta (ver <see cref="EjecutarTransaccion"/>).
        /// No recalcula el DV — el caller debe llamar a <see cref="RecalcularDV"/> después de confirmar la transacción.</summary>
        void ModificarEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cliente cliente);

        /// <summary>Cobro recurrente (N01): fija el nuevo vencimiento y limpia la gracia SOLO si el
        /// vencimiento sigue siendo <paramref name="vencimientoLeido"/> (control optimista).
        /// Devuelve false si otra sesión ya lo cambió.</summary>
        bool RenovarVencimientoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente,
                                    DateTime? vencimientoLeido, DateTime nuevoVencimiento);

        /// <summary>Suma <paramref name="monto"/> al crédito de referido de forma atómica (delta sobre el valor real de la BD).
        /// El UPDATE general de Cliente no escribe el crédito: solo estas dos operaciones lo modifican.</summary>
        void SumarCreditoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente, decimal monto);

        /// <summary>Resta <paramref name="monto"/> del crédito de referido de forma atómica, sin dejarlo negativo.</summary>
        void ConsumirCreditoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente, decimal monto);

        /// <summary>Recalcula el Dígito Verificador (T07) de UNA fila de Cliente y el DVV de la tabla
        /// (desde los DVH almacenados). Se invoca tras confirmar una transacción externa armada con
        /// <see cref="EjecutarTransaccion"/>, una vez por cada cliente modificado.</summary>
        void RecalcularDV(int idCliente);
    }
}
