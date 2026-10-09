// Diagramas de actividad (con carriles por actor). Cada paso corresponde a una regla verificada en el código
// (ver docs/NEGOCIO_Y_PROCESOS.md): los rótulos de decisión son las condiciones reales que evalúa la BLL.
const N = (id, tipo, carril, texto) => ({ id, tipo, carril, texto });
const F = (de, a, texto) => ({ de, a, texto });
// flujo de documento: de acción a documento (la produce) o de documento a acción (la usa)
const O = (de, a) => ({ de, a });

module.exports = [
  {
    tipo: 'actividad', id: 'ACT_n01_renovacion_cobro', titulo: 'Actividad — N01 Renovación y cobro de la suscripción',
    carriles: [{ id: 'S', nombre: 'Sistema' }, { id: 'V', nombre: 'Vendedor' }],
    nodos: [
      N('i', 'inicio', 'S'), N('a1', 'accion', 'S', 'Detecta suscripción vencida o próxima a vencer (7 días)'),
      N('a2', 'accion', 'V', 'Contacta al cliente fuera del sistema y carga la decisión'),
      N('d1', 'decision', 'V', 'Decisión del cliente'),
      N('r1', 'accion', 'S', 'Renovar: nuevo vencimiento por Builder (1, 3 o 12 meses)'),
      N('r2', 'accion', 'S', 'Cambiar plan: valida cupo y renueva con el plan nuevo'),
      N('r3', 'accion', 'S', 'Pausar: máx. 3 meses, sin prendas en uso, corre el vencimiento'),
      N('r4', 'accion', 'S', 'Baja: exige prendas devueltas'),
      N('a3', 'accion', 'S', 'Registra el resultado en el historial (transacción)'),
      N('d2', 'decision', 'V', '¿Cobro pagado?'),
      N('c1', 'accion', 'S', 'Cobro: precio mensual × meses − un descuento + cargos'),
      N('c2', 'accion', 'S', 'Pago fallido: gracia de 5 días, luego suspensión'),
      N('f', 'fin', 'S')
    ],
    flujos: [
      F('i', 'a1'), F('a1', 'a2'), F('a2', 'd1'), F('d1', 'r1', 'Renovar'), F('d1', 'r2', 'Cambiar plan'), F('d1', 'r3', 'Pausar'), F('d1', 'r4', 'Baja'),
      F('r1', 'a3'), F('r2', 'a3'), F('r3', 'a3'), F('r4', 'a3'), F('a3', 'd2'), F('d2', 'c1', 'Sí'), F('d2', 'c2', 'No'), F('c1', 'f'), F('c2', 'f')
    ]
  },
  {
    tipo: 'actividad', id: 'ACT_pn01_armar_pedido', titulo: 'Actividad — PN01 Armar pedido de prendas',
    carriles: [{ id: 'C', nombre: 'Cliente' }, { id: 'V', nombre: 'Vendedor' }, { id: 'D', nombre: 'Depósito (Controlador de Stock)' }],
    nodos: [
      N('i', 'inicio', 'C'), N('a1', 'accion', 'C', 'Solicitar prendas'),
      N('a2', 'accion', 'V', 'Solicitar información del cliente'),
      N('a3', 'accion', 'C', 'Brindar documentación'),
      N('a4', 'accion', 'V', 'Recibir identificación (DNI, nombre, apellido) → Ficha del cliente'),
      N('a5', 'accion', 'V', 'Verificar la vigencia de la suscripción'),
      N('d1', 'decision', 'V', '¿Suscripción vigente?'),
      N('a6', 'accion', 'V', 'Informar imposibilidad de continuar'),
      N('a7', 'accion', 'V', 'Revisar existencia de un pedido activo'),
      N('d2', 'decision', 'V', '¿Posee pedido activo?'),
      N('a8', 'accion', 'V', 'Informar existencia de pedido activo'),
      N('a9', 'accion', 'V', 'Presentar catálogo'),
      N('a10', 'accion', 'C', 'Seleccionar prendas'),
      N('a11', 'accion', 'V', 'Anotar la selección y comprobar el cupo del plan'),
      N('d3', 'decision', 'V', '¿Excede el cupo?'),
      N('a12', 'accion', 'V', 'Informar exceso de cupo'),
      N('d4', 'decision', 'C', '¿Ajustar selección?'),
      N('a13', 'accion', 'V', 'Asentar desistimiento (aviso)'),
      N('a14', 'accion', 'V', 'Enviar selección para control de stock (planilla)'),
      N('a15', 'accion', 'D', 'Revisar stock de las prendas'),
      N('d5', 'decision', 'D', '¿Selección disponible?'),
      N('a16', 'accion', 'D', 'Informe de prendas faltantes y alternativas'),
      N('a17', 'accion', 'V', 'Comunicar faltantes'),
      N('d6', 'decision', 'C', '¿Ajustar selección?'),
      N('a18', 'accion', 'V', 'Recibir selección ajustada por disponibilidad'),
      N('a19', 'accion', 'D', 'Confirmar prendas disponibles'),
      N('a20', 'accion', 'D', 'Separar prendas del pedido (constancia)'),
      N('a21', 'accion', 'V', 'Formalizar el pedido (sin modificaciones desde aquí)'),
      N('a22', 'accion', 'V', 'Preparar la confirmación'),
      N('a23', 'accion', 'C', 'Recibir confirmación'),
      N('f', 'fin', 'C'), N('f2', 'fin', 'C')
    ],
    flujos: [
      F('i', 'a1'), F('a1', 'a2'), F('a2', 'a3'), F('a3', 'a4'), F('a4', 'a5'), F('a5', 'd1'),
      F('d1', 'a6', 'No'), F('a6', 'f2'), F('d1', 'a7', 'Sí'), F('a7', 'd2'), F('d2', 'a8', 'Sí'), F('a8', 'f2'), F('d2', 'a9', 'No'),
      F('a9', 'a10'), F('a10', 'a11'), F('a11', 'd3'), F('d3', 'a12', 'Sí'), F('a12', 'd4'), F('d4', 'a11', 'Sí'), F('d4', 'a13', 'No'), F('a13', 'f2'),
      F('d3', 'a14', 'No'), F('a14', 'a15'), F('a15', 'd5'), F('d5', 'a16', 'No'), F('a16', 'a17'), F('a17', 'd6'),
      F('d6', 'a18', 'Sí'), F('a18', 'a11'), F('d6', 'a13', 'No'),
      F('d5', 'a19', 'Sí'), F('a19', 'a20'), F('a20', 'a21'), F('a21', 'a22'), F('a22', 'a23'), F('a23', 'f')
    ]
  },
  {
    tipo: 'actividad', id: 'ACT_pn02_comercializacion', titulo: 'Actividad — PN02 Comercialización de la suscripción',
    // Pasos de GUI/NuevaContratacionForm.cs, GUI/ContratacionesPendientesForm.cs y BLL/Contratacion.cs.
    // Documentos: los PDF de GUI/Exportacion/DocumentosContratacion.cs (Planes disponibles, Aviso de desistimiento, Orden de cobro,
    // Liquidación, Comprobante, Constancia de suscripción, Constancia de cancelación) y la información que circula entre carriles.
    // Pago en cuotas: solo Tarjeta de crédito (MedioPago.PermiteCuotas), planes de BLL.Politicas.PoliticaCuotas.Disponibles.
    // Anular: Caja anula una contratación Pendiente de pago con motivo (BLL/Contratacion.cs › Anular). Upgrade: BLL.Politicas.PoliticaCambioPlan.
    carriles: [{ id: 'C', nombre: 'Cliente' }, { id: 'V', nombre: 'Vendedor' }, { id: 'J', nombre: 'Caja' }],
    nodos: [
      N('i', 'inicio', 'C'), N('a1', 'accion', 'C', 'Solicitar información'),
      N('a2', 'accion', 'V', 'Identificar cliente (DNI, nombre o apellido)'),
      N('d1', 'decision', 'V', '¿Registrado?'), N('a3', 'accion', 'V', 'Registrar cliente (referente opcional)'),
      N('a4', 'accion', 'V', 'Presentar planes'),
      N('d2', 'decision', 'C', '¿Elige plan y modalidad?'),
      N('a5', 'accion', 'V', 'Asentar desistimiento'),
      N('r1', 'accion', 'C', 'Recibir aviso de desistimiento'),
      N('a19', 'accion', 'C', 'Elegir plan y modalidad'),
      N('a15', 'accion', 'V', 'Estimar importe a abonar en Caja'),
      N('a6', 'accion', 'V', 'Registrar contratación'),
      N('d3', 'decision', 'V', '¿Contratación válida?'),
      N('a7', 'accion', 'V', 'Informar motivo'),
      N('a16', 'accion', 'J', 'Consultar cola de pendientes de pago'),
      N('a8', 'accion', 'J', 'Calcular importe (un solo descuento; crédito si cambia a un plan más caro; más los cargos por daño o pérdida pendientes)'),
      N('a9', 'accion', 'C', 'Abonar'),
      N('d9', 'decision', 'J', '¿Paga con tarjeta de crédito?'),
      N('a20', 'accion', 'J', 'Ofrecer planes de cuotas (hasta los meses de la modalidad)'),
      N('a21', 'accion', 'C', 'Elegir cantidad de cuotas'),
      N('a22', 'accion', 'J', 'Calcular recargo y valor de cuota'),
      N('d4', 'decision', 'J', '¿Se concreta el pago?'),
      N('a10', 'accion', 'J', 'Confirmar cobro'),
      N('d7', 'decision', 'J', '¿Cambió el importe?'),
      N('a17', 'accion', 'J', 'Emitir comprobante y registrar el cobro'),
      N('a11', 'accion', 'J', 'Activar suscripción (al vencer la vigente, o desde hoy si es un plan más caro)'),
      N('d8', 'decision', 'J', '¿Se activó la suscripción?'),
      N('a18', 'accion', 'J', 'Reabrir el pago (vuelve a Pendiente de pago)'),
      N('d5', 'decision', 'J', '¿Referido?'),
      N('a12', 'accion', 'J', 'Acreditar crédito al referente'),
      N('r2', 'accion', 'C', 'Recibir comprobante y constancia de suscripción'),
      N('a13', 'accion', 'J', 'Registrar intento'),
      N('d6', 'decision', 'J', '¿Alcanzó el máximo de 3 intentos?'),
      N('a14', 'accion', 'J', 'Cancelar contratación'),
      N('d10', 'decision', 'J', '¿Se anula la contratación?'),
      N('a23', 'accion', 'J', 'Anular con motivo (no se cobra y sale de la cola)'),
      N('r3', 'accion', 'C', 'Recibir constancia de cancelación'),
      N('f', 'fin', 'C'), N('f2', 'fin', 'C'),
      // documentos (Artifact «Document», como en el PN01)
      N('o1', 'documento', 'C', 'Identificación (DNI, nombre o apellido)'),
      N('o2', 'documento', 'V', 'Ficha del cliente (plan, vencimiento, estado)'),
      N('o3', 'documento', 'V', 'Planes disponibles (precio y límite de prendas)'),
      N('o4', 'documento', 'C', 'Plan y modalidad elegidos'),
      N('o5', 'documento', 'V', 'Aviso de desistimiento (motivo)'),
      N('o6', 'documento', 'V', 'Orden de cobro (importe estimado)'),
      N('o7', 'documento', 'V', 'Contratación pendiente de pago (precio mensual pactado)'),
      N('o8', 'documento', 'V', 'Motivo de rechazo'),
      N('o9', 'documento', 'J', 'Liquidación (un solo descuento y crédito por cambio de plan)'),
      N('o10', 'documento', 'C', 'Medio de pago e importe'),
      N('o11', 'documento', 'J', 'Comprobante (CMP-NNNNNN-AAAAMMDD, cuotas y recargo)'),
      N('o12', 'documento', 'J', 'Constancia de suscripción (vigencia)'),
      N('o13', 'documento', 'J', 'Intento de pago (número, medio, motivo)'),
      N('o14', 'documento', 'J', 'Constancia de cancelación'),
      N('o15', 'documento', 'J', 'Planes de cuotas (1 sin interés; 3: 5 %; 6: 10 %; 12: 20 %)'),
      N('o16', 'documento', 'C', 'Cuotas elegidas'),
      N('o17', 'documento', 'J', 'Detalle de financiación (cuotas, recargo, valor de cuota)'),
      N('o18', 'documento', 'J', 'Motivo de anulación')
    ],
    flujos: [
      F('i', 'a1'), F('a1', 'a2'), F('a2', 'd1'), F('d1', 'a3', 'No'), F('a3', 'a2'), F('d1', 'a4', 'Sí'), F('a4', 'd2'),
      F('d2', 'a5', 'No'), F('a5', 'r1'), F('r1', 'f2'), F('d2', 'a19', 'Sí'), F('a19', 'a15'), F('a15', 'a6'), F('a6', 'd3'), F('d3', 'a7', 'No'), F('a7', 'd2'),
      F('d3', 'a16', 'Sí'), F('a16', 'd10'), F('d10', 'a8', 'No'), F('d10', 'a23', 'Sí'), F('a23', 'r3'), F('a8', 'a9'), F('a9', 'd9'), F('d9', 'a20', 'Sí'), F('a20', 'a21'), F('a21', 'a22'), F('a22', 'd4'), F('d9', 'd4', 'No'), F('d4', 'a10', 'Sí'), F('a10', 'd7'),
      F('d7', 'a8', 'Sí'), F('d7', 'a17', 'No'), F('a17', 'a11'), F('a11', 'd8'), F('d8', 'a18', 'No'), F('a18', 'a16'),
      F('d8', 'd5', 'Sí'), F('d5', 'a12', 'Sí'), F('a12', 'r2'), F('d5', 'r2', 'No'), F('r2', 'f'),
      F('d4', 'a13', 'No'), F('a13', 'd6'), F('d6', 'a16', 'No'), F('d6', 'a14', 'Sí'), F('a14', 'r3'), F('r3', 'f2')
    ],
    // ida y vuelta de información: { de: acción, a: documento } = la produce; { de: documento, a: acción } = la usa
    objetos: [
      O('a1', 'o1'), O('o1', 'a2'),
      O('a2', 'o2'), O('a3', 'o2'), O('o2', 'a4'), O('o2', 'a6'),
      O('a4', 'o3'), O('o3', 'd2'),
      O('a19', 'o4'), O('o4', 'a15'), O('o4', 'a6'),
      O('a5', 'o5'), O('o5', 'r1'),
      O('a15', 'o6'), O('o6', 'a9'),
      O('a6', 'o7'), O('o7', 'a16'),
      O('a7', 'o8'), O('o8', 'd2'),
      O('a8', 'o9'), O('o9', 'a9'), O('o9', 'a10'),
      O('a9', 'o10'), O('o10', 'd9'), O('o10', 'a10'), O('o10', 'a13'),
      O('a20', 'o15'), O('o15', 'a21'), O('a21', 'o16'), O('o16', 'a22'), O('a22', 'o17'), O('o17', 'a10'),
      O('a17', 'o11'), O('o11', 'r2'),
      O('a11', 'o12'), O('o12', 'r2'),
      O('a13', 'o13'), O('o13', 'd6'),
      O('a14', 'o14'), O('o14', 'r3'), O('a23', 'o18'), O('a23', 'o14')
    ]
  },
  {
    tipo: 'actividad', id: 'ACT_pn03_promociones', titulo: 'Actividad — PN03 Métricas, promociones y toma de decisiones',
    // El sistema actúa dentro de cada carril. Cada acción es un método de BLL.AnalisisPromociones /
    // BLL.SugerenciaPromocion / BLL.Promocion y cada decisión, una guarda de BE (PuedeEvaluarse, PuedeDictaminar,
    // PuedeReformularse, PuedeResolverseBaja, PuedeDesactivarseDirecto, DebeVencer). Estados: SugerenciaPromocion
    // Pendiente → Evaluada | Descartada; Promocion según Promocion.TransicionValida (BE/Promocion.cs:91-107).
    // La promoción nace de una sugerencia de Gerencia (con el reporte de métricas: origen Abandono o Rotación;
    // sin él: Manual) o del alta manual de Administración (CrearManual, sin sugerencia). La vigencia termina por
    // la baja pedida por el Vendedor, la desactivación directa o la fecha de fin (CerrarVencidas, que corre al
    // consultar las promociones); si se rechaza la baja vuelve a Vigente con el mismo dictamen contable.
    carriles: [{ id: 'G', nombre: 'Gerencia' }, { id: 'A', nombre: 'Administración' }, { id: 'K', nombre: 'Contabilidad' }, { id: 'V', nombre: 'Vendedor' }],
    nodos: [
      N('i', 'inicio', 'G'),
      N('d0', 'decision', 'G', '¿Cómo surge la promoción?'),
      N('a1', 'accion', 'G', 'Analizar métricas del período (por defecto 90 días): abandono por plan, rotación por categoría e impacto de las promociones'),
      N('d1', 'decision', 'G', '¿Hay oportunidad?'),
      N('f0', 'fin', 'G'),
      N('a2', 'accion', 'G', 'Registrar sugerencia: queda Pendiente'),
      N('d2', 'decision', 'A', '¿Acepta la sugerencia?'),
      N('a3', 'accion', 'A', 'Descartar sugerencia con motivo: Descartada'),
      N('f1', 'fin', 'A'),
      N('a4', 'accion', 'A', 'Crear promoción desde la sugerencia'),
      N('a4m', 'accion', 'A', 'Crear promoción manual, sin sugerencia'),
      N('a5', 'accion', 'A', 'Validar (destino único, valor, fecha de fin desde hoy) → En revisión contable'),
      N('a6', 'accion', 'K', 'Analizar margen e impacto (beneficio estimado vs. costo del descuento, promociones superpuestas)'),
      N('d3', 'decision', 'K', '¿Aprueba? (quien la creó no la dictamina; si ya pasó su fecha de fin, se rechaza)'),
      N('a7', 'accion', 'K', 'Rechazada por Contabilidad'),
      N('d4', 'decision', 'A', '¿Reformular?'),
      N('a8', 'accion', 'A', 'Reformular las condiciones'),
      N('a9', 'accion', 'A', 'Descartar promoción con motivo: Descartada'),
      N('f2', 'fin', 'A'),
      N('a10', 'accion', 'K', 'Vigente: un solo descuento por cobro'),
      N('d5', 'decision', 'A', '¿Qué la interrumpe?'),
      N('a11', 'accion', 'V', 'Solicitar la baja con motivo: Baja solicitada'),
      N('d6', 'decision', 'A', '¿Aprueba la baja?'),
      N('a12', 'accion', 'A', 'Desactivada (informe a Gerencia)'),
      N('a13', 'accion', 'A', 'Sigue Vigente con motivo (informe a Ventas)'),
      N('a14', 'accion', 'A', 'Desactivar directamente con motivo: Desactivada'),
      N('a15', 'accion', 'A', 'Vencida (el sistema la cierra al consultar): ya no aplica en el cobro'),
      N('f3', 'fin', 'A'), N('f4', 'fin', 'A'),
      // la información vuelve a quien la pidió (como en el PN01): Gerencia y Ventas reciben las resoluciones
      N('g1', 'accion', 'G', 'Recibir constancia de descarte de la sugerencia'),
      N('g2', 'accion', 'G', 'Recibir informe de la baja (promoción desactivada)'),
      N('v1', 'accion', 'V', 'Recibir resolución de baja rechazada (sigue vigente)'),
      // documentos (Artifact «Document», como en el PN01)
      N('o1', 'documento', 'G', 'Reporte de métricas (período, abandono por plan, rotación por categoría, impacto de las promociones)'),
      N('o2', 'documento', 'G', 'Sugerencia de promoción (Pendiente)'),
      N('o3', 'documento', 'A', 'Constancia de descarte de la sugerencia'),
      N('o4', 'documento', 'A', 'Ficha de promoción (En revisión contable)'),
      N('o5', 'documento', 'K', 'Dictamen contable (rechazo con observación)'),
      N('o6', 'documento', 'K', 'Dictamen contable (aprobada: Vigente)'),
      N('o7', 'documento', 'A', 'Constancia de descarte de la promoción'),
      N('o8', 'documento', 'V', 'Solicitud de baja (motivo)'),
      N('o9', 'documento', 'A', 'Resolución de baja'),
      N('o10', 'documento', 'K', 'Análisis de impacto (beneficio estimado, costo del descuento, promociones superpuestas)'),
      N('o11', 'documento', 'A', 'Condiciones reformuladas'),
      N('o12', 'documento', 'A', 'Constancia de desactivación (motivo)'),
      N('o13', 'documento', 'A', 'Ficha de promoción (Vencida)')
    ],
    // El orden de nodos y flujos fija la grilla de conv-actividad.js (EA-generador): no reordenar sin volver a revisar el layout.
    flujos: [
      F('i', 'd0'), F('d0', 'a1', 'Gerencia analiza las métricas'), F('d0', 'a2', 'Idea propia de Gerencia (origen Manual)'),
      F('d0', 'a4m', 'Alta manual de Administración'), F('a4m', 'a5'),
      F('a1', 'd1'), F('d1', 'f0', 'No: sin promoción'), F('d1', 'a2', 'Sí: elige una (origen Abandono o Rotación)'), F('a2', 'd2'),
      F('d2', 'a3', 'No'), F('a3', 'g1'), F('g1', 'f1'), F('d2', 'a4', 'Sí: la sugerencia queda Evaluada'), F('a4', 'a5'), F('a5', 'a6'), F('a6', 'd3'),
      F('d3', 'a7', 'No'), F('a7', 'd4'), F('d4', 'a8', 'Sí'), F('a8', 'a5'), F('d4', 'a9', 'No'), F('a9', 'f2'),
      F('d3', 'a10', 'Sí'), F('a10', 'd5'),
      F('d5', 'a11', 'El Vendedor pide la baja'), F('a11', 'd6'), F('d6', 'a12', 'Sí'), F('a12', 'g2'), F('g2', 'f3'), F('d6', 'a13', 'No'), F('a13', 'v1'), F('v1', 'a10'),
      F('d5', 'a14', 'Administración la desactiva'), F('a14', 'f3'),
      F('d5', 'a15', 'Llega la fecha de fin'), F('a15', 'f4')
    ],
    // ida y vuelta de información: { de: acción, a: documento } = la produce; { de: documento, a: acción } = la usa
    objetos: [
      O('a1', 'o1'), O('o1', 'a2'),
      O('a2', 'o2'), O('o2', 'd2'), O('o2', 'a4'),
      O('a3', 'o3'), O('o3', 'g1'),
      O('a5', 'o4'), O('o4', 'a6'),
      O('a7', 'o5'), O('o5', 'd4'), O('o5', 'a8'),
      O('a10', 'o6'), O('o6', 'a11'),
      O('a9', 'o7'),
      O('a11', 'o8'), O('o8', 'd6'),
      O('a12', 'o9'), O('a13', 'o9'), O('o9', 'g2'), O('o9', 'v1'),
      O('a6', 'o10'), O('o10', 'd3'),
      O('a8', 'o11'), O('o11', 'a5'),
      O('a14', 'o12'),
      O('a15', 'o13')
    ]
  },
  {
    tipo: 'actividad', id: 'ACT_pn04_devolucion', titulo: 'Actividad — PN04 Inspección de devolución',
    // GUI/InspeccionDevolucionForm.cs (CU05-DEP), GUI/PedidosRealizados.cs › BtnReportarPerdida_Click (CU06-DEP),
    // GUI/CargoPrendaDialog.cs y BLL/InspeccionDevolucion.cs: cargo y baja en UNA transacción (DAL/InspeccionDevolucion.cs).
    // Compra tácita (BLL/Politicas/PoliticaCompraTacita.cs): la pérdida solo con el pedido Entregado hace 30 días o más.
    // El cargo Pendiente lo suma el próximo cobro de la suscripción (BLL/Cobro.cs, CargoPrenda.ObtenerPendientesPorCliente).
    carriles: [{ id: 'C', nombre: 'Cliente' }, { id: 'D', nombre: 'Depósito' }, { id: 'S', nombre: 'Sistema' }],
    nodos: [
      N('i', 'inicio', 'C'), N('d0', 'decision', 'C', '¿Devuelve las prendas?'),
      N('a0', 'accion', 'C', 'Devolver las prendas del pedido'),
      N('a1', 'accion', 'D', 'Registra la devolución: las prendas pasan a En limpieza'),
      N('a8', 'accion', 'D', 'Consultar la cola de prendas En limpieza'),
      N('a2', 'accion', 'D', 'Inspecciona cada prenda'),
      N('d1', 'decision', 'D', '¿Desgaste normal?'),
      N('a3', 'accion', 'D', 'Aprueba el reingreso'),
      N('a4', 'accion', 'S', 'Prenda Disponible sin cargo (cierra mantenimiento y avisa a la lista de espera)'),
      N('dt', 'decision', 'D', '¿Pasaron 30 días desde la entrega? (compra tácita)'),
      N('a9', 'accion', 'D', 'Consultar el detalle de prendas del pedido Entregado (En uso)'),
      N('a5', 'accion', 'D', 'Indica motivo del daño o de la pérdida y monto'),
      N('d2', 'decision', 'S', '¿Tiene último cliente y datos válidos?'),
      N('a10', 'accion', 'D', 'Recibir el rechazo'),
      N('a6', 'accion', 'S', 'Registra el cargo contra el último cliente, da la prenda de baja, cierra su mantenimiento y cancela su lista de espera (una transacción)'),
      N('a7', 'accion', 'S', 'El cargo se suma al próximo cobro de la suscripción'),
      N('r1', 'accion', 'C', 'Recibir el próximo cobro con el cargo de reposición'),
      N('f', 'fin', 'S'), N('f2', 'fin', 'C'), N('f3', 'fin', 'D'),
      // documentos (Artifact «Document», como en el PN01)
      N('o1', 'documento', 'C', 'Prendas devueltas (pedido)'),
      N('o2', 'documento', 'D', 'Cola de prendas En limpieza (último cliente)'),
      N('o3', 'documento', 'D', 'Detalle de prendas del pedido (estado)'),
      N('o4', 'documento', 'D', 'Motivo y monto (precio de reposición)'),
      N('o5', 'documento', 'S', 'Cargo de reposición (Pendiente, último cliente)'),
      N('o6', 'documento', 'S', 'Detalle del próximo cobro (cargos sumados)'),
      N('o7', 'documento', 'S', 'Motivo de rechazo'),
      N('o8', 'documento', 'S', 'Reserva para la lista de espera (48 h)'),
      N('o9', 'documento', 'D', 'Informe de inspección (estado de cada prenda)'),
      N('o10', 'documento', 'D', 'Prenda apta para reingreso'),
      N('o11', 'documento', 'D', 'Pedido Entregado (fecha de entrega)'),
      N('o12', 'documento', 'S', 'Constancia de baja de la prenda (cargo registrado)')
    ],
    flujos: [
      F('i', 'd0'), F('d0', 'a0', 'Sí'), F('a0', 'a1'), F('a1', 'a8'), F('a8', 'a2'), F('a2', 'd1'), F('d1', 'a3', 'Sí'), F('a3', 'a4'), F('a4', 'f'),
      F('d1', 'a5', 'No, dañada'), F('d0', 'dt', 'No'), F('dt', 'd0', 'No, sigue en alquiler'), F('dt', 'a9', 'Sí'), F('a9', 'a5'), F('a5', 'd2'), F('d2', 'a10', 'No'), F('a10', 'f3'),
      F('d2', 'a6', 'Sí'), F('a6', 'a7'), F('a7', 'r1'), F('r1', 'f2')
    ],
    objetos: [
      O('a0', 'o1'), O('o1', 'a1'),
      O('a1', 'o2'), O('o2', 'a8'), O('o2', 'a2'),
      O('a9', 'o3'), O('o3', 'a5'),
      O('a5', 'o4'), O('o4', 'd2'), O('o4', 'a6'),
      O('d2', 'o7'), O('o7', 'a10'),
      O('a6', 'o5'), O('o5', 'a7'),
      O('a7', 'o6'), O('o6', 'r1'),
      O('a4', 'o8'),
      O('a2', 'o9'), O('o9', 'd1'), O('o9', 'a5'),
      O('a3', 'o10'), O('o10', 'a4'),
      O('o11', 'dt'), O('o11', 'a9'),
      O('a6', 'o12'), O('o12', 'a7')
    ]
  }
];
