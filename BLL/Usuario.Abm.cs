using Seguridad;
using Servicios;
using System;
using System.Collections.Generic;

namespace BLL
{
    // Partial de BLL.Usuario — ABM administrativo: alta, modificación de datos NO sensibles,
    // cambio de rol y búsqueda. Ver BLL.Usuario (Usuario.cs) para el resto de grupos.
    public partial class Usuario
    {
        // Crea un nuevo usuario con rol y contraseña generada automáticamente.
        // Sobrecarga simple (sin datos de perfil) — usada por callers heredados.
        public string Alta(string modulo, string username, string perfil)
        {
            return Alta(modulo, username, perfil, null, null, null, null);
        }

        // Crea un nuevo usuario con rol, datos administrativos (no sensibles) y contraseña
        // generada automáticamente. La contraseña NO la ingresa el administrador — se genera
        // aquí y se exporta a un .txt en CredencialesGeneradas/. Devuelve la ruta del archivo.
        // Graba una versión base (Memento) para que el primer cambio tenga "valor anterior".
        public string Alta(string modulo, string username, string perfil,
                           string nombre, string apellido, DateTime? fechaNacimiento, string email)
        {
            ValidarEsAdministrador();

            // T07 — Verificar integridad de la base ANTES de modificar usuarios.
            Configuracion.AsegurarIntegridadUsuarios();

            if (string.IsNullOrWhiteSpace(username))
                throw new BE.AppException("err.bll.usuario.username_requerido",
                    "El nombre de usuario es obligatorio.");

            if (username.Trim().Length < 3)
                throw new BE.AppException("err.bll.usuario.username_corto",
                    "El nombre de usuario debe tener al menos 3 caracteres.");

            if (string.IsNullOrWhiteSpace(perfil))
                throw new BE.AppException("err.bll.usuario.perfil_requerido",
                    "El perfil/rol es obligatorio.");

            // Validaciones puras de datos administrativos (email, fecha, username único).
            ValidarDatosAdministrativos(username, email, fechaNacimiento, ObtenerUsernamesExcepto(0));

            perfil = NormalizarPerfil(perfil);

            string contrasena    = GeneradorCredenciales.GenerarContrasena();
            string claveHasheada = Encriptador.Hash(contrasena);
            usuarioDAL.Alta(username.Trim(), claveHasheada, perfil);

            // Persistir los datos administrativos del alta (si se ingresaron) y grabar la versión base.
            var nuevo = usuarioDAL.ObtenerPorUsername(username.Trim());
            if (nuevo != null)
            {
                bool hayPerfil = !string.IsNullOrWhiteSpace(nombre) || !string.IsNullOrWhiteSpace(apellido)
                                 || !string.IsNullOrWhiteSpace(email) || fechaNacimiento.HasValue;
                if (hayPerfil)
                    usuarioDAL.Modificar(nuevo.Id, Limpiar(nombre), Limpiar(apellido), username.Trim(),
                                         fechaNacimiento, Limpiar(email));

                try
                {
                    new VersionUsuario().GrabarVersion(nuevo.Id,
                        SessionManager.GetInstance().Usuario.Username,
                        "Alta de usuario (estado administrativo inicial).");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("[BLL.Usuario.Alta] snapshot base: " + ex.Message);
                }
            }

            string rutaArchivo = GeneradorCredenciales.ExportarCredenciales(username.Trim(), contrasena);

            bitacora.Registrar(modulo,
                "Alta Usuario: '" + username.Trim() + "' [" + perfil + "]",
                BE.Criticidad.Media);

            return rutaArchivo;
        }

        // ── ABM — Modificación de datos administrativos NO sensibles ─────────────
        // Edita nombre, apellido, username, fecha de nacimiento y email. NUNCA toca la
        // contraseña. Graba una versión (Memento) con el NUEVO estado, de modo que el
        // Historial de Cambios pueda mostrar campo/valor anterior/valor nuevo (diff entre
        // versiones consecutivas) y se registre en bitácora para auditoría.
        public void Modificar(string modulo, int idUsuario, string nombre, string apellido,
                              string username, DateTime? fechaNacimiento, string email)
        {
            ValidarEsAdministrador();
            Configuracion.AsegurarIntegridadUsuarios();

            BE.Usuario antes = ObtenerActivoPorId(idUsuario);
            if (antes == null)
                throw new BE.AppException("err.bll.usuario.no_existe",
                    "El usuario a modificar no existe o está archivado.");

            ValidarDatosAdministrativos(username, email, fechaNacimiento, ObtenerUsernamesExcepto(idUsuario));

            string nuevoUsername = username.Trim();
            string nuevoNombre   = Limpiar(nombre);
            string nuevoApellido = Limpiar(apellido);
            string nuevoEmail    = Limpiar(email);

            string detalle = DescribirCambios(antes, nuevoNombre, nuevoApellido, nuevoUsername, fechaNacimiento, nuevoEmail);
            if (detalle == null)
                throw new BE.AppException("err.bll.usuario.sin_cambios",
                    "No hay cambios para guardar.");

            string actor = SessionManager.GetInstance().Usuario.Username;

            // Si el usuario NO tiene historial previo (p. ej. usuarios semilla creados por script),
            // se graba un snapshot BASELINE del estado ACTUAL (antes del cambio) para que el Historial
            // de Cambios tenga "valor anterior" desde la primera modificación (diff entre versiones).
            var versionBLL = new VersionUsuario();
            if (versionBLL.ObtenerPorUsuario(idUsuario).Count == 0)
                versionBLL.GrabarVersion(idUsuario, actor, "Estado administrativo inicial.");

            usuarioDAL.Modificar(idUsuario, nuevoNombre, nuevoApellido, nuevoUsername, fechaNacimiento, nuevoEmail);

            // Snapshot del NUEVO estado (después del UPDATE) → alimenta el Historial de Cambios.
            versionBLL.GrabarVersion(idUsuario, actor, detalle);

            bitacora.RegistrarSinSesion(
                modulo:     modulo,
                actividad:  "Modificación de Usuario",
                criticidad: BE.Criticidad.Media,
                idUsuario:  SessionManager.GetInstance().Usuario.Id,
                detalle:    $"Usuario ID {idUsuario}: {detalle}");
        }

        // ── Cambio de ROL de un usuario (procedimiento documentado) ──────────────
        // Solo Administrador. Protege contra dejar al sistema sin Administradores activos.
        // Graba snapshot y registra en bitácora.
        public void CambiarRol(string modulo, int idUsuario, string nuevoPerfil)
        {
            ValidarEsAdministrador();
            Configuracion.AsegurarIntegridadUsuarios();

            BE.Usuario antes = ObtenerActivoPorId(idUsuario);
            if (antes == null)
                throw new BE.AppException("err.bll.usuario.no_existe",
                    "El usuario a modificar no existe o está archivado.");

            if (string.IsNullOrWhiteSpace(nuevoPerfil))
                throw new BE.AppException("err.bll.usuario.perfil_requerido", "El perfil/rol es obligatorio.");

            string perfilNorm = NormalizarPerfil(nuevoPerfil);

            // Validación pura (testeable): no quitar el rol al último Administrador activo.
            ValidarPuedeCambiarRol(antes.Perfil, perfilNorm, usuarioDAL.ContarAdministradoresActivos());

            if (string.Equals(antes.Perfil, perfilNorm, StringComparison.OrdinalIgnoreCase))
                throw new BE.AppException("err.bll.usuario.sin_cambios", "No hay cambios para guardar.");

            // Snapshot del estado actual antes del cambio (trazabilidad del cambio de rol).
            new VersionUsuario().GrabarVersion(idUsuario,
                SessionManager.GetInstance().Usuario.Username,
                $"Cambio de rol: '{antes.Perfil}' → '{perfilNorm}'.");

            usuarioDAL.CambiarRol(idUsuario, perfilNorm);

            bitacora.RegistrarSinSesion(
                modulo:     modulo,
                actividad:  "Cambio de Rol de Usuario",
                criticidad: BE.Criticidad.Alta,
                idUsuario:  SessionManager.GetInstance().Usuario.Id,
                detalle:    $"Usuario ID {idUsuario} ('{antes.Username}'): rol '{antes.Perfil}' → '{perfilNorm}'.");
        }

        // Búsqueda por datos NO sensibles (nombre, apellido, email, username). Filtra en
        // memoria sobre los usuarios activos (dataset chico; respeta protección de datos).
        public List<BE.Usuario> Buscar(string filtro)
        {
            var todos = usuarioDAL.ObtenerTodos();
            if (string.IsNullOrWhiteSpace(filtro)) return todos;
            string f = filtro.Trim().ToLowerInvariant();
            bool Coincide(string s) => !string.IsNullOrEmpty(s) && s.ToLowerInvariant().Contains(f);
            return todos.FindAll(u => Coincide(u.Nombre) || Coincide(u.Apellido)
                                      || Coincide(u.Email) || Coincide(u.Username));
        }

        // Lista todos los usuarios del sistema (sin contraseñas).
        public List<BE.Usuario> ObtenerTodos()
        {
            return usuarioDAL.ObtenerTodos();
        }

        // ── Validaciones PURAS (testeables sin sesión ni BD) ─────────────────────

        // Valida los datos administrativos de un usuario: username (no vacío, ≥3, único entre
        // 'otrosUsernames'), email (formato si se ingresó) y fecha de nacimiento (no futura,
        // mayoría de edad). Reutilizable por Alta y Modificar.
        public static void ValidarDatosAdministrativos(string username, string email,
                                                        DateTime? fechaNacimiento, IEnumerable<string> otrosUsernames)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new BE.AppException("err.bll.usuario.username_requerido", "El nombre de usuario es obligatorio.");
            if (username.Trim().Length < 3)
                throw new BE.AppException("err.bll.usuario.username_corto",
                    "El nombre de usuario debe tener al menos 3 caracteres.");
            if (otrosUsernames != null)
                foreach (var u in otrosUsernames)
                    if (string.Equals(u, username.Trim(), StringComparison.OrdinalIgnoreCase))
                        throw new BE.AppException("err.bll.usuario.username_duplicado",
                            "Ya existe un usuario con el nombre '{0}'.", username.Trim());

            if (!string.IsNullOrWhiteSpace(email) && !EsEmailValido(email.Trim()))
                throw new BE.AppException("err.bll.usuario.email_invalido",
                    "El correo electrónico no tiene un formato válido.");

            if (fechaNacimiento.HasValue)
            {
                if (fechaNacimiento.Value.Date > DateTime.Today)
                    throw new BE.AppException("err.bll.usuario.fechanac_futura",
                        "La fecha de nacimiento no puede ser futura.");
                if (fechaNacimiento.Value.Date > DateTime.Today.AddYears(-18))
                    throw new BE.AppException("err.bll.usuario.fechanac_menor",
                        "El usuario debe ser mayor de edad (18 años).");
            }
        }

        // No se puede quitar el rol de Administrador al ÚLTIMO Administrador activo.
        public static void ValidarPuedeCambiarRol(string perfilActual, string nuevoPerfil, int adminsActivos)
        {
            if (BE.Roles.EsAdministrador(perfilActual)
                && !BE.Roles.EsAdministrador(nuevoPerfil)
                && adminsActivos <= 1)
                throw new BE.AppException("err.bll.usuario.ultimo_admin_rol",
                    "No se puede quitar el rol de Administrador al último administrador activo. " +
                    "Asigná el rol Administrador a otro usuario antes de cambiar este.");
        }

        // Validación de email simple y robusta (sin regex frágil): un '@' con texto a ambos
        // lados y un '.' en el dominio.
        private static bool EsEmailValido(string email)
        {
            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1) return false;
            string dominio = email.Substring(at + 1);
            int punto = dominio.IndexOf('.');
            return punto > 0 && punto < dominio.Length - 1 && !email.Contains(" ");
        }

        // Devuelve un resumen "Campo: 'anterior' → 'nuevo'; ..." de los campos que cambian,
        // o null si no hay cambios. Compara solo datos administrativos NO sensibles.
        private static string DescribirCambios(BE.Usuario antes, string nombre, string apellido,
                                               string username, DateTime? fechaNac, string email)
        {
            var partes = new List<string>();
            void Cmp(string campo, string viejo, string nuevo)
            {
                if (!string.Equals(viejo ?? "", nuevo ?? "", StringComparison.Ordinal))
                    partes.Add($"{campo}: '{viejo ?? ""}' → '{nuevo ?? ""}'");
            }
            Cmp("Usuario",  antes.Username, username);
            Cmp("Nombre",   antes.Nombre,   nombre);
            Cmp("Apellido", antes.Apellido, apellido);
            Cmp("Email",    antes.Email,    email);
            string fechaVieja = antes.FechaNacimiento?.ToString("yyyy-MM-dd") ?? "";
            string fechaNueva = fechaNac?.ToString("yyyy-MM-dd") ?? "";
            if (!string.Equals(fechaVieja, fechaNueva, StringComparison.Ordinal))
                partes.Add($"Fecha nac.: '{fechaVieja}' → '{fechaNueva}'");
            return partes.Count == 0 ? null : string.Join("; ", partes);
        }

        private static string Limpiar(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // Usuario activo por Id (desde la lista de activos; la edición ABM opera sobre activos).
        private BE.Usuario ObtenerActivoPorId(int idUsuario)
        {
            return usuarioDAL.ObtenerTodos().Find(u => u.Id == idUsuario);
        }

        // Usernames de los demás usuarios (para validar unicidad excluyendo 'idExcluir').
        // Incluye ACTIVOS y ARCHIVADOS: el UNIQUE(Username) es global, así que un username de un
        // usuario archivado igual está tomado (evita un error de BD poco claro al chocar con la UQ).
        private List<string> ObtenerUsernamesExcepto(int idExcluir)
        {
            var lista = new List<string>();
            foreach (var u in usuarioDAL.ObtenerTodos())
                if (u.Id != idExcluir) lista.Add(u.Username);
            try
            {
                foreach (var u in usuarioDAL.ObtenerArchivados())
                    if (u.Id != idExcluir) lista.Add(u.Username);
            }
            catch { /* BD sin migrar (sin archivados): se valida solo contra activos */ }
            return lista;
        }

        // Convierte el nombre visible del perfil al código interno usado en BD.
        private static string NormalizarPerfil(string perfil)
        {
            switch (perfil.Trim())
            {
                // Jerarquía consolidada (2da entrega)
                case "Operador de Inventario":  return "OperadorDeInventario"; // mantenimiento de prendas
                case "Operador Logístico":      return "OperadorLogistico";    // pedidos / despacho
                case "Gerente Comercial":       return "GerenteComercial";
                case "Gerente de Inventario":   return "GerenteInventario";
                case "Auditor":                 return "Auditor";
                // Roles retirados → se mapean a su reemplazo (por si llega una etiqueta vieja)
                case "Controlador de Stock":    return "OperadorDeInventario";
                case "Encargado de Stock":      return "OperadorDeInventario";
                case "Supervisor":              return "GerenteComercial";
                default:                        return perfil.Trim();
            }
        }
    }
}
