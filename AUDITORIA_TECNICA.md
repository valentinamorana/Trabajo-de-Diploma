# Auditoría técnica — WardrobeFlow

Auditoría de las 5 capas (BE, BLL, DAL, GUI, Seguridad/Servicios) más BD y Tests, hecha el 2026-08-17 después de cerrar el Bloque 3 (PdN8/9/11/12/13). Cada hallazgo tiene archivo:línea, qué pasa y una sugerencia de arreglo. Marcá `[x]` a medida que los vayas resolviendo.

Los hallazgos ya corregidos antes de esta auditoría (crash de DNI legacy sin cifrar en PdN10, drift de umbrales de urgencia en 4 de los 5 dashboards) **no** están listados acá porque ya estaban resueltos y pusheados. **Toda la sección de Prioridad Alta ya está resuelta y pusheada** (commit siguiente al de esta auditoría) — queda como registro de qué se encontró y cómo se solucionó.

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

- [ ] **Sin tests directos para `BLL/Cliente.cs` ni `BLL/Pedido.cs`.** Son de las clases más críticas del sistema (cupo de suscripción, alta de pedido, paso de DNI) y no tienen ni un test unitario propio — `Tests/ComandoPedidoTests.cs` solo prueba el wrapper Command contra un `IPedidoService` mockeado, nunca la implementación real (validación de cupo, `CrearPedido`, `CalcularNivelUrgencia`, la lógica anti-TOCTOU de la transacción).

- [ ] **Las 5 clases BLL nuevas del Bloque 3** (`AnalisisEscasez`, `AnalisisMantenimiento`, `AnalisisRotacion`, `RecomendacionPrendas`, `ReporteVentasVendedor`) tampoco tienen tests todavía — esperable porque se agregaron esta sesión, lo marco para que no se pierda.

---

## 🟢 Prioridad baja / mejoras a futuro (sin resolver todavía)

- [ ] **`BLL/Familia.cs:305,315`** — `CrearPatente()`/`CrearFamilia()`, marcados `[Obsolete]`, sin ningún caller en toda la app ni en tests. Se pueden borrar directamente en vez de mantenerlos "por compatibilidad".

- [ ] **`BE/Cliente.cs:74`** (`DiasHastaVencimiento()`) devuelve `int.MaxValue` como sentinela de "sin fecha". Hoy solo se usa para mostrar en pantalla (`GUI/NuevoPedidoForm.cs:139,252`) así que no rompe nada, pero es una trampa para el próximo que escriba `if (dias < 30)` sin chequear antes si hay fecha. Cambiar el retorno a `int?` no cuesta nada en los 2 call-sites actuales.

- [ ] **`BD/04_Diagnostico_Limpieza_Nodos_Permiso.sql`** es una herramienta manual de un solo uso (se edita `@Nombre` y se corre a mano en SSMS), pero está numerada junto a los scripts de despliegue automático. No rompe nada si se corre en lote (no hace nada con el placeholder), pero conviene un header bien visible aclarando que es manual, o sacarla de la secuencia numerada.

- [ ] **`DAL/Cliente.cs`** — La verificación de DNI único (`BuscarIdPorDni`) descifra el DNI de **todos** los clientes activos en memoria en cada Alta/Modificar — es la contrapartida inevitable de usar AES con IV aleatorio para un campo que necesita unicidad (ya está documentado así en el propio comentario del código), pero no escala. Una columna adicional con un HMAC determinístico del DNI, usada *solo* para el lookup de unicidad (nunca mostrada ni descifrada), permitiría un `WHERE` indexado en SQL en vez de traer y descifrar todo.

- [ ] **Índices faltantes en las columnas nuevas de Bloque 3.** `DAL/Pedido.cs`: `ObtenerCantidadPedidosPorPrenda` agrupa por `PedidoPrenda.IdPrenda`, `ObtenerEstadisticasPorEmpleado` agrupa por `Pedido.IdEmpleado` — ninguna de las dos tiene índice propio (solo las PK). Irrelevante a la escala actual de datos de desarrollo; si el dataset crece, conviene un índice no-clustered en cada una.

- [ ] **Housekeeping de datos de desarrollo** (no es código, ver detalle arriba en el ítem de `BuscarIdPorDni`): 9 clientes demo con DNI irrecuperable por `key.dat` desincronizado, y 2 clientes duplicados (Martín Gómez, Sofía Rossi) por haber corrido el seed script dos veces tras un borrado manual. Ninguno de los dos rompe la app, pero conviene limpiarlos antes de una demo/entrega para que no llamen la atención sin necesidad.

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
