# Diagramas pendientes para Enterprise Architect

## Prompt y definiciones (se respetan en TODOS los diagramas)

> Hacer la lista de todos los diagramas que faltan. La prioridad son los de estas entregas: **PN01 y PN02**
> — DSS, diagramas de casos de uso, DER, diagrama de clases y Modelo Conceptual. Asegurarse de que
> **TODO esté basado en el sistema, en el código**, que **no se invente ningún parámetro ni método** y que
> **sea todo igual** al sistema.

Reglas que se desprenden de ese pedido:

1. **La fuente es el código y la base, nunca la memoria.** Cada clase, atributo, método, parámetro, tabla,
   columna y clave foránea que aparezca en un diagrama tiene que existir con ese mismo nombre en el
   repositorio (`BE/`, `BLL/`, `DAL/`, `GUI/`, `BD/00_Instalacion_Completa.sql`).
2. **Nada inventado.** Si un diagrama necesita algo que el código no tiene, no se dibuja: se anota en
   "Decisiones pendientes" y se resuelve en el código primero.
3. **Todo igual.** Los nombres de casos de uso, actores, actividades y objetos son los del diagrama de
   actividad aprobado y los de la aplicación (menús y botones en español).
4. **Herramienta: Enterprise Architect**, todos en un mismo proyecto (la tesis usa diagramas de EA, no
   Mermaid ni draw.io).
5. **Base de partida verificada:** `docs/diagramas/mermaid/` y `docs/diagramas/drawio/` se generan desde el
   código y la base, y `node docs/diagramas/fuente/verificar.js` comprueba que coincidan. Cada diagrama de
   EA se arma copiando ese contenido, no los dibujos viejos de la carpeta "Entrega 2" (son anteriores al
   rediseño de PN01 y PN02 y no coinciden con el código).
6. **El diagrama de actividad aprobado manda** sobre los demás del mismo proceso: los casos de uso, los DSS
   y las clases tienen que contar lo mismo que la actividad.

Estado en EA: ✅ está y coincide con el código · ⚠️ está pero desactualizado · ❌ no está.
Relevado del proyecto `Entrega - Procesos de Negocio/Entrega1.eapx` (contiene solo 4 diagramas, todos de PN01).

---

## Prioridad 1 — PN01 Armar pedido de prendas

| # | Diagrama | EA | Fuente verificada (`docs/diagramas/`) | Qué falta |
|---|---|---|---|---|
| 1 | Actividad | ✅ | `ACT_pn01_armar_pedido` | Nada. Es la referencia aprobada (carriles Cliente, Vendedor y Depósito). |
| 2 | Casos de uso | ⚠️ | `CU_pn01_armar_pedido` | El de EA tiene "CU01-DEP Verificar Disponibilidad" y "CU02-DEP Reservar Prendas", que **no existen en el código**. Faltan CU04-DEP Controlar stock, CU05-VEN Faltantes, CU06-VEN Formalizar, CU04-VEN Cancelar, CU01/02/03-DEP Despachar, Entrega y Devolución. Rehacer con los 10 CU de la fuente. |
| 3 | Modelo conceptual | ❌ | `MC_pn01_pedidos` | Crear. (`Diagramas VALEN/WardrobeFlow_ModeloConceptual.png` es una imagen, no un diagrama de EA.) |
| 4 | DER | ⚠️ | `DER_pn01_pedidos` | Faltan las tablas `PedidoFaltante` y `PedidoFaltanteAlternativa`, y las columnas nuevas de `Pedido` (FechaEnvioControl, FechaControl, IdEmpleadoControl, FechaSeparacion, FechaFormalizacion, MotivoDesistimiento, EtapaDesistimiento) y `PedidoPrenda.Confirmada`. |
| 5 | Clases | ⚠️ | `CLASES_pn01_pedidos` | Faltan las clases del control de stock (`GUI.ControlStockForm`, `BE.PedidoFaltante`, `BE/BLL.EvaluacionControlStock`, documentos de `GUI/Exportacion`) y los métodos nuevos de `BLL.Pedido` / `DAL.Pedido`. Copiar firmas exactas de la fuente. |
| 6 | Patrón Command (cancelar pedido y registrar devolución) | ❌ | `CLASES_patron_command_pedido` | Crear. |

**DSS de PN01 (uno por caso de uso) — todos ❌ en EA**

| # | Caso de uso | Fuente |
|---|---|---|
| 7 | CU01-VEN Armar pedido y enviar a control de stock | `DSS_PN01_CU01_ArmarPedido` |
| 8 | CU02-VEN Consultar catálogo | `DSS_PN01_CU02_ConsultarCatalogo` |
| 9 | CU03-VEN Consultar situación del cliente | `DSS_PN01_CU03_ConsultarSituacionCliente` |
| 10 | CU04-VEN Cancelar pedido (patrón Command) | `DSS_PN01_CU07_CancelarPedido` |
| 11 | CU05-VEN Comunicar faltantes, ajustar selección o desistir | `DSS_PN01_CU09_GestionarFaltantes` |
| 12 | CU06-VEN Formalizar pedido y preparar la confirmación | `DSS_PN01_CU10_FormalizarPedido` |
| 13 | CU01-DEP Despachar pedido | `DSS_PN01_CU04_DespacharPedido` |
| 14 | CU02-DEP Registrar entrega | `DSS_PN01_CU05_RegistrarEntrega` |
| 15 | CU03-DEP Registrar devolución | `DSS_PN01_CU06_RegistrarDevolucion` |
| 16 | CU04-DEP Controlar stock del pedido | `DSS_PN01_CU08_ControlarStock` |

(El número del archivo, por ejemplo `CU08`, es un id interno. En EA se usa el nombre del caso de uso.)

---

## Prioridad 1 — PN02 Comercialización de la suscripción

| # | Diagrama | EA | Fuente verificada | Qué falta |
|---|---|---|---|---|
| 17 | Actividad (flujo adaptado aprobado) | ❌ | `ACT_pn02_comercializacion` | Crear. Carriles Cliente, Vendedor y Caja. |
| 18 | Casos de uso | ❌ | `CU_pn02_comercializacion` | Crear. |
| 19 | Modelo conceptual | ❌ | `MC_pn02_contrataciones` | Crear. |
| 20 | DER | ❌ | `DER_pn02_contrataciones` | Crear (incluye `MedioPago`, `ContratacionIntentoPago`, `DesistimientoContratacion`). |
| 21 | Clases | ❌ | `CLASES_pn02_contrataciones` | Crear. |
| 22 | Patrón Builder (activación de la suscripción por modalidad) | ❌ | `CLASES_patron_builder_suscripcion` | Crear. |

**DSS de PN02 — todos ❌ en EA**

| # | Caso de uso | Fuente |
|---|---|---|
| 23 | CU01-VTA Gestionar suscripción (contratación) | `DSS_PN02_CU01_VTA_GestionarSuscripcion` |
| 24 | CU01-CAJ Gestionar cobro (incluye CU02-CAJ) | `DSS_PN02_CU01_CAJ_GestionarCobro` |
| 25 | CU02-CAJ Emitir comprobante | `DSS_PN02_CU02_CAJ_EmitirComprobante` |
| 26 | CU03-CAJ Registrar intento y cancelar (3 intentos) | `DSS_PN02_CU03_CAJ_CancelarContratacion` |

---

## Decisiones pendientes (no se dibujan hasta resolverlas)

Son diferencias entre documentos. **No se inventa nada**: se elige qué texto queda y se corrige donde haga falta.

| # | Diferencia | Dónde | Propuesta |
|---|---|---|---|
| D1 | La tesis lista **7** casos de uso de PN01; el código y el diagrama de casos de uso tienen **10** (faltan CU04-DEP Controlar stock, CU05-VEN Faltantes y CU06-VEN Formalizar). | `docs/diagramas/fuente/tesis/contenido.js` (cuLista de PN01) | Agregar los 3 a la tesis. |
| D2 | PN02: la tesis dice "CU03-CAJ Cancelar Contratación"; el DSS dice "Registrar intento y cancelar (3 intentos)". | `contenido.js` vs. `modelos/secuencias.js` | Unificar el nombre. |
| D3 | La regla "quien vende no cobra" ahora tiene excepción para el Administrador (decisión del 2026-10-05). | DSS CU01-CAJ, texto de PN02 | Agregar una nota en el DSS y en la tesis. |
| D4 | PN04 está postergado. Sus diagramas generados existen, pero el proceso se va a rehacer. | Prioridad 4 | No pasarlos a EA todavía. |

---

## Prioridad 2 — N01, PN03 y modelos globales del negocio

Todos ❌ en EA. Fuente en `docs/diagramas/`.

| Grupo | Diagramas |
|---|---|
| N01 Clientes y suscripciones | `ACT_n01_renovacion_cobro`, `CU_n01_clientes_suscripciones`, `MC_n01_clientes_suscripciones`, `DER_n01_clientes_suscripciones`, `CLASES_n01_clientes_suscripciones`, `CLASES_patron_chain_renovacion`, `CLASES_patron_chain_cobro`; DSS: `DSS_N01_CU01_GestionarCliente`, `DSS_N01_CU02_RenovarSuscripcion`, `DSS_N01_CU04_GestionarPlanes` (CU03-VEN), `DSS_N01_CU03_CobrarSuscripcion` (CU01-CAJ) |
| PN03 Métricas, promociones y decisiones | `ACT_pn03_promociones`, `CU_pn03_promociones`, `MC_pn03_promociones`, `DER_pn03_promociones`, `CLASES_pn03_promociones`, `CLASES_patron_strategy_abandono`; DSS: `DSS_PN03_CU01_GER_SugerirPromocion`, `DSS_PN03_CU02_GER_ConsultarAnalitica`, `DSS_PN03_CU01_ADM_GestionarPromociones`, `DSS_PN03_CU03_ADM_DescartarSugerencia`, `DSS_PN03_CU04_ADM_DescartarPromocion`, `DSS_PN03_CU05_ADM_DesactivarYVencer`, `DSS_PN03_CU01_CONT_AnalizarPromocion`, `DSS_PN03_CU01_VEN_SugerirBaja`, `DSS_PN03_CU02_ADM_ResolverBaja` |
| Globales del negocio | `MC_negocio`, `DER_negocio`, `DER_global`, `CLASES_global_dominio_a`, `CLASES_global_dominio_b`, `CLASES_patron_state_prenda` |

## Prioridad 3 — Seguridad (T02 a T08)

Todos ❌ en EA: `DER_seguridad`, `DER_seg_usuarios`, `DER_seg_permisos_idiomas`, `DER_seg_auditoria`, y por cada
tema su clases, DER y DSS: T02 (`CLASES_T02_login_logout`, `DER_T02_login`, `DSS_T02_1_Login`, `DSS_T02_2_Logout`),
T04 (`CLASES_T04_perfiles`, `DER_T04_perfiles`, `DSS_T04_AsignarPermiso`), T05 (`CLASES_T05_idiomas`,
`DER_T05_idiomas`, `DSS_T05_CambiarIdioma`, `DSS_T05_GuardarTraduccion`), T06 (`CLASES_T06a_bitacora`,
`CLASES_T06b_control_cambios`, `DER_T06_bitacora`, `DSS_T06a_ConsultarBitacora`, `DSS_T06a_RegistrarBitacora`,
`DSS_T06b_RestaurarVersion`), T07 (`CLASES_T07_digitos_verificadores`, `DER_T07_dv`, `DSS_T07_VerificarIntegridad`,
`DSS_T07_RecuperarIntegridad`), T08 (`CLASES_T08_backup`, `DER_T08_backup`, `DSS_T08_RealizarBackup`,
`DSS_T08_RestaurarBackup`).

## Prioridad 4 — PN04 Inspección de devolución (postergado)

`ACT_pn04_devolucion`, `CU_pn04_devolucion`, `MC_pn04_devolucion`, `DER_pn04_devolucion`, `CLASES_pn04_devolucion`,
`DSS_PN04_CU01_DEP_InspeccionarDevolucion`, `DSS_PN04_CU02_DEP_ReportarPrendaPerdida`. Esperar a que se rehaga PN04.

---

## Resumen

| Prioridad | Alcance | Diagramas | En EA hoy |
|---|---|---|---|
| 1 | PN01 | 16 | 1 al día, 3 desactualizados |
| 1 | PN02 | 10 | 0 |
| 2 | N01, PN03 y globales | 32 | 0 |
| 3 | Seguridad T02–T08 | 29 | 0 |
| 4 | PN04 (postergado) | 7 | 0 |

## Cómo se arma cada diagrama sin inventar

1. Correr `node docs/diagramas/fuente/verificar.js`: tiene que decir "coinciden con el código y con la base".
2. Abrir el `.drawio` o `.mmd` de la fuente y copiar a EA exactamente los mismos elementos y nombres.
3. Para clases y DSS, confirmar cada método y parámetro buscándolo en el `.cs` citado (`grep` del nombre).
4. Si algo no está en el código, no se dibuja: se agrega a "Decisiones pendientes".
