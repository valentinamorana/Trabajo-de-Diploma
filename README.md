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
| Encriptado | PBKDF2-SHA256 (contraseñas) · AES-128 + PBKDF2 (backups .wfbak) |

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
Instalador/                               Script de Inno Setup, DbInstaller (cliente SQL embebido)
                                          y credenciales iniciales. El .exe se genera en
                                          Instalador/Salida/ y no se versiona
docs/NEGOCIO_Y_PROCESOS.md               Documento de referencia: reglas y procesos de negocio, roles y arquitectura
docs/MAPA_DE_NAVEGACION.md               Mapa de menús, formularios, procesos y roles
WardrobeFlow.slnx                         Solución de Visual Studio
```

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
| PN02 | Comercialización de la Suscripción | Vendedor (crea la contratación) / **Caja** (cobra y formaliza) | Rol `Caja` separado de Vendedor (quien vende no cobra). El cobro es un *claim* atómico (`UPDATE ... AND Estado = Pendiente`): dos sesiones de Caja no pueden cobrar la misma contratación, y si la activación falla se compensa reabriéndola. Rechaza planes dados de baja y muestra el importe con el descuento y el comprobante emitido |
| PN03 | Métricas, Promociones y Toma de Decisiones | GerenteComercial (sugiere) / **AdministracionComercial** (crea y reformula) / **Contabilidad** (aprueba) | Circuito **reporte → sugerencia → Administración → Contabilidad → Vigente → se aplica al cobro**. Gerencia puede cargar ideas detectadas por los reportes de rotación y abandono (`BLL.AnalisisPromociones`). Regla de NUULY (5.1): **un solo descuento por ciclo**, el mayor entre la promoción vigente del plan y el crédito por referido (`BE.PoliticaDescuento`); el crédito no usado se acumula. Las promociones por categoría son informativas |
| PN04 | Inspección de Devolución | Deposito ("Depósito") | Sin rol nuevo, sin aprobador: reingresa sin cargo o se da de baja registrando **antes** el cargo por el daño irreparable (decisión propia, a diferencia de NUULY que no cobra daños). En Limpieza → Baja solo desde la Inspección (la BLL lo exige). Al registrarse la devolución las prendas dejan de estar En Uso y la cuenta del cliente se desbloquea (PN01) |

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

Los 6 reportes son de solo lectura, con exportación a PDF/CSV (Factory Method, `GUI.Exportacion`); cada uno tiene su propia patente (`mnuAnalisisXxx`), agrupados bajo el menú "Analítica de Negocio".

---

## Roles del sistema

| Rol | Permisos | Jerarquía (Composite) |
|-----|----------|-----------------------|
| **Administrador** | Acceso total | — (acceso total) |
| **Auditor** | Solo Bitácora / Auditoría | rol plano, transversal (no participa de ningún PN/PdN) |
| **Vendedor** | Prendas, Clientes, Planes, Renovación, Cobro, Realizar Ventas, Contratación (PN02), Recomendación de Prendas (PdN13) | rol base comercial |
| **GerenteComercial** | lo de Vendedor + Ver Pedidos Realizados, Ventas por Vendedor (PdN8), Análisis de Abandono (PdN10) | ⊃ Vendedor |
| **OperadorLogistico** | Ver Pedidos Realizados (despacho) | rol base inventario |
| **Deposito** *(PN01, PN04)* | Ver Prendas + Gestionar Stock (mantenimiento), Inspección de Devolución (PN04) | rol base inventario |
| **GerenteInventario** | lo de ambos operadores + Rotación/Mantenimiento/Escasez (PdN9/11/12) | ⊃ OperadorLogistico + Deposito |
| **Caja** *(PN02)* | Contrataciones pendientes de pago: cobrar, registrar intento fallido | rol simple, sin herencia |
| **AdministracionComercial** *(PN03)* | Gestionar Promociones: crear desde sugerencia o manual, desactivar, resolver bajas | rol simple, sin herencia |
| **Contabilidad** *(PN03)* | Revisión Contable de Promociones: aprobar o rechazar | rol simple, sin herencia |

Los permisos se resuelven recursivamente desde el árbol Composite (tabla `PermisoRelacion`) y se cargan en sesión al hacer login. Se gestionan desde **Administrar → Perfiles y Permisos**. Usuarios demo para los 3 roles nuevos: `caja`, `admcomercial`, `contable` (clave `usuario1!`, sección "Base de datos" abajo).

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
| **Nueva Contratación / Contrataciones Pendientes** *(PN02)* | Vendedor da de alta la contratación (plan + modalidad); Caja la cobra y formaliza la suscripción, o registra intentos fallidos hasta cancelarla |
| **Promociones** *(PN03)* | Sugerir (GerenteComercial) → Gestionar/crear (AdministracionComercial) → Revisión Contable, aprobar o rechazar (Contabilidad) → consultar vigentes y sugerir baja (Vendedor) |
| **Inspección de Devolución** *(PN04)* | Cola de prendas `EnLimpieza`: reingresa sin cargo o se da de baja cobrando el precio de reposición; también permite reportar una prenda `EnUso` como perdida sin esperar la devolución |
| **Analítica de Negocio** *(Bloque 3, PdN8-13)* | 6 reportes de decisión comercial de solo lectura (Ventas por Vendedor, Rotación, Abandono, Mantenimiento, Escasez, Recomendación de Prendas), exportables a PDF/CSV |

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

**Instalación nueva → un solo script.** Ejecutar desde SSMS (o `sqlcmd -f 65001`, ver comentario en el archivo):

```
BD/00_Instalacion_Completa.sql   -- Crea WardrobeFlowDB completa: estructura, datos semilla
                                  -- y los 4 procesos de negocio (PN01-PN04). Idempotente:
                                  -- se puede volver a ejecutar sin duplicar ni romper nada.
```

Es el único archivo de esquema que se edita. Los scripts individuales que introdujeron cada módulo históricamente (`01`, `03`, `05`, `06`, `08` a `20`) ya no están en el repo — todo su contenido está absorbido en `00`, que es la única fuente de verdad; si hace falta ver cómo se introdujo una funcionalidad puntual, buscarlo en el historial de git en vez de en un archivo aparte. El instalador (`Instalador/WardrobeFlow_Setup.iss`) usa este único script.

**Es el único script de la carpeta `BD/`.** Incluye también los **datos de prueba** de todos los procesos (sección 21: clientes en distintos estados de suscripción, prendas en cada estado, pedidos, contrataciones pendientes, promociones, etc.), así que la base queda lista para probar apenas se instala — el instalador (`Instalador/`) ejecuta este mismo archivo. Sobre una base ya instalada con una versión anterior también es seguro volver a ejecutarlo: migra el rol `OperadorDeInventario` a `Deposito` y retira los roles viejos.

**Notas de los módulos** (para ubicarlos dentro de `00_Instalacion_Completa.sql` — los números de sección adentro del archivo coinciden con estos, y son el orden histórico en el que se agregaron):
- **Lista de Espera (mejora opcional)** — sección 16 en `00`; el resto del sistema funciona sin ella (`BLL.Prenda`/`BLL.Pedido` degradan a su comportamiento anterior si la tabla `ListaEspera` no existe).
- **Comercialización de la suscripción (PN02)** — sección 17 en `00`. Crea el rol `Caja` (separado de Vendedor) y sus patentes; hay que asignarle el rol `Caja` a algún usuario desde Administrar → Usuarios para poder probar el módulo (o usar el usuario demo `caja`).
- **Métricas, promociones y toma de decisiones (PN03)** — sección 18 en `00`. Crea los roles `AdministracionComercial` y `Contabilidad` (Gerencia y Vendedor reusan `GerenteComercial`/`Vendedor` ya existentes); hay usuarios demo (`admcomercial`, `contable`) para probar el módulo sin dar de alta nada a mano.
- **Inspección de Devolución (PN04)** — sección 19 en `00`. Sin rol nuevo: reusa el rol `Deposito` (el "Depósito" de PN01). Lógica alineada a Nuuly (binaria, sin aprobador): reingresa sin cargo o se da de baja cobrando el precio de reposición (`BLL.CargoPrenda.RegistrarCargo`, existente desde Bloque 1).
- **Aplicación de promociones al cobro e integridad (PN02/PN03/PN04)** — sección 20b en `00`. Agrega `Importe`, `DescuentoAplicado` e `IdPromocion` a `Contratacion` (lo que realmente se cobró), los `CHECK` de `CargoPrenda.Monto` e `IntentosPago`, las restricciones únicas (una contratación pendiente por cliente, una anotación activa por prenda y cliente) y los índices de las tablas nuevas.
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

Abrir `WardrobeFlow.slnx` en Visual Studio, compilar la solución (8 proyectos: BE, Seguridad, DAL, Servicios, BLL, GUI, Tests e Instalador/DbInstaller) y correr los tests desde el Explorador de pruebas (suite MSTest sobre `Tests.dll`).

### Generar el instalador

1. Compilar la solución en modo **Release** (y `Instalador/DbInstaller` en Release).
2. Ejecutar `Instalador/compilar-y-firmar.ps1`: compila `WardrobeFlow_Setup.iss` con Inno Setup y firma el `.exe` con SignTool. El certificado (`wardrobeflow.pfx`) se busca en `%USERPROFILE%` y no se versiona; si no está, el instalador queda compilado sin firmar.
3. Sale un único archivo: `Instalador/Salida/Instalador_WardrobeFlow_V1.exe`, con la aplicación, la base de datos y los datos de prueba adentro.
