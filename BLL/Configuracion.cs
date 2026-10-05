using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BLL
{
    // Resultado estructurado de la verificación de integridad — permite formatear el mensaje en cualquier idioma.
    public class ResultadoIntegridad
    {
        public List<string> FilasCorruptas { get; set; } = new List<string>();
        public int? DvvAlmacenado  { get; set; }
        public int  DvvCalculado   { get; set; }
        public bool HayDvhInvalido { get; set; }
        public bool HayDvvInvalido { get; set; }
        public string ErrorTecnico { get; set; }

        // Fallback en español para la sobrecarga legacy (out string).
        public string MensajeES
        {
            get
            {
                if (ErrorTecnico != null) return $"Advertencia al verificar integridad DV:\n{ErrorTecnico}";
                var sb = new StringBuilder();
                sb.AppendLine("ALERTA DE INTEGRIDAD — Tabla Usuario");
                sb.AppendLine(new string('─', 50));
                sb.AppendLine();
                if (HayDvhInvalido)
                {
                    sb.AppendLine($"Se detectaron {FilasCorruptas.Count} fila(s) con DVH inválido:");
                    foreach (var f in FilasCorruptas) sb.AppendLine($"  • Usuario {f}");
                    sb.AppendLine();
                }
                if (HayDvvInvalido)
                {
                    sb.AppendLine("El DVV de la tabla no coincide con el valor almacenado.");
                    sb.AppendLine($"  Almacenado: {(DvvAlmacenado?.ToString() ?? "—")}  |  Calculado: {DvvCalculado}");
                    sb.AppendLine();
                }
                sb.AppendLine("Posibles causas: modificación directa en la base de datos,");
                sb.AppendLine("restauración parcial de backup o error en la migración.");
                sb.AppendLine();
                sb.AppendLine("Para restaurar la integridad, un Administrador debe:");
                sb.AppendLine("  1. Revisar los registros alterados en SQL Server.");
                sb.AppendLine("  2. Corregir los valores afectados manualmente.");
                sb.AppendLine("  3. Ejecutar el recálculo de DVH/DVV desde Administrar → Usuarios.");
                return sb.ToString();
            }
        }
    }

    // Resultado para diagnóstico granular (ObtenerDiagnostico).
    public class ResultadoDiagnostico
    {
        public bool   Integro          { get; set; }
        public int?   DVVAlmacenado    { get; set; }
        public int    DVVCalculado     { get; set; }
        public List<BE.FilaUsuarioDV> FilasRotas { get; set; } = new List<BE.FilaUsuarioDV>();
        // Tablas adicionales protegidas (Cliente, Empleado, Pedido) cuyo DV no coincide.
        // Antes el diagnóstico solo miraba Usuario, así que una corrupción en Cliente
        // nunca aparecía ni habilitaba "Recalcular Todo".
        public List<string> TablasAdicionalesCorruptas { get; set; } = new List<string>();
    }

    /// <summary>
    /// Capa de Lógica de Negocio — Configuración del Sistema.
    ///
    /// Responsabilidades de arranque (Program.Main):
    ///   1. VerificarConexionDAL()  — confirma que SQL Server responde antes del Login.
    ///   2. VerificarIntegridadDV() — T07: controla DVH/DVV de las tablas protegidas. Se ejecuta
    ///      ANTES de mostrar la ventana de Login (requisito de cátedra). Retorna false si detecta
    ///      manipulación externa; Program deja constancia en bitácora y, tras autenticarse,
    ///      reserva el detalle y la reparación al Administrador.
    /// </summary>
    public class Configuracion
    {
        /// <summary>
        /// Verifica la conexión a SQL Server usando DAL.Acceso.VerificarConexion().
        /// Retorna false y un mensaje de error si la conexión falla.
        /// Se invoca desde Program.Main() antes de mostrar cualquier formulario.
        /// </summary>
        public static bool VerificarConexionDAL(out string mensajeError)
        {
            mensajeError = null;
            try
            {
                bool ok = DAL.Acceso.GetInstance().VerificarConexion();

                if (!ok)
                {
                    mensajeError = "No se pudo conectar a la base de datos en el servidor '" + ServidorConfigurado() + "'.\n" +
                                   "Verifique que SQL Server esté en ejecución.\n\n" + AyudaCadenaConexion();
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                mensajeError = $"Error al inicializar la conexión:\n{ex.Message}\n\n" + AyudaCadenaConexion();
                return false;
            }
        }

        // Servidor (Data Source) de la cadena de conexión configurada, para mostrarlo en el error.
        private static string ServidorConfigurado()
        {
            try
            {
                var entry = System.Configuration.ConfigurationManager.ConnectionStrings["WardrobeFlowDB"];
                return entry == null ? "?" : new System.Data.SqlClient.SqlConnectionStringBuilder(entry.ConnectionString).DataSource;
            }
            catch { return "?"; }
        }

        // En una instalación la cadena no está en "App.config" sino en GUI.exe.config, al lado del
        // ejecutable (el instalador la escribe con el servidor elegido): se indica la ruta real.
        private static string AyudaCadenaConexion() =>
            "La cadena de conexión 'WardrobeFlowDB' está en:\n" +
            AppDomain.CurrentDomain.SetupInformation.ConfigurationFile +
            "\n(o reinstale WardrobeFlow y elija otra instancia de SQL Server).";

        // ── T07 — Dígitos verificadores ────────────────────────────────────────────
        //
        // REGLAS DE VERIFICACIÓN (formato 2):
        //   • El script de instalación deja en ParametroSistema la versión del formato ('FormatoDV')
        //     y, cuando instala o actualiza el formato, la marca 'DVInicializacionPendiente' = 1 con
        //     las tablas protegidas "sin calcular" (sin fila en DVVertical y DVH = 0).
        //   • Solo en ese caso la app INICIALIZA los dígitos de una tabla (y lo deja asentado en la
        //     bitácora y en el historial de integridad). Al terminar, baja la marca.
        //   • Cualquier otra anomalía —DVV ausente sin marca, DVH en cero, formato distinto, error al
        //     leer— es "NO íntegro" y va a la consola de recuperación. Ya no se recalcula solo nada
        //     por heurística (antes, poner todos los DVH en 0, o todos < 10, o borrar el marcador de
        //     formato, hacía que la app "sellara" en silencio una base manipulada).

        /// <summary>
        /// true si la última verificación de integridad (la de arranque) detectó una base no íntegra.
        /// Con la integridad comprometida, el login se valida contra el espejo de integridad
        /// (BLL.Usuario.Login), no contra la tabla Usuario, que pudo haber sido alterada.
        /// </summary>
        public static bool IntegridadComprometida { get; internal set; }

        // Descriptor de una tabla protegida además de Usuario.
        private sealed class TablaProtegida
        {
            public string Nombre;
            public Func<List<BE.FilaDV>> ObtenerFilas;
            public Action RecalcularTodo;
        }

        // Tablas protegidas además de Usuario (fuente única para verificar, diagnosticar y recalcular).
        private static List<TablaProtegida> TablasAdicionales()
        {
            var dv = new DAL.DigitoVerificador();
            var pedido = new DAL.Pedido();
            var permiso = new DAL.Permiso();
            return new List<TablaProtegida>
            {
                new TablaProtegida { Nombre = DAL.Cliente.DV_Tabla,
                    ObtenerFilas   = () => dv.ObtenerFilas(DAL.Cliente.DV_Tabla, DAL.Cliente.DV_Pk, DAL.Cliente.DV_Columnas),
                    RecalcularTodo = () => dv.RecalcularTabla(DAL.Cliente.DV_Tabla, DAL.Cliente.DV_Pk, DAL.Cliente.DV_Columnas) },
                new TablaProtegida { Nombre = DAL.Empleado.DV_Tabla,
                    ObtenerFilas   = () => dv.ObtenerFilas(DAL.Empleado.DV_Tabla, DAL.Empleado.DV_Pk, DAL.Empleado.DV_Columnas),
                    RecalcularTodo = () => dv.RecalcularTabla(DAL.Empleado.DV_Tabla, DAL.Empleado.DV_Pk, DAL.Empleado.DV_Columnas) },
                new TablaProtegida { Nombre = DAL.Contratacion.DV_Tabla,
                    ObtenerFilas   = () => dv.ObtenerFilas(DAL.Contratacion.DV_Tabla, DAL.Contratacion.DV_Pk, DAL.Contratacion.DV_Columnas),
                    RecalcularTodo = () => dv.RecalcularTabla(DAL.Contratacion.DV_Tabla, DAL.Contratacion.DV_Pk, DAL.Contratacion.DV_Columnas) },
                // Pedido: objeto MULTI-TABLA (pedido + líneas PedidoPrenda).
                new TablaProtegida { Nombre = DAL.Pedido.DV_Tabla,
                    ObtenerFilas   = () => pedido.ObtenerFilasDV(),
                    RecalcularTodo = () => pedido.RecalcularDV() },
                // PermisoRelacion: árbol de roles y patentes (clave compuesta).
                new TablaProtegida { Nombre = DAL.Permiso.DV_TablaRelacion,
                    ObtenerFilas   = () => permiso.ObtenerFilasDVRelacion(),
                    RecalcularTodo = () => permiso.RecalcularDVRelaciones() },
            };
        }

        /// <summary>Nombres de todas las tablas protegidas con dígitos verificadores.</summary>
        public static List<string> NombresTablasProtegidas()
        {
            var l = new List<string> { "Usuario" };
            foreach (var t in TablasAdicionales()) l.Add(t.Nombre);
            return l;
        }

        // Pura (testeable): resultado de comparar los DVH almacenados con los recalculados y el DVV.
        internal static (List<int> Rotas, int DvvCalculado, bool DvvOk) Comparar(
            IList<string[]> campos, IList<int?> dvhAlmacenados, int? dvvAlmacenado, Seguridad.ICalculadorDV svc)
        {
            var rotas = new List<int>();
            var dvhs  = new List<int>(campos.Count);
            for (int i = 0; i < campos.Count; i++)
            {
                int calc = svc.CalcularDVH(campos[i]);
                dvhs.Add(calc);
                if (dvhAlmacenado(dvhAlmacenados, i) != calc) rotas.Add(i);
            }
            int dvv = svc.CalcularDVV(dvhs);
            return (rotas, dvv, dvvAlmacenado.HasValue && dvvAlmacenado.Value == dvv);
        }

        private static int? dvhAlmacenado(IList<int?> l, int i) => l[i];

        // Pura (testeable): ¿corresponde inicializar la tabla? Solo si el instalador dejó la marca
        // de inicialización pendiente, la tabla no tiene DVV y ninguna fila tiene DVH calculado.
        internal static bool CorrespondeInicializar(bool pendiente, int? dvvAlmacenado, IEnumerable<int?> dvhs)
        {
            if (!pendiente || dvvAlmacenado.HasValue) return false;
            foreach (var d in dvhs) if (d.HasValue && d.Value != 0) return false;
            return true;
        }

        /// <summary>
        /// T07 — Verifica la integridad de TODAS las tablas protegidas (Usuario, Cliente, Empleado,
        /// Contratacion, Pedido, PermisoRelacion) mediante DVH y DVV. Inicializa solo las tablas que
        /// el instalador dejó pendientes. Ante cualquier error, la base se considera NO íntegra.
        /// </summary>
        public static bool VerificarIntegridadDV(out ResultadoIntegridad resultado)
        {
            resultado = null;
            try
            {
                var dvDAL = new DAL.DigitoVerificador();
                var svc   = Seguridad.CalculadorDV.Crear();

                string formato = dvDAL.ObtenerParametro(DAL.DigitoVerificador.ClaveFormato);
                if (formato != DAL.DigitoVerificador.FormatoActual.ToString())
                {
                    resultado = new ResultadoIntegridad
                    {
                        HayDvhInvalido = true,
                        FilasCorruptas = new List<string>
                        {
                            $"Formato de dígitos verificadores de la base: '{formato ?? "(sin marca)"}'; " +
                            $"esta versión requiere el formato {DAL.DigitoVerificador.FormatoActual}. " +
                            "Ejecutá el instalador (script de la base) antes de usar el sistema."
                        }
                    };
                    LogearVerificacion("Formato DV", null, 0, false, 1, "Arranque");
                    IntegridadComprometida = true;
                    return false;
                }

                bool pendiente = dvDAL.ObtenerParametro(DAL.DigitoVerificador.ClavePendiente) == "1";
                var corruptas = new List<string>();
                var inicializadas = new List<string>();

                // ── Usuario ──
                var filas = dvDAL.ObtenerFilasUsuario();
                int? dvvUsuario = dvDAL.ObtenerDVV("Usuario");
                int dvvUsuarioCalc = 0;
                bool dvhUsuarioOk = true, dvvUsuarioOk = true;
                if (CorrespondeInicializar(pendiente, dvvUsuario, filas.ConvertAll(f => f.DVHAlmacenado)))
                {
                    RecalcularTodoDV(dvDAL, svc, filas);
                    inicializadas.Add("Usuario");
                    LogearVerificacion("Usuario", dvDAL.ObtenerDVV("Usuario"), dvDAL.ObtenerDVV("Usuario") ?? 0, true, 0, "Inicialización");
                }
                else
                {
                    var cmp = Comparar(filas.ConvertAll(f => f.CamposParaDVH()), filas.ConvertAll(f => f.DVHAlmacenado), dvvUsuario, svc);
                    foreach (int i in cmp.Rotas) corruptas.Add($"Usuario '{filas[i].Username}' (ID {filas[i].Id})");
                    dvvUsuarioCalc = cmp.DvvCalculado;
                    dvhUsuarioOk   = cmp.Rotas.Count == 0;
                    dvvUsuarioOk   = cmp.DvvOk;
                    if (!dvvUsuarioOk) corruptas.Add(dvvUsuario.HasValue ? "Usuario (DVV)" : "Usuario (DVV ausente)");
                    LogearVerificacion("Usuario", dvvUsuario, dvvUsuarioCalc, dvhUsuarioOk && dvvUsuarioOk,
                                       cmp.Rotas.Count, "Arranque");
                    // Base sana pero sin espejo todavía → sembrarlo desde estas filas íntegras.
                    if (dvhUsuarioOk && dvvUsuarioOk) SeedEspejoSiVacio(filas);
                }

                // ── Tablas adicionales ──
                foreach (var t in TablasAdicionales())
                    VerificarUnaTabla(dvDAL, svc, t, pendiente, corruptas, inicializadas);

                if (inicializadas.Count > 0)
                    RegistrarInicializacion(inicializadas);

                if (pendiente && corruptas.Count == 0)
                    dvDAL.GuardarParametro(DAL.DigitoVerificador.ClavePendiente, "0");

                if (corruptas.Count == 0)
                {
                    IntegridadComprometida = false;
                    return true;
                }

                resultado = new ResultadoIntegridad
                {
                    FilasCorruptas = corruptas,
                    DvvAlmacenado  = dvvUsuario,
                    DvvCalculado   = dvvUsuarioCalc,
                    HayDvhInvalido = true,
                    HayDvvInvalido = !dvvUsuarioOk
                };
                IntegridadComprometida = true;
                return false;
            }
            catch (Exception ex)
            {
                // FAIL-SAFE: ante CUALQUIER error (incluida una columna DVH o la tabla DVVertical
                // ausente) NO se asume integridad: se bloquea el acceso y se informa al administrador.
                System.Diagnostics.Trace.TraceError($"[Configuracion.VerificarIntegridadDV] {ex.Message}");
                resultado = new ResultadoIntegridad
                {
                    HayDvhInvalido = true,
                    FilasCorruptas = new List<string> { "Error técnico al verificar la integridad: " + ex.Message },
                    DvvAlmacenado  = null,
                    DvvCalculado   = 0,
                    ErrorTecnico   = ex.Message
                };
                IntegridadComprometida = true;
                return false;
            }
        }

        private static void VerificarUnaTabla(DAL.DigitoVerificador dvDAL, Seguridad.ICalculadorDV svc,
            TablaProtegida t, bool pendiente, List<string> corruptas, List<string> inicializadas)
        {
            var filas = t.ObtenerFilas();   // un error de lectura se propaga: NO íntegro
            int? dvvAlm = dvDAL.ObtenerDVV(t.Nombre);

            if (CorrespondeInicializar(pendiente, dvvAlm, filas.ConvertAll(f => f.DVHAlmacenado)))
            {
                t.RecalcularTodo();
                inicializadas.Add(t.Nombre);
                int? dvvNuevo = dvDAL.ObtenerDVV(t.Nombre);
                LogearVerificacion(t.Nombre, dvvNuevo, dvvNuevo ?? 0, true, 0, "Inicialización");
                return;
            }

            var cmp = Comparar(filas.ConvertAll(f => f.Campos), filas.ConvertAll(f => f.DVHAlmacenado), dvvAlm, svc);
            foreach (int i in cmp.Rotas) corruptas.Add(filas[i].Descripcion + " (DVH)");
            if (!cmp.DvvOk) corruptas.Add(t.Nombre + (dvvAlm.HasValue ? " (DVV)" : " (DVV ausente)"));
            int rotas = cmp.Rotas.Count + (cmp.DvvOk ? 0 : 1);
            LogearVerificacion(t.Nombre, dvvAlm, cmp.DvvCalculado, rotas == 0, rotas, "Arranque");
        }

        // Deja constancia (bitácora, criticidad Alta) de que se inicializaron dígitos verificadores
        // tras una instalación o actualización del formato: si alguien forzara una
        // "inicialización" para sellar datos manipulados, queda el rastro.
        private static void RegistrarInicializacion(List<string> tablas)
        {
            try
            {
                FabricaBitacora().RegistrarSinSesion(
                    modulo:     "Integridad de Datos",
                    actividad:  "Inicialización de Dígitos Verificadores",
                    criticidad: BE.Criticidad.Alta,
                    detalle:    "El instalador dejó pendiente la inicialización de los dígitos verificadores " +
                                $"(formato {DAL.DigitoVerificador.FormatoActual}). Se calcularon para: " +
                                string.Join(", ", tablas) + $" a las {DateTime.Now:HH:mm:ss}.");
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[Configuracion] " + ex.Message); }
        }

        private static Servicios.IRegistroBitacora FabricaBitacora() => Servicios.FabricaBitacora.CrearSistema();

        /// <summary>
        /// T07 — Asegura la integridad de las tablas protegidas ANTES de una operación sensible
        /// (alta/reset/desbloqueo de usuarios). Lanza AppException si la base fue manipulada,
        /// de modo que la operación no se ejecute sobre datos corruptos.
        /// </summary>
        public static void AsegurarIntegridadUsuarios()
        {
            if (!VerificarIntegridadDV(out ResultadoIntegridad _))
                throw new BE.AppException("err.bll.integridad",
                    "Operación cancelada: se detectó una posible manipulación de los datos de usuarios " +
                    "(dígito verificador inválido). Reiniciá el sistema para reparar la integridad antes de continuar.");
        }

        // ── Siembra inicial (primer arranque) ───────────────────────────────────────
        // Marcas persistentes (ParametroSistema): se siembra UNA sola vez en la vida de la base.
        // Antes se sembraba cada vez que faltaba admin2 o no había claves, así que quien borrara
        // admin2 o las claves y abriera la app recibía credenciales nuevas en texto plano.
        internal const string MarcaAdmin2   = "SeedAdmin2";
        internal const string MarcaClaves   = "SeedClavesEmergencia";

        // Garantiza que exista un segundo Administrador ("admin2") en una instalación nueva, para
        // que si admin1 queda bloqueado haya otro admin que pueda desbloquearlo. Solo corre si la
        // marca de siembra no existe (nunca se sembró) y admin2 no existe; deja la marca y lo
        // registra en la bitácora. El archivo de credenciales queda accesible solo para el usuario
        // de Windows actual. Retorna la ruta del archivo, o null si no sembró nada.
        public static string SeedAdminSecundario()
        {
            const string Username = "admin2";
            try
            {
                var dv = new DAL.DigitoVerificador();
                if (dv.ObtenerParametro(MarcaAdmin2) != null) return null;

                var usuarioDAL = new DAL.Usuario();
                if (usuarioDAL.ObtenerPorUsername(Username) != null)
                {
                    // Base actualizada que ya tenía admin2: solo se deja la marca.
                    dv.GuardarParametro(MarcaAdmin2, "existente");
                    return null;
                }

                string contrasena    = Servicios.GeneradorCredenciales.GenerarContrasena();
                string claveHasheada = Seguridad.Encriptador.Hash(contrasena);
                usuarioDAL.Alta(Username, claveHasheada, BE.Roles.Administrador);
                dv.GuardarParametro(MarcaAdmin2, DateTime.Now.ToString("s"));

                string ruta = Servicios.GeneradorCredenciales.ExportarCredenciales(Username, contrasena);
                FabricaBitacora().RegistrarSinSesion(
                    modulo:     "Arranque",
                    actividad:  "Alta del administrador de respaldo (admin2)",
                    criticidad: BE.Criticidad.Alta,
                    detalle:    $"Primer arranque: se creó 'admin2' y sus credenciales se exportaron a '{ruta}' " +
                                $"(acceso restringido al usuario de Windows {Environment.UserName}) a las {DateTime.Now:HH:mm:ss}.");
                return ruta;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Configuracion.SeedAdminSecundario] {ex.Message}");
                return null;
            }
        }

        // RF-10 — Genera el set inicial de claves de emergencia (10) una sola vez en la vida de la
        // base (marca persistente), lo registra en la bitácora y exporta el .txt con acceso solo
        // para el usuario actual. Devuelve la ruta del .txt, o null si no generó nada.
        public static string SeedClavesEmergencia()
        {
            try
            {
                var dv = new DAL.DigitoVerificador();
                if (dv.ObtenerParametro(MarcaClaves) != null) return null;

                if (new DAL.ClaveRecuperacion().ContarTotal() > 0)
                {
                    dv.GuardarParametro(MarcaClaves, "existente");
                    return null;
                }

                string ruta = RecuperacionAdmin.GenerarClavesEmergencia(10);
                dv.GuardarParametro(MarcaClaves, DateTime.Now.ToString("s"));
                FabricaBitacora().RegistrarSinSesion(
                    modulo:     "Arranque",
                    actividad:  "Generación del set inicial de claves de emergencia",
                    criticidad: BE.Criticidad.Alta,
                    detalle:    $"Primer arranque: se generaron 10 claves de emergencia exportadas a '{ruta}' " +
                                $"(acceso restringido al usuario de Windows {Environment.UserName}) a las {DateTime.Now:HH:mm:ss}.");
                return ruta;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Configuracion.SeedClavesEmergencia] {ex.Message}");
                return null;
            }
        }

        // ── Métodos de diagnóstico y reparación granular ──────────────────────

        // Diagnóstico SOLO LECTURA (sin inicializar ni escribir) de todas las tablas protegidas.
        public static ResultadoDiagnostico ObtenerDiagnostico()
        {
            var dvDAL = new DAL.DigitoVerificador();
            var svc   = Seguridad.CalculadorDV.Crear();
            var filas = dvDAL.ObtenerFilasUsuario();
            bool pendiente = dvDAL.ObtenerParametro(DAL.DigitoVerificador.ClavePendiente) == "1";
            bool formatoOk = dvDAL.ObtenerParametro(DAL.DigitoVerificador.ClaveFormato)
                             == DAL.DigitoVerificador.FormatoActual.ToString();

            int? dvvAlmacenado = dvDAL.ObtenerDVV("Usuario");
            var cmp = Comparar(filas.ConvertAll(f => f.CamposParaDVH()), filas.ConvertAll(f => f.DVHAlmacenado), dvvAlmacenado, svc);
            var rotas = cmp.Rotas.ConvertAll(i => filas[i]);

            var adicionales = new List<string>();
            foreach (var t in TablasAdicionales())
            {
                List<BE.FilaDV> ft;
                try { ft = t.ObtenerFilas(); }
                catch { adicionales.Add(t.Nombre); continue; }   // no se pudo leer → no íntegra
                int? dvv = dvDAL.ObtenerDVV(t.Nombre);
                if (CorrespondeInicializar(pendiente, dvv, ft.ConvertAll(f => f.DVHAlmacenado))) continue;
                var c = Comparar(ft.ConvertAll(f => f.Campos), ft.ConvertAll(f => f.DVHAlmacenado), dvv, svc);
                if (c.Rotas.Count > 0 || !c.DvvOk) adicionales.Add(t.Nombre);
            }

            bool usuarioPendiente = CorrespondeInicializar(pendiente, dvvAlmacenado, filas.ConvertAll(f => f.DVHAlmacenado));
            bool usuarioOk = usuarioPendiente || (rotas.Count == 0 && cmp.DvvOk);
            if (!formatoOk) adicionales.Add("Formato DV");
            return new ResultadoDiagnostico
            {
                Integro       = usuarioOk && adicionales.Count == 0,
                DVVAlmacenado = dvvAlmacenado,
                DVVCalculado  = cmp.DvvCalculado,
                FilasRotas    = usuarioPendiente ? new List<BE.FilaUsuarioDV>() : rotas,
                TablasAdicionalesCorruptas = adicionales
            };
        }

        // #5 — Guard fail-closed para operaciones sobre Dígitos Verificadores: exige una sesión
        // de Administrador. Un usuario autenticado sin permiso queda BLOQUEADO y el intento se
        // REGISTRA en bitácora (criticidad Alta). Antes, sin sesión se permitía todo ("break-glass
        // de arranque"); hoy la consola de recuperación solo se abre después de un login de
        // Administrador validado contra el espejo de integridad, así que no hace falta esa excepción.
        private static void ExigirAdminParaDV(string operacion)
        {
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada",
                    "La sesión expiró. Volvé a iniciar sesión.");

            var u = Seguridad.SessionManager.GetInstance().Usuario;
            if (u != null && u.EsAdministrador) return;

            // Acceso no autorizado: registrar el evento para que el administrador lo vea.
            try
            {
                FabricaBitacora().RegistrarSinSesion(
                    modulo:     "Integridad de Datos",
                    actividad:  "Acceso DENEGADO a Dígitos Verificadores",
                    criticidad: BE.Criticidad.Alta,
                    idUsuario:  u?.Id,
                    detalle:    $"El usuario '{u?.Username ?? "?"}' (rol '{u?.Perfil ?? "?"}') intentó ejecutar " +
                                $"'{operacion}' sin permiso de Administrador a las {DateTime.Now:HH:mm:ss}.");
            }
            catch { /* el fallo del log no debe ocultar el bloqueo */ }

            throw new BE.AppException("err.bll.dv.sin_permiso",
                "No tenés permiso para ejecutar operaciones sobre los Dígitos Verificadores. " +
                "Esta acción es exclusiva del Administrador y quedó registrada.");
        }

        // "Recalcular todo": acepta los datos ACTUALES de todas las tablas protegidas como
        // legítimos, recalcula sus DVH/DVV, sella el formato vigente y baja la marca de
        // inicialización pendiente. Exclusivo de un Administrador con sesión (recuperación).
        public static void RecalcularIntegridadDV()
        {
            ExigirAdminParaDV("Recalcular DV");
            var dvDAL = new DAL.DigitoVerificador();
            var svc   = Seguridad.CalculadorDV.Crear();
            RecalcularTodoDV(dvDAL, svc, dvDAL.ObtenerFilasUsuario());

            var tablas = new List<string> { "Usuario" };
            foreach (var t in TablasAdicionales())
            {
                t.RecalcularTodo();
                tablas.Add(t.Nombre);
            }
            dvDAL.GuardarParametro(DAL.DigitoVerificador.ClaveFormato, DAL.DigitoVerificador.FormatoActual.ToString());
            dvDAL.GuardarParametro(DAL.DigitoVerificador.ClavePendiente, "0");
            IntegridadComprometida = false;

            var u = Seguridad.SessionManager.GetInstance().Usuario;
            FabricaBitacora().RegistrarSinSesion(
                modulo:     "Integridad de Datos",
                actividad:  "Recálculo de Dígitos Verificadores",
                criticidad: BE.Criticidad.Alta,
                idUsuario:  u.Id,
                detalle:    $"{u.Username} ejecutó el recálculo de DVH/DVV ({string.Join(", ", tablas)}) " +
                            $"a las {DateTime.Now:HH:mm:ss}.");
        }

        // Helper compartido entre la inicialización y RecalcularIntegridadDV: recalcula TODOS los
        // DVH de Usuario y su DVV (con el bloqueo del DV tomado) y reconstruye el espejo.
        private static void RecalcularTodoDV(DAL.DigitoVerificador dvDAL,
                                              Seguridad.ICalculadorDV svc,
                                              List<BE.FilaUsuarioDV> filas)
        {
            filas.Clear();
            filas.AddRange(dvDAL.RecalcularTablaUsuario());
            // T07 — Reconstruir el espejo de integridad para que refleje el estado recién
            // aceptado como legítimo (inicialización o "Asumir pérdida"/"Recalcular Todo").
            new DAL.EspejoUsuario().Reconstruir(filas);
        }

        // T07 — Recalcula DVH/DVV SOLO de la tabla Usuario y reconstruye su espejo de integridad.
        // Lo usa la recuperación asistida tras restaurar valores desde el espejo. Exige Administrador.
        public static void RecalcularUsuario()
        {
            ExigirAdminParaDV("Recalcular Usuario (DV)");
            var dvDAL = new DAL.DigitoVerificador();
            RecalcularTodoDV(dvDAL, Seguridad.CalculadorDV.Crear(), dvDAL.ObtenerFilasUsuario());
        }

        // Expone el guard de autorización de DV para la recuperación asistida (mismo fail-closed).
        public static void ExigirAdminDV(string operacion) => ExigirAdminParaDV(operacion);

        // T07 — Siembra el espejo de integridad SOLO si está vacío, a partir de filas ya verificadas
        // como íntegras. Nunca siembra desde datos corruptos (se llama solo en el camino OK).
        private static void SeedEspejoSiVacio(List<BE.FilaUsuarioDV> filas)
        {
            try
            {
                var esp = new DAL.EspejoUsuario();
                if (filas != null && filas.Count > 0 && esp.ObtenerFilas().Count == 0)
                    esp.Reconstruir(filas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Configuracion.SeedEspejoSiVacio] {ex.Message}");
            }
        }

        /// <summary>
        /// Detalle de las tablas protegidas ADICIONALES a Usuario cuyo dígito verificador no cierra
        /// (solo lectura). Lista vacía si están íntegras. Lo usa la consola de recuperación.
        /// </summary>
        public static List<string> ObtenerTablasAdicionalesCorruptas()
        {
            return ObtenerDiagnostico().TablasAdicionalesCorruptas;
        }

        // Devuelve los últimos N registros del historial de verificaciones DV.
        // Encapsula el acceso a DAL para que la GUI no dependa de DAL.HistorialIntegridad.
        public static List<BE.HistorialIntegridad> ObtenerHistorialIntegridad(int n)
        {
            return new DAL.HistorialIntegridad().ObtenerUltimos(n);
        }

        // Registra una verificación periódica (Timer del Menu) en el historial.
        // Centraliza el acceso a DAL para que Menu.cs no dependa de DAL directamente.
        public static void RegistrarVerificacionPeriodica(ResultadoDiagnostico diag)
        {
            // Solo con una sesión iniciada (lo dispara el Timer del menú).
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada", "La sesión expiró. Volvé a iniciar sesión.");
            try
            {
                new DAL.HistorialIntegridad().Insertar(new BE.HistorialIntegridad
                {
                    NombreTabla    = "Usuario",
                    DVVAlmacenado  = diag.DVVAlmacenado,
                    DVVCalculado   = diag.DVVCalculado,
                    Resultado      = diag.Integro,
                    FilasCorruptas = diag.FilasRotas.Count + diag.TablasAdicionalesCorruptas.Count,
                    DisparadoPor   = "Timer"
                });
            }
            catch { /* tabla aún no existe */ }
        }

        // ── Recordatorio de backup ────────────────────────────────────────────

        private static readonly string RutaConfigRecordatorio =
            Path.Combine(Backup.CarpetaBackups, "recordatorio.cfg");

        private const int DiasRecordatorioDefault = 7;

        public static int ObtenerDiasRecordatorio()
        {
            try
            {
                if (File.Exists(RutaConfigRecordatorio) &&
                    int.TryParse(File.ReadAllText(RutaConfigRecordatorio).Trim(), out int d) && d > 0)
                    return d;
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[Configuracion.ObtenerDiasRecordatorio] " + ex.Message); }
            return DiasRecordatorioDefault;
        }

        public static void GuardarDiasRecordatorio(int dias)
        {
            BLLHelper.ExigirAdministrador("err.bll.backup.sin_permiso",
                "Solo un Administrador puede configurar el recordatorio de backup.");
            try
            {
                string dir = Path.GetDirectoryName(RutaConfigRecordatorio);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(RutaConfigRecordatorio, dias.ToString());
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[Configuracion.GuardarDiasRecordatorio] " + ex.Message); }
        }

        // Registra silenciosamente cada verificación en HistorialIntegridad.
        // Falla silenciosamente si la tabla aún no existe (antes de la migración).
        private static void LogearVerificacion(string tabla, int? dvvAlm, int dvvCalc, bool resultado, int filasRotas, string origen)
        {
            try
            {
                new DAL.HistorialIntegridad().Insertar(new BE.HistorialIntegridad
                {
                    NombreTabla    = tabla,
                    DVVAlmacenado  = dvvAlm,
                    DVVCalculado   = dvvCalc,
                    Resultado      = resultado,
                    FilasCorruptas = filasRotas,
                    DisparadoPor   = origen
                });
            }
            catch { /* tabla aún no existe — ignorar */ }
        }
    }
}
