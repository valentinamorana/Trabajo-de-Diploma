using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BLL
{
    /// <summary>
    /// USABILIDAD — Guía de primer uso por rol: qué hace el sistema para ese rol, por dónde
    /// empezar, qué muestra su Panel de Control, la barra inferior y F1.
    ///
    /// Dónde se guarda "No volver a mostrar": en un archivo local por usuario de Windows
    /// (%LOCALAPPDATA%\WardrobeFlow\guia_primer_uso.txt), con una línea por usuario de
    /// WardrobeFlow que pidió no verla más. Se eligió así, y no una columna nueva en la tabla
    /// Preferencia, porque es una preferencia de presentación de ESTE equipo (como la carpeta de
    /// versiones o los errores en XML, que ya viven en la misma carpeta): no cambia el esquema
    /// de la base, no toca dígitos verificadores ni el backup, y una instalación existente no
    /// necesita migración. En el archivo no queda el nombre del usuario sino su hash SHA-256
    /// (en minúsculas), así no expone quién usa el equipo.
    ///
    /// Nunca lanza: si el archivo no se puede leer, la guía se ofrece (valor por defecto); si no
    /// se puede escribir, se registra el aviso y la aplicación sigue.
    /// </summary>
    public class GuiaPrimerUso
    {
        /// <summary>Un paso de la guía: clave de traducción y texto en español (fallback).</summary>
        public sealed class Paso
        {
            public string Clave   { get; }
            public string TextoEs { get; }
            public Paso(string clave, string textoEs) { Clave = clave; TextoEs = textoEs; }
        }

        public const string NombreArchivo = "guia_primer_uso.txt";

        /// <summary>
        /// Carpeta de datos locales de WardrobeFlow (la misma de Versiones y Errores): la raíz que define
        /// en un solo lugar Servicios.Serializacion.GestorArchivos (%LOCALAPPDATA%\WardrobeFlow).
        /// </summary>
        public static string CarpetaPorDefecto => Servicios.Serializacion.GestorArchivos.CarpetaRaizPorDefecto;

        private readonly string _ruta;
        private static readonly object Candado = new object();

        public GuiaPrimerUso() : this(CarpetaPorDefecto) { }

        /// <summary>Permite indicar otra carpeta (tests).</summary>
        public GuiaPrimerUso(string carpeta)
        {
            _ruta = Path.Combine(string.IsNullOrWhiteSpace(carpeta) ? CarpetaPorDefecto : carpeta, NombreArchivo);
        }

        public string RutaArchivo => _ruta;

        /// <summary>True si la guía se tiene que mostrar al iniciar sesión (por defecto, sí).</summary>
        public bool DebeMostrarse(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            // Si el archivo no se puede leer, la guía se ofrece (valor por defecto).
            return !LeerMarcas(out HashSet<string> marcas) || !marcas.Contains(Huella(username));
        }

        /// <summary>El usuario marcó "No volver a mostrar".</summary>
        public void NoVolverAMostrar(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            Modificar(marcas => marcas.Add(Huella(username)));
        }

        /// <summary>El usuario desmarcó "No volver a mostrar": la guía vuelve a abrirse al ingresar.</summary>
        public void VolverAMostrar(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            Modificar(marcas => marcas.Remove(Huella(username)));
        }

        /// <summary>
        /// Leer → cambiar → escribir, con tres resguardos (y sin lanzar nunca):
        ///   • Candado entre procesos: un archivo .lock abierto en exclusiva (varias sesiones de
        ///     Windows del mismo usuario, o dos instancias de la app). Si no se obtiene tras unos
        ///     reintentos, no se escribe.
        ///   • Si el archivo existe pero no se pudo LEER, no se escribe: reescribirlo con lo que se
        ///     tiene en memoria borraría las marcas de los demás usuarios.
        ///   • Escritura atómica: se escribe un .tmp y se reemplaza el original (File.Replace), así un
        ///     corte a mitad de camino nunca deja el archivo a medias.
        /// </summary>
        private void Modificar(Func<HashSet<string>, bool> cambio)
        {
            lock (Candado)
            {
                FileStream candadoArchivo = null;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_ruta));
                    candadoArchivo = AbrirCandado();
                    if (candadoArchivo == null)
                    {
                        System.Diagnostics.Trace.TraceWarning("[GuiaPrimerUso] Archivo en uso por otro proceso; no se guardó la preferencia.");
                        return;
                    }
                    if (!LeerMarcas(out HashSet<string> marcas)) return;
                    if (cambio(marcas)) EscribirAtomico(marcas);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning("[GuiaPrimerUso] No se pudo guardar " + _ruta + ": " + ex.Message);
                }
                finally
                {
                    candadoArchivo?.Dispose();
                }
            }
        }

        private FileStream AbrirCandado()
        {
            string rutaCandado = _ruta + ".lock";
            for (int intento = 0; intento < 10; intento++)
            {
                try
                {
                    return new FileStream(rutaCandado, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                        1, FileOptions.None);
                }
                catch (IOException) { System.Threading.Thread.Sleep(50); }
                catch (UnauthorizedAccessException) { return null; }
            }
            return null;
        }

        /// <summary>
        /// Lee las marcas. Devuelve false si el archivo existe pero no se pudo leer (en uso,
        /// sin permisos, es una carpeta…); un archivo que no existe es una lista vacía válida.
        /// </summary>
        private bool LeerMarcas(out HashSet<string> marcas)
        {
            marcas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (Directory.Exists(_ruta)) return false;     // ocupa el nombre: ilegible
                if (!File.Exists(_ruta)) return true;
                using (var fs = new FileStream(_ruta, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    string linea;
                    while ((linea = sr.ReadLine()) != null)
                        if (!string.IsNullOrWhiteSpace(linea)) marcas.Add(linea.Trim());
                }
                return true;
            }
            catch (FileNotFoundException) { return true; }
            catch (DirectoryNotFoundException) { return true; }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[GuiaPrimerUso] No se pudo leer " + _ruta + ": " + ex.Message);
                marcas.Clear();
                return false;
            }
        }

        private void EscribirAtomico(HashSet<string> marcas)
        {
            string temporal = _ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllLines(temporal, marcas.OrderBy(m => m, StringComparer.Ordinal), Encoding.UTF8);
                if (File.Exists(_ruta)) File.Replace(temporal, _ruta, null);
                else File.Move(temporal, _ruta);
            }
            finally
            {
                try { if (File.Exists(temporal)) File.Delete(temporal); } catch { }
            }
        }

        /// <summary>SHA-256 del usuario normalizado (sin espacios a los costados, en minúsculas).</summary>
        internal static string Huella(string username)
        {
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(username.Trim().ToLowerInvariant()));
                return string.Concat(h.Select(b => b.ToString("x2")));
            }
        }

        // ── Cuándo NO se muestra nunca ───────────────────────────────────────────

        /// <summary>
        /// La guía no se muestra si la aplicación se ejecutó con /silent o /singuia (también con
        /// guion) o con la variable de entorno WARDROBEFLOW_SIN_GUIA=1: corridas automatizadas,
        /// demos guionadas. Puro, para poder testearlo.
        /// </summary>
        public static bool SuprimidaPorEntorno(IEnumerable<string> argumentos, string variableEntorno)
        {
            if (string.Equals((variableEntorno ?? "").Trim(), "1", StringComparison.Ordinal)) return true;
            return (argumentos ?? Enumerable.Empty<string>()).Any(a =>
            {
                string s = (a ?? "").Trim().TrimStart('/', '-').ToLowerInvariant();
                return s == "silent" || s == "singuia";
            });
        }

        // ── Pasos por rol ────────────────────────────────────────────────────────

        /// <summary>
        /// Clave del rol para las traducciones (ux.guia.&lt;rol&gt;.N): el perfil en minúsculas, sin
        /// espacios, paréntesis ni tildes (mismo criterio que perm.rol.&lt;rol&gt;). Un Administrador
        /// siempre usa la guía de administrador; un rol que no tiene guía propia (creado en
        /// Perfiles y Permisos), la genérica.
        /// </summary>
        public static string ClaveRol(string perfil, bool esAdministrador)
        {
            if (esAdministrador) return "administrador";
            string clave = SinTildes((perfil ?? "").ToLowerInvariant())
                .Replace(" ", "").Replace("(", "").Replace(")", "");
            return PasosPorRol.ContainsKey(clave) ? clave : "generico";
        }

        private static string SinTildes(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>Roles con guía propia (incluye "generico" para los roles creados a mano).</summary>
        public static IEnumerable<string> RolesConGuia => PasosPorRol.Keys;

        /// <summary>Pasos de la guía para el rol: los 3 del rol + los 2 comunes (barra inferior y F1).</summary>
        public static IList<Paso> PasosPara(string perfil, bool esAdministrador)
        {
            string rol = ClaveRol(perfil, esAdministrador);
            var pasos = new List<Paso>();
            string[] textos = PasosPorRol[rol];
            for (int i = 0; i < textos.Length; i++)
                pasos.Add(new Paso("ux.guia." + rol + "." + (i + 1), textos[i]));
            pasos.Add(new Paso("ux.guia.comun.barra", PasoBarraEs));
            pasos.Add(new Paso("ux.guia.comun.f1", PasoF1Es));
            return pasos;
        }

        public const string PasoBarraEs =
            "La barra inferior de la ventana le indica en todo momento dónde está (por ejemplo, Ventas → Pedidos de Venta) y qué puede hacer en esa pantalla. Si apoya el mouse sobre un botón, verá para qué sirve.";

        public const string PasoF1Es =
            "Presione F1 en cualquier pantalla para ver su ayuda. Puede volver a abrir esta guía cuando quiera desde Sesión → Ver guía de inicio.";

        // Qué hace el sistema para el rol / por dónde empezar / qué muestra su Panel de Control.
        private static readonly Dictionary<string, string[]> PasosPorRol = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["administrador"] = new[]
            {
                "Como Administrador, usted ve todo el menú: gestiona usuarios, roles y permisos, idiomas, copias de seguridad e integridad de los datos, y puede operar cualquier proceso.",
                "Empiece por Administrar → Usuarios para crear las cuentas del equipo y asignarles su rol. Administrar → Sistema reúne Backup y Diagnóstico de Integridad.",
                "El Panel de Control muestra las prendas disponibles, los clientes, los pedidos pendientes, los días sin backup y la actividad reciente del sistema.",
            },
            ["vendedor"] = new[]
            {
                "Como Vendedor, usted atiende a los clientes: los registra, les contrata un plan y arma sus pedidos de prendas.",
                "Empiece por Ventas → Pedidos de Venta para armar un pedido, o por Suscriptores → Nueva Contratación si el cliente todavía no tiene plan.",
                "Su Panel de Ventas muestra los pedidos para atender: los que están en control de stock, los que tienen faltantes y los separados listos para formalizar.",
            },
            ["caja"] = new[]
            {
                "Como Caja, usted registra los cobros: las contrataciones nuevas que cargó Ventas y las renovaciones de las suscripciones.",
                "Empiece por Caja → Cobro de Contrataciones. Las suscripciones vencidas o por vencer se cobran en Caja → Cobro de Renovaciones.",
                "El Panel de Control muestra cuántas contrataciones y renovaciones esperan su cobro, junto con sus tareas pendientes.",
            },
            ["deposito"] = new[]
            {
                "Como Depósito, usted controla el stock de cada pedido, separa las prendas e inspecciona las que devuelven los clientes.",
                "Empiece por Inventario → Control de Stock. Las prendas devueltas se revisan en Inventario → Inspección de Devolución.",
                "Su Panel de Depósito muestra los pedidos a controlar, las prendas en mantenimiento según su antigüedad y la ocupación del stock.",
            },
            ["operadorlogistico"] = new[]
            {
                "Como Operador Logístico, usted despacha los pedidos formalizados y registra su entrega y la devolución de las prendas.",
                "Empiece por Ventas → Pedidos Realizados: elija un pedido y avance su circuito.",
                "Su Panel de Logística muestra, como un tablero, los pedidos pendientes, despachados y entregados.",
            },
            ["gerentecomercial"] = new[]
            {
                "Como Gerente Comercial, usted analiza el negocio y decide las promociones para retener clientes, además de las tareas de Ventas.",
                "Empiece por Promociones → Centro de decisiones comerciales. Los reportes de abandono y de ventas por vendedor están en Analítica de Negocio.",
                "El Panel de Control muestra los indicadores generales y sus tareas pendientes; haga clic en una tarea para resolverla.",
            },
            ["gerenteinventario"] = new[]
            {
                "Como Gerente de Inventario, usted supervisa el stock, el depósito y la logística, y analiza la rotación, el mantenimiento y la escasez de prendas.",
                "Empiece por Analítica de Negocio para los reportes de inventario. La operación diaria está en Inventario y en Ventas → Pedidos Realizados.",
                "El Panel de Control muestra las prendas disponibles, la ocupación del stock, los pedidos pendientes y sus tareas.",
            },
            ["administracioncomercial"] = new[]
            {
                "Como Administración Comercial, usted crea las promociones a partir de las sugerencias de Gerencia y atiende las solicitudes de baja de Ventas.",
                "Empiece por Promociones → Gestión de Promociones.",
                "El Panel de Control muestra cuántas sugerencias y solicitudes de baja esperan su respuesta.",
            },
            ["contabilidad"] = new[]
            {
                "Como Contabilidad, usted revisa las promociones antes de que entren en vigencia: las aprueba o las rechaza con una observación.",
                "Empiece por Promociones → Revisión Contable.",
                "El Panel de Control muestra cuántas promociones esperan su revisión.",
            },
            ["auditor"] = new[]
            {
                "Como Auditor, usted consulta, sin modificar nada, lo que ocurrió en el sistema: eventos de seguridad, de negocio y el resumen de cada jornada.",
                "Empiece por Auditoría → Bitácora del Sistema o Bitácora de Negocio. El Reporte de Jornada resume un día completo.",
                "El Panel de Control muestra la actividad reciente y el resumen de eventos.",
            },
            ["generico"] = new[]
            {
                "WardrobeFlow gestiona el alquiler de prendas por suscripción. El menú muestra solo las opciones que su rol tiene permitidas.",
                "Empiece por las opciones habilitadas en el menú superior. Alertas reúne los avisos que requieren su atención.",
                "El Panel de Control muestra sus indicadores y sus tareas pendientes.",
            },
        };
    }

    /// <summary>
    /// Orden de lo que se muestra al ingresar: primero la guía de primer uso (modal) y DESPUÉS
    /// lo que se haya encolado (p. ej. un aviso no modal). Lo usa GUI.Menu.DespuesDeLaGuia:
    ///   • Mientras no se decidió si la guía se muestra, las acciones se encolan.
    ///   • Resolver() (la guía se cerró, o no se iba a mostrar) despacha las encoladas en orden.
    ///   • Una acción que llega después de Resolver() se despacha en el momento.
    /// El "despachador" lo pone quien la usa (el Menú la pasa a Menu.CuandoLaVentanaEsteLibre, que espera
    /// a que no haya un diálogo modal abierto: ver ColaHastaVentanaLibre); acá no hay UI, así que la
    /// secuencia se prueba sin formularios. Una acción que falla no corta a las siguientes.
    /// </summary>
    public sealed class SecuenciaDespuesDeLaGuia
    {
        private readonly List<Action> _pendientes = new List<Action>();
        private readonly object _candado = new object();
        private readonly Action<Action> _despachar;

        public SecuenciaDespuesDeLaGuia(Action<Action> despachar)
        {
            _despachar = despachar ?? (a => a());
        }

        /// <summary>True cuando la guía ya se cerró o se decidió no mostrarla.</summary>
        public bool Resuelta { get; private set; }

        public void Agregar(Action accion)
        {
            if (accion == null) return;
            lock (_candado)
            {
                if (!Resuelta) { _pendientes.Add(accion); return; }
            }
            Despachar(accion);
        }

        /// <summary>Marca la guía como resuelta y despacha lo encolado. Idempotente.</summary>
        public void Resolver()
        {
            List<Action> aDespachar;
            lock (_candado)
            {
                if (Resuelta) return;
                Resuelta = true;
                aDespachar = new List<Action>(_pendientes);
                _pendientes.Clear();
            }
            foreach (var accion in aDespachar) Despachar(accion);
        }

        private void Despachar(Action accion)
        {
            try { _despachar(() => Ejecutar(accion)); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[SecuenciaDespuesDeLaGuia] " + ex.Message); }
        }

        private static void Ejecutar(Action accion)
        {
            try { accion(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[SecuenciaDespuesDeLaGuia] Acción posterior a la guía: " + ex.Message); }
        }
    }

    /// <summary>
    /// Acciones que esperan a que la ventana principal quede libre (sin un diálogo modal abierto), en el
    /// orden en que llegaron. El Menú la usa para que lo encolado tras la guía (p. ej. el aviso comercial
    /// de PN03) no aparezca encima del aviso modal del backup del login: Procesar() se llama al encolar y
    /// después cada 400 ms hasta que no queda nada. La condición "libre" la pone quien la usa (el Menú
    /// mira IsWindowEnabled y sus formularios modales, igual que antes de mostrar la guía); acá no hay
    /// UI, así que se prueba sin formularios. Se usa desde un solo hilo (el de la interfaz) y nunca lanza:
    /// una acción que falla no corta a las siguientes, y si la condición falla se toma como libre (para
    /// no dejar un aviso esperando para siempre).
    /// </summary>
    public sealed class ColaHastaVentanaLibre
    {
        private readonly Queue<Action> _pendientes = new Queue<Action>();
        private readonly Func<bool> _ventanaLibre;

        public ColaHastaVentanaLibre(Func<bool> ventanaLibre)
        {
            _ventanaLibre = ventanaLibre ?? (() => true);
        }

        /// <summary>Cuántas acciones siguen esperando.</summary>
        public int Pendientes => _pendientes.Count;

        public void Agregar(Action accion)
        {
            if (accion != null) _pendientes.Enqueue(accion);
        }

        /// <summary>
        /// Ejecuta en orden las pendientes mientras la ventana siga libre; la condición se vuelve a mirar
        /// antes de cada una (si una abre un diálogo modal, las siguientes esperan a que se cierre).
        /// Devuelve true si no quedó ninguna pendiente.
        /// </summary>
        public bool Procesar()
        {
            while (_pendientes.Count > 0)
            {
                if (!EstaLibre()) return false;
                var accion = _pendientes.Dequeue();
                try { accion(); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("[ColaHastaVentanaLibre] " + ex.Message); }
            }
            return true;
        }

        private bool EstaLibre()
        {
            try { return _ventanaLibre(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("[ColaHastaVentanaLibre] No se pudo ver si hay un diálogo abierto: " + ex.Message);
                return true;
            }
        }
    }
}
