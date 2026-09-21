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
      c('V', 'F', 'Elige cliente y decisión (Renovar, Cambiar plan, Pausar, Baja)'),
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
    tipo: 'secuencia', id: 'DSS_N01_CU03_CobrarSuscripcion', titulo: 'N01 · CU03-VEN Cobrar suscripción (cobro recurrente)',
    participantes: [A('V', 'Vendedor'), P('F', 'CobroSuscripcionForm'), P('B', 'BLL.Cobro'), P('H1', 'DetectarCobroHandler'), P('H2', 'ProcesarPagoHandler'), P('H3', 'AplicarGracia / Suspender Handler'), P('D', 'DAL (Cliente, Cobro, CargoPrenda, Promocion)')],
    pasos: [
      c('V', 'F', 'Elige cliente, modalidad y resultado del cobro (Cobrado o Pago fallido)'),
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
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU01_ArmarPedido', titulo: 'PN01 · CU01-VEN Armar pedido',
    participantes: [A('V', 'Vendedor'), P('F', 'NuevoPedidoForm'), P('B', 'BLL.Pedido'), P('PB', 'BLL.Prenda'), P('LE', 'BLL.ListaEspera'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('V', 'F', 'Elige al cliente'),
      c('F', 'B', 'ValidarPuedeArmarPedido(idCliente)'),
      nota('Suscripción vigente, sin pausa ni suspensión · sin pedido Despachado · sin prendas En uso (cuenta desbloqueada)', 'B'),
      { alt: 'Cliente no apto', pasos: [r('B', 'F', 'AppException(motivo)'), r('F', 'V', 'Informa la situación y no permite avanzar')],
        sino: [{ etiqueta: 'Cliente apto', pasos: [
          r('B', 'F', 'cliente validado'),
          c('V', 'F', 'Selecciona las prendas del catálogo y confirma'),
          c('F', 'B', 'CrearPedido(modulo, idCliente, prendas)'),
          nota('Exigir(PedidosVentaEditar) · ValidarCupoDisponible: cantidad ≤ LimitePrendas del plan', 'B'),
          { alt: 'Excede el cupo del plan', pasos: [r('B', 'F', 'AppException(cupo)'), r('F', 'V', 'Informa el exceso: ajustar o desistir')],
            sino: [{ etiqueta: 'Dentro del cupo', pasos: [
              c('B', 'PB', 'VerificarDisponibilidad(prendas)  [relee el estado en la BD]'),
              r('PB', 'B', '(disponible, noDisponibles)'),
              c('B', 'LE', 'ObtenerIdsReservadosParaOtro(idCliente)'),
              { alt: 'Alguna prenda no disponible o reservada para otro cliente', pasos: [r('B', 'F', 'AppException(prenda_no_disponible / prenda_reservada)'), r('F', 'V', 'Informa la falta: ajustar o desistir')],
                sino: [{ etiqueta: 'Todas disponibles', pasos: [
                  c('B', 'D', 'Alta(pedido)  [transacción: Pedido + PedidoPrenda + prendas a En uso, con reclamo por prenda]'),
                  { alt: 'Otra sesión tomó una prenda', pasos: [r('D', 'B', 'error de concurrencia'), r('B', 'F', 'AppException'), r('F', 'V', 'Revierte todo el pedido')],
                    sino: [{ etiqueta: 'Reserva confirmada', pasos: [
                      r('D', 'B', 'idPedido'),
                      c('B', 'H', 'RegistrarCambios(idPedido, "CREAR")'),
                      c('B', 'LE', 'CerrarSiReservada(idPrenda, idCliente)'),
                      r('B', 'F', 'idPedido'),
                      r('F', 'V', 'Pedido creado: queda bloqueado y con número único')
                    ] }] }
                ] }] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU02_ConsultarCatalogo', titulo: 'PN01 · CU02-VEN Consultar catálogo',
    participantes: [A('V', 'Vendedor'), P('F', 'Prendas / NuevoPedidoForm'), P('B', 'BLL.Prenda'), P('D', 'DAL.Prenda')],
    pasos: [
      c('V', 'F', 'Solicita consultar el catálogo'),
      c('F', 'B', 'ObtenerDisponibles()'),
      c('B', 'D', 'ObtenerTodos() filtrado por Estado = Disponible'),
      r('D', 'B', 'lista de prendas'),
      r('B', 'F', 'prendas Disponibles'),
      r('F', 'V', 'Muestra talle, color, categoría y valor de reposición'),
      { alt: 'No hay prendas disponibles', pasos: [r('F', 'V', 'Muestra la grilla vacía')] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU03_ConsultarSituacionCliente', titulo: 'PN01 · CU03-VEN Consultar situación del cliente',
    participantes: [A('V', 'Vendedor'), P('F', 'NuevoPedidoForm'), P('B', 'BLL.Cliente'), P('D', 'DAL.Cliente')],
    pasos: [
      c('V', 'F', 'Ingresa DNI o código y confirma la consulta'),
      c('F', 'B', 'ObtenerEstadoComercial(cliente, prendasSolicitadas)'),
      c('B', 'D', 'ObtenerPorId(idCliente)'),
      r('D', 'B', 'cliente (plan, cupo, vencimiento, prendas en uso)'),
      { alt: 'Sin plan', pasos: [r('B', 'F', 'MotivoBloqueo = SIN_PLAN')],
        sino: [{ etiqueta: 'Suscripción vencida', pasos: [r('B', 'F', 'MotivoBloqueo = SUSCRIPCION_VENCIDA + fecha')] },
               { etiqueta: 'Vigente', pasos: [r('B', 'F', 'EstadoComercialCliente (PuedeProceder, cupo disponible)')] }] },
      r('F', 'V', 'Muestra plan, cupo disponible, vigencia y pedido activo')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU04_DespacharPedido', titulo: 'PN01 · CU01-DEP Despachar pedido',
    participantes: [A('L', 'Depósito / Logística'), P('F', 'PedidosRealizados'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('L', 'F', 'Selecciona un pedido Pendiente y pulsa Despachar'),
      c('F', 'B', 'Despachar(modulo, pedido)'),
      nota('Exigir(PedidosRealizadosEditar) · pedido.PuedeDespachar()', 'B'),
      { alt: 'El pedido no está Pendiente', pasos: [r('B', 'F', 'AppException(despachar_estado)'), r('F', 'L', 'Informa el estado actual')],
        sino: [{ etiqueta: 'Pendiente', pasos: [
          c('B', 'D', 'Despachar(idPedido)'),
          c('B', 'H', 'RegistrarCambios(idPedido, "DESPACHAR")'),
          r('B', 'F', 'ok'),
          r('F', 'L', 'Pedido Despachado')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU05_RegistrarEntrega', titulo: 'PN01 · CU02-DEP Registrar entrega',
    participantes: [A('L', 'Depósito / Logística'), P('F', 'PedidosRealizados'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('L', 'F', 'Selecciona un pedido Despachado y pulsa Marcar Entregado'),
      c('F', 'B', 'MarcarEntregado(modulo, pedido)'),
      nota('Exigir(PedidosRealizadosEditar) · pedido.PuedeEntregarse()', 'B'),
      { alt: 'El pedido no está Despachado', pasos: [r('B', 'F', 'AppException(entregar_estado)'), r('F', 'L', 'Informa el estado actual')],
        sino: [{ etiqueta: 'Despachado', pasos: [
          c('B', 'D', 'MarcarEntregado(idPedido)'),
          c('B', 'H', 'RegistrarCambios(idPedido, "ENTREGAR")'),
          r('B', 'F', 'ok'),
          r('F', 'L', 'Pedido Entregado')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU06_RegistrarDevolucion', titulo: 'PN01 · CU03-DEP Registrar devolución (desbloquea la cuenta)',
    participantes: [A('L', 'Depósito / Logística'), P('F', 'PedidosRealizados'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido'), P('H', 'DAL.PedidoHistorial')],
    pasos: [
      c('L', 'F', 'Selecciona un pedido Entregado y pulsa Registrar Devolución'),
      c('F', 'B', 'RegistrarDevolucion(modulo, pedido)'),
      nota('Exigir(PedidosRealizadosEditar) · el pedido debe estar Entregado', 'B'),
      c('B', 'D', 'RegistrarDevolucion(idPedido, idCliente)'),
      nota('Una transacción: abre MantenimientoPrenda y pasa a En limpieza solo las prendas que siguen En uso por ese cliente', 'D'),
      r('D', 'B', 'cantidad de prendas devueltas'),
      { alt: 'Ninguna prenda devuelta (ya registrada)', pasos: [r('B', 'F', 'AppException(devolucion_ya_hecha)')],
        sino: [{ etiqueta: 'Devolución registrada', pasos: [
          c('B', 'H', 'RegistrarCambios(idPedido, "DEVOLUCION")'),
          r('B', 'F', 'ok'),
          r('F', 'L', 'Prendas en limpieza: la cuenta del cliente queda desbloqueada (PN04 inspecciona)')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN01_CU07_CancelarPedido', titulo: 'PN01 · CU04-VEN Cancelar pedido (patrón Command)',
    participantes: [A('V', 'Vendedor'), P('F', 'PedidosVenta'), P('I', 'InvocadorPedido'), P('K', 'CancelacionCommand'), P('B', 'BLL.Pedido'), P('D', 'DAL.Pedido')],
    pasos: [
      c('V', 'F', 'Selecciona un pedido Pendiente, indica el motivo y pulsa Cancelar'),
      c('F', 'I', 'TomarOrden(new CancelacionCommand(pedido, motivo))'),
      c('F', 'I', 'ProcesarOrdenes()'),
      c('I', 'K', 'Ejecutar()'),
      c('K', 'B', 'Cancelar(modulo, pedido, motivo)'),
      nota('Exigir(PedidosVentaEditar) · pedido.PuedeCancelarse() · el motivo es obligatorio', 'B'),
      { alt: 'No está Pendiente o falta el motivo', pasos: [r('B', 'F', 'AppException(cancelar_estado / cancelar_sin_motivo)'), r('F', 'V', 'Informa el error')],
        sino: [{ etiqueta: 'Válido', pasos: [
          c('B', 'D', 'Cancelar(idPedido, motivo)  [libera las prendas a Disponible]'),
          r('B', 'F', 'ok'),
          r('F', 'V', 'Pedido Cancelado (se puede des-cancelar si las prendas siguen disponibles)')
        ] }] }
    ]
  },

  // ───────────────────────────── PN02 — Comercialización de la suscripción ─────────────────────────────
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU01_VTA_GestionarSuscripcion', titulo: 'PN02 · CU01-VTA Gestionar suscripción (contratación)',
    participantes: [A('V', 'Vendedor'), P('F', 'NuevaContratacionForm'), P('B', 'BLL.Contratacion'), P('DC', 'DAL.Cliente / DAL.PlanSuscripcion'), P('D', 'DAL.Contratacion')],
    pasos: [
      c('V', 'F', 'Elige cliente, plan y modalidad (Mensual, Trimestral, Anual)'),
      c('F', 'B', 'CrearContratacion(modulo, idCliente, idPlan, modalidad)'),
      nota('Exigir(ClientesEditar)', 'B'),
      c('B', 'DC', 'ObtenerPorId(idCliente) · ObtenerPorId(idPlan)'),
      { alt: 'Cliente inexistente, plan inexistente o inactivo', pasos: [r('B', 'F', 'AppException(cliente_inexistente / plan_inexistente)')],
        sino: [{ etiqueta: 'Datos válidos', pasos: [
          nota('ValidarCupo: el plan debe alcanzar para las prendas que el cliente tiene en uso', 'B'),
          c('B', 'D', 'ObtenerPendientesDePago()'),
          { alt: 'El cliente ya tiene una contratación pendiente', pasos: [r('B', 'F', 'AppException(pendiente_existente)')],
            sino: [{ etiqueta: 'Sin pendientes', pasos: [
              c('B', 'D', 'Alta(contratación: estado PendientePago, IdVendedor)'),
              r('D', 'B', 'idContratacion'),
              r('B', 'F', 'idContratacion'),
              r('F', 'V', 'Contratación pendiente de pago: derivar a Caja (la suscripción todavía no está vigente)')
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU01_CAJ_GestionarCobro', titulo: 'PN02 · CU01-CAJ Gestionar cobro (incluye CU02-CAJ Emitir comprobante)',
    participantes: [A('C', 'Caja'), P('F', 'ContratacionesPendientesForm'), P('B', 'BLL.Contratacion'), P('PD', 'PoliticaDescuento'), P('CB', 'BLL.Cliente'), P('D', 'DAL.Contratacion'), P('DC', 'DAL.Cliente')],
    pasos: [
      c('C', 'F', 'Consulta la cola, elige una contratación e indica el medio de pago'),
      c('F', 'B', 'ConfirmarPago(modulo, idContratacion, medioPago)'),
      nota('Exigir(CajaEditar) · medio de pago obligatorio · contratación PendientePago · plan activo · ValidarCupo', 'B'),
      c('B', 'PD', 'Resolver(bruto = Precio × meses, promociones vigentes, crédito de referido)'),
      r('PD', 'B', 'un solo descuento (el mayor) y total'),
      c('B', 'D', 'ConfirmarPago(id, idCaja, medio, comprobante, total, descuento, idPromocion)  [reclamo atómico: WHERE Estado = PendientePago]'),
      { alt: 'Otra sesión de Caja ya la resolvió', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(cobrar_concurrente)'), r('F', 'C', 'Actualizá la cola de Caja')],
        sino: [{ etiqueta: 'Reclamo obtenido (comprobante emitido)', pasos: [
          c('B', 'CB', 'ActivarSuscripcionDesdeContratacion(cliente, idPlan, modalidad, consumoCredito)'),
          nota('Builder según modalidad: vencimiento = fin del período vigente + 1, 3 o 12 meses; limpia gracia y pausa', 'CB'),
          c('CB', 'DC', 'EjecutarTransaccion: ModificarEnTx + ConsumirCreditoEnTx + SumarCreditoEnTx (referente)'),
          { alt: 'Falla la activación', pasos: [c('B', 'D', 'ReabrirPago(id)  [compensación: vuelve a PendientePago]'), r('B', 'F', 'error (o cobro_sin_activar si no se pudo reabrir)')],
            sino: [{ etiqueta: 'Activada', pasos: [r('B', 'F', 'LiquidacionContratacion'), r('F', 'C', 'Suscripción formalizada y comprobante emitido')] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU02_CAJ_EmitirComprobante', titulo: 'PN02 · CU02-CAJ Emitir comprobante',
    participantes: [A('C', 'Caja'), P('B', 'BLL.Contratacion'), P('D', 'DAL.Contratacion')],
    pasos: [
      nota('Se dispara dentro de ConfirmarPago (CU01-CAJ): no requiere una acción independiente de Caja', 'C', 'B'),
      c('B', 'D', 'ConfirmarPago(..., numeroComprobante = CMP-{id:D6}-{yyyyMMdd}, fechaComprobante)'),
      r('D', 'B', 'contratación Pagada con número y fecha de comprobante'),
      r('B', 'C', 'comprobante visible en el mensaje de confirmación')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN02_CU03_CAJ_CancelarContratacion', titulo: 'PN02 · CU03-CAJ Cancelar contratación (intentos fallidos)',
    participantes: [A('C', 'Caja'), P('F', 'ContratacionesPendientesForm'), P('B', 'BLL.Contratacion'), P('D', 'DAL.Contratacion')],
    pasos: [
      c('C', 'F', 'Registra el intento fallido de una contratación'),
      c('F', 'B', 'RegistrarIntentoFallido(modulo, idContratacion)'),
      nota('Exigir(CajaEditar)', 'B'),
      c('B', 'D', 'IncrementarIntento(id)  [solo si sigue PendientePago y IntentosPago < 3]'),
      { alt: 'La contratación ya no está pendiente', pasos: [r('D', 'B', '-1'), r('B', 'F', 'AppException(cobrar_concurrente)')],
        sino: [{ etiqueta: 'Intento registrado', pasos: [
          r('D', 'B', 'intentos'),
          { alt: 'intentos = 3', pasos: [c('B', 'D', 'Cancelar(id)  [WHERE Estado = PendientePago]'), r('B', 'F', 'Contratación Cancelada')],
            sino: [{ etiqueta: 'intentos < 3', pasos: [r('B', 'F', 'Sigue pendiente, queda disponible otro intento')] }] }
        ] }] }
    ]
  },

  // ───────────────────────────── PN03 — Métricas, promociones y toma de decisiones ─────────────────────────────
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_GER_SugerirPromocion', titulo: 'PN03 · CU01-GER Sugerir promoción',
    participantes: [A('G', 'Gerencia'), P('F', 'SugerirPromocionForm'), P('AN', 'BLL.AnalisisPromociones'), P('B', 'BLL.SugerenciaPromocion'), P('D', 'DAL.SugerenciaPromocion')],
    pasos: [
      { opt: 'Toma una idea del análisis de datos', pasos: [
        c('G', 'F', 'Pulsa "Desde el análisis…"'),
        c('F', 'AN', 'Detectar()'),
        r('AN', 'F', 'candidatas (abandono por plan, baja rotación por categoría)'),
        r('F', 'G', 'Precarga plan o categoría, motivo, tipo y beneficio estimado')
      ] },
      c('G', 'F', 'Indica destino (plan o categoría), motivo, tipo de descuento y beneficio'),
      c('F', 'B', 'Crear(modulo, sugerencia)'),
      nota('Exigir(SugerenciaPromocion) · destino único (plan o categoría) · motivo obligatorio · beneficio válido', 'B'),
      { alt: 'Datos inválidos', pasos: [r('B', 'F', 'AppException(motivo específico)'), r('F', 'G', 'Informa y retoma la carga')],
        sino: [{ etiqueta: 'Válidos', pasos: [
          c('B', 'D', 'Alta(sugerencia: estado Pendiente)'),
          r('B', 'F', 'ok'),
          r('F', 'G', 'Sugerencia registrada para Administración')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_ADM_GestionarPromociones', titulo: 'PN03 · CU01-ADM Gestionar promociones (alta desde sugerencia)',
    participantes: [A('A', 'Administración'), P('F', 'PromocionesAdministracionForm'), P('B', 'BLL.Promocion'), P('DS', 'DAL.SugerenciaPromocion'), P('D', 'DAL.Promocion')],
    pasos: [
      c('A', 'F', 'Consulta las sugerencias y elige "Alta desde sugerencia"'),
      c('F', 'B', 'CrearDesdeSugerencia(modulo, promocion, idSugerencia)'),
      nota('Exigir(PromocionesAdminEditar) · destino único · valor > 0 (≤ 100 si es Porcentaje) · fin ≥ inicio', 'B'),
      c('B', 'DS', 'MarcarEvaluada(idSugerencia)  [reclamo: solo si sigue Pendiente]'),
      { alt: 'Otra sesión ya la evaluó', pasos: [r('DS', 'B', 'false'), r('B', 'F', 'AppException(sugerencia_evaluada)')],
        sino: [{ etiqueta: 'Reclamo obtenido', pasos: [
          c('B', 'D', 'Alta(promoción: estado EnRevisionContable)'),
          { alt: 'Falla el alta', pasos: [c('B', 'DS', 'ReabrirEvaluacion(idSugerencia)  [compensación]'), r('B', 'F', 'error')],
            sino: [{ etiqueta: 'Alta correcta', pasos: [r('B', 'F', 'idPromocion'), r('F', 'A', 'Promoción pendiente de revisión contable')] }] }
        ] }] },
      nota('Variantes: CrearManual (sin sugerencia) · Modificar y Reformular (UPDATE solo si sigue EnRevisionContable) · Desactivar (Vigente → Desactivada)', 'A', 'D')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_CONT_AnalizarPromocion', titulo: 'PN03 · CU01-CONT Analizar promoción',
    participantes: [A('K', 'Contabilidad'), P('F', 'PromocionesContabilidadForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion')],
    pasos: [
      c('K', 'F', 'Consulta la cola, analiza margen e impacto, ingresa la observación y decide'),
      { alt: 'Aprueba', pasos: [c('F', 'B', 'AprobarContable(modulo, promocion, observacion)')],
        sino: [{ etiqueta: 'Rechaza', pasos: [c('F', 'B', 'RechazarContable(modulo, promocion, observacion)')] }] },
      nota('Exigir(PromocionesContableEditar) · la observación es obligatoria · solo desde EnRevisionContable', 'B'),
      c('B', 'D', 'CambiarEstado(id, esperado = EnRevisionContable, nuevo = Vigente o RechazadaContabilidad, observación)'),
      { alt: 'Otra sesión ya la resolvió', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(estado_concurrente)')],
        sino: [{ etiqueta: 'Estado cambiado', pasos: [r('B', 'F', 'ok'), r('F', 'K', 'Promoción Vigente (se aplica al cobro) o Rechazada (vuelve a Administración)')] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU01_VEN_SugerirBaja', titulo: 'PN03 · CU01-VEN Sugerir baja de promoción',
    participantes: [A('V', 'Vendedor'), P('F', 'PromocionesVigentesForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion')],
    pasos: [
      c('V', 'F', 'Consulta las promociones vigentes, elige una e indica el motivo'),
      c('F', 'B', 'SugerirBaja(modulo, promocion, motivo)'),
      nota('Exigir(PromocionesVigentesEditar) · el motivo es obligatorio · solo desde Vigente', 'B'),
      c('B', 'D', 'SolicitarBaja(id, motivo)  [UPDATE condicionado a Vigente]'),
      { alt: 'Ya no está Vigente', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(estado_concurrente)')],
        sino: [{ etiqueta: 'Solicitada', pasos: [r('B', 'F', 'ok'), r('F', 'V', 'Promoción en Baja solicitada: Administración la resuelve')] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU02_ADM_ResolverBaja', titulo: 'PN03 · CU02-ADM Resolver baja de promoción',
    participantes: [A('A', 'Administración'), P('F', 'PromocionesAdministracionForm'), P('B', 'BLL.Promocion'), P('D', 'DAL.Promocion')],
    pasos: [
      c('A', 'F', 'Consulta las promociones con baja solicitada y revisa el motivo'),
      { alt: 'Aprueba la baja', pasos: [c('F', 'B', 'AprobarBaja(modulo, promocion)'), c('B', 'D', 'CambiarEstado(id, BajaSolicitada → Desactivada)')],
        sino: [{ etiqueta: 'Rechaza la baja (exige motivo)', pasos: [c('F', 'B', 'RechazarBaja(modulo, promocion, motivo)'), c('B', 'D', 'CambiarEstado(id, BajaSolicitada → Vigente)  [conserva la observación de Contabilidad]')] }] },
      { alt: 'Otra sesión ya la resolvió', pasos: [r('D', 'B', 'false'), r('B', 'F', 'AppException(estado_concurrente)')],
        sino: [{ etiqueta: 'Resuelta', pasos: [r('B', 'F', 'ok'), r('F', 'A', 'Promoción Desactivada o de nuevo Vigente')] }] }
    ]
  },

  // ───────────────────────────── PN04 — Inspección de devolución ─────────────────────────────
  {
    tipo: 'secuencia', id: 'DSS_PN04_CU01_DEP_InspeccionarDevolucion', titulo: 'PN04 · CU-DEP-01 Inspeccionar devolución',
    participantes: [A('D', 'Depósito'), P('F', 'InspeccionDevolucionForm'), P('PB', 'BLL.Prenda'), P('CG', 'BLL.CargoPrenda'), P('DA', 'DAL (Prenda, Mantenimiento, CargoPrenda)'), P('LE', 'BLL.ListaEspera')],
    pasos: [
      c('D', 'F', 'Abre la cola de prendas En limpieza'),
      c('F', 'PB', 'ObtenerEnLimpieza()'),
      r('PB', 'F', 'prendas En limpieza'),
      c('D', 'F', 'Inspecciona una prenda y decide'),
      { alt: 'Desgaste normal: Aprobar reingreso', pasos: [
        c('F', 'PB', 'CambiarEstado(modulo, prenda, Disponible, actor, viaInspeccion: true)'),
        nota('Exigir(StockEditar) · patrón State: EnLimpieza → Disponible', 'PB'),
        c('PB', 'DA', 'CambiarEstado(id, esperado = EnLimpieza, nuevo = Disponible)  [UPDATE condicionado] · CerrarMantenimiento(id)'),
        c('PB', 'LE', 'NotificarSiCorresponde(idPrenda)  [reserva 48 h si hay lista de espera]'),
        r('PB', 'F', 'ok'), r('F', 'D', 'Prenda de nuevo en el catálogo, sin cargo')
      ], sino: [{ etiqueta: 'Daño irreparable: Dar de baja y cobrar', pasos: [
        c('F', 'CG', 'RegistrarCargo(modulo, prenda, motivo, monto, actor)  [primero el cargo]'),
        nota('Exigir(StockEditar) · la prenda debe tener último cliente · motivo obligatorio · monto > 0', 'CG'),
        c('CG', 'DA', 'Alta(cargo: Pendiente, IdCliente = último cliente)'),
        c('F', 'PB', 'CambiarEstado(modulo, prenda, Baja, actor, viaInspeccion: true)  [después la baja]'),
        c('PB', 'DA', 'CambiarEstado(id, esperado = EnLimpieza, nuevo = Baja)'),
        r('PB', 'F', 'ok'), r('F', 'D', 'Prenda dada de baja: el cargo se suma al próximo cobro del cliente')
      ] }] },
      { alt: 'Otra sesión ya la resolvió', pasos: [r('DA', 'PB', 'UPDATE sin filas'), r('PB', 'F', 'AppException(estado_cambio)'), r('F', 'D', 'Actualizá la cola')] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN04_CU02_DEP_ReportarPrendaPerdida', titulo: 'PN04 · CU-DEP-02 Reportar prenda perdida',
    participantes: [A('D', 'Depósito'), P('F', 'PedidosRealizados'), P('CG', 'BLL.CargoPrenda'), P('PB', 'BLL.Prenda'), P('DA', 'DAL (CargoPrenda, Prenda)')],
    pasos: [
      c('D', 'F', 'Ve el detalle de prendas del pedido, elige una En uso y pulsa Reportar Pérdida'),
      c('D', 'F', 'Indica el motivo y el monto (precargado con el precio de reposición)'),
      c('F', 'CG', 'RegistrarCargo(modulo, prenda, motivo, monto, actor)'),
      nota('La prenda debe tener último cliente registrado · motivo obligatorio · monto > 0', 'CG'),
      { alt: 'Sin último cliente o datos inválidos', pasos: [r('CG', 'F', 'AppException(sin_cliente / motivo_requerido / monto_invalido)'), r('F', 'D', 'Rechaza la operación')],
        sino: [{ etiqueta: 'Cargo registrado', pasos: [
          c('CG', 'DA', 'Alta(cargo: Pendiente)'),
          c('F', 'PB', 'CambiarEstado(modulo, prenda, Baja, actor, viaFlujoPerdida: true)'),
          c('PB', 'DA', 'CambiarEstado(id, esperado = EnUso, nuevo = Baja)'),
          r('PB', 'F', 'ok'),
          r('F', 'D', 'Prenda dada de baja: el cargo se suma al próximo cobro')
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_N01_CU04_GestionarPlanes', titulo: 'N01 · CU04-VEN Gestionar planes de suscripción',
    participantes: [A('V', 'Vendedor'), P('F', 'Planes'), P('B', 'BLL.PlanSuscripcion'), P('D', 'DAL.PlanSuscripcion')],
    pasos: [
      c('V', 'F', 'Completa nombre, límite de prendas y precio mensual, y pulsa Guardar Plan'),
      c('F', 'B', 'Alta(modulo, plan)  [o Modificar(modulo, plan) si edita uno existente]'),
      nota('Exigir(PlanSuscripcionesEditar) · nombre obligatorio (solo letras) · límite de prendas válido · precio mayor a cero', 'B'),
      { alt: 'Datos inválidos', pasos: [r('B', 'F', 'AppException(nombre_requerido / limite_invalido / precio_cero)'), r('F', 'V', 'Informa el error')],
        sino: [{ etiqueta: 'Datos válidos', pasos: [
          c('B', 'D', 'Alta(plan) / Modificar(plan)'),
          r('B', 'F', 'ok'),
          r('F', 'V', 'Plan guardado (queda activo)')
        ] }] },
      c('V', 'F', 'Pulsa Desactivar Plan'),
      c('F', 'B', 'Desactivar(modulo, plan)'),
      { alt: 'El plan tiene clientes asignados', pasos: [r('B', 'F', 'AppException(tiene_clientes)')],
        sino: [{ etiqueta: 'Sin clientes', pasos: [c('B', 'D', 'Desactivar(idPlan)'), r('B', 'F', 'ok')] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_PN03_CU02_GER_ConsultarAnalitica', titulo: 'PN03 · CU02-GER Consultar analítica de negocio',
    participantes: [A('G', 'Gerencia'), P('F', 'Analisis*Form / ReporteVentasVendedorForm'), P('B', 'BLL.Analisis* / ReporteVentasVendedor'), P('D', 'DAL (Cliente, Pedido, Prenda, Mantenimiento)'), P('X', 'GeneradorReporte / Exportador')],
    pasos: [
      c('G', 'F', 'Elige el reporte (abandono, ventas por vendedor, rotación, mantenimiento, escasez o recomendación)'),
      { opt: 'Análisis de abandono: elige el criterio de riesgo (Strategy)', pasos: [c('F', 'B', 'CambiarEstrategia(estrategia)')] },
      { opt: 'Escasez de stock: indica el umbral mínimo', pasos: [nota('Detectar(umbralMinimo)', 'F', 'B')] },
      c('G', 'F', 'Pulsa Generar'),
      c('F', 'B', 'Detectar() / Obtener() / Recomendar(idCliente)'),
      c('B', 'D', 'Consulta los datos agregados'),
      r('D', 'B', 'datos'),
      r('B', 'F', 'lista de resultados (clientes en riesgo, desempeño, prendas, combinaciones talle+categoría)'),
      r('F', 'G', 'Muestra la grilla (sin resultados: lo informa)'),
      { opt: 'Exporta el resultado', pasos: [
        c('G', 'F', 'Pulsa Exportar a PDF o Guardar como .CSV'),
        c('F', 'X', 'CrearExportador(formato) y exportar el ReporteExportable'),
        r('X', 'G', 'Archivo generado')
      ] }
    ]
  },
];

// Sub-diagrama auxiliar: aviso cuando otra sesión ya cobró los cargos pendientes.
function alt_cargo() {
  return { alt: 'Otra sesión ya cobró los cargos pendientes', pasos: [r('D', 'H2', 'MarcarCobradosEnTx = false'), r('H2', 'B', 'AppException(cargo_concurrente): se revierte todo el cobro')],
    sino: [{ etiqueta: 'Cargos liquidados', pasos: [r('D', 'H2', 'ok')] }] };
}
