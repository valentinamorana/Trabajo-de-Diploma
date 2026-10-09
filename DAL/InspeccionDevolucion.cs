using System;
using System.Data.SqlClient;

namespace DAL
{
    namespace Interfaces
    {
        /// <summary>
        /// Escritura atómica de PN04: registrar el cargo al último cliente y dar de baja la
        /// prenda en UNA sola transacción (ver <see cref="DAL.InspeccionDevolucion"/>).
        /// </summary>
        public interface IInspeccionDevolucionDAL
        {
            /// <summary>
            /// Inserta el cargo (Pendiente) y pasa la prenda a Baja solo si su estado actual
            /// sigue siendo <paramref name="estadoEsperado"/>. Si el UPDATE no afecta filas
            /// (el estado cambió entre la lectura y la escritura) se hace rollback de AMBAS
            /// escrituras y se lanza AppException. Devuelve el Id del cargo creado.
            /// </summary>
            int DarDeBajaConCargo(BE.CargoPrenda cargo, BE.EstadoPrenda estadoEsperado, int? idPedidoACerrar = null);

            /// <summary>
            /// Último pedido que tiene la prenda asignada (Separado, Pendiente, Despachado o
            /// Entregado SIN devolución registrada: un pedido devuelto sigue Entregado pero con
            /// FechaDevolucion, y se ignora), con su estado y su fecha de entrega. Null si no hay ninguno.
            /// PN04 lo usa para la compra tácita: solo se reporta perdida una prenda entregada.
            /// </summary>
            BE.Pedido ObtenerPedidoEnCurso(int idPrenda);
        }
    }

    /// <summary>
    /// PN04 — Inspección de Devolución (EnLimpieza → Baja con cargo) y Reportar Prenda Perdida
    /// (EnUso → Baja con cargo). Antes la GUI llamaba por separado a BLL.CargoPrenda.RegistrarCargo
    /// y a BLL.Prenda.CambiarEstado, sin transacción: si el segundo paso fallaba quedaba un cargo
    /// cobrado sobre una prenda que seguía en circulación. Ahora el INSERT del cargo y el UPDATE
    /// condicionado de la prenda comparten la misma transacción (Acceso.EjecutarTransaccion).
    /// </summary>
    public class InspeccionDevolucion : BaseDAL, Interfaces.IInspeccionDevolucionDAL
    {
        // idPedidoACerrar (Reportar Prenda Perdida): si con esta baja al pedido ya no le queda ninguna
        // prenda en poder del cliente, se completa su FechaDevolucion (queda cerrado: deja de contar
        // como atrasado). Se recalcula el DV de Pedido en la misma transacción.
        public int DarDeBajaConCargo(BE.CargoPrenda cargo, BE.EstadoPrenda estadoEsperado, int? idPedidoACerrar = null)
        {
            if (cargo == null) throw new ArgumentNullException(nameof(cargo));
            int idNuevo = 0;

            acceso.EjecutarTransaccion((cn, tx) =>
            {
                if (idPedidoACerrar.HasValue)
                    DigitoVerificador.Bloquear(cn, tx, Pedido.DV_Tabla);   // antes de escribir nada

                using (var cmd = new SqlCommand(
                    "INSERT INTO CargoPrenda (IdPrenda, IdCliente, Motivo, Monto, FechaRegistro, Actor, Estado) " +
                    "VALUES (@IdPrenda, @IdCliente, @Motivo, @Monto, @FechaRegistro, @Actor, @Estado); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@IdPrenda",      cargo.IdPrenda);
                    cmd.Parameters.AddWithValue("@IdCliente",     cargo.IdCliente);
                    cmd.Parameters.AddWithValue("@Motivo",        cargo.Motivo);
                    cmd.Parameters.AddWithValue("@Monto",         cargo.Monto);
                    cmd.Parameters.AddWithValue("@FechaRegistro", cargo.FechaRegistro);
                    cmd.Parameters.AddWithValue("@Actor",         (object)cargo.Actor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado",        (int)BE.EstadoCargo.Pendiente);
                    object r = cmd.ExecuteScalar();
                    idNuevo = r == null || r == DBNull.Value ? 0 : Convert.ToInt32(r);
                }

                // Mismo UPDATE condicionado (anti-TOCTOU) que DAL.Prenda.CambiarEstado para un
                // destino distinto de EnUso: limpia IdClienteActual y conserva IdUltimoCliente.
                using (var cmd = new SqlCommand(
                    "UPDATE Prenda SET Estado=@Estado, IdClienteActual=NULL " +
                    "WHERE IdPrenda=@IdPrenda AND Estado=@EstadoAnterior", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@Estado",         (int)BE.EstadoPrenda.Baja);
                    cmd.Parameters.AddWithValue("@EstadoAnterior", (int)estadoEsperado);
                    cmd.Parameters.AddWithValue("@IdPrenda",       cargo.IdPrenda);
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new BE.AppException("err.dal.prenda.estado_cambio",
                            "El estado de la prenda cambió desde que se consultó. Actualizá la pantalla e intentá de nuevo.");
                }

                // Una prenda dada de baja sale del circuito: se cierra el mantenimiento abierto (si
                // venía de la cola de inspección, si no quedaba para siempre en el tablero de Depósito
                // y en Tareas pendientes) y se cancelan las anotaciones de la lista de espera, que ya
                // no se pueden cumplir.
                using (var cmd = new SqlCommand(
                    "UPDATE MantenimientoPrenda SET FechaSalida = GETDATE() " +
                    "WHERE IdPrenda = @IdPrenda AND FechaSalida IS NULL; " +
                    "UPDATE ListaEspera SET Estado = @Cancelada, FechaResolucion = GETDATE(), Actor = @Actor " +
                    "WHERE IdPrenda = @IdPrenda AND Estado IN (@Pendiente, @Reservada);", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@IdPrenda",  cargo.IdPrenda);
                    cmd.Parameters.AddWithValue("@Cancelada", (int)BE.EstadoListaEspera.Cancelada);
                    cmd.Parameters.AddWithValue("@Pendiente", (int)BE.EstadoListaEspera.Pendiente);
                    cmd.Parameters.AddWithValue("@Reservada", (int)BE.EstadoListaEspera.Reservada);
                    cmd.Parameters.AddWithValue("@Actor",     (object)cargo.Actor ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                if (idPedidoACerrar.HasValue)
                {
                    int cerrado;
                    using (var cmd = new SqlCommand(
                        "UPDATE Pedido SET FechaDevolucion = GETDATE() " +
                        "WHERE IdPedido = @IdPedido AND FechaDevolucion IS NULL " +
                        "AND NOT EXISTS (SELECT 1 FROM PedidoPrenda pp INNER JOIN Prenda pr ON pr.IdPrenda = pp.IdPrenda " +
                        "                WHERE pp.IdPedido = @IdPedido AND pr.Estado = @EnUso AND pr.IdClienteActual = @IdCliente)",
                        cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@IdPedido",  idPedidoACerrar.Value);
                        cmd.Parameters.AddWithValue("@EnUso",     (int)BE.EstadoPrenda.EnUso);
                        cmd.Parameters.AddWithValue("@IdCliente", cargo.IdCliente);
                        cerrado = cmd.ExecuteNonQuery();
                    }
                    if (cerrado > 0) new Pedido().ActualizarDVEnTx(cn, tx, idPedidoACerrar.Value);
                }
            });

            return idNuevo;
        }

        public BE.Pedido ObtenerPedidoEnCurso(int idPrenda)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdPrenda",   idPrenda),
                new SqlParameter("@Separado",   (int)BE.EstadoPedido.Separado),
                new SqlParameter("@Pendiente",  (object)(int)BE.EstadoPedido.Pendiente),   // (object): un 0 constante se tomaría como SqlDbType
                new SqlParameter("@Despachado", (int)BE.EstadoPedido.Despachado),
                new SqlParameter("@Entregado",  (int)BE.EstadoPedido.Entregado)
            };
            var dt = acceso.Leer(
                "SELECT TOP 1 p.IdPedido, p.IdCliente, p.Estado, p.FechaEntrega, p.FechaDevolucion " +
                "FROM Pedido p INNER JOIN PedidoPrenda pp ON pp.IdPedido = p.IdPedido " +
                "WHERE pp.IdPrenda = @IdPrenda AND p.Estado IN (@Separado, @Pendiente, @Despachado, @Entregado) " +
                // PN04: un pedido ya devuelto no está "en curso" (la prenda volvió con ese pedido).
                "AND p.FechaDevolucion IS NULL " +
                "ORDER BY p.FechaPedido DESC, p.IdPedido DESC", p);
            if (dt.Rows.Count == 0) return null;

            var r = dt.Rows[0];
            return new BE.Pedido
            {
                IdPedido        = Convert.ToInt32(r["IdPedido"]),
                IdCliente       = Convert.ToInt32(r["IdCliente"]),
                Estado          = (BE.EstadoPedido)Convert.ToInt32(r["Estado"]),
                FechaEntrega    = r["FechaEntrega"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaEntrega"]),
                FechaDevolucion = r["FechaDevolucion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaDevolucion"])
            };
        }
    }
}
