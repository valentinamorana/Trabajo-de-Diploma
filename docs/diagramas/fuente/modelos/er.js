// Modelos de DER. Las tablas y columnas salen de fuente/schema.json (extraído de la BD real instalada).
module.exports = [
  {
    tipo: 'er', id: 'DER_global', titulo: 'DER global — WardrobeFlow', columnas: 6,
    tablas: [
      // Seguridad y administración
      'Usuario', 'Usuario_Seguridad', 'Empleado', 'Permiso', 'PermisoRelacion', 'RolPermiso',
      'Control', 'ControlMapeado', 'Idioma', 'Traduccion', 'Preferencia', 'ClaveRecuperacion',
      'HistorialUsuario', 'DVVertical', 'HistorialIntegridad', 'Bitacora', 'BitacoraNegocio',
      // Negocio
      'PlanSuscripcion', 'Cliente', 'HistorialRenovacion', 'HistorialCobro', 'Contratacion',
      'Pedido', 'PedidoPrenda', 'PedidoHistorial', 'Prenda', 'MantenimientoPrenda', 'CargoPrenda',
      'ListaEspera', 'SugerenciaPromocion', 'Promocion'
    ]
  },
  {
    tipo: 'er', id: 'DER_seguridad', titulo: 'DER — Seguridad, usuarios, permisos, idiomas y auditoría', columnas: 4,
    tablas: [
      'Usuario', 'Usuario_Seguridad', 'Empleado', 'Preferencia',
      'Permiso', 'PermisoRelacion', 'RolPermiso', 'ControlMapeado', 'Control',
      'Idioma', 'Traduccion', 'ClaveRecuperacion', 'HistorialUsuario',
      'Bitacora', 'BitacoraNegocio', 'DVVertical', 'HistorialIntegridad'
    ]
  },
  {
    tipo: 'er', id: 'DER_n01_clientes_suscripciones', titulo: 'DER — N01 Clientes y suscripciones', columnas: 3,
    tablas: ['PlanSuscripcion', 'Cliente', 'HistorialRenovacion', 'HistorialCobro', 'CargoPrenda', 'Prenda']
  },
  {
    tipo: 'er', id: 'DER_pn01_pedidos', titulo: 'DER — PN01 Armar pedido', columnas: 3,
    tablas: ['Cliente', 'Empleado', 'Pedido', 'PedidoPrenda', 'PedidoHistorial', 'Prenda', 'ListaEspera', 'MantenimientoPrenda']
  },
  {
    tipo: 'er', id: 'DER_pn02_contrataciones', titulo: 'DER — PN02 Comercialización de la suscripción', columnas: 3,
    tablas: ['Cliente', 'PlanSuscripcion', 'Empleado', 'Contratacion', 'Promocion']
  },
  {
    tipo: 'er', id: 'DER_pn03_promociones', titulo: 'DER — PN03 Métricas, promociones y toma de decisiones', columnas: 3,
    tablas: ['PlanSuscripcion', 'SugerenciaPromocion', 'Promocion', 'Contratacion', 'Cliente']
  },
  {
    tipo: 'er', id: 'DER_pn04_devolucion', titulo: 'DER — PN04 Inspección de devolución', columnas: 3,
    tablas: ['Cliente', 'Pedido', 'PedidoPrenda', 'Prenda', 'MantenimientoPrenda', 'CargoPrenda']
  }
];

// Modelos conceptuales: los mismos DER por proceso, sin columnas (solo entidades y relaciones).
module.exports = module.exports.concat(
  module.exports.filter(m => /^DER_(n01|pn0)/.test(m.id)).map(m => ({
    ...m, id: m.id.replace(/^DER_/, 'MC_'), titulo: m.titulo.replace(/^DER —/, 'Modelo conceptual —'), sinColumnas: true
  }))
);
