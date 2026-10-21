using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI.Ayuda
{
    /// <summary>
    /// D02 — Ayuda en línea: F1 en cualquier pantalla (FormBase o el Menú) abre la ayuda de esa
    /// pantalla. El texto se busca en las traducciones ("ayuda.&lt;Pantalla&gt;" y
    /// "ayuda.&lt;Pantalla&gt;.titulo", así se puede traducir) y, si no está, se usa el texto en
    /// español de este catálogo. Las pantallas sin texto propio muestran la ayuda general.
    /// </summary>
    internal static class AyudaEnLinea
    {
        private sealed class TemaAyuda
        {
            public string Titulo;
            public string Texto;
            public TemaAyuda(string titulo, string texto) { Titulo = titulo; Texto = texto; }
        }

        // Generado desde la misma fuente que las traducciones ayuda.* del .tsv: es el texto en español
        // que se muestra si la clave no está en la base.
        private static readonly Dictionary<string, TemaAyuda> Catalogo = new Dictionary<string, TemaAyuda>(StringComparer.Ordinal)
        {
            ["General"] = new TemaAyuda("Ayuda de WardrobeFlow",
                "WardrobeFlow gestiona el alquiler de prendas por suscripción.\n" +
                "\n" +
                "• El menú muestra solo las opciones que su rol tiene permitidas.\n" +
                "• En cualquier pantalla, F1 (o Sesión › Ayuda) abre la ayuda de esa pantalla.\n" +
                "• Los mensajes de resultado aparecen en la barra inferior de cada pantalla (verde: correcto, rojo: error).\n" +
                "• Las confirmaciones de acciones que no se pueden deshacer traen \"No\" como opción por defecto.\n" +
                "• Mi Perfil permite cambiar el idioma, la fuente, el tema y el formato de fecha.\n" +
                "\n" +
                "Procesos principales:\n" +
                "• PN01 Armar pedido: Vendedor (Pedidos de venta) y Depósito (Control de stock).\n" +
                "• PN02 Comercialización: Vendedor (Nueva contratación) y Caja (Cobro de contrataciones).\n" +
                "• PN03 Promociones: Gerencia sugiere, Administración Comercial crea, Contabilidad aprueba.\n" +
                "• PN04 Devoluciones: Depósito inspecciona las prendas devueltas y reporta las perdidas.\n" +
                "• N01 Suscripciones: Caja registra el cobro de las renovaciones (Cobro de renovaciones)."),

            ["Clientes"] = new TemaAyuda("Clientes",
                "Alta, edición y baja de clientes.\n" +
                "\n" +
                "• Nuevo: registra los datos personales. El plan no se asigna acá: se contrata desde Nueva contratación y queda activo cuando Caja cobra.\n" +
                "• Si el DNI ya existe, el formulario muestra el error y no se pierde lo cargado.\n" +
                "• Baja: no se permite si el cliente tiene prendas en uso, una contratación pendiente de pago, un pedido en curso o cargos por daño o pérdida pendientes.\n" +
                "• Editar no cambia la gracia, la pausa ni el beneficio por referido."),

            ["ClienteForm"] = new TemaAyuda("Datos del cliente",
                "Complete nombre, apellido, DNI y, opcionalmente, email y cliente que lo refirió.\n" +
                "Al guardar se valida todo; si hay un error se muestra abajo y el formulario sigue abierto."),

            ["PedidosVenta"] = new TemaAyuda("Pedidos de venta (PN01)",
                "Circuito del pedido desde el lado del Vendedor.\n" +
                "\n" +
                "1. Nuevo pedido: elija el cliente (debe tener la suscripción vigente y ningún pedido en curso) y las prendas dentro de su cupo. Se envía a control de stock.\n" +
                "2. Depósito controla: si faltan prendas, el pedido vuelve \"Con faltantes\": Ver faltantes, y el cliente ajusta la selección o desiste.\n" +
                "3. Cuando Depósito separa las prendas: Formalizar.\n" +
                "\n" +
                "• Documentos ▾: planilla, informe de faltantes, constancias, confirmación del pedido y aviso de desistimiento.\n" +
                "• Cancelar: pedidos en control de stock, separados o pendientes de despacho (con motivo).\n" +
                "• El buscador filtra por número, cliente, vendedor o estado."),

            ["NuevoPedidoForm"] = new TemaAyuda("Nuevo pedido",
                "1. Busque al cliente: se verifica que la suscripción esté vigente y que no tenga otro pedido en curso.\n" +
                "2. Elija las prendas disponibles; el cupo del plan (prendas en uso + seleccionadas) se controla en el momento.\n" +
                "3. Enviar a control de stock. Si la selección excede el cupo, el cliente puede corregirla o desistir."),

            ["ControlStockForm"] = new TemaAyuda("Control de stock (Depósito, PN01)",
                "Cola de pedidos enviados por los vendedores, del más antiguo al más nuevo.\n" +
                "\n" +
                "• Al elegir un pedido se revisa la disponibilidad de cada prenda.\n" +
                "• ¿Selección disponible? No → Informar faltantes (con alternativas). Sí → Confirmar prendas y Separar (quedan en uso a nombre del cliente).\n" +
                "• Documentos ▾: planilla de control, informe de faltantes, detalle de prendas confirmadas y constancia de separación."),

            ["PedidosRealizados"] = new TemaAyuda("Pedidos realizados",
                "Seguimiento de los pedidos formalizados: Despachar, registrar la Entrega y registrar la Devolución (las prendas pasan a la inspección de PN04).\n" +
                "\n" +
                "• Reportar pérdida: solo para una prenda En uso de un pedido Entregado hace 30 días o más (compra tácita). Se le cobra la reposición al cliente con su próximo cobro."),

            ["NuevaContratacionForm"] = new TemaAyuda("Nueva contratación (PN02)",
                "1. Identifique al cliente por DNI o nombre. Si no está registrado, Registrar cliente.\n" +
                "2. Elija el plan y la modalidad (mensual, trimestral o anual): se muestra el importe a abonar con el descuento y los cargos pendientes que correspondan.\n" +
                "3. Registrar: la contratación queda pendiente de pago hasta que Caja la cobre.\n" +
                "\n" +
                "• Si el cliente pasa a un plan más caro con su plan vigente, el plan nuevo rige desde hoy y se descuentan los días no usados.\n" +
                "• A un plan igual o más barato se cambia al vencer el período pagado.\n" +
                "• Si el cliente no contrata: Asentar desistimiento (con motivo)."),

            ["ContratacionesPendientesForm"] = new TemaAyuda("Cobro de contrataciones (Caja, PN02)",
                "Cola de contrataciones pendientes de pago.\n" +
                "\n" +
                "• Elija el medio de pago (con tarjeta de crédito, las cuotas) y Cobrar: se emite el comprobante y la suscripción queda activa. El total incluye los cargos por daño o pérdida pendientes del cliente.\n" +
                "• Intento fallido: registra el intento con motivo; al tercero la contratación se cancela.\n" +
                "• Anular: cancela una contratación sin cobrarla (el cliente se arrepintió o hubo un error de carga), con motivo.\n" +
                "• Quien registró la contratación no puede cobrarla (salvo el Administrador).\n" +
                "• En la vista de resueltas se reimprimen el comprobante y las constancias."),

            ["CobroSuscripcionForm"] = new TemaAyuda("Cobro de renovaciones (Caja, N01)",
                "Cobro recurrente de las suscripciones vencidas o próximas a vencer.\n" +
                "\n" +
                "• Elija el cliente y la modalidad: se muestra el total a cobrar (período − descuento + cargos por daño o pérdida).\n" +
                "• Cobrado: indique el medio de pago; se emite el comprobante y se extiende la vigencia.\n" +
                "• Pago fallido: el cliente entra en período de gracia y, si ya estaba en gracia, se suspende."),

            ["RenovacionSuscripcionForm"] = new TemaAyuda("Renovación de la suscripción",
                "Renovar o cambiar de plan sin pasar por Caja es solo para el Administrador (correcciones). El Vendedor puede pausar o dar de baja; para cambiar de plan, registre una Nueva contratación."),

            ["Planes"] = new TemaAyuda("Planes de suscripción",
                "Alta y edición de planes (nombre, límite de prendas y precio mensual).\n" +
                "\n" +
                "• Elija un plan y Editar plan (o doble clic) para cambiarlo.\n" +
                "• Desactivar: no se permite si hay clientes activos con el plan o contrataciones pendientes de pago."),

            ["Prendas"] = new TemaAyuda("Catálogo de prendas",
                "Alta, edición y cambio de estado de las prendas.\n" +
                "\n" +
                "• Una prenda que entró a limpieza en el depósito se puede volver a dejar disponible o dar de baja, sin cargo.\n" +
                "• Las que volvieron de un cliente se resuelven en Inspección de devolución, y las que el cliente no devuelve, con Reportar pérdida en Pedidos realizados."),

            ["SugerirPromocionForm"] = new TemaAyuda("Sugerir promoción (Gerencia, PN03)",
                "1. Elija el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.\n" +
                "2. Si hay oportunidad por plan, use la idea propuesta (se completa el formulario) o cárguela a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.\n" +
                "3. Enviar a Administración. La sugerencia se imprime con Imprimir sugerencia.\n" +
                "4. La retención esperada (30 % por defecto) define el beneficio estimado: ingreso en riesgo × retención. El reporte incluye el Resultado de las promociones: bajas, pausas y renovaciones del plan durante la vigencia contra el período anterior, y la retención real contra la esperada."),

            ["PromocionesAdministracionForm"] = new TemaAyuda("Promociones (Administración Comercial, PN03)",
                "• Sugerencias: crear la promoción a partir de una sugerencia o descartarla (con motivo).\n" +
                "• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si está de acuerdo, la promoción se da de baja, si no, sigue vigente.\n" +
                "• Promociones: crear (siempre para un plan), reformular las rechazadas por Contabilidad, o Dar de baja una vigente o una rechazada (con motivo).\n" +
                "• Imprimir ▾: ficha, dictamen, solicitud de baja, respuesta a la solicitud y constancia de baja.\n" +
                "La fecha de fin no puede haber pasado."),

            ["PromocionesContabilidadForm"] = new TemaAyuda("Revisión contable (Contabilidad, PN03)",
                "Promociones en revisión contable: Aprobar (queda vigente) o Rechazar (vuelve a Administración), con observación.\n" +
                "No se puede aprobar una promoción cuya fecha de fin ya pasó. Quien creó la promoción no puede dictaminarla (salvo el Administrador)."),

            ["PromocionesVigentesForm"] = new TemaAyuda("Promociones vigentes",
                "Consulta de las promociones vigentes. Ventas puede solicitar la baja de una (con motivo); la columna Baja solicitada muestra si ya hay una pendiente. Administración atiende la solicitud: mientras tanto la promoción sigue vigente y se aplica en los cobros."),

            ["Usuarios"] = new TemaAyuda("Usuarios",
                "Administración de usuarios en dos pestañas (solo Administrador).\n" +
                "\n" +
                "Cuentas:\n" +
                "• Resetear contraseña: genera una nueva y la exporta a un archivo (pide confirmación).\n" +
                "• Desbloquear: habilita una cuenta bloqueada por intentos fallidos de ingreso.\n" +
                "• Archivar: el usuario no puede ingresar y sale de la lista, pero se conserva su historial.\n" +
                "• Purgar: elimina definitivamente a los archivados hace más de un año, salvo los que firmaron registros de promociones.\n" +
                "\n" +
                "Datos administrativos:\n" +
                "• Nuevo usuario: el único alta de usuarios. Cada usuario nuevo queda vinculado a su legajo de empleado, para poder vender, cobrar o controlar stock; la contraseña se genera y se exporta a un archivo.\n" +
                "• Guardar cambios / Cambiar rol: nombre, apellido, usuario, email, fecha de nacimiento y rol (el sistema no puede quedar sin administrador).\n" +
                "• Ver historial de cambios: las versiones anteriores del usuario."),

            ["GestorPermisos"] = new TemaAyuda("Perfiles y permisos",
                "Roles y permisos (patentes) del sistema. Un rol con usuarios asignados no se puede eliminar ni renombrar.\n" +
                "\n" +
                "• Gestión de roles: crear, renombrar y eliminar roles, y asignar o quitar permisos y sub-roles.\n" +
                "• Vista completa: el árbol completo de roles, familias y permisos, en solo lectura."),

            ["BackupForm"] = new TemaAyuda("Copias de seguridad",
                "Crear, restaurar y eliminar copias de seguridad de la base (solo Administrador). Antes de crear una copia se verifica la integridad de los datos.\n" +
                "\n" +
                "Backups automáticos (sin cifrar):\n" +
                "• Al iniciar sesión (cualquier rol), si el último backup es más antiguo que el intervalo configurado (60 minutos por defecto; el Administrador lo cambia con \"Backup automático al iniciar sesión...\").\n" +
                "• Cada 24 horas mientras la aplicación esté abierta.\n" +
                "• Rotan entre auto_domingo.bak … auto_sabado.bak (uno por día de la semana) y se restauran sin contraseña.\n" +
                "\n" +
                "Restablecer datos de demo... (solo Administrador): borra los datos de negocio y vuelve a cargar los de prueba con las fechas corridas a hoy. Antes genera una copia sin cifrar WardrobeFlow_PrevioDemo_<fecha>_<usuario>.bak en la carpeta de backups (se restaura como cualquier otra) y, al terminar, verifica la integridad y reinicia la aplicación."),

            ["Bitacora"] = new TemaAyuda("Bitácora",
                "Consulta de los eventos del sistema con filtros por usuario, fecha, módulo y criticidad. Se puede exportar a PDF.\n" +
                "\n" +
                "• Errores (XML/JSON): los errores inesperados quedan serializados en XML; se pueden ver, importar desde otra PC y exportar, en XML o JSON."),

            ["MiPerfilForm"] = new TemaAyuda("Mi perfil",
                "Preferencias del usuario: idioma, fuente, tamaño, tema y formato de fecha (se aplican a todas las pantallas). La contraseña la cambia el Administrador desde Usuarios."),

            ["InspeccionDevolucionForm"] = new TemaAyuda("Inspección de devolución (PN04)",
                "Cola de prendas que volvieron de un cliente.\n" +
                "\n" +
                "• Desgaste normal → Aprobar reingreso: la prenda vuelve a estar disponible, sin cargo (y se avisa a la lista de espera).\n" +
                "• Daño irreparable → Dar de baja con cargo: se cobra la reposición al último cliente con su próximo cobro.\n" +
                "Las prendas que entraron a limpieza en el depósito no aparecen acá."),

            ["ErroresXmlForm"] = new TemaAyuda("Errores (XML/JSON)",
                "Errores inesperados de la aplicación, serializados en XML (un archivo por mes en esta PC).\n" +
                "\n" +
                "• Ver los de esta PC: lee los archivos locales.\n" +
                "• Importar XML/JSON: abre un archivo de errores en XML o JSON, por ejemplo traído de otra PC.\n" +
                "• Exportar XML/JSON: guarda la lista que se está viendo en un archivo; el formato lo da el tipo elegido (.xml o .json).\n" +
                "Los dos diálogos abren en %LOCALAPPDATA%\\WardrobeFlow\\Errores\\Exportados, aparte del registro mensual (la carpeta se crea si no existe); la importación puede abrir un archivo de cualquier carpeta."),

            ["VersionHistorialForm"] = new TemaAyuda("Historial de Cambios de Usuarios",
                "Versiones (patrón Memento) de los datos administrativos de cada usuario: usuario, nombre, apellido, email y fecha de nacimiento. La contraseña nunca se versiona.\n" +
                "\n" +
                "• Cargar: muestra las versiones del usuario elegido guardadas en la base.\n" +
                "• Restaurar Versión Seleccionada (solo Administrador): deja al usuario exactamente en ese estado. Queda registrado como un cambio más, así que también se puede deshacer.\n" +
                "• Exportar versiones…: serializa las versiones del usuario elegido a XML o JSON (según el tipo elegido), sin la clave ni el estado de bloqueo. Por defecto se guardan en %LOCALAPPDATA%\\WardrobeFlow\\Versiones (la carpeta se crea si no existe).\n" +
                "• Importar versiones…: lee un archivo exportado (XML o JSON), verifica su formato y que pertenezca a un usuario existente, y muestra sus versiones marcadas como \"importada\". Elija una y presione Restaurar: se aplican los mismos controles que en una restauración normal."),

            ["AlertasForm"] = new TemaAyuda("Alertas",
                "Avisos que necesitan atención según su rol: suscripciones vencidas o por vencer, pedidos esperando control de stock o con faltantes, prendas en limpieza, contrataciones pendientes de pago, integridad de datos y copias de seguridad. Actualizar las vuelve a calcular."),

            ["ListaEsperaForm"] = new TemaAyuda("Lista de espera",
                "Clientes anotados para una prenda que hoy está en uso. Cuando la prenda vuelve a estar disponible, se reserva para el primero de la lista durante 48 horas. Si la prenda se da de baja por daño o pérdida, las anotaciones se cancelan."),

            ["AnaliticaNegocioForm"] = new TemaAyuda("Analítica de Negocio",
                "Reportes para la toma de decisiones. El selector muestra solo los reportes que su rol tiene permitidos.\n" +
                "\n" +
                "1. Elija el reporte y, si tiene, su filtro: criterio de riesgo (Análisis de Abandono), umbral mínimo (Escasez de Stock) o cliente (Recomendación de Prendas).\n" +
                "2. Generar: el resultado se muestra en la grilla, con un resumen arriba.\n" +
                "3. Exportar a PDF (se guarda y se abre) o Guardar como .CSV.\n" +
                "\n" +
                "• Gerencia Comercial: Análisis de Abandono y Ventas por Vendedor.\n" +
                "• Gerencia de Inventario: Rotación de Prendas, Tiempos de Mantenimiento y Escasez de Stock.\n" +
                "• Ventas: Recomendación de Prendas."),

            ["CentroDecisionesForm"] = new TemaAyuda("Centro de decisiones comerciales (Gerencia, PN03)",
                "Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar → medir.\n" +
                "\n" +
                "1. Analizar: período y plan; indicadores contra el período anterior (▲/▼) y gráficos con 3 meses proyectados.\n" +
                "1b. Cohortes: retención por mes de alta y peor cohorte.\n" +
                "2. Recomendaciones: qué pasa, por qué, qué hacer y su impacto. Aceptar y ejecutar siempre hace algo: sugiere, extiende o pide la baja de una promoción, exporta los contactos de una campaña (se mide después), genera la orden de compra o da de baja / manda a revisión prendas. Descartar pide motivo; Posponer la oculta N días.\n" +
                "3. Historial: impacto estimado contra el real.\n" +
                "4. Simulador: ¿qué pasa si…? y hasta 3 escenarios.\n" +
                "5. Medir resultado: veredicto por promoción.\n" +
                "Metas: objetivo mensual por indicador con real, proyección y semáforo; en riesgo, sube la prioridad de lo que la mueve y se avisa al iniciar sesión.\n" +
                "\n" +
                "Umbrales: lo que dispara cada regla. Informe ejecutivo: PDF con todo."),

            ["UmbralesComercialesDialog"] = new TemaAyuda("Umbrales del motor de recomendaciones",
                "Cada umbral decide cuándo el motor del Centro de decisiones propone una acción. Edite la columna Valor (dentro del rango indicado) y pulse Guardar; los cambios rigen desde el próximo análisis y quedan en la bitácora.\n" +
                "\n" +
                "• Restablecer valores de fábrica: vuelve a los valores por defecto (sin guardar todavía).\n" +
                "• El descuento máximo no puede ser menor que el base, y la ocupación mínima tiene que ser menor que la máxima."),

            ["ReporteJornadaForm"] = new TemaAyuda("Reporte de jornada",
                "Resumen de una jornada: eventos de negocio del día, prendas disponibles, clientes y días sin copia de seguridad.\n" +
                "\n" +
                "• Comparar jornadas: dos días lado a lado.\n" +
                "• Tendencia (rango): evolución de los eventos entre dos fechas.\n" +
                "• Exportar TXT: guarda el reporte en un archivo.")
        };

        // Texto en español de ux.ayuda.ventana (igual al del .tsv): se agrega a la ayuda general.
        private const string AyudaVentanaPrincipal =
            "La ventana principal:\n" +
            "• La barra de estado, al pie, indica dónde está (la ruta del menú, por ejemplo Ventas → Pedidos de Venta), qué puede hacer en esa pantalla y con qué usuario y rol ingresó; «F1: Ayuda» en esa barra también abre la ayuda.\n" +
            "• Al apoyar el mouse sobre un botón, un globo explica para qué sirve.\n" +
            "• El Panel de Control muestra los indicadores y las tareas del día según su rol.\n" +
            "• Al ingresar por primera vez se abre una guía de primeros pasos para su rol; Sesión › Ver guía de inicio la vuelve a abrir.";

        /// <summary>Abre la ayuda de la pantalla <paramref name="nombrePantalla"/> (nombre de la clase del formulario).</summary>
        /// <param name="anexo">Texto que la pantalla agrega a su ayuda (p. ej. el glosario de métricas del Centro de decisiones).</param>
        public static void Mostrar(IWin32Window propietario, string nombrePantalla, string anexo = null)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string Tr(string clave, string fb) => t.ContainsKey(clave) ? t[clave].Texto : fb;

            Catalogo.TryGetValue(nombrePantalla ?? "", out var tema);
            string clave = tema != null ? nombrePantalla : "General";
            tema = tema ?? Catalogo["General"];

            string titulo = Tr("ayuda." + clave + ".titulo", tema.Titulo);
            string texto  = Tr("ayuda." + clave, tema.Texto);
            // Usabilidad: la ayuda general describe también la ventana principal (barra de estado,
            // tooltips, Panel de Control y guía de primer uso). Clave aparte (ux.ayuda.ventana) para
            // no superar el largo de Traduccion.Texto ni pisar ayuda.General en bases ya instaladas.
            if (clave == "General")
                texto += "\n\n" + Tr("ux.ayuda.ventana", AyudaVentanaPrincipal);
            if (!string.IsNullOrWhiteSpace(anexo))
                texto += "\n\n" + anexo.Replace("\r\n", "\n");
            if (clave != "General")
                texto += "\n\n" + Tr("ayuda.pie", "Para la ayuda general, presione F1 en el menú principal.");

            using (var dlg = new Form
            {
                Text = Tr("ayuda.ventana", "Ayuda") + " — " + titulo,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(560, 420),
                BackColor = GUI.Tema.Papel
            })
            {
                var encabezado = new Label
                {
                    Dock = DockStyle.Top, Height = 40, Text = titulo, TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0), BackColor = GUI.Tema.RosaOscuro, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold)
                };
                var cuerpo = new TextBox
                {
                    Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                    BorderStyle = BorderStyle.None, BackColor = GUI.Tema.Papel, ForeColor = GUI.Tema.Tinta,
                    Font = new Font("Segoe UI", 10f), Text = texto.Replace("\n", Environment.NewLine), TabStop = false
                };
                var cerrar = new Button { Text = Tr("btn.cerrar", "Cerrar"), Dock = DockStyle.Bottom, Height = 32, DialogResult = DialogResult.OK };
                var margen = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
                margen.Controls.Add(cuerpo);
                dlg.Controls.Add(margen);
                dlg.Controls.Add(cerrar);
                dlg.Controls.Add(encabezado);
                dlg.AcceptButton = cerrar;
                dlg.CancelButton = cerrar;
                dlg.ShowDialog(propietario);
            }
        }
    }
}
