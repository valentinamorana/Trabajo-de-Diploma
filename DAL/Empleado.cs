using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — Empleado.
    /// Opera sobre la tabla [Empleado] de WardrobeFlowDB.
    /// Incluye JOIN opcional con [Usuario] para mostrar el username asociado.
    /// </summary>
    public class Empleado : BaseDAL<BE.Empleado>, Interfaces.IEmpleadoDAL
    {
        // T07 — Definición del Dígito Verificador de esta tabla (fuente única).
        public const  string   DV_Tabla    = "Empleado";
        public const  string   DV_Pk       = "IdEmpleado";
        // Formato 2: también IdUsuario — el vínculo empleado↔usuario decide quién figura como
        // vendedor o cajero de cada operación (separación de funciones).
        public static readonly string[] DV_Columnas = { "Nombre", "Apellido", "DNI", "Email", "Puesto", "Legajo", "IdUsuario" };

        // Recalcula el DVH de la fila y el DVV de la tabla (desde los DVH almacenados).
        public void ActualizarDV(int idEmpleado)
        {
            try { new DigitoVerificador().ActualizarFila(DV_Tabla, DV_Pk, DV_Columnas, idEmpleado); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[DAL.Empleado.ActualizarDV] " + ex.Message); }
        }

        // Devuelve todos los empleados con su username (si tienen usuario).
        public override List<BE.Empleado> ObtenerTodos()
        {
            var lista = new List<BE.Empleado>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT e.IdEmpleado, e.Nombre, e.Apellido, e.DNI, e.Email, " +
                    "       e.FechaIngreso, e.Puesto, e.Legajo, e.IdUsuario, " +
                    "       u.Username " +
                    "FROM Empleado e " +
                    "LEFT JOIN Usuario u ON u.IdUsuario = e.IdUsuario " +
                    "ORDER BY e.Apellido, e.Nombre",
                    null);

                foreach (DataRow row in tabla.Rows)
                    lista.Add(Mapear(row));
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la lista de empleados.", ex);
            }
            return lista;
        }

        // Obtiene un empleado por ID.
        public override BE.Empleado ObtenerPorId(int idEmpleado)
        {
            SqlParameter[] p = { new SqlParameter("@IdEmpleado", idEmpleado) };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT e.IdEmpleado, e.Nombre, e.Apellido, e.DNI, e.Email, " +
                    "       e.FechaIngreso, e.Puesto, e.Legajo, e.IdUsuario, " +
                    "       u.Username " +
                    "FROM Empleado e " +
                    "LEFT JOIN Usuario u ON u.IdUsuario = e.IdUsuario " +
                    "WHERE e.IdEmpleado = @IdEmpleado",
                    p);

                if (tabla == null || tabla.Rows.Count == 0) return null;
                return Mapear(tabla.Rows[0]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el empleado.", ex);
            }
        }

        // Obtiene el empleado vinculado a un usuario del sistema.
        public BE.Empleado ObtenerPorUsuario(int idUsuario)
        {
            SqlParameter[] p = { new SqlParameter("@IdUsuario", idUsuario) };
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT e.IdEmpleado, e.Nombre, e.Apellido, e.DNI, e.Email, " +
                    "       e.FechaIngreso, e.Puesto, e.Legajo, e.IdUsuario, " +
                    "       u.Username " +
                    "FROM Empleado e " +
                    "LEFT JOIN Usuario u ON u.IdUsuario = e.IdUsuario " +
                    "WHERE e.IdUsuario = @IdUsuario",
                    p);

                if (tabla == null || tabla.Rows.Count == 0) return null;
                return Mapear(tabla.Rows[0]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el empleado por usuario.", ex);
            }
        }

        private BE.Empleado Mapear(DataRow row)
        {
            return new BE.Empleado
            {
                IdEmpleado = Convert.ToInt32(row["IdEmpleado"]),
                Nombre = row["Nombre"].ToString(),
                Apellido = row["Apellido"].ToString(),
                DNI = row["DNI"].ToString(),
                Email = row["Email"] != DBNull.Value ? row["Email"].ToString() : null,
                FechaIngreso = Convert.ToDateTime(row["FechaIngreso"]),
                Puesto = row["Puesto"] != DBNull.Value ? row["Puesto"].ToString() : null,
                Legajo = row["Legajo"] != DBNull.Value ? row["Legajo"].ToString() : null,
                IdUsuario = row["IdUsuario"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdUsuario"]) : null,
                Username = row["Username"] != DBNull.Value ? row["Username"].ToString() : null
            };
        }
    }
}
