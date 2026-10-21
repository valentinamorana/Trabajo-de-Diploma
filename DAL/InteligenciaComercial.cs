using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// PN03 «Inteligencia comercial» — lecturas crudas para el Centro de decisiones comerciales.
    /// Cada consulta trae filas sin calcular nada: MRR, % de bajas, proyecciones y reglas los resuelve
    /// BLL.Inteligencia (testeable con un doble de <see cref="Interfaces.IInteligenciaComercialDAL"/>).
    /// Los umbrales del motor se guardan en ParametroSistema con el prefijo "Comercial.".
    /// </summary>
    public class InteligenciaComercial : BaseDAL, Interfaces.IInteligenciaComercialDAL
    {
        // Mismo criterio que DAL.Pedido: ventas concretadas (Pendiente de despacho, Despachado, Entregado).
        private static readonly string ESTADOS_VENTA =
            "(" + (int)BE.EstadoPedido.Pendiente + "," + (int)BE.EstadoPedido.Despachado + "," +
                  (int)BE.EstadoPedido.Entregado + ")";

        private static SqlParameter[] Rango(DateTime desde, DateTime hastaExcl) => new[]
        {
            new SqlParameter("@Desde", SqlDbType.DateTime) { Value = desde },
            new SqlParameter("@HastaExcl", SqlDbType.DateTime) { Value = hastaExcl }
        };

        private static DateTime? Fecha(object v) => v == null || v == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(v);
        private static int? Entero(object v) => v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
        private static decimal? Decimal(object v) => v == null || v == DBNull.Value ? (decimal?)null : Convert.ToDecimal(v);
        private static string Texto(object v) => v == null || v == DBNull.Value ? null : v.ToString();

        public List<BE.ClienteComercial> ObtenerClientes()
        {
            var lista = new List<BE.ClienteComercial>();
            try
            {
                DataTable t = acceso.Leer(
                    "SELECT c.IdCliente, c.Nombre, c.Apellido, c.Email, c.IdPlan, c.FechaVencimiento, c.FechaPausaHasta, c.FechaLimiteGracia, " +
                    "       (SELECT COUNT(*) FROM Prenda p WHERE p.IdClienteActual = c.IdCliente AND p.Estado = @EnUso) AS PrendasEnUso " +
                    "FROM Cliente c WHERE c.Activo = 1",
                    new[] { new SqlParameter("@EnUso", (int)BE.EstadoPrenda.EnUso) });
                foreach (DataRow r in t.Rows)
                    lista.Add(new BE.ClienteComercial
                    {
                        IdCliente = Convert.ToInt32(r["IdCliente"]),
                        Nombre = Texto(r["Nombre"]),
                        Apellido = Texto(r["Apellido"]),
                        Email = Texto(r["Email"]),
                        IdPlan = Entero(r["IdPlan"]),
                        FechaVencimiento = Fecha(r["FechaVencimiento"]),
                        FechaPausaHasta = Fecha(r["FechaPausaHasta"]),
                        FechaLimiteGracia = Fecha(r["FechaLimiteGracia"]),
                        PrendasEnUso = Convert.ToInt32(r["PrendasEnUso"])
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener los clientes para el análisis comercial.", ex); }
            return lista;
        }

        public List<BE.MovimientoSuscripcion> ObtenerMovimientos(DateTime desde, DateTime hastaExcl)
        {
            var lista = new List<BE.MovimientoSuscripcion>();
            try
            {
                // 1 Renovada, 2 CambioPlan, 3 Baja, 4 Pausada (BE.EstadoRenovacion); 0 Pendiente no es una decisión.
                DataTable t = acceso.Leer(
                    "SELECT IdCliente, IdPlanAnterior, IdPlanNuevo, FechaResolucion, Resultado FROM HistorialRenovacion " +
                    "WHERE Resultado IN (1, 2, 3, 4) AND FechaResolucion >= @Desde AND FechaResolucion < @HastaExcl",
                    Rango(desde, hastaExcl));
                foreach (DataRow r in t.Rows)
                    lista.Add(new BE.MovimientoSuscripcion
                    {
                        IdCliente = Convert.ToInt32(r["IdCliente"]),
                        IdPlanAnterior = Entero(r["IdPlanAnterior"]),
                        IdPlanNuevo = Entero(r["IdPlanNuevo"]),
                        Fecha = Convert.ToDateTime(r["FechaResolucion"]),
                        Resultado = (BE.EstadoRenovacion)Convert.ToInt32(r["Resultado"])
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener los movimientos de suscripción.", ex); }
            return lista;
        }

        public List<BE.AltaSuscripcion> ObtenerAltas(DateTime desde, DateTime hastaExcl)
        {
            var lista = new List<BE.AltaSuscripcion>();
            try
            {
                // Alta de un suscriptor: su primera contratación pagada (PN02); si no tiene (clientes
                // cargados antes de PN02), la fecha de alta del cliente siempre que tenga o haya tenido plan.
                DataTable t = acceso.Leer(
                    "SELECT c.IdCliente, COALESCE(pc.IdPlan, c.IdPlan) AS IdPlan, COALESCE(pc.Fecha, c.FechaAlta) AS Fecha " +
                    "FROM Cliente c " +
                    "OUTER APPLY (SELECT TOP 1 ct.IdPlan, ct.FechaComprobante AS Fecha FROM Contratacion ct " +
                    "             WHERE ct.IdCliente = c.IdCliente AND ct.Estado = 1 AND ct.FechaComprobante IS NOT NULL " +
                    "             ORDER BY ct.FechaComprobante) pc " +
                    "WHERE (pc.Fecha IS NOT NULL OR c.IdPlan IS NOT NULL " +
                    "       OR EXISTS (SELECT 1 FROM HistorialRenovacion h WHERE h.IdCliente = c.IdCliente)) " +
                    "  AND COALESCE(pc.Fecha, c.FechaAlta) >= @Desde AND COALESCE(pc.Fecha, c.FechaAlta) < @HastaExcl",
                    Rango(desde, hastaExcl));
                foreach (DataRow r in t.Rows)
                    lista.Add(new BE.AltaSuscripcion
                    {
                        IdCliente = Convert.ToInt32(r["IdCliente"]),
                        IdPlan = Entero(r["IdPlan"]),
                        Fecha = Convert.ToDateTime(r["Fecha"])
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener las altas de suscriptores.", ex); }
            return lista;
        }

        public List<BE.CobroComercial> ObtenerCobros(DateTime desde, DateTime hastaExcl)
        {
            var lista = new List<BE.CobroComercial>();
            try
            {
                // Mismas fuentes que DAL.Promocion.ObtenerImpacto: contrataciones pagadas (PN02) y cobros
                // recurrentes cobrados (N01, Resultado 1). El plan del cobro recurrente es el actual del cliente.
                DataTable t = acceso.Leer(
                    "SELECT ct.FechaComprobante AS Fecha, ISNULL(ct.Importe, 0) AS Importe, ISNULL(ct.DescuentoAplicado, 0) AS Descuento, " +
                    "       ct.IdPromocion, ct.IdPlan " +
                    "FROM Contratacion ct WHERE ct.Estado = 1 AND ct.FechaComprobante >= @Desde AND ct.FechaComprobante < @HastaExcl " +
                    "UNION ALL " +
                    "SELECT hc.FechaResolucion, hc.Importe, ISNULL(hc.DescuentoAplicado, 0), hc.IdPromocion, c.IdPlan " +
                    "FROM HistorialCobro hc LEFT JOIN Cliente c ON c.IdCliente = hc.IdCliente " +
                    "WHERE hc.Resultado = 1 AND hc.FechaResolucion >= @Desde AND hc.FechaResolucion < @HastaExcl",
                    Rango(desde, hastaExcl));
                foreach (DataRow r in t.Rows)
                    lista.Add(new BE.CobroComercial
                    {
                        Fecha = Convert.ToDateTime(r["Fecha"]),
                        Importe = Convert.ToDecimal(r["Importe"]),
                        Descuento = Convert.ToDecimal(r["Descuento"]),
                        IdPromocion = Entero(r["IdPromocion"]),
                        IdPlan = Entero(r["IdPlan"])
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener los cobros del período.", ex); }
            return lista;
        }

        public List<BE.PrendaComercial> ObtenerPrendas(DateTime desde, DateTime hastaExcl)
        {
            var lista = new List<BE.PrendaComercial>();
            var porId = new Dictionary<int, BE.PrendaComercial>();
            try
            {
                DataTable t = acceso.Leer(
                    "SELECT p.IdPrenda, p.Nombre, p.Categoria, p.Talle, p.Estado, p.FechaAlta, p.PrecioReposicion, " +
                    "       (SELECT COUNT(*) FROM PedidoPrenda pp INNER JOIN Pedido pe ON pe.IdPedido = pp.IdPedido " +
                    "        WHERE pp.IdPrenda = p.IdPrenda AND pe.Estado IN " + ESTADOS_VENTA + " " +
                    "          AND pe.FechaPedido >= @Desde AND pe.FechaPedido < @HastaExcl) AS PedidosPeriodo " +
                    "FROM Prenda p",
                    Rango(desde, hastaExcl));
                foreach (DataRow r in t.Rows)
                {
                    var p = new BE.PrendaComercial
                    {
                        IdPrenda = Convert.ToInt32(r["IdPrenda"]),
                        Nombre = Texto(r["Nombre"]),
                        Categoria = Texto(r["Categoria"]),
                        Talle = Texto(r["Talle"]),
                        Estado = (BE.EstadoPrenda)Convert.ToInt32(r["Estado"]),
                        FechaAlta = Convert.ToDateTime(r["FechaAlta"]),
                        PrecioReposicion = Decimal(r["PrecioReposicion"]),
                        PedidosPeriodo = Convert.ToInt32(r["PedidosPeriodo"])
                    };
                    lista.Add(p);
                    porId[p.IdPrenda] = p;
                }
            }
            catch (Exception ex) { throw new Exception("Error al obtener las prendas para el análisis comercial.", ex); }

            // Lista de Espera es una mejora opcional: sin la tabla, la demanda se mide solo por pedidos.
            try
            {
                DataTable le = acceso.Leer(
                    "IF OBJECT_ID('ListaEspera', 'U') IS NOT NULL " +
                    "  SELECT IdPrenda, COUNT(*) AS Cantidad FROM ListaEspera WHERE Estado IN (0, 1) GROUP BY IdPrenda " +
                    "ELSE SELECT CAST(NULL AS INT) AS IdPrenda, 0 AS Cantidad WHERE 1 = 0", null);
                foreach (DataRow r in le.Rows)
                    if (porId.TryGetValue(Convert.ToInt32(r["IdPrenda"]), out var p))
                        p.EnListaEspera = Convert.ToInt32(r["Cantidad"]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[DAL.InteligenciaComercial] Lista de espera no disponible: " + ex.Message);
            }
            return lista;
        }

        public Dictionary<string, string> ObtenerParametros(string prefijo)
        {
            var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable t = acceso.Leer(
                    "SELECT Clave, Valor FROM ParametroSistema WHERE Clave LIKE @Prefijo",
                    new[] { new SqlParameter("@Prefijo", SqlDbType.NVarChar, 100) { Value = Escapar(prefijo) + "%" } });
                foreach (DataRow r in t.Rows)
                {
                    string clave = r["Clave"].ToString();
                    valores[clave.Substring(Math.Min(prefijo.Length, clave.Length))] = Texto(r["Valor"]);
                }
            }
            catch (Exception ex) { throw new Exception("Error al leer los umbrales comerciales.", ex); }
            return valores;
        }

        public void GuardarParametros(string prefijo, Dictionary<string, string> valores)
        {
            if (valores == null || valores.Count == 0) return;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    foreach (var kv in valores)
                        using (var cmd = new SqlCommand(
                            "UPDATE ParametroSistema SET Valor = @Valor, Fecha = GETDATE() WHERE Clave = @Clave; " +
                            "IF @@ROWCOUNT = 0 INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (@Clave, @Valor, GETDATE());",
                            cn, tx))
                        {
                            cmd.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 100) { Value = prefijo + kv.Key });
                            cmd.Parameters.Add(new SqlParameter("@Valor", SqlDbType.NVarChar, 400) { Value = (object)kv.Value ?? DBNull.Value });
                            cmd.ExecuteNonQuery();
                        }
                });
            }
            catch (Exception ex) { throw new Exception("Error al guardar los umbrales comerciales.", ex); }
        }

        // LIKE: el prefijo se toma literal ("_" y "%" no son comodines).
        private static string Escapar(string texto) =>
            (texto ?? string.Empty).Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
