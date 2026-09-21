// Diagramas de clases. Las clases, atributos, métodos, herencias y dependencias (constructor) se leen
// del código real (lib/csharp.js); acá solo se elige QUÉ clases entran en cada diagrama.
// Los enums (estados, modalidad, tipo de descuento) no se dibujan como cajas: aparecen como tipo de los
// atributos y sus valores se describen en el documento.
const E = (ref, extra = {}) => ({ ref, ...extra });

module.exports = [
  {
    tipo: 'clases', id: 'CLASES_global_dominio_a', titulo: 'Diagrama de clases — Dominio: clientes, suscripciones y promociones', columnas: 4,
    clases: [
      E('BE.Cliente', { attrs: 'keys' }), E('BE.PlanSuscripcion', { attrs: 'all' }), E('BE.Contratacion', { attrs: 'keys' }), E('BE.Renovacion', { attrs: 'keys' }),
      E('BE.Cobro', { attrs: 'keys' }), E('BE.Promocion', { attrs: 'keys' }), E('BE.SugerenciaPromocion', { attrs: 'keys' }), E('BE.Empleado', { attrs: 'keys' })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_global_dominio_b', titulo: 'Diagrama de clases — Dominio: pedidos y prendas', columnas: 4,
    clases: [
      E('BE.Pedido', { attrs: 'keys' }), E('BE.PedidoHistorial', { attrs: 'keys' }), E('BE.Prenda', { attrs: 'keys' }), E('BE.MantenimientoPrenda', { attrs: 'all' }),
      E('BE.CargoPrenda', { attrs: 'keys' }), E('BE.ListaEspera', { attrs: 'keys' }), E('BE.Cliente', { attrs: ['IdCliente', 'Nombre', 'Apellido'] }), E('BE.Empleado', { attrs: ['IdEmpleado', 'Nombre'] })
    ]
  },

  // ───────────── N01 ─────────────
  {
    tipo: 'clases', id: 'CLASES_n01_clientes_suscripciones', procesos: ['N01'], titulo: 'Diagrama de clases — N01 Clientes y suscripciones', columnas: 3,
    clases: [
      E('BE.Cliente', { attrs: 'all' }), E('BE.PlanSuscripcion', { attrs: 'all' }), E('BE.Renovacion', { attrs: 'all' }), E('BE.Cobro', { attrs: 'all' }),
      E('BLL.Cliente', { metodos: ['Alta', 'Modificar', 'Baja', 'ActivarSuscripcionDesdeContratacion', 'ReanudarPausa', 'ObtenerEstadoComercial'] }),
      E('BLL.Renovacion', { metodos: ['Procesar', 'ObtenerHistorial'] }), E('BLL.Cobro', { metodos: ['Procesar', 'ObtenerHistorial'] }),
      E('BLL.PlanSuscripcion', { metodos: 'all' }), E('IPlanSuscripcionDAL', { metodos: [] }), E('IClienteDAL', { metodos: ['SumarCreditoEnTx', 'ConsumirCreditoEnTx', 'EjecutarTransaccion'] })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_patron_builder_suscripcion', procesos: ['N01'], titulo: 'Patrón Builder — Activación de suscripción por modalidad de cobro', columnas: 3,
    clases: [
      E('BE.Builders.DirectorSuscripcion', { metodos: 'all' }), E('BE.Builders.SuscripcionBuilder', { metodos: 'all', attrs: 'none' }),
      E('BE.Builders.SuscripcionMensualBuilder'), E('BE.Builders.SuscripcionTrimestralBuilder'), E('BE.Builders.SuscripcionAnualBuilder'),
      E('BE.Builders.SuscripcionBuilderFactory', { metodos: 'all' }), E('BE.Builders.Suscripcion', { attrs: 'all' })
    ],
    relaciones: [{ tipo: 'depende', de: 'SuscripcionBuilderFactory', a: 'SuscripcionBuilder' }]
  },
  {
    tipo: 'clases', id: 'CLASES_patron_chain_renovacion', procesos: ['N01'], titulo: 'Patrón Chain of Responsibility — Renovación de suscripción', columnas: 3,
    clases: [
      E('BLL.Renovacion', { metodos: ['Procesar'] }), E('BLL.Manejadores.ManejadorRenovacion', { metodos: 'all' }),
      E('BLL.Manejadores.VerificarVencimientoHandler', { metodos: 'all' }), E('BLL.Manejadores.IntentarRenovarHandler', { metodos: 'all' }),
      E('BLL.Manejadores.CambioPlanHandler', { metodos: 'all' }), E('BLL.Manejadores.PausarSuscripcionHandler', { metodos: 'all' }),
      E('BLL.Manejadores.BajaSuscripcionHandler', { metodos: 'all' }), E('BLL.Manejadores.ContextoRenovacion', { attrs: 'all' })
    ],
    relaciones: [
      { tipo: 'asocia', de: 'BLL_Renovacion', a: 'BLL_Manejadores_VerificarVencimientoHandler', etiqueta: 'primer eslabón', mult: '1' },
      { tipo: 'asocia', de: 'BLL_Manejadores_ManejadorRenovacion', a: 'BLL_Manejadores_ManejadorRenovacion', etiqueta: 'sucesor', mult: '0..1' }
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_patron_chain_cobro', procesos: ['N01'], titulo: 'Patrón Chain of Responsibility — Cobro recurrente de la suscripción', columnas: 3,
    clases: [
      E('BLL.Cobro', { metodos: ['Procesar'] }), E('BLL.Manejadores.ManejadorCobro', { metodos: 'all' }),
      E('BLL.Manejadores.DetectarCobroHandler', { metodos: 'all' }), E('BLL.Manejadores.ProcesarPagoHandler', { metodos: 'all' }),
      E('BLL.Manejadores.AplicarGraciaHandler', { metodos: 'all' }), E('BLL.Manejadores.SuspenderHandler', { metodos: 'all' }),
      E('BLL.Manejadores.ContextoCobro', { attrs: 'all' }), E('BE.PoliticaDescuento', { metodos: 'all' })
    ],
    relaciones: [
      { tipo: 'asocia', de: 'BLL_Cobro', a: 'BLL_Manejadores_DetectarCobroHandler', etiqueta: 'primer eslabón', mult: '1' },
      { tipo: 'asocia', de: 'BLL_Manejadores_ManejadorCobro', a: 'BLL_Manejadores_ManejadorCobro', etiqueta: 'sucesor', mult: '0..1' },
      { tipo: 'depende', de: 'BLL_Manejadores_ProcesarPagoHandler', a: 'PoliticaDescuento' }
    ]
  },

  // ───────────── PN01 ─────────────
  {
    tipo: 'clases', id: 'CLASES_pn01_pedidos', procesos: ['PN01'], titulo: 'Diagrama de clases — PN01 Armar pedido', columnas: 3,
    clases: [
      E('BE.Pedido', { attrs: 'all' }), E('BE.Prenda', { attrs: 'all' }), E('BE.ListaEspera', { attrs: 'all' }),
      E('BE.Cliente', { attrs: ['IdCliente', 'Nombre', 'Apellido', 'IdPlan', 'FechaVencimiento', 'FechaPausaHasta', 'StockUtilizado'] }),
      E('BE.PlanSuscripcion', { attrs: ['IdPlan', 'Nombre', 'LimitePrendas'] }),
      E('BLL.Pedido', { metodos: ['CrearPedido', 'ValidarPuedeArmarPedido', 'ValidarCupoDisponible', 'ReservarPrendas', 'Despachar', 'MarcarEntregado', 'RegistrarDevolucion', 'Cancelar', 'DesCancelar'] }),
      E('BLL.Prenda', { metodos: ['ObtenerDisponibles', 'VerificarDisponibilidad', 'CambiarEstado'] }),
      E('BLL.ListaEspera', { metodos: ['EstaReservadaParaOtro', 'CerrarSiReservada', 'NotificarSiCorresponde'] }),
      E('BLL.Cliente', { metodos: [] }), E('IClienteDAL', { metodos: [] }), E('IPrendaDAL', { metodos: [] }), E('IPedidoHistorialDAL', { metodos: [] }),
      E('IPedidoDAL', { metodos: ['Alta', 'Despachar', 'MarcarEntregado', 'RegistrarDevolucion', 'Cancelar', 'DesCancelar'] })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_patron_command_pedido', procesos: ['PN01'], titulo: 'Patrón Command — Cancelar pedido y registrar devolución', columnas: 3,
    clases: [
      E('BLL.Comandos.InvocadorPedido', { metodos: 'all' }), E('BLL.Comandos.PedidoCommand', { metodos: 'all' }),
      E('BLL.Comandos.CancelacionCommand', { metodos: 'all' }), E('BLL.Comandos.DevolucionCommand', { metodos: 'all' }),
      E('BLL.Interfaces.IPedidoService', { metodos: ['Cancelar', 'RegistrarDevolucion'] }), E('BLL.Pedido', { metodos: ['Cancelar', 'RegistrarDevolucion'] }),
      E('BE.Pedido', { attrs: ['IdPedido', 'Estado'] })
    ],
    relaciones: [
      { tipo: 'asocia', de: 'BLL_Comandos_InvocadorPedido', a: 'BLL_Comandos_PedidoCommand', etiqueta: 'órdenes', mult: '*' },
      { tipo: 'asocia', de: 'BLL_Comandos_PedidoCommand', a: 'BLL_Interfaces_IPedidoService', etiqueta: 'receptor', mult: '1' },
      { tipo: 'asocia', de: 'BLL_Comandos_PedidoCommand', a: 'Pedido', etiqueta: 'pedido', mult: '1' },
      { tipo: 'implementa', de: 'BLL_Pedido', a: 'BLL_Interfaces_IPedidoService' }
    ]
  },

  // ───────────── PN02 ─────────────
  {
    tipo: 'clases', id: 'CLASES_pn02_contrataciones', procesos: ['PN02'], titulo: 'Diagrama de clases — PN02 Comercialización de la suscripción', columnas: 3,
    clases: [
      E('BE.Contratacion', { attrs: 'all' }), E('BE.Cliente', { attrs: ['IdCliente', 'Nombre', 'Apellido', 'IdPlan', 'FechaVencimiento', 'DescuentoProximoCobro'] }),
      E('BE.PlanSuscripcion', { attrs: ['IdPlan', 'Nombre', 'LimitePrendas', 'Precio'] }), E('BE.Promocion', { attrs: ['IdPromocion', 'Nombre', 'TipoDescuento', 'Valor'] }),
      E('BE.PoliticaDescuento', { metodos: 'all' }),
      E('BLL.Cliente', { metodos: [] }),
      E('BLL.Contratacion', { metodos: ['CrearContratacion', 'ConfirmarPago', 'CalcularImporte', 'RegistrarIntentoFallido'] }),
      E('IContratacionDAL', { metodos: ['Alta', 'ConfirmarPago', 'IncrementarIntento', 'ReabrirPago', 'Cancelar'] })
    ]
  },

  // ───────────── PN03 ─────────────
  {
    tipo: 'clases', id: 'CLASES_pn03_promociones', procesos: ['PN03'], titulo: 'Diagrama de clases — PN03 Métricas, promociones y toma de decisiones', columnas: 3,
    clases: [
      E('BE.SugerenciaPromocion', { attrs: 'all' }), E('BE.Promocion', { attrs: 'all' }), E('BE.PlanSuscripcion', { attrs: ['IdPlan', 'Nombre', 'Precio'] }), E('BE.CandidataSugerencia', { attrs: 'all' }),
      E('BLL.SugerenciaPromocion', { metodos: ['Crear', 'ObtenerPendientes'] }), E('BLL.AnalisisPromociones', { metodos: 'all' }),
      E('BLL.Promocion', { metodos: ['CrearDesdeSugerencia', 'CrearManual', 'Modificar', 'Reformular', 'Desactivar', 'AprobarContable', 'RechazarContable', 'SugerirBaja', 'AprobarBaja', 'RechazarBaja'] }),
      E('IPromocionDAL', { metodos: ['Alta', 'Modificar', 'CambiarEstado', 'SolicitarBaja'] }), E('ISugerenciaPromocionDAL', { metodos: ['MarcarEvaluada', 'ReabrirEvaluacion'] })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_patron_strategy_abandono', procesos: ['PN03'], titulo: 'Patrón Strategy — Análisis de abandono (criterio de riesgo intercambiable)', columnas: 3,
    clases: [
      E('BLL.AnalisisAbandono', { metodos: 'all' }), E('BLL.Estrategias.EstrategiaRiesgo', { metodos: 'all' }),
      E('BLL.Estrategias.EstrategiaInactividadPura', { metodos: 'all' }), E('BLL.Estrategias.EstrategiaVencimientoInactividad', { metodos: 'all' }),
      E('BLL.Estrategias.EstrategiaClienteNuevoInactivo', { metodos: 'all' }), E('BE.ClienteEnRiesgo', { attrs: 'all' })
    ],
    relaciones: [{ tipo: 'asocia', de: 'BLL_AnalisisAbandono', a: 'BLL_Estrategias_EstrategiaRiesgo', etiqueta: 'estrategia activa', mult: '1' }]
  },

  // ───────────── PN04 ─────────────
  {
    tipo: 'clases', id: 'CLASES_pn04_devolucion', procesos: ['PN04'], titulo: 'Diagrama de clases — PN04 Inspección de devolución', columnas: 3,
    clases: [
      E('BE.Prenda', { attrs: 'all' }), E('BE.MantenimientoPrenda', { attrs: 'all' }), E('BE.CargoPrenda', { attrs: 'all' }), E('BE.Cliente', { attrs: ['IdCliente', 'Nombre', 'Apellido'] }),
      E('BLL.ListaEspera', { metodos: [] }), E('BLL.Prenda', { metodos: ['ObtenerEnLimpieza', 'CambiarEstado', 'ObtenerHistorialMantenimiento'] }), E('BLL.CargoPrenda', { metodos: ['RegistrarCargo', 'ObtenerPendientesPorCliente'] }),
      E('IPrendaDAL', { metodos: ['CambiarEstado'] }), E('ICargoPrendaDAL', { metodos: ['Alta', 'ObtenerPendientesPorCliente', 'MarcarCobradosEnTx'] })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_patron_state_prenda', procesos: ['PN04'], titulo: 'Patrón State — Ciclo de vida de una prenda', columnas: 3,
    clases: [
      E('BE.Prenda', { attrs: ['IdPrenda', 'Nombre', 'Estado'], metodos: ['ControlarEstado', 'EstaDisponible'] }),
      E('BE.Estados.Estado', { metodos: 'all' }), E('BE.Estados.EstadoDisponible', { metodos: 'all' }), E('BE.Estados.EstadoEnUso', { metodos: 'all' }),
      E('BE.Estados.EstadoEnLimpieza', { metodos: 'all' }), E('BE.Estados.EstadoBaja', { metodos: 'all' })
    ],
    relaciones: [{ tipo: 'asocia', de: 'Prenda', a: 'Estado', etiqueta: 'estado actual', mult: '1' }]
  }
];
