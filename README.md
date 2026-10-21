# WardrobeFlow

Sistema de escritorio MDI para la gestión de suscripciones de indumentaria.
Desarrollado en C# / .NET Framework 4.7.2 / Windows Forms / SQL Server.

**Materia:** Trabajo de Diploma — UAI 2026
**Autora:** Valentina Morana

---

## Descripción

WardrobeFlow permite a una empresa de alquiler de ropa administrar clientes, prendas, planes de suscripción, pedidos de venta y la renovación periódica de suscripciones. El acceso está restringido a empleados internos con roles diferenciados, cada uno con visibilidad y operaciones acotadas a su función.

Este repositorio parte de la base construida durante la materia Ingeniería de Software y la evoluciona para el Trabajo de Diploma, incorporando los procesos de negocio y patrones de diseño del **Bloque 1 — Operativos Core: Clientes (Fidelización de clientes)**.

---

## Stack tecnológico

| Componente | Tecnología |
|-----------|-----------|
| Lenguaje | C# (.NET Framework 4.7.2) |
| UI | Windows Forms (MDI) |
| Base de datos | SQL Server |
| Acceso a datos | ADO.NET puro (sin ORM) |
| Encriptado | PBKDF2-SHA256 (contraseñas) · AES-128 + HMAC-SHA256 con clave PBKDF2 (backups manuales .wfbak, encrypt-then-MAC; los automáticos `auto_*.bak` van sin cifrar) |

---

## Arquitectura en capas

```
GUI (WinForms MDI)
 └── BLL (lógica de negocio)
      ├── DAL (ADO.NET → SQL Server)
      ├── BE  (entidades de dominio + DTOs + patrones estructurales/creacionales/de comportamiento)
      ├── Servicios (bitácora · multiidioma · generador de credenciales)
      └── Seguridad (sesión · encriptado · dígitos verificadores)
```

La GUI nunca accede a DAL ni a Seguridad directamente. Toda la lógica de negocio y las validaciones viven en BLL. Los formularios solo capturan eventos, invocan BLL y muestran resultados.

---

## Estructura del repositorio

```
BE/ BLL/ DAL/ GUI/ Seguridad/ Servicios/   Capas de la aplicación (ver arriba)
Tests/                                    Tests unitarios (MSTest) con fakes de DAL
BD/00_Instalacion_Completa.sql            Único script: esquema, datos semilla y datos de prueba
BD/01_Esquema_Final_DDL.sql               DDL limpio del esquema final (documentación; no lo corre el instalador)
Instalador/                               Script de Inno Setup, DbInstaller (cliente SQL embebido)
                                          y credenciales iniciales. El .exe se genera en
                                          Instalador/Salida/ y no se versiona: el instalador
                                          firmado se distribuye como GitHub Release
WardrobeFlow.slnx                         Solución de Visual Studio
```

Los diagramas (casos de uso, clases, secuencia, actividad y DER) están en el modelo de Enterprise Architect (`.eapx`), que se mantiene fuera del repositorio (`*.eapx` está en `.gitignore`).

---

## Procesos de negocio — Bloque 1: Operativos Core (Clientes)

| PdN | Proceso | Patrón aplicado |
|-----|---------|------------------|
| PdN1 | Activación de suscripción (alta de cliente con modalidad de cobro) | **Builder** |
| PdN2 | Gestión de estados de prenda (Disponible · EnUso · EnLimpieza · Baja) | **State** |
| PdN3 | Gestión de pedidos (cancelación, devolución) | **Command** |
| PdN4 | Transición de estado de prenda asociada a un pedido | **State** |
| PdN5 | Renovación de suscripción (verificar vencimiento → renovar / cambiar plan / dar de baja) | **Chain of Responsibility** |
| PdN6 | Cobro y pago de suscripción (detectar cobro → procesar pago / aplicar gracia → suspender pedidos) | **Chain of Responsibility** |

---

## Procesos de negocio nuevos (PN01-PN04)

| PN | Proceso | Rol(es) protagonista(s) | Detalle |
|----|---------|--------------------------|---------|
| PN01 | Armar pedido de prendas | Vendedor / Deposito | `BLL.Pedido.ValidarPuedeArmarPedido` valida suscripción vigente, sin pausa ni suspensión, sin pedido despachado sin entregar y **cuenta desbloqueada** (sin prendas pendientes de devolución, como NUULY 4.2/4.6). El asistente lo avisa al elegir el cliente. La disponibilidad se relee por lote (`BLL.Prenda.VerificarDisponibilidad`) y la reserva es atómica (`UPDATE ... AND Estado = Disponible`) |
| PN02 | Comercialización de la Suscripción | Vendedor (crea la contratación) / **Caja** (cobra y formaliza) | Rol `Caja` separado de Vendedor (quien vende no cobra): quien registró la contratación no puede cobrarla, registrar un intento fallido ni anularla, salvo el Administrador. El cobro es un *claim* atómico (`UPDATE ... AND Estado = Pendiente`): dos sesiones de Caja no pueden cobrar la misma contratación, y si la activación falla se compensa reabriéndola en una sola transacción (`ReabrirPago`). Rechaza planes dados de baja y muestra el importe con el descuento y el comprobante emitido |
| PN03 | **Inteligencia comercial y promociones**: analizar → recomendar/decidir → simular → aplicar → medir | GerenteComercial (analiza, decide y sugiere en el **Centro de decisiones comerciales**) / **AdministracionComercial** (crea y reformula) / **Contabilidad** (aprueba) | Circuito **reporte → sugerencia → Administración → Contabilidad → Vigente → se aplica al cobro**. Gerencia puede cargar ideas detectadas por el reporte de abandono por plan (`BLL.AnalisisPromociones`; la rotación por categoría queda como dato). Regla de NUULY (5.1): **un solo descuento por ciclo**, el mayor entre la promoción vigente del plan y el crédito por referido (`BE.PoliticaDescuento`); el crédito no usado se acumula. Las promociones son siempre por plan; Ventas pide la baja y Administración la atiende o la da de baja. La sugerencia lleva el ingreso en riesgo y una **tasa de retención esperada** (30 % por defecto): el beneficio estimado es ingreso × tasa. El Reporte de métricas suma **"Resultado de las promociones"**: bajas, cambios de plan, pausas y renovaciones durante la vigencia contra el período anterior, y la retención lograda contra la esperada. El **Centro de decisiones comerciales** (ver abajo) suma KPI con variación, gráficos con proyección, un motor de reglas que recomienda qué hacer (aceptar ejecuta siempre una acción / descartar / posponer, con historial y resultado real), metas mensuales con semáforo que suben la prioridad de lo que las mueve, avisos al iniciar sesión, un simulador ¿qué pasa si…? y el veredicto de cada promoción |
| PN04 | Inspeccionar prendas devueltas | Deposito ("Depósito") | Sin rol nuevo, sin aprobador: reingresa sin cargo o se da de baja registrando **antes** el cargo por el daño irreparable (decisión propia, a diferencia de NUULY que no cobra daños). Una prenda devuelta solo sale de En Limpieza por la Inspección: reingreso (`ReingresarInspeccionada`) o baja con cargo; la BLL lo exige. El cambio de estado y el cierre del mantenimiento van en la misma transacción. Al registrarse la devolución las prendas dejan de estar En Uso y la cuenta del cliente se desbloquea (PN01) |

### PN03 — Inteligencia comercial y promociones (Centro de decisiones comerciales)

**Promociones → Centro de decisiones comerciales** (`GUI.CentroDecisionesForm`, rol GerenteComercial; lo gobierna la patente de Gerencia `mnuSugerenciaPromocion`, sin patente nueva). Recorre el ciclo completo de PN03: **analizar → recomendar/decidir → simular → aplicar → medir**. Toda la lógica vive en `BLL.Inteligencia` (pura y probada con dobles, sin SQL Server); la pantalla solo dibuja, confirma, llama a las operaciones existentes y corre las lecturas en segundo plano (`Task.Run` + cursor de espera).

| Pestaña | Qué hace | Clases |
|---------|----------|--------|
| **1. Analizar** | 8 KPI con su variación contra el período anterior de igual duración (▲/▼ en verde o rojo según convenga) y una interpretación de una línea; filtros de período (desde/hasta y atajos: 30/90/180 días, este mes, mes anterior) y de plan. Gráficos (MSChart): tendencia de 6 meses de altas, bajas e ingresos con **3 meses proyectados por regresión lineal** (punteado, con R²), ingreso en riesgo por plan, stock por categoría y estado (barras apiladas) y rotación (más y menos pedidas). Sin datos, cada gráfico muestra un mensaje en vez de quedar vacío | `InteligenciaComercial.Analizar`, `CalculoKpi`, `ProyeccionSerie` |
| **1b. Cohortes** | Retención por **mes de alta**: mapa de calor (cohortes × meses desde el alta, "85 %", columna de clientes, leyenda de colores, `*` = mes en curso), curva de retención promedio y la de la **peor cohorte** (MSChart), con la explicación de cómo leerlo. Usa el filtro de plan del Centro. **Actuar sobre la peor cohorte** (botón o doble clic en su fila) lleva a *Recomendaciones* y selecciona la de bajas del plan que más explica esa cohorte (`ChurnPlan:<IdPlan>`); si el motor no la propuso, dice por qué (promoción vigente, % de bajas bajo el umbral, plan fuera del filtro) y ofrece abrir el *Simulador* con ese plan | `AnalisisCohortes`, `GUI.Inteligencia.PanelCohortes` |
| **2. Recomendaciones** | El motor de reglas propone qué hacer: prioridad, **qué está pasando** (con los números), **por qué**, **qué hacer**, **impacto estimado con su fórmula**, confianza y **alternativas con su propio impacto**. Cada una se puede **Aceptar y ejecutar** (toda opción **hace algo** con una operación existente y la decisión queda Ejecutada; ver la tabla de reglas), **Descartar** (motivo obligatorio) o **Posponer** N días. Si una meta del mes está en riesgo, el detalle explica "Prioridad elevada: la meta de X está en riesgo" | `MotorRecomendaciones`, `Reglas/*`, `DecisionesComerciales`, `AccionesRecomendacion` |
| **3. Historial de decisiones** | Las decisiones tomadas (tabla `DecisionComercial`) con filtros; para las aceptadas compara el **impacto estimado contra el real**: el sistema lo mide cuando la decisión terminó en una promoción (vía `AnalisisPromociones`) o fue una **campaña de contacto** (cuántos de los contactados renovaron o cambiaron de plan en los 30 días desde la ejecución, por encima de la tasa de rutina, `MedicionCampanias`). Mientras la ventana está abierta la campaña **no muestra importe**: el estado es *En medición* con el avance y el cierre (p. ej. «En medición: 2 de 6 respondieron (cierra el 20/11/2026)») en la grilla, el detalle, el CSV y el Informe ejecutivo, y no entra en los totales del historial (que lo aclaran); el día del cierre se muestra el resultado, aunque sea negativo. Gerencia puede registrarlo a mano (el valor propuesto es solo un resultado final) | `DecisionesComerciales`, `InteligenciaComercial.MedirDecisiones` |
| **4. Simulador** | "¿Qué pasa si…?": plan, descuento, duración, retención esperada, clientes objetivo y horizonte; calcula en vivo costo, ingreso retenido, beneficio neto, ROI y la **retención de equilibrio** ("conviene si retiene al menos X %"), con veredicto en color (Conviene / Riesgoso / No conviene), curva de sensibilidad y hasta 3 escenarios lado a lado. "Crear sugerencia con este escenario" entra al circuito aprobado de PN03 | `SimuladorPromocion` |
| **5. Medir resultado** | El «Resultado de la promoción» en tabla y gráfico (antes vs durante: bajas, pausas, cambios de plan, renovaciones), retención esperada vs real, ROI real vs esperado y **veredicto automático** (Mantener / Extender / Dar de baja); el botón ejecuta Extender o Dar de baja (Mantener no tiene nada que ejecutar) | `MedicionPromociones` |
| **Metas** | Gerencia fija el **objetivo del mes** por indicador (MRR a alcanzar, % de bajas máximo, ocupación del stock); se ve el real, el avance, la **proyección al cierre**, el **semáforo** (cumplida / en camino / en riesgo / no cumplida) y el gráfico real contra meta de 6 meses. Una meta en riesgo **sube un nivel la prioridad** de las recomendaciones que la mueven y se avisa al iniciar sesión. Control propio (`PanelMetasComerciales`) | `MetasComerciales`, `CatalogoIndicadores` |

**Análisis de cohortes** (`BLL.Inteligencia.AnalisisCohortes`, puro y probado con datos armados a mano). Una **cohorte** son los clientes cuya primera suscripción (primer pago de una contratación, o el alta del cliente si es anterior a PN02) cae en el mismo mes; se muestran los 12 meses de alta que terminan en el fin del período analizado (como máximo hoy). Un cliente está **activo en el mes k** (k = 0 es el mes del alta) si no tiene una **baja** registrada (`HistorialRenovacion`, Resultado = Baja) antes del cierre de ese mes: igual que en el % de bajas, las pausas, las suscripciones vencidas y las suspensiones por falta de pago **no** son bajas, y la primera baja corta la permanencia. Con filtro de plan, la cohorte son los que se suscribieron a ese plan. Un mes que todavía no terminó es **parcial**: se calcula con las bajas registradas hasta el corte y se muestra con `*`, pero no entra en la curva promedio (Σ activos / Σ clientes de las cohortes con ese mes completo) ni en la comparación; si ninguna cohorte completó un mes todavía, el gráfico lo dice y el informe no lleva la curva. Los meses se muestran como `2026-05` (la cultura de la app es fija, es-AR, y un nombre de mes saldría en español en EN, PT o RU; la tendencia y la pestaña Metas usan el mismo formato). El % se redondea a entero para mostrarlo y el color de la celda sale de ese mismo valor. **Peor cohorte**: entre las de **4 o más clientes** (menos es ruido), la de menor retención en el **mes 3** o, si todavía no hay datos, en el último mes que completaron al menos 2 de ellas (se compara la proporción exacta, no el % redondeado); empate → la de más clientes y después la más antigua. El **plan que la explica** es el que dejaron más clientes de esa cohorte hasta ese mes: el plan anterior de la baja (`IdPlanAnterior`), el mismo dato con que el % de bajas y `ReglaChurnPlan` cuentan las bajas de cada plan; una baja sin plan anterior baja la retención pero no se atribuye a ningún plan. Empate → el de mayor proporción de bajas sobre sus altas en la cohorte. Si el motor no propuso la recomendación de ese plan, la pantalla dice por qué: plan fuera del filtro, la regla de bajas falló en el análisis, promoción vigente, el plan no tenía clientes al inicio del período o su % de bajas no llega al umbral. **Limitación conocida:** la fecha de alta sale de `DAL.InteligenciaComercial.ObtenerAltas` (la misma que usan las altas, el % de bajas y el MRR del Centro): el primer pago de una contratación y, solo si no hay ninguna, la fecha de alta del cliente. Un cliente cargado antes de PN02 que después pagó una contratación (por ejemplo, un upgrade) cae en la cohorte de ese pago y no en la de su alta original; no se cambió porque movería también los KPI del período.

**Informe ejecutivo (PDF)**: portada con filtros y fecha, resumen de KPI, los gráficos como imágenes (MSChart → PNG → iTextSharp), retención por cohorte (tabla de los meses 0 a 5, la peor cohorte y su curva), recomendaciones con impacto y fórmula, decisiones tomadas, escenarios simulados, medición de promociones, **metas del mes** (tabla, explicación y gráfico real contra meta) y glosario. Recomendaciones y decisiones también se exportan a CSV. `ExportadorPdf` suma un modo *compuesto* (`ReporteExportable.Secciones`) sin cambiar los reportes de siempre; el creador es `GeneradorInformeComercial` (Factory Method).

**Motor de recomendaciones (patrón Strategy, 7 reglas).** Cada regla es una clase que implementa `IReglaRecomendacion` y evalúa el contexto (`BE.ContextoComercial`); el motor las recorre todas, las ordena por prioridad e impacto, y si una falla no corta a las demás (la pantalla avisa). Cuando una regla tiene varias opciones, **gana la de mayor impacto** y las otras quedan como alternativas. **Toda opción ejecuta algo al aceptarla** (no hay opciones que solo registren la decisión ni que solo abran una pantalla). **Cada usuario ve solo las opciones que puede ejecutar** (`AccionesRecomendacion.FiltrarPorPermiso`, en la BLL, antes de las metas y los avisos): si la principal pide una patente que no tiene, pasa a principal la mejor de las que quedan, y una recomendación sin ninguna opción ejecutable no se le muestra, no cuenta en sus avisos ni sube por las metas. Por ejemplo, dar de baja o mandar a revisión prendas pide `mnuStockEditar`: con `gcomercial` (GerenteComercial) Baja rotación y Mantenimiento no aparecen; con el Administrador, sí. Si toca prendas, se confirma la lista; cada prenda se cambia sola y, pase lo que pase, la decisión queda Ejecutada con las que sí cambiaron (detalle y estado en una sola transacción) y las que no, anotadas con su motivo. Una acción que ya no existe se descarta con un mensaje claro.

| Regla | Se dispara cuando… | Al aceptar (qué hace) | Impacto estimado | Alternativas |
|-------|--------------------|--------------------------|------------------|--------------|
| **Bajas por plan** (`ReglaChurnPlan`) | % de bajas del plan ≥ `ChurnAltoPct` o subió `SubidaChurnPp` pp, y el plan no tiene una promoción vigente | Promoción de retención con Y % durante Z días (abre **Sugerir promoción** precargada → Administración → Contabilidad) | objetivo × precio × retención × horizonte − clientes del plan × precio × Y % × meses. Y = descuento base × gravedad (múltiplo de 5, hasta el máximo); Z = 60 días si las bajas duplican el umbral, si no 30; retención = mejora histórica de las promociones medidas del plan o la de fábrica | Promoción más suave (−5 %, 75 % de la retención); contacto personal sin descuento (un tercio de la retención, sin costo) |
| **Renovaciones en juego** (`ReglaRenovaciones`) | Suscripciones vencidas (hasta 30 días) o que vencen en ≤ `DiasPorVencer` días | **Campaña de renovación**: exporta los contactos (CSV), la decisión queda Ejecutada y guarda a quiénes se contactó; el Historial mide después el resultado **incremental** a los 30 días de la ejecución: (renovaron − tasa base × comparables) × precio del plan, donde los comparables son los contactados que resolvieron una renovación en la ventana y la tasa base es, en los 30 días anteriores, la parte de los clientes de esos planes que renovó o cambió de plan entre los que resolvieron una renovación (ver «Resultado de una campaña de contacto») | Σ precio en juego × `TasaRecuperoCobroPct` | — (la campaña por email era la misma acción) |
| **Escasez con demanda** (`ReglaEscasez`) | Categoría y talle con menos disponibles que `EscasezMinimo` **y** demanda (lista de espera o ≥ `DemandaEscasez` pedidos) | **Orden de compra sugerida (PDF)** | clientes en espera × ticket promedio − Σ cantidad × costo de reposición | Reponer solo lo que pide la lista de espera |
| **Baja rotación** (`ReglaBajaRotacion`) | ≥ `MinPrendasLiquidar` prendas disponibles sin pedidos en el período y con ≥ `DiasSinRotacion` días en el catálogo | **Da de baja las prendas** (Disponible → Baja con `BLL.Prenda.CambiarEstado`: confirmación con la lista, patente `mnuStockEditar`, bitácora por prenda; las que están en uso se omiten) | capital inmovilizado × `RecuperoLiquidacionPct` | — |
| **Mantenimiento excesivo** (`ReglaMantenimiento`) | Prendas que el Análisis de Mantenimiento (PdN11) marca como excesivas | **Da de baja las prendas** (como la anterior) o **las manda a revisión** (Disponible → En limpieza: la transición manual de PN04, abre su mantenimiento) | días fuera de servicio × valor de un día de prenda − costo de reposición (revisar: 0) | La otra opción (el motor elige la de mayor impacto) |
| **Rendimiento de promociones** (`ReglaRendimientoPromocion`) | Promoción vigente con veredicto Dar de baja o Extender en "Medir resultado" | **Solicitud de baja** (camino existente) o **extensión** (nueva sugerencia para el plan) | baja: descuento diario × 30 que se deja de dar; extender: beneficio real mensual | — (esperar no es una acción; para eso están Descartar y Posponer) |
| **Upgrade de plan** (`ReglaUpgradePlan`) | Clientes que usan todo su cupo y existe un plan con más prendas | Ofrecer el plan superior: exporta los contactos y después se mide cuántos pasaron a un plan más caro (Σ diferencia × horizonte) | clientes × `ConversionUpgradePct` × diferencia de precio × horizonte | Primer mes al precio actual (conversión × 1,5, un mes menos) |

**Reglas retiradas** (solo registraban la decisión; el negocio no tiene una operación que las ejecute): **Desempeño de vendedores** (`ReglaVendedores`: "redistribuir la cartera" o "capacitar" — no existe reasignación de clientes a vendedores) y **Ocupación del stock** (`ReglaOcupacionStock`: "frenar compras" / "priorizar la reposición" — la reposición ya la cubre Escasez con su orden de compra; la ocupación ahora se gestiona con una **meta**). Se quitaron sus clases, umbrales (`ConcentracionVendedorPct`, `BajoPromedioVendedorPct`, `OcupacionMinimaPct`, `OcupacionMaximaPct`, borrados de `ParametroSistema` por la sección 20p), textos (salvo el nombre de la regla, `cdc.regla.vendedores` / `cdc.regla.ocupacionstock`, que queda marcado "(retirada)" para que el historial siga legible), pruebas y la decisión de demo (re-apuntada a Baja rotación). También se quitaron las alternativas que no hacían nada (destacar prendas, esperar el vencimiento, dejarla vencer, contacto sin contactos) y el veredicto "Mantener" ya no registra una decisión.

**Metas comerciales** (`BE.MetaComercial`, tabla `MetaComercial`, sección **20p**; sin dígito verificador, como `DecisionComercial`): Id, Periodo (primer día del mes), Indicador (`BE.IndicadorMeta`: MRR, % de bajas, ocupación; el catálogo `CatalogoIndicadores` define nombre, unidad, sentido, rango, si es acumulativo, si tiene historia y qué reglas lo mueven, así que sumar un indicador es un valor del enum + una entrada), Valor objetivo, Sentido (mayor/menor es mejor), usuario y fecha. Reglas: **una meta por indicador y mes** (guardar dos veces modifica; `UQ_MetaComercial_PeriodoIndicador`), objetivo dentro del rango del indicador, se crean/modifican/eliminan **solo para el mes en curso y hasta 12 meses adelante**, las edita Gerencia con la patente del Centro (`mnuSugerenciaPromocion`, la misma de los umbrales) y quedan en la bitácora. Semáforo y proyección: ver el glosario. Movimiento: `MetasComerciales.ElevarPrioridad` sube un nivel (Baja → Media → Alta) las recomendaciones con impacto positivo de las reglas que mueven una meta del mes en riesgo (bajas en riesgo → bajas por plan, renovaciones y rendimiento de promociones; MRR → además upgrade; ocupación → baja rotación, upgrade y la baja de las de mantenimiento excesivo; la opción de mandarlas a revisión no sube porque no cambia la ocupación).

**Avisos al iniciar sesión** (`BLL.Inteligencia.AvisosComerciales`, `GUI.Inteligencia.AvisosInicioSesion`): después del login, para quien tiene el Centro de decisiones, se calcula en segundo plano (últimos 90 días, sin dejar el análisis en la bitácora) cuántas recomendaciones de prioridad Alta hay para decidir, cuántas decisiones pospuestas llegaron a su fecha (hoy o antes; ese día la recomendación vuelve a verse) y cuántas metas del mes están en riesgo. Cada situación se cuenta una vez (una alta que además es una pospuesta vencida cuenta como pospuesta) y no se avisan pospuestas de situaciones que el motor ya no recomienda a ese usuario (reglas retiradas, opciones sin permiso). El cálculo arranca al abrir el menú, fuera del hilo de la interfaz; el aviso se muestra con `Menu.DespuesDeLaGuia`, así en el primer ingreso aparece después de la guía de primer uso. Si hay algo, aparece un aviso **no modal** en la esquina del menú con "Ir al Centro de decisiones" (usa `Menu.AbrirUnico`) y "No mostrar hoy" (por usuario, en `ParametroSistema` `AvisoComercial.Oculto.<IdUsuario>`). Si no hay nada o el cálculo falla, no se muestra nada.

**Una sola decisión abierta por situación** (aceptada o pospuesta): lo valida la BLL y lo respalda el índice filtrado `UX_DecisionComercial_Abierta`. Una aceptada sin ejecutar cuya opción ya no existe (de una versión anterior) se cierra sola al aceptar otra. La decisión guarda su **fecha de ejecución** (`FechaEjecucion`): desde ahí rige y desde ahí se mide (una pospuesta 90 días y ejecutada hoy rige desde hoy).

**Decisiones** (`BE.DecisionComercial`, tabla `DecisionComercial`, sección **20o** del script): `Pendiente → Aceptada | Descartada | Pospuesta`, `Pospuesta → Aceptada | Descartada | Pospuesta`, `Aceptada → Ejecutada | Descartada`; Descartada y Ejecutada son finales. Cada cambio es un claim atómico (`UPDATE … WHERE Estado = @esperado`) y queda en la bitácora del sistema y en la de negocio (evento `DecisionComercial`). Una recomendación pospuesta se oculta hasta su fecha; una descartada, durante `DiasSilencioDescarte` días. Una decisión final deja de bloquear la recomendación cuando ya no rige (`DecisionesComerciales.DecisionVigente`/`PuedeDecidirse`): la descartada, pasado ese silencio; la ejecutada, si es anterior al inicio del período analizado (la recomendación se calculó con datos posteriores: es una situación nueva). Entonces la recomendación figura "Para decidir" y la decisión anterior queda como dato. **Simular esta opción** lleva una opción de promoción (plan, %, días, retención y clientes objetivo) al Simulador y la simula (`InteligenciaComercial.EsSimulable`/`EscenarioDesdeOpcion`); "Sugerir promoción" no guarda % ni duración (la sugerencia solo tiene plan, tipo de descuento, ingreso en riesgo y retención), así que esos valores viajan en el motivo. Al ejecutar, la decisión guarda qué generó (`Sugerencia`, `Promocion` o `SolicitudBaja` + ID): así, cuando la sugerencia termina en una promoción, el historial muestra su **beneficio real** al lado del estimado.

**Umbrales** (botón *Umbrales*, editables por Gerencia con su explicación; se guardan en `ParametroSistema` con el prefijo `Comercial.`; un valor ausente o fuera de rango usa el de fábrica):

| Umbral | De fábrica | Qué decide |
|--------|-----------|------------|
| `ChurnAltoPct` / `SubidaChurnPp` | 5 % / 2 pp | Cuándo las bajas de un plan piden una promoción de retención |
| `DescuentoBasePct` / `DescuentoMaximoPct` | 10 % / 30 % | Descuento de partida y tope del descuento propuesto |
| `TasaRetencionDefecto` / `HorizonteMeses` | 30 % / 6 meses | Retención supuesta sin historia y meses que paga un cliente retenido |
| `RoiMinimoPct` / `MargenSeguridadPp` | 20 % / 10 pp | Cuándo el simulador califica una promoción como riesgosa |
| `DiasPorVencer` / `TasaRecuperoCobroPct` | 7 días / 60 % | Renovaciones en juego y recupero esperado |
| `EscasezMinimo` / `DemandaEscasez` | 3 / 2 pedidos | Stock mínimo por categoría y talle, y demanda que justifica comprar |
| `DiasSinRotacion` / `MinPrendasLiquidar` / `RecuperoLiquidacionPct` | 60 días / 3 / 40 % | Baja rotación y recupero de una liquidación |
+

| `RendimientoMinimoPct` / `DiasMinimosEvaluacion` / `DiasAvisoVencimientoPromo` | 50 % / 14 días / 15 días | Veredicto de una promoción vigente (dar de baja / extender) |
+

| `ConversionUpgradePct` | 20 % | Conversión esperada de una oferta de upgrade |
| `DiasSilencioDescarte` | 30 días | Cuánto tiempo se oculta una recomendación descartada |

#### Glosario de métricas

También está en la ayuda F1 del Centro de decisiones, en el botón *Glosario* y al final del Informe ejecutivo.

| Métrica | Fórmula | Fuente | Umbral | Decisión que sugiere |
|---------|---------|--------|--------|----------------------|
| Ingreso recurrente mensual (MRR) | Σ precio mensual del plan de los clientes que **pagan hoy**: con plan y sin pausa vigente, sin la suscripción vencida y sin suspensión por falta de pago (mismos criterios que `BE.Cliente`: `EstaPausada`, `VencimientoExpirado`, `EstaSuspendidoPorPago`; `BE.ClienteComercial.PagaAl`). Al **fin del período** se reconstruye desde hoy: MRR de hoy − MRR de las altas + MRR de las bajas posteriores al período (si el período termina hoy, es el de hoy); al inicio: MRR al fin − MRR de las altas + MRR de las bajas del período | `Cliente`, `PlanSuscripcion`, `HistorialRenovacion`, `Contratacion` | — | Si cae, revisar bajas y renovaciones en juego |
| % de bajas (churn) | bajas del período / clientes al inicio × 100; clientes al fin del período = activos hoy − altas + bajas posteriores al período; clientes al inicio = clientes al fin − altas + bajas del período (por plan, igual en la regla de bajas) | `HistorialRenovacion` (Resultado = Baja) | 5 % o +2 pp | Promoción de retención en el plan |
| Retención por cohorte | clientes de la cohorte (mismo mes de alta) sin baja al cierre del mes k desde el alta / clientes de la cohorte × 100; pausas, vencimientos y suspensiones no son bajas; el mes en curso (`*`) no entra en el promedio | `Contratacion` (primer pago), `HistorialRenovacion` (Resultado = Baja) | peor cohorte: menor retención al mes 3 entre las de 4 o más clientes | Recomendación de bajas del plan que más explica la peor cohorte, o el Simulador |
| Clientes en riesgo | Clientes que el Análisis de Abandono (PdN10) marca hoy | `BLL.AnalisisAbandono` (Strategy) | — | Son el objetivo de la promoción |
| Ingreso en riesgo | Σ precio mensual del plan de los clientes en riesgo | ídem + `PlanSuscripcion` | — | Base del beneficio de una sugerencia |
| Descontado por promociones | Σ descuentos aplicados en los cobros del período | `Contratacion` + `HistorialCobro` | — | Costo real de las promociones |
| Ocupación del stock | prendas en uso / prendas activas × 100 | `Prenda` | meta mensual | Con la meta en riesgo suben dar de baja prendas sin rotación o con mantenimiento excesivo (menos activas) y el upgrade (más en uso); mandarlas a revisión no la cambia (siguen activas) |
| Ticket promedio | ingresos cobrados / cantidad de cobros del período | `Contratacion` + `HistorialCobro` | — | Valor de un cliente en las estimaciones |
| Ingresos cobrados | Σ importes cobrados en el período | `Contratacion` + `HistorialCobro` | — | Tendencia del negocio (se proyecta 3 meses) |
| Proyección | Regresión lineal por mínimos cuadrados sobre 6 meses: b = Σ(x−x̄)(y−ȳ)/Σ(x−x̄)², a = ȳ − b·x̄; R² = 1 − SSres/SStot (sin valores negativos). Cada mes cuenta hasta el fin del período; si el período termina antes de fin de mes (p. ej. el mes en curso), ese **mes parcial** se grafica con marcador hueco y asterisco pero **no entra en la regresión** (contarlo como mes completo tiraría la proyección para abajo); conserva su lugar en el eje, así que la proyección empieza el mes siguiente | serie mensual | R² bajo = orientativa | Anticipar caídas de ingresos o subas de bajas |
| Retención de equilibrio | costo del descuento / (clientes objetivo × precio × horizonte) × 100 | simulador | — | La promoción conviene si retiene al menos ese % |
| ROI | beneficio neto / costo del descuento × 100 | simulador / medición | 20 % | Debajo del mínimo, riesgosa |
| Mejora de retención | retención durante la vigencia − retención del período anterior (pp) | `BLL.AnalisisPromociones` | 50 % de lo esperado | Dar de baja / extender |
| Beneficio real de una promoción | clientes en juego × mejora × precio × horizonte − total descontado | `AnalisisPromociones` + cobros | — | Se compara con el impacto estimado (cierre del ciclo) |
| Resultado de una campaña de contacto | (respondieron − tasa base × comparables) × valor por respuesta, es decir (tasa de la campaña − tasa base) × comparables × valor, en la ventana fija de 30 días desde la **ejecución** (o desde la creación en decisiones anteriores a ese dato): de la ejecución al comienzo del día ejecución + 30, que es el día en que cierra. Mientras está abierta no hay importe (`BE.MedicionDecision`, estado `EnMedicion`: respondieron, contactados y fecha de cierre) y `MedicionDecisiones.Resumir` la deja fuera de las sumas; al cerrar, estado `Medido` con el importe (también negativo). **Población comparable** (la misma definición en las dos ventanas): los clientes que resolvieron una renovación en la ventana —renovaron, cambiaron de plan o se dieron de baja (`HistorialRenovacion`)—, que es la forma observable de haber tenido un vencimiento; un contactado sin movimiento no tiene resultado que comparar y no cuenta ni a favor ni en contra (antes la base salía de las renovaciones resueltas pero se restaba sobre **todos** los contactados, y una campaña igual a la rutina daba negativo). Comparables de la campaña: los contactados que resolvieron una renovación en su ventana. Responde: el primer movimiento resuelto es renovación o cambio de plan (upgrade: cualquier pase a un plan más caro en la ventana, aunque antes haya renovado). Tasa base: en los 30 días anteriores, de los clientes de los planes contactados que resolvieron una renovación (por cliente, con su primer movimiento resuelto), la parte que respondió con el mismo criterio (sin historia, 0). Ejemplo: 10 contactados, 6 renuevan y 4 no tienen movimiento; en la ventana anterior renovaron 9 de 10 → (6 − 0,9 × 6) × precio = +0,6 × precio (con la fórmula anterior daba −3 × precio); si esos 4 se hubieran dado de baja, (6 − 0,9 × 10) = −3: ahí sí quedó debajo de la rutina. Una campaña igual a la rutina da 0. Al cerrar la ventana el importe no cambia: solo cuentan movimientos dentro de ella. Valor por respuesta: precio **actual** del plan (no hay historial de precios); bajas por plan: × horizonte; upgrade: diferencia × horizonte. Cada cliente cuenta para la primera campaña que lo contactó, entre **todas** las campañas ejecutadas (la BLL las lee aparte: el filtro del Historial no cambia el resultado de una decisión) | `DecisionComercialDetalle` + `HistorialRenovacion` | — | Lo logrado por encima de las renovaciones de rutina; se compara con el impacto estimado |
| Avance de una meta | mayor es mejor: real / objetivo × 100; menor es mejor: real / máximo permitido × 100. El real se mide con las fórmulas de los KPI al cierre del mes (o a hoy en el mes en curso); la ocupación es una foto del día (solo el mes en curso) | `MetaComercial` + fuentes de los KPI | 100 % | — |
| Proyección al cierre del mes (metas) | valor de hoy + pendiente × días que faltan, con la pendiente de la regresión lineal por mínimos cuadrados (`ProyeccionSerie`, la misma de la tendencia) sobre el valor de cada día transcurrido; el último día es el valor real; desde el día 7; no negativa, un % no pasa de 100; la ocupación no se proyecta | serie diaria | — | Si no llega al objetivo, la meta está en riesgo |
| Semáforo de una meta | mes futuro: pendiente; mes cerrado: cumplida / no cumplida; mes en curso (con la proyección, o el valor de hoy si no hay): real y proyección cumplen → cumplida; la proyección cumple → en camino; si no → en riesgo; un máximo acumulativo (% de bajas) ya superado → no cumplida | `MetasComerciales.Semaforo` | — | En riesgo: sube la prioridad de lo que la mueve y se avisa al iniciar sesión |

Supuestos documentados: el costo del descuento se calcula como lo hace Contabilidad (lo reciben **todos** los clientes activos del plan); el clientes-al-inicio y el MRR anterior son reconstrucciones a partir de altas y bajas; los cambios de plan no cuentan como bajas.

---

## Bloque 3 — Idea de Negocio (analítica de decisión comercial)

| PdN | Reporte | Rol dueño | Patrón / detalle |
|-----|---------|-----------|-------------------|
| PdN8 | Reporte de Ventas por Vendedor | GerenteComercial | — |
| PdN9 | Análisis de Rotación de Prendas (candidatas a baja / reposición) | GerenteInventario | — |
| PdN10 | Análisis de Abandono (clientes en riesgo) | GerenteComercial | **Strategy** — 3 criterios de riesgo intercambiables |
| PdN11 | Análisis de Tiempos de Mantenimiento | GerenteInventario | — |
| PdN12 | Detección de Escasez por Talle/Categoría | GerenteInventario | — |
| PdN13 | Recomendación de Prendas para un Cliente | Vendedor | — |

Los 6 reportes son de solo lectura, con exportación a PDF/CSV (Factory Method, `GUI.Exportacion`). Están en **un solo formulario, "Analítica de Negocio"**, con un selector que muestra solo los reportes que permiten las patentes del usuario (`mnuAnalisisXxx`, una por reporte).

---

## Roles del sistema

| Rol | Permisos | Jerarquía (Composite) |
|-----|----------|-----------------------|
| **Administrador** | Acceso total | — (acceso total) |
| **Auditor** | Solo Bitácora / Auditoría | rol plano, transversal (no participa de ningún PN/PdN) |
| **Vendedor** | Prendas, Clientes, Planes, Renovación, Cobro, Realizar Ventas, Contratación (PN02), Recomendación de Prendas (PdN13) | rol base comercial |
| **GerenteComercial** | lo de Vendedor + Ver Pedidos Realizados, Ventas por Vendedor (PdN8), Análisis de Abandono (PdN10), Sugerir Promoción y Centro de decisiones comerciales (PN03) | ⊃ Vendedor |
| **OperadorLogistico** | Ver Pedidos Realizados (despacho) | rol base inventario |
| **Deposito** *(PN01, PN04)* | Ver Prendas + Gestionar Stock (mantenimiento), Inspección de Devolución (PN04) | rol base inventario |
| **GerenteInventario** | lo de ambos operadores + Rotación/Mantenimiento/Escasez (PdN9/11/12) | ⊃ OperadorLogistico + Deposito |
| **Caja** *(PN02)* | Contrataciones pendientes de pago: cobrar, registrar intento fallido | rol simple, sin herencia |
| **AdministracionComercial** *(PN03)* | Gestionar Promociones: crear desde sugerencia o manual, reformular, dar de baja, atender solicitudes de baja | rol simple, sin herencia |
| **Contabilidad** *(PN03)* | Revisión Contable de Promociones: aprobar o rechazar | rol simple, sin herencia |

Los permisos se resuelven recursivamente desde el árbol Composite (tabla `PermisoRelacion`) y se cargan en sesión al hacer login. Se gestionan desde **Administrar → Perfiles y Permisos**. Usuarios demo para los 3 roles nuevos: `caja`, `admcomercial`, `contable` (clave `usuario1!`, sección "Base de datos" abajo).

---

## Módulos

| Módulo | Descripción |
|--------|-------------|
| **Login / Logout** | Autenticación con bloqueo de login progresivo (1/5/15/60 min tras 3 intentos; un ingreso exitoso reinicia la escala), claves de emergencia de autodesbloqueo y bloqueo de sesión en memoria |
| **Administración de Usuarios** | Un solo formulario con dos pestañas. **Cuentas:** reset de contraseña, desbloqueo, archivado (baja lógica) y purga diferida. **Datos administrativos:** alta de usuarios (único lugar; contraseña generada con RNG criptográfico), edición de datos no sensibles, cambio de rol e Historial de Cambios |
| **Perfiles y Permisos** | Pestaña **Gestión de roles** (árbol Composite por rol, ABM de roles y asignación de permisos en tiempo real) y pestaña **Vista completa** (árbol de solo lectura de todo el sistema); mapeo de controles por patente |
| **Mi Perfil** | Preferencias de UI por usuario (idioma, tipografía, tamaño, tema, formato de fecha) |
| **Clientes** | ABM de suscriptores con plan, vencimiento, modalidad de cobro y columna `en uso / límite`; alta activa la suscripción vía **Builder** |
| **Prendas** | Inventario con estados (Disponible · EnUso · EnLimpieza · Baja) gestionados vía **State** |
| **Planes de Suscripción** | ABM de planes; bloquea desactivación con clientes asignados; bloquea asignación con límite menor al stock en uso |
| **Pedidos de Venta** | Creación de pedidos respetando límite del plan; cancelación y devolución vía **Command** |
| **Pedidos Realizados** | Ciclo post-venta: Despachar → Marcar Entregado → Registrar Devolución |
| **Renovación de Suscripción** | Verificación de vencimiento, renovación, cambio de plan o baja resueltos por una cadena de manejadores (**Chain of Responsibility**) |
| **Cobro de Suscripción** | Detección de cobro pendiente, confirmación de pago (extiende la vigencia), período de gracia ante un pago fallido y suspensión de nuevos pedidos si el plazo vence sin regularizar — cadena de manejadores (**Chain of Responsibility**) |
| **Bitácora** | Registro de eventos del sistema y de negocio con filtros, criticidad y exportación a PDF |
| **Historial de Cambios** | Cambios de datos administrativos por usuario a nivel de campo, con rollback (Memento); no restaura una versión cuyo nombre de usuario ya usa otro |
| **Serialización XML / JSON** *(A02)* | Estados de objetos: las versiones (Mementos) de un usuario se serializan a XML o JSON desde Historial de Cambios ("Exportar versiones…", sin clave ni estado de bloqueo) y se deserializan ("Importar versiones…", validando formato y que el usuario exista) para restaurarlo a una de ellas con el mismo flujo del rollback. Además, cada error inesperado queda serializado en `errores_AAAAMM.xml` (Bitácora → Errores (XML/JSON), que también exporta e importa en XML o JSON). Carpetas genéricas por usuario de Windows, creadas si no existen: `%LOCALAPPDATA%\WardrobeFlow\Versiones` y `\Errores`. Ver [Serialización](#serialización-a02) |
| **Idiomas** | ABM de traducciones directamente en la BD |
| **Dashboard** | Panel de control personalizado por rol, con auto-refresh y carga asíncrona |
| **Backup / Restauración** | Copias manuales cifradas con contraseña (`.wfbak`, AES + HMAC-SHA256, clave PBKDF2) con verificación de integridad previa, más backups automáticos sin cifrar al iniciar sesión y cada 24 h (ver [Backups automáticos](#backups-automáticos)); al arrancar se borran las copias sin cifrar que haya dejado un backup o una restauración interrumpidos |
| **Reporte de Jornada** | Exportación PDF de actividad del día filtrable por rol |
| **Diagnóstico de Integridad** | Visualización y reparación asistida de filas con DVH/DVV corruptos |
| **Lista de Espera** *(mejora opcional)* | Un cliente se anota por una prenda `EnUso`; al liberarse, queda reservada exclusivamente para él por 48hs (visible solo para ese cliente en Nuevo Pedido) antes de volver a estar disponible para cualquiera |
| **Nueva Contratación / Contrataciones Pendientes** *(PN02)* | Vendedor da de alta la contratación (plan + modalidad); Caja la cobra y formaliza la suscripción, o registra intentos fallidos hasta cancelarla |
| **Centro de decisiones comerciales** *(PN03)* | Analizar (KPI, tendencia con proyección, riesgo, stock, rotación) → recomendaciones con impacto y alternativas → aceptar/descartar/posponer → simulador → medir resultado; Informe ejecutivo en PDF y CSV. Rol GerenteComercial |
| **Promociones** *(PN03)* | Sugerir (GerenteComercial) → Gestionar/crear (AdministracionComercial) → Revisión Contable, aprobar o rechazar (Contabilidad) → consultar vigentes y sugerir baja (Vendedor) |
| **Inspección de Devolución** *(PN04)* | Cola de prendas `EnLimpieza` devueltas: reingresa sin cargo o se da de baja cobrando el precio de reposición. Desde Pedidos Realizados, Depósito reporta como perdida una prenda `EnUso` de un pedido entregado hace 30 días o más (compra tácita); sin la patente de stock el botón no se muestra |
| **Analítica de Negocio** *(Bloque 3, PdN8-13)* | Un formulario con 6 reportes de decisión comercial de solo lectura (Ventas por Vendedor, Rotación, Abandono, Mantenimiento, Escasez, Recomendación de Prendas), elegidos con un selector según las patentes, exportables a PDF/CSV |

### Serialización (A02)

Estructura de `Servicios/Serializacion`, equivalente a la del material de la cátedra (Unidad 4):

| Clase | Rol | Equivale a |
|-------|-----|------------|
| `SerializadorAbstracto` / `SerializadorAbstracto<T>` | Base abstracta: `Extension`, `Formato`, `Serializar(T, Stream)` y `Deserializar(Stream)` abstractos; `GuardarArchivo` (escribe a `.tmp` y reemplaza: un corte no deja un archivo truncado), `LeerArchivo`, las variantes con texto (`string`) y `ExigirVersionFormato` comunes (método plantilla) | `AbstractSerializator` |
| `SerializadorXml<T>` | `XmlSerializer`, UTF-8 sin BOM, indentado; al leer, sin DTD ni entidades externas (XXE) | `XmlSerializator<T>` |
| `SerializadorJson<T>` | `DataContractJsonSerializer` (viene con .NET, sin NuGet), UTF-8 indentado, fechas ISO 8601 legibles, cuotas de lectura | `JsonSerializator<T>` |
| `FabricaSerializadores` | Elige el serializador por la extensión (`.xml` / `.json`); otra extensión se rechaza con un mensaje claro | — |
| `GestorArchivos` (Singleton, `Instancia`) | Arma las rutas `%LOCALAPPDATA%\WardrobeFlow\<subcarpeta>` con `Environment.SpecialFolder`, crea la carpeta y sugiere el nombre del archivo con la extensión elegida | `FileStreamManager.Instance` |

- **Dónde se usa cada formato:** exportar/importar versiones de usuario y errores admite XML o JSON (lo decide la extensión elegida en el diálogo); cada importación valida lo mismo en los dos formatos: permisos, tamaño máximo (5 MB), formato, versión de formato y registros (versiones repetidas o de otro usuario, errores vacíos), más la pertenencia al usuario en el caso de las versiones, y deja su entrada en la bitácora. El registro interno `errores_AAAAMM.xml` es siempre XML y solo se leen los archivos con ese nombre exacto; las exportaciones de errores van por defecto a `Errores\Exportados`, aparte.
- **Qué no viaja:** la clave y el estado de bloqueo del Memento llevan `[XmlIgnore]` (XML) e `[IgnoreDataMember]` (JSON). Las clases de BE serializadas a JSON declaran `[DataContract]`/`[DataMember]`: sin eso, por ser `[Serializable]`, `DataContractJsonSerializer` escribiría los campos internos (`<Id>k__BackingField`), clave incluida. Los archivos XML ya exportados se siguen importando (mismo formato, verificado por pruebas).
- **Lo que se dejó afuera a propósito:**
  - `BinaryFormatter`: obsoleto (SYSLIB0011) e inseguro, porque deserializar un archivo ajeno puede ejecutar código; además el binario no lo lee otra plataforma ni una persona.
  - `SoapFormatter`: obsoleto y no admite colecciones genéricas como `List<T>`, que usan nuestras clases.
  - `ISerializable`, `IDeserializationCallback` y `[NonSerialized]`: solo los usan los formateadores `IFormatter` (binario/SOAP). `XmlSerializer` y `DataContractJsonSerializer` excluyen miembros con `[XmlIgnore]` / `[IgnoreDataMember]`, que son nuestros equivalentes.
  - `XmlSerializerNamespaces` y validación con XSD: el formato lo escribe y lo lee una sola aplicación; la estructura la garantiza el `XmlSerializer` fuertemente tipado más nuestras validaciones, y el atributo `version` resuelve la evolución del formato.

### Backups automáticos

Además del botón "Generar Copia de Seguridad", el sistema hace backups por su cuenta, por dos vías. Todos los backups (manual, de instalación limpia, de inicio de sesión y del reloj) pasan por el mismo método, `BLL.Backup.HacerBackup`, que verifica la integridad (RF-04) antes de copiar; con la integridad comprometida el backup automático se saltea, para no guardar una base adulterada como punto de restauración bueno.

| Disparador | Cuándo | Comportamiento |
|------------|--------|----------------|
| **Al iniciar sesión** | En cada ingreso, con cualquier rol | Se saltea si el último backup de la carpeta `Backups` es más nuevo que el intervalo configurado (por defecto 60 min; 0 = en todos los inicios de sesión; máximo 1440). El Administrador lo configura en Backup y Restauración → "Backup automático al iniciar sesión..." (se guarda en `Backups/intervalo_backup_login.cfg`). Es sincrónico: el ingreso espera a que termine. Si falla, el ingreso continúa, la falla queda en la bitácora y el Menú avisa una sola vez (el Administrador ve la ruta y el error; los demás roles, un mensaje genérico) |
| **Reloj de 24 h** (`BLL.BackupProgramado`) | Mientras la aplicación está abierta: primera revisión al minuto de abrir el Menú, después una por hora | Hace el backup si ya pasaron 24 h desde el último. Compara contra los archivos de la carpeta `Backups`, así cualquier backup reinicia la cuenta y uno atrasado se recupera al volver a abrir la aplicación. Si falla, reintenta cada hora pero registra en la bitácora como máximo un error cada 24 h; un bloqueo con `Interlocked` evita revisiones superpuestas. Se detiene al cerrar el Menú |

- **Rotación semanal:** los backups automáticos se guardan como `.bak` **sin cifrar** en 7 archivos fijos, `auto_domingo.bak` … `auto_sabado.bak`, y cada uno pisa el del mismo día de la semana anterior. El espacio en disco queda acotado y queda, como máximo, un punto de restauración por cada uno de los últimos 7 días (el último backup de ese día, si la aplicación se usó ese día). Los backups manuales y el de instalación limpia siguen siendo `.wfbak` cifrados con contraseña.
- **Pantalla Backup y Restauración:** muestra "Próximo backup automático: <último backup + 24 h>"; en la lista, los archivos `auto_*.bak` figuran con autor "sistema (automático)" y se restauran desde la misma pantalla, sin contraseña.
- **Limitaciones conocidas:**
  - El reloj de 24 h no es un programador de tareas del sistema operativo: solo corre mientras la aplicación está abierta. Con la aplicación cerrada no se hacen backups; el atrasado se hace al volver a abrirla.
  - Cuando corresponde un backup al iniciar sesión, el ingreso se demora lo que tarda el backup.
  - Los backups automáticos no están cifrados y quedan en la carpeta `Backups`, al lado del ejecutable: cualquiera con acceso a esa carpeta puede leerlos. Para una copia que vaya a salir del equipo conviene el backup manual cifrado.

---

## Mejoras opcionales (no requeridas por la cátedra)

Surgida de comparar WardrobeFlow con el TP de un compañero de cursada (ExperienceHub,
que tiene Lista de Espera para sus reservas). No es un PdN de la idea de negocio ni un
requisito de ninguna entrega — es un diferencial de producto que reutiliza el patrón
State ya entregado (PdN2/PdN4) sin tocarlo.

| Módulo | Resumen |
|--------|---------|
| **Lista de Espera de Prendas** | Matchea por prenda específica (mismo `IdPrenda`, no por categoría). Al liberarse, `BLL.Prenda.CambiarEstado` dispara `BLL.ListaEspera.NotificarSiCorresponde`, que reserva la fila `Pendiente` más antigua (FIFO) por `BLL.ListaEspera.HORAS_RESERVA` (48hs). Mientras la reserva está vigente, `BLL.Pedido` la bloquea para cualquier otro cliente (`err.bll.pedido.prenda_reservada`) y la cierra sola (`Convertida`) al crear el pedido del cliente correcto. Si nadie la retira a tiempo, vuelve a estar disponible para cualquiera por simple comparación de fecha — sin job en background, mismo criterio que `Cliente.FechaLimiteGracia` (PdN6). Ver `BD/00_Instalacion_Completa.sql` (sección 16), `BLL/ListaEspera.cs`, `GUI/ListaEsperaForm.cs`. |

---

## Patrones de diseño implementados

### Bloque 1 — Trabajo de Diploma (verificados contra el material de cátedra)

| Patrón | Dónde | Nota |
|--------|-------|------|
| **Builder** | `BE.Builders.SuscripcionBuilder` (abstracta) + `SuscripcionMensualBuilder` / `SuscripcionTrimestralBuilder` / `SuscripcionAnualBuilder` + `DirectorSuscripcion` | Cada builder concreto resuelve un único paso variable (`CalcularVencimiento`) que devuelve el valor, y un método concreto `BuildSuscripcion()` arma el producto (`Suscripcion`) vía constructor — misma estructura que `PizzaBuilder`/`Pizza` del ejemplo de cátedra. `DirectorSuscripcion` se mantiene como clase separada porque así lo muestra el diagrama de clase de la PPT |
| **State** | `BE.Estados.Estado` (abstracta) + `EstadoDisponible` / `EstadoEnLimpieza` / `EstadoEnUso` / `EstadoBaja`, contexto en `BE.Prenda.ControlarEstado` | Cada estado concreto valida y muta el contexto (`Prenda`) directamente, igual que `Estado`/`Switch` del ejemplo de cátedra |
| **Command** | `BLL.Comandos.PedidoCommand` (abstracta) + `CancelacionCommand` / `DevolucionCommand`, invocador `InvocadorPedido` | Cola de órdenes (`TomarOrden` / `ProcesarOrdenes`) que se ejecutan en lote, igual que `OrdenCommand`/`EmpresaInvoker` del ejemplo de cátedra — sin pila de deshacer, porque el material tampoco la tiene |
| **Chain of Responsibility** | `BLL.Manejadores.ManejadorRenovacion` (abstracta) + `VerificarVencimientoHandler` → `IntentarRenovarHandler` → `CambioPlanHandler` → `BajaSuscripcionHandler`, orquestada en `BLL.Renovacion` | Cadena armada de cola a cabeza con `AgregarSiguiente`, cada eslabón decide inline si atiende o delega — igual que `Aprobador`/`Program.cs` del ejemplo de cátedra |
| **Chain of Responsibility** (PdN6) | `BLL.Manejadores.ManejadorCobro` (abstracta) + `DetectarCobroHandler` → `ProcesarPagoHandler` → `AplicarGraciaHandler` → `SuspenderHandler`, orquestada en `BLL.Cobro` | Misma estructura que la cadena de Renovación (PdN5) — un cobro exitoso confirma la renovación reutilizando el Builder de PdN1; uno fallido abre un período de gracia antes de bloquear pedidos |

### Base heredada de Ingeniería de Software

| Patrón | Dónde |
|--------|-------|
| **Singleton** | `SessionManager` (sesión activa) · `ContadorSesion` (intentos de login) · `DAL.Acceso` (conexión BD) |
| **Observer** | `GestorIdioma` (Subject) → formularios como observers — cambio de idioma dinámico en tiempo de ejecución |
| **Composite** | `Componente` → `Patente` (hoja) / `Rol` (nodo compuesto, anidable *rol-en-rol*) — árbol de permisos con resolución recursiva, dedup y anti-ciclos |
| **Factory Method** | `GUI.Exportacion.GeneradorReporte` (Creator, con la operación `Exportar` que usa el método fábrica `CrearExportador`) → un creador por reporte; productos `ExportadorPdf` / `ExportadorTxt` / `ExportadorCsv` |
| **Memento** | `BE.Usuario` (Originator) + `BE.VersionUsuario` (Memento) + `BLL.CuidadorHistorial` (Caretaker) — versiona datos administrativos no sensibles y permite rollback |

---

## Validaciones de negocio en BLL

- **Sin plan asignado, no hay renovación:** `BLL.Renovacion.Procesar` exige que el cliente tenga un plan antes de entrar a la cadena de manejadores.
- **Bloqueo de reducción de plan:** si un cliente tiene prendas en uso y se intenta asignarle un plan con menor límite, la operación falla.
- **Bloqueo de pedido duplicado despachado:** no se puede crear un nuevo pedido si el cliente ya tiene uno en estado `Despachado` pendiente de entrega.
- **Bloqueo de desactivación de plan:** no se puede desactivar un plan si tiene clientes activos asignados.
- **Alerta de suscripción próxima a vencer:** se detecta y propaga en todo el flujo cuando la suscripción vence en ≤ 7 días.
- **Pedidos bloqueados por falta de pago:** `BLL.Pedido.CrearPedido` rechaza nuevos pedidos si venció el período de gracia otorgado tras un cobro fallido (`Cliente.EstaSuspendidoPorPago`), sin importar que la suscripción en sí siga vigente.
- **Validación de permisos por operación:** cada servicio BLL valida el permiso del usuario en sesión antes de ejecutar cualquier operación de escritura.

---

## Características de seguridad

- Contraseñas nunca en texto plano: PBKDF2-SHA256 con salt aleatorio y 100.000 iteraciones; verificación en tiempo constante
- Bloqueo de login progresivo (1 → 5 → 15 → 60 min) con claves de emergencia de un solo uso
- Handler global de excepciones no controladas: registra el detalle técnico en bitácora y muestra un mensaje genérico al usuario
- Dígitos verificadores (DVH por fila + DVV por tabla) sobre `Usuario`, `Cliente`, `Empleado`, `Pedido`, `Contratacion` y `PermisoRelacion`; cada escritura recalcula el DVH de las filas que tocó y el DVV **en la misma transacción** (`DAL.DigitoVerificador.EscribirConDV`, con el bloqueo del DV tomado antes de escribir): si el recálculo falla se revierte la escritura, así una falla nunca deja la base marcada como manipulada
- Backups cifrados con contraseña (AES-128 + HMAC-SHA256, clave derivada con PBKDF2) y Clave Maestra de Recuperación opcional. Los backups automáticos (`auto_*.bak`) no se cifran: quedan en la carpeta `Backups`, rotando sobre los últimos 7 días
- Unicidad respaldada en la base: DNI de los clientes activos, DNI, legajo y usuario de cada empleado (índices únicos filtrados), y número de operación del historial de pedidos
- **Usuarios demo con claves fijas (decisión deliberada para la defensa):** las cuentas de `Instalador/Credenciales_Iniciales.txt` no exigen cambio de clave en el primer ingreso. En una implementación real hay que cambiar todas esas contraseñas (desde Mi Perfil, o forzando el cambio desde Administrar → Usuarios) y borrar o restringir ese archivo. Las cuentas demo solo se crean en una instalación nueva; una actualización nunca las recrea.
- **Acceso a la base:** el instalador da al grupo local "Usuarios" de Windows el rol `db_owner` sobre `WardrobeFlowDB` (hace falta para restaurar backups desde la app). Quién puede operar lo decide el login y las patentes de la aplicación; un cambio de datos hecho por fuera de la app lo detectan los dígitos verificadores al iniciar.

---

## Usabilidad

Objetivo: que en todo momento se entienda **dónde está parado el usuario, qué hace el sistema y cómo avanzar**.

| Recurso | Qué ve el usuario | Dónde está |
|---------|-------------------|------------|
| **Panel de Control por rol** | Al ingresar se abre el tablero de su rol: Ventas (pedidos para atender, con faltantes y separados), Depósito (pedidos a controlar, prendas en mantenimiento, ocupación), Logística (pendientes/despachados/entregados) y uno general para el resto (indicadores y "Mis tareas pendientes" según las patentes). Un clic en una tarjeta o tarea abre la pantalla que la resuelve | `GUI.Menu.CrearDashboardDelRol`, `Dashboard*` |
| **Barra de estado "Usted está en"** | Al pie de la ventana principal: la ruta del menú de la pantalla activa (p. ej. *Promociones → Centro de decisiones comerciales*), una pista de una línea con lo que se puede hacer ahí, el usuario con su rol y el enlace **F1: Ayuda**. Se actualiza al cambiar de ventana, al cerrar la última ventana ("Inicio") y mientras está abierto un diálogo que se abre **desde el menú** (Nueva Contratación, Sugerir Promoción, Backup, Mi Perfil, la guía); los diálogos que abren las ventanas hijas no cambian la ubicación. Si se abre un diálogo del menú dentro de otro, al cerrarse vuelve a mostrar el anterior. Nunca interrumpe: un texto mal editado en Gestión de Idiomas (p. ej. un `{0` sin cerrar) cae al texto de fábrica | `GUI/Menu.Ubicacion.cs` + `BLL.MapaNavegacion` |
| **Ayuda en línea (F1)** | F1 en cualquier pantalla (o Sesión › Ayuda, o el enlace de la barra) abre la ayuda de esa pantalla; la ayuda general describe además la ventana principal | `GUI.Ayuda.AyudaEnLinea` (claves `ayuda.*`, `ux.ayuda.ventana`) |
| **Tooltips** | 106 botones de 26 pantallas explican su función al apoyar el mouse (los que no se entienden solo por el texto: Formalizar, Separar prendas, Intento Fallido, Purgar, Recalcular Todo…) | `GUI/FormBase.Usabilidad.cs` (claves `tip.<Formulario>.<control>`) |
| **Guía de primer uso** | En el primer ingreso de cada usuario, después del Panel de Control y de los avisos de arranque, se abre "Bienvenido a WardrobeFlow" con 5 pasos para su rol: qué hace el sistema para él, por qué menú empezar, qué muestra su panel, la barra de estado y F1. La guía vuelve a aparecer en cada ingreso hasta que el usuario marque "No volver a mostrar al iniciar sesión" (desmarcada por defecto); se puede reabrir en cualquier momento desde **Sesión › Ver guía de inicio**. Los pasos se eligen por el **nombre del rol** del usuario: un rol renombrado o creado en Perfiles y Permisos recibe la guía genérica (el Administrador siempre ve la suya) | `GUI/GuiaPrimerUsoForm.cs` + `BLL.GuiaPrimerUso` |
| **Idiomas** | Todo lo anterior está en los 4 idiomas y cambia en vivo con el selector de idioma | `traducciones.tsv` (claves `ux.*` y `tip.*`) |

- **La ruta de la barra no está escrita a mano.** `BLL.MapaNavegacion` solo dice qué ítem del menú abre cada formulario (y su pista); el Menú busca ese `ToolStripMenuItem` y sube por sus `OwnerItem` leyendo el texto traducido de cada nivel, así que si un ítem cambia de submenú la ruta lo sigue. Un test (`UsabilidadTests`) coteja el catálogo con los manejadores reales de `GUI/Menu.cs` y exige que toda pantalla alcanzable desde el menú tenga su pista en los 4 idiomas.
- **Tooltips centralizados.** `FormBase` busca, por cada campo-control del formulario, la clave `tip.<Formulario>.<campo>` y la asigna como tooltip; se re-aplican al cambiar de idioma. Agregar un tooltip es agregar la clave al `.tsv` (sin tocar el formulario).
- **Dónde se guarda "No volver a mostrar".** En `%LOCALAPPDATA%\WardrobeFlow\guia_primer_uso.txt` (la misma carpeta de Versiones y Errores), una línea por usuario con el **hash SHA-256** del nombre de usuario, no el nombre. Es una preferencia de presentación del equipo: no cambia el esquema, ni los dígitos verificadores, ni el backup. Se escribe de forma atómica (archivo temporal + reemplazo), con un candado entre procesos, y si el archivo existe pero no se puede leer no se reescribe (no se pierden las marcas de los demás usuarios). La guía no se abre si la aplicación corre sin interfaz (`Environment.UserInteractive`), con `/silent` o `/singuia`, o con la variable de entorno `WARDROBEFLOW_SIN_GUIA=1`.
- **Orden de lo que aparece al ingresar.** Panel de Control → avisos modales de arranque (p. ej. el del backup) → guía de primer uso → lo que se haya encolado con `Menu.DespuesDeLaGuia(accion)`, pensado para avisos no modales como el aviso comercial de PN03. `DespuesDeLaGuia` ejecuta la acción al cerrarse la guía o, si la guía no se va a mostrar, apenas se decide. La cola es `BLL.SecuenciaDespuesDeLaGuia`, probada sin interfaz. Además, lo encolado nunca se superpone a un diálogo modal: `Menu.CuandoLaVentanaEsteLibre` lo hace esperar (con `BLL.ColaHastaVentanaLibre`, también probada sin interfaz) a que la ventana esté habilitada y sin modales propios, la misma condición con la que espera la guía; así, si la guía no se muestra y el aviso del backup sigue abierto, el aviso comercial aparece recién al cerrarlo.

---

## Multiidioma

Soporta **Español · English · Русский · Português** con cambio dinámico en tiempo de ejecución. Las traducciones se almacenan en la tabla `Traduccion`; un corpus embebido (`traducciones.tsv`) actúa como fallback por clave y siembra las claves que falten al iniciar. La interfaz en español está **en usted** (sin voseo), igual que el instalador.

---

## Instalación y configuración

### Requisitos

- Visual Studio 2022 (o superior)
- .NET Framework 4.7.2
- SQL Server 2012 o posterior (local o remoto); ver abajo

### Requisitos: SQL Server

WardrobeFlow guarda todos sus datos en SQL Server (base `WardrobeFlowDB`), así que necesita un motor instalado. El instalador lo resuelve así:

- **Ya hay SQL Server en el equipo:** detecta las instancias (Express, LocalDB u otra) con su versión y permite elegir; también se puede ingresar a mano un servidor remoto.
- **No hay ningún motor (opción recomendada, preseleccionada):** en la página «No se encontró SQL Server en este equipo» se elige *Instalar SQL Server Express LocalDB*, que viene incluido en el instalador; no hace falta descargar nada. La base de LocalDB es privada del usuario de Windows que instala.
- **SQL Server 2022 Express completo** (para que varios usuarios de Windows compartan la base): se descarga de <https://www.microsoft.com/es-es/sql-server/sql-server-downloads>, se instala con el tipo «Básica» y después se vuelve a ejecutar el instalador eligiendo `.\SQLEXPRESS`.

El paso a paso de todas las opciones, cómo verificar el servicio (`services.msc`), la solución de problemas frecuentes (servicio detenido, permisos, versión antigua, servidor remoto con TCP/IP, puerto 1433, firewall y SQL Server Browser) y dónde quedan los logs del instalador están en el tutorial **[`Instalador/Tutorial_SQL_Server.html`](Instalador/Tutorial_SQL_Server.html)** (HTML autónomo, funciona sin Internet y se puede imprimir). Desde el instalador se abre con el botón *Ver tutorial para instalar SQL Server* de la página «No se encontró SQL Server», y también se ofrece abrirlo si se cancela por no tener motor o si falla la instalación de LocalDB. Una vez instalado WardrobeFlow, queda en `{app}\Docs\Tutorial_SQL_Server.html` (por defecto `C:\Program Files\WardrobeFlow\Docs\`) y en el menú Inicio como «Cómo instalar SQL Server».

### Base de datos

**Instalación nueva → un solo script.** Ejecutar desde SSMS (o `sqlcmd -f 65001`, ver comentario en el archivo):

```
BD/00_Instalacion_Completa.sql   -- Crea WardrobeFlowDB completa: estructura, datos semilla
                                  -- y los 4 procesos de negocio (PN01-PN04). Idempotente:
                                  -- se puede volver a ejecutar sin duplicar ni romper nada.
```

Es el único archivo de esquema que se edita. Los scripts individuales que introdujeron cada módulo históricamente (los viejos `01`, `03`, `05`, `06`, `08` a `20`) ya no están en el repo — todo su contenido está absorbido en `00`, que es la única fuente de verdad; si hace falta ver cómo se introdujo una funcionalidad puntual, buscarlo en el historial de git en vez de en un archivo aparte. El instalador (`Instalador/WardrobeFlow_Setup.iss`) usa este único script.

**Es el único script que corre el instalador.** (En `BD/` están además `01_Esquema_Final_DDL.sql`, el DDL limpio del esquema final como documentación, y `Reset_Datos_Demo.sql`, que pide recargar los datos de demo en la próxima corrida.) Incluye también los **datos de prueba** de todos los procesos (sección 21: clientes en distintos estados de suscripción, prendas en cada estado, pedidos, contrataciones pendientes, promociones, etc.), así que la base queda lista para probar apenas se instala — el instalador (`Instalador/`) ejecuta este mismo archivo. Sobre una base ya instalada con una versión anterior también es seguro volver a ejecutarlo: migra el rol `OperadorDeInventario` a `Deposito` y retira los roles viejos.

**Paso obligatorio después de actualizar desde la 1.5.0:** después de actualizar desde 1.5.0, ejecute **Backup → Restablecer datos de demo** (con el usuario `admin`). La actualización migra el esquema sin tocar los datos, así que una base 1.5.0 conserva su demo anterior, que no tiene las metas comerciales, las decisiones sembradas ni los movimientos que usan el Centro de decisiones, las cohortes y el Informe ejecutivo de la 1.6.0 (se ven pestañas vacías o parciales). El instalador no siembra la demo por su cuenta en una actualización (borraría los datos cargados); el paso también figura en `Instalador/Credenciales_Iniciales.txt`. Una instalación nueva ya trae la demo completa.

**Notas de los módulos** (para ubicarlos dentro de `00_Instalacion_Completa.sql` — los números de sección adentro del archivo coinciden con estos, y son el orden histórico en el que se agregaron):
- **Lista de Espera (mejora opcional)** — sección 16 en `00`; el resto del sistema funciona sin ella (`BLL.Prenda`/`BLL.Pedido` degradan a su comportamiento anterior si la tabla `ListaEspera` no existe).
- **Comercialización de la suscripción (PN02)** — sección 17 en `00`. Crea el rol `Caja` (separado de Vendedor) y sus patentes; hay que asignarle el rol `Caja` a algún usuario desde Administrar → Usuarios para poder probar el módulo (o usar el usuario demo `caja`).
- **Inteligencia comercial y promociones (PN03)** — sección 18 en `00` (promociones) y **20o** (tabla `DecisionComercial`, umbrales `Comercial.*` en `ParametroSistema` y mapeo del ítem de menú del Centro de decisiones; sin patente nueva). **20p**: tablas `MetaComercial` y `DecisionComercialDetalle` y borrado de los umbrales de las reglas retiradas; **20p** también agrega `DecisionComercial.FechaEjecucion` y el índice filtrado `UX_DecisionComercial_Abierta`; **21z10**: textos del Centro de decisiones (acciones y metas) en bases ya instaladas; **21z13**: deja `ayuda.CentroDecisionesForm` en el texto final (acciones, metas y la línea de Cohortes) partiendo del texto de fábrica de la 1.6.0, del de 21z10 o del de 21z12. **21z14**: en bases ya instaladas actualiza el ciclo de la portada del Informe ejecutivo (`cdc.informe.ciclo`, el mismo de la ayuda F1), los mensajes de metas con el mes como `yyyy-MM`, la ayuda de la barra de estado de Historial de Cambios (XML o JSON), la fórmula del glosario de las campañas de contacto (`glos.campania.formula`, población comparable) y la ayuda F1 de los umbrales (sin los de ocupación, que ya no existen), solo si siguen con el texto de fábrica anterior. La demo (`dbo.RestablecerDatosDemo`, paso 9b2) siembra metas relativas a hoy: MRR del mes cumplida, ocupación del mes en riesgo, % de bajas del mes y MRR del mes anterior no cumplida. Crea los roles `AdministracionComercial` y `Contabilidad` (Gerencia y Vendedor reusan `GerenteComercial`/`Vendedor` ya existentes); hay usuarios demo (`admcomercial`, `contable`) para probar el módulo sin dar de alta nada a mano.
- **Cohortes del Centro de decisiones (PN03)** — sección 21z12 en `00`: sin tablas nuevas (usa `Contratacion` y `HistorialRenovacion`); solo actualiza la ayuda F1 del Centro en una base ya instalada si sigue con el texto de fábrica de la 1.6.0. Los textos nuevos (`cdc.cohorte.*`, `glos.cohorte.*`) los inserta la aplicación desde `traducciones.tsv`.
- **Inspección de Devolución (PN04)** — sección 19 en `00`. Sin rol nuevo: reusa el rol `Deposito` (el "Depósito" de PN01). Lógica alineada a Nuuly (binaria, sin aprobador): reingresa sin cargo o se da de baja cobrando el precio de reposición (`BLL.CargoPrenda.RegistrarCargo`, existente desde Bloque 1).
- **Aplicación de promociones al cobro e integridad (PN02/PN03/PN04)** — sección 20b en `00`. Agrega `Importe`, `DescuentoAplicado` e `IdPromocion` a `Contratacion` (lo que realmente se cobró), los `CHECK` de `CargoPrenda.Monto` e `IntentosPago`, las restricciones únicas (una contratación pendiente por cliente, una anotación activa por prenda y cliente) y los índices de las tablas nuevas.
- **Auditoría del 13/10** — secciones 20g a 20n y 21z6 en `00`: **20g** PN03 solo por plan, **20h** tasa de retención de la sugerencia, **20i** analítica en un formulario, **20j** pantallas de seguridad consolidadas, **20k** número de operación único en `PedidoHistorial`, **20n** unicidad de DNI, legajo y usuario del empleado, **21z6** textos en español en usted (solo cambia los que siguen iguales a los de fábrica).
- **Hardening de integridad (auditoría de BD)** — sección 20 en `00`. Agrega los CHECK constraints que faltaban en columnas `Estado`/`Resultado` respaldadas por enum (`Prenda`, `Pedido`, `HistorialRenovacion`, `HistorialCobro`, `ListaEspera`, `CargoPrenda`, `Bitacora`) y los índices sobre `Prenda.Estado`/`Pedido.Estado`.

### Cadena de conexión

Configurar en `GUI/App.config`:

```xml
<connectionStrings>
  <add name="WardrobeFlowDB"
       connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=WardrobeFlowDB;Integrated Security=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Pasos

```
1. Clonar el repositorio
2. Abrir WardrobeFlow.slnx en Visual Studio
3. Configurar la cadena de conexión en GUI/App.config
4. Ejecutar BD/00_Instalacion_Completa.sql (o usar el instalador)
5. Compilar y ejecutar GUI como proyecto de inicio
```

En el primer arranque el sistema seedea automáticamente las tablas de traducciones, idiomas, permisos y el usuario `admin2` de respaldo (se crea en el primer arranque; no figura en `Instalador/Credenciales_Iniciales.txt`).

### Build y tests

Abrir `WardrobeFlow.slnx` en Visual Studio, compilar la solución (8 proyectos: BE, Seguridad, DAL, Servicios, BLL, GUI, Tests e Instalador/DbInstaller) y correr los tests desde el Explorador de pruebas (suite MSTest sobre `Tests.dll`, 1214 pruebas al 21/10/2026: 1196 pasan y 18 de integración se omiten cuando no está disponible la base de pruebas). Las pruebas de integración usan una base aparte, `WardrobeFlowDB_Tests` (el mismo script con ese nombre); nunca tocan `WardrobeFlowDB`.

### Generar el instalador

1. Compilar la solución en modo **Release** (y `Instalador/DbInstaller` en Release).
2. Ejecutar `Instalador/compilar-y-firmar.ps1`: descarga `Instalador/Redist/SqlLocalDB.msi` si falta (instalador oficial de SQL Server 2022 Express LocalDB, no se versiona), compila `WardrobeFlow_Setup.iss` con Inno Setup y firma el `.exe` con SignTool, o con `Set-AuthenticodeSignature` si no está el Windows SDK. Antes de compilar corre todas las pruebas. Usa el certificado de WardrobeFlow instalado en el almacén de Windows (o `wardrobeflow.pfx` en `%USERPROFILE%`, que no se versiona); si no hay ninguno, el instalador queda sin firmar. El certificado es autofirmado: Windows puede mostrarlo como editor desconocido.
3. Sale un único archivo: `Instalador/Salida/Instalador_WardrobeFlow_1.6.0.exe` (el número sale de `MyAppVersion` del `.iss`) (~64 MB), con la aplicación, la base de datos, los datos de prueba y SQL Server LocalDB adentro. En un equipo sin ningún SQL Server, la opción por defecto instala LocalDB y sigue la instalación normal. Sobre una base existente, el instalador hace un backup, corre el script (que migra sin perder datos) y verifica que haya al menos un Administrador activo. La última versión publicada es la **1.6.0** (GitHub Releases, tag `v1.6.0`).
