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
            int DarDeBajaConCargo(BE.CargoPrenda cargo, BE.EstadoPrenda estadoEsperado);
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
        public int DarDeBajaConCargo(BE.CargoPrenda cargo, BE.EstadoPrenda estadoEsperado)
        {
            if (cargo == null) throw new ArgumentNullException(nameof(cargo));
            int idNuevo = 0;

            acceso.EjecutarTransaccion((cn, tx) =>
            {
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
            });

            return idNuevo;
        }
    }
}
