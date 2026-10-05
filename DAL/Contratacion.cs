using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — Contratacion (PN02, Comercialización de la suscripción).
    /// Tablas: Contratacion, ContratacionIntentoPago, MedioPago y DesistimientoContratacion.
    /// </summary>
    public class Contratacion : BaseDAL, Interfaces.IContratacionDAL
    {
        // T07 — Dígito Verificador (formato 2): la contratación tiene el dinero cobrado, quién
        // vendió y quién cobró. Una alteración por SQL (por ejemplo del importe o de IdCaja)
        // queda detectada por la verificación de integridad.
        public const  string   DV_Tabla    = "Contratacion";
        public const  string   DV_Pk       = "IdContratacion";
        public static readonly string[] DV_Columnas =
        {
            "IdCliente", "IdPlan", "IdVendedor", "IdCaja", "Modalidad", "Estado", "FechaAlta",
            "FechaResolucion", "IdMedioPago", "NumeroComprobante", "FechaComprobante", "Importe",
            "DescuentoAplicado", "IdPromocion", "VigenciaDesde", "VigenciaHasta", "PrecioMensual",
            "IdReferenteAcreditado"
        };

        // Recalcula el DVH de la fila y el DVV de la tabla. Best-effort: la escritura de negocio
        // ya quedó confirmada; si esto falla, la verificación de integridad lo detecta.
        private static void ActualizarDV(int idContratacion)
        {
            try { new DigitoVerificador().ActualizarFila(DV_Tabla, DV_Pk, DV_Columnas, idContratacion); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[DAL.Contratacion.ActualizarDV] " + ex.Message); }
        }

        private const string SELECT_BASE =
            "SELECT c.IdContratacion, c.IdCliente, c.IdPlan, c.IdVendedor, c.IdCaja, c.Modalidad, " +
            "c.Estado, c.FechaAlta, c.FechaResolucion, c.IdMedioPago, mp.Nombre AS NombreMedioPago, " +
            "c.NumeroComprobante, c.FechaComprobante, c.Importe, c.DescuentoAplicado, c.IdPromocion, " +
            "c.VigenciaDesde, c.VigenciaHasta, c.PrecioMensual, c.IdReferenteAcreditado, " +
            "rf.Nombre + ' ' + rf.Apellido AS NombreReferenteAcreditado, " +
            "(SELECT COUNT(*) FROM ContratacionIntentoPago i WHERE i.IdContratacion = c.IdContratacion) AS IntentosPago, " +
            "cli.Nombre + ' ' + cli.Apellido AS NombreCliente, pl.Nombre AS NombrePlan, pl.Precio AS MontoPlan, " +
            "ven.Nombre + ' ' + ven.Apellido AS NombreVendedor, caj.Nombre + ' ' + caj.Apellido AS NombreCaja " +
            "FROM Contratacion c " +
            "JOIN Cliente cli ON cli.IdCliente = c.IdCliente " +
            "JOIN PlanSuscripcion pl ON pl.IdPlan = c.IdPlan " +
            "JOIN Empleado ven ON ven.IdEmpleado = c.IdVendedor " +
            "LEFT JOIN Empleado caj ON caj.IdEmpleado = c.IdCaja " +
            "LEFT JOIN MedioPago mp ON mp.IdMedioPago = c.IdMedioPago " +
            "LEFT JOIN Cliente rf ON rf.IdCliente = c.IdReferenteAcreditado ";

        public List<BE.Contratacion> ObtenerPendientesDePago() =>
            Listar(SELECT_BASE + "WHERE c.Estado = 0 ORDER BY c.FechaAlta", "Error al obtener las contrataciones pendientes de pago.");

        public List<BE.Contratacion> ObtenerResueltas() =>
            Listar(SELECT_BASE + "WHERE c.Estado <> 0 ORDER BY c.FechaResolucion DESC", "Error al obtener las contrataciones resueltas.");

        private List<BE.Contratacion> Listar(string sql, string error)
        {
            var lista = new List<BE.Contratacion>();
            try
            {
                foreach (DataRow row in acceso.Leer(sql, null).Rows)
                    lista.Add(Mapear(row));
            }
            catch (Exception ex) { throw new Exception(error, ex); }
            return lista;
        }

        public BE.Contratacion ObtenerPorId(int idContratacion)
        {
            SqlParameter[] p = { new SqlParameter("@IdContratacion", idContratacion) };
            try
            {
                DataTable tabla = acceso.Leer(SELECT_BASE + "WHERE c.IdContratacion = @IdContratacion", p);
                return tabla == null || tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
            }
            catch (Exception ex) { throw new Exception("Error al obtener la contratación.", ex); }
        }

        public int Alta(BE.Contratacion contratacion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdCliente",  contratacion.IdCliente),
                new SqlParameter("@IdPlan",     contratacion.IdPlan),
                new SqlParameter("@IdVendedor", contratacion.IdVendedor),
                new SqlParameter("@Modalidad",  (int)contratacion.Modalidad),
                new SqlParameter("@FechaAlta",  contratacion.FechaAlta),
                new SqlParameter("@PrecioMensual", (object)contratacion.PrecioMensual ?? DBNull.Value)
            };
            try
            {
                DataTable tabla = acceso.Leer(
                    "INSERT INTO Contratacion (IdCliente, IdPlan, IdVendedor, Modalidad, Estado, FechaAlta, PrecioMensual) " +
                    "VALUES (@IdCliente, @IdPlan, @IdVendedor, @Modalidad, 0, @FechaAlta, @PrecioMensual); " +
                    "SELECT SCOPE_IDENTITY() AS IdNuevo", p);
                int id = tabla != null && tabla.Rows.Count > 0 ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"]) : 0;
                if (id > 0) ActualizarDV(id);
                return id;
            }
            catch (Exception ex) { throw new Exception("Error al registrar la contratación.", ex); }
        }

        public bool ConfirmarCobro(int idContratacion, int idCaja, int idMedioPago, string numeroComprobante,
                                   decimal importe, decimal descuento, int? idPromocion)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdContratacion",    idContratacion),
                new SqlParameter("@Estado",            (int)BE.EstadoContratacion.Pagada),
                new SqlParameter("@IdCaja",            idCaja),
                new SqlParameter("@IdMedioPago",       idMedioPago),
                new SqlParameter("@NumeroComprobante", numeroComprobante),
                new SqlParameter("@Ahora",             DateTime.Now),
                new SqlParameter("@Importe",           importe),
                new SqlParameter("@Descuento",         descuento),
                new SqlParameter("@IdPromocion",       (object)idPromocion ?? DBNull.Value)
            };
            try
            {
                // "Claim" atómico: solo una sesión puede pasar de PendientePago (0) a Pagada.
                bool ok = acceso.Escribir(
                    "UPDATE Contratacion SET Estado = @Estado, IdCaja = @IdCaja, IdMedioPago = @IdMedioPago, " +
                    "NumeroComprobante = @NumeroComprobante, FechaComprobante = @Ahora, " +
                    "Importe = @Importe, DescuentoAplicado = @Descuento, IdPromocion = @IdPromocion, " +
                    "FechaResolucion = @Ahora WHERE IdContratacion = @IdContratacion AND Estado = 0", p) > 0;
                if (ok) ActualizarDV(idContratacion);
                return ok;
            }
            catch (Exception ex) { throw new Exception("Error al confirmar el cobro de la contratación.", ex); }
        }

        public void RegistrarVigencia(int idContratacion, DateTime desde, DateTime hasta, int? idReferenteAcreditado = null)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdContratacion", idContratacion),
                new SqlParameter("@Desde", desde.Date),
                new SqlParameter("@Hasta", hasta.Date),
                new SqlParameter("@IdReferente", (object)idReferenteAcreditado ?? DBNull.Value)
            };
            try
            {
                acceso.Escribir(
                    "UPDATE Contratacion SET VigenciaDesde = @Desde, VigenciaHasta = @Hasta, IdReferenteAcreditado = @IdReferente " +
                    "WHERE IdContratacion = @IdContratacion AND Estado = 1", p);
                ActualizarDV(idContratacion);
            }
            catch (Exception ex) { throw new Exception("Error al registrar la vigencia de la contratación.", ex); }
        }

        public void ReabrirPago(int idContratacion)
        {
            SqlParameter[] p = { new SqlParameter("@IdContratacion", idContratacion) };
            try
            {
                acceso.Escribir(
                    "UPDATE Contratacion SET Estado = 0, IdCaja = NULL, IdMedioPago = NULL, NumeroComprobante = NULL, " +
                    "FechaComprobante = NULL, Importe = NULL, DescuentoAplicado = NULL, IdPromocion = NULL, " +
                    "FechaResolucion = NULL, VigenciaDesde = NULL, VigenciaHasta = NULL, IdReferenteAcreditado = NULL " +
                    "WHERE IdContratacion = @IdContratacion AND Estado = 1", p);
                ActualizarDV(idContratacion);
            }
            catch (Exception ex) { throw new Exception("Error al reabrir la contratación tras un cobro fallido.", ex); }
        }

        public BE.ResultadoIntentoPago RegistrarIntentoFallido(int idContratacion, int? idMedioPago, string motivo, int idCaja, int maximo)
        {
            BE.ResultadoIntentoPago resultado = null;
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    // Bloquea la fila: dos sesiones de Caja no pueden numerar el mismo intento.
                    object estado;
                    using (var cmd = new SqlCommand(
                        "SELECT Estado FROM Contratacion WITH (UPDLOCK, ROWLOCK) WHERE IdContratacion = @Id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", idContratacion);
                        estado = cmd.ExecuteScalar();
                    }
                    if (estado == null || Convert.ToInt32(estado) != (int)BE.EstadoContratacion.PendientePago)
                        return;   // otra sesión ya la resolvió

                    int nro;
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) + 1 FROM ContratacionIntentoPago WHERE IdContratacion = @Id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", idContratacion);
                        nro = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    if (nro > maximo) nro = maximo;   // ya estaba en el tope: se cancela sin agregar intentos
                    else
                        using (var cmd = new SqlCommand(
                            "INSERT INTO ContratacionIntentoPago (IdContratacion, NroIntento, Fecha, IdMedioPago, Motivo, IdCaja) " +
                            "VALUES (@Id, @Nro, @Ahora, @IdMedioPago, @Motivo, @IdCaja)", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", idContratacion);
                            cmd.Parameters.AddWithValue("@Nro", nro);
                            cmd.Parameters.AddWithValue("@Ahora", DateTime.Now);
                            cmd.Parameters.AddWithValue("@IdMedioPago", (object)idMedioPago ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Motivo", motivo);
                            cmd.Parameters.AddWithValue("@IdCaja", idCaja);
                            cmd.ExecuteNonQuery();
                        }

                    bool cancelar = nro >= maximo;
                    if (cancelar)
                        using (var cmd = new SqlCommand(
                            "UPDATE Contratacion SET Estado = @Cancelada, FechaResolucion = @Ahora " +
                            "WHERE IdContratacion = @Id AND Estado = 0", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Cancelada", (int)BE.EstadoContratacion.Cancelada);
                            cmd.Parameters.AddWithValue("@Ahora", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Id", idContratacion);
                            cmd.ExecuteNonQuery();
                        }

                    resultado = new BE.ResultadoIntentoPago { NroIntento = nro, Maximo = maximo, Cancelada = cancelar };
                });
            }
            catch (Exception ex) { throw new Exception("Error al registrar el intento de pago.", ex); }
            if (resultado != null && resultado.Cancelada) ActualizarDV(idContratacion);
            return resultado;
        }

        public List<BE.IntentoPago> ObtenerIntentos(int idContratacion)
        {
            var lista = new List<BE.IntentoPago>();
            SqlParameter[] p = { new SqlParameter("@Id", idContratacion) };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT i.IdIntento, i.IdContratacion, i.NroIntento, i.Fecha, i.IdMedioPago, i.Motivo, i.IdCaja, " +
                    "       mp.Nombre AS NombreMedioPago, e.Nombre + ' ' + e.Apellido AS NombreCaja " +
                    "FROM ContratacionIntentoPago i " +
                    "LEFT JOIN MedioPago mp ON mp.IdMedioPago = i.IdMedioPago " +
                    "LEFT JOIN Empleado e ON e.IdEmpleado = i.IdCaja " +
                    "WHERE i.IdContratacion = @Id ORDER BY i.NroIntento", p);
                foreach (DataRow r in tabla.Rows)
                    lista.Add(new BE.IntentoPago
                    {
                        IdIntento       = Convert.ToInt32(r["IdIntento"]),
                        IdContratacion  = Convert.ToInt32(r["IdContratacion"]),
                        NroIntento      = Convert.ToInt32(r["NroIntento"]),
                        Fecha           = Convert.ToDateTime(r["Fecha"]),
                        IdMedioPago     = r["IdMedioPago"] != DBNull.Value ? (int?)Convert.ToInt32(r["IdMedioPago"]) : null,
                        Motivo          = r["Motivo"].ToString(),
                        IdCaja          = r["IdCaja"] != DBNull.Value ? Convert.ToInt32(r["IdCaja"]) : 0,
                        NombreMedioPago = r["NombreMedioPago"] != DBNull.Value ? r["NombreMedioPago"].ToString() : null,
                        NombreCaja      = r["NombreCaja"] != DBNull.Value ? r["NombreCaja"].ToString() : null
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener los intentos de pago.", ex); }
            return lista;
        }

        public List<BE.MedioPago> ObtenerMediosPago()
        {
            var lista = new List<BE.MedioPago>();
            try
            {
                foreach (DataRow r in acceso.Leer("SELECT IdMedioPago, Nombre, ClaveTraduccion FROM MedioPago ORDER BY IdMedioPago", null).Rows)
                    lista.Add(new BE.MedioPago
                    {
                        IdMedioPago     = Convert.ToInt32(r["IdMedioPago"]),
                        Nombre          = r["Nombre"].ToString(),
                        ClaveTraduccion = r["ClaveTraduccion"].ToString()
                    });
            }
            catch (Exception ex) { throw new Exception("Error al obtener los medios de pago.", ex); }
            return lista;
        }

        public int AltaDesistimiento(BE.DesistimientoContratacion d)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@IdCliente",  d.IdCliente),
                new SqlParameter("@IdPlan",     (object)d.IdPlan ?? DBNull.Value),
                new SqlParameter("@Modalidad",  d.Modalidad.HasValue ? (object)(int)d.Modalidad.Value : DBNull.Value),
                new SqlParameter("@Motivo",     d.Motivo),
                new SqlParameter("@Fecha",      d.Fecha),
                new SqlParameter("@IdVendedor", d.IdVendedor)
            };
            try
            {
                DataTable tabla = acceso.Leer(
                    "INSERT INTO DesistimientoContratacion (IdCliente, IdPlan, Modalidad, Motivo, Fecha, IdVendedor) " +
                    "VALUES (@IdCliente, @IdPlan, @Modalidad, @Motivo, @Fecha, @IdVendedor); " +
                    "SELECT SCOPE_IDENTITY() AS IdNuevo", p);
                return tabla != null && tabla.Rows.Count > 0 ? Convert.ToInt32(tabla.Rows[0]["IdNuevo"]) : 0;
            }
            catch (Exception ex) { throw new Exception("Error al registrar el desistimiento.", ex); }
        }

        public BE.DesistimientoContratacion ObtenerDesistimiento(int idDesistimiento)
        {
            SqlParameter[] p = { new SqlParameter("@Id", idDesistimiento) };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT d.IdDesistimiento, d.IdCliente, d.IdPlan, d.Modalidad, d.Motivo, d.Fecha, d.IdVendedor, " +
                    "       cli.Nombre + ' ' + cli.Apellido AS NombreCliente, pl.Nombre AS NombrePlan, " +
                    "       ven.Nombre + ' ' + ven.Apellido AS NombreVendedor " +
                    "FROM DesistimientoContratacion d " +
                    "JOIN Cliente cli ON cli.IdCliente = d.IdCliente " +
                    "JOIN Empleado ven ON ven.IdEmpleado = d.IdVendedor " +
                    "LEFT JOIN PlanSuscripcion pl ON pl.IdPlan = d.IdPlan " +
                    "WHERE d.IdDesistimiento = @Id", p);
                if (tabla == null || tabla.Rows.Count == 0) return null;
                var r = tabla.Rows[0];
                return new BE.DesistimientoContratacion
                {
                    IdDesistimiento = Convert.ToInt32(r["IdDesistimiento"]),
                    IdCliente       = Convert.ToInt32(r["IdCliente"]),
                    IdPlan          = r["IdPlan"] != DBNull.Value ? (int?)Convert.ToInt32(r["IdPlan"]) : null,
                    Modalidad       = r["Modalidad"] != DBNull.Value ? (BE.Builders.ModalidadCobro?)Convert.ToInt32(r["Modalidad"]) : null,
                    Motivo          = r["Motivo"].ToString(),
                    Fecha           = Convert.ToDateTime(r["Fecha"]),
                    IdVendedor      = Convert.ToInt32(r["IdVendedor"]),
                    NombreCliente   = r["NombreCliente"].ToString(),
                    NombrePlan      = r["NombrePlan"] != DBNull.Value ? r["NombrePlan"].ToString() : null,
                    NombreVendedor  = r["NombreVendedor"].ToString()
                };
            }
            catch (Exception ex) { throw new Exception("Error al obtener el desistimiento.", ex); }
        }

        private static BE.Contratacion Mapear(DataRow row)
        {
            DateTime? Fecha(string col) => row[col] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row[col]) : null;
            string Texto(string col) => row[col] != DBNull.Value ? row[col].ToString() : null;
            return new BE.Contratacion
            {
                IdContratacion    = Convert.ToInt32(row["IdContratacion"]),
                IdCliente         = Convert.ToInt32(row["IdCliente"]),
                IdPlan            = Convert.ToInt32(row["IdPlan"]),
                IdVendedor        = Convert.ToInt32(row["IdVendedor"]),
                IdCaja            = row["IdCaja"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdCaja"]) : null,
                Modalidad         = (BE.Builders.ModalidadCobro)Convert.ToInt32(row["Modalidad"]),
                Estado            = (BE.EstadoContratacion)Convert.ToInt32(row["Estado"]),
                IntentosPago      = Convert.ToInt32(row["IntentosPago"]),
                FechaAlta         = Convert.ToDateTime(row["FechaAlta"]),
                FechaResolucion   = Fecha("FechaResolucion"),
                IdMedioPago       = row["IdMedioPago"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdMedioPago"]) : null,
                NombreMedioPago   = Texto("NombreMedioPago"),
                NumeroComprobante = Texto("NumeroComprobante"),
                FechaComprobante  = Fecha("FechaComprobante"),
                Importe           = row["Importe"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["Importe"]) : null,
                DescuentoAplicado = row["DescuentoAplicado"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["DescuentoAplicado"]) : null,
                IdPromocion       = row["IdPromocion"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdPromocion"]) : null,
                VigenciaDesde     = Fecha("VigenciaDesde"),
                VigenciaHasta     = Fecha("VigenciaHasta"),
                PrecioMensual     = row["PrecioMensual"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["PrecioMensual"]) : null,
                IdReferenteAcreditado = row["IdReferenteAcreditado"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdReferenteAcreditado"]) : null,
                NombreReferenteAcreditado = Texto("NombreReferenteAcreditado"),
                NombreCliente     = row["NombreCliente"].ToString(),
                NombrePlan        = row["NombrePlan"].ToString(),
                MontoPlan         = Convert.ToDecimal(row["MontoPlan"]),
                NombreVendedor    = Texto("NombreVendedor"),
                NombreCaja        = Texto("NombreCaja")
            };
        }
    }
}
