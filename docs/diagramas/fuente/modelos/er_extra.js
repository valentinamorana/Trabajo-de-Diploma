// DER por módulo, para el modelo de datos global (G08): la BD tiene 31 tablas y en una sola hoja no se leen.
module.exports = [
  { tipo: 'er', id: 'DER_seg_usuarios', titulo: 'DER — Módulo de usuarios: cuentas, empleados, preferencias y recuperación', columnas: 3,
    tablas: ['Usuario', 'Usuario_Seguridad', 'Empleado', 'Preferencia', 'HistorialUsuario', 'ClaveRecuperacion'] },
  { tipo: 'er', id: 'DER_seg_permisos_idiomas', titulo: 'DER — Módulo de permisos (Composite) e idiomas', columnas: 3,
    tablas: ['Permiso', 'PermisoRelacion', 'RolPermiso', 'Control', 'ControlMapeado', 'Idioma', 'Traduccion'] },
  { tipo: 'er', id: 'DER_seg_auditoria', titulo: 'DER — Módulo de auditoría e integridad', columnas: 4,
    tablas: ['Bitacora', 'BitacoraNegocio', 'DVVertical', 'HistorialIntegridad'] },
  { tipo: 'er', id: 'DER_negocio', titulo: 'DER — Módulos de negocio: clientes, suscripciones, pedidos, prendas y promociones', columnas: 5,
    tablas: ['PlanSuscripcion', 'Cliente', 'Empleado', 'Contratacion', 'Promocion', 'SugerenciaPromocion', 'HistorialRenovacion', 'HistorialCobro', 'Pedido', 'PedidoPrenda', 'PedidoHistorial', 'Prenda', 'MantenimientoPrenda', 'CargoPrenda', 'ListaEspera'] }
,
  { tipo: 'er', id: 'MC_negocio', titulo: 'Modelo conceptual — Módulos de negocio', sinColumnas: true, columnas: 5,
    tablas: ['PlanSuscripcion', 'Cliente', 'Empleado', 'Contratacion', 'Promocion', 'SugerenciaPromocion', 'HistorialRenovacion', 'HistorialCobro', 'Pedido', 'PedidoPrenda', 'PedidoHistorial', 'Prenda', 'MantenimientoPrenda', 'CargoPrenda', 'ListaEspera'] }
];
