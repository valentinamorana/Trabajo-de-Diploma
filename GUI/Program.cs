using System;
using System.Windows.Forms;

namespace GUI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // El negocio opera en Argentina: montos en pesos ($ 1.500,00) y fechas dd/MM/yyyy sin
            // importar la configuración regional de la PC (con Windows en España, "C2" salía en €).
            // useUserOverride: false → ignora símbolos personalizados en el Panel de control.
            // El idioma de la interfaz (ES/EN/RU/PT) es aparte: lo maneja GestorIdioma.
            var culturaNegocio = new System.Globalization.CultureInfo("es-AR", false);
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culturaNegocio;
            System.Threading.Thread.CurrentThread.CurrentCulture         = culturaNegocio;

            // Handler GLOBAL de excepciones no controladas: las registra en la bitácora (criticidad
            // Alta) y muestra un aviso, en vez de cerrar la app de forma muda. (Patrón de Stach.)
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, a) => ManejarExcepcionGlobal(a.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, a) => ManejarExcepcionGlobal(a.ExceptionObject as Exception);

            if (!BLL.Configuracion.VerificarConexionDAL(out string errConexion))
            {
                MessageBox.Show(errConexion, "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Etapa 5 — i18n 100% desde BD: sembrar (si falta) y cargar el idioma por defecto desde
            // la BD ANTES de mostrar cualquier pantalla, para que toda la app (Login y diálogos de
            // arranque incluidos) se renderice desde la base. Si la BD/i18n fallara, el Traductor
            // sigue cayendo a los diccionarios hardcodeados (fallback de seguridad).
            InicializarIdiomaDesdeBD();

            // T07 — VERIFICACIÓN DE INTEGRIDAD ANTES DEL LOGIN (requisito de cátedra).
            // "Al iniciar la aplicación, y antes de dar acceso a la ventana de log-in, se debe
            //  realizar el proceso de verificación de integridad de la base de datos."
            // Va ANTES de cualquier escritura de arranque (siembra de admin2 / claves): en una
            // instalación nueva primero se inicializan los dígitos verificadores y recién después se
            // agregan filas, y con la base comprometida no se escribe nada (escribir recalcularía el
            // DVH de filas posiblemente alteradas). Con la integridad comprometida, el login se valida
            // contra el espejo de integridad (BLL.Usuario.Login), no contra la tabla Usuario.
            bool integridadOk = BLL.Configuracion.VerificarIntegridadDV(out BLL.ResultadoIntegridad _);
            if (!integridadOk)
            {
                try
                {
                    new BLL.Bitacora().RegistrarSinSesion(
                        modulo:     "Arranque",
                        actividad:  "Integridad DV inválida detectada antes del Login",
                        criticidad: BE.Criticidad.Alta,
                        detalle:    "La verificación de dígitos verificadores (DVH/DVV) previa al Login " +
                                    "detectó una posible manipulación externa de la base. Se requiere que un " +
                                    "Administrador inicie sesión para reparar o restaurar el sistema.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(
                        "[Program] No se pudo auditar la falla de integridad de arranque: " + ex.Message);
                }
            }
            else
            {
                // Primer arranque de una instalación nueva: admin de respaldo y claves de emergencia.
                // Se siembran UNA sola vez en la vida de la base (marca persistente), quedan en la
                // bitácora y el archivo con las credenciales solo lo puede leer el usuario actual.
                string rutaAdmin2 = BLL.Configuracion.SeedAdminSecundario();
                if (rutaAdmin2 != null)
                    MessageBox.Show(
                        "Se creó el usuario administrador de respaldo 'admin2'.\n" +
                        "Sus credenciales fueron guardadas en:\n\n" + rutaAdmin2 +
                        "\n\nGuardá ese archivo en un lugar seguro.",
                        "Administrador de respaldo creado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                string rutaClaves = BLL.Configuracion.SeedClavesEmergencia();
                if (rutaClaves != null)
                    MessageBox.Show(
                        "Se generaron 10 claves de emergencia de un solo uso.\n" +
                        "Sirven para desbloquear una cuenta de Administrador bloqueada.\n\n" +
                        "Se guardaron en:\n" + rutaClaves +
                        "\n\nGuardá ese archivo en un lugar seguro.",
                        "Claves de emergencia generadas",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
            }

            using (var frmLogin = new Login())
            {
                if (frmLogin.ShowDialog() != DialogResult.OK)
                    return;   // login cancelado → fin

                // T07 — La verificación ya se ejecutó ANTES del Login (arriba). Con el usuario
                // autenticado solo se ENRUTA la respuesta: el detalle y la reparación son solo-admin.
                if (!integridadOk)
                {
                    var usuario = BLL.Sesion.Usuario;

                    // Solo un Administrador ve el detalle de los dígitos rotos y puede repararlos.
                    // A cualquier otro usuario se le bloquea el ingreso con un mensaje GENÉRICO de
                    // mantenimiento: NO se le revela que la base fue manipulada (no darle esa pista a
                    // un posible atacante). El detalle real solo lo ve el Administrador.
                    if (usuario == null || !usuario.EsAdministrador)
                    {
                        var t = Servicios.Multiidioma.Traductor.ObtenerTraducciones(
                                    Servicios.Multiidioma.GestorIdioma.IdiomaActual);
                        string Tx(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;
                        MessageBox.Show(
                            Tx("msg.mantenimiento.cuerpo",
                               "El sistema no está disponible en este momento por tareas de mantenimiento. Reintentá más tarde o contactá al Administrador."),
                            Tx("msg.mantenimiento.titulo", "Sistema en mantenimiento"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        new BLL.Usuario().Logout("Arranque");
                        return;
                    }

                    // Administrador: abrir la Consola de Recuperación (Reparar desde Espejo /
                    // Asumir Pérdida / Restaurar Backup). Única vía de recuperación del sistema.
                    using (var rec = new RecuperacionEspejoForm())
                    {
                        Application.Run(rec);
                        if (!rec.RecuperadoExitosamente)
                            return;   // no reparó → no se entra con la base comprometida
                    }
                }

                Application.Run(new Menu());
            }
        }

        // Etapa 5 — Deja la BD como fuente de las traducciones desde el arranque: dispara el seed
        // (idempotente) la primera vez y carga el diccionario del idioma por defecto en el
        // GestorIdioma, de modo que el Traductor sirva textos de BD ya en la primera pantalla.
        private static void InicializarIdiomaDesdeBD()
        {
            try
            {
                var svc = new BLL.IdiomaService();
                var def = Servicios.Multiidioma.Traductor.ObtenerIdiomaDefault();
                if (def == null) return;

                var dict = svc.CargarTraducciones(def.Id);   // seedea (1ª vez) + carga desde BD
                if (dict != null && dict.Count > 0)
                    Servicios.Multiidioma.GestorIdioma.CambiarIdioma(def, dict);

                var activos = svc.ObtenerIdiomasActivosComoIdioma();
                if (activos != null && activos.Count > 0)
                    Servicios.Multiidioma.GestorIdioma.SetIdiomasDisponibles(activos);
            }
            catch (Exception ex)
            {
                // Sin BD/i18n disponible: el Traductor usa el fallback hardcodeado. No es crítico.
                System.Diagnostics.Trace.TraceError("[Program.InicializarIdiomaDesdeBD] " + ex.Message);
            }
        }

        // Registra cualquier excepción no controlada en la bitácora y avisa al usuario.
        // Nunca relanza: el logueo no debe tapar el error original ni provocar un segundo crash.
        private static void ManejarExcepcionGlobal(Exception ex)
        {
            if (ex == null) return;
            try
            {
                int? idUsuario = BLL.Sesion.IdUsuario;
                new BLL.Bitacora().RegistrarSinSesion(
                    modulo:     "Aplicación",
                    actividad:  "Excepción no controlada: " + ex.GetType().Name,
                    criticidad: BE.Criticidad.Alta,
                    idUsuario:  idUsuario,
                    // Solo tipo y mensaje: el ToString() completo puede arrastrar datos (valores de
                    // parámetros, rutas, contenido de excepciones internas) a la bitácora.
                    detalle:    ex.GetType().FullName + ": " + ex.Message);
            }
            catch { /* si falla el logueo, igual mostramos el error */ }

            // #6 — Ante un error NO controlado se muestra un mensaje GENÉRICO (no se expone
            // información técnica al usuario). El detalle técnico ya quedó en la bitácora.
            string titulo  = "Error inesperado";
            string mensaje = "Ha ocurrido un error inesperado. Por favor, contacte al administrador del sistema.";
            try
            {
                var t = Servicios.Multiidioma.Traductor.ObtenerTraducciones(
                            Servicios.Multiidioma.GestorIdioma.IdiomaActual);
                if (t.ContainsKey("msg.error.titulo"))     titulo  = t["msg.error.titulo"].Texto;
                if (t.ContainsKey("msg.error.inesperado"))  mensaje = t["msg.error.inesperado"].Texto;
            }
            catch { /* sin i18n disponible: se usa el texto por defecto */ }

            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
