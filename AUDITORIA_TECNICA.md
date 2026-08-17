# Auditoría técnica — WardrobeFlow

Auditoría de las 5 capas (BE, BLL, DAL, GUI, Seguridad/Servicios) más BD y Tests, hecha el 2026-08-17 después de cerrar el Bloque 3 (PdN8/9/11/12/13). Cada hallazgo tiene archivo:línea, qué pasa y una sugerencia de arreglo. Marcá `[x]` a medida que los vayas resolviendo.

No se tocó ningún archivo durante esta auditoría — es solo lectura. Los hallazgos ya corregidos en la misma sesión (crash de DNI legacy sin cifrar en PdN10, drift de umbrales de urgencia en 4 de los 5 dashboards) **no** están listados acá porque ya están resueltos y pusheados.

---

## 🔴 Prioridad alta (bugs reales, corregir primero)

- [ ] **`Tests/UsuarioCambioClaveTests.cs:61`** — El test espera `err.bll.usuario.clave_invalida`, pero esa clave no existe en ningún lado del código. `Seguridad/Encriptador.cs` solo define `clave_corta` / `clave_sinnumero` / `clave_sinespecial`, y con la contraseña de prueba `"abc"` (3 caracteres) el chequeo de longitud dispara primero, así que la excepción real es `clave_corta`. **Este test falla en cualquier build limpio, ya lo vengo arrastrando.**
  Arreglo: cambiar la clave esperada a `"err.bll.usuario.clave_corta"`, o si la intención era testear la rama de "sin número/especial", usar una contraseña de 8+ caracteres sin dígitos ni especiales (ej. `"abcdefgh"`).

- [ ] **`Seguridad/SessionManager.cs:41`** (`TienePermiso`) — Compara `p.NombreMenu == nombreMenu` con **case-sensitive** (comparación ordinal por defecto), mientras que `BLL/MenuVisibilidad.cs` usa `StringComparer.OrdinalIgnoreCase` explícitamente (con su propio test `PatentesCaseInsensitive`) y SQL Server compara sin distinguir mayúsculas por default. Este método es el segundo gate de autorización real (lo llaman `BLL/BLLHelper.cs:17` y `BLL/PermisosAccion.cs:78-79`) — una diferencia de mayúsculas en cualquier punto de la cadena podría **denegar silenciosamente** un permiso que en el resto del sistema sí se resuelve bien.
  Arreglo: `p.NombreMenu.Equals(nombreMenu, StringComparison.OrdinalIgnoreCase)`.

- [ ] **`DAL/Renovacion.cs` (`Resolver`) y su equivalente en `DAL/Cobro.cs`** — Escriben el historial (`HistorialRenovacion`/`HistorialCobro`) y el estado real del cliente (`DAL.Cliente.Modificar`, llamado aparte desde el handler de la cadena en BLL) como **dos round-trips independientes**, sin envolver en `acceso.EjecutarTransaccion`. Si la conexión se cae o el proceso crashea entre medio, queda el historial de auditoría desincronizado del estado real de la suscripción — en un flujo que toca cobros, no es menor. `DAL/Pedido.cs` (`Alta`, `Cancelar`, `DesCancelar`) ya usa el patrón correcto con `EjecutarTransaccion`; aplicar lo mismo acá.

- [ ] **`GUI/DashboardForm.cs:568-582`** (panel "Mis Tareas Pendientes") — Se me pasó este archivo cuando centralicé los umbrales de urgencia esta sesión. Todavía calcula "días" a mano y compara con números mágicos propios, y encima **quedó desincronizado**: usa `dias >= 3` binario para mantenimiento, mientras que el resto de los dashboards (ya corregidos) usan el criterio de 3 niveles `> 7 Urgente / >= 2 Normal / si no Reciente`. La parte de pedidos (`dias >= 2`) da la casualidad de que coincide con `EsUrgentePorAntiguedad`, pero solo por casualidad.
  Arreglo: reemplazar por `m.NivelUrgencia` / `p.EsUrgentePorAntiguedad` igual que en `DashboardVendedor`/`DashboardOperador`/`DashboardSupervisor`/`DashboardControlStock`.

- [ ] **`DAL/Cliente.cs` (`BuscarIdPorDni`, ~línea 121-134)** — Si `Encriptador.TryDesencriptar` falla para el DNI guardado de un cliente (cae al texto cifrado tal cual), ese registro **nunca puede volver a matchear** contra un DNI nuevo tipeado en texto plano en la comparación `string.Equals` — o sea, `ExisteDNI`/`ExisteDNIParaOtro` da falso negativo para esa persona y se podría dar de alta un duplicado sin que el sistema lo note. Esto explica directamente el hallazgo incidental de la clienta "Lucia Fernandez" (su DNI no descifra con el `key.dat` actual — lo vi en una captura de pantalla de prueba esta sesión). No identifiqué la causa raíz todavía (no hay evidencia en el código de que se haya regenerado `key.dat` o de una segunda pasada de cifrado) — para confirmar si es dato corrupto o una clave distinta, habría que mirar directo en la BD: `SELECT DNI FROM Cliente WHERE Nombre='Lucia'` y probar `Encriptador.Desencriptar` manualmente sobre ese valor.

- [ ] **`DAL/Backup.cs:233-245`** (`RestaurarBackup`) — El bloque `SET SINGLE_USER` → `RESTORE DATABASE` → `SET MULTI_USER` no tiene manejo de error. Si el `RESTORE` falla después de que `SINGLE_USER` ya se aplicó (disco lleno, corrupción que `VERIFYONLY` no detectó), la base queda **bloqueada en modo single-user permanentemente**, sin ningún camino de código para recuperarla.
  Arreglo: envolver en `TRY/CATCH` de T-SQL que fuerce `SET MULTI_USER` en el `CATCH`, o al menos documentar el comando manual de recuperación.

---

## 🟡 Prioridad media (deuda técnica, no bugs activos)

- [ ] **Patrón Command (PdN3) es decorativo, no se usa realmente.** `BLL/Comandos/CancelacionCommand.cs`, `DevolucionCommand.cs` e `InvocadorPedido.cs` están bien implementados (textbook Command), pero **nada en la GUI los invoca** — `PedidosVenta.cs`/`PedidosRealizados.cs` llaman directo a `BLL.Pedido.Cancelar()`/`.RegistrarDevolucion()`. Solo los ejercitan sus propios tests. Dos caminos: (a) conectar la GUI para que arme y despache `PedidoCommand` de verdad, para que `InvocadorPedido` batchee operaciones reales, o (b) si no vale la pena el esfuerzo para la entrega, documentar que PdN3 usa BLL directo y el Command vive solo como demostración del patrón — así no queda como algo "roto" si alguien lo revisa.

- [ ] **Cadena de Responsabilidad sin guarda de null.** `ManejadorRenovacion`/`ManejadorCobro`.`_sucesor` no se valida antes de `_sucesor.Procesar(...)` (`BLL/Manejadores/VerificarVencimientoHandler.cs:31`, `DetectarCobroHandler.cs:29`, etc.). Hoy `BLL/Renovacion.cs`/`BLL/Cobro.cs` arman bien la cadena así que nunca explota, pero si algún día se agrega o reordena un handler y se olvida un link, el error sale como `NullReferenceException` genérica en vez de un mensaje claro de "cadena mal configurada".

- [ ] **`BajaSuscripcionHandler`/`SuspenderHandler` (eslabones terminales) no validan `contexto.Decision`.** Ejecutan sin chequear qué decisión llegó — hoy es seguro porque la GUI solo manda 3 valores válidos por radio buttons, pero si `DecisionRenovacion`/`DecisionCobro` gana un miembro nuevo en el enum, cae silenciosamente en "Baja"/"Suspendido" en vez de fallar fuerte.

- [ ] **Sin tests directos para `BLL/Cliente.cs` ni `BLL/Pedido.cs`.** Son de las clases más críticas del sistema (cupo de suscripción, alta de pedido, paso de DNI) y no tienen ni un test unitario propio — `Tests/ComandoPedidoTests.cs` solo prueba el wrapper Command contra un `IPedidoService` mockeado, nunca la implementación real (validación de cupo, `CrearPedido`, `CalcularNivelUrgencia`, la lógica anti-TOCTOU de la transacción).

- [ ] **Las 5 clases BLL nuevas del Bloque 3** (`AnalisisEscasez`, `AnalisisMantenimiento`, `AnalisisRotacion`, `RecomendacionPrendas`, `ReporteVentasVendedor`) tampoco tienen tests todavía — esperable porque se agregaron esta sesión, lo marco para que no se pierda.

---

## 🟢 Prioridad baja / mejoras a futuro

- [ ] **`BLL/Familia.cs:305,315`** — `CrearPatente()`/`CrearFamilia()`, marcados `[Obsolete]`, sin ningún caller en toda la app ni en tests. Se pueden borrar directamente en vez de mantenerlos "por compatibilidad".

- [ ] **`BE/Cliente.cs:74`** (`DiasHastaVencimiento()`) devuelve `int.MaxValue` como sentinela de "sin fecha". Hoy solo se usa para mostrar en pantalla (`GUI/NuevoPedidoForm.cs:139,252`) así que no rompe nada, pero es una trampa para el próximo que escriba `if (dias < 30)` sin chequear antes si hay fecha. Cambiar el retorno a `int?` no cuesta nada en los 2 call-sites actuales.

- [ ] **`BD/04_Diagnostico_Limpieza_Nodos_Permiso.sql`** es una herramienta manual de un solo uso (se edita `@Nombre` y se corre a mano en SSMS), pero está numerada junto a los scripts de despliegue automático. No rompe nada si se corre en lote (no hace nada con el placeholder), pero conviene un header bien visible aclarando que es manual, o sacarla de la secuencia numerada.

- [ ] **`DAL/Cliente.cs`** — La verificación de DNI único (`BuscarIdPorDni`) descifra el DNI de **todos** los clientes activos en memoria en cada Alta/Modificar — es la contrapartida inevitable de usar AES con IV aleatorio para un campo que necesita unicidad (ya está documentado así en el propio comentario del código), pero no escala. Una columna adicional con un HMAC determinístico del DNI, usada *solo* para el lookup de unicidad (nunca mostrada ni descifrada), permitiría un `WHERE` indexado en SQL en vez de traer y descifrar todo.

- [ ] **Índices faltantes en las columnas nuevas de Bloque 3.** `DAL/Pedido.cs`: `ObtenerCantidadPedidosPorPrenda` agrupa por `PedidoPrenda.IdPrenda`, `ObtenerEstadisticasPorEmpleado` agrupa por `Pedido.IdEmpleado` — ninguna de las dos tiene índice propio (solo las PK). Irrelevante a la escala actual de datos de desarrollo; si el dataset crece, conviene un índice no-clustered en cada una.

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

Se dividió en 3 auditorías paralelas de solo-lectura (BE+BLL, DAL+BD+Seguridad, GUI+Servicios+Tests), cada una leyendo el código real archivo por archivo, sin asumir nada del README ni de comentarios. Los hallazgos de mayor severidad de esta lista (el de `SessionManager.cs`, el de `DashboardForm.cs`, el de `DAL/Renovacion.cs` y el del test que falla) los volví a verificar yo directamente leyendo el código antes de escribirlos acá, así que están confirmados dos veces, no solo reportados por el agente.
