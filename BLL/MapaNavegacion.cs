using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// USABILIDAD — "¿Dónde estoy?": catálogo puro de las pantallas que se abren desde el menú
    /// principal. Por cada pantalla (nombre de la clase del formulario) guarda el .Name del
    /// ToolStripMenuItem que la abre y la pista de una línea ("qué puede hacer acá") que muestra
    /// la barra de estado de la ventana principal.
    ///
    /// El RECORRIDO del menú (Promociones → Centro de decisiones comerciales) NO se escribe acá:
    /// GUI.Menu busca el ítem por su .Name y sube por sus OwnerItem leyendo el texto traducido
    /// de cada nivel, así que si el Designer mueve un ítem a otro submenú la ruta se actualiza
    /// sola. Mismo criterio de separación que BLL.MenuVisibilidad: la DECISIÓN (qué pantalla
    /// cuelga de qué ítem y qué pista lleva) vive acá, testeable sin levantar la UI
    /// (Tests/UsabilidadTests.cs, que además coteja este catálogo con los manejadores reales de
    /// GUI/Menu.cs); la GUI solo la presenta.
    /// </summary>
    public static class MapaNavegacion
    {
        /// <summary>Clave de la "pantalla" que se muestra cuando no hay ninguna ventana abierta.</summary>
        public const string Inicio = "Inicio";

        /// <summary>Pantalla del menú: formulario, ítem que la abre y pista en español (fallback del .tsv).</summary>
        public sealed class Pantalla
        {
            public string Formulario { get; }
            public string ItemMenu   { get; }
            public string PistaEs    { get; }
            /// <summary>Se abre como diálogo modal (ShowDialog) en vez de como ventana hija MDI.</summary>
            public bool   EsModal    { get; }

            public Pantalla(string formulario, string itemMenu, string pistaEs, bool esModal = false)
            {
                Formulario = formulario; ItemMenu = itemMenu; PistaEs = pistaEs; EsModal = esModal;
            }
        }

        /// <summary>Pista de la ventana principal sin pantallas abiertas.</summary>
        public const string PistaInicioEs =
            "Elija una opción del menú superior. Para ver sus pendientes del día, abra Panel de Control.";

        /// <summary>
        /// Todas las pantallas alcanzables desde el menú principal. Un formulario puede colgar de
        /// más de un ítem (Bitácora: del Sistema y de Negocio abren la misma pantalla con pestañas).
        /// </summary>
        public static readonly Pantalla[] Pantallas =
        {
            // Panel de Control: un tablero distinto por rol (GUI.Menu.CrearDashboardDelRol).
            new Pantalla("DashboardForm",      "panelControlToolStripMenuItem",
                "Revise sus indicadores y tareas pendientes; haga clic en una tarea para abrir la pantalla que la resuelve."),
            new Pantalla("DashboardVendedor",  "panelControlToolStripMenuItem",
                "Revise sus pedidos: comunique al cliente los que tienen faltantes y formalice los separados. Haga clic en una tarjeta para abrir el pedido."),
            new Pantalla("DashboardDeposito",  "panelControlToolStripMenuItem",
                "Revise los pedidos a controlar y las prendas en mantenimiento. Haga clic en una tarjeta para abrir Control de Stock o Prendas."),
            new Pantalla("DashboardLogistica", "panelControlToolStripMenuItem",
                "Siga los pedidos pendientes, despachados y entregados. Haga clic en una tarjeta para abrir Pedidos Realizados."),

            // Suscriptores
            new Pantalla("Clientes", "clientesToolStripMenuItem",
                "Busque un cliente por nombre o DNI. Con + Nuevo Cliente lo registra; con Editar corrige sus datos."),
            new Pantalla("Planes", "planesToolStripMenuItem",
                "Elija un plan y presione Editar plan para cambiarlo, o complete el formulario y presione Guardar Plan para crear uno."),
            new Pantalla("RenovacionSuscripcionForm", "renovacionSuscripcionToolStripMenuItem",
                "Elija el cliente y la decisión tomada al contactarlo (renovar, cambiar de plan, pausar o dar de baja) y presione Procesar."),
            new Pantalla("NuevaContratacionForm", "nuevaContratacionToolStripMenuItem",
                "Busque al cliente por DNI o nombre, elija el plan y la modalidad y presione Registrar contratación; luego Caja la cobra.",
                esModal: true),

            // Inventario
            new Pantalla("Prendas", "prendasToolStripMenuItem",
                "Elija una prenda y presione Editar, Estado o Mantenimiento; con + Nueva Prenda la agrega al catálogo."),
            new Pantalla("ControlStockForm", "controlStockToolStripMenuItem",
                "Elija el pedido más antiguo de la cola: si falta alguna prenda, Informar faltantes; si están todas, Confirmar y Separar prendas."),
            new Pantalla("InspeccionDevolucionForm", "inspeccionDevolucionToolStripMenuItem",
                "Elija una prenda devuelta: Aprobar Reingreso si tiene desgaste normal, o Dar de Baja y Cobrar si el daño es irreparable."),
            new Pantalla("ListaEsperaForm", "listaEsperaToolStripMenuItem",
                "Consulte qué clientes esperan cada prenda; elija una anotación y presione Cancelar para quitarla de la lista."),

            // Ventas
            new Pantalla("PedidosVenta", "pedidosVentaToolStripMenuItem",
                "Presione + Nuevo Pedido para armar uno; elija un pedido de la lista para ver sus faltantes, formalizarlo o cancelarlo."),
            new Pantalla("PedidosRealizados", "pedidosRealizadosToolStripMenuItem",
                "Elija un pedido y avance su circuito: Despachar, Marcar Entregado y, cuando vuelvan las prendas, Registrar Devolución."),

            // Caja
            new Pantalla("ContratacionesPendientesForm", "contratacionesPendientesToolStripMenuItem",
                "Elija una contratación de la cola, indique el medio de pago y presione Cobrar; si el pago se rechaza, Intento Fallido."),
            new Pantalla("CobroSuscripcionForm", "cobroSuscripcionToolStripMenuItem",
                "Elija el cliente y la modalidad, indique si el cobro fue Cobrado o Pago fallido y presione Procesar."),

            // Promociones (PN03)
            new Pantalla("CentroDecisionesForm", "centroDecisionesToolStripMenuItem",
                "Elija el período y el plan para analizar; luego revise las recomendaciones, simule opciones y mida el resultado de las promociones."),
            new Pantalla("SugerirPromocionForm", "sugerirPromocionToolStripMenuItem",
                "Elija el período y presione Analizar métricas; complete la sugerencia para un plan y presione Enviar Sugerencia.",
                esModal: true),
            new Pantalla("PromocionesAdministracionForm", "gestionPromocionesToolStripMenuItem",
                "Cree promociones desde las sugerencias de Gerencia o con Alta Manual, y atienda las solicitudes de baja de Ventas."),
            new Pantalla("PromocionesContabilidadForm", "revisionContablePromocionesToolStripMenuItem",
                "Elija una promoción en revisión, escriba la observación y presione Aprobar y Activar o Rechazar."),
            new Pantalla("PromocionesVigentesForm", "promocionesVigentesToolStripMenuItem",
                "Consulte las promociones que se aplican hoy; si una no conviene, elíjala y presione Solicitar baja."),

            // Auditoría
            new Pantalla("Bitacora", "bitSistemaToolStripMenuItem",
                "Filtre por fecha, usuario, módulo o criticidad y presione Buscar; con Exportar PDF guarda el resultado."),
            new Pantalla("Bitacora", "bitNegocioToolStripMenuItem",
                "Filtre por fecha, usuario, módulo o criticidad y presione Buscar; con Exportar PDF guarda el resultado."),
            new Pantalla("ReporteJornadaForm", "reporteJornadaToolStripMenuItem",
                "Elija una fecha y presione Generar; también puede comparar dos jornadas o ver la tendencia de un rango de fechas."),

            // Analítica de Negocio
            new Pantalla("AnaliticaNegocioForm", "analiticaNegocioToolStripMenuItem",
                "Elija un reporte y, si corresponde, su filtro; presione Generar y luego exporte el resultado a PDF o CSV."),

            // Administrar
            new Pantalla("Usuarios", "usuariosToolStripMenuItem",
                "En Cuentas desbloquee cuentas o resetee contraseñas; en Datos administrativos cree usuarios con Nuevo usuario o cambie su rol."),
            new Pantalla("VersionHistorialForm", "historialUsuariosToolStripMenuItem",
                "Elija un usuario y presione Cargar para ver sus versiones anteriores; puede restaurar una o exportarlas a XML o JSON."),
            new Pantalla("GestorPermisos", "perfilesToolStripMenuItem",
                "Elija un rol en el árbol para ver sus permisos; asigne o quite permisos y sub-roles con Asignar y Quitar."),
            new Pantalla("FormIdiomas", "idiomasToolStripMenuItem",
                "Elija un idioma, corrija sus textos en la grilla y presione Guardar cambios; con Nuevo idioma agrega otro."),
            new Pantalla("BackupForm", "backupToolStripMenuItem",
                "Presione Generar Copia de Seguridad para respaldar la base; elija una copia de la lista para restaurarla o eliminarla.",
                esModal: true),
            new Pantalla("DiagnosticoIntegridadForm", "integridadToolStripMenuItem",
                "Revise si hay filas con dígitos verificadores inválidos; Recalcular Todo o Recuperación (Espejo) las reparan."),

            // Fuera de los menús gobernados por patente (los ve cualquier usuario logueado).
            new Pantalla("AlertasForm", "alertasItem",
                "Revise los avisos que requieren su atención según su rol; presione Actualizar para volver a calcularlos."),
            new Pantalla("MiPerfilForm", "miPerfilItem",
                "Elija idioma, fuente, tamaño, tema y formato de fecha, y presione Guardar preferencias.",
                esModal: true),
            new Pantalla("GuiaPrimerUsoForm", "guiaItem",
                "Lea los primeros pasos para su rol y presione Comenzar. Puede volver a abrir esta guía desde Sesión.",
                esModal: true),
            new Pantalla("AcercaDeForm", "acercaDeItem",
                "Consulte la versión instalada y la base de datos a la que está conectado; presione Cerrar para volver.",
                esModal: true),
        };

        /// <summary>
        /// Ítems del menú que abren una pantalla y NO están gobernados por patente en
        /// <see cref="MenuVisibilidad"/> (siempre visibles para un usuario logueado). guiaItem y
        /// acercaDeItem se crean por código (GUI/Menu.Ubicacion.cs), como ayudaItem.
        /// </summary>
        public static readonly string[] ItemsSiempreVisibles = { "alertasItem", "miPerfilItem", "guiaItem", "acercaDeItem" };

        /// <summary>
        /// Todos los ítems del menú que abren una pantalla: las hojas de MenuVisibilidad, Panel de
        /// Control, Analítica de Negocio y los siempre visibles. Cada uno tiene que figurar en
        /// <see cref="Pantallas"/> (lo verifica UsabilidadTests).
        /// </summary>
        public static IEnumerable<string> ItemsQueAbrenPantalla()
        {
            return MenuVisibilidad.Hojas.Select(h => h.Item)
                .Concat(new[] { "panelControlToolStripMenuItem", "analiticaNegocioToolStripMenuItem" })
                .Concat(ItemsSiempreVisibles)
                .Distinct(StringComparer.Ordinal);
        }

        /// <summary>Formularios distintos alcanzables desde el menú (para cubrirlos con su pista).</summary>
        public static IEnumerable<string> Formularios()
            => Pantallas.Select(p => p.Formulario).Distinct(StringComparer.Ordinal);

        /// <summary>Entradas del catálogo de ese formulario (vacío si no se abre desde el menú).</summary>
        public static IList<Pantalla> EntradasDe(string formulario)
            => Pantallas.Where(p => string.Equals(p.Formulario, formulario, StringComparison.Ordinal)).ToList();

        /// <summary>Pista de una pantalla que no está en el catálogo (p. ej. una ventana nueva sin pista propia).</summary>
        public const string PistaGenericaEs = "Presione F1 para ver qué puede hacer en esta pantalla.";

        /// <summary>
        /// True si el formulario es uno de los diálogos modales que se abren desde el menú (Nueva
        /// contratación, Backup, Mi Perfil…). Solo esos se informan a la barra: un diálogo abierto
        /// desde una ventana hija (ClienteForm, CambioEstadoDialog…) también tiene al Menú como
        /// dueño, pero no cambia "dónde está" el usuario.
        /// </summary>
        public static bool EsDialogoDelMenu(string formulario)
            => EntradasDe(formulario).Any(p => p.EsModal);

        /// <summary>
        /// string.Format que nunca lanza: los textos salen de la BD y se editan en Gestión de
        /// Idiomas, así que una plantilla rota ("Usted está en: {0") no puede romper la barra en
        /// cada cambio de ventana. Si la plantilla falla se prueba con la de fábrica y, si también
        /// falla, se devuelven los argumentos unidos con " · ".
        /// </summary>
        public static string FormatoSeguro(string plantilla, string plantillaDeFabrica, params object[] args)
        {
            args = args ?? new object[0];
            foreach (var p in new[] { plantilla, plantillaDeFabrica })
            {
                if (string.IsNullOrEmpty(p)) continue;
                try { return string.Format(p, args); }
                catch (FormatException) { }
            }
            return string.Join(" · ", args.Select(a => a?.ToString() ?? string.Empty));
        }

        /// <summary>
        /// Clave de la pista de una línea de la pantalla: ux.hint.&lt;Formulario&gt; si está en el
        /// catálogo, ux.hint.Inicio sin pantalla y ux.hint.generica si no la conoce.
        /// </summary>
        public static string ClavePista(string formulario)
        {
            if (string.IsNullOrEmpty(formulario) || formulario == Inicio) return "ux.hint." + Inicio;
            return EntradasDe(formulario).Count > 0 ? "ux.hint." + formulario : "ux.hint.generica";
        }

        /// <summary>Pista en español de la pantalla (fallback si la clave no está en las traducciones).</summary>
        public static string PistaEs(string formulario)
        {
            if (string.IsNullOrEmpty(formulario) || formulario == Inicio) return PistaInicioEs;
            return EntradasDe(formulario).Select(p => p.PistaEs).FirstOrDefault() ?? PistaGenericaEs;
        }

        /// <summary>
        /// Clave del tooltip de un control: tip.&lt;Formulario&gt;.&lt;campo del control&gt; (el nombre del
        /// campo en el Designer, no Control.Name, que muchos formularios no asignan). La aplica
        /// GUI.FormBase en todos los formularios; si la clave no existe, el control queda sin tooltip.
        /// </summary>
        public static string ClaveTooltip(string formulario, string control) => "tip." + formulario + "." + control;

        /// <summary>
        /// Arma el texto de la barra "Usted está en": los niveles del menú (ya traducidos, de la
        /// raíz a la hoja) unidos con " → ". Si la pantalla cuelga de varios ítems, se usan los
        /// niveles comunes más el título de la ventana (Auditoría → Bitácora). Sin niveles, el título.
        /// </summary>
        public static string ArmarRuta(IList<IList<string>> recorridos, string tituloVentana)
        {
            var validos = (recorridos ?? new List<IList<string>>())
                .Where(r => r != null && r.Count > 0).ToList();
            if (validos.Count == 0)
                return string.IsNullOrWhiteSpace(tituloVentana) ? Inicio : tituloVentana.Trim();

            List<string> niveles;
            if (validos.Count == 1)
                niveles = validos[0].ToList();
            else
            {
                // Prefijo común (los padres compartidos) + el título de la ventana como hoja.
                niveles = new List<string>();
                for (int i = 0; i < validos.Min(r => r.Count) - 1; i++)
                {
                    string nivel = validos[0][i];
                    if (validos.All(r => r[i] == nivel)) niveles.Add(nivel); else break;
                }
                niveles.Add(string.IsNullOrWhiteSpace(tituloVentana) ? validos[0].Last() : tituloVentana.Trim());
            }
            return string.Join(" → ", niveles.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()));
        }
    }
}
