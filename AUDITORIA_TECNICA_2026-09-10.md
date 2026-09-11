# Auditoría técnica — WardrobeFlow (2026-09-10)

> **Actualización (2026-09-11): todos los 🔴 Alta están resueltos**, ~64 de los 95 hallazgos
> 🟡 Media/🟢 Baja también, y el ítem #1 del scan transversal (§10) — el helper de traducción
> duplicado ~84 veces — quedó centralizado en `FormBase.Tr` en los 45 formularios que heredan
> `FormBase` (ver el marcador `✅ RESUELTO (2026-09-11)` en cada uno, con su "Solución
> aplicada"). Build + tests verificados en verde después de cada tanda de cambios (407 tests,
> 405 passed, 2 skipped preexistentes, 0 fallando). Los ítems Media/Baja sin ese marcador
> quedan anotados `(pendiente)`
> cuando requieren un refactor de mayor alcance, una decisión de producto, o simplemente no se
> llegó a esta pasada — no se tocó nada fuera de lo explícitamente marcado como resuelto. El scan
> transversal (§10) sigue sin empezar salvo el ítem #12 (color de marca), resuelto como parte de
> §8 GUI-Infraestructura.

> **Metodología:** revisión manual y crítica hecha por 10 agentes en paralelo, cada uno con un
> alcance acotado (una capa o un grupo funcional de pantallas), sobre las ~56.000 líneas de
> `BE/BLL/DAL/GUI/Seguridad/Servicios/Tests`. Cada agente verificó primero contra
> `AUDITORIA_TECNICA.md` (2026-08-17) qué hallazgos previos seguían vigentes, para no repetirlos.
> Es una revisión de **solo lectura** — no se modificó ningún archivo. El detalle completo de cada
> área está más abajo; esta sección de arriba es un resumen priorizado para no tener que leer las
> ~120 observaciones una por una.
>
> **Cobertura:** BE+DAL, BLL, Seguridad+Servicios, GUI (Dashboards/Análisis/Reportes,
> Pedidos/Prendas/Clientes, Usuarios/Seguridad, Promociones/Suscripciones, infraestructura), Tests,
> y un scan transversal de consistencia sobre todo el repo. La sección de Tests quedó **parcial**:
> cubre en profundidad Pedido/Cliente/Cobro/Renovación/Promoción/Seguridad-Permisos, pero no llegó
> a cerrar Contratación/Análisis/Reportes/Prenda-CargoPrenda-ListaEspera con el mismo detalle (ver
> nota al pie de esa sección).

---

## Resumen por área

| Área | 🔴 Alta | 🟡 Media | 🟢 Baja | Total |
|---|---|---|---|---|
| BE + DAL | 2 | 8 | 7 | 17 |
| BLL | 2 | 6 | 2 | 10 |
| Seguridad + Servicios | 1 | 4 | 7 | 12 |
| GUI — Dashboards/Análisis/Reportes | 6 | 8 | 6 | 20 |
| GUI — Pedidos/Prendas/Clientes | 3 | 7 | 4 | 14 |
| GUI — Usuarios/Seguridad | 3 | 6 | 7 | 16 |
| GUI — Promociones/Suscripciones | 3 | 8 | 4 | 15 |
| GUI — Infraestructura | 3 | 4 | 2 | 9 |
| Tests (parcial) | 4 | 4 | 1 | 9 |
| **Total** | **27** | **55** | **40** | **122** |

Más el scan transversal (13 patrones de consistencia a nivel de todo el repo, no contados arriba
por severidad — ver su propia sección).

---

## Top prioritario (si solo vas a mirar una lista, que sea esta)

Los ~18 hallazgos de mayor impacto de negocio/seguridad de toda la revisión, en ningún orden particular de gravedad dentro del grupo:

1. **`BLL/Familia.cs:274-288` — altas/bajas de permisos no atómicas.** Un ciclo detectado a mitad de camino deja relaciones ya persistidas en BD sin poder revertirlas (§BLL #1).
2. **`BLL/ReporteJornada.cs:44-56` — la alerta Crítica "sin backups" está en falso positivo permanente** porque solo busca `*.bak` y el sistema ya solo genera `.wfbak` cifrados (§BLL #2).
3. **`BLL/Bitacora.cs:54-59` — restricción de acceso a la bitácora comparada contra un rol retirado ("Supervisor")**, nunca se aplica en la práctica (§BLL #3).
4. **`BLL/Familia.cs:75-91` — 3 `catch` fail-open sobre el guard anti-lockout de administradores**: un fallo transitorio de BD salta en silencio la protección de "no dejar el sistema sin ningún admin" (§BLL #4).
5. **`GUI/OlvideContrasenaForm.cs:96-110` — enumeración de usuarios**: mensaje distinto según si el username existe o no, justo el antipatrón que `Login.cs` evita a propósito (§GUI-Usuarios #1).
6. **DNI de cliente logueado en texto plano en la Bitácora** (`BLL/Cliente.cs:56`), evadiendo el cifrado que se le aplica en la tabla de negocio (§Seguridad #1).
7. **Condición de carrera real sobre `Pedido`**: la GUI actúa sobre estado cacheado y el `UPDATE` en DAL no valida el estado esperado — dos operadores pueden pisarse (despachar un pedido que otro acaba de cancelar) (§GUI-Operación #1).
8. **Selección múltiple habilitada por defecto pero ignorada en todas las grillas de acción** (Pedidos, Prendas, Clientes, Lista de Espera, Inspección): el usuario puede creer que actuó sobre 3 filas seleccionadas y solo se aplicó a la primera, sin aviso (§GUI-Operación #2).
9. **`ContratacionesPendientesForm` nunca muestra el monto a cobrar** — Caja confirma un cobro real sin ver el importe en pantalla (§GUI-Promociones #3).
10. **`CobroSuscripcionForm`/`RenovacionSuscripcionForm` sin ninguna confirmación antes de procesar**, incluyendo la opción "Dar de baja" (irreversible) (§GUI-Promociones #2).
11. ✅ RESUELTO — **`GestorPermisos.CrearSubRol` — si falla el segundo paso, el rol nuevo queda huérfano** en la base sin vínculo al padre, sin rollback ni aviso claro (§GUI-Usuarios #4).
12. **9 de 16 pantallas (dashboards, reportes, historiales, alertas) no heredan `FormBase`**: ignoran tema oscuro/fuente del usuario, duplican a mano la carga de ícono, y `AlertasForm` directamente se olvidó de copiarlo (abre con ícono por defecto) (§GUI-Dashboards #1).
13. **`DashboardSupervisor` es el único de los 4 dashboards con Kanban sin navegación por clic** en sus tarjetas — inconsistencia funcional entre pantallas gemelas (§GUI-Dashboards #3).
14. **`DAL/Interfaces/IUsuarioDAL` no expone `RestaurarVersion` ni `ObtenerPorId`**, rompiendo la inversión de dependencias justo para el patrón Memento (el más sensible del proyecto) — no se puede testear esa ruta con un doble (§BE-DAL #1).
15. **`DAL/Pedido.cs` — `RecalcularDV()` sin `try/catch` después del `Commit`** en 6 puntos: si el recálculo falla, el usuario ve "error al despachar" con el pedido ya despachado en la base (§BE-DAL #2).
16. **`BLL.Cobro`/`BLL.Renovacion` (las clases fachada reales) nunca se instancian en los tests** — la cadena de Responsabilidad se prueba reconstruida "a mano" en el test, no la que arma el constructor real; si alguien invierte el orden real, ningún test lo detecta (§Tests #1).
17. **`CambioPlanHandler`/`BajaSuscripcionHandler` reciben DAL concretos en vez de interfaces → 0% testeados**, incluida la validación de "plan insuficiente para el stock en uso" al cambiar de plan (§Tests #2).
18. ✅ RESUELTO — **Helper de traducción reinventado ~84 veces con ~24 nombres distintos** (`T`, `Tx`, `T_ce`, `T_dlg`, `T_c`...) en 48 archivos — mismo cuerpo copiado sin centralizar (§Transversal #1).

---

## 1. BE + DAL

**Alcance:** `BE/` (Builders, Estados, Memento) y `DAL/` (Interfaces). ~80 archivos, ~8.550 líneas.

Contexto: transacciones no atómicas Cliente+Renovación/Cobro — **verificado resuelto**. DNI cifrado con búsqueda O(n) — comportamiento conocido, decisión explícita de no resolver ahora (HMAC-DNI pospuesto), no se repite como hallazgo nuevo. La capa BE es de muy buena calidad: entidades con invariantes claros, sin código muerto, Builder/State/Memento genuinamente implementados (no decorativos).

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — `IUsuarioDAL` no expone `RestaurarVersion` ni `ObtenerPorId` — rompe el DIP que la propia interfaz dice garantizar.**
*Solución aplicada:* se agregaron ambos métodos a `IUsuarioDAL` y se implementó el espía correspondiente en `FakeUsuarioDAL`.
`DAL/Usuario.cs:336` (`RestaurarVersion`, aplica un snapshot del Memento) y `:478` (`ObtenerPorId`) son públicos pero no están en `IUsuarioDAL`. Cualquier capa que dependa de la abstracción no puede invocar la restauración del Memento sin castear al tipo concreto — rompe la DIP justo para el patrón más sensible del proyecto, e impide testear esa ruta con un doble. Mismo agujero en `ICobroDAL`/`IRenovacionDAL`/`ICargoPrendaDAL` (no exponen `ObtenerPorId` aunque la clase concreta sí lo tiene vía `BaseDAL<T>`).
*Sugerencia:* agregar `ObtenerPorId`/`RestaurarVersion` a `IUsuarioDAL` (y el `ObtenerPorId` faltante a las otras 3 interfaces si se necesita testear esas rutas).

**2. ✅ RESUELTO (2026-09-11) — `DAL/Pedido.cs` — `RecalcularDV()` sin `try/catch` puede convertir una operación exitosa en un error para el usuario.**
*Solución aplicada:* se agregó `RecalcularDVSilencioso()` (try/catch + `Trace.TraceError`, mismo criterio que Cliente/Empleado) y los 6 call-sites internos ahora la usan; `RecalcularDV()` en sí sigue propagando para el recálculo administrativo manual.
Se invoca sin protección inmediatamente después del `Commit` en `Alta`(287), `Despachar`(310), `MarcarEntregado`(332), `RegistrarDevolucion`(357), `Cancelar`(522), `DesCancelar`(575). Si el recálculo del DVH/DVV falla por cualquier motivo transitorio, la excepción sube como si el despacho/cancelación/entrega hubiera fallado, cuando el cambio de estado ya quedó persistido — el usuario puede reintentar sobre un pedido que ya cambió de estado. Contradice el criterio que el propio proyecto usa en `DAL/Cliente.cs:23-27`/`DAL/Empleado.cs:22-26` ("la falla del DV no debe abortar la operación de negocio").
*Sugerencia:* envolver las 6 llamadas con el mismo criterio que Cliente/Empleado (loguear y continuar).

### 🟡 Media

**3. ✅ RESUELTO (2026-09-11) — SQL con enteros interpolados en vez de parametrizados (+ un magic number).** `DAL/Contratacion.cs:122,143`, `DAL/SugerenciaPromocion.cs:83` interpolan un cast de enum en el texto del UPDATE (sin riesgo real, pero rompe la convención parametrizada del resto del DAL); `DAL/Promocion.cs:188` es peor — `Estado=3` es un magic number literal, sin comentario, y 15 líneas más abajo `CambiarEstado` en el mismo archivo sí parametriza `@Estado` correctamente.
*Solución aplicada:* los 3 sitios pasan a `@Estado` parametrizado.

**4. Detección de "BD sin migrar" por matcheo de texto sobre `SqlException.Message`.** Más de 10 veces en `DAL/Usuario.cs` (afecta el login), también en `DAL/Permiso.cs`/`DAL/VersionUsuario.cs`. Frágil: depende del idioma del servidor y de que el mensaje contenga la subcadena esperada; un error real que mencione esa palabra por coincidencia se trataría como "no migrado" en el camino más transitado del sistema. *(pendiente — refactor transversal de mayor alcance, se deja para una pasada aparte)*

**5. ✅ RESUELTO (2026-09-11) — `DAL/VersionUsuario.cs:49-50` — el fallback de `LeerLista` solo reconstruye el primer parámetro del array**, ignorando silenciosamente el resto si en el futuro un filtro usa 2+ parámetros. Bug latente, no activo hoy.
*Solución aplicada:* el fallback ahora copia todos los parámetros vía `Array.ConvertAll`, no solo `parametros[0]`.

**6. Inconsistencia sistemática: `BaseDAL<T>` (que centraliza el acceso a BD) vs. ~17 clases DAL que redeclaran `Acceso.GetInstance()` a mano** en vez de heredar — pierden el contrato mínimo uniforme que la clase base fuerza. *(pendiente — refactor de ~17 archivos, se deja para una pasada aparte)*

**7. ✅ RESUELTO (2026-09-11) — `Bitacora.Registrar`/`BitacoraNegocio.Registrar` tragan cualquier falla del INSERT de auditoría con criterios distintos entre sí** (una atrapa y solo traza, la otra ni siquiera tiene try/catch) — riesgo de pérdida silenciosa e indefinida de rastro de auditoría.
*Solución aplicada:* `BitacoraNegocio.Registrar` ahora tiene try/catch con el mismo criterio que `Bitacora.Registrar`.

**8. ✅ RESUELTO (2026-09-11) — `DAL/Permiso.cs:268-281` (`BajaComponente`) — DELETE+UPDATE como batch sin `EjecutarTransaccion` explícita**, a diferencia del resto de operaciones multi-tabla del proyecto.
*Solución aplicada:* envuelto en `acceso.EjecutarTransaccion`.

**9. `AddWithValue` sin tipar vs. `SqlParameter` explícito — split sistemático entre métodos normales y `*EnTx`** dentro del mismo archivo DAL (anti-patrón de ADO.NET documentado). *(pendiente — refactor transversal, se deja para una pasada aparte)*

**10. ✅ RESUELTO (2026-09-11) — Helper `Id()` de whitelist de identificadores SQL duplicado byte a byte** entre `DAL/DigitoVerificador.cs:128-138` y `DAL/Backup.cs:212-222` (el propio comentario de Backup reconoce la duplicación).
*Solución aplicada:* extraído a `DAL/SqlIdentificador.Validar(string)`, usado por ambos archivos.

### 🟢 Baja

11. ✅ RESUELTO (2026-09-11) — `HistorialIntegridad.cs` es la única clase DAL que nombra el campo `_acceso` en vez de `acceso`.
*Solución aplicada:* renombrado a `acceso`.
12. Mezcla de `camelCase`/`PascalCase` en nombres de `SqlParameter` dentro del mismo archivo (`DAL/Usuario.cs`). *(pendiente)*
13. `IEmpleadoDAL` no tiene el equivalente a `ExisteDNIParaOtro` que sí tiene `IClienteDAL` (misma necesidad de negocio resuelta de forma asimétrica). *(evaluado y descartado a propósito: `DAL.Empleado.ExisteDNI` no tiene ningún caller en BLL — no existe `BLL.Empleado` — agregar la paridad en la interfaz sería solo código muerto nuevo)*
14. `DAL.Usuario` reimplementa ~90% de `DigitoVerificador.RecalcularTabla` para poder sincronizar la tabla espejo — duplicación justificable pero con riesgo de divergencia. *(pendiente)*
15. Full-table scan de `Usuario` (dos veces) en cada intento de login fallido/exitoso — no escala, invisible a esta escala de datos. *(pendiente)*
16. ✅ RESUELTO (2026-09-11) — `catch { }` vacíos al limpiar archivos temporales de backup, sin ningún `Trace` — un `.bak` temporal que no se borra queda invisible en los logs.
*Solución aplicada:* ambos `catch` ahora loguean vía `Trace.TraceWarning`.
17. `DAL.Cliente.BuscarIdPorDni` sigue siendo O(n) con descifrado en memoria — comportamiento conocido y decisión explícita de no resolver, se deja constancia.

---

## 2. BLL

**Alcance:** `BLL/` completa (Comandos, Estrategias, Manejadores, Interfaces, clases de negocio). ~82 archivos, ~7.800 líneas.

Contexto: patrón Command decorativo y Chain of Responsibility sin guarda de null — **ambos verificados resueltos**, no se repiten. Las 5 clases de Análisis mantienen cada una su propio umbral con su propia justificación de negocio (no hay drift accidental). Esquema de errores `err.bll.<módulo>.<motivo>` aplicado consistentemente.

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — `BLL/Familia.cs:274-288` (`GuardarAsignacionRol`) — altas/bajas de permisos no atómicas.**
*Solución aplicada:* se separó "validar" de "aplicar" en dos pasadas — se validan TODOS los ciclos antes de persistir ninguna relación nueva, así un ciclo detectado a mitad de camino ya no deja altas previas escritas en BD.
Si el rol tiene 3 componentes nuevos (A, B, C) y C formaría un ciclo, el método ya persistió A y B en BD antes de validar C — el segundo `foreach` (las bajas) nunca se ejecuta. El usuario ve "ciclo detectado" pero el rol ya cambió parcialmente, sin bitácora ni feedback de qué quedó aplicado. Es el motor real de autorización (`PermisoRelacion`), no un caso de borde exótico.
*Sugerencia:* envolver en transacción (mismo patrón `EjecutarTransaccion` ya usado en Cliente/Renovación/Cobro), o validar todos los ciclos antes de persistir ninguna relación.

**2. ✅ RESUELTO (2026-09-11) — `BLL/ReporteJornada.cs:44-56` (`ObtenerDiasSinBackup`) — solo busca `*.bak`, pero el sistema ya solo genera `.wfbak` cifrados.**
*Solución aplicada:* ahora busca `GetFiles("*.bak").Concat(GetFiles("*" + Backup.ExtensionCifrada))`, mismo criterio que ya usaba `GUI/BackupForm.cs`.
`BLL/Backup.cs` cifra siempre el `.bak` intermedio y lo borra. `GUI/BackupForm.cs` ya sabe esto y busca ambas extensiones — pero `ReporteJornada` quedó con el filtro viejo. Consecuencia: en cualquier instalación que solo use backups cifrados, esta función siempre devuelve `-1` ("no hay backups"), alimentando una alerta **Crítica** permanentemente falsa (`err.bll... alert.backup.nunca`), lo que entrena a los operadores a ignorarla.
*Sugerencia:* `GetFiles("*.bak").Concat(GetFiles("*" + BLL.Backup.ExtensionCifrada))`, mismo criterio que ya usa `BackupForm`.

### 🟡 Media

**3. ✅ RESUELTO (2026-09-11) — `BLL/Bitacora.cs:54-59` (`UsuarioPuedeVerSistema`) — compara contra un perfil retirado.** `"Supervisor"` fue remapeado a `"GerenteComercial"` (`Usuario.Abm.cs:307`, `BD/07_Reset_Perfiles_Permisos.sql:27-28`). El método devuelve `true` para todo el mundo en la práctica — la restricción de negocio ("el Supervisor solo ve bitácora de negocio") quedó muerta tras la consolidación de roles.
*Solución aplicada:* ahora compara contra `"GerenteComercial"`.

**4. ✅ RESUELTO (2026-09-11) — `BLL/Familia.cs:75-91` (`ExigirSistemaConservaGestion`) — 3 `catch` fail-open sobre el guard de "último administrador".** Si hay un problema transitorio de BD (el escenario más probable de disparo), la operación destructiva procede **sin el chequeo**, en vez de fallar cerrado — antipatrón para un guard de seguridad.
*Solución aplicada:* se mantiene el fail-open (cambiarlo a fail-closed rompería el guard en un caso legítimo de BD caída), pero ahora cada catch loguea explícitamente en Bitácora con `Criticidad.Alta` vía el nuevo helper `RegistrarGuardSaltado`, así el salteo queda auditado en vez de silencioso.

**5. `BLL/Configuracion.cs:220-222` (`VerificarIntegridadDV`) — heurística de texto frágil** (`msg.Contains("DVH")`) para distinguir "BD sin migrar" de corrupción real; un error real que mencione esas palabras por coincidencia se trataría como no-migrado. *(pendiente — mismo patrón que §1 #4, se deja para la misma pasada)*

**6. ✅ RESUELTO (2026-09-11) — `BLL/Pedido.cs:380-390` — `catch` mudo (sin loguear) que traga cualquier excepción al validar reserva de Lista de Espera**, inconsistente con el catch hermano de `CrearPedido` (30 líneas antes) que sí loguea el mismo tipo de degradación.
*Solución aplicada:* ahora loguea vía `Trace.TraceWarning`, mismo criterio que el catch hermano.

**7. ✅ RESUELTO (2026-09-11) — `BLL/Promocion.Modificar` (121-158) es código muerto** (sin caller en GUI ni tests) y, de usarse, no tiene guarda de estado (podría modificar una promoción ya `Vigente` sin repasar por Contabilidad) ni reutiliza la validación de `CrearInterna` (duplicada, riesgo de divergencia).
*Solución aplicada:* se extrajo `ValidarCamposComunes(...)` compartida por `CrearInterna` y `Modificar`; se agregó el guard de estado (`!PuedeAprobarseORechazarseContable()` → `err.bll.promocion.modificar_estado`); se agregaron 4 tests (ver §9 #7). Sigue sin ningún caller en GUI — el código muerto en sí no se resolvió (ninguna pantalla ofrece "editar promoción"), pero ya no tiene los dos riesgos de diseño que tenía el método.

**8. ✅ RESUELTO (2026-09-11) — `BLL/Backup.cs` (`ExtraerAutorDeNombre`) no reconoce el prefijo `"WardrobeFlow_Inicial_"`** de los backups iniciales — la UI siempre muestra autor desconocido para ellos.
*Solución aplicada:* ahora reconoce ambos prefijos (`"WardrobeFlow_Backup_"`/`"WardrobeFlow_Inicial_"`) vía un array `PrefijosConocidos` + `FirstOrDefault`.

### 🟢 Baja

9. ✅ RESUELTO (2026-09-11) — Comentario desactualizado en `DecisionRenovacion.cs:11` (habla de "Pausar" como hipotético cuando ya es un valor real implementado).
*Solución aplicada:* comentario actualizado.
10. ✅ RESUELTO (2026-09-11) — `BLL/RecuperacionAdmin.cs` tiene dos métodos sin ningún caller en todo el repo (`ContarClavesDisponibles`, `ValidarEsAdministrador`).
*Solución aplicada:* `ValidarEsAdministrador` (dead code real) se eliminó; `ContarClavesDisponibles` se documentó como intencionalmente sin uso por ahora (diseñado para una futura pantalla de estado) y además ganó test (ver §9 #5).

---

## 3. Seguridad + Servicios

**Alcance:** `Seguridad/` completa, `Servicios/` completa (Multiidioma, Exportación). Incluye verificación de las 4 líneas nuevas sin commitear en `traducciones.tsv`.

Contexto: `SessionManager.TienePermiso` con `OrdinalIgnoreCase` — verificado resuelto. Riesgo de DNI irrecuperable por `key.dat` — documentado, decisión ya tomada, no se repite.

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — El DNI del cliente se loguea en TEXTO PLANO en la Bitácora, evadiendo el cifrado.**
*Solución aplicada:* los 4 mensajes de bitácora de `BLL/Cliente.cs` (Alta/Modificar/Baja) ya no incluyen el DNI en texto — usan `IdCliente`, que ya identifica el registro sin exponerlo.
`BLL/Cliente.cs:56`: `bitacora.Registrar(..., $"Alta Cliente: {cliente.NombreCompleto} (DNI {cliente.DNI})", ...)`. Todo el trabajo de `Encriptador` para proteger el DNI en la tabla `Cliente` queda anulado: el mismo DNI queda en claro y searcheable en la tabla de auditoría, que típicamente tiene menos controles de acceso. Un Auditor (rol con acceso legítimo a Bitácora) puede leer DNIs en claro sin pasar por `Encriptador`.
*Sugerencia:* no incluir el DNI completo en el `Detalle`; usar el `Id` o enmascarar (`***1234`).

### 🟡 Media

**2. ✅ RESUELTO (2026-09-11) — `SessionManager.cs` — Singleton sin `volatile`, inconsistente con `ContadorSesion`** (que sí lo hace y lo documenta). Antipatrón de doble-checked locking sin `volatile` en el punto de entrada más usado de BLL/GUI.
*Solución aplicada:* campo `_session` ahora `volatile`.

**3. `GeneradorCredenciales` — contraseñas y claves de emergencia en `.txt` sin cifrar**, en carpeta predecible (`Mis Documentos\WardrobeFlow\CredencialesGeneradas\`), acumulándose indefinidamente sin borrado ni expiración. *(pendiente — requiere decisión de producto sobre almacenamiento cifrado, se deja para una pasada aparte)*

**4. ✅ RESUELTO (2026-09-11) — `SerializadorCsv.Escapar` — sin mitigación de CSV/Formula Injection** (CWE-1236): una celda que empiece con `=`/`+`/`-`/`@` queda tal cual, interpretable como fórmula por Excel al abrir el export.
*Solución aplicada:* `Escapar` ahora prepende `'` para campos que empiecen con `=+-@` o tab, vía el array `PrefijosFormula`; 5 tests nuevos de CSV-injection.

**5. ✅ RESUELTO (2026-09-11) — ~40 claves `err.bll.*`/`msg.*`/`col.*` de Contratación/Lista de Espera/Promociones usadas en BLL no existen en `traducciones.tsv`.** Un usuario con UI en EN/RU/PT ve estos mensajes de error específicos en español (fallback hardcodeado), rompiendo la promesa de 100% multiidioma para estos módulos nuevos.
*Solución aplicada:* se agregaron 41 claves faltantes (Contratación 5, Lista de Espera 15, Promoción 15, SugerenciaPromocion 4, Pedido 1, más 1 corrección de ancla) × 4 idiomas, detectadas diffeando las claves usadas en código contra la columna ES del TSV.

### 🟢 Baja

6. Claves huérfanas documentadas en el comentario de `Traductor.cs` (`mnu.usuarios`, `mnu.bitacora`) que ya no se usan en el código actual. *(el comentario en sí se corrigió — ver la nota del commit de Seguridad+Servicios — pero el hallazgo de fondo, claves huérfanas reales en el TSV, no se investigó a fondo; queda pendiente si se quiere ir más allá de la corrección del comentario)*
7. Las 4 líneas nuevas de `traducciones.tsv` (`mnu.ventana.cerrartodas`) verificadas explícitamente: esquema, duplicados y completitud por idioma — todo OK, sin hallazgo.
8. `Encriptador` usa AES-128-CBC sin HMAC/AEAD — desviación de la práctica recomendada (cifrar-y-autenticar), riesgo bajo (requiere acceso directo a BD). *(pendiente — decisión de mayor alcance, documentada como diferida)*
9. ✅ RESUELTO (2026-09-11) — `CifradorArchivos` no limpia el archivo de salida si `Cifrar`/`Descifrar` fallan a mitad de camino — puede dejar un `.wfbak` corrupto con nombre "definitivo".
*Solución aplicada:* ambos métodos quedaron envueltos en try/catch que llama a `BorrarSiExiste(ruta)` antes de re-lanzar.
10. ✅ RESUELTO (2026-09-11) — `GeneradorCredenciales.cs:129` — `catch { }` sin log, rompe la convención del resto de la capa.
*Solución aplicada:* ahora loguea vía `Trace.TraceWarning`.
11. ✅ RESUELTO (2026-09-11) — `Encriptador.ConvertirBase64` — wrapper público trivial sin uso externo.
*Solución aplicada:* cambiado de `public` a `private` (ya no expone superficie sin uso).
12. `ContadorSesion` de intentos de login es global al proceso (no por usuario) — documentado como intencional para app de escritorio mono-usuario.

---

## 4. GUI — Dashboards, Análisis, Reportes, Historiales, Bitácora, Alertas

**Alcance:** 16 pares de pantallas.

Contexto: el criterio de urgencia/mantenimiento entre los 5 dashboards (objeto de la auditoría anterior) **verificado correctamente centralizado**, sin regresión. Las divergencias nuevas están en otras dimensiones (navegación, tema, traducción).

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — 9 de 16 pantallas no heredan `FormBase`** → ignoran tema oscuro/tamaño de fuente del usuario y duplican a mano la carga de ícono (8 pantallas); **`AlertasForm` directamente se olvidó de copiar el bloque** y abre con el ícono por defecto de .NET.
*Solución aplicada:* las 9 pantallas (5 dashboards, `ReporteJornadaForm`, `VersionHistorialForm`, `MantenimientoHistorialForm`, `AlertasForm`) ahora heredan `FormBase`; se removió el código de ícono duplicado.

**2. ✅ RESUELTO (2026-09-11) — `ReporteJornadaForm` muestra KPIs sensibles (incl. "días sin backup") a roles sin permiso de Administrador** — el parámetro `permisos` se recibe en el constructor pero nunca se usa; `DashboardForm` sí filtra ese mismo KPI por permiso granular. Auditor/Supervisor pueden ver datos que el Dashboard trata como exclusivos de Administrador.
*Solución aplicada:* mismo criterio que `DashboardForm` (`_verPrendas`/`_verClientes`/`_verBackup` por `NombreMenu`) — los 3 KPIs sensibles ahora se ocultan y ni siquiera se consultan a BLL si el usuario no tiene el permiso.

**3. ✅ RESUELTO (2026-09-11) — `DashboardSupervisor` es el único de los 4 dashboards con Kanban sin navegación por clic en sus tarjetas** — ni siquiera existe el método `HabilitarClic...` que sí tienen sus 3 hermanos.
*Solución aplicada:* se agregaron los 3 helpers `HabilitarClicAbrir*` (Pedidos→PedidosVenta, Mantenimiento→Prendas, Bitácora→Bitacora), mismo patrón que los otros dashboards.

**4. ✅ RESUELTO (2026-09-11) — `MantenimientoHistorialForm` no implementa `IIdiomaObserver`**, a diferencia de sus 2 hermanas de Historial — no se actualiza si el usuario cambia el idioma con la ventana abierta.
*Solución aplicada:* ahora implementa `IIdiomaObserver` y se suscribe/desuscribe en `OnLoad`/`OnFormClosing`, mismo patrón que `PedidoHistorialForm`/`VersionHistorialForm`; de paso, la carga de datos se movió del constructor a `OnLoad`.

**5. ✅ RESUELTO (2026-09-11) — `VersionHistorialForm.btnRestaurar` nunca se deshabilita sin selección**, a diferencia de `PedidoHistorialForm.btnRestaurar` (mismo propósito).
*Solución aplicada:* `btnRestaurar.Enabled = false` por diseño + `Dgv_SelectionChanged`, mismo patrón que `PedidoHistorialForm`.

**6. ✅ RESUELTO (2026-09-11) — `VersionHistorialForm` es la única de las 3 pantallas de Historial sin botón para cerrarla** (ni `CancelButton` asignado).
*Solución aplicada:* se agregó `btnCerrar` (panel inferior junto a Restaurar) con `this.CancelButton = btnCerrar`, traducido en los 4 idiomas.

### 🟡 Media

7. Boilerplate de layout/paint casi idéntico duplicado 4-5 veces entre los 5 dashboards (mismo origen que el bug de umbrales de la auditoría anterior, ahora en pintado/layout). *(pendiente — extracción de mayor alcance)*
8. ✅ RESUELTO (2026-09-11) — `lblSub` (subtítulo del header) nunca se traduce en ninguno de los 5 dashboards — queda fijo en español con cualquier idioma activo.
*Solución aplicada:* `lblSub.Text = Tr(...)` en los 5 dashboards, con 5 claves nuevas `dash.*.subtitulo`.
9. ✅ RESUELTO (2026-09-11) — `DashboardForm` hardcodea `"Sistema (30d)"` fuera del sistema de traducción, rompiendo el patrón que el resto del archivo sí sigue.
*Solución aplicada:* nueva clave `dash.stats.sistema30d`.
10. Tres patrones distintos de "cuándo cargo mis datos por primera vez" (`OnLoad` override vs. evento `Load` del Designer vs. directo en el constructor) entre las 16 pantallas. *(pendiente — unificación de mayor alcance)*
11. Manejo de excepciones inconsistente: desde `FormBase.MostrarError` (traducido + auditado) hasta `AlertasForm` mostrando `ex.Message` crudo sin traducir directo en una tarjeta "Crítica". *(parcial: el caso puntual de `AlertasForm` ya se resolvió como parte del hallazgo Alta #1 de esta sección — ahora usa un mensaje genérico traducido y logueado en bitácora; la inconsistencia de fondo entre las 3 implementaciones de `MostrarError` del repo sigue pendiente, ver §10 transversal #5)*
12. Carga de datos síncrona en el hilo de UI sin indicador de progreso en casi todas las pantallas fuera de los 5 dashboards (que sí usan `Task.Run`). *(pendiente)*
13. ✅ RESUELTO (2026-09-11) — `Font` recreado sin `Dispose` en cada refresco periódico (cada 2 min) en 2 dashboards — fuga de handles GDI en sesiones largas.
*Solución aplicada:* `DashboardForm`/`DashboardControlStock` ahora reusan campos `Font` estáticos en vez de crear uno nuevo en cada refresco.
14. `DashboardControlStock`: la tarjeta KPI "En mantenimiento" (por cantidad) y el Kanban debajo (por antigüedad) usan criterios de alerta distintos en la misma pantalla, pueden contradecirse visualmente. *(no se cambió el comportamiento — es una decisión de negocio válida usar dos criterios distintos para dos preguntas distintas — se agregó un comentario aclaratorio en el código para que no se lea como un descuido)*

### 🟢 Baja

15. ✅ RESUELTO (2026-09-11) — `DashboardVendedor` nombra su panel Kanban distinto a sus 3 hermanos (`kanbanWrapper` vs `wrapper`).
*Solución aplicada:* renombrado a `wrapper` en el Designer.
16. Convención de nombres de event handlers distinta entre las 3 pantallas de Historial (PascalCase vs camelCase). *(pendiente)*
17. Contenedores de columna con nombres genéricos `col1`/`col2`/`col3` en los 4 dashboards con Kanban. *(pendiente)*
18. Cero controles `ToolTip` en las 16 pantallas revisadas (incl. un botón solo-ícono "⚙" sin texto ni tooltip). *(parcial 2026-09-11: el botón "⚙" de `DashboardForm` ya tiene tooltip; el resto de las 16 pantallas sigue sin ninguno)*
19. ✅ RESUELTO (2026-09-11) — `AutoScaleDimensions`/`AutoScaleMode` configurados de forma distinta entre las 3 pantallas de Historial.
*Solución aplicada:* `PedidoHistorialForm` (la única que no los declaraba) ahora los declara igual que las otras dos, sin cambio de comportamiento visible.
20. ✅ RESUELTO (2026-09-11) — Botones "Exportar a PDF/CSV" nunca se deshabilitan sin datos generados (el guard llega recién al clic).
*Solución aplicada:* en las 6 pantallas con botones dedicados de exportación (los 4 de Análisis, Recomendación de Prendas, Ventas por Vendedor), `btnExportarPdf`/`btnExportarCsv` arrancan deshabilitados y se habilitan solo cuando `BtnGenerar_Click` produce resultados.

---

## 5. GUI — Pedidos, Prendas, Clientes (operación diaria)

**Alcance:** 13 pantallas/diálogos.

Contexto: el patrón Command para Cancelación/Devolución de Pedido (fix de la auditoría anterior) **sigue vigente y correctamente implementado, sin bypass** — verificado end-to-end.

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — Condición de carrera real sobre `Pedido`.** `PedidosVenta`/`PedidosRealizados` actúan sobre el objeto cacheado en memoria (sin re-consultar BD) y el `UPDATE` en `DAL/Pedido.cs` no valida `WHERE Estado=@EstadoEsperado`. Dos operadores trabajando casi simultáneamente pueden pisarse (despachar un pedido que otro acaba de cancelar). El propio archivo tiene un caso (`BtnDevolucion_Click`) que sí re-consulta antes de actuar — evidencia de que es un descuido puntual, no una decisión consistente.
*Solución aplicada:* los 4 handlers (Cancelar/DesCancelar en PedidosVenta, Despachar/Entregar en PedidosRealizados) ahora re-consultan `pedidoBLL.ObtenerPorId(...)` antes de actuar, mismo criterio que ya usaba `BtnDevolucion_Click`.

**2. ✅ RESUELTO (2026-09-11) — Selección múltiple habilitada por defecto pero silenciosamente ignorada en 6 grillas** (Pedidos, Prendas, Clientes, Lista de Espera, Inspección de Devolución): ninguna fija `MultiSelect = false`, y todo el código toma `SelectedRows[0]` sin avisar que el resto de la selección se ignoró.
*Solución aplicada:* `MultiSelect = false` agregado a las 6 grillas (`dgvPedidos` x2, `dgvPrendas` x2, `dgvClientes`, `dgvListaEspera`).

**3. ✅ RESUELTO (2026-09-11) — `PrendaForm` no hereda `FormBase`**: reimplementa el manejo de errores a mano y expone el mensaje técnico crudo de excepciones inesperadas (ej. SQL) directo al usuario, sin registrar nada en bitácora — exactamente lo que `FormBase.MostrarError` existe para evitar.
*Solución aplicada:* `PrendaForm` ahora hereda `FormBase` (con `MensajeLabel => lblMensaje`) y usa `MostrarError(ex)` heredado en vez del catch manual.

### 🟡 Media

4. ✅ RESUELTO (2026-09-11) — Regla "EnUso→Baja solo por el flujo dedicado de pérdida" impuesta únicamente en la GUI (`Prendas.cs`, un `continue` en una lista de opciones), sin respaldo en BLL/BE — cualquier código futuro que llame `CambiarEstado` directo la saltea.
*Solución aplicada:* `IPrendaService.CambiarEstado`/`BLL.Prenda.CambiarEstado` ganaron un parámetro `bool viaFlujoPerdida = false`; ahora lanza `err.bll.prenda.baja_requiere_flujoperdida` si se intenta `EnUso→Baja` sin pasar por el flujo dedicado, sin importar quién llame — la regla vive en BLL, no solo en la GUI.
5. Cobro + baja como dos llamadas BLL no atómicas, repetido en 3 lugares distintos con el mismo riesgo aceptado pero sin ningún helper común de compensación. *(pendiente — requiere diseñar el helper de compensación, mayor alcance)*
6. Tres implementaciones distintas de "diálogo simple" en vez de reusar `GUI.InputDialog` — una de ellas con botones hardcodeados sin traducir ("OK" en inglés fijo). *(parcial 2026-09-11: `SeleccionarClienteDialog` en `Prendas.cs` — el que tenía "OK"/"Cancelar" sin traducir — ahora usa `btn.aceptar`/`btn.cancelar`; sigue sin consolidarse en un componente compartido porque usa un `ComboBox`, no un `TextBox`, y no encaja directamente en `InputDialog`)*
7. ✅ RESUELTO (2026-09-11) — Feedback de validación inconsistente entre diálogos modales similares (`CargoPrendaDialog`/`CambioEstadoDialog` sin color/ícono de error, a diferencia de `InspeccionDevolucionForm` que sí hereda `FormBase`).
*Solución aplicada:* verificado que ambos ya tenían `lblMensaje.ForeColor = DarkRed` en el Designer — el color no faltaba. Lo que sí faltaba era el prefijo "✗ " que usa `FormBase.MostrarError`; se agregó en los 3 call sites de validación.
8. ✅ RESUELTO (2026-09-11) — Errores al cargar detalle de pedido se silencian por completo (solo `Trace`, sin `MostrarError` ni auditoría) en `PedidosVenta`/`PedidosRealizados`.
*Solución aplicada:* ambos catches ahora también llaman a `MostrarError(ex)`, no solo `Trace.TraceError`.
9. Alcance inconsistente del patrón Command dentro del mismo agregado `Pedido`: Cancelar/Devolución pasan por Command, pero Despachar/MarcarEntregado/DesCancelar llaman a BLL directo, sin que el criterio esté documentado. *(pendiente — decisión de diseño, no un bug puntual)*
10. ✅ RESUELTO (2026-09-11) — Feedback "Procesando..." que probablemente nunca se pinta en `NuevoPedidoForm` (falta `Refresh()`/`DoEvents()` antes de la llamada sincrónica).
*Solución aplicada:* se agregó `this.Refresh()` antes de la llamada sincrónica a `CrearPedido`.

### 🟢 Baja

11. ✅ RESUELTO (2026-09-11) — Handler de evento vacío (código muerto) en `ListaEsperaForm_Load`.
*Solución aplicada:* eliminado el handler vacío y su cableado en el Designer.
12. ✅ RESUELTO (2026-09-11) — `ExploradorCompositeForm` tiene un chequeo de tipo redundante que no aprovecha la interfaz uniforme del Composite (funciona igual sin él).
*Solución aplicada:* se verificó que `BE.Patente.Hijos` devuelve lista vacía (nunca lanza) y se eliminó el `if (esFamilia)` redundante alrededor de la recursión.
13. Verbos inconsistentes para "confirmar/continuar" entre pantallas (Aceptar/Confirmar/OK, uno de ellos sin traducir). *(parcial 2026-09-11: el caso sin traducir era el mismo "OK" de `SeleccionarClienteDialog` ya corregido en §5 #6; la inconsistencia de fondo — qué verbo usar en cada pantalla — es una decisión de copywriting/rename de alcance amplio, se deja para una pasada aparte)*
14. Emojis de estado/urgencia hardcodeados como literales en la lógica de presentación en vez de una capa de estilos. *(pendiente)*

---

## 6. GUI — Usuarios, Login, Seguridad, Acceso

**Alcance:** 12 pantallas del módulo más sensible del sistema.

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — `OlvideContrasenaForm.cs:96-110` — enumeración de usuarios.** Mensaje distinto según si el username existe ("No se encontró..." vs "Usuario encontrado") — reintroduce el antipatrón que `Login.cs` evita explícitamente por diseño (mismo mensaje exista o no el usuario). Reduce drásticamente el espacio de ataque para fuerza bruta/ingeniería social.
*Solución aplicada:* ahora muestra siempre el mismo mensaje neutro ("Si el usuario existe, contactá al administrador..."), sin distinguir la respuesta visible según `SolicitarRecuperacionClave` devuelva true/false.

**2. ✅ RESUELTO (2026-09-11) — `ManejadorSeguridad.cs:92-95` — fail-open en la aplicación de seguridad de controles.** Si cualquier excepción ocurre a mitad del `foreach` que oculta/muestra controles según permiso, el catch la traga completa: los controles no procesados quedan con su estado previo (potencialmente visibles), sin aviso a nadie.
*Solución aplicada:* cada mapeo ahora se aplica en su propio try/catch (no un solo catch para todo el `foreach`), así un mapeo que falla no deja sin procesar al resto; el log de error ahora incluye formulario+control.

**3. ✅ RESUELTO (2026-09-11) — `ManejadorSeguridad.cs:108-112` — ocultar un control no es equivalente a deshabilitarlo.** La única acción de seguridad a nivel de control es `Visible = false`, nunca `Enabled = false` — base frágil para la única capa de control de acceso a nivel de UI del sistema.
*Solución aplicada:* se agregó `SetEnabled` y ahora se aplican tanto `Visible` como `Enabled` (y su reverso al re-mostrar controles ya no mapeados).

### 🟡 Media

4. ✅ RESUELTO (2026-09-11) — `GestorPermisos.CrearSubRol` — si `AgregarComponente` falla tras `CrearRol` exitoso, el rol queda persistido como raíz huérfana, sin rollback visible ni aviso de que quedó a medias.
*Solución aplicada:* `CrearSubRol` envuelve `AgregarComponente` en try/catch; si falla, intenta `EliminarComponente(nuevoId)` como rollback, y si el rollback también falla muestra explícitamente `perm.err.subrol_huerfano` en vez de un error genérico.
5. `ConfirmarAdminForm` (gate de la Clave Maestra) sin límite de intentos visible en la GUI — vulnerable a fuerza bruta sin fricción sobre el secreto más crítico del sistema si la BLL tampoco lo limita. *(pendiente)*
6. ✅ RESUELTO (2026-09-11) — `DesbloqueoEmergenciaForm` — el campo de clave de emergencia se muestra en **texto plano** (sin `PasswordChar`), a diferencia de todos los demás campos de contraseña del sistema.
*Solución aplicada:* `txtClave` ahora enmascarado por defecto (`PasswordChar = '●'`), con un botón `btnMostrarClave` para alternar, mismo mecanismo que ya usa `Login.cs`.
7. ✅ RESUELTO (2026-09-11) — `CambioClaveObligatorioForm` sin ninguna validación de vacío/fortaleza en la GUI antes de invocar BLL — depende 100% de que la BLL nunca falle en aplicar la regla.
*Solución aplicada:* `Confirmar()` ahora llama a `_usuarioBLL.ValidarContrasena(txtNueva.Text)` (existía en BLL sin ningún caller) más un chequeo de vacío, para feedback inmediato.
8. `GestorPermisos` sin resguardo visible contra que un usuario modifique/potencie el rol al que él mismo pertenece (auto-escalación de privilegios), a diferencia de la protección de "último administrador" que sí existe para usuarios puntuales. *(pendiente)*
9. `DiagnosticoIntegridadForm` no distingue en pantalla los dos problemas ya conocidos de auditorías previas (DNI sin descifrar, duplicados de clientes) — todo se reduce a un genérico "DV inválido" por tabla. *(pendiente)*

### 🟢 Baja

10. ✅ RESUELTO (2026-09-11) — `Usuarios.PedirTexto` — método completo sin ningún caller (código muerto duplicado).
*Solución aplicada:* eliminado (confirmado cero callers en todo el repo antes de borrar).
11. Panel completo de "Alta de Usuario" oculto (`Visible=false`) pero funcionalmente vivo en `Usuarios.cs`, con una lista de roles obsoleta (`"Supervisor"`) — dos implementaciones de alta mantenidas en paralelo. *(pendiente — requiere decisión de producto sobre cuál alta es la vigente)*
12. ✅ RESUELTO (2026-09-11) — `btnEliminar`/`BtnEliminar_Click` nombra "Eliminar" una acción que en realidad archiva (soft-delete) — el texto visible sí dice "Archivar", pero el nombre en código no.
*Solución aplicada:* renombrado a `btnArchivar`/`BtnArchivar_Click` en `.cs` y `.Designer.cs` (confirmado sin referencias cruzadas antes del rename).
13. Sufijo `Form` no uniforme en las clases (`Login`, `Usuarios`, `GestorPermisos` no lo llevan) — justo el trío más crítico del módulo. *(pendiente — rename de alcance amplio)*
14. ✅ RESUELTO (2026-09-11) — Typo "Selecioná" (falta una c) en dos labels de `Usuarios.Designer.cs`.
*Solución aplicada:* corregido a "Seleccioná" en los 4 fallbacks hardcodeados de `Usuarios.Designer.cs`/`NuevoPedidoForm.Designer.cs` (el TSV ya estaba bien escrito).
15. ✅ RESUELTO (2026-09-11) — Fraseo no unificado de "credenciales inválidas" entre `Login` y `ConfirmarAdminForm`.
*Solución aplicada:* `ConfirmarAdminForm` reusa el mismo fraseo base que `Login` ("Usuario o contraseña incorrectos"), agregando la cláusula específica de admin.
16. Indicador visual del toggle mostrar/ocultar contraseña en `Login` (tachado de fuente) poco convencional comparado con el patrón usual de ícono de ojo. *(pendiente — rediseño cosmético)*

---

## 7. GUI — Promociones, Cobro/Renovación de Suscripción, Contrataciones

**Alcance:** 10 pantallas que manejan dinero.

Contexto: el criterio de "promoción vigente" es consistente entre las 5 pantallas de Promociones — verificado, ninguna lo calcula por su cuenta en la GUI.

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — `RenovacionSuscripcionForm` — el "Estado actual" mostrado tras Procesar/Reanudar queda desactualizado.** Sigue leyendo el objeto cliente cacheado del combo en vez de recargarlo desde BLL tras la acción — un vendedor puede pensar que "Reanudar" no se habilitó, cuando la pausa sí se aplicó en base.
*Solución aplicada:* `MostrarEstadoActual` ahora acepta el `BE.Cliente` recién releído/mutado por la cadena de Manejadores; ambos handlers se lo pasan explícitamente en vez de releer `cmbCliente.SelectedItem`.

**2. ✅ RESUELTO (2026-09-11) — `CobroSuscripcionForm`/`RenovacionSuscripcionForm` — cero confirmación antes de procesar**, incluyendo la opción "Dar de baja" (irreversible desde la UI). Contrasta con el resto de pantallas del mismo alcance, que sí confirman antes de acciones sensibles equivalentes.
*Solución aplicada:* ambas pantallas piden confirmación (`MessageBox` Sí/No) antes de `Procesar`; en Renovación, "Dar de baja" tiene texto y default de botón distintos (irreversible) del resto de las decisiones.

**3. ✅ RESUELTO (2026-09-11) — `ContratacionesPendientesForm` nunca muestra el monto a cobrar** — ni en la grilla ni en el diálogo de confirmación de cobro. `BE.Contratacion` ni siquiera tiene el campo. Caja confirma un cobro real sin ver el importe en pantalla.
*Solución aplicada:* se agregó `BE.Contratacion.MontoPlan` (JOIN con `PlanSuscripcion.Precio` en `DAL.Contratacion`), y ahora se muestra en la grilla (columna "Monto") y en el texto de confirmación del cobro.

### 🟡 Media

4. Formato de moneda inconsistente entre pantallas: `N2` explícito, `ToString()` por defecto (decimales variables), y binding crudo sin formato — tres representaciones del mismo concepto en 10 pantallas. *(pendiente — refactor transversal, se deja para una pasada aparte)*
5. Dos patrones de multiidioma distintos conviviendo (Tag+diccionario genérico vs. hardcodeo dentro de `Traducir()`) — justo en las dos pantallas donde aparece el problema de formato de moneda. *(pendiente — refactor de mayor alcance)*
6. ✅ RESUELTO (2026-09-11) — `cmbMedioPago` con strings hardcodeados nunca traducidos, la única lista de opciones así en las 10 pantallas.
*Solución aplicada:* nueva clase `MedioPagoItem` (Value/Label, mismo patrón que `Usuarios.PerfilItem`), poblada desde el idioma actual en `CargarMediosPago()`.
7. ✅ RESUELTO (2026-09-11) — `cmbModalidad` poblado de dos formas distintas (hardcodeado vs. `Enum.GetValues`) entre pantallas hermanas — un cuarto valor de enum a futuro solo se reflejaría en una de las tres.
*Solución aplicada:* `CobroSuscripcionForm`/`RenovacionSuscripcionForm` pasan a `Enum.GetValues(typeof(ModalidadCobro))`, igual que ya hacía `NuevaContratacionForm`.
8. ✅ RESUELTO (2026-09-11) — Clave de traducción compartida entre pantallas no relacionadas (`"renov.modalidad"` reusada en `CobroSuscripcionForm`), acopla el copy de ambas.
*Solución aplicada:* `CobroSuscripcionForm` usa su propia clave `"cobro.modalidad"`.
9. ✅ RESUELTO (2026-09-11) — `DateTimePicker.Format = Short` depende del locale del SO en 3 controles, mientras las etiquetas de solo-lectura de las mismas pantallas fuerzan `dd/MM/yyyy` explícito — riesgo real de leer mal una fecha límite.
*Solución aplicada:* `Format = Custom` + `CustomFormat = "dd/MM/yyyy"` en `AltaPromocionForm` (dtpInicio/dtpFin) y `RenovacionSuscripcionForm` (dtpPausaHasta).
10. ✅ RESUELTO (2026-09-11) — El manejo de error en `catch` no refresca el estado mostrado en ninguna de las dos pantallas de Cobro/Renovación — si la falla ocurrió tras una escritura parcial, la pantalla no lo refleja.
*Solución aplicada:* los catch de `BtnProcesar_Click` (ambas pantallas) y `BtnReanudar_Click` (Renovación) ahora refrescan el estado mostrado después de mostrar el error (best-effort, envuelto en su propio try/catch para no tapar el error ya mostrado).
11. Convención de nombres de campos de servicio BLL inconsistente (`promocionBLL` vs `_bllCliente`) dentro del mismo conjunto de 10 pantallas. *(pendiente — rename cosmético de riesgo amplio, se deja para una pasada aparte)*

### 🟢 Baja

12. ✅ RESUELTO (2026-09-11) — Clase `ClienteItem` duplicada literalmente entre `CobroSuscripcionForm` y `RenovacionSuscripcionForm`.
*Solución aplicada:* extraída a `GUI/ClienteItem.cs`.
13. ✅ RESUELTO (2026-09-11) — `AltaPromocionForm` no valida en cliente rango de fechas ni tope de porcentaje antes de enviar (sí lo hace BLL, pero el feedback es tardío y genérico).
*Solución aplicada:* `BtnConfirmar_Click` valida ambas reglas del lado del cliente antes de invocar al BLL, reusando las claves de traducción ya existentes.
14. ✅ RESUELTO (2026-09-11) — `btnRefrescar` sin texto traducible ni tooltip, único caso así entre las acciones del conjunto.
*Solución aplicada:* ToolTip traducible (`tip.actualizar`) agregado en las 4 pantallas afectadas (PromocionesVigentes/Administracion/Contabilidad, ContratacionesPendientes).
15. Eventos `CheckedChanged` cableados a un solo radio button del par en varios formularios (acoplamiento implícito). *(revisado 2026-09-11, sin cambios: el radio "apagado" del par dispara su propio `CheckedChanged` al descheckearse — todos los handlers leen `.Checked` del control, no `sender` — así que el control dependiente siempre queda en el estado correcto sin importar cuál de los dos se clickee; es cosmético, no un bug)*

---

## 8. GUI — Infraestructura (FormBase, Menu, Program, Tema, Exportación)

**Alcance:** componentes base compartidos por toda la app, incluido `GUI/Menu.cs` (cambios sin commitear).

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — Exportación CSV/TXT sin manejo de errores de I/O.** `ExportadorCsv`/`ExportadorTxt` llaman `File.WriteAllText` sin try/catch, y ninguno de los 10 call-sites lo envuelve tampoco — un archivo abierto en Excel o un disco lleno cae en el handler global genérico, sin decirle al usuario la causa real (contrasta con `BackupForm`, que sí da contexto específico).
*Solución aplicada:* ambos exportadores envuelven el `File.WriteAllText` en try/catch, distinguiendo `IOException` (archivo en uso/disco lleno) y `UnauthorizedAccessException` (sin permisos), con mensaje específico traducido.

**2. ✅ RESUELTO (2026-09-11) — `FormIdiomas` (la pantalla de gestión de idiomas) tiene sus propios textos hardcodeados y nunca traducidos** — encabezados de sus 3 grillas y valores "Sí"/"No" en español fijo, con el agravante de que es la propia pantalla de administración de idiomas.
*Solución aplicada:* los encabezados de las 3 grillas y los valores "Sí"/"No" ahora pasan por `Tx(...)`; `UpdateLanguage` reconstruye las grillas (antes solo traducía labels/botones).

**3. ✅ RESUELTO (2026-09-11) — `BackupForm`/`InputDialog` no heredan `FormBase`** → ignoran tema oscuro/fuente del usuario y duplican a mano el mismo snippet de carga de ícono que ya vive en `FormBase`.
*Solución aplicada:* ambos ahora heredan `FormBase`; se removió el código de ícono duplicado.

### 🟡 Media

4. ✅ RESUELTO (2026-09-11) — El nuevo `cerrarTodasLasVentanasToolStripMenuItem_Click` en `Menu.cs` (cambio sin commitear) es funcionalmente correcto y sigue el patrón del archivo, pero cierra todas las ventanas MDI **sin ninguna confirmación previa**, a diferencia de "Cerrar sesión" (acción mucho menos destructiva) que sí la pide.
*Solución aplicada:* ahora pide confirmación con el mismo diálogo Sí/No que "Cerrar sesión" (`ConfirmarCerrarSesion` se generalizó a `ConfirmarSiNo(titulo, mensaje)`, compartido por ambas acciones).
5. ✅ RESUELTO (2026-09-11) — Backup/Restore corren síncronos en el hilo de UI, sin cursor de espera ni deshabilitar botones — con BD grande, la ventana puede marcarse "No responde".
*Solución aplicada:* `BackupForm` ejecuta backup/backup inicial/restauración en `Task.Run`, con cursor de espera y controles deshabilitados mientras corre.
6. ✅ RESUELTO (2026-09-11) — Mini-diálogos ad-hoc duplicados (`Menu.ConfirmarCerrarSesion`, `FormIdiomas.Pedir`) en vez de reusar `InputDialog`, que existe explícitamente para esto.
*Solución aplicada:* `ConfirmarCerrarSesion` se generalizó a `ConfirmarSiNo` (diálogo de confirmación Sí/No, no aplica `InputDialog` que es de entrada de texto — ver #4). `FormIdiomas.Pedir` ahora delega en `InputDialog` en vez de armar un `Form` a mano.
7. ✅ RESUELTO (2026-09-11) — El rosa de marca (`210,100,135`) sigue hardcodeado en 25 archivos pese a existir `Tema.RosaPrimario`/`EstiloFormulario.Rosa` para centralizarlo — la migración "de a poco" documentada en el propio `Tema.cs` no avanzó sobre el código existente.
*Solución aplicada:* los 25 archivos migraron a `Tema.RosaPrimario`; se eliminaron además las constantes `RosaPrimario` locales y duplicadas de `AdministracionUsuariosForm` y `GestorPermisos`.

### 🟢 Baja

8. Los ítems del `Menu` aparecen "mapeables" en `GestorPermisos` pero sin ningún efecto real (`Menu` se gobierna solo por `MenuVisibilidad`) — diseño intencional, pero la UI de mapeo no lo distingue y puede confundir a un admin. *(pendiente)*
9. Textos hardcodeados menores fuera del sistema de traducción (título de un `OpenFileDialog`, filtros de archivo en inglés). *(parcial: el título del `OpenFileDialog` de "Desde archivo..." en `BackupForm` ahora usa el sistema de traducción; los filtros de archivo restantes no son en realidad texto en inglés — no se encontró más pendiente en los 4 archivos que usan `OpenFileDialog`/`SaveFileDialog`)*

**Verificado sin hallazgos:** `Program.cs` sí tiene handler global de excepciones no controladas; el diff de `Menu.cs` no tiene código de debug ni nada a medio hacer, y su traducción está completa en los 4 idiomas; `BackupForm.Restaurar()` maneja bien el error de restauración (el único gap real es la falta de hilo en background, ítem #5).

---

## 9. Tests (parcial — ver nota al final)

**Alcance cubierto en profundidad:** `Pedido`, `Cliente`, `Cobro`, `Renovación`, `Promoción` (incl. cruce contra el código BLL real) + un bloque adicional de Seguridad/Usuarios/Permisos.

### 🔴 Alta

**1. ✅ RESUELTO (2026-09-11) — `BLL.Cobro`/`BLL.Renovacion` (las clases fachada reales) nunca se instancian en los tests.** Todos los tests arman los `Manejadores.*Handler` sueltos y **reconstruyen a mano** el orden de la cadena en el propio test — si alguien invierte el orden real en el constructor de producción, ningún test lo detecta. El guard de entrada `PermisosAccion.Exigir` y el chequeo `sin_plan` de ambas clases quedan sin ningún test.
*Solución aplicada:* se agregaron tests que instancian `BLL.Cobro`/`BLL.Renovacion` reales (con Fakes) y ejercitan `Procesar(...)` de punta a punta con la cadena real, más los guards `sin_sesion`/`sin_plan` (8 tests nuevos entre ambos archivos). De paso se corrigió el propio constructor de `BLL.Renovacion`, que tomaba `DAL.PlanSuscripcion`/`DAL.Prenda` concretos en vez de sus interfaces — el mismo problema del hallazgo #2, pero en la fachada.

**2. ✅ RESUELTO (2026-09-11) — `CambioPlanHandler`/`BajaSuscripcionHandler` reciben DAL concretos en vez de interfaces (`DAL.PlanSuscripcion`/`DAL.Prenda` en vez de `IPlanSuscripcionDAL`/`IPrendaDAL`, que ya existen y se usan en `BLL.Pedido`)** — por eso no se pueden instanciar con un Fake y quedan 100% sin test, incluida la validación de "plan insuficiente para el stock en uso" al cambiar de plan. A diferencia de otros gaps del proyecto, este no tiene ningún comentario que lo justifique.
*Solución aplicada:* ambos constructores ahora reciben `IPlanSuscripcionDAL`/`IPrendaDAL`; se agregaron 8 tests nuevos (`CambioPlan_*` x5, `Baja_*` x3) cubriendo el caso feliz, plan requerido/inexistente/insuficiente, con/sin prendas en uso, y delegación al sucesor.

**3. ✅ RESUELTO (2026-09-11) — `BLL.Pedido.MarcarEntregado` — 0 tests**, a diferencia de todas las demás transiciones de estado del mismo agregado (que sí tienen caso feliz + rechazo).
*Solución aplicada:* se agregaron `MarcarEntregado_PedidoDespachado_Entrega` y `MarcarEntregado_PedidoNoDespachado_LanzaEntregarEstado`.

**4. ✅ RESUELTO (2026-09-11) — `BLL.Pedido.ObtenerClienteValidado` — la rama de "suscripción pausada" no tiene ningún test**, mientras las otras dos guardas de negocio (pago suspendido, vencimiento) sí están cubiertas explícitamente, incluido el orden relativo entre ellas.
*Solución aplicada:* se agregó `CrearPedido_SuscripcionPausada_LanzaSuscripcionPausada`.

**5. ✅ RESUELTO (2026-09-11) — (Del bloque de Seguridad/Permisos) `BLL/RecuperacionAdmin.cs` — cero tests pese a estar diseñada explícitamente para testearse** (constructor por interfaces, doc-comment lo dice) — la ruta de recuperación de cuentas de Administrador bloqueadas no tiene ni el Fake necesario.
*Solución aplicada:* se creó `Tests/Fakes/FakeClaveRecuperacionDAL.cs` (no existía) y `Tests/RecuperacionAdminTests.cs` con 14 tests: las 7 ramas de `DesbloquearConClave` (campos vacíos, usuario inexistente, no-admin, no-bloqueado, sin claves, clave incorrecta/ya consumida, éxito con consumo de uso único) + normalización de mayúsculas, `ValidarClaveMaestra` (vacía/null/sin configurar) y `ContarClavesDisponibles`.

### 🟡 Media

6. ✅ RESUELTO (2026-09-11) — `BLL.Cliente.ActivarSuscripcion`/rama de cambio de plan en `Modificar` — sin tests porque `dalPlan` no está inyectado por constructor (única clase "core" con este gap; documentado honestamente en el propio archivo de test, pero evitable).
*Solución aplicada:* `dalPlan` pasa de `DAL.PlanSuscripcion` concreto fijo a `IPlanSuscripcionDAL` inyectable por constructor (mismo patrón que `BLL.Renovacion`); 4 tests nuevos cubren ambos métodos.
7. ✅ RESUELTO (2026-09-11) — `BLL.Promocion.Modificar` — 0 tests, en un archivo por lo demás ejemplar en cobertura.
*Solución aplicada:* se agregaron 4 tests (`Modificar_EnRevisionContableConDatosValidos_ActualizaYRegistra`, `Modificar_PromocionVigente_LanzaModificarEstado`, `Modificar_BajaSolicitada_LanzaModificarEstado`, `Modificar_AmbosDestinos_LanzaDestinoInvalido`).
8. ✅ RESUELTO (2026-09-11) — `BLL.Pedido` — catches silenciosos de Lista de Espera nunca ejercitados por ningún test (no se sabe si el fail-open es intencional o esconde un bug).
*Solución aplicada:* nuevo `FakeListaEsperaService` configurable para lanzar; 2 tests verifican explícitamente que `CrearPedido` no se bloquea si Lista de Espera falla al verificar o al cerrar una reserva — el fail-open es intencional y ahora queda documentado por test.
9. ✅ RESUELTO (2026-09-11) — `RestaurarOperacion`/`DesCancelar` — solo se testea el camino de error, nunca el camino feliz (incluido un swap de campos sutil en el re-registro de historial).
*Solución aplicada:* se agregó el camino feliz de ambos; el de `RestaurarOperacion` verifica explícitamente el swap Anterior/Nuevo al re-registrar el historial. `FakePedidoHistorialDAL`/`FakePedidoDAL` ahora capturan lo que reciben (antes solo contaban invocaciones).
10. ✅ RESUELTO (2026-09-11) — (Del bloque de Seguridad/Permisos) `Seguridad/SessionManager.TienePermiso` sin test directo (solo se testea la función pura que recibe los booleanos ya resueltos, no el método real que hace el bypass de admin/comparación case-insensitive).
*Solución aplicada:* 3 tests nuevos contra el método real (bypass de Administrador, permiso presente/ausente con comparación case-insensitive).
11. ✅ RESUELTO (2026-09-11) — (Del bloque de Seguridad/Permisos) `BLL/Familia.cs` — todos los métodos de escritura (`GuardarAsignacionRol`, `CrearRol`, `EliminarRol`, `ValidarSinCiclo`) sin test — solo la función pura `SistemaConservaGestion` está bien cubierta, no la orquestación real que la dispara en producción.
*Solución aplicada:* `FakePermisoDAL` ganó árbol configurable + espías; 10 tests nuevos cubren `CrearRol`, `EliminarRol` y `AgregarComponente`/`ValidarSinCiclo` (ciclo directo e indirecto) de punta a punta, más las validaciones de `GuardarAsignacionRol` previas al guard sistémico (rol inexistente, autobloqueo). Deliberadamente sin cobertura del camino feliz de `GuardarAsignacionRol`/`EliminarRol`/`QuitarComponente`: los tres pasan por `ExigirSistemaConservaGestion`, que internamente instancia `new Usuario()` (BLL/DAL concretos, no inyectables) para enumerar usuarios reales — ejercitarlos de punta a punta pegaría contra la BD real con un resultado dependiente de su contenido. Ese acoplamiento en sí queda como hallazgo residual, no resuelto.
12. ✅ RESUELTO (2026-09-11) — (Del bloque de Seguridad/Permisos) `Encriptador.ValidarContrasena` — rama `clave_sinespecial` nunca ejercitada por ningún test.
*Solución aplicada:* se agregó `CambiarClavePropia_ClaveSinEspecial_LanzaClaveSinEspecial_SinTocarDAL`.
13. ✅ RESUELTO (2026-09-11) — (Del bloque de Seguridad/Permisos) `Usuario.Claves.ResetearClave`/`SolicitarRecuperacionClave`/`ValidarCredencialesAdmin` sin ningún test.
*Solución aplicada:* 12 tests nuevos. `ValidarCredencialesAdmin` queda con cobertura completa (6 ramas, sin dependencia de BD). `ResetearClave`/`SolicitarRecuperacionClave` cubren el guard/camino determinístico previo a cualquier side-effect; el resto de ambos métodos escribe contra Bitácora/VersionUsuario/disco de forma no inyectable, mismo motivo de exclusión que el ítem #11.

### 🟢 Baja

14. Verificado explícitamente: los `Assert.IsNotNull` en `UsuarioAbmTests`/`AnalisisMantenimientoTests`/`BackupTests` son guardas previas a un assert específico, no asserts débiles — no son un hallazgo, se deja constancia para no re-marcarlos en una futura pasada superficial.
15. (Del bloque de Seguridad/Permisos) `FakeUsuarioDAL`/`FakePermisoDAL` tienen varios miembros de escritura con cuerpo vacío — correcto mientras nadie los necesite, pero limita ampliar la cobertura del hallazgo #11 sin tocarlos primero.

> **Nota de cobertura:** esta sección no llegó a cubrir con el mismo detalle Contratación, los 5
> Análisis, Reportes, y Prenda/CargoPrenda/ListaEspera, ni un barrido dedicado de duplicación entre
> Fakes/estilo de tests en esas áreas — el agente que la produjo se quedó sin tiempo/presupuesto de
> API a mitad de esa segunda pasada. Si se quiere cerrar esa cobertura, es la única sección de las
> 10 que ameritaría una pasada adicional.

---

## 10. Scan transversal de consistencia (todo el repo)

Búsquedas sobre el conjunto completo (no archivo por archivo) para detectar patrones que solo se ven mirando el todo.

**1. ✅ RESUELTO (2026-09-11) — Helper de traducción reinventado ~84 veces con ~24 nombres distintos** (`T`, `Tx`, `T_ce`, `T_dlg`, `T_c`, `T_d`, `T_e`...) en 48 archivos — mismo cuerpo `ContainsKey ? Texto : fallback` copiado sin centralizar en ningún helper compartido (ej. en `FormBase`).
*Solución aplicada:* se agregó `FormBase.Tr(clave, fallback, args = null)` (wrapper de `Traductor.Resolver` con el idioma activo) como único punto de traducción, y se migraron los ~121 usos encontrados (más de los ~84 estimados originalmente) en los 45 formularios que heredan `FormBase` — 14 commits, uno por lote, cada uno verificado con build + tests. Varios `RellenarCombo*`/`TraducirX` perdieron parámetros de idioma/diccionario que ya no necesitaban. Se preservaron intactos, a propósito, los dos closures locales de `MostrarEstadoActual()` en `CobroSuscripcionForm`/`RenovacionSuscripcionForm`, que cachean un único fetch del diccionario para 5 sub-resoluciones (optimización deliberada y documentada, no duplicación). *Alcance no cubierto:* 11 formularios que heredan `Form` directo en vez de `FormBase` (`Login`, `Menu`, `MiPerfilForm`, `ConfirmarAdminForm`, `DesbloqueoEmergenciaForm`, `DiagnosticoIntegridadForm`, `ExploradorCompositeForm`, `RecuperacionEspejoForm`, `CambioClaveObligatorioForm`, `CargoPrendaDialog`, `CambioEstadoDialog`) no pueden usar el helper heredado sin además migrarlos a `FormBase` — cambio de mayor alcance, fuera de esta pasada. Los 3 `Exportadores` y `Program.cs` tampoco son `Form` y quedan igual sin tocar.

**2. Convención de campos privados dividida por capa, no aleatoria.** DAL: 100% sin `_` (17 archivos). GUI: 100% con `_` (27 archivos, salvo `Login.cs`, única excepción). BLL: mezcla ambas **dentro de la misma clase** (11 archivos) según si el campo es inyectado (`dalPedido`) o lazy (`_listaEsperaLazy`) — regla informal nunca documentada.

**3. DAL nunca usa la jerarquía de excepciones propia del proyecto.** 79 `throw new Exception(...)` genéricos en 14 archivos (12 en DAL) — pese a existir `BE.AppException`/`LoginException`/`SesionException`, usadas consistentemente en BLL/GUI para distinguir error esperado de inesperado (`GUI/FormBase.cs:113-118` chequea explícitamente `is BE.AppException`).

**4. `MessageBox.Show` — 82 ocurrencias en 38 archivos, 0 con literal directo salvo un solo archivo**: `GUI/Program.cs` (mensajes de arranque, antes de que exista un `Form` con su propio helper de traducción) y puntualmente `Login.cs:347` — el resto del proyecto (517 llamados a `T(`/`Tx(`) tiene una disciplina fuerte de nunca mostrar un literal sin traductor.

**5. ✅ RESUELTO (2026-09-11) — Catches genéricos: 273 en 84 archivos, pero 0 completamente vacíos** — contradice la hipótesis inicial de "catches que tragan en silencio". El problema real es otro: **al menos 3 implementaciones independientes de `MostrarError(Exception ex)`** (`FormBase`, `RecuperacionEspejoForm`, `ReporteJornadaForm`) con lógica ligeramente distinta, en vez de una sola función compartida.
*Solución aplicada:* `RecuperacionEspejoForm` pasó a heredar `FormBase` (antes heredaba `Form` directo) y usa la implementación heredada — de paso corrigió el mismo gap de exponer `ex.Message` crudo sin auditar que ya se había resuelto en otros formularios. `ReporteJornadaForm.MostrarError(ex, claveTitulo, tituloFallback)` es una variante legítima (necesita título de `MessageBox` personalizado por pantalla) que se mantiene, pero ahora delega en `FormBase.RegistrarExcepcion` (pasado de `private` a `protected`) para el caso de excepción inesperada, con el mismo criterio que la base.

**6. ✅ RESUELTO (2026-09-11) — Constante `DiasSinActividadParaRiesgo` duplicada con valores distintos** entre `EstrategiaVencimientoInactividad.cs` (30) y `EstrategiaInactividadPura.cs` (60) — mismo nombre, misma semántica aparente, sin comentario que explique la diferencia (podría ser deliberado, pero no está documentado como tal).
*Solución aplicada:* verificado que la diferencia es intencional (una estrategia usa la inactividad como señal secundaria sobre un vencimiento ya próximo, la otra como única señal y necesita un umbral más largo para evitar falsos positivos); se documentó con un comentario cruzado en ambas clases en vez de unificar los valores.

**7. Interfaces BLL: 17/17 con su única clase implementadora, 1 con nombre inconsistente** (`IRecomendacionService` debería ser `IRecomendacionPrendasService`). 17 clases BLL de nivel similar no implementan ninguna interfaz, sin una regla escrita que distinga cuáles la necesitan.

**8. Métodos más largos del repo (candidatos a god-method):** `Traductor.InferirFormulario` (~131 líneas), `Configuracion.VerificarIntegridadDV` (~129), `DashboardForm.ConstruirElementosCondicionales` (~110), `RecuperacionIntegridad.Diagnosticar` (~105), `Usuario.Autenticacion.Login` (~99), entre otros — mezclan validación, orquestación de DAL y side-effects en un solo bloque, a diferencia de los servicios que sí delegan en Manejadores/Estrategias.

**9. `var` vs. tipo explícito: 1549 vs. 3 — consistente, no es un problema real.**

**10. TODO/FIXME/HACK: 0 ocurrencias en todo el repo** — señal positiva de limpieza, confirmada con números en vez de asumida.

**11. Idioma de nombres de método: 180 verbos en español vs. 1 sola excepción** (`Acceso.GetInstance()`, nombre técnico de patrón, no de dominio) — sin mezcla real de idioma en nomenclatura de negocio.

**12. ✅ RESUELTO (2026-09-11) — Color de marca `210,100,135` hardcodeado en 25 archivos**, confirmado (ver también §8 GUI-Infraestructura #7, donde está la solución aplicada).

**13. Pares Designer/Form: 0 huérfanos reales** de 56 archivos `.Designer.cs` (los 2 sin par son `Resources.Designer.cs`/`Settings.Designer.cs`, scaffolding estándar, no formularios).

---

## Cómo usar este documento

Los hallazgos 🔴 Alta son los que más vale la pena mirar primero (afectan dinero, seguridad, o
integridad de datos). Los 🟡 Media son deuda técnica real pero no bugs activos hoy. Los 🟢 Baja son
mejoras de calidad/consistencia, útiles para una entrega prolija pero no urgentes. El scan
transversal (§10) es el que más conviene mirar si el objetivo es mejorar la *consistencia* general
del código (vs. corregir bugs puntuales) — son los patrones que un lector técnico (o un jurado)
nota de inmediato al recorrer varios archivos seguidos.
