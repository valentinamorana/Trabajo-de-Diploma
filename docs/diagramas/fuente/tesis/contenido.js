// Contenido de la sección N00 del documento de la tesis. Todo está verificado contra el código
// (ver docs/NEGOCIO_Y_PROCESOS.md). Se vuelca a Word con aplicar-tesis.ps1 (ver README de diagramas).
const spec = (id, nombre, o) => ({ id, nombre, sec: '-', ext: '-', inc: '-', ...o });

module.exports = {
  // ─────────────────────────────── N01 ───────────────────────────────
  n01: {
    titulo: 'N01. Gestión de Clientes y Suscripciones',
    intro: 'El proceso comprende la gestión de los clientes de WardrobeFlow y de su suscripción a lo largo de todo su ciclo de vida: el alta del cliente (con su eventual referente), la administración de los planes de suscripción, la renovación al vencer, el cobro recurrente y la pausa. Toma como referencia el modelo de Nuuly: una cuota fija por período y sin permanencia mínima. El precio de un plan es el de un mes y cada cobro cubre 1, 3 o 12 meses según la modalidad elegida (mensual, trimestral o anual).',
    roles: [
      ['Cliente (externo)', 'Contrata y usa la suscripción y decide qué hacer cuando vence. No accede al sistema; interactúa por medios externos.',
        ['Brindar sus datos y, si corresponde, quién lo refirió', 'Elegir plan y modalidad de cobro', 'Comunicar su decisión al vencer: renovar, cambiar de plan, pausar o darse de baja', 'Abonar el cobro']],
      ['Vendedor (incluido en el Gerente Comercial)', 'Registra y mantiene los clientes, administra los planes y procesa la renovación y el cobro recurrente de cada suscripción.',
        ['Registrar, modificar y dar de baja clientes', 'Administrar los planes (precio mensual y límite de prendas)', 'Procesar la renovación: renovar, cambiar de plan, pausar o dar de baja', 'Reanudar una pausa', 'Procesar el cobro recurrente y sus resultados']]
    ],
    descripcion: [
      'El Vendedor registra al cliente con sus datos (nombre, apellido, DNI, email, medio de pago y fecha de nacimiento). Si el cliente llegó por recomendación de otro, indica quién lo refirió: ese dato se fija una sola vez, al alta. El alta no exige asignar un plan.',
      'Los planes de suscripción (nombre, límite de prendas simultáneas y precio mensual) se administran desde el módulo Planes. Un plan solo puede desactivarse si no tiene clientes asignados.',
      'La suscripción se activa por el proceso PN02 (el Vendedor contrata y Caja cobra). Al activarse, el vencimiento se calcula con el patrón Builder según la modalidad elegida (1, 3 o 12 meses); si el cliente todavía tenía tiempo pagado, el nuevo período se suma a continuación del vigente.',
      'Cuando la suscripción vence o está próxima a vencer (7 días), el Vendedor contacta al cliente por fuera del sistema y carga su decisión. Una cadena de responsabilidad la procesa: renovar, cambiar de plan (el plan nuevo debe alcanzar para las prendas en uso), pausar o dar de baja (exige haber devuelto todas las prendas). Cada resultado queda en el historial de renovaciones.',
      'La pausa puede pedirse en cualquier momento, dura como máximo 3 meses y requiere no tener prendas pendientes de devolución. Mientras está pausada no se pueden armar pedidos y el vencimiento se corre por los días de pausa; si se reanuda antes de tiempo, se devuelven los días no usados.',
      'El cobro recurrente se procesa con otra cadena de responsabilidad. Si el cobro se concreta, el importe es el precio mensual del plan por los meses de la modalidad, menos un único descuento (el mayor entre la promoción vigente del plan y el crédito por referido), más los cargos por daño o pérdida pendientes; se extiende el vencimiento y se limpia la gracia. Si el pago falla, se otorga una gracia de 5 días y, vencida, la cuenta se suspende y no puede armar pedidos.',
      'Cuando un cliente referido activa su suscripción por primera vez, su referente recibe un crédito fijo de $1000 para su próximo cobro (una sola vez por cliente referido).'
    ],
    reglas: [
      'El DNI del cliente no puede repetirse y se guarda cifrado.',
      'Un cliente con prendas en uso no puede darse de baja.',
      'Cambiar el plan o el vencimiento de un cliente directamente, sin pasar por una contratación, es una corrección administrativa exclusiva del Administrador.',
      'El precio de un plan es el de un mes y debe ser mayor a cero. El importe de cada cobro es ese precio por los meses de la modalidad (1, 3 o 12), sin descuento por plazo.',
      'La renovación, el cambio de plan y la baja solo se procesan con la suscripción vencida o próxima a vencer; la pausa puede pedirse en cualquier momento.',
      'Un cambio de plan exige que el plan nuevo tenga capacidad para las prendas que el cliente ya tiene en uso.',
      'La baja de la suscripción exige que el cliente haya devuelto todas las prendas.',
      'La pausa dura como máximo 3 meses, no se puede pedir con prendas pendientes de devolución ni con otra pausa vigente, y corre el vencimiento por los días pausados.',
      'Solo se aplica un descuento por cobro: el mayor entre la promoción vigente del plan y el crédito por referido. El crédito no usado queda acumulado.',
      'Un pago fallido otorga 5 días de gracia; vencida la gracia, la cuenta queda suspendida hasta que se registre un cobro.',
      'El beneficio por referido se acredita una única vez por cliente referido, al activar su suscripción por primera vez.'
    ],
    actividad: 'ACT_n01_renovacion_cobro',
    cuLista: [['CU01-VEN', 'Gestionar Cliente', 'Vendedor'], ['CU02-VEN', 'Renovar Suscripción', 'Vendedor'], ['CU03-VEN', 'Cobrar Suscripción', 'Vendedor'], ['CU04-VEN', 'Gestionar Planes', 'Vendedor']],
    cuDiagrama: 'CU_n01_clientes_suscripciones',
    specs: [
      spec('CU01-VEN-GESTIONAR CLIENTE', 'Gestionar Cliente', {
        desc: 'Permite al Vendedor registrar, modificar y dar de baja a los clientes, indicando opcionalmente quién los refirió. El alta no requiere plan: la suscripción se contrata luego en PN02.',
        actor: 'Vendedor', pre: ['El Vendedor tiene sesión activa con permiso de edición de clientes.'],
        esc: ['1. El Vendedor abre el módulo Clientes y consulta el listado (con filtro por texto).', '2. El Vendedor selecciona Nuevo Cliente.', '3. El Vendedor completa nombre, apellido, DNI, email, medio de pago, fecha de nacimiento y, si corresponde, "Referido por".', '4. El sistema valida los datos obligatorios y que el DNI no esté registrado.', '5. El sistema registra al cliente con el DNI cifrado y deja constancia en bitácora.', '6. El sistema informa que el cliente fue registrado.'],
        alt: ['4.1 Datos inválidos o DNI duplicado: el sistema rechaza la operación y solicita corregir. Se retoma el punto 3.', '3.1 Modificar cliente: mismas validaciones, con unicidad de DNI excluyendo al propio cliente. Cambiar el plan o el vencimiento es exclusivo del Administrador.', '3.2 Dar de baja: no se permite si el cliente tiene prendas en uso; la baja es lógica.'],
        post: ['El cliente queda registrado, sin plan asignado.', 'Si fue referido, su referente queda fijado (no se puede cambiar).'], dss: 'DSS_N01_CU01_GestionarCliente' }),
      spec('CU02-VEN-RENOVAR SUSCRIPCIÓN', 'Renovar Suscripción', {
        desc: 'Permite al Vendedor cargar la decisión del cliente sobre su suscripción vencida o próxima a vencer (renovar, cambiar de plan, pausar o dar de baja) y procesarla mediante una cadena de responsabilidad.',
        actor: 'Vendedor', pre: ['El Vendedor tiene sesión activa con permiso de edición de clientes.', 'El cliente tiene un plan asignado.'],
        esc: ['1. El Vendedor selecciona al cliente y el sistema muestra su estado (vigente, por vencer, vencida o pausada).', '2. El Vendedor registra la decisión que el cliente comunicó por fuera del sistema.', '3. El sistema verifica que la suscripción esté vencida o próxima a vencer (7 días); la pausa puede pedirse siempre.', '4. Según la decisión: Renovar calcula el nuevo vencimiento a continuación del vigente según la modalidad; Cambiar plan valida que el plan nuevo alcance para las prendas en uso; Pausar indica hasta cuándo (máximo 3 meses) y corre el vencimiento; Baja exige que el cliente haya devuelto todas las prendas.', '5. El sistema registra el resultado en el historial de renovaciones junto con la actualización del cliente, en una única transacción.', '6. El sistema informa el resultado.'],
        alt: ['3.1 Todavía no corresponde renovar: el sistema informa "Pendiente" y no modifica nada.', '4.1 Pausa que supera los 3 meses, ya pausada o con prendas en uso: el sistema rechaza la operación.', '4.2 Baja con prendas sin devolver: el sistema informa que deben devolverse y no la efectiviza.', '4.3 Cliente pausado: puede reanudarse desde la misma pantalla ("Reanudar ahora"); se devuelven los días de pausa no usados.'],
        post: ['El resultado (Renovada, Cambio de plan, Pausada o Baja) queda en el historial de renovaciones.', 'El vencimiento y el plan del cliente reflejan la decisión.'], dss: 'DSS_N01_CU02_RenovarSuscripcion' }),
      spec('CU03-VEN-COBRAR SUSCRIPCIÓN', 'Cobrar Suscripción', {
        desc: 'Permite al Vendedor cargar el resultado del cobro recurrente de una suscripción (efectuado fuera del sistema) y procesarlo mediante una cadena de responsabilidad.',
        actor: 'Vendedor', pre: ['El Vendedor tiene sesión activa con permiso de edición de clientes.', 'El cliente tiene un plan asignado.'],
        esc: ['1. El Vendedor selecciona al cliente y la modalidad de cobro.', '2. El Vendedor indica si el cobro se concretó o si el pago falló.', '3. El sistema verifica que corresponda cobrar (suscripción vencida o próxima a vencer).', '4. Si el cobro se concretó, el sistema calcula el importe: precio mensual por los meses de la modalidad, menos un único descuento, más los cargos pendientes.', '5. El sistema extiende el vencimiento, limpia la gracia y, en una única transacción, registra el cobro, consume el crédito usado y liquida los cargos.', '6. El sistema informa el resultado y el detalle del importe.'],
        alt: ['3.1 Todavía no corresponde cobrar: el sistema informa "Pendiente".', '5.1 Otra sesión ya cobró los cargos pendientes: el sistema rechaza el cobro completo.', '2.1 El pago falló: el sistema otorga 5 días de gracia (primer fallo) o suspende la cuenta si la gracia ya venció.'],
        post: ['El cobro queda registrado en el historial de cobros.', 'La cuenta queda al día, en gracia o suspendida según el resultado.'], dss: 'DSS_N01_CU03_CobrarSuscripcion' }),
      spec('CU04-VEN-GESTIONAR PLANES', 'Gestionar Planes', {
        desc: 'Permite al Vendedor crear y modificar los planes de suscripción, y activarlos o desactivarlos.',
        actor: 'Vendedor', pre: ['El Vendedor tiene sesión activa con permiso de edición de planes.'],
        esc: ['1. El Vendedor abre el módulo Planes y consulta el listado.', '2. El Vendedor completa nombre, límite de prendas y precio mensual.', '3. El sistema valida que el nombre sea obligatorio y solo con letras, que el límite sea válido y que el precio sea mayor a cero.', '4. El sistema guarda el plan (activo) y deja constancia en bitácora.', '5. El sistema informa que el plan fue guardado.'],
        alt: ['3.1 Datos inválidos: el sistema rechaza la operación e informa el motivo.', '4.1 Desactivar un plan con clientes asignados: el sistema rechaza la operación.'],
        post: ['El plan queda disponible para nuevas contrataciones, o desactivado si corresponde.'], dss: 'DSS_N01_CU04_GestionarPlanes' })
    ],
    clases: [['CLASES_n01_clientes_suscripciones', 'Clases del proceso'], ['CLASES_patron_builder_suscripcion', 'Patrón Builder: activación de la suscripción por modalidad'], ['CLASES_patron_chain_renovacion', 'Patrón Chain of Responsibility: renovación'], ['CLASES_patron_chain_cobro', 'Patrón Chain of Responsibility: cobro recurrente']],
    mc: 'MC_n01_clientes_suscripciones', der: 'DER_n01_clientes_suscripciones'
  },

  // ─────────────────────────────── PN01 ───────────────────────────────
  pn01: {
    titulo: 'PN01. Armar pedido de prendas',
    intro: 'El proceso comprende las actividades necesarias para que un cliente con suscripción vigente seleccione prendas del catálogo, se valide que la selección respeta el cupo de su plan y que su cuenta está habilitada, se confirme la disponibilidad física real de cada unidad y se formalice el pedido, obteniendo un pedido confirmado y bloqueado con las prendas reservadas a su nombre. Luego Depósito o Logística lo despacha, registra su entrega y, al finalizar el uso, registra la devolución.',
    roles: [
      ['Cliente', 'Solicita prendas y recibe la confirmación del pedido. No accede al sistema; interactúa por medios externos.', ['Solicitar prendas', 'Brindar identificación (DNI/código)', 'Seleccionar prendas del catálogo', 'Ajustar la selección si excede el cupo o falta disponibilidad, o desistir', 'Recibir la confirmación del pedido', 'Devolver las prendas al finalizar el uso']],
      ['Vendedor', 'Atiende al cliente, verifica su situación y el cupo, y formaliza el pedido.', ['Solicitar datos al cliente', 'Verificar la situación del cliente: suscripción vigente y cuenta habilitada', 'Presentar el catálogo de prendas', 'Verificar el cupo del plan', 'Formalizar el pedido y confirmarlo al cliente', 'Cancelar un pedido Pendiente']],
      ['Depósito / Logística', 'Prepara, despacha y entrega los pedidos, y registra la devolución de las prendas.', ['Despachar los pedidos Pendientes', 'Registrar la entrega al cliente', 'Registrar la devolución (desbloquea la cuenta del cliente)', 'Consultar el historial de un pedido']]
    ],
    descripcion: [
      'El Cliente solicita prendas.',
      'El Vendedor solicita su identificación (DNI/código) y localiza su ficha.',
      'El sistema verifica la situación del cliente: suscripción vigente (no pausada ni suspendida por falta de pago), sin un pedido despachado pendiente de entrega y sin prendas pendientes de devolución (cuenta desbloqueada). Si no cumple, se informa la situación y el proceso finaliza.',
      'El Vendedor presenta el catálogo de prendas disponibles.',
      'El Cliente consulta el catálogo y selecciona las prendas.',
      'El Vendedor registra la selección y el sistema verifica el cupo del plan. Si excede el límite, informa el exceso; el Cliente ajusta la selección (se vuelve a verificar) o desiste.',
      'Al confirmar, el sistema vuelve a leer el estado real de cada prenda y verifica que todas estén disponibles y que ninguna esté reservada por la lista de espera para otro cliente. Si alguna no lo está, se informa la falta y el Cliente ajusta la selección o desiste.',
      'Si todas están disponibles, el sistema reserva las prendas (pasan a En uso) y crea el pedido, bloqueado y con número único, en una única transacción. Si otra sesión tomó una prenda en el mismo instante, se revierte todo el pedido.',
      'El Vendedor confirma el pedido al Cliente.',
      'Depósito o Logística despacha el pedido (Despachado) y registra su entrega al cliente (Entregado).',
      'Cuando el Cliente devuelve las prendas, Depósito registra la devolución: las prendas pasan a En limpieza y la cuenta del cliente queda habilitada para un nuevo pedido. La inspección de esas prendas es el proceso PN04.'
    ],
    reglas: [
      'No se puede armar un pedido si la suscripción del cliente no está vigente, está pausada o está suspendida por falta de pago.',
      'Un cliente con un pedido despachado pendiente de entrega no puede generar un pedido nuevo.',
      'Cuenta desbloqueada (igual que en Nuuly): un cliente con prendas en uso, es decir con pedidos Pendiente, Despachado o Entregado cuya devolución no se registró, no puede armar un pedido nuevo. Este chequeo ocurre al elegir al cliente, no recién al formalizar.',
      'La cantidad de prendas seleccionadas no puede superar el límite de prendas del plan del cliente.',
      'Guardar una prenda como interés no reserva stock: la reserva real ocurre al confirmar, cuando el sistema verifica la disponibilidad y las pasa a En uso en la misma transacción que crea el pedido.',
      'Una prenda que la lista de espera reservó (por 48 horas) para otro cliente no puede incluirse en el pedido.',
      'El pedido queda bloqueado al crearse: no admite modificaciones. Solo puede cancelarse, con un motivo, mientras esté Pendiente, y des-cancelarse si sus prendas siguen disponibles.',
      'Despachar exige un pedido Pendiente, registrar la entrega exige uno Despachado y registrar la devolución exige uno Entregado; la devolución solo afecta a las prendas que siguen en uso por ese cliente.',
      'Cada operación sobre un pedido queda en su historial y en las bitácoras; con permiso de edición, el historial permite restaurar una operación.'
    ],
    actividad: 'ACT_pn01_armar_pedido',
    cuLista: [['CU01-VEN', 'Armar Pedido', 'Vendedor'], ['CU02-VEN', 'Consultar Catálogo', 'Vendedor'], ['CU03-VEN', 'Consultar Situación Cliente', 'Vendedor'], ['CU04-VEN', 'Cancelar Pedido', 'Vendedor'], ['CU01-DEP', 'Despachar Pedido', 'Depósito / Logística'], ['CU02-DEP', 'Registrar Entrega', 'Depósito / Logística'], ['CU03-DEP', 'Registrar Devolución', 'Depósito / Logística']],
    cuDiagrama: 'CU_pn01_armar_pedido',
    specs: [
      spec('CU01-VEN-ARMAR PEDIDO', 'Armar Pedido', {
        desc: 'Permite al Vendedor gestionar el armado de un pedido de alquiler para un cliente con la cuenta habilitada, desde la selección de prendas hasta la formalización, una vez confirmada la disponibilidad real.',
        actor: 'Vendedor', pre: ['El Vendedor tiene sesión activa con permiso de edición de pedidos de venta.', 'El cliente tiene una suscripción vigente, no pausada ni suspendida.'],
        inc: 'CU02-VEN-Consultar Catálogo, CU03-VEN-Consultar Situación Cliente',
        esc: ['1. El sistema solicita la identificación del cliente (DNI o código).', '2. El Vendedor ingresa la identificación y selecciona al cliente.', '3. El sistema verifica la situación del cliente (CU03-VEN): suscripción vigente, sin pausa ni suspensión, sin pedido despachado pendiente y sin prendas en uso.', '4. El sistema muestra el catálogo de prendas disponibles (CU02-VEN).', '5. El Vendedor registra la selección de prendas indicada por el cliente.', '6. El sistema valida que la selección no exceda el cupo del plan.', '7. El Vendedor confirma la selección.', '8. El sistema vuelve a leer el estado de cada prenda y verifica que todas estén disponibles y no reservadas para otro cliente por la lista de espera.', '9. El sistema reserva las prendas (En uso) y crea el pedido, bloqueado y con número único, en una única transacción; registra el historial y cierra la reserva de la lista de espera si correspondía.', '10. El sistema confirma el pedido al Vendedor.'],
        alt: ['3.1 Suscripción no vigente, sin plan, pausada, suspendida, con otro pedido activo o con prendas sin devolver: el sistema informa la situación y no permite avanzar.', '6.1 La selección excede el cupo del plan: el sistema informa el exceso. El Vendedor ajusta la selección (retoma el paso 5) o desiste.', '8.1 Alguna prenda no está disponible o está reservada para otro cliente: el sistema informa la falta. El Vendedor ajusta la selección (retoma el paso 5) o desiste.', '9.1 Otra sesión tomó una prenda en el mismo instante: el sistema rechaza la operación y revierte todo el pedido.'],
        post: ['El pedido queda creado en estado Pendiente, bloqueado y con número único.', 'Las prendas seleccionadas quedan en estado En uso.', 'El pedido queda disponible para Pedidos Realizados (Depósito / Logística).', 'Queda registrado el historial del pedido y su constancia en las bitácoras.'], dss: 'DSS_PN01_CU01_ArmarPedido' }),
      spec('CU02-VEN-CONSULTAR CATÁLOGO', 'Consultar Catálogo', {
        desc: 'Permite al Vendedor consultar las prendas del catálogo que están en estado Disponible para ofrecerlas al cliente.',
        actor: 'Vendedor', pre: ['Debe existir al menos una prenda registrada en el catálogo.'],
        esc: ['1. El Vendedor solicita consultar el catálogo.', '2. El sistema obtiene las prendas en estado Disponible.', '3. El sistema muestra el listado con talle, color, categoría y valor de reposición.'],
        alt: ['2.1 No hay prendas disponibles: el sistema muestra la grilla vacía.'], post: ['El catálogo disponible queda expuesto para la selección. No se modifica información.'], dss: 'DSS_PN01_CU02_ConsultarCatalogo' }),
      spec('CU03-VEN-CONSULTAR SITUACIÓN DEL CLIENTE', 'Consultar Situación del Cliente', {
        desc: 'Permite al Vendedor consultar el estado comercial de un cliente (plan, cupo, prendas en uso, vigencia de la suscripción) de forma independiente de armar un pedido.',
        actor: 'Vendedor', pre: ['El cliente está registrado en el sistema y tiene una suscripción asociada.'],
        esc: ['1. El sistema solicita el DNI o código del cliente.', '2. El Vendedor ingresa el dato y confirma la consulta.', '3. El sistema obtiene el estado comercial del cliente.', '4. El sistema muestra el plan, el cupo disponible, la vigencia de la suscripción y el pedido activo si corresponde.'],
        alt: ['3.1 Cliente inexistente: el sistema informa que el cliente no existe. Finaliza el caso de uso.', '3.2 Cliente sin plan asignado: el sistema informa SIN_PLAN. Finaliza el caso de uso.', '3.3 Suscripción vencida: el sistema informa SUSCRIPCION_VENCIDA con la fecha de vencimiento. Finaliza el caso de uso.'], post: ['-'], dss: 'DSS_PN01_CU03_ConsultarSituacionCliente' }),
      spec('CU04-VEN-CANCELAR PEDIDO', 'Cancelar Pedido', {
        desc: 'Permite al Vendedor cancelar un pedido Pendiente, con un motivo, liberando sus prendas. La operación se encapsula con el patrón Command.',
        actor: 'Vendedor', pre: ['El Vendedor tiene sesión activa con permiso de edición de pedidos de venta.', 'Existe un pedido en estado Pendiente.'],
        esc: ['1. El Vendedor selecciona un pedido Pendiente y pulsa Cancelar.', '2. El Vendedor indica el motivo de la cancelación.', '3. El sistema encola y ejecuta la orden de cancelación (Command).', '4. El sistema valida que el pedido siga Pendiente y que el motivo no esté vacío.', '5. El sistema cancela el pedido, libera sus prendas a Disponible y registra el historial y las bitácoras.', '6. El sistema informa que el pedido fue cancelado.'],
        alt: ['4.1 El pedido ya no está Pendiente o falta el motivo: el sistema rechaza la operación.', '6.1 Des-cancelar: un pedido Cancelado puede reactivarse si sus prendas siguen disponibles.'],
        post: ['El pedido queda Cancelado y sus prendas Disponibles.'], dss: 'DSS_PN01_CU07_CancelarPedido' }),
      spec('CU01-DEP-DESPACHAR PEDIDO', 'Despachar Pedido', {
        desc: 'Permite a Depósito o Logística marcar como Despachado un pedido Pendiente cuyas prendas ya están reservadas.',
        actor: 'Depósito / Logística', sec: 'Vendedor', pre: ['El usuario tiene sesión activa con permiso de edición de pedidos realizados.', 'Existe un pedido en estado Pendiente.'],
        esc: ['1. El usuario abre Pedidos Realizados y consulta la grilla, con el nivel de urgencia de cada pedido.', '2. El usuario selecciona un pedido Pendiente y pulsa Despachar.', '3. El sistema valida que el pedido esté Pendiente.', '4. El sistema pasa el pedido a Despachado y registra el historial y las bitácoras.', '5. El sistema informa que el pedido fue despachado.'],
        alt: ['3.1 El pedido no está Pendiente: el sistema rechaza la operación e informa su estado actual.'], post: ['El pedido queda Despachado.'], dss: 'DSS_PN01_CU04_DespacharPedido' }),
      spec('CU02-DEP-REGISTRAR ENTREGA', 'Registrar Entrega', {
        desc: 'Permite a Depósito o Logística registrar que un pedido Despachado fue entregado al cliente.',
        actor: 'Depósito / Logística', pre: ['El usuario tiene sesión activa con permiso de edición de pedidos realizados.', 'Existe un pedido en estado Despachado.'],
        esc: ['1. El usuario selecciona un pedido Despachado y pulsa Marcar Entregado.', '2. El sistema valida que el pedido esté Despachado.', '3. El sistema pasa el pedido a Entregado y registra el historial y las bitácoras.', '4. El sistema informa que el pedido fue entregado.'],
        alt: ['2.1 El pedido no está Despachado: el sistema rechaza la operación e informa su estado actual.'], post: ['El pedido queda Entregado; sus prendas siguen En uso hasta que se registre la devolución.'], dss: 'DSS_PN01_CU05_RegistrarEntrega' }),
      spec('CU03-DEP-REGISTRAR DEVOLUCIÓN', 'Registrar Devolución', {
        desc: 'Permite a Depósito registrar que el cliente devolvió las prendas de un pedido Entregado. Las prendas pasan a En limpieza y la cuenta del cliente queda habilitada para un nuevo pedido.',
        actor: 'Depósito / Logística', pre: ['El usuario tiene sesión activa con permiso de edición de pedidos realizados.', 'Existe un pedido en estado Entregado.'],
        esc: ['1. El usuario selecciona un pedido Entregado y pulsa Registrar Devolución.', '2. El sistema valida que el pedido esté Entregado.', '3. En una única transacción, el sistema abre el registro de mantenimiento y pasa a En limpieza solo las prendas que siguen En uso por ese cliente.', '4. El sistema registra el historial y las bitácoras.', '5. El sistema informa que la devolución fue registrada.'],
        alt: ['2.1 El pedido no está Entregado: el sistema rechaza la operación.', '3.1 Ninguna prenda quedó afectada (devolución ya registrada): el sistema rechaza la operación.'],
        post: ['Las prendas quedan En limpieza, pendientes de inspección (PN04).', 'La cuenta del cliente queda desbloqueada.'], dss: 'DSS_PN01_CU06_RegistrarDevolucion' })
    ],
    clases: [['CLASES_pn01_pedidos', 'Clases del proceso'], ['CLASES_patron_command_pedido', 'Patrón Command: cancelación y devolución de pedidos']],
    mc: 'MC_pn01_pedidos', der: 'DER_pn01_pedidos'
  },

  // ─────────────────────────────── PN02 ───────────────────────────────
  pn02: {
    titulo: 'PN02. Comercialización de la Suscripción',
    intro: 'El proceso comprende las actividades necesarias para que un cliente registrado elija un plan de suscripción y una modalidad de cobro, se derive el cobro a Caja de forma separada de quien lo atendió, y recién al confirmarse el pago se formalice la suscripción, calculando su vencimiento y emitiendo el comprobante correspondiente. La separación entre quien capta la venta (Vendedor) y quien cobra (Caja) es intencional: reproduce la separación de funciones habitual en un comercio, donde el operador de venta no maneja el cobro.',
    roles: [
      ['Cliente (externo)', 'Elige un plan de suscripción y una modalidad de cobro. No accede al sistema; interactúa por medios externos.', ['Brindar identificación (DNI/código)', 'Elegir el plan y la modalidad de cobro', 'Abonar en Caja']],
      ['Vendedor', 'Atiende al cliente y registra su elección de plan y modalidad de cobro, dejando la contratación pendiente de pago.', ['Registrar al cliente si no existe', 'Presentar los planes de suscripción disponibles', 'Registrar la contratación (plan + modalidad elegidos)', 'Derivar el cobro a Caja']],
      ['Caja', 'Cobra la contratación pendiente de pago y formaliza la suscripción del cliente. No participa en la venta ni en la elección del plan.', ['Consultar la cola de contrataciones pendientes de pago', 'Confirmar el cobro e indicar el medio de pago', 'Registrar intentos de pago fallidos', 'Emitir el comprobante']]
    ],
    descripcion: [
      'El Cliente se acerca a Vendedor y elige un plan de suscripción y una modalidad de cobro (mensual, trimestral o anual). Si el Cliente no está registrado, Vendedor lo registra primero; el alta de un cliente no requiere asignarle un plan en ese momento.',
      'Vendedor registra la contratación con el plan y la modalidad elegidos. El sistema valida que el cliente exista, que el plan esté activo, que el plan alcance para las prendas que el cliente ya tiene en uso y que el cliente no tenga otra contratación pendiente, y deja la contratación en estado Pendiente de Pago. La suscripción del cliente todavía no está vigente en este punto.',
      'Caja consulta la cola de contrataciones pendientes de pago, independientemente de quién las haya generado, ve el importe a cobrar, cobra al Cliente y confirma el pago indicando el medio utilizado.',
      'Al confirmarse el pago, el sistema calcula el importe (precio mensual del plan por los meses de la modalidad, menos un único descuento: el mayor entre la promoción vigente del plan y el crédito por referido), marca la contratación como Pagada registrando la caja, el medio de pago, el importe, el descuento y el comprobante, y activa la suscripción: asigna el plan, calcula el vencimiento (a continuación del período vigente si el cliente todavía tenía tiempo pagado) y limpia la gracia y cualquier pausa. Si el cliente fue referido y todavía no se acreditó el beneficio, lo acredita al referente en ese momento. Si la activación falla, la contratación vuelve a Pendiente de Pago para reintentar.',
      'Si el intento de cobro no se concreta, Caja registra el intento fallido. El sistema permite hasta tres intentos; al agotarse, cancela automáticamente la contratación.'
    ],
    reglas: [
      'No se puede confirmar el cobro de una contratación que no esté en estado Pendiente de Pago.',
      'La suscripción del cliente no queda vigente hasta que Caja confirme el cobro; la sola creación de la contratación por Vendedor no activa nada.',
      'Vendedor y Caja son roles separados a propósito: Vendedor no tiene permiso para confirmar cobros, y Caja no tiene permiso para registrar clientes ni crear contrataciones.',
      'Cambiar el plan o la fecha de vencimiento de un cliente directamente, sin pasar por una contratación, es una corrección administrativa reservada exclusivamente al Administrador.',
      'El importe de un cobro es el precio mensual del plan por los meses de la modalidad (1, 3 o 12), sin descuento por plazo, menos un único descuento: el mayor entre la promoción vigente del plan y el crédito por referido.',
      'El vencimiento se calcula desde la fecha de activación o, si el cliente todavía tenía tiempo pagado, a continuación del vencimiento vigente.',
      'Si el cliente fue referido por otro y es la primera vez que activa su suscripción, se acredita un beneficio fijo al referente en su próximo cobro.',
      'Al tercer intento de pago fallido, la contratación se cancela automáticamente y deja de estar disponible para cobro.',
      'Un cliente no puede tener dos contrataciones pendientes de pago, y el plan elegido debe alcanzar para las prendas que ya tiene en uso.',
      'Dos sesiones de Caja no pueden cobrar la misma contratación: el cobro se reclama de forma atómica y, si la activación falla después, se compensa reabriendo el cobro.'
    ],
    actividad: 'ACT_pn02_comercializacion',
    cuLista: [['CU01-VTA', 'Gestionar Suscripción', 'Vendedor'], ['CU01-CAJ', 'Gestionar Cobro', 'Caja'], ['CU02-CAJ', 'Emitir Comprobante', 'Caja'], ['CU03-CAJ', 'Cancelar Contratación', 'Caja']],
    cuDiagrama: 'CU_pn02_comercializacion',
    specs: [
      spec('CU01-VTA-GESTIONAR SUSCRIPCIÓN', 'Gestionar Suscripción', {
        desc: 'Permite al Vendedor registrar la elección de plan y modalidad de cobro de un cliente, dejando la contratación pendiente de pago hasta que Caja confirme el cobro.',
        actor: 'Vendedor', pre: ['El Vendedor tiene sesión activa con los permisos correspondientes.', 'El cliente está registrado en el sistema.', 'Existe al menos un plan de suscripción activo.'],
        esc: ['1. El Vendedor selecciona al cliente y el plan de suscripción deseado.', '2. El Vendedor indica la modalidad de cobro (Mensual, Trimestral o Anual).', '3. El sistema valida que el cliente exista, que el plan esté activo, que alcance para las prendas que el cliente tiene en uso y que no haya otra contratación pendiente.', '4. El sistema registra la contratación en estado Pendiente de Pago, vinculada al Vendedor que la generó.', '5. El sistema informa al Vendedor que la contratación quedó pendiente de pago y debe derivarse a Caja.'],
        alt: ['3.1 El cliente no existe: el sistema rechaza la operación con el motivo correspondiente.', '3.2 El plan no existe o no está activo: el sistema rechaza la operación.', '3.3 El plan no alcanza para las prendas en uso del cliente: el sistema rechaza la operación.', '3.4 El cliente ya tiene una contratación pendiente de pago: el sistema rechaza la operación.'],
        post: ['La contratación queda registrada en estado Pendiente de Pago.', 'El cliente no tiene su suscripción activa todavía: eso ocurre recién cuando Caja confirme el cobro (CU01-CAJ).', 'La contratación queda disponible en la cola de Caja.'], dss: 'DSS_PN02_CU01_VTA_GestionarSuscripcion' }),
      spec('CU01-CAJ-GESTIONAR COBRO', 'Gestionar Cobro', {
        desc: 'Permite a Caja confirmar el cobro de una contratación pendiente, formalizando la suscripción del cliente y emitiendo el comprobante correspondiente (CU02-CAJ).',
        actor: 'Caja', pre: ['Caja tiene sesión activa con los permisos correspondientes.', 'Existe una contratación en estado Pendiente de Pago.'], inc: 'CU02-CAJ-Emitir Comprobante',
        esc: ['1. Caja consulta la cola de contrataciones pendientes de pago, con el importe a cobrar.', '2. Caja selecciona una contratación e indica el medio de pago.', '3. El sistema revalida contra la base de datos que la contratación siga Pendiente de Pago, que el plan esté activo y que alcance para las prendas en uso.', '4. El sistema calcula el importe: precio mensual por los meses de la modalidad, menos un único descuento (promoción vigente del plan o crédito por referido).', '5. El sistema reclama la contratación de forma atómica y la marca como Pagada, registrando caja, medio de pago, importe, descuento y comprobante (CU02-CAJ).', '6. El sistema activa la suscripción: asigna el plan, calcula el vencimiento, limpia la gracia y la pausa, consume el crédito usado y acredita al referente si corresponde.', '7. El sistema informa a Caja el importe, el descuento aplicado y el comprobante.'],
        alt: ['2.1 No se indica el medio de pago: el sistema rechaza la operación y solicita completarlo.', '3.1 La contratación ya no está Pendiente de Pago: el sistema rechaza la operación con el motivo específico. Se retoma el punto 1.', '3.2 El plan fue dado de baja entre la contratación y el cobro: el sistema rechaza la operación; la contratación permanece Pendiente de Pago.', '5.1 Otra sesión de Caja ya la cobró: el sistema rechaza la operación y solicita actualizar la cola.', '6.1 Falla la activación de la suscripción: el sistema reabre la contratación como Pendiente de Pago para reintentar; si no pudiera reabrirla, deja constancia crítica en bitácora y avisa al Administrador.'],
        post: ['La contratación queda Pagada, con importe, descuento y comprobante.', 'La suscripción del cliente queda vigente con la fecha de vencimiento calculada.', 'Si correspondía, el referente fue acreditado.'], dss: 'DSS_PN02_CU01_CAJ_GestionarCobro' }),
      spec('CU02-CAJ-EMITIR COMPROBANTE', 'Emitir Comprobante', {
        desc: 'Genera automáticamente un comprobante de pago al confirmarse el cobro de una contratación, sin requerir una acción independiente de Caja.',
        actor: 'Caja (a través de CU01-CAJ)', pre: ['CU01-CAJ confirmó el cobro de la contratación.'],
        esc: ['1. El sistema genera un número de comprobante único con el formato CMP-(identificador de la contratación)-(fecha de cobro).', '2. El sistema registra el número y la fecha de emisión en la contratación, junto con el cobro.'],
        alt: ['-'], post: ['La contratación queda con un número de comprobante y una fecha de emisión asociados.'], dss: 'DSS_PN02_CU02_CAJ_EmitirComprobante' }),
      spec('CU03-CAJ-CANCELAR CONTRATACIÓN', 'Cancelar Contratación', {
        desc: 'Permite a Caja registrar un intento de cobro que no se concretó; al alcanzar el máximo de intentos permitidos, el sistema cancela automáticamente la contratación.',
        actor: 'Caja', pre: ['Existe una contratación en estado Pendiente de Pago.'],
        esc: ['1. Caja intenta cobrar una contratación y el intento no se concreta (por ejemplo, el cliente no completa el pago).', '2. Caja registra el intento fallido.', '3. El sistema revalida contra la base de datos que la contratación siga Pendiente de Pago e incrementa el contador de intentos.', '4. Si el contador alcanza el máximo permitido (3), el sistema cancela automáticamente la contratación.'],
        alt: ['3.1 La contratación ya no está Pendiente de Pago: el sistema rechaza el registro del intento.', '4.1 El contador todavía no alcanzó el máximo: la contratación permanece Pendiente de Pago, disponible para un nuevo intento.'],
        post: ['Si se alcanzó el máximo de intentos, la contratación queda Cancelada.', 'Si no, la contratación permanece Pendiente de Pago con el contador de intentos actualizado.'], dss: 'DSS_PN02_CU03_CAJ_CancelarContratacion' })
    ],
    clases: [['CLASES_pn02_contrataciones', 'Clases del proceso']],
    mc: 'MC_pn02_contrataciones', der: 'DER_pn02_contrataciones'
  },

  // ─────────────────────────────── PN03 ───────────────────────────────
  pn03: {
    titulo: 'PN03. Métricas, Promociones y Toma de Decisiones',
    intro: 'El proceso comprende las actividades necesarias para que Gerencia detecte una oportunidad comercial a partir de las métricas del negocio y la proponga como promoción, Administración la formalice (definiendo condiciones, plan o categoría alcanzada, vigencia y descuento), Contabilidad evalúe su impacto económico antes de activarla, y Vendedor pueda sugerir su baja una vez vigente cuando deje de tener sentido comercial. Es una cadena de aprobación entre roles distintos en momentos distintos, no una operación de un único actor.',
    roles: [
      ['Gerencia (rol GerenteComercial; la analítica de inventario la consulta el GerenteInventario)', 'Detecta oportunidades comerciales a partir de la analítica del negocio y propone promociones para que Administración las evalúe.', ['Consultar la analítica de negocio: abandono, ventas por vendedor, rotación, mantenimiento, escasez y recomendaciones', 'Sugerir promociones sobre un plan o una categoría de prenda']],
      ['Vendedor', 'Consulta las promociones vigentes en su operación diaria y puede sugerir que una deje de aplicarse.', ['Consultar promociones vigentes', 'Sugerir la baja de una promoción vigente']],
      ['Administración (AdministracionComercial)', 'Convierte las sugerencias de Gerencia en promociones formales, define sus condiciones comerciales y resuelve las bajas sugeridas por Vendedor.', ['Evaluar sugerencias de Gerencia', 'Crear promociones (desde una sugerencia o manualmente)', 'Modificar y reformular promociones', 'Desactivar promociones vigentes directamente', 'Resolver (aprobar o rechazar) bajas sugeridas']],
      ['Contabilidad', 'Evalúa el impacto económico de una promoción antes de que quede vigente. Rol separado de Administración a propósito: quien redacta la promoción no es quien aprueba su impacto económico.', ['Analizar margen e impacto económico estimados', 'Aprobar o rechazar promociones en revisión']]
    ],
    descripcion: [
      'Gerencia consulta la analítica de negocio (clientes en riesgo de abandono, desempeño de vendedores, rotación de prendas, tiempos de mantenimiento, escasez de stock) y detecta una oportunidad de promoción sobre un plan de suscripción o sobre una categoría de prenda. El sistema también propone ideas a partir de los datos (planes con más abandono, categorías de baja rotación). Gerencia la sugiere indicando el motivo, el tipo de descuento sugerido y el beneficio estimado.',
      'Administración consulta las sugerencias pendientes. Puede tomar una sugerencia como base o crear una promoción manualmente, completando nombre, descripción, tipo de descuento, valor, vigencia, margen estimado e impacto económico. La promoción nace en estado En Revisión Contable: todavía no aplica ningún descuento.',
      'Contabilidad consulta la cola de promociones en revisión, analiza el margen y el impacto económico estimados, y aprueba o rechaza indicando una observación. Si aprueba, la promoción queda Vigente y empieza a aplicarse al cobro de la suscripción. Si rechaza, vuelve a la cola de Administración, que reformula sus condiciones y la envía de nuevo a revisión contable.',
      'Mientras la promoción está vigente, Vendedor la ve en su consulta habitual y puede sugerir su baja indicando un motivo. La solicitud queda pendiente hasta que Administración la resuelva: si la aprueba, la promoción se desactiva; si la rechaza, sigue vigente sin perder la observación original de Contabilidad. Administración también puede desactivar una promoción vigente directamente, sin que medie una sugerencia de baja de Vendedor.',
      'Por cada cobro de suscripción se aplica un solo descuento: el mayor entre la promoción vigente del plan y el crédito por referido del cliente. Las promociones por categoría de prenda son informativas y no reducen el importe del cobro.'
    ],
    reglas: [
      'Una promoción aplica a un plan de suscripción o a una categoría de prenda, nunca a ambos ni a ninguno.',
      'El valor de una promoción debe ser mayor a cero; si el tipo de descuento es Porcentaje, no puede superar el 100%. Un precio promocional es un precio mensual: el descuento resultante escala con los meses del cobro.',
      'La fecha de fin de la vigencia no puede ser anterior a la fecha de inicio.',
      'Una promoción nueva nace En Revisión Contable: no aplica ningún descuento hasta que Contabilidad la apruebe.',
      'Solo se pueden aprobar, rechazar, modificar o reformular promociones que estén En Revisión Contable (la reformulación parte de una promoción Rechazada por Contabilidad).',
      'Solo se puede sugerir la baja de una promoción Vigente, y solo se puede resolver (aprobar o rechazar) la baja de una promoción con Baja Solicitada.',
      'Si Administración rechaza una baja sugerida, el motivo del rechazo se registra en bitácora pero no pisa la observación original de Contabilidad.',
      'Por cada cobro se aplica un solo descuento: el mayor entre la promoción vigente del plan y el crédito por referido; el crédito no usado queda acumulado.',
      'Las promociones por categoría de prenda son informativas: no reducen el importe del cobro.',
      'Los cambios de estado se reclaman de forma atómica: si otra sesión ya resolvió la promoción o la sugerencia, la operación se rechaza.',
      'Gerencia (rol reusado), Administración y Contabilidad están separados a propósito: quien sugiere no es quien define las condiciones, y quien define las condiciones no es quien aprueba el impacto económico.'
    ],
    actividad: 'ACT_pn03_promociones',
    cuLista: [['CU01-GER', 'Sugerir Promoción', 'Gerencia'], ['CU02-GER', 'Consultar Analítica de Negocio', 'Gerencia'], ['CU01-ADM', 'Gestionar Promociones', 'Administración'], ['CU01-CONT', 'Analizar Promoción', 'Contabilidad'], ['CU01-VEN', 'Sugerir Baja de Promoción', 'Vendedor'], ['CU02-ADM', 'Resolver Baja de Promoción', 'Administración']],
    cuDiagrama: 'CU_pn03_promociones',
    specs: [
      spec('CU01-GER-SUGERIR PROMOCIÓN', 'Sugerir Promoción', {
        desc: 'Permite a Gerencia sugerir una idea de promoción, sobre un plan o una categoría de prenda, para que Administración la evalúe y, si corresponde, la convierta en una promoción formal.',
        actor: 'Gerencia (rol GerenteComercial)', pre: ['Gerencia tiene sesión activa con los permisos correspondientes.'],
        esc: ['1. Opcionalmente, Gerencia toma una idea del análisis de datos ("Desde el análisis…"): el sistema propone candidatas (abandono por plan, baja rotación por categoría) y precarga el formulario.', '2. Gerencia indica si la sugerencia aplica a un plan de suscripción o a una categoría de prenda.', '3. Gerencia indica el motivo, el tipo de descuento sugerido y el beneficio estimado.', '4. El sistema valida que se haya indicado exactamente un destino (plan o categoría) y que el beneficio estimado sea válido.', '5. El sistema registra la sugerencia en estado Pendiente.', '6. El sistema informa a Gerencia que la sugerencia quedó registrada para su evaluación.'],
        alt: ['1.1 El análisis no detecta candidatas: el sistema lo informa y Gerencia completa la sugerencia manualmente.', '4.1 Se indican ambos destinos o ninguno: el sistema rechaza la operación. Se retoma el punto 2.', '4.2 No se indica un motivo, o el beneficio estimado no es válido: el sistema rechaza la operación e informa el motivo. Se retoma el punto 3.'],
        post: ['La sugerencia queda registrada en estado Pendiente, disponible para que Administración la evalúe (CU01-ADM).'], dss: 'DSS_PN03_CU01_GER_SugerirPromocion' }),
      spec('CU02-GER-CONSULTAR ANALÍTICA DE NEGOCIO', 'Consultar Analítica de Negocio', {
        desc: 'Permite a Gerencia consultar los reportes de decisión comercial y de inventario y exportarlos: análisis de abandono, ventas por vendedor, rotación de prendas, tiempos de mantenimiento, escasez de stock y recomendación de prendas.',
        actor: 'Gerencia (GerenteComercial o GerenteInventario, según el reporte)', pre: ['Gerencia tiene sesión activa con el permiso propio de cada reporte.'],
        esc: ['1. Gerencia elige el reporte.', '2. Según el reporte, elige el criterio de riesgo (abandono, patrón Strategy) o el umbral mínimo (escasez).', '3. Gerencia pulsa Generar.', '4. El sistema calcula el resultado a partir de los datos del negocio.', '5. El sistema muestra la grilla de resultados.', '6. Opcionalmente, Gerencia exporta el resultado a PDF o CSV.'],
        alt: ['4.1 No hay resultados: el sistema lo informa.'], post: ['No se modifica información: es una operación de consulta.'], dss: 'DSS_PN03_CU02_GER_ConsultarAnalitica' }),
      spec('CU01-ADM-GESTIONAR PROMOCIONES', 'Gestionar Promociones', {
        desc: 'Permite a Administración crear una promoción (a partir de una sugerencia de Gerencia o de forma manual), modificarla o reformularla mientras corresponda, o desactivar directamente una promoción vigente.',
        actor: 'Administración (rol AdministracionComercial)', sec: 'Gerencia', pre: ['Administración tiene sesión activa con los permisos correspondientes.'],
        esc: ['1. Administración consulta las sugerencias pendientes de Gerencia.', '2. Administración elige una sugerencia como base, o decide crear una promoción manualmente.', '3. Administración completa el nombre, la descripción, el tipo de descuento, el valor, el rango de vigencia, el margen estimado y el impacto económico.', '4. El sistema valida los datos: destino único (plan o categoría), valor mayor a cero (y no mayor a 100 si es Porcentaje), fecha de fin no anterior a la de inicio.', '5. El sistema marca la sugerencia como evaluada (de forma atómica) y registra la promoción en estado En Revisión Contable.', '6. El sistema informa a Administración que la promoción quedó pendiente de revisión contable.'],
        alt: ['2.1 La sugerencia elegida ya fue evaluada por otra sesión: el sistema rechaza la operación. Se retoma el punto 1.', '4.1 Los datos no son válidos: el sistema rechaza la operación e informa el motivo. Se retoma el punto 3.', '6.1 Reformular: una promoción Rechazada por Contabilidad se corrige y vuelve a En Revisión Contable, conservando la observación de Contabilidad.', '6.2 Desactivar: Administración desactiva directamente una promoción Vigente; el sistema valida que esté Vigente y la pasa a Desactivada.'],
        post: ['La promoción queda registrada En Revisión Contable, disponible para que Contabilidad la evalúe (CU01-CONT). Si se desactivó directamente, queda Desactivada.'], dss: 'DSS_PN03_CU01_ADM_GestionarPromociones' }),
      spec('CU01-CONT-ANALIZAR PROMOCIÓN', 'Analizar Promoción', {
        desc: 'Permite a Contabilidad evaluar el impacto económico de una promoción en revisión y aprobarla (queda vigente) o rechazarla (vuelve a Administración para reformularse).',
        actor: 'Contabilidad', pre: ['Existe una promoción en estado En Revisión Contable.'],
        esc: ['1. Contabilidad consulta la cola de promociones pendientes de revisión contable.', '2. Contabilidad selecciona una promoción y analiza su margen e impacto económico estimados.', '3. Contabilidad ingresa una observación y decide aprobar o rechazar.', '4. El sistema valida que la promoción siga En Revisión Contable y que se haya indicado una observación.', '5. Si Contabilidad aprueba, el sistema pasa la promoción a Vigente. Si rechaza, la pasa a Rechazada por Contabilidad.', '6. El sistema informa el resultado a Contabilidad.'],
        alt: ['3.1 No se ingresa una observación: el sistema rechaza la operación y la solicita.', '4.1 La promoción ya no está En Revisión Contable: el sistema rechaza la operación. Se retoma el punto 1.'],
        post: ['La promoción queda Vigente (visible para Vendedor, CU01-VEN, y aplicada al cobro) o Rechazada por Contabilidad (vuelve a la cola de Administración para reformularse).'], dss: 'DSS_PN03_CU01_CONT_AnalizarPromocion' }),
      spec('CU01-VEN-SUGERIR BAJA DE PROMOCIÓN', 'Sugerir Baja de Promoción', {
        desc: 'Permite al Vendedor, desde su consulta habitual de promociones vigentes, sugerir que una deje de aplicarse.',
        actor: 'Vendedor', pre: ['Existe una promoción en estado Vigente.'],
        esc: ['1. El Vendedor consulta las promociones vigentes.', '2. El Vendedor selecciona una e indica el motivo por el cual sugiere darla de baja.', '3. El sistema valida que la promoción siga Vigente y que se haya indicado un motivo.', '4. El sistema registra la solicitud de baja y pasa la promoción a Baja Solicitada.', '5. El sistema informa al Vendedor que la sugerencia fue enviada a Administración.'],
        alt: ['3.1 La promoción ya no está Vigente, o no se indicó un motivo: el sistema rechaza la operación.'], post: ['La promoción queda en estado Baja Solicitada, disponible para que Administración la resuelva (CU02-ADM).'], dss: 'DSS_PN03_CU01_VEN_SugerirBaja' }),
      spec('CU02-ADM-RESOLVER BAJA DE PROMOCIÓN', 'Resolver Baja de Promoción', {
        desc: 'Permite a Administración aprobar o rechazar la baja de una promoción sugerida por Vendedor.',
        actor: 'Administración', sec: 'Vendedor', pre: ['Existe una promoción en estado Baja Solicitada.'],
        esc: ['1. Administración consulta las promociones con baja solicitada.', '2. Administración revisa el motivo sugerido por Vendedor y decide aprobar o rechazar la baja.', '3. El sistema valida que la promoción siga en Baja Solicitada.', '4. Si Administración aprueba, el sistema pasa la promoción a Desactivada. Si rechaza, el sistema exige un motivo y la vuelve a Vigente, conservando la observación original de Contabilidad.', '5. El sistema informa el resultado.'],
        alt: ['3.1 La promoción ya no está en Baja Solicitada: el sistema rechaza la operación.', '4.1 Administración rechaza la baja sin indicar un motivo: el sistema rechaza la operación y lo solicita.'],
        post: ['La promoción queda Desactivada, o vuelve a Vigente sin perder la observación original de Contabilidad.'], dss: 'DSS_PN03_CU02_ADM_ResolverBaja' })
    ],
    clases: [['CLASES_pn03_promociones', 'Clases del proceso'], ['CLASES_patron_strategy_abandono', 'Patrón Strategy: análisis de abandono']],
    mc: 'MC_pn03_promociones', der: 'DER_pn03_promociones'
  },

  // ─────────────────────────────── PN04 ───────────────────────────────
  pn04: {
    titulo: 'PN04. Inspección de Devolución',
    intro: 'El proceso comprende las actividades necesarias para resolver, sin aprobación de nadie, qué ocurre con una prenda cuando el cliente la devuelve o cuando se reporta que nunca va a volver. El diseño es binario y no tiene figuras intermedias, como en Nuuly. Si la prenda vuelve en condiciones de desgaste normal, reingresa al catálogo sin cargo, cubierta por la cuota de suscripción. Si vuelve dañada sin reparación posible, o directamente no vuelve, se da de baja del catálogo y se cobra al cliente el precio de reposición completo. A diferencia de Nuuly, que no cobra daños, WardrobeFlow cobra también el daño irreparable: es una decisión propia para proteger el inventario. No existe un aprobador intermedio.',
    roles: [
      ['Depósito (rol Deposito)', 'Inspecciona las prendas devueltas y resuelve directo, sin aprobación de nadie, entre reingresarlas o darlas de baja con cargo. También reporta como perdida una prenda que nunca vuelve.', ['Inspeccionar prendas devueltas', 'Aprobar el reingreso de una prenda en condiciones', 'Dar de baja una prenda dañada y registrar el cargo', 'Reportar una prenda perdida']],
      ['Cliente (externo)', 'Devuelve las prendas al finalizar el uso, o no las devuelve. No accede al sistema.', ['Devolver las prendas', 'Eventualmente, no devolverlas (prenda perdida)']]
    ],
    descripcion: [
      'Cuando un pedido pasa a Entregado y el cliente devuelve las prendas, Depósito registra la devolución (proceso PN01) y éstas pasan a estado En Limpieza, con su registro de mantenimiento abierto. Desde ahí, Depósito las inspecciona una por una.',
      'Si la prenda está en condiciones de desgaste normal, Depósito aprueba su reingreso: la prenda vuelve a estado Disponible sin ningún cargo al cliente, cubierta por la cuota de la suscripción. Se cierra su registro de mantenimiento y, si hay clientes en la lista de espera de esa prenda, el primero recibe una reserva de 48 horas.',
      'Si la prenda está dañada sin reparación posible, Depósito indica el motivo del daño y el monto, y el sistema registra un cargo contra el cliente que la tuvo por última vez, por el precio de reposición, y recién después da la prenda de baja del catálogo. El cargo se suma automáticamente al próximo cobro de la suscripción del cliente.',
      'Si una prenda que el cliente tiene En Uso nunca vuelve (se reporta como perdida), Depósito puede reportarla directamente como tal sin esperar a que pase por Limpieza: se registra el mismo cargo por precio de reposición y la prenda se da de baja.'
    ],
    reglas: [
      'No hay aprobador: Depósito resuelve directo, sin que intervenga ningún Supervisor o Administrador.',
      'Solo se puede inspeccionar (aprobar reingreso o dar de baja con cargo) una prenda que esté En Limpieza. Ninguna otra pantalla puede retirar del catálogo una prenda En Limpieza.',
      'Solo se puede reportar como perdida una prenda que esté En Uso; es la única transición manual permitida desde ese estado.',
      'El cargo se calcula sobre el precio de reposición cargado en la ficha de la prenda; si no está cargado, Depósito puede indicarlo manualmente. El monto debe ser mayor a cero, el motivo es obligatorio y la prenda debe tener un último cliente registrado.',
      'El cargo se registra antes de dar la prenda de baja, no después: si el registro del cargo fallara, la prenda no queda destruida sin haberse cobrado.',
      'El cargo pendiente se suma al próximo cobro de la suscripción del cliente y se liquida en la misma transacción que el cobro.',
      'Una prenda dada de baja es un estado final: no admite ninguna transición posterior.',
      'Si otra sesión de Depósito ya resolvió la prenda, la operación se rechaza (el cambio de estado se reclama de forma atómica).'
    ],
    actividad: 'ACT_pn04_devolucion',
    cuLista: [['CU-DEP-01', 'Inspeccionar Devolución', 'Depósito'], ['CU-DEP-02', 'Reportar Prenda Perdida', 'Depósito']],
    cuDiagrama: 'CU_pn04_devolucion',
    specs: [
      spec('CU-DEP-01-INSPECCIONAR DEVOLUCIÓN', 'Inspeccionar Devolución', {
        desc: 'Permite a Depósito resolver, prenda por prenda, qué ocurre con lo que un cliente devolvió: reingresarla al catálogo sin cargo, o darla de baja y cobrar el precio de reposición.',
        actor: 'Depósito (rol Deposito)', pre: ['Depósito tiene sesión activa con los permisos correspondientes.', 'Existe al menos una prenda en estado En Limpieza.'],
        esc: ['1. Depósito consulta la cola de prendas en estado En Limpieza pendientes de inspección.', '2. Depósito selecciona una prenda e inspecciona su condición física.', '3. Si la prenda está en condiciones de desgaste normal, Depósito aprueba su reingreso.', '4. El sistema valida que la prenda siga En Limpieza y la pasa a Disponible, sin generar ningún cargo, cierra su mantenimiento y avisa a la lista de espera.', '5. El sistema informa a Depósito que la prenda reingresó al catálogo.'],
        alt: ['3.1 La prenda está dañada sin reparación posible: Depósito indica el motivo del daño y el monto (precargado con el precio de reposición). El sistema valida que la prenda tenga un último cliente, registra el cargo contra ese cliente y recién entonces da la prenda de baja. Informa que la prenda fue dada de baja y el cargo quedó registrado.', '4.1 La prenda ya no está En Limpieza (otra sesión de Depósito ya la resolvió): el sistema rechaza la operación. Se retoma el punto 1.'],
        post: ['La prenda queda Disponible para un nuevo pedido, o Baja con un cargo registrado contra el cliente que la tuvo por última vez, a sumarse en su próximo cobro.'], dss: 'DSS_PN04_CU01_DEP_InspeccionarDevolucion' }),
      spec('CU-DEP-02-REPORTAR PRENDA PERDIDA', 'Reportar Prenda Perdida', {
        desc: 'Permite a Depósito registrar que una prenda que un cliente tiene En Uso nunca va a devolverse, dándola de baja y cobrando el precio de reposición sin esperar a que pase por Inspección de Devolución.',
        actor: 'Depósito', pre: ['Depósito tiene sesión activa con los permisos correspondientes.', 'Existe una prenda en estado En Uso.'],
        esc: ['1. Depósito consulta el detalle de prendas de un pedido y selecciona una que esté En Uso.', '2. Depósito indica el motivo de la pérdida y el monto a cobrar (precargado con el precio de reposición).', '3. El sistema valida que la prenda tenga un último cliente registrado y registra el cargo contra ese cliente.', '4. El sistema da la prenda de baja del catálogo.', '5. El sistema informa a Depósito que la prenda fue reportada como perdida y el cargo quedó registrado.'],
        alt: ['3.1 La prenda no tiene un último cliente registrado, no se indicó el motivo o el monto no es válido: el sistema rechaza la operación.'], post: ['La prenda queda en estado Baja, con un cargo registrado contra el cliente que la tenía, a sumarse en su próximo cobro.'], dss: 'DSS_PN04_CU02_DEP_ReportarPrendaPerdida' })
    ],
    clases: [['CLASES_pn04_devolucion', 'Clases del proceso'], ['CLASES_patron_state_prenda', 'Patrón State: ciclo de vida de la prenda']],
    mc: 'MC_pn04_devolucion', der: 'DER_pn04_devolucion'
  },

  // ─────────────────────────────── Secciones globales ───────────────────────────────
  globales: {
    g01_compra: 'La compra definitiva de una prenda en alquiler no forma parte de esta versión del sistema: el catálogo es rotativo y las prendas solo se alquilan. Cuando una prenda no vuelve o vuelve dañada sin reparación, se da de baja del catálogo y se cobra el precio de reposición (proceso PN04).',
    g02_intro: 'El sistema gestiona el ciclo completo del servicio de alquiler de prendas: desde el registro del cliente y la activación de su suscripción, hasta la devolución, la inspección de las prendas y el cobro de los cargos por daño o pérdida.',
    g02_modulos: [
      ['M01 – Usuarios y Permisos', 'Ciclo de vida de usuarios, perfiles y permisos con el patrón Composite (roles, familias y patentes). Asignación dinámica de perfiles y habilitación de accesos según el rol.', 'Administrador'],
      ['M02 – Clientes y Suscripciones (N01)', 'Registro de clientes y gestión del ciclo de vida de la suscripción: planes, renovación, cobro recurrente, pausa y programa de referidos.', 'Vendedor, Gerente Comercial, Administrador'],
      ['M03 – Catálogo de Prendas', 'Inventario con atributos (talle, color, categoría, estado, valor de reposición), ciclo de vida de la prenda (Disponible, En uso, En limpieza, Baja), mantenimiento y lista de espera.', 'Depósito, Gerente de Inventario, Vendedor (consulta), Administrador'],
      ['M04 – Pedidos de Alquiler (PN01)', 'Creación, seguimiento y cierre de pedidos. Validación de cupo, suscripción vigente y cuenta desbloqueada. Un cliente con prendas sin devolver no puede armar otro pedido.', 'Vendedor, Gerente Comercial, Administrador'],
      ['M05 – Pedidos Realizados', 'Despacho, entrega y devolución de los pedidos, con nivel de urgencia. Sin envío de email ni tracking.', 'Depósito, Operador Logístico, Gerente de Inventario, Gerente Comercial, Administrador'],
      ['M06 – Devoluciones e Incidencias (PN04)', 'Inspección de las prendas devueltas: reingreso al catálogo sin cargo, o baja con cargo por reposición (daño irreparable o pérdida). Sin aprobador.', 'Depósito, Gerente de Inventario, Administrador'],
      ['M07 – Reportes y Auditoría', 'Bitácoras del sistema y de negocio con filtros combinados, reporte de jornada y control de integridad con dígitos verificadores.', 'Auditor, Administrador'],
      ['M08 – Comercialización de la Suscripción (PN02)', 'Contratación de un plan y una modalidad de cobro, cobro en Caja con comprobante y activación de la suscripción.', 'Vendedor, Caja, Administrador'],
      ['M09 – Métricas, Promociones y Decisiones (PN03)', 'Analítica de negocio y cadena de promociones: sugerencia, alta, revisión contable, vigencia y baja.', 'Gerente Comercial, Gerente de Inventario, Administración Comercial, Contabilidad, Vendedor, Administrador']
    ],
    g05_roles: [
      ['Administrador', 'Configuración y gestión general: usuarios, perfiles, idiomas, backup e integridad. Tiene acceso a todas las pantallas.', 'Todos los módulos.'],
      ['Auditor', 'Consulta de auditoría, de solo lectura.', 'Bitácoras del sistema y de negocio, reporte de jornada.'],
      ['Vendedor', 'Primer contacto operativo: registra clientes, contrata planes, genera y cancela pedidos, procesa renovaciones y cobros recurrentes y consulta promociones vigentes.', 'M02, M03 (consulta), M04, M08 (contratar), consulta de promociones y recomendaciones.'],
      ['Gerente Comercial', 'Decisión comercial. Incluye al Vendedor y suma la sugerencia de promociones, el análisis de abandono y el reporte de ventas por vendedor.', 'Lo del Vendedor, más Pedidos Realizados, Sugerir promoción y analítica comercial.'],
      ['Operador Logístico', 'Ejecución de despachos y entregas, con vista restringida a los pedidos realizados.', 'Pedidos Realizados.'],
      ['Depósito', 'Gestión física del catálogo: alta y edición de prendas, inspección de devoluciones, reporte de pérdidas, despacho y registro de devoluciones.', 'M03 (gestión), M05, M06.'],
      ['Gerente de Inventario', 'Decisión de inventario. Incluye al Operador Logístico y al Depósito y suma los análisis de rotación, mantenimiento y escasez.', 'Lo del Depósito y del Operador Logístico, más analítica de inventario.'],
      ['Caja', 'Cobra las contrataciones pendientes, emite el comprobante y registra los intentos de pago fallidos.', 'Contrataciones pendientes (M08).'],
      ['Administración Comercial', 'Formaliza las promociones: alta, modificación, reformulación, desactivación y resolución de bajas.', 'Gestión de promociones (M09).'],
      ['Contabilidad', 'Evalúa el impacto económico de las promociones y las aprueba o rechaza.', 'Revisión contable de promociones (M09).'],
      ['Cliente (externo)', 'Usuario final del servicio. No accede al sistema en ningún momento.', 'Sin acceso. Sus acciones son registradas por el personal.']
    ],
    g07_figs: [['CLASES_global_dominio_a', 'Dominio: clientes, suscripciones y promociones'], ['CLASES_global_dominio_b', 'Dominio: pedidos y prendas']],
    g08_figs: [['MC_negocio', 'Módulos de negocio (vista conceptual; el detalle de cada proceso está en N00)'], ['DER_seg_usuarios', 'Módulo de usuarios'], ['DER_seg_permisos_idiomas', 'Módulo de permisos e idiomas'], ['DER_seg_auditoria', 'Módulo de auditoría e integridad']],
    g07_texto: 'El diagrama de clases del dominio muestra las entidades de negocio y sus relaciones. Se organiza por capas (BE, BLL, DAL) y cada proceso incluye su diagrama propio con las clases de negocio, los servicios y las interfaces de acceso a datos, además de los patrones de diseño aplicados (Builder, State, Command, Chain of Responsibility y Strategy). Las clases, atributos, métodos y dependencias de todos los diagramas se generan a partir del código fuente.',
    g08_texto: 'El modelo de datos global se presenta por módulos: la base de datos WardrobeFlowDB tiene 31 tablas y en una sola hoja no se leen. Los diagramas se generan a partir del esquema real de la base instalada, con sus claves primarias y foráneas, y coinciden con el script de instalación.'
  }
};
