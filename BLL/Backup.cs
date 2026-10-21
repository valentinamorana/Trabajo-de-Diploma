using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BLL
{
    public class Backup
    {
        private readonly DAL.Interfaces.IBackupDAL _dal;
        // Bitácora perezosa: solo se instancia cuando una operación de escritura la usa, para que
        // construir BLL.Backup (y testear el preview RF-08) no toque la BD ni el App.config.
        private Servicios.IRegistroBitacora _bitacoraLazy;
        private Servicios.IRegistroBitacora _bitacora => _bitacoraLazy ?? (_bitacoraLazy = Servicios.FabricaBitacora.CrearSistema());

        // DI: el constructor por defecto usa el DAL real; el otro permite inyectar un doble
        // de prueba (tests del preview de pérdida RF-08 sin tocar SQL Server).
        public Backup() : this(new DAL.Backup()) { }
        public Backup(DAL.Interfaces.IBackupDAL dal)
        {
            _dal = dal;
        }

        /// <summary>Al arrancar: borra las copias sin cifrar que haya dejado un backup o una
        /// restauración interrumpidos (ver DAL.Backup.LimpiarTemporalesHuerfanos). Nunca lanza.</summary>
        public static int LimpiarTemporalesHuerfanos() => DAL.Backup.LimpiarTemporalesHuerfanos();

        // Extensión de los backups CIFRADOS (con contraseña). Los .bak planos legacy se siguen
        // pudiendo restaurar para no romper copias anteriores.
        public const string ExtensionCifrada = ".wfbak";

        /// <summary>Carpeta única de los backups (al lado del ejecutable). Fuente única para la
        /// BLL y la GUI (antes la ruta estaba repetida en cuatro lugares).</summary>
        public static readonly string CarpetaBackups =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

        /// <summary>Backups de la carpeta (.bak legacy y cifrados), del más reciente al más viejo.
        /// Lista vacía si la carpeta todavía no existe.</summary>
        public static List<FileInfo> ObtenerBackups()
        {
            if (!Directory.Exists(CarpetaBackups)) return new List<FileInfo>();
            var dir = new DirectoryInfo(CarpetaBackups);
            return dir.GetFiles("*.bak")
                .Concat(dir.GetFiles("*" + ExtensionCifrada))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();
        }

        /// <summary>Backup más reciente de la carpeta (.bak legacy o cifrado), o null si no hay.</summary>
        public static FileInfo ObtenerUltimoBackup() => ObtenerBackups().FirstOrDefault();

        /// <summary>Crea la carpeta de backups si no existe y devuelve su ruta.</summary>
        public static string AsegurarCarpetaBackups()
        {
            Directory.CreateDirectory(CarpetaBackups);
            return CarpetaBackups;
        }

        // ¿La ruta apunta a un archivo DENTRO de la carpeta de backups (sin "..", ni otra carpeta)?
        internal static bool EstaDentroDeCarpetaBackups(string rutaArchivo, string carpeta = null)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo)) return false;
            try
            {
                string baseDir = Path.GetFullPath(carpeta ?? CarpetaBackups)
                                     .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string completa = Path.GetFullPath(rutaArchivo);
                return string.Equals(Path.GetDirectoryName(completa)?.TrimEnd(Path.DirectorySeparatorChar), baseDir,
                                     StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception) { return false; }
        }

        private static void ValidarAdministrador()
        {
            BLLHelper.ExigirAdministrador("err.bll.backup.sin_permiso",
                "Solo un Administrador puede realizar operaciones de backup y restauración.");
        }

        // RF-04 — Antes de generar un backup se verifica la integridad (DVH/DVV) de la
        // información a respaldar. Si la base está comprometida, se cancela el backup para
        // no perpetuar datos corruptos en la copia. Lanza AppException con el detalle.
        private static void VerificarIntegridadAntesDeRespaldar(bool automatico = false)
        {
            bool comprometidaAntes = Configuracion.IntegridadComprometida;
            if (!Configuracion.VerificarIntegridadDV(out ResultadoIntegridad r))
            {
                // En un backup automático, un error TÉCNICO al verificar (timeout, base en
                // SINGLE_USER) no es corrupción: no se deja la sesión marcada como comprometida
                // (apagaría los automáticos y los controles de BLLHelper) y el backup cuenta como
                // fallido, así queda en la bitácora y se reintenta.
                if (automatico && r?.ErrorTecnico != null)
                {
                    Configuracion.IntegridadComprometida = comprometidaAntes;
                    throw new InvalidOperationException("No se pudo verificar la integridad de la base: " + r.ErrorTecnico);
                }

                string detalle = r?.MensajeES ?? "Se detectaron inconsistencias en los dígitos verificadores.";
                throw new BE.AppException("err.bll.backup.integridad",
                    "No se puede generar el backup: la base tiene problemas de integridad.\n" +
                    "Repárelos desde Administrar → Diagnóstico de Integridad antes de respaldar.\n\n" + detalle);
            }
        }

        // ── Punto único de generación de backups ──────────────────────────────────────────
        // El botón "Generar Copia de Seguridad", el de instalación limpia, el login y el reloj de
        // 24 h terminan todos acá: mismas validaciones (integridad RF-04), mismo DAL y registro en
        // la bitácora. Solo cambian el nombre del archivo y el cifrado según el origen:
        //   Manual / Inicial      → WardrobeFlow_Backup|Inicial_yyyyMMddHHmmss_USUARIO.wfbak, cifrado.
        //   Login / Programado    → auto_<día>.bak, sin cifrar, rota por día de la semana.
        //   PrevioDemo            → WardrobeFlow_PrevioDemo_yyyyMMddHHmmss_USUARIO.bak, sin cifrar.
        private string HacerBackup(OrigenBackup origen, string modulo, string dirDestino, string claveCifrado, DateTime fecha)
        {
            VerificarIntegridadAntesDeRespaldar(EsOrigenAutomatico(origen));   // RF-04

            string filename = NombreArchivo(origen, fecha);
            string ruta     = Path.Combine(dirDestino, filename);

            if (EsOrigenAutomatico(origen) || origen == OrigenBackup.PrevioDemo)
                _dal.RealizarBackup(ruta);   // automáticos: pisa el archivo del día (rotación semanal)
            else
                GenerarBackupCifrado(ruta, claveCifrado);

            _bitacora.Registrar(modulo, ActividadBitacora(origen, filename),
                EsOrigenAutomatico(origen) ? BE.Criticidad.Media : BE.Criticidad.Alta);
            return filename;
        }

        private static bool EsOrigenAutomatico(OrigenBackup origen)
            => origen == OrigenBackup.Login || origen == OrigenBackup.Programado;

        private static string NombreArchivo(OrigenBackup origen, DateTime fecha)
        {
            if (EsOrigenAutomatico(origen)) return NombreArchivoAutomatico(fecha);

            // El usuario en el nombre permite saber quién hizo cada backup desde la lista.
            string usuario = Seguridad.SessionManager.IsLoggedIn
                ? Seguridad.SessionManager.GetInstance().Usuario.Username
                : "sistema";
            string usuarioSanitizado = Regex.Replace(usuario, @"[^\w]", "_");
            if (origen == OrigenBackup.PrevioDemo)
                return $"{PrefijoPrevioDemo}{fecha:yyyyMMddHHmmss}_{usuarioSanitizado}.bak";
            string tipo = origen == OrigenBackup.Inicial ? "Inicial" : "Backup";
            return $"WardrobeFlow_{tipo}_{fecha:yyyyMMddHHmmss}_{usuarioSanitizado}{ExtensionCifrada}";
        }

        private static string ActividadBitacora(OrigenBackup origen, string filename)
        {
            switch (origen)
            {
                case OrigenBackup.Inicial: return $"{BE.ActividadesBitacora.BackupInstalacionLimpiaPrefijo}'{filename}'";
                case OrigenBackup.Manual:  return $"{BE.ActividadesBitacora.BackupCifradoGeneradoPrefijo}'{filename}'";
                case OrigenBackup.PrevioDemo: return $"{BE.ActividadesBitacora.BackupPrevioDemoPrefijo}'{filename}'";
                default:                  return $"{BE.ActividadesBitacora.BackupAutomaticoGeneradoPrefijo}'{filename}' {BE.ActividadesBitacora.SufijoOrigenBackup(origen.ToString())}";
            }
        }

        // Botón "Generar Copia de Seguridad": solo Administrador, cifrado con la contraseña.
        // Formato: WardrobeFlow_Backup_yyyyMMddHHmmss_USUARIO.wfbak
        public string RealizarBackup(string modulo, string dirDestino, string claveCifrado)
        {
            ValidarAdministrador();
            lock (_operacionEnCurso)
                return HacerBackup(OrigenBackup.Manual, modulo, dirDestino, claveCifrado, DateTime.Now);
        }

        // Resguardo previo a "Restablecer datos de demo" (BLL.DatosDemo): solo Administrador. Es un
        // .bak SIN cifrar (como los automáticos: no pide otra contraseña) en la carpeta de backups,
        // y se restaura desde esta misma pantalla. Verifica antes la integridad (RF-04): si la base
        // está comprometida o el backup falla, lanza y la demo no se restablece.
        public const string PrefijoPrevioDemo = "WardrobeFlow_PrevioDemo_";

        /// <summary>
        /// "Restablecer datos de demo" de punta a punta DENTRO del mismo bloqueo de backup/restauración
        /// (_operacionEnCurso): backup de resguardo → <paramref name="restablecer"/> (procedimiento
        /// dbo.RestablecerDatosDemo) → inicialización y verificación de los dígitos verificadores. Así
        /// ningún backup automático ni restauración se mete entre el resguardo y la carga, ni copia la
        /// base a mitad de la inicialización. Devuelve el nombre del backup previo.
        /// </summary>
        public string RestablecerDemoConResguardo(string modulo, Action restablecer)
        {
            ValidarAdministrador();
            if (restablecer == null) throw new ArgumentNullException(nameof(restablecer));
            lock (_operacionEnCurso)
            {
                // Resguardo: si falla (o la integridad está comprometida), se corta acá.
                string backup = HacerBackup(OrigenBackup.PrevioDemo, modulo, AsegurarCarpetaBackups(), null, DateTime.Now);

                restablecer();

                // Las tablas que cambió el procedimiento quedaron "sin calcular": se inicializan ahora.
                if (!Configuracion.VerificarIntegridadDV(out ResultadoIntegridad r))
                    throw new BE.AppException("err.bll.demo.integridad",
                        "Los datos de demo se cargaron, pero la verificación de integridad no cerró. " +
                        "Revise Administrar → Sistema → Diagnóstico de Integridad.\n\n" + (r?.MensajeES ?? ""));
                return backup;
            }
        }

        // Un backup o una restauración a la vez en todo el proceso: el reloj de 24 h corre en otro
        // hilo y no debe chocar con una restauración (SINGLE_USER, archivo que se está copiando)
        // ni con el botón. Los manuales y la restauración esperan; el automático se saltea.
        private static readonly object _operacionEnCurso = new object();

        // Genera el .bak con SQL en un temporal plano y lo CIFRA hacia la ruta final (.wfbak).
        // El temporal plano se borra siempre (finally) para no dejar datos sin cifrar en disco.
        private void GenerarBackupCifrado(string rutaFinal, string claveCifrado)
        {
            if (string.IsNullOrEmpty(claveCifrado))
                throw new BE.AppException("err.bll.backup.clave_requerida",
                    "Debe ingresar una contraseña para cifrar el backup.");

            string tempPlano = Path.Combine(Path.GetTempPath(), $"WF_bak_{Guid.NewGuid():N}.bak");
            try
            {
                _dal.RealizarBackup(tempPlano);
                Seguridad.CifradorArchivos.Cifrar(tempPlano, rutaFinal, claveCifrado);
            }
            finally
            {
                if (File.Exists(tempPlano)) try { File.Delete(tempPlano); } catch { }
            }
        }

        // RF-06 — Genera un backup de INSTALACIÓN LIMPIA (estado inicial del sistema).
        // Se nombra con el marcador "Inicial" para distinguirlo en la lista y poder volver
        // siempre al punto de partida conocido. También valida integridad antes de generarlo.
        // Formato: WardrobeFlow_Inicial_yyyyMMddHHmmss_USUARIO.bak
        public string RealizarBackupInicial(string modulo, string dirDestino, string claveCifrado)
        {
            ValidarAdministrador();
            lock (_operacionEnCurso)
                return HacerBackup(OrigenBackup.Inicial, modulo, dirDestino, claveCifrado, DateTime.Now);
        }

        // ── Backups automáticos (al iniciar sesión y reloj de 24 h) ───────────────────────
        // Los dispara el sistema, no una pantalla: por eso no exigen Administrador (corren con
        // cualquier rol) ni piden contraseña (son .bak sin cifrar). No crean un archivo nuevo
        // cada vez: rotan sobre 7 archivos fijos, uno por día de la semana (auto_lunes.bak, ...),
        // y cada backup pisa el de ese día. Así el disco queda acotado y siempre hay un punto de
        // restauración por cada uno de los últimos 7 días.

        public const string PrefijoAutomatico = "auto_";

        private static readonly string[] DiasSemana =
            { "domingo", "lunes", "martes", "miercoles", "jueves", "viernes", "sabado" };

        /// <summary>Nombre del archivo rotativo del día: auto_lunes.bak, auto_martes.bak, ...</summary>
        public static string NombreArchivoAutomatico(DateTime fecha)
            => $"{PrefijoAutomatico}{DiasSemana[(int)fecha.DayOfWeek]}.bak";

        public static bool EsAutomatico(string nombreArchivo)
            => !string.IsNullOrEmpty(nombreArchivo) &&
               Path.GetFileName(nombreArchivo).StartsWith(PrefijoAutomatico, StringComparison.OrdinalIgnoreCase);

        /// <summary>Fecha del último backup de cualquier tipo (manual, inicial o automático), o
        /// null si no hay. Cualquier backup reinicia la cuenta del login y del reloj de 24 h.</summary>
        public static DateTime? FechaUltimoBackup() => ObtenerUltimoBackup()?.LastWriteTime;

        /// <summary>Cuándo le toca al reloj de 24 h: último backup + 24 h (ahora si no hay ninguno).</summary>
        public static DateTime ProximoBackupProgramado(DateTime ahora)
        {
            DateTime? ultimo = FechaUltimoBackup();
            return ultimo.HasValue ? ultimo.Value.AddHours(24) : ahora;
        }

        /// <summary>
        /// Backup al iniciar sesión (cualquier rol). Se saltea si la integridad está comprometida
        /// (no se guarda una base adulterada como punto de restauración bueno) o si el último backup
        /// es más nuevo que el intervalo configurado (0 = en todos los logins). Nunca lanza: si
        /// falla, el login sigue y el resultado se avisa en el menú.
        /// </summary>
        public ResultadoBackupAutomatico HacerBackupAlIniciarSesion()
        {
            try
            {
                int intervalo = Configuracion.ObtenerIntervaloBackupLoginMinutos();
                DateTime? ultimo = FechaUltimoBackup();   // lee la carpeta: puede fallar (permisos)
                if (intervalo > 0 && ultimo.HasValue && DateTime.Now - ultimo.Value < TimeSpan.FromMinutes(intervalo))
                    return ResultadoBackupAutomatico.Salteado("Hay un backup más nuevo que el intervalo configurado.");

                return HacerBackupAutomatico(OrigenBackup.Login);
            }
            catch (Exception ex)
            {
                return ResultadoBackupAutomatico.Fallido(CarpetaBackups, ex.Message);
            }
        }

        /// <summary>
        /// Backup automático (login o reloj de 24 h). Usa el mismo método que el botón
        /// (<see cref="HacerBackup"/>), sin exigir Administrador porque lo dispara el sistema.
        /// Se saltea si la integridad está comprometida. Con un origen automático nunca lanza.
        /// </summary>
        public ResultadoBackupAutomatico HacerBackupAutomatico(OrigenBackup origen)
        {
            if (!EsOrigenAutomatico(origen))
                throw new ArgumentException("Solo Login o Programado son backups automáticos.", nameof(origen));

            DateTime fecha = DateTime.Now;
            string ruta = Path.Combine(CarpetaBackups, NombreArchivoAutomatico(fecha));
            bool tomado = false;
            try
            {
                // Chequeo barato primero: con la base ya marcada como comprometida no se toca nada.
                if (Configuracion.IntegridadComprometida)
                    return ResultadoBackupAutomatico.Salteado("La integridad de la base está comprometida.");

                System.Threading.Monitor.TryEnter(_operacionEnCurso, ref tomado);
                if (!tomado)
                    return ResultadoBackupAutomatico.Salteado("Hay un backup o una restauración en curso.");

                HacerBackup(origen, "Backup automático", AsegurarCarpetaBackups(), null, fecha);
                return ResultadoBackupAutomatico.Realizado(ruta);
            }
            catch (BE.AppException ex) when (ex.Clave == "err.bll.backup.integridad")
            {
                // RF-04: no se guarda una base adulterada como punto de restauración bueno.
                return ResultadoBackupAutomatico.Salteado("La integridad de la base está comprometida.");
            }
            catch (Exception ex)
            {
                return ResultadoBackupAutomatico.Fallido(ruta, ex.Message);
            }
            finally
            {
                if (tomado) System.Threading.Monitor.Exit(_operacionEnCurso);
            }
        }

        /// <summary>Registra en la bitácora un backup automático fallido. Nunca lanza.</summary>
        public void RegistrarFalloAutomatico(OrigenBackup origen, ResultadoBackupAutomatico r)
        {
            try
            {
                _bitacora.Registrar("Backup automático",
                    $"{BE.ActividadesBitacora.BackupAutomaticoFallidoPrefijo}{BE.ActividadesBitacora.SufijoOrigenBackup(origen.ToString())} {r.Ruta}: {r.Error}",
                    BE.Criticidad.Alta);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Backup.RegistrarFalloAutomatico] " + ex.Message);
            }
        }

        // Aviso del backup de login para mostrar UNA vez al abrir el menú (lo deja el Login y lo
        // consume el Menu). null = nada que avisar.
        private static ResultadoBackupAutomatico _avisoLogin;

        public static void GuardarAvisoLogin(ResultadoBackupAutomatico r)
            => _avisoLogin = r != null && r.Estado == EstadoBackupAutomatico.Fallido ? r : null;

        public static ResultadoBackupAutomatico TomarAvisoLogin()
        {
            var r = _avisoLogin;
            _avisoLogin = null;
            return r;
        }

        // RF-08 — Fecha en que se completó un backup (para informar el alcance de la pérdida
        // antes de restaurar). Devuelve null si no puede leerse.
        public DateTime? ObtenerFechaBackup(string rutaArchivo)
        {
            return _dal.ObtenerFechaBackup(rutaArchivo);
        }

        // RF-08 — Preview de pérdida a nivel REGISTRO: dada la fecha del backup, devuelve cuántos
        // registros de la base actual son posteriores a esa fecha (los que se perderían al
        // restaurar), desglosados por entidad. Solo incluye entidades con al menos 1 registro.
        // Es de SOLO LECTURA (no abre sesión ni modifica nada): se puede usar también en el
        // flujo de recuperación de arranque, donde todavía no hay sesión iniciada.
        // Devuelve lista vacía si la fecha es null o no hay cambios posteriores.
        public List<BE.CambioPosterior> ObtenerCambiosDesde(DateTime? fechaBackup)
        {
            if (!fechaBackup.HasValue) return new List<BE.CambioPosterior>();
            return _dal.ContarCambiosPosterioresA(fechaBackup.Value);
        }

        // Indica si un archivo es un backup CIFRADO (por su extensión .wfbak).
        public static bool EsCifrado(string rutaArchivo)
        {
            return !string.IsNullOrEmpty(rutaArchivo) &&
                   rutaArchivo.EndsWith(ExtensionCifrada, StringComparison.OrdinalIgnoreCase);
        }

        // Restaura un backup. Si es .wfbak (cifrado) lo descifra con la clave a un temporal y
        // restaura desde ahí; si es .bak plano (legacy) restaura directo. claveCifrado solo se
        // usa para los cifrados; una clave incorrecta da un mensaje claro.
        public void RestaurarBackup(string modulo, string rutaArchivo, string claveCifrado = null)
        {
            ValidarAdministrador();
            lock (_operacionEnCurso)
                RestaurarBackupSinBloqueo(modulo, rutaArchivo, claveCifrado);
        }

        private void RestaurarBackupSinBloqueo(string modulo, string rutaArchivo, string claveCifrado)
        {

            bool sinVerificacion = false;
            if (EsCifrado(rutaArchivo))
            {
                // Descifrar a una carpeta que la cuenta de SQL Server pueda LEER (no al %TEMP% del
                // usuario, que da Access denied al restaurar).
                string tempPlano = Path.Combine(DAL.Backup.DirectorioTempSeguro(), $"WF_restore_{Guid.NewGuid():N}.bak");
                try
                {
                    Seguridad.FormatoArchivoCifrado formato;
                    try
                    {
                        // v2: el HMAC se verifica ANTES de descifrar; v1 (sin HMAC) se sigue leyendo.
                        formato = Seguridad.CifradorArchivos.Descifrar(rutaArchivo, tempPlano, claveCifrado);
                    }
                    catch (Seguridad.ArchivoCifradoAlteradoException)
                    {
                        throw new BE.AppException("err.bll.backup.alterado",
                            "El backup fue alterado o está incompleto: no pasó la verificación de integridad y no se restauró.");
                    }
                    catch (CryptographicException)
                    {
                        throw new BE.AppException("err.bll.backup.clave_invalida",
                            "La contraseña del backup es incorrecta o el archivo está dañado.");
                    }
                    sinVerificacion = formato == Seguridad.FormatoArchivoCifrado.V1SinAutenticacion;
                    _dal.RestaurarBackup(tempPlano);
                }
                finally
                {
                    if (File.Exists(tempPlano)) try { File.Delete(tempPlano); } catch { }
                }
            }
            else
            {
                _dal.RestaurarBackup(rutaArchivo);   // .bak plano (legacy)
            }

            _bitacora.Registrar(modulo,
                $"{BE.ActividadesBitacora.BaseDeDatosRestauradaPrefijo}'{Path.GetFileName(rutaArchivo)}'",
                BE.Criticidad.Alta);
            // Un .wfbak del formato anterior no trae HMAC: se restaura para no perder copias
            // viejas, pero queda asentado que su integridad no pudo verificarse.
            if (sinVerificacion)
                _bitacora.Registrar(modulo,
                    $"{BE.ActividadesBitacora.BackupSinVerificacionPrefijo}'{Path.GetFileName(rutaArchivo)}'",
                    BE.Criticidad.Alta);
        }

        public void EliminarBackup(string modulo, string rutaArchivo)
        {
            ValidarAdministrador();
            // Solo se borran backups de la carpeta de backups: la ruta llega de la pantalla y no
            // debe poder apuntar a cualquier archivo del equipo.
            string extension = Path.GetExtension(rutaArchivo ?? "");
            if (!EstaDentroDeCarpetaBackups(rutaArchivo) ||
                !(extension.Equals(".bak", StringComparison.OrdinalIgnoreCase) ||
                  extension.Equals(ExtensionCifrada, StringComparison.OrdinalIgnoreCase)))
                throw new BE.AppException("err.bll.backup.fuera_de_carpeta",
                    "Solo se pueden eliminar archivos de backup de la carpeta de backups del sistema.");
            if (!File.Exists(rutaArchivo))
                throw new FileNotFoundException("El archivo de backup no existe.", rutaArchivo);

            string filename = Path.GetFileName(rutaArchivo);
            File.Delete(rutaArchivo);
            _bitacora.Registrar(modulo,
                $"{BE.ActividadesBitacora.BackupEliminadoPrefijo}'{filename}'",
                BE.Criticidad.Alta);
        }

        // Extrae el autor del nombre de un archivo de backup.
        // Formato: WardrobeFlow_Backup_yyyyMMddHHmmss_USUARIO.bak
        // Devuelve "—" si el nombre no sigue la convención.
        // Reconoce ambos prefijos: "WardrobeFlow_Backup_" (RealizarBackup) y
        // "WardrobeFlow_Inicial_" (RealizarBackupInicial) — antes solo reconocía el primero, así
        // que la UI (GUI.BackupForm, columna "Autor") siempre mostraba autor desconocido para
        // cualquier backup de instalación limpia, aunque el username estuviera en el nombre.
        private static readonly string[] PrefijosConocidos = { "WardrobeFlow_Backup_", "WardrobeFlow_Inicial_", PrefijoPrevioDemo };

        public string ExtraerAutorDeNombre(string nombreArchivo)
        {
            if (EsAutomatico(nombreArchivo)) return "sistema (automático)";

            string prefix = PrefijosConocidos.FirstOrDefault(p => nombreArchivo.StartsWith(p));
            if (prefix == null || nombreArchivo.Length <= prefix.Length + 15)
                return "—";

            string rest = nombreArchivo.Substring(prefix.Length);
            bool isNewFormat = rest.Length >= 15
                && rest.Substring(0, 14).All(char.IsDigit)
                && rest[14] == '_';

            if (!isNewFormat) return "—";

            return Path.GetFileNameWithoutExtension(rest.Substring(15));
        }
    }

    /// <summary>Quién disparó un backup automático (queda en la bitácora).</summary>
    public enum OrigenBackup { Manual, Inicial, Login, Programado, PrevioDemo }

    public enum EstadoBackupAutomatico { Realizado, Salteado, Fallido }

    /// <summary>Resultado de un backup automático: se hizo, se salteó (y por qué) o falló.</summary>
    public class ResultadoBackupAutomatico
    {
        public EstadoBackupAutomatico Estado { get; private set; }
        public string Ruta   { get; private set; }
        public string Motivo { get; private set; }   // por qué se salteó
        public string Error  { get; private set; }   // detalle técnico de la falla

        public static ResultadoBackupAutomatico Realizado(string ruta)
            => new ResultadoBackupAutomatico { Estado = EstadoBackupAutomatico.Realizado, Ruta = ruta };
        public static ResultadoBackupAutomatico Salteado(string motivo)
            => new ResultadoBackupAutomatico { Estado = EstadoBackupAutomatico.Salteado, Motivo = motivo };
        public static ResultadoBackupAutomatico Fallido(string ruta, string error)
            => new ResultadoBackupAutomatico { Estado = EstadoBackupAutomatico.Fallido, Ruta = ruta, Error = error };
    }
}
