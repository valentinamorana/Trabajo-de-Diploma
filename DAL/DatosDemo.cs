using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Restablece los DATOS DE DEMO ejecutando el procedimiento dbo.RestablecerDatosDemo, que crea el
    /// script de instalación (BD/00_Instalacion_Completa.sql, sección 21z3b). Es el mismo código que
    /// carga la demo en una instalación nueva: borra los datos de negocio (clientes, prendas, pedidos,
    /// contrataciones, cobros, promociones y bitácora de negocio) y vuelve a cargar los escenarios
    /// con fechas relativas a hoy. Usuarios, permisos, planes, traducciones y bitácora del sistema no
    /// se tocan. Las tablas protegidas que cambia quedan con la inicialización de sus dígitos
    /// verificadores pendiente (marca 'DVInicializacionPendiente'): la BLL la completa enseguida.
    /// </summary>
    public class DatosDemo : BaseDAL
    {
        public const string Procedimiento = "dbo.RestablecerDatosDemo";

        // Texto que el procedimiento exige para ejecutarse (evita un EXEC accidental).
        public const string Confirmacion = "RESTABLECER";

        /// <summary>true si la base tiene el procedimiento (la crea el script de instalación).</summary>
        public bool ProcedimientoDisponible()
        {
            DataTable t = acceso.Leer("SELECT OBJECT_ID(@Nombre, 'P') AS Id",
                new[] { new SqlParameter("@Nombre", Procedimiento) });
            return t.Rows.Count > 0 && t.Rows[0]["Id"] != System.DBNull.Value;
        }

        /// <summary>Ejecuta el procedimiento (todo o nada: si falla, la base queda como estaba).</summary>
        public void Restablecer()
        {
            var entrada = ConfigurationManager.ConnectionStrings["WardrobeFlowDB"];
            using (var conexion = new SqlConnection(entrada.ConnectionString))
            using (var cmd = new SqlCommand(Procedimiento, conexion))
            {
                cmd.CommandType    = CommandType.StoredProcedure;
                cmd.CommandTimeout = 300;   // borra y recarga varios cientos de filas
                cmd.Parameters.AddWithValue("@Confirmacion", Confirmacion);
                conexion.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
