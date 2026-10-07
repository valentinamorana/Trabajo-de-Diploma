using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Capa de Acceso a Datos — Usuario.
    /// Opera sobre la tabla [Usuario] de WardrobeFlowDB.
    ///
    /// Columnas de la tabla:
    ///   IdUsuario         int PK     → alias "Id" en consultas
    ///   Username          varchar    → nombre de acceso único
    ///   Clave             varchar    → hash PBKDF2-SHA256 (alias "Contraseña")
    ///   Rol               varchar    → rol técnico
    ///   Perfil            varchar    → nombre visible
    ///   Estado            bit        → 1=activo, 0=bloqueado
    ///   IntentosFallidos  int        → contador persistente de intentos fallidos
    /// </summary>
    /// <summary>
    /// Hereda de BaseDAL<BE.Usuario>
    ///   - acceso  → Singleton de BD (heredado, no se redeclara)
    ///   - ObtenerTodos() y ObtenerPorId() → implementados con SQL de Usuario
    /// </summary>
    public class Usuario : BaseDAL<BE.Usuario>, Interfaces.IUsuarioDAL
    {
        // Nota: el script de instalación garantiza todas las columnas de Usuario (bases nuevas y
        // actualizadas), así que ya no hay consultas de "BD sin migrar". En particular, el viejo
        // fallback de ObtenerPorUsername NO filtraba Activo=1 y habría dejado entrar a usuarios
        // archivados si alguna vez se ejecutaba.

        // Estado vigente de un usuario (para revalidar la sesión en BLL.PermisosAccion).
        // Devuelve null si el usuario ya no existe.
        public (bool Activo, string Perfil, string Rol)? ObtenerVigencia(int idUsuario)
        {
            DataTable t = acceso.Leer(
                "SELECT Activo, Perfil, Rol FROM Usuario WHERE IdUsuario = @id",
                new SqlParameter[] { new SqlParameter("@id", idUsuario) });
            if (t == null || t.Rows.Count == 0) return null;
            var r = t.Rows[0];
            return (Convert.ToBoolean(r["Activo"]),
                    r["Perfil"] != DBNull.Value ? r["Perfil"].ToString() : null,
                    r["Rol"]    != DBNull.Value ? r["Rol"].ToString()    : null);
        }

        // Inserta un nuevo usuario con contraseña hasheada y rol asignado.
        // Estado=1 (activo) e IntentosFallidos=0 por defecto al crear.
        // Después del INSERT calcula y persiste el DVH de la nueva fila.
        public void Alta(string username, string clave, string perfil)
        {
            try
            {
                SqlParameter[] parametros = new SqlParameter[]
                {
                    new SqlParameter("@username", username),
                    new SqlParameter("@clave",    clave),
                    new SqlParameter("@perfil",   perfil),
                    new SqlParameter("@rol",      perfil)
                };
                acceso.Escribir(
                    "INSERT INTO Usuario (Username, Clave, Rol, Estado, Perfil, IntentosFallidos) " +
                    "VALUES (@username, @clave, @rol, 1, @perfil, 0)",
                    parametros);

                BE.Usuario nuevo = ObtenerPorUsername(username);
                if (nuevo != null)
                {
                    // Clave generada por el sistema → el usuario debe cambiarla en su primer login.
                    SetRequiereCambioClave(nuevo.Id, true);
                    RecalcularDVH(nuevo.Id);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al dar de alta al usuario '{username}'.", ex);
            }
        }

        // Helper compartido por ObtenerPorUsername y ObtenerPorId.
        // idiomaDefault se usa cuando la columna IdIdioma no existe en BD (migración v4.0 pendiente).
        private BE.Usuario LeerUsuarioPorQuery(string sql, SqlParameter[] parametros, string idiomaDefault = null)
        {
            DataTable tabla = acceso.Leer(sql, parametros);
            if (tabla == null || tabla.Rows.Count == 0) return null;

            DataRow row = tabla.Rows[0];
            bool tieneIdIdioma = tabla.Columns.Contains("IdIdioma");
            return new BE.Usuario
            {
                Id               = Convert.ToInt32(row["Id"]),
                Username         = row["Username"].ToString(),
                Contraseña       = row["Contraseña"].ToString(),
                Rol              = row["Rol"] != DBNull.Value ? row["Rol"].ToString() : null,
                Perfil           = row["Perfil"] != DBNull.Value ? row["Perfil"].ToString() : null,
                Bloqueado        = row["Estado"] != DBNull.Value && Convert.ToInt32(row["Estado"]) == 0,
                IntentosFallidos = row["IntentosFallidos"] != DBNull.Value
                                       ? Convert.ToInt32(row["IntentosFallidos"]) : 0,
                IdIdioma         = tieneIdIdioma && row["IdIdioma"] != DBNull.Value
                                       ? row["IdIdioma"].ToString()
                                       : (idiomaDefault ?? "ES"),
                // Bloqueo progresivo (columnas opcionales; 0/null si la BD no está migrada).
                CantidadBloqueos = tabla.Columns.Contains("CantidadBloqueos") && row["CantidadBloqueos"] != DBNull.Value
                                       ? Convert.ToInt32(row["CantidadBloqueos"]) : 0,
                FechaBloqueo     = tabla.Columns.Contains("FechaBloqueo") && row["FechaBloqueo"] != DBNull.Value
                                       ? (DateTime?)Convert.ToDateTime(row["FechaBloqueo"]) : null,
                // Cambio de clave obligatorio (columna opcional; false si la BD no está migrada).
                RequiereCambioClave = tabla.Columns.Contains("RequiereCambioClave") && row["RequiereCambioClave"] != DBNull.Value
                                       && Convert.ToInt32(row["RequiereCambioClave"]) == 1,
                // Datos administrativos NO sensibles (columnas opcionales; null si BD sin migrar).
                Nombre          = tabla.Columns.Contains("Nombre")   && row["Nombre"]   != DBNull.Value ? row["Nombre"].ToString()   : null,
                Apellido        = tabla.Columns.Contains("Apellido") && row["Apellido"] != DBNull.Value ? row["Apellido"].ToString() : null,
                Email           = tabla.Columns.Contains("Email")    && row["Email"]    != DBNull.Value ? row["Email"].ToString()    : null,
                FechaNacimiento = tabla.Columns.Contains("FechaNacimiento") && row["FechaNacimiento"] != DBNull.Value
                                       ? (DateTime?)Convert.ToDateTime(row["FechaNacimiento"]) : null
            };
        }

        // Busca un usuario por Username para el proceso de Login.
        // Incluye Estado e IntentosFallidos para el control de bloqueo.
        public BE.Usuario ObtenerPorUsername(string username)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Username", username)
            };

            try
            {
                // RF-10 — los usuarios archivados (Activo=0) NO pueden iniciar sesión.
                return LeerUsuarioPorQuery(
                    "SELECT IdUsuario AS Id, Username, Clave AS Contraseña, Rol, Perfil, " +
                    "       Estado, IntentosFallidos, ISNULL(IdIdioma, 'ES') AS IdIdioma, " +
                    "       CantidadBloqueos, FechaBloqueo, RequiereCambioClave, " +
                    "       Nombre, Apellido, Email, FechaNacimiento " +
                    "FROM Usuario WHERE Username = @Username AND Activo = 1",
                    parametros);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el usuario desde la base de datos.", ex);
            }
        }

        // Bloquea la cuenta de un usuario (Estado=0).
        // Se llama tras superar el máximo de intentos fallidos.
        public void Bloquear(int idUsuario)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Estado = 0 WHERE IdUsuario = @idUsuario",
                    new SqlParameter[] { new SqlParameter("@idUsuario", idUsuario) });
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al bloquear el usuario ID {idUsuario}.", ex);
            }
        }

        // Bloqueo PROGRESIVO: marca el bloqueo con timestamp e incrementa la escala (1/5/15/60 min).
        // Reemplaza a Bloquear() en el flujo de login.
        public void BloquearConTiempo(int idUsuario)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Estado = 0, FechaBloqueo = GETDATE(), " +
                    "       CantidadBloqueos = ISNULL(CantidadBloqueos, 0) + 1 " +
                    "WHERE IdUsuario = @idUsuario",
                    new SqlParameter[] { new SqlParameter("@idUsuario", idUsuario) });
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al bloquear (progresivo) el usuario ID {idUsuario}.", ex);
            }
        }

        // Auto-desbloqueo al EXPIRAR el bloqueo temporal: reactiva, limpia intentos y la fecha de
        // bloqueo (pero conserva CantidadBloqueos: la próxima vez el bloqueo dura más).
        public void AutoDesbloquear(int idUsuario)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Estado = 1, IntentosFallidos = 0, FechaBloqueo = NULL " +
                    "WHERE IdUsuario = @idUsuario",
                    new SqlParameter[] { new SqlParameter("@idUsuario", idUsuario) });
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al auto-desbloquear el usuario ID {idUsuario}.", ex);
            }
        }

        // Desbloquea la cuenta de un usuario (Estado=1) y resetea el contador de intentos.
        // Reset COMPLETO (también la escala de bloqueos y la fecha): es una acción explícita del
        // Administrador (o de una clave de emergencia), no un auto-desbloqueo por tiempo.
        public void Desbloquear(int idUsuario)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Estado = 1, IntentosFallidos = 0, " +
                    "       CantidadBloqueos = 0, FechaBloqueo = NULL WHERE IdUsuario = @idUsuario",
                    new SqlParameter[] { new SqlParameter("@idUsuario", idUsuario) });
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al desbloquear el usuario ID {idUsuario}.", ex);
            }
        }

        // Ejecuta una acción dentro de una única transacción (commit/rollback automático).
        // Mismo patrón que DAL.Cliente.EjecutarTransaccion — le da a BLL una forma de componer,
        // desde afuera de esta clase, varias escrituras en una sola operación atómica.
        public void EjecutarTransaccion(Action<SqlConnection, SqlTransaction> accion)
            => acceso.EjecutarTransaccion(accion);

        // Elimina una fila de Usuario dentro de una transacción ya abierta por el caller (ver
        // EjecutarTransaccion). Usado por BLL.RecuperacionIntegridad.RepararDesdeEspejo para
        // borrar inserciones externas detectadas al comparar contra el espejo de integridad.
        public void EliminarEnTx(SqlConnection conexion, SqlTransaction tx, int idUsuario)
        {
            using (var cmd = new SqlCommand("DELETE FROM Usuario WHERE IdUsuario = @id", conexion, tx))
            {
                cmd.Parameters.AddWithValue("@id", idUsuario);
                cmd.ExecuteNonQuery();
            }
        }

        // Revierte una fila de Usuario a los valores legítimos guardados en el espejo de
        // integridad, dentro de una transacción ya abierta por el caller. Usado por
        // BLL.RecuperacionIntegridad.RepararDesdeEspejo para deshacer modificaciones externas.
        public void RevertirDesdeEspejoEnTx(SqlConnection conexion, SqlTransaction tx, BE.FilaUsuarioDV valoresEspejo)
        {
            // Mismos parámetros que el espejo (incluye Activo/RequiereCambioClave/CantidadBloqueos/
            // FechaBloqueo, que forman parte del DVH desde el formato 2). @dvh no se usa acá.
            using (var cmd = new SqlCommand(
                "UPDATE Usuario SET Username=@u, Clave=@c, Rol=@r, Perfil=@p, Estado=@e, " +
                "IntentosFallidos=@i, Activo=@a, RequiereCambioClave=@rc, CantidadBloqueos=@cb, " +
                "FechaBloqueo=@fb WHERE IdUsuario=@id", conexion, tx))
            {
                cmd.Parameters.AddRange(EspejoUsuario.ParametrosFila(valoresEspejo));
                cmd.ExecuteNonQuery();
            }
        }

        // Incrementa en 1 el contador de intentos fallidos para el username dado y devuelve el
        // valor RESULTANTE leído en la misma sentencia (OUTPUT inserted): así dos intentos
        // simultáneos no deciden el bloqueo con una lectura vieja. Devuelve null si el usuario
        // no existe (o está archivado).
        // El contador persiste en BD: sobrevive reinicios de la aplicación.
        public int? IncrementarIntentosFallidos(string username)
        {
            DataTable t = acceso.Leer(
                "UPDATE Usuario SET IntentosFallidos = ISNULL(IntentosFallidos, 0) + 1 " +
                "OUTPUT inserted.IdUsuario, inserted.IntentosFallidos " +
                "WHERE Username = @username AND Activo = 1",
                new SqlParameter[] { new SqlParameter("@username", username ?? string.Empty) });
            if (t == null || t.Rows.Count == 0) return null;
            // T07 — IntentosFallidos forma parte del DVH: recalcular para no dejar la
            // fila "corrupta" y bloquear la app en el próximo arranque.
            RecalcularDVH(Convert.ToInt32(t.Rows[0]["IdUsuario"]));
            return Convert.ToInt32(t.Rows[0]["IntentosFallidos"]);
        }

        // Resetea a 0 el contador de intentos fallidos para el username dado.
        // Se llama tras un login exitoso.
        public void ResetearIntentosFallidos(string username)
        {
            try
            {
                DataTable t = acceso.Leer(
                    "UPDATE Usuario SET IntentosFallidos = 0 OUTPUT inserted.IdUsuario " +
                    "WHERE Username = @username AND IntentosFallidos <> 0",
                    new SqlParameter[] { new SqlParameter("@username", username ?? string.Empty) });
                // T07 — IntentosFallidos forma parte del DVH: recalcular tras el reset.
                if (t != null)
                    foreach (DataRow r in t.Rows) RecalcularDVH(Convert.ToInt32(r["IdUsuario"]));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[DAL.Usuario.ResetearIntentosFallidos] {ex.Message}");
            }
        }

        // Aplica el snapshot de una versión histórica a la fila activa del usuario.
        // SOLO restaura datos administrativos NO sensibles (username/nombre/apellido/fecha nac./email);
        // la contraseña y el estado de bloqueo NUNCA se revierten (no hay rollback de credenciales).
        // El Username forma parte del DVH → recalcular tras el UPDATE.
        public void RestaurarVersion(BE.VersionUsuario v)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Username = @Username, Nombre = @Nombre, Apellido = @Apellido, " +
                    "       FechaNacimiento = @FechaNac, Email = @Email WHERE IdUsuario = @Id",
                    new SqlParameter[]
                    {
                        new SqlParameter("@Username", (object)v.UsernameSnapshot ?? DBNull.Value),
                        new SqlParameter("@Nombre",   (object)v.NombreSnapshot   ?? DBNull.Value),
                        new SqlParameter("@Apellido", (object)v.ApellidoSnapshot ?? DBNull.Value),
                        new SqlParameter("@FechaNac", (object)v.FechaNacSnapshot ?? DBNull.Value),
                        new SqlParameter("@Email",    (object)v.EmailSnapshot    ?? DBNull.Value),
                        new SqlParameter("@Id",       v.IdUsuario)
                    });
                RecalcularDVH(v.IdUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al restaurar la versión del usuario ID {v.IdUsuario}.", ex);
            }
        }

        // Actualiza la contraseña de un usuario existente (ya hasheada por la BLL).
        public void ResetearClave(int idUsuario, string claveHasheada)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Clave = @clave WHERE IdUsuario = @idUsuario",
                    new SqlParameter[]
                    {
                        new SqlParameter("@clave",     claveHasheada),
                        new SqlParameter("@idUsuario", idUsuario)
                    });
                // Clave reseteada por un admin → el usuario debe cambiarla en su próximo login.
                SetRequiereCambioClave(idUsuario, true);
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al resetear la clave del usuario ID {idUsuario}.", ex);
            }
        }

        // Obtiene un usuario por su clave primaria (IdUsuario).
        // Incluye Estado e IntentosFallidos para el control de bloqueo.
        // Cambio de clave por el PROPIO usuario (clave ya hasheada por la BLL).
        // Persiste la nueva clave, baja el flag RequiereCambioClave y recalcula el DVH.
        public void CambiarClave(int idUsuario, string claveHasheada)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Clave = @clave WHERE IdUsuario = @idUsuario",
                    new SqlParameter[]
                    {
                        new SqlParameter("@clave",     claveHasheada),
                        new SqlParameter("@idUsuario", idUsuario)
                    });
                // El usuario ya eligió su propia clave → deja de ser temporal.
                SetRequiereCambioClave(idUsuario, false);
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cambiar la clave del usuario ID {idUsuario}.", ex);
            }
        }

        // ABM — Modifica los datos administrativos NO sensibles de un usuario (nombre, apellido,
        // username, fecha de nacimiento, email). El Username forma parte del DVH, por lo que se
        // recalcula tras el UPDATE para no dejar la fila marcada como corrupta.
        public void Modificar(int idUsuario, string nombre, string apellido, string username,
                              DateTime? fechaNacimiento, string email)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Username = @username, Nombre = @nombre, Apellido = @apellido, " +
                    "       FechaNacimiento = @fechaNac, Email = @email WHERE IdUsuario = @id",
                    new SqlParameter[]
                    {
                        new SqlParameter("@username", (object)username ?? DBNull.Value),
                        new SqlParameter("@nombre",   (object)nombre   ?? DBNull.Value),
                        new SqlParameter("@apellido", (object)apellido ?? DBNull.Value),
                        new SqlParameter("@fechaNac", (object)fechaNacimiento ?? DBNull.Value),
                        new SqlParameter("@email",    (object)email    ?? DBNull.Value),
                        new SqlParameter("@id",       idUsuario)
                    });
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al modificar el usuario ID {idUsuario}.", ex);
            }
        }

        // ABM — Cambia el rol/perfil de un usuario. Rol forma parte del DVH → recalcular.
        public void CambiarRol(int idUsuario, string rol)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Rol = @rol, Perfil = @rol WHERE IdUsuario = @id",
                    new SqlParameter[]
                    {
                        new SqlParameter("@rol", (object)rol ?? DBNull.Value),
                        new SqlParameter("@id",  idUsuario)
                    });
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cambiar el rol del usuario ID {idUsuario}.", ex);
            }
        }

        // Marca/desmarca el flag de cambio obligatorio para un usuario. Desde el formato 2 de DV
        // el flag SÍ entra al DVH: todos los llamadores recalculan el DVH después.
        private void SetRequiereCambioClave(int idUsuario, bool requiere)
        {
            acceso.Escribir(
                "UPDATE Usuario SET RequiereCambioClave = @r WHERE IdUsuario = @id",
                new SqlParameter[]
                {
                    new SqlParameter("@r",  SqlDbType.Bit) { Value = requiere },
                    new SqlParameter("@id", idUsuario)
                });
        }

        public override BE.Usuario ObtenerPorId(int idUsuario)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdUsuario", idUsuario)
            };
            try
            {
                return LeerUsuarioPorQuery(
                    "SELECT IdUsuario AS Id, Username, Clave AS Contraseña, Rol, Perfil, " +
                    "       Estado, IntentosFallidos, ISNULL(IdIdioma, 'ES') AS IdIdioma, " +
                    "       Nombre, Apellido, Email, FechaNacimiento " +
                    "FROM Usuario WHERE IdUsuario = @IdUsuario",
                    parametros);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el usuario por ID.", ex);
            }
        }

        // Persiste la preferencia de idioma del usuario (por ejemplo 'ES', 'EN', 'RU', 'PT').
        // Llamado desde BLL cuando el usuario cambia idioma en Menu.
        public void GuardarIdioma(int idUsuario, string idIdioma)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET IdIdioma = @idIdioma WHERE IdUsuario = @idUsuario",
                    new SqlParameter[]
                    {
                        new SqlParameter("@idUsuario", idUsuario),
                        new SqlParameter("@idIdioma",  idIdioma ?? "ES")
                    });
            }
            catch (Exception ex)
            {
                // No es crítico: si falla, el usuario solo pierde la preferencia de idioma.
                System.Diagnostics.Trace.TraceWarning(
                    $"[DAL.Usuario.GuardarIdioma] Error al guardar idioma para usuario ID {idUsuario}: {ex.Message}");
            }
        }

        // T07 — Recalcula SOLO el DVH de la fila del usuario y el DVV de la tabla a partir de los
        // DVH ALMACENADOS, con el bloqueo del DV tomado (DigitoVerificador.EjecutarConBloqueo).
        // Antes se recalculaba el DVV con todos los DVH, lo que no lavaba filas ajenas, pero el
        // recálculo no estaba serializado: dos escrituras simultáneas podían pisarse el DVV.
        // Se llama después de cualquier operación de escritura sobre un usuario.
        private void RecalcularDVH(int idUsuario)
        {
            try
            {
                BE.FilaUsuarioDV fila = null;
                new DigitoVerificador().EjecutarConBloqueo("Usuario", (cn, tx) =>
                {
                    var dt = DigitoVerificador.LeerEnTx(cn, tx,
                        DigitoVerificador.SelectFilasUsuario + " WHERE IdUsuario = @id",
                        new SqlParameter("@id", idUsuario));
                    if (dt.Rows.Count > 0)
                    {
                        fila = DigitoVerificador.MapearFilaUsuario(dt.Rows[0]);
                        int dvh = Seguridad.CalculadorDV.Crear().CalcularDVH(fila.CamposParaDVH());
                        using (var cmd = new SqlCommand("UPDATE Usuario SET DVH = @dvh WHERE IdUsuario = @id", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@dvh", dvh);
                            cmd.Parameters.AddWithValue("@id", idUsuario);
                            cmd.ExecuteNonQuery();
                        }
                        fila.DVHAlmacenado = dvh;
                    }
                    DigitoVerificador.GuardarDVVDesdeAlmacenadosEnTx(cn, tx, "Usuario", "IdUsuario");
                });

                // T07 — Espejo de integridad: registrar el nuevo estado legítimo de esta fila
                // (o quitarla si el usuario se eliminó físicamente).
                var espejo = new EspejoUsuario();
                if (fila != null) espejo.Upsert(fila);
                else espejo.Eliminar(idUsuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[DAL.Usuario.RecalcularDVH] {ex.Message}");
            }
        }

        // Lista los usuarios ACTIVOS del sistema (sin contraseña). RF-10: los archivados
        // (Activo=0) quedan fuera para no contaminar la vista ni las métricas.
        public override List<BE.Usuario> ObtenerTodos()
        {
            return LeerListaUsuarios(soloActivos: true);
        }

        // RF-10 — Lista los usuarios ARCHIVADOS (Activo=0), con su FechaBaja, para gestión/purga.
        public List<BE.Usuario> ObtenerArchivados()
        {
            return LeerListaUsuarios(soloActivos: false);
        }

        // Lector compartido por ObtenerTodos / ObtenerArchivados. Filtra por Activo cuando la
        // columna existe; si la BD aún no está migrada, devuelve todos como activos (fallback).
        private List<BE.Usuario> LeerListaUsuarios(bool soloActivos)
        {
            var lista = new List<BE.Usuario>();
            string filtro = soloActivos ? "Activo = 1" : "Activo = 0";
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT IdUsuario AS Id, Username, Perfil, Estado, IntentosFallidos, " +
                    "       Activo, FechaBaja, " +
                    "       Nombre, Apellido, Email, FechaNacimiento " +
                    "FROM Usuario WHERE " + filtro + " ORDER BY Username",
                    null);

                bool tieneActivo    = tabla.Columns.Contains("Activo");
                bool tieneFechaBaja = tabla.Columns.Contains("FechaBaja");
                bool tieneNombre    = tabla.Columns.Contains("Nombre");
                bool tieneApellido  = tabla.Columns.Contains("Apellido");
                bool tieneEmail     = tabla.Columns.Contains("Email");
                bool tieneFechaNac  = tabla.Columns.Contains("FechaNacimiento");
                foreach (DataRow row in tabla.Rows)
                {
                    lista.Add(new BE.Usuario
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Username = row["Username"].ToString(),
                        Perfil = row["Perfil"] != DBNull.Value ? row["Perfil"].ToString() : null,
                        Bloqueado = row["Estado"] != DBNull.Value && Convert.ToInt32(row["Estado"]) == 0,
                        IntentosFallidos = row["IntentosFallidos"] != DBNull.Value
                                              ? Convert.ToInt32(row["IntentosFallidos"]) : 0,
                        Activo = !tieneActivo || row["Activo"] == DBNull.Value || Convert.ToInt32(row["Activo"]) == 1,
                        FechaBaja = tieneFechaBaja && row["FechaBaja"] != DBNull.Value
                                        ? (DateTime?)Convert.ToDateTime(row["FechaBaja"]) : null,
                        Nombre   = tieneNombre   && row["Nombre"]   != DBNull.Value ? row["Nombre"].ToString()   : null,
                        Apellido = tieneApellido && row["Apellido"] != DBNull.Value ? row["Apellido"].ToString() : null,
                        Email    = tieneEmail    && row["Email"]    != DBNull.Value ? row["Email"].ToString()    : null,
                        FechaNacimiento = tieneFechaNac && row["FechaNacimiento"] != DBNull.Value
                                        ? (DateTime?)Convert.ToDateTime(row["FechaNacimiento"]) : null
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la lista de usuarios.", ex);
            }
            return lista;
        }

        // RF-10 — Baja lógica (archivar): el usuario deja de poder loguear y sale de la lista,
        // pero se conserva su historial para trazabilidad. Recalcula DVH/DVV de la fila.
        public void BajaLogica(int idUsuario)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Usuario SET Activo = 0, FechaBaja = GETDATE(), Estado = 0 " +
                    "WHERE IdUsuario = @id",
                    new SqlParameter[] { new SqlParameter("@id", idUsuario) });
                RecalcularDVH(idUsuario);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al archivar (baja lógica) el usuario ID {idUsuario}.", ex);
            }
        }

        // RF-10 — Eliminación FÍSICA con limpieza de FKs, en una transacción atómica.
        // Desvincula la bitácora (conserva los registros con usuario NULL), borra el historial
        // propio del usuario y elimina la fila. Luego recalcula DVH/DVV de toda la tabla.
        public void EliminarFisico(int idUsuario)
        {
            try
            {
                var empleadosDesvinculados = new List<int>();
                acceso.EjecutarTransaccion((conn, tx) =>
                {
                    using (var cmd = new SqlCommand("SELECT IdEmpleado FROM Empleado WHERE IdUsuario = @id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", idUsuario);
                        using (var rd = cmd.ExecuteReader())
                            while (rd.Read()) empleadosDesvinculados.Add(rd.GetInt32(0));
                    }
                    void Exec(string sql)
                    {
                        using (var cmd = new SqlCommand(sql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", idUsuario);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    // Desvincular referencias nullables (preserva los registros de auditoría).
                    Exec("UPDATE Bitacora        SET usuario   = NULL WHERE usuario   = @id");
                    Exec("UPDATE BitacoraNegocio SET IdUsuario = NULL WHERE IdUsuario = @id");
                    Exec("UPDATE PedidoHistorial SET IdUsuario = NULL WHERE IdUsuario = @id");
                    Exec("UPDATE Empleado        SET IdUsuario = NULL WHERE IdUsuario = @id");
                    // El historial de versiones del usuario se va con el usuario (FK NOT NULL).
                    Exec("DELETE FROM HistorialUsuario WHERE IdUsuario = @id");
                    Exec("DELETE FROM Usuario WHERE IdUsuario = @id");
                });

                // La fila ya no existe: solo cambia el DVV (desde los DVH almacenados) y se quita
                // del espejo. Los Empleados desvinculados cambian su DVH (IdUsuario entra al cálculo).
                RecalcularDVH(idUsuario);
                foreach (int idEmp in empleadosDesvinculados)
                    new Empleado().ActualizarDV(idEmp);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar definitivamente el usuario ID {idUsuario}.", ex);
            }
        }

        // RF-10 — Registros de negocio de PN03 firmados por el usuario: son la traza de quién creó,
        // sugirió, dictaminó o resolvió cada promoción. Un usuario con alguno no se purga.
        public bool TieneRegistrosDeNegocio(int idUsuario)
        {
            DataTable t = acceso.Leer(
                "SELECT CASE WHEN " +
                "  EXISTS (SELECT 1 FROM DictamenContable WHERE IdUsuario = @id) OR " +
                "  EXISTS (SELECT 1 FROM SolicitudBajaPromocion WHERE IdUsuarioSolicita = @id OR IdUsuarioResuelve = @id) OR " +
                "  EXISTS (SELECT 1 FROM Promocion WHERE IdUsuarioAlta = @id) OR " +
                "  EXISTS (SELECT 1 FROM PromocionHistorial WHERE IdUsuario = @id) OR " +
                "  EXISTS (SELECT 1 FROM SugerenciaPromocion WHERE IdUsuarioAlta = @id) " +
                "THEN 1 ELSE 0 END AS Tiene",
                new[] { new SqlParameter("@id", idUsuario) });
            return t != null && t.Rows.Count > 0 && Convert.ToInt32(t.Rows[0]["Tiene"]) == 1;
        }

        // RF-10 — Cuenta los Administradores ACTIVOS (para impedir borrar el último).
        public int ContarAdministradoresActivos()
        {
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT COUNT(*) AS Total FROM Usuario " +
                    "WHERE Perfil = @perfil AND Activo = 1",
                    new SqlParameter[] { new SqlParameter("@perfil", BE.Roles.Administrador) });
                return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0]["Total"]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al contar administradores activos.", ex);
            }
        }

        // RF-10 — Archivados elegibles para purga física: Activo=0 y FechaBaja anterior al
        // umbral de retención (p. ej. 1 año). Mantiene la base limpia sin perder datos antes de tiempo.
        public List<BE.Usuario> ObtenerArchivadosParaPurga(int diasRetencion)
        {
            var lista = new List<BE.Usuario>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT IdUsuario AS Id, Username, Perfil, FechaBaja " +
                    "FROM Usuario " +
                    "WHERE Activo = 0 AND FechaBaja IS NOT NULL " +
                    "  AND FechaBaja <= DATEADD(day, -@dias, GETDATE()) " +
                    "ORDER BY FechaBaja",
                    new SqlParameter[] { new SqlParameter("@dias", diasRetencion) });

                foreach (DataRow row in tabla.Rows)
                {
                    lista.Add(new BE.Usuario
                    {
                        Id        = Convert.ToInt32(row["Id"]),
                        Username  = row["Username"].ToString(),
                        Perfil    = row["Perfil"] != DBNull.Value ? row["Perfil"].ToString() : null,
                        Activo    = false,
                        FechaBaja = row["FechaBaja"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["FechaBaja"]) : null
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener usuarios archivados para purga.", ex);
            }
            return lista;
        }
    }
}
