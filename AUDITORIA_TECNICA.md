# Auditoría técnica — WardrobeFlow

Auditoría de las 5 capas (BE, BLL, DAL, GUI, Seguridad/Servicios) más BD y Tests, hecha el 2026-08-17 después de cerrar el Bloque 3 (PdN8/9/11/12/13). Cada hallazgo tiene archivo:línea, qué pasa y una sugerencia de arreglo. Marcá `[x]` a medida que los vayas resolviendo.

Los hallazgos ya corregidos antes de esta auditoría (crash de DNI legacy sin cifrar en PdN10, drift de umbrales de urgencia en 4 de los 5 dashboards) **no** están listados acá porque ya estaban resueltos y pusheados. **Las 3 secciones (alta, media, baja) ya están resueltas** — queda como registro de qué se encontró y cómo se solucionó, y de las 2 decisiones que quedaron en tus manos (HMAC-DNI: no implementar ahora; el caso Lucia/Lucía Fernández: sin decidir).

---

## 🔴 Prioridad alta — ✅ TODO RESUELTO

- [x] **`Tests/UsuarioCambioClaveTests.cs:61`** — El test esperaba `err.bll.usuario.clave_invalida`, que no existe en ningún lado del código; con `"abc"` (3 caracteres) la excepción real es `clave_corta`.
  **Solución aplicada:** se corrigió el test a `clave_corta` y se agregó un segundo test nuevo (`CambiarClavePropia_ClaveSinNumero_LanzaClaveSinNumero_SinTocarDAL`) para no perder cobertura de la rama "sin número". 173 tests, 171 correctas, 2 omitidas (preexistentes, no relacionadas), 0 fallando.

- [x] **`Seguridad/SessionManager.cs:41`** (`TienePermiso`) — Comparaba `NombreMenu` case-sensitive, inconsistente con `BLL/MenuVisibilidad.cs` (que usa `OrdinalIgnoreCase`).
  **Solución aplicada:** cambiado a `string.Equals(p.NombreMenu, nombreMenu, StringComparison.OrdinalIgnoreCase)`.

- [x] **`DAL/Renovacion.cs`/`DAL/Cobro.cs` — escrituras no transaccionales.** `dalCliente.Modificar(cliente)` + `dalRenovacion.Alta(...)`/`dalCobro.Alta(...)` eran dos round-trips independientes en los 5 manejadores que tocan ambas tablas (`IntentarRenovarHandler`, `CambioPlanHandler`, `BajaSuscripcionHandler`, `ProcesarPagoHandler`, `AplicarGraciaHandler`).
  **Solución aplicada:** se agregaron `IClienteDAL.EjecutarTransaccion`/`.ModificarEnTx`, `IRenovacionDAL.AltaEnTx` e `ICobroDAL.AltaEnTx` (mismo patrón que ya usaba `DAL/Pedido.cs` internamente), y los 5 manejadores ahora envuelven el UPDATE de Cliente + el INSERT del historial en una única transacción (`RecalcularDV()` se sigue llamando después del commit, igual que antes). Se actualizaron los 3 dobles de prueba (`FakeClienteDAL`, `FakeRenovacionDAL`, `FakeCobroDAL`) para implementar los métodos nuevos delegando a la lógica en memoria existente — todos los tests de la cadena de Renovación/Cobro siguen pasando sin cambios en sus asserts.

- [x] **`GUI/DashboardForm.cs:568-582`** (panel "Mis Tareas Pendientes") — Umbral de mantenimiento desincronizado (`dias >= 3` binario) del resto de los dashboards (`> 7 Urgente / >= 2 Normal`, ya centralizado).
  **Solución aplicada:** reemplazado por `m.NivelUrgencia`/`p.EsUrgentePorAntiguedad`, igual que los otros 4 dashboards.

- [x] **`DAL/Cliente.cs` (`BuscarIdPorDni`) — DNI sin descifrar rompe la detección de duplicados.** **Investigado a fondo y causa raíz confirmada:** escribí un test directo contra `Seguridad.Encriptador.Desencriptar` sobre los 9 clientes "demo" (IDs 1-9, incluida "Lucia Fernandez") — **los 9 fallan** con `CryptographicException: relleno inválido`, no solo uno. Eso descarta "dato corrupto puntual": es un **`key.dat` de este entorno de desarrollo que no coincide con el que se usó para cifrar esos 9 DNIs** en algún momento anterior (posiblemente `bin/Debug` se limpió y `CargarOCrearClave()` generó una clave nueva, o esos datos vinieron de otro entorno). Es el riesgo que el propio comentario de `Encriptador.cs` ya advertía ("Eliminar key.dat hace que los DNI cifrados existentes sean irrecuperables") — **no es un bug de código, es un dato de desarrollo huérfano**, y no es recuperable sin la clave original.
  Además encontré, cruzando los datos, que **"Martín Gómez" (28999111) y "Sofía Rossi" (35444555) están duplicados de verdad** en la BD de desarrollo (IDs 12/1013 y 13/1014) — pero vienen de que el seed de `BD/01_Crear_BaseDeDatos.sql`/`02_Actualizar_BaseDeDatos.sql` se volvió a correr después de que esas filas se borraran a mano (su guarda `WHERE NOT EXISTS (... Nombre = v.Nombre ...)` no las encontró porque ya no estaban) — **no es la app bypasseando `ExisteDNI`**, es una consecuencia de administrar la BD de desarrollo a mano.
  **Solución aplicada:** el riesgo de código real (que un DNI sin descifrar quede invisible para la detección de duplicados) sigue existiendo — no tiene arreglo sin la clave correcta — pero ahora queda **registrado** en vez de fallar en silencio: `BuscarIdPorDni` loguea un `Trace.TraceWarning` con el ID del cliente afectado cada vez que esto pasa, así que la próxima vez que un caso así aparezca, se va a poder diagnosticar sin tener que hacer forense manual como esta vez.
  **Pendiente para vos (no es código, es housekeeping de datos):** si estos 9 clientes demo no son necesarios, lo más simple es borrarlos y re-sembrarlos (`DELETE FROM Cliente WHERE IdCliente IN (1,2,3,4,5,6,7,8,9)` + volver a correr el bloque de seed) para que los DNI queden descifrables con la clave actual. Los duplicados de Martín Gómez/Sofía Rossi también se pueden limpiar a mano si molestan (no rompen nada funcionalmente, `Activo` sigue en 1 para ambas copias).

- [x] **`DAL/Backup.cs:233-245`** (`RestaurarBackup`) — Sin manejo de error entre `SINGLE_USER` y `RESTORE`; si el `RESTORE` fallaba, la base quedaba bloqueada en single-user para siempre.
  **Solución aplicada:** el `RESTORE` ahora va en `BEGIN TRY/CATCH` de T-SQL; si falla, el `CATCH` fuerza `SET MULTI_USER` antes de relanzar el error con `THROW`, así el caller en C# sigue viendo la excepción real pero la base nunca queda bloqueada.

---

## 🟡 Prioridad media (deuda técnica, no bugs activos)

- [x] **Patrón Command (PdN3) era decorativo, no se usaba realmente.** `BLL/Comandos/CancelacionCommand.cs`, `DevolucionCommand.cs` e `InvocadorPedido.cs` estaban bien implementados (textbook Command), pero nada en la GUI los invocaba.
  **Solución aplicada:** `GUI/PedidosVenta.cs` (cancelación) y `GUI/PedidosRealizados.cs` (devolución) ahora arman el `PedidoCommand` correspondiente y lo despachan a través de `InvocadorPedido`, en vez de llamar `BLL.Pedido` directo. Sin lógica nueva en la GUI: `Ejecutar()` de cada Command sigue siendo una sola línea que delega a `BLL.Pedido`, la GUI solo empaqueta la petición (mismo rol de "Cliente" del patrón).

- [x] **Cadena de Responsabilidad sin guarda de null.** `ManejadorRenovacion`/`ManejadorCobro`.`_sucesor` no se validaba antes de `_sucesor.Procesar(...)`.
  **Solución aplicada:** `_sucesor` pasó a privado en ambas clases base, con un método `DelegarASucesor(contexto)` que valida y tira `InvalidOperationException` con un mensaje claro ("Cadena de Renovación/Cobro mal configurada: X no tiene un sucesor asignado") en vez de una `NullReferenceException` genérica. Los 6 handlers no-terminales (`VerificarVencimientoHandler`, `IntentarRenovarHandler`, `CambioPlanHandler`, `DetectarCobroHandler`, `ProcesarPagoHandler`, `AplicarGraciaHandler`) actualizados para usarlo.

- [x] **`BajaSuscripcionHandler`/`SuspenderHandler` (eslabones terminales) no validan `contexto.Decision`.** Investigado: son catch-all terminales *a propósito* (mismo criterio que `DirectorGeneral` del ejemplo de cátedra — el último eslabón resuelve sin condición), así que agregarles una validación de rechazo iría contra su propio diseño documentado.
  **Solución aplicada:** en vez de cambiar el comportamiento, se documentó el riesgo real justo donde haría falta actuar — `DecisionRenovacion`/`DecisionCobro` (los enums) ahora tienen un comentario `⚠` explicando que agregar un valor nuevo requiere insertar su propio Handler ANTES del eslabón terminal correspondiente, o se va a tratar silenciosamente como Baja/Suspendido.

- [x] **Sin tests directos para `BLL/Cliente.cs`.**
  **Solución aplicada:** `Tests/ClienteTests.cs` (19 tests nuevos) cubre `Alta` (todas las validaciones: nombre/apellido/DNI requeridos, formato y longitud de DNI, DNI numérico, plan requerido, mayoría de edad, DNI duplicado, alta feliz), `Baja` (bloqueo con prendas en uso), `Modificar` (DNI duplicado para otro cliente, actualización sin cambio de plan) y `ObtenerEstadoComercial` (las 4 ramas: sin plan, vencida, supera límite, caso normal). Al escribirlos apareció un problema de infraestructura: `BLL.Cliente` tiene un campo `dalPlan = new DAL.PlanSuscripcion()` que se inicializa siempre (aunque no se use esa rama), y eso dispara `Acceso.GetInstance()` — que explota si no encuentra la entrada `WardrobeFlowDB` en el config del proceso. El proyecto `Tests` no tenía `App.config`. **Se agregó `Tests/App.config`** con la misma connection string que `GUI/App.config` (nunca se llega a abrir de verdad — solo hace falta que la entrada exista) — esto destraba testear CUALQUIER BLL que tenga un campo DAL concreto sin inyectar, no solo `BLL.Cliente`.
  **No cubierto (documentado en el propio archivo de test):** `ActivarSuscripcion()` y la rama de "cambio de plan" de `Modificar()` llaman `dalPlan.ObtenerPorId(...)` directo — ese campo no está inyectado (no hay `IPlanSuscripcionDAL`), así que esas dos rutas necesitan una conexión real. Mismo patrón de acoplamiento que tiene `BLL.Pedido` completo (ver abajo).

- [x] **Sin tests directos para `BLL/Pedido.cs`.** Era la clase más crítica del sistema sin ni un test propio, y no se podía testear porque hardcodeaba sus 5 dependencias DAL directo en el constructor.
  **Solución aplicada** (con tu confirmación explícita, ya que tocaba la clase más sensible de la app): se crearon 4 interfaces nuevas (`IPedidoDAL`, `IEmpleadoDAL`, `IPlanSuscripcionDAL`, `IPedidoHistorialDAL`, mismo criterio que `IClienteDAL`) y se refactorizó `BLL.Pedido` para recibir las 5 dependencias por constructor — el constructor sin parámetros sigue existiendo y usa los DAL reales, así que ningún caller (GUI, otras BLL) cambió. `Tests/PedidoTests.cs` (23 tests nuevos) cubre toda la cadena de validación de `CrearPedido` (sin prendas, cliente inexistente, sin plan, suspendido por pago — verificando que ese chequeo vaya ANTES que el de vencimiento genérico, como documenta el propio código —, suscripción vencida, pedido despachado bloqueando uno nuevo, límite del plan, prenda no disponible, empleado sin vínculo, sin sesión, alta feliz), las guardas de estado de `Despachar`/`RegistrarDevolucion`/`Cancelar`/`DesCancelar`, las 4 ramas de `CalcularNivelUrgencia` (lógica pura) y el caso vacío de `RestaurarOperacion`. 214 tests en total, 0 fallando.

- [x] **Las 5 clases BLL nuevas del Bloque 3** (`AnalisisEscasez`, `AnalisisMantenimiento`, `AnalisisRotacion`, `RecomendacionPrendas`, `ReporteVentasVendedor`) tampoco tenían tests.
  **Solución aplicada:** mismo problema estructural que `BLL.Pedido` — las 5 tomaban `DAL.Prenda`/`DAL.Pedido`/`DAL.MantenimientoPrenda` concretos. Se agregaron 2 interfaces más (`IPrendaDAL`, `IMantenimientoPrendaDAL`) y se refactorizaron las 5 clases para recibir sus dependencias por constructor (constructor sin parámetros intacto). 22 tests nuevos repartidos en 5 archivos, cubriendo los umbrales de clasificación de cada una (escasez por debajo/igual al umbral, mantenimiento por cantidad/duración/ambos, rotación por antigüedad/demanda, recomendación por categoría/color/ambos, y el desempeño por vendedor). 236 tests en total, 0 fallando.

---

## 🟢 Prioridad baja / mejoras a futuro — ✅ TODO RESUELTO O DECIDIDO

- [x] **`BLL/Familia.cs:305,315`** — `CrearPatente()`/`CrearFamilia()`, marcados `[Obsolete]`, sin ningún caller.
  **Solución aplicada:** se borraron directamente (se verificó cero callers en toda la app antes de borrar).

- [x] **`BE/Cliente.cs:74`** (`DiasHastaVencimiento()`) devolvía `int.MaxValue` como sentinela de "sin fecha".
  **Solución aplicada:** ahora devuelve `int?` (`null` si no hay fecha). Los 2 call-sites (`GUI/NuevoPedidoForm.cs:139,252`) ya estaban guardados por `SuscripcionProximaAVencer()`/`estado.SuscripcionProximaAVencer` así que el cambio no altera ningún comportamiento, solo hace explícito en el tipo lo que antes dependía de que el caller recordara chequear el sentinela. `BE/EstadoComercialCliente.DiasHastaVencimiento` (el DTO) también pasó a `int?` para no perder la garantía en el camino.

- [x] **`BD/04_Diagnostico_Limpieza_Nodos_Permiso.sql`** mezclado con la secuencia de despliegue automático.
  **Solución aplicada:** se agregó un header bien visible (`⚠ HERRAMIENTA MANUAL`) al principio del archivo, aclarando que no es parte de la secuencia automática y que requiere editar el placeholder antes de correrlo.

- [x] **`DAL/Cliente.cs` — HMAC-DNI para no descifrar todo en memoria.** Decisión tuya: **no implementar ahora** — es una optimización de escala sin problema real al volumen de datos actual, y tocar el esquema de cifrado agrega riesgo sin necesidad urgente. Queda como mejora a futuro si el dataset crece de verdad.

- [x] **Índices faltantes en `PedidoPrenda.IdPrenda`/`Pedido.IdEmpleado`.** Al ir a agregarlos se descubrió que **el hallazgo era incorrecto**: ambos índices (`IX_Pedido_IdEmpleado`, `IX_PedidoPrenda_IdPrenda`) ya existen en `BD/01_Crear_BaseDeDatos.sql` (sección "ÍNDICES NO-CLUSTERED", líneas ~1131 y ~1135), agregados en algún momento anterior a esta sesión. No hacía falta ningún cambio.

- [x] **Housekeeping de datos de desarrollo.** Decisión tuya: limpiar solo lo que no tenía nada atado (7 de los 9 clientes con DNI irrecuperable SÍ tenían pedido y prendas reales — esos quedaron como estaban, a propósito). Se borraron: Ignacio León (DNI irrecuperable, sin pedidos/prendas) y las 2 copias duplicadas de Martín Gómez/Sofía Rossi (se conservaron los originales, IDs 12 y 13). Antes de borrar hubo que anular 2 referencias en `BitacoraNegocio.IdCliente` (columna nullable — se preservó el texto del log, solo se soltó la referencia) porque una FK lo bloqueaba. Se volvió a correr `01_Crear_BaseDeDatos.sql` completo (idempotente) para intentar re-sembrar — no re-creó a Ignacio León porque **nunca fue parte del script de seed**, se había dado de alta a mano desde la app en algún momento anterior; no hay pérdida real ya que no aportaba nada como dato de prueba (DNI roto, sin pedidos). Sin duplicados de nombre+apellido restantes, excepto un caso que quedó anotado pero sin tocar: "Lucia Fernandez" (ID 6, DNI roto, con pedido/prendas — no tocada) y "Lucía Fernández" (ID 1012, funciona bien, sin nada atado) probablemente son la misma persona cargada dos veces con/sin tilde — fuera del alcance aprobado, queda para que decidas vos si las unificás.

---

## ✅ Verificado limpio (no hace falta tocar nada)

Para que quede constancia de lo que SÍ se revisó a fondo y no tiene problemas, no solo lo que falla:

- **Patrones GoF**: State (`BE/Estados/*`), Strategy (`BLL/Estrategias/*`), Builder (`BE/Builders/*`), Composite (`BE/Componente.cs` + `BLL/Familia.cs`, con guardas de ciclo reales y sin bug de profundidad) y Memento (`BE/Usuario.cs` + `BLL/CuidadorHistorial.cs`) siguen su forma de libro correctamente.
- **Consistencia de roles y permisos**: `BLL/MenuVisibilidad.cs` ↔ `GUI/Menu.cs` ↔ todos los `BD/*.sql` coinciden 1:1, sin patentes huérfanas en ningún sentido.
- **Inyección SQL**: cero consultas concatenadas sin parametrizar en todo `DAL/`. Los 2 únicos lugares con SQL dinámico (`DAL/DigitoVerificador.cs:141-147`, `DAL/Backup.cs:185-186`) usan una whitelist validada contra constantes internas, no input de usuario.
- **Suscripción/desuscripción de `IIdiomaObserver`**: los 35 formularios que se suscriben también se desuscriben correctamente al cerrar — no hay observers colgados.
- **Cobertura de traducciones**: muestreo de ~190 claves reales en 8 formularios distintos, 100% presentes en ES/EN/RU/PT.
- **Red de seguridad de excepciones**: `GUI/Program.cs` atrapa cualquier excepción no manejada de un event handler (la registra en Bitácora y muestra un diálogo genérico) — un `NullReferenceException` suelto en un handler de GUI no tira abajo la app entera.
- **`GUI/Exportacion` vs `Servicios/Exportacion`**: no hay duplicación, son capas distintas con responsabilidades distintas (el segundo es un utilitario de bajo nivel que el primero consume).

---

## Cómo lo armé

Se dividió en 3 auditorías paralelas de solo-lectura (BE+BLL, DAL+BD+Seguridad, GUI+Servicios+Tests), cada una leyendo el código real archivo por archivo, sin asumir nada del README ni de comentarios. Los hallazgos de mayor severidad de la lista original (el de `SessionManager.cs`, el de `DashboardForm.cs`, el de `DAL/Renovacion.cs` y el del test que falla) los volví a verificar yo directamente leyendo el código antes de escribirlos, así que estaban confirmados dos veces, no solo reportados por el agente. El de `BuscarIdPorDni`/Lucía Fernández lo terminé de confirmar corriendo un descifrado real contra los 9 DNIs sospechosos.
