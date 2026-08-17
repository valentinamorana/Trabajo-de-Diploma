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
| Encriptado | PBKDF2-SHA256 (contraseñas) · AES-128-CBC (datos sensibles) |

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

## Roles del sistema

| Rol | Permisos | Jerarquía (Composite) |
|-----|----------|-----------------------|
| **Administrador** | Acceso total: Inventario, Ventas, Administrar, Bitácora, Perfiles, Backup | — (acceso total) |
| **Auditor** | Solo Bitácora / Auditoría | rol plano |
| **Vendedor** | Prendas, Clientes, Planes, Renovación, Cobro, Realizar Ventas | rol base comercial |
| **GerenteComercial** | lo de Vendedor + Ver Pedidos Realizados | ⊃ Vendedor |
| **OperadorLogistico** | Ver Pedidos Realizados (despacho) | rol base inventario |
| **OperadorDeInventario** | Ver Prendas + Gestionar Stock (mantenimiento) | rol base inventario |
| **GerenteInventario** | lo de ambos operadores + Categorías/Outfits | ⊃ OperadorLogistico + OperadorDeInventario |

Los permisos se resuelven recursivamente desde el árbol Composite (tabla `PermisoRelacion`) y se cargan en sesión al hacer login. Se gestionan desde **Administrar → Perfiles y Permisos**.

---

## Módulos

| Módulo | Descripción |
|--------|-------------|
| **Login / Logout** | Autenticación con bloqueo de login progresivo (1/5/15/60 min tras 3 intentos), claves de emergencia de autodesbloqueo y bloqueo de sesión en memoria |
| **Usuarios** (operaciones de cuenta) | Alta de empleados, reset de contraseña, desbloqueo, archivado (baja lógica) y purga diferida; contraseñas generadas automáticamente (RNG criptográfico) |
| **Administración de Usuarios** (ABM de datos) | Edición de datos administrativos no sensibles, búsqueda/filtros, cambio de rol y Historial de Cambios |
| **Perfiles y Permisos** | Árbol Composite por rol; ABM de roles y asignación de permisos en tiempo real; mapeo de controles por patente |
| **Mi Perfil** | Preferencias de UI por usuario (idioma, tipografía, tamaño, tema, formato de fecha) |
| **Clientes** | ABM de suscriptores con plan, vencimiento, modalidad de cobro y columna `en uso / límite`; alta activa la suscripción vía **Builder** |
| **Prendas** | Inventario con estados (Disponible · EnUso · EnLimpieza · Baja) gestionados vía **State** |
| **Planes de Suscripción** | ABM de planes; bloquea desactivación con clientes asignados; bloquea asignación con límite menor al stock en uso |
| **Pedidos de Venta** | Creación de pedidos respetando límite del plan; cancelación y devolución vía **Command** |
| **Pedidos Realizados** | Ciclo post-venta: Despachar → Marcar Entregado → Registrar Devolución |
| **Renovación de Suscripción** | Verificación de vencimiento, renovación, cambio de plan o baja resueltos por una cadena de manejadores (**Chain of Responsibility**) |
| **Cobro de Suscripción** | Detección de cobro pendiente, confirmación de pago (extiende la vigencia), período de gracia ante un pago fallido y suspensión de nuevos pedidos si el plazo vence sin regularizar — cadena de manejadores (**Chain of Responsibility**) |
| **Bitácora** | Registro de eventos del sistema y de negocio con filtros, criticidad y exportación a PDF |
| **Historial de Cambios** | Cambios de datos administrativos por usuario a nivel de campo, con rollback (Memento) |
| **Idiomas** | ABM de traducciones directamente en la BD |
| **Dashboard** | Panel de control personalizado por rol, con auto-refresh y carga asíncrona |
| **Backup / Restauración** | Copias cifradas con contraseña (`.wfbak`, AES+PBKDF2) con verificación de integridad previa |
| **Reporte de Jornada** | Exportación PDF de actividad del día filtrable por rol |
| **Diagnóstico de Integridad** | Visualización y reparación asistida de filas con DVH/DVV corruptos |
| **Lista de Espera** *(mejora opcional)* | Un cliente se anota por una prenda `EnUso`; al liberarse, queda reservada exclusivamente para él por 48hs (visible solo para ese cliente en Nuevo Pedido) antes de volver a estar disponible para cualquiera |

---

## Mejoras opcionales (no requeridas por la cátedra)

Surgida de comparar WardrobeFlow con el TP de un compañero de cursada (ExperienceHub,
que tiene Lista de Espera para sus reservas). No es un PdN de la idea de negocio ni un
requisito de ninguna entrega — es un diferencial de producto que reutiliza el patrón
State ya entregado (PdN2/PdN4) sin tocarlo.

| Módulo | Resumen |
|--------|---------|
| **Lista de Espera de Prendas** | Matchea por prenda específica (mismo `IdPrenda`, no por categoría). Al liberarse, `BLL.Prenda.CambiarEstado` dispara `BLL.ListaEspera.NotificarSiCorresponde`, que reserva la fila `Pendiente` más antigua (FIFO) por `BLL.ListaEspera.HORAS_RESERVA` (48hs). Mientras la reserva está vigente, `BLL.Pedido` la bloquea para cualquier otro cliente (`err.bll.pedido.prenda_reservada`) y la cierra sola (`Convertida`) al crear el pedido del cliente correcto. Si nadie la retira a tiempo, vuelve a estar disponible para cualquiera por simple comparación de fecha — sin job en background, mismo criterio que `Cliente.FechaLimiteGracia` (PdN6). Ver `BD/16_Lista_Espera.sql`, `BLL/ListaEspera.cs`, `GUI/ListaEsperaForm.cs`. |

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
- Datos sensibles (DNI) encriptados con AES-128-CBC
- Bloqueo de login progresivo (1 → 5 → 15 → 60 min) con claves de emergencia de un solo uso
- Handler global de excepciones no controladas: registra el detalle técnico en bitácora y muestra un mensaje genérico al usuario
- Dígitos verificadores (DVH por fila + DVV por tabla) sobre `Usuario`, `Cliente` y `Empleado`
- Backups cifrados con contraseña (AES-128 + PBKDF2) y Clave Maestra de Recuperación opcional

---

## Multiidioma

Soporta **Español · English · Русский · Português** con cambio dinámico en tiempo de ejecución. Las traducciones se almacenan en la tabla `Traduccion`; un corpus embebido (`traducciones.tsv`) actúa como fallback por clave.

---

## Instalación y configuración

### Requisitos

- Visual Studio 2022 (o superior)
- .NET Framework 4.7.2
- SQL Server (local o remoto)

### Base de datos

Ejecutar desde SSMS, en orden, los scripts de `BD/` (todos idempotentes):

```
BD/01_Crear_BaseDeDatos.sql               -- Instalación nueva: estructura + datos semilla
BD/02_Actualizar_BaseDeDatos.sql          -- BD existente: migraciones incrementales
BD/03_Permisos_Granulares.sql
BD/04_Diagnostico_Limpieza_Nodos_Permiso.sql
BD/05_Renovacion_Suscripcion.sql          -- Tabla y permisos del módulo de Renovación (PdN5)
BD/06_Rediseno_Menu.sql                   -- Actualiza el texto "Bitácora" → "Analítica" en BD existentes
BD/07_Reset_Perfiles_Permisos.sql         -- Reconstruye desde cero los permisos de los 7 roles reales
BD/08_Cobro_Pago.sql                      -- Tabla y permisos del módulo de Cobro de Suscripción (PdN6)
BD/09_Analisis_Abandono.sql               -- Permisos del módulo de Análisis de Abandono (PdN10) — sin tablas nuevas
BD/16_Lista_Espera.sql                    -- Lista de Espera de prendas (mejora opcional, no requerida por la cátedra)
```

- **Instalación nueva** → ejecutar `01_Crear_BaseDeDatos.sql` y luego `08_Cobro_Pago.sql` y `09_Analisis_Abandono.sql`.
- **Actualizar una BD existente** → ejecutar `02` a `09` en orden.
- **BD con el árbol de permisos desincronizado** (un rol no ve lo que debería) → ejecutar `07_Reset_Perfiles_Permisos.sql`. Reescribe las patentes de los 7 roles reales al estado correcto — hacer un backup antes si hay permisos customizados a mano.
- **Lista de Espera (mejora opcional)** → ejecutar `16_Lista_Espera.sql` en cualquier momento; el resto del sistema funciona sin él (`BLL.Prenda`/`BLL.Pedido` degradan a su comportamiento anterior si la tabla `ListaEspera` no existe).

### Cadena de conexión

Configurar en `GUI/App.config`:

```xml
<connectionStrings>
  <add name="WardrobeFlowDB"
       connectionString="Data Source=.;Initial Catalog=WardrobeFlowDB;Integrated Security=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Pasos

```
1. Clonar el repositorio
2. Abrir WardrobeFlow.slnx en Visual Studio
3. Configurar la cadena de conexión en GUI/App.config
4. Ejecutar los scripts SQL en orden
5. Compilar y ejecutar GUI como proyecto de inicio
```

En el primer arranque el sistema seedea automáticamente las tablas de traducciones, idiomas, permisos y el usuario `admin2` de respaldo.

### Build y tests

```powershell
.\build-and-test.ps1
```

Compila las 7 capas (BE, Seguridad, DAL, Servicios, BLL, GUI, Tests) y corre la suite de MSTest sobre `Tests.dll`.
