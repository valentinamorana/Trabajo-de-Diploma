// Diagramas de casos de uso. Los identificadores (CU01-VEN, CU01-CAJ, ...) son los del documento de la tesis.
module.exports = [
  {
    tipo: 'casos', id: 'CU_n01_clientes_suscripciones', titulo: 'Casos de uso — N01 Clientes y suscripciones', sistema: 'WardrobeFlow — N01 Clientes y suscripciones',
    actores: [{ id: 'V', nombre: 'Vendedor' }],
    casos: [
      { id: 'c1', nombre: 'CU01-VEN Gestionar Cliente' }, { id: 'c2', nombre: 'CU02-VEN Renovar Suscripción' },
      { id: 'c3', nombre: 'CU03-VEN Cobrar Suscripción' }, { id: 'c4', nombre: 'CU04-VEN Gestionar Planes' }
    ],
    enlaces: [{ actor: 'V', caso: 'c1' }, { actor: 'V', caso: 'c2' }, { actor: 'V', caso: 'c3' }, { actor: 'V', caso: 'c4' }]
  },
  {
    tipo: 'casos', id: 'CU_pn01_armar_pedido', titulo: 'Casos de uso — PN01 Armar pedido', sistema: 'WardrobeFlow — PN01 Armar pedido',
    actores: [{ id: 'V', nombre: 'Vendedor' }, { id: 'D', nombre: 'Depósito / Logística' }],
    casos: [
      { id: 'c1', nombre: 'CU01-VEN Armar Pedido' }, { id: 'c2', nombre: 'CU02-VEN Consultar Catálogo' }, { id: 'c3', nombre: 'CU03-VEN Consultar Situación del Cliente' },
      { id: 'c4', nombre: 'CU04-VEN Cancelar Pedido' }, { id: 'c5', nombre: 'CU01-DEP Despachar Pedido' }, { id: 'c6', nombre: 'CU02-DEP Registrar Entrega' },
      { id: 'c7', nombre: 'CU03-DEP Registrar Devolución' }
    ],
    enlaces: [{ actor: 'V', caso: 'c1' }, { actor: 'V', caso: 'c4' }, { actor: 'D', caso: 'c5' }, { actor: 'D', caso: 'c6' }, { actor: 'D', caso: 'c7' }],
    incluye: [{ de: 'c1', a: 'c2' }, { de: 'c1', a: 'c3' }]
  },
  {
    tipo: 'casos', id: 'CU_pn02_comercializacion', titulo: 'Casos de uso — PN02 Comercialización de la suscripción', sistema: 'WardrobeFlow — PN02 Comercialización de la suscripción',
    actores: [{ id: 'V', nombre: 'Vendedor' }, { id: 'C', nombre: 'Caja' }],
    casos: [
      { id: 'c1', nombre: 'CU01-VTA Gestionar Suscripción' }, { id: 'c2', nombre: 'CU01-CAJ Gestionar Cobro' },
      { id: 'c3', nombre: 'CU02-CAJ Emitir Comprobante' }, { id: 'c4', nombre: 'CU03-CAJ Cancelar Contratación' }
    ],
    enlaces: [{ actor: 'V', caso: 'c1' }, { actor: 'C', caso: 'c2' }, { actor: 'C', caso: 'c4' }],
    incluye: [{ de: 'c2', a: 'c3' }]
  },
  {
    tipo: 'casos', id: 'CU_pn03_promociones', titulo: 'Casos de uso — PN03 Métricas, promociones y toma de decisiones', sistema: 'WardrobeFlow — PN03 Promociones',
    actores: [{ id: 'G', nombre: 'Gerencia' }, { id: 'A', nombre: 'Administración' }, { id: 'K', nombre: 'Contabilidad' }, { id: 'V', nombre: 'Vendedor' }],
    casos: [
      { id: 'c1', nombre: 'CU01-GER Sugerir Promoción' }, { id: 'c2', nombre: 'CU01-ADM Gestionar Promociones' }, { id: 'c3', nombre: 'CU01-CONT Analizar Promoción' },
      { id: 'c4', nombre: 'CU01-VEN Sugerir Baja de Promoción' }, { id: 'c5', nombre: 'CU02-ADM Resolver Baja de Promoción' },
      { id: 'c6', nombre: 'CU02-GER Consultar Analítica de Negocio' }
    ],
    enlaces: [{ actor: 'G', caso: 'c1' }, { actor: 'A', caso: 'c2' }, { actor: 'K', caso: 'c3' }, { actor: 'V', caso: 'c4' }, { actor: 'A', caso: 'c5' }, { actor: 'G', caso: 'c6' }]
  },
  {
    tipo: 'casos', id: 'CU_pn04_devolucion', titulo: 'Casos de uso — PN04 Inspección de devolución', sistema: 'WardrobeFlow — PN04 Inspección de devolución',
    actores: [{ id: 'D', nombre: 'Depósito' }],
    casos: [{ id: 'c1', nombre: 'CU-DEP-01 Inspeccionar Devolución' }, { id: 'c2', nombre: 'CU-DEP-02 Reportar Prenda Perdida' }],
    enlaces: [{ actor: 'D', caso: 'c1' }, { actor: 'D', caso: 'c2' }]
  }
];
