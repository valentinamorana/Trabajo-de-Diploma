using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace DAL
{
    /// <summary>
    /// Acceso a datos de las tablas [Pedido] y [PedidoPrenda].
    /// NOTA — Estado de Prenda: los UPDATE de Prenda.Estado de esta clase (SepararPrendas,
    /// RegistrarDevolucion, ReconciliarEnTx, Cancelar, DesCancelar) son intencionalmente
    /// SQL directo dentro de la misma transacción del Pedido, y no pasan por
    /// BLL.Prenda.CambiarEstado / el patrón State de BE.Estados. Es la única vía por la
    /// que una Prenda entra o sale de EnUso — documentado en BE.Prenda.TransicionPermitida
    /// y en BE.Estados.EstadoEnUso. No unificar sin preservar la protección anti-TOCTOU
    /// (UPDATE condicionado + chequeo de filas afectadas dentro de la transacción).
    /// </summary>
    public class Pedido : BaseDAL<BE.Pedido>, Interfaces.IPedidoDAL
    {

        // SELECT base compartido por todos los métodos de lectura
        private const string SELECT_BASE =
            "SELECT ped.IdPedido, ped.IdCliente, ped.IdEmpleado, ped.Estado, " +
            "       ped.FechaPedido, ped.FechaDespacho, ped.FechaEntrega, ped.FechaDevolucion, " +
            "       ped.MotivoCancelacion, " +
            "       ped.FechaEnvioControl, ped.FechaControl, ped.IdEmpleadoControl, " +
            "       ped.FechaSeparacion, ped.FechaFormalizacion, " +
            "       ped.MotivoDesistimiento, ped.EtapaDesistimiento, " +
            "       cli.Nombre + ' ' + cli.Apellido AS NombreCliente, " +
            "       emp.Nombre + ' ' + emp.Apellido AS NombreEmpleado, " +
            "       ctl.Nombre + ' ' + ctl.Apellido AS NombreEmpleadoControl " +
            "FROM Pedido ped " +
            "INNER JOIN Cliente cli ON cli.IdCliente = ped.IdCliente " +
            "INNER JOIN Empleado emp ON emp.IdEmpleado = ped.IdEmpleado " +
            "LEFT JOIN Empleado ctl ON ctl.IdEmpleado = ped.IdEmpleadoControl";

        // Pedidos que cuentan como venta concretada para reportes y analítica: los formalizados
        // (Pendiente de despacho) y los que siguieron el ciclo logístico. Los que están en el
        // circuito de control de stock (todavía sin formalizar), los desistidos y los cancelados
        // no representan una venta.
        private static readonly string ESTADOS_VENTA =
            "(" + (int)BE.EstadoPedido.Pendiente + "," + (int)BE.EstadoPedido.Despachado + "," +
                  (int)BE.EstadoPedido.Entregado + ")";

        // Ventas concretadas más las que se cancelaron después de formalizarse (desempeño por vendedor).
        private static readonly string ESTADOS_VENTA_O_CANCELADO =
            "(" + (int)BE.EstadoPedido.Pendiente + "," + (int)BE.EstadoPedido.Despachado + "," +
                  (int)BE.EstadoPedido.Entregado + "," + (int)BE.EstadoPedido.Cancelado + ")";

        // Devuelve todos los pedidos. Las prendas se cargan por separado en ObtenerPorId.
        public override List<BE.Pedido> ObtenerTodos()
        {
            var lista = new List<BE.Pedido>();
            try
            {
                DataTable tabla = acceso.Leer(
                    SELECT_BASE + " ORDER BY ped.FechaPedido DESC",
                    null);

                foreach (DataRow row in tabla.Rows)
                    lista.Add(MapearCabecera(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la lista de pedidos.", ex);
            }
            return lista;
        }

        // Devuelve los pedidos pendientes (para el módulo de Despacho).
        public List<BE.Pedido> ObtenerPendientes()
        {
            var lista = new List<BE.Pedido>();
            try
            {
                DataTable tabla = acceso.Leer(
                    SELECT_BASE +
                    " WHERE ped.Estado = @Estado" +
                    " ORDER BY ped.FechaPedido",
                    new[] { new SqlParameter("@Estado", (object)(int)BE.EstadoPedido.Pendiente) });

                foreach (DataRow row in tabla.Rows)
                    lista.Add(MapearCabecera(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener pedidos pendientes.", ex);
            }
            return lista;
        }

        // PN01 — pedidos en un estado dado (por ej. la cola de Control de Stock de Depósito),
        // del más antiguo al más nuevo según el envío a control.
        public List<BE.Pedido> ObtenerPorEstado(BE.EstadoPedido estado)
        {
            var lista = new List<BE.Pedido>();
            try
            {
                DataTable tabla = acceso.Leer(
                    SELECT_BASE +
                    " WHERE ped.Estado = @Estado" +
                    " ORDER BY ISNULL(ped.FechaEnvioControl, ped.FechaPedido), ped.IdPedido",
                    new[] { new SqlParameter("@Estado", (object)(int)estado) });

                foreach (DataRow row in tabla.Rows)
                    lista.Add(MapearCabecera(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los pedidos por estado.", ex);
            }
            return lista;
        }

        // PdN10 — Fecha del pedido más reciente de cada cliente (solo ventas concretadas: un
        // pedido cancelado, desistido o todavía en control no representa actividad real).
        // Usada por BLL.AnalisisAbandono para cruzar "último pedido" contra vencimiento.
        public Dictionary<int, DateTime> ObtenerFechaUltimoPedidoPorCliente()
        {
            var resultado = new Dictionary<int, DateTime>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT IdCliente, MAX(FechaPedido) AS UltimaFecha " +
                    "FROM Pedido WHERE Estado IN " + ESTADOS_VENTA + " GROUP BY IdCliente",
                    null);

                if (tabla != null)
                    foreach (DataRow row in tabla.Rows)
                        resultado[Convert.ToInt32(row["IdCliente"])] = Convert.ToDateTime(row["UltimaFecha"]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la fecha del último pedido por cliente.", ex);
            }
            return resultado;
        }

        // PdN8 — Desempeño por vendedor: total de pedidos, entregados y cancelados por Empleado.
        // Usada por BLL.ReporteVentasVendedor para evaluar desempeño comercial.
        public List<BE.DesempenoVendedor> ObtenerEstadisticasPorEmpleado()
        {
            var lista = new List<BE.DesempenoVendedor>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT emp.IdEmpleado, emp.Nombre + ' ' + emp.Apellido AS NombreEmpleado, " +
                    "       COUNT(*) AS TotalPedidos, " +
                    "       SUM(CASE WHEN ped.Estado = @Entregado THEN 1 ELSE 0 END) AS Entregados, " +
                    "       SUM(CASE WHEN ped.Estado = @Cancelado THEN 1 ELSE 0 END) AS Cancelados " +
                    "FROM Pedido ped " +
                    "INNER JOIN Empleado emp ON emp.IdEmpleado = ped.IdEmpleado " +
                    // Solo pedidos formalizados (y los cancelados después de formalizar): los que
                    // están en control de stock o fueron desistidos no son ventas del vendedor.
                    "WHERE ped.Estado IN " + ESTADOS_VENTA_O_CANCELADO + " " +
                    "GROUP BY emp.IdEmpleado, emp.Nombre, emp.Apellido " +
                    "ORDER BY TotalPedidos DESC",
                    new[]
                    {
                        new SqlParameter("@Entregado", (int)BE.EstadoPedido.Entregado),
                        new SqlParameter("@Cancelado", (int)BE.EstadoPedido.Cancelado)
                    });

                if (tabla != null)
                    foreach (DataRow row in tabla.Rows)
                        lista.Add(new BE.DesempenoVendedor
                        {
                            IdEmpleado = Convert.ToInt32(row["IdEmpleado"]),
                            NombreEmpleado = row["NombreEmpleado"].ToString(),
                            TotalPedidos = Convert.ToInt32(row["TotalPedidos"]),
                            Entregados = Convert.ToInt32(row["Entregados"]),
                            Cancelados = Convert.ToInt32(row["Cancelados"])
                        });
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las estadísticas por vendedor.", ex);
            }
            return lista;
        }

        // PdN9 — Cantidad de veces que cada prenda fue pedida (solo ventas concretadas: un
        // pedido cancelado, desistido o en control no representa demanda real).
        // Usada por BLL.AnalisisRotacion.
        public Dictionary<int, int> ObtenerCantidadPedidosPorPrenda(DateTime? desde = null)
        {
            var resultado = new Dictionary<int, int>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT pp.IdPrenda, COUNT(*) AS Cantidad " +
                    "FROM PedidoPrenda pp " +
                    "INNER JOIN Pedido p ON p.IdPedido = pp.IdPedido " +
                    "WHERE p.Estado IN " + ESTADOS_VENTA + " " +
                    (desde.HasValue ? "AND p.FechaPedido >= @Desde " : "") +
                    "GROUP BY pp.IdPrenda",
                    desde.HasValue ? new[] { new SqlParameter("@Desde", desde.Value.Date) } : null);

                if (tabla != null)
                    foreach (DataRow row in tabla.Rows)
                        resultado[Convert.ToInt32(row["IdPrenda"])] = Convert.ToInt32(row["Cantidad"]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la cantidad de pedidos por prenda.", ex);
            }
            return resultado;
        }

        // PdN13 — Prendas que un cliente pidió alguna vez (solo ventas concretadas), para
        // construir su perfil de preferencias. Usada por BLL.RecomendacionPrendas.
        public List<BE.Prenda> ObtenerPrendasHistoricasPorCliente(int idCliente)
        {
            var lista = new List<BE.Prenda>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT pr.IdPrenda, pr.Nombre, pr.Descripcion, pr.Talle, pr.Color, " +
                    "       pr.Categoria, pr.Estado, pr.IdClienteActual, pr.FechaAlta " +
                    "FROM PedidoPrenda pp " +
                    "INNER JOIN Pedido p ON p.IdPedido = pp.IdPedido " +
                    "INNER JOIN Prenda pr ON pr.IdPrenda = pp.IdPrenda " +
                    "WHERE p.IdCliente = @IdCliente AND p.Estado IN " + ESTADOS_VENTA,
                    new[] { new SqlParameter("@IdCliente", idCliente) });

                if (tabla != null)
                    foreach (DataRow row in tabla.Rows)
                        lista.Add(new BE.Prenda
                        {
                            IdPrenda = Convert.ToInt32(row["IdPrenda"]),
                            Nombre = row["Nombre"].ToString(),
                            Descripcion = row["Descripcion"] != DBNull.Value ? row["Descripcion"].ToString() : null,
                            Talle = row["Talle"] != DBNull.Value ? row["Talle"].ToString() : null,
                            Color = row["Color"] != DBNull.Value ? row["Color"].ToString() : null,
                            Categoria = row["Categoria"] != DBNull.Value ? row["Categoria"].ToString() : null,
                            Estado = (BE.EstadoPrenda)Convert.ToInt32(row["Estado"]),
                            FechaAlta = Convert.ToDateTime(row["FechaAlta"])
                        });
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el historial de prendas del cliente.", ex);
            }
            return lista;
        }

        // ¿El cliente tiene un pedido en el circuito (mismos estados que BE.Pedido.EsActivo)?
        public bool TienePedidoActivo(int idCliente)
        {
            var activos = new[] { BE.EstadoPedido.EnControlStock, BE.EstadoPedido.ConFaltantes,
                                  BE.EstadoPedido.Separado, BE.EstadoPedido.Pendiente, BE.EstadoPedido.Despachado };
            var enLista = string.Join(",", Array.ConvertAll(activos, e => ((int)e).ToString(CultureInfo.InvariantCulture)));
            DataTable t = acceso.Leer(
                "SELECT COUNT(*) AS N FROM Pedido WHERE IdCliente = @IdCliente AND Estado IN (" + enLista + ")",
                new[] { new SqlParameter("@IdCliente", idCliente) });
            return t != null && t.Rows.Count > 0 && Convert.ToInt32(t.Rows[0]["N"]) > 0;
        }

        // Obtiene un pedido por ID incluyendo sus prendas.
        public override BE.Pedido ObtenerPorId(int idPedido)
        {
            SqlParameter[] p = { new SqlParameter("@IdPedido", idPedido) };
            try
            {
                DataTable tabla = acceso.Leer(
                    SELECT_BASE + " WHERE ped.IdPedido = @IdPedido",
                    p);

                if (tabla == null || tabla.Rows.Count == 0) return null;

                var pedido = MapearCabecera(tabla.Rows[0]);
                pedido.Prendas = ObtenerPrendasDePedido(idPedido);
                pedido.PrendasConfirmadas = ObtenerPrendasConfirmadas(idPedido);
                return pedido;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el pedido.", ex);
            }
        }

        // ── PN01 — circuito de control de stock ─────────────────────────────────
        // Ninguno de estos métodos reserva prendas salvo SepararPrendas: hasta que Depósito
        // separa, la selección es solo un pedido de revisión (las prendas siguen Disponibles).
        // Cada transición de estado se hace con un UPDATE condicionado al estado esperado
        // ("claim" atómico): si otra sesión ya movió el pedido, no se afecta ninguna fila y la
        // operación se rechaza en vez de pisar el estado.

        // Inserta el pedido y sus líneas SIN tocar el estado de las prendas. Se usa al enviar la
        // selección a control de stock (EnControlStock) y al asentar un desistimiento por exceso
        // de cupo (Desistido). Devuelve el ID generado.
        public int AltaSinReserva(BE.Pedido pedido)
        {
            int idNuevo = 0;

            ConPedidoActivoUnico(() => acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaEnvioControl, " +
                    "                    MotivoDesistimiento, EtapaDesistimiento) " +
                    "VALUES (@IdCliente, @IdEmpleado, @Estado, @FechaPedido, @FechaEnvioControl, " +
                    "        @MotivoDesistimiento, @EtapaDesistimiento); " +
                    "SELECT SCOPE_IDENTITY() AS IdNuevo",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", pedido.IdCliente);
                    cmd.Parameters.AddWithValue("@IdEmpleado", pedido.IdEmpleado);
                    cmd.Parameters.AddWithValue("@Estado", (int)pedido.Estado);
                    cmd.Parameters.AddWithValue("@FechaPedido", pedido.FechaPedido);
                    cmd.Parameters.AddWithValue("@FechaEnvioControl",
                        (object)pedido.FechaEnvioControl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MotivoDesistimiento",
                        (object)pedido.MotivoDesistimiento ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EtapaDesistimiento",
                        pedido.EtapaDesistimiento.HasValue ? (object)pedido.EtapaDesistimiento.Value.ToString() : DBNull.Value);

                    var resultado = cmd.ExecuteScalar();
                    if (resultado == null || resultado == DBNull.Value)
                        throw new Exception("No se pudo insertar el pedido en la base de datos.");

                    idNuevo = Convert.ToInt32(resultado);
                }

                InsertarLineasEnTx(conexion, tx, idNuevo, pedido.Prendas);
            }));

            ActualizarDV(idNuevo);   // T07: DV multi-tabla (pedido + líneas)
            return idNuevo;
        }

        // UX_Pedido_ClienteActivo: si dos operadores envían a la vez un pedido del mismo cliente,
        // la BLL valida antes pero solo el índice único filtrado garantiza un pedido en curso.
        private static void ConPedidoActivoUnico(Action accion)
        {
            try { accion(); }
            catch (SqlException ex) when ((ex.Number == 2601 || ex.Number == 2627)
                                          && ex.Message.Contains("UX_Pedido_ClienteActivo"))
            {
                throw new BE.AppException("err.dal.pedido.pedido_activo_concurrente",
                    "El cliente ya tiene otro pedido en curso: otro operador lo registró recién. Actualizá la lista.");
            }
        }

        // "Recibir selección ajustada por disponibilidad": reemplaza las líneas de un pedido con
        // faltantes informados, borra el informe anterior y lo devuelve a control de stock.
        public void ReemplazarSeleccion(int idPedido, List<BE.Prenda> prendas)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Nuevo, FechaEnvioControl=@Ahora, FechaControl=NULL, " +
                    "       IdEmpleadoControl=NULL " +
                    "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Nuevo",    (int)BE.EstadoPedido.EnControlStock);
                    cmd.Parameters.AddWithValue("@Ahora",    DateTime.Now);
                    cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmd.Parameters.AddWithValue("@Esperado", (int)BE.EstadoPedido.ConFaltantes);
                    if (cmd.ExecuteNonQuery() == 0) throw EstadoCambiado(idPedido);
                }

                BorrarInformeFaltantesEnTx(conexion, tx, idPedido);
                Ejecutar(conexion, tx, "DELETE FROM PedidoPrenda WHERE IdPedido=@IdPedido", idPedido);
                InsertarLineasEnTx(conexion, tx, idPedido, prendas);
            });
            ActualizarDV(idPedido);   // T07 — cambiaron las líneas del pedido
        }

        // "Informe de prendas faltantes": EnControlStock → ConFaltantes. Guarda una fila por
        // prenda faltante (PedidoFaltante) y una por alternativa propuesta (PedidoFaltanteAlternativa).
        public void RegistrarFaltantes(int idPedido, int idEmpleadoControl, List<BE.PedidoFaltante> faltantes)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Nuevo, FechaControl=@Ahora, IdEmpleadoControl=@IdEmpleado " +
                    "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Nuevo",      (int)BE.EstadoPedido.ConFaltantes);
                    cmd.Parameters.AddWithValue("@Ahora",      DateTime.Now);
                    cmd.Parameters.AddWithValue("@IdEmpleado", idEmpleadoControl);
                    cmd.Parameters.AddWithValue("@IdPedido",   idPedido);
                    cmd.Parameters.AddWithValue("@Esperado",   (int)BE.EstadoPedido.EnControlStock);
                    if (cmd.ExecuteNonQuery() == 0) throw EstadoCambiado(idPedido);
                }

                BorrarInformeFaltantesEnTx(conexion, tx, idPedido);
                Ejecutar(conexion, tx, "UPDATE PedidoPrenda SET Confirmada=0 WHERE IdPedido=@IdPedido", idPedido);

                foreach (var f in faltantes)
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO PedidoFaltante (IdPedido, IdPrenda, EstadoAlRevisar, ReservadaParaOtro) " +
                        "VALUES (@IdPedido, @IdPrenda, @EstadoAlRevisar, @Reservada)",
                        conexion, tx))
                    {
                        cmd.Parameters.AddWithValue("@IdPedido",        idPedido);
                        cmd.Parameters.AddWithValue("@IdPrenda",        f.Prenda.IdPrenda);
                        cmd.Parameters.AddWithValue("@EstadoAlRevisar", (int)f.EstadoAlRevisar);
                        cmd.Parameters.AddWithValue("@Reservada",       f.ReservadaParaOtro);
                        cmd.ExecuteNonQuery();
                    }

                    foreach (var alt in f.Alternativas ?? new List<BE.Prenda>())
                    {
                        using (var cmd = new SqlCommand(
                            "INSERT INTO PedidoFaltanteAlternativa (IdPedido, IdPrenda, IdPrendaAlternativa) " +
                            "VALUES (@IdPedido, @IdPrenda, @IdAlt)",
                            conexion, tx))
                        {
                            cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                            cmd.Parameters.AddWithValue("@IdPrenda", f.Prenda.IdPrenda);
                            cmd.Parameters.AddWithValue("@IdAlt",    alt.IdPrenda);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            });
            ActualizarDV(idPedido);   // T07
        }

        // "Confirmar prendas disponibles": marca todas las líneas como confirmadas por Depósito.
        // El pedido sigue EnControlStock hasta que se separen las prendas.
        public void ConfirmarPrendas(int idPedido, int idEmpleadoControl)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Pedido SET FechaControl=@Ahora, IdEmpleadoControl=@IdEmpleado " +
                    "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Ahora",      DateTime.Now);
                    cmd.Parameters.AddWithValue("@IdEmpleado", idEmpleadoControl);
                    cmd.Parameters.AddWithValue("@IdPedido",   idPedido);
                    cmd.Parameters.AddWithValue("@Esperado",   (int)BE.EstadoPedido.EnControlStock);
                    if (cmd.ExecuteNonQuery() == 0) throw EstadoCambiado(idPedido);
                }

                Ejecutar(conexion, tx, "UPDATE PedidoPrenda SET Confirmada=1 WHERE IdPedido=@IdPedido", idPedido);
            });
            ActualizarDV(idPedido);   // T07 — la confirmación de las líneas entra al DVH
        }

        // "Separar prendas del pedido": EnControlStock → Separado y cada prenda Disponible → EnUso
        // a nombre del cliente, todo en una transacción. Si otra operación tomó una prenda desde
        // la revisión, el UPDATE condicionado no afecta filas y se revierte TODO (no queda nada
        // reservado a medias).
        public void SepararPrendas(int idPedido, int idCliente)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Nuevo, FechaSeparacion=@Ahora " +
                    "WHERE IdPedido=@IdPedido AND Estado=@Esperado " +
                    "  AND NOT EXISTS (SELECT 1 FROM PedidoPrenda WHERE IdPedido=@IdPedido AND Confirmada=0)",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Nuevo",    (int)BE.EstadoPedido.Separado);
                    cmd.Parameters.AddWithValue("@Ahora",    DateTime.Now);
                    cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmd.Parameters.AddWithValue("@Esperado", (int)BE.EstadoPedido.EnControlStock);
                    if (cmd.ExecuteNonQuery() == 0) throw EstadoCambiado(idPedido);
                }

                var lineas = new List<(int IdPrenda, string Nombre)>();
                using (var cmd = new SqlCommand(
                    "SELECT pr.IdPrenda, pr.Nombre FROM PedidoPrenda pp " +
                    "INNER JOIN Prenda pr ON pr.IdPrenda = pp.IdPrenda WHERE pp.IdPedido=@IdPedido",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read())
                            lineas.Add((Convert.ToInt32(rd["IdPrenda"]), rd["Nombre"].ToString()));
                }

                foreach (var linea in lineas)
                {
                    using (var cmdPr = new SqlCommand(
                        // IdUltimoCliente (a diferencia de IdClienteActual) NUNCA se limpia al
                        // devolverse — así BLL.CargoPrenda puede saber quién tuvo la prenda por
                        // última vez aunque ya esté en Mantenimiento (EnLimpieza) sin dueño actual.
                        "UPDATE Prenda SET Estado=@Estado, IdClienteActual=@IdCliente, IdUltimoCliente=@IdCliente " +
                        "WHERE IdPrenda=@IdPrenda AND Estado=@EstadoDisponible",
                        conexion, tx))
                    {
                        cmdPr.Parameters.AddWithValue("@Estado",           (int)BE.EstadoPrenda.EnUso);
                        cmdPr.Parameters.AddWithValue("@IdCliente",        idCliente);
                        cmdPr.Parameters.AddWithValue("@IdPrenda",         linea.IdPrenda);
                        cmdPr.Parameters.AddWithValue("@EstadoDisponible", (int)BE.EstadoPrenda.Disponible);
                        // Control de concurrencia (anti-TOCTOU): si otra operación tomó la prenda
                        // desde la revisión de stock, no se afecta ninguna fila → rollback de todo.
                        if (cmdPr.ExecuteNonQuery() == 0)
                            throw new BE.AppException("err.dal.pedido.prenda_tomada",
                                "La prenda '{0}' ya no está disponible. Actualizá la selección e intentá de nuevo.",
                                linea.Nombre);
                    }
                }
            });
            ActualizarDV(idPedido);   // T07
        }

        // "Formalizar el pedido": Separado → Pendiente (formalizado, pendiente de despacho).
        public void Formalizar(int idPedido)
        {
            int afectadas = acceso.Escribir(
                "UPDATE Pedido SET Estado=@Nuevo, FechaFormalizacion=@Ahora " +
                "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                new SqlParameter[]
                {
                    // Pendiente vale 0: el literal 0 elegiría el constructor (nombre, SqlDbType) y el
                    // parámetro quedaría sin valor, por eso se asigna Value explícitamente.
                    new SqlParameter("@Nuevo",    System.Data.SqlDbType.Int) { Value = (int)BE.EstadoPedido.Pendiente },
                    new SqlParameter("@Ahora",    DateTime.Now),
                    new SqlParameter("@IdPedido", idPedido),
                    new SqlParameter("@Esperado", (int)BE.EstadoPedido.Separado)
                });
            if (afectadas == 0) throw EstadoCambiado(idPedido);
            ActualizarDV(idPedido);   // T07
        }

        // "Asentar desistimiento" de un pedido con faltantes informados: ConFaltantes → Desistido.
        // Si se pasa la selección ajustada (desistimiento por cupo al ajustar), reemplaza las
        // líneas (y descarta el informe anterior) en la misma transacción. No hay prendas que
        // liberar: nunca se reservaron.
        public void RegistrarDesistimiento(int idPedido, string motivo, BE.EtapaDesistimiento etapa,
                                           List<BE.Prenda> seleccionAjustada = null)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Nuevo, MotivoDesistimiento=@Motivo, EtapaDesistimiento=@Etapa " +
                    "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Nuevo",    (int)BE.EstadoPedido.Desistido);
                    cmd.Parameters.AddWithValue("@Motivo",   (object)motivo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Etapa",    etapa.ToString());
                    cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmd.Parameters.AddWithValue("@Esperado", (int)BE.EstadoPedido.ConFaltantes);
                    if (cmd.ExecuteNonQuery() == 0) throw EstadoCambiado(idPedido);
                }

                if (seleccionAjustada != null && seleccionAjustada.Count > 0)
                {
                    // Igual que ReemplazarSeleccion: la selección ajustada reemplaza a la informada,
                    // así que el informe de faltantes anterior (que apunta a esas líneas) se descarta.
                    BorrarInformeFaltantesEnTx(conexion, tx, idPedido);
                    Ejecutar(conexion, tx, "DELETE FROM PedidoPrenda WHERE IdPedido=@IdPedido", idPedido);
                    InsertarLineasEnTx(conexion, tx, idPedido, seleccionAjustada);
                }
            });
            ActualizarDV(idPedido);   // T07
        }

        // "Informe de disponibilidad (prendas faltantes y alternativas)" de un pedido.
        public List<BE.PedidoFaltante> ObtenerFaltantes(int idPedido)
        {
            var porPrenda = new Dictionary<int, BE.PedidoFaltante>();
            var orden = new List<int>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT pf.IdPrenda, pf.EstadoAlRevisar, pf.ReservadaParaOtro, " +
                    "       f.Nombre, f.Talle, f.Color, f.Categoria, f.Estado, " +
                    "       a.IdPrenda AS AltId, a.Nombre AS AltNombre, a.Talle AS AltTalle, " +
                    "       a.Color AS AltColor, a.Categoria AS AltCategoria, a.Estado AS AltEstado " +
                    "FROM PedidoFaltante pf " +
                    "INNER JOIN Prenda f ON f.IdPrenda = pf.IdPrenda " +
                    "LEFT JOIN PedidoFaltanteAlternativa pa ON pa.IdPedido = pf.IdPedido AND pa.IdPrenda = pf.IdPrenda " +
                    "LEFT JOIN Prenda a ON a.IdPrenda = pa.IdPrendaAlternativa " +
                    "WHERE pf.IdPedido = @IdPedido ORDER BY pf.IdPrenda, a.IdPrenda",
                    new[] { new SqlParameter("@IdPedido", idPedido) });

                foreach (DataRow row in tabla.Rows)
                {
                    int idPrenda = Convert.ToInt32(row["IdPrenda"]);
                    if (!porPrenda.TryGetValue(idPrenda, out var faltante))
                    {
                        faltante = new BE.PedidoFaltante
                        {
                            IdPedido          = idPedido,
                            EstadoAlRevisar   = (BE.EstadoPrenda)Convert.ToInt32(row["EstadoAlRevisar"]),
                            ReservadaParaOtro = Convert.ToBoolean(row["ReservadaParaOtro"]),
                            Prenda = new BE.Prenda
                            {
                                IdPrenda  = idPrenda,
                                Nombre    = row["Nombre"].ToString(),
                                Talle     = row["Talle"] != DBNull.Value ? row["Talle"].ToString() : null,
                                Color     = row["Color"] != DBNull.Value ? row["Color"].ToString() : null,
                                Categoria = row["Categoria"] != DBNull.Value ? row["Categoria"].ToString() : null,
                                Estado    = (BE.EstadoPrenda)Convert.ToInt32(row["Estado"])
                            }
                        };
                        porPrenda[idPrenda] = faltante;
                        orden.Add(idPrenda);
                    }

                    if (row["AltId"] != DBNull.Value)
                        faltante.Alternativas.Add(new BE.Prenda
                        {
                            IdPrenda  = Convert.ToInt32(row["AltId"]),
                            Nombre    = row["AltNombre"].ToString(),
                            Talle     = row["AltTalle"] != DBNull.Value ? row["AltTalle"].ToString() : null,
                            Color     = row["AltColor"] != DBNull.Value ? row["AltColor"].ToString() : null,
                            Categoria = row["AltCategoria"] != DBNull.Value ? row["AltCategoria"].ToString() : null,
                            Estado    = (BE.EstadoPrenda)Convert.ToInt32(row["AltEstado"])
                        });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener el informe de faltantes del pedido ID {idPedido}.", ex);
            }
            return orden.ConvertAll(id => porPrenda[id]);
        }

        // Inserta las líneas del pedido (sin confirmar) sobre una transacción ya abierta.
        private static void InsertarLineasEnTx(SqlConnection conexion, SqlTransaction tx,
                                               int idPedido, List<BE.Prenda> prendas)
        {
            foreach (var prenda in prendas)
            {
                using (var cmdPP = new SqlCommand(
                    "INSERT INTO PedidoPrenda (IdPedido, IdPrenda, Confirmada) VALUES (@IdPedido, @IdPrenda, 0)",
                    conexion, tx))
                {
                    cmdPP.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmdPP.Parameters.AddWithValue("@IdPrenda", prenda.IdPrenda);
                    cmdPP.ExecuteNonQuery();
                }
            }
        }

        // Descarta el informe de faltantes anterior (alternativas primero, por la FK).
        private static void BorrarInformeFaltantesEnTx(SqlConnection conexion, SqlTransaction tx, int idPedido)
        {
            Ejecutar(conexion, tx, "DELETE FROM PedidoFaltanteAlternativa WHERE IdPedido=@IdPedido", idPedido);
            Ejecutar(conexion, tx, "DELETE FROM PedidoFaltante WHERE IdPedido=@IdPedido", idPedido);
        }

        // Ejecuta una sentencia parametrizada solo por @IdPedido sobre una transacción abierta.
        private static void Ejecutar(SqlConnection conexion, SqlTransaction tx, string sql, int idPedido)
        {
            using (var cmd = new SqlCommand(sql, conexion, tx))
            {
                cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                cmd.ExecuteNonQuery();
            }
        }

        private static BE.AppException EstadoCambiado(int idPedido) =>
            new BE.AppException("err.dal.pedido.estado_cambiado",
                "El Pedido #{0} cambió de estado en otra sesión. Actualizá la lista y volvé a intentarlo.",
                idPedido);

        // Marca un pedido como Despachado y registra la fecha. Claim atómico: solo pasa si sigue
        // Pendiente (formalizado); si otra sesión lo movió, no se afecta ninguna fila y se rechaza.
        public void Despachar(int idPedido)
        {
            int afectadas = acceso.Escribir(
                "UPDATE Pedido SET Estado=@Estado, FechaDespacho=@FechaDespacho " +
                "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                new SqlParameter[]
                {
                    new SqlParameter("@Estado",        SqlDbType.Int) { Value = (int)BE.EstadoPedido.Despachado },
                    new SqlParameter("@FechaDespacho", DateTime.Now),
                    new SqlParameter("@IdPedido",      idPedido),
                    // Pendiente vale 0: se asigna Value explícito (el literal 0 elegiría el
                    // constructor (nombre, SqlDbType) y el parámetro quedaría sin valor).
                    new SqlParameter("@Esperado",      SqlDbType.Int) { Value = (int)BE.EstadoPedido.Pendiente }
                });
            if (afectadas == 0) throw EstadoCambiado(idPedido);
            ActualizarDV(idPedido);   // T07
        }

        // Marca un pedido como Entregado y registra la fecha. Claim atómico: solo si sigue Despachado.
        public void MarcarEntregado(int idPedido)
        {
            int afectadas = acceso.Escribir(
                "UPDATE Pedido SET Estado=@Estado, FechaEntrega=@FechaEntrega " +
                "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                new SqlParameter[]
                {
                    new SqlParameter("@Estado",       SqlDbType.Int) { Value = (int)BE.EstadoPedido.Entregado },
                    new SqlParameter("@FechaEntrega", DateTime.Now),
                    new SqlParameter("@IdPedido",     idPedido),
                    new SqlParameter("@Esperado",     SqlDbType.Int) { Value = (int)BE.EstadoPedido.Despachado }
                });
            if (afectadas == 0) throw EstadoCambiado(idPedido);
            ActualizarDV(idPedido);   // T07
        }

        // Pasa a EnLimpieza SOLO las prendas del pedido que siguen EnUso por este cliente y registra
        // la FechaDevolucion del pedido (PN04), todo en la misma transacción.
        // Es idempotente: una segunda devolución del mismo pedido no afecta filas (las prendas
        // ya no están EnUso) y no pisa prendas que ya hayan vuelto a circular en otro pedido.
        // Devuelve la cantidad de prendas efectivamente devueltas.
        public int RegistrarDevolucion(int idPedido, int idCliente)
        {
            int afectadas = 0;
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                // T07: bloqueo del DV de Pedido primero; el DV se recalcula antes del commit (abajo).
                DigitoVerificador.Bloquear(conexion, tx, DV_Tabla);

                // Abre el registro de mantenimiento de cada prenda que entra a limpieza (antes que el
                // UPDATE, con el mismo criterio de selección): sin esto las devoluciones no quedaban en
                // el historial de mantenimiento ni en el análisis de tiempos (PdN11), porque el cambio de
                // estado se hace en SQL directo y no pasa por BLL.Prenda.CambiarEstado.
                using (var cmd = new SqlCommand(
                    "INSERT INTO MantenimientoPrenda (IdPrenda, FechaEntrada, Actor, Origen) " +
                    "SELECT IdPrenda, GETDATE(), NULL, @OrigenDevolucion FROM Prenda " +
                    "WHERE Estado=@EstadoEnUso AND IdClienteActual=@IdCliente AND IdPrenda IN " +
                    "  (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido) " +
                    "AND NOT EXISTS (SELECT 1 FROM MantenimientoPrenda m " +
                    "                WHERE m.IdPrenda = Prenda.IdPrenda AND m.FechaSalida IS NULL)",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@EstadoEnUso", (int)BE.EstadoPrenda.EnUso);
                    cmd.Parameters.AddWithValue("@IdCliente",   idCliente);
                    cmd.Parameters.AddWithValue("@IdPedido",    idPedido);
                    // Origen = Devolución: es lo que lleva la prenda a la Inspección de Devolución (PN04).
                    cmd.Parameters.Add(new SqlParameter("@OrigenDevolucion", SqlDbType.TinyInt) { Value = (byte)BE.OrigenMantenimiento.Devolucion });
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(
                    "UPDATE Prenda SET Estado=@Estado, IdClienteActual=NULL " +
                    "WHERE Estado=@EstadoEnUso AND IdClienteActual=@IdCliente AND IdPrenda IN " +
                    "  (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido)",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Estado",      (int)BE.EstadoPrenda.EnLimpieza);
                    cmd.Parameters.AddWithValue("@EstadoEnUso", (int)BE.EstadoPrenda.EnUso);
                    cmd.Parameters.AddWithValue("@IdCliente",   idCliente);
                    cmd.Parameters.AddWithValue("@IdPedido",    idPedido);
                    afectadas = cmd.ExecuteNonQuery();
                }

                // PN04: el pedido sigue Entregado pero queda marcado como devuelto (misma transacción
                // que las prendas). Solo si efectivamente volvió alguna prenda y la fecha no estaba:
                // una segunda devolución no la pisa.
                if (afectadas > 0)
                {
                    using (var cmd = new SqlCommand(
                        "UPDATE Pedido SET FechaDevolucion=@FechaDevolucion " +
                        "WHERE IdPedido=@IdPedido AND FechaDevolucion IS NULL",
                        conexion, tx))
                    {
                        cmd.Parameters.Add("@FechaDevolucion", SqlDbType.DateTime).Value = DateTime.Now;
                        cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                        cmd.ExecuteNonQuery();
                    }
                    ActualizarDVEnTx(conexion, tx, idPedido);   // T07, en la misma transacción
                }
            });
            return afectadas;
        }

        // Reconcilia el estado de las prendas del pedido con el estado ACTUAL del pedido, sobre
        // una transacción YA abierta (la de RestaurarOperacionAtomica). Nunca reasigna a ciegas:
        //   • estado con prendas reservadas (Separado/Pendiente/Despachado/Entregado): las que
        //     están Disponibles se reservan con "WHERE Estado=Disponible"; si al final no TODAS
        //     las líneas quedaron EnUso a nombre de este cliente (alguna la tiene otro cliente,
        //     está en limpieza, de baja, etc.) se lanza y la transacción se revierte entera;
        //   • Cancelado: se liberan SOLO las prendas que siguen EnUso por ESTE cliente
        //     ("WHERE Estado=EnUso AND IdClienteActual=@IdCliente"), sin pisar las de otros.
        // PN01: un pedido en control de stock, con faltantes o desistido nunca reservó nada.
        private void ReconciliarEnTx(SqlConnection conexion, SqlTransaction tx, int idPedido)
        {
            int estado, idCliente;
            using (var cmd = new SqlCommand(
                "SELECT Estado, IdCliente FROM Pedido WHERE IdPedido=@IdPedido", conexion, tx))
            {
                cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return;
                    estado    = Convert.ToInt32(rd["Estado"]);
                    idCliente = Convert.ToInt32(rd["IdCliente"]);
                }
            }

            if (estado == (int)BE.EstadoPedido.Cancelado)
            {
                LiberarPrendasEnTx(conexion, tx, idPedido, idCliente);
                return;
            }

            bool reservado = estado == (int)BE.EstadoPedido.Separado  ||
                             estado == (int)BE.EstadoPedido.Pendiente ||
                             estado == (int)BE.EstadoPedido.Despachado ||
                             estado == (int)BE.EstadoPedido.Entregado;
            if (!reservado) return;

            using (var cmd = new SqlCommand(
                // IdUltimoCliente NUNCA se limpia: registra quién tuvo la prenda por última vez.
                "UPDATE Prenda SET Estado=@EnUso, IdClienteActual=@IdCliente, IdUltimoCliente=@IdCliente " +
                "WHERE Estado=@Disponible AND IdPrenda IN (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido)",
                conexion, tx))
            {
                cmd.Parameters.AddWithValue("@EnUso",      (int)BE.EstadoPrenda.EnUso);
                cmd.Parameters.AddWithValue("@Disponible", (int)BE.EstadoPrenda.Disponible);
                cmd.Parameters.AddWithValue("@IdCliente",  idCliente);
                cmd.Parameters.AddWithValue("@IdPedido",   idPedido);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) AS Lineas, " +
                "       SUM(CASE WHEN pr.Estado=@EnUso AND pr.IdClienteActual=@IdCliente THEN 1 ELSE 0 END) AS Reservadas " +
                "FROM PedidoPrenda pp INNER JOIN Prenda pr ON pr.IdPrenda = pp.IdPrenda WHERE pp.IdPedido=@IdPedido",
                conexion, tx))
            {
                cmd.Parameters.AddWithValue("@EnUso",     (int)BE.EstadoPrenda.EnUso);
                cmd.Parameters.AddWithValue("@IdCliente", idCliente);
                cmd.Parameters.AddWithValue("@IdPedido",  idPedido);
                using (var rd = cmd.ExecuteReader())
                {
                    rd.Read();
                    int lineas     = Convert.ToInt32(rd["Lineas"]);
                    int reservadas = rd["Reservadas"] == DBNull.Value ? 0 : Convert.ToInt32(rd["Reservadas"]);
                    if (reservadas < lineas)
                        throw new BE.AppException("err.dal.pedido.restaurar_prendas",
                            "No se puede restaurar el Pedido #{0}: una o más de sus prendas ya no están disponibles " +
                            "para el cliente (las tiene otro pedido, están en limpieza o de baja).", idPedido);
                }
            }
        }

        // Libera a Disponible SOLO las prendas del pedido que siguen EnUso por este cliente.
        private static void LiberarPrendasEnTx(SqlConnection conexion, SqlTransaction tx, int idPedido, int idCliente)
        {
            using (var cmd = new SqlCommand(
                "UPDATE Prenda SET Estado=@Disponible, IdClienteActual=NULL " +
                "WHERE Estado=@EnUso AND IdClienteActual=@IdCliente " +
                "  AND IdPrenda IN (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido)",
                conexion, tx))
            {
                cmd.Parameters.AddWithValue("@Disponible", (int)BE.EstadoPrenda.Disponible);
                cmd.Parameters.AddWithValue("@EnUso",      (int)BE.EstadoPrenda.EnUso);
                cmd.Parameters.AddWithValue("@IdCliente",  idCliente);
                cmd.Parameters.AddWithValue("@IdPedido",   idPedido);
                cmd.ExecuteNonQuery();
            }
        }

        // ── T06b — Restauración ATÓMICA desde el historial ──────────────────────
        // Revierte los campos del pedido a sus valores anteriores Y reconcilia sus prendas dentro de
        // UNA ÚNICA transacción. Claim atómico: el primer paso exige que el pedido siga en
        // 'estadoEsperado' (el ValorNuevo de la operación que se revierte); si otra sesión lo movió,
        // no se toca nada. El DV se recalcula después de confirmar.
        public void RestaurarOperacionAtomica(int idPedido, BE.EstadoPedido estadoEsperado,
                                              IList<(string Campo, string ValorAnterior)> campos)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Pedido SET Estado = Estado WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmd.Parameters.AddWithValue("@Esperado", (int)estadoEsperado);
                    if (cmd.ExecuteNonQuery() == 0) throw EstadoCambiado(idPedido);
                }
                foreach (var c in campos)
                    RestaurarCampoEnTx(conexion, tx, idPedido, c.Campo, c.ValorAnterior);
                ReconciliarEnTx(conexion, tx, idPedido);
            });
            ActualizarDV(idPedido);   // T07
        }

        // Restaura un campo de [Pedido] a su valor anterior, sobre una transacción YA abierta.
        // Campos soportados: Estado | FechaDespacho | FechaEntrega | MotivoCancelacion.
        // "Prendas" (informativo: el estado de las prendas se reconcilia aparte) y "FechaPedido"
        // (inmutable una vez creado el pedido) se omiten silenciosamente.
        private void RestaurarCampoEnTx(SqlConnection conexion, SqlTransaction tx,
                                        int idPedido, string campo, string valorAnterior)
        {
            string sql;
            SqlParameter[] parametros;

            switch (campo)
            {
                case "Estado":
                    if (!Enum.TryParse(valorAnterior, out BE.EstadoPedido estado))
                        throw new Exception($"Valor de estado inválido para restaurar: '{valorAnterior}'.");
                    sql = "UPDATE Pedido SET Estado = @Valor WHERE IdPedido = @IdPedido";
                    parametros = new[]
                    {
                        new SqlParameter("@Valor",    (int)estado),
                        new SqlParameter("@IdPedido", idPedido)
                    };
                    break;

                case "FechaDespacho":
                    sql = "UPDATE Pedido SET FechaDespacho = @Valor WHERE IdPedido = @IdPedido";
                    parametros = new[]
                    {
                        new SqlParameter("@Valor", string.IsNullOrEmpty(valorAnterior)
                            ? (object)DBNull.Value
                            : DateTime.ParseExact(valorAnterior, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                        new SqlParameter("@IdPedido", idPedido)
                    };
                    break;

                case "FechaEntrega":
                    sql = "UPDATE Pedido SET FechaEntrega = @Valor WHERE IdPedido = @IdPedido";
                    parametros = new[]
                    {
                        new SqlParameter("@Valor", string.IsNullOrEmpty(valorAnterior)
                            ? (object)DBNull.Value
                            : DateTime.ParseExact(valorAnterior, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                        new SqlParameter("@IdPedido", idPedido)
                    };
                    break;

                case "MotivoCancelacion":
                    sql = "UPDATE Pedido SET MotivoCancelacion = @Valor WHERE IdPedido = @IdPedido";
                    parametros = new[]
                    {
                        new SqlParameter("@Valor", string.IsNullOrEmpty(valorAnterior)
                            ? (object)DBNull.Value : valorAnterior),
                        new SqlParameter("@IdPedido", idPedido)
                    };
                    break;

                case "Prendas":      // informativo — el estado de las prendas se reconcilia aparte
                case "FechaPedido":  // inmutable una vez creado el pedido
                    return;

                default:
                    throw new Exception($"Campo '{campo}' no es restaurable desde el historial.");
            }

            using (var cmd = new SqlCommand(sql, conexion, tx))
            {
                cmd.Parameters.AddRange(parametros);
                cmd.ExecuteNonQuery();
            }
        }

        // Cancela el pedido (formalizado = Pendiente, o con las prendas ya separadas = Separado),
        // guarda el motivo y libera las prendas. Todo en una transacción:
        //   • claim atómico "WHERE Estado=@Esperado": si otra sesión ya lo despachó/canceló, no se
        //     toca nada y se rechaza;
        //   • solo se liberan las prendas que siguen EnUso por ESTE cliente (no se pisa una prenda
        //     que ya volvió a circular en otro pedido).
        public void Cancelar(int idPedido, int idCliente, BE.EstadoPedido estadoEsperado, string motivo)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmdPedido = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Estado, MotivoCancelacion=@Motivo " +
                    "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                    conexion, tx))
                {
                    cmdPedido.Parameters.AddWithValue("@Estado",   (int)BE.EstadoPedido.Cancelado);
                    cmdPedido.Parameters.AddWithValue("@Motivo",   (object)motivo ?? DBNull.Value);
                    cmdPedido.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmdPedido.Parameters.AddWithValue("@Esperado", (int)estadoEsperado);
                    if (cmdPedido.ExecuteNonQuery() == 0) throw EstadoCambiado(idPedido);
                }

                LiberarPrendasEnTx(conexion, tx, idPedido, idCliente);
            });
            ActualizarDV(idPedido);   // T07
        }

        // Revierte la cancelación. Devuelve false si alguna prenda ya no está Disponible.
        // La verificación de disponibilidad se ejecuta DENTRO de la transacción para
        // evitar race conditions: si otra operación cambia el estado de una prenda entre
        // la verificación y el UPDATE, la transacción lo detecta con bloqueo consistente.
        // Reactivar un pedido cancelado: vuelve a "Enviar selección para control stock"
        // (Cancelado → EnControlStock) SIN reservar prendas; Depósito revisa otra vez el stock.
        // Claim atómico: solo pasa si sigue Cancelado. Devuelve false si otra sesión lo cambió.
        public bool DesCancelar(int idPedido, int idCliente)
        {
            bool ok = false;
            ConPedidoActivoUnico(() => acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Nuevo, MotivoCancelacion=NULL, FechaEnvioControl=@Ahora, " +
                    "       FechaControl=NULL, IdEmpleadoControl=NULL, FechaSeparacion=NULL, FechaFormalizacion=NULL " +
                    "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@Nuevo",    (int)BE.EstadoPedido.EnControlStock);
                    cmd.Parameters.AddWithValue("@Ahora",    DateTime.Now);
                    cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmd.Parameters.AddWithValue("@Esperado", (int)BE.EstadoPedido.Cancelado);
                    ok = cmd.ExecuteNonQuery() > 0;
                }
                if (!ok) return;
                BorrarInformeFaltantesEnTx(conexion, tx, idPedido);
                Ejecutar(conexion, tx, "UPDATE PedidoPrenda SET Confirmada = 0 WHERE IdPedido=@IdPedido", idPedido);
            }));
            if (ok) ActualizarDV(idPedido);   // T07
            return ok;
        }

        private List<BE.Prenda> ObtenerPrendasDePedido(int idPedido)
        {
            var lista = new List<BE.Prenda>();
            try
            {
            SqlParameter[] p = { new SqlParameter("@IdPedido", idPedido) };

            DataTable tabla = acceso.Leer(
                "SELECT pr.IdPrenda, pr.Nombre, pr.Descripcion, pr.Talle, pr.Color, " +
                "       pr.Categoria, pr.Estado, pr.IdClienteActual, pr.IdUltimoCliente, " +
                "       pr.PrecioReposicion, pr.FechaAlta, " +
                "       NULL AS NombreCliente " +
                "FROM PedidoPrenda pp " +
                "INNER JOIN Prenda pr ON pr.IdPrenda = pp.IdPrenda " +
                "WHERE pp.IdPedido = @IdPedido",
                p);

            foreach (DataRow row in tabla.Rows)
            {
                lista.Add(new BE.Prenda
                {
                    IdPrenda = Convert.ToInt32(row["IdPrenda"]),
                    Nombre = row["Nombre"].ToString(),
                    Descripcion = row["Descripcion"] != DBNull.Value ? row["Descripcion"].ToString() : null,
                    Talle = row["Talle"] != DBNull.Value ? row["Talle"].ToString() : null,
                    Color = row["Color"] != DBNull.Value ? row["Color"].ToString() : null,
                    Categoria = row["Categoria"] != DBNull.Value ? row["Categoria"].ToString() : null,
                    Estado = (BE.EstadoPrenda)Convert.ToInt32(row["Estado"]),
                    // PN04, CU-DEP-02 Reportar Prenda Perdida (GUI/PedidosRealizados.cs) necesita
                    // IdUltimoCliente para poder cargar el cobro contra el cliente que la tenía
                    // — sin esto, BLL.CargoPrenda.RegistrarCargo siempre rechazaba con
                    // "sin_cliente" DESPUÉS de que la prenda ya había pasado a Baja.
                    IdUltimoCliente = row["IdUltimoCliente"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdUltimoCliente"]) : null,
                    PrecioReposicion = row["PrecioReposicion"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["PrecioReposicion"]) : null,
                    FechaAlta = Convert.ToDateTime(row["FechaAlta"])
                });
            }

            return lista;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener las prendas del pedido ID {idPedido}.", ex);
            }
        }

        private BE.Pedido MapearCabecera(DataRow row)
        {
            return new BE.Pedido
            {
                IdPedido = Convert.ToInt32(row["IdPedido"]),
                IdCliente = Convert.ToInt32(row["IdCliente"]),
                IdEmpleado = Convert.ToInt32(row["IdEmpleado"]),
                Estado = (BE.EstadoPedido)Convert.ToInt32(row["Estado"]),
                FechaPedido = Convert.ToDateTime(row["FechaPedido"]),
                FechaDespacho = row["FechaDespacho"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaDespacho"]) : null,
                FechaEntrega = row["FechaEntrega"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaEntrega"]) : null,
                FechaDevolucion = FechaNula(row, "FechaDevolucion"),
                MotivoCancelacion = row.Table.Columns.Contains("MotivoCancelacion") && row["MotivoCancelacion"] != DBNull.Value
                                        ? row["MotivoCancelacion"].ToString() : null,
                FechaEnvioControl     = FechaNula(row, "FechaEnvioControl"),
                FechaControl          = FechaNula(row, "FechaControl"),
                IdEmpleadoControl     = row["IdEmpleadoControl"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdEmpleadoControl"]) : null,
                NombreEmpleadoControl = row["NombreEmpleadoControl"] != DBNull.Value ? row["NombreEmpleadoControl"].ToString() : null,
                FechaSeparacion       = FechaNula(row, "FechaSeparacion"),
                FechaFormalizacion    = FechaNula(row, "FechaFormalizacion"),
                MotivoDesistimiento   = row["MotivoDesistimiento"] != DBNull.Value ? row["MotivoDesistimiento"].ToString() : null,
                EtapaDesistimiento    = row["EtapaDesistimiento"] != DBNull.Value
                                        && Enum.TryParse(row["EtapaDesistimiento"].ToString(), out BE.EtapaDesistimiento etapa)
                                            ? (BE.EtapaDesistimiento?)etapa : null,
                NombreCliente = row["NombreCliente"].ToString(),
                NombreEmpleado = row["NombreEmpleado"].ToString()
            };
        }

        private static DateTime? FechaNula(DataRow row, string columna) =>
            row[columna] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row[columna]) : null;

        // Ids de las líneas del pedido que Depósito confirmó como disponibles.
        private List<int> ObtenerPrendasConfirmadas(int idPedido)
        {
            var ids = new List<int>();
            DataTable tabla = acceso.Leer(
                "SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido = @IdPedido AND Confirmada = 1",
                new[] { new SqlParameter("@IdPedido", idPedido) });
            if (tabla != null)
                foreach (DataRow row in tabla.Rows) ids.Add(Convert.ToInt32(row["IdPrenda"]));
            return ids;
        }

        // ── T07 — DV MULTI-TABLA del Pedido ─────────────────────────────────────
        // El estado de un Pedido se compone de su fila MÁS sus líneas (PedidoPrenda).
        // El DVH incorpora un digest de las líneas (prenda y si Depósito la confirmó): así,
        // agregar / quitar / intercambiar / confirmar prendas por fuera del sistema se detecta.
        // Formato 2: además de cliente/empleado/estado entran las fechas del ciclo, los motivos y
        // las columnas del circuito de control de stock (PN01), con formato invariante.
        public const string DV_Tabla = "Pedido";

        private const string SELECT_DV =
            "SELECT IdPedido, IdCliente, IdEmpleado, Estado, FechaPedido, FechaDespacho, FechaEntrega, " +
            "       MotivoCancelacion, FechaEnvioControl, FechaControl, IdEmpleadoControl, FechaSeparacion, " +
            "       FechaFormalizacion, MotivoDesistimiento, EtapaDesistimiento, FechaDevolucion, DVH FROM Pedido";

        // PN04: FechaDevolucion también entra (falsearla escondería un pedido atrasado); el script
        // pidió el recálculo único al agregar la columna (sección 21z3).
        private static readonly string[] ColumnasDV =
        {
            "IdPedido", "IdCliente", "IdEmpleado", "Estado", "FechaPedido", "FechaDespacho", "FechaEntrega",
            "MotivoCancelacion", "FechaEnvioControl", "FechaControl", "IdEmpleadoControl", "FechaSeparacion",
            "FechaFormalizacion", "MotivoDesistimiento", "EtapaDesistimiento", "FechaDevolucion"
        };

        private static BE.FilaDV MapearFilaDV(DataRow row, string digestLineas)
        {
            int id = Convert.ToInt32(row["IdPedido"]);
            var campos = new List<string>();
            foreach (var c in ColumnasDV) campos.Add(DigitoVerificador.Formatear(row[c]));
            campos.Add(digestLineas ?? "");
            return new BE.FilaDV
            {
                Id            = id,
                Campos        = campos.ToArray(),
                DVHAlmacenado = row["DVH"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DVH"]),
                Descripcion   = "Pedido #" + id
            };
        }

        // Huellas de las líneas de todos los pedidos (o de uno): "IdPrenda:Confirmada" ordenadas.
        private static Dictionary<int, string> Digests(DataTable lineas)
        {
            var porPedido = new Dictionary<int, List<string>>();
            foreach (DataRow r in lineas.Rows)
            {
                int id = Convert.ToInt32(r["IdPedido"]);
                if (!porPedido.TryGetValue(id, out var l)) porPedido[id] = l = new List<string>();
                l.Add(DigitoVerificador.Formatear(r["IdPrenda"]) + ":" + DigitoVerificador.Formatear(r["Confirmada"]));
            }
            var res = new Dictionary<int, string>();
            foreach (var kv in porPedido) res[kv.Key] = string.Join(",", kv.Value);
            return res;
        }

        private const string SELECT_LINEAS_DV = "SELECT IdPedido, IdPrenda, Confirmada FROM PedidoPrenda";

        public List<BE.FilaDV> ObtenerFilasDV()
        {
            var lista = new List<BE.FilaDV>();
            DataTable dt = acceso.Leer(SELECT_DV + " ORDER BY IdPedido", null);
            var digests = Digests(acceso.Leer(SELECT_LINEAS_DV + " ORDER BY IdPedido, IdPrenda", null));
            foreach (DataRow row in dt.Rows)
            {
                digests.TryGetValue(Convert.ToInt32(row["IdPedido"]), out var d);
                lista.Add(MapearFilaDV(row, d));
            }
            return lista;
        }

        // Recalcula el DVH de UN pedido (fila + líneas) y el DVV de la tabla desde los DVH
        // almacenados, con el bloqueo del DV tomado. Best-effort: la operación de negocio ya
        // quedó confirmada; si el recálculo falla, la verificación de integridad lo detecta.
        public void ActualizarDV(int idPedido)
        {
            try
            {
                new DigitoVerificador().EjecutarConBloqueo(DV_Tabla, (cn, tx) => ActualizarDVEnTx(cn, tx, idPedido));
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[DAL.Pedido.ActualizarDV] " + ex.Message); }
        }

        // Recalcula el DVH del pedido (con sus líneas) y el DVV DENTRO de una transacción que ya tiene
        // tomado el bloqueo del DV de Pedido (DigitoVerificador.Bloquear al empezar la transacción).
        internal void ActualizarDVEnTx(SqlConnection cn, SqlTransaction tx, int idPedido)
        {
            var p = new SqlParameter("@id", idPedido);
            var dt = DigitoVerificador.LeerEnTx(cn, tx, SELECT_DV + " WHERE IdPedido = @id", p);
            if (dt.Rows.Count > 0)
            {
                var lineas = DigitoVerificador.LeerEnTx(cn, tx,
                    SELECT_LINEAS_DV + " WHERE IdPedido = @id ORDER BY IdPrenda", new SqlParameter("@id", idPedido));
                Digests(lineas).TryGetValue(idPedido, out var d);
                var fila = MapearFilaDV(dt.Rows[0], d);
                using (var cmd = new SqlCommand("UPDATE Pedido SET DVH=@dvh WHERE IdPedido=@id", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@dvh", Seguridad.CalculadorDV.Crear().CalcularDVH(fila.Campos));
                    cmd.Parameters.AddWithValue("@id", idPedido);
                    cmd.ExecuteNonQuery();
                }
            }
            DigitoVerificador.GuardarDVVDesdeAlmacenadosEnTx(cn, tx, DV_Tabla, "IdPedido");
        }

        // Recalcula el DVH de TODOS los pedidos y el DVV. Acepta los datos actuales como legítimos:
        // solo para el recálculo administrativo explícito y la inicialización tras instalar.
        // Propaga cualquier excepción (el administrador necesita enterarse si falla).
        public void RecalcularDV()
        {
            new DigitoVerificador().EjecutarConBloqueo(DV_Tabla, (cn, tx) =>
            {
                var svc = Seguridad.CalculadorDV.Crear();
                var dt = DigitoVerificador.LeerEnTx(cn, tx, SELECT_DV + " ORDER BY IdPedido");
                var digests = Digests(DigitoVerificador.LeerEnTx(cn, tx, SELECT_LINEAS_DV + " ORDER BY IdPedido, IdPrenda"));
                var dvhs = new List<int>();
                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["IdPedido"]);
                    digests.TryGetValue(id, out var d);
                    int dvh = svc.CalcularDVH(MapearFilaDV(row, d).Campos);
                    using (var cmd = new SqlCommand("UPDATE Pedido SET DVH=@dvh WHERE IdPedido=@id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@dvh", dvh);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    dvhs.Add(dvh);
                }
                DigitoVerificador.GuardarDVVEnTx(cn, tx, DV_Tabla, svc.CalcularDVV(dvhs));
            });
        }
    }
}
