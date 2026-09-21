# Diagramas de WardrobeFlow

Todos los diagramas (DER, modelos conceptuales, clases, casos de uso, actividad y secuencia) se generan desde **una única fuente**, en dos formatos equivalentes:

| Carpeta | Formato | Cómo abrirlo |
|---|---|---|
| `mermaid/` | Código Mermaid (`.mmd`) | https://mermaid.live, extensión de VS Code o `mmdc` |
| `drawio/` | draw.io / diagrams.net (`.drawio`) | https://app.diagrams.net (Archivo > Abrir) o la app de escritorio |
| `png/` | Imágenes de alta resolución (no se versionan; se regeneran) | — |

Como ambos formatos salen del mismo modelo, **coinciden entre sí**. Y como el modelo sale del código y de la base, **coinciden con el sistema**:

* **DER y modelos conceptuales**: `fuente/schema.json`, extraído de la base instalada (`sys.tables`, `sys.columns`, `sys.foreign_keys`).
* **Diagramas de clases**: se leen de los `.cs` reales (clases, propiedades, métodos públicos, herencia y dependencias del constructor). `fuente/modelos/clases.js` solo elige qué clases entran en cada diagrama.
* **Casos de uso, actividad y secuencia**: `fuente/modelos/*.js`. Cada mensaje de una secuencia es un método real de la pantalla, la BLL o la DAL (verificados leyendo el código).

## Regenerar

```
cd docs/diagramas/fuente
node generar.js            # regenera mermaid/ y drawio/
node generar.js DSS        # solo los que contienen "DSS" en el id
```

Para las imágenes (requiere Node y un navegador Chromium/Edge):

```
npm install @mermaid-js/mermaid-cli
npx mmdc -i mermaid/DSS_PN02_CU01_CAJ_GestionarCobro.mmd -o png/DSS_PN02_CU01_CAJ_GestionarCobro.png -c fuente/mm-config.json -w 2400 -s 2 -b white
```

Si cambia el esquema de la base, volver a extraer `fuente/schema.json` (tablas, columnas, claves primarias y foráneas) desde una base instalada.

## Contenido del documento de la tesis

`fuente/tesis/contenido.js` tiene los textos de la sección N00 (procesos de negocio N01 y PN01 a PN04: roles, descripción, reglas, casos de uso) y de las secciones G01 a G08 que se corrigieron, ya contrastados con el código.

## Diagramas

### DER y modelos conceptuales (MC_) (17)

| Archivo | Contenido |
|---|---|
| `DER_global` | DER global — WardrobeFlow |
| `DER_seguridad` | DER — Seguridad, usuarios, permisos, idiomas y auditoría |
| `DER_n01_clientes_suscripciones` | DER — N01 Clientes y suscripciones |
| `DER_pn01_pedidos` | DER — PN01 Armar pedido |
| `DER_pn02_contrataciones` | DER — PN02 Comercialización de la suscripción |
| `DER_pn03_promociones` | DER — PN03 Métricas, promociones y toma de decisiones |
| `DER_pn04_devolucion` | DER — PN04 Inspección de devolución |
| `MC_n01_clientes_suscripciones` | Modelo conceptual — N01 Clientes y suscripciones |
| `MC_pn01_pedidos` | Modelo conceptual — PN01 Armar pedido |
| `MC_pn02_contrataciones` | Modelo conceptual — PN02 Comercialización de la suscripción |
| `MC_pn03_promociones` | Modelo conceptual — PN03 Métricas, promociones y toma de decisiones |
| `MC_pn04_devolucion` | Modelo conceptual — PN04 Inspección de devolución |
| `DER_seg_usuarios` | DER — Módulo de usuarios: cuentas, empleados, preferencias y recuperación |
| `DER_seg_permisos_idiomas` | DER — Módulo de permisos (Composite) e idiomas |
| `DER_seg_auditoria` | DER — Módulo de auditoría e integridad |
| `DER_negocio` | DER — Módulos de negocio: clientes, suscripciones, pedidos, prendas y promociones |
| `MC_negocio` | Modelo conceptual — Módulos de negocio |

### Diagramas de clases (13)

| Archivo | Contenido |
|---|---|
| `CLASES_global_dominio_a` | Diagrama de clases — Dominio: clientes, suscripciones y promociones |
| `CLASES_global_dominio_b` | Diagrama de clases — Dominio: pedidos y prendas |
| `CLASES_n01_clientes_suscripciones` | Diagrama de clases — N01 Clientes y suscripciones |
| `CLASES_patron_builder_suscripcion` | Patrón Builder — Activación de suscripción por modalidad de cobro |
| `CLASES_patron_chain_renovacion` | Patrón Chain of Responsibility — Renovación de suscripción |
| `CLASES_patron_chain_cobro` | Patrón Chain of Responsibility — Cobro recurrente de la suscripción |
| `CLASES_pn01_pedidos` | Diagrama de clases — PN01 Armar pedido |
| `CLASES_patron_command_pedido` | Patrón Command — Cancelar pedido y registrar devolución |
| `CLASES_pn02_contrataciones` | Diagrama de clases — PN02 Comercialización de la suscripción |
| `CLASES_pn03_promociones` | Diagrama de clases — PN03 Métricas, promociones y toma de decisiones |
| `CLASES_patron_strategy_abandono` | Patrón Strategy — Análisis de abandono (criterio de riesgo intercambiable) |
| `CLASES_pn04_devolucion` | Diagrama de clases — PN04 Inspección de devolución |
| `CLASES_patron_state_prenda` | Patrón State — Ciclo de vida de una prenda |

### Casos de uso (5)

| Archivo | Contenido |
|---|---|
| `CU_n01_clientes_suscripciones` | Casos de uso — N01 Clientes y suscripciones |
| `CU_pn01_armar_pedido` | Casos de uso — PN01 Armar pedido |
| `CU_pn02_comercializacion` | Casos de uso — PN02 Comercialización de la suscripción |
| `CU_pn03_promociones` | Casos de uso — PN03 Métricas, promociones y toma de decisiones |
| `CU_pn04_devolucion` | Casos de uso — PN04 Inspección de devolución |

### Diagramas de actividad (5)

| Archivo | Contenido |
|---|---|
| `ACT_n01_renovacion_cobro` | Actividad — N01 Renovación y cobro de la suscripción |
| `ACT_pn01_armar_pedido` | Actividad — PN01 Armar pedido |
| `ACT_pn02_comercializacion` | Actividad — PN02 Comercialización de la suscripción |
| `ACT_pn03_promociones` | Actividad — PN03 Métricas, promociones y toma de decisiones |
| `ACT_pn04_devolucion` | Actividad — PN04 Inspección de devolución |

### Diagramas de secuencia del sistema (DSS) (23)

| Archivo | Contenido |
|---|---|
| `DSS_N01_CU01_GestionarCliente` | N01 · CU01-VEN Gestionar Cliente (alta) |
| `DSS_N01_CU02_RenovarSuscripcion` | N01 · CU02-VEN Renovar suscripción (Chain of Responsibility) |
| `DSS_N01_CU03_CobrarSuscripcion` | N01 · CU03-VEN Cobrar suscripción (cobro recurrente) |
| `DSS_PN01_CU01_ArmarPedido` | PN01 · CU01-VEN Armar pedido |
| `DSS_PN01_CU02_ConsultarCatalogo` | PN01 · CU02-VEN Consultar catálogo |
| `DSS_PN01_CU03_ConsultarSituacionCliente` | PN01 · CU03-VEN Consultar situación del cliente |
| `DSS_PN01_CU04_DespacharPedido` | PN01 · CU01-DEP Despachar pedido |
| `DSS_PN01_CU05_RegistrarEntrega` | PN01 · CU02-DEP Registrar entrega |
| `DSS_PN01_CU06_RegistrarDevolucion` | PN01 · CU03-DEP Registrar devolución (desbloquea la cuenta) |
| `DSS_PN01_CU07_CancelarPedido` | PN01 · CU04-VEN Cancelar pedido (patrón Command) |
| `DSS_PN02_CU01_VTA_GestionarSuscripcion` | PN02 · CU01-VTA Gestionar suscripción (contratación) |
| `DSS_PN02_CU01_CAJ_GestionarCobro` | PN02 · CU01-CAJ Gestionar cobro (incluye CU02-CAJ Emitir comprobante) |
| `DSS_PN02_CU02_CAJ_EmitirComprobante` | PN02 · CU02-CAJ Emitir comprobante |
| `DSS_PN02_CU03_CAJ_CancelarContratacion` | PN02 · CU03-CAJ Cancelar contratación (intentos fallidos) |
| `DSS_PN03_CU01_GER_SugerirPromocion` | PN03 · CU01-GER Sugerir promoción |
| `DSS_PN03_CU01_ADM_GestionarPromociones` | PN03 · CU01-ADM Gestionar promociones (alta desde sugerencia) |
| `DSS_PN03_CU01_CONT_AnalizarPromocion` | PN03 · CU01-CONT Analizar promoción |
| `DSS_PN03_CU01_VEN_SugerirBaja` | PN03 · CU01-VEN Sugerir baja de promoción |
| `DSS_PN03_CU02_ADM_ResolverBaja` | PN03 · CU02-ADM Resolver baja de promoción |
| `DSS_PN04_CU01_DEP_InspeccionarDevolucion` | PN04 · CU-DEP-01 Inspeccionar devolución |
| `DSS_PN04_CU02_DEP_ReportarPrendaPerdida` | PN04 · CU-DEP-02 Reportar prenda perdida |
| `DSS_N01_CU04_GestionarPlanes` | N01 · CU04-VEN Gestionar planes de suscripción |
| `DSS_PN03_CU02_GER_ConsultarAnalitica` | PN03 · CU02-GER Consultar analítica de negocio |

