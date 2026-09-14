namespace BE
{
    /// <summary>
    /// Constantes de texto para los literales de "actividad" que la BLL escribe en la
    /// bitácora del sistema (Servicios.Bitacora.Registrar/RegistrarSinSesion). Existen para
    /// que quien ESCRIBE (BLL) y quien TRADUCE para mostrar (GUI.DashboardForm.
    /// TraducirActividad) usen la misma fuente en vez de retipear el mismo string en dos
    /// archivos — antes, cambiar el texto en uno sin actualizar el otro rompía la
    /// traducción en silencio (ver AUDITORIA_TECNICA_2026-09-10.md, revisión de lógica
    /// en la GUI). La tabla sigue guardando texto plano en español, no una clave — esto
    /// no es un cambio de esquema, solo elimina la duplicación del literal en sí.
    ///
    /// Los que terminan en "Prefijo" son la parte FIJA de una actividad con datos
    /// dinámicos (nombre de archivo, username, ID de versión...) concatenados después.
    /// </summary>
    public static class ActividadesBitacora
    {
        public const string InicioSesion                   = "Inicio Sesion";
        public const string CierreSesion                   = "Cierre Sesion";
        public const string CambioContrasenaPropia         = "Cambio de Contrasena Propia";
        public const string BloqueoDeCuenta                = "Bloqueo de Cuenta";
        public const string IntentoFallidoLogin             = "Intento Fallido Login";
        public const string BajaLogicaUsuario               = "Baja Logica Usuario";
        public const string CambioDeRolDeUsuario            = "Cambio de Rol de Usuario";
        public const string DesbloqueoConClaveDeEmergencia  = "Desbloqueo con Clave de Emergencia";
        public const string ModificacionDeUsuario           = "Modificación de Usuario";
        public const string PurgaUsuariosArchivados         = "Purga Usuarios Archivados";
        public const string ResetContrasena                 = "Reset Contrasena";
        public const string SolicitudRecuperacionClave      = "Solicitud Recuperacion Clave";

        public const string BackupInstalacionLimpiaPrefijo = "Backup de instalación limpia (cifrado) generado: ";
        public const string BackupCifradoGeneradoPrefijo   = "Backup cifrado generado: ";
        public const string BackupEliminadoPrefijo         = "Backup eliminado: ";
        public const string BaseDeDatosRestauradaPrefijo   = "Base de datos restaurada desde ";
        public const string DesbloqueoDeCuentaPrefijo      = "Desbloqueo de Cuenta: ";
        public const string AltaUsuarioPrefijo             = "Alta Usuario: ";
        public const string RestauracionAVersionPrefijo    = "Restauración a versión ";
    }
}
