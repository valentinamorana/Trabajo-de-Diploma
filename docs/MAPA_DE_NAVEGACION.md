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
| | Cobro de suscripción | `CobroSuscripcionForm` | mnuCobroSuscripcion | VEN, GCO, ADM | N01 |
| | Nueva contratación | `NuevaContratacionForm` (modal) | mnuClientes | VEN, GCO, ADM | PN02 |
| **Inventario** | Prendas | `Prendas` | mnuPrendas | VEN, GCO, DEP, GIN, ADM | PN01 / PN04 |
| | Inspección de Devolución | `InspeccionDevolucionForm` | mnuInspeccionDevolucion | DEP, GIN, ADM | PN04 |
| | Lista de Espera | `ListaEsperaForm` | mnuListaEspera | VEN, GCO, DEP, GIN, ADM | EXT |
| **Ventas** | Pedidos de Venta | `PedidosVenta` | mnuPedidosVenta | VEN, GCO, ADM | PN01 |
| | Pedidos Realizados | `PedidosRealizados` | mnuPedidosRealizados | DEP, GCO, GIN, LOG, ADM | PN01 / PN04 |
| **Caja** | Contrataciones Pendientes | `ContratacionesPendientesForm` | mnuCaja | CAJ, ADM | PN02 |
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
| `PedidosVenta` | Lista pedidos y permite crearlos o cancelarlos. | Nuevo Pedido · Cancelar · Des-cancelar · Historial · Actualizar |
| `NuevoPedidoForm` (modal) | Asistente en pasos: cliente y prendas. Valida cuenta bloqueada, suscripción vigente y pedido abierto. | Siguiente → · ← Volver · Confirmar Pedido |
| `PedidoHistorialForm` | Historial de cambios de un pedido (solo lectura). | — |
| `PedidosRealizados` | Vista de Depósito/Logística: despacho, entrega, devolución y pérdida, con nivel de urgencia. | Despachar · Marcar Entregado · Registrar Devolución · Reportar Pérdida · Ver Notificación · Historial · Actualizar |
| `Prendas` | Catálogo con filtro por estado (State). Alta y edición requieren mnuStock. | Nueva Prenda · Editar · Estado · Mantenimiento · Lista de Espera · Actualizar |
| `PrendaForm` (modal) | Alta y edición de una prenda. | Guardar Cambios · Cancelar |
| `CambioEstadoDialog` | Elige el nuevo estado válido de la prenda. | — |
| `MantenimientoHistorialForm` | Historial de mantenimiento/limpieza (solo lectura). | — |
| `ListaEsperaForm` | Ver y cancelar anotaciones de espera por prenda. | Cancelar · Actualizar |

### PN02 — Comercialización de la suscripción
| Formulario | Qué hace | Acciones |
|---|---|---|
| `NuevaContratacionForm` | El vendedor elige cliente, plan y modalidad; queda pendiente de cobro. | Confirmar · Cancelar |
| `ContratacionesPendientesForm` | Caja cobra, emite comprobante o registra intento fallido (máximo 3). Aplica el descuento de PN03. | Cobrar · Intento Fallido · Actualizar |

### PN03 — Métricas, promociones y decisiones
| Formulario | Qué hace | Acciones |
|---|---|---|
| `SugerirPromocionForm` | Gerencia sugiere una promoción a Administración, con candidatas del análisis de datos. | Enviar Sugerencia · "Desde el análisis…" |
| `PromocionesAdministracionForm` | Administración da de alta (manual o desde sugerencia), reformula, desactiva y resuelve bajas. | Alta Manual · Alta desde Sugerencia · Reformular · Desactivar · Aprobar Baja · Rechazar Baja · Actualizar |
| `AltaPromocionForm` (modal) | Alta o reformulación de una promoción. | — |
| `PromocionesContabilidadForm` | Contabilidad analiza y activa o rechaza. | Aprobar y Activar · Rechazar · Actualizar |
| `PromocionesVigentesForm` | Vendedor consulta las vigentes y sugiere una baja. | Sugerir Baja · Actualizar |
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
| `Usuarios` | Cuentas: alta, desbloqueo, reset de clave, archivar y purgar. | Agregar · Desbloquear Cuenta · Resetear Contraseña · Archivar · Ver archivados · Purgar (>1 año) · Refrescar |
| `AdministracionUsuariosForm` | Datos administrativos no sensibles y cambio de rol. | Nuevo usuario · Guardar cambios · Cambiar rol · Buscar · Ver historial de cambios |
| `GestorPermisos` | Roles y familias con el Composite: crear, renombrar, asignar y quitar. | Crear rol raíz · Crear sub-rol · Renombrar · Eliminar · Asignar · Quitar · Ver vista completa |
| `ExploradorCompositeForm` | Vista de solo lectura del árbol Composite. | — |
| `FormIdiomas` | ABM de idiomas y traducciones. | Nuevo idioma · Renombrar · Activar · Desactivar · Guardar cambios |
| `BackupForm` | Copias de seguridad y restauración. | Generar Copia · Restaurar seleccionado · Desde archivo… · Eliminar · Backup de instalación limpia |
| `DiagnosticoIntegridadForm` | Verifica DVH/DVV de las tablas. | Recalcular Todo · Recuperación (Espejo)… · Actualizar |
| `RecuperacionEspejoForm` | Repara integridad desde el espejo `Usuario_Seguridad`. | Reparar desde Espejo · Asumir Pérdida · Restaurar Backup… · Cerrar |
| `ConfirmarAdminForm` · `InputDialog` | Diálogos de confirmación y entrada de texto. | — |
| `VersionHistorialForm` | Historial de versiones del sistema. | — |

### AUD — Auditoría y transversales
| Formulario | Qué hace | Acciones |
|---|---|---|
| `Bitacora` | Bitácora de Sistema o de Negocio, con filtros. | Buscar · Limpiar · Ver · Exportar PDF |
| `ReporteJornadaForm` | Reporte de la jornada, tendencia por rango y comparación entre jornadas. | Generar · Tendencia (rango) · Comparar Jornadas · Exportar reporte… · Exportar comparación… · Exportar TXT · Limpiar |
| `AlertasForm` | Centro de alertas (integridad, backups, stock, pedidos). | Actualizar |
| `DashboardForm` | Panel genérico (ADM, AUD, GCO, GIN, CAJ, ACO, CON). | — |
| `DashboardVendedor` | Pedidos pendientes, clientes, planes, suscripciones por vencer, Kanban de pedidos. | Actualizar |
| `DashboardDeposito` | Tareas de Depósito. | — |
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
| DEP | `DashboardDeposito` | Inventario (Prendas, Inspección, Lista de Espera), Ventas (Pedidos Realizados) |
| GIN | genérico | lo de DEP + Rotación, Mantenimiento, Escasez |
| CAJ | genérico | Caja |
| ACO | genérico | Promociones (Gestión) |
| CON | genérico | Promociones (Revisión contable) |

---

## 5. Puntos para revisar (cobertura)

Marcado con lo que sí verifiqué en el código y lo que no.

1. **PN01, paso de Depósito:** no tiene pantalla propia. Lo cubre `PedidosRealizados` (despachar, entregar, devolver, perdida). Confirmar que la documentación lo describa así.
2. **Stock sin menú propio:** mnuStock no es una opción; solo habilita los botones de alta, edición y estado dentro de `Prendas`. Está bien, pero conviene aclararlo en la documentación.
3. **Pausa y referidos (N01):** resuelto. La pausa se pide y se reanuda desde `RenovacionSuscripcionForm` ("Pausar hasta:" con tope de 3 meses y "Reanudar ahora"); el referente se elige en `ClienteForm` ("Referido por") y el crédito se ve al cobrar (`CobroSuscripcionForm`, `ContratacionesPendientesForm`).
4. **Renovación y Cobro:** cada uno tiene un solo botón "Procesar" más los campos de decisión. No revisé que la cadena (Chain of Responsibility) muestre en pantalla cada paso.
5. **Modalidad trimestral/anual:** resuelto según NUULY (cobra por mes): el precio del plan es mensual y cada cobro cubre 1, 3 o 12 meses (`Precio` × meses, sin descuento por modalidad). Ver decisión D1 en `NEGOCIO_Y_PROCESOS.md`.
6. **Roles con una sola pantalla:** CAJ, ACO, CON y LOG. Es coherente con los procesos, pero su panel de inicio es el genérico (salvo LOG).
7. **Alertas:** visible para todos los usuarios logueados, sin patente propia. Si algún rol no debería verlas, hay que agregarla.
8. **GCO no ve análisis de inventario, y GIN no ve análisis comerciales:** intencional, cada análisis tiene su patente.
9. **Todo Administrar depende de una sola patente (mnuUsuarios):** quien la tenga ve Backup, Integridad e Idiomas. Si se quiere separar, hacen falta patentes nuevas.
10. **Diagramas pendientes (para el final):** diagramas de PN02–PN04, de clases, DER y la documentación de N01.
