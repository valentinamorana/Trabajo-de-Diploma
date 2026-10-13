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
        // Catálogo de medios de pago (activos e históricos) para el medio de pago preferido.
        List<BE.MedioPago> ObtenerMediosPago();

        /// <summary>Ejecuta una acción dentro de una única transacción de BD (commit si no lanza, rollback si lanza).
        /// Permite que los manejadores de Renovación/Cobro actualicen Cliente y su historial de forma atómica —
        /// antes eran dos round-trips independientes sin garantía de consistencia entre sí.
        /// T07: el DV de las filas de Cliente escritas con los métodos ...EnTx se recalcula dentro de esta
        /// misma transacción, antes del commit (si falla, se revierte todo).</summary>
        void EjecutarTransaccion(Action<SqlConnection, SqlTransaction> accion);

        /// <summary>Igual que <see cref="Modificar"/>, pero sobre una transacción YA abierta (ver <see cref="EjecutarTransaccion"/>).
        /// El DV de la fila lo recalcula <see cref="EjecutarTransaccion"/> antes del commit.</summary>
        void ModificarEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cliente cliente);

        /// <summary>Cobro recurrente (N01): fija el nuevo vencimiento y limpia la gracia SOLO si el
        /// vencimiento sigue siendo <paramref name="vencimientoLeido"/> (control optimista).
        /// Devuelve false si otra sesión ya lo cambió.</summary>
        bool RenovarVencimientoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente,
                                    DateTime? vencimientoLeido, DateTime nuevoVencimiento);

        /// <summary>Suma <paramref name="monto"/> al crédito de referido de forma atómica (delta sobre el valor real de la BD).
        /// El UPDATE general de Cliente no escribe el crédito: solo estas dos operaciones lo modifican.</summary>
        void SumarCreditoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente, decimal monto);

        /// <summary>Resta <paramref name="monto"/> del crédito de referido de forma atómica, SOLO si el saldo
        /// real de la base lo cubre; si no (otro cobro ya lo aplicó), lanza y la transacción se revierte.</summary>
        void ConsumirCreditoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente, decimal monto);
    }
}
