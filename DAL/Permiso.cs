/*
 * SCRIPT SQL — ejecutar en WardrobeFlowDB para migrar al Composite relacional:
 *
 *   -- 1. Agregar discriminador EsFamilia
 *   ALTER TABLE Permiso ADD EsFamilia BIT NOT NULL DEFAULT 0;
 *
 *   -- 2. Crear tabla de relaciones padre-hijo
 *   CREATE TABLE PermisoRelacion (
 *       IdPadre INT NOT NULL REFERENCES Permiso(IdPermiso),
 *       IdHijo  INT NOT NULL REFERENCES Permiso(IdPermiso),
 *       PRIMARY KEY (IdPadre, IdHijo)
 *   );
 *
 *   -- 3. Crear nodos Familia por cada grupo TipoComponente existente
 *   DECLARE @mapa TABLE (Grupo NVARCHAR(100), IdFamilia INT);
 *
 *   INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia)
 *   OUTPUT INSERTED.Nombre, INSERTED.IdPermiso INTO @mapa (Grupo, IdFamilia)
 *   SELECT DISTINCT TipoComponente, TipoComponente, TipoComponente, 1, 1
 *   FROM   Permiso
 *   WHERE  TipoComponente IS NOT NULL AND LTRIM(RTRIM(TipoComponente)) <> ''
 *   AND    EsFamilia = 0;
 *
 *   -- 4. Vincular cada Patente con su Familia
 *   INSERT INTO PermisoRelacion (IdPadre, IdHijo)
 *   SELECT m.IdFamilia, p.IdPermiso
 *   FROM   Permiso p
 *   INNER JOIN @mapa m ON p.TipoComponente = m.Grupo
 *   WHERE  p.EsFamilia = 0;
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// DAL del Composite de permisos (T04).
    ///
    /// FUENTE DE VERDAD EN RUNTIME: la tabla [PermisoRelacion] (aristas padre→hijo). Todo el CRUD
    /// de autorización lee/escribe ahí.
    ///
    /// [RolPermiso] es LEGACY y de SEED/BOOTSTRAP únicamente: los scripts SQL la usan para sembrar
    /// las asignaciones planas rol→patente y, desde ellas, generar los nodos-rol y las aristas de
    /// [PermisoRelacion]. En runtime NO se escribe nunca y solo se LEE en ramas de fallback, para
    /// bases todavía sin migrar al Composite (sin columna EsRol / sin PermisoRelacion poblada).
    /// En una base migrada esas ramas no se ejecutan.
    /// </summary>
    public class Permiso : BaseDAL, Interfaces.IPermisoDAL
    {
        // ── T07 — Dígito Verificador de PermisoRelacion (formato 2) ─────────────────
        // Las aristas padre→hijo definen qué puede hacer cada rol: agregar por SQL una patente a
        // un rol (escalada de privilegios) queda detectado. La tabla tiene clave compuesta, por
        // eso no usa el DV genérico por Id: el DVH pondera IdPadre e IdHijo y el DVV se calcula
        // en el orden (IdPadre, IdHijo).
        public const string DV_TablaRelacion = "PermisoRelacion";
        private static readonly string OrdenRelacion = DigitoVerificador.OrdenPor("IdPadre", "IdHijo");

        private static BE.FilaDV MapearRelacion(DataRow r)
        {
            int padre = Convert.ToInt32(r["IdPadre"]), hijo = Convert.ToInt32(r["IdHijo"]);
            return new BE.FilaDV
            {
                Id            = padre,
                Campos        = new[] { DigitoVerificador.Formatear(padre), DigitoVerificador.Formatear(hijo) },
                DVHAlmacenado = r["DVH"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["DVH"]),
                Descripcion   = "PermisoRelacion " + padre + "→" + hijo
            };
        }

        public List<BE.FilaDV> ObtenerFilasDVRelacion()
        {
            var lista = new List<BE.FilaDV>();
            DataTable dt = acceso.Leer("SELECT IdPadre, IdHijo, DVH FROM PermisoRelacion ORDER BY " + OrdenRelacion, null);
            foreach (DataRow r in dt.Rows) lista.Add(MapearRelacion(r));
            return lista;
        }

        // Recalcula el DVH de la arista (si existe) y el DVV desde los DVH almacenados.
        // El bloqueo del DV ya debe estar tomado en la transacción.
        private static void ActualizarDVRelacionEnTx(SqlConnection cn, SqlTransaction tx, int idPadre, int idHijo)
        {
            using (var cmd = new SqlCommand(
                "UPDATE PermisoRelacion SET DVH = @dvh WHERE IdPadre = @p AND IdHijo = @h", cn, tx))
            {
                cmd.Parameters.AddWithValue("@dvh", Seguridad.CalculadorDV.Crear().CalcularDVH(
                    DigitoVerificador.Formatear(idPadre), DigitoVerificador.Formatear(idHijo)));
                cmd.Parameters.AddWithValue("@p", idPadre);
                cmd.Parameters.AddWithValue("@h", idHijo);
                cmd.ExecuteNonQuery();
            }
            DigitoVerificador.GuardarDVVDesdeAlmacenadosEnTx(cn, tx, DV_TablaRelacion, OrdenRelacion);
        }

        // Recalcula TODAS las aristas (solo recálculo administrativo / inicialización).
        public void RecalcularDVRelaciones()
        {
            new DigitoVerificador().EjecutarConBloqueo(DV_TablaRelacion, (cn, tx) =>
            {
                var svc = Seguridad.CalculadorDV.Crear();
                var dt = DigitoVerificador.LeerEnTx(cn, tx, "SELECT IdPadre, IdHijo, DVH FROM PermisoRelacion ORDER BY " + OrdenRelacion);
                var dvhs = new List<int>();
                foreach (DataRow r in dt.Rows)
                {
                    var fila = MapearRelacion(r);
                    int dvh = svc.CalcularDVH(fila.Campos);
                    using (var cmd = new SqlCommand(
                        "UPDATE PermisoRelacion SET DVH = @dvh WHERE IdPadre = @p AND IdHijo = @h", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@dvh", dvh);
                        cmd.Parameters.AddWithValue("@p", Convert.ToInt32(r["IdPadre"]));
                        cmd.Parameters.AddWithValue("@h", Convert.ToInt32(r["IdHijo"]));
                        cmd.ExecuteNonQuery();
                    }
                    dvhs.Add(dvh);
                }
                DigitoVerificador.GuardarDVVEnTx(cn, tx, DV_TablaRelacion, svc.CalcularDVV(dvhs));
            });
        }

        // Construye el árbol Composite completo desde BD.
        // Lee Permiso (EsFamilia discrimina tipo) y PermisoRelacion (padre→hijo).
        // Retorna los nodos raíz (Familias sin padre) listas para que BLL las envuelva.
        public List<BE.Componente> ObtenerArbol()
        {
            DataTable dt = acceso.Leer(
                "SELECT IdPermiso, Nombre, NombreMenu, EsFamilia, " +
                "ISNULL(EsRol,0) AS EsRol " +
                "FROM Permiso WHERE Estado = 1 ORDER BY EsFamilia DESC, Nombre",
                null);

            var nodos = new Dictionary<int, BE.Componente>();

            foreach (DataRow row in dt.Rows)
            {
                int  id        = Convert.ToInt32(row["IdPermiso"]);
                bool esFamilia = row["EsFamilia"] != DBNull.Value && Convert.ToBoolean(row["EsFamilia"]);
                bool esRol     = row["EsRol"] != DBNull.Value && Convert.ToBoolean(row["EsRol"]);

                if (esRol)
                {
                    nodos[id] = new BE.Rol { Id = id, Nombre = row["Nombre"].ToString(), EsFijo = true };
                }
                else if (esFamilia)
                {
                    nodos[id] = new BE.Familia { Id = id, Nombre = row["Nombre"].ToString() };
                }
                else
                {
                    nodos[id] = new BE.Patente
                    {
                        Id         = id,
                        Nombre     = row["Nombre"].ToString(),
                        NombreMenu = row["NombreMenu"] != DBNull.Value ? row["NombreMenu"].ToString() : string.Empty
                    };
                }
            }

            DataTable rels = acceso.Leer("SELECT IdPadre, IdHijo FROM PermisoRelacion", null);
            var conPadre = new HashSet<int>();

            foreach (DataRow row in rels.Rows)
            {
                int idPadre = Convert.ToInt32(row["IdPadre"]);
                int idHijo  = Convert.ToInt32(row["IdHijo"]);

                if (nodos.TryGetValue(idPadre, out BE.Componente nodoPadre) &&
                    nodos.TryGetValue(idHijo,  out BE.Componente nodoHijo)  &&
                    nodoPadre is BE.Familia familia)
                {
                    familia.AgregarHijo(nodoHijo);
                    conPadre.Add(idHijo);
                }
            }

            var raices = new List<BE.Componente>();
            foreach (var kvp in nodos)
                if (!conPadre.Contains(kvp.Key))
                    raices.Add(kvp.Value);

            return raices;
        }

        // Roles disponibles en el sistema.
        // T04: los roles son nodos del Composite (EsRol=1), de modo que un rol recién
        // creado aparece aunque todavía no tenga permisos asignados.
        public List<string> ObtenerRoles()
        {
            var lista = new List<string>();
            try
            {
                DataTable tabla = acceso.Leer(
                    "SELECT Nombre FROM Permiso WHERE ISNULL(EsRol,0) = 1 AND Estado = 1 " +
                    "ORDER BY Nombre", null);
                if (tabla == null) return lista;
                foreach (DataRow row in tabla.Rows)
                    lista.Add(row["Nombre"].ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener roles.", ex);
            }
            return lista;
        }

        // ── T04 — CRUD del Composite (Patentes / Familias / Roles) ──────────────

        // Devuelve el Id del nodo-rol cuyo Nombre coincide, o 0 si no existe.
        public int ObtenerIdRol(string rolNombre)
        {
            if (string.IsNullOrWhiteSpace(rolNombre)) return 0;
            DataTable t = acceso.Leer(
                "SELECT TOP 1 IdPermiso FROM Permiso WHERE Nombre = @n AND ISNULL(EsRol,0) = 1",
                new[] { new SqlParameter("@n", rolNombre) });
            if (t == null || t.Rows.Count == 0) return 0;
            return Convert.ToInt32(t.Rows[0]["IdPermiso"]);
        }

        // Alta de un componente (Patente, Familia o Rol). Devuelve el IdPermiso generado.
        public int AltaComponente(string nombre, string nombreMenu, bool esFamilia, bool esRol, string tipoComponente)
        {
            try
            {
                DataTable t = acceso.Leer(
                    "INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol) " +
                    "VALUES (@nom, @menu, @tipo, 1, @fam, @rol); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id",
                    new[]
                    {
                        new SqlParameter("@nom",  nombre),
                        // NombreMenu puede ser NOT NULL en esquemas legacy: para familias/roles
                        // (sin menú propio) se usa el nombre como valor por defecto.
                        new SqlParameter("@menu", (object)(string.IsNullOrEmpty(nombreMenu) ? nombre : nombreMenu)),
                        // TipoComponente puede ser NOT NULL en esquemas legacy → valor por defecto.
                        new SqlParameter("@tipo", (object)(string.IsNullOrEmpty(tipoComponente) ? "General" : tipoComponente)),
                        new SqlParameter("@fam",  esFamilia || esRol),
                        new SqlParameter("@rol",  esRol)
                    });
                return (t != null && t.Rows.Count > 0) ? Convert.ToInt32(t.Rows[0]["Id"]) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear el componente '{nombre}'.", ex);
            }
        }

        // Modifica nombre y nombre de menú de un componente.
        public void ModificarComponente(int idPermiso, string nombre, string nombreMenu)
        {
            try
            {
                acceso.Escribir(
                    "UPDATE Permiso SET Nombre = @nom, NombreMenu = @menu WHERE IdPermiso = @id",
                    new[]
                    {
                        new SqlParameter("@nom",  nombre),
                        new SqlParameter("@menu", (object)nombreMenu ?? DBNull.Value),
                        new SqlParameter("@id",   idPermiso)
                    });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al modificar el componente {idPermiso}.", ex);
            }
        }

        // Baja lógica de un componente (Estado=0) y limpieza de sus relaciones,
        // tanto como padre (sus hijos) como hijo (en otros nodos).
        public void BajaComponente(int idPermiso)
        {
            try
            {
                // Envuelto en una transacción explícita (antes viajaba como un solo Escribir() con
                // dos sentencias, sin EjecutarTransaccion, a diferencia del resto de las operaciones
                // multi-tabla del proyecto): si el DELETE de relaciones tiene éxito pero el UPDATE de
                // Estado falla (o viceversa), el componente podía quedar con sus relaciones borradas
                // pero todavía activo, o dado de baja con relaciones colgantes.
                acceso.EjecutarTransaccion((conexion, tx) =>
                {
                    DigitoVerificador.Bloquear(conexion, tx, DV_TablaRelacion);
                    using (var cmd = new SqlCommand(
                        "DELETE FROM PermisoRelacion WHERE IdPadre = @id OR IdHijo = @id; " +
                        "UPDATE Permiso SET Estado = 0 WHERE IdPermiso = @id",
                        conexion, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", idPermiso);
                        cmd.ExecuteNonQuery();
                    }
                    // T07 — cambió el conjunto de aristas: DVV desde los DVH almacenados.
                    DigitoVerificador.GuardarDVVDesdeAlmacenadosEnTx(conexion, tx, DV_TablaRelacion, OrdenRelacion);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al dar de baja el componente {idPermiso}.", ex);
            }
        }

        // Ids de los hijos DIRECTOS de un nodo (un solo nivel).
        public List<int> ObtenerIdsHijos(int idPadre)
        {
            var lista = new List<int>();
            DataTable t = acceso.Leer(
                "SELECT IdHijo FROM PermisoRelacion WHERE IdPadre = @p",
                new[] { new SqlParameter("@p", idPadre) });
            if (t == null) return lista;
            foreach (DataRow row in t.Rows)
                lista.Add(Convert.ToInt32(row["IdHijo"]));
            return lista;
        }

        // Crea una arista padre→hijo en el árbol Composite (idempotente).
        public void AgregarRelacion(int idPadre, int idHijo)
        {
            try
            {
                // La arista y su DV se escriben en la MISMA transacción (con el bloqueo del DV
                // tomado primero): no queda una arista nueva sin DVH válido.
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    DigitoVerificador.Bloquear(cn, tx, DV_TablaRelacion);
                    using (var cmd = new SqlCommand(
                        "IF NOT EXISTS (SELECT 1 FROM PermisoRelacion WHERE IdPadre = @p AND IdHijo = @h) " +
                        "INSERT INTO PermisoRelacion (IdPadre, IdHijo, DVH) VALUES (@p, @h, 0)", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@p", idPadre);
                        cmd.Parameters.AddWithValue("@h", idHijo);
                        cmd.ExecuteNonQuery();
                    }
                    ActualizarDVRelacionEnTx(cn, tx, idPadre, idHijo);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al relacionar componentes {idPadre}→{idHijo}.", ex);
            }
        }

        // Elimina una arista padre→hijo del árbol Composite.
        public void QuitarRelacion(int idPadre, int idHijo)
        {
            try
            {
                acceso.EjecutarTransaccion((cn, tx) =>
                {
                    DigitoVerificador.Bloquear(cn, tx, DV_TablaRelacion);
                    using (var cmd = new SqlCommand(
                        "DELETE FROM PermisoRelacion WHERE IdPadre = @p AND IdHijo = @h", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@p", idPadre);
                        cmd.Parameters.AddWithValue("@h", idHijo);
                        cmd.ExecuteNonQuery();
                    }
                    DigitoVerificador.GuardarDVVDesdeAlmacenadosEnTx(cn, tx, DV_TablaRelacion, OrdenRelacion);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al quitar relación {idPadre}→{idHijo}.", ex);
            }
        }

        // Cantidad de usuarios que tienen asignado el rol.
        // Cubre el mismo criterio que el login (Rol ?? Perfil).
        private const string FiltroUsuarioRol =
            "(Rol = @rol OR (Rol IS NULL AND Perfil = @rol))";

        public int ContarUsuariosPorRol(string rol)
        {
            DataTable t = acceso.Leer(
                "SELECT COUNT(*) AS Total FROM Usuario WHERE " + FiltroUsuarioRol,
                new[] { new SqlParameter("@rol", rol) });
            if (t == null || t.Rows.Count == 0) return 0;
            return Convert.ToInt32(t.Rows[0]["Total"]);
        }

        // Nombres de usuario que tienen asignado el rol (para advertir antes de eliminar).
        public List<string> ObtenerUsuariosPorRol(string rol)
        {
            var lista = new List<string>();
            DataTable t = acceso.Leer(
                "SELECT Username FROM Usuario WHERE " + FiltroUsuarioRol + " ORDER BY Username",
                new[] { new SqlParameter("@rol", rol) });
            if (t == null) return lista;
            foreach (DataRow row in t.Rows)
                lista.Add(row["Username"].ToString());
            return lista;
        }

    }
}
