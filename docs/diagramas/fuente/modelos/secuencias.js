// Diagramas de secuencia (DSS). Cada mensaje corresponde a un método real de la solución
// (pantalla GUI → BLL → DAL). Verificados leyendo el código; ver docs/NEGOCIO_Y_PROCESOS.md.
const A = (id, etiqueta) => ({ id, etiqueta, actor: true });
const P = (id, etiqueta) => ({ id, etiqueta });
const c = (de, a, msg) => ({ de, a, msg });
const r = (de, a, ret) => ({ de, a, ret });
const nota = (texto, ...sobre) => ({ nota: texto, sobre });

module.exports = [
  // ───────────────────────────── N01 — Clientes y suscripciones ─────────────────────────────
  {
    tipo: 'secuencia', id: 'DSS_N01_CU01_GestionarCliente', titulo: 'N01 · CU01-VEN Gestionar Cliente (alta)',
    participantes: [A('V', 'Vendedor'), P('F', 'ClienteForm'), P('B', 'BLL.Cliente'), P('D', 'DAL.Cliente'), P('L', 'Bitácoras')],
    pasos: [
      c('V', 'F', 'Completa los datos del cliente (y "Referido por", opcional)'),
      c('F', 'B', 'Alta(modulo, cliente)'),
      nota('PermisosAccion.Exigir(ClientesEditar) · Validar(cliente)', 'B'),
      c('B', 'D', 'ExisteDNI(dni)'),
      r('D', 'B', 'existe: bool'),
      { alt: 'DNI ya registrado', pasos: [r('B', 'F', 'AppException(dni_duplicado)'), r('F', 'V', 'Informa el error')],
        sino: [{ etiqueta: 'DNI nuevo', pasos: [
          c('B', 'D', 'Alta(cliente)'),
          r('D', 'B', 'idCliente'),
          c('B', 'L', 'Registrar(Alta Cliente) + BitacoraNegocio(AltaCliente)'),
          r('B', 'F', 'cliente registrado'),
          r('F', 'V', 'Confirma el alta (sin plan: la suscripción se contrata en PN02)')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_N01_CU02_RenovarSuscripcion', titulo: 'N01 · CU02-VEN Renovar suscripción (Chain of Responsibility)',
    participantes: [A('V', 'Vendedor'), P('F', 'RenovacionSuscripcionForm'), P('B', 'BLL.Renovacion'), P('H1', 'VerificarVencimientoHandler'), P('H2', 'IntentarRenovarHandler'), P('H3', 'CambioPlan / Pausar / Baja Handler'), P('D', 'DAL.Cliente + DAL.Renovacion')],
    pasos: [
      c('V', 'F', 'Marca la decisión (Renovar, Cambiar plan, Pausar, Baja)'),
      c('F', 'B', 'ObtenerElegibles(decision)'),
      c('B', 'D', 'ObtenerTodos() · TieneContratacionPendiente(idCliente)'),
      r('B', 'F', 'clientes con plan, sin contratación pendiente y en condiciones para esa decisión (más los pausados)'),
      c('V', 'F', 'Elige el cliente'),
      c('F', 'B', 'Procesar(modulo, cliente, decision, idPlanNuevo, modalidad, actor, fechaPausaHasta)'),
      nota('Exigir(ClientesEditar) · el cliente debe tener plan', 'B'),
      c('B', 'H1', 'Procesar(contexto)'),
      { alt: 'Ni vencida ni próxima a vencer (y la decisión no es Pausar)', pasos: [r('H1', 'B', 'Resultado: Pendiente (todavía no corresponde renovar)')],
        sino: [{ etiqueta: 'Vencida, próxima a vencer o decisión Pausar', pasos: [
          c('H1', 'H2', 'Procesar(contexto) del sucesor'),
          { alt: 'Decisión = Renovar', pasos: [
            c('H2', 'D', 'EjecutarTransaccion(ModificarEnTx + AltaEnTx HistorialRenovacion)'),
            r('H2', 'B', 'Resultado: Renovada (nuevo vencimiento por Builder)')
          ], sino: [{ etiqueta: 'Otra decisión', pasos: [
            c('H2', 'H3', 'Procesar(contexto) del sucesor'),
            c('H3', 'D', 'CambiarPlan / Pausar (tope 3 meses, sin prendas en uso) / Baja (exige devolución)'),
            r('H3', 'B', 'Resultado: CambioPlan, Pausada o Baja')
          ] }] }
        ] }] },
      c('B', 'D', 'RecalcularDV() · Bitácoras'),
      r('B', 'F', 'ResultadoRenovacion (mensaje traducible)'),
      r('F', 'V', 'Informa el resultado')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_N01_CU03_CobrarSuscripcion', titulo: 'N01 · CU01-CAJ Cobrar suscripción (cobro recurrente)',
    participantes: [A('V', 'Caja'), P('F', 'CobroSuscripcionForm'), P('B', 'BLL.Cobro'), P('H1', 'DetectarCobroHandler'), P('H2', 'ProcesarPagoHandler'), P('H3', 'AplicarGracia / Suspender Handler'), P('D', 'DAL (Cliente, Cobro, CargoPrenda, Promocion)')],
    pasos: [
      c('F', 'B', 'ObtenerElegibles()'),
      c('B', 'D', 'ObtenerTodos() · TieneContratacionPendiente(idCliente)'),
      r('B', 'F', 'clientes con plan, vencidos o próximos a vencer y sin contratación pendiente'),
      c('V', 'F', 'Elige el cliente'),
      c('F', 'B', 'PrevisualizarCobro(idCliente)'),
      c('B', 'D', 'ObtenerPendientesPorCliente(idCliente)'),
      r('B', 'F', 'cantidad y total de cargos pendientes que sumará el cobro'),
      c('V', 'F', 'Elige modalidad y resultado del cobro (Cobrado o Pago fallido)'),
      c('F', 'B', 'Procesar(modulo, cliente, decision, modalidad, actor)'),
      c('B', 'H1', 'Procesar(contexto)'),
      { alt: 'Todavía no corresponde cobrar', pasos: [r('H1', 'B', 'Resultado: Pendiente')],
        sino: [{ etiqueta: 'Vencida o próxima a vencer', pasos: [
          c('H1', 'H2', 'Procesar(contexto) del sucesor'),
          { alt: 'Decisión = Cobrado', pasos: [
            c('H2', 'D', 'ObtenerVigentes() · ObtenerPendientesPorCliente() (cargos)'),
            nota('Importe = Precio mensual × meses de la modalidad − un solo descuento (promoción o crédito de referido) + cargos', 'H2'),
            c('H2', 'D', 'EjecutarTransaccion: ModificarEnTx + ConsumirCreditoEnTx + AltaEnTx(Cobro) + MarcarCobradosEnTx'),
            alt_cargo(),
            r('H2', 'B', 'Resultado: Cobrado (nuevo vencimiento por Builder)')
          ], sino: [{ etiqueta: 'Decisión = Pago fallido', pasos: [
            c('H2', 'H3', 'Procesar(contexto) del sucesor'),
            c('H3', 'D', 'Primer fallo: gracia de 5 días · vencida la gracia: suspensión'),
            r('H3', 'B', 'Resultado: Gracia o Suspendido')
          ] }] }
        ] }] },
      c('B', 'D', 'RecalcularDV() · Bitácoras'),
      r('B', 'F', 'ResultadoCobro'),
      r('F', 'V', 'Informa el resultado')
    ]
  },

  // ───────────────────────────── PN01 — Armar pedido ─────────────────────────────
  // Sigue el diagrama de actividad de EA (Entrega1.eapx): el Vendedor arma y envía la selección,
  // Depósito (carril "Controlador de Stock") la controla y la separa, y el Vendedor la formaliza.
  // El nombre de cada DSS es exactamente el del caso de uso (casos.js); lo que antes iba entre paréntesis
  // en el título va como nota dentro del diagrama. Los argumentos llevan el nombre del parámetro en el código.
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU01_ArmarPedido', titulo: 'PN01 · CU01-VEN Armar Pedido y Enviar a Control de Stock',
    participantes: [A('V', 'Vendedor'), P('F', 'NuevoPedidoForm'), P('CB', 'BLL.Cliente'), P('B', 'BLL.Pedido'), P('PB', 'BLL.Prenda'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('V', 'F', 'Ingresa la identificación del cliente (DNI, nombre o apellido)'),
      c('F', 'CB', 'BuscarPorIdentificacion(texto)'),
      r('CB', 'F', 'clientes que coinciden (DNI exacto o nombre/apellido parcial)'),
      nota('«include» CU03-VEN Consultar Situación del Cliente: con varias coincidencias el Vendedor elige al cliente', 'V', 'F'),
      c('F', 'B', 'ObtenerTodos()  [último pedido del cliente, para la ficha]'),
      r('B', 'F', 'pedidos'),
      c('F', 'B', 'VerificarVigencia(idCliente)'),
      nota('Exigir(PedidosVentaEditar) · vigente = existe, con plan, sin suspensión por pago, sin pausa y sin vencer', 'B'),
      { alt: 'Suscripción no vigente', pasos: [r('B', 'F', 'AppException(cliente_inexistente / sin_plan / pago_suspendido / suscripcion_pausada / suscripcion_vencida)'), r('F', 'V', 'Aviso de suscripción no vigente (imprimible): no deja avanzar')],
        sino: [{ etiqueta: 'Vigente', pasos: [
          r('B', 'F', 'cliente'),
          c('F', 'B', 'RevisarPedidoActivo(cliente)'),
          { alt: 'Posee pedido activo o prendas sin devolver', pasos: [r('B', 'F', 'AppException(ya_despachado / pedido_activo / cuenta_bloqueada)'), r('F', 'V', 'Aviso de pedido activo (imprimible): no deja avanzar')],
            sino: [{ etiqueta: 'Sin pedido activo', pasos: [
              c('F', 'CB', 'ObtenerEstadoComercial(cliente, prendasSolicitadas)  [prendasSolicitadas = 0]'),
              r('CB', 'F', 'estado comercial (¿vence pronto?)'),
              r('F', 'V', 'Ficha del cliente; aviso si la suscripción está por vencer'),
              c('V', 'F', 'Siguiente (paso 2)'),
              c('F', 'PB', 'ObtenerDisponibles(idClienteSolicitante)'),
              r('PB', 'F', 'Catálogo: prendas Disponibles, sin las reservadas por Lista de Espera para otro'),
              nota('«include» CU02-VEN Consultar Catálogo', 'F', 'PB'),
              { loop: 'Por cada prenda que el Vendedor anota (o quita) de la selección', pasos: [
                c('F', 'CB', 'ObtenerEstadoComercial(cliente, prendasSolicitadas)'),
                r('CB', 'F', 'cupo disponible del plan (¿supera el límite?)'),
                r('F', 'V', 'Detalle de selección (prenda, talle, color, cantidad) y estado del cupo')
              ] },
              { alt: 'Excede el cupo y el cliente no ajusta', pasos: [
                c('V', 'F', 'Registrar desistimiento + motivo'),
                c('F', 'B', 'AsentarDesistimiento(modulo, idCliente, prendas, motivo)'),
                nota('Exigir(PedidosVentaEditar) · motivo obligatorio (desistir_sin_motivo, motivo_largo)', 'B'),
                c('B', 'B', 'ValidarPuedeArmarPedido(idCliente)  [VerificarVigencia + RevisarPedidoActivo]'),
                nota('Solo si la selección excede el cupo; si no: AppException(desistimiento_sin_exceso)', 'B'),
                c('B', 'D', 'AltaSinReserva(pedido)  [Estado = Desistido, EtapaDesistimiento = Cupo]'),
                r('D', 'B', 'idPedido'),
                c('B', 'H', 'RegistrarCambios(cambios)  [Accion = DESISTIR]'),
                r('B', 'F', 'idPedido'),
                r('F', 'V', 'Aviso de desistimiento (PDF)')
              ], sino: [{ etiqueta: 'Dentro del cupo', pasos: [
                c('V', 'F', 'Enviar a control de stock'),
                c('F', 'B', 'EnviarAControlStock(modulo, idCliente, prendas)'),
                c('B', 'B', 'ValidarPuedeArmarPedido(idCliente)  [revalida vigencia y pedido activo]'),
                c('B', 'B', 'ComprobarCupo(cliente, cantidadPrendas)  [cantidadPrendas = prendas.Count]'),
                c('B', 'D', 'AltaSinReserva(pedido)  [Estado = EnControlStock; Pedido + PedidoPrenda, prendas siguen Disponibles]'),
                r('D', 'B', 'idPedido'),
                c('B', 'H', 'RegistrarCambios(cambios)  [Accion = ENVIAR_CONTROL]'),
                r('B', 'F', 'idPedido'),
                r('F', 'V', 'Planilla de control de existencias (PDF): el pedido queda en la cola de Depósito')
              ] }] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU08_ControlarStock', titulo: 'PN01 · CU04-DEP Controlar Stock del Pedido',
    participantes: [A('DP', 'Depósito'), P('F', 'ControlStockForm'), P('EV', 'BLL.EvaluacionControlStock'), P('B', 'BLL.Pedido'), P('PB', 'BLL.Prenda'), P('LE', 'BLL.ListaEspera'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('F', 'B', 'ObtenerColaControlStock()'),
      c('B', 'D', 'ObtenerPorEstado(estado)  [estado = EnControlStock]'),
      r('B', 'F', 'cola: pedidos EnControlStock (del más antiguo al más nuevo)'),
      c('DP', 'F', 'Elige un pedido de la cola'),
      c('F', 'B', 'ObtenerPorId(id)  [relee el pedido; si ya no está EnControlStock recarga la cola]'),
      r('B', 'F', 'pedido'),
      c('F', 'B', 'RevisarStock(pedido)'),
      nota('Exigir(ControlStockEditar) · solo EnControlStock (si no: AppException(control_estado))', 'B'),
      c('B', 'PB', 'VerificarDisponibilidad(seleccion)  [prendas del pedido; relee su estado en la BD]'),
      r('PB', 'B', '(Disponible, NoDisponibles)'),
      { loop: 'Por cada prenda que sigue Disponible', pasos: [
        c('B', 'LE', 'EstaReservadaParaOtro(idPrenda, idClienteSolicitante)'),
        r('LE', 'B', '¿reservada por Lista de Espera para otro cliente?')
      ] },
      r('B', 'F', 'líneas de la planilla (estado actual, reservada, confirmada)'),
      c('F', 'EV', 'Evaluar(lineas, pedido)'),
      r('EV', 'F', '¿Selección disponible? y acciones habilitadas'),
      r('F', 'DP', 'Planilla de control de existencias'),
      { alt: '¿Selección disponible? No', pasos: [
        c('DP', 'F', 'Informar faltantes'),
        c('F', 'B', 'InformarFaltantes(modulo, pedido)'),
        c('B', 'B', 'RevisarStock(pedido)'),
        { loop: 'Por cada prenda no disponible', pasos: [
          c('B', 'PB', 'ObtenerDisponibles(idClienteSolicitante)  [alternativas: misma categoría y talle, máx. 3]')
        ] },
        c('B', 'D', 'RegistrarFaltantes(idPedido, idEmpleadoControl, faltantes)  [→ ConFaltantes]'),
        c('B', 'H', 'RegistrarCambios(cambios)  [Accion = INFORMAR_FALTANTES]'),
        r('B', 'F', 'faltantes con sus alternativas'),
        r('F', 'DP', 'Informe de disponibilidad (PDF) para el Vendedor')
      ], sino: [{ etiqueta: 'Sí', pasos: [
        c('DP', 'F', 'Confirmar prendas disponibles'),
        c('F', 'B', 'ConfirmarPrendasDisponibles(modulo, pedido)'),
        c('B', 'B', 'RevisarStock(pedido)  [si alguna falta: AppException(hay_faltantes)]'),
        c('B', 'D', 'ConfirmarPrendas(idPedido, idEmpleadoControl)'),
        c('B', 'H', 'RegistrarCambios(cambios)  [Accion = CONFIRMAR_PRENDAS]'),
        r('F', 'DP', 'Detalle de prendas confirmadas (PDF)'),
        c('DP', 'F', 'Separar prendas'),
        c('F', 'B', 'SepararPrendas(modulo, pedido)'),
        nota('Exigir(ControlStockEditar) · EnControlStock con todas las líneas confirmadas (control_estado / sin_confirmar)', 'B'),
        c('B', 'B', 'ObtenerClienteValidado(idCliente)  [privado: revalida la vigencia, pudo cambiar en la cola]'),
        { alt: 'Suscripción no vigente', pasos: [r('B', 'F', 'AppException(cliente_inexistente / sin_plan / pago_suspendido / suscripcion_pausada / suscripcion_vencida)'), r('F', 'DP', 'Informa el motivo: no se reserva nada')],
          sino: [{ etiqueta: 'Vigente', pasos: [
            c('B', 'B', 'RevisarStock(pedido)'),
            { alt: 'Todas disponibles', pasos: [
              c('B', 'D', 'SepararPrendas(idPedido, idCliente)  [transacción: prendas a En uso con reclamo, → Separado]'),
              r('D', 'B', 'ok, o AppException(prenda_tomada)  [rollback: no se reserva nada]')
            ] },
            { alt: 'Alguna prenda ya no está disponible (al revisar o prenda_tomada)', pasos: [
              c('B', 'D', 'ObtenerPorId(idPedido)  [pedido releído]'),
              c('B', 'B', 'InformarFaltantes(modulo, pedido)'),
              r('B', 'F', 'AppException(separar_faltantes)'),
              r('F', 'DP', 'Informa que el pedido pasó a faltantes (informe imprimible)')
            ], sino: [{ etiqueta: 'Separadas', pasos: [
              c('B', 'H', 'RegistrarCambios(cambios)  [Accion = SEPARAR]'),
              { loop: 'Por cada prenda del pedido', pasos: [c('B', 'LE', 'CerrarSiReservada(modulo, idPrenda, idCliente, actor)')] },
              r('B', 'F', 'ok'),
              r('F', 'DP', 'Constancia de prendas separadas (PDF)')
            ] }] }
          ] }] }
      ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU09_GestionarFaltantes', titulo: 'PN01 · CU05-VEN Comunicar Faltantes, Ajustar o Desistir',
    participantes: [A('V', 'Vendedor'), P('PV', 'PedidosVenta'), P('NF', 'NuevoPedidoForm'), P('CB', 'BLL.Cliente'), P('PB', 'BLL.Prenda'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('V', 'PV', 'Selecciona un pedido ConFaltantes (Ver faltantes / Ajustar)'),
      c('PV', 'B', 'ObtenerPorId(id)  [relee el pedido]'),
      r('B', 'PV', 'pedido'),
      c('PV', 'B', 'ObtenerInformeFaltantes(idPedido)'),
      c('B', 'D', 'ObtenerFaltantes(idPedido)'),
      r('B', 'PV', 'faltantes con sus alternativas'),
      r('PV', 'V', 'Informe de disponibilidad (pantalla y PDF) para comunicar al cliente'),
      { alt: 'El cliente ajusta la selección', pasos: [
        c('PV', 'NF', 'new NuevoPedidoForm(pedidoConFaltantes, faltantes)  [modo ajuste]'),
        c('NF', 'CB', 'ObtenerPorId(idCliente)'),
        r('CB', 'NF', 'cliente'),
        c('NF', 'PB', 'ObtenerDisponibles(idClienteSolicitante)'),
        r('PB', 'NF', 'catálogo sin los faltantes; alternativas resaltadas primero'),
        { loop: 'Por cada prenda que el Vendedor anota o quita', pasos: [
          c('NF', 'CB', 'ObtenerEstadoComercial(cliente, prendasSolicitadas)'),
          r('CB', 'NF', 'estado del cupo')
        ] },
        { alt: 'Dentro del cupo', pasos: [
          c('V', 'NF', 'Reenviar a control de stock'),
          c('NF', 'B', 'AjustarSeleccion(modulo, pedido, prendas)'),
          nota('Exigir(PedidosVentaEditar) · solo ConFaltantes (ajustar_estado) · vuelve al punto de unión: solo comprueba el cupo', 'B'),
          c('B', 'B', 'ComprobarCupo(cliente, cantidadPrendas)'),
          c('B', 'D', 'ReemplazarSeleccion(idPedido, prendas)  [→ EnControlStock, informe anterior descartado]'),
          c('B', 'H', 'RegistrarCambios(cambios)  [Accion = AJUSTAR_SELECCION]'),
          r('NF', 'V', 'Pedido reenviado a control de stock (planilla PDF)')
        ], sino: [{ etiqueta: 'Excede el cupo y no lo corrige', pasos: [
          c('V', 'NF', 'Registrar desistimiento + motivo'),
          c('NF', 'B', 'AsentarDesistimiento(modulo, pedido, motivo, etapa, seleccionAjustada)  [etapa = Cupo]'),
          c('B', 'D', 'RegistrarDesistimiento(idPedido, motivo, etapa, seleccionAjustada)  [→ Desistido]'),
          c('B', 'H', 'RegistrarCambios(cambios)  [Accion = DESISTIR]'),
          r('NF', 'V', 'Aviso de desistimiento (PDF)')
        ] }] }
      ], sino: [{ etiqueta: 'El cliente desiste', pasos: [
        c('V', 'PV', 'Registrar desistimiento + motivo'),
        c('PV', 'B', 'AsentarDesistimiento(modulo, pedido, motivo, etapa)  [etapa = Disponibilidad]'),
        nota('Exigir(PedidosVentaEditar) · solo ConFaltantes (desistir_estado) · motivo obligatorio (desistir_sin_motivo, motivo_largo)', 'B'),
        c('B', 'D', 'RegistrarDesistimiento(idPedido, motivo, etapa, seleccionAjustada)  [seleccionAjustada = null; → Desistido]'),
        c('B', 'H', 'RegistrarCambios(cambios)  [Accion = DESISTIR]'),
        r('PV', 'V', 'Aviso de desistimiento (PDF)')
      ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU10_FormalizarPedido', titulo: 'PN01 · CU06-VEN Formalizar Pedido',
    participantes: [A('V', 'Vendedor'), P('F', 'PedidosVenta'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      nota('Formaliza el pedido y prepara la confirmación para el cliente', 'V', 'F'),
      c('V', 'F', 'Selecciona un pedido Separado y pulsa Formalizar'),
      c('F', 'B', 'ObtenerPorId(id)  [relee el pedido]'),
      r('B', 'F', 'pedido'),
      c('F', 'B', 'FormalizarPedido(modulo, pedido)'),
      nota('Exigir(PedidosVentaEditar) · solo Separado (formalizar_estado)', 'B'),
      c('B', 'B', 'ObtenerClienteValidado(idCliente)  [privado: revalida la vigencia del cliente]'),
      { alt: 'No está Separado o suscripción no vigente', pasos: [r('B', 'F', 'AppException(formalizar_estado / cliente_inexistente / sin_plan / pago_suspendido / suscripcion_pausada / suscripcion_vencida)'), r('F', 'V', 'Informa el motivo')],
        sino: [{ etiqueta: 'Separado y vigente', pasos: [
          c('B', 'D', 'Formalizar(idPedido)  [→ Pendiente de despacho, selección cerrada]'),
          c('B', 'H', 'RegistrarCambios(cambios)  [Accion = FORMALIZAR]'),
          r('B', 'F', 'ok'),
          c('F', 'B', 'PrepararConfirmacion(modulo, idPedido)'),
          c('B', 'D', 'ObtenerPorId(idPedido)'),
          r('B', 'F', 'pedido formalizado'),
          r('F', 'V', 'Confirmación y constancia del pedido (PDF) para el cliente')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU02_ConsultarCatalogo', titulo: 'PN01 · CU02-VEN Consultar Catálogo',
    participantes: [A('V', 'Vendedor'), P('F', 'NuevoPedidoForm'), P('B', 'BLL.Prenda'), P('LE', 'BLL.ListaEspera'), P('D', 'DAL.Prenda')],
    pasos: [
      c('V', 'F', 'Pasa al paso 2 (Siguiente): pide el catálogo'),
      c('F', 'B', 'ObtenerDisponibles(idClienteSolicitante)'),
      c('B', 'D', 'ObtenerDisponibles(idClienteSolicitante)'),
      r('D', 'B', 'prendas Disponibles'),
      c('B', 'LE', 'ObtenerIdsReservadosParaOtro(idClienteSolicitante)'),
      r('LE', 'B', 'ids reservados por Lista de Espera para otro cliente'),
      r('B', 'F', 'prendas Disponibles sin las reservadas para otro'),
      r('F', 'V', 'Grilla: nombre, categoría, talle y color'),
      { alt: 'No hay prendas disponibles', pasos: [r('F', 'V', 'Muestra la grilla vacía')] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU03_ConsultarSituacionCliente', titulo: 'PN01 · CU03-VEN Consultar Situación del Cliente',
    participantes: [A('V', 'Vendedor'), P('F', 'NuevoPedidoForm'), P('CB', 'BLL.Cliente'), P('B', 'BLL.Pedido'), P('D', 'DAL.Cliente')],
    pasos: [
      c('V', 'F', 'Ingresa DNI, nombre o apellido'),
      c('F', 'CB', 'BuscarPorIdentificacion(texto)'),
      c('CB', 'D', 'ObtenerTodos()'),
      r('D', 'CB', 'clientes (plan, vencimiento, prendas en uso)'),
      r('CB', 'F', 'coincidencias: DNI exacto o nombre/apellido parcial'),
      { alt: 'Ninguna coincidencia', pasos: [r('F', 'V', 'No se encontró ningún cliente con esa identificación')],
        sino: [{ etiqueta: 'Una o varias (el Vendedor elige al cliente)', pasos: [
          c('F', 'B', 'ObtenerTodos()  [último pedido del cliente, para la ficha]'),
          r('B', 'F', 'pedidos'),
          c('F', 'B', 'VerificarVigencia(idCliente)'),
          c('B', 'D', 'ObtenerPorId(idCliente)  [relee al cliente]'),
          r('D', 'B', 'cliente'),
          { alt: 'Suscripción no vigente', pasos: [r('B', 'F', 'AppException(cliente_inexistente / sin_plan / pago_suspendido / suscripcion_pausada / suscripcion_vencida)'), r('F', 'V', 'Ficha + motivo: no se puede continuar (aviso imprimible)')],
            sino: [{ etiqueta: 'Vigente', pasos: [
              r('B', 'F', 'cliente'),
              c('F', 'B', 'RevisarPedidoActivo(cliente)'),
              { alt: 'Posee pedido activo o prendas sin devolver', pasos: [r('B', 'F', 'AppException(ya_despachado / pedido_activo / cuenta_bloqueada)'), r('F', 'V', 'Ficha + pedido activo (aviso imprimible)')],
                sino: [{ etiqueta: 'Sin pedido activo', pasos: [
                  c('F', 'CB', 'ObtenerEstadoComercial(cliente, prendasSolicitadas)  [prendasSolicitadas = 0]'),
                  r('CB', 'F', 'EstadoComercialCliente (próxima a vencer, días hasta el vencimiento)'),
                  r('F', 'V', 'Ficha: plan, límite, vencimiento, prendas en uso y último pedido; aviso si vence pronto')
                ] }] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU04_DespacharPedido', titulo: 'PN01 · CU01-DEP Despachar Pedido',
    participantes: [A('L', 'Depósito / Logística'), P('F', 'PedidosRealizados'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('L', 'F', 'Selecciona un pedido Pendiente y pulsa Despachar'),
      c('F', 'B', 'ObtenerPorId(id)  [relee el estado actual]'),
      r('B', 'F', 'pedido'),
      c('F', 'B', 'Despachar(modulo, pedido)'),
      nota('Exigir(PedidosRealizadosEditar) · pedido.PuedeDespachar()', 'B'),
      { alt: 'El pedido no está Pendiente', pasos: [r('B', 'F', 'AppException(despachar_estado)'), r('F', 'L', 'Informa el estado actual')],
        sino: [{ etiqueta: 'Pendiente', pasos: [
          c('B', 'D', 'Despachar(idPedido)'),
          c('B', 'H', 'RegistrarCambios(cambios)  [Accion = DESPACHAR]'),
          r('B', 'F', 'ok'),
          r('F', 'L', 'Pedido Despachado')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU05_RegistrarEntrega', titulo: 'PN01 · CU02-DEP Registrar Entrega',
    participantes: [A('L', 'Depósito / Logística'), P('F', 'PedidosRealizados'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('L', 'F', 'Selecciona un pedido Despachado y pulsa Marcar Entregado'),
      c('F', 'B', 'ObtenerPorId(id)  [relee el estado actual]'),
      r('B', 'F', 'pedido'),
      c('F', 'B', 'MarcarEntregado(modulo, pedido)'),
      nota('Exigir(PedidosRealizadosEditar) · pedido.PuedeEntregarse()', 'B'),
      { alt: 'El pedido no está Despachado', pasos: [r('B', 'F', 'AppException(entregar_estado)'), r('F', 'L', 'Informa el estado actual')],
        sino: [{ etiqueta: 'Despachado', pasos: [
          c('B', 'D', 'MarcarEntregado(idPedido)'),
          c('B', 'H', 'RegistrarCambios(cambios)  [Accion = ENTREGAR]'),
          r('B', 'F', 'ok'),
          r('F', 'L', 'Pedido Entregado')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU06_RegistrarDevolucion', titulo: 'PN01 · CU03-DEP Registrar Devolución',
    participantes: [A('L', 'Depósito / Logística'), P('F', 'PedidosRealizados'), P('I', 'InvocadorPedido'), P('K', 'DevolucionCommand'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      nota('Patrón Command: PedidosRealizados (Client) arma DevolucionCommand y se lo entrega a InvocadorPedido (Invoker); BLL.Pedido es el Receiver', 'F', 'I', 'K'),
      c('L', 'F', 'Selecciona un pedido Entregado y pulsa Registrar Devolución'),
      c('F', 'B', 'ObtenerPorId(id)  [relee el pedido completo]'),
      r('B', 'F', 'pedido'),
      c('F', 'I', 'TomarOrden(new DevolucionCommand(receptor, pedido, modulo))'),
      c('F', 'I', 'ProcesarOrdenes()'),
      c('I', 'K', 'Ejecutar()'),
      c('K', 'B', 'RegistrarDevolucion(modulo, pedido)'),
      nota('Exigir(PedidosRealizadosEditar)', 'B'),
      { alt: 'No está Entregado', pasos: [r('B', 'F', 'AppException(devolucion_estado)'), r('F', 'L', 'Informa el estado actual')],
        sino: [{ etiqueta: 'Entregado', pasos: [
          c('B', 'D', 'RegistrarDevolucion(idPedido, idCliente)'),
          nota('Una transacción: abre MantenimientoPrenda y pasa a En limpieza solo las prendas que siguen En uso por ese cliente', 'D'),
          r('D', 'B', 'cantidad de prendas devueltas'),
          { alt: 'Ninguna prenda devuelta (ya registrada)', pasos: [r('B', 'F', 'AppException(devolucion_ya_hecha)'), r('F', 'L', 'Informa que la devolución ya estaba registrada')],
            sino: [{ etiqueta: 'Devolución registrada', pasos: [
              c('B', 'H', 'RegistrarCambios(cambios)  [Accion = DEVOLUCION]'),
              r('B', 'F', 'ok'),
              r('F', 'L', 'Prendas en limpieza (PN04 las inspecciona)'),
              nota('Desbloquea la cuenta: sin prendas En uso, RevisarPedidoActivo ya no lanza cuenta_bloqueada y el cliente puede armar otro pedido', 'B')
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU07_CancelarPedido', titulo: 'PN01 · CU04-VEN Cancelar Pedido',
    participantes: [A('V', 'Vendedor'), P('F', 'PedidosVenta'), P('I', 'InvocadorPedido'), P('K', 'CancelacionCommand'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      nota('Patrón Command: PedidosVenta (Client) arma CancelacionCommand y se lo entrega a InvocadorPedido (Invoker); BLL.Pedido es el Receiver', 'F', 'I', 'K'),
      c('V', 'F', 'Selecciona un pedido Pendiente o Separado y pulsa Cancelar'),
      c('F', 'B', 'ObtenerPorId(id)  [relee el estado actual]'),
      r('B', 'F', 'pedido'),
      c('V', 'F', 'Indica el motivo y confirma'),
      c('F', 'I', 'TomarOrden(new CancelacionCommand(receptor, pedido, modulo, motivo))'),
      c('F', 'I', 'ProcesarOrdenes()'),
      c('I', 'K', 'Ejecutar()'),
      c('K', 'B', 'Cancelar(modulo, pedido, motivo)'),
      nota('Exigir(PedidosVentaEditar) · pedido.PuedeCancelarse(): Pendiente o Separado · motivo obligatorio (hasta 500 caracteres)', 'B'),
      { alt: 'No está Pendiente ni Separado o falta el motivo', pasos: [r('B', 'F', 'AppException(cancelar_estado_separado / cancelar_sin_motivo / motivo_largo)'), r('F', 'V', 'Informa el error')],
        sino: [{ etiqueta: 'Válido', pasos: [
          c('B', 'D', 'Cancelar(idPedido, idCliente, estadoEsperado, motivo)  [libera las prendas a Disponible]'),
          c('B', 'H', 'RegistrarCambios(cambios)  [Accion = CANCELAR]'),
          r('B', 'F', 'ok'),
          r('F', 'V', 'Pedido Cancelado: prendas liberadas'),
          nota('Reactivar (DesCancelar, otra acción): vuelve a EnControlStock revalidando vigencia, pedido activo y cupo; Depósito revisa el stock de nuevo', 'B')
        ] }] }
    ]
  },

  // ───────────────────────────── PN02 — Comercialización de la suscripción ─────────────────────────────
  // Sigue el flujo aprobado (adaptado a WardrobeFlow): Vendedor identifica, presenta planes y
  // registra la contratación; Caja calcula, cobra o registra intentos (al tercero se cancela).
  // CU02-VTA Asentar Desistimiento («extend» de CU01-VTA) tiene su propio DSS (EA-generador/dss-extra.js).
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU01_VTA_GestionarSuscripcion', titulo: 'PN02 · CU01-VTA Gestionar Suscripción',
    participantes: [A('V', 'Vendedor'), P('F', 'NuevaContratacionForm'), P('B', 'BLL.Contratacion'), P('CB', 'BLL.Cliente'), P('D', 'DAL.Contratacion')],
    pasos: [
      nota('Contratación: el Vendedor la registra Pendiente de pago; la suscripción se activa cuando Caja cobra (CU01-CAJ Gestionar Cobro)', 'V', 'F'),
      c('V', 'F', 'Abre Nueva contratación'),
      c('F', 'B', 'PresentarPlanes()'),
      r('B', 'F', 'Planes disponibles (nombre, precio mensual, límite de prendas)'),
      c('V', 'F', 'Ingresa DNI, nombre o apellido del cliente'),
      c('F', 'B', 'IdentificarCliente(identificacion)'),
      c('B', 'CB', 'BuscarPorIdentificacion(texto)'),
      r('CB', 'B', 'clientes que coinciden'),
      r('B', 'F', 'List<Cliente>  [vacía = no registrado]'),
      { alt: '¿Registrado? No', pasos: [
        r('F', 'V', 'El cliente no está registrado: Registrar cliente'),
        c('V', 'F', 'Carga los datos del cliente (referente opcional)'),
        c('F', 'CB', 'Alta(modulo, cliente)'),
        r('CB', 'F', 'ok'),
        c('F', 'B', 'IdentificarCliente(identificacion)  [busca por el DNI registrado]'),
        r('B', 'F', 'List<Cliente>')
      ] },
      c('V', 'F', 'Elige el cliente, el plan y la modalidad'),
      c('F', 'B', 'ValidarContratacion(idCliente, idPlan)'),
      { alt: '¿Contratación válida? No', pasos: [r('B', 'F', 'AppException(cliente_inexistente / plan_inexistente / plan_insuficiente / pendiente_existente)'), r('F', 'V', 'Informar motivo (Registrar queda deshabilitado)')],
        sino: [{ etiqueta: 'Sí', pasos: [r('B', 'F', 'plan')] }] },
      c('F', 'B', 'EstimarImporte(idCliente, idPlan, modalidad)'),
      r('B', 'F', 'liquidación estimada (un solo descuento)'),
      r('F', 'V', 'Importe a abonar en Caja'),
      nota('¿Elige plan y modalidad? No → «extend» CU02-VTA Asentar Desistimiento (botón Desistir)', 'V', 'F'),
      c('V', 'F', 'Registrar contratación'),
      c('F', 'B', 'RegistrarContratacion(modulo, idCliente, idPlan, modalidad)'),
      nota('Exigir(ClientesEditar)', 'B'),
      c('B', 'B', 'ValidarContratacion(idCliente, idPlan)  [revalida]'),
      { alt: 'No válida', pasos: [
        r('B', 'F', 'AppException(cliente_inexistente / plan_inexistente / plan_insuficiente / pendiente_existente)  [queda en la bitácora]'),
        r('F', 'V', 'Informar motivo: el formulario sigue abierto y vuelve a validar')
      ], sino: [{ etiqueta: 'Válida', pasos: [
        c('B', 'D', 'Alta(contratacion)  [Estado = PendientePago, PrecioMensual = precio del plan pactado]'),
        r('D', 'B', 'idContratacion'),
        r('B', 'F', 'idContratacion'),
        c('F', 'B', 'ObtenerPorId(idContratacion)'),
        r('B', 'F', 'contratación'),
        c('F', 'B', 'CalcularImporte(contratacion)'),
        r('B', 'F', 'liquidación'),
        r('F', 'V', 'Orden de cobro (PDF): el cliente abona en Caja')
      ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU01_CAJ_GestionarCobro', titulo: 'PN02 · CU01-CAJ Gestionar Cobro',
    participantes: [A('C', 'Caja'), P('F', 'ContratacionesPendientesForm'), P('B', 'BLL.Contratacion'), P('PD', 'PoliticaDescuento'), P('PC', 'PoliticaCuotas'), P('CB', 'BLL.Cliente'), P('D', 'DAL.Contratacion')],
    pasos: [
      nota('«include» CU02-CAJ Emitir Comprobante: se ejecuta dentro de ConfirmarCobro · «extend» CU04-CAJ Financiar en Cuotas: con tarjeta de crédito', 'B'),
      c('F', 'B', 'ObtenerPendientesDePago()  [Consultar cola]'),
      c('B', 'D', 'ObtenerPendientesDePago()'),
      r('B', 'F', 'contrataciones Pendientes de pago (de la más antigua a la más nueva)'),
      c('F', 'B', 'CalcularImportes(contrataciones)'),
      { loop: 'Por cada contratación de la cola', pasos: [
        c('B', 'PD', 'Resolver(bruto, idPlan, promocionesDelPlan, creditoReferido, meses)  [bruto = PrecioMensual pactado × meses]')
      ] },
      r('B', 'F', 'importe de cada una: un solo descuento (el mayor)'),
      c('C', 'F', 'Elige la contratación y el medio de pago'),
      { opt: 'Tarjeta de crédito: «extend» CU04-CAJ Financiar en Cuotas', pasos: [
        c('F', 'B', 'ObtenerPlanesCuotas(modalidad)'),
        c('B', 'D', 'ObtenerPlanesCuotas()'),
        r('D', 'B', 'planes de cuotas'),
        c('B', 'PC', 'Disponibles(planes, modalidad)'),
        r('B', 'F', 'planes de hasta los meses de la modalidad'),
        c('C', 'F', 'Elige la cantidad de cuotas')
      ] },
      c('C', 'F', 'Pulsa Cobrar'),
      c('F', 'B', 'CalcularImporte(contratacion, idMedioPago, idPlanCuotas)'),
      c('B', 'PD', 'Resolver(bruto, idPlan, promocionesDelPlan, creditoReferido, meses)'),
      c('B', 'B', 'ResolverCuotas(medio, idPlanCuotas, modalidad)'),
      c('B', 'PC', 'Financiar(total, plan)'),
      r('B', 'F', 'Liquidación con el detalle de financiación'),
      c('C', 'F', 'El cliente abona: confirma el importe'),
      c('F', 'B', 'ConfirmarCobro(modulo, contratacion, idMedioPago, importeConfirmado, idPlanCuotas)  [importeConfirmado = Total de la Liquidación]'),
      nota('Exigir(CajaEditar) · medio de pago del catálogo · quien registró la contratación no la cobra, salvo el Administrador', 'B'),
      c('B', 'D', 'ObtenerPorId(idContratacion)  [relee el estado]'),
      r('D', 'B', 'contratación actual'),
      c('B', 'B', 'ResolverCuotas(medio, idPlanCuotas, modalidad)'),
      c('B', 'PD', 'Resolver(bruto, idPlan, promocionesDelPlan, creditoReferido, meses)  [vuelve a liquidar]'),
      { alt: 'No se puede cobrar o cambió el importe', pasos: [
        r('B', 'F', 'AppException(cobrar_estado / plan_baja / plan_insuficiente / cobra_el_vendedor / importe_cambiado / cuotas_medio / cuotas_invalidas / cuotas_modalidad)'),
        r('F', 'C', 'Informa el motivo; si cambió el importe: volver a Calcular importe y confirmar')
      ], sino: [{ etiqueta: 'Mismo importe', pasos: [
        c('B', 'PC', 'Financiar(total, plan)  [recargo y valor de cuota]'),
        c('B', 'B', 'EmitirComprobante(idContratacion)  [privado: → numeroComprobante; CU02-CAJ]'),
        c('B', 'D', 'ConfirmarCobro(idContratacion, idCaja, idMedioPago, numeroComprobante, importe, descuento, idPromocion, idPlanCuotas, recargoCuotas)  [claim: WHERE Estado = PendientePago]'),
        { alt: 'false: otra sesión ya la resolvió', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(cobrar_concurrente)')],
          sino: [{ etiqueta: 'true', pasos: [
            r('D', 'B', 'true'),
            c('B', 'CB', 'ActivarSuscripcionDesdeContratacion(modulo, cliente, idPlan, modalidad, consumoCredito)  [Builder; + crédito al referente]'),
            { alt: 'Falla la activación', pasos: [
              c('B', 'D', 'ReabrirPago(idContratacion)  [compensación: vuelve a Pendiente de pago]'),
              r('B', 'F', 'relanza el error (o AppException(cobro_sin_activar) si no pudo reabrir)')
            ], sino: [{ etiqueta: 'Activada', pasos: [
              c('B', 'D', 'RegistrarVigencia(idContratacion, desde, hasta, idReferenteAcreditado)'),
              r('B', 'F', 'Liquidación con comprobante, vigencia y referente acreditado'),
              r('F', 'C', 'Comprobante (con las cuotas y el recargo) y Constancia de suscripción (PDF)')
            ] }] }
          ] }] }
      ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU02_CAJ_EmitirComprobante', titulo: 'PN02 · CU02-CAJ Emitir Comprobante',
    participantes: [A('C', 'Caja'), P('F', 'ContratacionesPendientesForm'), P('B', 'BLL.Contratacion'), P('D', 'DAL.Contratacion')],
    pasos: [
      nota('Incluido en CU01-CAJ Gestionar Cobro: el comprobante se emite dentro de ConfirmarCobro', 'C', 'F'),
      c('C', 'F', 'Confirma el cobro'),
      c('F', 'B', 'ConfirmarCobro(modulo, contratacion, idMedioPago, importeConfirmado, idPlanCuotas)'),
      c('B', 'B', 'EmitirComprobante(idContratacion)  [privado: → numeroComprobante = CMP-{id:D6}-{yyyyMMdd}]'),
      c('B', 'D', 'ConfirmarCobro(idContratacion, idCaja, idMedioPago, numeroComprobante, importe, descuento, idPromocion, idPlanCuotas, recargoCuotas)  [el número se guarda en el mismo UPDATE del cobro]'),
      { alt: 'false', pasos: [r('D', 'B', 'false  [otra sesión ya la resolvió]'), r('B', 'F', 'AppException(cobrar_concurrente)'), r('F', 'C', 'Informa: actualizar la cola')],
        sino: [{ etiqueta: 'true', pasos: [
          r('D', 'B', 'true'),
          nota('Sigue la activación de la suscripción (CU01-CAJ Gestionar Cobro)', 'B'),
          r('B', 'F', 'Liquidación con NumeroComprobante'),
          c('C', 'F', 'Imprimir el comprobante'),
          c('F', 'B', 'ObtenerPorId(idContratacion)'),
          r('B', 'F', 'contratación Pagada (número y fecha del comprobante)'),
          r('F', 'C', 'Comprobante (PDF) armado por la pantalla; reimprimible desde la vista Resueltas')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU04_CAJ_FinanciarEnCuotas', titulo: 'PN02 · CU04-CAJ Financiar en Cuotas',
    // «extend» de CU01-CAJ Gestionar Cobro: con Tarjeta de crédito (MedioPago.PermiteCuotas) la pantalla ofrece los planes
    // de cuotas de la modalidad (GUI/ContratacionesPendientesForm.cs › CargarCuotas / ActualizarDetalleCuotas) y el cobro
    // registra el plan y el recargo (BLL/Contratacion.cs › ResolverCuotas, ConfirmarCobro; BE/PlanCuotas.cs › PoliticaCuotas).
    participantes: [A('C', 'Caja'), P('F', 'ContratacionesPendientesForm'), P('B', 'BLL.Contratacion'), P('PC', 'PoliticaCuotas'), P('D', 'DAL.Contratacion')],
    pasos: [
      nota('Punto de extensión de CU01-CAJ: el medio de pago elegido es Tarjeta de crédito (PermiteCuotas)', 'C', 'F'),
      c('C', 'F', 'Elige Tarjeta de crédito como medio de pago'),
      c('F', 'B', 'ObtenerPlanesCuotas(modalidad)'),
      c('B', 'D', 'ObtenerPlanesCuotas()'),
      r('D', 'B', 'planes de cuotas'),
      c('B', 'PC', 'Disponibles(planes, modalidad)'),
      nota('Solo planes activos y sin superar los meses de la modalidad: Mensual 1, Trimestral hasta 3, Anual hasta 12', 'PC'),
      r('PC', 'B', 'planes disponibles'),
      r('B', 'F', 'planes de cuotas'),
      r('F', 'C', 'Planes de cuotas (1 sin interés; 3: 5 %; 6: 10 %; 12: 20 %)'),
      c('C', 'F', 'Elige la cantidad de cuotas'),
      c('F', 'PC', 'Financiar(total, plan)'),
      r('PC', 'F', 'detalle de financiación'),
      r('F', 'C', 'Valor de cuota, recargo y total a abonar'),
      c('C', 'F', 'Pulsa Cobrar'),
      c('F', 'B', 'ConfirmarCobro(modulo, contratacion, idMedioPago, importeConfirmado, idPlanCuotas)'),
      c('B', 'B', 'ResolverCuotas(medio, idPlanCuotas, modalidad)'),
      c('B', 'D', 'ObtenerPlanesCuotas()'),
      r('D', 'B', 'planes de cuotas'),
      { alt: 'El medio no financia, el plan no existe o supera los meses de la modalidad', pasos: [
          r('B', 'F', 'AppException(cuotas_medio / cuotas_invalidas / cuotas_modalidad)'),
          r('F', 'C', 'Informa el motivo: no se cobra')
        ], sino: [{ etiqueta: 'Plan válido', pasos: [
          c('B', 'PC', 'Financiar(total, plan)'),
          r('PC', 'B', 'recargo y valor de cuota'),
          c('B', 'D', 'ConfirmarCobro(idContratacion, idCaja, idMedioPago, numeroComprobante, importe, descuento, idPromocion, idPlanCuotas, recargoCuotas)'),
          r('D', 'B', 'true'),
          nota('Sigue la emisión del comprobante y la activación de la suscripción (CU01-CAJ / CU02-CAJ)', 'B'),
          r('B', 'F', 'Liquidación con cuotas, recargo y valor de cuota'),
          r('F', 'C', 'Comprobante (PDF) con las cuotas y el recargo')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU03_CAJ_CancelarContratacion', titulo: 'PN02 · CU03-CAJ Registrar Intento y Cancelar Contratación',
    participantes: [A('C', 'Caja'), P('F', 'ContratacionesPendientesForm'), P('B', 'BLL.Contratacion'), P('D', 'DAL.Contratacion')],
    pasos: [
      nota('Máximo de 3 intentos (Contratacion.MaxIntentosPago): al tercero la contratación se cancela', 'B'),
      c('C', 'F', 'El pago no se concretó: medio intentado + motivo'),
      c('F', 'B', 'RegistrarIntentoFallido(modulo, contratacion, idMedioPago, motivo)'),
      nota('Exigir(CajaEditar)', 'B'),
      c('B', 'D', 'ObtenerPorId(idContratacion)  [relee el estado]'),
      r('D', 'B', 'contratación actual'),
      { alt: 'No está pendiente o falta el motivo', pasos: [r('B', 'F', 'AppException(cobrar_estado / intento_sin_motivo / motivo_largo)'), r('F', 'C', 'Informa el error')],
        sino: [{ etiqueta: 'Válido', pasos: [
          c('B', 'D', 'RegistrarIntentoFallido(idContratacion, idMedioPago, motivo, idCaja, maximo)  [maximo = MaxIntentosPago = 3; transacción con bloqueo de fila]'),
          { alt: 'Ya no está pendiente', pasos: [r('D', 'B', 'null'), r('B', 'F', 'AppException(cobrar_concurrente)')],
            sino: [{ etiqueta: 'Intento registrado', pasos: [
              r('D', 'B', 'resultado (NroIntento, Maximo, Cancelada)'),
              r('B', 'F', 'resultado (NroIntento, Maximo, Cancelada)'),
              { alt: '¿Alcanzó el máximo? Sí (Cancelada)', pasos: [r('F', 'C', 'Contratación cancelada: Constancia de cancelación (PDF)')],
                sino: [{ etiqueta: 'No', pasos: [r('F', 'C', 'Intento N de 3 registrado: sigue en la cola para volver a cobrarla')] }] }
            ] }] }
        ] }] }
    ]
  },

  // ───────────────────────────── PN03 — Métricas, promociones y toma de decisiones ─────────────────────────────
  // Sigue el flujo aprobado: cada mensaje a la BLL es un método por actividad; cada transición es un
  // claim (UPDATE ... WHERE Estado = esperado) que inserta su fila de PromocionHistorial en la misma transacción.
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_GER_SugerirPromocion', titulo: 'PN03 · CU01-GER Sugerir Promoción',
    participantes: [A('G', 'Gerencia'), P('F', 'SugerirPromocionForm'), P('AN', 'BLL.AnalisisPromociones'), P('AR', 'BLL.AnalisisRotacion'), P('AA', 'BLL.AnalisisAbandono'),
                    P('DP', 'DAL.PlanSuscripcion'), P('B', 'BLL.SugerenciaPromocion'), P('D', 'DAL.SugerenciaPromocion')],
    pasos: [
      nota('«extend» CU03-GER Analizar Métricas (opcional): AnalizarMetricas y ¿Hay oportunidad? (HayOportunidad). Sin análisis, la sugerencia queda con origen Manual', 'G', 'F'),
      { opt: 'Gerencia analiza las métricas (opcional)', pasos: [
        c('G', 'F', 'Pulsa "Analizar métricas…"'),
        c('F', 'AN', 'AnalizarMetricas(modulo)'),
        nota('PermisosAccion.Exigir(SugerenciaPromocion, SugerenciaPromocion)', 'AN'),
        c('AN', 'AR', 'Detectar()'),
        r('AR', 'AN', 'List<RotacionPrenda>'),
        c('AN', 'AA', 'Detectar()  [criterio por defecto: EstrategiaVencimientoInactividad]'),
        r('AA', 'AN', 'List<ClienteEnRiesgo>'),
        c('AN', 'DP', 'ObtenerTodos()'),
        r('DP', 'AN', 'planes'),
        c('AN', 'AN', 'AbandonoPorPlan(enRiesgo, planes)'),
        c('AN', 'AN', 'Oportunidades(rot, ab, planes)  [rotación: ≥ 2 prendas sin pedidos por categoría; abandono: clientes en riesgo por plan]'),
        nota('bitacora.Registrar(modulo, …): cantidad de oportunidades o "sin oportunidad de promoción"', 'AN'),
        r('AN', 'F', 'ReporteMetricas (abandono por plan, rotación por categoría, oportunidades)'),
        c('F', 'AN', 'HayOportunidad(reporte)'),
        r('AN', 'F', 'hayOportunidad'),
        c('F', 'F', 'MostrarReporte(reporte, hayOportunidad)'),
        r('F', 'G', '«Reporte de métricas» en pantalla (imprimible) con las oportunidades para elegir'),
        c('G', 'F', 'Pulsa "Usar esta idea" sobre una oportunidad, o Cerrar'),
        c('F', 'AN', 'HayOportunidad(reporte)  [¿Hay oportunidad?]'),
        { alt: '¿Hay oportunidad? No', pasos: [r('F', 'G', '"Los reportes de rotación y abandono no detectan casos": fin sin promoción')],
          sino: [{ etiqueta: 'Sí', pasos: [
            { opt: 'Eligió una oportunidad', pasos: [r('F', 'G', 'Precarga destino (plan o categoría), tipo, beneficio estimado y motivo; origen = Abandono o Rotación')] }
          ] }] }
      ] },
      nota('Sin usar el reporte (o sin elegir una oportunidad) Gerencia carga los datos a mano: el origen queda Manual', 'G', 'F'),
      c('G', 'F', 'Ajusta destino (plan o categoría), motivo, tipo de descuento y beneficio estimado; pulsa "Enviar Sugerencia"'),
      c('F', 'B', 'RegistrarSugerencia(modulo, origen, idPlan, categoriaPrenda, motivo, tipoSugerido, beneficioEstimado)  [origen = Abandono, Rotación o Manual]'),
      nota('PermisosAccion.Exigir(SugerenciaPromocion, SugerenciaPromocion)', 'B'),
      { alt: 'Plan y categoría a la vez, o ninguno', pasos: [r('B', 'F', 'AppException(destino_invalido)'), r('F', 'G', 'Informa el motivo y retoma la carga')],
        sino: [{ etiqueta: 'Destino único', pasos: [
          { opt: 'Aplica a un plan', pasos: [c('B', 'DP', 'ObtenerPorId(idPlan)'), r('DP', 'B', 'plan (null si no existe)')] },
          { alt: 'Plan inexistente, sin motivo o beneficio ≤ 0', pasos: [r('B', 'F', 'AppException(plan_inexistente / motivo_requerido / beneficio_invalido)'), r('F', 'G', 'Informa el motivo y retoma la carga')],
            sino: [{ etiqueta: 'Válidos', pasos: [
              c('B', 'D', 'Alta(sugerencia)  [Estado = Pendiente, OrigenMetrica = origen, IdUsuarioAlta = usuario de la sesión]'),
              r('D', 'B', 'idNuevo'),
              nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Venta, …)', 'B'),
              r('B', 'F', 'id de la sugerencia'),
              r('F', 'G', '"Sugerencia #id enviada a Administración"'),
              c('F', 'B', 'ObtenerTodas()  [refresca las sugerencias registradas]'),
              { opt: '¿Imprimir la sugerencia? Sí', pasos: [c('F', 'B', 'ObtenerPorId(idSugerencia)'), r('B', 'F', 'sugerencia'), r('F', 'G', 'Sugerencia de promoción (PDF)')] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_ADM_GestionarPromociones', titulo: 'PN03 · CU01-ADM Gestionar Promociones',
    participantes: [A('A', 'Administración'), P('F', 'PromocionesAdministracionForm'), P('AF', 'AltaPromocionForm'), P('BS', 'BLL.SugerenciaPromocion'), P('B', 'BLL.Promocion'),
                    P('DS', 'DAL.SugerenciaPromocion'), P('D', 'DAL.Promocion')],
    pasos: [
      nota('Crear desde sugerencia o manual, y reformular · «extend» CU03-ADM (¿Acepta la sugerencia? No) y CU04-ADM (¿Reformular? No)', 'A', 'F'),
      c('A', 'F', 'Abre Gestión de Promociones (o pulsa Actualizar)'),
      c('F', 'BS', 'ObtenerPendientes()'),
      c('BS', 'DS', 'ObtenerPendientes()'),
      r('BS', 'F', 'sugerencias Pendientes de Gerencia'),
      c('F', 'B', 'ObtenerTodas()  [antes cierra las vencidas, ver CU05-ADM]'),
      r('B', 'F', 'promociones'),
      r('F', 'A', 'Sugerencias pendientes y promociones (acciones habilitadas según el estado)'),
      { alt: '¿Acepta la sugerencia? Sí: "Alta desde Sugerencia"', pasos: [
        c('F', 'AF', 'new AltaPromocionForm(sugerenciaOrigen, promocionAReformular)  [sugerencia elegida, null]'),
        r('AF', 'A', 'Destino de la sugerencia (bloqueado), tipo sugerido y valor inicial precargados'),
        c('A', 'AF', 'Completa nombre, descripción, tipo, valor, vigencia, margen e impacto; pulsa Registrar'),
        c('AF', 'B', 'CrearDesdeSugerencia(modulo, idSugerencia, nombre, descripcion, tipo, valor, fechaInicio, fechaFin, margenEstimado, impactoEconomico)'),
        nota('PermisosAccion.Exigir(PromocionesAdminEditar, PromocionesAdmin)', 'B'),
        c('B', 'DS', 'ObtenerPorId(idSugerencia)'),
        r('DS', 'B', 'sugerencia'),
        { alt: 'No existe, o ya no está Pendiente (PuedeEvaluarse / TransicionValida(Evaluada))', pasos: [r('B', 'AF', 'AppException(sugerencia_inexistente / sugerencia_evaluada)')],
          sino: [{ etiqueta: 'Pendiente', pasos: [
            c('B', 'B', 'ValidarPromocion(promocion)  [armada con el destino de la sugerencia, EnRevisionContable]'),
            { alt: 'Datos inválidos', pasos: [r('B', 'AF', 'AppException(nombre_requerido / destino_invalido / plan_inexistente / valor_invalido / porcentaje_invalido / rango_fechas_invalido)')],
              sino: [{ etiqueta: 'Válidos', pasos: [
                c('B', 'DS', 'MarcarEvaluada(idSugerencia, fecha)  [claim: WHERE Estado = Pendiente]'),
                { alt: 'false: otra sesión ya la evaluó', pasos: [r('DS', 'B', 'false'), r('B', 'AF', 'AppException(sugerencia_evaluada)')],
                  sino: [{ etiqueta: 'true', pasos: [
                    r('DS', 'B', 'true'),
                    c('B', 'B', 'Registrar(modulo, promocion, observacion)  [observacion = "Alta desde la sugerencia #id"]'),
                    c('B', 'D', 'Alta(promocion, historial)  [IdUsuarioAlta = usuario de la sesión; historial: — → EnRevisionContable]'),
                    { alt: 'Registrar lanza una excepción (p. ej. falla el alta)', pasos: [c('B', 'DS', 'ReabrirEvaluacion(idSugerencia)  [compensación: vuelve a Pendiente]'), r('B', 'AF', 'relanza la excepción')],
                      sino: [{ etiqueta: 'Alta registrada', pasos: [r('D', 'B', 'idNuevo'), nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Venta, …)', 'B'), r('B', 'AF', 'idPromocion')] }] }
                  ] }] }
              ] }] }
          ] }] }
      ], sino: [
        { etiqueta: 'Alta manual: "Alta Manual"', pasos: [
          c('F', 'AF', 'new AltaPromocionForm(sugerenciaOrigen, promocionAReformular)  [null, null]'),
          c('A', 'AF', 'Elige el destino (plan o categoría), completa las condiciones y pulsa Registrar'),
          c('AF', 'B', 'CrearManual(modulo, nombre, descripcion, tipo, valor, fechaInicio, fechaFin, idPlan, categoriaPrenda, margenEstimado, impactoEconomico)'),
          nota('PermisosAccion.Exigir(PromocionesAdminEditar, PromocionesAdmin)', 'B'),
          c('B', 'B', 'ValidarPromocion(promocion)'),
          { alt: 'Datos inválidos', pasos: [r('B', 'AF', 'AppException(nombre_requerido / destino_invalido / plan_inexistente / valor_invalido / porcentaje_invalido / rango_fechas_invalido)')],
            sino: [{ etiqueta: 'Válidos', pasos: [
              c('B', 'B', 'Registrar(modulo, promocion, observacion)  [observacion = "Alta manual"]'),
              c('B', 'D', 'Alta(promocion, historial)  [historial: — → EnRevisionContable]'),
              r('D', 'B', 'idNuevo'),
              r('B', 'AF', 'idPromocion')
            ] }] }
        ] },
        { etiqueta: '¿Reformular? Sí: elige una Rechazada por Contabilidad y pulsa "Reformular"', pasos: [
          c('F', 'AF', 'new AltaPromocionForm(sugerenciaOrigen, promocionAReformular)  [null, promoción elegida]'),
          r('AF', 'A', 'Condiciones actuales y observación de Contabilidad (destino bloqueado)'),
          c('A', 'AF', 'Corrige las condiciones y pulsa Registrar'),
          c('AF', 'B', 'Reformular(modulo, promocion)'),
          nota('PermisosAccion.Exigir(PromocionesAdminEditar, PromocionesAdmin)', 'B'),
          { alt: 'No está Rechazada por Contabilidad (PuedeReformularse / TransicionValida(EnRevisionContable))', pasos: [r('B', 'AF', 'AppException(reformular_estado)')],
            sino: [{ etiqueta: 'Rechazada', pasos: [
              c('B', 'B', 'ValidarPromocion(promocion)'),
              { alt: 'Datos inválidos', pasos: [r('B', 'AF', 'AppException(nombre_requerido / destino_invalido / plan_inexistente / valor_invalido / porcentaje_invalido / rango_fechas_invalido)')],
                sino: [{ etiqueta: 'Válidos', pasos: [
                  c('B', 'D', 'Reformular(promocion, historial)  [claim: WHERE Estado = RechazadaContabilidad; historial: RechazadaContabilidad → EnRevisionContable]'),
                  { alt: 'false: otra sesión ya la cambió', pasos: [r('D', 'B', 'false'), r('B', 'AF', 'AppException(estado_concurrente)')],
                    sino: [{ etiqueta: 'true', pasos: [r('D', 'B', 'true'), nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Venta, …)', 'B'), r('B', 'AF', 'ok')] }] }
                ] }] }
            ] }] }
        ] }
      ] },
      nota('Ante una AppException, AltaPromocionForm muestra el mensaje y sigue abierto para corregir', 'AF'),
      r('AF', 'F', 'DialogResult.OK + IdPromocionCreada'),
      r('F', 'A', '"Promoción #id registrada (o reformulada): pendiente de revisión contable"'),
      c('F', 'BS', 'ObtenerPendientes()  [recarga]'),
      c('F', 'B', 'ObtenerTodas()  [recarga]'),
      { opt: '¿Imprimir la ficha para Contabilidad? Sí', pasos: [
        c('F', 'B', 'ObtenerPorId(idPromocion)'),
        c('F', 'B', 'ObtenerHistorial(idPromocion)'),
        r('F', 'A', 'Ficha de promoción (PDF) para Contabilidad')
      ] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU03_ADM_DescartarSugerencia', titulo: 'PN03 · CU03-ADM Descartar Sugerencia',
    participantes: [A('A', 'Administración'), P('F', 'PromocionesAdministracionForm'), P('B', 'BLL.SugerenciaPromocion'), P('D', 'DAL.SugerenciaPromocion')],
    pasos: [
      nota('«extend» de CU01-ADM Gestionar Promociones: ¿Acepta la sugerencia? No', 'A', 'F'),
      c('F', 'B', 'ObtenerPendientes()'),
      c('B', 'D', 'ObtenerPendientes()'),
      r('B', 'F', 'sugerencias Pendientes'),
      c('A', 'F', 'Elige la sugerencia y pulsa "Descartar sugerencia"'),
      c('A', 'F', 'Ingresa el motivo (InputDialog); si cancela, no se descarta'),
      c('F', 'B', 'DescartarSugerencia(modulo, idSugerencia, motivo)'),
      nota('PermisosAccion.Exigir(PromocionesAdminEditar, PromocionesAdmin)', 'B'),
      c('B', 'D', 'ObtenerPorId(idSugerencia)'),
      r('D', 'B', 'sugerencia'),
      { alt: 'No existe, ya no está Pendiente (PuedeEvaluarse / TransicionValida(Descartada)) o sin motivo', pasos: [
        r('B', 'F', 'AppException(sugerencia_inexistente / sugerencia_evaluada / motivodescarte_requerido)'), r('F', 'A', 'Informa el motivo')],
        sino: [{ etiqueta: 'Pendiente y con motivo', pasos: [
          c('B', 'D', 'Descartar(idSugerencia, motivo, fecha)  [claim: WHERE Estado = Pendiente]'),
          { alt: 'false: otra sesión ya la evaluó', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(sugerencia_evaluada)'), r('F', 'A', 'Informa el motivo')],
            sino: [{ etiqueta: 'true', pasos: [
              r('D', 'B', 'true'),
              nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Cancelacion, …)', 'B'),
              r('B', 'F', 'ok'),
              r('F', 'A', '"Sugerencia #id descartada"'),
              c('F', 'B', 'ObtenerPendientes()  [recarga]'),
              { opt: '¿Imprimir la constancia de descarte? Sí', pasos: [c('F', 'B', 'ObtenerPorId(idSugerencia)'), r('B', 'F', 'sugerencia Descartada'), r('F', 'A', 'Constancia de descarte (PDF): fin')] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_CONT_AnalizarPromocion', titulo: 'PN03 · CU01-CONT Analizar Promoción',
    participantes: [A('K', 'Contabilidad'), P('F', 'PromocionesContabilidadForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion'), P('DS', 'DAL.SugerenciaPromocion')],
    pasos: [
      nota('Analiza el margen y el impacto → ¿Aprueba?', 'K', 'F'),
      c('F', 'B', 'ObtenerPendientesRevisionContable()'),
      c('B', 'D', 'ObtenerPendientesRevisionContable()'),
      r('B', 'F', 'promociones En Revisión Contable'),
      c('K', 'F', 'Selecciona una promoción'),
      c('F', 'B', 'AnalizarMargenEImpacto(idPromocion)'),
      nota('PermisosAccion.Exigir(PromocionesContable, PromocionesContable)', 'B'),
      c('B', 'D', 'ObtenerPorId(idPromocion)'),
      { alt: 'Ya no existe', pasos: [r('D', 'B', 'null'), r('B', 'F', 'AppException(inexistente)'), r('F', 'K', 'Informa el error')],
        sino: [{ etiqueta: 'Existe', pasos: [
          r('D', 'B', 'promocion'),
          c('B', 'B', 'PuedeDictaminar(promocion)  [quien la creó no la dictamina, salvo el Administrador]'),
          c('B', 'D', 'ObtenerTodas()  [Superpuestas: las que cumplen promocion.SeSuperponeCon]'),
          { opt: 'Viene de una sugerencia (IdSugerenciaOrigen.HasValue)', pasos: [
            c('B', 'DS', 'ObtenerPorId(idSugerencia)  [idSugerencia = promocion.IdSugerenciaOrigen.Value]'),
            r('DS', 'B', 'sugerencia (beneficio estimado y origen)')] },
          r('B', 'F', 'AnalisisImpactoPromocion (beneficio estimado, superpuestas, UsuarioPuedeDictaminar)'),
          r('F', 'K', 'Análisis de margen e impacto; Aprobar y Rechazar habilitados solo si puede dictaminar')
        ] }] },
      c('K', 'F', 'Ingresa la observación, decide y confirma'),
      { alt: 'Aprueba ("Aprobar y Activar")', pasos: [
        c('F', 'B', 'AprobarContable(modulo, promocion, observacion)'),
        c('B', 'B', 'Dictaminar(promocion, observacion, aprobada)  [aprobada = true → Vigente]')],
        sino: [{ etiqueta: 'Rechaza ("Rechazar")', pasos: [
          c('F', 'B', 'RechazarContable(modulo, promocion, observacion)'),
          c('B', 'B', 'Dictaminar(promocion, observacion, aprobada)  [aprobada = false → RechazadaContabilidad]')] }] },
      nota('Dictaminar: PermisosAccion.Exigir(PromocionesContableEditar, PromocionesContable)', 'B'),
      { alt: 'No está En Revisión Contable, la creó el usuario o sin observación', pasos: [
        r('B', 'F', 'AppException(revisioncontable_estado / creador_no_dictamina / observacion_requerida)'), r('F', 'K', 'Informa el motivo')],
        sino: [{ etiqueta: 'Válido', pasos: [
          c('B', 'D', 'Dictaminar(dictamen, historial)  [claim: WHERE Estado = EnRevisionContable; + DictamenContable; historial: EnRevisionContable → Vigente o RechazadaContabilidad]'),
          { alt: 'Otra sesión ya la resolvió', pasos: [r('D', 'B', '0'), r('B', 'F', 'AppException(estado_concurrente)'), r('F', 'K', 'Informa el motivo')],
            sino: [{ etiqueta: 'Dictamen guardado', pasos: [
              r('D', 'B', 'idDictamen'),
              nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Venta o Cancelacion, …)', 'B'),
              r('B', 'F', 'idDictamen'),
              r('F', 'K', '"Promoción aprobada y activada" o "Promoción rechazada"'),
              c('F', 'B', 'ObtenerPendientesRevisionContable()  [recarga la cola]'),
              { opt: '¿Imprimir el dictamen contable? Sí', pasos: [
                c('F', 'B', 'ObtenerPorId(idPromocion)'),
                c('F', 'B', 'ObtenerUltimoDictamen(idPromocion)'),
                r('F', 'K', 'Dictamen contable (PDF): Vigente, o vuelve a Administración')] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU04_ADM_DescartarPromocion', titulo: 'PN03 · CU04-ADM Descartar Promoción Rechazada',
    participantes: [A('A', 'Administración'), P('F', 'PromocionesAdministracionForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion')],
    pasos: [
      nota('«extend» de CU01-ADM Gestionar Promociones: ¿Reformular? No', 'A', 'F'),
      c('A', 'F', 'Elige una promoción Rechazada por Contabilidad y pulsa "Descartar"'),
      c('A', 'F', 'Ingresa el motivo (InputDialog); si cancela, no se descarta'),
      c('F', 'B', 'DescartarPromocion(modulo, promocion, motivo)'),
      nota('PermisosAccion.Exigir(PromocionesAdminEditar, PromocionesAdmin)', 'B'),
      { alt: 'No está Rechazada (PuedeDescartarse / TransicionValida(Descartada)) o sin motivo', pasos: [
        r('B', 'F', 'AppException(descartar_estado / motivodescarte_requerido)'), r('F', 'A', 'Informa el motivo')],
        sino: [{ etiqueta: 'Rechazada y con motivo', pasos: [
          c('B', 'B', 'Transicionar(promocion, destino, observacion)  [destino = Descartada, observacion = motivo]'),
          c('B', 'D', 'CambiarEstado(idPromocion, estadoEsperado, historial)  [claim: estadoEsperado = RechazadaContabilidad; historial: RechazadaContabilidad → Descartada, con el motivo]'),
          { alt: 'false: otra sesión ya la cambió', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(estado_concurrente)'), r('F', 'A', 'Informa el motivo')],
            sino: [{ etiqueta: 'true', pasos: [
              r('D', 'B', 'true'),
              nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Cancelacion, …)', 'B'),
              r('B', 'F', 'ok'),
              r('F', 'A', '"Promoción descartada"'),
              c('F', 'B', 'ObtenerTodas()  [recarga]'),
              { opt: '¿Imprimir la constancia de descarte? Sí', pasos: [
                c('F', 'B', 'ObtenerPorId(idPromocion)'),
                c('F', 'B', 'ObtenerDescarte(idPromocion)'),
                r('F', 'A', 'Constancia de descarte (PDF): fin')] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_VEN_SugerirBaja', titulo: 'PN03 · CU07-VEN Solicitar Baja de Promoción',
    participantes: [A('V', 'Vendedor'), P('F', 'PromocionesVigentesForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion')],
    pasos: [
      c('V', 'F', 'Abre Promociones Vigentes (o pulsa Actualizar)'),
      c('F', 'B', 'ObtenerParaVentas()'),
      c('B', 'B', 'CerrarVencidasSinFallar()  [CerrarVencidas(): las Vigentes con fecha de fin pasada pasan a Vencida, ver CU05-ADM]'),
      c('B', 'D', 'ObtenerTodas()  [quedan las Vigentes y las de baja solicitada, por fecha de fin]'),
      r('B', 'F', 'promociones para Ventas'),
      c('V', 'F', 'Elige una promoción Vigente y pulsa "Solicitar baja"'),
      c('V', 'F', 'Ingresa el motivo (InputDialog); si cancela, no se solicita'),
      c('F', 'B', 'SolicitarBaja(modulo, promocion, motivo)'),
      nota('PermisosAccion.Exigir(PromocionesVigentesEditar, PromocionesVigentes)', 'B'),
      { alt: 'No está Vigente (PuedeSolicitarseBaja / TransicionValida(BajaSolicitada)) o sin motivo', pasos: [
        r('B', 'F', 'AppException(sugerirbaja_estado / motivobaja_requerido)'), r('F', 'V', 'Informa el motivo')],
        sino: [{ etiqueta: 'Vigente y con motivo', pasos: [
          c('B', 'D', 'SolicitarBaja(solicitud, historial)  [claim: WHERE Estado = Vigente; + SolicitudBajaPromocion Pendiente; historial: Vigente → BajaSolicitada]'),
          { alt: 'Ya no está Vigente', pasos: [r('D', 'B', '0'), r('B', 'F', 'AppException(estado_concurrente)'), r('F', 'V', 'Informa el motivo')],
            sino: [{ etiqueta: 'Solicitada', pasos: [
              r('D', 'B', 'idSolicitud'),
              nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Venta, …)', 'B'),
              r('B', 'F', 'idSolicitud'),
              r('F', 'V', '"Se envió a Administración la sugerencia de baja"'),
              c('F', 'B', 'ObtenerParaVentas()  [recarga]'),
              { opt: '¿Imprimir la solicitud de baja? Sí', pasos: [
                c('F', 'B', 'ObtenerPorId(idPromocion)'),
                c('F', 'B', 'ObtenerUltimaSolicitudBaja(idPromocion)'),
                r('F', 'V', 'Solicitud de baja (PDF): Administración la resuelve')] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU02_ADM_ResolverBaja', titulo: 'PN03 · CU02-ADM Resolver Baja de Promoción',
    participantes: [A('A', 'Administración'), P('F', 'PromocionesAdministracionForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion')],
    pasos: [
      nota('¿Aprueba la baja? (solicitada por Ventas en CU07-VEN)', 'A', 'F'),
      c('F', 'B', 'ObtenerTodas()'),
      r('B', 'F', 'promociones (las de baja solicitada muestran el motivo de Ventas)'),
      c('A', 'F', 'Elige una promoción con baja solicitada y revisa el motivo de Ventas'),
      { alt: 'Aprueba la baja: "Aprobar Baja"', pasos: [
        c('A', 'F', 'Ingresa una observación (opcional)'),
        c('F', 'B', 'AprobarBaja(modulo, promocion, observacion)'),
        c('B', 'B', 'ResolverBaja(promocion, resultado, destino, motivoResolucion)  [Aprobada → Desactivada; observación vacía = null]')
      ], sino: [{ etiqueta: 'Rechaza la baja: "Rechazar Baja"', pasos: [
        c('A', 'F', 'Ingresa el motivo (obligatorio)'),
        c('F', 'B', 'RechazarBaja(modulo, promocion, motivo)'),
        { alt: 'Sin motivo', pasos: [r('B', 'F', 'AppException(motivorechazobaja_requerido)'), r('F', 'A', 'Informa el motivo')],
          sino: [{ etiqueta: 'Con motivo', pasos: [c('B', 'B', 'ResolverBaja(promocion, resultado, destino, motivoResolucion)  [Rechazada → Vigente; el dictamen contable no se toca]')] }] }
      ] }] },
      nota('ResolverBaja: PermisosAccion.Exigir(PromocionesAdminEditar, PromocionesAdmin)', 'B'),
      { alt: 'No está con baja solicitada (PuedeResolverseBaja / TransicionValida(destino))', pasos: [r('B', 'F', 'AppException(resolverbaja_estado)'), r('F', 'A', 'Informa el motivo')],
        sino: [{ etiqueta: 'Baja solicitada', pasos: [
          c('B', 'D', 'ObtenerSolicitudesBaja(idPromocion)  [la última pendiente]'),
          { alt: 'Sin solicitud pendiente', pasos: [r('B', 'F', 'AppException(solicitud_inexistente)'), r('F', 'A', 'Informa el motivo')],
            sino: [{ etiqueta: 'Solicitud pendiente', pasos: [
              c('B', 'D', 'ResolverBaja(resolucion, historial)  [claim: WHERE Estado = BajaSolicitada; actualiza la solicitud; historial: BajaSolicitada → Desactivada o Vigente]'),
              { alt: 'false: otra sesión ya la resolvió', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(estado_concurrente)'), r('F', 'A', 'Informa el motivo')],
                sino: [{ etiqueta: 'true', pasos: [
                  r('D', 'B', 'true'),
                  nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Cancelacion o Venta, …)', 'B'),
                  r('B', 'F', 'idSolicitud'),
                  r('F', 'A', '"Promoción dada de baja" o "Se rechazó la baja: sigue vigente"'),
                  c('F', 'B', 'ObtenerTodas()  [recarga]'),
                  { opt: '¿Imprimir la resolución de baja? Sí', pasos: [
                    c('F', 'B', 'ObtenerPorId(idPromocion)'),
                    c('F', 'B', 'ObtenerUltimaSolicitudBaja(idPromocion)'),
                    r('F', 'A', 'Resolución de baja (PDF): informe a Gerencia (aprobada) o a Ventas (rechazada)')] }
                ] }] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU05_ADM_DesactivarYVencer', titulo: 'PN03 · CU05-ADM Desactivar Promoción',
    participantes: [A('A', 'Administración'), P('F', 'PromocionesAdministracionForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion')],
    pasos: [
      nota('Desactivación directa (b) y cierre por fecha de fin (c): al consultar, las Vigentes vencidas pasan a Vencida', 'A', 'F'),
      c('A', 'F', 'Abre Gestión de Promociones (o pulsa Actualizar)'),
      c('F', 'B', 'ObtenerTodas()'),
      c('B', 'B', 'CerrarVencidasSinFallar()  [si falla, lo deja en el Trace y la consulta sigue]'),
      c('B', 'B', 'CerrarVencidas()  [(c) llega la fecha de fin]'),
      c('B', 'D', 'ObtenerTodas()  [se quedan las que cumplen DebeVencer(hoy): Vigente con FechaFin pasada]'),
      { loop: 'Por cada promoción que debe vencer', pasos: [
        c('B', 'D', 'CambiarEstado(idPromocion, estadoEsperado, historial)  [claim: estadoEsperado = Vigente; historial: Vigente → Vencida]'),
        r('D', 'B', 'true, o false si otra sesión ya la cerró (no duplica el historial)')
      ] },
      nota('bitacoraNeg.Registrar(Cancelacion, …) por cada vencida · bitacora.Registrar("Promociones", …) si cerró alguna', 'B'),
      c('B', 'D', 'ObtenerTodas()'),
      r('B', 'F', 'promociones (las vencidas ya no aplican en el cobro)'),
      c('A', 'F', '(b) Elige una Vigente, pulsa "Desactivar" e ingresa el motivo'),
      c('F', 'B', 'Desactivar(modulo, promocion, motivo)'),
      nota('PermisosAccion.Exigir(PromocionesAdminEditar, PromocionesAdmin)', 'B'),
      { alt: 'No está Vigente (PuedeDesactivarseDirecto / TransicionValida(Desactivada)) o sin motivo', pasos: [
        r('B', 'F', 'AppException(desactivar_estado / motivodesactivar_requerido)'), r('F', 'A', 'Informa el motivo')],
        sino: [{ etiqueta: 'Vigente y con motivo', pasos: [
          c('B', 'B', 'Transicionar(promocion, destino, observacion)  [destino = Desactivada, observacion = motivo]'),
          c('B', 'D', 'CambiarEstado(idPromocion, estadoEsperado, historial)  [claim: estadoEsperado = Vigente; historial: Vigente → Desactivada, con el motivo]'),
          { alt: 'false: otra sesión ya la cambió', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(estado_concurrente)'), r('F', 'A', 'Informa el motivo')],
            sino: [{ etiqueta: 'true', pasos: [
              r('D', 'B', 'true'),
              nota('bitacora.Registrar(modulo, …) · bitacoraNeg.Registrar(Cancelacion, …)', 'B'),
              r('B', 'F', 'ok'),
              r('F', 'A', '"Promoción desactivada": fin'),
              c('F', 'B', 'ObtenerTodas()  [recarga]')
            ] }] }
        ] }] }
    ]
  },

  // ───────────────────────────── PN04 — Inspección de devolución ─────────────────────────────
  {
    tipo: 'secuencia', id: 'DSS_PN04_CU01_DEP_InspeccionarDevolucion', titulo: 'PN04 · CU05-DEP Inspeccionar devolución',
    // GUI/InspeccionDevolucionForm.cs › CargarPrendas, BtnAprobarReingreso_Click, BtnDarDeBajaConCargo_Click;
    // BLL/InspeccionDevolucion.cs (AprobarReingreso, DarDeBajaConCargo → BajaConCargo); BLL/Prenda.cs › CambiarEstado (patrón State);
    // DAL/InspeccionDevolucion.cs: INSERT del cargo + UPDATE condicionado de la prenda en una sola transacción.
    participantes: [A('D', 'Depósito'), P('F', 'InspeccionDevolucionForm'), P('CD', 'CargoPrendaDialog'), P('IN', 'BLL.InspeccionDevolucion'),
                    P('PB', 'BLL.Prenda'), P('PR', 'BE.Prenda'), P('CG', 'BLL.CargoPrenda'), P('LE', 'BLL.ListaEspera'),
                    P('DP', 'DAL.Prenda'), P('DM', 'DAL.MantenimientoPrenda'), P('DL', 'DAL.ListaEspera'), P('DI', 'DAL.InspeccionDevolucion')],
    pasos: [
      c('D', 'F', 'Abre Inspección de Devolución'),
      c('F', 'PB', 'ObtenerEnLimpieza()'),
      c('PB', 'DP', 'ObtenerTodos()'),
      r('DP', 'PB', 'prendas'),
      r('PB', 'F', 'prendas En limpieza'),
      r('F', 'D', 'Cola de prendas pendientes de inspección (con su último cliente)'),
      c('D', 'F', 'Selecciona una prenda y la inspecciona'),
      { alt: 'Desgaste normal: Aprobar reingreso', pasos: [
        c('D', 'F', 'Pulsa "Aprobar reingreso" y confirma'),
        c('F', 'IN', 'AprobarReingreso(modulo, prenda)'),
        c('IN', 'PB', 'CambiarEstado(modulo, prenda, Disponible, actor)'),
        nota('Exigir(StockEditar) · una prenda En limpieza solo se da de baja desde la Inspección', 'PB'),
        c('PB', 'PR', 'ControlarEstado(Disponible)'),
        nota('Patrón State: EstadoEnLimpieza permite EnLimpieza → Disponible', 'PR'),
        r('PR', 'PB', 'true'),
        c('PB', 'DP', 'CambiarEstado(idPrenda, EnLimpieza, Disponible, null)'),
        c('PB', 'DM', 'CerrarMantenimiento(idPrenda)'),
        c('PB', 'LE', 'NotificarSiCorresponde(idPrenda, actor)'),
        c('LE', 'DL', 'ObtenerPendienteMasAntigua(idPrenda)'),
        r('DL', 'LE', 'primer cliente en espera (si hay)'),
        { alt: 'Hay un cliente en espera', pasos: [c('LE', 'DL', 'CambiarEstado(idListaEspera, Reservada, limite, actor, Pendiente)  [reserva por 48 h]')] },
        r('PB', 'IN', 'ok'),
        r('IN', 'F', 'ok'),
        r('F', 'D', 'La prenda reingresó a Disponible, sin cargo')
      ], sino: [{ etiqueta: 'Daño irreparable: Dar de baja con cargo', pasos: [
        c('D', 'F', 'Pulsa "Dar de baja con cargo"'),
        c('F', 'CD', 'Abre el diálogo con el precio de reposición precargado'),
        c('D', 'CD', 'Ingresa el motivo del daño y confirma o ajusta el monto'),
        c('CD', 'CG', 'ValidarDatos(motivo, monto)'),
        r('CG', 'CD', 'ok (motivo obligatorio, monto > 0)'),
        r('CD', 'F', 'motivo y monto'),
        c('F', 'IN', 'DarDeBajaConCargo(modulo, prenda, motivo, monto)'),
        c('IN', 'IN', 'BajaConCargo(modulo, prenda, motivo, monto, actor, EnLimpieza)'),
        nota('Exigir(StockEditar) · la prenda sigue En limpieza y tiene último cliente', 'IN'),
        c('IN', 'CG', 'ValidarDatos(motivo, monto)'),
        c('IN', 'DI', 'DarDeBajaConCargo(cargo, estadoEsperado)'),
        nota('Una sola transacción: primero el cargo (Pendiente, al último cliente) y después la baja condicionada', 'DI'),
        r('DI', 'IN', 'idCargo'),
        r('IN', 'F', 'idCargo'),
        r('F', 'D', 'Prenda dada de baja: el cargo se suma al próximo cobro del cliente')
      ] }] },
      { alt: 'Otra sesión ya la resolvió (el UPDATE no afecta filas)', pasos: [r('IN', 'F', 'AppException(estado_cambio): no queda ni el cargo ni la baja'), r('F', 'D', 'Informa el error y actualiza la cola')] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN04_CU02_DEP_ReportarPrendaPerdida', titulo: 'PN04 · CU06-DEP Reportar prenda perdida',
    // GUI/PedidosRealizados.cs › CargarDetallePrendas, BtnReportarPerdida_Click; BLL/InspeccionDevolucion.cs › ReportarPerdida
    // (En uso → Baja con cargo, en la misma transacción que CU05-DEP).
    participantes: [A('D', 'Depósito'), P('F', 'PedidosRealizados'), P('CD', 'CargoPrendaDialog'), P('PE', 'BLL.Pedido'), P('PR', 'BE.Prenda'),
                    P('IN', 'BLL.InspeccionDevolucion'), P('CG', 'BLL.CargoPrenda'), P('DPE', 'DAL.Pedido'), P('DI', 'DAL.InspeccionDevolucion')],
    pasos: [
      c('D', 'F', 'Consulta los pedidos realizados y elige un pedido'),
      c('F', 'PE', 'ObtenerPorId(id)'),
      c('PE', 'DPE', 'ObtenerPorId(idPedido)'),
      r('DPE', 'PE', 'pedido con sus prendas'),
      r('PE', 'F', 'pedido'),
      r('F', 'D', 'Detalle de prendas del pedido con su estado'),
      c('D', 'F', 'Selecciona la prenda En uso que no va a volver y pulsa "Reportar pérdida"'),
      c('F', 'PR', 'PuedeReportarsePerdida()'),
      r('PR', 'F', 'true (está En uso)'),
      c('F', 'CD', 'Abre el diálogo con el precio de reposición precargado'),
      c('D', 'CD', 'Ingresa el motivo de la pérdida y confirma o ajusta el monto'),
      c('CD', 'CG', 'ValidarDatos(motivo, monto)'),
      r('CG', 'CD', 'ok (motivo obligatorio, monto > 0)'),
      r('CD', 'F', 'motivo y monto'),
      c('F', 'IN', 'ReportarPerdida(modulo, prenda, motivo, monto)'),
      c('IN', 'IN', 'BajaConCargo(modulo, prenda, motivo, monto, actor, EnUso)'),
      nota('Exigir(StockEditar) · la prenda sigue En uso', 'IN'),
      { alt: 'Sin último cliente registrado', pasos: [r('IN', 'F', 'AppException(sin_cliente)'), r('F', 'D', 'Rechaza la operación (fin del caso de uso)')],
        sino: [{ etiqueta: 'Con último cliente', pasos: [
          c('IN', 'CG', 'ValidarDatos(motivo, monto)'),
          c('IN', 'DI', 'DarDeBajaConCargo(cargo, estadoEsperado)'),
          nota('Una sola transacción: el cargo (Pendiente, al último cliente) y la baja En uso → Baja', 'DI'),
          r('DI', 'IN', 'idCargo'),
          r('IN', 'F', 'idCargo'),
          r('F', 'D', 'Prenda reportada como perdida: el cargo se suma al próximo cobro')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_N01_CU04_GestionarPlanes', titulo: 'N01 · CU03-VEN Gestionar planes de suscripción',
    participantes: [A('V', 'Vendedor'), P('F', 'Planes'), P('B', 'BLL.PlanSuscripcion'), P('D', 'DAL.PlanSuscripcion')],
    pasos: [
      c('V', 'F', 'Completa nombre, límite de prendas y precio mensual, y pulsa Guardar Plan'),
      c('F', 'B', 'Alta(modulo, plan)  [o Modificar(modulo, plan) si edita uno existente]'),
      nota('Exigir(PlanSuscripcionesEditar) · nombre obligatorio (solo letras) · límite de prendas válido · precio mayor a cero', 'B'),
      { alt: 'Datos inválidos', pasos: [r('B', 'F', 'AppException(nombre_requerido / limite_invalido / precio_cero)'), r('F', 'V', 'Informa el error')],
        sino: [{ etiqueta: 'Datos válidos', pasos: [
          nota('Alta: el plan queda activo · Modificar: conserva el estado guardado (ObtenerPorId)', 'B'),
          c('B', 'D', 'Alta(plan) / Modificar(plan)'),
          r('B', 'F', 'ok'),
          r('F', 'V', 'Plan guardado')
        ] }] },
      c('V', 'F', 'Pulsa Desactivar Plan'),
      c('F', 'B', 'Desactivar(modulo, plan)'),
      { alt: 'El plan tiene clientes asignados', pasos: [r('B', 'F', 'AppException(tiene_clientes)')],
        sino: [{ etiqueta: 'Sin clientes', pasos: [c('B', 'D', 'Desactivar(idPlan)'), r('B', 'F', 'ok')] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU03_GER_AnalizarMetricas', titulo: 'PN03 · CU03-GER Analizar Métricas',
    // «extend» de CU01-GER Sugerir Promoción (botón "Analizar métricas…" de GUI/SugerirPromocionForm.cs).
    // BLL/AnalisisPromociones.cs › AnalizarMetricas: rotación (BLL.AnalisisRotacion.Detectar) + abandono
    // (BLL.AnalisisAbandono.Detectar, Strategy con el criterio por defecto) + planes → «Reporte de métricas».
    participantes: [A('G', 'Gerencia'), P('F', 'SugerirPromocionForm'), P('AN', 'BLL.AnalisisPromociones'), P('AR', 'BLL.AnalisisRotacion'),
                    P('AA', 'BLL.AnalisisAbandono'), P('ER', 'EstrategiaRiesgo'), P('DPE', 'DAL.Pedido'), P('DR', 'DAL.Prenda'), P('DC', 'DAL.Cliente'),
                    P('DP', 'DAL.PlanSuscripcion')],
    pasos: [
      nota('Punto de extensión de CU01-GER: Gerencia pulsa "Analizar métricas…" antes de cargar la sugerencia', 'G', 'F'),
      c('G', 'F', 'Pulsa "Analizar métricas…"'),
      c('F', 'AN', 'AnalizarMetricas(modulo)'),
      nota('PermisosAccion.Exigir(SugerenciaPromocion, SugerenciaPromocion)', 'AN'),
      c('AN', 'AR', 'Detectar()'),
      c('AR', 'DPE', 'ObtenerCantidadPedidosPorPrenda()'),
      r('DPE', 'AR', 'pedidos por prenda'),
      c('AR', 'DR', 'ObtenerTodos()'),
      r('DR', 'AR', 'prendas'),
      r('AR', 'AN', 'List<RotacionPrenda>'),
      c('AN', 'AA', 'Detectar()  [criterio por defecto: EstrategiaVencimientoInactividad]'),
      c('AA', 'DPE', 'ObtenerFechaUltimoPedidoPorCliente()'),
      r('DPE', 'AA', 'fecha del último pedido de cada cliente'),
      c('AA', 'DC', 'ObtenerTodos()'),
      r('DC', 'AA', 'clientes'),
      { loop: 'Por cada cliente con plan (TienePlan)', pasos: [
        c('AA', 'ER', 'Evaluar(datos)'),
        r('ER', 'AA', 'ResultadoRiesgo (EnRiesgo, Motivo)')
      ] },
      r('AA', 'AN', 'List<ClienteEnRiesgo>'),
      c('AN', 'DP', 'ObtenerTodos()'),
      r('DP', 'AN', 'planes'),
      c('AN', 'AN', 'AbandonoPorPlan(enRiesgo, planes)'),
      c('AN', 'AN', 'Oportunidades(rot, ab, planes)  [rotación: ≥ 2 prendas sin pedidos por categoría; abandono: clientes en riesgo por plan]'),
      nota('bitacora.Registrar(modulo, …): cantidad de oportunidades o "sin oportunidad de promoción"', 'AN'),
      r('AN', 'F', 'ReporteMetricas (abandono por plan, rotación por categoría, oportunidades)'),
      c('F', 'AN', 'HayOportunidad(reporte)'),
      r('AN', 'F', 'hayOportunidad'),
      c('F', 'F', 'MostrarReporte(reporte, hayOportunidad)'),
      { alt: '¿Hay oportunidad? No', pasos: [r('F', 'G', '«Reporte de métricas»: "Los reportes de rotación y abandono no detectan casos" (fin sin promoción)')],
        sino: [{ etiqueta: 'Sí', pasos: [
          r('F', 'G', '«Reporte de métricas» (imprimible) con las oportunidades para elegir'),
          c('G', 'F', 'Pulsa "Usar esta idea" sobre una oportunidad'),
          r('F', 'G', 'Precarga destino (plan o categoría), tipo, beneficio estimado y motivo; origen = Abandono o Rotación'),
          nota('Sigue CU01-GER Sugerir Promoción: RegistrarSugerencia con el origen de la métrica', 'G', 'F')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU02_GER_ConsultarAnalitica', titulo: 'PN03 · CU02-GER Consultar Analítica de Negocio',
    participantes: [A('G', 'Gerencia'), P('M', 'Menu'), P('FA', 'AnalisisAbandonoForm'), P('BA', 'BLL.AnalisisAbandono'), P('ER', 'EstrategiaRiesgo'), P('DC', 'DAL.Cliente'),
                    P('GR', 'GeneradorReporte'), P('EX', 'Exportador'), P('FV', 'ReporteVentasVendedorForm'), P('BV', 'BLL.ReporteVentasVendedor'),
                    P('FR', 'AnalisisRotacionForm'), P('BR', 'BLL.AnalisisRotacion'), P('FM', 'AnalisisMantenimientoForm'), P('BM', 'BLL.AnalisisMantenimiento'),
                    P('DM', 'DAL.MantenimientoPrenda'), P('FE', 'AnalisisEscasezForm'), P('BE', 'BLL.AnalisisEscasez'), P('FC', 'RecomendacionPrendasForm'),
                    P('BC', 'BLL.RecomendacionPrendas'), P('DP', 'DAL.Pedido'), P('DR', 'DAL.Prenda')],
    pasos: [
      nota('Menú "Analítica de Negocio": seis reportes de solo lectura; cada ítem se muestra según los permisos del usuario', 'G', 'M'),
      { alt: 'Análisis de Abandono', pasos: [
        c('G', 'M', 'Analítica de Negocio → Análisis de Abandono'),
        c('M', 'FA', 'new AnalisisAbandonoForm()  [AbrirUnico: hijo MDI]'),
        nota('Al cargar, CargarEstrategias() ofrece EstrategiaVencimientoInactividad (por defecto), EstrategiaInactividadPura y EstrategiaClienteNuevoInactivo', 'FA'),
        c('G', 'FA', 'Elige el criterio de riesgo y pulsa Generar'),
        c('FA', 'BA', 'CambiarEstrategia(estrategia)  [patrón Strategy: la elegida en el combo]'),
        c('FA', 'BA', 'Detectar()'),
        c('BA', 'DP', 'ObtenerFechaUltimoPedidoPorCliente()'),
        r('DP', 'BA', 'fecha del último pedido de cada cliente'),
        c('BA', 'DC', 'ObtenerTodos()'),
        r('DC', 'BA', 'clientes'),
        { loop: 'Por cada cliente con plan (TienePlan)', pasos: [
          c('BA', 'ER', 'Evaluar(datos)  [cliente + fecha de su último pedido]'),
          r('ER', 'BA', 'ResultadoRiesgo (EnRiesgo, Motivo)')
        ] },
        r('BA', 'FA', 'List<ClienteEnRiesgo>'),
        r('FA', 'G', 'Grilla de clientes en riesgo y cantidad según el criterio (exportar habilitado si hay resultados)'),
        { opt: 'Exportar a PDF o Guardar como .CSV', pasos: [
          c('G', 'FA', 'Pulsa "Exportar a PDF" o "Guardar como .CSV"'),
          c('FA', 'FA', 'Exportar(formato)  [formato = "pdf" o "csv"; arma el ReporteExportable con la grilla]'),
          c('FA', 'GR', 'CrearExportador(formato)  [generador: GeneradorAnalisisAbandono, Factory Method]'),
          r('GR', 'FA', 'Exportador (PDF o CSV)'),
          c('FA', 'EX', 'Exportar(reporte, propietario)'),
          r('EX', 'G', 'PDF (vista previa) o archivo .CSV')
        ] }
      ], sino: [
        { etiqueta: 'Ventas por Vendedor', pasos: [
          c('G', 'M', 'Analítica de Negocio → Ventas por Vendedor'),
          c('M', 'FV', 'new ReporteVentasVendedorForm()'),
          c('G', 'FV', 'Pulsa Generar'),
          c('FV', 'BV', 'Obtener()'),
          c('BV', 'DP', 'ObtenerEstadisticasPorEmpleado()'),
          r('BV', 'FV', 'List<DesempenoVendedor>'),
          r('FV', 'G', 'Grilla por vendedor: pedidos, entregados, cancelados y tasa de cancelación')
        ] },
        { etiqueta: 'Rotación de Prendas', pasos: [
          c('G', 'M', 'Analítica de Negocio → Rotación de Prendas'),
          c('M', 'FR', 'new AnalisisRotacionForm()'),
          c('G', 'FR', 'Pulsa Generar'),
          c('FR', 'BR', 'Detectar()'),
          c('BR', 'DP', 'ObtenerCantidadPedidosPorPrenda()'),
          c('BR', 'DR', 'ObtenerTodos()  [la BLL descarta las de Baja]'),
          r('BR', 'FR', 'List<RotacionPrenda>  [baja demanda: sin pedidos y 30 días o más en catálogo; alta: 5 pedidos o más]'),
          r('FR', 'G', 'Grilla de prendas marcadas por rotación')
        ] },
        { etiqueta: 'Tiempos de Mantenimiento', pasos: [
          c('G', 'M', 'Analítica de Negocio → Tiempos de Mantenimiento'),
          c('M', 'FM', 'new AnalisisMantenimientoForm()'),
          c('G', 'FM', 'Pulsa Generar'),
          c('FM', 'BM', 'Detectar()'),
          c('BM', 'DM', 'ObtenerTodos()'),
          r('BM', 'FM', 'List<TiempoMantenimientoPrenda>  [3 mantenimientos o más, o promedio de 5 días o más]'),
          r('FM', 'G', 'Grilla de prendas con mantenimientos excesivos')
        ] },
        { etiqueta: 'Escasez de Stock', pasos: [
          c('G', 'M', 'Analítica de Negocio → Escasez de Stock'),
          c('M', 'FE', 'new AnalisisEscasezForm()'),
          c('G', 'FE', 'Indica el umbral mínimo (por defecto 3) y pulsa Generar'),
          c('FE', 'BE', 'Detectar(umbralMinimo)  [(int)numUmbral.Value]'),
          c('BE', 'DR', 'ObtenerConteoDisponiblesPorTalleCategoria()'),
          r('BE', 'FE', 'List<EscasezStock>  [talle + categoría con menos disponibles que el umbral]'),
          r('FE', 'G', 'Grilla de combinaciones en escasez')
        ] },
        { etiqueta: 'Recomendación de Prendas', pasos: [
          c('G', 'M', 'Analítica de Negocio → Recomendación de Prendas'),
          c('M', 'FC', 'new RecomendacionPrendasForm()'),
          nota('Al cargar, CargarClientes() llena el combo con BLL.Cliente.ObtenerTodos()', 'FC'),
          c('G', 'FC', 'Elige el cliente y pulsa Generar'),
          c('FC', 'BC', 'Recomendar(idCliente)  [item.Cliente.IdCliente]'),
          c('BC', 'DP', 'ObtenerPrendasHistoricasPorCliente(idCliente)'),
          { alt: 'Sin historial de pedidos', pasos: [
            r('BC', 'FC', 'lista vacía'),
            r('FC', 'G', '"El cliente no tiene historial de pedidos suficiente para recomendar"')],
            sino: [{ etiqueta: 'Con historial', pasos: [
              c('BC', 'DR', 'ObtenerDisponibles()'),
              r('BC', 'FC', 'List<PrendaRecomendada>  [categoría y/o color favoritos, sin las ya pedidas]'),
              r('FC', 'G', 'Grilla de prendas recomendadas')
            ] }] }
        ] }
      ] },
      nota('Los otros cinco formularios exportan igual: Exportar(formato) → CrearExportador(formato) → Exportar(reporte, propietario), con GeneradorVentasVendedor, GeneradorRotacion, GeneradorMantenimiento, GeneradorEscasez y GeneradorRecomendacion', 'G')
    ]
  },
];

// Sub-diagrama auxiliar: aviso cuando otra sesión ya cobró los cargos pendientes.
function alt_cargo() {
  return { alt: 'Otra sesión ya cobró los cargos pendientes', pasos: [r('D', 'H2', 'MarcarCobradosEnTx = false'), r('H2', 'B', 'AppException(cargo_concurrente): se revierte todo el cobro')],
    sino: [{ etiqueta: 'Cargos liquidados', pasos: [r('D', 'H2', 'ok')] }] };
}
