using System;

namespace BLL
{
    /// <summary>
    /// "Restablecer datos de demo" (Administración → Backup): vuelve a dejar la base con los datos de
    /// prueba de la instalación, con las fechas corridas a HOY, para la defensa. Pasos:
    ///   1. Solo un Administrador con sesión vigente.
    ///   2. Backup de resguardo previo (.bak sin cifrar en la carpeta de backups; verifica antes la
    ///      integridad, RF-04). Si no se puede hacer, NO se restablece nada.
    ///   3. dbo.RestablecerDatosDemo (procedimiento del script de instalación): borra los datos de
    ///      negocio y carga la demo en una sola transacción.
    ///   4. Inicializa los dígitos verificadores de las tablas que cambió (mismo mecanismo que tras
    ///      una instalación: marca 'DVInicializacionPendiente') y verifica la integridad de todo.
    ///   5. Lo deja en la bitácora (criticidad Alta).
    /// </summary>
    public class DatosDemo
    {
        private readonly DAL.DatosDemo _dal = new DAL.DatosDemo();

        /// <summary>Texto que el Administrador tiene que escribir para confirmar.</summary>
        public const string TextoConfirmacion = DAL.DatosDemo.Confirmacion;

        /// <summary>Restablece la demo. Devuelve el nombre del backup previo.</summary>
        public string Restablecer(string modulo, string confirmacion)
        {
            BLLHelper.ExigirAdministrador("err.bll.demo.sin_permiso",
                "Solo un Administrador puede restablecer los datos de demo.");
            if (!string.Equals(confirmacion?.Trim(), TextoConfirmacion, StringComparison.Ordinal))
                throw new BE.AppException("err.bll.demo.confirmacion",
                    "El texto de confirmación no coincide: no se restableció nada.");
            if (!_dal.ProcedimientoDisponible())
                throw new BE.AppException("err.bll.demo.sin_procedimiento",
                    "La base no tiene el procedimiento de datos de demo. Ejecute el instalador (script de la base) de esta versión.");

            // Pasos 2 a 4 en un solo bloqueo de backup/restauración (BLL.Backup): ningún backup
            // automático ni restauración se mete entre el resguardo, la carga y la verificación.
            string backup = new Backup().RestablecerDemoConResguardo(modulo, _dal.Restablecer);

            Servicios.FabricaBitacora.CrearSistema().Registrar(modulo,
                $"{BE.ActividadesBitacora.DatosDemoRestablecidosPrefijo}'{backup}'", BE.Criticidad.Alta);
            return backup;
        }
    }
}
