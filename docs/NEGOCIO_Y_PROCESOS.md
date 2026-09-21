# WardrobeFlow — Negocio, procesos y arquitectura (documento de referencia)

> **Para qué sirve este archivo.** Es la fuente de verdad para trabajar en el proyecto: qué es el
> negocio, quién hace qué, qué reglas se cumplen y **dónde vive cada una en el código**. Se redactó
> leyendo el repo (código, `BD/00_Instalacion_Completa.sql`, `README.md`) y contrastándolo con el
> documento del trabajo y con la investigación de NUULY. Cada afirmación sobre código cita un archivo;
> lo que **no** está implementado se dice explícitamente. Si el código y este documento discrepan,
> gana el código: corregir el documento.
>
> Convención: `Archivo.cs › Método` = ubicación. **T:** = archivo de tests que lo cubre.
> Estado al escribirlo: 457 tests (455 pasan, 2 omitidos preexistentes).

---

## 1. Propósito y fuente de verdad

**WardrobeFlow** es un sistema de escritorio (C# / .NET Framework 4.7.2 / Windows Forms MDI / SQL Server,
ADO.NET puro) para **gestionar internamente** un servicio de **alquiler de indumentaria por suscripción**.

- **Es un sistema interno.** El cliente final (suscriptor) **nunca accede al sistema**: interactúa por
  teléfono/mail/WhatsApp y todas sus acciones las registra personal interno (Vendedor, Caja, Depósito, etc.).
  Fuera de alcance: pasarelas de pago externas, app móvil del cliente, interfaz web pública.
- **Contexto académico:** Trabajo de Diploma, UAI 2026 (Valentina Morana). Parte de la base de
  Ingeniería de Software ("Campo") y se extiende con los procesos N01 y PN01–PN04.

### Relación con NUULY (fuente: `Investigacion_NUULY_WardrobeFlow.docx`)

NUULY (URBN) es el negocio de referencia. El documento de investigación es **insumo**, no la
especificación de WardrobeFlow.

| Aspecto | NUULY | WardrobeFlow |
|---|---|---|
| Modelo | Suscripción mensual (USD 98, 6 prendas, +4 por USD 22), sin niveles | **Adaptado:** varios planes (`PlanSuscripcion`, con `LimitePrendas` y `Precio`) y 3 modalidades de cobro (mensual/trimestral/anual) que solo definen la **duración** del ciclo |
| Pedido | Se confirma contra stock y se **bloquea**; el centro de distribución prepara después | **Adoptado:** el pedido se confirma y reserva de forma atómica; no hay edición posterior |
| Desbloqueo | No se arma otro pedido hasta que la devolución esté escaneada (4.2/4.6) | **Adoptado** como *cuenta desbloqueada* (§4, PN01/PN04) |
| Descuentos | **Un solo descuento por ciclo**; los no usados se acumulan (5.1) | **Adoptado** (`BE.PoliticaDescuento`, §4 PN03) |
| Referidos | Descuento de bienvenida, crédito al referidor tras 7 días, tope de 12/año | **Simplificado:** crédito fijo de $1000 al referente **al activar** el referido; sin espera de 7 días ni tope anual |
| Cargos por daño | **No hay** cargos por daño/limpieza; solo se cobra la prenda no devuelta | **Decisión propia distinta:** se cobra el precio de reposición por daño irreparable y por pérdida (§4 PN04) |
| Compra de prenda / Thrift / bonus items / sustitución por quiebre | Existen | **No implementados** |
| Devolución tardía | Sin cargos por demora | Igual: **no hay cargos por demora** en el código |
| Pausa | Máx. 3 meses | Implementada (`FechaPausaHasta`); el código **no fija un tope** de meses (no verificado línea por línea en `PausarSuscripcionHandler`) |
| Personalización con IA | Sí | No; el equivalente son los reportes de analítica (§4b) |

**Decisiones de diseño derivadas** (vigentes): ver §7.

---

## 2. Actores y roles reales

### 2.1 Personas del negocio
- **Cliente (externo):** no accede al sistema; ver arriba.
- **Personal interno:** cada persona tiene un `Usuario` (login) y, si opera pedidos/cobros, un `Empleado`
  vinculado (`Empleado.IdUsuario`). **Sin ese vínculo no se puede crear un pedido ni cobrar**
  (`BLL/BLLHelper.cs › ResolverEmpleadoActivo`, error `err.bll.empleado_sin_vinculo`). La instalación
  siembra el vínculo para `vendedor`, `deposito`, `caja` y `admin` (sección 21 del script).

### 2.2 Roles (10) y permisos efectivos

Los permisos son **patentes** (`mnuXxx`, constantes en `BE/Patentes.cs`). Cada patente tiene una variante
`...Editar` que separa VER de EDITAR. Los permisos efectivos de abajo salen del árbol Composite
(`PermisoRelacion`) de una instalación nueva (consultados con una CTE recursiva sobre la BD de prueba).

| Rol | Patentes efectivas | Hereda de |
|---|---|---|
| **Administrador** | todas (incluye `Usuarios`, `Auditoria`, todo Ventas/Inventario/Analítica/PN) | — |
| **Vendedor** | Clientes(+Editar), PlanSuscripciones(+Editar), Prendas, PedidosVenta(+Editar), CobroSuscripcion, RenovacionSuscripcion, ListaEspera, RecomendacionPrendas, PromocionesVigentes(+Editar) | — |
| **GerenteComercial** | lo de Vendedor + PedidosRealizados(+Editar), AnalisisAbandono, VentasVendedor, SugerenciaPromocion | ⊃ Vendedor |
| **OperadorLogistico** | PedidosRealizados(+Editar) (despacho) | — |
| **Deposito** *(el "Depósito" de PN01/PN04)* | Prendas, Stock(+Editar), ListaEspera, PedidosRealizados, InspeccionDevolucion | — |
| **GerenteInventario** | lo de Deposito + OperadorLogistico + AnalisisRotacion, AnalisisMantenimiento, AnalisisEscasez | ⊃ Deposito + OperadorLogistico |
| **Caja** *(PN02)* | Caja(+Editar) | — |
| **AdministracionComercial** *(PN03)* | PromocionesAdmin(+Editar) | — |
| **Contabilidad** *(PN03)* | PromocionesContable(+Editar) | — |
| **Auditor** | Auditoria (bitácoras, solo lectura) | — |

Notas:
- **Composite** = clases `BE.Componente` / `BE.Familia` / `BE.Rol` / `BE.Patente`, persistido en `Permiso` +
  `PermisoRelacion`. Hay guard anti-ciclos y anti-autoescalación (`BLL/Familia.cs`).
- Los roles `Supervisor`, `ControladorDeStock` y `EncargadoDeStock` **ya no existen**; el script los migra
  (`Supervisor`→`GerenteComercial`, los otros → `Deposito`) y `OperadorDeInventario` → `Deposito`
  (sección "Renombre de rol" de `00_Instalacion_Completa.sql`).
- `OperadorLogistico`, `GerenteInventario` y `Auditor` **no protagonizan ninguno de PN01–PN04**; sostienen
  despacho, analítica de inventario y auditoría respectivamente.

### 2.3 Separación de funciones (intencional)
| Par | Regla | Dónde se garantiza |
|---|---|---|
| Vendedor ≠ Caja | quien vende no cobra: Vendedor no tiene `CajaEditar`; Caja no tiene `ClientesEditar` | `BLL/Contratacion.cs › CrearContratacion / ConfirmarPago` (`PermisosAccion.Exigir`); T: `PermisosAccionTests`, `ContratacionTests` |
| Gerencia ≠ Administración ≠ Contabilidad | quien sugiere no define condiciones y quien define no aprueba el impacto económico | `BLL/SugerenciaPromocion.cs`, `BLL/Promocion.cs` (patentes distintas por acción); T: `PromocionTests`, `SugerenciaPromocionTests` |
| Depósito sin aprobador | inspecciona y resuelve solo | `BLL/CargoPrenda.cs`, `BLL/Prenda.cs` (solo exigen `StockEditar`) |

### 2.4 Usuarios demo (instalación nueva)
Fuente: `Instalador/Credenciales_Iniciales.txt` (contraseñas verificadas contra los hashes).

| Usuario | Clave | Rol |
|---|---|---|
| admin | `administrador1!` | Administrador |
| vendedor | `vendedor1!` | Vendedor |
| deposito | `deposito1!` | Deposito |
| caja / admcomercial / contable | `usuario1!` | Caja / AdministracionComercial / Contabilidad |
| gcomercial | `gcomercial1!` | GerenteComercial |
| ginventario | `ginventario1!` | GerenteInventario |
| logistico | `logistico1!` | OperadorLogistico |
| auditor | `auditor1!` | Auditor |

---

## 3. Modelo de dominio

Todas las tablas están en `BD/00_Instalacion_Completa.sql` (31 `CREATE TABLE`). Entidades en `BE/`, acceso en `DAL/`.

### 3.1 Tablas de negocio principales
| Tabla | Entidad `BE` | Campos clave |
|---|---|---|
| `PlanSuscripcion` | `PlanSuscripcion` | `Nombre`, `LimitePrendas`, `Precio` (= **importe de cada cobro**), `Estado` (activo) |
| `Cliente` | `Cliente` | `DNI` cifrado (AES), `IdPlan`, `FechaVencimiento`, `FechaLimiteGracia`, `FechaPausaHasta`, `IdClienteReferente`, `DescuentoProximoCobro`, `BeneficioReferidoOtorgado`, `DVH` |
| `Empleado` | `Empleado` | `IdUsuario` (vínculo con `Usuario`), `Legajo` |
| `Prenda` | `Prenda` | `Estado` (0–3), `IdClienteActual` (quién la tiene), `IdUltimoCliente` (nunca se limpia), `PrecioReposicion`, `Talle/Color/Categoria` |
| `Pedido` / `PedidoPrenda` / `PedidoHistorial` | `Pedido`, `PedidoHistorial` | `Estado` (0–3), fechas de despacho/entrega, `MotivoCancelacion`; el historial permite restaurar operaciones |
| `MantenimientoPrenda` | `MantenimientoPrenda` | ciclo En Limpieza: `FechaEntrada`, `FechaSalida` (abierto si es NULL) |
| `CargoPrenda` | `CargoPrenda` | `IdPrenda`, `IdCliente`, `Motivo`, `Monto` (>0, CHECK), `Estado` (0 Pendiente / 1 Cobrado) |
| `ListaEspera` | `ListaEspera` | `Estado` (0–3), `FechaLimiteReserva`; índice único de anotación activa por (prenda, cliente) |
| `Contratacion` | `Contratacion` | `IdVendedor`, `IdCaja`, `Modalidad` (0 mensual/1 trimestral/2 anual), `Estado`, `IntentosPago` (0..3, CHECK), `MedioPago`, `NumeroComprobante`, `Importe`, `DescuentoAplicado`, `IdPromocion`; índice único: **una pendiente por cliente** |
| `SugerenciaPromocion` | `SugerenciaPromocion` | `IdPlan` **o** `CategoriaPrenda`, `Motivo`, `TipoDescuentoSugerido`, `BeneficioEstimado`, `Estado` |
| `Promocion` | `Promocion` | `IdPlan` **o** `CategoriaPrenda` (CHECK), `TipoDescuento`, `Valor` (CHECK >0; ≤100 si %), vigencia (CHECK fin ≥ inicio), `Estado`, `Observacion`, `MotivoBaja` |
| `HistorialRenovacion` / `HistorialCobro` | `Renovacion`, `Cobro` | auditoría de los procesos N01 |
| `BitacoraNegocio` / `Bitacora` | `BitacoraNegocio`, `Bitacora` | auditoría de negocio y de sistema |

### 3.2 Máquinas de estado
| Entidad | Estados (enum en `BE/`) | Transiciones válidas |
|---|---|---|
| **Prenda** (`EstadoPrenda`, patrón **State** en `BE/Estados/`) | Disponible 0 · EnUso 1 · EnLimpieza 2 · Baja 3 | Disponible→EnLimpieza\|Baja; EnLimpieza→Disponible\|Baja; EnUso→Baja (solo por *Reportar prenda perdida*); Baja = final. **→EnUso** solo al reservar por pedido (`DAL/Pedido.cs`); **EnUso→EnLimpieza** solo al registrar la devolución. Restricciones extra en `BLL/Prenda.cs › CambiarEstado`: EnUso→Baja exige `viaFlujoPerdida`; EnLimpieza→Baja exige `viaInspeccion` |
| **Pedido** (`EstadoPedido`) | Pendiente 0 · Despachado 1 · Entregado 2 · Cancelado 3 | Pendiente→Despachado (`Despachar`)→Entregado (`MarcarEntregado`); Pendiente→Cancelado (`Cancelar`, libera prendas); `DesCancelar`; `RegistrarDevolucion` solo sobre Entregado (prendas EnUso→EnLimpieza). Implementado con **Command** (`BLL/Comandos/`) |
| **Contratacion** (`EstadoContratacion`) | PendientePago 0 · Pagada 1 · Cancelada 2 | Pendiente→Pagada (cobro, claim atómico) · Pendiente→Cancelada (3.er intento fallido) · Pagada→Pendiente solo como **compensación** si falla la activación (`ReabrirPago`) |
| **Promocion** (`EstadoPromocion`) | EnRevisionContable 0 · Vigente 1 · RechazadaContabilidad 2 · BajaSolicitada 3 · Desactivada 4 | EnRevisión→Vigente\|Rechazada (Contabilidad); Rechazada→EnRevisión (**Reformular**, Administración); Vigente→BajaSolicitada (Vendedor) → Desactivada (aprueba baja) \| Vigente (rechaza baja); Vigente→Desactivada (Administración, directo). Cada cambio es un `UPDATE ... WHERE Estado=@esperado` (`DAL/Promocion.cs`) |
| **SugerenciaPromocion** (`EstadoSugerencia`) | Pendiente 0 · Evaluada 1 | Pendiente→Evaluada al crear una promoción desde ella (una sugerencia evaluada no se reutiliza) |
| **ListaEspera** (`EstadoListaEspera`) | Pendiente 0 · Reservada 1 · Convertida 2 · Cancelada 3 | Pendiente→Reservada (al pasar la prenda a Disponible, FIFO, ventana `HORAS_RESERVA = 48` en `BLL/ListaEspera.cs`) → Convertida (el cliente arma su pedido) \| Cancelada |
| **CargoPrenda** (`EstadoCargo`) | Pendiente 0 · Cobrado 1 | Pendiente→Cobrado dentro de la transacción del próximo cobro (`ProcesarPagoHandler`) |
| **Cobro** (`EstadoCobro`) | Pendiente · Cobrado · Gracia · Suspendido | ver N01 |
| **Renovación** (`EstadoRenovacion`) | Pendiente · Renovada · CambioPlan · Baja · Pausada | ver N01 |

### 3.3 Estado de la suscripción del cliente (derivado de fechas, `BE/Cliente.cs`)
| Situación | Predicado | Efecto |
|---|---|---|
| Vigente | `SuscripcionVigente()` (plan asignado y `FechaVencimiento` ≥ hoy) | puede operar |
| En gracia | `EstaEnGracia` (`FechaLimiteGracia` ≥ hoy) | se abre al fallar un cobro; dura **5 días** (`AplicarGraciaHandler.DiasDeGracia`) |
| Suspendido por pago | `EstaSuspendidoPorPago` (`FechaLimiteGracia` < hoy) | **bloquea pedidos nuevos** hasta un cobro exitoso |
| Pausada | `EstaPausada` (`FechaPausaHasta` ≥ hoy) | bloquea pedidos; **no toca** `FechaVencimiento` (pausar no extiende la suscripción) |
| Referido | `IdClienteReferente`, `BeneficioReferidoOtorgado`, `DescuentoProximoCobro` | crédito para el referente |

---

## 4. Procesos de negocio

Numeración: **N01** (Entrega 1: Clientes y Suscripciones), **PN01–PN04** (Entrega 2). En el README y el código
N01 aparece como "Bloque 1 / PdN1–PdN6"; PN01–PN04 como "procesos nuevos".

### N01 — Gestión de Clientes y Suscripciones (Bloque 1, PdN1–PdN6)

**Objetivo.** Dar de alta clientes con su plan, activar/renovar la suscripción, cobrarla y gestionar pausa,
referidos y cargos.
**Actores.** Vendedor (alta, renovación, cobro), Administrador (correcciones administrativas de plan/vencimiento).
**Alcance.** Abarca: alta de cliente, activación de suscripción, renovación, cobro con gracia y suspensión,
pausa, referidos. **No abarca:** pasarela de pago, factura fiscal, cobro automático.

| Sub-proceso | Patrón | Ubicación | T: |
|---|---|---|---|
| PdN1 Activación de suscripción | **Builder** — `SuscripcionBuilder` (abstracta) + `SuscripcionMensual/Trimestral/AnualBuilder` + `DirectorSuscripcion`, creados por `SuscripcionBuilderFactory` (`BE/Builders/`); vencimiento = activación + 1/3/12 meses | `BLL/Cliente.cs › ActivarSuscripcionInterna` | `SuscripcionBuilderTests`, `ClienteTests` |
| PdN5 Renovación | **Chain of Responsibility** — `VerificarVencimientoHandler` → `IntentarRenovarHandler` → `CambioPlanHandler` / `BajaSuscripcionHandler` / `PausarSuscripcionHandler` (`BLL/Manejadores/`) | `BLL/Renovacion.cs` | `RenovacionTests` (23) |
| PdN6 Cobro | **Chain of Responsibility** — `DetectarCobroHandler` → `ProcesarPagoHandler` → `AplicarGraciaHandler` → `SuspenderHandler` | `BLL/Cobro.cs` | `CobroTests` (19) |
| PdN2/PdN4 Estados de prenda | **State** | `BE/Estados/*`, `BE/Prenda.cs › ControlarEstado` | `PrendaEstadoTests` |
| PdN3 Pedidos (cancelar/devolver) | **Command** — `PedidoCommand` (abstracta), `CancelacionCommand`, `DevolucionCommand`, `InvocadorPedido` | `BLL/Comandos/` | `ComandoPedidoTests` |
| Pausa / reanudación | — | `BLL/Cliente.cs › ReanudarPausa`; `PausarSuscripcionHandler` | `ClienteTests`, `RenovacionTests` |
| Referidos | — | `BLL/Cliente.cs › ActivarSuscripcionInterna` | `EndurecimientoPn02Pn03Tests › ActivarSuscripcion_*` |

**Reglas de negocio N01**
1. Un cliente sin plan no puede pedir ni operar (`ObtenerClienteValidado`).
2. Al fallar un cobro sobre una suscripción vencida/por vencer se abre una **gracia de 5 días**; vencida la gracia
   sin cobro, el cliente queda **suspendido por pago** (pedidos bloqueados) hasta un cobro exitoso.
3. Un cobro exitoso renueva la vigencia según la modalidad, limpia la gracia y liquida cargos pendientes.
4. **Referidos:** si el cliente activado tiene `IdClienteReferente` y `BeneficioReferidoOtorgado = false`, el referente
   suma **$1000** (`MontoBeneficioReferido`) a `DescuentoProximoCobro`; ambos `UPDATE` van en **una transacción**; el
   beneficio se otorga **una sola vez** por referido.
5. Pausar/reanudar no cambia la fecha de vencimiento; una suscripción pausada bloquea pedidos.
6. Cambiar el plan o el vencimiento de un cliente **directamente** (sin contratación) es una corrección reservada al
   Administrador (`BLL/Cliente.cs`; test `plan_solo_admin`).
7. **Cobro (importe):** `Precio del plan` − **un** descuento (ver PN03) + cargos por daño/pérdida pendientes;
   todo se persiste en una transacción (`ProcesarPagoHandler`).

Casos de uso: Registrar cliente, Activar suscripción, Renovar suscripción, Cobrar/procesar pago, Pausar/Reanudar,
Baja de suscripción, Cambiar plan (pantallas `Clientes`, `ClienteForm`, `RenovacionSuscripcionForm`, `CobroSuscripcionForm`).

---

### PN01 — Armar pedido de prendas

**Objetivo.** Que un cliente con suscripción vigente reciba un pedido confirmado con sus prendas reservadas.
**Actores.** Vendedor (arma el pedido). Depósito (rol `Deposito`) verifica disponibilidad y participa en la
preparación física; en el código **no hay un paso separado de Depósito**: la verificación y la reserva las ejecuta la
BLL en la confirmación.
**Precondiciones.** Vendedor con sesión y vínculo `Empleado`; cliente registrado.
**Pantallas.** `GUI/NuevoPedidoForm.cs` (asistente), `PedidosVenta.cs`, `PedidosRealizados.cs`.

**Flujo (código)**
1. El asistente elige al cliente → `BLL.Pedido.ValidarPuedeArmarPedido` (avisa el motivo en pantalla si no puede).
2. Selecciona prendas del catálogo (`BLL.Prenda.ObtenerDisponibles`, excluye las reservadas por Lista de Espera para otro cliente).
3. `BLL.Pedido.CrearPedido`: valida **cupo** (`ValidarCupoDisponible`), **relee la disponibilidad por lote**
   (`BLL.Prenda.VerificarDisponibilidad`) y **reserva** (`ReservarPrendas` → `DAL.Pedido.Alta`).
4. Registra historial (`PedidoHistorial`), bitácora y cierra reservas de Lista de Espera del cliente.
5. Postcondición: pedido `Pendiente` con número único; prendas `EnUso` a nombre del cliente.

**Reglas de negocio PN01**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | No se arma pedido sin suscripción vigente ni con suscripción pausada ni suspendida por pago | `BLL/Pedido.cs › ObtenerClienteValidado` | `PedidoTests` (`SuscripcionVencida/Pausada/SuspendidoPorPago`) |
| 2 | Con un pedido **Despachado** sin entregar no se arma otro | `BLL/Pedido.cs › ValidarPuedeArmarPedido` (`err.bll.pedido.ya_despachado`) | `PedidoTests › CrearPedido_ConDespachoActivo_LanzaYaDespachado` |
| 3 | **Cuenta bloqueada:** con prendas `EnUso` (pendientes de devolución) no se arma otro pedido; se desbloquea cuando PN04 registra la devolución | `ValidarPuedeArmarPedido` (`err.bll.pedido.cuenta_bloqueada`); aviso en `NuevoPedidoForm › CmbCliente_SelectedIndexChanged` | `PedidoTests › CrearPedido_ConPrendasPendientesDeDevolucion_LanzaCuentaBloqueada`, `ValidarPuedeArmarPedido_*` |
| 4 | La cantidad no puede superar el `LimitePrendas` del plan | `ValidarCupoDisponible` | `PedidoTests › CrearPedido_SuperaLimiteDelPlan_LanzaLimitePlan` |
| 5 | La disponibilidad se relee **contra la BD** justo antes de confirmar (cierra la ventana TOCTOU) | `BLL/Prenda.cs › VerificarDisponibilidad` | `PedidoTests`, `PrendaTests` |
| 6 | La reserva es **atómica**: `UPDATE ... AND Estado = Disponible` dentro de la transacción; si otra operación tomó la prenda se revierte todo | `DAL/Pedido.cs › Alta` | (sin test contra BD real; los tests usan fakes) |
| 7 | Guardar una prenda como interés **no reserva stock** (solo la confirmación reserva) | diseño (no hay "closet" persistido) | — |
| 8 | El pedido queda bloqueado al crearse: no existe API de edición de pedido | `BLL/Pedido.cs` (solo cancelar/despachar/entregar/devolver) | — |
| 9 | Una prenda que sale de En Limpieza a Disponible se reserva 48 h para el primer anotado de la Lista de Espera | `BLL/ListaEspera.cs › NotificarSiCorresponde` | `ListaEsperaTests` (11) |

**Casos de uso** (nombres del documento): CU01-VEN Armar Pedido, CU02-VEN Consultar Catálogo, CU03-VEN Consultar
Situación del Cliente, CU01-DEP Verificar Disponibilidad, CU02-DEP Reservar Prendas.
**Estado de implementación:** CU01-VEN ✔; CU02-VEN ✔ (sin "valor de reposición" en la grilla); CU03-VEN **sin pantalla propia**
(la situación se ve dentro del asistente); CU01/CU02-DEP ✔ **como lógica de BLL** pero **sin actor Depósito ni cola/pantalla**;
no hay notificación a Depósito, planilla de existencias ni registro de desistimiento.
**Alcance.** Abarca: validar cliente, catálogo disponible, cupo, disponibilidad, reserva y creación del pedido.
**No abarca:** preparación/empaque físico, envío con tracking, email al cliente, sustitución por quiebre de stock.
**Ciclo posterior del pedido** (pantalla `PedidosRealizados`, rol `OperadorLogistico`/Vendedor según patente): Despachar → Marcar entregado → Registrar devolución (PN04). No hay estados "En curso / On Hold" ni email de despacho.

---

### PN02 — Comercialización de la suscripción

**Objetivo.** Que un cliente registrado elija plan y modalidad, se derive el cobro a Caja (separada de quien vendió) y, recién al cobrar, se formalice la suscripción.
**Actores.** Vendedor (crea la contratación), Caja (cobra), Cliente (externo).
**Pantallas.** `GUI/NuevaContratacionForm.cs`, `GUI/ContratacionesPendientesForm.cs`.
**Precondiciones.** Cliente existente (el alta de cliente **no** exige plan); al menos un plan activo.

**Flujo**
1. Vendedor: `BLL.Contratacion.CrearContratacion(cliente, plan, modalidad)` → contratación `PendientePago`. La suscripción **aún no está vigente**.
2. Caja ve la cola (`ObtenerPendientesDePago`) con el **importe a cobrar** (`CalcularImporte`: precio del plan menos un único descuento).
3. Caja: `ConfirmarPago(medioPago)`:
   a. revalida contra la BD que siga Pendiente y que el plan siga activo;
   b. calcula importe y descuento;
   c. **claim atómico** (`DAL.Contratacion.ConfirmarPago`: `UPDATE ... AND Estado = 0`, devuelve `false` si otra sesión ya la cobró);
   d. activa la suscripción (`BLL.Cliente.ActivarSuscripcionDesdeContratacion`: Builder + crédito de referido);
   e. si (d) falla, **compensa** con `ReabrirPago`;
   f. emite comprobante `CMP-{id:D6}-{yyyyMMdd}` y lo devuelve; bitácora.
4. Si el intento de cobro no se concreta: `RegistrarIntentoFallido` (UPDATE condicionado a Pendiente); al **3.er intento** la contratación pasa a `Cancelada`.

**Reglas de negocio PN02**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | La contratación exige cliente existente y plan **activo** | `BLL/Contratacion.cs › CrearContratacion` | `ContratacionTests` (`ClienteInexistente`, `PlanInexistente`, `PlanInactivo`) |
| 2 | Un cliente no puede tener **dos** contrataciones pendientes | `CrearContratacion` + índice único `UX_Contratacion_UnaPendientePorCliente` | `ContratacionTests › ClienteYaTienePendiente` |
| 3 | Solo se cobra una contratación **Pendiente de pago** (revalidada contra la BD) | `ConfirmarPago` (`err.bll.contratacion.cobrar_estado`) | `ContratacionTests` |
| 4 | El medio de pago es obligatorio (efectivo/tarjeta/transferencia) | `ConfirmarPago` (`medio_pago_requerido`) | `ContratacionTests` |
| 5 | Si el plan fue dado de baja antes del cobro se rechaza y la contratación sigue pendiente | `ConfirmarPago` (`err.bll.contratacion.plan_baja`) | `ContratacionTests › PlanDadoDeBaja...` |
| 6 | **Doble cobro imposible:** solo una sesión de Caja gana el claim | `DAL/Contratacion.cs › ConfirmarPago`; `BLL` (`cobrar_concurrente`) | `ContratacionTests › OtraSesionGanoElClaim...` (con fake; no contra BD real) |
| 7 | Si la activación falla, la contratación vuelve a Pendiente (nunca queda Pagada sin suscripción) | `ConfirmarPago` + `DAL.ReabrirPago` | `ContratacionTests › FallaLaActivacion_Reabre...`. Si además falla la compensación se deja constancia CRÍTICA en bitácora y Caja recibe `cobro_sin_activar` (`EndurecimientoPn02Pn03Tests`) |
| 8 | Tercer intento fallido cancela automáticamente | `RegistrarIntentoFallido` (`MaxIntentosPago = 3`); CHECK `IntentosPago 0..3` y el UPDATE de `DAL.IncrementarIntento` respeta el tope (`IntentosPago < 3`; devuelve -1 si ya no está pendiente) | `ContratacionTests` |
| 9 | Vendedor no cobra y Caja no vende (patentes) | ver §2.3 | `PermisosAccionTests` |
| 10 | Al activar se acredita el referido (una sola vez) | `BLL/Cliente.cs` | `EndurecimientoPn02Pn03Tests › ActivarSuscripcion_ClienteReferido...` |
| 11 | El importe cobrado, el descuento y la promoción aplicada **se guardan** en `Contratacion` | `DAL/Contratacion.cs` (columnas `Importe`, `DescuentoAplicado`, `IdPromocion`) | `ContratacionTests` (fake) |

**Casos de uso:** CU01-VTA Gestionar Suscripción, CU01-CAJ Gestionar Cobro, CU02-CAJ Emitir Comprobante, CU03-CAJ Cancelar Contratación.
**Alcance.** Abarca: contratar, cobrar, comprobante numerado, cancelación por intentos, descuento en el cobro.
**No abarca:** comprobante impreso/PDF, factura fiscal, conciliación con medios de pago reales. El comprobante es solo un número mostrado y guardado (sin entidad propia). El flujo alternativo del referido "en el momento" queda simplificado (§7).

---

### PN03 — Métricas, promociones y toma de decisiones

**Objetivo.** Convertir los datos del negocio en decisiones comerciales: detectar una oportunidad con un dato, formalizarla,
aprobar su impacto económico y **aplicarla realmente al cobro**.
**Actores.** GerenteComercial ("Gerencia"), AdministracionComercial ("Administración"), Contabilidad, Vendedor.
**Pantallas.** `SugerirPromocionForm`, `PromocionesAdministracionForm` + `AltaPromocionForm`, `PromocionesContabilidadForm`, `PromocionesVigentesForm`; reportes `Analisis*Form`.

**Circuito:** *reporte → sugerencia → Administración → Contabilidad → Vigente → se aplica al cobro.*
1. **Métricas → idea** (`BLL/AnalisisPromociones.cs › Detectar`, botón "Desde el análisis…" de `SugerirPromocionForm`):
   - Rotación: ≥2 prendas de una categoría sin pedidos → candidata **por categoría** (monto fijo; beneficio inicial = n × 1000, editable).
   - Abandono: clientes en riesgo agrupados por plan → candidata **por plan** (porcentaje; beneficio = n × precio del plan = ingreso mensual en riesgo).
2. **Gerencia** crea la sugerencia (`BLL/SugerenciaPromocion.cs › Crear`, estado Pendiente). Puede partir de una idea del análisis o escribirla a mano.
3. **Administración** crea la promoción desde la sugerencia o manual (`BLL/Promocion.cs › CrearDesdeSugerencia / CrearManual`) → `EnRevisionContable`; marca la sugerencia Evaluada.
4. **Contabilidad** aprueba (→ `Vigente`) o rechaza (→ `RechazadaContabilidad`), siempre con observación.
5. Si se rechazó, **Administración reformula** (`Reformular`, botón "Reformular" en `PromocionesAdministracionForm`) y vuelve a la cola.
6. **Vendedor** ve las vigentes y puede **sugerir la baja** con motivo (`SugerirBaja` → `BajaSolicitada`); **Administración** la aprueba (→ `Desactivada`) o rechaza con motivo (vuelve a `Vigente` conservando la observación de Contabilidad). Administración también puede desactivar directo.
7. **Aplicación al cobro:** en el cobro recurrente (`ProcesarPagoHandler`) y en el cobro de contratación (`BLL.Contratacion.ConfirmarPago`) se resuelve el descuento con `BE.PoliticaDescuento.Resolver`.

**Reglas de negocio PN03**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | Una promoción aplica a **un plan o una categoría, nunca a ambos ni a ninguno** | `BLL/Promocion.cs › ValidarCamposComunes`; CHECK `CHK_Promocion_Destino` | `PromocionTests`, `SugerenciaPromocionTests` |
| 2 | `Valor > 0`; si es Porcentaje, ≤ 100 | ídem; CHECK `CHK_Promocion_Valor/Porcentaje` | `PromocionTests` |
| 3 | Fecha fin ≥ fecha inicio | ídem; CHECK `CHK_Promocion_Fechas` | `PromocionTests` |
| 4 | Toda promoción **nace En Revisión Contable** y no aplica descuento hasta ser aprobada | `CrearInterna`; DEFAULT 0 | `PromocionTests` |
| 5 | Solo se aprueba/rechaza lo que está En Revisión y con observación obligatoria | `AprobarContable/RechazarContable` | `PromocionTests` |
| 6 | Solo se sugiere la baja de una **Vigente**, con motivo | `SugerirBaja` | `PromocionTests` |
| 7 | Solo se resuelve la baja de una **BajaSolicitada**; rechazarla exige motivo y **no pisa** la observación de Contabilidad (`COALESCE`) | `AprobarBaja/RechazarBaja`; `DAL/Promocion.cs › CambiarEstado` | `PromocionTests` |
| 8 | Una sugerencia ya **evaluada** no se reutiliza | `CrearDesdeSugerencia`: reclamo atómico (`DAL.MarcarEvaluada ... AND Estado = 0`); si la creación falla se compensa con `ReabrirEvaluacion` | `EndurecimientoPn02Pn03Tests › CrearDesdeSugerencia_*` |
| 9 | Solo una promoción **Rechazada** se puede reformular | `Reformular` (`reformular_estado`) | `EndurecimientoPn02Pn03Tests › Reformular_*` |
| 10 | Los cambios de estado son atómicos frente a otra sesión (`UPDATE ... WHERE Estado=@esperado`) | `BLL/Promocion.cs › CambiarEstadoOFalla` | `PromocionTests` (con fake) |
| 11 | **Un solo descuento por ciclo:** compiten la mejor promoción vigente **del plan del cliente** y el crédito por referido; se aplica el **mayor**; si gana la promoción el crédito **no se consume** (queda acumulado; si gana el crédito solo se descuenta lo aplicado y el excedente también queda acumulado); en empate gana la promoción | `BE/PoliticaDescuento.cs › Resolver` | `PoliticaDescuentoTests` (11), `CobroTests`, `ContratacionTests` |
| 12 | Tipos de descuento: `Porcentaje` (% del bruto), `MontoFijo` (tope = bruto), `PrecioPromocional` (`Valor` = precio final) | `PoliticaDescuento.DescuentoDe` | `PoliticaDescuentoTests` |
| 13 | Solo aplican promociones **Vigentes**, dentro de fechas y del **plan** del cliente; las de **categoría son informativas** (no tienen importe donde aplicarse en el cobro de suscripción) | `Promocion.EstaVigente`, `PoliticaDescuento.Resolver` | `PoliticaDescuentoTests` |

**Casos de uso:** CU01-GER Sugerir Promoción, CU01-ADM Gestionar Promociones, CU01-CONT Analizar Promoción, CU01-VEN Sugerir Baja, CU02-ADM Resolver Baja.
**Alcance.** Abarca el circuito completo y su aplicación al importe del cobro. **No abarca:** aplicar promociones por categoría a un precio (no hay compra de prenda);
descuentos acumulables; vigencia automática por calendario más allá de la fecha; el "margen estimado" es solo informativo para Contabilidad.
Nota de origen: la estructura del circuito (sugerir → crear → aprobar → baja) se adaptó de otro proyecto de la cursada (SIRVI); la regla del descuento único viene de NUULY.

---

### PN04 — Inspección de devolución

**Objetivo.** Resolver qué pasa con una prenda cuando el cliente la devuelve o se reporta perdida, sin aprobación de nadie.
**Actores.** Depósito (rol `Deposito`), Cliente (externo).
**Pantallas.** `GUI/InspeccionDevolucionForm.cs` (CU-DEP-01), `GUI/PedidosRealizados.cs` (registrar devolución y CU-DEP-02), `CargoPrendaDialog`, `Prendas.cs`.

**Flujo**
1. Un pedido `Entregado` se devuelve: `BLL.Pedido.RegistrarDevolucion` pasa sus prendas `EnUso` → `EnLimpieza` (y abre `MantenimientoPrenda`). **Ese es el "desbloqueo" de la cuenta** (PN01 regla 3).
2. Depósito ve la cola de prendas En Limpieza (`BLL.Prenda.ObtenerEnLimpieza`) y, por prenda:
   - **Desgaste normal → aprueba el reingreso:** `CambiarEstado(Disponible)`, sin cargo (cubierto por la cuota). Dispara la Lista de Espera.
   - **Daño irreparable → baja con cargo:** `CargoPrenda.RegistrarCargo` (precio de reposición; pre-cargado desde `Prenda.PrecioReposicion`, editable) y **recién después** `CambiarEstado(Baja, viaInspeccion: true)`.
3. **Prenda perdida** (CU-DEP-02, desde el detalle del pedido): cargo + `CambiarEstado(Baja, viaFlujoPerdida: true)` sin pasar por Limpieza.
4. El cargo queda `Pendiente` contra `IdUltimoCliente` y **se suma al próximo cobro** de ese cliente (`ProcesarPagoHandler`, misma transacción).

**Reglas de negocio PN04**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | Sin aprobador: resuelve Depósito directamente (solo `StockEditar`) | `BLL/CargoPrenda.cs`, `BLL/Prenda.cs` | `CargoPrendaTests`, `PrendaTests` |
| 2 | Solo se inspecciona una prenda **En Limpieza**; el `UPDATE` es condicionado al estado anterior | `DAL/Prenda.cs › CambiarEstado` | `PrendaEstadoTests`, `PrendaTests` |
| 3 | Solo se reporta como perdida una prenda **En Uso**; es la única baja permitida desde EnUso | `BLL/Prenda.cs › CambiarEstado` (`baja_requiere_flujoperdida`) | `PrendaTests` |
| 4 | **En Limpieza → Baja solo desde la Inspección** (exige el cargo previo): la BLL lo impone y la pantalla genérica de Prendas no ofrece esa opción | `BLL/Prenda.cs` (`baja_requiere_inspeccion`), `GUI/Prendas.cs` | `PrendaTests › CambiarEstado_EnLimpiezaABaja*` |
| 5 | El cargo se registra **antes** de la baja | `InspeccionDevolucionForm.cs`, `PedidosRealizados.cs` | ⚠ orden garantizado por la pantalla; sin transacción (riesgo aceptado, ver §7) |
| 6 | El cargo exige un **último cliente** registrado, motivo y monto > 0 | `BLL/CargoPrenda.cs › RegistrarCargo`; CHECK `CK_CargoPrenda_Monto` | `CargoPrendaTests` |
| 7 | Baja es estado final | `BE/Estados/EstadoBaja.cs` | `PrendaEstadoTests` |
| 8 | Al registrar la devolución la cuenta se **desbloquea** (las prendas dejan de estar EnUso) | `BLL/Pedido.cs › RegistrarDevolucion` | `PedidoTests` (`ValidarPuedeArmarPedido_..._SeDesbloquea`) |
| 9 | **No hay cargos por demora** en la devolución | (ausencia de lógica) | — |

**Casos de uso:** CU-DEP-01 Inspeccionar Devolución, CU-DEP-02 Reportar Prenda Perdida.
**Alcance.** Abarca: reingreso, baja con cargo, prenda perdida, desbloqueo. **No abarca:** reparación/limpieza detallada, cobro inmediato del cargo (se difiere al próximo cobro), reposición automática de stock, cargo por demora.

---

## 4b. Funcionalidades fuera de los PN (extras)

| Extra | Qué hace | Ubicación |
|---|---|---|
| **Lista de Espera** (mejora opcional, inspirada en otro proyecto) | Anotar clientes en una prenda En Uso; al liberarse se reserva 48 h FIFO | `BLL/ListaEspera.cs`, `GUI/ListaEsperaForm.cs`, sección 16 del script |
| **Analítica PdN8–PdN13** | 6 reportes de solo lectura con exportación PDF/CSV: ventas por vendedor, rotación, abandono (**Strategy**: `EstrategiaVencimientoInactividad`, `EstrategiaInactividadPura`, `EstrategiaClienteNuevoInactivo`), mantenimiento, escasez por talle/categoría, recomendación de prendas | `BLL/Analisis*.cs`, `BLL/Estrategias/`, `GUI/Analisis*Form.cs`, `GUI/Exportacion/` |
| **Backup / restauración** | Copias cifradas con contraseña (`.wfbak`) con verificación de integridad | `BLL/Backup.cs`, `Seguridad/CifradorArchivos.cs`, `GUI/BackupForm.cs` |
| **Integridad (DV)** | Dígitos verificadores DVH/DVV por tabla, chequeo al iniciar y reparación desde tabla espejo | `Seguridad/DigitoVerificador.cs`, `BLL/Configuracion.cs`, `BLL/RecuperacionIntegridad.cs` |
| **Multiidioma** | ES/EN/RU/PT en vivo | §5 |
| **Exportación** | **Factory Method** `GeneradorReporte`→`Exportador` (`ExportadorPdf/Csv/Txt`) | `GUI/Exportacion/` |
| **Panel de alertas, reporte de jornada, dashboards por rol** | resumen operativo | `BLL/PanelAlertas.cs`, `BLL/ReporteJornada.cs`, `GUI/Dashboard*.cs` |
| **Historial de usuario con rollback** | **Memento** | `BE/VersionUsuario.cs`, `BE/Memento/`, `BLL/CuidadorHistorial.cs` |

---

## 5. Reglas transversales

| Tema | Regla | Ubicación |
|---|---|---|
| **Contraseñas** | PBKDF2-SHA256, sal aleatoria de 16 B, **100 000 iteraciones**, verificación en tiempo constante | `Seguridad/Encriptador.cs` |
| **Datos sensibles** | DNI del cliente cifrado con **AES-128-CBC**; el DNI no se escribe en la bitácora | `Seguridad/Encriptador.cs`, `BLL/Cliente.cs` |
| **Login** | 3 intentos fallidos bloquean; bloqueo **progresivo** (1/5/15/60 min); claves de emergencia de autodesbloqueo del admin | `BLL/Usuario.Autenticacion.cs`, `BLL/RecuperacionAdmin.cs`, `Seguridad/ContadorSesion.cs` |
| **Integridad** | DVH (por fila) y DVV (por tabla) sobre Usuario, Cliente, Empleado, Pedido; al iniciar, filas con `DVH=0` se **recalculan** (no es falsa alarma); una discrepancia real bloquea el login hasta que el Administrador repara | `Seguridad/DigitoVerificador.cs`, `BLL/Configuracion.cs › VerificarIntegridadDV` |
| **Bitácora** | `Bitacora` (sistema, con criticidad) y `BitacoraNegocio` (eventos de negocio); toda escritura relevante deja rastro | `Servicios/Bitacora.cs`, `Servicios/BitacoraNegocio.cs` |
| **Permisos** | Toda operación de BLL exige patente (`PermisosAccion.Exigir(editar, ver)`); la GUI nunca accede a DAL ni Seguridad | `BLL/PermisosAccion.cs`, `BLL/MenuVisibilidad.cs` |
| **Multiidioma** | 4 idiomas (ES/EN/RU/PT) en vivo (**Observer**: `GestorIdioma` + `IIdiomaObserver`); corpus en `Servicios/Multiidioma/traducciones.tsv` (formato `IDIOMA⇥clave⇥texto`, más de 1.300 claves por idioma, **las mismas claves en los 4**). Los errores de negocio son `BE.AppException(clave, fallback, args)` y se resuelven con `Traductor.Resolver`. Los roles se traducen con `perfil.*` y `perm.rol.*` | `Servicios/Multiidioma/` |
| **Concurrencia** | Las transiciones críticas usan `UPDATE ... WHERE Estado=@esperado` (claim atómico) en vez de confiar en el objeto de memoria | `DAL/Contratacion.cs`, `DAL/Promocion.cs`, `DAL/Prenda.cs`, `DAL/Pedido.cs` |
| **Errores** | Excepciones de negocio (`AppException`) se muestran traducidas sin ruido; las inesperadas se registran y se muestra un mensaje genérico | `GUI/FormBase.cs › MostrarError` |

---

## 6. Arquitectura y estructura del repositorio

```
BE/          Entidades, enums, DTOs, política de descuento y los patrones de dominio (Builders, Estados, Memento)
BLL/         Lógica de negocio (una clase por servicio + Interfaces/, Manejadores/, Comandos/, Estrategias/)
DAL/         Acceso a datos ADO.NET puro (Acceso singleton, BaseDAL, un DAL por entidad + Interfaces/)
GUI/         Windows Forms MDI (FormBase, Menu, formularios, Exportacion/)
Seguridad/   Sesión, cifrado, dígitos verificadores
Servicios/   Bitácora, multiidioma, generador de credenciales
Tests/       MSTest con Fakes/ (dobles de DAL/servicios)
BD/00_Instalacion_Completa.sql   Único script de base de datos
Instalador/  Inno Setup + DbInstaller + credenciales + script de firma
docs/        Este documento
```
Regla de capas: `GUI → BLL → DAL/BE/Servicios/Seguridad`; la GUI no toca DAL/Seguridad. Inyección de dependencias por constructor
(constructor por defecto con DAL reales + overloads con dobles para tests).

### 6.1 Patrones de diseño (con clase concreta)
| Patrón | Dónde | Origen |
|---|---|---|
| **Builder** | `BE/Builders/` (`SuscripcionBuilder` + 3 concretos + `DirectorSuscripcion` + `SuscripcionBuilderFactory`) | Bloque 1 TD |
| **State** | `BE/Estados/` (`Estado`, `EstadoDisponible/EnUso/EnLimpieza/Baja`), usado por `BE.Prenda` | Bloque 1 TD |
| **Command** | `BLL/Comandos/` (`PedidoCommand`, `CancelacionCommand`, `DevolucionCommand`, `InvocadorPedido`) | Bloque 1 TD |
| **Chain of Responsibility** | `BLL/Manejadores/` (cadena de Renovación y cadena de Cobro) | Bloque 1 TD |
| **Strategy** | `BLL/Estrategias/` (`EstrategiaRiesgo` + 3 concretas) para el análisis de abandono | Bloque 3 |
| **Observer** | `Servicios/Multiidioma/GestorIdioma` + `IIdiomaObserver` (cambio de idioma en vivo) | Ing. de Software |
| **Composite** | `BE/Componente`, `Familia`, `Rol`, `Patente` (árbol de permisos) | Ing. de Software |
| **Memento** | `BE/VersionUsuario` (`IMemento`), `BLL/CuidadorHistorial` (rollback de datos de usuario) | Ing. de Software |
| **Singleton** | `Seguridad/SessionManager`, `Seguridad/ContadorSesion`, `DAL/Acceso` | Ing. de Software |
| **Factory Method** | `GUI/Exportacion/GeneradorReporte` (Creator) → `Exportador` (Product: PDF/CSV/TXT) | Bloque 3 |

### 6.2 Base de datos
- **Un solo script**: `BD/00_Instalacion_Completa.sql`. Idempotente de punta a punta; crea la BD `WardrobeFlowDB`, tablas, seeds,
  árbol de permisos, usuarios y datos demo. Migra bases instaladas con versiones previas (renombre de rol, retiro de roles viejos).
- **Secciones** (numeración histórica): 01 base y núcleo · 05 renovación · 06 menú · 08 cobro · 09–14 analítica (PdN8–13) ·
  15 fidelización (pausa, referidos, cargo) · 16 lista de espera · 17 PN02 · 18 PN03 · 19 PN04 · 20 hardening de integridad ·
  **20b** importe/promoción en `Contratacion`, CHECKs, índices únicos y de consulta · **21** datos de prueba.
- **Datos de prueba (sección 21):** 11 clientes en distintos estados de suscripción, 20 prendas (3 En Limpieza, 1 Baja con cargo),
  9 pedidos, 3 contrataciones (2 pendientes, 1 cobrada), 3 promociones y 2 sugerencias en distintos estados, 1 lista de espera, empleados
  vinculados a `caja`/`admin`. Se aplica una sola vez (marca: cliente Julieta Navarro).
- Los dígitos verificadores de los datos sembrados van en 0 y la app los recalcula en el primer arranque.
- El acceso usa `Integrated Security=True`: el script **no** crea logins/`GRANT`; la app debe correr con una cuenta de Windows con acceso a la BD.

### 6.3 Instalador (A01)
- `Instalador/WardrobeFlow_Setup.iss` (Inno Setup 6): verifica .NET 4.7.2, detecta instancias SQL (registro/LocalDB), verifica que el servicio esté iniciado
  (lo arranca si puede), reescribe el `Data Source` de `GUI.exe.config`, ejecuta el script con `DbInstaller.exe run-script`, hace **rollback** ante fallos y
  guarda `install.log`. Copia la app (14 archivos de `GUI/bin/Release`), el `.sql`, `DbInstaller.exe` y `Credenciales_Iniciales.txt`.
- `Instalador/DbInstaller/` (C#): cliente SQL embebido (subcomandos `run-script`, chequeo/arranque de servicio, prueba de conexión, borrado de BD).
- **Un solo `.exe`** de salida: `Instalador/Salida/Instalador_WardrobeFlow_V1.exe` (no se versiona).
- `Instalador/compilar-y-firmar.ps1`: compila con Inno Setup y firma con SignTool (certificado en `%USERPROFILE%\wardrobeflow.pfx`, **autofirmado**, fuera del repo;
  contraseña por `WF_PFX_PASS` o prompt). Un certificado autofirmado **no** evita el aviso de SmartScreen en otras PCs.
- Requisitos de la cátedra (clase 4): un `.exe`, crea BD/tablas por script (sin `.bak`), usuarios/roles, multiidioma, datos de ejemplo, una sola corrida.
  Casos especiales (sin instancia, sin motor, servicio detenido) son de la Entrega 3 y ya están contemplados.
  **No probado de punta a punta en una máquina limpia** (ver §8).

### 6.4 Compilar y probar
- Abrir `WardrobeFlow.slnx` en Visual Studio, compilar y correr los tests desde el Explorador de pruebas; o por línea de comandos con MSBuild (`WardrobeFlow.slnx /p:Configuration=Release /restore`) y `vstest.console.exe Tests\bin\Release\Tests.dll`.
- Los tests son unitarios con fakes (`Tests/Fakes/`): **no** ejecutan SQL. Las garantías de concurrencia a nivel de BD (claims atómicos, índices únicos) se probaron a mano contra SQL Server, no con tests automáticos.
- Para generar el instalador: compilar en Release (incluido `Instalador/DbInstaller`) y correr `Instalador/compilar-y-firmar.ps1`.

---

## 7. Decisiones de diseño vigentes y desvíos conocidos

### 7.1 Decisiones (con su justificación)
| # | Decisión | Por qué |
|---|---|---|
| D1 | **`Plan.Precio` = importe de cada cobro**; la modalidad (mensual/trimestral/anual) solo define la duración del ciclo | Es lo que ya hace el cobro recurrente (`ProcesarPagoHandler` cobra `plan.Precio`); NUULY es solo mensual. Si se quisiera "precio × meses" habría que decidirlo y cambiar cobro y contratación |
| D2 | **Cargo por daño irreparable** (además de por pérdida) | Decisión propia para proteger el inventario; **difiere de NUULY** (que no cobra daños). Debe redactarse así en el documento del trabajo (no como "alineado a NUULY") |
| D3 | **Crédito de referido inmediato** ($1000 fijo al activar) | Simplificación de NUULY (7 días de espera, tope 12/año, descuento de bienvenida) |
| D4 | **Cuenta desbloqueada = sin prendas `EnUso`** (cubre pedidos Pendiente, Despachado y Entregado sin devolver) | Adopta NUULY 4.2/4.6 y reemplaza la validación por cupo con prendas en uso, que era más laxa que G02 |
| D5 | **Un solo descuento por ciclo**, el mayor entre promoción y crédito por referido | NUULY 5.1; el crédito no usado se acumula |
| D6 | Promociones **por categoría informativas** | No hay compra/precio de prenda donde aplicarlas |
| D7 | Verificación y reserva de PN01 **atómicas en un paso** (sin cola de Depósito) | NUULY confirma contra stock y bloquea; el centro de distribución prepara después |
| D8 | Cargo y baja de PN04 **sin transacción común** (BLL.Prenda y BLL.CargoPrenda son servicios distintos); el cargo va primero | Riesgo aceptado y comentado en el código: peor caso, un cargo sin baja |
| D9 | Claim + activación de PN02 **sin transacción común**; se compensa con `ReabrirPago` | Tablas distintas; la ventana residual (caída entre ambos pasos) queda documentada en `BLL/Contratacion.cs` |
| D10 | Pausar **no extiende** el vencimiento | Decisión de diseño documentada en `BE/Cliente.cs` |

### 7.2 Desvíos conocidos entre el documento del trabajo (`WardrobeFlow - Trabajo de Diploma.docx`) y el código
| Tema | Documento | Código actual | Acción sugerida |
|---|---|---|---|
| Roles (G05) | Supervisor, Operador de Inventario, Depósito | 10 roles reales (§2.2); Supervisor no existe; "Depósito" = rol `Deposito` | actualizar G05 (y G02/G04) |
| Estados de prenda (G04) | "Disponible / En uso" | 4 estados | actualizar G04 |
| Despacho (M05) | email con tracking; estados Despachado/En curso/On Hold | sin email/tracking; estados Pendiente/Despachado/Entregado/Cancelado | ajustar el documento |
| Cargos (M06) | autorización por Supervisor | sin aprobador (PN04 del propio documento lo dice) | ajustar M06 |
| Incidencias por demora | listadas | no hay cargo ni registro por demora | quitar del alcance |
| Compra definitiva (G01) | descrita | **no implementada** | quitar del alcance o marcar futuro |
| PN01 con Depósito | CU01/02-DEP como pasos separados | lógica en BLL, sin actor/pantalla de Depósito | reescribir PN01 (D7) |
| PN04 "alineado a NUULY" | binaria "sin cargos por daño" implícito | cobra daño irreparable | reescribir el texto (D2) |
| N01 | debe estar documentado (Plan de Entregas) | **no aparece en este `.docx`** (verificar si vive en otro archivo/Entrega 1) | confirmar |
| Diagramas | PN02–PN04 y secuencias de PN01 "a incorporar desde Enterprise Architect"; Diagrama de Clases/Modelo de Datos de PN02–PN04 vacíos | — | pendiente (se dejan para el final) |

---

## 8. Plan de entregas y estado de cumplimiento

Fuente: `Plan de Entregas TD 2026.xlsx` y notas de la clase 4. Fechas: **Entrega 1** 31/08 · **Parcial 1** 21/09 · **Entrega 2** 05/10 · **Parcial 2** 02/11 ·
**Entrega 3** 09/11 · **Recuperatorio** 16/11.

| Entrega | Exige (Plan) | Estado |
|---|---|---|
| **1** | G00–G08 + N01 analizado y diseñado | N01 diseñado en su momento (`Entrega1.eapx`, diagramas "PN01 - …" de `Diagramas VALEN/`); ver §7.2 sobre su ausencia en el `.docx` actual |
| **2** | N01 implementado y documentado · N02 (= **PN01–PN04**) analizado, diseñado e implementado (roles, descripción funcional, diagrama de proceso, modelo conceptual, casos de uso no-ABM, diagrama de clases y modelo de datos) · **A01** instalador (caso simple) · G07/G08 refinados · balanceo con la implementación | **Código:** N01 ✔; PN02 ✔; PN04 ✔; PN03 ✔ (ahora con aplicación al cobro); PN01 parcial (sin actor Depósito, §4). **Instalador:** `.exe` firmado, BD por script con datos de prueba; **falta la prueba en máquina limpia**. **Documento:** texto, roles y casos de uso de PN01–PN04 escritos; **faltan diagramas** (actividad, casos de uso, secuencia, clases, conceptual, DER) y los ajustes de §7.2; G07/G08 a regenerar desde código/SQL (78 BE, 81 BLL, 52 DAL, 76 GUI aprox.; 31 tablas) |
| **3** | N03 (proceso complejo que cruce información para decidir) · D01 manual de instalación · D02 ayuda en línea · D03 material de usuario · A01 casos especiales · A02 informe PDF y **serialización** | Instalador con casos especiales: hecho (`.iss`). PDF: hecho (analítica). **Serialización (A02): no encontrada en el código** (búsqueda de `XmlSerializer/BinaryFormatter/DataContractSerializer/JsonConvert/[Serializable]` sin resultados; solo hay exportación CSV/TXT). **Ayuda en línea (D02): no hay** (`HelpProvider`/F1 sin resultados). N03: PN03 conectado a la analítica es la base natural |

Criterios de evaluación del Plan (balanceo de clases, DER, casos de uso, secuencia; UI; POO; BD 3FN; presentación): el código y el SQL son la fuente para el balanceo;
puntos discutibles de 3FN ya detectados: `Categoria`/`Talle`/`Color` son texto libre repetido (sin tablas de catálogo); `Contratacion` mezcla datos del pago; `Prenda.IdClienteActual`/`IdUltimoCliente` desnormalizados; campos `Actor` como texto.

---

## 9. Glosario

| Término | Significado |
|---|---|
| **Cuenta bloqueada / desbloqueada** | Cliente con / sin prendas `EnUso` pendientes de devolución (D4). Se desbloquea al registrar la devolución |
| **Claim atómico** | `UPDATE ... WHERE Estado = @esperado` que solo una sesión puede ganar |
| **Compensación** | Deshacer un paso previo (`ReabrirPago`) cuando falla el siguiente |
| **Patente** | Permiso simple (`mnuXxx`); un rol agrupa patentes y otros roles (Composite) |
| **DVH / DVV** | Dígito verificador horizontal (fila) / vertical (tabla) |
| **Gracia** | Plazo de 5 días tras un cobro fallido antes de suspender por pago |
| **Modalidad** | Mensual / Trimestral / Anual: duración del ciclo de cobro (no cambia el precio, D1) |
| **PN / PdN / N** | PN01–PN04 procesos de la Entrega 2; PdN1–13 procesos/reportes del Bloque 1 y 3; N01 proceso de la Entrega 1 |
| **Depósito** | Nombre de negocio del rol técnico `Deposito` (antes `OperadorDeInventario`) |
| **Gerencia / Administración / Contabilidad** | Nombres de negocio de `GerenteComercial` / `AdministracionComercial` / `Contabilidad` en PN03 |
| **Crédito por referido** | `Cliente.DescuentoProximoCobro` acumulado por referir; se consume solo si gana frente a una promoción |
| **Unlock** | Concepto de NUULY: nueva selección solo tras devolver; equivale a "cuenta desbloqueada" |
| **Thrift** | Canal de NUULY de venta de excedente; **no** implementado |
| **Patente Editar** | Variante `...Editar` de una patente: permite modificar, no solo ver |
