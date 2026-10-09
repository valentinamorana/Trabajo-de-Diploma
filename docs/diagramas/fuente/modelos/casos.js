// Diagramas de casos de uso. Los identificadores (CU01-VEN, CU01-CAJ, ...) son los del documento de la tesis.
module.exports = [
  {
    tipo: 'casos', id: 'CU_n01_clientes_suscripciones', titulo: 'Casos de uso — N01 Clientes y suscripciones', sistema: 'WardrobeFlow — N01 Clientes y suscripciones',
    actores: [{ id: 'V', nombre: 'Vendedor' }, { id: 'C', nombre: 'Caja' }],
    casos: [
      { id: 'c1', nombre: 'CU01-VEN Gestionar Cliente' }, { id: 'c2', nombre: 'CU02-VEN Renovar Suscripción' },
      { id: 'c4', nombre: 'CU03-VEN Gestionar Planes' }, { id: 'c3', nombre: 'CU01-CAJ Cobrar Suscripción' }
    ],
    // El cobro recurrente lo hace Caja: quien vende no cobra (mismo criterio que PN02).
    enlaces: [{ actor: 'V', caso: 'c1' }, { actor: 'V', caso: 'c2' }, { actor: 'V', caso: 'c4' }, { actor: 'C', caso: 'c3' }]
  },
  {
    tipo: 'casos', id: 'CU_pn01_armar_pedido', titulo: 'Casos de uso — PN01 Armar pedido', sistema: 'WardrobeFlow — PN01 Armar pedido',
    // CU04-DEP solo lo ejecuta el rol Deposito (patentes ControlStock, BD/00_Instalacion_Completa.sql:2972-2975;
    // BLL/Pedido.cs:358,461); despacho, entrega y devolución los hacen Depósito y Logística (PedidosRealizadosEditar).
    actores: [{ id: 'V', nombre: 'Vendedor' }, { id: 'D', nombre: 'Depósito / Logística' }, { id: 'DP', nombre: 'Depósito' }],
    casos: [
      { id: 'c1', nombre: 'CU01-VEN Armar Pedido y Enviar a Control de Stock' }, { id: 'c2', nombre: 'CU02-VEN Consultar Catálogo' }, { id: 'c3', nombre: 'CU03-VEN Consultar Situación del Cliente' },
      { id: 'c4', nombre: 'CU04-VEN Cancelar Pedido' }, { id: 'c5', nombre: 'CU01-DEP Despachar Pedido' }, { id: 'c6', nombre: 'CU02-DEP Registrar Entrega' },
      { id: 'c7', nombre: 'CU03-DEP Registrar Devolución' }, { id: 'c8', nombre: 'CU04-DEP Controlar Stock del Pedido' },
      { id: 'c9', nombre: 'CU05-VEN Comunicar Faltantes, Ajustar o Desistir' }, { id: 'c10', nombre: 'CU06-VEN Formalizar Pedido' }
    ],
    enlaces: [{ actor: 'V', caso: 'c1' }, { actor: 'V', caso: 'c4' }, { actor: 'V', caso: 'c9' }, { actor: 'V', caso: 'c10' },
              { actor: 'D', caso: 'c5' }, { actor: 'D', caso: 'c6' }, { actor: 'D', caso: 'c7' }, { actor: 'DP', caso: 'c8' }],
    incluye: [{ de: 'c1', a: 'c2' }, { de: 'c1', a: 'c3' }]
  },
  {
    tipo: 'casos', id: 'CU_pn02_comercializacion', titulo: 'Casos de uso — PN02 Comercialización de la suscripción', sistema: 'WardrobeFlow — PN02 Comercialización de la suscripción',
    actores: [{ id: 'V', nombre: 'Vendedor' }, { id: 'C', nombre: 'Caja' }],
    casos: [
      { id: 'c1', nombre: 'CU01-VTA Gestionar Suscripción' }, { id: 'c2', nombre: 'CU01-CAJ Gestionar Cobro' },
      { id: 'c3', nombre: 'CU02-CAJ Emitir Comprobante' }, { id: 'c4', nombre: 'CU03-CAJ Registrar Intento y Cancelar Contratación' },
      { id: 'c5', nombre: 'CU02-VTA Asentar Desistimiento' }, { id: 'c6', nombre: 'CU04-CAJ Financiar en Cuotas' },
      { id: 'c7', nombre: 'CU05-CAJ Anular Contratación' }
    ],
    // CU02-VTA solo se alcanza desde la contratación (botón Desistir de NuevaContratacionForm.cs:286-309,
    // habilitado con el cliente identificado): es «extend» de CU01-VTA, sin asociación directa con el actor.
    // CU04-CAJ es «extend» de CU01-CAJ: solo si el medio de pago es Tarjeta de crédito (MedioPago.PermiteCuotas).
    enlaces: [{ actor: 'V', caso: 'c1' }, { actor: 'C', caso: 'c2' }, { actor: 'C', caso: 'c4' }, { actor: 'C', caso: 'c7' }],
    incluye: [{ de: 'c2', a: 'c3' }],
    extiende: [{ de: 'c5', a: 'c1' }, { de: 'c6', a: 'c2' }]
  },
  {
    tipo: 'casos', id: 'CU_pn03_promociones', titulo: 'Casos de uso — PN03 Métricas, promociones y toma de decisiones', sistema: 'WardrobeFlow — PN03 Promociones',
    // Actor = rol que tiene la patente que exige el CU (PermisosAccion.Exigir en BLL/SugerenciaPromocion.cs,
    // BLL/AnalisisPromociones.cs y BLL/Promocion.cs; asignaciones en BD/00_Instalacion_Completa.sql:2265-2276,
    // 785-791 y 804): Gerencia = GerenteComercial (Sugerir promoción; abandono y ventas por vendedor; contiene al
    // rol Vendedor, por eso también solicita la baja), Gerencia de Inventario = GerenteInventario (rotación,
    // mantenimiento y escasez), Administración = AdministracionComercial, Contabilidad, y Vendedor (baja de
    // promoción y Recomendación de prendas, que está en el menú Analítica de Negocio). El Administrador puede
    // todo y, como en los demás diagramas, no se dibuja.
    // CU03-GER es «extend» de CU01-GER: SugerirPromocionForm deja enviar la sugerencia sin analizar las
    // métricas (OrigenMetrica.Manual). El orden de casos y enlaces fija la grilla de hacerCasos (motor-ea.js).
    actores: [{ id: 'G', nombre: 'Gerencia' }, { id: 'K', nombre: 'Contabilidad' }, { id: 'A', nombre: 'Administración' },
              { id: 'V', nombre: 'Vendedor' }, { id: 'GI', nombre: 'Gerencia de Inventario' }],
    casos: [
      { id: 'c1', nombre: 'CU01-GER Sugerir Promoción' }, { id: 'c3', nombre: 'CU01-CONT Analizar Promoción' },
      { id: 'c2', nombre: 'CU01-ADM Gestionar Promociones' }, { id: 'c10', nombre: 'CU05-ADM Desactivar Promoción' },
      { id: 'c5', nombre: 'CU02-ADM Resolver Baja de Promoción' }, { id: 'c4', nombre: 'CU07-VEN Solicitar Baja de Promoción' },
      { id: 'c6', nombre: 'CU02-GER Consultar Analítica de Negocio' },
      { id: 'c7', nombre: 'CU03-GER Analizar Métricas' }, { id: 'c8', nombre: 'CU03-ADM Descartar Sugerencia' },
      { id: 'c9', nombre: 'CU04-ADM Descartar Promoción Rechazada' }
    ],
    enlaces: [{ actor: 'G', caso: 'c1' }, { actor: 'K', caso: 'c3' }, { actor: 'A', caso: 'c2' }, { actor: 'A', caso: 'c10' },
              { actor: 'A', caso: 'c5' }, { actor: 'V', caso: 'c4' }, { actor: 'G', caso: 'c4' },
              { actor: 'GI', caso: 'c6' }, { actor: 'G', caso: 'c6' }, { actor: 'V', caso: 'c6' }],
    extiende: [{ de: 'c7', a: 'c1' }, { de: 'c8', a: 'c2' }, { de: 'c9', a: 'c2' }]
  },
  {
    tipo: 'casos', id: 'CU_pn04_devolucion', titulo: 'Casos de uso — PN04 Inspección de devolución', sistema: 'WardrobeFlow — PN04 Inspección de devolución',
    actores: [{ id: 'D', nombre: 'Depósito' }],
    casos: [{ id: 'c1', nombre: 'CU05-DEP Inspeccionar Devolución' }, { id: 'c2', nombre: 'CU06-DEP Reportar Prenda Perdida' }],
    enlaces: [{ actor: 'D', caso: 'c1' }, { actor: 'D', caso: 'c2' }]
  }
];
