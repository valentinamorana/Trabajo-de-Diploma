using System;
using System.Threading;

namespace BLL
{
    /// <summary>
    /// Reloj que garantiza al menos un backup cada 24 h mientras la aplicación está abierta.
    ///
    /// La primera revisión es al minuto de iniciarse y después una por hora. En cada revisión
    /// calcula próximo = último backup + 24 h y, si ya llegó esa hora, hace el backup automático.
    /// Compara contra los archivos de la carpeta de backups, no contra un contador en memoria:
    /// cualquier backup (manual, de login o del reloj) reinicia la cuenta, y si la aplicación se
    /// cierra no se pierde nada (al volver a abrirla, si quedó un backup atrasado, lo hace).
    ///
    /// Si falla, reintenta en la revisión siguiente, pero deja en la bitácora un solo error cada
    /// 24 h para no llenarla.
    /// </summary>
    public static class BackupProgramado
    {
        private static readonly TimeSpan PrimeraRevision = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Periodo         = TimeSpan.FromHours(1);
        private static readonly TimeSpan Frecuencia      = TimeSpan.FromHours(24);

        private static readonly object _lock = new object();
        private static Timer _timer;
        private static int _enCurso;                 // 1 mientras corre una revisión
        private static DateTime? _ultimoErrorRegistrado;

        public static void Iniciar()
        {
            lock (_lock)
            {
                if (_timer != null) return;
                _timer = new Timer(_ => Revisar(), null, PrimeraRevision, Periodo);
            }
        }

        // Espera (hasta 2 min) a que termine una revisión en curso: si el proceso se cierra o se
        // reinicia en medio del backup, el archivo del día podría quedar a medio escribir.
        private static readonly TimeSpan EsperaAlDetener = TimeSpan.FromMinutes(2);

        public static void Detener()
        {
            lock (_lock)
            {
                if (_timer == null) return;
                var terminado = new ManualResetEvent(false);
                // Si la espera vence con la revisión todavía corriendo, el evento no se libera: el
                // timer lo señaliza al terminar y no debe encontrarlo ya liberado.
                if (!_timer.Dispose(terminado) || terminado.WaitOne(EsperaAlDetener))
                    terminado.Dispose();
                _timer = null;
            }
        }

        /// <summary>Una revisión del reloj. Pública para poder probarla sin esperar al timer.</summary>
        public static ResultadoBackupAutomatico Revisar()
        {
            // El timer no espera a que termine la revisión anterior: si un backup largo sigue
            // corriendo, esta revisión no hace nada.
            if (Interlocked.CompareExchange(ref _enCurso, 1, 0) != 0)
                return ResultadoBackupAutomatico.Salteado("Hay una revisión en curso.");
            try
            {
                if (Configuracion.IntegridadComprometida)
                    return ResultadoBackupAutomatico.Salteado("La integridad de la base está comprometida.");

                DateTime ahora = DateTime.Now;
                if (ahora < Backup.ProximoBackupProgramado(ahora))
                    return ResultadoBackupAutomatico.Salteado("Todavía no pasaron 24 h desde el último backup.");

                var bll = new Backup();
                var r = bll.HacerBackupAutomatico(OrigenBackup.Programado);
                if (r.Estado == EstadoBackupAutomatico.Fallido &&
                    (!_ultimoErrorRegistrado.HasValue || ahora - _ultimoErrorRegistrado.Value >= Frecuencia))
                {
                    bll.RegistrarFalloAutomatico(OrigenBackup.Programado, r);
                    _ultimoErrorRegistrado = ahora;
                }
                return r;
            }
            catch (Exception ex)
            {
                // Corre en un hilo del pool: una excepción acá cerraría la aplicación.
                System.Diagnostics.Trace.TraceError("[BackupProgramado.Revisar] " + ex.Message);
                return ResultadoBackupAutomatico.Fallido(Backup.CarpetaBackups, ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _enCurso, 0);
            }
        }
    }
}
