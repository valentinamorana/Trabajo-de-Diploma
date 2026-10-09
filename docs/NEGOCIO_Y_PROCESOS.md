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
| Modelo | Suscripción mensual (USD 98, 6 prendas, +4 por USD 22), sin niveles ni permanencia mínima | **Adaptado:** varios planes (`PlanSuscripcion`, con `LimitePrendas` y `Precio` **mensual**) y 3 modalidades de cobro (mensual/trimestral/anual, requeridas por el Builder de la cátedra). Cada cobro cubre 1, 3 o 12 meses: importe = `Precio` × meses, **sin descuento por modalidad** (los descuentos salen solo de las promociones, PN03) |
| Pedido | Se confirma contra stock y se **bloquea**; el centro de distribución prepara después | **Adoptado:** el pedido se confirma y reserva de forma atómica; no hay edición posterior |
| Desbloqueo | No se arma otro pedido hasta que la devolución esté escaneada (4.2/4.6) | **Adoptado** como *cuenta desbloqueada* (§4, PN01/PN04) |
| Descuentos | **Un solo descuento por ciclo**; los no usados se acumulan (5.1) | **Adoptado** (`BE.PoliticaDescuento`, §4 PN03) |
| Referidos | Descuento de bienvenida, crédito al referidor tras 7 días, tope de 12/año | **Simplificado:** crédito fijo de $1000 al referente **al activar** el referido; sin espera de 7 días ni tope anual |
| Cargos por daño | **No hay** cargos por daño/limpieza; solo se cobra la prenda no devuelta | **Decisión propia distinta:** se cobra el precio de reposición por daño irreparable y por pérdida (§4 PN04) |
| Compra de prenda / Thrift / bonus items / sustitución por quiebre | Existen | **No implementados** |
| Devolución tardía | Sin cargos por demora | Igual: **no hay cargos por demora** en el código |
| Pausa | Máx. 3 meses; requiere no tener pedido pendiente de devolución (4.8) | **Adoptado:** `PausarSuscripcionHandler` rechaza fechas a más de 3 meses (`err.bll.renovacion.pausa_excede_tope`) y pausas con prendas en uso (`pausa_con_prendas`); el selector de fecha de `RenovacionSuscripcionForm` está topado a 3 meses |
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
| Vendedor ≠ Caja | quien vende no cobra (salvo el Administrador, que puede hacer las dos cosas): Vendedor no tiene `CajaEditar`; Caja no tiene `ClientesEditar` | `BLL/Contratacion.cs › CrearContratacion / ConfirmarPago` (`PermisosAccion.Exigir`); T: `PermisosAccionTests`, `ContratacionTests` |
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
| `PlanSuscripcion` | `PlanSuscripcion` | `Nombre`, `LimitePrendas`, `Precio` (= **precio de un mes**; cada cobro = `Precio` × meses de la modalidad), `Estado` (activo) |
| `Cliente` | `Cliente` | `DNI`, `IdPlan`, `FechaVencimiento`, `FechaLimiteGracia`, `FechaPausaHasta`, `IdClienteReferente`, `DescuentoProximoCobro`, `BeneficioReferidoOtorgado`, `DVH` |
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
| **Prenda** (`EstadoPrenda`, patrón **State** en `BE/Estados/`) | Disponible 0 · EnUso 1 · EnLimpieza 2 · Baja 3 | Disponible→EnLimpieza\|Baja; EnLimpieza→Disponible\|Baja; EnUso→Baja (solo por *Reportar prenda perdida*); Baja = final. **→EnUso** solo al separar las prendas de un pedido (`DAL/Pedido.cs › SepararPrendas`); **EnUso→EnLimpieza** solo al registrar la devolución. `BLL/Prenda.cs › CambiarEstado` rechaza siempre EnUso→Baja y EnLimpieza→Baja: las dos van con cargo y solo las hace `BLL/InspeccionDevolucion.cs` |
| **Pedido** (`EstadoPedido`) | Pendiente 0 (formalizado) · Despachado 1 · Entregado 2 · Cancelado 3 · EnControlStock 4 · ConFaltantes 5 · Separado 6 · Desistido 7 | Armado (PN01): EnControlStock→ConFaltantes\|Separado; ConFaltantes→EnControlStock (ajuste)\|Desistido; Separado→Pendiente (formalizar). Ciclo: Pendiente→Despachado (`Despachar`)→Entregado (`MarcarEntregado`); Pendiente→Cancelado (`Cancelar`, libera prendas); Cancelado→EnControlStock (`DesCancelar` = "Reactivar": revalida y vuelve a control de stock, sin reservar); `RegistrarDevolucion` solo sobre Entregado (prendas EnUso→EnLimpieza). Entregado y Desistido son finales. Cancelar/devolver con **Command** (`BLL/Comandos/`) |
| **Contratacion** (`EstadoContratacion`) | PendientePago 0 · Pagada 1 · Cancelada 2 | Pendiente→Pagada (cobro, claim atómico) · Pendiente→Cancelada (3.er intento fallido, automático) · Pagada→Pendiente solo como **compensación** si falla la activación (`ReabrirPago`). Regla en `BE.Contratacion.TransicionValida` |
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

**Objetivo.** Que un cliente con suscripción vigente reciba un pedido formalizado, con sus prendas controladas y separadas.
**Referencia.** El código sigue el diagrama de actividad de EA (`Entrega - Procesos de Negocio/Entrega1.eapx`, `PN01 - Diagrama de Actividad.bmp`).
El carril "Controlador de Stock" lo cumple el rol **Depósito**.
**Actores.** El Vendedor identifica al cliente, arma la selección, comunica faltantes, asienta desistimientos y formaliza. Depósito revisa el stock, informa faltantes, confirma y separa.
**Precondiciones.** Vendedor y Depósito con sesión y vínculo `Empleado`; cliente registrado.
**Pantallas.**
- `GUI/NuevoPedidoForm.cs`: asistente de armado, y modo ajuste para los pedidos con faltantes.
- `GUI/ControlStockForm.cs`: Inventario → Control de Stock, patentes `mnuControlStock` y `mnuControlStockEditar`.
- `GUI/PedidosVenta.cs`: Ver faltantes · Ajustar · Desistir · Formalizar · Confirmación.
- `PedidosRealizados.cs`: muestra solo los pedidos formalizados en adelante.
**Documentos (PDF, Factory Method en `GUI/Exportacion/DocumentosPedido.cs`).**
- Planilla de control de existencias.
- Informe de disponibilidad, con los faltantes y sus alternativas.
- Constancia de prendas separadas.
- Confirmación y constancia del pedido.
- Aviso de desistimiento.
- Aviso de suscripción no vigente y aviso de pedido activo.
- Detalle de restricciones de cupo.
- Detalle de prendas confirmadas.

**Flujo (actividad del diagrama → método)**
| Carril | Actividad del diagrama | Código |
|---|---|---|
| Vendedor | Recibir identificación → Ficha del cliente | `BLL.Cliente.BuscarPorIdentificacion` busca por DNI exacto, o por nombre o apellido parcial. La ficha se muestra en el paso 1 del asistente. |
| Vendedor | Verificar la vigencia → *Informar imposibilidad* | `BLL.Pedido.VerificarVigencia`: sin plan, vencida, pausada o suspendida por pago |
| Vendedor | Revisar existencia de pedido activo → *Informar existencia* | `BLL.Pedido.RevisarPedidoActivo`. Bloquea si hay un pedido EnControlStock, ConFaltantes, Separado, Pendiente o Despachado, o prendas sin devolver. |
| Vendedor | Presentar catálogo | `BLL.Prenda.ObtenerDisponibles(idCliente)`: excluye las prendas reservadas por Lista de Espera para otro cliente |
| Vendedor | Anotar la selección ∥ Comprobar el cupo | En cada tilde se recalcula el cupo con `BLL.Cliente.ObtenerEstadoComercial`. En el envío se vuelve a validar con `BLL.Pedido.ComprobarCupo`. |
| Vendedor | ¿Excede el cupo? → Informar exceso → ¿Ajustar? No → Asentar desistimiento | `AsentarDesistimiento(modulo, idCliente, prendas, motivo)` crea un pedido **Desistido**, en la etapa Cupo y sin reservar nada |
| Vendedor | Enviar selección para control de stock | `EnviarAControlStock` → `DAL.Pedido.AltaSinReserva`: el pedido queda **EnControlStock** y las prendas siguen Disponibles |
| Depósito | Revisar stock → ¿Selección disponible? | `RevisarStock`: relee el estado de cada prenda (`VerificarDisponibilidad`) y si está reservada para otro cliente |
| Depósito | Informe de prendas faltantes | `InformarFaltantes`: sugiere hasta 3 alternativas por faltante (misma categoría y talle, disponibles) y pasa el pedido a **ConFaltantes** |
| Vendedor | Comunicar faltantes → ¿Ajustar? | Desde Pedidos de Venta: `ObtenerInformeFaltantes` |
| Vendedor | Recibir selección ajustada por disponibilidad | `AjustarSeleccion`: vuelve al punto de unión del diagrama, así que **solo** vuelve a comprobar el cupo. Reemplaza las líneas, descarta el informe anterior y el pedido vuelve a **EnControlStock** |
| Vendedor | Al ajustar excede el cupo y no lo corrige → Asentar desistimiento | `AsentarDesistimiento(modulo, pedido, motivo, Cupo, seleccionAjustada)`: exige que la selección ajustada exceda el cupo y la guarda como la selección desistida |
| Vendedor | No ajusta → Asentar desistimiento | `AsentarDesistimiento(modulo, pedido, motivo, Disponibilidad)` pasa el pedido a **Desistido** y conserva el informe de faltantes |
| Depósito | Confirmar prendas disponibles | `ConfirmarPrendasDisponibles`: marca `PedidoPrenda.Confirmada` y registra el empleado y la fecha de control |
| Depósito | Separar prendas del pedido | `SepararPrendas`: es la **reserva**. Pasa las prendas a EnUso dentro de una transacción y el pedido a **Separado**. Si otra operación tomó una prenda, no se reserva nada y se emite el informe de faltantes. |
| Vendedor | Formalizar el pedido (desde aquí no admite cambios) | `FormalizarPedido`: el pedido pasa de **Separado** a **Pendiente**, que significa "formalizado, pendiente de despacho" |
| Vendedor | Preparar la confirmación | `PrepararConfirmacion` → Confirmación y constancia del pedido |

Cada transición registra:
- `PedidoHistorial`: ENVIAR_CONTROL, DESISTIR, AJUSTAR_SELECCION, INFORMAR_FALTANTES, CONFIRMAR_PRENDAS, SEPARAR y FORMALIZAR;
- la bitácora;
- la bitácora de negocio: `EnvioControlStock`, `InformeFaltantes`, `SeparacionPrendas`, `Desistimiento` y `Venta` (al formalizar).

**Reglas de negocio PN01**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | No se arma pedido sin suscripción vigente, ni con la suscripción pausada o suspendida por pago | `BLL/Pedido.cs › VerificarVigencia` | `PedidoTests › VerificarVigencia_*` |
| 2 | No se arma pedido si el cliente tiene otro pedido activo (en cualquier estado del armado, o despachado) | `RevisarPedidoActivo` (`err.bll.pedido.pedido_activo` / `ya_despachado`) | `RevisarPedidoActivo_PedidoEnCadaEstadoDelArmado_LanzaPedidoActivo`, `RevisarPedidoActivo_ConDespachoActivo_LanzaYaDespachado` |
| 3 | **Cuenta bloqueada:** con prendas `EnUso` sin devolver no se arma otro pedido. Se desbloquea cuando PN04 registra la devolución. | `RevisarPedidoActivo` (`err.bll.pedido.cuenta_bloqueada`) | `RevisarPedidoActivo_ConPrendasPendientesDeDevolucion_LanzaCuentaBloqueada` |
| 4 | La cantidad no puede superar el `LimitePrendas` del plan | `ComprobarCupo` | `ComprobarCupo_*`, `EnviarAControlStock_SuperaLimiteDelPlan_LanzaLimitePlan` |
| 5 | Enviar a control **no reserva**: las prendas se reservan recién al separarlas | `EnviarAControlStock` → `AltaSinReserva` | `EnviarAControlStock_DatosValidos_CreaPedidoEnControlSinReservar` |
| 6 | El desistimiento por cupo solo se asienta si la selección (nueva o ajustada) excede el cupo, después de pasar por "¿Posee pedido activo? No", y siempre exige un motivo | `AsentarDesistimiento` | `AsentarDesistimiento_*` |
| 7 | Solo Depósito revisa, informa faltantes, confirma y separa | `PermisosAccion.Exigir(ControlStockEditar)` | `AccionesDeDeposito_SinPermisoDeControlDeStock_Rechazan` |
| 8 | Las alternativas son prendas Disponibles de la misma categoría y talle, que no están en el pedido (máximo 3) | `SugerirAlternativas` | `InformarFaltantes_SugiereAlternativasDeLaMismaCategoriaYTalle` |
| 9 | Solo se separa lo confirmado. La separación es **atómica** (`UPDATE ... AND Estado = Disponible`): si otra operación tomó una prenda, se revierte todo y el pedido pasa a faltantes. | `SepararPrendas`, `DAL/Pedido.cs › SepararPrendas` | `SepararPrendas_*` |
| 10 | Solo se formaliza un pedido Separado, y después de formalizar no hay operación que cambie la selección. Cada transición del armado exige `BE.Pedido.TransicionValida` (`err.bll.pedido.transicion_invalida`) | `FormalizarPedido`, `ExigirTransicion` | `FormalizarPedido_*`, `PedidoFormalizado_NoAdmiteModificaciones`, `TransicionesDelArmado_SiguenElDiagrama` |
| 11 | Los pasos del armado no se pueden revertir desde el historial | `RestaurarOperacion` (`err.bll.pedido.restaurar_no_permitido`) | `RestaurarOperacion_PasoDelControlDeStock_NoSePuedeRevertir` |
| 12 | Reactivar un pedido cancelado revalida la vigencia, el pedido activo y el cupo, y lo devuelve a control de stock sin reservar | `DesCancelar` | `DesCancelar_ClienteConOtroPedidoActivo_Rechaza`, `DesCancelar_SuscripcionVencida_Rechaza` |
| 14 | Los avisos que cortan el circuito (suscripción no vigente, pedido activo) quedan en la bitácora y se pueden imprimir | `VerificarVigencia`, `RevisarPedidoActivo` (`RegistrarAviso`); `DocumentosPedido.AvisoImposibilidad` | Arnés contra la BD real |
| 15 | Solo se prepara la confirmación de un pedido formalizado que no fue cancelado | `BE.Pedido.EstaFormalizado` | `PrepararConfirmacion_*` |
| 13 | Una prenda que pasa de En Limpieza a Disponible se reserva 48 h para el primer anotado de la Lista de Espera. La reserva se cierra al separar. | `BLL/ListaEspera.cs › NotificarSiCorresponde`, `SepararPrendas` | `ListaEsperaTests`, `SepararPrendas_Confirmadas_ReservaYCierraLaListaDeEspera` |

**Base de datos.**
- `Pedido` tiene columnas nuevas: FechaEnvioControl, FechaControl, IdEmpleadoControl, FechaSeparacion, FechaFormalizacion, MotivoDesistimiento y EtapaDesistimiento.
- `PedidoPrenda` suma la columna `Confirmada`.
- El informe de faltantes se guarda en 3FN, en `PedidoFaltante` y `PedidoFaltanteAlternativa`.

**Casos de uso:**
- CU01-VEN Armar Pedido y Enviar a Control de Stock;
- CU02-VEN Consultar Catálogo;
- CU03-VEN Consultar Situación del Cliente;
- CU04-DEP Controlar Stock del Pedido;
- CU05-VEN Comunicar Faltantes, Ajustar o Desistir;
- CU06-VEN Formalizar Pedido.

**Alcance.** Abarca todo el diagrama de actividad, desde la solicitud hasta la confirmación al cliente.
**No abarca:** empaque físico, envío con tracking ni email al cliente.
**Ciclo posterior del pedido formalizado** (`PedidosRealizados`): Despachar → Marcar entregado → Registrar devolución (PN04). Un pedido formalizado todavía se puede cancelar desde Pedidos de Venta (patrón Command).

---

### PN02 — Comercialización de la suscripción

**Objetivo.** Que un cliente identificado elija un plan y una modalidad, abone en Caja (separada de quien vendió) y, recién al cobrar, quede vigente su suscripción.
**Referencia.** Flujo aprobado por la alumna. El diagrama original venía de otro trabajo ("ExperienceHub") y se adaptó a WardrobeFlow:
- se quitaron "Informar condiciones" y "¿Acepta condiciones?";
- se agregaron la identificación por DNI, la validación, el descuento único, el referido y los intentos.

**Actores.** Cliente (externo), Vendedor y Caja.
**Pantallas.**
- `GUI/NuevaContratacionForm.cs` (Vendedor). También se abre desde Clientes después de un alta.
- `GUI/ContratacionesPendientesForm.cs` (Caja), con dos vistas: Pendientes y Resueltas, para volver a imprimir.

**Documentos (PDF, Factory Method en `GUI/Exportacion/DocumentosContratacion.cs`):**
- Planes disponibles;
- Aviso de desistimiento;
- Orden de cobro;
- Liquidación (con los intentos);
- Comprobante;
- Constancia de suscripción;
- Constancia de cancelación.

La Liquidación lista los **planes de cuotas** de la modalidad y el Comprobante muestra las cuotas, el recargo y el
valor de cada cuota cuando se pagó con tarjeta de crédito.

**Flujo (actividad → método de `BLL/Contratacion.cs`)**
| Carril | Actividad | Código |
|---|---|---|
| Vendedor | Identificar cliente → ¿Registrado? | `IdentificarCliente` (`BLL.Cliente.BuscarPorIdentificacion`). Si no está registrado: "Registrar cliente" (ABM, referente opcional). |
| Vendedor | Presentar planes («Planes disponibles») | `PresentarPlanes`: planes activos con precio y límite |
| Cliente/Vendedor | ¿Elige plan y modalidad? No → Asentar desistimiento | `AsentarDesistimiento` → tabla `DesistimientoContratacion` (motivo obligatorio) |
| Vendedor | Registrar contratación → ¿Contratación válida? | `ValidarContratacion` (consulta) y `RegistrarContratacion`: guarda la contratación PendientePago con el **precio mensual pactado**. Si no es válida, "Informar motivo" queda en la bitácora. |
| Caja | Consultar cola → Calcular importe («Liquidación») | `ObtenerPendientesDePago`, `CalcularImporte(s)`: precio pactado × meses menos **un** descuento (promoción PN03 o crédito por referido, el mayor) |
| Cliente/Caja | Abonar → ¿Paga con tarjeta de crédito? Sí → Ofrecer planes de cuotas («Planes de cuotas») → Elegir cantidad de cuotas → Calcular recargo y valor de cuota («Detalle de financiación») | `ObtenerPlanesCuotas(modalidad)` (`BE.PoliticaCuotas.Disponibles`), `CalcularImporte(contratacion, idMedioPago, idPlanCuotas)` (`BE.PoliticaCuotas.Financiar`). La tarjeta financia: Caja cobra el total en un solo cobro y se registra el plan y el recargo. |
| Cliente/Caja | ¿Se concreta el pago? Sí | `ConfirmarCobro(idMedioPago, importeConfirmado, idPlanCuotas)` (ver los pasos debajo de la tabla) |
| Caja | ¿Se concreta? No → Registrar intento → ¿Máximo de 3? | `RegistrarIntentoFallido(idMedioPago, motivo)`: en una transacción con bloqueo de fila guarda el intento en `ContratacionIntentoPago` y, en el 3.º, **cancela automáticamente** («Constancia de cancelación»). Si no se llegó a 3, la contratación sigue en la cola. |

Pasos de `ConfirmarCobro`:
1. Revalida el estado, el medio de pago (catálogo `MedioPago`), el plan de cuotas (`ResolverCuotas`), el plan, el cupo y que el importe sea el confirmado.
2. **Claim atómico** que emite el comprobante `CMP-NNNNNN-AAAAMMDD`, guarda el plan de cuotas y el recargo (si pagó con tarjeta de crédito) y, en la misma transacción, liquida los cargos por daño o pérdida pendientes del cliente (PN04) que se sumaron al importe.
3. Activa la suscripción con el Builder (el período va a continuación del vencimiento vigente).
4. ¿Referido? Sí: acredita $1000 al referente.
5. Guarda la vigencia y el referente acreditado («Constancia de suscripción»).
6. Si la activación falla, **compensa** con `ReabrirPago`.

**Reglas de negocio PN02**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | La contratación exige un cliente activo y un plan activo, y que el plan alcance para las prendas en uso | `ValidarContratacion` | `ContratacionTests › ValidarContratacion_*` |
| 2 | Un cliente no puede tener **dos** contrataciones pendientes | `ValidarContratacion` + índice único `UX_Contratacion_UnaPendientePorCliente` | `ValidarContratacion_ClienteYaTienePendiente_*` |
| 3 | Desistir exige un motivo; no genera contratación ni cobro | `AsentarDesistimiento`; CHECK `CHK_DesistContr_ModalidadConPlan` | `AsentarDesistimiento_*` |
| 4 | Caja cobra el **precio pactado** en la orden aunque el plan cambie de precio mientras espera | `Contratacion.PrecioMensual` | `ConfirmarCobro_ElPlanCambioDePrecio_CobraElPrecioPactado` |
| 5 | Un solo descuento por cobro: el mayor entre la promoción vigente y el crédito por referido | `BE.PoliticaDescuento` | `EndurecimientoPn02Pn03Tests` |
| 6 | No se cobra un importe distinto del confirmado por Caja | `ConfirmarCobro` (`importe_cambiado`) | `ConfirmarCobro_ImporteDistintoDelConfirmado_*` |
| 7 | El medio de pago sale del catálogo `MedioPago` (3FN) | `ValidarMedioPago` (`medio_invalido`) | `ConfirmarCobro_MedioDePagoInexistente_*` |
| 8 | **Doble cobro imposible:** solo una sesión de Caja gana el claim | `DAL.ConfirmarCobro` (`WHERE Estado = 0`) | `ConfirmarCobro_OtraSesion*` |
| 9 | Si la activación falla, la contratación vuelve a Pendiente; si además no se puede reabrir, se registra un aviso CRÍTICO | `ConfirmarCobro` + `ReabrirPago` | `ConfirmarCobro_FallaLaActivacion_*` |
| 10 | Cada intento queda registrado (número, medio, motivo, quién); al tercero se cancela automáticamente | `DAL.RegistrarIntentoFallido` (transacción, `UPDLOCK`); CHECK `NroIntento 1..3` | `RegistrarIntentoFallido_*` |
| 11 | El referido se acredita una sola vez y queda registrado en la contratación | `BLL.Cliente.ActivarSuscripcionInterna`; `Contratacion.IdReferenteAcreditado` | `ConfirmarCobro_ClienteReferido*` |
| 12 | Máquina de estados: PendientePago → Pagada \| Cancelada; Pagada → PendientePago solo como compensación | `BE.Contratacion.TransicionValida` | `TransicionValida_*` |
| 13 | El Vendedor no cobra y Caja no vende (patentes) | `PermisosAccion` | `RegistrarContratacion_UsuarioDeCaja_*`, `ConfirmarCobroYRegistrarIntento_UsuarioVendedor_*` |
| 14 | Con una contratación pendiente no se puede dar de baja al cliente, ni renovar ni cobrar por N01 | `Cliente.Baja`, `Renovacion.Procesar`, `Cobro.Procesar` | `Cliente_Baja_*`, `Renovacion_*`, `Cobro_*` |
| 15 | Activar una suscripción o corregir el plan sin pasar por Contratación + Caja es exclusivo del Administrador | `Cliente.ActivarSuscripcion`, `PuedeCorregirPlanDirectamente` | `Cliente_ActivarSuscripcion_NoAdministrador_Rechaza` |
| 16 | Solo la **tarjeta de crédito** permite pagar en cuotas (`MedioPago.PermiteCuotas`); con otro medio se cobra en un solo pago | `ResolverCuotas` (`cuotas_medio`) | `ConfirmarCobro_CuotasConTarjetaDeDebito_*`, `ConfirmarCobro_MedioQueNoFinancia_*` |
| 17 | No más cuotas que los meses que cubre la modalidad (Mensual 1, Trimestral hasta 3, Anual hasta 12) y solo planes activos | `BE.PoliticaCuotas.PermiteModalidad` (`cuotas_modalidad`, `cuotas_invalidas`) | `ObtenerPlanesCuotas_*`, `ConfirmarCobro_MasCuotasQueMeses*`, `ConfirmarCobro_PlanDeCuotasInexistenteOInactivo_*` |
| 19 | **Cambio a un plan igual o más barato con el período vigente: no se registra**; rige al vencer el período pagado (el sistema no guarda un "plan siguiente"). Un plan más caro rige desde hoy con crédito por los días no usados (upgrade), aunque el total quede en 0 | `ValidarContratacion` (`cambio_plan_vigente`), `BLL.Politicas.PoliticaCambioPlan` | `ValidarContratacion_PlanMasBarato*`, `ConfirmarCobro_UpgradeConTotalCero_IgualArrancaHoy` |
| 20 | **Los cargos por daño o pérdida pendientes (PN04) se suman al cobro** y se liquidan en la misma transacción; si la activación falla vuelven a Pendiente | `ConfirmarCobro`, `DAL.Contratacion.ConfirmarCobro`/`ReabrirPago` | `CalcularImporte_ConCargosPendientes_*`, `ConfirmarCobro_ConCargosPendientes_*`, `ConfirmarCobro_FallaLaActivacion_LosCargosVuelvenAPendientes` |
| 18 | Recargo por financiación según el plan (1 cuota sin interés; 3 cuotas 5 %; 6 cuotas 10 %; 12 cuotas 20 %), sobre el total con el descuento, redondeado a 2 decimales; la tarjeta financia y Caja cobra el total en un solo cobro | `BE.PoliticaCuotas.Financiar`, catálogo `PlanCuotas` | `PoliticaCuotas_Financiar_*`, `ConfirmarCobro_TarjetaCreditoEn3Cuotas*` |

**Base de datos (3FN).**
- `MedioPago` es un catálogo y reemplaza el texto libre: Efectivo, Tarjeta de débito, Transferencia y Tarjeta de crédito (`PermiteCuotas`).
- `PlanCuotas` (catálogo): cantidad de cuotas, % de recargo y si está activo. `Contratacion` suma `IdPlanCuotas` (FK) y `RecargoCuotas` (el valor de cada cuota se deriva). Sección 20c2 del script.
- `ContratacionIntentoPago` reemplaza al contador derivable `IntentosPago`.
- Tabla nueva `DesistimientoContratacion`.
- `Contratacion` suma `IdMedioPago`, `PrecioMensual`, `VigenciaDesde/Hasta` e `IdReferenteAcreditado`.
- La sección 20c del script migra las bases ya instaladas.

**Casos de uso:**
- CU01-VTA Gestionar Suscripción;
- CU02-VTA Asentar Desistimiento;
- CU01-CAJ Gestionar Cobro;
- CU02-CAJ Emitir Comprobante;
- CU03-CAJ Registrar Intento y Cancelar Contratación;
- CU04-CAJ Financiar en Cuotas («extend» de CU01-CAJ: solo con tarjeta de crédito).

**No abarca:** factura fiscal ni conciliación con medios de pago reales.

---

### PN03 — Métricas, promociones y toma de decisiones

**Objetivo.** Convertir los datos del negocio en decisiones comerciales: detectar una oportunidad con un dato, formalizarla,
aprobar su impacto económico, **aplicarla al cobro** mientras está vigente y cerrarla (baja, desactivación o vencimiento).
**Referencia.** Flujo corregido y aprobado por la alumna (salidas faltantes, vigencia con vencimiento, historial y objetos).
Se mantiene que las promociones por categoría son informativas: no descuentan en el cobro.

**Actores.** Gerencia (rol GerenteComercial), Administración (AdministracionComercial), Contabilidad y Vendedor. El sistema actúa dentro de cada carril.
**Pantallas.**
- `GUI/SugerirPromocionForm.cs` (Gerencia): "Analizar métricas…", registrar la sugerencia y reimprimir las registradas.
- `GUI/PromocionesAdministracionForm.cs` + `GUI/AltaPromocionForm.cs` (Administración): aceptar o descartar sugerencias, crear, reformular, descartar, desactivar y resolver bajas; historial e impresiones.
- `GUI/PromocionesContabilidadForm.cs` (Contabilidad): análisis de margen e impacto y dictamen.
- `GUI/PromocionesVigentesForm.cs` (Vendedor): vigentes y con baja pedida; solicitar la baja.

**Documentos (PDF, Factory Method en `GUI/Exportacion/DocumentosPromocion.cs` + `GeneradorDocumentoPromocion.cs`):**
- Reporte de métricas;
- Sugerencia de promoción;
- Ficha de promoción (con su historial de estados);
- Dictamen contable;
- Solicitud de baja;
- Resolución de baja (informe a Gerencia si se aprueba, a Ventas si se rechaza);
- Constancia de descarte (de sugerencia o de promoción).

**Flujo (actividad → método)**
| Carril | Actividad | Código |
|---|---|---|
| Gerencia | Analizar métricas («Reporte de métricas») | `BLL.AnalisisPromociones.AnalizarMetricas`: abandono por plan (Strategy) y rotación por categoría; arma las oportunidades |
| Gerencia | ¿Hay oportunidad? No → fin "Sin promoción" | `AnalisisPromociones.HayOportunidad` (`BE.ReporteMetricas.HayOportunidad`) |
| Gerencia | Registrar sugerencia («Sugerencia de promoción») | `BLL.SugerenciaPromocion.RegistrarSugerencia`: guarda `OrigenMetrica` (Abandono/Rotación/Manual) e `IdUsuarioAlta` |
| Administración | ¿Acepta la sugerencia? No → Descartar sugerencia | `SugerenciaPromocion.DescartarSugerencia` (motivo obligatorio, claim `Pendiente → Descartada`) |
| Administración | ¿Acepta? Sí → Crear promoción (o crearla manual) | `BLL.Promocion.CrearDesdeSugerencia` (claim `Pendiente → Evaluada`, compensación `ReabrirEvaluacion`) / `CrearManual` |
| Sistema | Validar → En revisión contable («Ficha de promoción») | `Promocion.ValidarPromocion` (destino único, valor, fechas); guarda `IdUsuarioAlta` y el historial `— → EnRevisionContable` |
| Contabilidad | Analizar margen e impacto | `Promocion.AnalizarMargenEImpacto`: beneficio estimado de la sugerencia y promociones Vigentes del mismo plan superpuestas en fechas (advertencia) |
| Contabilidad | ¿Aprueba? (guarda: quien la creó no la dictamina) | `Promocion.PuedeDictaminar` (`BE.Promocion.PuedeDictaminarla`); `AprobarContable` → Vigente / `RechazarContable` → RechazadaContabilidad, ambas con «Dictamen contable» |
| Administración | ¿Reformular? Sí → Reformular (vuelve a Validar) | `Promocion.Reformular` (claim `RechazadaContabilidad → EnRevisionContable`) |
| Administración | ¿Reformular? No → Descartar promoción | `Promocion.DescartarPromocion` (motivo obligatorio → Descartada; «Constancia de descarte») |
| Vendedor | (a) Solicitar la baja («Solicitud de baja») | `Promocion.SolicitarBaja` (motivo obligatorio → BajaSolicitada) |
| Administración | ¿Aprueba la baja? Sí / No («Resolución de baja») | `Promocion.AprobarBaja` → Desactivada / `RechazarBaja` (motivo obligatorio) → Vigente |
| Administración | (b) Desactivar directamente | `Promocion.Desactivar` (motivo obligatorio) |
| Sistema | (c) Llega la FechaFin → Vencida | `Promocion.CerrarVencidas`, que se ejecuta al consultar las promociones (`ObtenerTodas`, `ObtenerVigentes`, `ObtenerParaVentas`) |

Aplicación al cobro: mientras está **Vigente** y dentro de sus fechas, la promoción entra en `BE.PoliticaDescuento.Resolver`,
que usan el cobro de contratación (`BLL.Contratacion.CalcularImporte/ConfirmarCobro`, PN02) y el cobro recurrente (`ProcesarPagoHandler`, N01).
Una promoción con baja solicitada, desactivada, descartada o **vencida** no se aplica.

Cada transición pasa por el DAL como un **claim atómico** (`UPDATE ... WHERE Estado = @esperado`) que, en la misma transacción,
inserta su fila de `PromocionHistorial` y el objeto que genera (`DictamenContable`, `SolicitudBajaPromocion`).

**Reglas de negocio PN03**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | Una promoción aplica a **un plan o una categoría, nunca a ambos ni a ninguno** | `Promocion.ValidarPromocion`; CHECK `CHK_Promocion_Destino` | `PromocionTests › CrearManual_AmbosDestinos_*`, `SugerenciaPromocionTests` |
| 2 | `Valor > 0`; si es Porcentaje, ≤ 100; fecha fin ≥ fecha inicio | ídem; CHECK `CHK_Promocion_Valor/Porcentaje/Fechas` | `PromocionTests › CrearManual_*` |
| 3 | Sin oportunidad en el reporte, el flujo termina sin promoción; la sugerencia guarda el origen de la métrica y quién la creó | `AnalizarMetricas`, `HayOportunidad`, `RegistrarSugerencia` | `AnalisisPromocionesTests › AnalizarMetricas_*`, `SugerenciaPromocionTests › RegistrarSugerencia_DatosValidosConPlan_*` |
| 4 | Solo una sugerencia **Pendiente** se acepta o se descarta; descartarla exige motivo | `BE.SugerenciaPromocion.PuedeEvaluarse`; `DAL.Descartar/MarcarEvaluada ... AND Estado = 0` | `SugerenciaPromocionTests › DescartarSugerencia_*`, `EndurecimientoPn02Pn03Tests › CrearDesdeSugerencia_*` |
| 5 | Toda promoción **nace En Revisión Contable** (tras Validar) y no aplica descuento hasta ser aprobada | `CrearDesdeSugerencia/CrearManual` | `PromocionTests › CrearManual_GuardaElCreadorYEscribeHistorialDeAlta` |
| 6 | **Quien creó la promoción no puede dictaminarla** (también el Administrador) | `Promocion.PuedeDictaminar`; `IdUsuarioAlta` | `PromocionTests › AprobarContable_QuienCreo*`, `RechazarContable_QuienCreo*` |
| 7 | El dictamen exige observación y queda guardado (resultado, observación, usuario, fecha) | `AprobarContable/RechazarContable`; tabla `DictamenContable` | `PromocionTests › AprobarContable_GuardaElDictamen*`, `RechazarContable_*` |
| 8 | Contabilidad ve el beneficio estimado de la sugerencia y las promociones vigentes del mismo plan superpuestas (no impide aprobar) | `AnalizarMargenEImpacto`, `BE.Promocion.SeSuperponeCon` | `PromocionTests › AnalizarMargenEImpacto_*` |
| 9 | Solo una promoción **Rechazada** se reformula o se descarta; descartar exige motivo | `Reformular`, `DescartarPromocion` | `PromocionTests › DescartarPromocion_*`, `EndurecimientoPn02Pn03Tests › Reformular_*` |
| 10 | Solo se pide la baja de una **Vigente**, con motivo; la solicitud queda guardada | `SolicitarBaja`; tabla `SolicitudBajaPromocion` (una pendiente por promoción) | `PromocionTests › SolicitarBaja_*` |
| 11 | Solo se resuelve una **BajaSolicitada**; rechazarla exige motivo; la resolución queda guardada y **no se pierde** el dictamen contable | `AprobarBaja/RechazarBaja` | `PromocionTests › AprobarBaja_*`, `RechazarBaja_*` |
| 12 | La desactivación directa exige motivo (queda en el historial) | `Desactivar` | `PromocionTests › Desactivar_*` |
| 13 | Al llegar la FechaFin la promoción pasa a **Vencida** y no se aplica en el cobro | `CerrarVencidas`; `PoliticaDescuento.Resolver` (`!EstaVencida() && EstaVigente()`) | `PromocionTests › CerrarVencidas_*`, `PoliticaDescuentoTests › Resolver_PromocionVencida_NoSeAplica` |
| 14 | **Cada transición escribe historial** y es atómica frente a otra sesión | `DAL/Promocion.cs` (claim + `PromocionHistorial` en una transacción) | `PromocionTests › RecorridoCompleto_CadaTransicionEscribeSuFilaDeHistorial`, `EndurecimientoPn02Pn03Tests › *_OtraSesion*` |
| 15 | Máquina de estados: EnRevisión → Vigente \| Rechazada; Rechazada → EnRevisión \| Descartada; Vigente → BajaSolicitada \| Desactivada \| Vencida; BajaSolicitada → Desactivada \| Vigente. Desactivada, Descartada y Vencida son finales | `BE.Promocion.TransicionValida`, `BE.SugerenciaPromocion.TransicionValida` | `PromocionTests › TransicionValida_*` |
| 16 | **Un solo descuento por ciclo:** compiten la mejor promoción vigente **del plan del cliente** y el crédito por referido; se aplica el **mayor**; si gana la promoción el crédito queda acumulado; en empate gana la promoción | `BE/PoliticaDescuento.cs › Resolver` | `PoliticaDescuentoTests`, `CobroTests`, `ContratacionTests` |
| 17 | Tipos de descuento: `Porcentaje`, `MontoFijo` (tope = bruto), `PrecioPromocional` (`Valor` = precio **mensual**) | `PoliticaDescuento.DescuentoDe` | `PoliticaDescuentoTests` |
| 18 | Las promociones por **categoría son informativas** (no descuentan en el cobro de la suscripción) | `PoliticaDescuento.Resolver` (solo `AplicaAPlan`) | `PoliticaDescuentoTests` |

**Base de datos (3FN).**
- `SugerenciaPromocion` suma `OrigenMetrica`, `IdUsuarioAlta`, `MotivoDescarte`, `FechaEvaluacion` y el estado Descartada (2).
- `Promocion` suma `IdUsuarioAlta` y los estados Descartada (5) y Vencida (6).
- Tablas nuevas `PromocionHistorial`, `DictamenContable` y `SolicitudBajaPromocion`.
- `Promocion.Observacion` y `Promocion.MotivoBaja` se migran a esas tablas y se quitan.
- La sección 20d del script migra las bases ya instaladas.

**Casos de uso:**
- CU01-GER Sugerir Promoción (incluye CU03-GER Analizar Métricas);
- CU02-GER Consultar Analítica de Negocio;
- CU01-ADM Gestionar Promociones (crear desde sugerencia o manual, reformular), extendido por CU03-ADM Descartar Sugerencia y CU04-ADM Descartar Promoción Rechazada;
- CU05-ADM Desactivar Promoción;
- CU01-CONT Analizar Promoción;
- CU01-VEN Solicitar Baja de Promoción;
- CU02-ADM Resolver Baja de Promoción.

**Alcance.** Abarca todo el diagrama de actividad y su aplicación al importe del cobro. **No abarca:** aplicar promociones por categoría a un precio (no hay compra de prenda);
descuentos acumulables; el "margen estimado" es solo informativo para Contabilidad.
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
   - **Daño irreparable → baja con cargo:** `BLL.InspeccionDevolucion.DarDeBajaConCargo` (precio de reposición; pre-cargado desde `Prenda.PrecioReposicion`, editable). Cargo, baja, cierre del mantenimiento y cancelación de la lista de espera van en **una transacción** (`DAL/InspeccionDevolucion.cs`); la transición la valida el patrón State (`Prenda.TransicionPermitida` / `ControlarEstado`).
3. **Prenda perdida = compra tácita** (CU-DEP-02, desde el detalle del pedido): `BLL.InspeccionDevolucion.ReportarPerdida`, solo si el pedido está **Entregado hace 30 días o más** (`BLL/Politicas/PoliticaCompraTacita.cs`). Antes, la prenda sigue siendo un alquiler en curso; si el pedido no se entregó, el cliente nunca la recibió.
4. El cargo queda `Pendiente` contra `IdUltimoCliente` y **se suma al próximo cobro** de ese cliente (`ProcesarPagoHandler`, misma transacción).

**Reglas de negocio PN04**
| # | Regla | Ubicación | T: |
|---|---|---|---|
| 1 | Sin aprobador: resuelve Depósito directamente (solo `StockEditar`) | `BLL/CargoPrenda.cs`, `BLL/Prenda.cs` | `CargoPrendaTests`, `PrendaTests` |
| 2 | Solo se inspecciona una prenda **En Limpieza**; el `UPDATE` es condicionado al estado anterior | `DAL/Prenda.cs › CambiarEstado` | `PrendaEstadoTests`, `PrendaTests` |
| 3 | Solo se reporta como perdida una prenda **En Uso** de un pedido **Entregado hace 30 días o más** (compra tácita); `CambiarEstado` rechaza siempre EnUso→Baja | `BLL/InspeccionDevolucion.cs › ReportarPerdida`, `BLL/Politicas/PoliticaCompraTacita.cs`, `BLL/Prenda.cs` (`baja_requiere_flujoperdida`) | `AuditoriaGuiTests › ReportarPerdida_*`, `PrendaTests` |
| 4 | **En Limpieza → Baja solo desde la Inspección** (exige el cargo previo): la BLL lo impone y la pantalla genérica de Prendas no ofrece esa opción | `BLL/Prenda.cs` (`baja_requiere_inspeccion`), `GUI/Prendas.cs` | `PrendaTests › CambiarEstado_EnLimpiezaABaja*` |
| 5 | Cargo, baja, cierre del mantenimiento y cancelación de la lista de espera en **una transacción** | `DAL/InspeccionDevolucion.cs › DarDeBajaConCargo` | `AuditoriaGuiTests › DarDeBajaConCargo_*` |
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
| **Datos sensibles** | El DNI se guarda en texto plano (se quitó el cifrado AES); igualmente no se escribe en la bitácora | `Seguridad/Encriptador.cs`, `BLL/Cliente.cs` |
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
  **20b** importe/promoción en `Contratacion`, CHECKs, índices únicos y de consulta · **20c** PN02 (medios de pago, intentos, desistimientos) ·
  **20d** PN03 (historial, dictamen, solicitud de baja, vencimiento) · **21** datos de prueba.
- **Datos de prueba (sección 21):** 11 clientes en distintos estados de suscripción, 20 prendas (3 En Limpieza, 1 Baja con cargo),
  9 pedidos, 3 contrataciones (2 pendientes, 1 cobrada), 3 promociones y 2 sugerencias en distintos estados, 1 lista de espera, empleados
  vinculados a `caja`/`admin`. Se aplica una sola vez (marca: cliente Julieta Navarro).
- Los dígitos verificadores de los datos sembrados van en 0 y la app los recalcula en el primer arranque.
- El acceso usa `Integrated Security=True`: el script **no** crea logins/`GRANT`; la app debe correr con una cuenta de Windows con acceso a la BD.

### 6.3 Instalador (A01)
- `Instalador/WardrobeFlow_Setup.iss` (Inno Setup 6): verifica .NET 4.7.2, detecta instancias SQL (registro/LocalDB), verifica que el servicio esté iniciado
  (lo arranca si puede), reescribe el `Data Source` de `GUI.exe.config`, ejecuta el script con `DbInstaller.exe run-script`, hace **rollback** ante fallos y
  guarda `install.log`. Copia la app (14 archivos de `GUI/bin/Release`), el `.sql`, `DbInstaller.exe` y `Credenciales_Iniciales.txt`.
  Si el equipo no tiene ningún SQL Server, la opción por defecto instala **SQL Server 2022 Express LocalDB** (MSI oficial embebido, `msiexec /passive`)
  y continúa sobre `(localdb)\MSSQLLocalDB`. LocalDB es privado de cada usuario de Windows: la base queda para el usuario que instala.
- `Instalador/DbInstaller/` (C#): cliente SQL embebido (subcomandos `run-script`, chequeo/arranque de servicio, prueba de conexión, borrado de BD).
- **Un solo `.exe`** de salida: `Instalador/Salida/Instalador_WardrobeFlow_V1.exe` (no se versiona).
- `Instalador/compilar-y-firmar.ps1`: descarga `Redist/SqlLocalDB.msi` si falta, compila con Inno Setup y firma con SignTool, o con `Set-AuthenticodeSignature` sin Windows SDK (certificado en `%USERPROFILE%\wardrobeflow.pfx`, **autofirmado**, fuera del repo;
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
| D1 | **`Plan.Precio` = precio de UN mes**; cada cobro cubre los meses de la modalidad (mensual 1, trimestral 3, anual 12) e importa `Precio` × meses, sin descuento por modalidad | NUULY cobra por mes y no tiene niveles ni permanencia: la unidad de precio es el mes. Trimestral y anual salen del Builder que pide la cátedra; para que no regalen meses se cobran por adelantado. Los descuentos por plazo, si se quisieran, se modelan como promoción (PN03) |
| D2 | **Cargo por daño irreparable** (además de por pérdida) | Decisión propia para proteger el inventario; **difiere de NUULY** (que no cobra daños). Debe redactarse así en el documento del trabajo (no como "alineado a NUULY") |
| D3 | **Crédito de referido inmediato** ($1000 fijo al activar) | Simplificación de NUULY (7 días de espera, tope 12/año, descuento de bienvenida). No se adopta la espera de 7 días porque requeriría un proceso en segundo plano que la aplicación de escritorio no tiene; el referente se fija una sola vez al alta (no hay auto-referencia ni referido con cuenta previa) |
| D4 | **Cuenta desbloqueada = sin prendas `EnUso`** (cubre pedidos Pendiente, Despachado y Entregado sin devolver) | Adopta NUULY 4.2/4.6 y reemplaza la validación por cupo con prendas en uso, que era más laxa que G02 |
| D5 | **Un solo descuento por ciclo**, el mayor entre promoción y crédito por referido | NUULY 5.1; el crédito no usado se acumula |
| D6 | Promociones **por categoría informativas** | No hay compra/precio de prenda donde aplicarlas |
| D7 | Verificación y reserva de PN01 **atómicas en un paso** (sin cola de Depósito) | NUULY confirma contra stock y bloquea; el centro de distribución prepara después |
| D8 | ~~Cargo y baja de PN04 sin transacción común~~ | **Resuelto:** una sola transacción en `DAL/InspeccionDevolucion.cs` |
| D9 | Claim + activación de PN02 **sin transacción común**; se compensa con `ReabrirPago` | Tablas distintas; la ventana residual (caída entre ambos pasos) queda documentada en `BLL/Contratacion.cs` |
| D10 | **Pausar corre el vencimiento** por los días de pausa (solo si quedaba tiempo pagado); reanudar antes de tiempo devuelve los días no usados | NUULY 4.8: mientras dura la pausa no se cobra, así que la pausa no consume lo ya pagado. Implementado en `PausarSuscripcionHandler` y `BLL.Cliente.ReanudarPausa` |
| D11 | **Concurrencia entre sesiones**: el crédito de referido se modifica solo con `SumarCreditoEnTx` / `ConsumirCreditoEnTx` (UPDATE atómico sobre el valor real; el UPDATE general de Cliente ya no lo escribe); el cobro de cargos exige `Estado=Pendiente` y aborta todo el cobro si otra sesión ya los cobró; `Promocion.Modificar` exige `Estado=EnRevisionContable`; contratar y cobrar validan el cupo del plan contra las prendas en uso | Evita perder actualizaciones, cobrar dos veces un cargo y editar una promoción ya aprobada por otra sesión |

### 7.2 Desvíos entre el documento del trabajo (`WardrobeFlow - Trabajo de Diploma.docx`) y el código
Estado tras la edición de la tesis (N01 y PN01–PN04 documentados desde el código; G01, G02, G04, G05, G06, G07 y G08 corregidos).

| Tema | Estado | Detalle |
|---|---|---|
| Roles (G05), estados de prenda (G04) | **Resuelto** | 10 roles reales y 4 estados de prenda |
| Despacho, cargos, incidencias por demora, compra definitiva | **Resuelto** | La tesis describe solo lo implementado (sin email/tracking, sin aprobador de cargos, sin compra definitiva) |
| PN01 con Depósito, PN04 con cargo por daño | **Resuelto** | Reescritos según D7 y D2 |
| N01 | **Resuelto** | Incorporado a la tesis (sección N00) |
| Diagramas PN01–PN04 y N01 (proceso, casos de uso, secuencia, clases, conceptual, DER) | **Resuelto** | 63 diagramas generados desde el código (`docs/diagramas/`, Mermaid + draw.io), verificados en cada commit |
| Balanceo clases–secuencia (corrección de la Entrega 1) | **Resuelto** | `verificar.js` exige que toda clase y método citado en las secuencias de un proceso figure en sus diagramas de clases |
| Diagramas T02–T08 (login, perfiles, idiomas, bitácora, control de cambios, dígitos verificadores, backup) | **Resuelto** | Secuencias, clases y DER regenerados desde el código y la base; T01 (componentes, mapa MDI) y los casos de uso de T02–T08 se mantienen |
| Calidad de impresión | Limitación | Los diagramas de clases y DER son grandes: se leen ampliando en el Word |
| draw.io | Limitación | Archivos generados con disposición automática; no se abrieron uno por uno en draw.io |

---|---|---|---|
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
| **1** | G00–G08 + N01 analizado y diseñado | N01 diseñado en su momento (`Entrega1.eapx`, diagramas "PN01 - …" de `Diagramas VALEN/`); ya incorporado a la tesis (§7.2) |
| **2** | N01 implementado y documentado · N02 (= **PN01–PN04**) analizado, diseñado e implementado (roles, descripción funcional, diagrama de proceso, modelo conceptual, casos de uso no-ABM, diagrama de clases y modelo de datos) · **A01** instalador (caso simple) · G07/G08 refinados · balanceo con la implementación | **Código:** N01 y PN01–PN04 implementados (498 tests, 2 omitidos). **Instalador:** `.exe` firmado, BD por script con datos de prueba; **falta la prueba en máquina limpia**. **Documento:** hecho en la tesis para N01 y PN01–PN04 (roles, descripción, proceso, modelo conceptual, casos de uso, clases, datos) con diagramas generados desde el código; G07/G08 refinados; balanceo clases–secuencia verificado automáticamente. **Pendiente:** prueba manual (`CHECKLIST_PRUEBA_MANUAL.md`) y prueba del instalador en máquina limpia |
| **3** | N03 (proceso complejo que cruce información para decidir) · D01 manual de instalación · D02 ayuda en línea · D03 material de usuario · A01 casos especiales · A02 informe PDF y **serialización** | Instalador con casos especiales: hecho (`.iss`). PDF: hecho con iTextSharp, sin impresora virtual. **Serialización (A02): hecha** — cada error inesperado se serializa a XML con `XmlSerializer` (`Servicios/Serializacion/SerializadorXml.cs`, `RegistroErrores.cs`; `BE.RegistroError`/`LibroErrores` `[Serializable]`, un archivo por mes en `%LOCALAPPDATA%/WardrobeFlow/Errores`); Bitácora › **Errores (XML)** (`GUI/ErroresXmlForm.cs`, `BLL.ErroresSerializados`, patente de Auditoría) los muestra, importa un XML de otra PC (deserializa, sin DTD) y exporta (serializa); `SerializacionTests`. **Ayuda en línea (D02):** F1 en cualquier pantalla (`GUI/Ayuda/AyudaEnLinea.cs`). N03: PN03 conectado a la analítica es la base natural |

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
| **Modalidad** | Mensual / Trimestral / Anual: meses que cubre cada cobro (1 / 3 / 12); importe = precio mensual del plan × meses (D1) |
| **PN / PdN / N** | PN01–PN04 procesos de la Entrega 2; PdN1–13 procesos/reportes del Bloque 1 y 3; N01 proceso de la Entrega 1 |
| **Depósito** | Nombre de negocio del rol técnico `Deposito` (antes `OperadorDeInventario`) |
| **Gerencia / Administración / Contabilidad** | Nombres de negocio de `GerenteComercial` / `AdministracionComercial` / `Contabilidad` en PN03 |
| **Crédito por referido** | `Cliente.DescuentoProximoCobro` acumulado por referir; se consume solo si gana frente a una promoción |
| **Unlock** | Concepto de NUULY: nueva selección solo tras devolver; equivale a "cuenta desbloqueada" |
| **Thrift** | Canal de NUULY de venta de excedente; **no** implementado |
| **Patente Editar** | Variante `...Editar` de una patente: permite modificar, no solo ver |
