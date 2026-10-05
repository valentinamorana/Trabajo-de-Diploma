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
            "       ped.FechaPedido, ped.FechaDespacho, ped.FechaEntrega, " +
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
        public Dictionary<int, int> ObtenerCantidadPedidosPorPrenda()
        {
            var resultado = new Dictionary<int, int>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT pp.IdPrenda, COUNT(*) AS Cantidad " +
                    "FROM PedidoPrenda pp " +
                    "INNER JOIN Pedido p ON p.IdPedido = pp.IdPedido " +
                    "WHERE p.Estado IN " + ESTADOS_VENTA + " " +
                    "GROUP BY pp.IdPrenda",
                    null);

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

            acceso.EjecutarTransaccion((conexion, tx) =>
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
            });

            RecalcularDVSilencioso();   // T07: DV multi-tabla (pedido + líneas)
            return idNuevo;
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
            RecalcularDVSilencioso();   // T07 — cambiaron las líneas del pedido
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
            RecalcularDVSilencioso();   // T07
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
            RecalcularDVSilencioso();   // T07
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
            RecalcularDVSilencioso();   // T07
        }

        // "Asentar desistimiento" de un pedido con faltantes informados: ConFaltantes → Desistido.
        // No hay prendas que liberar: nunca se reservaron.
        public void RegistrarDesistimiento(int idPedido, string motivo, BE.EtapaDesistimiento etapa)
        {
            int afectadas = acceso.Escribir(
                "UPDATE Pedido SET Estado=@Nuevo, MotivoDesistimiento=@Motivo, EtapaDesistimiento=@Etapa " +
                "WHERE IdPedido=@IdPedido AND Estado=@Esperado",
                new SqlParameter[]
                {
                    new SqlParameter("@Nuevo",    (int)BE.EstadoPedido.Desistido),
                    new SqlParameter("@Motivo",   (object)motivo ?? DBNull.Value),
                    new SqlParameter("@Etapa",    etapa.ToString()),
                    new SqlParameter("@IdPedido", idPedido),
                    new SqlParameter("@Esperado", (int)BE.EstadoPedido.ConFaltantes)
                });
            if (afectadas == 0) throw EstadoCambiado(idPedido);
            RecalcularDVSilencioso();   // T07
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

        // Marca un pedido como Despachado y registra la fecha.
        public void Despachar(int idPedido)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Pedido SET Estado=@Estado, FechaDespacho=@FechaDespacho " +
                    "WHERE IdPedido=@IdPedido",
                    new SqlParameter[]
                    {
                        new SqlParameter("@Estado",        (int)BE.EstadoPedido.Despachado),
                        new SqlParameter("@FechaDespacho", DateTime.Now),
                        new SqlParameter("@IdPedido",      idPedido)
                    });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al despachar el pedido ID {idPedido}.", ex);
            }
            RecalcularDVSilencioso();   // T07
        }

        // Marca un pedido como Entregado y registra la fecha.
        public void MarcarEntregado(int idPedido)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Pedido SET Estado=@Estado, FechaEntrega=@FechaEntrega " +
                    "WHERE IdPedido=@IdPedido",
                    new SqlParameter[]
                    {
                        new SqlParameter("@Estado",       (int)BE.EstadoPedido.Entregado),
                        new SqlParameter("@FechaEntrega", DateTime.Now),
                        new SqlParameter("@IdPedido",     idPedido)
                    });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al marcar como entregado el pedido ID {idPedido}.", ex);
            }
            RecalcularDVSilencioso();   // T07
        }

        // Pasa a EnLimpieza SOLO las prendas del pedido que siguen EnUso por este cliente.
        // Es idempotente: una segunda devolución del mismo pedido no afecta filas (las prendas
        // ya no están EnUso) y no pisa prendas que ya hayan vuelto a circular en otro pedido.
        // Devuelve la cantidad de prendas efectivamente devueltas.
        public int RegistrarDevolucion(int idPedido, int idCliente)
        {
            int afectadas = 0;
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                // Abre el registro de mantenimiento de cada prenda que entra a limpieza (antes que el
                // UPDATE, con el mismo criterio de selección): sin esto las devoluciones no quedaban en
                // el historial de mantenimiento ni en el análisis de tiempos (PdN11), porque el cambio de
                // estado se hace en SQL directo y no pasa por BLL.Prenda.CambiarEstado.
                using (var cmd = new SqlCommand(
                    "INSERT INTO MantenimientoPrenda (IdPrenda, FechaEntrada, Actor) " +
                    "SELECT IdPrenda, GETDATE(), N'Devolución' FROM Prenda " +
                    "WHERE Estado=@EstadoEnUso AND IdClienteActual=@IdCliente AND IdPrenda IN " +
                    "  (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido) " +
                    "AND NOT EXISTS (SELECT 1 FROM MantenimientoPrenda m " +
                    "                WHERE m.IdPrenda = Prenda.IdPrenda AND m.FechaSalida IS NULL)",
                    conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@EstadoEnUso", (int)BE.EstadoPrenda.EnUso);
                    cmd.Parameters.AddWithValue("@IdCliente",   idCliente);
                    cmd.Parameters.AddWithValue("@IdPedido",    idPedido);
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
            });
            if (afectadas > 0) RecalcularDVSilencioso();   // T07 — mantener el DV del pedido consistente
            return afectadas;
        }

        // Reconcilia el estado de las prendas del pedido con el estado ACTUAL del pedido.
        // Se usa tras restaurar un pedido desde el historial: si quedó en un estado activo
        // (Pendiente/Despachado/Entregado) sus prendas deben estar EnUso del cliente; si quedó
        // Cancelado, deben estar Disponibles. Evita dejar el stock inconsistente.
        public void ReconciliarPrendasConEstado(int idPedido)
        {
            acceso.EjecutarTransaccion((conexion, tx) => ReconciliarEnTx(conexion, tx, idPedido));
        }

        // Núcleo de la reconciliación, sobre una transacción YA abierta. Reutilizable por la
        // restauración atómica (RestaurarOperacionAtomica) para no abrir una segunda transacción.
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

            bool cancelado = estado == (int)BE.EstadoPedido.Cancelado;

            // PN01: las prendas solo están reservadas (EnUso) desde que Depósito las separa. Un
            // pedido que quedó en control de stock, con faltantes o desistido nunca reservó
            // nada: no se tocan sus prendas (podrían estar en uso por otro pedido).
            bool reservado = estado == (int)BE.EstadoPedido.Separado  ||
                             estado == (int)BE.EstadoPedido.Pendiente ||
                             estado == (int)BE.EstadoPedido.Despachado ||
                             estado == (int)BE.EstadoPedido.Entregado;
            if (!cancelado && !reservado) return;

            using (var cmd = new SqlCommand(
                // IdUltimoCliente NUNCA se limpia (a diferencia de IdClienteActual): si se cancela,
                // @IdClienteUltimo llega NULL y COALESCE conserva el valor que ya tenía la prenda.
                "UPDATE Prenda SET Estado=@Estado, IdClienteActual=@IdCliente, " +
                "IdUltimoCliente=COALESCE(@IdClienteUltimo, IdUltimoCliente) " +
                "WHERE IdPrenda IN (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido)",
                conexion, tx))
            {
                cmd.Parameters.AddWithValue("@Estado",
                    cancelado ? (int)BE.EstadoPrenda.Disponible : (int)BE.EstadoPrenda.EnUso);
                cmd.Parameters.AddWithValue("@IdCliente",
                    cancelado ? (object)DBNull.Value : idCliente);
                cmd.Parameters.AddWithValue("@IdClienteUltimo",
                    cancelado ? (object)DBNull.Value : idCliente);
                cmd.Parameters.AddWithValue("@IdPedido", idPedido);
                cmd.ExecuteNonQuery();
            }
        }

        // ── T06b — Restauración ATÓMICA desde el historial ──────────────────────
        // Revierte TODOS los campos del pedido a sus valores anteriores Y reconcilia el estado
        // de sus prendas dentro de UNA ÚNICA transacción: si cualquier paso falla, se revierte
        // todo (EjecutarTransaccion hace Rollback), evitando que el pedido quede con un estado y
        // las prendas con otro. El DV se recalcula aparte (es recomputable y no debe abortar el rollback).
        public void RestaurarOperacionAtomica(int idPedido, IList<(string Campo, string ValorAnterior)> campos)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                foreach (var c in campos)
                    RestaurarCampoEnTx(conexion, tx, idPedido, c.Campo, c.ValorAnterior);
                ReconciliarEnTx(conexion, tx, idPedido);
            });
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

        // Cancela el pedido, guarda el motivo y libera las prendas a Disponible.
        // Ambas operaciones se ejecutan en una única transacción: si falla alguna,
        // ningún cambio queda aplicado (integridad transaccional).
        public void Cancelar(int idPedido, string motivo)
        {
            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                using (var cmdPedido = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Estado, MotivoCancelacion=@Motivo " +
                    "WHERE IdPedido=@IdPedido",
                    conexion, tx))
                {
                    cmdPedido.Parameters.AddWithValue("@Estado",   (int)BE.EstadoPedido.Cancelado);
                    cmdPedido.Parameters.AddWithValue("@Motivo",   (object)motivo ?? DBNull.Value);
                    cmdPedido.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmdPedido.ExecuteNonQuery();
                }

                // Liberar prendas del pedido → Disponible
                using (var cmdPrendas = new SqlCommand(
                    "UPDATE Prenda SET Estado=@Estado, IdClienteActual=NULL " +
                    "WHERE IdPrenda IN (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido)",
                    conexion, tx))
                {
                    cmdPrendas.Parameters.AddWithValue("@Estado",   (int)BE.EstadoPrenda.Disponible);
                    cmdPrendas.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmdPrendas.ExecuteNonQuery();
                }
            });
            RecalcularDVSilencioso();   // T07
        }

        // Revierte la cancelación. Devuelve false si alguna prenda ya no está Disponible.
        // La verificación de disponibilidad se ejecuta DENTRO de la transacción para
        // evitar race conditions: si otra operación cambia el estado de una prenda entre
        // la verificación y el UPDATE, la transacción lo detecta con bloqueo consistente.
        public bool DesCancelar(int idPedido, int idCliente)
        {
            bool puedeReactivar = true;

            acceso.EjecutarTransaccion((conexion, tx) =>
            {
                // Verificar disponibilidad DENTRO de la transacción (con bloqueo compartido)
                using (var cmdCheck = new SqlCommand(
                    "SELECT COUNT(*) AS Ocupadas " +
                    "FROM PedidoPrenda pp " +
                    "INNER JOIN Prenda pr ON pr.IdPrenda = pp.IdPrenda " +
                    "WHERE pp.IdPedido = @IdPedido AND pr.Estado <> @Estado",
                    conexion, tx))
                {
                    cmdCheck.Parameters.AddWithValue("@Estado",   (int)BE.EstadoPrenda.Disponible);
                    cmdCheck.Parameters.AddWithValue("@IdPedido", idPedido);
                    int ocupadas = Convert.ToInt32(cmdCheck.ExecuteScalar());
                    if (ocupadas > 0)
                    {
                        puedeReactivar = false;
                        return;  // salir del lambda; la transacción se revierte en EjecutarTransaccion
                    }
                }

                using (var cmdPedido = new SqlCommand(
                    "UPDATE Pedido SET Estado=@Estado, MotivoCancelacion=NULL " +
                    "WHERE IdPedido=@IdPedido",
                    conexion, tx))
                {
                    cmdPedido.Parameters.AddWithValue("@Estado",   (int)BE.EstadoPedido.Pendiente);
                    cmdPedido.Parameters.AddWithValue("@IdPedido", idPedido);
                    cmdPedido.ExecuteNonQuery();
                }

                using (var cmdPrendas = new SqlCommand(
                    "UPDATE Prenda SET Estado=@Estado, IdClienteActual=@IdCliente, IdUltimoCliente=@IdCliente " +
                    "WHERE IdPrenda IN (SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido=@IdPedido)",
                    conexion, tx))
                {
                    cmdPrendas.Parameters.AddWithValue("@Estado",    (int)BE.EstadoPrenda.EnUso);
                    cmdPrendas.Parameters.AddWithValue("@IdCliente", idCliente);
                    cmdPrendas.Parameters.AddWithValue("@IdPedido",  idPedido);
                    cmdPrendas.ExecuteNonQuery();
                }
            });

            if (puedeReactivar) RecalcularDVSilencioso();   // T07
            return puedeReactivar;
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
        // El DVH incorpora un digest de las líneas: así, agregar / quitar / intercambiar
        // prendas del pedido por fuera del sistema cambia el DVH y se detecta.
        public const string DV_Tabla = "Pedido";

        public List<BE.FilaDV> ObtenerFilasDV()
        {
            var lista = new List<BE.FilaDV>();
            DataTable dt = acceso.Leer(
                "SELECT IdPedido, IdCliente, IdEmpleado, Estado, DVH FROM Pedido ORDER BY IdPedido", null);
            if (dt == null) return lista;
            foreach (DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["IdPedido"]);
                lista.Add(new BE.FilaDV
                {
                    Id = id,
                    Campos = new[]
                    {
                        id.ToString(),
                        row["IdCliente"].ToString(),
                        row["IdEmpleado"].ToString(),
                        row["Estado"].ToString(),
                        DigestLineas(id)
                    },
                    DVHAlmacenado = row["DVH"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DVH"]),
                    Descripcion = "Pedido #" + id
                });
            }
            return lista;
        }

        // Huella de las líneas del pedido: IdPrenda concatenados y ordenados.
        private string DigestLineas(int idPedido)
        {
            DataTable dt = acceso.Leer(
                "SELECT IdPrenda FROM PedidoPrenda WHERE IdPedido = @id ORDER BY IdPrenda",
                new SqlParameter[] { new SqlParameter("@id", idPedido) });
            var ids = new List<string>();
            if (dt != null)
                foreach (DataRow r in dt.Rows) ids.Add(r["IdPrenda"].ToString());
            return string.Join(",", ids);
        }

        // Recalcula el DVH de cada Pedido y el DVV de la tabla.
        // Propaga cualquier excepción: el caller decide si ignorarla (post-restauración)
        // o dejarla subir (recálculo administrativo desde BLL.Configuracion).
        public void RecalcularDV()
        {
            var svc   = Seguridad.CalculadorDV.Crear();
            var dvDAL = new DigitoVerificador();
            var dvhs  = new List<int>();
            foreach (var f in ObtenerFilasDV())
            {
                int dvh = svc.CalcularDVH(f.Campos);
                acceso.Escribir("UPDATE Pedido SET DVH=@dvh WHERE IdPedido=@id",
                    new SqlParameter[] { new SqlParameter("@dvh", dvh), new SqlParameter("@id", f.Id) });
                dvhs.Add(dvh);
            }
            dvDAL.GuardarDVV(DV_Tabla, svc.CalcularDVV(dvhs));
        }

        // Variante "best-effort" para los puntos internos de esta clase que llaman a
        // RecalcularDV() inmediatamente después de haber confirmado (Commit) la operación de
        // negocio principal: si el recálculo del DV falla, no debe reportarse como si la
        // operación ya persistida (Alta/Despachar/Entregar/Devolución/Cancelar/DesCancelar)
        // hubiera fallado — mismo criterio que DAL.Cliente.RecalcularDV/DAL.Empleado.RecalcularDV.
        // RecalcularDV() en sí sigue propagando: la usa el recálculo administrativo manual desde
        // BLL.Configuracion, que sí necesita enterarse si falla.
        private void RecalcularDVSilencioso()
        {
            try { RecalcularDV(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[DAL.Pedido.RecalcularDV] " + ex.Message); }
        }
    }
}
