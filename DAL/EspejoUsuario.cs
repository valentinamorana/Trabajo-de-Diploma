using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — T07 Tabla ESPEJO de integridad (Usuario_Seguridad).
    ///
    /// Mantiene una copia sombra de los campos que entran al DVH de cada usuario, más su DVH.
    /// La app la sincroniza con cada escritura LEGÍTIMA (junto al recálculo del DVH), de modo que
    /// el espejo siempre refleja el "último estado válido conocido". Eso habilita dos cosas que el
    /// DVH solo no permite:
    ///   1. DIAGNÓSTICO a nivel de campo: comparar la fila actual contra el espejo para saber QUÉ
    ///      campo fue alterado (no solo "esta fila no coincide").
    ///   2. REPARACIÓN a valores legítimos: restaurar el dato bueno desde el espejo, sin necesitar
    ///      un backup completo de la base.
    ///   3. AUTENTICACIÓN de emergencia: si la tabla Usuario no es íntegra, el login se valida
    ///      contra el espejo (ver BLL.Usuario.AutenticarContraEspejo).
    ///
    /// Inspirado en la tabla 'Usuario_Seguridad' del proyecto de referencia (Agus).
    /// El script de instalación crea la tabla y sus columnas (sección 22), así que no hay modo
    /// "sin migrar": los errores se propagan.
    /// </summary>
    public class EspejoUsuario : BaseDAL
    {
        private const string Columnas =
            "IdUsuario, Username, Clave, Rol, Perfil, Estado, IntentosFallidos, " +
            "Activo, RequiereCambioClave, CantidadBloqueos, FechaBloqueo, DVH";

        // Indica si la tabla espejo existe.
        public bool Existe()
        {
            DataTable dt = acceso.Leer(
                "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Usuario_Seguridad'", null);
            return dt != null && dt.Rows.Count > 0;
        }

        // Lee todas las filas del espejo, ordenadas por IdUsuario. Reusa BE.FilaUsuarioDV para que
        // el DVH se calcule con CamposParaDVH() exactamente igual que sobre la tabla real.
        public List<BE.FilaUsuarioDV> ObtenerFilas()
        {
            var lista = new List<BE.FilaUsuarioDV>();
            DataTable dt = acceso.Leer("SELECT " + Columnas + " FROM Usuario_Seguridad ORDER BY IdUsuario", null);
            if (dt == null) return lista;
            foreach (DataRow r in dt.Rows) lista.Add(DigitoVerificador.MapearFilaUsuario(r));
            return lista;
        }

        // Fila del espejo de un usuario por nombre (null si no está).
        public BE.FilaUsuarioDV ObtenerPorUsername(string username)
        {
            DataTable dt = acceso.Leer(
                "SELECT " + Columnas + " FROM Usuario_Seguridad WHERE Username = @u",
                new SqlParameter[] { new SqlParameter("@u", username ?? string.Empty) });
            return dt == null || dt.Rows.Count == 0 ? null : DigitoVerificador.MapearFilaUsuario(dt.Rows[0]);
        }

        // Inserta o actualiza (upsert) la fila del espejo para un usuario. La llama la app tras cada
        // escritura legítima sobre el usuario (cuando recalcula su DVH).
        public void Upsert(BE.FilaUsuarioDV fila)
        {
            if (fila == null) return;
            try
            {
                acceso.Escribir(
                    "IF EXISTS (SELECT 1 FROM Usuario_Seguridad WHERE IdUsuario = @id) " +
                    "    UPDATE Usuario_Seguridad SET Username=@u, Clave=@c, Rol=@r, Perfil=@p, " +
                    "        Estado=@e, IntentosFallidos=@i, Activo=@a, RequiereCambioClave=@rc, " +
                    "        CantidadBloqueos=@cb, FechaBloqueo=@fb, DVH=@dvh, FechaActualizacion=GETDATE() " +
                    "    WHERE IdUsuario=@id " +
                    "ELSE " +
                    "    INSERT INTO Usuario_Seguridad (" + Columnas + ", FechaActualizacion) " +
                    "    VALUES (@id, @u, @c, @r, @p, @e, @i, @a, @rc, @cb, @fb, @dvh, GETDATE())",
                    ParametrosFila(fila));
            }
            catch (Exception ex)
            {
                // El espejo es un respaldo: no aborta la escritura de negocio ya confirmada, pero se registra.
                System.Diagnostics.Trace.TraceError($"[DAL.EspejoUsuario.Upsert] {ex.Message}");
            }
        }

        // Elimina una fila del espejo (cuando se da de baja FÍSICA un usuario legítimamente).
        public void Eliminar(int idUsuario)
        {
            acceso.Escribir("DELETE FROM Usuario_Seguridad WHERE IdUsuario = @id",
                new SqlParameter[] { new SqlParameter("@id", idUsuario) });
        }

        // Reconstruye TODO el espejo desde una lista de filas legítimas (wipe + reinsert), en una
        // transacción atómica. Se usa tras "Recalcular Todo"/"Asumir pérdida" y en la inicialización
        // tras instalar, donde el espejo debe quedar idéntico al estado aceptado como válido.
        public void Reconstruir(List<BE.FilaUsuarioDV> filas)
        {
            if (filas == null) return;
            acceso.EjecutarTransaccion((conn, tx) =>
            {
                using (var del = new SqlCommand("DELETE FROM Usuario_Seguridad", conn, tx))
                    del.ExecuteNonQuery();

                foreach (var f in filas)
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO Usuario_Seguridad (" + Columnas + ", FechaActualizacion) " +
                        "VALUES (@id, @u, @c, @r, @p, @e, @i, @a, @rc, @cb, @fb, @dvh, GETDATE())", conn, tx))
                    {
                        cmd.Parameters.AddRange(ParametrosFila(f));
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        // Convierte la FechaBloqueo normalizada (texto invariante) de vuelta a DATETIME.
        public static object FechaDesdeTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return DBNull.Value;
            return DateTime.ParseExact(texto, DigitoVerificador.FormatoFecha, CultureInfo.InvariantCulture);
        }

        // Arma los SqlParameter de una fila del espejo a partir del DTO de DVH.
        internal static SqlParameter[] ParametrosFila(BE.FilaUsuarioDV f)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@id",  f.Id),
                new SqlParameter("@u",   (object)f.Username ?? string.Empty),
                new SqlParameter("@c",   (object)f.Clave    ?? string.Empty),
                new SqlParameter("@r",   (object)f.Rol      ?? DBNull.Value),
                new SqlParameter("@p",   (object)f.Perfil   ?? DBNull.Value),
                new SqlParameter("@e",   SqlDbType.Bit) { Value = ParseEntero(f.Estado, 1) != 0 },
                new SqlParameter("@i",   SqlDbType.Int) { Value = ParseEntero(f.IntentosFallidos, 0) },
                new SqlParameter("@a",   SqlDbType.Bit) { Value = ParseEntero(f.Activo, 1) != 0 },
                new SqlParameter("@rc",  SqlDbType.Bit) { Value = ParseEntero(f.RequiereCambioClave, 0) != 0 },
                new SqlParameter("@cb",  SqlDbType.Int) { Value = ParseEntero(f.CantidadBloqueos, 0) },
                new SqlParameter("@fb",  SqlDbType.DateTime) { Value = FechaDesdeTexto(f.FechaBloqueo) },
                new SqlParameter("@dvh", SqlDbType.Int) { Value = f.DVHAlmacenado ?? 0 }
            };
        }

        internal static int ParseEntero(string valor, int porDefecto)
        {
            return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : porDefecto;
        }
    }
}
