// Diagramas de actividad (con carriles por actor). Cada paso corresponde a una regla verificada en el código
// (ver docs/NEGOCIO_Y_PROCESOS.md): los rótulos de decisión son las condiciones reales que evalúa la BLL.
const N = (id, tipo, carril, texto) => ({ id, tipo, carril, texto });
const F = (de, a, texto) => ({ de, a, texto });

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
    tipo: 'actividad', id: 'ACT_pn01_armar_pedido', titulo: 'Actividad — PN01 Armar pedido',
    carriles: [{ id: 'C', nombre: 'Cliente' }, { id: 'V', nombre: 'Vendedor' }, { id: 'S', nombre: 'Sistema' }, { id: 'D', nombre: 'Depósito / Logística' }],
    nodos: [
      N('i', 'inicio', 'C'), N('a1', 'accion', 'C', 'Solicita prendas'),
      N('a2', 'accion', 'V', 'Solicita la identificación y localiza la ficha'),
      N('d1', 'decision', 'S', '¿Cuenta apta? vigente, sin pausa, sin pedido despachado, sin prendas en uso'),
      N('a3', 'accion', 'V', 'Informa la situación al cliente'),
      N('a4', 'accion', 'V', 'Presenta el catálogo de prendas disponibles'),
      N('a5', 'accion', 'C', 'Selecciona las prendas'),
      N('a6', 'accion', 'V', 'Registra la selección'),
      N('d2', 'decision', 'S', '¿Dentro del cupo del plan?'),
      N('d3', 'decision', 'S', '¿Todas disponibles y no reservadas para otro?'),
      N('a7', 'accion', 'V', 'Informa el exceso o la falta'),
      N('d4', 'decision', 'C', '¿Ajusta la selección?'),
      N('a8', 'accion', 'S', 'Reserva las prendas (En uso) y crea el pedido bloqueado, en una transacción'),
      N('a9', 'accion', 'V', 'Confirma el pedido al cliente'),
      N('a10', 'accion', 'D', 'Despacha el pedido (Despachado)'),
      N('a11', 'accion', 'D', 'Registra la entrega (Entregado)'),
      N('f', 'fin', 'D'), N('f2', 'fin', 'C')
    ],
    flujos: [
      F('i', 'a1'), F('a1', 'a2'), F('a2', 'd1'), F('d1', 'a3', 'No'), F('a3', 'f2'), F('d1', 'a4', 'Sí'), F('a4', 'a5'), F('a5', 'a6'), F('a6', 'd2'),
      F('d2', 'd3', 'Sí'), F('d2', 'a7', 'No'), F('d3', 'a7', 'No'), F('d3', 'a8', 'Sí'), F('a7', 'd4'), F('d4', 'a6', 'Sí'), F('d4', 'f2', 'Desiste'),
      F('a8', 'a9'), F('a9', 'a10'), F('a10', 'a11'), F('a11', 'f')
    ]
  },
  {
    tipo: 'actividad', id: 'ACT_pn02_comercializacion', titulo: 'Actividad — PN02 Comercialización de la suscripción',
    carriles: [{ id: 'C', nombre: 'Cliente' }, { id: 'V', nombre: 'Vendedor' }, { id: 'S', nombre: 'Sistema' }, { id: 'J', nombre: 'Caja' }],
    nodos: [
      N('i', 'inicio', 'C'), N('a1', 'accion', 'C', 'Elige plan y modalidad de cobro (mensual, trimestral o anual)'),
      N('d1', 'decision', 'V', '¿Cliente registrado?'), N('a2', 'accion', 'V', 'Registra al cliente'),
      N('a3', 'accion', 'V', 'Registra la contratación (plan + modalidad)'),
      N('d2', 'decision', 'S', '¿Cliente y plan válidos, con cupo y sin otra contratación pendiente?'),
      N('a4', 'accion', 'V', 'Informa el motivo del rechazo'),
      N('a5', 'accion', 'S', 'Contratación en Pendiente de pago (la suscripción todavía no está vigente)'),
      N('a6', 'accion', 'J', 'Consulta la cola y cobra al cliente indicando el medio de pago'),
      N('d3', 'decision', 'J', '¿Pago concretado?'),
      N('a7', 'accion', 'S', 'Calcula el importe: precio × meses − un solo descuento'),
      N('a8', 'accion', 'S', 'Activa la suscripción (Builder), acredita al referente y emite el comprobante'),
      N('a9', 'accion', 'J', 'Registra el intento fallido'),
      N('d4', 'decision', 'S', '¿Tercer intento?'),
      N('a10', 'accion', 'S', 'Cancela la contratación'),
      N('f', 'fin', 'S'), N('f2', 'fin', 'S')
    ],
    flujos: [
      F('i', 'a1'), F('a1', 'd1'), F('d1', 'a2', 'No'), F('a2', 'a3'), F('d1', 'a3', 'Sí'), F('a3', 'd2'), F('d2', 'a4', 'No'), F('a4', 'f2'),
      F('d2', 'a5', 'Sí'), F('a5', 'a6'), F('a6', 'd3'), F('d3', 'a7', 'Sí'), F('a7', 'a8'), F('a8', 'f'), F('d3', 'a9', 'No'), F('a9', 'd4'),
      F('d4', 'a10', 'Sí'), F('a10', 'f2'), F('d4', 'a6', 'No')
    ]
  },
  {
    tipo: 'actividad', id: 'ACT_pn03_promociones', titulo: 'Actividad — PN03 Métricas, promociones y toma de decisiones',
    carriles: [{ id: 'G', nombre: 'Gerencia' }, { id: 'A', nombre: 'Administración' }, { id: 'K', nombre: 'Contabilidad' }, { id: 'V', nombre: 'Vendedor' }, { id: 'S', nombre: 'Sistema' }],
    nodos: [
      N('i', 'inicio', 'G'), N('a1', 'accion', 'G', 'Detecta una oportunidad (análisis de abandono y rotación) y sugiere una promoción'),
      N('a2', 'accion', 'A', 'Crea la promoción desde la sugerencia o en forma manual'),
      N('a3', 'accion', 'S', 'Valida (destino único, valor, fechas) y registra En revisión contable'),
      N('a4', 'accion', 'K', 'Analiza margen e impacto económico'),
      N('d1', 'decision', 'K', '¿Aprueba?'),
      N('a5', 'accion', 'S', 'Rechazada por Contabilidad: vuelve a Administración'),
      N('a6', 'accion', 'A', 'Reformula las condiciones'),
      N('a7', 'accion', 'S', 'Vigente: se aplica un solo descuento por cobro'),
      N('d2', 'decision', 'V', '¿Sugiere la baja?'),
      N('a8', 'accion', 'V', 'Sugiere la baja indicando el motivo (Baja solicitada)'),
      N('d3', 'decision', 'A', '¿Aprueba la baja?'),
      N('a9', 'accion', 'S', 'Desactivada'), N('a10', 'accion', 'S', 'Sigue Vigente (conserva la observación contable)'),
      N('f', 'fin', 'S')
    ],
    flujos: [
      F('i', 'a1'), F('a1', 'a2'), F('a2', 'a3'), F('a3', 'a4'), F('a4', 'd1'), F('d1', 'a5', 'No'), F('a5', 'a6'), F('a6', 'a3'),
      F('d1', 'a7', 'Sí'), F('a7', 'd2'), F('d2', 'f', 'No'), F('d2', 'a8', 'Sí'), F('a8', 'd3'), F('d3', 'a9', 'Sí'), F('d3', 'a10', 'No'), F('a9', 'f'), F('a10', 'f')
    ]
  },
  {
    tipo: 'actividad', id: 'ACT_pn04_devolucion', titulo: 'Actividad — PN04 Inspección de devolución',
    carriles: [{ id: 'C', nombre: 'Cliente' }, { id: 'D', nombre: 'Depósito' }, { id: 'S', nombre: 'Sistema' }],
    nodos: [
      N('i', 'inicio', 'C'), N('d0', 'decision', 'C', '¿Devuelve las prendas?'),
      N('a1', 'accion', 'D', 'Registra la devolución: las prendas pasan a En limpieza'),
      N('a2', 'accion', 'D', 'Inspecciona cada prenda'),
      N('d1', 'decision', 'D', '¿Desgaste normal?'),
      N('a3', 'accion', 'D', 'Aprueba el reingreso'),
      N('a4', 'accion', 'S', 'Prenda Disponible sin cargo (cierra mantenimiento y avisa a la lista de espera)'),
      N('a5', 'accion', 'D', 'Indica motivo del daño y monto (o reporta la prenda perdida)'),
      N('a6', 'accion', 'S', 'Registra el cargo contra el último cliente y luego da la prenda de baja'),
      N('a7', 'accion', 'S', 'El cargo se suma al próximo cobro de la suscripción'),
      N('f', 'fin', 'S'), N('f2', 'fin', 'S')
    ],
    flujos: [
      F('i', 'd0'), F('d0', 'a1', 'Sí'), F('a1', 'a2'), F('a2', 'd1'), F('d1', 'a3', 'Sí'), F('a3', 'a4'), F('a4', 'f'),
      F('d1', 'a5', 'No, dañada'), F('d0', 'a5', 'No, perdida'), F('a5', 'a6'), F('a6', 'a7'), F('a7', 'f2')
    ]
  }
];
