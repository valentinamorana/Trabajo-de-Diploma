using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>Estado vigente (en la base) del usuario de la sesión: si sigue activo y con qué rol.</summary>
    public class UsuarioVigente
    {
        public UsuarioVigente(bool activo, string perfil, string rol = null)
        {
            Activo = activo;
            Perfil = perfil;
            Rol    = rol;
        }

        public bool   Activo { get; }
        public string Perfil { get; }
        public string Rol    { get; }
    }

    /// <summary>
    /// Permisos granulares de ACCIÓN — separan "Ver" de "Configurar" (idea tomada de los
    /// proyectos Agus / tp-ing-soft, adaptada al modelo de patentes de WardrobeFlow).
    ///
    /// • VER un módulo  → patente de menú (ej. mnuClientes): muestra la pantalla y permite leer.
    /// • CONFIGURARLO   → patente de edición (ej. mnuClientesEditar): alta / modificación / baja.
    ///
    /// FAIL-CLOSED: toda escritura exige la patente de edición. El script de instalación
    /// (BD/00_Instalacion_Completa.sql, sección "PERMISOS GRANULARES" y 21c) crea todas las
    /// patentes de edición en bases nuevas y en las actualizadas, así que ya no hay un modo
    /// "sin migrar" que caiga al permiso de VER (antes, si el catálogo no se podía leer, la
    /// acción se habilitaba con la patente de ver: fallaba abierto).
    /// El Administrador siempre tiene acceso (bypass por perfil).
    ///
    /// Además, cada Exigir revalida contra la base que el usuario de la sesión siga ACTIVO y con
    /// el MISMO rol con el que inició sesión (resultado cacheado ~60 s por usuario): un usuario
    /// archivado o al que le cambiaron el rol no puede seguir operando con la sesión abierta.
    /// </summary>
    public static class PermisosAccion
    {
        // ── Decisión PURA (testeable sin BD ni sesión) ──────────────────────────────────
        // ¿El usuario puede ejecutar la acción de escritura?
        //   esAdmin     → bypass total.
        //   tieneEditar → el usuario posee la patente de edición.
        public static bool PermiteAccion(bool esAdmin, bool tieneEditar) => esAdmin || tieneEditar;

        // ── Revalidación del usuario de la sesión ───────────────────────────────────────
        private static readonly TimeSpan VigenciaCache = TimeSpan.FromSeconds(60);
        private static readonly object _lockCache = new object();
        // La entrada se asocia a la SESIÓN (instancia de SessionManager): un nuevo login no reutiliza
        // lo leído para la sesión anterior.
        private static readonly Dictionary<int, (object Sesion, DateTime Leido, UsuarioVigente Valor)> _cache =
            new Dictionary<int, (object, DateTime, UsuarioVigente)>();

        private static Func<int, UsuarioVigente> _lector = LeerDeBase;

        /// <summary>
        /// Lee de la base el estado vigente de un usuario (null si no existe). Reemplazable para
        /// pruebas (Tests/ConfiguracionTests): asignar null restaura la lectura real.
        /// </summary>
        internal static Func<int, UsuarioVigente> LectorUsuarioVigente
        {
            get => _lector;
            set { _lector = value ?? LeerDeBase; LimpiarCacheVigencia(); }
        }

        /// <summary>Descarta la caché de vigencia (por ejemplo al cerrar sesión).</summary>
        public static void LimpiarCacheVigencia()
        {
            lock (_lockCache) _cache.Clear();
        }

        private static UsuarioVigente LeerDeBase(int idUsuario)
        {
            var v = new DAL.Usuario().ObtenerVigencia(idUsuario);
            return v == null ? null : new UsuarioVigente(v.Value.Activo, v.Value.Perfil, v.Value.Rol);
        }

        private static bool MismoTexto(string a, string b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

        // Exige que el usuario de la sesión siga activo y con el rol con el que entró.
        // Falla CERRADO: si no se puede leer la base, la operación no se ejecuta.
        public static void ExigirUsuarioVigente(BE.Usuario usuario, object sesion = null)
        {
            UsuarioVigente vigente;
            var ahora = DateTime.UtcNow;
            lock (_lockCache)
            {
                if (_cache.TryGetValue(usuario.Id, out var c) && ReferenceEquals(c.Sesion, sesion)
                    && ahora - c.Leido < VigenciaCache)
                    vigente = c.Valor;
                else
                    vigente = null;
            }

            if (vigente == null)
            {
                try { vigente = _lector(usuario.Id); }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("[PermisosAccion] No se pudo revalidar el usuario: " + ex.Message);
                    throw new BE.AppException("err.bll.permisos.no_verificable",
                        "No se pudo verificar tu usuario contra la base de datos. La operación no se ejecutó.");
                }
                if (vigente != null)
                    lock (_lockCache) _cache[usuario.Id] = (sesion, ahora, vigente);
            }

            bool mismoRol = vigente != null
                            && MismoTexto(vigente.Perfil, usuario.Perfil)
                            && (vigente.Rol == null || MismoTexto(vigente.Rol, usuario.Rol));
            if (vigente == null || !vigente.Activo || !mismoRol)
                throw new BE.AppException("err.bll.permisos.usuario_no_vigente",
                    "Tu usuario fue desactivado o cambió de rol. Cerrá la sesión y volvé a ingresar.");
        }

        // ── Guard de negocio (fail-closed) ──────────────────────────────────────────────
        // Exige permiso para CONFIGURAR el módulo. 'patenteVer' se conserva en la firma por
        // compatibilidad con los llamadores (documenta qué pantalla gobierna la acción); ya no
        // habilita la escritura por sí sola.
        public static void Exigir(string patenteEditar, string patenteVer)
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");

            var sm = Seguridad.SessionManager.GetInstance();
            if (!PermiteAccion(sm.Usuario.EsAdministrador, sm.TienePermiso(patenteEditar)))
                throw new BE.AppException("err.bll.sin_permiso",
                    "No tiene permiso para ejecutar esta operación ('{0}').", patenteEditar);

            ExigirUsuarioVigente(sm.Usuario, sm);
        }
    }
}
