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
                "• PN02 Renovaciones: Caja registra el cobro de las renovaciones (Cobro de renovaciones)."),

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

            ["CobroSuscripcionForm"] = new TemaAyuda("Cobro de renovaciones (Caja, PN02)",
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
                "• El descuento máximo no puede ser menor que el base."),

            ["ReporteJornadaForm"] = new TemaAyuda("Reporte de jornada",
                "Resumen de una jornada: eventos de negocio del día, prendas disponibles, clientes y días sin copia de seguridad.\n" +
                "\n" +
                "• Comparar jornadas: dos días lado a lado.\n" +
                "• Tendencia (rango): evolución de los eventos entre dos fechas.\n" +
                "• Exportar TXT: guarda el reporte en un archivo."),

            ["Login"] = new TemaAyuda("Ingreso al sistema",
                "Ingrese su usuario y su contraseña y presione Ingresar.\n" +
                "\n" +
                "• Idioma: el selector de la franja superior cambia el idioma de esta pantalla y del sistema.\n" +
                "• Después de 3 intentos fallidos la cuenta se bloquea en forma progresiva (1, 5, 15 y 60 minutos); si sigue fallando, queda bloqueada hasta que un Administrador la desbloquee. Un ingreso correcto reinicia la escala.\n" +
                "• Olvidé mi contraseña: indica cómo pedirle al Administrador una contraseña nueva.\n" +
                "• ¿Cuenta bloqueada? Usar clave de emergencia: solo para cuentas de Administrador, con una de sus claves de un solo uso.\n" +
                "• Si su contraseña es temporal (usuario nuevo o contraseña reseteada), al ingresar se le pide definir una nueva."),

            ["OlvideContrasenaForm"] = new TemaAyuda("Recuperar contraseña",
                "WardrobeFlow no envía correos: la contraseña la resetea un Administrador.\n" +
                "\n" +
                "1. Ingrese su nombre de usuario y presione Enviar solicitud.\n" +
                "2. Comuníquese con el Administrador: desde Administrar › Usuarios › Resetear contraseña le genera una contraseña temporal.\n" +
                "3. Al ingresar con esa contraseña, el sistema le pide definir una nueva."),

            ["DesbloqueoEmergenciaForm"] = new TemaAyuda("Desbloqueo con clave de emergencia",
                "Permite que un Administrador bloqueado por intentos fallidos desbloquee su propia cuenta sin depender de otro Administrador.\n" +
                "\n" +
                "• Ingrese su usuario y una de sus claves de emergencia y presione Desbloquear.\n" +
                "• Cada clave sirve una sola vez: después de usarla queda anulada.\n" +
                "• Solo funciona con cuentas de Administrador bloqueadas. Los demás usuarios deben pedirle el desbloqueo a un Administrador (Administrar › Usuarios › Desbloquear).\n" +
                "• El desbloqueo queda registrado en la bitácora."),

            ["CambioClaveObligatorioForm"] = new TemaAyuda("Cambio de contraseña obligatorio",
                "Su contraseña es temporal (usuario nuevo o contraseña reseteada por el Administrador): defina una nueva para continuar.\n" +
                "\n" +
                "• La contraseña debe tener al menos 8 caracteres, un número y un carácter especial (!@#$%...).\n" +
                "• Escríbala dos veces; si no coinciden, se le avisa y la ventana sigue abierta.\n" +
                "• Si cancela, se cierra la sesión y vuelve a la pantalla de ingreso."),

            ["ConfirmarAdminForm"] = new TemaAyuda("Confirmar Administrador",
                "Algunas acciones delicadas, como recalcular los dígitos verificadores o recuperar la integridad de la base, piden que un Administrador confirme con su usuario y su contraseña.\n" +
                "\n" +
                "• Si el usuario no es Administrador o la contraseña no es correcta, la acción no se ejecuta.\n" +
                "• Después de 3 intentos fallidos la ventana deja de aceptar intentos: ciérrela y vuelva a intentarlo más tarde."),

            ["AltaPromocionForm"] = new TemaAyuda("Alta de promoción (Administración Comercial, PN03)",
                "Crea una promoción para un plan. Queda en revisión contable hasta que Contabilidad la apruebe o la rechace.\n" +
                "\n" +
                "• Desde una sugerencia de Gerencia: el plan viene de la sugerencia y no se puede cambiar; el tipo y el valor se completan con la propuesta.\n" +
                "• Alta manual: elija el plan.\n" +
                "• Reformular una promoción rechazada: arriba se ve la observación de Contabilidad; el plan no se cambia.\n" +
                "\n" +
                "• Tipo: Porcentaje (hasta 100 %), Monto fijo o Precio promocional. El valor debe ser mayor a cero.\n" +
                "• Vigencia: la fecha de fin no puede ser anterior a la de inicio ni haber pasado.\n" +
                "• Margen estimado e impacto económico: Contabilidad los tiene en cuenta para dictaminar.\n" +
                "• Registrar: si falta un dato, el error se muestra abajo y no se pierde lo cargado."),

            ["CargoPrendaDialog"] = new TemaAyuda("Cargo por daño o pérdida (PN04)",
                "Registra lo que se le cobra al último cliente que tuvo la prenda. Se abre desde la Inspección de devolución (Dar de baja con cargo, por daño) y desde Pedidos realizados (Reportar pérdida).\n" +
                "\n" +
                "• Motivo: describa el daño o la pérdida (obligatorio).\n" +
                "• Monto a cobrar: viene precargado con el precio de reposición de la prenda, si tiene uno; se puede corregir y debe ser mayor a cero.\n" +
                "• Registrar cargo: la prenda pasa a Baja y el cargo se suma al próximo cobro del cliente.\n" +
                "• Cancelar: no se registra nada y la prenda no cambia."),

            ["PrendaForm"] = new TemaAyuda("Datos de la prenda",
                "Alta o edición de una prenda del catálogo.\n" +
                "\n" +
                "• Obligatorios: nombre, talle y categoría.\n" +
                "• Precio de reposición (opcional): es el monto que se propone cobrarle al cliente si la prenda vuelve dañada o se pierde (PN04).\n" +
                "• Las prendas nuevas quedan Disponibles.\n" +
                "• Al guardar se valida todo; si hay un error se muestra abajo y el formulario sigue abierto."),

            ["CambioEstadoDialog"] = new TemaAyuda("Cambiar estado de la prenda",
                "Cambia a mano el estado de una prenda del depósito. Solo se ofrecen los cambios permitidos entre Disponible, En limpieza y Baja.\n" +
                "\n" +
                "• Una prenda En uso no se puede cambiar: está a nombre de un cliente.\n" +
                "• Las prendas que volvieron de un cliente se resuelven en Inspección de devolución, y las que el cliente no devuelve, con Reportar pérdida en Pedidos realizados.\n" +
                "• Una prenda dada de baja no se puede reactivar."),

            ["DashboardForm"] = new TemaAyuda("Panel de Control",
                "Se abre al iniciar sesión y muestra solo los indicadores que su rol puede ver: prendas disponibles, clientes, pedidos pendientes, ocupación del stock, contrataciones y renovaciones por cobrar, sugerencias y bajas de promociones pendientes, promociones en revisión y días sin copia de seguridad (Administrador).\n" +
                "\n" +
                "• Mis tareas pendientes: un clic en una fila abre la pantalla donde se resuelve.\n" +
                "• Actividad reciente y Resumen de eventos: lo último que pasó en el sistema.\n" +
                "• La tarjeta de copias de seguridad cambia de color según la antigüedad del último backup; su botón configura cada cuántos días recordarlo.\n" +
                "• Actualizar: vuelve a calcular los indicadores."),

            ["DashboardVendedor"] = new TemaAyuda("Panel de Ventas",
                "Panel del Vendedor al iniciar sesión.\n" +
                "\n" +
                "• Indicadores: pedidos para atender, clientes registrados, contrataciones esperando a Caja y suscripciones por vencer.\n" +
                "• Tablero de pedidos: En control de stock, Con faltantes (hay que comunicarlo al cliente) y Separados (hay que formalizarlos).\n" +
                "• Un clic en el indicador de pedidos o en una tarjeta abre Pedidos de Venta.\n" +
                "• Actualizar: vuelve a cargar los datos."),

            ["DashboardDeposito"] = new TemaAyuda("Panel de Depósito",
                "Panel del Depósito al iniciar sesión.\n" +
                "\n" +
                "• Indicadores: pedidos a controlar, prendas disponibles, prendas en mantenimiento y ocupación del stock.\n" +
                "• Prendas en mantenimiento según el tiempo que llevan: Reciente (menos de 2 días), En curso (de 2 a 7 días) y Urgente (más de 7 días).\n" +
                "• Un clic en Pedidos a controlar abre Control de Stock; un clic en una prenda abre el catálogo de prendas.\n" +
                "• Actualizar: vuelve a cargar los datos."),

            ["DashboardLogistica"] = new TemaAyuda("Panel de Logística",
                "Panel del Operador logístico al iniciar sesión.\n" +
                "\n" +
                "• Indicadores: pedidos pendientes de despacho, despachados y entregados.\n" +
                "• Tablero con los pedidos de cada etapa: Pendiente, Despachado y Entregado.\n" +
                "• Un clic en un indicador abre Pedidos realizados, donde se despacha y se registran la entrega y la devolución.\n" +
                "• Actualizar: vuelve a cargar los datos."),

            ["PedidoHistorialForm"] = new TemaAyuda("Historial de cambios del pedido",
                "Todos los cambios del pedido: quién, cuándo, qué campo, el valor anterior y el nuevo (control de cambios, T06b).\n" +
                "\n" +
                "• Filtros: rango de fechas y tipo de acción (crear, despachar, entregar, cancelar, devolución, restaurar...). Buscar aplica el filtro.\n" +
                "• Restaurar: elija una fila y confirme; el pedido vuelve al estado anterior a esa operación. La restauración queda registrada como una operación más.\n" +
                "• El estado de las prendas del pedido no se revierte automáticamente."),

            ["MantenimientoHistorialForm"] = new TemaAyuda("Historial de mantenimiento",
                "Registros de limpieza y mantenimiento de la prenda elegida, en solo lectura: entrada, salida, duración en días, origen (devolución de un pedido o manual), responsable y estado (En limpieza o Finalizado)."),

            ["DiagnosticoIntegridadForm"] = new TemaAyuda("Diagnóstico de integridad (T07)",
                "Controla los dígitos verificadores (DVH por fila y DVV por tabla) de los datos protegidos.\n" +
                "\n" +
                "• Diagnóstico: estado general (Íntegro o Comprometido) y las filas o tablas cuyos dígitos no coinciden.\n" +
                "• Historial de verificaciones: cada control realizado y su resultado.\n" +
                "• Actualizar: vuelve a verificar.\n" +
                "• Recalcular todo: acepta los datos actuales como válidos y recalcula los dígitos. Pide confirmación y la de un Administrador; úselo solo si sabe por qué cambiaron los datos.\n" +
                "• Recuperación (Espejo)...: abre la consola para reparar los datos de los usuarios desde el espejo de seguridad."),

            ["RecuperacionEspejoForm"] = new TemaAyuda("Recuperación de integridad (T07)",
                "Muestra qué cambió en los datos de los usuarios respecto del último estado válido guardado en el espejo: filas modificadas, insertadas desde fuera del sistema, eliminadas o con el dígito verificador dañado.\n" +
                "\n" +
                "• Reparar desde Espejo: restaura los valores legítimos guardados en el espejo.\n" +
                "• Asumir Pérdida: acepta los datos actuales y recalcula los dígitos verificadores.\n" +
                "• Restaurar Backup...: cuando el espejo no alcanza, por ejemplo si se vació la tabla.\n" +
                "\n" +
                "Los botones se habilitan según el tipo de daño y cada acción pide la confirmación de un Administrador."),

            ["FormIdiomas"] = new TemaAyuda("Gestión de Idiomas (T05)",
                "Idiomas del sistema y sus traducciones.\n" +
                "\n" +
                "• Nuevo idioma: código (hasta 5 letras) y nombre. Se crea inactivo hasta que cargue sus traducciones.\n" +
                "• Activar / Desactivar: un idioma activo se puede elegir al ingresar y en Mi perfil. Si faltan traducciones se avisa, y esos textos se muestran en el idioma por defecto. El idioma por defecto no se puede desactivar.\n" +
                "• Renombrar: cambia el nombre visible.\n" +
                "• Traducciones: edite la columna Texto / Traducción y presione Guardar cambios; la columna Referencia muestra el texto por defecto.\n" +
                "• Controles traducibles: las claves y la pantalla a la que pertenecen, en solo lectura."),

            ["GuiaPrimerUsoForm"] = new TemaAyuda("Guía de primeros pasos",
                "Cinco pasos con lo esencial de su rol: por dónde empezar, qué pantallas va a usar y cómo pedir ayuda.\n" +
                "\n" +
                "• Se abre sola la primera vez que ingresa. Marque «No volver a mostrar al iniciar sesión» para que no aparezca más.\n" +
                "• Se puede volver a abrir en cualquier momento desde Sesión › Ver guía de inicio."),

            ["AvisoComercialForm"] = new TemaAyuda("Avisos comerciales (PN03)",
                "Al iniciar sesión, Gerencia ve un resumen de lo que necesita su atención en el Centro de decisiones: recomendaciones de prioridad alta, decisiones pospuestas que llegaron a su fecha y metas del mes en riesgo.\n" +
                "\n" +
                "• Ir al Centro de decisiones: abre el Centro de decisiones comerciales.\n" +
                "• No mostrar hoy: el aviso no vuelve a aparecer hasta mañana.\n" +
                "• Cerrar: lo cierra. El aviso no bloquea el resto del sistema."),

            ["AcercaDeForm"] = new TemaAyuda("Acerca de WardrobeFlow",
                "Datos de la aplicación instalada (solo lectura; la ve cualquier usuario con sesión iniciada).\n" +
                "\n" +
                "• Versión: la del programa instalado, leída del ejecutable; es la misma que muestra el instalador.\n" +
                "• Compilación: fecha del ejecutable instalado.\n" +
                "• Servidor y Base de datos: la instancia de SQL Server y la base a las que está conectada la aplicación (cadena de conexión WardrobeFlowDB).\n" +
                "\n" +
                "Al pedir soporte, informe la versión y el servidor que se ven en esta pantalla.")
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

            using (var dlg = new AyudaDialog())
            {
                dlg.Mostrar(Tr("ayuda.ventana", "Ayuda") + " — " + titulo, titulo, texto, Tr("btn.cerrar", "Cerrar"));
                dlg.ShowDialog(propietario);
            }
        }
    }
}
