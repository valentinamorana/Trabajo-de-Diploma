# WardrobeFlow — Mapa de navegación y funcionalidades

Documento para verificar que no falte nada. Cada fila sale del código: el menú de `GUI/Menu.Designer.cs`, las reglas de visibilidad de `BLL/MenuVisibilidad.cs`, los permisos sembrados en `BD/00_Instalacion_Completa.sql` y los botones de cada `*.Designer.cs`.

**Siglas de procesos:** N01 Clientes/Suscripciones · PN01 Armar pedido · PN02 Comercialización de la suscripción · PN03 Métricas, promociones y decisiones · PN04 Inspección de devolución · SEG Seguridad y administración · AUD Auditoría · EXT mejora extra (no exigida por la cátedra).

**Roles (10):** ADM Administrador · AUD Auditor · VEN Vendedor · GCO GerenteComercial (incluye Vendedor) · LOG OperadorLogistico · DEP Deposito · GIN GerenteInventario (incluye OperadorLogistico + Deposito) · CAJ Caja · ACO AdministracionComercial · CON Contabilidad.

El Administrador ve todo (bypass en las 3 capas). Un grupo del menú se muestra solo si el usuario ve al menos una de sus opciones.

---

## 1. Barra de menú principal (`Menu`, contenedor MDI)

| Menú | Opción | Formulario que abre | Patente | Roles que la ven | Proceso |
|---|---|---|---|---|---|
| **Sesión** | Mi Perfil | `MiPerfilForm` (modal) | ninguna | todos | SEG |
| | Cerrar Sesión | vuelve a `Login` | ninguna | todos | SEG |
| **Ventana** | Cerrar todas las ventanas | — | ninguna | todos | SEG |
| **Panel de Control** | (ítem directo) | Dashboard según rol | ninguna | todos | transversal |
| **Alertas** | (ítem directo, con contador) | `AlertasForm` | ninguna | todos | transversal |
| **Suscriptores** | Clientes | `Clientes` | mnuClientes | VEN, GCO, ADM | N01 |
| | Planes | `Planes` | mnuPlanSuscripciones | VEN, GCO, ADM | N01 |
| | Renovación de suscripción | `RenovacionSuscripcionForm` | mnuRenovacionSuscripcion | VEN, GCO, ADM | N01 |
| | Nueva contratación | `NuevaContratacionForm` (modal) | mnuClientes | VEN, GCO, ADM | PN02 |
| **Inventario** | Prendas | `Prendas` | mnuPrendas | VEN, GCO, DEP, GIN, ADM | PN01 / PN04 |
| | Inspección de Devolución | `InspeccionDevolucionForm` | mnuInspeccionDevolucion | DEP, GIN, ADM | PN04 |
| | Lista de Espera | `ListaEsperaForm` | mnuListaEspera | VEN, GCO, DEP, GIN, ADM | EXT |
| | Control de Stock | `ControlStockForm` | mnuControlStock (acciones: mnuControlStockEditar) | DEP, GIN, ADM | PN01 |
| **Ventas** | Pedidos de Venta | `PedidosVenta` | mnuPedidosVenta | VEN, GCO, ADM | PN01 |
| | Pedidos Realizados | `PedidosRealizados` | mnuPedidosRealizados | DEP, GCO, GIN, LOG, ADM | PN01 / PN04 |
| **Caja** | Contrataciones Pendientes | `ContratacionesPendientesForm` | mnuCaja | CAJ, ADM | PN02 |
| | Cobro de suscripción | `CobroSuscripcionForm` | mnuCobroSuscripcion (acciones: mnuCajaEditar) | CAJ, ADM | N01 |
| **Promociones** | Sugerir promoción | `SugerirPromocionForm` (modal) | mnuSugerenciaPromocion | GCO, ADM | PN03 |
| | Gestión de promociones | `PromocionesAdministracionForm` | mnuPromocionesAdmin | ACO, ADM | PN03 |
| | Revisión contable | `PromocionesContabilidadForm` | mnuPromocionesContable | CON, ADM | PN03 |
| | Promociones vigentes | `PromocionesVigentesForm` | mnuPromocionesVigentes | VEN, GCO, ADM | PN03 |
| **Auditoría** | Bitácora del Sistema | `Bitacora("sistema")` | mnuAuditoria | AUD, ADM | AUD |
| | Bitácora de Negocio | `Bitacora("negocio")` | mnuAuditoria | AUD, ADM | AUD |
| | Reporte de Jornada | `ReporteJornadaForm` | mnuAuditoria | AUD, ADM | AUD |
| **Analítica de Negocio** | Análisis de Abandono | `AnalisisAbandonoForm` | mnuAnalisisAbandono | GCO, ADM | PN03 (PdN10) |
| | Ventas por Vendedor | `ReporteVentasVendedorForm` | mnuVentasVendedor | GCO, ADM | PN03 (PdN8) |
| | Rotación de Prendas | `AnalisisRotacionForm` | mnuAnalisisRotacion | GIN, ADM | PN03 (PdN9) |
| | Tiempos de Mantenimiento | `AnalisisMantenimientoForm` | mnuAnalisisMantenimiento | GIN, ADM | PN03 (PdN11) |
| | Escasez de Stock | `AnalisisEscasezForm` | mnuAnalisisEscasez | GIN, ADM | PN03 (PdN12) |
| | Recomendación de Prendas | `RecomendacionPrendasForm` | mnuRecomendacionPrendas | VEN, GCO, ADM | PN03 (PdN13) |
| **Administrar** (todo con mnuUsuarios: solo ADM) | Usuarios ▸ Administración de usuarios | `AdministracionUsuariosForm` | mnuUsuarios | ADM | SEG |
| | Usuarios ▸ Cuentas de usuario | `Usuarios` | mnuUsuarios | ADM | SEG |
| | Perfiles | `GestorPermisos` | mnuUsuarios | ADM | SEG |
| | Sistema ▸ Idiomas | `FormIdiomas` | mnuUsuarios | ADM | SEG |
| | Sistema ▸ Historial de versiones | `VersionHistorialForm` | mnuUsuarios | ADM | SEG |
| | Sistema ▸ Backup | `BackupForm` (modal) | mnuUsuarios | ADM | SEG |
| | Sistema ▸ Integridad | `DiagnosticoIntegridadForm` | mnuUsuarios | ADM | SEG |

Selector de idioma (ES/EN/RU/PT) en la barra superior: visible para todos.

---

## 2. Qué hace cada formulario

### N01 — Clientes y suscripciones
| Formulario | Qué hace | Acciones (botones) |
|---|---|---|
| `Clientes` | Listado y ABM de clientes con filtro. | Nuevo Cliente · Editar · Dar de Baja · Actualizar |
| `ClienteForm` (modal) | Alta y modificación de un cliente. | Registrar Cliente · Cancelar |
| `Planes` | ABM de planes de suscripción. | Guardar Plan · Limpiar/Nuevo · Activar Plan · Desactivar Plan |
| `RenovacionSuscripcionForm` | Carga la decisión de renovación del cliente (Chain of Responsibility). | Procesar · Reanudar ahora |
| `CobroSuscripcionForm` | Carga el resultado del cobro fuera del sistema (Chain of Responsibility); elige modalidad. | Procesar |

### PN01 — Armar pedido y logística
| Formulario | Qué hace | Acciones |
|---|---|---|
| `PedidosVenta` | Lista los pedidos del Vendedor y las acciones que corresponden a cada estado del armado (PN01). | + Nuevo Pedido · Ver faltantes · Ajustar selección · Registrar desistimiento · Formalizar pedido · Confirmación del pedido · Cancelar · Reactivar (vuelve a control de stock) · Historial · Actualizar |
| `NuevoPedidoForm` (modal) | Asistente en 2 pasos. Paso 1: identifica al cliente por DNI, nombre o apellido, muestra la ficha y verifica la vigencia y el pedido activo. Paso 2: catálogo, detalle de la selección y cupo del plan. Tiene un modo ajuste para los pedidos con faltantes, con las alternativas resaltadas. Imprime la planilla o el aviso de desistimiento. | Buscar · Siguiente → · ← Volver · Enviar a control de stock (o Reenviar) · Registrar desistimiento |
| `ControlStockForm` | Pantalla de Depósito. Muestra la cola de pedidos enviados a control y la planilla con el estado real de cada prenda (disponible, reservada para otro o faltante). | Informar faltantes · Confirmar prendas disponibles · Separar prendas · Imprimir planilla · Actualizar |
| `PedidoHistorialForm` | Historial de cambios de un pedido (solo lectura). | — |
| `PedidosRealizados` | Vista de Depósito/Logística de los pedidos **formalizados**: despacho, entrega, devolución y pérdida, con nivel de urgencia. | Despachar · Marcar Entregado · Registrar Devolución · Reportar Pérdida · Ver Notificación · Historial · Actualizar |
| `Prendas` | Catálogo con filtro por estado (State). Alta y edición requieren mnuStock. | Nueva Prenda · Editar · Estado · Mantenimiento · Lista de Espera · Actualizar |
| `PrendaForm` (modal) | Alta y edición de una prenda. | Guardar Cambios · Cancelar |
| `CambioEstadoDialog` | Elige el nuevo estado válido de la prenda. | — |
| `MantenimientoHistorialForm` | Historial de mantenimiento/limpieza (solo lectura). | — |
| `ListaEsperaForm` | Ver y cancelar anotaciones de espera por prenda. | Cancelar · Actualizar |

### PN02 — Comercialización de la suscripción
| Formulario | Qué hace | Acciones |
|---|---|---|
| `NuevaContratacionForm` | Carril Vendedor de PN02. Identifica al cliente por DNI, nombre o apellido (si no está registrado, lo registra) y presenta los planes con precio y límite. Valida la contratación e informa el motivo si no es válida. Muestra el importe a abonar y registra la contratación («Orden de cobro»), o asienta el desistimiento. | Buscar · Registrar cliente · Imprimir planes · Registrar contratación · El cliente desiste · Cerrar |
| `ContratacionesPendientesForm` | Carril Caja de PN02. Muestra la cola con la liquidación (un solo descuento) y cobra con un medio de pago del catálogo: emite el comprobante, activa la suscripción y acredita al referente. También registra intentos con motivo; al tercero cancela automáticamente. La vista Resueltas permite reimprimir. | Pendientes/Resueltas · Cobrar · Intento Fallido · Ver intentos · Imprimir liquidación · Imprimir comprobante · Imprimir constancia · Actualizar |

### PN03 — Métricas, promociones y decisiones
| Formulario | Qué hace | Acciones |
|---|---|---|
| `SugerirPromocionForm` | Carril Gerencia de PN03. Analiza las métricas (abandono por plan, rotación por categoría) y muestra el «Reporte de métricas»; si no hay oportunidad, termina sin promoción. Registra la sugerencia con el origen de la métrica y lista las registradas con su estado. | Analizar métricas… (Imprimir reporte · Usar esta idea) · Enviar Sugerencia · Imprimir sugerencia (o constancia de descarte) |
| `PromocionesAdministracionForm` | Carril Administración de PN03. Acepta (alta desde sugerencia) o descarta las sugerencias con motivo; da de alta manual; reformula o descarta las rechazadas por Contabilidad; desactiva con motivo; aprueba o rechaza las bajas pedidas por Ventas. Muestra el historial de estados e imprime los documentos de cada promoción. | Alta desde Sugerencia · Descartar sugerencia · Imprimir sugerencia · Alta Manual · Reformular · Descartar · Desactivar · Aprobar Baja · Rechazar Baja · Historial · Imprimir ▾ (ficha, dictamen, solicitud, resolución, constancia de descarte) · Actualizar |
| `AltaPromocionForm` (modal) | Alta (desde sugerencia o manual) o reformulación de una promoción; la validación es de la BLL. | Registrar · Cancelar |
| `PromocionesContabilidadForm` | Carril Contabilidad de PN03. Muestra el análisis de margen e impacto (beneficio estimado, margen proyectado contra el costo del descuento sobre los clientes activos del plan y promociones vigentes superpuestas) y dictamina con observación; quien creó la promoción no puede dictaminarla. | Aprobar y Activar · Rechazar · Imprimir ▾ · Actualizar |
| `PromocionesVigentesForm` | Carril Vendedor de PN03. Consulta las promociones vigentes y las que tienen la baja pedida; solicita la baja con motivo («Solicitud de baja»). | Solicitar baja · Imprimir ▾ (ficha, solicitud, resolución) · Actualizar |
| `AnalisisAbandonoForm` | Lista de clientes en riesgo según criterio elegido (Strategy). | Generar · Exportar a PDF · Guardar como .CSV |
| `ReporteVentasVendedorForm` | Desempeño por vendedor: totales, entregados, cancelados. | Generar · Exportar a PDF · Guardar como .CSV |
| `AnalisisRotacionForm` | Prendas de baja o alta demanda. | Generar · Exportar a PDF · Guardar como .CSV |
| `AnalisisMantenimientoForm` | Prendas cuyo mantenimiento supera el umbral. | Generar · Exportar a PDF · Guardar como .CSV |
| `AnalisisEscasezForm` | Talle+Categoría bajo un umbral de stock. | Generar · Exportar a PDF · Guardar como .CSV |
| `RecomendacionPrendasForm` | Prendas afines al historial de un cliente. | Generar · Exportar a PDF · Guardar como .CSV |

### PN04 — Inspección de devolución
| Formulario | Qué hace | Acciones |
|---|---|---|
| `InspeccionDevolucionForm` | Depósito decide por prenda devuelta: reingreso a stock, o baja y cobro de cargo al último cliente. | Aprobar Reingreso · Dar de Baja y Cobrar · Actualizar |
| `CargoPrendaDialog` (modal) | Registra el cargo por daño o pérdida. | — |

### SEG — Seguridad y administración
| Formulario | Qué hace | Acciones |
|---|---|---|
| `Login` | Ingreso; enlaza a recuperar contraseña y desbloqueo de emergencia. | Ingresar · Salir · mostrar clave |
| `OlvideContrasenaForm` | Recuperación de contraseña de empleados. | — |
| `DesbloqueoEmergenciaForm` | Desbloqueo del Administrador con clave de emergencia de un solo uso. | Desbloquear · Cancelar |
| `CambioClaveObligatorioForm` | Fuerza el cambio de clave temporal tras el login. | — |
| `MiPerfilForm` | Idioma, tipografía, tamaño y tema del usuario en sesión. | Guardar preferencias · Restaurar valores de fábrica · Recibir notificaciones |
| `Usuarios` | Cuentas: alta, desbloqueo, reset de clave, archivar y purgar. | Agregar Usuario · Desbloquear Cuenta · Resetear Contraseña · Archivar usuario · Ver archivados · Purgar (>1 año) · Refrescar Lista |
| `AdministracionUsuariosForm` | Datos administrativos no sensibles y cambio de rol. | Nuevo usuario · Guardar cambios · Cambiar rol · Buscar · Ver historial de cambios |
| `GestorPermisos` | Roles y familias con el Composite: crear, renombrar, asignar y quitar. | Crear rol raíz · Crear sub-rol · Renombrar · Eliminar · Asignar · Quitar · Ver vista completa |
| `ExploradorCompositeForm` | Vista de solo lectura del árbol Composite. | — |
| `FormIdiomas` | ABM de idiomas y traducciones. | Nuevo idioma · Renombrar · Activar · Desactivar · Guardar cambios |
| `BackupForm` | Copias de seguridad y restauración. | Generar Copia de Seguridad · Restaurar seleccionado · Desde archivo… · Eliminar · Backup de instalación limpia |
| `DiagnosticoIntegridadForm` | Verifica DVH/DVV de las tablas. | Recalcular Todo · Recuperación (Espejo)… · Actualizar |
| `RecuperacionEspejoForm` | Repara integridad desde el espejo `Usuario_Seguridad`. | Reparar desde Espejo · Asumir Pérdida · Restaurar Backup… · Cerrar |
| `ConfirmarAdminForm` · `InputDialog` | Diálogos de confirmación y entrada de texto. | — |
| `VersionHistorialForm` | Historial de versiones del sistema. | — |

### AUD — Auditoría y transversales
| Formulario | Qué hace | Acciones |
|---|---|---|
| `Bitacora` | Bitácora de Sistema o de Negocio, con filtros. | Buscar · Limpiar · Ver · Exportar PDF · Errores (XML) |
| `ErroresXmlForm` | A02 Serialización: errores inesperados serializados en XML (se abre desde la Bitácora del Sistema). | Ver los de esta PC · Importar XML · Exportar XML · Cerrar |
| `ReporteJornadaForm` | Reporte de la jornada, tendencia por rango y comparación entre jornadas. | Generar · Tendencia (rango) · Comparar Jornadas · Exportar reporte… · Exportar comparación… · Volver al reporte · menú Guardar como .TXT / Imprimir |
| `AlertasForm` | Centro de alertas (integridad, backups, stock, pedidos). | Actualizar |
| `DashboardForm` | Panel genérico (ADM, AUD, GCO, GIN, CAJ, ACO, CON). | — |
| `DashboardVendedor` | Tareas de PN01 y PN02 (`BLL.PanelTareas`): pedidos para atender (con faltantes + separados), clientes, contrataciones esperando a Caja, suscripciones por vencer. Tablero: En control de stock · Con faltantes: comunicar · Separados: formalizar (cada pedido abre Pedidos de Venta). | Actualizar |
| `DashboardDeposito` | Pedidos a controlar (abre Control de Stock), prendas disponibles, en mantenimiento, ocupación y tablero de mantenimiento por antigüedad. | Actualizar |
| `DashboardLogistica` | Tareas de Logística. | — |

---

## 3. Matriz rol × pantalla (● tiene acceso)

| Pantalla | ADM | AUD | VEN | GCO | LOG | DEP | GIN | CAJ | ACO | CON |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Clientes / Nueva contratación | ● | | ● | ● | | | | | | |
| Planes | ● | | ● | ● | | | | | | |
| Renovación / Cobro de suscripción | ● | | ● | ● | | | | | | |
| Prendas (consulta) | ● | | ● | ● | | ● | ● | | | |
| Prendas (alta/edición/estado, mnuStock) | ● | | | | | ● | ● | | | |
| Inspección de Devolución | ● | | | | | ● | ● | | | |
| Lista de Espera | ● | | ● | ● | | ● | ● | | | |
| Pedidos de Venta | ● | | ● | ● | | | | | | |
| Pedidos Realizados | ● | | | ● | ● | ● | ● | | | |
| Contrataciones Pendientes (Caja) | ● | | | | | | | ● | | |
| Sugerir promoción | ● | | | ● | | | | | | |
| Gestión de promociones | ● | | | | | | | | ● | |
| Revisión contable | ● | | | | | | | | | ● |
| Promociones vigentes | ● | | ● | ● | | | | | | |
| Análisis de Abandono / Ventas por Vendedor | ● | | | ● | | | | | | |
| Rotación / Mantenimiento / Escasez | ● | | | | | | ● | | | |
| Recomendación de Prendas | ● | | ● | ● | | | | | | |
| Bitácoras / Reporte de Jornada | ● | ● | | | | | | | | |
| Administrar (usuarios, perfiles, idiomas, backup, integridad) | ● | | | | | | | | | |
| Mi Perfil, Alertas, Panel, Cerrar sesión | ● | ● | ● | ● | ● | ● | ● | ● | ● | ● |

---

## 4. Qué ve cada rol al ingresar

| Rol | Dashboard | Menús visibles |
|---|---|---|
| ADM | genérico | todos |
| AUD | genérico | Auditoría |
| VEN | `DashboardVendedor` | Suscriptores, Inventario (Prendas, Lista de Espera), Ventas (Pedidos de Venta), Promociones (vigentes), Analítica (Recomendación) |
| GCO | genérico | lo de VEN + Pedidos Realizados, Sugerir promoción, Análisis de Abandono, Ventas por Vendedor |
| LOG | `DashboardLogistica` | Ventas (Pedidos Realizados) |
| DEP | `DashboardDeposito` | Inventario (Prendas, Control de Stock, Inspección, Lista de Espera), Ventas (Pedidos Realizados) |
| GIN | genérico | lo de DEP + Rotación, Mantenimiento, Escasez |
| CAJ | genérico | Caja |
| ACO | genérico | Promociones (Gestión) |
| CON | genérico | Promociones (Revisión contable) |

---

## 5. Puntos para revisar (cobertura)

Marcado con lo que sí verifiqué en el código y lo que no.

1. **PN01, paso de Depósito:** resuelto. `ControlStockForm` (Inventario → Control de Stock) cubre el carril "Controlador de Stock" del diagrama de actividad: revisar stock, informar faltantes con alternativas, confirmar y separar.
2. **Stock sin menú propio:** mnuStock no es una opción; solo habilita los botones de alta, edición y estado dentro de `Prendas`. Está bien, pero conviene aclararlo en la documentación.
3. **Pausa y referidos (N01):** resuelto. La pausa se pide y se reanuda desde `RenovacionSuscripcionForm` ("Pausar hasta:" con tope de 3 meses y "Reanudar ahora"); el referente se elige en `ClienteForm` ("Referido por") y el crédito se ve al cobrar (`CobroSuscripcionForm`, `ContratacionesPendientesForm`).
4. **Renovación y Cobro:** cada uno tiene un solo botón "Procesar" más los campos de decisión. No revisé que la cadena (Chain of Responsibility) muestre en pantalla cada paso.
5. **Modalidad trimestral/anual:** resuelto según NUULY (cobra por mes): el precio del plan es mensual y cada cobro cubre 1, 3 o 12 meses (`Precio` × meses, sin descuento por modalidad). Ver decisión D1 en `NEGOCIO_Y_PROCESOS.md`.
6. **Roles con una sola pantalla:** CAJ, ACO, CON y LOG. Es coherente con los procesos, pero su panel de inicio es el genérico (salvo LOG).
7. **Alertas:** visible para todos los usuarios logueados, sin patente propia. Si algún rol no debería verlas, hay que agregarla.
8. **GCO no ve análisis de inventario, y GIN no ve análisis comerciales:** intencional, cada análisis tiene su patente.
9. **Todo Administrar depende de una sola patente (mnuUsuarios):** quien la tenga ve Backup, Integridad e Idiomas. Si se quiere separar, hacen falta patentes nuevas.
10. **Diagramas pendientes (para el final):** diagramas de PN02–PN04, de clases, DER y la documentación de N01.
